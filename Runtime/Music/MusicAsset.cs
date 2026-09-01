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
        private bool   m_LoopPointsValid;

        double IMusicAsset.              LoopStartTime => m_LoopStartTime;
        double IMusicAsset.              LoopEndTime   => m_LoopEndTime;
        bool IMusicAsset.                Loop          => m_Loop && m_LoopPointsValid;
        IReadOnlyList<IStem> IMusicAsset.Tracks        => m_Tracks;

        private void OnEnable()
        {
            CalculateLoopPoints();
        }

        private void OnValidate()
        {
            CalculateLoopPoints();
        }

        private void CalculateLoopPoints()
        {
            m_LoopStartTime   = 0.0;
            m_LoopEndTime     = 0.0;
            m_LoopPointsValid = false;

            if (!m_Loop)
            {
                return;
            }

            if (m_BPM <= 0f || m_BeatsPerBar <= 0)
            {
                Debug.LogWarning($"{nameof(MusicAsset)} [{name}] is marked to loop but has BPM [{m_BPM}] and beats per bar [{m_BeatsPerBar}]. Both must be greater than zero. It will play through without looping");

                return;
            }

            if (m_LoopStartBar < 1 || m_LoopEndBar <= m_LoopStartBar)
            {
                Debug.LogWarning($"{nameof(MusicAsset)} [{name}] is marked to loop but its loop runs from bar [{m_LoopStartBar}] to bar [{m_LoopEndBar}]. The end must come after the start and the first bar is 1. It will play through without looping");

                return;
            }

            m_LoopStartTime   = BarToSeconds(m_LoopStartBar - 1, m_BPM, m_BeatsPerBar);
            m_LoopEndTime     = BarToSeconds(m_LoopEndBar   - 1, m_BPM, m_BeatsPerBar);
            m_LoopPointsValid = true;
        }

        private static double BarToSeconds(int bar, float bpm, int beatsPerBar)
        {
            double secondsPerBeat = 60.0 / bpm;
            return bar * beatsPerBar * secondsPerBeat;
        }
    }
}