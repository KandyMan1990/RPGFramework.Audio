using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio.Bundles
{
    /// <summary>
    /// Counts what holds each asset, so its bundle opens on the first hold and closes when the last lets go. An asset
    /// already held comes back as a finished task, so a caller awaiting it carries on in the same frame.
    /// </summary>
    internal sealed class AudioBundleHolds<TAsset> where TAsset : ScriptableObject, IAudioAsset
    {
        private readonly IAudioBundleSource      m_Source;
        private readonly Dictionary<ulong, Hold> m_Holds;

        internal AudioBundleHolds(IAudioBundleSource source)
        {
            m_Source = source;
            m_Holds  = new Dictionary<ulong, Hold>();
        }

        internal async Task<TAsset> AcquireAsync(ulong nameHash)
        {
            if (!m_Holds.TryGetValue(nameHash, out Hold hold))
            {
                hold = new Hold();
                m_Holds.Add(nameHash, hold);
            }

            hold.Count++;
            hold.Loading ??= LoadAsync(nameHash, hold);

            Task<TAsset> loading = hold.Loading;

            try
            {
                TAsset asset = await loading;

                return asset;
            }
            catch
            {
                hold.Count--;

                // So the next to ask tries again, rather than being handed the same failure.
                if (hold.Loading == loading)
                {
                    hold.Loading = null;
                }

                throw;
            }
        }

        internal void Release(ulong nameHash)
        {
            if (!m_Holds.TryGetValue(nameHash, out Hold hold) || hold.Count == 0)
            {
                return;
            }

            hold.Count--;

            if (hold.Count > 0)
            {
                return;
            }

            Task<TAsset> loading = hold.Loading;

            hold.Loading   = null;
            hold.Unloading = UnloadAsync(nameHash, loading);
        }

        private async Task<TAsset> LoadAsync(ulong nameHash, Hold hold)
        {
            // Asked for again while its bundle closes: let it close, then open it afresh.
            if (hold.Unloading != null)
            {
                await hold.Unloading;
            }

            TAsset asset = await m_Source.LoadAsync<TAsset>(nameHash);

            return asset;
        }

        private async Task UnloadAsync(ulong nameHash, Task<TAsset> loading)
        {
            try
            {
                await loading;
            }
            catch
            {
                return;
            }

            await m_Source.UnloadAsync(nameHash);
        }

        private sealed class Hold
        {
            internal int          Count;
            internal Task<TAsset> Loading;
            internal Task         Unloading;
        }
    }
}