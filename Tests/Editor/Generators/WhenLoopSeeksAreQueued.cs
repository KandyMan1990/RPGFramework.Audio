using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Generators
{
    /// <summary>How many loop seeks wait, for a song's thirty-second loop and a sound's quarter-second one.</summary>
    internal sealed class WhenLoopSeeksAreQueued
    {
        private int m_ForALongLoop;
        private int m_ForAShortLoop;

        [SetUp]
        public void SetUp()
        {
            m_ForALongLoop  = StemTimeline.LoopsToQueue(30.0);
            m_ForAShortLoop = StemTimeline.LoopsToQueue(0.25);
        }

        [Test]
        public void ThenTwoWaitForALongLoop()
        {
            Assert.AreEqual(2, m_ForALongLoop);
        }

        [Test]
        public void ThenEnoughWaitForAShortLoopToCoverTwoSeconds()
        {
            Assert.AreEqual(9, m_ForAShortLoop);
        }
    }
}
