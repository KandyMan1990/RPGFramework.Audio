using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>Each test sets the loop itself: a test cannot expect what its set-up logs.</summary>
    internal sealed class WhenASongStartingAfterBar1IsSetToLoop
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(2, 120f, 4, NoteValue.Quarter));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItPlaysThroughWithoutLooping()
        {
            SongAuthoring.SetLoop(m_Song, 1, 2);

            Assert.IsFalse(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenItWarnsThatTheFirstSectionMustStartAtBar1()
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("its first tempo section starts at bar [2] rather than bar 1")));

            SongAuthoring.SetLoop(m_Song, 1, 2);
        }
    }
}
