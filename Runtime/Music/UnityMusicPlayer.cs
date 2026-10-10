using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Music
{
    public class UnityMusicPlayer : IMusicPlayer, IAudioUpdatable
    {
        private const string MUSIC_BUS_NAME          = "Music";
        private const string MUSIC_REVERB_SEND       = "MusicReverbSend";
        private const string MUSIC_ECHO              = "MusicEcho";
        private const string MUSIC_GAME_OBJECT_NAME  = "MusicPlayer";
        private const string REVERB_PRESET           = "ReverbPreset";
        private const string REVERB_DEPTH            = "ReverbDepth";
        private const string ECHO_DELAY              = "EchoDelay";
        private const string ECHO_DECAY              = "EchoDecay";
        private const float  ECHO_DELAY_TOLERANCE_MS = 0.5f;
        private const ulong  NO_MUSIC                = 0;

        private static readonly string[] VOLUME_BUS_NAMES = { MUSIC_BUS_NAME, MUSIC_REVERB_SEND, MUSIC_ECHO };

        private readonly IMusicPlayer                   m_This;
        private readonly List<Song>                     m_Sounding;
        private readonly Dictionary<ulong, int>         m_Preloads;
        private readonly Dictionary<ulong, IMusicAsset> m_Preloaded;

        private int                     m_Requests;
        private int                     m_PreloadRequests;
        private IMusicAssetProvider     m_MusicAssetProvider;
        private Song                    m_Playing;
        private MusicChannelPool        m_ChannelPool;
        private AudioSource[]           m_Sources;
        private AudioMixer              m_AudioMixer;
        private CancellationTokenSource m_CancellationTokenSource;
        private string[]                m_SendParameterNames;
        private string[]                m_EchoSendParameterNames;
        private bool                    m_RegisteredForUpdate;
        private bool                    m_Disposed;
        private GameObject              m_PlayerObject;
        private AudioUpdateDriver       m_UpdateDriver;

        public UnityMusicPlayer()
        {
            m_Sounding  = new List<Song>(2);
            m_Preloads  = new Dictionary<ulong, int>();
            m_Preloaded = new Dictionary<ulong, IMusicAsset>();
            m_This      = this;
        }

        async Task IMusicPlayer.PlayAsync(ulong nameHash, ulong initialStemStateHash, float fadeInTime, float volume)
        {
            if (IsCurrentSong(nameHash))
            {
                return;
            }

            IMusicAsset musicAsset = await AcquireAsync(nameHash);
            bool[]      stems      = musicAsset.GetStemsForState(initialStemStateHash);

            CancelCts();
            StopAllSongs();

            await StartSong(nameHash, musicAsset, stems, 0f, fadeInTime, volume, null);
        }

        async Task IMusicPlayer.CrossfadeAsync(ulong nameHash, ulong initialStemStateHash, float seconds, float volume)
        {
            if (IsCurrentSong(nameHash))
            {
                return;
            }

            IMusicAsset musicAsset = await AcquireAsync(nameHash);
            bool[]      stems      = musicAsset.GetStemsForState(initialStemStateHash);
            Song        outgoing   = m_Playing;

            CancelCts();
            StopSongsFadingOut();

            await StartSong(nameHash, musicAsset, stems, 0f, seconds, volume, outgoing);
        }

        async Task IMusicPlayer.ResumeAsync(MusicSnapshot snapshot, float seconds, float volume)
        {
            // Nothing was playing when it was taken, so nothing should be now.
            if (snapshot.NameHash == NO_MUSIC)
            {
                await m_This.StopAsync(seconds);

                return;
            }

            if (IsCurrentSong(snapshot.NameHash))
            {
                return;
            }

            IMusicAsset musicAsset = await AcquireAsync(snapshot.NameHash);
            Song        outgoing   = m_Playing;

            CancelCts();
            StopSongsFadingOut();

            await StartSong(snapshot.NameHash, musicAsset, snapshot.Stems, snapshot.Position, seconds, volume, outgoing);
        }

        Task IMusicPlayer.PreloadAsync(IReadOnlyList<ulong> nameHashes)
        {
            Task[] preloads = new Task[nameHashes.Count];

            for (int i = 0; i < nameHashes.Count; i++)
            {
                preloads[i] = PreloadAsync(nameHashes[i]);
            }

            return Task.WhenAll(preloads);
        }

        void IMusicPlayer.Unload(IReadOnlyList<ulong> nameHashes)
        {
            for (int i = 0; i < nameHashes.Count; i++)
            {
                ulong nameHash = nameHashes[i];

                if (!m_Preloads.Remove(nameHash))
                {
                    continue;
                }

                // Still loading, it lets go itself when it finishes.
                if (!m_Preloaded.Remove(nameHash, out IMusicAsset musicAsset))
                {
                    continue;
                }

                UnloadUnusedClips(musicAsset);
                m_MusicAssetProvider.Release(nameHash);
            }
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

        MusicSnapshot IMusicPlayer.Pause()
        {
            m_Requests++;

            MusicSnapshot snapshot = m_This.IsPlaying() ? TakeSnapshot(m_Playing) : default;

            CancelCts();
            StopAllSongs();

            return snapshot;
        }

        Task IMusicPlayer.StopAsync(float fadeTime)
        {
            m_Requests++;

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

        void IMusicPlayer.SetMusicAssetProvider(IMusicAssetProvider provider)
        {
            m_MusicAssetProvider = provider;
        }

        void IMusicPlayer.SetStemMixerGroups(AudioMixerGroup[] groups)
        {
            CancelCts();
            StopAllSongs();

            m_AudioMixer             = groups[0].audioMixer;
            m_ChannelPool            = new MusicChannelPool(groups.Length);
            m_Sources                = new AudioSource[groups.Length];
            m_SendParameterNames     = new string[groups.Length];
            m_EchoSendParameterNames = new string[groups.Length];

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

                m_SendParameterNames[i]     = $"{groups[i].name}_Send";
                m_EchoSendParameterNames[i] = $"{groups[i].name}_EchoSend";
            }
        }

        Task IMusicPlayer.SetStemStateFadeAsync(ulong stemStateHash, float transitionLength)
        {
            if (m_Playing == null)
            {
                return Task.CompletedTask;
            }

            bool[] state = m_Playing.Asset.GetStemsForState(stemStateHash);

            return SetStemStateFadeAsync(m_Playing, state, transitionLength);
        }

        void IMusicPlayer.SetStemStateImmediate(ulong stemStateHash)
        {
            if (m_Playing == null)
            {
                return;
            }

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
            for (int i = 0; i < m_Sounding.Count; i++)
            {
                Song song = m_Sounding[i];

                if (ChecksLoop(song))
                {
                    LoopIfPastEnd(song);
                }
            }

            if (FollowsTempo(m_Playing))
            {
                ApplyEchoDelay(m_Playing, m_Sources[m_Playing.Channels[0]].time);
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        // A song still loading has not moved from where it was asked to start. The stems are copied, so the snapshot
        // stays as it was whatever the asset does next.
        private MusicSnapshot TakeSnapshot(Song song)
        {
            float position = song.Scheduled ? m_Sources[song.Channels[0]].time : song.StartTime;

            return new MusicSnapshot(song.NameHash, position, (bool[])song.Stems.Clone());
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

            for (int i = 0; i < song.Channels.Length; i++)
            {
                int channel = song.Channels[i];

                AudioSource source = m_Sources[channel];

                if (source.isPlaying)
                {
                    source.time = (float)newTime;
                }
            }
        }

        private Task SetStemStateFadeAsync(Song song, bool[] state, float transitionLength)
        {
            song.Stems = state;

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

            song.Stems = state;

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

        private static async Task EnsureAudioClipLoaded(AudioClip audioClip, Func<bool> abandoned)
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
                if (abandoned())
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }
        }

        private Task StartSong(ulong nameHash, IMusicAsset musicAsset, bool[] stems, float startTime, float fadeSeconds, float volume, Song outgoing)
        {
            int[] channels = m_ChannelPool.Take(musicAsset.Name, musicAsset.Tracks.Count);
            Song  song     = new Song(nameHash, musicAsset, channels);

            song.Volume    = math.clamp(volume, 0f, 1f);
            song.StartTime = startTime;
            song.Stems     = stems;

            SetStemLevels(song, stems);

            m_Sounding.Add(song);
            m_Playing = song;

            return PlaySongAsync(song, fadeSeconds, outgoing);
        }

        private async Task PlaySongAsync(Song song, float fadeSeconds, Song outgoing)
        {
            bool fadesIn   = fadeSeconds > 0f;
            bool scheduled = await ScheduleSongAsync(song, fadesIn);

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
        private async Task<bool> ScheduleSongAsync(Song song, bool fadesIn)
        {
            IReadOnlyList<IStem> tracks = song.Asset.Tracks;
            Task[]               loads  = new Task[tracks.Count];

            for (int i = 0; i < tracks.Count; i++)
            {
                loads[i] = EnsureAudioClipLoaded(tracks[i].Clip, () => song.Stopped);
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
                source.time        = song.StartTime;

                float sendLevel = AudioUtils.PercentToDb(tracks[i].ReverbSendLevel);
                m_AudioMixer.SetFloat(m_SendParameterNames[channel], sendLevel);

                float echoSendLevel = AudioUtils.PercentToDb(tracks[i].EchoSendLevel);
                m_AudioMixer.SetFloat(m_EchoSendParameterNames[channel], echoSendLevel);

                source.PlayScheduled(scheduledStartTime);
            }

            song.Scheduled  = true;
            song.MasterFade = fadesIn ? 0f : song.Volume;

            ApplyStemVolumes(song);
            ApplySongReverb(song.Asset.Reverb);
            ApplySongEcho(song);
            UpdateTickRegistration();

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

        private void ApplySongEcho(Song song)
        {
            EchoSettings echo = song.Asset.Echo;

            if (echo.Timing == EchoTiming.None)
            {
                return;
            }

            AudioUtils.SetParameter(m_AudioMixer, ECHO_DECAY, echo.Decay);

            song.EchoDelay = -1f;
            ApplyEchoDelay(song, song.StartTime);
        }

        // A note length's delay changes with the tempo; written only when it has.
        private void ApplyEchoDelay(Song song, double seconds)
        {
            float delay = song.Asset.GetEchoDelayMilliseconds(seconds);

            if (Math.Abs(delay - song.EchoDelay) < ECHO_DELAY_TOLERANCE_MS)
            {
                return;
            }

            song.EchoDelay = delay;
            AudioUtils.SetParameter(m_AudioMixer, ECHO_DELAY, delay);
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

            for (int i = 0; i < song.Channels.Length; i++)
            {
                int channel = song.Channels[i];

                m_Sources[channel].Stop();
                m_Sources[channel].clip = null;
            }

            m_ChannelPool.Free(song.Channels);

            UnloadUnusedClips(song.Asset);
            m_MusicAssetProvider.Release(song.NameHash);

            UpdateTickRegistration();
        }

        // A song's clips can still be wanted after it stops: by the same song, preloaded, or, from the in-build provider,
        // by another song using the same clip.
        private bool IsClipSounding(AudioClip clip)
        {
            for (int i = 0; i < m_Sounding.Count; i++)
            {
                if (AudioUtils.UsesClip(m_Sounding[i].Asset, clip))
                {
                    return true;
                }
            }

            foreach (IMusicAsset preloaded in m_Preloaded.Values)
            {
                if (AudioUtils.UsesClip(preloaded, clip))
                {
                    return true;
                }
            }

            return false;
        }

        /// <returns>The song, held, or null when something asked for later has taken over, which lets it go again.</returns>
        private async Task<IMusicAsset> AcquireAsync(ulong nameHash)
        {
            int         request    = ++m_Requests;
            IMusicAsset musicAsset = await m_MusicAssetProvider.AcquireAsync(nameHash);

            if (request != m_Requests)
            {
                m_MusicAssetProvider.Release(nameHash);
                musicAsset = null;
            }

            return musicAsset;
        }

        private async Task PreloadAsync(ulong nameHash)
        {
            if (m_Preloads.ContainsKey(nameHash))
            {
                return;
            }

            int request = ++m_PreloadRequests;
            m_Preloads.Add(nameHash, request);

            IMusicAsset musicAsset = await m_MusicAssetProvider.AcquireAsync(nameHash);

            // Unloaded while it loaded, and perhaps preloaded again since, by a request that keeps it instead.
            if (!m_Preloads.TryGetValue(nameHash, out int current) || current != request)
            {
                m_MusicAssetProvider.Release(nameHash);

                return;
            }

            m_Preloaded.Add(nameHash, musicAsset);

            IReadOnlyList<IStem> tracks = musicAsset.Tracks;
            Task[]               loads  = new Task[tracks.Count];

            for (int i = 0; i < tracks.Count; i++)
            {
                loads[i] = EnsureAudioClipLoaded(tracks[i].Clip, () => !m_Preloaded.ContainsKey(nameHash));
            }

            await Task.WhenAll(loads);
        }

        private void UnloadUnusedClips(IMusicAsset musicAsset)
        {
            for (int i = 0; i < musicAsset.Tracks.Count; i++)
            {
                IStem stem = musicAsset.Tracks[i];

                if (stem.Clip.preloadAudioData || IsClipSounding(stem.Clip))
                {
                    continue;
                }

                stem.Clip.UnloadAudioData();
            }
        }

        private static bool ChecksLoop(Song song)
        {
            return song.Scheduled && song.Asset.Loop;
        }

        // The song playing owns the echo; one fading out under it no longer moves it.
        private bool FollowsTempo(Song song)
        {
            return song != null && song == m_Playing && song.Scheduled && !song.Stopped && song.Asset.EchoFollowsTempo;
        }

        private void UpdateTickRegistration()
        {
            bool registered = FollowsTempo(m_Playing);

            for (int i = 0; i < m_Sounding.Count; i++)
            {
                Song song = m_Sounding[i];

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
            m_This.Unload(new List<ulong>(m_Preloads.Keys));
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

            // The state its stems were last sent to, for a snapshot: a stem fade under way counts as finished.
            internal bool[] Stems;
            internal float  StartTime;

            // Where the song's fades take it, and where they have got to, each a gain on every stem.
            internal float Volume;
            internal float MasterFade;
            internal bool  Scheduled;
            internal bool  Stopped;

            // The echo delay last written for it, so a section that does not change it costs no write.
            internal float EchoDelay;

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