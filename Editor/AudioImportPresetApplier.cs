using System.IO;
using UnityEditor;
using UnityEditor.Presets;

namespace RPGFramework.Audio.Editor
{
    public class AudioImportPresetApplier : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            string presetPath = FindPresetUpTree(assetPath);

            if (string.IsNullOrEmpty(presetPath))
            {
                return;
            }

            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
            preset.ApplyTo(assetImporter);
        }

        private static string FindPresetUpTree(string assetPath)
        {
            string currentFolder = Path.GetDirectoryName(assetPath);

            while (!string.IsNullOrEmpty(currentFolder) && currentFolder != "Assets")
            {
                string[] presetGUIDs = AssetDatabase.FindAssets("t:Preset", new[] { currentFolder });
                if (presetGUIDs.Length > 0)
                {
                    return AssetDatabase.GUIDToAssetPath(presetGUIDs[0]);
                }

                currentFolder = Path.GetDirectoryName(currentFolder);
            }

            return null;
        }
    }
}