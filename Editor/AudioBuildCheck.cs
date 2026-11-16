using System.Collections.Generic;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RPGFramework.Audio.Editor
{
    /// <summary>
    /// Refuses to build a player while a looping song or sound in the project cannot loop — a stem neither a WAV nor an
    /// OGG, a loop ending past a stem's end or not after its start, or, for the web, where the player watches the
    /// playhead, too little audio after the loop's end — since in play it would not loop as authored, and nothing at run
    /// time can tell.
    /// <see cref="AudioBundleBuilder" /> refuses the same for the songs and sounds it bundles.
    /// </summary>
    internal sealed class AudioBuildCheck : IPreprocessBuildWithReport
    {
        int IOrderedCallback.callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            string[]               guids  = AssetDatabase.FindAssets($"t:{nameof(MusicAsset)} t:{nameof(SfxPlayable)}");
            List<ScriptableObject> assets = new List<ScriptableObject>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                assets.Add(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guids[i])));
            }

            List<string> problems = FindProblems(assets, report.summary.platform == BuildTarget.WebGL);

            if (problems.Count > 0)
            {
                throw new BuildFailedException($"{problems.Count} looping song(s) or sound(s) cannot loop:\n{string.Join("\n", problems)}");
            }
        }

        /// <returns>
        /// Every song and sound among <paramref name="assets" />, a collection's sounds included, that cannot loop in a
        /// build for the web or not (<paramref name="forWeb" />), a line each.
        /// </returns>
        internal static List<string> FindProblems(IReadOnlyList<ScriptableObject> assets, bool forWeb)
        {
            List<string> problems = new List<string>();

            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] is MusicAsset song)
                {
                    Add(problems, song, song.FindLoopProblem(forWeb));

                    continue;
                }

                if (assets[i] is not SfxPlayable playable)
                {
                    continue;
                }

                IReadOnlyList<ISfxAsset> recordings = playable.Recordings;

                for (int j = 0; j < recordings.Count; j++)
                {
                    if (recordings[j] is SfxAsset sound)
                    {
                        Add(problems, sound, sound.FindLoopProblem(forWeb));
                    }
                }
            }

            return problems;
        }

        private static void Add(List<string> problems, ScriptableObject asset, string problem)
        {
            if (problem == null)
            {
                return;
            }

            string line = $"[{AssetDatabase.GetAssetPath(asset)}] loops, but {problem}";

            if (!problems.Contains(line))
            {
                problems.Add(line);
            }
        }
    }
}
