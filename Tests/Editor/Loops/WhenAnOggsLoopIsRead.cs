using NUnit.Framework;
using RPGFramework.Audio.Editor.Loops;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>A thousand-sample OGG at 44,100 Hz, read for its loop from its comments.</summary>
    internal sealed class WhenAnOggsLoopIsRead
    {
        private static bool TryRead(out StemMarkers markers, out string problem, bool acrossPages, params string[] comments)
        {
            return OggLoopReader.TryReadMarkers(OggFile.Write(44100, comments: comments, commentsAcrossPages: acrossPages), out markers, out problem);
        }

        private static StemMarkers Read(params string[] comments)
        {
            TryRead(out StemMarkers markers, out _, false, comments);

            return markers;
        }

        [Test]
        public void ThenALoopLengthIsCountedFromItsStart()
        {
            StemMarkers markers = Read("LOOPSTART=100", "LOOPLENGTH=700");

            Assert.AreEqual(100, markers.LoopStart);
            Assert.AreEqual(800, markers.LoopEnd);
        }

        [Test]
        public void ThenALoopEndIsItsEnd()
        {
            Assert.AreEqual(800, Read("LOOPSTART=100", "LOOPEND=800").LoopEnd);
        }

        [Test]
        public void ThenALoopStartAloneLoopsToTheFilesEnd()
        {
            Assert.AreEqual(1000, Read("LOOPSTART=100").LoopEnd);
        }

        [Test]
        public void ThenItsLengthIsItsLastPagesGranule()
        {
            Assert.AreEqual(1000, Read().Frames);
        }

        [Test]
        public void ThenWithoutCommentsItHasNoLoop()
        {
            Assert.IsFalse(Read("TITLE=Song").HasLoop);
        }

        [Test]
        public void ThenALengthAndAnEndMoreThanASampleApartAreRefused()
        {
            TryRead(out _, out string problem, false, "LOOPSTART=100", "LOOPLENGTH=700", "LOOPEND=900");

            StringAssert.Contains("more than a sample apart", problem);
        }

        [Test]
        public void ThenALengthAndAnEndASampleApartAgree()
        {
            Assert.IsTrue(TryRead(out _, out _, false, "LOOPSTART=100", "LOOPLENGTH=700", "LOOPEND=801"));
        }

        [Test]
        public void ThenKeysAreReadWhateverTheirCase()
        {
            Assert.AreEqual(800, Read("loopstart=100", "LoopLength=700").LoopEnd);
        }

        [Test]
        public void ThenCommentsAcrossTwoPagesAreRead()
        {
            TryRead(out StemMarkers markers, out _, true, "LOOPSTART=100", "LOOPLENGTH=700");

            Assert.AreEqual(800, markers.LoopEnd);
        }

        [Test]
        public void ThenALoopEndWithoutALoopStartIsRefused()
        {
            TryRead(out _, out string problem, false, "LOOPEND=800");

            StringAssert.Contains("no LOOPSTART", problem);
        }
    }
}
