namespace RPGFramework.Audio.Bundles
{
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