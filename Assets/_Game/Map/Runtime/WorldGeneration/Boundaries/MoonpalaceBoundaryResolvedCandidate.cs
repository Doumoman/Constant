using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryResolvedCandidate
    {
        internal MoonpalaceBoundaryResolvedCandidate(
            MoonpalaceBoundaryCandidateDefinition candidate,
            MoonpalaceBiomePair canonicalPair,
            MoonpalaceBoundaryRequestDirection requestDirection,
            MoonpalaceBoundaryTransformPolicy transformPolicy,
            MoonpalaceBoundaryCandidateKey selectedKey)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            if (!canonicalPair.IsDefined) throw new ArgumentException("Canonical pair is undefined.", nameof(canonicalPair));
            if (!selectedKey.IsDefined) throw new ArgumentException("Selected key is undefined.", nameof(selectedKey));
            if (candidate.Key != selectedKey || selectedKey.Pair != canonicalPair)
            {
                throw new ArgumentException("Resolved candidate, key, and canonical pair must match.");
            }

            TransformPolicy = transformPolicy ?? throw new ArgumentNullException(nameof(transformPolicy));
            if (transformPolicy.Direction != requestDirection ||
                transformPolicy.Orientation != selectedKey.Orientation)
            {
                throw new ArgumentException("Transform policy does not match the request direction and key.");
            }

            CanonicalPair = canonicalPair;
            RequestDirection = requestDirection;
            SelectedKey = selectedKey;
        }

        public MoonpalaceBoundaryCandidateDefinition Candidate { get; }
        public MoonpalaceBiomePair CanonicalPair { get; }
        public MoonpalaceBoundaryRequestDirection RequestDirection { get; }
        public MoonpalaceBoundaryTransformPolicy TransformPolicy { get; }
        public MoonpalaceBoundaryCandidateKey SelectedKey { get; }
    }
}
