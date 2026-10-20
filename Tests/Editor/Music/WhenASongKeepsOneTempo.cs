using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongKeepsOneTempo
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 120f, 4, NoteValue.Quarter));
            SongAuthoring.SetLoop(m_Song, 5, 9);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItLoops()
        {
            Assert.IsTrue(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenTheLoopStartsAtItsBarsTime()
        {
            Assert.AreEqual(8.0, ((IMusicAsset)m_Song).LoopStartTime);
        }

        [Test]
        public void ThenTheLoopEndsAtItsBarsTime()
        {
            Assert.AreEqual(16.0, ((IMusicAsset)m_Song).LoopEndTime);
        }
    }
}
