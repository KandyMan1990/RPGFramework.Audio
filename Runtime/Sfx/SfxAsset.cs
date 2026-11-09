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
    }
}