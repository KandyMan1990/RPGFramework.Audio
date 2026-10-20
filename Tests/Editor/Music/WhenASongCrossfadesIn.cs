using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongCrossfadesIn
    {
        private int[] m_WorldMap;

        [SetUp]
        public void SetUp()
        {
            MusicChannelPool channels = new MusicChannelPool(6);

            channels.Take("Field", 4);
            m_WorldMap = channels.Take("World Map", 2);
        }

        [Test]
        public void ThenItTakesTheNextFreeChannels()
        {
            CollectionAssert.AreEqual(new[] { 4, 5 }, m_WorldMap);
        }
    }
}
