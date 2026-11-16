using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Preloads
{
    /// <summary>
    /// A song preloaded, unloaded and preloaded again while the first preload's asset is still on its way; then both
    /// arrive.
    /// </summary>
    internal sealed class WhenAPreloadIsAskedForAgainAfterAnUnload
    {
        private AudioPreloads<string> m_Preloads;
        private bool                  m_FirstHeld;
        private bool                  m_SecondHeld;

        [SetUp]
        public void SetUp()
        {
            m_Preloads = new AudioPreloads<string>();

            m_Preloads.TryBegin(1UL, out int first);
            m_Preloads.TryEnd(1UL, out string _);
            m_Preloads.TryBegin(1UL, out int second);

            m_FirstHeld  = m_Preloads.TryHold(1UL, first, "Song");
            m_SecondHeld = m_Preloads.TryHold(1UL, second, "Song");
        }

        [Test]
        public void ThenTheFirstLetsGo()
        {
            Assert.IsFalse(m_FirstHeld);
        }

        [Test]
        public void ThenTheSecondHoldsIt()
        {
            Assert.IsTrue(m_SecondHeld);
        }
    }
}
