using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>
    /// Reads an Ogg Vorbis source file's headers, which Unity's import ignores, without decoding its audio: its rate, its
    /// length from the last page's granule position, and its loop from the comments loop-tagging tools write —
    /// <c>LOOPSTART</c> with <c>LOOPLENGTH</c> or <c>LOOPEND</c>, or alone, looping to the file's end.
    /// </summary>
    internal static class OggLoopReader
    {
        private const int    PAGE_HEADER_LENGTH = 27;
        private const string LOOP_START         = "LOOPSTART";
        private const string LOOP_LENGTH        = "LOOPLENGTH";
        private const string LOOP_END           = "LOOPEND";

        /// <summary>The stream's sample rate, from its Vorbis identification header.</summary>
        internal static bool TryReadRate(Stream stream, out int rate, out string problem)
        {
            rate = 0;

            byte[] page = new byte[PAGE_HEADER_LENGTH];

            if (!StreamBytes.TryRead(stream, page) || Encoding.ASCII.GetString(page, 0, 4) != "OggS")
            {
                problem = "it is not an Ogg file";

                return false;
            }

            byte[] lacing = new byte[page[PAGE_HEADER_LENGTH - 1]];

            if (!StreamBytes.TryRead(stream, lacing))
            {
                problem = "its first page is cut short";

                return false;
            }

            // The identification header is the stream's first packet, alone on its first page.
            int length = 0;

            for (int i = 0; i < lacing.Length; i++)
            {
                length += lacing[i];

                if (lacing[i] < 255)
                {
                    break;
                }
            }

            byte[] packet = new byte[length];

            if (!StreamBytes.TryRead(stream, packet) || !TryReadIdentification(packet, out rate))
            {
                problem = "it is not Ogg Vorbis";

                return false;
            }

            problem = null;

            return true;
        }

        /// <summary>
        /// The stream's rate, length and loop. Comment keys are compared ignoring case, as Vorbis comments are, and a
        /// <c>LOOPLENGTH</c> and a <c>LOOPEND</c> more than a sample apart are refused.
        /// </summary>
        internal static bool TryReadMarkers(Stream stream, out StemMarkers markers, out string problem)
        {
            markers = null;

            List<byte[]> packets = new List<byte[]>(2);
            MemoryStream current = new MemoryStream();
            long         frames  = -1;
            byte[]       page    = new byte[PAGE_HEADER_LENGTH];
            bool         first   = true;

            while (StreamBytes.TryRead(stream, page))
            {
                if (Encoding.ASCII.GetString(page, 0, 4) != "OggS")
                {
                    problem = first ? "it is not an Ogg file" : $"it has no Ogg page where one should start, at byte [{stream.Position - PAGE_HEADER_LENGTH}]";

                    return false;
                }

                first = false;

                long   granule = BinaryPrimitives.ReadInt64LittleEndian(page.AsSpan(6));
                byte[] lacing  = new byte[page[PAGE_HEADER_LENGTH - 1]];

                if (!StreamBytes.TryRead(stream, lacing))
                {
                    break;
                }

                int bodyLength = 0;

                for (int i = 0; i < lacing.Length; i++)
                {
                    bodyLength += lacing[i];
                }

                if (packets.Count < 2)
                {
                    byte[] body = new byte[bodyLength];

                    if (!StreamBytes.TryRead(stream, body))
                    {
                        break;
                    }

                    int at = 0;

                    for (int i = 0; i < lacing.Length && packets.Count < 2; i++)
                    {
                        current.Write(body, at, lacing[i]);
                        at += lacing[i];

                        if (lacing[i] < 255)
                        {
                            packets.Add(current.ToArray());
                            current.SetLength(0);
                        }
                    }
                }
                else
                {
                    stream.Position += bodyLength;
                }

                if (granule >= 0)
                {
                    frames = granule;
                }
            }

            if (packets.Count < 2 || !TryReadIdentification(packets[0], out int rate))
            {
                problem = "it is not Ogg Vorbis";

                return false;
            }

            if (!TryReadComments(packets[1], out Dictionary<string, string> comments))
            {
                problem = "its comment header cannot be read";

                return false;
            }

            if (frames <= 0)
            {
                problem = "its length cannot be read from its last page";

                return false;
            }

            return TryReadLoop(comments, rate, frames, out markers, out problem);
        }

        private static bool TryReadIdentification(byte[] packet, out int rate)
        {
            rate = 0;

            if (packet.Length < 16 || packet[0] != 1 || Encoding.ASCII.GetString(packet, 1, 6) != "vorbis")
            {
                return false;
            }

            rate = (int)BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(12));

            return true;
        }

        // After its type and "vorbis", a vendor string, then a count of comments, each a length and KEY=value in UTF-8.
        private static bool TryReadComments(byte[] packet, out Dictionary<string, string> comments)
        {
            comments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (packet.Length < 11 || packet[0] != 3 || Encoding.ASCII.GetString(packet, 1, 6) != "vorbis")
            {
                return false;
            }

            long at = 11 + BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(7));

            if (at + 4 > packet.Length)
            {
                return false;
            }

            uint count = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan((int)at));

            at += 4;

            for (uint i = 0; i < count; i++)
            {
                if (at + 4 > packet.Length)
                {
                    return false;
                }

                long size = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan((int)at));

                if (at + 4 + size > packet.Length)
                {
                    return false;
                }

                string comment = Encoding.UTF8.GetString(packet, (int)at + 4, (int)size);
                int    equals  = comment.IndexOf('=');

                if (equals > 0)
                {
                    comments[comment.Substring(0, equals)] = comment.Substring(equals + 1).Trim();
                }

                at += 4 + size;
            }

            return true;
        }

        private static bool TryReadLoop(Dictionary<string, string> comments, int rate, long frames, out StemMarkers markers, out string problem)
        {
            markers = null;

            bool hasStart  = comments.TryGetValue(LOOP_START, out string startText);
            bool hasLength = comments.TryGetValue(LOOP_LENGTH, out string lengthText);
            bool hasEnd    = comments.TryGetValue(LOOP_END, out string endText);

            if (!hasStart)
            {
                if (hasLength || hasEnd)
                {
                    problem = $"it has a {(hasEnd ? LOOP_END : LOOP_LENGTH)} comment but no {LOOP_START}";

                    return false;
                }

                markers = new StemMarkers(rate, frames);
                problem = null;

                return true;
            }

            long length = 0;
            long end    = 0;

            if (!TryParse(LOOP_START, startText, out long start, out problem)
                || (hasLength && !TryParse(LOOP_LENGTH, lengthText, out length, out problem))
                || (hasEnd && !TryParse(LOOP_END, endText, out end, out problem)))
            {
                return false;
            }

            if (hasLength && hasEnd && Math.Abs(start + length - end) > 1)
            {
                problem = $"its {LOOP_LENGTH}, [{length}] from [{start}], and its {LOOP_END}, [{end}], are more than a sample apart";

                return false;
            }

            string source = $"{LOOP_START} comment alone, looping to the file's end";

            if (hasEnd)
            {
                source = hasLength ? $"{LOOP_START}, {LOOP_LENGTH} and {LOOP_END} comments" : $"{LOOP_START} and {LOOP_END} comments";
            }
            else if (hasLength)
            {
                end    = start + length;
                source = $"{LOOP_START} and {LOOP_LENGTH} comments";
            }
            else
            {
                end = frames;
            }

            return StemMarkers.TryCreateLoop(rate, frames, start, end, source, null, out markers, out problem);
        }

        private static bool TryParse(string key, string text, out long value, out string problem)
        {
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                problem = null;

                return true;
            }

            problem = $"its {key} comment, [{text}], is not a number of samples";

            return false;
        }
    }
}
