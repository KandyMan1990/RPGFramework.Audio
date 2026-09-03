using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Sfx
{
    public class UnitySfxPlayer : ISfxPlayer, IAudioUpdatable, IDisposable
    {
        private const string SFX_BUS_NAME    = "Sfx";
        private const string SFX_REVERB_SEND = "SfxReverbSend";

        private static readonly string[] VOLUME_BUS_NAMES = { SFX_BUS_NAME, SFX_REVERB_SEND };

        private readonly ISfxPlayer m_This;

        private ISfxAssetProvider m_SfxAssetProvider;
        private AudioSource[]     m_CurrentSources;
        private AudioMixerGroup[] m_StemMixerGroups;
        private AudioMixer        m_AudioMixer;
        private bool              m_Disposed;
        private string[]          m_SendParameterNames;
        private ISfxReference[]   m_VoiceOwners;
        private GameObject        m_PlayerObject;
        private AudioUpdateDriver m_UpdateDriver;

        private readonly List<ISfxReference> m_SfxReferences;

        public UnitySfxPlayer()
        {
            m_SfxReferences = new List<ISfxReference>();
            m_This          = this;
        }

        ISfxReference ISfxPlayer.Play(int id)
        {
            return ScheduleSfx(id, 0f);
        }

        void ISfxPlayer.Pause(ISfxReference sfxReference)
        {
            sfxReference.Pause();
        }

        void ISfxPlayer.PauseAll()
        {
            foreach (ISfxReference sfxReference in m_SfxReferences)
            {
                m_This.Pause(sfxReference);
            }
        }

        void ISfxPlayer.Resume(ISfxReference sfxReference)
        {
            sfxReference.Resume();
        }

        void ISfxPlayer.ResumeAll()
        {
            foreach (ISfxReference sfxReference in m_SfxReferences)
            {
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
            ValidateMixerGroups(groups);

            m_StemMixerGroups = groups;
            m_AudioMixer      = m_StemMixerGroups[0].audioMixer;

            m_CurrentSources     = new AudioSource[m_StemMixerGroups.Length];
            m_SendParameterNames = new string[m_StemMixerGroups.Length];
            m_VoiceOwners        = new ISfxReference[m_StemMixerGroups.Length];

            DestroyPlayerObject();

            m_PlayerObject = new GameObject("SfxPlayer");
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

            for (int i = m_SfxReferences.Count - 1; i >= 0; i--)
            {
                ISfxReference sfxReference = m_SfxReferences[i];
                sfxReference.CheckForEventToRaise();
                sfxReference.CheckForLoop();
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        private ISfxReference ScheduleSfx(int id, float startTime)
        {
            if (m_SfxAssetProvider == null)
            {
                throw new InvalidOperationException($"{nameof(UnitySfxPlayer)}::{nameof(ScheduleSfx)} No asset provider. Call {nameof(ISfxPlayer.SetSfxAssetProvider)} before playing anything");
            }

            if (m_CurrentSources == null)
            {
                throw new InvalidOperationException($"{nameof(UnitySfxPlayer)}::{nameof(ScheduleSfx)} No voices. Call {nameof(ISfxPlayer.SetStemMixerGroups)} before playing anything");
            }

            ISfxAsset sfxAsset = m_SfxAssetProvider.GetSfxAsset(id);

            ValidateStems(id, sfxAsset);

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

                EnsureLoaded(source.clip);

                source.PlayScheduled(scheduledStartTime);
            }

            SfxReference sfxRef = new SfxReference(audioSourceReferences, sfxAsset, RemoveSfxReference);

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

        private static void ValidateStems(int id, ISfxAsset sfxAsset)
        {
            IReadOnlyList<IStem> tracks = sfxAsset.Tracks;

            if (tracks.Count == 0)
            {
                throw new InvalidOperationException($"{nameof(UnitySfxPlayer)}::{nameof(ValidateStems)} Sfx [{id}] has no stems. Give it at least one stem with a clip assigned");
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].Clip == null)
                {
                    throw new InvalidOperationException($"{nameof(UnitySfxPlayer)}::{nameof(ValidateStems)} Sfx [{id}] stem [{i}] has no clip assigned");
                }
            }
        }

        private static void ValidateMixerGroups(AudioMixerGroup[] groups)
        {
            if (groups == null || groups.Length == 0)
            {
                throw new InvalidOperationException($"{nameof(UnitySfxPlayer)}::{nameof(ISfxPlayer.SetStemMixerGroups)} At least one mixer group is required. Each one becomes a voice this player can use");
            }

            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null)
                {
                    throw new InvalidOperationException($"{nameof(UnitySfxPlayer)}::{nameof(ISfxPlayer.SetStemMixerGroups)} Mixer group [{i}] is not assigned");
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
            foreach (IStem stem in asset.Tracks)
            {
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
                foreach (IStem stem in m_SfxReferences[i].Asset.Tracks)
                {
                    if (ReferenceEquals(stem.Clip, clip))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}