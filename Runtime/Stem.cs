using UnityEngine;

namespace RPGFramework.Audio
{
    [System.Serializable]
    internal class Stem : IStem
    {
        [SerializeField]
        private AudioClip m_AudioClip;
        [SerializeField]
        [Range(0f, 1f)]
        private float m_ReverbSendLevel;
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of this stem goes to the echo the song sets. Music only")]
        private float m_EchoSendLevel;

        AudioClip IStem.Clip            => m_AudioClip;
        float IStem.    ReverbSendLevel => m_ReverbSendLevel;
        float IStem.    EchoSendLevel   => m_EchoSendLevel;
    }
}