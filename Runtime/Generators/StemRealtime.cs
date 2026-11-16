using Unity.Burst;
using Unity.IntegerTime;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    /// <summary>
    /// A stem generator's realtime part, on the audio thread: it renders the clip's generator nested in it, keeps the
    /// stem's place, tells the control part where it is, and finishes a stem that does not loop when it runs out, leaving
    /// silence after it.
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    internal struct StemRealtime : GeneratorInstance.IRealtime
    {
        // Pitched up, a block takes more of the clip than it has frames, so the end is looked for this many blocks ahead.
        private const int END_BLOCKS_AHEAD = 4;

        private readonly GeneratorInstance m_Clip;
        private readonly DiscreteTime?     m_Length;
        private          StemTimeline      m_Timeline;
        private          int               m_SampleRate;

        internal StemRealtime(GeneratorInstance clip, DiscreteTime? length, StemTimeline timeline)
        {
            m_Clip       = clip;
            m_Length     = length;
            m_Timeline   = timeline;
            m_SampleRate = 0;
        }

        bool GeneratorInstance.ICapabilities.isFinite => m_Length.HasValue;

        bool GeneratorInstance.ICapabilities.isRealtime => false;

        DiscreteTime? GeneratorInstance.ICapabilities.length => m_Length;

        /// <summary>The rate the clip's generator renders at, which the control part learns as it is configured.</summary>
        internal void Configure(int sampleRate)
        {
            m_SampleRate = sampleRate;
        }

        void ProcessorInstance.IRealtime.Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
        {
        }

        ProcessorInstance.Response ProcessorInstance.IRealtime.OnMessage(RealtimeMessageContext context, ProcessorInstance.Pipe pipe, ProcessorInstance.Message message)
        {
            return ProcessorInstance.Response.Unhandled;
        }

        GeneratorInstance.Result GeneratorInstance.IRealtime.Process(in RealtimeContext context, ProcessorInstance.Pipe pipe, ChannelBuffer buffer, GeneratorInstance.Arguments args)
        {
            if (m_Timeline.EndsWithin((double)END_BLOCKS_AHEAD * buffer.frameCount / m_SampleRate))
            {
                buffer.Clear();
            }

            // The clip's processed frames are its own, the source's pitch already in them.
            GeneratorInstance.Result result = context.Process(m_Clip, buffer, args);

            m_Timeline.Advance((double)result.processedFrames / m_SampleRate);

            StemReport report = new StemReport(m_Timeline.Position, m_Timeline.Passes, m_Timeline.Ended);

            pipe.SendData(context, in report);

            return m_Timeline.Ended ? GeneratorInstance.Result.Finished(result.processedFrames) : result;
        }
    }
}
