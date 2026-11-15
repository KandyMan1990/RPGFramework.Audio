using System.IO;
using System.Text;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>
    /// An Ogg Vorbis stream's headers and last page, written in memory: the identification header alone on the first page,
    /// the comment header on the next, or across two, and a last page whose granule position is the stream's length.
    /// Checksums are left at zero, which the reader does not check.
    /// </summary>
    internal static class OggFile
    {
        /// <param name="rate">Its sample rate.</param>
        /// <param name="codec">The six letters after the identification header's type.</param>
        /// <param name="comments">Its comments, each <c>KEY=value</c>.</param>
        /// <param name="samples">Its length, the last page's granule position.</param>
        /// <param name="commentsAcrossPages">Whether the comment header is long enough to need two pages, and takes them.</param>
        internal static MemoryStream Write(int rate, string codec = "vorbis", string[] comments = null, long samples = 1000, bool commentsAcrossPages = false)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true);

            MemoryStream identification = new MemoryStream();
            BinaryWriter id             = new BinaryWriter(identification, Encoding.ASCII);

            id.Write((byte)1);
            id.Write(Encoding.ASCII.GetBytes(codec));
            id.Write(0u);
            id.Write((byte)2);
            id.Write((uint)rate);
            id.Write(0u);
            id.Write(128000u);
            id.Write(0u);
            id.Write((byte)0xB8);
            id.Write((byte)1);
            Page(writer, 2, 0, 0, Lacing(identification.ToArray().Length), identification.ToArray());

            MemoryStream comment = new MemoryStream();
            BinaryWriter header  = new BinaryWriter(comment, Encoding.UTF8);
            string       vendor  = commentsAcrossPages ? new string('v', 600) : "Test";
            string[]     written = comments ?? new string[0];

            header.Write((byte)3);
            header.Write(Encoding.ASCII.GetBytes("vorbis"));
            header.Write((uint)vendor.Length);
            header.Write(Encoding.UTF8.GetBytes(vendor));
            header.Write((uint)written.Length);

            for (int i = 0; i < written.Length; i++)
            {
                byte[] text = Encoding.UTF8.GetBytes(written[i]);

                header.Write((uint)text.Length);
                header.Write(text);
            }

            header.Write((byte)1);

            byte[] packet = comment.ToArray();
            byte[] lacing = Lacing(packet.Length);

            if (commentsAcrossPages)
            {
                byte[] firstLacing = { 255 };
                byte[] restLacing  = new byte[lacing.Length - 1];
                byte[] first       = new byte[255];
                byte[] rest        = new byte[packet.Length - 255];

                System.Array.Copy(lacing, 1, restLacing, 0, restLacing.Length);
                System.Array.Copy(packet, 0, first, 0, 255);
                System.Array.Copy(packet, 255, rest, 0, rest.Length);
                Page(writer, 0, -1, 1, firstLacing, first);
                Page(writer, 1, 0, 2, restLacing, rest);
            }
            else
            {
                Page(writer, 0, 0, 1, lacing, packet);
            }

            Page(writer, 4, samples, 3, new byte[] { 0 }, new byte[0]);
            writer.Flush();
            stream.Position = 0;

            return stream;
        }

        private static byte[] Lacing(int length)
        {
            byte[] lacing = new byte[length / 255 + 1];

            for (int i = 0; i < lacing.Length - 1; i++)
            {
                lacing[i] = 255;
            }

            lacing[lacing.Length - 1] = (byte)(length % 255);

            return lacing;
        }

        private static void Page(BinaryWriter writer, byte type, long granule, uint sequence, byte[] lacing, byte[] body)
        {
            writer.Write(Encoding.ASCII.GetBytes("OggS"));
            writer.Write((byte)0);
            writer.Write(type);
            writer.Write(granule);
            writer.Write(1u);
            writer.Write(sequence);
            writer.Write(0u);
            writer.Write((byte)lacing.Length);
            writer.Write(lacing);
            writer.Write(body);
        }
    }
}
