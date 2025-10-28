using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    [CreateAssetMenu(fileName = "Music Asset", menuName = "RPG Framework/Audio/Music Asset")]
    public class MusicAsset : ScriptableObject, IMusicAsset
    {
        [SerializeField]
        private float m_BPM;

        [SerializeField]
        private int m_LoopStartBar;

        [SerializeField]
        private int m_LoopEndBar;

        [SerializeField]
        private int m_BeatsPerBar = 4;

        [SerializeField]
        private bool m_Loop;

        [SerializeField]
        private List<Stem> m_Tracks;

        private double m_LoopStartTime;
        private double m_LoopEndTime;

        double IMusicAsset.LoopStartTime => m_LoopStartTime;
        double IMusicAsset.LoopEndTime   => m_LoopEndTime;
        bool IMusicAsset.  Loop          => m_Loop;

        IReadOnlyList<IStem> IMusicAsset.Tracks => m_Tracks;

        void IMusicAsset.CalculateLoopPoints()
        {
            m_LoopStartTime = BarToSeconds(m_LoopStartBar - 1, m_BPM, m_BeatsPerBar);
            m_LoopEndTime   = BarToSeconds(m_LoopEndBar   - 1, m_BPM, m_BeatsPerBar);
        }

        private static double BarToSeconds(int bar, float bpm, int beatsPerBar)
        {
            double secondsPerBeat = 60.0 / bpm;
            return bar * beatsPerBar * secondsPerBeat;
        }
    }
}