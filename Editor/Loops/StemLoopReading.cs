using System;
using System.Collections.Generic;
using System.Text;
using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>
    /// A song's loop read from its stems' source files: every stem the same length, and every stem with a loop the same
    /// loop, compared in seconds within half a sample of each one's rate, so stems at different rates agree when their
    /// markers are the same moment. A stem with no loop is skipped and named; the loop is counted in the first stem with
    /// one's samples.
    /// </summary>
    internal sealed class StemLoopReading
    {
        private StemLoopReading(string report, bool refused)
        {
            Report  = report;
            Refused = refused;
        }

        private StemLoopReading(string report, int loopStart, int loopEnd, int sampleRate)
        {
            Report     = report;
            Found      = true;
            LoopStart  = loopStart;
            LoopEnd    = loopEnd;
            SampleRate = sampleRate;
        }

        /// <summary>Whether the stems agree on a loop.</summary>
        internal bool Found { get; }

        /// <summary>Whether something stops the stems' loop being read: a file that cannot be read, or stems that disagree.</summary>
        internal bool Refused { get; }

        /// <summary>What was read and from which stems, or what stopped it, for the inspector to say.</summary>
        internal string Report { get; }

        internal int LoopStart { get; }

        internal int LoopEnd { get; }

        internal int SampleRate { get; }

        /// <summary>
        /// Whether a loop read as a stem is added is written to the song: when it has no loop yet, or loops by samples. A
        /// song looping by bars is offered the loop instead, as its bars were chosen.
        /// </summary>
        internal static bool AppliesWhenAdded(bool songLoops, LoopUnit unit)
        {
            return !songLoops || unit == LoopUnit.Samples;
        }

        /// <param name="names">Each stem's name.</param>
        /// <param name="markers">What each stem's file says, or null for a stem with no clip, or one that could not be read.</param>
        /// <param name="problems">Why each stem's file could not be read, or null.</param>
        internal static StemLoopReading Read(IReadOnlyList<string> names, IReadOnlyList<StemMarkers> markers, IReadOnlyList<string> problems)
        {
            StringBuilder report = new StringBuilder();

            for (int i = 0; i < problems.Count; i++)
            {
                if (problems[i] != null)
                {
                    report.Append($"Stem [{i + 1}], [{names[i]}], cannot be read: {problems[i]}.\n");
                }
            }

            if (report.Length > 0)
            {
                return new StemLoopReading(report.ToString().TrimEnd(), true);
            }

            int first = FirstWith(markers, false);

            if (first < 0)
            {
                return new StemLoopReading("There are no stems with a clip to read.", false);
            }

            for (int i = first + 1; i < markers.Count; i++)
            {
                if (markers[i] != null && !SameMoment(markers[first], markers[first].Frames, markers[i], markers[i].Frames))
                {
                    return new StemLoopReading($"The stems are not the same length, so their loop is not read:\n{Describe(names, markers, false)}", true);
                }
            }

            int looped = FirstWith(markers, true);

            if (looped < 0)
            {
                return new StemLoopReading("None of the stems' source files carries a loop: two markers named LOOPSTART and LOOPEND, or a sampler loop, in a WAV; LOOPSTART with LOOPLENGTH or LOOPEND, or alone, in an OGG. Type the loop instead.", false);
            }

            StemMarkers loop = markers[looped];

            for (int i = looped + 1; i < markers.Count; i++)
            {
                StemMarkers other = markers[i];

                if (other != null && other.HasLoop && (!SameMoment(loop, loop.LoopStart, other, other.LoopStart) || !SameMoment(loop, loop.LoopEnd, other, other.LoopEnd)))
                {
                    return new StemLoopReading($"The stems' loops differ, so none is read:\n{Describe(names, markers, true)}", true);
                }
            }

            if (loop.LoopEnd > int.MaxValue)
            {
                return new StemLoopReading($"Stem [{looped + 1}]'s loop ends past the samples a song can count, at [{loop.LoopEnd}].", true);
            }

            report.Append($"Read the loop, [{loop.LoopStart}] to [{loop.LoopEnd}] at [{loop.Rate}] Hz ({loop.Seconds(loop.LoopStart):0.000} s to {loop.Seconds(loop.LoopEnd):0.000} s), from stem [{looped + 1}]'s {loop.Source}");

            int agreeing = 0;

            for (int i = 0; i < markers.Count; i++)
            {
                agreeing += markers[i] != null && markers[i].HasLoop ? 1 : 0;
            }

            report.Append(agreeing > 1 ? $", every one of the [{agreeing}] stems with a loop agreeing." : ".");

            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i] == null)
                {
                    report.Append($" Stem [{i + 1}] has no clip, so was skipped.");
                }
                else if (!markers[i].HasLoop)
                {
                    report.Append($" Stem [{i + 1}], [{names[i]}], carries no loop, so was skipped.");
                }
                else if (markers[i].SetAside != null)
                {
                    report.Append($" Stem [{i + 1}]'s {markers[i].SetAside} was set aside for its {markers[i].Source}.");
                }
            }

            return new StemLoopReading(report.ToString(), (int)loop.LoopStart, (int)loop.LoopEnd, loop.Rate);
        }

        private static int FirstWith(IReadOnlyList<StemMarkers> markers, bool loop)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i] != null && (!loop || markers[i].HasLoop))
                {
                    return i;
                }
            }

            return -1;
        }

        // Two counts of samples, each at its own file's rate, are one moment when within half a sample of each.
        private static bool SameMoment(StemMarkers a, long aSamples, StemMarkers b, long bSamples)
        {
            return Math.Abs(a.Seconds(aSamples) - b.Seconds(bSamples)) <= 0.5 / a.Rate + 0.5 / b.Rate;
        }

        private static string Describe(IReadOnlyList<string> names, IReadOnlyList<StemMarkers> markers, bool loops)
        {
            StringBuilder lines = new StringBuilder();

            for (int i = 0; i < markers.Count; i++)
            {
                StemMarkers stem = markers[i];

                if (stem == null || (loops && !stem.HasLoop))
                {
                    continue;
                }

                lines.Append(loops
                                 ? $"Stem [{i + 1}], [{names[i]}]: [{stem.LoopStart}] to [{stem.LoopEnd}] at [{stem.Rate}] Hz, {stem.Seconds(stem.LoopStart):0.000} s to {stem.Seconds(stem.LoopEnd):0.000} s\n"
                                 : $"Stem [{i + 1}], [{names[i]}]: [{stem.Frames}] samples at [{stem.Rate}] Hz, {stem.Seconds(stem.Frames):0.000} s\n");
            }

            return lines.ToString().TrimEnd();
        }
    }
}
