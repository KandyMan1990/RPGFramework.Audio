using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Audio.Sfx
{
    /// <summary>What a sound's name plays: one <see cref="SfxAsset" />, or an <see cref="SfxCollection" /> to pick from.</summary>
    public abstract class SfxPlayable : ScriptableObject
    {
        /// <summary>The recordings it can play.</summary>
        internal abstract IReadOnlyList<ISfxAsset> Recordings { get; }

        /// <summary>How far either way from normal its pitch varies each time it plays, 0 for none.</summary>
        internal abstract float PitchVariation { get; }

        /// <summary>Whether its recordings play one after another, rather than picked at random.</summary>
        internal abstract bool PlaysInOrder { get; }
    }
}
