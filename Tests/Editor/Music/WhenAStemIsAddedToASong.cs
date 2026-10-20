using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenAStemIsAddedToASong
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetTrackCount(m_Song, 3);
            SongAuthoring.AddState(m_Song, "Calm", true, false, true);
            SongAuthoring.SetTrackCount(m_Song, 5);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenEachStateHasOneFlagPerStem()
        {
            Assert.AreEqual(5, SongAuthoring.StemsIn(m_Song, "Calm").Length);
        }

        [Test]
        public void ThenAStemItsAuthorSilencedStaysSilent()
        {
            Assert.IsFalse(SongAuthoring.StemsIn(m_Song, "Calm")[1]);
        }

        [Test]
        public void ThenTheNewStemsAreAudible()
        {
            bool[] stems = SongAuthoring.StemsIn(m_Song, "Calm");

            Assert.IsTrue(stems[3]);
            Assert.IsTrue(stems[4]);
        }
    }
}
