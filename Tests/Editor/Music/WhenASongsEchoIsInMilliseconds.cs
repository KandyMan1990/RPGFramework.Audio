using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>At 120 for sixteen seconds, then at 30.</summary>
    internal sealed class WhenASongsEchoIsInMilliseconds
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 120f, 4, NoteValue.Quarter), new Section(9, 30f, 4, NoteValue.Quarter));
            SongAuthoring.SetEchoMilliseconds(m_Song, 250f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [TestCase(0.0)]
        [TestCase(20.0)]
        public void ThenItsDelayIgnoresTheTempo(double seconds)
        {
            Assert.AreEqual(250f, ((IMusicAsset)m_Song).GetEchoDelayMilliseconds(seconds));
        }

        [Test]
        public void ThenItNeverNeedsFollowing()
        {
            Assert.IsFalse(((IMusicAsset)m_Song).EchoFollowsTempo);
        }
    }
}
