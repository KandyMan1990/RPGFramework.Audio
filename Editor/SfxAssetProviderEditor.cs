using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    [CustomEditor(typeof(SfxAssetProvider))]
    public class SfxAssetProviderEditor : UnityEditor.Editor
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

        private void OnGenerateAllSfxEventsIndividuallyClicked()
        {
            m_SfxEventGeneratorEditor = new SfxEventGeneratorEditor();
            m_SfxEventGeneratorEditor.OpenModal(serializedObject, "IndividualSfxEvents", false);
        }
    }
}