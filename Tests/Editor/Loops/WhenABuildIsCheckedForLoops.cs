using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Audio.Editor;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Tests.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>
    /// Two songs on one four-second in-memory stem: one looping to its very end and one leaving a second after its loop,
    /// checked for a build off the web, where a generator loops at the sample, and for the web, where the player watches
    /// the playhead and cannot catch a loop with nothing after it.
    /// </summary>
    internal sealed class WhenABuildIsCheckedForLoops
    {
        private AudioClip    m_Clip;
        private MusicAsset   m_EndsAtTheEnd;
        private MusicAsset   m_LeavesASecond;
        private List<string> m_OffTheWeb;
        private List<string> m_ForTheWeb;

        [SetUp]
        public void SetUp()
        {
            m_Clip          = AudioClip.Create("Stem", 176400, 1, 44100, false);
            m_EndsAtTheEnd  = SongAuthoring.New();
            m_LeavesASecond = SongAuthoring.New();

            SongAuthoring.SetStem(m_EndsAtTheEnd, m_Clip);
            SongAuthoring.SetStem(m_LeavesASecond, m_Clip);
            SongAuthoring.SetLoopInSamples(m_EndsAtTheEnd, 0, 176400, 44100);
            SongAuthoring.SetLoopInSamples(m_LeavesASecond, 0, 132300, 44100);

            ScriptableObject[] songs = { m_EndsAtTheEnd, m_LeavesASecond };

            m_OffTheWeb = AudioBuildCheck.FindProblems(songs, false);
            m_ForTheWeb = AudioBuildCheck.FindProblems(songs, true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_EndsAtTheEnd);
            Object.DestroyImmediate(m_LeavesASecond);
            Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenOffTheWebBothCanLoop()
        {
            Assert.AreEqual(0, m_OffTheWeb.Count);
        }

        [Test]
        public void ThenForTheWebOnlyTheSongLoopingToItsEndIsRefused()
        {
            Assert.AreEqual(1, m_ForTheWeb.Count);
        }

        [Test]
        public void ThenItsProblemSaysWhy()
        {
            StringAssert.Contains("before the end of stem [1]", m_ForTheWeb[0]);
        }
    }
}
