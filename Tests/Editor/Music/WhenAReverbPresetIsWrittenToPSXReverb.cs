using NUnit.Framework;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Music
{
    internal sealed class WhenAReverbPresetIsWrittenToPSXReverb
    {
        [TestCase(ReverbPreset.Off,     0)]
        [TestCase(ReverbPreset.Room,    1)]
        [TestCase(ReverbPreset.StudioA, 2)]
        [TestCase(ReverbPreset.StudioB, 3)]
        [TestCase(ReverbPreset.StudioC, 4)]
        [TestCase(ReverbPreset.Hall,    5)]
        [TestCase(ReverbPreset.Space,   6)]
        [TestCase(ReverbPreset.Echo,    7)]
        [TestCase(ReverbPreset.Delay,   8)]
        [TestCase(ReverbPreset.Pipe,    9)]
        public void ThenItIsTheNumberPSXReverbGivesIt(ReverbPreset preset, int number)
        {
            Assert.AreEqual(number, (int)preset);
        }
    }
}
