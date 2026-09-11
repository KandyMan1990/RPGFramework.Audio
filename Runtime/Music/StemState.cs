using System;
using UnityEngine;

namespace RPGFramework.Audio.Music
{
    [Serializable]
    internal sealed class StemState
    {
        internal const string DEFAULT_STATE_NAME = "Default";

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private bool[] m_ActiveStems;

        internal string Name        => m_Name;
        internal bool[] ActiveStems => m_ActiveStems;

        internal static StemState CreateAllStemsOn(int stemCount)
        {
            StemState state = new StemState
                              {
                                  m_Name           = DEFAULT_STATE_NAME,
                                  m_ActiveStems    = new bool[stemCount]
                              };

            for (int i = 0; i < stemCount; i++)
            {
                state.m_ActiveStems[i] = true;
            }

            return state;
        }

        internal void MatchStemCount(int stemCount)
        {
            if (m_ActiveStems != null && m_ActiveStems.Length == stemCount)
            {
                return;
            }

            bool[] resized = new bool[stemCount];

            int copyCount = 0;

            if (m_ActiveStems != null)
            {
                copyCount = Mathf.Min(m_ActiveStems.Length, stemCount);

                Array.Copy(m_ActiveStems, resized, copyCount);
            }

            for (int i = copyCount; i < stemCount; i++)
            {
                resized[i] = true;
            }

            m_ActiveStems = resized;
        }
    }
}