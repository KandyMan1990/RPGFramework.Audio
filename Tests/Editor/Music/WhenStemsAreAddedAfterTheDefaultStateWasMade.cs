using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenStemsAreAddedAfterTheDefaultStateWasMade
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetTrackCount(m_Song, 1);
            SongAuthoring.SetTrackCount(m_Song, 3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenTheyAreAudibleInIt()
        {
            CollectionAssert.AreEqual(new[] { true, true, true }, ((IMusicAsset)m_Song).GetStemsForState(0));
        }
    }
}
