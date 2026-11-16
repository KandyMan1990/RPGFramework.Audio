using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>
    /// What the song and sound inspectors share for a loop counted in samples: the stems' clips as last seen, so a stem
    /// set can be noticed and its file read; every stem's source file read for the loop; the sample rate filled from the
    /// first stem's file, and the warning when they differ; and the default inspector's fields found and replaced.
    /// </summary>
    internal static class StemLoopFields
    {
        internal static AudioClip[] CurrentClips(IReadOnlyList<IStem> tracks)
        {
            AudioClip[] clips = new AudioClip[tracks?.Count ?? 0];

            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = tracks[i].Clip;
            }

            return clips;
        }

        /// <summary>Whether a stem's clip has been set since <paramref name="before" />, as an added stem's is.</summary>
        internal static bool AStemWasSet(AudioClip[] before, AudioClip[] now)
        {
            for (int i = 0; i < now.Length; i++)
            {
                if (now[i] != null && (i >= before.Length || before[i] != now[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Every stem's source file read for the loop, and the stems' agreement.</summary>
        internal static StemLoopReading ReadStems(IReadOnlyList<IStem> tracks)
        {
            int           count    = tracks?.Count ?? 0;
            string[]      names    = new string[count];
            StemMarkers[] markers  = new StemMarkers[count];
            string[]      problems = new string[count];

            for (int i = 0; i < count; i++)
            {
                AudioClip clip = tracks[i].Clip;

                names[i] = clip != null ? clip.name : $"Stem {i + 1}";

                if (clip != null)
                {
                    StemSourceFiles.TryReadMarkers(clip, out markers[i], out problems[i]);
                }
            }

            return StemLoopReading.Read(names, markers, problems);
        }

        /// <summary>
        /// Fills <paramref name="rate" /> from the first stem's source file while it is unset and <paramref name="fills" />,
        /// and says what is wrong with it, or null: the numbers are never changed under an author.
        /// </summary>
        internal static string CheckRate(SerializedProperty rate, IReadOnlyList<IStem> tracks, bool fills)
        {
            bool read = StemSourceFiles.TryReadRate(tracks != null && tracks.Count > 0 ? tracks[0].Clip : null, out int fileRate, out string problem);

            if (fills && rate.intValue <= 0 && read)
            {
                rate.intValue = fileRate;
                rate.serializedObject.ApplyModifiedProperties();
            }

            if (!read)
            {
                return $"The sample rate is read from the first stem's source file, but {problem}";
            }

            if (rate.intValue > 0 && rate.intValue != fileRate)
            {
                return $"The samples are counted at [{rate.intValue}] Hz, but the first stem's source file is at [{fileRate}] Hz. Its numbers are kept as they are: type them again, or the rate, if the stem was exported anew";
            }

            return null;
        }

        internal static PropertyField FindField(VisualElement root, string propertyPath)
        {
            return root.Query<PropertyField>().Where(field => field.bindingPath == propertyPath).First();
        }

        /// <summary>Puts <paramref name="row" /> where the default field for the property was, and hides that.</summary>
        internal static void Replace(VisualElement root, string propertyPath, VisualElement row)
        {
            PropertyField field = FindField(root, propertyPath);

            field.parent.Insert(field.parent.IndexOf(field), row);
            field.style.display = DisplayStyle.None;
        }
    }
}
