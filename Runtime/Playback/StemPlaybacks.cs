using UnityEngine;

namespace RPGFramework.Audio
{
    /// <summary>
    /// Makes a channel's <see cref="IStemPlayback" />: on a generator of ours, or, for the web, which has no scriptable
    /// audio, on the source's own clip — in the editor too while the web is the build target, so play mode sounds as the
    /// build will.
    /// </summary>
    internal static class StemPlaybacks
    {
        internal static IStemPlayback Create(AudioSource source)
        {
#if UNITY_WEBGL
            return new PlayheadStemPlayback(source);
#else
            return new GeneratorStemPlayback(source);
#endif
        }
    }
}
