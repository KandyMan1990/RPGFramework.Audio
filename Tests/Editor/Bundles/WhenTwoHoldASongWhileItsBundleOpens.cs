using System.Threading.Tasks;
using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Bundles
{
    internal sealed class WhenTwoHoldASongWhileItsBundleOpens
    {
        private SongBundles      m_Bundles;
        private Task<MusicAsset> m_First;
        private Task<MusicAsset> m_Second;

        [SetUp]
        public void SetUp()
        {
            m_Bundles = new SongBundles();
            m_First   = m_Bundles.Holds.AcquireAsync(SongBundles.SONG);
            m_Second  = m_Bundles.Holds.AcquireAsync(SongBundles.SONG);
        }

        [TearDown]
        public void TearDown()
        {
            m_Bundles.Destroy();
        }

        [Test]
        public void ThenItsBundleOpensOnce()
        {
            Assert.AreEqual(1, m_Bundles.Source.Opens);
        }

        [Test]
        public void ThenTheBundleOpenedIsTheSongsNameHashs()
        {
            Assert.AreEqual(SongBundles.SONG, m_Bundles.Source.LastOpened);
        }

        [Test]
        public void ThenNeitherHasItYet()
        {
            Assert.IsFalse(m_First.IsCompleted);
            Assert.IsFalse(m_Second.IsCompleted);
        }
    }
}
