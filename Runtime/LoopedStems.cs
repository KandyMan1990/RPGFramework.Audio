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
    /// the loop points, a loop ending past a stem's end, or, where the playhead is watched, too little audio after the
    /// loop's end to jump back before a stem stops — the web seeks once it sees the playhead past the end, up to a mixer
    /// buffer late, where a generator of ours loops at the sample.
    /// </summary>
    internal static class LoopedStems
    {
        // Several mixer buffers and a frame at any buffer size.
        internal const double MINIMUM_TAIL_SECONDS = 0.1;

        /// <summary>Whether the build target is the web, whose songs and sounds loop by watching the playhead, so need a tail.</summary>
        internal static bool TargetsWeb => EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;

        internal static bool IsLoopableFormat(string path)
        {
            string extension = Path.GetExtension(path);

            return string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase);
        }

        /// <returns>
        /// The first thing that stops <paramref name="stems" /> looping at <paramref name="loopEndSeconds" />, the tail
        /// after it checked when <paramref name="watchesPlayhead" />, to follow "loops, but" in a warning, or null when
        /// nothing does.
        /// </returns>
        internal static string FindProblem(IReadOnlyList<IStem> stems, double loopEndSeconds, bool watchesPlayhead)
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

                // In samples, as a clip's length in seconds is a float, which falls short of a loop ending on its last
                // sample; half a sample spared for a clip imported at a rate other than the loop's.
                double tail = (double)clip.samples / clip.frequency - loopEndSeconds;

                if (tail < -0.5 / clip.frequency)
                {
                    return $"its loop ends [{-tail:0.000}] seconds past the end of stem [{i + 1}], [{clip.name}]";
                }

                if (watchesPlayhead && tail < MINIMUM_TAIL_SECONDS)
                {
                    return $"its loop ends [{Math.Max(tail, 0.0):0.000}] seconds before the end of stem [{i + 1}], [{clip.name}]. Where it watches the playhead, the player needs at least [{MINIMUM_TAIL_SECONDS}] seconds of audio after a loop's end to jump back before the stem stops";
                }
            }

            return null;
        }
    }
}
#endif
