using System.Threading;
using RPGFramework.Audio.Bundles;
using RPGFramework.Audio.Music;
using UnityEngine;

namespace RPGFramework.Audio.Tests.Bundles
{
    /// <summary>
    /// A song's bundle holds, over a fake source. Until <see cref="Destroy" />, a task's continuations run as it
    /// finishes rather than on the editor's next update, so what an open or a close sets off has happened by the next
    /// line.
    /// </summary>
    internal sealed class SongBundles
    {
        internal const ulong SONG = 0x5EEDUL;

        private readonly SynchronizationContext m_EditorContext;

        internal SongBundles()
        {
            m_EditorContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);

            Song   = ScriptableObject.CreateInstance<MusicAsset>();
            Source = new FakeAudioBundleSource(Song);
            Holds  = new AudioBundleHolds<MusicAsset>(Source);
        }

        internal MusicAsset                   Song   { get; }
        internal FakeAudioBundleSource        Source { get; }
        internal AudioBundleHolds<MusicAsset> Holds  { get; }

        internal void Destroy()
        {
            SynchronizationContext.SetSynchronizationContext(m_EditorContext);
            Object.DestroyImmediate(Song);
        }
    }
}
