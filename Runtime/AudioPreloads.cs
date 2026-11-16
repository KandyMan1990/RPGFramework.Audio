using System.Collections.Generic;

namespace RPGFramework.Audio
{
    /// <summary>
    /// The songs or sounds a player holds preloaded, by name hash, and what lets one still loading go when it is unloaded
    /// meanwhile: each preload is numbered, and only the latest for a name may hold what it loaded.
    /// </summary>
    internal sealed class AudioPreloads<T> where T : class
    {
        private readonly Dictionary<ulong, int> m_Requests;
        private readonly Dictionary<ulong, T>   m_Held;

        private int m_LastRequest;

        internal AudioPreloads()
        {
            m_Requests    = new Dictionary<ulong, int>();
            m_Held        = new Dictionary<ulong, T>();
            m_LastRequest = 0;
        }

        /// <summary>Every asset held, to ask whether one uses a clip.</summary>
        internal Dictionary<ulong, T>.ValueCollection Held => m_Held.Values;

        /// <summary>Every name preloaded or still preloading, for unloading them all.</summary>
        internal List<ulong> GetNames()
        {
            return new List<ulong>(m_Requests.Keys);
        }

        /// <summary>Starts a preload of <paramref name="nameHash" />, numbered; false when one is under way or held.</summary>
        internal bool TryBegin(ulong nameHash, out int request)
        {
            if (m_Requests.ContainsKey(nameHash))
            {
                request = 0;

                return false;
            }

            request = ++m_LastRequest;
            m_Requests.Add(nameHash, request);

            return true;
        }

        /// <summary>
        /// Holds what <paramref name="request" /> loaded; false when its name was unloaded since, and perhaps preloaded
        /// again by a later request that holds it instead, so the caller lets it go.
        /// </summary>
        internal bool TryHold(ulong nameHash, int request, T asset)
        {
            if (!m_Requests.TryGetValue(nameHash, out int current) || current != request)
            {
                return false;
            }

            m_Held.Add(nameHash, asset);

            return true;
        }

        /// <summary>Whether <paramref name="nameHash" /> is held, so its clips are still wanted.</summary>
        internal bool Holds(ulong nameHash)
        {
            return m_Held.ContainsKey(nameHash);
        }

        /// <summary>
        /// Ends <paramref name="nameHash" />'s preload: true with what it held, for the caller to unload and let go; false
        /// when there was none, or it is still loading, which lets go itself as it finishes.
        /// </summary>
        internal bool TryEnd(ulong nameHash, out T asset)
        {
            asset = null;

            return m_Requests.Remove(nameHash) && m_Held.Remove(nameHash, out asset);
        }
    }
}
