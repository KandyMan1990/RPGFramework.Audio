using NUnit.Framework;
using RPGFramework.Audio.Editor;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Authoring
{
    /// <summary>
    /// A sound's first stem added as the inspector adds it, zeroed, then its reverb turned down to a quarter and a second
    /// stem added, which the inspector copies from the first.
    /// </summary>
    internal sealed class WhenAStemIsAddedInTheInspector
    {
        private SfxAsset m_Sound;
        private float    m_First;
        private float    m_Second;

        [SetUp]
        public void SetUp()
        {
            m_Sound = ScriptableObject.CreateInstance<SfxAsset>();

            SerializedObject   asset  = new SerializedObject(m_Sound);
            SerializedProperty tracks = asset.FindProperty("m_Tracks");

            tracks.arraySize = 1;
            asset.ApplyModifiedPropertiesWithoutUndo();
            StemTracks.DefaultNewStems(asset, 0);
            m_First = ((IAudioAsset)m_Sound).Tracks[0].ReverbSendLevel;

            asset.Update();
            tracks.GetArrayElementAtIndex(0).FindPropertyRelative("m_ReverbSendLevel").floatValue = 0.25f;
            tracks.arraySize = 2;
            asset.ApplyModifiedPropertiesWithoutUndo();
            StemTracks.DefaultNewStems(asset, 1);
            m_Second = ((IAudioAsset)m_Sound).Tracks[1].ReverbSendLevel;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Sound);
        }

        [Test]
        public void ThenTheFirstSendsInFullToTheReverb()
        {
            Assert.AreEqual(1f, m_First);
        }

        [Test]
        public void ThenOneCopiedKeepsWhatItCopied()
        {
            Assert.AreEqual(0.25f, m_Second);
        }
    }
}
