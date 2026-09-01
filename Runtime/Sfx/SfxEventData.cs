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

        private readonly int m_SampleRate;

        string ISfxEventData.EventName                 => m_EventName;
        float ISfxEventData. EventTriggerTime          => SamplesToSeconds(m_EventTriggerTime);
        int ISfxEventData.   EventTriggerTimeInSamples => m_EventTriggerTime;
        bool ISfxEventData.  RemoveEventOnceTriggered  => m_RemoveEventOnceTriggered;

        internal SfxEventData(string eventName, int eventTriggerTimeInSamples, int sampleRate)
        {
            m_EventName        = eventName;
            m_EventTriggerTime = eventTriggerTimeInSamples;
            m_SampleRate       = sampleRate;
        }

        internal SfxEventData(ISfxEventData authored, int sampleRate)
        {
            m_EventName                = authored.EventName;
            m_EventTriggerTime         = authored.EventTriggerTimeInSamples;
            m_RemoveEventOnceTriggered = authored.RemoveEventOnceTriggered;
            m_SampleRate               = sampleRate;
        }

        private float SamplesToSeconds(int value)
        {
            return (float)value / m_SampleRate;
        }
    }
}