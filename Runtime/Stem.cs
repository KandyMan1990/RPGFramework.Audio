using UnityEngine;

namespace RPGFramework.Audio
{
    [System.Serializable]
    public class Stem : IStem
    {
        [SerializeField]
        private AudioClip m_AudioClip;
        [SerializeField]
        [Range(0f, 1f)]
        private float m_ReverbSendLevel;

        public AudioClip Clip            => m_AudioClip;
        public float     ReverbSendLevel => m_ReverbSendLevel;
    }
}