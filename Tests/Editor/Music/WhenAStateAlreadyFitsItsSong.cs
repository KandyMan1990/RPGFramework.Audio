using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenAStateAlreadyFitsItsSong
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetTrackCount(m_Song, 2);
            SongAuthoring.AddState(m_Song, "Calm", true, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItIsAsItsAuthorLeftIt()
        {
            CollectionAssert.AreEqual(new[] { true, false }, SongAuthoring.StemsIn(m_Song, "Calm"));
        }
    }
}
