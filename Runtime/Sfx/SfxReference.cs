using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    internal sealed class SfxReference : ISfxReference
#if ENABLE_RPG_FRAMEWORK_DEBUG
        , IDebugSfxReference
#endif
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

        float ISfxReference.Position => m_Stems.Length > 0 ? (float)m_Stems[0].Position : 0f;

#if ENABLE_RPG_FRAMEWORK_DEBUG
        DebugTrack IDebugSfxReference.Track
        {
            get
            {
                bool   loops = m_SfxAsset.Loop;
                double rate  = m_SampleRate;

                return new DebugTrack(m_SfxAsset is UnityEngine.Object asset ? asset.name : string.Empty,
                                      m_Stems.Length > 0 ? m_Stems[0].Position : 0.0,
                                      (double)m_FirstClip.samples / m_FirstClip.frequency,
                                      loops,
                                      loops ? m_SfxAsset.LoopStart / rate : 0.0,
                                      loops ? m_SfxAsset.LoopEnd / rate : 0.0);
            }
        }
#endif

        private Action<string, ISfxReference> m_OnEvent;

        private readonly ulong                        m_NameHash;
        private readonly IStemPlayback[]              m_Stems;
        private readonly IReadOnlyList<ISfxEventData> m_Events;
        private readonly Action<ISfxReference>        m_OnAllEventsCompleted;
        private readonly ISfxAsset                    m_SfxAsset;
        private readonly bool[]                       m_Triggered;
        private readonly int                          m_SampleRate;
        private readonly bool                         m_Completes;
        private readonly AudioClip                    m_FirstClip;
        private readonly TaskCompletionSource<bool>   m_Completion;

        private SfxLevel m_Volume;
        private SfxLevel m_Pan;
        private double   m_LastPosition;

        private IReadOnlyList<ISfxEventData> m_PublishedEvents;

        private bool m_Completed;

        internal SfxReference(ulong                 nameHash,
                              IStemPlayback[]       stems,
                              ISfxAsset             sfxAsset,
                              float                 startSeconds,
                              float                 volume,
                              float                 pan,
                              Action<ISfxReference> onAllEventsCompleted)
        {
            m_NameHash = nameHash;
            m_Stems    = stems;
            m_SfxAsset = sfxAsset;

            m_Events     = sfxAsset.Events;
            m_Triggered  = new bool[m_Events.Count];
            m_SampleRate = sfxAsset.SampleRate;
            m_Completes  = !sfxAsset.Loop;
            m_FirstClip  = sfxAsset.Tracks[0].Clip;

            m_Volume       = new SfxLevel(volume);
            m_Pan          = new SfxLevel(pan);
            m_LastPosition = startSeconds;

            int startSamples = (int)(startSeconds * m_SampleRate);

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

            double position = m_Stems[0].Position;

            // Gone back round its loop since the last look: the rest of the pass was due first, then the repeating
            // events come round again.
            if (m_SfxAsset.Loop && position < m_LastPosition)
            {
                RaiseEventsReached(m_SfxAsset.LoopEnd);
                RearmLoopingEvents();
            }

            m_LastPosition = position;

            RaiseEventsReached((int)(position * m_SampleRate));

            if (!m_Completes || !m_Stems[0].Ended)
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

            for (int i = 0; i < m_Stems.Length; i++)
            {
                m_Stems[i].Tick();
            }
        }

        private void RaiseEventsReached(int positionInSamples)
        {
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

            for (int i = 0; i < m_Stems.Length; i++)
            {
                AudioSource source = m_Stems[i].Source;

                if (volumeChanged)
                {
                    source.volume = m_Volume.Current * allVolume;
                }

                if (panChanged)
                {
                    source.panStereo = m_Pan.Current;
                }
            }
        }

        void ISfxReference.Pause()
        {
            for (int i = 0; i < m_Stems.Length; i++)
            {
                m_Stems[i].Pause();
            }
        }

        void ISfxReference.Resume()
        {
            for (int i = 0; i < m_Stems.Length; i++)
            {
                m_Stems[i].UnPause();
            }
        }

        void ISfxReference.Stop()
        {
            m_Completed = true;
            m_Completion.TrySetResult(true);
        }

        private void Complete()
        {
            if (!m_Completes || m_Completed)
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
            ISfxEventData[] published = new ISfxEventData[m_Events.Count + (m_Completes ? 1 : 0)];

            for (int i = 0; i < m_Events.Count; i++)
            {
                published[i] = new SfxEventData(m_Events[i], m_SampleRate);
            }

            // The sound's end is its first stem's, in that clip's own samples.
            if (m_Completes)
            {
                published[m_Events.Count] = new SfxEventData(ISfxReference.SFX_COMPLETE, m_FirstClip.samples, m_FirstClip.frequency);
            }

            return published;
        }
    }
}
