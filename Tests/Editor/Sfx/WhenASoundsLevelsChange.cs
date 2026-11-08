using NUnit.Framework;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Sfx
{
    /// <summary>A sound playing on two stems at volume 0.5 and pan 0.25.</summary>
    internal sealed class WhenASoundsLevelsChange
    {
        private const float TOLERANCE = 0.001f;

        private GameObject    m_Player;
        private AudioClip     m_Clip;
        private SfxAsset      m_Sound;
        private AudioSource[] m_Stems;
        private ISfxReference m_Reference;

        [SetUp]
        public void SetUp()
        {
            m_Player = new GameObject("Player");
            m_Clip   = AudioClip.Create("Clip", 4410, 1, 44100, false);
            m_Sound  = ScriptableObject.CreateInstance<SfxAsset>();

            SerializedObject   asset  = new SerializedObject(m_Sound);
            SerializedProperty tracks = asset.FindProperty("m_Tracks");

            tracks.arraySize = 1;
            tracks.GetArrayElementAtIndex(0).FindPropertyRelative("m_AudioClip").objectReferenceValue = m_Clip;
            asset.ApplyModifiedPropertiesWithoutUndo();

            m_Stems = new[] { m_Player.AddComponent<AudioSource>(), m_Player.AddComponent<AudioSource>() };

            m_Reference = new SfxReference(1UL, m_Stems, m_Sound, AudioSettings.dspTime, 0f, 0.5f, 0.25f, _ => { });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Sound);
            Object.DestroyImmediate(m_Clip);
            Object.DestroyImmediate(m_Player);
        }

        [Test]
        public void ThenAVolumeGoesOnEveryStem()
        {
            m_Reference.SetVolume(0.75f, 0f);
            m_Reference.UpdateLevels(0.1f, 1f, false);

            Assert.AreEqual(0.75f, m_Stems[0].volume, TOLERANCE);
            Assert.AreEqual(0.75f, m_Stems[1].volume, TOLERANCE);
        }

        [Test]
        public void ThenAPanGoesOnEveryStem()
        {
            m_Reference.SetPan(-1f, 0f);
            m_Reference.UpdateLevels(0.1f, 1f, false);

            Assert.AreEqual(-1f, m_Stems[0].panStereo, TOLERANCE);
            Assert.AreEqual(-1f, m_Stems[1].panStereo, TOLERANCE);
        }

        [Test]
        public void ThenAFadeGoesPartWayEachUpdate()
        {
            m_Reference.SetVolume(1f, 1f);
            m_Reference.UpdateLevels(0.5f, 1f, false);

            Assert.AreEqual(0.75f, m_Stems[0].volume, TOLERANCE);
        }

        [Test]
        public void ThenEverySoundsVolumeScalesItsOwn()
        {
            m_Reference.UpdateLevels(0.1f, 0.5f, true);

            Assert.AreEqual(0.25f, m_Stems[0].volume, TOLERANCE);
        }

        [Test]
        public void ThenASoundThatHasEndedLeavesItsVoicesAlone()
        {
            m_Stems[0].volume = 0.1f;

            m_Reference.Stop();
            m_Reference.SetVolume(1f, 0f);
            m_Reference.UpdateLevels(0.1f, 1f, true);

            Assert.AreEqual(0.1f, m_Stems[0].volume, TOLERANCE);
        }

        [Test]
        public void ThenAOneShotDoesNotLoop()
        {
            Assert.IsFalse(m_Reference.Loops);
        }
    }
}
