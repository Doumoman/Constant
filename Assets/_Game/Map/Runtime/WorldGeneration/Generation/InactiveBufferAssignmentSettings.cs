using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class InactiveBufferAssignmentSettings
    {
        public InactiveBufferAssignmentSettings(
            bool requireFullWorldAccounting,
            bool requireClosedInactiveBoundaries,
            bool classifyClaimAdjacentAsDecorativeBoundary)
        {
            if (!requireFullWorldAccounting)
                throw new ArgumentException("Full-world accounting is required.", nameof(requireFullWorldAccounting));
            if (!requireClosedInactiveBoundaries)
                throw new ArgumentException("Inactive boundaries must be closed.", nameof(requireClosedInactiveBoundaries));
            if (!classifyClaimAdjacentAsDecorativeBoundary)
                throw new ArgumentException("Claim-adjacent inactive sectors must be decorative boundaries.", nameof(classifyClaimAdjacentAsDecorativeBoundary));

            RequireFullWorldAccounting = requireFullWorldAccounting;
            RequireClosedInactiveBoundaries = requireClosedInactiveBoundaries;
            ClassifyClaimAdjacentAsDecorativeBoundary = classifyClaimAdjacentAsDecorativeBoundary;
        }

        public bool RequireFullWorldAccounting { get; }
        public bool RequireClosedInactiveBoundaries { get; }
        public bool ClassifyClaimAdjacentAsDecorativeBoundary { get; }
    }
}
