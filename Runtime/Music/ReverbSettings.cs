using System;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// What a song does to PSX Reverb when it starts. Each setting is optional: one left unticked leaves the reverb as
    /// the last song or script set it.
    /// </summary>
    [Serializable]
    internal sealed class ReverbSettings
    {
        // PSX Reverb's own defaults: studio C, at depth 40 of 127.
        internal const ReverbPreset DEFAULT_PRESET = ReverbPreset.StudioC;
        internal const float        DEFAULT_VOLUME = 40f / AudioUtils.MAX_REVERB_DEPTH;

        [SerializeField]
        private bool m_SetPreset;

        [SerializeField]
        private ReverbPreset m_Preset = DEFAULT_PRESET;

        [SerializeField]
        private bool m_SetVolume;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_Volume = DEFAULT_VOLUME;

        internal bool         SetsPreset => m_SetPreset;
        internal ReverbPreset Preset     => m_Preset;
        internal bool         SetsVolume => m_SetVolume;
        internal float        Volume     => m_Volume;
    }
}
