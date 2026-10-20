using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongsEchoIsANoteLength
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [TestCase(120f, NoteValue.Quarter, NoteFeel.Straight, 500f)]
        [TestCase(104f, NoteValue.Eighth,  NoteFeel.Dotted,   432.69f)]
        [TestCase(120f, NoteValue.Quarter, NoteFeel.Triplet,  333.33f)]
        public void ThenItsDelayIsTheNoteAtTheSongsTempo(float bpm, NoteValue note, NoteFeel feel, float milliseconds)
        {
            SongAuthoring.SetSections(m_Song, new Section(1, bpm, 4, NoteValue.Quarter));
            SongAuthoring.SetEchoNote(m_Song, note, feel);

            Assert.AreEqual(milliseconds, ((IMusicAsset)m_Song).GetEchoDelayMilliseconds(0.0), 0.01f);
        }

        [Test]
        public void ThenADelayPastTheEchosLongestIsHeldThere()
        {
            SongAuthoring.SetSections(m_Song, new Section(1, 40f, 4, NoteValue.Quarter));
            SongAuthoring.SetEchoNote(m_Song, NoteValue.Whole, NoteFeel.Straight);

            Assert.AreEqual(EchoSettings.MAX_DELAY_MS, ((IMusicAsset)m_Song).GetEchoDelayMilliseconds(0.0));
        }
    }
}
