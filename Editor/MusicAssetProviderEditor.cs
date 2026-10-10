using RPGFramework.Audio.Music;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    [CustomEditor(typeof(MusicAssetProvider))]
    internal class MusicAssetProviderEditor : UnityEditor.Editor
    {
        private AudioAssetProviderHelper<MusicAsset> m_AudioAssetProviderHelper;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            Button button = new Button(OnButtonClicked)
                            {
                                    text = "Generate enum for Music Asset Provider"
                            };

            root.Add(button);
            root.Add(new Button(OnBuildBundlesClicked)
                     {
                         text    = "Build bundles",
                         tooltip = $"Builds a bundle per song in StreamingAssets/{BundledMusicAssetProvider.FOLDER}, which {nameof(BundledMusicAssetProvider)} reads, for the editor's active platform"
                     });

            return root;
        }

        private void OnBuildBundlesClicked()
        {
            AudioBundleBuilder.Build(AudioBundleBuilder.ListedAssets(serializedObject, "m_MusicAssets"), BundledMusicAssetProvider.FOLDER);
        }

        private void OnButtonClicked()
        {
            m_AudioAssetProviderHelper = new AudioAssetProviderHelper<MusicAsset>();
            m_AudioAssetProviderHelper.OpenModal("Music", "Generate Music Asset Enum's", "MusicEnum.cs", "m_MusicAssets", serializedObject);
        }
    }
}