using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Generators
{
    /// <summary>A four-second stem looping from three seconds to its end, started at two and a half and played on a second and three quarters.</summary>
    internal sealed class WhenAStemPassesItsLoopEnd
    {
        private StemTimeline m_Timeline;

        [SetUp]
        public void SetUp()
        {
            m_Timeline = new StemTimeline(2.5, 4.0, true, 3.0, 4.0);
            m_Timeline.Advance(1.75);
        }

        [Test]
        public void ThenItIsAsFarPastTheLoopStartAsItWentPastTheEnd()
        {
            Assert.AreEqual(3.25, m_Timeline.Position, 1e-9);
        }

        [Test]
        public void ThenItCountsThePass()
        {
            Assert.AreEqual(1, m_Timeline.Passes);
        }

        [Test]
        public void ThenItHasNotEnded()
        {
            Assert.IsFalse(m_Timeline.Ended);
        }
    }
}
