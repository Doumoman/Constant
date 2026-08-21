using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public static class DeterministicRngSeedDeriver
    {
        private static readonly byte[] Domain =
        {
            0x53, 0x54, 0x41, 0x52, 0x4E, 0x49, 0x47, 0x48, 0x54, 0x5F,
            0x4D, 0x41, 0x50, 0x5F, 0x52, 0x4E, 0x47, 0x5F, 0x56, 0x31
        };

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static ulong DeriveInitialState(
            ulong worldSeed,
            RngStreamDefinition definition,
            RngStreamScope scope)
        {
            var definitionScope = ValidateDefinition(definition);
            scope.Validate();
            if (definitionScope != scope.ResetScope)
            {
                throw new ArgumentException("Definition reset scope does not match stream scope.", nameof(scope));
            }

            var material = new List<byte>(128);
            material.AddRange(Domain);
            AppendUInt64BigEndian(material, worldSeed);
            foreach (var value in definition.SaltHex.Bytes)
            {
                material.Add(value);
            }

            AppendString(material, definition.RngStreamId);
            AppendString(material, RngResetScopeToken.Format(definitionScope));
            AppendString(material, scope.Identity);
            AppendUInt64BigEndian(material, (ulong)scope.AttemptOrdinal);

            byte[] digest;
            using (var sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(material.ToArray());
            }

            return ReadUInt64BigEndian(digest, 0);
        }

        internal static RngResetScope ValidateDefinition(RngStreamDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!definition.Active)
            {
                throw new ArgumentException("RNG stream definition must be active.", nameof(definition));
            }

            if (definition.RngStreamId == null)
            {
                throw new ArgumentException("RNG stream ID cannot be null.", nameof(definition));
            }

            if (definition.RngStreamId.Length == 0)
            {
                throw new ArgumentException("RNG stream ID cannot be empty.", nameof(definition));
            }

            if (definition.SaltHex == null ||
                definition.SaltHex.Bytes == null ||
                definition.SaltHex.Bytes.Count != 8)
            {
                throw new ArgumentException("RNG salt must contain exactly 8 bytes.", nameof(definition));
            }

            return RngResetScopeToken.Parse(definition.ResetScope);
        }

        private static void AppendString(List<byte> target, string value)
        {
            var bytes = StrictUtf8.GetBytes(value);
            AppendUInt64BigEndian(target, (ulong)bytes.Length);
            target.AddRange(bytes);
        }

        private static void AppendUInt64BigEndian(List<byte> target, ulong value)
        {
            target.Add((byte)(value >> 56));
            target.Add((byte)(value >> 48));
            target.Add((byte)(value >> 40));
            target.Add((byte)(value >> 32));
            target.Add((byte)(value >> 24));
            target.Add((byte)(value >> 16));
            target.Add((byte)(value >> 8));
            target.Add((byte)value);
        }

        private static ulong ReadUInt64BigEndian(byte[] source, int offset)
        {
            return ((ulong)source[offset] << 56) |
                   ((ulong)source[offset + 1] << 48) |
                   ((ulong)source[offset + 2] << 40) |
                   ((ulong)source[offset + 3] << 32) |
                   ((ulong)source[offset + 4] << 24) |
                   ((ulong)source[offset + 5] << 16) |
                   ((ulong)source[offset + 6] << 8) |
                   source[offset + 7];
        }
    }
}
