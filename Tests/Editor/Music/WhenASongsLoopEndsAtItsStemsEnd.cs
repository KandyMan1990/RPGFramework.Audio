using System.Text.RegularExpressions;
using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>
    /// A song of one four-second stem, on an in-memory clip. Each test sets the loop itself: a test cannot expect what
    /// its set-up logs.
    /// </summary>
    internal sealed class WhenASongsLoopEndsAtItsStemsEnd
    {
        private MusicAsset m_Song;
        private AudioClip  m_Clip;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();
            m_Clip = AudioClip.Create("Stem", 176400, 1, 44100, false);

            SongAuthoring.SetStem(m_Song, m_Clip);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
            Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenItWarnsThatThePlayerNeedsAudioAfterTheLoop()
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("its loop ends [0.000] seconds before the end of stem [1], [Stem]")));

            SongAuthoring.SetLoopInSamples(m_Song, 0, 176400, 44100);
        }

        [Test]
        public void ThenItStillLoops()
        {
            SongAuthoring.SetLoopInSamples(m_Song, 0, 176400, 44100);

            Assert.IsTrue(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenATenthOfASecondAfterTheLoopIsEnough()
        {
            SongAuthoring.SetLoopInSamples(m_Song, 0, 176400 - 4410, 44100);

            LogAssert.NoUnexpectedReceived();
        }
    }
}
