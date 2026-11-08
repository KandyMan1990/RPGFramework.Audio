using NUnit.Framework;
using RPGFramework.Audio.Sfx;

namespace RPGFramework.Audio.Tests.Sfx
{
    /// <summary>A level at 1 taken to 0.</summary>
    internal sealed class WhenALevelMoves
    {
        private const float TOLERANCE = 0.001f;

        private SfxLevel m_Level;

        [SetUp]
        public void SetUp()
        {
            m_Level = new SfxLevel(1f);
        }

        [Test]
        public void ThenBeforeAnythingHasNothingNew()
        {
            Assert.IsFalse(m_Level.Tick(1f));
        }

        [Test]
        public void ThenNoTimeGoesThereAtOnce()
        {
            m_Level.MoveTo(0f, 0f);

            Assert.AreEqual(0f, m_Level.Current);
        }

        [Test]
        public void ThenNoTimeHasSomethingNewOnce()
        {
            m_Level.MoveTo(0f, 0f);

            Assert.IsTrue(m_Level.Tick(0.1f));
            Assert.IsFalse(m_Level.Tick(0.1f));
        }

        [Test]
        public void ThenHalfTheTimeIsHalfWay()
        {
            m_Level.MoveTo(0f, 2f);
            m_Level.Tick(1f);

            Assert.AreEqual(0.5f, m_Level.Current, TOLERANCE);
        }

        [Test]
        public void ThenItHasSomethingNewUntilItArrives()
        {
            m_Level.MoveTo(0f, 2f);

            Assert.IsTrue(m_Level.Tick(1f));
            Assert.IsTrue(m_Level.Tick(1f));
            Assert.IsFalse(m_Level.Tick(1f));
        }

        [Test]
        public void ThenItArrivesExactly()
        {
            m_Level.MoveTo(0.25f, 2f);
            m_Level.Tick(5f);

            Assert.AreEqual(0.25f, m_Level.Current);
        }

        [Test]
        public void ThenASecondMoveGoesFromWhereItHasGotTo()
        {
            m_Level.MoveTo(0f, 2f);
            m_Level.Tick(1f);
            m_Level.MoveTo(1f, 1f);
            m_Level.Tick(0.5f);

            Assert.AreEqual(0.75f, m_Level.Current, TOLERANCE);
        }
    }
}
