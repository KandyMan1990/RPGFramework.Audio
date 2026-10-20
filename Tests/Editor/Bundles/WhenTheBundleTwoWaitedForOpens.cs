using System.Threading.Tasks;
using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Bundles
{
    internal sealed class WhenTheBundleTwoWaitedForOpens
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

            m_Bundles.Source.FinishOpening();
        }

        [TearDown]
        public void TearDown()
        {
            m_Bundles.Destroy();
        }

        [Test]
        public void ThenBothHaveTheSong()
        {
            Assert.AreSame(m_Bundles.Song, m_First.Result);
            Assert.AreSame(m_Bundles.Song, m_Second.Result);
        }
    }
}
