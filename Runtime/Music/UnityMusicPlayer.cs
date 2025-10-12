using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core.Shared;
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

        private readonly Transform m_Parent;

        private const string SEND_SUFFIX = "_Send";
        private static readonly Dictionary<bool, int> TRANSITION_MAP = new Dictionary<bool, int>
                                                                       {
                                                                               { false, 0 },
                                                                               { true, 1 }
                                                                       };

        public UnityMusicPlayer(Transform parent)
        {
            m_Parent = parent;
        }

        public void Play(int id)
        {
            if (m_CurrentSongId == id)
                return;

            m_CurrentSongId = id;

            float startTime = 0f;

            if (m_CurrentSongId == m_PausedSongId)
            {
                startTime = (float)m_PausedPosition;

                m_PausedSongId   = -1;
                m_PausedPosition = 0.0;
            }

            ScheduleCurrentSong(startTime);
        }

        public void Pause()
        {
            if (m_CurrentSongId < 0)
                return;

            m_PausedSongId   = m_CurrentSongId;
            m_PausedPosition = m_CurrentSources[0].time;

            m_CancellationTokenSource?.Cancel();
            ClearCurrentSong(true);
        }

        public void Stop(float fadeTime = 0.001f)
        {
            m_CancellationTokenSource?.Cancel();
            _ = FadeOutAndStopAsync(fadeTime);
        }

        public void SetMusicAssetProvider(IMusicAssetProvider provider)
        {
            m_MusicAssetProvider = provider;
        }

        public void Update()
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

        public void SetStemMixerGroups(AudioMixerGroup[] groups)
        {
            m_StemMixerGroups = groups;
            m_AudioMixer      = m_StemMixerGroups[0].audioMixer;

            m_CurrentSources = new AudioSource[m_StemMixerGroups.Length];

            GameObject musicParent = new GameObject("Music");
            musicParent.transform.parent = m_Parent;

            for (int i = 0; i < m_CurrentSources.Length; i++)
            {
                GameObject go = new GameObject(m_StemMixerGroups[i].name);
                go.transform.parent                       = musicParent.transform;
                m_CurrentSources[i]                       = go.AddComponent<AudioSource>();
                m_CurrentSources[i].outputAudioMixerGroup = m_StemMixerGroups[i];
            }
        }

        public void SetActiveStemsImmediate(Dictionary<int, bool> stemValues)
        {
            m_CancellationTokenSource?.Cancel();

            foreach (KeyValuePair<int, bool> kvp in stemValues)
            {
                m_CurrentSources[kvp.Key].volume = TRANSITION_MAP[kvp.Value];
            }
        }

        public void SetActiveStemsFade(Dictionary<int, bool> stemValues, float transitionLength)
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
                        float target = TRANSITION_MAP[kvp.Value];

                        m_CurrentSources[kvp.Key].volume = math.lerp(start, target, progress);
                    }

                    progress += Time.deltaTime / transitionLength;

                    await Awaitable.NextFrameAsync();
                }

                SetActiveStemsImmediate(stemValues);
            }

            m_CancellationTokenSource = new CancellationTokenSource();
            _                         = Transition();
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

            ClearCurrentSong(false);
        }

        private void ScheduleCurrentSong(float startTime)
        {
            m_CurrentMusicAsset = m_MusicAssetProvider.GetMusicAsset(m_CurrentSongId);

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

        private void ClearCurrentSong(bool pause)
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

            m_CurrentMusicAsset = null;
            m_CurrentSongId     = -1;
        }
    }
}