using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenAStateHasNoStemsChosen
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetTrackCount(m_Song, 3);
            SongAuthoring.AddState(m_Song, "Calm");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenEveryStemIsAudibleInIt()
        {
            CollectionAssert.AreEqual(new[] { true, true, true }, SongAuthoring.StemsIn(m_Song, "Calm"));
        }
    }
}
