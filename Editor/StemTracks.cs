using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor
{
    /// <summary>
    /// A song's or a sound's stems as authored: clips added as stems, in name order, a new empty stem given the reverb a
    /// stem has by default, as the inspector zeroes the first element it adds and copies the last for each after, and a
    /// name for an asset made from clips.
    /// </summary>
    internal static class StemTracks
    {
        private const string TRACKS = "m_Tracks";
        private const string CLIP   = "m_AudioClip";
        private const string REVERB = "m_ReverbSendLevel";
        private const string ECHO   = "m_EchoSendLevel";

        // As a stem's field starts.
        private const float DEFAULT_REVERB = 1f;

        /// <summary>The clips among <paramref name="objects" />, by name, so stems numbered in their names come in order.</summary>
        internal static AudioClip[] ClipsAmong(IReadOnlyList<Object> objects)
        {
            int count = 0;

            for (int i = 0; i < objects.Count; i++)
            {
                count += objects[i] is AudioClip ? 1 : 0;
            }

            AudioClip[] clips = new AudioClip[count];

            for (int i = 0, at = 0; i < objects.Count; i++)
            {
                if (objects[i] is AudioClip clip)
                {
                    clips[at++] = clip;
                }
            }

            System.Array.Sort(clips, (a, b) => EditorUtility.NaturalCompare(a.name, b.name));

            return clips;
        }

        /// <summary>
        /// Lets clips dragged onto an inspector, anywhere but onto one of its object fields, which sets that field itself,
        /// be added as stems.
        /// </summary>
        internal static void AcceptDrops(VisualElement inspector, SerializedObject asset)
        {
            inspector.RegisterCallback<DragUpdatedEvent>(dragged =>
                                                         {
                                                             if (DraggedClips(dragged.target).Length > 0)
                                                             {
                                                                 DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                                             }
                                                         });
            inspector.RegisterCallback<DragPerformEvent>(dropped =>
                                                         {
                                                             AudioClip[] clips = DraggedClips(dropped.target);

                                                             if (clips.Length == 0)
                                                             {
                                                                 return;
                                                             }

                                                             DragAndDrop.AcceptDrag();
                                                             AddClips(asset, clips);
                                                         });
        }

        private static AudioClip[] DraggedClips(IEventHandler target)
        {
            for (VisualElement element = target as VisualElement; element != null; element = element.parent)
            {
                if (element is ObjectField)
                {
                    return System.Array.Empty<AudioClip>();
                }
            }

            return ClipsAmong(DragAndDrop.objectReferences);
        }

        /// <summary>Adds each clip as a stem after those there, sending to the reverb as a new stem does.</summary>
        internal static void AddClips(SerializedObject asset, IReadOnlyList<AudioClip> clips)
        {
            asset.Update();

            SerializedProperty tracks = asset.FindProperty(TRACKS);

            for (int i = 0; i < clips.Count; i++)
            {
                int at = tracks.arraySize;

                tracks.arraySize = at + 1;

                SerializedProperty stem = tracks.GetArrayElementAtIndex(at);
                stem.FindPropertyRelative(CLIP).objectReferenceValue = clips[i];
                stem.FindPropertyRelative(REVERB).floatValue         = DEFAULT_REVERB;
                stem.FindPropertyRelative(ECHO).floatValue           = 0f;
            }

            asset.ApplyModifiedProperties();
        }

        /// <summary>
        /// Gives each stem added since there were <paramref name="before" /> that is still empty — no clip, no reverb, as
        /// the inspector makes a first one — the reverb a new stem has. One copied from the last keeps what it copied.
        /// </summary>
        internal static void DefaultNewStems(SerializedObject asset, int before)
        {
            asset.Update();

            SerializedProperty tracks  = asset.FindProperty(TRACKS);
            bool               changed = false;

            for (int i = before; i < tracks.arraySize; i++)
            {
                SerializedProperty stem   = tracks.GetArrayElementAtIndex(i);
                SerializedProperty reverb = stem.FindPropertyRelative(REVERB);

                if (stem.FindPropertyRelative(CLIP).objectReferenceValue == null && reverb.floatValue == 0f)
                {
                    reverb.floatValue = DEFAULT_REVERB;
                    changed           = true;
                }
            }

            if (changed)
            {
                asset.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// One clip's name, or what several clips' names share, numbers and separators trimmed from its end — Battle_Boss
        /// for Battle_Boss01 to 03 — or <paramref name="defaultName" /> when they share nothing, or there are none.
        /// </summary>
        internal static string NameFor(AudioClip[] clips, string defaultName)
        {
            if (clips.Length == 0)
            {
                return defaultName;
            }

            if (clips.Length == 1)
            {
                return clips[0].name;
            }

            string shared = clips[0].name;

            for (int i = 1; i < clips.Length; i++)
            {
                int length = 0;

                while (length < shared.Length && length < clips[i].name.Length && shared[length] == clips[i].name[length])
                {
                    length++;
                }

                shared = shared.Substring(0, length);
            }

            shared = shared.TrimEnd(' ', '_', '-', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

            return shared.Length > 0 ? shared : defaultName;
        }
    }
}
