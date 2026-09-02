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

        ISfxAsset ISfxReference.Asset => m_SfxAsset;

        private readonly AudioSource[]         m_AudioSources;
        private readonly ISfxEventData[]       m_Events;
        private readonly Action<ISfxReference> m_OnAllEventsCompleted;
        private readonly ISfxAsset             m_SfxAsset;
        private readonly bool[]                m_Triggered;
        private readonly ISfxEventData         m_CompleteEvent;

        private bool m_Completed;

        internal SfxReference(AudioSource[] audioSources, ISfxAsset sfxAsset, Action<ISfxReference> onAllEventsCompleted)
        {
            m_AudioSources = audioSources;
            m_SfxAsset     = sfxAsset;

            AudioClip clip = sfxAsset.Tracks[0].Clip;

            m_Events    = new ISfxEventData[sfxAsset.Events.Count];
            m_Triggered = new bool[m_Events.Length];

            for (int i = 0; i < m_Events.Length; i++)
            {
                m_Events[i] = new SfxEventData(sfxAsset.Events[i], clip.frequency);
            }

            List<ISfxEventData> publishedEvents = new List<ISfxEventData>(m_Events);

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

            for (int i = 0; i < m_Events.Length; i++)
            {
                if (m_Triggered[i])
                {
                    continue;
                }

                ISfxEventData sfxEventData = m_Events[i];

                if (positionInSamples < sfxEventData.EventTriggerTimeInSamples)
                {
                    continue;
                }

                m_Triggered[i] = true;

                OnEvent?.Invoke(sfxEventData.EventName, this);
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

                RearmLoopingEvents();
            }
        }

        private void RearmLoopingEvents()
        {
            for (int i = 0; i < m_Events.Length; i++)
            {
                if (m_Events[i].RemoveEventOnceTriggered)
                {
                    continue;
                }

                m_Triggered[i] = false;
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