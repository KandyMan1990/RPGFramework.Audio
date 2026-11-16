using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Preloads
{
    /// <summary>A song preloaded and held, asked for again, then unloaded.</summary>
    internal sealed class WhenAPreloadedSongIsUnloaded
    {
        private AudioPreloads<string> m_Preloads;
        private bool                  m_AskedAgain;
        private bool                  m_Ended;
        private string                m_Released;

        [SetUp]
        public void SetUp()
        {
            m_Preloads = new AudioPreloads<string>();

            m_Preloads.TryBegin(1UL, out int request);
            m_Preloads.TryHold(1UL, request, "Song");

            m_AskedAgain = m_Preloads.TryBegin(1UL, out int _);
            m_Ended      = m_Preloads.TryEnd(1UL, out m_Released);
        }

        [Test]
        public void ThenAskingAgainWhileHeldStartsNothing()
        {
            Assert.IsFalse(m_AskedAgain);
        }

        [Test]
        public void ThenTheUnloadLetsGoOfWhatWasHeld()
        {
            Assert.IsTrue(m_Ended);
            Assert.AreEqual("Song", m_Released);
        }

        [Test]
        public void ThenNothingIsLeftToUnload()
        {
            Assert.AreEqual(0, m_Preloads.GetNames().Count);
        }
    }
}
