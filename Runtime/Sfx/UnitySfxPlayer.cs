using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Sfx
{
    public sealed class UnitySfxPlayer : ISfxPlayer, IAudioUpdatable
    {
        private const string SFX_BUS_NAME         = "Sfx";
        private const string SFX_REVERB_SEND      = "SfxReverbSend";
        private const string SFX_GAME_OBJECT_NAME = "SfxPlayer";

        private static readonly string[] VOLUME_BUS_NAMES = { SFX_BUS_NAME, SFX_REVERB_SEND };

        private readonly ISfxPlayer                     m_This;
        private readonly List<ISfxReference>            m_SfxReferences;
        private readonly List<ISfxReference>            m_UpdateBuffer;
        private readonly List<ISfxAsset>                m_Loading;
        private readonly Dictionary<ulong, int>         m_Preloads;
        private readonly Dictionary<ulong, SfxPlayable> m_Preloaded;
        private readonly Dictionary<ulong, int>         m_LastPicked;

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
        private SfxLevel          m_AllVolume;

        public UnitySfxPlayer()
        {
            m_SfxReferences = new List<ISfxReference>();
            m_UpdateBuffer  = new List<ISfxReference>();
            m_Loading       = new List<ISfxAsset>();
            m_Preloads      = new Dictionary<ulong, int>();
            m_Preloaded     = new Dictionary<ulong, SfxPlayable>();
            m_LastPicked    = new Dictionary<ulong, int>();
            m_This          = this;
            m_AllVolume     = new SfxLevel(1f);
        }

        Task<ISfxReference> ISfxPlayer.PlayAsync(ulong nameHash)
        {
            return PlayAsync(nameHash, 1f, 0f, 0f, ISfxPlayer.NEXT_IN_ORDER);
        }

        Task<ISfxReference> ISfxPlayer.PlayAsync(ulong nameHash, float volume, float pan, float startSeconds, int position)
        {
            return PlayAsync(nameHash, math.saturate(volume), math.clamp(pan, -1f, 1f), startSeconds, position);
        }

        private async Task<ISfxReference> PlayAsync(ulong nameHash, float volume, float pan, float startSeconds, int position)
        {
            int         stops    = m_Stops;
            SfxPlayable playable = await m_SfxAssetProvider.AcquireAsync(nameHash);
            ISfxAsset   sfxAsset = PickRecording(nameHash, playable, position);
            float       pitch    = SfxVariation.PickPitch(playable.PitchVariation, UnityEngine.Random.value);

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

            ISfxReference playing = ScheduleSfx(nameHash, sfxAsset, startSeconds, volume, pan, pitch);

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
                if (!m_Preloaded.Remove(nameHash, out SfxPlayable playable))
                {
                    continue;
                }

                for (int j = 0; j < playable.Recordings.Count; j++)
                {
                    UnloadUnusedClips(playable.Recordings[j]);
                }

                m_SfxAssetProvider.Release(nameHash);
            }
        }

        void ISfxPlayer.SetSoundVolume(ISfxReference sfxReference, float volume, float seconds)
        {
            sfxReference.SetVolume(math.saturate(volume), seconds);
        }

        void ISfxPlayer.SetSoundPan(ISfxReference sfxReference, float pan, float seconds)
        {
            sfxReference.SetPan(math.clamp(pan, -1f, 1f), seconds);
        }

        void ISfxPlayer.SetAllSoundsVolume(float volume, float seconds)
        {
            m_AllVolume.MoveTo(math.saturate(volume), seconds);
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
            float deltaTime  = Time.unscaledDeltaTime;
            bool  allChanged = m_AllVolume.Tick(deltaTime);

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
                sfxReference.UpdateLevels(deltaTime, m_AllVolume.Current, allChanged);
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }

        private async Task PreloadAsync(ulong nameHash)
        {
            if (m_Preloads.ContainsKey(nameHash))
            {
                return;
            }

            int request = ++m_PreloadRequests;
            m_Preloads.Add(nameHash, request);

            SfxPlayable playable = await m_SfxAssetProvider.AcquireAsync(nameHash);

            // Unloaded while it loaded, and perhaps preloaded again since, by a request that keeps it instead.
            if (!m_Preloads.TryGetValue(nameHash, out int current) || current != request)
            {
                m_SfxAssetProvider.Release(nameHash);

                return;
            }

            m_Preloaded.Add(nameHash, playable);

            for (int i = 0; i < playable.Recordings.Count; i++)
            {
                await LoadClipsAsync(playable.Recordings[i]);
            }
        }

        private ISfxAsset PickRecording(ulong nameHash, SfxPlayable playable, int position)
        {
            IReadOnlyList<ISfxAsset> recordings = playable.Recordings;

            if (recordings.Count == 1)
            {
                return recordings[0];
            }

            int last = m_LastPicked.TryGetValue(nameHash, out int picked) ? picked : SfxVariation.NONE;
            int index = playable.PlaysInOrder
                            ? SfxVariation.InOrderIndex(recordings.Count, last, position)
                            : SfxVariation.PickIndex(recordings.Count, last, UnityEngine.Random.value);

            m_LastPicked[nameHash] = index;

            return recordings[index];
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
            ISfxReference stopped = new SfxReference(nameHash, Array.Empty<AudioSource>(), sfxAsset, AudioSettings.dspTime, 0f, 1f, 0f, 1f, _ => { });

            stopped.Stop();

            return stopped;
        }

        private ISfxReference ScheduleSfx(ulong nameHash, ISfxAsset sfxAsset, float startTime, float volume, float pan, float pitch)
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
                source.volume                = volume * m_AllVolume.Current;
                source.panStereo             = pan;
                source.pitch                 = pitch;
                source.time                  = startTime;
                source.outputAudioMixerGroup = m_StemMixerGroups[voice];

                float sendLevel = AudioUtils.PercentToDb(sfxAsset.Tracks[i].ReverbSendLevel);
                m_AudioMixer.SetFloat(m_SendParameterNames[voice], sendLevel);

                source.PlayScheduled(scheduledStartTime);
            }

            SfxReference sfxRef = new SfxReference(nameHash, audioSourceReferences, sfxAsset, scheduledStartTime, startTime, volume, pan, pitch, RemoveSfxReference);

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

            foreach (SfxPlayable preloaded in m_Preloaded.Values)
            {
                for (int i = 0; i < preloaded.Recordings.Count; i++)
                {
                    if (AudioUtils.UsesClip(preloaded.Recordings[i], clip))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
