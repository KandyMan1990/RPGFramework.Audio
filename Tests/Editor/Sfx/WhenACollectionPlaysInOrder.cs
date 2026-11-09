using NUnit.Framework;
using RPGFramework.Audio.Sfx;

namespace RPGFramework.Audio.Tests.Sfx
{
    /// <summary>Which of three sounds a collection playing in order plays.</summary>
    internal sealed class WhenACollectionPlaysInOrder
    {
        private const int COUNT = 3;

        [Test]
        public void ThenTheFirstTimeItPlaysTheFirst()
        {
            Assert.AreEqual(0, SfxVariation.InOrderIndex(COUNT, SfxVariation.NONE, ISfxPlayer.NEXT_IN_ORDER));
        }

        [Test]
        public void ThenItPlaysTheOneAfterTheLast()
        {
            Assert.AreEqual(2, SfxVariation.InOrderIndex(COUNT, 1, ISfxPlayer.NEXT_IN_ORDER));
        }

        [Test]
        public void ThenAfterTheLastItGoesRound()
        {
            Assert.AreEqual(0, SfxVariation.InOrderIndex(COUNT, 2, ISfxPlayer.NEXT_IN_ORDER));
        }

        [Test]
        public void ThenAPositionStartsItWhereItSays()
        {
            Assert.AreEqual(0, SfxVariation.InOrderIndex(COUNT, 1, 0));
        }

        [Test]
        public void ThenAPositionPastTheEndGoesRound()
        {
            Assert.AreEqual(1, SfxVariation.InOrderIndex(COUNT, 0, 4));
        }
    }
}
