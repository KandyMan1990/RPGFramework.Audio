using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio.Bundles;

namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// Songs from the bundles a music asset provider's Build bundles writes to StreamingAssets/Audio/Music, each opened
    /// when it is first played or preloaded and closed when nothing holds it.
    /// </summary>
    public sealed class BundledMusicAssetProvider : IMusicAssetProvider
#if ENABLE_RPG_FRAMEWORK_DEBUG
        , IDebugMusicAssetProvider
#endif
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

#if ENABLE_RPG_FRAMEWORK_DEBUG
        Task<IReadOnlyList<string>> IDebugMusicAssetProvider.LoadNamesAsync()
        {
            return m_Holds.LoadNamesAsync(FOLDER);
        }

        async Task<IReadOnlyList<string>> IDebugMusicAssetProvider.LoadStemStateNamesAsync(ulong nameHash)
        {
            MusicAsset   song  = await m_Holds.AcquireAsync(nameHash);
            List<string> names = new List<string>(song.StemStateNames);

            m_Holds.Release(nameHash);

            return names;
        }
#endif
    }
}