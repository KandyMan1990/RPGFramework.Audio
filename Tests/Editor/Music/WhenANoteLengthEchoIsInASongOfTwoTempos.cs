using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>A quarter note's echo, at 120 for eight bars of 4/4, sixteen seconds, then at 90.</summary>
    internal sealed class WhenANoteLengthEchoIsInASongOfTwoTempos
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 120f, 4, NoteValue.Quarter), new Section(9, 90f, 4, NoteValue.Quarter));
            SongAuthoring.SetEchoNote(m_Song, NoteValue.Quarter, NoteFeel.Straight);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItFollowsTheTempo()
        {
            Assert.IsTrue(((IMusicAsset)m_Song).EchoFollowsTempo);
        }

        [Test]
        public void ThenBeforeTheChangeItsDelayIsTheFirstSectionsNote()
        {
            Assert.AreEqual(500f, ((IMusicAsset)m_Song).GetEchoDelayMilliseconds(10.0), 0.01f);
        }

        [Test]
        public void ThenAfterTheChangeItsDelayIsTheSecondSectionsNote()
        {
            Assert.AreEqual(666.67f, ((IMusicAsset)m_Song).GetEchoDelayMilliseconds(17.0), 0.01f);
        }
    }
}
