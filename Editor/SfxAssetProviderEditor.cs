using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    [CustomEditor(typeof(SfxAssetProvider))]
    internal class SfxAssetProviderEditor : UnityEditor.Editor
    {
        private AudioAssetProviderHelper<SfxAsset> m_AudioAssetProviderHelper;
        private SfxEventGeneratorEditor            m_SfxEventGeneratorEditor;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            Button generateEnumBtn = new Button(OnGenerateEnumButtonClicked)
                                     {
                                             text = "Generate enum for Sfx Asset Provider"
                                     };

            Button generateAllSfxEventsBtn = new Button(OnGenerateAllSfxEventsClicked)
                                             {
                                                     text = "Generate single class for all Sfx event data"
                                             };

            Button generateAllSfxEventsIndividuallyBtn = new Button(OnGenerateAllSfxEventsIndividuallyClicked)
                                                         {
                                                                 text = "Generate class per Sfx for its Sfx event data"
                                                         };

            root.Add(generateEnumBtn);
            root.Add(generateAllSfxEventsBtn);
            root.Add(generateAllSfxEventsIndividuallyBtn);
            root.Add(new Button(OnBuildBundlesClicked)
                     {
                         text    = "Build bundles",
                         tooltip = $"Builds a bundle per sound in StreamingAssets/{BundledSfxAssetProvider.FOLDER}, which {nameof(BundledSfxAssetProvider)} reads, for the editor's active platform"
                     });

            return root;
        }

        private void OnGenerateEnumButtonClicked()
        {
            m_AudioAssetProviderHelper = new AudioAssetProviderHelper<SfxAsset>();
            m_AudioAssetProviderHelper.OpenModal("Sfx", "Generate Sfx Asset Enum's", "SfxEnum.cs", "m_SfxAssets", serializedObject);
        }

        private void OnGenerateAllSfxEventsClicked()
        {
            m_SfxEventGeneratorEditor = new SfxEventGeneratorEditor();
            m_SfxEventGeneratorEditor.OpenModal(serializedObject, "SfxEvents", true);
        }

        private void OnBuildBundlesClicked()
        {
            AudioBundleBuilder.Build(AudioBundleBuilder.ListedAssets(serializedObject, "m_SfxAssets"), BundledSfxAssetProvider.FOLDER);
        }

        private void OnGenerateAllSfxEventsIndividuallyClicked()
        {
            m_SfxEventGeneratorEditor = new SfxEventGeneratorEditor();
            m_SfxEventGeneratorEditor.OpenModal(serializedObject, "IndividualSfxEvents", false);
        }
    }
}