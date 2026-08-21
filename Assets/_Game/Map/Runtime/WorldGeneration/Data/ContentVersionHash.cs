using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ContentVersionHash : IEquatable<ContentVersionHash>
    {
        public const int DigestLength = 32;

        private static readonly char[] HexDigits = "0123456789abcdef".ToCharArray();
        private readonly byte[] digest;
        private readonly ReadOnlyCollection<byte> bytes;

        internal ContentVersionHash(IEnumerable<byte> sourceDigest)
        {
            digest = new List<byte>(sourceDigest ??
                throw new ArgumentNullException(nameof(sourceDigest))).ToArray();
            if (digest.Length != DigestLength)
            {
                throw new ArgumentException("A content version hash must contain exactly 32 bytes.");
            }

            bytes = new ReadOnlyCollection<byte>((byte[])digest.Clone());
            var characters = new char[digest.Length * 2];
            for (var index = 0; index < digest.Length; index++)
            {
                characters[index * 2] = HexDigits[digest[index] >> 4];
                characters[(index * 2) + 1] = HexDigits[digest[index] & 0x0f];
            }

            Hex = new string(characters);
        }

        public string Hex { get; }

        public IReadOnlyList<byte> Bytes => bytes;

        public byte[] ToByteArray() => (byte[])digest.Clone();

        public bool Equals(ContentVersionHash other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            for (var index = 0; index < digest.Length; index++)
            {
                if (digest[index] != other.digest[index]) return false;
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as ContentVersionHash);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                for (var index = 0; index < digest.Length; index++)
                {
                    hash = (hash * 31) + digest[index];
                }

                return hash;
            }
        }

        public override string ToString() => Hex;

        public static bool operator ==(ContentVersionHash left, ContentVersionHash right)
        {
            return ReferenceEquals(left, right) ||
                   (!ReferenceEquals(left, null) && left.Equals(right));
        }

        public static bool operator !=(ContentVersionHash left, ContentVersionHash right) =>
            !(left == right);
    }
}
