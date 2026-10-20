using System;
using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenASongHasMoreStemsThanChannels
    {
        private MusicChannelPool m_Channels;

        [SetUp]
        public void SetUp()
        {
            m_Channels = new MusicChannelPool(4);
        }

        [Test]
        public void ThenTakingThemThrows()
        {
            Assert.Throws<InvalidOperationException>(() => m_Channels.Take("Boss", 5));
        }

        [Test]
        public void ThenTheErrorNamesTheSongItsStemsAndTheChannels()
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => m_Channels.Take("Boss", 5));

            StringAssert.Contains("[Boss] has 5 stems", error.Message);
            StringAssert.Contains("4 music channels", error.Message);
        }
    }
}
