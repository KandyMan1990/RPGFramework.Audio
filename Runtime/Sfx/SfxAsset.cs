using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [CreateAssetMenu(fileName = "SFX Asset", menuName = "RPG Framework/Audio/SFX Asset")]
    public sealed class SfxAsset : SfxPlayable, ISfxAsset
    {
        [SerializeField]
        private Stem[] m_Tracks = Array.Empty<Stem>();
        [SerializeField]
        private SfxEventData[] m_Events = Array.Empty<SfxEventData>();
        [SerializeField]
        private bool m_Loop;
        [SerializeField]
        private int m_LoopStart;
        [SerializeField]
        private int m_LoopEnd;

        private ISfxAsset[] m_Recordings;

        internal override IReadOnlyList<ISfxAsset> Recordings => m_Recordings ??= new ISfxAsset[] { this };

        internal override float PitchVariation => 0f;

        internal override bool PlaysInOrder => false;

        IReadOnlyList<IStem> IAudioAsset.      Tracks    => m_Tracks;
        IReadOnlyList<ISfxEventData> ISfxAsset.Events    => m_Events;
        bool ISfxAsset.                        Loop      => m_Loop;
        int ISfxAsset.                         LoopStart => m_LoopStart;
        int ISfxAsset.                         LoopEnd   => m_LoopEnd;

#if UNITY_EDITOR
        /// <summary>What stops the sound looping, as <see cref="LoopedStems.FindProblem" /> words it, or null.</summary>
        internal string FindLoopProblem()
        {
            if (!m_Loop || m_Tracks.Length == 0 || m_Tracks[0] == null || ((IStem)m_Tracks[0]).Clip == null)
            {
                return null;
            }

            // The loop is counted in the first stem's samples, as the player reads its playhead.
            return LoopedStems.FindProblem(m_Tracks, (double)m_LoopEnd / ((IStem)m_Tracks[0]).Clip.frequency);
        }

        private void OnValidate()
        {
            string problem = FindLoopProblem();

            if (problem != null)
            {
                Debug.LogWarning($"{nameof(SfxAsset)} [{name}] loops, but {problem}");
            }
        }
#endif
    }
}
