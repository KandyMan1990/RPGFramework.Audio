using System.IO;
using System.Text;

namespace RPGFramework.Audio.Tests.Loops
{
    /// <summary>
    /// A WAV written in memory, as a DAW writes one: optional chunks before its format, then silence, then a sampler loop,
    /// named cue points and the chunks FL Studio adds.
    /// </summary>
    internal static class WavFile
    {
        /// <param name="rate">Its sample rate.</param>
        /// <param name="frames">How many sample frames of silence it holds.</param>
        /// <param name="junkFirst">Whether a <c>JUNK</c> chunk comes before its format, as FL Studio writes one.</param>
        /// <param name="riff">Its first four bytes.</param>
        /// <param name="markerNames">Its cue points' names, in <c>LIST adtl</c>.</param>
        /// <param name="markerSamples">Its cue points' sample frames, one for each name.</param>
        /// <param name="samplerLoop">A <c>smpl</c> loop's first and last frames, the last played inclusive, or none.</param>
        /// <param name="flStudioChunks">Whether it carries FL Studio's <c>tlst</c> and a <c>LIST INFO</c>.</param>
        internal static MemoryStream Write(int rate, int frames, bool junkFirst = false, string riff = "RIFF", string[] markerNames = null, uint[] markerSamples = null, uint[] samplerLoop = null, bool flStudioChunks = false)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true);

            writer.Write(Encoding.ASCII.GetBytes(riff));
            writer.Write(0u);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            if (junkFirst)
            {
                Chunk(writer, "JUNK", new byte[28]);
            }

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16u);
            writer.Write((ushort)1);
            writer.Write((ushort)2);
            writer.Write((uint)rate);
            writer.Write((uint)(rate * 4));
            writer.Write((ushort)4);
            writer.Write((ushort)16);
            Chunk(writer, "data", new byte[frames * 4]);

            if (samplerLoop != null)
            {
                MemoryStream smpl = new MemoryStream();
                BinaryWriter body = new BinaryWriter(smpl);

                body.Write(new byte[28]);
                body.Write(1u);
                body.Write(0u);
                body.Write(131072u);
                body.Write(1024u);
                body.Write(samplerLoop[0]);
                body.Write(samplerLoop[1]);
                body.Write(0u);
                body.Write(0u);
                Chunk(writer, "smpl", smpl.ToArray());
            }

            if (markerNames != null)
            {
                MemoryStream cue  = new MemoryStream();
                BinaryWriter cues = new BinaryWriter(cue, Encoding.ASCII);

                cues.Write((uint)markerNames.Length);

                for (int i = 0; i < markerNames.Length; i++)
                {
                    cues.Write((uint)(65536 + i));
                    cues.Write(markerSamples[i]);
                    cues.Write(Encoding.ASCII.GetBytes("data"));
                    cues.Write(0u);
                    cues.Write(0u);
                    cues.Write(markerSamples[i]);
                }

                Chunk(writer, "cue ", cue.ToArray());

                MemoryStream adtl   = new MemoryStream();
                BinaryWriter labels = new BinaryWriter(adtl, Encoding.ASCII);

                labels.Write(Encoding.ASCII.GetBytes("adtl"));

                for (int i = 0; i < markerNames.Length; i++)
                {
                    byte[] text = Encoding.ASCII.GetBytes(markerNames[i] + "\0");

                    labels.Write(Encoding.ASCII.GetBytes("labl"));
                    labels.Write((uint)(4 + text.Length));
                    labels.Write((uint)(65536 + i));
                    labels.Write(text);

                    if (((4 + text.Length) & 1) == 1)
                    {
                        labels.Write((byte)0);
                    }
                }

                Chunk(writer, "LIST", adtl.ToArray());
            }

            if (flStudioChunks)
            {
                Chunk(writer, "tlst", new byte[52]);

                byte[] info = Encoding.ASCII.GetBytes("INFOISFT\u000f\0\0\0FL Studio 2026\0\0");

                Chunk(writer, "LIST", info);
            }

            writer.Flush();
            stream.Position = 0;

            return stream;
        }

        private static void Chunk(BinaryWriter writer, string id, byte[] body)
        {
            writer.Write(Encoding.ASCII.GetBytes(id));
            writer.Write((uint)body.Length);
            writer.Write(body);

            if ((body.Length & 1) == 1)
            {
                writer.Write((byte)0);
            }
        }
    }
}
