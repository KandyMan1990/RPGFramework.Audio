using NUnit.Framework;
using RPGFramework.Audio.Editor;
using RPGFramework.Audio.Sfx;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Authoring
{
    /// <summary>Three in-memory clips, picked out in the order Stem_10, Stem_02, Stem_01, added to a new sound's stems.</summary>
    internal sealed class WhenClipsAreAddedAsStems
    {
        private SfxAsset    m_Sound;
        private AudioClip[] m_Clips;
        private AudioClip[] m_InOrder;

        [SetUp]
        public void SetUp()
        {
            m_Sound = ScriptableObject.CreateInstance<SfxAsset>();
            m_Clips = new[]
                      {
                          AudioClip.Create("Stem_10", 4410, 1, 44100, false),
                          AudioClip.Create("Stem_02", 4410, 1, 44100, false),
                          AudioClip.Create("Stem_01", 4410, 1, 44100, false)
                      };

            m_InOrder = StemTracks.ClipsAmong(new Object[] { m_Clips[0], m_Sound, m_Clips[1], m_Clips[2] });

            StemTracks.AddClips(new SerializedObject(m_Sound), m_InOrder);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Sound);

            for (int i = 0; i < m_Clips.Length; i++)
            {
                Object.DestroyImmediate(m_Clips[i]);
            }
        }

        [Test]
        public void ThenOnlyTheClipsAreTakenInTheirNamesOrder()
        {
            Assert.AreEqual(new[] { "Stem_01", "Stem_02", "Stem_10" }, new[] { m_InOrder[0].name, m_InOrder[1].name, m_InOrder[2].name });
        }

        [Test]
        public void ThenEachIsAStemInThatOrder()
        {
            IStem[] stems = { ((IAudioAsset)m_Sound).Tracks[0], ((IAudioAsset)m_Sound).Tracks[1], ((IAudioAsset)m_Sound).Tracks[2] };

            Assert.AreEqual(new[] { m_Clips[2], m_Clips[1], m_Clips[0] }, new[] { stems[0].Clip, stems[1].Clip, stems[2].Clip });
        }

        [Test]
        public void ThenEachSendsInFullToTheReverb()
        {
            Assert.AreEqual(1f, ((IAudioAsset)m_Sound).Tracks[2].ReverbSendLevel);
        }
    }
}
