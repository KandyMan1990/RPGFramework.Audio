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

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            Button button = new Button(OnButtonClicked)
                            {
                                    text = "Generate enum for Sfx Asset Provider"
                            };

            root.Add(button);

            return root;
        }

        private void OnButtonClicked()
        {
            m_AudioAssetProviderHelper = new AudioAssetProviderHelper<SfxAsset>();
            m_AudioAssetProviderHelper.OpenModal("Sfx", "m_SfxAssets", serializedObject);
        }
    }
}