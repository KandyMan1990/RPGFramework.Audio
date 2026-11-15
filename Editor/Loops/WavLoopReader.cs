using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>
    /// Reads a WAV source file's chunks, which Unity's import ignores, seeking past the audio rather than loading it: its
    /// rate, its length, and its loop — two <c>cue </c> points named <c>LOOPSTART</c> and <c>LOOPEND</c> in
    /// <c>LIST adtl</c>'s labels, else a <c>smpl</c> loop.
    /// </summary>
    internal static class WavLoopReader
    {
        private const string LOOP_START = "LOOPSTART";
        private const string LOOP_END   = "LOOPEND";

        // The chunks read whole, which are small; anything larger is skipped.
        private const uint MAX_READ_CHUNK = 1 << 20;

        /// <summary>The file's sample rate, from its <c>fmt </c> chunk.</summary>
        internal static bool TryReadRate(Stream stream, out int rate, out string problem)
        {
            rate = 0;

            if (!TryReadHeader(stream, out problem))
            {
                return false;
            }

            byte[] chunk = new byte[8];

            while (StreamBytes.TryRead(stream, chunk))
            {
                uint size = BinaryPrimitives.ReadUInt32LittleEndian(chunk.AsSpan(4));
                long next = stream.Position + size + (size & 1);

                if (Encoding.ASCII.GetString(chunk, 0, 4) == "fmt " && size >= 16)
                {
                    byte[] format = new byte[8];

                    if (!StreamBytes.TryRead(stream, format))
                    {
                        break;
                    }

                    rate    = (int)BinaryPrimitives.ReadUInt32LittleEndian(format.AsSpan(4));
                    problem = null;

                    return true;
                }

                stream.Position = next;
            }

            problem = "it has no fmt chunk";

            return false;
        }

        /// <summary>
        /// The file's rate, length and loop. Named markers win over a <c>smpl</c> loop spanning the whole file, which says
        /// only that everything loops; markers and a <c>smpl</c> loop of its own that differ, or two <c>smpl</c> loops,
        /// are refused, as which one is meant cannot be told.
        /// </summary>
        internal static bool TryReadMarkers(Stream stream, out StemMarkers markers, out string problem)
        {
            markers = null;

            if (!TryReadHeader(stream, out problem))
            {
                return false;
            }

            int                      rate        = 0;
            int                      frameBytes  = 0;
            long                     dataBytes   = -1;
            Dictionary<uint, long>   cues        = new Dictionary<uint, long>();
            Dictionary<uint, string> labels      = new Dictionary<uint, string>();
            List<long>               loopStarts  = new List<long>();
            List<long>               loopEnds    = new List<long>();
            byte[]                   chunkHeader = new byte[8];

            while (StreamBytes.TryRead(stream, chunkHeader))
            {
                string id   = Encoding.ASCII.GetString(chunkHeader, 0, 4);
                uint   size = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader.AsSpan(4));
                long   next = stream.Position + size + (size & 1);

                if (id == "data")
                {
                    dataBytes = size;
                }
                else if ((id == "fmt " || id == "cue " || id == "LIST" || id == "smpl") && size <= MAX_READ_CHUNK)
                {
                    byte[] body = new byte[size];

                    if (!StreamBytes.TryRead(stream, body))
                    {
                        break;
                    }

                    if (id == "fmt " && size >= 16)
                    {
                        rate       = (int)BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(4));
                        frameBytes = BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(12));
                    }
                    else if (id == "cue ")
                    {
                        ReadCues(body, cues);
                    }
                    else if (id == "LIST" && size >= 4 && Encoding.ASCII.GetString(body, 0, 4) == "adtl")
                    {
                        ReadLabels(body, labels);
                    }
                    else if (id == "smpl")
                    {
                        ReadSamplerLoops(body, loopStarts, loopEnds);
                    }
                }

                stream.Position = next;
            }

            if (rate <= 0 || frameBytes <= 0)
            {
                problem = "it has no fmt chunk";

                return false;
            }

            if (dataBytes < 0)
            {
                problem = "it has no data chunk";

                return false;
            }

            long frames = dataBytes / frameBytes;

            if (!TryFindNamedMarkers(cues, labels, out bool named, out long start, out long end, out problem))
            {
                return false;
            }

            if (loopStarts.Count > 1)
            {
                problem = $"it holds [{loopStarts.Count}] sampler loops, so which one loops cannot be told";

                return false;
            }

            bool   sampled  = loopStarts.Count == 1;
            string source   = "LOOPSTART and LOOPEND markers";
            string setAside = null;

            if (named && sampled)
            {
                bool wholeFile = loopStarts[0] == 0 && loopEnds[0] >= frames;

                if (wholeFile)
                {
                    setAside = "sampler loop over the whole file";
                }
                else if (loopStarts[0] != start || loopEnds[0] != end)
                {
                    problem = $"its LOOPSTART and LOOPEND markers, [{start}] to [{end}], and its sampler loop, [{loopStarts[0]}] to [{loopEnds[0]}], differ";

                    return false;
                }
            }
            else if (sampled)
            {
                start  = loopStarts[0];
                end    = loopEnds[0];
                source = loopStarts[0] == 0 && loopEnds[0] >= frames ? "sampler loop over the whole file" : "sampler loop";
            }
            else if (!named)
            {
                markers = new StemMarkers(rate, frames);

                return true;
            }

            return StemMarkers.TryCreateLoop(rate, frames, start, end, source, setAside, out markers, out problem);
        }

        private static bool TryReadHeader(Stream stream, out string problem)
        {
            byte[] header = new byte[12];

            if (!StreamBytes.TryRead(stream, header))
            {
                problem = "it is too short to be a WAV";

                return false;
            }

            string riff = Encoding.ASCII.GetString(header, 0, 4);

            if (riff == "RF64")
            {
                problem = "it is an RF64 WAV, written only above 4 GB, which is not read";

                return false;
            }

            if (riff != "RIFF" || Encoding.ASCII.GetString(header, 8, 4) != "WAVE")
            {
                problem = "it is not a RIFF WAVE file";

                return false;
            }

            problem = null;

            return true;
        }

        // Each cue point is 24 bytes after the count: its id, then where it is, the sample offset last.
        private static void ReadCues(byte[] body, Dictionary<uint, long> cues)
        {
            if (body.Length < 4)
            {
                return;
            }

            uint count = BinaryPrimitives.ReadUInt32LittleEndian(body);

            for (int i = 0; i < count && 4 + (i + 1) * 24 <= body.Length; i++)
            {
                int at = 4 + i * 24;

                cues[BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(at))] = BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(at + 20));
            }
        }

        private static void ReadLabels(byte[] body, Dictionary<uint, string> labels)
        {
            int at = 4;

            while (at + 8 <= body.Length)
            {
                string id   = Encoding.ASCII.GetString(body, at, 4);
                int    size = (int)BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(at + 4));

                if (id == "labl" && size >= 4 && at + 8 + size <= body.Length)
                {
                    uint   cue  = BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(at + 8));
                    string text = Encoding.UTF8.GetString(body, at + 12, size - 4);
                    int    end  = text.IndexOf('\0');

                    labels[cue] = (end >= 0 ? text.Substring(0, end) : text).Trim();
                }

                at += 8 + size + (size & 1);
            }
        }

        // The loops follow a 36-byte header whose eighth word counts them; each is 24 bytes, its start and end the third
        // and fourth words, the end the last sample played.
        private static void ReadSamplerLoops(byte[] body, List<long> starts, List<long> ends)
        {
            if (body.Length < 36)
            {
                return;
            }

            uint count = BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(28));

            for (int i = 0; i < count && 36 + (i + 1) * 24 <= body.Length; i++)
            {
                int at = 36 + i * 24;

                starts.Add(BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(at + 8)));
                ends.Add(BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(at + 12)) + 1L);
            }
        }

        private static bool TryFindNamedMarkers(Dictionary<uint, long> cues, Dictionary<uint, string> labels, out bool named, out long start, out long end, out string problem)
        {
            int startCount = 0;
            int endCount   = 0;

            start = 0;
            end   = 0;

            foreach (KeyValuePair<uint, string> label in labels)
            {
                if (!cues.TryGetValue(label.Key, out long sample))
                {
                    continue;
                }

                if (string.Equals(label.Value, LOOP_START, StringComparison.OrdinalIgnoreCase))
                {
                    start = sample;
                    startCount++;
                }
                else if (string.Equals(label.Value, LOOP_END, StringComparison.OrdinalIgnoreCase))
                {
                    end = sample;
                    endCount++;
                }
            }

            named = startCount == 1 && endCount == 1;

            if (startCount > 1 || endCount > 1)
            {
                problem = $"it has [{startCount}] markers named {LOOP_START} and [{endCount}] named {LOOP_END}, where a loop takes one of each";

                return false;
            }

            if (startCount != endCount)
            {
                problem = startCount == 1 ? $"it has a {LOOP_START} marker but no {LOOP_END}" : $"it has a {LOOP_END} marker but no {LOOP_START}";

                return false;
            }

            problem = null;

            return true;
        }
    }
}
