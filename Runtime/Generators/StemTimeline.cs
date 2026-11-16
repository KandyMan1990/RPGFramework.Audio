namespace RPGFramework.Audio
{
    /// <summary>
    /// Where in its clip a stem is, in the content's seconds, kept as the clip's generator plays: it adds what was played
    /// and wraps on crossing the loop's end, as the seek queued there does. A seek whose moment has already passed is
    /// dropped, so a stem started past its loop's end does not wrap, and ends with its clip.
    /// </summary>
    internal struct StemTimeline
    {
        // The longest the main thread may stall without a loop running dry, as each next seek is queued from it.
        private const double SECONDS_QUEUED = 2.0;

        private readonly double m_End;
        private readonly bool   m_Loops;
        private readonly double m_LoopStart;
        private readonly double m_LoopEnd;

        internal StemTimeline(double start, double end, bool loops, double loopStart, double loopEnd)
        {
            Position    = start;
            Passes      = 0;
            m_End       = end;
            m_Loops     = loops;
            m_LoopStart = loopStart;
            m_LoopEnd   = loopEnd;
        }

        internal double Position { get; private set; }

        /// <summary>How many times it has gone back from the loop's end to its start.</summary>
        internal int Passes { get; private set; }

        internal bool Ended => !WillWrap && Position >= m_End;

        private bool WillWrap => m_Loops && Position < m_LoopEnd;

        /// <summary>Whether the clip runs out within the next <paramref name="seconds" />, leaving silence after it.</summary>
        internal bool EndsWithin(double seconds)
        {
            return !WillWrap && Position + seconds >= m_End;
        }

        /// <summary>
        /// How many loop seeks to keep queued for a loop of <paramref name="loopLength" /> seconds: two at least, so one
        /// waits behind the one about to be taken, and enough to cover <see cref="SECONDS_QUEUED" />.
        /// </summary>
        internal static int LoopsToQueue(double loopLength)
        {
            return System.Math.Max(2, (int)System.Math.Ceiling(SECONDS_QUEUED / loopLength) + 1);
        }

        internal void Advance(double seconds)
        {
            bool wraps = WillWrap;

            Position += seconds;

            while (wraps && Position >= m_LoopEnd)
            {
                Position -= m_LoopEnd - m_LoopStart;
                Passes++;
            }
        }
    }
}
