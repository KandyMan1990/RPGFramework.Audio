using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    /// <summary>How a collection chooses which of its sounds to play.</summary>
    internal enum SfxOrder
    {
        /// <summary>Any of them, never the same one twice running.</summary>
        Random = 0,

        /// <summary>One after another and round again.</summary>
        InOrder = 1
    }

    /// <summary>
    /// A sound that varies: each time it plays, one of its sounds at a slightly different pitch. One sound with a pitch
    /// variation is a sound that only varies in pitch.
    /// </summary>
    [CreateAssetMenu(fileName = "SFX Collection", menuName = "RPG Framework/Audio/SFX Collection")]
    public sealed class SfxCollection : SfxPlayable
    {
        [SerializeField]
        [Tooltip("The sounds to pick from. They need not be listed in the provider as well")]
        private SfxAsset[] m_Sounds = Array.Empty<SfxAsset>();

        [SerializeField]
        [Tooltip("Random never plays the same sound twice running. In Order goes through them from the first, and something that counts its own plays, as a character's footsteps do, can start it over")]
        private SfxOrder m_Order;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("How far either way from normal the pitch can go each time it plays: 0.05 is 5% lower to 5% higher")]
        private float m_PitchVariation;

        internal override IReadOnlyList<ISfxAsset> Recordings => m_Sounds;

        internal override float PitchVariation => m_PitchVariation;

        internal override bool PlaysInOrder => m_Order == SfxOrder.InOrder;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_Sounds.Length == 0 || Array.IndexOf(m_Sounds, null) >= 0)
            {
                Debug.LogWarning($"{nameof(SfxCollection)} [{name}] needs at least one sound, and none left empty", this);
            }
        }
#endif
    }
}
