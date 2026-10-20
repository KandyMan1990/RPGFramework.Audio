using NUnit.Framework;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongIsNew
    {
        private MusicAsset m_Song;

        [SetUp]
        public void SetUp()
        {
            m_Song = SongAuthoring.New();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Song);
        }

        [Test]
        public void ThenItDoesNotLoop()
        {
            Assert.IsFalse(((IMusicAsset)m_Song).Loop);
        }

        [Test]
        public void ThenItLeavesTheEchoAsItWas()
        {
            Assert.AreEqual(EchoTiming.None, ((IMusicAsset)m_Song).Echo.Timing);
        }

        [Test]
        public void ThenItLeavesTheReverbPresetAsItWas()
        {
            Assert.IsFalse(((IMusicAsset)m_Song).Reverb.SetsPreset);
        }

        [Test]
        public void ThenItLeavesTheReverbVolumeAsItWas()
        {
            Assert.IsFalse(((IMusicAsset)m_Song).Reverb.SetsVolume);
        }

        [Test]
        public void ThenItsReverbVolumeOnceTickedIsPSXReverbsDefaultDepth()
        {
            Assert.AreEqual(40f, AudioUtils.ReverbVolumeToDepth(((IMusicAsset)m_Song).Reverb.Volume), 0.001f);
        }
    }
}
