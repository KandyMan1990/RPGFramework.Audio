using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio.Bundles
{
    internal sealed class FileStreamingBundleLoader : IStreamingBundleLoader
    {
        async Task<AssetBundle> IStreamingBundleLoader.LoadAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"{nameof(FileStreamingBundleLoader)}::{nameof(IStreamingBundleLoader.LoadAsync)} [{path}] does not exist. Is its song or sound in the provider's list, and were the bundles built since it was added?");
            }

            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);

            await request;

            AssetBundle bundle = request.assetBundle;

            if (bundle == null)
            {
                throw new IOException($"{nameof(FileStreamingBundleLoader)}::{nameof(IStreamingBundleLoader.LoadAsync)} [{path}] could not be opened as a bundle. Build the bundles again for this platform");
            }

            return bundle;
        }
    }
}