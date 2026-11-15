using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>Looped by bars, then by samples, then by bars again.</summary>
    internal sealed class WhenASongGoesBackToBars
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 120f, 4, NoteValue.Quarter));
            SongAuthoring.SetLoopInBars(m_Song, 3, 5);
            SongAuthoring.SetLoopInSamples(m_Song, 88200, 617400, 44100);
            SongAuthoring.SetUnit(m_Song, LoopUnit.Bars);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItLoopsFromItsStartBarAsItWas()
        {
            Assert.AreEqual(4.0, ((IMusicAsset)m_Song).LoopStartTime);
        }

        [Test]
        public void ThenItLoopsToItsEndBarAsItWas()
        {
            Assert.AreEqual(8.0, ((IMusicAsset)m_Song).LoopEndTime);
        }
    }
}
