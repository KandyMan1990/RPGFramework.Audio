using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio.Music
{
    public class UnityMusicPlayer : IMusicPlayer, IUpdatable
    {
        private const string MUSIC_BUS_NAME    = "Music";
        private const string MUSIC_REVERB_SEND = "MusicReverbSend";

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
        private int                     m_PlayGeneration;
        private bool                    m_RegisteredForUpdate;

        public UnityMusicPlayer()
        {
            m_This = this;
        }

        Task IMusicPlayer.Play(int id, Dictionary<int, bool> initialStems, float fadeInTime)
        {
            if (m_CurrentSongId == id)
            {
                return Task.CompletedTask;
            }

            ClearCurrentSong();

            m_CurrentSongId = id;

            m_PlayGeneration++;

            float startTime = 0f;

            if (m_CurrentSongId == m_PausedSongId)
            {
                startTime = (float)m_PausedPosition;

                m_This.ClearPausedMusic();
            }

            Task scheduled = ScheduleCurrentSong(startTime, initialStems, fadeInTime, m_PlayGeneration);

            return scheduled;
        }

        void IMusicPlayer.Pause()
        {
            if (m_CurrentSongId < 0)
            {
                return;
            }

            m_PausedSongId   = m_CurrentSongId;
            m_PausedPosition = m_CurrentSources[0].time;

            m_PlayGeneration++;

            CancelCts();
            ClearCurrentSong();
        }

        Task IMusicPlayer.Stop(float fadeTime)
        {
            if (m_CurrentMusicAsset == null)
            {
                return Task.CompletedTask;
            }

            CancelCts();

            m_PlayGeneration++;

            Task stopping = FadeOutAndStopAsync(fadeTime, m_PlayGeneration);

            return stopping;
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

            GameObject musicPlayer = new GameObject("MusicPlayer");
            Object.DontDestroyOnLoad(musicPlayer);

            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                GameObject go = new GameObject(m_StemMixerGroups[i].name);
                go.transform.parent                       = musicPlayer.transform;
                m_CurrentSources[i]                       = go.AddComponent<AudioSource>();
                m_CurrentSources[i].outputAudioMixerGroup = m_StemMixerGroups[i];

                m_SendParameterNames[i] = $"{m_StemMixerGroups[i].name}_Send";
            }
        }

        void IMusicPlayer.SetActiveStemsImmediate(Dictionary<int, bool> stemValues)
        {
            CancelCts();

            SetStemLevels(stemValues);
            ApplyStemVolumes();
        }

        private void SetStemLevels(Dictionary<int, bool> stemValues)
        {
            foreach (KeyValuePair<int, bool> kvp in stemValues)
            {
                m_StemLevels[kvp.Key] = kvp.Value ? 1f : 0f;
            }
        }

        private void ApplyStemVolumes()
        {
            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                m_CurrentSources[i].volume = m_StemLevels[i] * m_MasterFade;
            }
        }

        async Task IMusicPlayer.SetActiveStemsFade(Dictionary<int, bool> stemValues, float transitionLength)
        {
            if (transitionLength <= 0f)
            {
                m_This.SetActiveStemsImmediate(stemValues);

                return;
            }

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

                foreach (KeyValuePair<int, bool> kvp in stemValues)
                {
                    float target = kvp.Value ? 1f : 0f;

                    m_StemLevels[kvp.Key] = math.lerp(m_FadeStartLevels[kvp.Key], target, progress);
                }

                ApplyStemVolumes();

                progress += Time.deltaTime / transitionLength;

                await Awaitable.NextFrameAsync(cts.Token);
            }

            m_This.SetActiveStemsImmediate(stemValues);
        }

        float IMusicPlayer.GetVolume()
        {
            return AudioUtils.GetVolume(m_AudioMixer, MUSIC_BUS_NAME);
        }

        void IMusicPlayer.SetVolume(float percent)
        {
            string[] busNames = new string[]
                                {
                                    MUSIC_BUS_NAME,
                                    MUSIC_REVERB_SEND
                                };

            AudioUtils.SetVolume(m_AudioMixer, busNames, percent);
        }

        void IUpdatable.Update()
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

        private async Task FadeOutAndStopAsync(float duration, int generation)
        {
            await FadeMasterAsync(0f, duration, generation);

            if (m_PlayGeneration != generation)
            {
                return;
            }

            ClearCurrentSong();
        }

        private async Task FadeMasterAsync(float target, float duration, int generation)
        {
            float t     = 0f;
            float start = m_MasterFade;

            while (t < 1f)
            {
                if (m_PlayGeneration != generation)
                {
                    return;
                }

                t += Time.deltaTime / duration;

                m_MasterFade = math.lerp(start, target, math.min(t, 1f));

                ApplyStemVolumes();

                await Awaitable.NextFrameAsync();
            }

            if (m_PlayGeneration == generation)
            {
                m_MasterFade = target;

                ApplyStemVolumes();
            }
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

        private async Task ScheduleCurrentSong(float startTime, Dictionary<int, bool> initialStems, float fadeInTime, int generation)
        {
            m_CurrentMusicAsset = m_MusicAssetProvider.GetMusicAsset(m_CurrentSongId);
            m_MasterFade        = fadeInTime > 0f ? 0f : 1f;

            for (int i = 0; i < m_StemLevels.Length; i++)
            {
                m_StemLevels[i] = 1f;
            }

            if (initialStems != null)
            {
                SetStemLevels(initialStems);
            }

            int    trackCount = m_CurrentMusicAsset.Tracks.Count;
            Task[] tasks      = new Task[trackCount];

            for (int i = 0; i < trackCount; i++)
            {
                IStem stem = m_CurrentMusicAsset.Tracks[i];
                tasks[i] = EnsureAudioClipLoaded(stem.Clip);
            }

            await Task.WhenAll(tasks);

            if (m_PlayGeneration != generation)
            {
                return;
            }

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
                await FadeMasterAsync(1f, fadeInTime, generation);
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

            m_RegisteredForUpdate = registered;

            if (registered)
            {
                UpdateManager.RegisterUpdatable(this);

                return;
            }

            UpdateManager.UnregisterUpdatable(this);
        }

        private void CancelCts()
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();
            m_CancellationTokenSource = null;
        }
    }
}