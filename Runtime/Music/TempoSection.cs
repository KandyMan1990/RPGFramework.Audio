using System;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// A stretch of a song at one tempo and time signature, from its start bar until the next section's.
    /// </summary>
    [Serializable]
    internal sealed class TempoSection
    {
        [SerializeField]
        [Tooltip("The bar this section starts at, counted from 1. The first section starts at bar 1, and each runs until the next one starts")]
        private int m_StartBar = 1;

        [SerializeField]
        [Tooltip("Quarter notes per minute, as a DAW reports")]
        private float m_BPM = 120f;

        [SerializeField]
        private int m_BeatsPerBar = 4;

        [SerializeField]
        private NoteValue m_BeatUnit = NoteValue.Quarter;

#if UNITY_EDITOR
        internal TempoSection(int startBar, float bpm, int beatsPerBar, NoteValue beatUnit)
        {
            m_StartBar    = startBar;
            m_BPM         = bpm;
            m_BeatsPerBar = beatsPerBar;
            m_BeatUnit    = beatUnit;
        }
#endif

        internal int StartBar => m_StartBar;

        internal bool HasTempo => m_BPM > 0f && m_BeatsPerBar > 0 && Enum.IsDefined(typeof(NoteValue), m_BeatUnit);

        internal double SecondsPerBar
        {
            get
            {
                double secondsPerQuarterNote = 60.0 / m_BPM;
                double secondsPerBeat        = 4.0 / (int)m_BeatUnit * secondsPerQuarterNote;
                double secondsPerBar         = m_BeatsPerBar * secondsPerBeat;

                return secondsPerBar;
            }
        }
    }
}
