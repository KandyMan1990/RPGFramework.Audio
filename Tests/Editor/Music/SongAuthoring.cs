using RPGFramework.Audio.Music;
using RPGFramework.Hashing;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>
    /// Songs edited as an author edits them, through the asset's serialized properties. Each edit is applied as the
    /// inspector applies it, so the song works out its loop and fits its stem states to its tracks afresh.
    /// </summary>
    internal static class SongAuthoring
    {
        /// <summary>A new song, as the create menu makes it: one section, at 120 BPM in 4/4, and no loop.</summary>
        internal static MusicAsset New()
        {
            MusicAsset song = ScriptableObject.CreateInstance<MusicAsset>();

            song.name = "Song";

            return song;
        }

        internal static void SetSections(MusicAsset song, params Section[] sections)
        {
            SerializedObject   asset    = new SerializedObject(song);
            SerializedProperty property = asset.FindProperty("m_Sections");

            property.arraySize = sections.Length;

            for (int i = 0; i < sections.Length; i++)
            {
                SerializedProperty section = property.GetArrayElementAtIndex(i);

                section.FindPropertyRelative("m_StartBar").intValue    = sections[i].StartBar;
                section.FindPropertyRelative("m_BPM").floatValue       = sections[i].BPM;
                section.FindPropertyRelative("m_BeatsPerBar").intValue = sections[i].BeatsPerBar;
                section.FindPropertyRelative("m_BeatUnit").intValue    = (int)sections[i].BeatUnit;
            }

            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetLoop(MusicAsset song, int startBar, int endBar)
        {
            SerializedObject asset = new SerializedObject(song);

            asset.FindProperty("m_Loop").boolValue        = true;
            asset.FindProperty("m_LoopStartBar").intValue = startBar;
            asset.FindProperty("m_LoopEndBar").intValue   = endBar;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Stems added or taken from the end of the song's list.</summary>
        internal static void SetTrackCount(MusicAsset song, int count)
        {
            SerializedObject asset = new SerializedObject(song);

            asset.FindProperty("m_Tracks").arraySize = count;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A stem state after the song's others, sounding the stems ticked here.</summary>
        internal static void AddState(MusicAsset song, string name, params bool[] stems)
        {
            SerializedObject   asset  = new SerializedObject(song);
            SerializedProperty states = asset.FindProperty("m_States");

            states.arraySize++;

            SerializedProperty state  = states.GetArrayElementAtIndex(states.arraySize - 1);
            SerializedProperty active = state.FindPropertyRelative("m_ActiveStems");

            state.FindPropertyRelative("m_Name").stringValue = name;
            active.arraySize                                 = stems.Length;

            for (int i = 0; i < stems.Length; i++)
            {
                active.GetArrayElementAtIndex(i).boolValue = stems[i];
            }

            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetEchoNote(MusicAsset song, NoteValue note, NoteFeel feel)
        {
            SerializedObject asset = new SerializedObject(song);

            asset.FindProperty("m_Echo.m_Timing").intValue = (int)EchoTiming.NoteLength;
            asset.FindProperty("m_Echo.m_Note").intValue   = (int)note;
            asset.FindProperty("m_Echo.m_Feel").intValue   = (int)feel;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetEchoMilliseconds(MusicAsset song, float milliseconds)
        {
            SerializedObject asset = new SerializedObject(song);

            asset.FindProperty("m_Echo.m_Timing").intValue         = (int)EchoTiming.Milliseconds;
            asset.FindProperty("m_Echo.m_Milliseconds").floatValue = milliseconds;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Which stems sound in the state with this name, as the player asks for them.</summary>
        internal static bool[] StemsIn(MusicAsset song, string state)
        {
            return ((IMusicAsset)song).GetStemsForState(Fnv1a64.Hash(state));
        }
    }
}
