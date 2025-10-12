using System.Collections.Generic;
using RPGFramework.Core.Shared;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    [CreateAssetMenu(fileName = "Music Asset", menuName = "RPG Framework/Music Asset")]
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
        private List<Stem> m_Tracks;

        [SerializeField]
        private bool m_Loop;

        public double LoopStartTime { get; private set; }
        public double LoopEndTime   { get; private set; }
        public bool   Loop          => m_Loop;

        public IReadOnlyList<IStem> Tracks => m_Tracks;

        public void CalculateLoopPoints()
        {
            LoopStartTime = BarToSeconds(m_LoopStartBar - 1, m_BPM, m_BeatsPerBar);
            LoopEndTime   = BarToSeconds(m_LoopEndBar   - 1, m_BPM, m_BeatsPerBar);
        }

        private static double BarToSeconds(int bar, float bpm, int beatsPerBar)
        {
            double secondsPerBeat = 60.0 / bpm;
            return bar * beatsPerBar * secondsPerBeat;
        }
    }
}