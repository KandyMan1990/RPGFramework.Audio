using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [CreateAssetMenu(fileName = "SFX Asset", menuName = "RPG Framework/Audio/SFX Asset")]
    public class SfxAsset : ScriptableObject, ISfxAsset
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

        IReadOnlyList<IStem> IAudioAsset.      Tracks    => m_Tracks;
        IReadOnlyList<ISfxEventData> ISfxAsset.Events    => m_Events;
        bool ISfxAsset.                        Loop      => m_Loop;
        int ISfxAsset.                         LoopStart => m_LoopStart;
        int ISfxAsset.                         LoopEnd   => m_LoopEnd;
    }
}