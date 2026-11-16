using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>A song of one four-second stem, on an in-memory clip, whose loop ends a tenth of a second before it does.</summary>
    internal sealed class WhenASongLeavesATenthOfASecondAfterItsLoop
    {
        private MusicAsset m_Song;
        private AudioClip  m_Clip;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();
            m_Clip = AudioClip.Create("Stem", 176400, 1, 44100, false);

            SongAuthoring.SetStem(m_Song, m_Clip);
            SongAuthoring.SetLoopInSamples(m_Song, 0, 176400 - 4410, 44100);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
            Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenEvenTheWebCanLoopIt()
        {
            Assert.IsNull(m_Song.FindLoopProblem(true));
        }
    }
}
