using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongDeclaresNoStates
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetTrackCount(m_Song, 4);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItsFirstStateHasEveryStemAudible()
        {
            CollectionAssert.AreEqual(new[] { true, true, true, true }, ((IMusicAsset)m_Song).GetStemsForState(0));
        }

        [Test]
        public void ThenThatStateIsCalledDefault()
        {
            Assert.AreEqual(4, SongAuthoring.StemsIn(m_Song, "Default").Length);
        }
    }
}
