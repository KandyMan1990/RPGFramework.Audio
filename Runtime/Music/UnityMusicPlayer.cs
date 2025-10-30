using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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
        private int    m_CurrentSongId  = -1;
        private int    m_PausedSongId   = -1;
        private double m_PausedPosition = 0.0;

        private IMusicAssetProvider     m_MusicAssetProvider;
        private IMusicAsset             m_CurrentMusicAsset;
        private AudioSource[]           m_CurrentSources;
        private AudioMixerGroup[]       m_StemMixerGroups;
        private AudioMixer              m_AudioMixer;
        private CancellationTokenSource m_CancellationTokenSource;
        private string[]                m_SendParameterNames;

        Task IMusicPlayer.Play(int id)
        {
            if (m_CurrentSongId == id)
                return Task.CompletedTask;

            m_CurrentSongId = id;

            float startTime = 0f;

            if (m_CurrentSongId == m_PausedSongId)
            {
                startTime = (float)m_PausedPosition;

                ((IMusicPlayer)this).ClearPausedMusic();
            }

            return ScheduleCurrentSong(startTime);
        }

        void IMusicPlayer.Pause()
        {
            if (m_CurrentSongId < 0)
                return;

            m_PausedSongId   = m_CurrentSongId;
            m_PausedPosition = m_CurrentSources[0].time;

            CancelCts();
            ClearCurrentSong();
        }

        Task IMusicPlayer.Stop(float fadeTime)
        {
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

            foreach (KeyValuePair<int, bool> kvp in stemValues)
            {
                m_CurrentSources[kvp.Key].volume = kvp.Value ? 1f : 0f;
            }
        }

        void IMusicPlayer.SetActiveStemsFade(Dictionary<int, bool> stemValues, float transitionLength)
        {
            async Task Transition(CancellationToken token)
            {
                float progress = 0f;

                Dictionary<int, float> startVolumes = new Dictionary<int, float>();

                foreach (KeyValuePair<int, bool> kvp in stemValues)
                {
                    startVolumes[kvp.Key] = m_CurrentSources[kvp.Key].volume;
                }

                while (progress < 1.0f)
                {
                    if (token.IsCancellationRequested)
                        return;

                    foreach (KeyValuePair<int, bool> kvp in stemValues)
                    {
                        float start  = startVolumes[kvp.Key];
                        float target = kvp.Value ? 1f : 0f;

                        m_CurrentSources[kvp.Key].volume = math.lerp(start, target, progress);
                    }

                    progress += Time.deltaTime / transitionLength;

                    await Awaitable.NextFrameAsync(token);
                }

                ((IMusicPlayer)this).SetActiveStemsImmediate(stemValues);
            }

            if (transitionLength <= 0f)
            {
                ((IMusicPlayer)this).SetActiveStemsImmediate(stemValues);
            }

            CancelCts();
            m_CancellationTokenSource = new CancellationTokenSource();
            _                         = Transition(m_CancellationTokenSource.Token);
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

        private async Task FadeOutAndStopAsync(float duration)
        {
            float t           = 0f;
            float startVolume = m_CurrentSources[0].volume;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float newVolume = math.lerp(startVolume, 0f, t);

                foreach (AudioSource source in m_CurrentSources)
                {
                    source.volume = newVolume;
                }

                await Awaitable.NextFrameAsync();
            }

            ClearCurrentSong();
        }

        private static async Task EnsureAudioClipLoaded(AudioClip audioClip)
        {
            if (!audioClip.preloadAudioData && audioClip.loadState != AudioDataLoadState.Loaded)
            {
                audioClip.LoadAudioData();
                while (audioClip.loadState != AudioDataLoadState.Loaded)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
        }

        private async Task ScheduleCurrentSong(float startTime)
        {
            m_CurrentMusicAsset = m_MusicAssetProvider.GetMusicAsset(m_CurrentSongId);

            int trackCount = m_CurrentMusicAsset.Tracks.Count;

            ArrayPool<Task> pool  = ArrayPool<Task>.Shared;
            Task[]          tasks = pool.Rent(trackCount);

            try
            {
                for (int i = 0; i < trackCount; i++)
                {
                    IStem stem = m_CurrentMusicAsset.Tracks[i];
                    tasks[i] = EnsureAudioClipLoaded(stem.Clip);
                }

                await Task.WhenAll(tasks.Take(trackCount));

                double scheduledStartTime = AudioSettings.dspTime + Time.deltaTime;

                for (int i = 0; i < trackCount; i++)
                {
                    AudioSource source = m_CurrentSources[i];

                    source.clip                  = m_CurrentMusicAsset.Tracks[i].Clip;
                    source.playOnAwake           = false;
                    source.loop                  = false;
                    source.volume                = 1f;
                    source.time                  = startTime;
                    source.outputAudioMixerGroup = m_StemMixerGroups[i];

                    float sendLevel = math.lerp(-10f, 0f, m_CurrentMusicAsset.Tracks[i].ReverbSendLevel);
                    m_AudioMixer.SetFloat(m_SendParameterNames[i], sendLevel);

                    source.PlayScheduled(scheduledStartTime);
                }

                if (m_CurrentMusicAsset.Loop)
                {
                    UpdateManager.RegisterUpdatable(this);
                }
            }
            finally
            {
                pool.Return(tasks, true);
            }
        }

        private void ClearCurrentSong()
        {
            foreach (AudioSource source in m_CurrentSources)
            {
                source.Stop();
                source.clip = null;
            }

            if (m_CurrentMusicAsset.Loop)
            {
                UpdateManager.UnregisterUpdatable(this);
            }

            foreach (IStem stem in m_CurrentMusicAsset.Tracks)
            {
                stem.Clip.UnloadAudioData();
            }

            m_CurrentMusicAsset = null;
            m_CurrentSongId     = -1;
        }

        private void CancelCts()
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();
            m_CancellationTokenSource = null;
        }
    }
}