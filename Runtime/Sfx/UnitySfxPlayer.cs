using System;
using System.Collections.Generic;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Sfx
{
    public class UnitySfxPlayer : ISfxPlayer, IUpdatable, IDisposable
    {
        private const string SFX_BUS_NAME    = "Sfx";
        private const string SFX_REVERB_SEND = "SfxReverbSend";

        private readonly ISfxPlayer m_This;

        private ISfxAssetProvider m_SfxAssetProvider;
        private AudioSource[]     m_CurrentSources;
        private AudioMixerGroup[] m_StemMixerGroups;
        private AudioMixer        m_AudioMixer;
        private bool              m_Disposed;
        private string[]          m_SendParameterNames;
        private ISfxReference[]   m_VoiceOwners;

        private readonly List<ISfxReference> m_SfxReferences;

        public UnitySfxPlayer()
        {
            m_SfxReferences = new List<ISfxReference>();
            m_This          = this;

            UpdateManager.RegisterUpdatable(this);
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
            if (!m_SfxReferences.Contains(sfxReference))
            {
                return;
            }

            sfxReference.Stop();
            RemoveSfxReference(sfxReference);
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
            m_StemMixerGroups = groups;
            m_AudioMixer      = m_StemMixerGroups[0].audioMixer;

            m_CurrentSources     = new AudioSource[m_StemMixerGroups.Length];
            m_SendParameterNames = new string[m_StemMixerGroups.Length];
            m_VoiceOwners        = new ISfxReference[m_StemMixerGroups.Length];

            GameObject sfxPlayer = new GameObject("SfxPlayer");
            UnityEngine.Object.DontDestroyOnLoad(sfxPlayer);

            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                GameObject go = new GameObject(m_StemMixerGroups[i].name);
                go.transform.parent                       = sfxPlayer.transform;
                m_CurrentSources[i]                       = go.AddComponent<AudioSource>();
                m_CurrentSources[i].outputAudioMixerGroup = m_StemMixerGroups[i];

                m_SendParameterNames[i] = $"{m_StemMixerGroups[i].name}_Send";
            }
        }

        private ISfxReference ScheduleSfx(int id, float startTime)
        {
            ISfxAsset sfxAsset  = m_SfxAssetProvider.GetSfxAsset(id);
            int       stemCount = sfxAsset.Tracks.Count;

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

        float ISfxPlayer.GetVolume()
        {
            return AudioUtils.GetVolume(m_AudioMixer, SFX_BUS_NAME);
        }

        void ISfxPlayer.SetVolume(float percent)
        {
            string[] busNames = new string[]
                                {
                                    SFX_BUS_NAME,
                                    SFX_REVERB_SEND
                                };

            AudioUtils.SetVolume(m_AudioMixer, busNames, percent);
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        void IUpdatable.Update()
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

        private void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;
            UpdateManager.UnregisterUpdatable(this);
        }

        private void RemoveSfxReference(ISfxReference sfxReference)
        {
            ReleaseVoices(sfxReference);

            m_SfxReferences.Remove(sfxReference);
        }
    }
}