using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio.Bundles;

namespace RPGFramework.Audio.Sfx
{
    /// <summary>
    /// Sounds from the bundles an sfx asset provider's Build bundles writes to StreamingAssets/Audio/Sfx, each opened
    /// when it is first played or preloaded and closed when nothing holds it.
    /// </summary>
    public sealed class BundledSfxAssetProvider : ISfxAssetProvider
#if ENABLE_RPG_FRAMEWORK_DEBUG
        , IDebugSfxAssetProvider
#endif
    {
        internal const string FOLDER = "Audio/Sfx";

        private readonly AudioBundleHolds<SfxAsset> m_Holds;

        public BundledSfxAssetProvider()
        {
            m_Holds = new AudioBundleHolds<SfxAsset>(new StreamingAudioBundleSource(FOLDER));
        }

        Task<SfxAsset> ISfxAssetProvider.AcquireAsync(ulong nameHash)
        {
            Task<SfxAsset> asset = m_Holds.AcquireAsync(nameHash);

            return asset;
        }

        void ISfxAssetProvider.Release(ulong nameHash)
        {
            m_Holds.Release(nameHash);
        }

#if ENABLE_RPG_FRAMEWORK_DEBUG
        Task<IReadOnlyList<string>> IDebugSfxAssetProvider.LoadNamesAsync()
        {
            return m_Holds.LoadNamesAsync(FOLDER);
        }
#endif
    }
}