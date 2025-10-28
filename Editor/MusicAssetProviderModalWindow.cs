using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    public class MusicAssetProviderModalWindow : EditorWindow
    {
        private TextField                      m_PathTextField;
        private TextField                      m_FileNameTextField;
        private TextField                      m_NamespaceTextField;
        private Action<string, string, string> m_OnConfirm;

        public static void ShowWindow(Action<string, string, string> onConfirm)
        {
            MusicAssetProviderModalWindow window = CreateInstance<MusicAssetProviderModalWindow>();

            window.titleContent = new GUIContent("Generate Music Asset Enum's");
            window.minSize      = new Vector2(600, 200);
            window.m_OnConfirm  = onConfirm;
            window.ShowModal();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            root.style.paddingTop    = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft   = 10;
            root.style.paddingRight  = 10;
            root.style.flexDirection = FlexDirection.Column;

            m_PathTextField = new TextField("Path:")
                                        {
                                                value = Application.dataPath
                                        };

            m_FileNameTextField = new TextField("File Name:")
                                  {
                                          value = "MusicEnum.cs"
                                  };

            m_NamespaceTextField = new TextField("Namespace:")
                                   {
                                           value = "MyNamespace"
                                   };

            VisualElement buttonRow = new VisualElement
                                      {
                                              style =
                                              {
                                                      flexDirection  = FlexDirection.Row,
                                                      justifyContent = Justify.SpaceBetween
                                              }
                                      };

            Button continueButton = new Button(OnButtonConfirm)
                                    {
                                            text = "Generate Music Asset Enum's"
                                    };

            buttonRow.Add(continueButton);

            root.Add(m_PathTextField);
            root.Add(m_FileNameTextField);
            root.Add(m_NamespaceTextField);
            root.Add(buttonRow);
        }

        private void OnButtonConfirm()
        {
            m_OnConfirm?.Invoke(m_PathTextField.value, m_FileNameTextField.value, m_NamespaceTextField.value);
            Close();
        }
    }
}