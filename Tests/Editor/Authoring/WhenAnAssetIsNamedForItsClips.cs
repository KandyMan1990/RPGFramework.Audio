using NUnit.Framework;
using RPGFramework.Audio.Editor;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Authoring
{
    /// <summary>Names for a song or a sound made from clips: Battle_Boss01 to 03, Rain alone, two sharing nothing, and none.</summary>
    internal sealed class WhenAnAssetIsNamedForItsClips
    {
        private AudioClip[] m_Boss;
        private AudioClip   m_Rain;
        private AudioClip   m_Sea;

        [SetUp]
        public void SetUp()
        {
            m_Boss = new[]
                     {
                         AudioClip.Create("Battle_Boss01", 4410, 1, 44100, false),
                         AudioClip.Create("Battle_Boss02", 4410, 1, 44100, false),
                         AudioClip.Create("Battle_Boss03", 4410, 1, 44100, false)
                     };
            m_Rain = AudioClip.Create("Rain", 4410, 1, 44100, false);
            m_Sea  = AudioClip.Create("Sea", 4410, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < m_Boss.Length; i++)
            {
                Object.DestroyImmediate(m_Boss[i]);
            }

            Object.DestroyImmediate(m_Rain);
            Object.DestroyImmediate(m_Sea);
        }

        [Test]
        public void ThenStemsAreNamedForWhatTheyShare()
        {
            Assert.AreEqual("Battle_Boss", StemTracks.NameFor(m_Boss, "SFX Asset"));
        }

        [Test]
        public void ThenOneClipGivesItsOwnName()
        {
            Assert.AreEqual("Rain", StemTracks.NameFor(new[] { m_Rain }, "SFX Asset"));
        }

        [Test]
        public void ThenClipsSharingNothingGiveTheDefault()
        {
            Assert.AreEqual("SFX Asset", StemTracks.NameFor(new[] { m_Rain, m_Sea }, "SFX Asset"));
        }

        [Test]
        public void ThenNoClipsGiveTheDefault()
        {
            Assert.AreEqual("Music Asset", StemTracks.NameFor(new AudioClip[0], "Music Asset"));
        }
    }
}
