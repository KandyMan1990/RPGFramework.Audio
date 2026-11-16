using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    /// <summary>
    /// A channel's stem played through a generator of our own, set as its source's generator, which nests the clip's own
    /// generator: an <see cref="AudioSource" /> playing a clip exposes no handle a <see cref="SeekMessage" /> can reach,
    /// while one playing a generator of ours does. A source playing a generator gives no position, so the generator keeps
    /// the stem's place (<see cref="Report" />). A component, as a source's generator must be a Unity object: its setter
    /// casts to one.
    /// </summary>
    internal sealed class StemGenerator : MonoBehaviour, IStemGenerator
    {
        private AudioSource m_Source;
        private AudioClip   m_Clip;
        private double      m_Start;
        private bool        m_Loops;
        private double      m_LoopStart;
        private double      m_LoopEnd;

        /// <summary>A generator on <paramref name="source" />'s object, set as its generator.</summary>
        internal static StemGenerator AddTo(AudioSource source)
        {
            StemGenerator generator = source.gameObject.AddComponent<StemGenerator>();

            generator.m_Source = source;
            source.generator   = generator;

            return generator;
        }

        /// <summary>What the source's next <see cref="AudioSource.Play()" /> plays, in the clip's seconds.</summary>
        internal void Set(AudioClip clip, double start, bool loops, double loopStart, double loopEnd)
        {
            m_Clip      = clip;
            m_Start     = start;
            m_Loops     = loops;
            m_LoopStart = loopStart;
            m_LoopEnd   = loopEnd;
        }

        /// <summary>Where the stem playing is, from its realtime part's latest report; empty when nothing plays.</summary>
        internal StemReport Report
        {
            get
            {
                StemReport report = default;

                if (ControlContext.builtIn.Exists(m_Source.generatorInstance))
                {
                    ControlContext.builtIn.SendMessage(m_Source.generatorInstance, ref report);
                }

                return report;
            }
        }

        AudioClip IStemGenerator.Clip => m_Clip;

        bool GeneratorInstance.ICapabilities.isFinite => !m_Loops;

        bool GeneratorInstance.ICapabilities.isRealtime => false;

        // Between stems it holds no clip, and has no length to give.
        DiscreteTime? GeneratorInstance.ICapabilities.length => m_Loops || m_Clip == null ? null : ((GeneratorInstance.ICapabilities)m_Clip).length;

        GeneratorInstance IAudioGenerator.CreateInstance(ControlContext context, AudioFormat? nestedFormat, GeneratorInstance.CreationParameters creationParameters)
        {
            // Nested in ours, in the system's format when ours is the source's own: a clip's generator made with no format is
            // a root, which nothing updates, so what its seeks leave behind piles up until it overflows.
            AudioFormat       childFormat = nestedFormat ?? new AudioFormat(AudioSettings.GetConfiguration());
            GeneratorInstance clip        = m_Clip.CreateInstance(context, childFormat, creationParameters);
            DiscreteTime      loopStart   = new DiscreteTime(m_LoopStart);
            DiscreteTime      loopEnd     = new DiscreteTime(m_LoopEnd);
            StemTimeline      timeline    = new StemTimeline(m_Start, (double)m_Clip.samples / m_Clip.frequency, m_Loops, m_LoopStart, m_LoopEnd);
            int               loopsQueued = m_Loops && m_Start < m_LoopEnd ? StemTimeline.LoopsToQueue(m_LoopEnd - m_LoopStart) : 0;

            if (m_Start > 0)
            {
                SeekMessage start = new SeekMessage(new DiscreteTime(m_Start), null);

                context.SendMessage(clip, ref start);
            }

            for (int i = 0; i < loopsQueued; i++)
            {
                SeekMessage loop = new SeekMessage(loopStart, loopEnd);

                context.SendMessage(clip, ref loop);
            }

            StemRealtime realtime = new StemRealtime(clip, ((GeneratorInstance.ICapabilities)this).length, timeline);
            StemControl  control  = new StemControl(clip, new StemReport(m_Start, 0, false), loopStart, loopEnd, loopsQueued);

            // Updated on every sweep, so it updates the clip's generator in turn, as a parent must.
            creationParameters.controlUpdateSetting = ProcessorInstance.UpdateSetting.UpdateAlways;

            return context.AllocateGenerator(realtime, control, nestedFormat, creationParameters);
        }
    }
}
