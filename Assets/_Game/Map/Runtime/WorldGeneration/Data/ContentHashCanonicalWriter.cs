using System;
using System.IO;
using System.Text;

namespace StarNight.Map.WorldGeneration.Data
{
    internal sealed class ContentHashCanonicalWriter : IDisposable
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private readonly MemoryStream stream = new MemoryStream();

        public void WriteString(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var encoded = StrictUtf8.GetBytes(value);
            WriteUInt64((ulong)encoded.Length);
            stream.Write(encoded, 0, encoded.Length);
        }

        public void WriteCount(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            WriteUInt64((ulong)count);
        }

        public byte[] ToArray() => stream.ToArray();

        public void Dispose() => stream.Dispose();

        private void WriteUInt64(ulong value)
        {
            for (var shift = 56; shift >= 0; shift -= 8)
            {
                stream.WriteByte((byte)(value >> shift));
            }
        }
    }
}
