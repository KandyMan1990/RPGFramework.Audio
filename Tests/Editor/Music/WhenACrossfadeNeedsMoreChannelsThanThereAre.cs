using System;
using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenACrossfadeNeedsMoreChannelsThanThereAre
    {
        private MusicChannelPool m_Channels;

        [SetUp]
        public void SetUp()
        {
            m_Channels = new MusicChannelPool(16);

            m_Channels.Take("Field Theme", 10);
        }

        [Test]
        public void ThenTakingThemThrows()
        {
            Assert.Throws<InvalidOperationException>(() => m_Channels.Take("World Map", 8));
        }

        [Test]
        public void ThenTheErrorNamesBothSongsAndTheChannels()
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => m_Channels.Take("World Map", 8));

            StringAssert.Contains("[Field Theme] (10 stems)", error.Message);
            StringAssert.Contains("[World Map] (8 stems)", error.Message);
            StringAssert.Contains("needs 18 music channels", error.Message);
            StringAssert.Contains("the mixer has 16", error.Message);
        }

        [Test]
        public void ThenNoneAreTaken()
        {
            Assert.Throws<InvalidOperationException>(() => m_Channels.Take("World Map", 8));

            CollectionAssert.AreEqual(new[] { 10, 11, 12, 13, 14, 15 }, m_Channels.Take("Town", 6));
        }
    }
}
