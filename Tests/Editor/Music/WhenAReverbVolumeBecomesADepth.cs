using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenAReverbVolumeBecomesADepth
    {
        [TestCase(0f,   0f)]
        [TestCase(0.5f, 63.5f)]
        [TestCase(1f,   127f)]
        public void ThenItRisesInAStraightLineTo127(float volume, float depth)
        {
            Assert.AreEqual(depth, AudioUtils.ReverbVolumeToDepth(volume));
        }

        [TestCase(-0.5f, 0f)]
        [TestCase(2f,    127f)]
        public void ThenItIsHeldBetween0And127(float volume, float depth)
        {
            Assert.AreEqual(depth, AudioUtils.ReverbVolumeToDepth(volume));
        }
    }
}
