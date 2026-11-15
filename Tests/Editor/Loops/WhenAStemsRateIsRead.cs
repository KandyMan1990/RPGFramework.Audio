using NUnit.Framework;
using RPGFramework.Audio.Editor.Loops;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>A stem's source file read for its sample rate, which an import can resample the clip away from.</summary>
    internal sealed class WhenAStemsRateIsRead
    {
        [Test]
        public void ThenAWavsIsItsFormatChunks()
        {
            WavLoopReader.TryReadRate(WavFile.Write(48000, 16), out int rate, out _);

            Assert.AreEqual(48000, rate);
        }

        [Test]
        public void ThenAWavWithAChunkBeforeItsFormatIsRead()
        {
            WavLoopReader.TryReadRate(WavFile.Write(44100, 16, junkFirst: true), out int rate, out _);

            Assert.AreEqual(44100, rate);
        }

        [Test]
        public void ThenAnRF64WavIsRefusedByName()
        {
            WavLoopReader.TryReadRate(WavFile.Write(44100, 16, riff: "RF64"), out _, out string problem);

            StringAssert.Contains("RF64", problem);
        }

        [Test]
        public void ThenAnOggIsNotReadAsAWav()
        {
            Assert.IsFalse(WavLoopReader.TryReadRate(OggFile.Write(44100), out _, out _));
        }

        [Test]
        public void ThenAnOggsIsItsIdentificationHeaders()
        {
            OggLoopReader.TryReadRate(OggFile.Write(22050), out int rate, out _);

            Assert.AreEqual(22050, rate);
        }

        [Test]
        public void ThenAnOggThatIsNotVorbisIsRefused()
        {
            Assert.IsFalse(OggLoopReader.TryReadRate(OggFile.Write(48000, "xorbis"), out _, out _));
        }

        [Test]
        public void ThenAWavIsNotReadAsAnOgg()
        {
            Assert.IsFalse(OggLoopReader.TryReadRate(WavFile.Write(44100, 16), out _, out _));
        }
    }
}
