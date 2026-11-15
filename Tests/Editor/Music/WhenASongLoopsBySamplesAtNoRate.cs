using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>Each test sets the loop itself: a test cannot expect what its set-up logs.</summary>
    internal sealed class WhenASongLoopsBySamplesAtNoRate
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
            SongAuthoring.SetLoopInSamples(m_Song, 0, 88200, 0);

            Assert.IsFalse(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenItWarnsThatItHasNoRate()
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("has no sample rate to count them at")));

            SongAuthoring.SetLoopInSamples(m_Song, 0, 88200, 0);
        }
    }
}
