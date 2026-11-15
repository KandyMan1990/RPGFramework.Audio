using System.IO;

namespace RPGFramework.Audio.Editor.Loops
{
    internal static class StreamBytes
    {
        /// <summary>Fills <paramref name="buffer" /> from the stream, false when the stream ends first.</summary>
        internal static bool TryRead(Stream stream, byte[] buffer)
        {
            int read = 0;

            while (read < buffer.Length)
            {
                int count = stream.Read(buffer, read, buffer.Length - read);

                if (count == 0)
                {
                    return false;
                }

                read += count;
            }

            return true;
        }
    }
}
