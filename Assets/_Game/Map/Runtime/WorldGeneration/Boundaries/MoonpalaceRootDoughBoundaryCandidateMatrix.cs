using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceRootDoughBoundaryCandidateMatrix
    {
        private readonly IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> candidates;
        private readonly IReadOnlyDictionary<string, string> microchunkByCandidateId;

        private MoonpalaceRootDoughBoundaryCandidateMatrix()
        {
            var specs = new[]
            {
                new Spec(0, "BOUND_TUNNEL", MoonpalaceBoundaryOrientation.Horizontal),
                new Spec(1, "BOUND_TUNNEL", MoonpalaceBoundaryOrientation.Vertical),
                new Spec(2, "BOUND_LAYER", MoonpalaceBoundaryOrientation.Vertical),
                new Spec(3, "BOUND_SOFT_BLEND", MoonpalaceBoundaryOrientation.Horizontal),
                new Spec(4, "BOUND_SOFT_BLEND", MoonpalaceBoundaryOrientation.Vertical),
            };

            var values = specs.Select(CreateCandidate).ToArray();
            candidates = new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(values);
            microchunkByCandidateId = new ReadOnlyDictionary<string, string>(
                specs.ToDictionary(
                    spec => MoonpalaceRootDoughBoundaryAuthoringContract.CandidateIds[spec.Index],
                    spec => MoonpalaceRootDoughBoundaryAuthoringContract.MicrochunkIds[spec.Index],
                    StringComparer.Ordinal));
            Index = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(values);
        }

        public static MoonpalaceRootDoughBoundaryCandidateMatrix Canonical { get; } =
            new MoonpalaceRootDoughBoundaryCandidateMatrix();

        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> Candidates => candidates;
        public MoonpalaceBoundaryCandidateIndex Index { get; }

        public string GetMicrochunkId(string candidateId)
        {
            if (candidateId == null) throw new ArgumentNullException(nameof(candidateId));
            if (!microchunkByCandidateId.TryGetValue(candidateId, out var microchunkId))
            {
                throw new KeyNotFoundException("Unknown Root/Dough candidate: " + candidateId);
            }

            return microchunkId;
        }

        private static MoonpalaceBoundaryCandidateDefinition CreateCandidate(Spec spec)
        {
            var signatureId = spec.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                ? MoonpalaceRootDoughBoundaryAuthoringContract.HorizontalEdgeSignatureId
                : MoonpalaceRootDoughBoundaryAuthoringContract.VerticalEdgeSignatureId;
            return new MoonpalaceBoundaryCandidateDefinition(
                MoonpalaceRootDoughBoundaryAuthoringContract.CandidateIds[spec.Index],
                MoonpalaceRootDoughBoundaryAuthoringContract.Pair,
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
