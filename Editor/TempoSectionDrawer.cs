using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    /// <summary>
    /// A song's tempo section, headed with what it says — <c>From bar 9: 90 BPM, 6/8</c> — so the list reads as the song's
    /// tempo map rather than as numbered elements.
    /// </summary>
    [CustomPropertyDrawer(typeof(Music.TempoSection))]
    internal sealed class TempoSectionDrawer : PropertyDrawer
    {
        private const string START_BAR     = "m_StartBar";
        private const string BPM           = "m_BPM";
        private const string BEATS_PER_BAR = "m_BeatsPerBar";
        private const string BEAT_UNIT     = "m_BeatUnit";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty[] fields =
            {
                property.FindPropertyRelative(START_BAR),
                property.FindPropertyRelative(BPM),
                property.FindPropertyRelative(BEATS_PER_BAR),
                property.FindPropertyRelative(BEAT_UNIT)
            };

            Foldout foldout = new Foldout { text = Describe(property) };

            for (int i = 0; i < fields.Length; i++)
            {
                foldout.Add(new PropertyField(fields[i]));
                foldout.TrackPropertyValue(fields[i], _ => foldout.text = Describe(property));
            }

            return foldout;
        }

        private static string Describe(SerializedProperty property)
        {
            int   startBar    = property.FindPropertyRelative(START_BAR).intValue;
            float bpm         = property.FindPropertyRelative(BPM).floatValue;
            int   beatsPerBar = property.FindPropertyRelative(BEATS_PER_BAR).intValue;
            int   beatUnit    = property.FindPropertyRelative(BEAT_UNIT).intValue;

            string description = $"From bar {startBar}: {bpm:0.##} BPM, {beatsPerBar}/{beatUnit}";

            return description;
        }
    }
}
