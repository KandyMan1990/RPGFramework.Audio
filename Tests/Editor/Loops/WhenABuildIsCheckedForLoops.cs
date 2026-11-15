using System.Collections.Generic;
using NUnit.Framework;
using RPGFramework.Audio.Editor;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Tests.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>
    /// Two songs on one four-second in-memory stem: one looping to its very end, which the player cannot catch, and one
    /// leaving a second after its loop.
    /// </summary>
    internal sealed class WhenABuildIsCheckedForLoops
    {
        private AudioClip    m_Clip;
        private MusicAsset   m_EndsAtTheEnd;
        private MusicAsset   m_LeavesASecond;
        private List<string> m_Problems;

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

            m_Problems = AudioBuildCheck.FindProblems(new ScriptableObject[] { m_EndsAtTheEnd, m_LeavesASecond });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_EndsAtTheEnd);
            Object.DestroyImmediate(m_LeavesASecond);
            Object.DestroyImmediate(m_Clip);
        }

        [Test]
        public void ThenOnlyTheSongLoopingToItsEndIsRefused()
        {
            Assert.AreEqual(1, m_Problems.Count);
        }

        [Test]
        public void ThenItsProblemSaysWhy()
        {
            StringAssert.Contains("before the end of stem [1]", m_Problems[0]);
        }
    }
}
