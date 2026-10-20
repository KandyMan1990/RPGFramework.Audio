using System.Threading.Tasks;
using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Bundles
{
    internal sealed class WhenASongIsAskedForWhileItsBundleCloses
    {
        private SongBundles      m_Bundles;
        private Task<MusicAsset> m_During;

        [SetUp]
        public void SetUp()
        {
            m_Bundles = new SongBundles();
            m_Bundles.Holds.AcquireAsync(SongBundles.SONG);
            m_Bundles.Source.FinishOpening();
            m_Bundles.Holds.Release(SongBundles.SONG);

            m_During = m_Bundles.Holds.AcquireAsync(SongBundles.SONG);
        }

        [TearDown]
        public void TearDown()
        {
            m_Bundles.Destroy();
        }

        [Test]
        public void ThenItsBundleIsNotOpenedUntilItHasClosed()
        {
            Assert.AreEqual(1, m_Bundles.Source.Opens);
        }

        [Test]
        public void ThenItDoesNotHaveItYet()
        {
            Assert.IsFalse(m_During.IsCompleted);
        }
    }
}
