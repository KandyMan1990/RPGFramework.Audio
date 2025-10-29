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

        private string m_SelectedDirectory;

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
            m_SelectedDirectory = EditorPrefs.GetString($"{Application.productName}_SelectedDirectory", Application.dataPath);

            VisualElement root = rootVisualElement;

            root.style.paddingTop    = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft   = 10;
            root.style.paddingRight  = 10;
            root.style.flexDirection = FlexDirection.Column;

            VisualElement pathRow = new VisualElement
                                    {
                                            style =
                                            {
                                                    flexDirection = FlexDirection.Row,
                                                    alignItems    = Align.FlexEnd
                                            }
                                    };

            m_PathTextField = new TextField("Path:")
                              {
                                      value = m_SelectedDirectory
                              };
            m_PathTextField.style.flexGrow = 1;

            Button browseButton = new Button(OnBrowseClicked)
                                  {
                                          text = "Browse"
                                  };
            browseButton.style.flexShrink = 0;

            pathRow.Add(m_PathTextField);
            pathRow.Add(browseButton);

            m_FileNameTextField = new TextField("File Name:")
                                  {
                                          value = EditorPrefs.GetString($"{Application.productName}_FileName", "MusicEnum.cs")
                                  };

            m_NamespaceTextField = new TextField("Namespace:")
                                   {
                                           value = EditorPrefs.GetString($"{Application.productName}_Namespace", "MyNamespace")
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

            root.Add(pathRow);
            root.Add(m_FileNameTextField);
            root.Add(m_NamespaceTextField);
            root.Add(buttonRow);
        }

        private void OnBrowseClicked()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder", m_SelectedDirectory, string.Empty);

            if (!string.IsNullOrEmpty(folderPath))
            {
                m_SelectedDirectory   = folderPath;
                m_PathTextField.value = m_SelectedDirectory;
            }
        }

        private void OnButtonConfirm()
        {
            EditorPrefs.SetString($"{Application.productName}_SelectedDirectory", m_SelectedDirectory);
            EditorPrefs.SetString($"{Application.productName}_FileName",          m_FileNameTextField.value);
            EditorPrefs.SetString($"{Application.productName}_Namespace",         m_NamespaceTextField.value);

            m_OnConfirm?.Invoke(m_SelectedDirectory, m_FileNameTextField.value, m_NamespaceTextField.value);
            Close();
        }
    }
}