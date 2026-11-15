using Unity.Scripting.LifecycleManagement;

namespace RPGFramework.Audio.Bundles
{
    [NoAutoStaticsCleanup]
    internal static class StreamingBundleLoaderProvider
    {
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
        private static readonly IStreamingBundleLoader m_Loader = new WebStreamingBundleLoader();
#else
        private static readonly IStreamingBundleLoader m_Loader = new FileStreamingBundleLoader();
#endif

        internal static IStreamingBundleLoader Get()
        {
            return m_Loader;
        }
    }
}
