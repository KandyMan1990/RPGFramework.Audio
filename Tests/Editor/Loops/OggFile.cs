using System.IO;
using System.Text;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>
    /// An Ogg Vorbis stream's first page, written in memory: the identification header alone on it, as the format puts it.
    /// Its checksum is left at zero, which the reader does not check.
    /// </summary>
    internal static class OggFile
    {
        internal static MemoryStream Write(int rate, string codec = "vorbis")
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true);

            writer.Write(Encoding.ASCII.GetBytes("OggS"));
            writer.Write((byte)0);
            writer.Write((byte)2);
            writer.Write(0L);
            writer.Write(1u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write((byte)1);
            writer.Write((byte)30);

            writer.Write((byte)1);
            writer.Write(Encoding.ASCII.GetBytes(codec));
            writer.Write(0u);
            writer.Write((byte)2);
            writer.Write((uint)rate);
            writer.Write(0u);
            writer.Write(128000u);
            writer.Write(0u);
            writer.Write((byte)0xB8);
            writer.Write((byte)1);
            writer.Flush();

            stream.Position = 0;

            return stream;
        }
    }
}
