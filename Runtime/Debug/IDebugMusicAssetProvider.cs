#if ENABLE_RPG_FRAMEWORK_DEBUG
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGFramework.Audio
{
    /// <summary>A music asset provider as the debug room reads it: every song it has, and each song's stem states.</summary>
    public interface IDebugMusicAssetProvider
    {
        /// <summary>The name of every song the provider has.</summary>
        Task<IReadOnlyList<string>> LoadNamesAsync();

        /// <summary>The names of the song's stem states, in the order it lists them.</summary>
        Task<IReadOnlyList<string>> LoadStemStateNamesAsync(ulong nameHash);
    }
}
#endif
