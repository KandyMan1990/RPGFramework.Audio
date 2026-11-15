namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>What a stem's source file says of its length and its loop, counted in the file's own samples.</summary>
    internal sealed class StemMarkers
    {
        internal StemMarkers(int rate, long frames)
        {
            Rate   = rate;
            Frames = frames;
        }

        internal StemMarkers(int rate, long frames, long loopStart, long loopEnd, string source, string setAside)
        {
            Rate      = rate;
            Frames    = frames;
            HasLoop   = true;
            LoopStart = loopStart;
            LoopEnd   = loopEnd;
            Source    = source;
            SetAside  = setAside;
        }

        internal int Rate { get; }

        internal long Frames { get; }

        internal bool HasLoop { get; }

        internal long LoopStart { get; }

        /// <summary>The sample the loop jumps back at, one past the last it plays.</summary>
        internal long LoopEnd { get; }

        /// <summary>What in the file the loop was read from, for the inspector to say.</summary>
        internal string Source { get; }

        /// <summary>A loop the file also holds that gave way to <see cref="Source" />, or null.</summary>
        internal string SetAside { get; }

        internal double Seconds(long samples)
        {
            return (double)samples / Rate;
        }

        /// <summary>A file's loop, refused when it ends before it starts or past the file's end.</summary>
        internal static bool TryCreateLoop(int rate, long frames, long start, long end, string source, string setAside, out StemMarkers markers, out string problem)
        {
            markers = null;

            if (start < 0 || end <= start)
            {
                problem = $"its loop, from its {source}, runs from sample [{start}] to sample [{end}], and its end must come after its start";

                return false;
            }

            if (end > frames)
            {
                problem = $"its loop, from its {source}, ends at sample [{end}], past the file's end at [{frames}]";

                return false;
            }

            markers = new StemMarkers(rate, frames, start, end, source, setAside);
            problem = null;

            return true;
        }
    }
}
