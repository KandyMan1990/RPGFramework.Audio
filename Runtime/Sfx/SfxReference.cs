using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    internal class SfxReference : ISfxReference
    {
        event Action<string, ISfxReference> ISfxReference.OnEvent
        {
            add    => m_OnEvent += value;
            remove => m_OnEvent -= value;
        }

        IReadOnlyList<ISfxEventData> ISfxReference.Events => m_PublishedEvents ??= BuildPublishedEvents();

        ISfxAsset ISfxReference.Asset => m_SfxAsset;

        private Action<string, ISfxReference> m_OnEvent;

        private readonly AudioSource[]                m_AudioSources;
        private readonly IReadOnlyList<ISfxEventData> m_Events;
        private readonly Action<ISfxReference>        m_OnAllEventsCompleted;
        private readonly ISfxAsset                    m_SfxAsset;
        private readonly bool[]                       m_Triggered;
        private readonly int                          m_SampleRate;
        private readonly int                          m_CompleteTriggerSamples;
        private readonly double                       m_ScheduledStartDspTime;
        private readonly double                       m_CompleteDspTime;

        private double m_PausedAtDspTime;
        private double m_PausedDuration;

        private IReadOnlyList<ISfxEventData> m_PublishedEvents;

        private bool m_Completed;

        internal SfxReference(AudioSource[] audioSources, ISfxAsset sfxAsset, double scheduledStartDspTime, Action<ISfxReference> onAllEventsCompleted)
        {
            m_AudioSources = audioSources;
            m_SfxAsset     = sfxAsset;

            AudioClip clip = sfxAsset.Tracks[0].Clip;

            m_Events                 = sfxAsset.Events;
            m_Triggered              = new bool[m_Events.Count];
            m_SampleRate             = clip.frequency;
            m_CompleteTriggerSamples = sfxAsset.Loop ? -1 : clip.samples;

            m_ScheduledStartDspTime = scheduledStartDspTime;
            m_CompleteDspTime       = scheduledStartDspTime + (double)clip.samples / clip.frequency;

            m_OnAllEventsCompleted = onAllEventsCompleted;
        }

        void ISfxReference.CheckForEventToRaise()
        {
            if (m_Completed)
            {
                return;
            }

            if (AudioSettings.dspTime < m_ScheduledStartDspTime + m_PausedDuration)
            {
                return;
            }

            int positionInSamples = m_AudioSources[0].timeSamples;

            for (int i = 0; i < m_Events.Count; i++)
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

                m_OnEvent?.Invoke(sfxEventData.EventName, this);
            }

            if (m_CompleteTriggerSamples < 0 || m_Completed)
            {
                return;
            }

            if (AudioSettings.dspTime < m_CompleteDspTime + m_PausedDuration)
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

                for (int i = 0; i < m_AudioSources.Length; i++)
                {
                    AudioSource source = m_AudioSources[i];

                    source.timeSamples = newTime;
                }

                RearmLoopingEvents();
            }
        }

        private void RearmLoopingEvents()
        {
            for (int i = 0; i < m_Events.Count; i++)
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
            if (m_PausedAtDspTime <= 0d)
            {
                m_PausedAtDspTime = AudioSettings.dspTime;
            }

            for (int i = 0; i < m_AudioSources.Length; i++)
            {
                AudioSource audioSource = m_AudioSources[i];

                audioSource.Pause();
            }
        }

        void ISfxReference.Resume()
        {
            if (m_PausedAtDspTime > 0d)
            {
                m_PausedDuration  += AudioSettings.dspTime - m_PausedAtDspTime;
                m_PausedAtDspTime =  0d;
            }

            for (int i = 0; i < m_AudioSources.Length; i++)
            {
                AudioSource audioSource = m_AudioSources[i];

                audioSource.UnPause();
            }
        }

        void ISfxReference.Stop()
        {
            m_Completed = true;
        }

        private void Complete()
        {
            if (m_CompleteTriggerSamples < 0 || m_Completed)
            {
                return;
            }

            m_Completed = true;

            RaiseUntriggeredEvents();

            m_OnEvent?.Invoke(ISfxReference.SFX_COMPLETE, this);

            m_OnAllEventsCompleted(this);
        }

        private void RaiseUntriggeredEvents()
        {
            for (int i = 0; i < m_Events.Count; i++)
            {
                if (m_Triggered[i])
                {
                    continue;
                }

                m_Triggered[i] = true;

                m_OnEvent?.Invoke(m_Events[i].EventName, this);
            }
        }

        private IReadOnlyList<ISfxEventData> BuildPublishedEvents()
        {
            bool            hasComplete = m_CompleteTriggerSamples >= 0;
            ISfxEventData[] published   = new ISfxEventData[m_Events.Count + (hasComplete ? 1 : 0)];

            for (int i = 0; i < m_Events.Count; i++)
            {
                published[i] = new SfxEventData(m_Events[i], m_SampleRate);
            }

            if (hasComplete)
            {
                published[m_Events.Count] = new SfxEventData(ISfxReference.SFX_COMPLETE, m_CompleteTriggerSamples, m_SampleRate);
            }

            return published;
        }
    }
}