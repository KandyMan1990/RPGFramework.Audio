using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongLoopingBySamplesChangesTempo
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetLoopInSamples(m_Song, 88200, 617400, 44100);
            SongAuthoring.SetSections(m_Song, new Section(1, 120f, 4, NoteValue.Quarter), new Section(3, 90f, 3, NoteValue.Quarter));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItsLoopStartDoesNotMove()
        {
            Assert.AreEqual(2.0, ((IMusicAsset)m_Song).LoopStartTime);
        }

        [Test]
        public void ThenItsLoopEndDoesNotMove()
        {
            Assert.AreEqual(14.0, ((IMusicAsset)m_Song).LoopEndTime);
        }
    }
}
