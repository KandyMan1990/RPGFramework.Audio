using System.Threading.Tasks;
using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Bundles
{
    internal sealed class WhenASongAlreadyHeldIsAskedFor
    {
        private SongBundles      m_Bundles;
        private Task<MusicAsset> m_Again;

        [SetUp]
        public void SetUp()
        {
            m_Bundles = new SongBundles();
            m_Bundles.Holds.AcquireAsync(SongBundles.SONG);
            m_Bundles.Source.FinishOpening();

            m_Again = m_Bundles.Holds.AcquireAsync(SongBundles.SONG);
        }

        [TearDown]
        public void TearDown()
        {
            m_Bundles.Destroy();
        }

        [Test]
        public void ThenItComesBackAtOnce()
        {
            Assert.IsTrue(m_Again.IsCompleted);
        }

        [Test]
        public void ThenItsBundleIsNotOpenedAgain()
        {
            Assert.AreEqual(1, m_Bundles.Source.Opens);
        }
    }
}
