using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Generators
{
    /// <summary>A four-second stem looping to its very end, a block of a second from it.</summary>
    internal sealed class WhenALoopingStemReachesTheEndOfItsLoop
    {
        private StemTimeline m_Timeline;
        private bool         m_EndsWithinTheBlock;

        [SetUp]
        public void SetUp()
        {
            m_Timeline           = new StemTimeline(3.0, 4.0, true, 1.0, 4.0);
            m_EndsWithinTheBlock = m_Timeline.EndsWithin(1.0);
            m_Timeline.Advance(1.0);
        }

        [Test]
        public void ThenItDoesNotExpectSilenceAfterIt()
        {
            Assert.IsFalse(m_EndsWithinTheBlock);
        }

        [Test]
        public void ThenItGoesBackRatherThanEnding()
        {
            Assert.IsFalse(m_Timeline.Ended);
        }

        [Test]
        public void ThenItIsAtTheLoopStart()
        {
            Assert.AreEqual(1.0, m_Timeline.Position, 1e-9);
        }
    }
}
