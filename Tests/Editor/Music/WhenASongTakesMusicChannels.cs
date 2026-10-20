using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongTakesMusicChannels
    {
        private int[] m_Field;

        [SetUp]
        public void SetUp()
        {
            MusicChannelPool channels = new MusicChannelPool(6);

            m_Field = channels.Take("Field", 4);
        }

        [Test]
        public void ThenItTakesOnePerStemLowestFirst()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, m_Field);
        }
    }
}
