using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>
    /// Reads an Ogg Vorbis source file's headers, which Unity's import ignores, without decoding its audio.
    /// </summary>
    internal static class OggLoopReader
    {
        private const int PAGE_HEADER_LENGTH = 27;

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

            if (length < 16 || !StreamBytes.TryRead(stream, packet) || packet[0] != 1 || Encoding.ASCII.GetString(packet, 1, 6) != "vorbis")
            {
                problem = "it is not Ogg Vorbis";

                return false;
            }

            rate    = (int)BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(12));
            problem = null;

            return true;
        }
    }
}
