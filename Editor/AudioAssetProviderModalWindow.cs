using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    public class AudioAssetProviderModalWindow : EditorWindow
    {
        public event Action<string, string, string> OnConfirm;

        private TextField m_PathTextField;
        private TextField m_FileNameTextField;
        private TextField m_NamespaceTextField;

        private string m_SelectedDirectory;
        private string m_AssetType;
        private string m_FileName;

        public void Init(string assetType, string windowTitle, string filename)
        {
            m_AssetType         = assetType;
            m_FileName          = filename;
            titleContent        = new GUIContent(windowTitle);
            minSize             = new Vector2(600, 200);
            m_SelectedDirectory = EditorPrefs.GetString($"{Application.productName}_SelectedDirectory_{m_AssetType}", Application.dataPath);
            ShowModal();
        }

        public void CreateGUI()
        {
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

            if (m_FileName != null)
            {
                m_FileNameTextField = new TextField("File Name:")
                                      {
                                              value = EditorPrefs.GetString($"{Application.productName}_FileName_{m_AssetType}", m_FileName)
                                      };
            }

            m_NamespaceTextField = new TextField("Namespace:")
                                   {
                                           value = EditorPrefs.GetString($"{Application.productName}_Namespace_{m_AssetType}", "MyNamespace")
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
                                            text = "Generate"
                                    };

            buttonRow.Add(continueButton);

            root.Add(pathRow);
            if (m_FileName != null)
            {
                root.Add(m_FileNameTextField);
            }
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
            EditorPrefs.SetString($"{Application.productName}_SelectedDirectory_{m_AssetType}", m_SelectedDirectory);
            if (m_FileName != null)
            {
                EditorPrefs.SetString($"{Application.productName}_FileName_{m_AssetType}", m_FileNameTextField.value);
            }
            EditorPrefs.SetString($"{Application.productName}_Namespace_{m_AssetType}",         m_NamespaceTextField.value);

            OnConfirm?.Invoke(m_SelectedDirectory, m_FileNameTextField?.value, m_NamespaceTextField.value);
            Close();
        }
    }
}