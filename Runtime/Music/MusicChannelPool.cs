using System;
using System.Collections.Generic;
using System.Text;

namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// The music mixer groups, shared by every song sounding at once: one channel per stem, lowest free first.
    /// </summary>
    internal sealed class MusicChannelPool
    {
        private readonly string[] m_Holders;

        internal MusicChannelPool(int channelCount)
        {
            m_Holders = new string[channelCount];
        }

        /// <summary>
        /// Takes one channel per stem for a song. Music must play in full, so too few free channels throws, naming every
        /// song that holds one, and takes nothing.
        /// </summary>
        internal int[] Take(string songName, int stemCount)
        {
            int free = 0;

            foreach (string holder in m_Holders)
            {
                if (holder == null)
                {
                    free++;
                }
            }

            if (free < stemCount)
            {
                throw new InvalidOperationException(DescribeShortage(songName, stemCount));
            }

            int[] channels = new int[stemCount];
            int   taken    = 0;

            for (int i = 0; taken < stemCount; i++)
            {
                if (m_Holders[i] != null)
                {
                    continue;
                }

                m_Holders[i]      = songName;
                channels[taken++] = i;
            }

            return channels;
        }

        internal void Free(int[] channels)
        {
            foreach (int channel in channels)
            {
                m_Holders[channel] = null;
            }
        }

        private string DescribeShortage(string songName, int stemCount)
        {
            const string ADVICE = "Add music channels to the mixer and to the groups passed to SetStemMixerGroups";

            List<string> holders = new List<string>();
            List<int>    held    = new List<int>();

            foreach (string holder in m_Holders)
            {
                if (holder == null)
                {
                    continue;
                }

                int index = holders.IndexOf(holder);

                if (index < 0)
                {
                    holders.Add(holder);
                    held.Add(1);

                    continue;
                }

                held[index]++;
            }

            if (holders.Count == 0)
            {
                string alone = $"[{songName}] has {stemCount} stems and the mixer has {m_Holders.Length} music channels. {ADVICE}";

                return alone;
            }

            StringBuilder message = new StringBuilder("Crossfading from ");
            int           needed  = stemCount;

            for (int i = 0; i < holders.Count; i++)
            {
                message.Append(i == 0 ? "" : " and ").Append($"[{holders[i]}] ({held[i]} stems)");

                needed += held[i];
            }

            message.Append($" to [{songName}] ({stemCount} stems) needs {needed} music channels, and the mixer has {m_Holders.Length}. {ADVICE}");

            string crossfade = message.ToString();

            return crossfade;
        }
    }
}
