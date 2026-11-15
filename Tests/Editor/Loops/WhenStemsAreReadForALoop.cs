using NUnit.Framework;
using RPGFramework.Audio.Editor.Loops;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>A song's stems read together, each as its source file was read.</summary>
    internal sealed class WhenStemsAreReadForALoop
    {
        private static StemLoopReading Read(params StemMarkers[] markers)
        {
            string[] names = new string[markers.Length];

            for (int i = 0; i < names.Length; i++)
            {
                names[i] = $"Stem{i + 1}";
            }

            return StemLoopReading.Read(names, markers, new string[markers.Length]);
        }

        private static StemMarkers Loop(int rate, long frames, long start, long end)
        {
            StemMarkers.TryCreateLoop(rate, frames, start, end, "its markers", null, out StemMarkers markers, out _);

            return markers;
        }

        [Test]
        public void ThenStemsThatAgreeGiveTheirLoop()
        {
            StemLoopReading reading = Read(Loop(44100, 1000, 100, 800), Loop(44100, 1000, 100, 800));

            Assert.IsTrue(reading.Found);
            Assert.AreEqual(100, reading.LoopStart);
            Assert.AreEqual(800, reading.LoopEnd);
            Assert.AreEqual(44100, reading.SampleRate);
        }

        [Test]
        public void ThenStemsAtDifferentRatesAgreeOnTheSameMoment()
        {
            StemLoopReading reading = Read(Loop(44100, 44100, 22050, 33075), Loop(48000, 48000, 24000, 36000));

            Assert.IsTrue(reading.Found);
            Assert.AreEqual(44100, reading.SampleRate);
        }

        [Test]
        public void ThenStemsWhoseLoopsDifferAreRefused()
        {
            StemLoopReading reading = Read(Loop(44100, 1000, 100, 800), Loop(44100, 1000, 100, 900));

            Assert.IsTrue(reading.Refused);
            Assert.IsFalse(reading.Found);
        }

        [Test]
        public void ThenStemsOfDifferentLengthsAreRefused()
        {
            StemLoopReading reading = Read(Loop(44100, 1000, 100, 800), Loop(44100, 2000, 100, 800));

            Assert.IsTrue(reading.Refused);
            StringAssert.Contains("not the same length", reading.Report);
        }

        [Test]
        public void ThenAStemWithoutALoopIsSkippedAndNamed()
        {
            StemLoopReading reading = Read(Loop(44100, 1000, 100, 800), new StemMarkers(44100, 1000));

            Assert.IsTrue(reading.Found);
            StringAssert.Contains("Stem [2], [Stem2], carries no loop", reading.Report);
        }

        [Test]
        public void ThenStemsWithoutALoopGiveNoneAndRefuseNothing()
        {
            StemLoopReading reading = Read(new StemMarkers(44100, 1000), new StemMarkers(44100, 1000));

            Assert.IsFalse(reading.Found);
            Assert.IsFalse(reading.Refused);
        }

        [Test]
        public void ThenAStemThatCannotBeReadRefusesTheRest()
        {
            StemLoopReading reading = StemLoopReading.Read(new[] { "Stem1", "Stem2" }, new[] { Loop(44100, 1000, 100, 800), null }, new[] { null, "it is not a RIFF WAVE file" });

            Assert.IsTrue(reading.Refused);
        }

        [Test]
        public void ThenALoopIsWrittenToASongWithNoLoopYet()
        {
            Assert.IsTrue(StemLoopReading.AppliesWhenAdded(false, LoopUnit.Bars));
        }

        [Test]
        public void ThenALoopIsWrittenToASongLoopingBySamples()
        {
            Assert.IsTrue(StemLoopReading.AppliesWhenAdded(true, LoopUnit.Samples));
        }

        [Test]
        public void ThenALoopIsOnlyOfferedToASongLoopingByBars()
        {
            Assert.IsFalse(StemLoopReading.AppliesWhenAdded(true, LoopUnit.Bars));
        }
    }
}
