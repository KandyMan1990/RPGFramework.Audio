using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Audio.Bundles
{
    internal sealed class WebStreamingBundleLoader : IStreamingBundleLoader
    {
        async Task<AssetBundle> IStreamingBundleLoader.LoadAsync(string path)
        {
            using UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(path);

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"{nameof(WebStreamingBundleLoader)}::{nameof(IStreamingBundleLoader.LoadAsync)} [{path}] could not be read: {request.error}. Is its song or sound in the provider's list, and were the bundles built since it was added?");
            }

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);

            if (bundle == null)
            {
                throw new IOException($"{nameof(WebStreamingBundleLoader)}::{nameof(IStreamingBundleLoader.LoadAsync)} [{path}] could not be opened as a bundle. Build the bundles again for this platform");
            }

            return bundle;
        }
    }
}