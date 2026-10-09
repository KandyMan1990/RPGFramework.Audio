using System;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    internal enum EchoTiming
    {
        None,
        Milliseconds,
        NoteLength
    }

    /// <summary>
    /// How a note length is counted: as written, half as long again, or two thirds as long.
    /// </summary>
    internal enum NoteFeel
    {
        Straight,
        Dotted,
        Triplet
    }

    /// <summary>
    /// The echo a song sets when it starts: how far apart the repeats are, and how much quieter each is than the last.
    /// How much of each stem goes in is the stem's own echo send.
    /// </summary>
    [Serializable]
    internal sealed class EchoSettings
    {
        internal const float MIN_DELAY_MS = 10f;
        internal const float MAX_DELAY_MS = 5000f;

        [SerializeField]
        [Tooltip("None leaves the echo as the last song set it. Milliseconds sets it once; a note length follows the song's tempo sections")]
        private EchoTiming m_Timing = EchoTiming.None;

        [SerializeField]
        [Range(MIN_DELAY_MS, MAX_DELAY_MS)]
        private float m_Milliseconds = 500f;

        [SerializeField]
        private NoteValue m_Note = NoteValue.Quarter;

        [SerializeField]
        private NoteFeel m_Feel = NoteFeel.Straight;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Each repeat as a share of the one before: 0.5 halves it, 1 never fades")]
        private float m_Decay = 0.5f;

        internal EchoTiming Timing => m_Timing;
        internal float      Decay  => m_Decay;

        /// <summary>
        /// The delay in milliseconds, held to what the mixer's echo takes, with a quarter note lasting
        /// <paramref name="secondsPerQuarterNote" /> where it is a note length.
        /// </summary>
        internal float GetDelayMilliseconds(double secondsPerQuarterNote)
        {
            double milliseconds = m_Timing == EchoTiming.NoteLength
                                      ? 4.0 / (int)m_Note * FeelScale(m_Feel) * secondsPerQuarterNote * 1000.0
                                      : m_Milliseconds;

            float delay = (float)Math.Clamp(milliseconds, MIN_DELAY_MS, MAX_DELAY_MS);

            return delay;
        }

        private static double FeelScale(NoteFeel feel)
        {
            double scale = feel switch
                           {
                                   NoteFeel.Dotted  => 1.5,
                                   NoteFeel.Triplet => 2.0 / 3.0,
                                   _                => 1.0
                           };

            return scale;
        }
    }
}
