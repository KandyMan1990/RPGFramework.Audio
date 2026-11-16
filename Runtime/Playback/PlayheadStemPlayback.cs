using UnityEngine;

namespace RPGFramework.Audio
{
    /// <summary>
    /// A stem on its source's own clip, for the web, which has no scriptable audio: scheduled on a <c>dspTime</c> so stems
    /// start together, and looped by <see cref="IStemPlayback.Tick" /> moving it back by the loop's length once it is
    /// past the end — seen up to a mixer buffer late, so a loop there needs audio after its end.
    /// </summary>
    internal sealed class PlayheadStemPlayback : IStemPlayback
    {
        private readonly AudioSource m_Source;

        private double m_Start;
        private double m_StartDspTime;
        private bool   m_Loops;
        private double m_LoopStart;
        private double m_LoopEnd;
        private bool   m_Paused;

        internal PlayheadStemPlayback(AudioSource source)
        {
            m_Source       = source;
            m_Start        = 0.0;
            m_StartDspTime = 0.0;
            m_Loops        = false;
            m_LoopStart    = 0.0;
            m_LoopEnd      = 0.0;
            m_Paused       = false;
        }

        AudioSource IStemPlayback.Source => m_Source;

        bool IStemPlayback.LoopsOnTick => true;

        // Where it was asked to start, until it has.
        double IStemPlayback.Position => AudioSettings.dspTime < m_StartDspTime ? m_Start : m_Source.time;

        // A paused source says it is not playing either.
        bool IStemPlayback.Ended => !m_Paused && !m_Source.isPlaying;

        void IStemPlayback.Play(AudioClip clip, double start, bool loops, double loopStart, double loopEnd, double dspTime)
        {
            m_Start        = start;
            m_StartDspTime = dspTime;
            m_Loops        = loops;
            m_LoopStart    = loopStart;
            m_LoopEnd      = loopEnd;
            m_Paused       = false;

            m_Source.clip        = clip;
            m_Source.playOnAwake = false;
            m_Source.loop        = false;
            m_Source.time        = (float)start;

            m_Source.PlayScheduled(dspTime);
        }

        void IStemPlayback.Tick()
        {
            if (!m_Loops || !m_Source.isPlaying)
            {
                return;
            }

            double time = m_Source.time;

            if (time >= m_LoopEnd)
            {
                m_Source.time = (float)(time - (m_LoopEnd - m_LoopStart));
            }
        }

        void IStemPlayback.Pause()
        {
            m_Paused = true;
            m_Source.Pause();
        }

        void IStemPlayback.UnPause()
        {
            m_Paused = false;
            m_Source.UnPause();
        }

        void IStemPlayback.Stop()
        {
            m_Paused = false;
            m_Source.Stop();
            m_Source.clip = null;
        }
    }
}
