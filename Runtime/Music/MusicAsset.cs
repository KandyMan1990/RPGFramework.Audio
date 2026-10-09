using System;
using System.Collections.Generic;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// The note that gets the beat, i.e. the lower number of a time signature.
    /// The value of each member is that lower number, so it can be used directly in the bar length calculation.
    /// </summary>
    internal enum NoteValue
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
        private const ulong NO_STATE_NAMED = 0;

        [SerializeField]
        [Tooltip("The song's tempo and time signature, in sections. The first starts at bar 1 and each runs until the next one starts, so a song that changes either loops by bar all the same")]
        private TempoSection[] m_Sections = Array.Empty<TempoSection>();

        [SerializeField]
        private int m_LoopStartBar;

        [SerializeField]
        private int m_LoopEndBar;

        [SerializeField]
        private bool m_Loop;

        [SerializeField]
        private Stem[] m_Tracks;

        [SerializeField]
        private StemState[] m_States;

        // Kept whether or not PSX Reverb is installed, so a song's settings survive the package being removed and added
        // back; Unity would drop them on the next save of a field compiled out. Shown only when it is installed.
        [SerializeField]
#if !RPGFRAMEWORK_PSXREVERB
        [HideInInspector]
#endif
        private ReverbSettings m_Reverb = new ReverbSettings();

        [SerializeField]
        private EchoSettings m_Echo = new EchoSettings();

        private Dictionary<ulong, StemState> m_StatesByNameHash;

        private double m_LoopStartTime;
        private double m_LoopEndTime;
        private bool   m_LoopPointsValid;

        string IMusicAsset.              Name          => name;
        double IMusicAsset.              LoopStartTime => m_LoopStartTime;
        double IMusicAsset.              LoopEndTime   => m_LoopEndTime;
        bool IMusicAsset.                Loop          => m_Loop && m_LoopPointsValid;
        IReadOnlyList<IStem> IMusicAsset.Tracks        => m_Tracks;
        ReverbSettings IMusicAsset.      Reverb        => m_Reverb;
        EchoSettings IMusicAsset.        Echo          => m_Echo;

        bool IMusicAsset.EchoFollowsTempo => m_Echo.Timing == EchoTiming.NoteLength && m_Sections != null && m_Sections.Length > 1;

        float IMusicAsset.GetEchoDelayMilliseconds(double seconds)
        {
            // A song whose sections cannot say has warned already; its echo falls back to 120 BPM rather than nothing.
            double secondsPerQuarterNote = TryFindSection(seconds, out TempoSection section, out _) ? section.SecondsPerQuarterNote : 0.5;
            float  delay                 = m_Echo.GetDelayMilliseconds(secondsPerQuarterNote);

            return delay;
        }

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
        internal IEnumerable<string> StemStateNames
        {
            get
            {
                if (m_States == null)
                {
                    yield break;
                }

                for (int i = 0; i < m_States.Length; i++)
                {
                    StemState state = m_States[i];

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
        /// <summary>
        /// The bar <paramref name="seconds" /> into the song is in, counted from 1 as the loop's bars are. False when the
        /// sections cannot say.
        /// </summary>
        internal bool TryGetBar(double seconds, out int bar)
        {
            bool found = TryFindSection(seconds, out TempoSection section, out double sectionStart);

            bar = found ? section.StartBar + (int)((seconds - sectionStart) / section.SecondsPerBar) : 0;

            return found;
        }

        /// <summary>
        /// A new song starts with one section, at 120 BPM in 4/4, for its author to change.
        /// </summary>
        private void Reset()
        {
            m_Sections = new[] { new TempoSection(1, 120f, 4, NoteValue.Quarter) };
        }

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

            if (m_StatesByNameHash.Count == m_States.Length)
            {
                return;
            }

            Debug.LogWarning($"{nameof(MusicAsset)} [{name}] has {m_States.Length} stem states but only {m_StatesByNameHash.Count} distinct names, so at least one can never be selected. Give every state its own name");
        }
#endif

        private void EnsureStates()
        {
            if (m_Tracks == null)
            {
                return;
            }

            if (m_States == null || m_States.Length == 0)
            {
                m_States = new[] { StemState.CreateAllStemsOn(m_Tracks.Length) };

                BuildStateLookup();

                return;
            }

            for (int i = 0; i < m_States.Length; i++)
            {
                StemState state = m_States[i];

                state.MatchStemCount(m_Tracks.Length);
            }

            BuildStateLookup();
        }

        private void BuildStateLookup()
        {
            m_StatesByNameHash = new Dictionary<ulong, StemState>(m_States.Length);

            for (int i = 0; i < m_States.Length; i++)
            {
                StemState state = m_States[i];

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

            string sectionProblem = FindSectionProblem();

            if (sectionProblem != null)
            {
                Debug.LogWarning($"{nameof(MusicAsset)} [{name}] is marked to loop but {sectionProblem}. It will play through without looping");

                return;
            }

            if (m_LoopStartBar < 1 || m_LoopEndBar <= m_LoopStartBar)
            {
                Debug.LogWarning($"{nameof(MusicAsset)} [{name}] is marked to loop but its loop runs from bar [{m_LoopStartBar}] to bar [{m_LoopEndBar}]. The end must come after the start and the first bar is 1. It will play through without looping");

                return;
            }

            m_LoopStartTime   = BarToSeconds(m_LoopStartBar);
            m_LoopEndTime     = BarToSeconds(m_LoopEndBar);
            m_LoopPointsValid = true;
        }

        /// <returns>What stops the sections giving a bar its time, or null when nothing does.</returns>
        private string FindSectionProblem()
        {
            if (m_Sections == null || m_Sections.Length == 0)
            {
                return "has no tempo sections";
            }

            if (m_Sections[0].StartBar != 1)
            {
                return $"its first tempo section starts at bar [{m_Sections[0].StartBar}] rather than bar 1";
            }

            for (int i = 0; i < m_Sections.Length; i++)
            {
                TempoSection section = m_Sections[i];

                if (!section.HasTempo)
                {
                    return $"its tempo section at bar [{section.StartBar}] needs a BPM and beats per bar greater than zero";
                }

                if (i > 0 && section.StartBar <= m_Sections[i - 1].StartBar)
                {
                    return $"its tempo section at bar [{section.StartBar}] does not start after the one before it, at bar [{m_Sections[i - 1].StartBar}]";
                }
            }

            return null;
        }

        /// <summary>
        /// The section <paramref name="seconds" /> into the song is in, and when it starts. False when the sections cannot
        /// say.
        /// </summary>
        private bool TryFindSection(double seconds, out TempoSection section, out double sectionStart)
        {
            section      = null;
            sectionStart = 0.0;

            if (FindSectionProblem() != null)
            {
                return false;
            }

            for (int i = 0; i < m_Sections.Length; i++)
            {
                section = m_Sections[i];

                bool   isLast = i == m_Sections.Length - 1;
                double length = isLast ? double.MaxValue : (m_Sections[i + 1].StartBar - section.StartBar) * section.SecondsPerBar;

                if (seconds < sectionStart + length)
                {
                    return true;
                }

                sectionStart += length;
            }

            return true;
        }

        /// <summary>
        /// When <paramref name="bar" />, counted from 1, starts: every whole section before it, then its place in its own.
        /// </summary>
        private double BarToSeconds(int bar)
        {
            double seconds = 0.0;

            for (int i = 0; i < m_Sections.Length; i++)
            {
                TempoSection section = m_Sections[i];
                int          end     = i == m_Sections.Length - 1 ? int.MaxValue : m_Sections[i + 1].StartBar;
                int          bars    = Math.Min(bar, end) - section.StartBar;

                if (bars <= 0)
                {
                    break;
                }

                seconds += bars * section.SecondsPerBar;
            }

            return seconds;
        }
    }
}