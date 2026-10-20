using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongLoopsBeforeItsTempoChanges
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 120f, 4, NoteValue.Quarter), new Section(9, 90f, 3, NoteValue.Quarter));
            SongAuthoring.SetLoop(m_Song, 2, 5);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenTheLoopStartIgnoresTheChange()
        {
            Assert.AreEqual(2.0, ((IMusicAsset)m_Song).LoopStartTime);
        }

        [Test]
        public void ThenTheLoopEndIgnoresTheChange()
        {
            Assert.AreEqual(8.0, ((IMusicAsset)m_Song).LoopEndTime);
        }
    }
}
