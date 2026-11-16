using NUnit.Framework;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Sfx
{
    /// <summary>
    /// A sound whose source file is at 44,100 Hz, imported resampled to 22,050, looping from its second second to its end:
    /// sample 88,200 at the file's rate, on a two-second in-memory clip of 44,100 samples.
    /// </summary>
    internal sealed class WhenASoundsSamplesAreCountedAtItsFilesRate
    {
        private SfxAsset  m_Sound;
        private AudioClip m_Clip;

        [SetUp]
        public void SetUp()
        {
            m_Sound = ScriptableObject.CreateInstance<SfxAsset>();
            m_Clip  = AudioClip.Create("Stem", 44100, 1, 22050, false);

            SerializedObject   asset  = new SerializedObject(m_Sound);
            SerializedProperty tracks = asset.FindProperty("m_Tracks");

            tracks.arraySize = 1;
            tracks.GetArrayElementAtIndex(0).FindPropertyRelative("m_AudioClip").objectReferenceValue = m_Clip;
            asset.FindProperty("m_Loop").boolValue      = true;
            asset.FindProperty("m_LoopStart").intValue  = 44100;
            asset.FindProperty("m_LoopEnd").intValue    = 88200;
            asset.FindProperty("m_SampleRate").intValue = 44100;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Sound);
            Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenItsLoopEndsAtTheClipsEndRatherThanPastIt()
        {
            Assert.IsNull(m_Sound.FindLoopProblem(false));
        }
    }
}
