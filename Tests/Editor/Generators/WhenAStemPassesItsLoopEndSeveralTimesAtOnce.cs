using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Generators
{
    /// <summary>A half-second loop from one second, played on two and a quarter seconds from its start.</summary>
    internal sealed class WhenAStemPassesItsLoopEndSeveralTimesAtOnce
    {
        private StemTimeline m_Timeline;

        [SetUp]
        public void SetUp()
        {
            m_Timeline = new StemTimeline(1.0, 2.0, true, 1.0, 1.5);
            m_Timeline.Advance(2.25);
        }

        [Test]
        public void ThenItCountsEveryPass()
        {
            Assert.AreEqual(4, m_Timeline.Passes);
        }

        [Test]
        public void ThenItIsWhereTheLastPassLeftIt()
        {
            Assert.AreEqual(1.25, m_Timeline.Position, 1e-9);
        }
    }
}
