using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Generators
{
    /// <summary>A four-second stem with no loop, started at three seconds; a block of a second is about to play.</summary>
    internal sealed class WhenAStemThatDoesNotLoopRunsOut
    {
        private StemTimeline m_Timeline;
        private bool         m_EndsWithinTheBlock;

        [SetUp]
        public void SetUp()
        {
            m_Timeline           = new StemTimeline(3.0, 4.0, false, 0.0, 0.0);
            m_EndsWithinTheBlock = m_Timeline.EndsWithin(1.0);
            m_Timeline.Advance(1.0);
        }

        [Test]
        public void ThenItSaysSoBeforeTheBlockPlays()
        {
            Assert.IsTrue(m_EndsWithinTheBlock);
        }

        [Test]
        public void ThenItEnds()
        {
            Assert.IsTrue(m_Timeline.Ended);
        }
    }
}
