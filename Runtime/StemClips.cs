using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGFramework.Audio
{
    /// <summary>
    /// Loading and unloading a song's or a sound's stems' audio data, the same for both players: loaded before the stems
    /// play, so they start together, and unloaded once nothing wants it — only for clips imported with Preload Audio Data
    /// off, as one imported with it on stays resident.
    /// </summary>
    internal static class StemClips
    {
        /// <summary>
        /// Loads each stem's clip not loaded already and waits for them all, a clip that fails logged and its stem left
        /// silent, until <paramref name="abandoned" /> says what wanted them has gone: an unload under a clip loading would
        /// leave it waiting for ever.
        /// </summary>
        internal static async Task LoadAsync(IAudioAsset asset, Func<bool> abandoned)
        {
            IReadOnlyList<IStem> tracks = asset.Tracks;

            for (int i = 0; i < tracks.Count; i++)
            {
                AudioClip clip = tracks[i].Clip;

                if (!clip.preloadAudioData && clip.loadState != AudioDataLoadState.Loaded)
                {
                    clip.LoadAudioData();
                }
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                AudioClip clip = tracks[i].Clip;

                while (clip.loadState != AudioDataLoadState.Loaded)
                {
                    if (clip.loadState == AudioDataLoadState.Failed)
                    {
                        Debug.LogError($"{nameof(StemClips)}::{nameof(LoadAsync)} Clip [{clip.name}] failed to load. That stem will be silent");

                        break;
                    }

                    if (abandoned())
                    {
                        return;
                    }

                    await Awaitable.NextFrameAsync();
                }
            }
        }

        /// <summary>
        /// Unloads each stem's clip, unless it was imported to preload or <paramref name="inUse" /> says something still
        /// wants it.
        /// </summary>
        internal static void UnloadUnused(IAudioAsset asset, Func<AudioClip, bool> inUse)
        {
            IReadOnlyList<IStem> tracks = asset.Tracks;

            for (int i = 0; i < tracks.Count; i++)
            {
                AudioClip clip = tracks[i].Clip;

                if (clip.preloadAudioData || inUse(clip))
                {
                    continue;
                }

                clip.UnloadAudioData();
            }
        }
    }
}
