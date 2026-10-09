using RPGFramework.Audio.Music;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    /// <summary>
    /// A song's echo, showing only what its timing uses: milliseconds, or a note length and how it is counted.
    /// </summary>
    [CustomPropertyDrawer(typeof(EchoSettings))]
    internal sealed class EchoSettingsDrawer : PropertyDrawer
    {
        private const string TIMING       = "m_Timing";
        private const string MILLISECONDS = "m_Milliseconds";
        private const string NOTE         = "m_Note";
        private const string FEEL         = "m_Feel";
        private const string DECAY        = "m_Decay";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty timing = property.FindPropertyRelative(TIMING);

            PropertyField milliseconds = new PropertyField(property.FindPropertyRelative(MILLISECONDS));
            PropertyField note         = new PropertyField(property.FindPropertyRelative(NOTE));
            PropertyField feel         = new PropertyField(property.FindPropertyRelative(FEEL));
            PropertyField decay        = new PropertyField(property.FindPropertyRelative(DECAY));

            Foldout foldout = new Foldout { text = property.displayName };
            foldout.Add(new PropertyField(timing));
            foldout.Add(milliseconds);
            foldout.Add(note);
            foldout.Add(feel);
            foldout.Add(decay);

            Show(timing, milliseconds, note, feel, decay);
            foldout.TrackPropertyValue(timing, changed => Show(changed, milliseconds, note, feel, decay));

            return foldout;
        }

        private static void Show(SerializedProperty timing, VisualElement milliseconds, VisualElement note, VisualElement feel, VisualElement decay)
        {
            EchoTiming chosen = (EchoTiming)timing.enumValueIndex;

            milliseconds.style.display = chosen == EchoTiming.Milliseconds ? DisplayStyle.Flex : DisplayStyle.None;
            note.style.display         = chosen == EchoTiming.NoteLength ? DisplayStyle.Flex : DisplayStyle.None;
            feel.style.display         = chosen == EchoTiming.NoteLength ? DisplayStyle.Flex : DisplayStyle.None;
            decay.style.display        = chosen == EchoTiming.None ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
