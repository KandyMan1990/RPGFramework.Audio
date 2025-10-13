using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    [CreateAssetMenu(fileName = "SFX Asset", menuName = "RPG Framework/SFX Asset")]
    public class SfxAsset : ScriptableObject, ISfxAsset
    {
        [SerializeField]
        private List<Stem> m_Tracks = new List<Stem>();
        [SerializeField]
        private List<SfxEventData> m_Events = new List<SfxEventData>();
        [SerializeField]
        private bool m_Loop;
        [SerializeField]
        private int m_LoopStart;
        [SerializeField]
        private int m_LoopEnd;

        public IReadOnlyList<IStem>         Tracks    => m_Tracks;
        public IReadOnlyList<ISfxEventData> Events    => m_Events;
        public bool                         Loop      => m_Loop;
        public int                          LoopStart => m_LoopStart;
        public int                          LoopEnd   => m_LoopEnd;
    }
}