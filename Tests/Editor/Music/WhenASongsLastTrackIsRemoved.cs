using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongsLastTrackIsRemoved
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetTrackCount(m_Song, 1);
            SongAuthoring.AddState(m_Song, "Calm", false);
            SongAuthoring.SetTrackCount(m_Song, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItsStatesAreEmpty()
        {
            CollectionAssert.IsEmpty(((IMusicAsset)m_Song).GetStemsForState(0));
            CollectionAssert.IsEmpty(SongAuthoring.StemsIn(m_Song, "Calm"));
        }
    }
}
