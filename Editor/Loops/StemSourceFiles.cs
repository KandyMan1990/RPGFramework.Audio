using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>A stem's source file, the WAV or OGG its clip was imported from, read for what Unity's import ignores.</summary>
    internal static class StemSourceFiles
    {
        /// <summary>The source file's sample rate, which an import can resample the clip away from.</summary>
        internal static bool TryReadRate(AudioClip clip, out int rate, out string problem)
        {
            rate = 0;

            if (!TryFindSource(clip, out string path, out bool wav, out problem))
            {
                return false;
            }

            using FileStream stream = File.OpenRead(FileUtil.GetPhysicalPath(path));

            bool read = wav ? WavLoopReader.TryReadRate(stream, out rate, out problem) : OggLoopReader.TryReadRate(stream, out rate, out problem);

            if (!read)
            {
                problem = $"[{path}] cannot be read: {problem}";
            }

            return read;
        }

        /// <summary>The source file's length and loop, as <see cref="WavLoopReader" /> and <see cref="OggLoopReader" /> read them.</summary>
        internal static bool TryReadMarkers(AudioClip clip, out StemMarkers markers, out string problem)
        {
            markers = null;

            if (!TryFindSource(clip, out string path, out bool wav, out problem))
            {
                return false;
            }

            using FileStream stream = File.OpenRead(FileUtil.GetPhysicalPath(path));

            bool read = wav ? WavLoopReader.TryReadMarkers(stream, out markers, out problem) : OggLoopReader.TryReadMarkers(stream, out markers, out problem);

            if (!read)
            {
                problem = $"[{path}] cannot be read: {problem}";
            }

            return read;
        }

        private static bool TryFindSource(AudioClip clip, out string path, out bool wav, out string problem)
        {
            path = null;
            wav  = false;

            if (clip == null)
            {
                problem = "it has no clip";

                return false;
            }

            path = AssetDatabase.GetAssetPath(clip);

            if (path.Length == 0)
            {
                problem = $"[{clip.name}] has no source file";

                return false;
            }

            wav = path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

            if (!wav && !path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                problem = $"[{path}] is neither a WAV nor an OGG";

                return false;
            }

            problem = null;

            return true;
        }
    }
}
