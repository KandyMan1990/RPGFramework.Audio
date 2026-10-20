using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongGivesItsChannelsBack
    {
        private int[] m_Battle;

        [SetUp]
        public void SetUp()
        {
            MusicChannelPool channels = new MusicChannelPool(6);
            int[]            field    = channels.Take("Field", 4);

            channels.Take("World Map", 2);
            channels.Free(field);

            m_Battle = channels.Take("Battle", 3);
        }

        [Test]
        public void ThenTheNextSongTakesThemLowestFirst()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, m_Battle);
        }
    }
}
