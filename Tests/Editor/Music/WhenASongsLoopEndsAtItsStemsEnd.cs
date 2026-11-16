using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>A song of one four-second stem, on an in-memory clip, looping to the stem's very end.</summary>
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
            SongAuthoring.SetLoopInSamples(m_Song, 0, 176400, 44100);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
            Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenItLoops()
        {
            Assert.IsTrue(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenOffTheWebNothingStopsIt()
        {
            Assert.IsNull(m_Song.FindLoopProblem(false));
        }

        [Test]
        public void ThenForTheWebItNeedsAudioAfterTheLoop()
        {
            StringAssert.Contains("its loop ends [0.000] seconds before the end of stem [1], [Stem]", m_Song.FindLoopProblem(true));
        }
    }
}
