using System.Collections.Generic;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// The note that gets the beat, i.e. the lower number of a time signature.
    /// The value of each member is that lower number, so it can be used directly in the bar length calculation.
    /// </summary>
    public enum NoteValue
    {
        Whole     = 1,
        Half      = 2,
        Quarter   = 4,
        Eighth    = 8,
        Sixteenth = 16
    }

    [CreateAssetMenu(fileName = "Music Asset", menuName = "RPG Framework/Audio/Music Asset")]
    public class MusicAsset : ScriptableObject, IMusicAsset
    {
        public const ulong NO_STATE_NAMED = 0;

        [SerializeField]
        private float m_BPM;

        [SerializeField]
        private int m_LoopStartBar;

        [SerializeField]
        private int m_LoopEndBar;

        [SerializeField]
        private int m_BeatsPerBar = 4;

        [SerializeField]
        private NoteValue m_BeatUnit = NoteValue.Quarter;

        [SerializeField]
        private bool m_Loop;

        [SerializeField]
        private List<Stem> m_Tracks;

        [SerializeField]
        private List<StemState> m_States;

        private Dictionary<ulong, StemState> m_StatesByNameHash;

        private double m_LoopStartTime;
        private double m_LoopEndTime;
        private bool   m_LoopPointsValid;

        double IMusicAsset.              LoopStartTime => m_LoopStartTime;
        double IMusicAsset.              LoopEndTime   => m_LoopEndTime;
        bool IMusicAsset.                Loop          => m_Loop && m_LoopPointsValid;
        IReadOnlyList<IStem> IMusicAsset.Tracks        => m_Tracks;

        bool[] IMusicAsset.GetStemsForState(ulong stateNameHash)
        {
            StemState stemState = stateNameHash == NO_STATE_NAMED
                                      ? m_States[0]
                                      : m_StatesByNameHash[stateNameHash];

            return stemState.ActiveStems;
        }

#if UNITY_EDITOR
        /// <summary>
        /// The state names a script can use with this asset, for tooling that offers them as a choice.
        /// </summary>
        public IEnumerable<string> StemStateNames
        {
            get
            {
                if (m_States == null)
                {
                    yield break;
                }

                foreach (StemState state in m_States)
                {
                    yield return state.Name;
                }
            }
        }
#endif

        private void OnEnable()
        {
            CalculateLoopPoints();
            EnsureStates();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CalculateLoopPoints();
            EnsureStates();
            WarnAboutDuplicateStateNames();
        }

        private void WarnAboutDuplicateStateNames()
        {
            if (m_States == null)
            {
                return;
            }

            if (m_StatesByNameHash.Count == m_States.Count)
            {
                return;
            }

            Debug.LogWarning($"{nameof(MusicAsset)} [{name}] has {m_States.Count} stem states but only {m_StatesByNameHash.Count} distinct names, so at least one can never be selected. Give every state its own name");
        }
#endif

        private void EnsureStates()
        {
            if (m_Tracks == null)
            {
                return;
            }

            m_States ??= new List<StemState>();

            if (m_States.Count == 0)
            {
                m_States.Add(StemState.CreateAllStemsOn(m_Tracks.Count));

                BuildStateLookup();

                return;
            }

            foreach (StemState state in m_States)
            {
                state.MatchStemCount(m_Tracks.Count);
            }

            BuildStateLookup();
        }

        private void BuildStateLookup()
        {
            m_StatesByNameHash = new Dictionary<ulong, StemState>(m_States.Count);

            foreach (StemState state in m_States)
            {
                m_StatesByNameHash[Fnv1a64.Hash(state.Name)] = state;
            }
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

            m_LoopStartTime   = BarToSeconds(m_LoopStartBar - 1, m_BPM, m_BeatsPerBar, m_BeatUnit);
            m_LoopEndTime     = BarToSeconds(m_LoopEndBar   - 1, m_BPM, m_BeatsPerBar, m_BeatUnit);
            m_LoopPointsValid = true;
        }

        private static double BarToSeconds(int bar, float bpm, int beatsPerBar, NoteValue beatUnit)
        {
            double secondsPerQuarterNote = 60.0 / bpm;
            double secondsPerBeat        = 4.0 / (int)beatUnit * secondsPerQuarterNote;
            double seconds               = bar * beatsPerBar * secondsPerBeat;

            return seconds;
        }
    }
}