using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>Each test sets the loop itself: a test cannot expect what its set-up logs.</summary>
    internal sealed class WhenASongsLoopEndsBeforeItStarts
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 120f, 4, NoteValue.Quarter));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItPlaysThroughWithoutLooping()
        {
            SongAuthoring.SetLoop(m_Song, 9, 5);

            Assert.IsFalse(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenItWarnsWhereTheLoopRuns()
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("its loop runs from bar [9] to bar [5]")));

            SongAuthoring.SetLoop(m_Song, 9, 5);
        }
    }
}
