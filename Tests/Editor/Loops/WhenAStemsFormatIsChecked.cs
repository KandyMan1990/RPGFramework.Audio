using NUnit.Framework;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>Only a WAV or an OGG can be looped, as other formats can move the loop points.</summary>
    internal sealed class WhenAStemsFormatIsChecked
    {
        [Test]
        public void ThenAWavCanLoop()
        {
            Assert.IsTrue(LoopedStems.IsLoopableFormat("Assets/Music/Song.wav"));
        }

        [Test]
        public void ThenAnOggCanLoopWhateverTheCaseOfItsExtension()
        {
            Assert.IsTrue(LoopedStems.IsLoopableFormat("Assets/Music/Song.OGG"));
        }

        [Test]
        public void ThenAnMp3CannotLoop()
        {
            Assert.IsFalse(LoopedStems.IsLoopableFormat("Assets/Music/Song.mp3"));
        }

        [Test]
        public void ThenAnAiffCannotLoop()
        {
            Assert.IsFalse(LoopedStems.IsLoopableFormat("Assets/Music/Song.aif"));
        }
    }
}
