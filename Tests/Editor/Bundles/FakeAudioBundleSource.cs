using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Bundles
{
    /// <summary>A bundle source holding one asset in memory, whose opens and closes finish when the test says.</summary>
    internal sealed class FakeAudioBundleSource : IAudioBundleSource
    {
        private readonly ScriptableObject m_Asset;

        private TaskCompletionSource<ScriptableObject> m_Opening;
        private TaskCompletionSource<bool>             m_Closing;

        internal FakeAudioBundleSource(ScriptableObject asset)
        {
            m_Asset = asset;
        }

        internal int   Opens      { get; private set; }
        internal int   Closes     { get; private set; }
        internal ulong LastOpened { get; private set; }

        internal void FinishOpening()
        {
            m_Opening.SetResult(m_Asset);
        }

        internal void FailOpening()
        {
            m_Opening.SetException(new IOException("No such bundle"));
        }

        internal void FinishClosing()
        {
            m_Closing.SetResult(true);
        }

        async Task<TAsset> IAudioBundleSource.LoadAsync<TAsset>(ulong nameHash)
        {
            Opens++;
            LastOpened = nameHash;
            m_Opening  = new TaskCompletionSource<ScriptableObject>();

            return (TAsset)await m_Opening.Task;
        }

        Task IAudioBundleSource.UnloadAsync(ulong nameHash)
        {
            Closes++;
            m_Closing = new TaskCompletionSource<bool>();

            return m_Closing.Task;
        }
    }
}
