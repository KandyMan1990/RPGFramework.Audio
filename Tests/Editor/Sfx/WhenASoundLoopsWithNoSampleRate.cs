using NUnit.Framework;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Sfx
{
    /// <summary>A sound marked to loop from sample 0 to 44,100, on an in-memory clip, with no sample rate set.</summary>
    internal sealed class WhenASoundLoopsWithNoSampleRate
    {
        private SfxAsset  m_Sound;
        private AudioClip m_Clip;

        [SetUp]
        public void SetUp()
        {
            m_Sound = ScriptableObject.CreateInstance<SfxAsset>();
            m_Clip  = AudioClip.Create("Stem", 132300, 1, 44100, false);

            SerializedObject   asset  = new SerializedObject(m_Sound);
            SerializedProperty tracks = asset.FindProperty("m_Tracks");

            tracks.arraySize = 1;
            tracks.GetArrayElementAtIndex(0).FindPropertyRelative("m_AudioClip").objectReferenceValue = m_Clip;
            asset.FindProperty("m_Loop").boolValue   = true;
            asset.FindProperty("m_LoopEnd").intValue = 44100;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Sound);
            Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenItPlaysThroughWithoutLooping()
        {
            Assert.IsFalse(((ISfxAsset)m_Sound).Loop);
        }

        [Test]
        public void ThenItsProblemSaysWhy()
        {
            StringAssert.Contains("no sample rate to count its loop at", m_Sound.FindLoopProblem(false));
        }
    }
}
