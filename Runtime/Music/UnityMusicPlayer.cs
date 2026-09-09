using System;
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

        private static readonly string[] VOLUME_BUS_NAMES = { MUSIC_BUS_NAME, MUSIC_REVERB_SEND };

        private int    m_CurrentSongId  = -1;
        private int    m_PausedSongId   = -1;
        private double m_PausedPosition = 0.0;

        private readonly IMusicPlayer m_This;

        private IMusicAssetProvider     m_MusicAssetProvider;
        private IMusicAsset             m_CurrentMusicAsset;
        private AudioSource[]           m_CurrentSources;
        private AudioMixerGroup[]       m_StemMixerGroups;
        private AudioMixer              m_AudioMixer;
        private CancellationTokenSource m_CancellationTokenSource;
        private string[]                m_SendParameterNames;
        private float[]                 m_StemLevels;
        private float[]                 m_FadeStartLevels;
        private float                   m_MasterFade = 1f;
        private bool                    m_RegisteredForUpdate;
        private bool                    m_Disposed;
        private GameObject              m_PlayerObject;
        private AudioUpdateDriver       m_UpdateDriver;

        public UnityMusicPlayer()
        {
            m_This = this;
        }

        Task IMusicPlayer.PlayAsync(int id, int initialStemStateIndex, float fadeInTime)
        {
            if (m_CurrentSongId == id)
            {
                return Task.CompletedTask;
            }

            IMusicAsset musicAsset = m_MusicAssetProvider.GetMusicAsset(id);

            ClearCurrentSong();

            m_CurrentSongId     = id;
            m_CurrentMusicAsset = musicAsset;

            float startTime = 0f;

            if (m_CurrentSongId == m_PausedSongId)
            {
                startTime = (float)m_PausedPosition;

                m_This.ClearPausedMusic();
            }

            return ScheduleCurrentSong(startTime, initialStemStateIndex, fadeInTime);
        }

        void IMusicPlayer.Pause()
        {
            if (m_CurrentSongId < 0)
            {
                return;
            }

            m_PausedSongId   = m_CurrentSongId;
            m_PausedPosition = m_CurrentSources[0].time;

            CancelCts();
            ClearCurrentSong();
        }

        Task IMusicPlayer.StopAsync(float fadeTime)
        {
            if (m_CurrentMusicAsset == null)
            {
                return Task.CompletedTask;
            }

            CancelCts();

            return FadeOutAndStopAsync(fadeTime);
        }

        void IMusicPlayer.ClearPausedMusic()
        {
            m_PausedSongId   = -1;
            m_PausedPosition = 0.0;
        }

        void IMusicPlayer.SetMusicAssetProvider(IMusicAssetProvider provider)
        {
            m_MusicAssetProvider = provider;
        }

        void IMusicPlayer.SetStemMixerGroups(AudioMixerGroup[] groups)
        {
            m_StemMixerGroups = groups;
            m_AudioMixer      = m_StemMixerGroups[0].audioMixer;

            m_CurrentSources     = new AudioSource[m_StemMixerGroups.Length];
            m_SendParameterNames = new string[m_StemMixerGroups.Length];
            m_StemLevels         = new float[m_StemMixerGroups.Length];
            m_FadeStartLevels    = new float[m_StemMixerGroups.Length];

            DestroyPlayerObject();

            m_PlayerObject = new GameObject(MUSIC_GAME_OBJECT_NAME);
            UnityEngine.Object.DontDestroyOnLoad(m_PlayerObject);

            m_UpdateDriver         = AudioUpdateDriver.Attach(m_PlayerObject, this);
            m_UpdateDriver.enabled = m_RegisteredForUpdate;

            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                GameObject go = new GameObject(m_StemMixerGroups[i].name);
                go.transform.parent                       = m_PlayerObject.transform;
                m_CurrentSources[i]                       = go.AddComponent<AudioSource>();
                m_CurrentSources[i].outputAudioMixerGroup = m_StemMixerGroups[i];

                m_SendParameterNames[i] = $"{m_StemMixerGroups[i].name}_Send";
            }
        }

        Task IMusicPlayer.SetStemStateFadeAsync(int stemStateIndex, float transitionLength)
        {
            bool[] state = m_CurrentMusicAsset.GetStemsForState(stemStateIndex);

            return SetStemStateFadeAsync(state, transitionLength);
        }

        void IMusicPlayer.SetStemStateImmediate(int stemStateIndex)
        {
            bool[] state = m_CurrentMusicAsset.GetStemsForState(stemStateIndex);

            SetStemStateImmediate(state);
        }

        float IMusicPlayer.GetVolume()
        {
            return AudioUtils.GetVolume(m_AudioMixer, MUSIC_BUS_NAME);
        }

        void IMusicPlayer.SetVolume(float percent)
        {
            AudioUtils.SetVolume(m_AudioMixer, VOLUME_BUS_NAMES, percent);
        }

        void IAudioUpdatable.Update()
        {
            double currentTime = m_CurrentSources[0].time;

            if (currentTime >= m_CurrentMusicAsset.LoopEndTime)
            {
                double newTime = currentTime - (m_CurrentMusicAsset.LoopEndTime - m_CurrentMusicAsset.LoopStartTime);

                foreach (AudioSource source in m_CurrentSources)
                {
                    if (source.isPlaying)
                    {
                        source.time = (float)newTime;
                    }
                }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        private Task SetStemStateFadeAsync(bool[] state, float transitionLength)
        {
            if (transitionLength <= 0f)
            {
                SetStemStateImmediate(state);

                return Task.CompletedTask;
            }

            return FadeStemsAsync(state, transitionLength);
        }

        private void SetStemStateImmediate(bool[] state)
        {
            CancelCts();

            SetStemLevels(state);
            ApplyStemVolumes();
        }

        private void SetStemLevels(bool[] stemValues)
        {
            for (int i = 0; i < stemValues.Length; i++)
            {
                m_StemLevels[i] = stemValues[i] ? 1f : 0f;
            }
        }

        private void ApplyStemVolumes()
        {
            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                m_CurrentSources[i].volume = m_StemLevels[i] * m_MasterFade;
            }
        }

        private async Task FadeStemsAsync(bool[] stemValues, float transitionLength)
        {
            CancelCts();

            CancellationTokenSource cts = new CancellationTokenSource();

            m_CancellationTokenSource = cts;

            for (int i = 0; i < m_StemLevels.Length; i++)
            {
                m_FadeStartLevels[i] = m_StemLevels[i];
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

                    m_StemLevels[i] = math.lerp(m_FadeStartLevels[i], target, progress);
                }

                ApplyStemVolumes();

                progress += Time.deltaTime / transitionLength;

                await Awaitable.NextFrameAsync(cts.Token);
            }

            SetStemStateImmediate(stemValues);
        }

        private async Task FadeOutAndStopAsync(float duration)
        {
            await FadeMasterAsync(0f, duration);

            ClearCurrentSong();
        }

        private async Task FadeMasterAsync(float target, float duration)
        {
            float t     = 0f;
            float start = m_MasterFade;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;

                m_MasterFade = math.lerp(start, target, math.min(t, 1f));

                ApplyStemVolumes();

                await Awaitable.NextFrameAsync();
            }

            m_MasterFade = target;

            ApplyStemVolumes();
        }

        private static async Task EnsureAudioClipLoaded(AudioClip audioClip)
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

                await Awaitable.NextFrameAsync();
            }
        }

        private async Task ScheduleCurrentSong(float startTime, int initialStemStateIndex, float fadeInTime)
        {
            m_MasterFade = fadeInTime > 0f ? 0f : 1f;

            for (int i = 0; i < m_StemLevels.Length; i++)
            {
                m_StemLevels[i] = 1f;
            }

            bool[] state = m_CurrentMusicAsset.GetStemsForState(initialStemStateIndex);

            SetStemLevels(state);

            int    trackCount = m_CurrentMusicAsset.Tracks.Count;
            Task[] tasks      = new Task[trackCount];

            for (int i = 0; i < trackCount; i++)
            {
                IStem stem = m_CurrentMusicAsset.Tracks[i];
                tasks[i] = EnsureAudioClipLoaded(stem.Clip);
            }

            await Task.WhenAll(tasks);

            double scheduledStartTime = AudioSettings.dspTime + Time.deltaTime;

            for (int i = 0; i < trackCount; i++)
            {
                AudioSource source = m_CurrentSources[i];

                source.clip                  = m_CurrentMusicAsset.Tracks[i].Clip;
                source.playOnAwake           = false;
                source.loop                  = false;
                source.time                  = startTime;
                source.outputAudioMixerGroup = m_StemMixerGroups[i];

                float sendLevel = AudioUtils.PercentToDb(m_CurrentMusicAsset.Tracks[i].ReverbSendLevel);
                m_AudioMixer.SetFloat(m_SendParameterNames[i], sendLevel);

                source.PlayScheduled(scheduledStartTime);
            }

            ApplyStemVolumes();

            if (m_CurrentMusicAsset.Loop)
            {
                SetRegisteredForUpdate(true);
            }

            if (fadeInTime > 0f)
            {
                await FadeMasterAsync(1f, fadeInTime);
            }
        }

        private void ClearCurrentSong()
        {
            if (m_CurrentMusicAsset == null)
            {
                return;
            }

            foreach (AudioSource source in m_CurrentSources)
            {
                source.Stop();
                source.clip = null;
            }

            SetRegisteredForUpdate(false);

            foreach (IStem stem in m_CurrentMusicAsset.Tracks)
            {
                if (stem.Clip.preloadAudioData)
                {
                    continue;
                }

                stem.Clip.UnloadAudioData();
            }

            m_CurrentMusicAsset = null;
            m_CurrentSongId     = -1;
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
            ClearCurrentSong();
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
    }
}