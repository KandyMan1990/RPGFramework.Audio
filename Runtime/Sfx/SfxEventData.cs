using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [System.Serializable]
    public class SfxEventData : ISfxEventData
    {
        [SerializeField]
        private string m_EventName;
        [SerializeField]
        private float m_EventTriggerTime;

        public string EventName        => m_EventName;
        public float  EventTriggerTime => m_EventTriggerTime;

        public SfxEventData(string eventName, float eventTriggerTime)
        {
            m_EventName        = eventName;
            m_EventTriggerTime = eventTriggerTime;
        }
    }
}