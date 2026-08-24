using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceMandatoryBoundaryFilter
    {
        private readonly MoonpalaceMandatoryBoundaryFilterPolicy policy;

        public MoonpalaceMandatoryBoundaryFilter(
            MoonpalaceMandatoryBoundaryFilterPolicy policy = null)
        {
            this.policy = policy ?? MoonpalaceMandatoryBoundaryFilterPolicy.Default;
        }

        public MoonpalaceMandatoryBoundaryFilterResult Apply(
            MoonpalaceMandatoryBoundaryFilterRequest request)
        {
            if (!TryGetCandidateKey(request, out var key))
            {
                return CreateInvalidRequestResult();
            }

            var original = request.CandidateIndex.GetCandidates(key);
            var accepted = new List<MoonpalaceBoundaryCandidateDefinition>(original.Count);
            var rejectionSummary =
                new Dictionary<MoonpalaceMandatoryBoundaryFilterIssue, int>();

            foreach (var candidate in original)
            {
                var rejection = policy.GetRejectionReason(
                    candidate,
                    request.MandatoryRouteBoundary);
                if (rejection == MoonpalaceMandatoryBoundaryFilterIssue.None)
                {
                    accepted.Add(candidate);
                    continue;
                }

                rejectionSummary[rejection] = rejectionSummary.TryGetValue(rejection, out var count)
                    ? count + 1
                    : 1;
            }

            var issues = accepted.Count == 0
                ? new[] { MoonpalaceMandatoryBoundaryFilterIssue.NoCandidatesAfterFilter }
                : Array.Empty<MoonpalaceMandatoryBoundaryFilterIssue>();
            var filteredIndex = accepted.Count == 0
                ? null
                : new MoonpalaceBoundaryCandidateIndex(new[]
                {
                    new MoonpalaceBoundaryCandidateIndexEntry(key, accepted),
                });

            return new MoonpalaceMandatoryBoundaryFilterResult(
                original.Count,
                accepted,
                rejectionSummary,
                issues,
                filteredIndex);
        }

        private static bool TryGetCandidateKey(
            MoonpalaceMandatoryBoundaryFilterRequest request,
            out MoonpalaceBoundaryCandidateKey key)
        {
            key = default;
            if (request?.ResolveRequest == null || request.CandidateIndex == null) return false;

            var resolveRequest = request.ResolveRequest;
            if (!resolveRequest.FromBiome.IsDefined ||
                !resolveRequest.ToBiome.IsDefined ||
                resolveRequest.FromBiome == resolveRequest.ToBiome ||
                !resolveRequest.Profile.IsDefined ||
                (resolveRequest.Orientation != MoonpalaceBoundaryOrientation.Horizontal &&
                 resolveRequest.Orientation != MoonpalaceBoundaryOrientation.Vertical) ||
                !resolveRequest.RouteRole.IsDefined ||
                !resolveRequest.EdgeSignature.IsDefined)
            {
                return false;
            }

            key = new MoonpalaceBoundaryCandidateKey(
                new MoonpalaceBiomePair(resolveRequest.FromBiome, resolveRequest.ToBiome),
                resolveRequest.Profile,
                resolveRequest.Orientation,
                resolveRequest.RouteRole,
                resolveRequest.EdgeSignature);
            return true;
        }

        private static MoonpalaceMandatoryBoundaryFilterResult CreateInvalidRequestResult()
        {
            return new MoonpalaceMandatoryBoundaryFilterResult(
                0,
                Array.Empty<MoonpalaceBoundaryCandidateDefinition>(),
                new Dictionary<MoonpalaceMandatoryBoundaryFilterIssue, int>(),
                new[] { MoonpalaceMandatoryBoundaryFilterIssue.InvalidRequest },
                null);
        }
    }
}
