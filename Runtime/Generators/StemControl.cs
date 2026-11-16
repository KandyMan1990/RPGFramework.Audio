using Unity.IntegerTime;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    /// <summary>
    /// A stem generator's control part: it configures, updates and destroys the clip's generator nested in it, passes a
    /// <see cref="SeekMessage" /> on to it, keeps the realtime part's latest report and answers a player asking for it,
    /// and queues a loop seek for each pass reported, so as many wait as were queued at the start. The clip's generator
    /// refuses a seek from the audio thread, so they are queued here.
    /// </summary>
    internal struct StemControl : GeneratorInstance.IControl<StemRealtime>
    {
        private readonly GeneratorInstance m_Clip;
        private readonly DiscreteTime      m_LoopStart;
        private readonly DiscreteTime      m_LoopEnd;
        private readonly int               m_LoopsAhead;
        private          StemReport        m_Report;
        private          int               m_LoopsQueued;

        internal StemControl(GeneratorInstance clip, StemReport first, DiscreteTime loopStart, DiscreteTime loopEnd, int loopsQueued)
        {
            m_Clip        = clip;
            m_LoopStart   = loopStart;
            m_LoopEnd     = loopEnd;
            m_LoopsAhead  = loopsQueued;
            m_Report      = first;
            m_LoopsQueued = loopsQueued;
        }

        void GeneratorInstance.IControl<StemRealtime>.Configure(ControlContext context, ref StemRealtime realtime, in AudioFormat format, out GeneratorInstance.Setup setup, ref GeneratorInstance.Properties properties)
        {
            // The clip's generator is made in the format ours is, and a nested configure is refused outside a system-wide
            // change: configure it only then.
            if (context.IsSystemWideReconfiguring)
            {
                context.Configure(m_Clip, format);
            }

            setup = context.GetConfiguration(m_Clip).setup;
            realtime.Configure(setup.sampleRate);
        }

        void ProcessorInstance.IControl<StemRealtime>.Update(ControlContext context, ProcessorInstance.Pipe pipe)
        {
            context.Update(m_Clip);

            foreach (ProcessorInstance.AvailableData.Element element in pipe.GetAvailableData(context))
            {
                element.TryGetData(out m_Report);
            }

            for (; m_LoopsQueued < m_Report.Passes + m_LoopsAhead; m_LoopsQueued++)
            {
                SeekMessage loop = new SeekMessage(m_LoopStart, m_LoopEnd);

                context.SendMessage(m_Clip, ref loop);
            }
        }

        ProcessorInstance.Response ProcessorInstance.IControl<StemRealtime>.OnMessage(ControlContext context, ProcessorInstance.Pipe pipe, ProcessorInstance.Message message)
        {
            if (message.Is<StemReport>())
            {
                message.Get<StemReport>() = m_Report;

                return ProcessorInstance.Response.Handled;
            }

            if (message.Is<SeekMessage>())
            {
                return context.SendMessage(m_Clip, ref message.Get<SeekMessage>());
            }

            return ProcessorInstance.Response.Unhandled;
        }

        void ProcessorInstance.IControl<StemRealtime>.Dispose(ControlContext context, ref StemRealtime realtime)
        {
            context.Destroy(m_Clip);
        }
    }
}
