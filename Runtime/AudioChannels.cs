using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    /// <summary>
    /// What a player plays on, the same for music and sounds: an object kept across scene loads holding the player's
    /// update driver, and a child per mixer group with its source and its stem playback, and each group's exposed reverb
    /// send.
    /// </summary>
    internal sealed class AudioChannels
    {
        private readonly GameObject m_Object;
        private readonly string[]   m_SendNames;

        internal AudioChannels(string name, AudioMixerGroup[] groups, IAudioUpdatable updatable)
        {
            m_Object = new GameObject(name);
            Object.DontDestroyOnLoad(m_Object);

            Mixer        = groups[0].audioMixer;
            UpdateDriver = AudioUpdateDriver.Attach(m_Object, updatable);
            Playbacks    = new IStemPlayback[groups.Length];
            m_SendNames  = new string[groups.Length];

            for (int i = 0; i < groups.Length; i++)
            {
                GameObject channel = new GameObject(groups[i].name);
                channel.transform.parent = m_Object.transform;

                AudioSource source = channel.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = groups[i];

                Playbacks[i]   = StemPlaybacks.Create(source);
                m_SendNames[i] = $"{groups[i].name}_Send";
            }
        }

        internal AudioMixer Mixer { get; }

        internal AudioUpdateDriver UpdateDriver { get; }

        /// <summary>A playback per channel, in the order of the mixer groups.</summary>
        internal IStemPlayback[] Playbacks { get; }

        /// <summary>How much of what <paramref name="channel" /> plays goes to the reverb, 0 to 1.</summary>
        internal void SetReverbSend(int channel, float level)
        {
            Mixer.SetFloat(m_SendNames[channel], AudioUtils.PercentToDb(level));
        }

        internal void Destroy()
        {
            UpdateDriver.enabled = false;

            Object.Destroy(m_Object);
        }
    }
}
