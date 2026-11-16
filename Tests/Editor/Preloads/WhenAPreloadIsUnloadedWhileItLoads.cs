using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Preloads
{
    /// <summary>A song preloaded, then unloaded before its asset arrives.</summary>
    internal sealed class WhenAPreloadIsUnloadedWhileItLoads
    {
        private AudioPreloads<string> m_Preloads;
        private bool                  m_EndedWithAnAsset;
        private bool                  m_Held;

        [SetUp]
        public void SetUp()
        {
            m_Preloads = new AudioPreloads<string>();

            m_Preloads.TryBegin(1UL, out int request);

            m_EndedWithAnAsset = m_Preloads.TryEnd(1UL, out string _);
            m_Held             = m_Preloads.TryHold(1UL, request, "Song");
        }

        [Test]
        public void ThenTheUnloadHasNothingToLetGo()
        {
            Assert.IsFalse(m_EndedWithAnAsset);
        }

        [Test]
        public void ThenWhatArrivesIsNotHeld()
        {
            Assert.IsFalse(m_Held);
        }

        [Test]
        public void ThenItsClipsAreNoLongerWanted()
        {
            Assert.IsFalse(m_Preloads.Holds(1UL));
        }
    }
}
