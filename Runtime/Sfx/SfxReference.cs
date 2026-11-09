using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    internal sealed class SfxReference : ISfxReference
    {
        event Action<string, ISfxReference> ISfxReference.OnEvent
        {
            add    => m_OnEvent += value;
            remove => m_OnEvent -= value;
        }

        IReadOnlyList<ISfxEventData> ISfxReference.Events => m_PublishedEvents ??= BuildPublishedEvents();

        Task ISfxReference.Completion => m_Completion.Task;

        ISfxAsset ISfxReference.Asset    => m_SfxAsset;
        ulong ISfxReference.    NameHash => m_NameHash;

        bool ISfxReference.Loops => m_SfxAsset.Loop;

        float ISfxReference.Position => m_AudioSources.Length > 0 ? m_AudioSources[0].timeSamples / (float)m_SampleRate : 0f;

        private Action<string, ISfxReference> m_OnEvent;

        private readonly ulong                        m_NameHash;
        private readonly AudioSource[]                m_AudioSources;
        private readonly IReadOnlyList<ISfxEventData> m_Events;
        private readonly Action<ISfxReference>        m_OnAllEventsCompleted;
        private readonly ISfxAsset                    m_SfxAsset;
        private readonly bool[]                       m_Triggered;
        private readonly int                          m_SampleRate;
        private readonly int                          m_CompleteTriggerSamples;
        private readonly double                       m_ScheduledStartDspTime;
        private readonly double                       m_CompleteDspTime;
        private readonly TaskCompletionSource<bool>   m_Completion;

        private SfxLevel m_Volume;
        private SfxLevel m_Pan;

        private double m_PausedAtDspTime;
        private double m_PausedDuration;

        private IReadOnlyList<ISfxEventData> m_PublishedEvents;

        private bool m_Completed;

        internal SfxReference(ulong                 nameHash,
                              AudioSource[]         audioSources,
                              ISfxAsset             sfxAsset,
                              double                scheduledStartDspTime,
                              float                 startSeconds,
                              float                 volume,
                              float                 pan,
                              float                 pitch,
                              Action<ISfxReference> onAllEventsCompleted)
        {
            m_NameHash     = nameHash;
            m_AudioSources = audioSources;
            m_SfxAsset     = sfxAsset;

            AudioClip clip = sfxAsset.Tracks[0].Clip;

            m_Events                 = sfxAsset.Events;
            m_Triggered              = new bool[m_Events.Count];
            m_SampleRate             = clip.frequency;
            m_CompleteTriggerSamples = sfxAsset.Loop ? -1 : clip.samples;

            m_ScheduledStartDspTime = scheduledStartDspTime;
            m_CompleteDspTime       = scheduledStartDspTime + ((double)clip.samples / clip.frequency - startSeconds) / pitch;

            m_Volume = new SfxLevel(volume);
            m_Pan    = new SfxLevel(pan);

            int startSamples = (int)(startSeconds * clip.frequency);

            for (int i = 0; i < m_Events.Count; i++)
            {
                m_Triggered[i] = m_Events[i].EventTriggerTimeInSamples < startSamples;
            }

            m_OnAllEventsCompleted = onAllEventsCompleted;
            m_Completion           = new TaskCompletionSource<bool>();
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

        void ISfxReference.SetVolume(float volume, float seconds)
        {
            m_Volume.MoveTo(volume, seconds);
        }

        void ISfxReference.SetPan(float pan, float seconds)
        {
            m_Pan.MoveTo(pan, seconds);
        }

        // A sound that has ended has given up its voices, which another may have since.
        void ISfxReference.UpdateLevels(float deltaTime, float allVolume, bool allChanged)
        {
            if (m_Completed)
            {
                return;
            }

            bool volumeChanged = m_Volume.Tick(deltaTime) || allChanged;
            bool panChanged    = m_Pan.Tick(deltaTime);

            for (int i = 0; i < m_AudioSources.Length; i++)
            {
                if (volumeChanged)
                {
                    m_AudioSources[i].volume = m_Volume.Current * allVolume;
                }

                if (panChanged)
                {
                    m_AudioSources[i].panStereo = m_Pan.Current;
                }
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
            m_Completion.TrySetResult(true);
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
            m_Completion.TrySetResult(true);
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