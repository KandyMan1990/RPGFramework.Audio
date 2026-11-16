using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>
    /// A song whose loop ends on the last of 5,837,188 samples at 44,100 Hz, an in-memory clip whose length in seconds,
    /// a float, falls short of that sample's time.
    /// </summary>
    internal sealed class WhenASongsLoopEndsOnALongStemsLastSample
    {
        private MusicAsset m_Song;
        private AudioClip  m_Clip;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();
            m_Clip = AudioClip.Create("Stem", 5837188, 1, 44100, false);

            SongAuthoring.SetStem(m_Song, m_Clip);
            SongAuthoring.SetLoopInSamples(m_Song, 1140133, 5837188, 44100);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
            Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenItIsNotTakenToEndPastTheStem()
        {
            Assert.IsNull(m_Song.FindLoopProblem(false));
        }
    }
}
