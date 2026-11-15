using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace RPGFramework.Audio.Editor.Loops
{
    /// <summary>
    /// Reads a WAV source file's chunks, which Unity's import ignores, seeking past the audio rather than loading it.
    /// </summary>
    internal static class WavLoopReader
    {
        /// <summary>The file's sample rate, from its <c>fmt </c> chunk.</summary>
        internal static bool TryReadRate(Stream stream, out int rate, out string problem)
        {
            rate = 0;

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
    }
}
