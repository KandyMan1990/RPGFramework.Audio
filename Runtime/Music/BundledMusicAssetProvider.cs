using System.Threading.Tasks;
using RPGFramework.Audio.Bundles;

namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// Songs from the bundles a music asset provider's Build bundles writes to StreamingAssets/Audio/Music, each opened
    /// when it is first played or preloaded and closed when nothing holds it.
    /// </summary>
    public sealed class BundledMusicAssetProvider : IMusicAssetProvider
    {
        internal const string FOLDER = "Audio/Music";

        private readonly AudioBundleHolds<MusicAsset> m_Holds;

        public BundledMusicAssetProvider()
        {
            m_Holds = new AudioBundleHolds<MusicAsset>(new StreamingAudioBundleSource(FOLDER));
        }

        Task<MusicAsset> IMusicAssetProvider.AcquireAsync(ulong nameHash)
        {
            Task<MusicAsset> asset = m_Holds.AcquireAsync(nameHash);

            return asset;
        }

        void IMusicAssetProvider.Release(ulong nameHash)
        {
            m_Holds.Release(nameHash);
        }
    }
}