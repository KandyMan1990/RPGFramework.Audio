using NUnit.Framework;
using RPGFramework.Audio.Sfx;

namespace RPGFramework.Audio.Tests.Sfx
{
    /// <summary>Which sound plays next, and at what pitch, from the random numbers given.</summary>
    internal sealed class WhenACollectionPicks
    {
        private const float TOLERANCE = 0.001f;

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void ThenTwoSoundsAlternate(int last, int next)
        {
            Assert.AreEqual(next, SfxVariation.PickIndex(2, last, 0.5f));
        }

        [TestCase(0f, 0)]
        [TestCase(0.99f, 2)]
        public void ThenTheLastIsSkippedOver(float roll, int picked)
        {
            Assert.AreEqual(picked, SfxVariation.PickIndex(3, 1, roll));
        }

        [Test]
        public void ThenTheFirstTimeAnyCanBePicked()
        {
            Assert.AreEqual(2, SfxVariation.PickIndex(4, SfxVariation.NONE, 0.5f));
        }

        [Test]
        public void ThenTheTopOfTheRollStaysInRange()
        {
            Assert.AreEqual(3, SfxVariation.PickIndex(4, SfxVariation.NONE, 1f));
        }

        [Test]
        public void ThenOneSoundIsPickedAgain()
        {
            Assert.AreEqual(0, SfxVariation.PickIndex(1, 0, 0.5f));
        }

        [Test]
        public void ThenALastBeyondTheListIsIgnored()
        {
            Assert.AreEqual(0, SfxVariation.PickIndex(2, 5, 0f));
        }

        [TestCase(0f, 0.95f)]
        [TestCase(0.5f, 1f)]
        [TestCase(1f, 1.05f)]
        public void ThenThePitchVariesEitherWay(float roll, float pitch)
        {
            Assert.AreEqual(pitch, SfxVariation.PickPitch(0.05f, roll), TOLERANCE);
        }

        [Test]
        public void ThenNoVariationIsNormalPitch()
        {
            Assert.AreEqual(1f, SfxVariation.PickPitch(0f, 0.1f), TOLERANCE);
        }
    }
}
