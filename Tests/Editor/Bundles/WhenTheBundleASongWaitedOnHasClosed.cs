using System.Threading.Tasks;
using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Bundles
{
    internal sealed class WhenTheBundleASongWaitedOnHasClosed
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
            m_Bundles.Source.FinishClosing();
        }

        [TearDown]
        public void TearDown()
        {
            m_Bundles.Destroy();
        }

        [Test]
        public void ThenItsBundleOpensAfresh()
        {
            Assert.AreEqual(2, m_Bundles.Source.Opens);
        }

        [Test]
        public void ThenItHasTheSongOnceTheBundleOpens()
        {
            m_Bundles.Source.FinishOpening();

            Assert.AreSame(m_Bundles.Song, m_During.Result);
        }
    }
}
