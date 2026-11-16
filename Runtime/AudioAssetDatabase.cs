using System.Collections.Generic;
using RPGFramework.Hashing;
using UnityEngine;

namespace RPGFramework.Audio
{
    /// <summary>
    /// An in-build provider's songs or sounds by the hash of each one's name, the same for both: what it lists is in the
    /// build already, so acquiring one is a lookup.
    /// </summary>
    internal sealed class AudioAssetDatabase<T> where T : Object
    {
        private readonly Dictionary<ulong, T> m_ByNameHash;
        private readonly string               m_Owner;
        private readonly string               m_Kind;

        /// <param name="assets">What the provider lists; an empty entry is skipped.</param>
        /// <param name="owner">The provider, as an error names it.</param>
        /// <param name="kind">What it holds, "song" or "sound", as an error names it.</param>
        internal AudioAssetDatabase(T[] assets, string owner, string kind)
        {
            m_ByNameHash = new Dictionary<ulong, T>(assets.Length);
            m_Owner      = owner;
            m_Kind       = kind;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != null)
                {
                    m_ByNameHash[Fnv1a64.Hash(assets[i].name)] = assets[i];
                }
            }
        }

        /// <summary>The asset whose name hashes to <paramref name="nameHash" />; throws for one it does not hold.</summary>
        internal T Get(ulong nameHash)
        {
            if (!m_ByNameHash.TryGetValue(nameHash, out T asset))
            {
                throw new KeyNotFoundException($"{m_Owner} has no {m_Kind} whose name hashes to [{nameHash}]");
            }

            return asset;
        }
    }
}
