namespace RPGFramework.Audio
{
    /// <summary>
    /// What a stem generator's realtime part tells its control part after each block, through the pipe, and the message
    /// a player sends the generator to read the latest.
    /// </summary>
    internal struct StemReport
    {
        internal StemReport(double position, int passes, bool ended)
        {
            Position = position;
            Passes   = passes;
            Ended    = ended;
        }

        /// <summary>Where in its clip the stem is, in the content's seconds.</summary>
        internal double Position { get; }

        internal int Passes { get; }

        /// <summary>
        /// Whether a stem that does not loop has run out. Its source still says it is playing — the beta does not stop
        /// a source when its generator finishes — so the player stops it.
        /// </summary>
        internal bool Ended { get; }
    }
}
