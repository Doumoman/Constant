using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceCraterDoughBoundaryCandidateMatrix
    {
        private readonly IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> candidates;
        private readonly IReadOnlyDictionary<string, string> microchunkByCandidateId;

        private MoonpalaceCraterDoughBoundaryCandidateMatrix()
        {
            var specs = new[]
            {
                new Spec(0, "BOUND_CLIFF", MoonpalaceBoundaryOrientation.Horizontal),
                new Spec(1, "BOUND_CLIFF", MoonpalaceBoundaryOrientation.Vertical),
                new Spec(2, "BOUND_LAYER", MoonpalaceBoundaryOrientation.Vertical),
                new Spec(3, "BOUND_SOFT_BLEND", MoonpalaceBoundaryOrientation.Horizontal),
                new Spec(4, "BOUND_SOFT_BLEND", MoonpalaceBoundaryOrientation.Vertical),
            };

            var values = specs.Select(CreateCandidate).ToArray();
            candidates = new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(values);
            microchunkByCandidateId = new ReadOnlyDictionary<string, string>(
                specs.ToDictionary(
                    spec => MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateIds[spec.Index],
                    spec => MoonpalaceCraterDoughBoundaryAuthoringContract.MicrochunkIds[spec.Index],
                    StringComparer.Ordinal));
            Index = MoonpalaceBoundaryCandidateIndexer.Canonical.Build(values);
        }

        public static MoonpalaceCraterDoughBoundaryCandidateMatrix Canonical { get; } =
            new MoonpalaceCraterDoughBoundaryCandidateMatrix();

        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> Candidates => candidates;
        public MoonpalaceBoundaryCandidateIndex Index { get; }

        public string GetMicrochunkId(string candidateId)
        {
            if (candidateId == null) throw new ArgumentNullException(nameof(candidateId));
            if (!microchunkByCandidateId.TryGetValue(candidateId, out var microchunkId))
            {
                throw new KeyNotFoundException("Unknown Crater/Dough candidate: " + candidateId);
            }

            return microchunkId;
        }

        private static MoonpalaceBoundaryCandidateDefinition CreateCandidate(Spec spec)
        {
            var signatureId = spec.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                ? MoonpalaceCraterDoughBoundaryAuthoringContract.HorizontalEdgeSignatureId
                : MoonpalaceCraterDoughBoundaryAuthoringContract.VerticalEdgeSignatureId;
            return new MoonpalaceBoundaryCandidateDefinition(
                MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateIds[spec.Index],
                MoonpalaceCraterDoughBoundaryAuthoringContract.Pair,
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
