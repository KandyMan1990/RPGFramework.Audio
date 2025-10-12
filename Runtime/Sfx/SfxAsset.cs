using System.Collections.Generic;
using RPGFramework.Core.Shared;
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

    [CreateAssetMenu(fileName = "SFX Asset", menuName = "RPG Framework/SFX Asset")]
    public class SfxAsset : ScriptableObject, ISfxAsset
    {
        [SerializeField]
        private List<Stem> m_Tracks = new List<Stem>();
        [SerializeField]
        private List<SfxEventData> m_Events = new List<SfxEventData>();
        [SerializeField]
        private bool m_Loop;
        [SerializeField]
        private int m_LoopStart;
        [SerializeField]
        private int m_LoopEnd;

        public IReadOnlyList<IStem>         Tracks    => m_Tracks;
        public IReadOnlyList<ISfxEventData> Events    => m_Events;
        public bool                         Loop      => m_Loop;
        public int                          LoopStart => m_LoopStart;
        public int                          LoopEnd   => m_LoopEnd;
    }
}