using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [System.Serializable]
    public class SfxEventData : ISfxEventData
    {
        [SerializeField]
        private string m_EventName;
        [SerializeField]
        private int m_EventTriggerTime;
        [SerializeField]
        private bool m_RemoveEventOnceTriggered;

        private int m_SampleRate;

        string ISfxEventData.EventName                 => m_EventName;
        float ISfxEventData. EventTriggerTime          => SamplesToSeconds(m_EventTriggerTime);
        int ISfxEventData.   EventTriggerTimeInSamples => m_EventTriggerTime;
        bool ISfxEventData.  RemoveEventOnceTriggered  => m_RemoveEventOnceTriggered;

        public SfxEventData(string eventName, int eventTriggerTimeInSamples, int sampleRate)
        {
            m_EventName        = eventName;
            m_EventTriggerTime = eventTriggerTimeInSamples;
            m_SampleRate       = sampleRate;
        }

        void ISfxEventData.SetSampleRate(int sampleRate)
        {
            m_SampleRate = sampleRate;
        }

        private float SamplesToSeconds(int value)
        {
            return (float)value / m_SampleRate;
        }
    }
}