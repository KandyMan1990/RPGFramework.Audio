using NUnit.Framework;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Sfx
{
    /// <summary>A looping sound of one three-second stem, on an in-memory clip, looping to its very end.</summary>
    internal sealed class WhenASoundsLoopEndsAtItsStemsEnd
    {
        private SfxAsset  m_Sound;
        private AudioClip m_Clip;

        [SetUp]
        public void SetUp()
        {
            m_Sound = ScriptableObject.CreateInstance<SfxAsset>();
            m_Clip  = AudioClip.Create("Stem", 132300, 1, 44100, false);

            m_Sound.name = "Sound";

            SerializedObject   asset  = new SerializedObject(m_Sound);
            SerializedProperty tracks = asset.FindProperty("m_Tracks");

            tracks.arraySize = 1;
            tracks.GetArrayElementAtIndex(0).FindPropertyRelative("m_AudioClip").objectReferenceValue = m_Clip;
            asset.FindProperty("m_Loop").boolValue      = true;
            asset.FindProperty("m_LoopEnd").intValue    = 132300;
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
        public void ThenItLoops()
        {
            Assert.IsTrue(((ISfxAsset)m_Sound).Loop);
        }

        [Test]
        public void ThenOffTheWebNothingStopsIt()
        {
            Assert.IsNull(m_Sound.FindLoopProblem(false));
        }

        [Test]
        public void ThenForTheWebItNeedsAudioAfterTheLoop()
        {
            StringAssert.Contains("its loop ends [0.000] seconds before the end of stem [1]", m_Sound.FindLoopProblem(true));
        }
    }
}
