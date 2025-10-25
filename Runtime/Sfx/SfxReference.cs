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

        public SfxReference(AudioSource[] audioSources, ISfxAsset sfxAsset, Action<ISfxReference> onAllEventsCompleted)
        {
            m_AudioSources = audioSources;
            m_SfxAsset     = sfxAsset;
            m_EventData = new List<ISfxEventData>(sfxAsset.Events)
                          {
                                  new SfxEventData(SFX_COMPLETE, sfxAsset.Tracks[0].Clip.samples)
                          };

            Events = new List<ISfxEventData>(m_EventData);

            m_OnAllEventsCompleted = onAllEventsCompleted;
        }

        void ISfxReference.CheckForEventToRaise()
        {
            List<ISfxEventData> eventsToRemove = new List<ISfxEventData>();

            foreach (ISfxEventData sfxEventData in m_EventData)
            {
                if (m_AudioSources[0].timeSamples >= sfxEventData.EventTriggerTime)
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
            }
        }
        void ISfxReference.Stop()
        {
            // sfx has already finished playing
            if (m_EventData.Count == 0)
            {
                return;
            }
            
            foreach (IStem stem in m_SfxAsset.Tracks)
            {
                foreach (AudioSource audioSource in m_AudioSources)
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
    }
}