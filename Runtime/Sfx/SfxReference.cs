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
        private readonly AudioClip[]           m_ClaimedClips;

        private bool m_Completed;

        internal SfxReference(AudioSource[] audioSources, ISfxAsset sfxAsset, Action<ISfxReference> onAllEventsCompleted)
        {
            m_AudioSources    = audioSources;
            m_SfxAsset        = sfxAsset;
            m_EventData       = new List<ISfxEventData>(sfxAsset.Events);
            m_EventsTriggered = new List<ISfxEventData>();

            AudioClip clip = sfxAsset.Tracks[0].Clip;

            m_ClaimedClips = new AudioClip[audioSources.Length];

            for (int i = 0; i < audioSources.Length; i++)
            {
                m_ClaimedClips[i] = audioSources[i].clip;
            }

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
            if (!TryGetOwnedPosition(out int positionInSamples))
            {
                Complete();

                return;
            }

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
            if (m_Completed)
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

        private bool TryGetOwnedPosition(out int positionInSamples)
        {
            for (int i = 0; i < m_AudioSources.Length; i++)
            {
                if (m_AudioSources[i].clip != m_ClaimedClips[i])
                {
                    continue;
                }

                positionInSamples = m_AudioSources[i].timeSamples;

                return true;
            }

            positionInSamples = 0;

            return false;
        }

        private void Complete()
        {
            if (m_CompleteEvent == null || m_Completed)
            {
                return;
            }

            m_Completed = true;

            OnEvent?.Invoke(m_CompleteEvent.EventName, this);

            ReleaseClaimedSources();

            m_OnAllEventsCompleted(this);
        }

        private void ReleaseClaimedSources()
        {
            for (int i = 0; i < m_AudioSources.Length; i++)
            {
                if (m_AudioSources[i].clip != m_ClaimedClips[i])
                {
                    continue;
                }

                m_AudioSources[i].clip = null;
            }
        }
    }
}