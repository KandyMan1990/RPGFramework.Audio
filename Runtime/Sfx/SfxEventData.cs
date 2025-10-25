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

        string ISfxEventData.EventName        => m_EventName;
        float ISfxEventData. EventTriggerTime => m_EventTriggerTime;

        public SfxEventData(string eventName, float eventTriggerTime)
        {
            m_EventName        = eventName;
            m_EventTriggerTime = eventTriggerTime;
        }
    }
}