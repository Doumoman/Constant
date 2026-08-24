using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryWarningProbe
    {
        public MoonpalaceBoundaryWarningProbeResult Evaluate(
            MoonpalaceBoundaryWarningProbeRequest request)
        {
            if (request == null)
            {
                return new MoonpalaceBoundaryWarningProbeResult(
                    null,
                    false,
                    new MoonpalaceBoundaryWarningMarkerCategory[0],
                    new[] { MoonpalaceBoundaryWarningIssue.InvalidRequest });
            }

            var invalidRequest = !IsStructurallyValid(request);
            var missingBoundaryProfile = request.WarningRequirement == null;
            if (!missingBoundaryProfile &&
                !request.WarningRequirement.IsCompatible(request.ResolveRequest, request.Candidate))
            {
                invalidRequest = true;
            }

            var invalidWarningLength = request.WarningMicrochunkCount < 0;
            var insufficientWarningLength = !invalidWarningLength &&
                                            request.WarningRequirement != null &&
                                            request.WarningMicrochunkCount <
                                            request.WarningRequirement.WarningMicrochunksMinimum;

            var observed = new HashSet<MoonpalaceBoundaryWarningMarkerCategory>();
            var unknownMarkerCategory = false;
            var duplicateMarkerCategory = false;
            if (request.ObservedMarkerCategories != null)
            {
                foreach (var token in request.ObservedMarkerCategories)
                {
                    if (!MoonpalaceBoundaryWarningMarkerCategory.TryParse(token, out var category) ||
                        !IsAllowed(request.WarningRequirement, category))
                    {
                        unknownMarkerCategory = true;
                        continue;
                    }

                    if (!observed.Add(category)) duplicateMarkerCategory = true;
                }
            }

            var orderedObserved = observed.OrderBy(category => category).ToArray();
            var insufficientMarkerCategories = request.WarningRequirement != null &&
                                               orderedObserved.Length <
                                               request.WarningRequirement.RequiredDistinctMarkerCategories;
            var targetBiomeMismatch = request.ResolveRequest != null &&
                                      request.ResolveRequest.ToBiome.IsDefined &&
                                      request.TargetBiome.IsDefined &&
                                      request.TargetBiome != request.ResolveRequest.ToBiome;

            var issues = new List<MoonpalaceBoundaryWarningIssue>();
            if (invalidRequest) issues.Add(MoonpalaceBoundaryWarningIssue.InvalidRequest);
            if (missingBoundaryProfile) issues.Add(MoonpalaceBoundaryWarningIssue.MissingBoundaryProfile);
            if (invalidWarningLength) issues.Add(MoonpalaceBoundaryWarningIssue.InvalidWarningLength);
            if (insufficientWarningLength) issues.Add(MoonpalaceBoundaryWarningIssue.InsufficientWarningLength);
            if (insufficientMarkerCategories) issues.Add(MoonpalaceBoundaryWarningIssue.InsufficientMarkerCategories);
            if (unknownMarkerCategory) issues.Add(MoonpalaceBoundaryWarningIssue.UnknownMarkerCategory);
            if (duplicateMarkerCategory) issues.Add(MoonpalaceBoundaryWarningIssue.DuplicateMarkerCategory);
            if (targetBiomeMismatch) issues.Add(MoonpalaceBoundaryWarningIssue.TargetBiomeMismatch);

            return new MoonpalaceBoundaryWarningProbeResult(
                request,
                issues.Count == 0,
                orderedObserved,
                issues);
        }

        private static bool IsStructurallyValid(MoonpalaceBoundaryWarningProbeRequest request)
        {
            var resolveRequest = request.ResolveRequest;
            if (resolveRequest == null ||
                request.Candidate == null ||
                request.ObservedMarkerCategories == null ||
                !request.TargetBiome.IsDefined ||
                !resolveRequest.FromBiome.IsDefined ||
                !resolveRequest.ToBiome.IsDefined ||
                resolveRequest.FromBiome == resolveRequest.ToBiome ||
                !resolveRequest.Profile.IsDefined ||
                !resolveRequest.RouteRole.IsDefined ||
                !resolveRequest.EdgeSignature.IsDefined)
            {
                return false;
            }

            return resolveRequest.Orientation == MoonpalaceBoundaryOrientation.Horizontal ||
                   resolveRequest.Orientation == MoonpalaceBoundaryOrientation.Vertical;
        }

        private static bool IsAllowed(
            MoonpalaceBoundaryWarningRequirement requirement,
            MoonpalaceBoundaryWarningMarkerCategory category)
        {
            return requirement == null || requirement.AllowedMarkerCategories.Contains(category);
        }
    }
}
