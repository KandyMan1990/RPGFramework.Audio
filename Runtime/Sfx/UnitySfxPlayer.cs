using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Sfx
{
    public class UnitySfxPlayer : ISfxPlayer, IAudioUpdatable
    {
        private const string SFX_BUS_NAME         = "Sfx";
        private const string SFX_REVERB_SEND      = "SfxReverbSend";
        private const string SFX_GAME_OBJECT_NAME = "SfxPlayer";

        private static readonly string[] VOLUME_BUS_NAMES = { SFX_BUS_NAME, SFX_REVERB_SEND };

        private readonly ISfxPlayer                   m_This;
        private readonly List<ISfxReference>          m_SfxReferences;
        private readonly List<ISfxReference>          m_UpdateBuffer;
        private readonly List<ISfxAsset>              m_Loading;
        private readonly Dictionary<ulong, int>       m_Preloads;
        private readonly Dictionary<ulong, ISfxAsset> m_Preloaded;

        private ISfxAssetProvider m_SfxAssetProvider;
        private AudioSource[]     m_CurrentSources;
        private AudioMixerGroup[] m_StemMixerGroups;
        private AudioMixer        m_AudioMixer;
        private bool              m_Disposed;
        private string[]          m_SendParameterNames;
        private ISfxReference[]   m_VoiceOwners;
        private GameObject        m_PlayerObject;
        private AudioUpdateDriver m_UpdateDriver;
        private int               m_Stops;
        private int               m_PreloadRequests;

        public UnitySfxPlayer()
        {
            m_SfxReferences = new List<ISfxReference>();
            m_UpdateBuffer  = new List<ISfxReference>();
            m_Loading       = new List<ISfxAsset>();
            m_Preloads      = new Dictionary<ulong, int>();
            m_Preloaded     = new Dictionary<ulong, ISfxAsset>();
            m_This          = this;
        }

        async Task<ISfxReference> ISfxPlayer.PlayAsync(ulong nameHash)
        {
            int       stops    = m_Stops;
            ISfxAsset sfxAsset = await m_SfxAssetProvider.AcquireAsync(nameHash);

            m_Loading.Add(sfxAsset);

            try
            {
                await LoadClipsAsync(sfxAsset);
            }
            finally
            {
                m_Loading.Remove(sfxAsset);
            }

            if (stops != m_Stops || m_Disposed)
            {
                UnloadUnusedClips(sfxAsset);
                m_SfxAssetProvider.Release(nameHash);

                ISfxReference stopped = StoppedReference(nameHash, sfxAsset);

                return stopped;
            }

            ISfxReference playing = ScheduleSfx(nameHash, sfxAsset, 0f);

            return playing;
        }

        Task ISfxPlayer.PreloadAsync(IReadOnlyList<ulong> nameHashes)
        {
            Task[] preloads = new Task[nameHashes.Count];

            for (int i = 0; i < nameHashes.Count; i++)
            {
                preloads[i] = PreloadAsync(nameHashes[i]);
            }

            return Task.WhenAll(preloads);
        }

        void ISfxPlayer.Unload(IReadOnlyList<ulong> nameHashes)
        {
            for (int i = 0; i < nameHashes.Count; i++)
            {
                ulong nameHash = nameHashes[i];

                if (!m_Preloads.Remove(nameHash))
                {
                    continue;
                }

                // Still loading, it lets go itself when it finishes.
                if (!m_Preloaded.Remove(nameHash, out ISfxAsset sfxAsset))
                {
                    continue;
                }

                UnloadUnusedClips(sfxAsset);
                m_SfxAssetProvider.Release(nameHash);
            }
        }

        void ISfxPlayer.Pause(ISfxReference sfxReference)
        {
            sfxReference.Pause();
        }

        void ISfxPlayer.PauseAll()
        {
            m_Stops++;

            for (int i = 0; i < m_SfxReferences.Count; i++)
            {
                ISfxReference sfxReference = m_SfxReferences[i];

                m_This.Pause(sfxReference);
            }
        }

        void ISfxPlayer.Resume(ISfxReference sfxReference)
        {
            sfxReference.Resume();
        }

        void ISfxPlayer.ResumeAll()
        {
            for (int i = 0; i < m_SfxReferences.Count; i++)
            {
                ISfxReference sfxReference = m_SfxReferences[i];

                m_This.Resume(sfxReference);
            }
        }

        void ISfxPlayer.Stop(ISfxReference sfxReference)
        {
            if (!m_SfxReferences.Remove(sfxReference))
            {
                return;
            }

            sfxReference.Stop();

            ReleaseReference(sfxReference);
        }

        void ISfxPlayer.StopAll()
        {
            m_Stops++;

            for (int i = m_SfxReferences.Count - 1; i >= 0; i--)
            {
                ISfxReference sfxReference = m_SfxReferences[i];
                m_This.Stop(sfxReference);
            }

            m_SfxReferences.Clear();
        }

        void ISfxPlayer.SetSfxAssetProvider(ISfxAssetProvider provider)
        {
            m_SfxAssetProvider = provider;
        }

        void ISfxPlayer.SetStemMixerGroups(AudioMixerGroup[] groups)
        {
            m_StemMixerGroups = groups;
            m_AudioMixer      = m_StemMixerGroups[0].audioMixer;

            m_CurrentSources     = new AudioSource[m_StemMixerGroups.Length];
            m_SendParameterNames = new string[m_StemMixerGroups.Length];
            m_VoiceOwners        = new ISfxReference[m_StemMixerGroups.Length];

            DestroyPlayerObject();

            m_PlayerObject = new GameObject(SFX_GAME_OBJECT_NAME);
            UnityEngine.Object.DontDestroyOnLoad(m_PlayerObject);

            m_UpdateDriver = AudioUpdateDriver.Attach(m_PlayerObject, this);

            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                GameObject go = new GameObject(m_StemMixerGroups[i].name);
                go.transform.parent                       = m_PlayerObject.transform;
                m_CurrentSources[i]                       = go.AddComponent<AudioSource>();
                m_CurrentSources[i].outputAudioMixerGroup = m_StemMixerGroups[i];

                m_SendParameterNames[i] = $"{m_StemMixerGroups[i].name}_Send";
            }
        }

        float ISfxPlayer.GetVolume()
        {
            return AudioUtils.GetVolume(m_AudioMixer, SFX_BUS_NAME);
        }

        void ISfxPlayer.SetVolume(float percent)
        {
            AudioUtils.SetVolume(m_AudioMixer, VOLUME_BUS_NAMES, percent);
        }

        void IAudioUpdatable.Update()
        {
            if (m_SfxReferences.Count == 0)
            {
                return;
            }

            m_UpdateBuffer.Clear();
            m_UpdateBuffer.AddRange(m_SfxReferences);

            for (int i = 0; i < m_UpdateBuffer.Count; i++)
            {
                ISfxReference sfxReference = m_UpdateBuffer[i];

                if (!m_SfxReferences.Contains(sfxReference))
                {
                    continue;
                }

                sfxReference.CheckForEventToRaise();
                sfxReference.CheckForLoop();
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task PreloadAsync(ulong nameHash)
        {
            if (m_Preloads.ContainsKey(nameHash))
            {
                return;
            }

            int request = ++m_PreloadRequests;
            m_Preloads.Add(nameHash, request);

            ISfxAsset sfxAsset = await m_SfxAssetProvider.AcquireAsync(nameHash);

            // Unloaded while it loaded, and perhaps preloaded again since, by a request that keeps it instead.
            if (!m_Preloads.TryGetValue(nameHash, out int current) || current != request)
            {
                m_SfxAssetProvider.Release(nameHash);

                return;
            }

            m_Preloaded.Add(nameHash, sfxAsset);

            await LoadClipsAsync(sfxAsset);
        }

        private static async Task LoadClipsAsync(ISfxAsset sfxAsset)
        {
            for (int i = 0; i < sfxAsset.Tracks.Count; i++)
            {
                EnsureLoaded(sfxAsset.Tracks[i].Clip);
            }

            for (int i = 0; i < sfxAsset.Tracks.Count; i++)
            {
                AudioClip clip = sfxAsset.Tracks[i].Clip;

                while (clip.loadState == AudioDataLoadState.Loading)
                {
                    await Awaitable.NextFrameAsync();
                }

                if (clip.loadState == AudioDataLoadState.Failed)
                {
                    Debug.LogError($"{nameof(UnitySfxPlayer)}::{nameof(LoadClipsAsync)} Clip [{clip.name}] failed to load. That stem will be silent");
                }
            }
        }

        private static ISfxReference StoppedReference(ulong nameHash, ISfxAsset sfxAsset)
        {
            ISfxReference stopped = new SfxReference(nameHash, Array.Empty<AudioSource>(), sfxAsset, AudioSettings.dspTime, _ => { });

            stopped.Stop();

            return stopped;
        }

        private ISfxReference ScheduleSfx(ulong nameHash, ISfxAsset sfxAsset, float startTime)
        {
            int stemCount = sfxAsset.Tracks.Count;

            if (stemCount > m_CurrentSources.Length)
            {
                stemCount = m_CurrentSources.Length;
            }

            while (CountFreeVoices() < stemCount)
            {
                EvictOldestSfx();
            }

            double        scheduledStartTime    = AudioSettings.dspTime + Time.deltaTime;
            AudioSource[] audioSourceReferences = new AudioSource[stemCount];

            int voiceIndex = 0;

            for (int i = 0; i < stemCount; i++)
            {
                while (m_VoiceOwners[voiceIndex] != null)
                {
                    voiceIndex++;
                }

                int         voice  = voiceIndex;
                AudioSource source = m_CurrentSources[voice];

                audioSourceReferences[i] = source;
                voiceIndex++;

                source.clip                  = sfxAsset.Tracks[i].Clip;
                source.playOnAwake           = false;
                source.loop                  = false;
                source.volume                = 1f;
                source.time                  = startTime;
                source.outputAudioMixerGroup = m_StemMixerGroups[voice];

                float sendLevel = AudioUtils.PercentToDb(sfxAsset.Tracks[i].ReverbSendLevel);
                m_AudioMixer.SetFloat(m_SendParameterNames[voice], sendLevel);

                source.PlayScheduled(scheduledStartTime);
            }

            SfxReference sfxRef = new SfxReference(nameHash, audioSourceReferences, sfxAsset, scheduledStartTime, RemoveSfxReference);

            TakeOwnership(audioSourceReferences, sfxRef);

            m_SfxReferences.Add(sfxRef);

            return sfxRef;
        }

        private int CountFreeVoices()
        {
            int free = 0;

            for (int i = 0; i < m_VoiceOwners.Length; i++)
            {
                if (m_VoiceOwners[i] == null)
                {
                    free++;
                }
            }

            return free;
        }

        private void EvictOldestSfx()
        {
            ISfxReference oldest = m_SfxReferences[0];

            oldest.Stop();
            RemoveSfxReference(oldest);
        }

        private void TakeOwnership(AudioSource[] sources, ISfxReference owner)
        {
            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                for (int j = 0; j < sources.Length; j++)
                {
                    if (!ReferenceEquals(m_CurrentSources[i], sources[j]))
                    {
                        continue;
                    }

                    m_VoiceOwners[i] = owner;

                    break;
                }
            }
        }

        private void ReleaseVoices(ISfxReference owner)
        {
            for (int i = 0; i < m_VoiceOwners.Length; i++)
            {
                if (!ReferenceEquals(m_VoiceOwners[i], owner))
                {
                    continue;
                }

                m_CurrentSources[i].Stop();
                m_CurrentSources[i].clip = null;
                m_VoiceOwners[i]         = null;
            }
        }

        private void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;

            m_This.StopAll();
            m_This.Unload(new List<ulong>(m_Preloads.Keys));

            DestroyPlayerObject();
        }

        private void DestroyPlayerObject()
        {
            if (m_PlayerObject == null)
            {
                return;
            }

            m_UpdateDriver.enabled = false;

            UnityEngine.Object.Destroy(m_PlayerObject);

            m_PlayerObject = null;
            m_UpdateDriver = null;
        }

        private void RemoveSfxReference(ISfxReference sfxReference)
        {
            if (!m_SfxReferences.Remove(sfxReference))
            {
                return;
            }

            ReleaseReference(sfxReference);
        }

        private void ReleaseReference(ISfxReference sfxReference)
        {
            ReleaseVoices(sfxReference);
            UnloadUnusedClips(sfxReference.Asset);
            m_SfxAssetProvider.Release(sfxReference.NameHash);
        }

        private static void EnsureLoaded(AudioClip clip)
        {
            if (clip.preloadAudioData || clip.loadState == AudioDataLoadState.Loaded)
            {
                return;
            }

            clip.LoadAudioData();
        }

        private void UnloadUnusedClips(ISfxAsset asset)
        {
            for (int i = 0; i < asset.Tracks.Count; i++)
            {
                IStem stem = asset.Tracks[i];

                if (stem.Clip.preloadAudioData || IsClipInUse(stem.Clip))
                {
                    continue;
                }

                stem.Clip.UnloadAudioData();
            }
        }

        private bool IsClipInUse(AudioClip clip)
        {
            for (int i = 0; i < m_SfxReferences.Count; i++)
            {
                if (AudioUtils.UsesClip(m_SfxReferences[i].Asset, clip))
                {
                    return true;
                }
            }

            for (int i = 0; i < m_Loading.Count; i++)
            {
                if (AudioUtils.UsesClip(m_Loading[i], clip))
                {
                    return true;
                }
            }

            foreach (ISfxAsset preloaded in m_Preloaded.Values)
            {
                if (AudioUtils.UsesClip(preloaded, clip))
                {
                    return true;
                }
            }

            return false;
        }
    }
}