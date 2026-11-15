#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio
{
    /// <summary>
    /// What stops a looping song's or sound's stems looping: a source neither a WAV nor an OGG, as other formats can move
    /// the loop points, or too little audio after the loop's end for the player to jump back before a stem stops — it
    /// seeks once it sees the playhead past the end, up to a mixer buffer late.
    /// </summary>
    internal static class LoopedStems
    {
        // Several mixer buffers and a frame at any buffer size.
        internal const double MINIMUM_TAIL_SECONDS = 0.1;

        internal static bool IsLoopableFormat(string path)
        {
            string extension = Path.GetExtension(path);

            return string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase);
        }

        /// <returns>
        /// The first thing that stops <paramref name="stems" /> looping at <paramref name="loopEndSeconds" />, to follow
        /// "loops, but" in a warning, or null when nothing does.
        /// </returns>
        internal static string FindProblem(IReadOnlyList<IStem> stems, double loopEndSeconds)
        {
            for (int i = 0; i < stems.Count; i++)
            {
                AudioClip clip = stems[i].Clip;

                if (clip == null)
                {
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(clip);

                if (path.Length > 0 && !IsLoopableFormat(path))
                {
                    return $"stem [{i + 1}]'s source, [{path}], is neither a WAV nor an OGG. Only those can loop, as other formats can move the loop points";
                }

                double tail = clip.length - loopEndSeconds;

                if (tail < 0.0)
                {
                    return $"its loop ends [{-tail:0.000}] seconds past the end of stem [{i + 1}], [{clip.name}]";
                }

                if (tail < MINIMUM_TAIL_SECONDS)
                {
                    return $"its loop ends [{tail:0.000}] seconds before the end of stem [{i + 1}], [{clip.name}]. The player needs at least [{MINIMUM_TAIL_SECONDS}] seconds of audio after a loop's end to jump back before the stem stops, until it seeks on Unity 6.7's audio";
                }
            }

            return null;
        }
    }
}
#endif
