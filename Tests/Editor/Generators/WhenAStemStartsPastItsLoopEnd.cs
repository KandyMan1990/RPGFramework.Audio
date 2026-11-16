using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Generators
{
    /// <summary>
    /// A four-second stem looping from one second to three, started at three and a half, where the loop's seek has
    /// already passed and is dropped; it plays on for a second.
    /// </summary>
    internal sealed class WhenAStemStartsPastItsLoopEnd
    {
        private StemTimeline m_Timeline;

        [SetUp]
        public void SetUp()
        {
            m_Timeline = new StemTimeline(3.5, 4.0, true, 1.0, 3.0);
            m_Timeline.Advance(1.0);
        }

        [Test]
        public void ThenItDoesNotGoBack()
        {
            Assert.AreEqual(0, m_Timeline.Passes);
        }

        [Test]
        public void ThenItEndsWithItsClip()
        {
            Assert.IsTrue(m_Timeline.Ended);
        }
    }
}
