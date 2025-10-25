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
        private int    m_CurrentSongId  = -1;
        private int    m_PausedSongId   = -1;
        private double m_PausedPosition = 0.0;

        private IMusicAssetProvider     m_MusicAssetProvider;
        private IMusicAsset             m_CurrentMusicAsset;
        private AudioSource[]           m_CurrentSources;
        private AudioMixerGroup[]       m_StemMixerGroups;
        private AudioMixer              m_AudioMixer;
        private CancellationTokenSource m_CancellationTokenSource;

        private const string SEND_SUFFIX = "_Send";
        private static readonly Dictionary<bool, int> m_TransitionMap = new Dictionary<bool, int>
                                                                        {
                                                                                { false, 0 },
                                                                                { true, 1 }
                                                                        };

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

            m_CancellationTokenSource?.Cancel();
            ClearCurrentSong();
        }

        Task IMusicPlayer.Stop(float fadeTime)
        {
            m_CancellationTokenSource?.Cancel();
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

            m_CurrentSources = new AudioSource[m_StemMixerGroups.Length];

            GameObject musicPlayer = new GameObject("MusicPlayer");
            Object.DontDestroyOnLoad(musicPlayer);

            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                GameObject go = new GameObject(m_StemMixerGroups[i].name);
                go.transform.parent                       = musicPlayer.transform;
                m_CurrentSources[i]                       = go.AddComponent<AudioSource>();
                m_CurrentSources[i].outputAudioMixerGroup = m_StemMixerGroups[i];
            }
        }

        void IMusicPlayer.SetActiveStemsImmediate(Dictionary<int, bool> stemValues)
        {
            m_CancellationTokenSource?.Cancel();

            foreach (KeyValuePair<int, bool> kvp in stemValues)
            {
                m_CurrentSources[kvp.Key].volume = m_TransitionMap[kvp.Value];
            }
        }

        void IMusicPlayer.SetActiveStemsFade(Dictionary<int, bool> stemValues, float transitionLength)
        {
            async Task Transition()
            {
                float progress = 0f;

                Dictionary<int, float> startVolumes = new Dictionary<int, float>();

                foreach (KeyValuePair<int, bool> kvp in stemValues)
                {
                    startVolumes[kvp.Key] = m_CurrentSources[kvp.Key].volume;
                }

                while (progress < 1.0f)
                {
                    if (m_CancellationTokenSource.IsCancellationRequested)
                        return;

                    foreach (KeyValuePair<int, bool> kvp in stemValues)
                    {
                        float start  = startVolumes[kvp.Key];
                        float target = m_TransitionMap[kvp.Value];

                        m_CurrentSources[kvp.Key].volume = math.lerp(start, target, progress);
                    }

                    progress += Time.deltaTime / transitionLength;

                    await Awaitable.NextFrameAsync();
                }

                ((IMusicPlayer)this).SetActiveStemsImmediate(stemValues);
            }

            m_CancellationTokenSource = new CancellationTokenSource();
            _                         = Transition();
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

            Task[] tasks = new Task[m_CurrentMusicAsset.Tracks.Count];
            for (int i = 0; i < m_CurrentMusicAsset.Tracks.Count; i++)
            {
                IStem stem = m_CurrentMusicAsset.Tracks[i];
                tasks[i] = EnsureAudioClipLoaded(stem.Clip);
            }

            await Task.WhenAll(tasks);

            double scheduledStartTime = AudioSettings.dspTime + Time.deltaTime;

            for (int i = 0; i < m_CurrentMusicAsset.Tracks.Count; i++)
            {
                AudioSource source = m_CurrentSources[i];

                source.clip                  = m_CurrentMusicAsset.Tracks[i].Clip;
                source.playOnAwake           = false;
                source.loop                  = false;
                source.volume                = 1f;
                source.time                  = startTime;
                source.outputAudioMixerGroup = m_StemMixerGroups[i];

                float sendLevel = math.lerp(-10f, 0f, m_CurrentMusicAsset.Tracks[i].ReverbSendLevel);
                m_AudioMixer.SetFloat($"{m_StemMixerGroups[i].name}{SEND_SUFFIX}", sendLevel);

                source.PlayScheduled(scheduledStartTime);
            }

            if (m_CurrentMusicAsset.Loop)
            {
                UpdateManager.RegisterUpdatable(this);
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
    }
}