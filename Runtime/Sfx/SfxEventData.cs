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

        string ISfxEventData.EventName                 => m_EventName;
        float ISfxEventData. EventTriggerTime          => SamplesToSeconds(m_EventTriggerTime);
        int ISfxEventData.   EventTriggerTimeInSamples => m_EventTriggerTime;

        public SfxEventData(string eventName, int eventTriggerTime)
        {
            m_EventName        = eventName;
            m_EventTriggerTime = eventTriggerTime;
        }

        private static float SamplesToSeconds(int value)
        {
            // TODO: make this based on the sample rate of the clip playing
            return (float)value / 44100;
        }
    }
}