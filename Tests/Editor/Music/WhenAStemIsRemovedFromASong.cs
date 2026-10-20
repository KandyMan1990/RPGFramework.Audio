using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenAStemIsRemovedFromASong
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetTrackCount(m_Song, 4);
            SongAuthoring.AddState(m_Song, "Calm", true, false, true, true);
            SongAuthoring.SetTrackCount(m_Song, 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenAStateDropsTheStemsThatNoLongerExist()
        {
            Assert.AreEqual(2, SongAuthoring.StemsIn(m_Song, "Calm").Length);
        }

        [Test]
        public void ThenAStateKeepsWhatItsAuthorChoseForTheRest()
        {
            CollectionAssert.AreEqual(new[] { true, false }, SongAuthoring.StemsIn(m_Song, "Calm"));
        }
    }
}
