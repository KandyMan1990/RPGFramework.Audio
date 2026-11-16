using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.Audio.Editor.Preview
{
    /// <summary>
    /// Plays an asset's stems in the editor as the game's players do: each on an <see cref="IStemPlayback" />, every stem
    /// started together once its clip has loaded, a loop taken on the sample by a generator or, on the web, by moving the
    /// stem back once past the loop's end. The players need play mode and the game's mixer, so the inspectors have this
    /// instead. Its sources skip the mixer, so a song's reverb is not heard.
    /// </summary>
    internal sealed class AudioStemPreview
    {
        private const double START_DELAY_SECONDS = 0.1;

        private GameObject      m_Object;
        private IStemPlayback[] m_Stems;
        private AudioClip[]     m_Clips;
        private List<AudioClip> m_LoadedHere;
        private bool            m_Loops;
        private double          m_Start;
        private double          m_LoopStart;
        private double          m_LoopEnd;
        private bool            m_Started;
        private double          m_LastPosition;

        /// <summary>
        /// Raised on every editor update while the stems play, with the first stem's position in seconds — after the end
        /// of the pass, should it have gone back round its loop since the last — before the web's loop is taken: the order
        /// the players check a sound's events in.
        /// </summary>
        internal event Action<double> Advanced;

        /// <summary>Raised each time the preview has gone back from the loop's end to its start.</summary>
        internal event Action Looped;

        /// <summary>Raised when the preview reaches the end of a stem that does not loop.</summary>
        internal event Action Ended;

        internal bool IsPlaying => m_Stems != null;

        /// <summary>Where the first stem is, in seconds: where it will start until it has.</summary>
        internal double Position => m_Started ? m_Stems[0].Position : m_Start;

        /// <param name="stems">The asset's stems, in order.</param>
        /// <param name="active">Which stems sound; the rest play silently, so they can be switched on in step.</param>
        /// <param name="start">Where to start, in seconds.</param>
        /// <param name="loopStart">Where the loop starts, in seconds.</param>
        /// <param name="loopEnd">Where it ends, in seconds; no later than the start for no loop.</param>
        internal void Play(IReadOnlyList<IStem> stems, bool[] active, double start, double loopStart, double loopEnd)
        {
            Stop();

            m_Object     = EditorUtility.CreateGameObjectWithHideFlags("Audio preview", HideFlags.HideAndDontSave);
            m_Stems      = new IStemPlayback[stems.Count];
            m_Clips      = new AudioClip[stems.Count];
            m_LoadedHere = new List<AudioClip>();

            for (int i = 0; i < stems.Count; i++)
            {
                AudioClip   clip   = stems[i].Clip;
                AudioSource source = m_Object.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.volume      = active[i] ? 1f : 0f;

                m_Stems[i] = StemPlaybacks.Create(source);
                m_Clips[i] = clip;

                if (clip.loadState != AudioDataLoadState.Loaded && !m_LoadedHere.Contains(clip))
                {
                    clip.LoadAudioData();

                    m_LoadedHere.Add(clip);
                }
            }

            m_Loops     = loopEnd > loopStart;
            m_Start     = start;
            m_LoopStart = loopStart;
            m_LoopEnd   = loopEnd;
            m_Started   = false;

            EditorApplication.update += Update;
        }

        internal void SetStemActive(int stem, bool active)
        {
            if (m_Stems == null || stem >= m_Stems.Length)
            {
                return;
            }

            m_Stems[stem].Source.volume = active ? 1f : 0f;
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
            m_Stems      = null;
            m_Clips      = null;
            m_LoadedHere = null;
        }

        private void Update()
        {
            if (!m_Started)
            {
                StartOnceLoaded();

                return;
            }

            IStemPlayback first = m_Stems[0];

            if (first.Ended)
            {
                Stop();
                Ended?.Invoke();

                return;
            }

            double position = first.Position;

            if (m_Loops && position < m_LastPosition)
            {
                Advanced?.Invoke(m_LoopEnd);
                Looped?.Invoke();
            }

            m_LastPosition = position;

            Advanced?.Invoke(position);

            for (int i = 0; i < m_Stems.Length; i++)
            {
                m_Stems[i].Tick();
            }
        }

        private void StartOnceLoaded()
        {
            for (int i = 0; i < m_Clips.Length; i++)
            {
                AudioClip clip = m_Clips[i];

                if (clip.loadState == AudioDataLoadState.Failed)
                {
                    Debug.LogError($"{nameof(AudioStemPreview)}::{nameof(StartOnceLoaded)} Clip [{clip.name}] failed to load, so the preview cannot play");

                    Stop();
                    Ended?.Invoke();

                    return;
                }

                if (clip.loadState != AudioDataLoadState.Loaded)
                {
                    return;
                }
            }

            double startDspTime = AudioSettings.dspTime + START_DELAY_SECONDS;

            for (int i = 0; i < m_Stems.Length; i++)
            {
                m_Stems[i].Play(m_Clips[i], m_Start, m_Loops, m_LoopStart, m_LoopEnd, startDspTime);
            }

            m_LastPosition = m_Start;
            m_Started      = true;
        }
    }
}
