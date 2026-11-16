#if ENABLE_RPG_FRAMEWORK_DEBUG
namespace RPGFramework.Audio
{
    /// <summary>A song or a sound as the debug room shows it: its name, where it is, how long it is, and its loop.</summary>
    public readonly struct DebugTrack
    {
        internal DebugTrack(string name, double position, double length, bool loops, double loopStart, double loopEnd)
        {
            Name      = name;
            Position  = position;
            Length    = length;
            Loops     = loops;
            LoopStart = loopStart;
            LoopEnd   = loopEnd;
        }

        public string Name { get; }

        /// <summary>Where it is, in seconds of its first stem.</summary>
        public double Position { get; }

        /// <summary>Its first stem's length, in seconds.</summary>
        public double Length { get; }

        public bool Loops { get; }

        /// <summary>Where its loop starts, in seconds; nothing when it does not loop.</summary>
        public double LoopStart { get; }

        /// <summary>Where its loop goes back to its start, in seconds; nothing when it does not loop.</summary>
        public double LoopEnd { get; }
    }
}
#endif
