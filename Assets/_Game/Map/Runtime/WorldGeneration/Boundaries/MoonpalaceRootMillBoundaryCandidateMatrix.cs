using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceRootMillBoundaryCandidateMatrix
    {
        private readonly IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> candidates;
        private readonly IReadOnlyDictionary<string, string> microchunkByCandidateId;

        private MoonpalaceRootMillBoundaryCandidateMatrix()
        {
            var specs = new[]
            {
                new Spec(0, "BOUND_RUIN", MoonpalaceBoundaryOrientation.Horizontal),
                new Spec(1, "BOUND_RUIN", MoonpalaceBoundaryOrientation.Vertical),
                new Spec(2, "BOUND_TUNNEL", MoonpalaceBoundaryOrientation.Horizontal),
                new Spec(3, "BOUND_TUNNEL", MoonpalaceBoundaryOrientation.Vertical),
                new Spec(4, "BOUND_SOFT_BLEND", MoonpalaceBoundaryOrientation.Horizontal),
                new Spec(5, "BOUND_SOFT_BLEND", MoonpalaceBoundaryOrientation.Vertical),
            };

            var values = specs.Select(CreateCandidate).ToArray();
            candidates = new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(values);
            microchunkByCandidateId = new ReadOnlyDictionary<string, string>(
                specs.ToDictionary(
                    spec => MoonpalaceRootMillBoundaryAuthoringContract.CandidateIds[spec.Index],
                    spec => MoonpalaceRootMillBoundaryAuthoringContract.MicrochunkIds[spec.Index],
                    StringComparer.Ordinal));
            Index = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(values);
        }

        public static MoonpalaceRootMillBoundaryCandidateMatrix Canonical { get; } =
            new MoonpalaceRootMillBoundaryCandidateMatrix();

        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> Candidates => candidates;
        public MoonpalaceBoundaryCandidateIndex Index { get; }

        public string GetMicrochunkId(string candidateId)
        {
            if (candidateId == null) throw new ArgumentNullException(nameof(candidateId));
            if (!microchunkByCandidateId.TryGetValue(candidateId, out var microchunkId))
            {
                throw new KeyNotFoundException("Unknown Root/Mill candidate: " + candidateId);
            }

            return microchunkId;
        }

        private static MoonpalaceBoundaryCandidateDefinition CreateCandidate(Spec spec)
        {
            var signatureId = spec.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                ? MoonpalaceRootMillBoundaryAuthoringContract.HorizontalEdgeSignatureId
                : MoonpalaceRootMillBoundaryAuthoringContract.VerticalEdgeSignatureId;
            return new MoonpalaceBoundaryCandidateDefinition(
                MoonpalaceRootMillBoundaryAuthoringContract.CandidateIds[spec.Index],
                MoonpalaceRootMillBoundaryAuthoringContract.Pair,
                new MoonpalaceBoundaryProfileId(spec.ProfileId),
                spec.Orientation,
                new MoonpalaceBoundaryRouteRole("Mandatory"),
                new MoonpalaceBoundaryEdgeSignature(signatureId),
                100,
                true,
                MoonpalaceBoundaryToolRequirement.None,
                MoonpalaceBoundaryWarningMarker.Tile | MoonpalaceBoundaryWarningMarker.Background);
        }

        private sealed class Spec
        {
            public Spec(int index, string profileId, MoonpalaceBoundaryOrientation orientation)
            {
                Index = index;
                ProfileId = profileId;
                Orientation = orientation;
            }

            public int Index { get; }
            public string ProfileId { get; }
            public MoonpalaceBoundaryOrientation Orientation { get; }
        }
    }
}
