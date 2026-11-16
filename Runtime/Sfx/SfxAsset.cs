using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    public sealed class SfxAsset : SfxPlayable, ISfxAsset
    {
        [SerializeField]
        private Stem[] m_Tracks = Array.Empty<Stem>();
        [SerializeField]
        [Tooltip("Each event's time, in samples at the sample rate below")]
        private SfxEventData[] m_Events = Array.Empty<SfxEventData>();
        [SerializeField]
        private bool m_Loop;
        [SerializeField]
        [Tooltip("The sample the loop starts at, counted from 0 at the sample rate")]
        private int m_LoopStart;
        [SerializeField]
        [Tooltip("The sample the loop ends at, where it goes back to its start")]
        private int m_LoopEnd;
        [SerializeField]
        [Tooltip("The rate the loop's and the events' samples are counted at: the stems' source files', read from the first stem's file. Never the clip's, which an import can resample")]
        private int m_SampleRate;

        private ISfxAsset[] m_Recordings;

        internal override IReadOnlyList<ISfxAsset> Recordings => m_Recordings ??= new ISfxAsset[] { this };

        internal override float PitchVariation => 0f;

        internal override bool PlaysInOrder => false;

        IReadOnlyList<IStem> IAudioAsset.      Tracks     => m_Tracks;
        IReadOnlyList<ISfxEventData> ISfxAsset.Events     => m_Events;
        // Only a loop with a rate to count it at, whose end comes after its start, as a song's; another is warned of where
        // it is authored.
        bool ISfxAsset.                        Loop       => m_Loop && m_SampleRate > 0 && m_LoopEnd > m_LoopStart;
        int ISfxAsset.                         LoopStart  => m_LoopStart;
        int ISfxAsset.                         LoopEnd    => m_LoopEnd;
        int ISfxAsset.                         SampleRate => m_SampleRate;

#if UNITY_EDITOR
        /// <summary>
        /// What stops the sound looping, as <see cref="LoopedStems.FindProblem" /> words it, or null; the tail after its
        /// loop checked only <paramref name="forWeb" />, where the player watches the playhead.
        /// </summary>
        internal string FindLoopProblem(bool forWeb)
        {
            if (!m_Loop || m_Tracks.Length == 0 || m_Tracks[0] == null || ((IStem)m_Tracks[0]).Clip == null)
            {
                return null;
            }

            if (m_SampleRate <= 0)
            {
                return "it has no sample rate to count its loop at, so it plays through without looping. Read it from the first stem's file in the inspector";
            }

            if (m_LoopEnd <= m_LoopStart)
            {
                return $"its loop ends at sample [{m_LoopEnd}], not after its start at [{m_LoopStart}], so it plays through without looping";
            }

            return LoopedStems.FindProblem(m_Tracks, (double)m_LoopEnd / m_SampleRate, forWeb);
        }

        private void OnValidate()
        {
            string problem = FindLoopProblem(LoopedStems.TargetsWeb);

            if (problem != null)
            {
                Debug.LogWarning($"{nameof(SfxAsset)} [{name}] loops, but {problem}");
            }

            if (m_Events.Length > 0 && m_SampleRate <= 0)
            {
                Debug.LogWarning($"{nameof(SfxAsset)} [{name}] has events but no sample rate to time them at. Read it from the first stem's file in the inspector");
            }
        }
#endif
    }
}
