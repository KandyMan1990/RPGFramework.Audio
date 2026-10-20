using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>Each test sets the loop itself: a test cannot expect what its set-up logs.</summary>
    internal sealed class WhenASongWithNoTempoIsSetToLoop
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 0f, 4, NoteValue.Quarter));
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
        public void ThenItWarnsThatTheSectionNeedsATempo()
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("its tempo section at bar [1] needs a BPM and beats per bar greater than zero")));

            SongAuthoring.SetLoop(m_Song, 1, 2);
        }
    }
}
