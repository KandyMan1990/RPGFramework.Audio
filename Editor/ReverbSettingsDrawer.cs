using System;
using RPGFramework.Audio.Music;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    /// <summary>
    /// A song's reverb settings as two rows, each with a tick to apply it and a button back to PSX Reverb's default:
    /// the preset, and the volume as a 0 to 1 slider beside the 0 to 127 depth PSX Reverb shows for it.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReverbSettings))]
    internal sealed class ReverbSettingsDrawer : PropertyDrawer
    {
        private const string SET_PRESET     = "m_SetPreset";
        private const string PRESET         = "m_Preset";
        private const string SET_VOLUME     = "m_SetVolume";
        private const string VOLUME         = "m_Volume";
        private const string DEFAULT_BUTTON = "Default";
        private const float  DEPTH_WIDTH    = 48f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty setPreset = property.FindPropertyRelative(SET_PRESET);
            SerializedProperty preset    = property.FindPropertyRelative(PRESET);
            SerializedProperty setVolume = property.FindPropertyRelative(SET_VOLUME);
            SerializedProperty volume    = property.FindPropertyRelative(VOLUME);

            EnumField presetField = new EnumField();
            presetField.BindProperty(preset);

            Button presetDefault = new Button(() => SetPreset(preset, ReverbSettings.DEFAULT_PRESET))
                                   {
                                       text    = DEFAULT_BUTTON,
                                       tooltip = "Studio C, PSX Reverb's own default"
                                   };

            Slider volumeSlider = new Slider(0f, 1f) { showInputField = true };
            volumeSlider.BindProperty(volume);

            IntegerField depthField = new IntegerField
                                      {
                                          value   = ToDepth(volume.floatValue),
                                          tooltip = "The same volume as PSX Reverb's Depth, 0 to 127"
                                      };
            depthField.style.width = DEPTH_WIDTH;
            depthField.RegisterValueChangedCallback(changed => SetVolume(volume, changed.newValue / AudioUtils.MAX_REVERB_DEPTH));
            depthField.TrackPropertyValue(volume, changed => depthField.SetValueWithoutNotify(ToDepth(changed.floatValue)));

            Button volumeDefault = new Button(() => SetVolume(volume, ReverbSettings.DEFAULT_VOLUME))
                                   {
                                       text    = DEFAULT_BUTTON,
                                       tooltip = "Depth 40, PSX Reverb's own default"
                                   };

            Foldout foldout = new Foldout { text = property.displayName };
            foldout.Add(Row(setPreset, "Preset", "Switch the reverb to this preset when the song starts. A different preset cuts the reverb's tail",
                            presetField, presetDefault));
            foldout.Add(Row(setVolume, "Volume", "Set how loud the reverb plays for every sound, sound effects included, while the song is on",
                            volumeSlider, depthField, volumeDefault));

            return foldout;
        }

        /// <summary>
        /// A tick, labelled as an inspector field is, then what it applies, greyed while it is unticked.
        /// </summary>
        private static VisualElement Row(SerializedProperty set, string label, string tooltip, VisualElement value, params VisualElement[] after)
        {
            Toggle toggle = new Toggle(label) { tooltip = tooltip };
            toggle.BindProperty(set);
            toggle.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
            toggle.style.flexGrow = 0f;

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.Add(toggle);

            value.style.flexGrow = 1f;
            row.Add(value);

            for (int i = 0; i < after.Length; i++)
            {
                VisualElement element = after[i];

                row.Add(element);
            }

            SetApplied(row, toggle, set.boolValue);
            row.TrackPropertyValue(set, changed => SetApplied(row, toggle, changed.boolValue));

            return row;
        }

        private static void SetApplied(VisualElement row, Toggle toggle, bool applied)
        {
            foreach (VisualElement element in row.Children())
            {
                if (element != toggle)
                {
                    element.SetEnabled(applied);
                }
            }
        }

        private static void SetPreset(SerializedProperty preset, ReverbPreset value)
        {
            preset.intValue = (int)value;
            preset.serializedObject.ApplyModifiedProperties();
        }

        private static void SetVolume(SerializedProperty volume, float value)
        {
            volume.floatValue = Math.Clamp(value, 0f, 1f);
            volume.serializedObject.ApplyModifiedProperties();
        }

        // As PSX Reverb rounds a depth: half away from zero.
        private static int ToDepth(float volume)
        {
            int depth = (int)Math.Round(AudioUtils.ReverbVolumeToDepth(volume), MidpointRounding.AwayFromZero);

            return depth;
        }
    }
}
