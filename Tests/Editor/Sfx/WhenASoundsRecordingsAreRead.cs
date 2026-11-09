using NUnit.Framework;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Sfx
{
    /// <summary>A sound on its own, and a collection of it with a pitch variation.</summary>
    internal sealed class WhenASoundsRecordingsAreRead
    {
        private SfxAsset      m_Sound;
        private SfxCollection m_Collection;

        [SetUp]
        public void SetUp()
        {
            m_Sound      = ScriptableObject.CreateInstance<SfxAsset>();
            m_Collection = ScriptableObject.CreateInstance<SfxCollection>();

            SerializedObject   asset  = new SerializedObject(m_Collection);
            SerializedProperty sounds = asset.FindProperty("m_Sounds");

            sounds.arraySize = 1;
            sounds.GetArrayElementAtIndex(0).objectReferenceValue = m_Sound;
            asset.FindProperty("m_PitchVariation").floatValue     = 0.1f;
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Collection);
            Object.DestroyImmediate(m_Sound);
        }

        [Test]
        public void ThenASoundIsItsOwnRecording()
        {
            Assert.AreSame(m_Sound, m_Sound.Recordings[0]);
            Assert.AreEqual(1, m_Sound.Recordings.Count);
        }

        [Test]
        public void ThenASoundDoesNotVaryInPitch()
        {
            Assert.AreEqual(0f, m_Sound.PitchVariation);
        }

        [Test]
        public void ThenACollectionsRecordingsAreItsSounds()
        {
            Assert.AreSame(m_Sound, m_Collection.Recordings[0]);
            Assert.AreEqual(1, m_Collection.Recordings.Count);
        }

        [Test]
        public void ThenACollectionHasItsPitchVariation()
        {
            Assert.AreEqual(0.1f, m_Collection.PitchVariation);
        }
    }
}
