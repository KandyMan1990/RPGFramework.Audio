#if ENABLE_RPG_FRAMEWORK_DEBUG
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGFramework.Audio
{
    /// <summary>A sound effect provider as the debug room reads it: every sound it has.</summary>
    public interface IDebugSfxAssetProvider
    {
        /// <summary>The name of every sound the provider has.</summary>
        Task<IReadOnlyList<string>> LoadNamesAsync();
    }
}
#endif
