using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Editor
{
    public class SfxEventGeneratorEditor
    {
        private AudioAssetProviderModalWindow m_Window;
        private SerializedObject              m_SerializedObject;
        private bool                          m_CombineToSingleClass;

        internal void OpenModal(SerializedObject serializedObject, string assetType, bool combineToSingleClass)
        {
            m_SerializedObject     = serializedObject;
            m_CombineToSingleClass = combineToSingleClass;

            m_Window           =  ScriptableObject.CreateInstance<AudioAssetProviderModalWindow>();
            m_Window.OnConfirm += OnGenerate;

            string filename = combineToSingleClass ? $"{assetType}.cs" : null;

            m_Window.Init(assetType, "Generate Sfx Events class", filename);
        }

        private void OnGenerate(string path, string filename, string namespaceForFile)
        {
            m_Window.OnConfirm -= OnGenerate;
            m_Window           =  null;

            SerializedProperty sfxAssets = m_SerializedObject.FindProperty("m_SfxAssets");

            if (m_CombineToSingleClass)
            {
                GenerateCombinedClass(sfxAssets, path, filename, namespaceForFile);
            }
            else
            {
                GenerateIndividualClasses(sfxAssets, path, namespaceForFile);
            }

            AssetDatabase.Refresh();
        }

        private static void GenerateCombinedClass(SerializedProperty sfxAssetsSerializedProperty, string path, string filename, string namespaceForFile)
        {
            OrderedDictionary sfxEvents = new OrderedDictionary();

            SfxAsset[] sfxAssets = GetSfxAssets(sfxAssetsSerializedProperty);

            for (int i = 0; i < sfxAssets.Length; i++)
            {
                SfxAsset           sfxAsset      = sfxAssets[i];
                ISfxEventData[]    sfxEventData  = GetSfxAssetEvents(sfxAsset);
                (string, string)[] sfxEventNames = GetSfxAssetEventNames(sfxEventData);

                for (int j = 0; j < sfxEventNames.Length; j++)
                {
                    (string key, string value) = sfxEventNames[j];

                    if (!sfxEvents.Contains(key))
                    {
                        sfxEvents.Add(key, value);
                    }
                }
            }

            CreateDirectory(path);

            string filePath = Path.Combine(path, filename);

            ClearFile(filePath);

            StringBuilder sb        = new StringBuilder();
            string        className = filename.Split('.')[0];

            GenerateStartOfFile(sb, namespaceForFile);
            GenerateEnumContents(sb, className, sfxEvents);
            GenerateClassContents(sb, className, sfxEvents);
            GenerateEndOfFile(sb);

            File.WriteAllText(filePath, sb.ToString());
        }

        private static void GenerateIndividualClasses(SerializedProperty sfxAssetsSerializedProperty, string path, string namespaceForFile)
        {
            SfxAsset[] sfxAssets = GetSfxAssets(sfxAssetsSerializedProperty);

            for (int i = 0; i < sfxAssets.Length; i++)
            {
                OrderedDictionary sfxEvents = new OrderedDictionary();

                SfxAsset           sfxAsset      = sfxAssets[i];
                ISfxEventData[]    sfxEventData  = GetSfxAssetEvents(sfxAsset);
                (string, string)[] sfxEventNames = GetSfxAssetEventNames(sfxEventData);

                for (int j = 0; j < sfxEventNames.Length; j++)
                {
                    (string key, string value) = sfxEventNames[j];

                    if (!sfxEvents.Contains(key))
                    {
                        sfxEvents.Add(key, value);
                    }
                }

                CreateDirectory(path);

                string filePath = Path.Combine(path, $"{sfxAsset.name}.cs");

                ClearFile(filePath);

                StringBuilder sb        = new StringBuilder();
                string        className = sfxAsset.name;

                GenerateStartOfFile(sb, namespaceForFile);
                GenerateEnumContents(sb, className, sfxEvents);
                GenerateClassContents(sb, className, sfxEvents);
                GenerateEndOfFile(sb);

                File.WriteAllText(filePath, sb.ToString());
            }
        }

        private static SfxAsset[] GetSfxAssets(SerializedProperty sfxAssets)
        {
            SfxAsset[] assets = new SfxAsset[sfxAssets.arraySize];

            for (int i = 0; i < sfxAssets.arraySize; i++)
            {
                SerializedProperty sfxAssetSerializedProperty = sfxAssets.GetArrayElementAtIndex(i);
                SfxAsset           sfxAsset                   = (SfxAsset)sfxAssetSerializedProperty.objectReferenceValue;

                assets[i] = sfxAsset;
            }

            return assets;
        }

        private static ISfxEventData[] GetSfxAssetEvents(SfxAsset sfxAsset)
        {
            SerializedObject   sfxAssetSerializedObject = new SerializedObject(sfxAsset);
            SerializedProperty events                   = sfxAssetSerializedObject.FindProperty("m_Events");

            ISfxEventData[] sfxEventDataArray = new ISfxEventData[events.arraySize];

            for (int i = 0; i < sfxEventDataArray.Length; i++)
            {
                SerializedProperty sfxEventProperty = events.GetArrayElementAtIndex(i);
                ISfxEventData      sfxEventData     = (ISfxEventData)sfxEventProperty.boxedValue;

                sfxEventDataArray[i] = sfxEventData;
            }

            return sfxEventDataArray;
        }

        private static (string key, string value)[] GetSfxAssetEventNames(ISfxEventData[] sfxEventData)
        {
            (string key, string value)[] names = new (string key, string value)[sfxEventData.Length];

            for (int i = 0; i < sfxEventData.Length; i++)
            {
                string eventName = sfxEventData[i].EventName;
                string key       = ToUpperSnakeCase(eventName);

                names[i] = (key, eventName);
            }

            return names;
        }

        private static void GenerateStartOfFile(StringBuilder sb, string namespaceForFile)
        {
            sb.AppendLine("// THIS FILE IS AUTOGENERATED, DO NOT MODIFY");
            sb.AppendLine($"// Last generated at {DateTime.UtcNow}\n");

            sb.AppendLine("using System;");
            sb.AppendLine();

            sb.AppendLine($"namespace {namespaceForFile}");
            sb.AppendLine("{");
        }

        private static void GenerateEnumContents(StringBuilder sb, string className, OrderedDictionary sfxEvents)
        {
            sb.AppendLine($"\tpublic enum {className}Enum");
            sb.AppendLine("\t{");

            int i = 0;
            foreach (DictionaryEntry dictionaryEntry in sfxEvents)
            {
                sb.AppendLine($"\t\t{dictionaryEntry.Key} = {i},");
                i++;
            }

            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        private static void GenerateClassContents(StringBuilder sb, string className, OrderedDictionary sfxEvents)
        {
            sb.AppendLine($"\tpublic static class {className}");
            sb.AppendLine("\t{");

            foreach (DictionaryEntry dictionaryEntry in sfxEvents)
            {
                sb.AppendLine($"\t\tpublic const string {dictionaryEntry.Key} = \"{dictionaryEntry.Value}\";");
            }

            sb.AppendLine();
            sb.AppendLine($"\t\tpublic static string GetByEnum({className}Enum value)");
            sb.AppendLine("\t\t{");

            sb.AppendLine("\t\t\treturn value switch");
            sb.AppendLine("\t\t\t{");

            foreach (DictionaryEntry dictionaryEntry in sfxEvents)
            {
                sb.AppendLine($"\t\t\t\t{className}Enum.{dictionaryEntry.Key} => {dictionaryEntry.Key},");
            }

            sb.AppendLine("\t\t\t\t_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)");

            sb.AppendLine("\t\t\t};");

            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");

            //TODO: create static classes for each SfxAsset so we can do Sfx_00.LoopStart, gives us flexibility
            //TODO: add checkboxes to the modal to decide if we want the enum, the generic class with all strings in, the individual sfx classes, or any combination
        }

        private static void GenerateEndOfFile(StringBuilder sb)
        {
            sb.AppendLine("}");
        }

        private static string ToUpperSnakeCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            string result = Regex.Replace(input, @"(?<=[a-z0-9])([A-Z])", "_$1");
            result = Regex.Replace(result, @"(?<=[A-Za-z])([0-9])", "_$1");
            result = Regex.Replace(result, @"(?<=[0-9])([A-Za-z])", "_$1");
            result = Regex.Replace(result, @"_+",                   "_");

            return result.ToUpperInvariant();
        }

        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void ClearFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}