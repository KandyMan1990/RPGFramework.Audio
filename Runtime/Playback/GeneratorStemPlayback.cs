using UnityEngine;

namespace RPGFramework.Audio
{
    /// <summary>
    /// A stem on a <see cref="StemGenerator" />, which loops at the sample, the clip's own generator seeking, so a loop
    /// may end at its file's very end. Its place and its end are the generator's report, as a source playing a generator
    /// gives neither.
    /// </summary>
    internal sealed class GeneratorStemPlayback : IStemPlayback
    {
        private readonly AudioSource   m_Source;
        private readonly StemGenerator m_Generator;

        internal GeneratorStemPlayback(AudioSource source)
        {
            m_Source    = source;
            m_Generator = StemGenerator.AddTo(source);
        }

        AudioSource IStemPlayback.Source => m_Source;

        bool IStemPlayback.LoopsOnTick => false;

        double IStemPlayback.Position => m_Generator.Report.Position;

        bool IStemPlayback.Ended => m_Generator.Report.Ended;

        // The frame's Play calls take effect together, so stems played in one frame start on the same sample with no time.
        void IStemPlayback.Play(AudioClip clip, double start, bool loops, double loopStart, double loopEnd, double dspTime)
        {
            m_Generator.Set(clip, start, loops, loopStart, loopEnd);
            m_Source.Play();
        }

        void IStemPlayback.Tick()
        {
        }

        void IStemPlayback.Pause()
        {
            m_Source.Pause();
        }

        void IStemPlayback.UnPause()
        {
            m_Source.UnPause();
        }

        void IStemPlayback.Stop()
        {
            m_Source.Stop();
            m_Generator.Set(null, 0.0, false, 0.0, 0.0);
        }
    }
}
