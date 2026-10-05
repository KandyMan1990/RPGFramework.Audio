using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Music
{
    public class UnityMusicPlayer : IMusicPlayer, IAudioUpdatable, IDisposable
    {
        private const string MUSIC_BUS_NAME         = "Music";
        private const string MUSIC_REVERB_SEND      = "MusicReverbSend";
        private const string MUSIC_GAME_OBJECT_NAME = "MusicPlayer";
        private const string REVERB_PRESET          = "ReverbPreset";
        private const string REVERB_DEPTH           = "ReverbDepth";

        private static readonly string[] VOLUME_BUS_NAMES = { MUSIC_BUS_NAME, MUSIC_REVERB_SEND };

        private const ulong NO_MUSIC = 0;

        private ulong  m_PausedSongHash = NO_MUSIC;
        private double m_PausedPosition = 0.0;

        private readonly IMusicPlayer m_This;

        // The playing song, and while a crossfade or a stop lasts, the songs fading out under it.
        private readonly List<Song> m_Sounding = new List<Song>(2);

        private IMusicAssetProvider     m_MusicAssetProvider;
        private Song                    m_Playing;
        private MusicChannelPool        m_ChannelPool;
        private AudioSource[]           m_Sources;
        private AudioMixer              m_AudioMixer;
        private CancellationTokenSource m_CancellationTokenSource;
        private string[]                m_SendParameterNames;
        private bool                    m_RegisteredForUpdate;
        private bool                    m_Disposed;
        private GameObject              m_PlayerObject;
        private AudioUpdateDriver       m_UpdateDriver;

        public UnityMusicPlayer()
        {
            m_This = this;
        }

        Task IMusicPlayer.PlayAsync(ulong nameHash, ulong initialStemStateHash, float fadeInTime, float volume)
        {
            if (IsCurrentSong(nameHash))
            {
                return Task.CompletedTask;
            }

            IMusicAsset musicAsset = m_MusicAssetProvider.GetMusicAsset(nameHash);

            CancelCts();
            StopAllSongs();

            return StartSong(nameHash, musicAsset, initialStemStateHash, fadeInTime, volume, null);
        }

        Task IMusicPlayer.CrossfadeAsync(ulong nameHash, ulong initialStemStateHash, float seconds, float volume)
        {
            if (IsCurrentSong(nameHash))
            {
                return Task.CompletedTask;
            }

            IMusicAsset musicAsset = m_MusicAssetProvider.GetMusicAsset(nameHash);
            Song        outgoing   = m_Playing;

            CancelCts();
            StopSongsFadingOut();

            return StartSong(nameHash, musicAsset, initialStemStateHash, seconds, volume, outgoing);
        }

        Task IMusicPlayer.SetSongVolumeAsync(float volume, float seconds)
        {
            if (m_Playing == null)
            {
                return Task.CompletedTask;
            }

            m_Playing.Volume = math.clamp(volume, 0f, 1f);

            // Not started yet, it starts at the new volume, or fades in to it.
            if (!m_Playing.Scheduled)
            {
                return Task.CompletedTask;
            }

            return FadeMasterAsync(m_Playing, m_Playing.Volume, seconds);
        }

        void IMusicPlayer.Pause()
        {
            if (m_Playing != null)
            {
                m_PausedSongHash = m_Playing.NameHash;
                m_PausedPosition = m_Sources[m_Playing.Channels[0]].time;
            }

            CancelCts();
            StopAllSongs();
        }

        Task IMusicPlayer.StopAsync(float fadeTime)
        {
            if (m_Sounding.Count == 0)
            {
                return Task.CompletedTask;
            }

            CancelCts();

            m_Playing = null;

            Song[] songs = m_Sounding.ToArray();
            Task[] fades = new Task[songs.Length];

            for (int i = 0; i < songs.Length; i++)
            {
                fades[i] = FadeOutAndStopAsync(songs[i], fadeTime);
            }

            return Task.WhenAll(fades);
        }

        bool IMusicPlayer.IsPlaying()
        {
            bool playing = m_Playing != null && (!m_Playing.Scheduled || m_Sources[m_Playing.Channels[0]].isPlaying);

            return playing;
        }

        void IMusicPlayer.ClearPausedMusic()
        {
            m_PausedSongHash = NO_MUSIC;
            m_PausedPosition = 0.0;
        }

        void IMusicPlayer.SetMusicAssetProvider(IMusicAssetProvider provider)
        {
            m_MusicAssetProvider = provider;
        }

        void IMusicPlayer.SetStemMixerGroups(AudioMixerGroup[] groups)
        {
            CancelCts();
            StopAllSongs();

            m_AudioMixer         = groups[0].audioMixer;
            m_ChannelPool        = new MusicChannelPool(groups.Length);
            m_Sources            = new AudioSource[groups.Length];
            m_SendParameterNames = new string[groups.Length];

            DestroyPlayerObject();

            m_PlayerObject = new GameObject(MUSIC_GAME_OBJECT_NAME);
            UnityEngine.Object.DontDestroyOnLoad(m_PlayerObject);

            m_UpdateDriver         = AudioUpdateDriver.Attach(m_PlayerObject, this);
            m_UpdateDriver.enabled = m_RegisteredForUpdate;

            for (int i = 0; i < m_Sources.Length; i++)
            {
                GameObject go = new GameObject(groups[i].name);
                go.transform.parent                = m_PlayerObject.transform;
                m_Sources[i]                       = go.AddComponent<AudioSource>();
                m_Sources[i].outputAudioMixerGroup = groups[i];

                m_SendParameterNames[i] = $"{groups[i].name}_Send";
            }
        }

        Task IMusicPlayer.SetStemStateFadeAsync(ulong stemStateHash, float transitionLength)
        {
            bool[] state = m_Playing.Asset.GetStemsForState(stemStateHash);

            return SetStemStateFadeAsync(m_Playing, state, transitionLength);
        }

        void IMusicPlayer.SetStemStateImmediate(ulong stemStateHash)
        {
            bool[] state = m_Playing.Asset.GetStemsForState(stemStateHash);

            SetStemStateImmediate(m_Playing, state);
        }

        float IMusicPlayer.GetVolume()
        {
            return AudioUtils.GetVolume(m_AudioMixer, MUSIC_BUS_NAME);
        }

        void IMusicPlayer.SetVolume(float percent)
        {
            AudioUtils.SetVolume(m_AudioMixer, VOLUME_BUS_NAMES, percent);
        }

        void IMusicPlayer.SetReverbPreset(ReverbPreset preset)
        {
            SetReverbParameter(REVERB_PRESET, (float)preset);
        }

        void IMusicPlayer.SetReverbVolume(float volume)
        {
            SetReverbParameter(REVERB_DEPTH, AudioUtils.ReverbVolumeToDepth(volume));
        }

        void IAudioUpdatable.Update()
        {
            foreach (Song song in m_Sounding)
            {
                if (ChecksLoop(song))
                {
                    LoopIfPastEnd(song);
                }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        private bool IsCurrentSong(ulong nameHash)
        {
            return m_Playing != null && m_Playing.NameHash == nameHash;
        }

        private void LoopIfPastEnd(Song song)
        {
            double currentTime = m_Sources[song.Channels[0]].time;

            if (currentTime < song.Asset.LoopEndTime)
            {
                return;
            }

            double newTime = currentTime - (song.Asset.LoopEndTime - song.Asset.LoopStartTime);

            foreach (int channel in song.Channels)
            {
                AudioSource source = m_Sources[channel];

                if (source.isPlaying)
                {
                    source.time = (float)newTime;
                }
            }
        }

        private Task SetStemStateFadeAsync(Song song, bool[] state, float transitionLength)
        {
            if (transitionLength <= 0f)
            {
                SetStemStateImmediate(song, state);

                return Task.CompletedTask;
            }

            return FadeStemsAsync(song, state, transitionLength);
        }

        private void SetStemStateImmediate(Song song, bool[] state)
        {
            CancelCts();

            SetStemLevels(song, state);
            ApplyStemVolumes(song);
        }

        private static void SetStemLevels(Song song, bool[] stemValues)
        {
            for (int i = 0; i < stemValues.Length; i++)
            {
                song.StemLevels[i] = stemValues[i] ? 1f : 0f;
            }
        }

        private void ApplyStemVolumes(Song song)
        {
            for (int i = 0; i < song.Channels.Length; i++)
            {
                m_Sources[song.Channels[i]].volume = song.StemLevels[i] * song.MasterFade;
            }
        }

        private async Task FadeStemsAsync(Song song, bool[] stemValues, float transitionLength)
        {
            CancelCts();

            CancellationTokenSource cts = new CancellationTokenSource();

            m_CancellationTokenSource = cts;

            for (int i = 0; i < song.StemLevels.Length; i++)
            {
                song.FadeStartLevels[i] = song.StemLevels[i];
            }

            float progress = 0f;

            while (progress < 1f)
            {
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                for (int i = 0; i < stemValues.Length; i++)
                {
                    float target = stemValues[i] ? 1f : 0f;

                    song.StemLevels[i] = math.lerp(song.FadeStartLevels[i], target, progress);
                }

                ApplyStemVolumes(song);

                progress += Time.deltaTime / transitionLength;

                await Awaitable.NextFrameAsync(cts.Token);
            }

            SetStemStateImmediate(song, stemValues);
        }

        private async Task FadeOutAndStopAsync(Song song, float duration)
        {
            bool faded = await FadeMasterAsync(song, 0f, duration);

            if (faded)
            {
                StopSong(song);
            }
        }

        /// <returns>False when the song was stopped, or given a later fade, before this one finished.</returns>
        private async Task<bool> FadeMasterAsync(Song song, float target, float duration)
        {
            if (song.Stopped)
            {
                return false;
            }

            int   fadeId = ++song.MasterFadeId;
            float start  = song.MasterFade;
            float t      = duration > 0f ? 0f : 1f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;

                song.MasterFade = math.lerp(start, target, math.min(t, 1f));

                ApplyStemVolumes(song);

                await Awaitable.NextFrameAsync();

                // A stopped song's channels may already belong to another.
                if (song.Stopped || song.MasterFadeId != fadeId)
                {
                    return false;
                }
            }

            song.MasterFade = target;

            ApplyStemVolumes(song);

            return true;
        }

        private static async Task EnsureAudioClipLoaded(AudioClip audioClip, Song song)
        {
            if (audioClip.preloadAudioData || audioClip.loadState == AudioDataLoadState.Loaded)
            {
                return;
            }

            audioClip.LoadAudioData();

            while (audioClip.loadState != AudioDataLoadState.Loaded)
            {
                if (audioClip.loadState == AudioDataLoadState.Failed)
                {
                    Debug.LogError($"{nameof(UnityMusicPlayer)}::{nameof(EnsureAudioClipLoaded)} Clip [{audioClip.name}] failed to load. That stem will be silent");

                    return;
                }

                // Stopping the song unloads the clip, so it would never finish loading.
                if (song.Stopped)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }
        }

        private Task StartSong(ulong nameHash, IMusicAsset musicAsset, ulong initialStemStateHash, float fadeSeconds, float volume, Song outgoing)
        {
            bool[] state    = musicAsset.GetStemsForState(initialStemStateHash);
            int[]  channels = m_ChannelPool.Take(musicAsset.Name, musicAsset.Tracks.Count);
            Song   song     = new Song(nameHash, musicAsset, channels);

            song.Volume = math.clamp(volume, 0f, 1f);

            SetStemLevels(song, state);

            m_Sounding.Add(song);
            m_Playing = song;

            float startTime = 0f;

            if (nameHash == m_PausedSongHash)
            {
                startTime = (float)m_PausedPosition;

                m_This.ClearPausedMusic();
            }

            return PlaySongAsync(song, startTime, fadeSeconds, outgoing);
        }

        private async Task PlaySongAsync(Song song, float startTime, float fadeSeconds, Song outgoing)
        {
            bool fadesIn   = fadeSeconds > 0f;
            bool scheduled = await ScheduleSongAsync(song, startTime, fadesIn);

            if (!scheduled)
            {
                return;
            }

            // The song fading out keeps playing in full until this one can be heard.
            Task fadeIn  = fadesIn ? FadeMasterAsync(song, song.Volume, fadeSeconds) : Task.CompletedTask;
            Task fadeOut = outgoing != null ? FadeOutAndStopAsync(outgoing, fadeSeconds) : Task.CompletedTask;

            await Task.WhenAll(fadeIn, fadeOut);
        }

        /// <returns>False when another song took over while this one's clips loaded, so it never sounded.</returns>
        private async Task<bool> ScheduleSongAsync(Song song, float startTime, bool fadesIn)
        {
            IReadOnlyList<IStem> tracks = song.Asset.Tracks;
            Task[]               loads  = new Task[tracks.Count];

            for (int i = 0; i < tracks.Count; i++)
            {
                loads[i] = EnsureAudioClipLoaded(tracks[i].Clip, song);
            }

            await Task.WhenAll(loads);

            if (song != m_Playing)
            {
                StopSong(song);

                return false;
            }

            double scheduledStartTime = AudioSettings.dspTime + Time.deltaTime;

            for (int i = 0; i < tracks.Count; i++)
            {
                int         channel = song.Channels[i];
                AudioSource source  = m_Sources[channel];

                source.clip        = tracks[i].Clip;
                source.playOnAwake = false;
                source.loop        = false;
                source.time        = startTime;

                float sendLevel = AudioUtils.PercentToDb(tracks[i].ReverbSendLevel);
                m_AudioMixer.SetFloat(m_SendParameterNames[channel], sendLevel);

                source.PlayScheduled(scheduledStartTime);
            }

            song.Scheduled  = true;
            song.MasterFade = fadesIn ? 0f : song.Volume;

            ApplyStemVolumes(song);
            ApplySongReverb(song.Asset.Reverb);
            UpdateLoopRegistration();

            return true;
        }

        private void ApplySongReverb(ReverbSettings reverb)
        {
            if (reverb.SetsPreset)
            {
                m_This.SetReverbPreset(reverb.Preset);
            }

            if (reverb.SetsVolume)
            {
                m_This.SetReverbVolume(reverb.Volume);
            }
        }

        // The exposed parameters are PSX Reverb's, so a game without it has none to write.
        private void SetReverbParameter(string parameter, float value)
        {
#if RPGFRAMEWORK_PSXREVERB
            AudioUtils.SetParameter(m_AudioMixer, parameter, value);
#endif
        }

        private void StopAllSongs()
        {
            for (int i = m_Sounding.Count - 1; i >= 0; i--)
            {
                StopSong(m_Sounding[i]);
            }
        }

        // One song fades out at a time, so a crossfade asked for during another cuts the one already leaving.
        private void StopSongsFadingOut()
        {
            for (int i = m_Sounding.Count - 1; i >= 0; i--)
            {
                if (m_Sounding[i] != m_Playing)
                {
                    StopSong(m_Sounding[i]);
                }
            }
        }

        private void StopSong(Song song)
        {
            if (song.Stopped)
            {
                return;
            }

            song.Stopped = true;

            m_Sounding.Remove(song);

            if (m_Playing == song)
            {
                m_Playing = null;
            }

            foreach (int channel in song.Channels)
            {
                m_Sources[channel].Stop();
                m_Sources[channel].clip = null;
            }

            m_ChannelPool.Free(song.Channels);

            foreach (IStem stem in song.Asset.Tracks)
            {
                if (stem.Clip.preloadAudioData || IsClipSounding(stem.Clip))
                {
                    continue;
                }

                stem.Clip.UnloadAudioData();
            }

            UpdateLoopRegistration();
        }

        // Two songs can share clips, and one still sounding needs them loaded.
        private bool IsClipSounding(AudioClip clip)
        {
            foreach (Song song in m_Sounding)
            {
                foreach (IStem stem in song.Asset.Tracks)
                {
                    if (stem.Clip == clip)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ChecksLoop(Song song)
        {
            return song.Scheduled && song.Asset.Loop;
        }

        private void UpdateLoopRegistration()
        {
            bool registered = false;

            foreach (Song song in m_Sounding)
            {
                registered |= ChecksLoop(song);
            }

            SetRegisteredForUpdate(registered);
        }

        private void SetRegisteredForUpdate(bool registered)
        {
            if (registered == m_RegisteredForUpdate)
            {
                return;
            }

            m_RegisteredForUpdate  = registered;
            m_UpdateDriver.enabled = registered;
        }

        private void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;

            CancelCts();
            StopAllSongs();
            DestroyPlayerObject();
        }

        private void DestroyPlayerObject()
        {
            if (m_PlayerObject == null)
            {
                return;
            }

            m_UpdateDriver.enabled = false;

            UnityEngine.Object.Destroy(m_PlayerObject);

            m_PlayerObject = null;
            m_UpdateDriver = null;
        }

        private void CancelCts()
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();
            m_CancellationTokenSource = null;
        }

        /// <summary>
        /// A song sounding on its own channels: the one playing, or one fading out under it.
        /// </summary>
        private sealed class Song
        {
            internal readonly ulong       NameHash;
            internal readonly IMusicAsset Asset;
            internal readonly int[]       Channels;
            internal readonly float[]     StemLevels;
            internal readonly float[]     FadeStartLevels;

            // Where the song's fades take it, and where they have got to, each a gain on every stem.
            internal float Volume;
            internal float MasterFade;
            internal bool  Scheduled;
            internal bool  Stopped;

            // Each master fade takes the next id, so one still running knows a later one has taken over.
            internal int MasterFadeId;

            internal Song(ulong nameHash, IMusicAsset asset, int[] channels)
            {
                NameHash        = nameHash;
                Asset           = asset;
                Channels        = channels;
                StemLevels      = new float[channels.Length];
                FadeStartLevels = new float[channels.Length];
            }
        }
    }
}