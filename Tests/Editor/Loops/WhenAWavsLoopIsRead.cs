using NUnit.Framework;
using RPGFramework.Audio.Editor.Loops;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>A thousand-frame WAV at 44,100 Hz, read for its loop as each way of marking one writes it.</summary>
    internal sealed class WhenAWavsLoopIsRead
    {
        private static string[] LoopNames => new[] { "LOOPSTART", "LOOPEND" };

        private static StemMarkers Read(string[] names = null, uint[] samples = null, uint[] samplerLoop = null, bool flStudio = false)
        {
            WavLoopReader.TryReadMarkers(WavFile.Write(44100, 1000, flStudio, markerNames: names, markerSamples: samples, samplerLoop: samplerLoop, flStudioChunks: flStudio), out StemMarkers markers, out _);

            return markers;
        }

        private static string Problem(string[] names = null, uint[] samples = null, uint[] samplerLoop = null)
        {
            WavLoopReader.TryReadMarkers(WavFile.Write(44100, 1000, markerNames: names, markerSamples: samples, samplerLoop: samplerLoop), out _, out string problem);

            return problem;
        }

        [Test]
        public void ThenNamedMarkersAreItsLoop()
        {
            StemMarkers markers = Read(LoopNames, new uint[] { 100, 800 });

            Assert.AreEqual(100, markers.LoopStart);
            Assert.AreEqual(800, markers.LoopEnd);
        }

        [Test]
        public void ThenItsLengthIsItsDataInFrames()
        {
            Assert.AreEqual(1000, Read().Frames);
        }

        [Test]
        public void ThenWithoutMarkersItHasNoLoop()
        {
            Assert.IsFalse(Read().HasLoop);
        }

        [Test]
        public void ThenMarkersAreNamedWhateverTheirCase()
        {
            Assert.AreEqual(800, Read(new[] { "LoopStart", "loopend" }, new uint[] { 100, 800 }).LoopEnd);
        }

        [Test]
        public void ThenASamplerLoopAfterAnIntroEndsOnePastItsLastFrame()
        {
            StemMarkers markers = Read(samplerLoop: new uint[] { 200, 799 });

            Assert.AreEqual(200, markers.LoopStart);
            Assert.AreEqual(800, markers.LoopEnd);
        }

        [Test]
        public void ThenASamplerLoopOverTheWholeFileLoopsTheWholeFile()
        {
            StemMarkers markers = Read(samplerLoop: new uint[] { 0, 999 });

            Assert.AreEqual(0, markers.LoopStart);
            Assert.AreEqual(1000, markers.LoopEnd);
        }

        [Test]
        public void ThenNamedMarkersWinOverASamplerLoopOverTheWholeFile()
        {
            StemMarkers markers = Read(LoopNames, new uint[] { 100, 800 }, new uint[] { 0, 999 });

            Assert.AreEqual(100, markers.LoopStart);
            Assert.IsNotNull(markers.SetAside);
        }

        [Test]
        public void ThenNamedMarkersAndASamplerLoopThatDifferAreRefused()
        {
            StringAssert.Contains("differ", Problem(LoopNames, new uint[] { 100, 800 }, new uint[] { 200, 799 }));
        }

        [Test]
        public void ThenALoopStartWithoutALoopEndIsRefused()
        {
            StringAssert.Contains("no LOOPEND", Problem(new[] { "LOOPSTART" }, new uint[] { 100 }));
        }

        [Test]
        public void ThenALoopEndingPastTheFileIsRefused()
        {
            StringAssert.Contains("past the file's end", Problem(LoopNames, new uint[] { 100, 1200 }));
        }

        [Test]
        public void ThenTheChunksFLStudioAddsAreSkipped()
        {
            Assert.AreEqual(800, Read(LoopNames, new uint[] { 100, 800 }, flStudio: true).LoopEnd);
        }
    }
}
