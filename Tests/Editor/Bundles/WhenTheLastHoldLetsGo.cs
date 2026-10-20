using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Bundles
{
    internal sealed class WhenTheLastHoldLetsGo
    {
        private SongBundles m_Bundles;

        [SetUp]
        public void SetUp()
        {
            m_Bundles = new SongBundles();
            m_Bundles.Holds.AcquireAsync(SongBundles.SONG);
            m_Bundles.Holds.AcquireAsync(SongBundles.SONG);
            m_Bundles.Source.FinishOpening();

            m_Bundles.Holds.Release(SongBundles.SONG);
            m_Bundles.Holds.Release(SongBundles.SONG);
        }

        [TearDown]
        public void TearDown()
        {
            m_Bundles.Destroy();
        }

        [Test]
        public void ThenItsBundleCloses()
        {
            Assert.AreEqual(1, m_Bundles.Source.Closes);
        }
    }
}
