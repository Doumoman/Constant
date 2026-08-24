using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryWarningProbeRequest
    {
        private readonly IReadOnlyList<string> observedMarkerCategories;

        public MoonpalaceBoundaryWarningProbeRequest(
            MoonpalaceBoundaryResolveRequest resolveRequest,
            MoonpalaceBoundaryCandidateDefinition candidate,
            MoonpalaceBoundaryWarningRequirement warningRequirement,
            int warningMicrochunkCount,
            IEnumerable<string> observedMarkerCategories,
            MoonpalaceBiomeId targetBiome)
        {
            ResolveRequest = resolveRequest;
            Candidate = candidate;
            WarningRequirement = warningRequirement;
            WarningMicrochunkCount = warningMicrochunkCount;
            this.observedMarkerCategories = observedMarkerCategories == null
                ? null
                : new ReadOnlyCollection<string>(observedMarkerCategories.ToArray());
            TargetBiome = targetBiome;
        }

        public MoonpalaceBoundaryResolveRequest ResolveRequest { get; }
        public MoonpalaceBoundaryCandidateDefinition Candidate { get; }
        public MoonpalaceBoundaryWarningRequirement WarningRequirement { get; }
        public int WarningMicrochunkCount { get; }
        public IReadOnlyList<string> ObservedMarkerCategories => observedMarkerCategories;
        public MoonpalaceBiomeId TargetBiome { get; }
    }
}
