using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongWithNoTracksIsEdited
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 90f, 4, NoteValue.Quarter));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItsDefaultStateIsEmpty()
        {
            CollectionAssert.IsEmpty(((IMusicAsset)m_Song).GetStemsForState(0));
        }
    }
}
