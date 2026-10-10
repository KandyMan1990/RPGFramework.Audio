using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio.Bundles
{
    /// <summary>
    /// Bundles in a folder under StreamingAssets, each named by its song's or sound's name hash.
    /// </summary>
    internal sealed class StreamingAudioBundleSource : IAudioBundleSource
    {
        private readonly string                         m_Folder;
        private readonly IStreamingBundleLoader         m_Loader;
        private readonly Dictionary<ulong, AssetBundle> m_Open;

        internal StreamingAudioBundleSource(string folder)
        {
            m_Folder = folder;
            m_Loader = StreamingBundleLoaderProvider.Get();
            m_Open   = new Dictionary<ulong, AssetBundle>();
        }

        async Task<TAsset> IAudioBundleSource.LoadAsync<TAsset>(ulong nameHash)
        {
            string             path    = $"{Application.streamingAssetsPath}/{m_Folder}/{nameHash}";
            AssetBundle        bundle  = await m_Loader.LoadAsync(path);
            AssetBundleRequest request = bundle.LoadAllAssetsAsync<TAsset>();

            await request;

            if (request.allAssets.Length == 0)
            {
                bundle.Unload(true);

                throw new InvalidDataException($"{nameof(StreamingAudioBundleSource)}::{nameof(IAudioBundleSource.LoadAsync)} [{path}] holds no {typeof(TAsset).Name}");
            }

            m_Open[nameHash] = bundle;

            TAsset asset = (TAsset)request.allAssets[0];

            return asset;
        }

        async Task IAudioBundleSource.UnloadAsync(ulong nameHash)
        {
            if (!m_Open.Remove(nameHash, out AssetBundle bundle))
            {
                return;
            }

            await bundle.UnloadAsync(true);
        }
    }
}