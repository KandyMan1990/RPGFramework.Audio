using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    /// <summary>
    /// The generator a player's source plays a stem through, everywhere but the web: what <see cref="AudioSource.clip" />
    /// told of a source before, as a source playing a generator holds no clip of its own. Read it from the source's
    /// <see cref="AudioSource.generator" />.
    /// </summary>
    public interface IStemGenerator : IAudioGenerator
    {
        /// <summary>The clip the stem plays; null between stems.</summary>
        AudioClip Clip { get; }
    }
}
