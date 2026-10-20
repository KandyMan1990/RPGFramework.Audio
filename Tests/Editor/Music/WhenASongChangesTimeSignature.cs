using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>Two seconds a bar in 4/4 at 120, then a second and a half in 6/8 from bar 9, sixteen seconds in.</summary>
    internal sealed class WhenASongChangesTimeSignature
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();

            SongAuthoring.SetSections(m_Song, new Section(1, 120f, 4, NoteValue.Quarter), new Section(9, 120f, 6, NoteValue.Eighth));
            SongAuthoring.SetLoop(m_Song, 3, 11);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItLoops()
        {
            Assert.IsTrue(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenALoopStartBeforeTheChangeIsInTheFirstSection()
        {
            Assert.AreEqual(4.0, ((IMusicAsset)m_Song).LoopStartTime);
        }

        [Test]
        public void ThenALoopEndAfterTheChangeAddsTheSectionsBeforeIt()
        {
            Assert.AreEqual(19.0, ((IMusicAsset)m_Song).LoopEndTime);
        }

        [TestCase(15.9, 8)]
        [TestCase(16.0, 9)]
        [TestCase(17.4, 9)]
        [TestCase(17.6, 10)]
        public void ThenThePlayheadsBarComesFromItsOwnSection(double seconds, int bar)
        {
            m_Song.TryGetBar(seconds, out int found);

            Assert.AreEqual(bar, found);
        }
    }
}
