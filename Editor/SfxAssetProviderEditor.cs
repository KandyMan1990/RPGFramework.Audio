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

            Button generateSfxEventBtn = new Button(OnGenerateSfxEventsClicked)
                                         {
                                                 text = "Generate class for Sfx event data"
                                         };

            root.Add(generateEnumBtn);
            root.Add(generateSfxEventBtn);

            return root;
        }

        private void OnGenerateEnumButtonClicked()
        {
            m_AudioAssetProviderHelper = new AudioAssetProviderHelper<SfxAsset>();
            m_AudioAssetProviderHelper.OpenModal("Sfx", "Generate Sfx Asset Enum's", "SfxEnum.cs", "m_SfxAssets", serializedObject);
        }

        private void OnGenerateSfxEventsClicked()
        {
            m_SfxEventGeneratorEditor = new SfxEventGeneratorEditor();
            m_SfxEventGeneratorEditor.OpenModal(serializedObject);
        }
    }
}