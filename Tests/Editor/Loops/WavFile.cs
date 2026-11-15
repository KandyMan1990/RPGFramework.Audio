using System.IO;
using System.Text;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>A WAV written in memory, as a DAW writes one: optional chunks before its format, then silence.</summary>
    internal static class WavFile
    {
        internal static MemoryStream Write(int rate, int frames, bool junkFirst = false, string riff = "RIFF")
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true);

            writer.Write(Encoding.ASCII.GetBytes(riff));
            writer.Write(0u);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            if (junkFirst)
            {
                writer.Write(Encoding.ASCII.GetBytes("JUNK"));
                writer.Write(28u);
                writer.Write(new byte[28]);
            }

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16u);
            writer.Write((ushort)1);
            writer.Write((ushort)2);
            writer.Write((uint)rate);
            writer.Write((uint)(rate * 4));
            writer.Write((ushort)4);
            writer.Write((ushort)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write((uint)(frames * 4));
            writer.Write(new byte[frames * 4]);
            writer.Flush();

            stream.Position = 0;

            return stream;
        }
    }
}
