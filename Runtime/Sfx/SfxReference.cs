using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

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
        private readonly ISfxEventData         m_CompleteEvent;

        private bool m_Completed;

        internal SfxReference(AudioSource[] audioSources, ISfxAsset sfxAsset, Action<ISfxReference> onAllEventsCompleted)
        {
            m_AudioSources    = audioSources;
            m_SfxAsset        = sfxAsset;
            m_EventData       = new List<ISfxEventData>(sfxAsset.Events);
            m_EventsTriggered = new List<ISfxEventData>();

            AudioClip clip = sfxAsset.Tracks[0].Clip;

            foreach (ISfxEventData sfxEventData in m_EventData)
            {
                sfxEventData.SetSampleRate(clip.frequency);
            }

            List<ISfxEventData> publishedEvents = new List<ISfxEventData>(m_EventData);

            if (!sfxAsset.Loop)
            {
                m_CompleteEvent = new SfxEventData(SFX_COMPLETE, clip.samples, clip.frequency);

                publishedEvents.Add(m_CompleteEvent);
            }

            Events = publishedEvents;

            m_OnAllEventsCompleted = onAllEventsCompleted;
        }

        void ISfxReference.CheckForEventToRaise()
        {
            if (m_Completed)
            {
                return;
            }

            int positionInSamples = m_AudioSources[0].timeSamples;

            List<ISfxEventData> eventsToRemove = ListPool<ISfxEventData>.Get();

            try
            {
                foreach (ISfxEventData sfxEventData in m_EventData)
                {
                    if (positionInSamples >= sfxEventData.EventTriggerTimeInSamples)
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

                if (m_CompleteEvent == null || m_Completed)
                {
                    return;
                }

                if (positionInSamples < m_CompleteEvent.EventTriggerTimeInSamples)
                {
                    return;
                }

                Complete();
            }
            finally
            {
                ListPool<ISfxEventData>.Release(eventsToRemove);
            }
        }

        void ISfxReference.CheckForLoop()
        {
            if (!m_SfxAsset.Loop || m_Completed)
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
            foreach (AudioSource audioSource in m_AudioSources)
            {
                audioSource.Pause();
            }
        }

        void ISfxReference.Resume()
        {
            foreach (AudioSource audioSource in m_AudioSources)
            {
                audioSource.UnPause();
            }
        }

        void ISfxReference.Stop()
        {
            m_Completed = true;
        }

        private void Complete()
        {
            if (m_CompleteEvent == null || m_Completed)
            {
                return;
            }

            m_Completed = true;

            OnEvent?.Invoke(m_CompleteEvent.EventName, this);

            m_OnAllEventsCompleted(this);
        }
    }
}