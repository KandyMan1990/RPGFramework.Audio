using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.Audio.Editor.Preview
{
    /// <summary>
    /// Plays an asset's stems in the editor as the game's players do: every stem scheduled to start on the same sample,
    /// and a loop taken by moving every stem back by the loop's length once the first passes its end. The players need
    /// play mode and the game's mixer, so the inspectors have this instead. Its sources skip the mixer, so a song's
    /// reverb is not heard.
    /// </summary>
    internal sealed class AudioStemPreview
    {
        private const double START_DELAY_SECONDS = 0.1;

        private GameObject      m_Object;
        private AudioSource[]   m_Sources;
        private List<AudioClip> m_LoadedHere;
        private int             m_StartSample;
        private int             m_LoopStart;
        private int             m_LoopEnd;
        private bool            m_Scheduled;
        private double          m_StartDspTime;

        /// <summary>
        /// Raised on every editor update while the stems play, with the first stem's position, before any loop is taken:
        /// the order the players check a sound's events in.
        /// </summary>
        internal event Action<int> Advanced;

        /// <summary>Raised each time the preview jumps back from the loop's end to its start.</summary>
        internal event Action Looped;

        /// <summary>Raised when the preview reaches the end of a stem that does not loop.</summary>
        internal event Action Ended;

        internal bool IsPlaying => m_Sources != null;

        /// <summary>Where the first stem is, in its samples: where it will start until it has.</summary>
        internal int Position
        {
            get
            {
                int position = m_Scheduled && AudioSettings.dspTime >= m_StartDspTime ? m_Sources[0].timeSamples : m_StartSample;

                return position;
            }
        }

        /// <param name="active">Which stems sound; the rest play silently, so they can be switched on in step.</param>
        /// <param name="loopEnd">In the first stem's samples, as <paramref name="loopStart" />; no later than the start for no loop.</param>
        internal void Play(IReadOnlyList<IStem> stems, bool[] active, int startSample, int loopStart, int loopEnd)
        {
            Stop();

            m_Object     = EditorUtility.CreateGameObjectWithHideFlags("Audio preview", HideFlags.HideAndDontSave);
            m_Sources    = new AudioSource[stems.Count];
            m_LoadedHere = new List<AudioClip>();

            for (int i = 0; i < stems.Count; i++)
            {
                AudioClip clip = stems[i].Clip;

                m_Sources[i]             = m_Object.AddComponent<AudioSource>();
                m_Sources[i].playOnAwake = false;
                m_Sources[i].loop        = false;
                m_Sources[i].clip        = clip;
                m_Sources[i].volume      = active[i] ? 1f : 0f;

                if (clip.loadState != AudioDataLoadState.Loaded && !m_LoadedHere.Contains(clip))
                {
                    clip.LoadAudioData();

                    m_LoadedHere.Add(clip);
                }
            }

            m_StartSample = startSample;
            m_LoopStart   = loopStart;
            m_LoopEnd     = loopEnd;
            m_Scheduled   = false;

            EditorApplication.update += Update;
        }

        internal void SetStemActive(int stem, bool active)
        {
            if (m_Sources == null || stem >= m_Sources.Length)
            {
                return;
            }

            m_Sources[stem].volume = active ? 1f : 0f;
        }

        internal void Stop()
        {
            EditorApplication.update -= Update;

            if (m_Object != null)
            {
                Object.DestroyImmediate(m_Object);
            }

            // What the preview loaded it unloads, as the players do when a song stops.
            for (int i = 0; m_LoadedHere != null && i < m_LoadedHere.Count; i++)
            {
                if (m_LoadedHere[i] != null && !m_LoadedHere[i].preloadAudioData)
                {
                    m_LoadedHere[i].UnloadAudioData();
                }
            }

            m_Object     = null;
            m_Sources    = null;
            m_LoadedHere = null;
        }

        private void Update()
        {
            if (!m_Scheduled)
            {
                ScheduleOnceLoaded();

                return;
            }

            if (AudioSettings.dspTime < m_StartDspTime)
            {
                return;
            }

            AudioSource first = m_Sources[0];

            if (!first.isPlaying)
            {
                Stop();
                Ended?.Invoke();

                return;
            }

            Advanced?.Invoke(first.timeSamples);

            if (m_LoopEnd <= m_LoopStart || first.timeSamples < m_LoopEnd)
            {
                return;
            }

            int position = first.timeSamples - (m_LoopEnd - m_LoopStart);

            for (int i = 0; i < m_Sources.Length; i++)
            {
                if (m_Sources[i].isPlaying)
                {
                    m_Sources[i].timeSamples = ToStemSamples(position, m_Sources[i].clip);
                }
            }

            Looped?.Invoke();
        }

        private void ScheduleOnceLoaded()
        {
            for (int i = 0; i < m_Sources.Length; i++)
            {
                AudioClip clip = m_Sources[i].clip;

                if (clip.loadState == AudioDataLoadState.Failed)
                {
                    Debug.LogError($"{nameof(AudioStemPreview)}::{nameof(ScheduleOnceLoaded)} Clip [{clip.name}] failed to load, so the preview cannot play");

                    Stop();
                    Ended?.Invoke();

                    return;
                }

                if (clip.loadState != AudioDataLoadState.Loaded)
                {
                    return;
                }
            }

            m_StartDspTime = AudioSettings.dspTime + START_DELAY_SECONDS;

            for (int i = 0; i < m_Sources.Length; i++)
            {
                AudioSource source = m_Sources[i];

                source.timeSamples = Mathf.Clamp(ToStemSamples(m_StartSample, source.clip), 0, source.clip.samples - 1);
                source.PlayScheduled(m_StartDspTime);
            }

            m_Scheduled = true;
        }

        /// <summary>A position in the first stem's samples, in another stem's, should their sample rates differ.</summary>
        private int ToStemSamples(int firstStemSamples, AudioClip clip)
        {
            int frequency = m_Sources[0].clip.frequency;
            int samples   = clip.frequency == frequency ? firstStemSamples : (int)((long)firstStemSamples * clip.frequency / frequency);

            return samples;
        }
    }
}
