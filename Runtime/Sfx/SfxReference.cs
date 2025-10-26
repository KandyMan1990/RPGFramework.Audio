using System;
using System.Collections.Generic;
using UnityEngine;

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
        private readonly List<ISfxEventData>   m_EventsTriggered;

        public SfxReference(AudioSource[] audioSources, ISfxAsset sfxAsset, Action<ISfxReference> onAllEventsCompleted)
        {
            m_AudioSources    = audioSources;
            m_SfxAsset        = sfxAsset;
            m_EventData       = new List<ISfxEventData>(sfxAsset.Events);
            m_EventsTriggered = new List<ISfxEventData>();

            foreach (ISfxEventData sfxEventData in m_EventData)
            {
                AudioClip clip = sfxAsset.Tracks[0].Clip;
                sfxEventData.SetSampleRate(clip.frequency);
            }

            if (!sfxAsset.Loop)
            {
                AudioClip clip = sfxAsset.Tracks[0].Clip;
                m_EventData.Add(new SfxEventData(SFX_COMPLETE, clip.samples, clip.frequency));
            }

            Events = new List<ISfxEventData>(m_EventData);

            m_OnAllEventsCompleted = onAllEventsCompleted;
        }

        void ISfxReference.CheckForEventToRaise()
        {
            List<ISfxEventData> eventsToRemove = new List<ISfxEventData>(m_EventData.Count);

            foreach (ISfxEventData sfxEventData in m_EventData)
            {
                if (m_AudioSources[0].timeSamples >= sfxEventData.EventTriggerTimeInSamples)
                {
                    if (sfxEventData.RemoveEventOnceTriggered)
                    {
                        eventsToRemove.Add(sfxEventData);
                    }
                    else
                    {
                        if (m_EventsTriggered.Contains(sfxEventData))
                        {
                            continue;
                        }

                        m_EventsTriggered.Add(sfxEventData);
                    }

                    OnEvent?.Invoke(sfxEventData.EventName, this);
                }
            }

            foreach (ISfxEventData sfxEventData in eventsToRemove)
            {
                m_EventData.Remove(sfxEventData);
            }

            if (m_EventData.Count != 0 || m_SfxAsset.Loop)
            {
                return;
            }

            foreach (AudioSource source in m_AudioSources)
            {
                source.clip = null;
            }

            m_OnAllEventsCompleted(this);
        }

        void ISfxReference.CheckForLoop()
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

                m_EventsTriggered.Clear();
            }
        }

        void ISfxReference.Pause()
        {
            foreach (IStem stem in m_SfxAsset.Tracks)
            {
                foreach (AudioSource audioSource in m_AudioSources)
                {
                    if (audioSource.clip != stem.Clip)
                    {
                        continue;
                    }

                    audioSource.Pause();
                    break;
                }
            }
        }

        void ISfxReference.Resume()
        {
            foreach (IStem stem in m_SfxAsset.Tracks)
            {
                foreach (AudioSource audioSource in m_AudioSources)
                {
                    if (audioSource.clip != stem.Clip)
                    {
                        continue;
                    }

                    audioSource.UnPause();
                    break;
                }
            }
        }

        void ISfxReference.Stop()
        {
            // sfx has already finished playing
            if (m_EventData.Count == 0 && !m_SfxAsset.Loop)
            {
                return;
            }

            foreach (IStem stem in m_SfxAsset.Tracks)
            {
                foreach (AudioSource audioSource in m_AudioSources)
                {
                    if (audioSource.clip != stem.Clip)
                    {
                        continue;
                    }

                    audioSource.Stop();
                    audioSource.clip = null;
                    break;
                }
            }
        }
    }
}