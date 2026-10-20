using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Bundles
{
    internal sealed class WhenASongsBundleFailsToOpen
    {
        private SongBundles m_Bundles;
        private Exception   m_Error;

        [SetUp]
        public void SetUp()
        {
            m_Bundles = new SongBundles();

            Task<MusicAsset> failed = m_Bundles.Holds.AcquireAsync(SongBundles.SONG);

            m_Bundles.Source.FailOpening();
            m_Error = failed.Exception?.InnerException;
        }

        [TearDown]
        public void TearDown()
        {
            m_Bundles.Destroy();
        }

        [Test]
        public void ThenTheHoldFailsWithTheSourcesError()
        {
            Assert.IsInstanceOf<IOException>(m_Error);
        }

        [Test]
        public void ThenTheNextHoldOpensItAgain()
        {
            m_Bundles.Holds.AcquireAsync(SongBundles.SONG);

            Assert.AreEqual(2, m_Bundles.Source.Opens);
        }
    }
}
