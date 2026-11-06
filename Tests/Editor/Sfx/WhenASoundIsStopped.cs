using System;
using NUnit.Framework;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Sfx
{
    internal sealed class WhenASoundIsStopped
    {
        private AudioClip     m_Clip;
        private SfxAsset      m_Sound;
        private ISfxReference m_Reference;

        [SetUp]
        public void SetUp()
        {
            m_Clip  = AudioClip.Create("Clip", 4410, 1, 44100, false);
            m_Sound = ScriptableObject.CreateInstance<SfxAsset>();

            SerializedObject   asset  = new SerializedObject(m_Sound);
            SerializedProperty tracks = asset.FindProperty("m_Tracks");

            tracks.arraySize = 1;
            tracks.GetArrayElementAtIndex(0).FindPropertyRelative("m_AudioClip").objectReferenceValue = m_Clip;
            asset.ApplyModifiedPropertiesWithoutUndo();

            m_Reference = new SfxReference(1UL, Array.Empty<AudioSource>(), m_Sound, AudioSettings.dspTime, _ => { });
            m_Reference.Stop();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_Sound);
            UnityEngine.Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenItsCompletionCompletes()
        {
            Assert.IsTrue(m_Reference.Completion.IsCompleted);
        }
    }
}
