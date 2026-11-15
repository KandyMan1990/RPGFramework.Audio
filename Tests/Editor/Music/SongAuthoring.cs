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

        internal static void SetLoopInBars(MusicAsset song, int startBar, int endBar)
        {
            SerializedObject asset = new SerializedObject(song);

            asset.FindProperty("m_Loop").boolValue        = true;
            asset.FindProperty("m_LoopUnit").intValue     = (int)LoopUnit.Bars;
            asset.FindProperty("m_LoopStartBar").intValue = startBar;
            asset.FindProperty("m_LoopEndBar").intValue   = endBar;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetLoopInSamples(MusicAsset song, int startSample, int endSample, int sampleRate)
        {
            SetUnit(song, LoopUnit.Samples);
            SetSamplePoints(song, startSample, endSample, sampleRate);
        }

        /// <summary>The loop ticked and its samples set, its unit left as it is.</summary>
        internal static void SetSamplePoints(MusicAsset song, int startSample, int endSample, int sampleRate)
        {
            SerializedObject asset = new SerializedObject(song);

            asset.FindProperty("m_Loop").boolValue           = true;
            asset.FindProperty("m_LoopStartSample").intValue = startSample;
            asset.FindProperty("m_LoopEndSample").intValue   = endSample;
            asset.FindProperty("m_LoopSampleRate").intValue  = sampleRate;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetUnit(MusicAsset song, LoopUnit unit)
        {
            SerializedObject asset = new SerializedObject(song);

            asset.FindProperty("m_LoopUnit").intValue = (int)unit;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>One stem, playing <paramref name="clip" />.</summary>
        internal static void SetStem(MusicAsset song, AudioClip clip)
        {
            SerializedObject   asset  = new SerializedObject(song);
            SerializedProperty tracks = asset.FindProperty("m_Tracks");

            tracks.arraySize = 1;
            tracks.GetArrayElementAtIndex(0).FindPropertyRelative("m_AudioClip").objectReferenceValue = clip;
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
