using System.Collections.Generic;
using System.IO;
using RPGFramework.Audio.Bundles;
using RPGFramework.Hashing;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Editor
{
    /// <summary>
    /// Builds one bundle per song or sound a provider lists, named by its name hash, which is how a bundled provider finds
    /// it. Unity rebuilds only the bundles whose assets changed, and this removes the bundles of assets no longer listed,
    /// so building a large library again is quick.
    /// </summary>
    internal static class AudioBundleBuilder
    {
        // Strict, so an error fails the build rather than leaving some bundles missing. Type trees are kept, so a content
        // pack built against one version of the game still loads in a later one whose scripts have changed.
        internal const BuildAssetBundleOptions OPTIONS = BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression;

        internal static void Build(IReadOnlyList<ScriptableObject> assets, string folder, BuildAssetBundleOptions options = OPTIONS)
        {
            string output = $"{Application.streamingAssetsPath}/{folder}";

            List<AssetBundleBuild> builds      = new List<AssetBundleBuild>(assets.Count);
            List<string>           bundleNames = new List<string>(assets.Count);

            for (int i = 0; i < assets.Count; i++)
            {
                ScriptableObject asset = assets[i];

                if (asset == null)
                {
                    continue;
                }

                string bundleName = Fnv1a64.Hash(asset.name).ToString();

                if (bundleNames.Contains(bundleName))
                {
                    Debug.LogError($"{nameof(AudioBundleBuilder)}::{nameof(Build)} Not building — two assets are named [{asset.name}], and a name is how each is asked for");

                    return;
                }

                bundleNames.Add(bundleName);
                builds.Add(new AssetBundleBuild
                           {
                               assetBundleName = bundleName,
                               assetNames      = new[] { AssetDatabase.GetAssetPath(asset) }
                           });
            }

            WarnAboutSharedClips(assets);

            Directory.CreateDirectory(output);

            AssetBundleManifest built = BuildPipeline.BuildAssetBundles(output, builds.ToArray(), options, EditorUserBuildSettings.activeBuildTarget);

            if (built == null)
            {
                Debug.LogError($"{nameof(AudioBundleBuilder)}::{nameof(Build)} Building the bundles in [{output}] failed; the console above says why");

                return;
            }

            RemoveUnlisted(output, bundleNames);

            AssetDatabase.Refresh();

            Debug.Log($"{nameof(AudioBundleBuilder)}::{nameof(Build)} Built [{bundleNames.Count}] bundle(s) in [{output}] for [{EditorUserBuildSettings.activeBuildTarget}]");
        }

        /// <summary>The assets a provider's inspector lists, in its serialized array.</summary>
        internal static ScriptableObject[] ListedAssets(SerializedObject provider, string arrayProperty)
        {
            SerializedProperty array  = provider.FindProperty(arrayProperty);
            ScriptableObject[] listed = new ScriptableObject[array.arraySize];

            for (int i = 0; i < listed.Length; i++)
            {
                listed[i] = (ScriptableObject)array.GetArrayElementAtIndex(i).objectReferenceValue;
            }

            return listed;
        }

        // A clip two assets use goes into both their bundles, so it ships twice and loads twice.
        private static void WarnAboutSharedClips(IReadOnlyList<ScriptableObject> assets)
        {
            Dictionary<string, string> firstUser = new Dictionary<string, string>();

            for (int i = 0; i < assets.Count; i++)
            {
                ScriptableObject asset = assets[i];

                if (asset == null)
                {
                    continue;
                }

                string[] dependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(asset), true);

                for (int j = 0; j < dependencies.Length; j++)
                {
                    string dependency = dependencies[j];

                    if (AssetDatabase.GetMainAssetTypeAtPath(dependency) != typeof(AudioClip))
                    {
                        continue;
                    }

                    if (firstUser.TryGetValue(dependency, out string other))
                    {
                        Debug.LogWarning($"{nameof(AudioBundleBuilder)}::{nameof(Build)} [{dependency}] is used by both [{other}] and [{asset.name}], so each bundle carries its own copy");

                        continue;
                    }

                    firstUser.Add(dependency, asset.name);
                }
            }
        }

        // Every bundle file this build did not write, with Unity's .manifest beside it, but for the folder's own manifest
        // bundle, which Unity names after the folder.
        private static void RemoveUnlisted(string output, List<string> bundleNames)
        {
            string   folderBundle = Path.GetFileName(output);
            string[] files        = Directory.GetFiles(output);

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string name = Path.GetFileName(file);

                if (name.EndsWith(".meta"))
                {
                    continue;
                }

                string bundle = name.EndsWith(".manifest") ? name.Substring(0, name.Length - ".manifest".Length) : name;

                if (bundle == folderBundle || bundleNames.Contains(bundle))
                {
                    continue;
                }

                File.Delete(file);

                if (File.Exists(file + ".meta"))
                {
                    File.Delete(file + ".meta");
                }
            }
        }
    }
}