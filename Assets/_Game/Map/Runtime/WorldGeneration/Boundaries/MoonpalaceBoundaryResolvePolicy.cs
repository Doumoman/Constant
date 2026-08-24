using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryResolvePolicy
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static MoonpalaceBoundaryResolvePolicy Default { get; } =
            new MoonpalaceBoundaryResolvePolicy();

        public MoonpalaceBoundaryCandidateDefinition Select(
            MoonpalaceBoundaryCandidateKey key,
            IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> candidates,
            ulong selectionSeed)
        {
            if (!key.IsDefined) throw new ArgumentException("Key is undefined.", nameof(key));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (candidates.Any(candidate => candidate == null || candidate.Key != key))
            {
                throw new ArgumentException("Candidates must be non-null and match the selected key.", nameof(candidates));
            }

            var ordered = candidates
                .OrderBy(candidate => candidate.CandidateId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Signature, StringComparer.Ordinal)
                .ToArray();
            if (ordered.Length == 0) return null;

            ulong totalWeight = 0;
            foreach (var candidate in ordered)
            {
                if (candidate.Weight <= 0) continue;
                var weight = (ulong)candidate.Weight;
                if (ulong.MaxValue - totalWeight < weight)
                {
                    throw new InvalidOperationException("Boundary candidate weight sum overflowed.");
                }

                totalWeight += weight;
            }

            if (totalWeight == 0) return ordered[0];

            var ticket = ComputeStableHash(key, ordered, selectionSeed) % totalWeight;
            ulong cumulative = 0;
            foreach (var candidate in ordered)
            {
                if (candidate.Weight <= 0) continue;
                cumulative += (ulong)candidate.Weight;
                if (ticket < cumulative) return candidate;
            }

            throw new InvalidOperationException("Weighted boundary candidate selection did not resolve a candidate.");
        }

        private static ulong ComputeStableHash(
            MoonpalaceBoundaryCandidateKey key,
            IEnumerable<MoonpalaceBoundaryCandidateDefinition> candidates,
            ulong selectionSeed)
        {
            var hash = FnvOffsetBasis;
            for (var shift = 0; shift < 64; shift += 8)
            {
                AddByte(ref hash, (byte)(selectionSeed >> shift));
            }

            AddString(ref hash, key.Signature);
            foreach (var candidate in candidates)
            {
                AddString(ref hash, candidate.CandidateId);
                AddString(ref hash, candidate.Signature);
            }

            return hash;
        }

        private static void AddString(ref ulong hash, string value)
        {
            foreach (var character in value)
            {
                AddByte(ref hash, (byte)character);
                AddByte(ref hash, (byte)(character >> 8));
            }

            AddByte(ref hash, byte.MaxValue);
        }

        private static void AddByte(ref ulong hash, byte value)
        {
            unchecked
            {
                hash ^= value;
                hash *= FnvPrime;
            }
        }
    }
}
