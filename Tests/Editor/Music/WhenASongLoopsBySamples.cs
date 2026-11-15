using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongLoopsBySamples
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetLoopInSamples(m_Song, 88200, 617400, 44100);
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
        public void ThenTheLoopStartsAtItsStartSampleOverTheRate()
        {
            Assert.AreEqual(2.0, ((IMusicAsset)m_Song).LoopStartTime);
        }

        [Test]
        public void ThenTheLoopEndsAtItsEndSampleOverTheRate()
        {
            Assert.AreEqual(14.0, ((IMusicAsset)m_Song).LoopEndTime);
        }
    }
}
