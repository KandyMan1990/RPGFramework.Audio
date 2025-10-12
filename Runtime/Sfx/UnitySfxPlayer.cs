using System;
using System.Collections.Generic;
using System.Linq;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core.Shared;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Sfx
{
    public class SfxReference : ISfxReference
    {
        public const string SFX_COMPLETE = "SfxComplete";

        public event Action<string, ISfxReference> OnEvent;

        public IReadOnlyList<ISfxEventData> Events { get; }

        private readonly AudioSource[]         m_AudioSources;
        private readonly List<ISfxEventData>   m_EventData;
        private readonly Action<ISfxReference> m_OnAllEventsCompleted;
        private readonly ISfxAsset             m_SfxAsset;

        public SfxReference(AudioSource[] audioSources, ISfxAsset sfxAsset, Action<ISfxReference> onAllEventsCompleted)
        {
            m_AudioSources = audioSources;
            m_SfxAsset     = sfxAsset;
            m_EventData = new List<ISfxEventData>(sfxAsset.Events)
                          {
                                  new SfxEventData(SFX_COMPLETE, sfxAsset.Tracks[0].Clip.length)
                          };

            Events = new List<ISfxEventData>(m_EventData);

            m_OnAllEventsCompleted = onAllEventsCompleted;
        }

        public void CheckForEventToRaise()
        {
            List<ISfxEventData> eventsToRemove = new List<ISfxEventData>();

            foreach (ISfxEventData sfxEventData in m_EventData)
            {
                if (m_AudioSources[0].time >= sfxEventData.EventTriggerTime)
                {
                    eventsToRemove.Add(sfxEventData);
                    OnEvent?.Invoke(sfxEventData.EventName, this);
                }
            }

            foreach (ISfxEventData sfxEventData in eventsToRemove)
            {
                m_EventData.Remove(sfxEventData);
            }

            if (m_EventData.Count != 0)
            {
                return;
            }

            foreach (AudioSource source in m_AudioSources)
            {
                source.clip = null;
            }

            m_OnAllEventsCompleted(this);
        }

        public void CheckForLoop()
        {
            if (!m_SfxAsset.Loop)
            {
                return;
            }

            int currentTime = m_AudioSources[0].timeSamples;

            if (currentTime >= m_SfxAsset.LoopEnd)
            {
                int newTime = currentTime - (m_SfxAsset.LoopEnd - m_SfxAsset.LoopStart);

                foreach (AudioSource source in m_AudioSources)
                {
                    source.timeSamples = newTime;
                }
            }
        }
    }

    public class UnitySfxPlayer : ISfxPlayer, IUpdatable
    {
        private ISfxAssetProvider m_SfxAssetProvider;
        private AudioSource[]     m_CurrentSources;
        private AudioMixerGroup[] m_StemMixerGroups;
        private AudioMixer        m_AudioMixer;

        private readonly Transform           m_Parent;
        private readonly List<ISfxReference> m_SfxReferences;

        private const string SEND_SUFFIX = "_Send";

        public UnitySfxPlayer(Transform parent)
        {
            m_Parent        = parent;
            m_SfxReferences = new List<ISfxReference>();

            UpdateManager.RegisterUpdatable(this);
        }

        ~UnitySfxPlayer()
        {
            UpdateManager.UnregisterUpdatable(this);
        }

        public ISfxReference Play(int id)
        {
            return ScheduleSfx(id, 0f);
        }

        public void Pause(int id)
        {
            ISfxAsset sfxAsset = m_SfxAssetProvider.GetSfxAsset(id);

            foreach (IStem stem in sfxAsset.Tracks)
            {
                foreach (AudioSource audioSource in m_CurrentSources)
                {
                    if (audioSource.clip == stem.Clip)
                    {
                        audioSource.Pause();
                        break;
                    }
                }
            }
        }

        public void PauseAll()
        {
            foreach (AudioSource source in m_CurrentSources)
            {
                source.Pause();
            }
        }

        public void Resume(int id)
        {
            ISfxAsset sfxAsset = m_SfxAssetProvider.GetSfxAsset(id);

            foreach (IStem stem in sfxAsset.Tracks)
            {
                foreach (AudioSource audioSource in m_CurrentSources)
                {
                    if (audioSource.clip == stem.Clip)
                    {
                        audioSource.UnPause();
                        break;
                    }
                }
            }
        }

        public void ResumeAll()
        {
            foreach (AudioSource source in m_CurrentSources)
            {
                source.UnPause();
            }
        }

        public void Stop(int id)
        {
            ISfxAsset sfxAsset = m_SfxAssetProvider.GetSfxAsset(id);

            foreach (IStem stem in sfxAsset.Tracks)
            {
                foreach (AudioSource audioSource in m_CurrentSources)
                {
                    if (audioSource.clip == stem.Clip)
                    {
                        continue;
                    }

                    audioSource.Stop();
                    audioSource.clip = null;
                    break;

                }
            }
        }

        public void StopAll()
        {
            foreach (AudioSource source in m_CurrentSources)
            {
                source.Stop();
                source.clip = null;
            }

            m_SfxReferences.Clear();
        }

        public void SetSfxAssetProvider(ISfxAssetProvider provider)
        {
            m_SfxAssetProvider = provider;
        }

        public void SetStemMixerGroups(AudioMixerGroup[] groups)
        {
            m_StemMixerGroups = groups;
            m_AudioMixer      = m_StemMixerGroups[0].audioMixer;

            m_CurrentSources = new AudioSource[m_StemMixerGroups.Length];

            GameObject sfxParent = new GameObject("Sfx");
            sfxParent.transform.parent = m_Parent;

            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                GameObject go = new GameObject(m_StemMixerGroups[i].name);
                go.transform.parent                       = sfxParent.transform;
                m_CurrentSources[i]                       = go.AddComponent<AudioSource>();
                m_CurrentSources[i].outputAudioMixerGroup = m_StemMixerGroups[i];
            }
        }

        public void Update()
        {
            if (m_SfxReferences.Count == 0)
            {
                return;
            }

            List<ISfxReference> sfxReferences = new List<ISfxReference>(m_SfxReferences);

            foreach (ISfxReference sfxReference in sfxReferences)
            {
                sfxReference.CheckForLoop();
                sfxReference.CheckForEventToRaise();
            }
        }

        private void RemoveSfxReference(ISfxReference sfxReference)
        {
            m_SfxReferences.Remove(sfxReference);
        }

        private ISfxReference ScheduleSfx(int id, float startTime)
        {
            ISfxAsset sfxAsset = m_SfxAssetProvider.GetSfxAsset(id);

            double        scheduledStartTime    = AudioSettings.dspTime + Time.deltaTime;
            AudioSource[] audioSourceReferences = new AudioSource[sfxAsset.Tracks.Count];

            for (int i = 0; i < sfxAsset.Tracks.Count; i++)
            {
                AudioSource source          = m_CurrentSources.First(s => !s.isPlaying);
                int         mixerGroupIndex = Array.IndexOf(m_CurrentSources, source);

                audioSourceReferences[i] = source;

                source.clip                  = sfxAsset.Tracks[i].Clip;
                source.playOnAwake           = false;
                source.loop                  = false;
                source.volume                = 1f;
                source.time                  = startTime;
                source.outputAudioMixerGroup = m_StemMixerGroups[mixerGroupIndex];

                float sendLevel = math.lerp(-10f, 0f, sfxAsset.Tracks[i].ReverbSendLevel);
                m_AudioMixer.SetFloat($"{m_StemMixerGroups[mixerGroupIndex].name}{SEND_SUFFIX}", sendLevel);

                source.PlayScheduled(scheduledStartTime);
            }

            SfxReference sfxRef = new SfxReference(audioSourceReferences, sfxAsset, RemoveSfxReference);

            m_SfxReferences.Add(sfxRef);

            return sfxRef;
        }
    }
}