using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>Each test sets the loop itself: a test cannot expect what its set-up logs.</summary>
    internal sealed class WhenASongsSampleLoopEndsBeforeItStarts
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

        [Test]
        public void ThenItPlaysThroughWithoutLooping()
        {
            SongAuthoring.SetLoopInSamples(m_Song, 617400, 88200, 44100);

            Assert.IsFalse(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenItWarnsWhereTheLoopRuns()
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("its loop runs from sample [617400] to sample [88200]")));

            SongAuthoring.SetLoopInSamples(m_Song, 617400, 88200, 44100);
        }
    }
}
