using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorActivityEventPlacementPlanner
    {
        public const string ReferencePublicationLabel = "REFERENCE QUIET ACTIVITY EVENT";

        public static SectorQuietActivityEventBuildResult Place(SectorActivityEventPlacementRequest request)
        {
            var errors = new List<SectorQuietActivityEventError>();
            ValidateRequest(request, errors);
            if (errors.Count != 0) return Failure(errors);

            var selectedActivities = request.ActivityFrequencyPlan.Decisions
                .GroupBy(value => value.OpportunityId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.Single(), StringComparer.Ordinal);
            var activityDecisions = new List<SectorActivityPlacementDecision>();
            var compatibleActivityCandidateCount = 0;
            foreach (var projection in request.ActivityOpportunities)
            {
                var candidates = request.ActivityCandidateIndex.GetCandidates(projection.OpportunityId);
                compatibleActivityCandidateCount += candidates.Count;
                if (selectedActivities.TryGetValue(projection.OpportunityId, out var selected))
                {
                    activityDecisions.Add(new SectorActivityPlacementDecision(
                        projection,
                        SectorActivityEventPlacementState.Selected,
                        selected.ActivityId,
                        selected.Strength,
                        "MAP12_FREQUENCY_SELECTED",
                        selected.CandidateKey,
                        selected.WorldStrongBefore,
                        selected.WorldStrongAfter,
                        selected.PatchStrongBefore,
                        selected.PatchStrongAfter,
                        selected.SectorStrongBefore,
                        selected.SectorStrongAfter));
                }
                else
                {
                    activityDecisions.Add(new SectorActivityPlacementDecision(
                        projection,
                        SectorActivityEventPlacementState.Rejected,
                        default(ActivityStructureId),
                        ActivityStrengthClass.Ordinary,
                        "MAP12_FREQUENCY_NOT_SELECTED",
                        string.Empty,
                        0, 0, 0, 0, 0, 0));
                }
            }

            var eventAuthorityDecisions = request.EventAssignmentPlan.Decisions
                .GroupBy(value => value.OpportunityId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.Single(), StringComparer.Ordinal);
            var eventDecisions = new List<SectorEventMarkerPlacementDecision>();
            var eventNonEmptyCompatibleCandidateCount = 0;
            var eventEmptyCompatibleCandidateCount = 0;
            var cooldownViolationCount = 0;
            foreach (var projection in request.EventOpportunities)
            {
                var candidates = request.EventCandidateIndex.GetCandidates(projection.OpportunityId);
                eventNonEmptyCompatibleCandidateCount += candidates.Count(value => !value.IsEmpty);
                eventEmptyCompatibleCandidateCount += candidates.Count(value => value.IsEmpty);
                var authority = eventAuthorityDecisions[projection.OpportunityId];
                var state = authority.DecisionKind == EventOverlayAssignmentDecisionKind.Empty
                    ? SectorActivityEventPlacementState.ExplicitEmpty
                    : SectorActivityEventPlacementState.Assigned;
                if (state == SectorActivityEventPlacementState.Assigned &&
                    authority.PreviousProgressionOrdinal >= 0 &&
                    authority.ActualProgressionGap < authority.RequiredProgressionGap)
                    cooldownViolationCount++;
                eventDecisions.Add(new SectorEventMarkerPlacementDecision(
                    projection,
                    state,
                    authority.EventId,
                    authority.EventKind,
                    authority.CandidateKey,
                    authority.PreviousProgressionOrdinal,
                    authority.CurrentProgressionOrdinal,
                    authority.RequiredProgressionGap,
                    authority.ActualProgressionGap,
                    authority.CooldownExclusionEvidence));
            }

            if (cooldownViolationCount != 0)
            {
                Add(errors, SectorQuietActivityEventErrorCode.EventCooldownViolation,
                    "event.cooldown", cooldownViolationCount.ToString(CultureInfo.InvariantCulture));
                return Failure(errors);
            }

            var provisional = new SectorQuietActivityEventPlan(
                request,
                activityDecisions,
                eventDecisions,
                compatibleActivityCandidateCount,
                activityDecisions.Count(value => value.State == SectorActivityEventPlacementState.Rejected),
                eventNonEmptyCompatibleCandidateCount,
                eventEmptyCompatibleCandidateCount,
                cooldownViolationCount,
                string.Empty);
            var digest = SectorQuietActivityEventCanonicalDigest.Compute(provisional);
            if (!string.IsNullOrEmpty(request.ExpectedCanonicalDigest) &&
                !string.Equals(request.ExpectedCanonicalDigest, digest, StringComparison.Ordinal))
            {
                Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication,
                    "expectedCanonicalDigest", "Published Activity/Event digest did not match the expected digest.");
                return Failure(errors);
            }

            var plan = new SectorQuietActivityEventPlan(
                request,
                activityDecisions,
                eventDecisions,
                compatibleActivityCandidateCount,
                activityDecisions.Count(value => value.State == SectorActivityEventPlacementState.Rejected),
                eventNonEmptyCompatibleCandidateCount,
                eventEmptyCompatibleCandidateCount,
                cooldownViolationCount,
                digest);
            return new SectorQuietActivityEventBuildResult(plan, digest, Array.Empty<SectorQuietActivityEventError>());
        }

        private static void ValidateRequest(
            SectorActivityEventPlacementRequest request,
            ICollection<SectorQuietActivityEventError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorQuietActivityEventErrorCode.MissingInput, "request", "Placement request is required.");
                return;
            }
            if (request.QuietFillPlan == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingInput, "quietFillPlan", "Successful Quiet fill plan is required.");
            if (request.ActivityCandidateIndex == null || request.ActivityFrequencyPlan == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingActivityAuthority,
                    "activityAuthority", "MAP12 Activity candidate/frequency authority is required.");
            if (request.EventCandidateIndex == null || request.EventAssignmentPlan == null)
                Add(errors, SectorQuietActivityEventErrorCode.MissingEventAuthority,
                    "eventAuthority", "MAP12 Event candidate/assignment authority is required.");
            if (request.ActivityOpportunities.Count == 0)
                Add(errors, SectorQuietActivityEventErrorCode.MissingActivityAuthority,
                    "activityOpportunities", "At least one Activity opportunity projection is required.");
            if (request.EventOpportunities.Count == 0)
                Add(errors, SectorQuietActivityEventErrorCode.MissingEventAuthority,
                    "eventOpportunities", "At least one Event opportunity projection is required.");
            if (errors.Count != 0) return;

            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication,
                    "publicationLabel", "Reference publication label is required.");
            if (!string.Equals(request.EventCandidateIndex.ActivityFrequencyPlanDigest,
                    request.ActivityFrequencyPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.EventAssignmentPlan.ActivityFrequencyPlanDigest,
                    request.ActivityFrequencyPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.EventAssignmentPlan.CandidateIndexDigest,
                    request.EventCandidateIndex.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication,
                    "map12.digestChain", "MAP12 Activity/Event public digest chain must be exact.");

            ValidateActivity(request, errors);
            ValidateEvents(request, errors);
            foreach (var fault in request.ReferenceFaults)
                Add(errors, fault, "referenceFaults", "Injected reference fault must fail atomically.");
            if (request.ActivityMarkerMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.ActivityMarkerMutationClaim,
                    "mutation.activityMarker", "Activity marker authority cannot be mutated.");
            if (request.EventMarkerMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.EventMarkerMutationClaim,
                    "mutation.eventMarker", "Event marker authority cannot be mutated.");
            if (request.SpecialPersistenceMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.SpecialPersistenceMutationClaim,
                    "mutation.specialPersistence", "Special persistence cannot be transferred to Event.");
            if (request.OwnershipMutationClaim)
                Add(errors, SectorQuietActivityEventErrorCode.OwnershipMutationClaim,
                    "mutation.ownership", "Final ownership is owned by MAP14_07.");
            AddCount(errors, SectorQuietActivityEventErrorCode.SolverMutationClaim,
                "mutation.solver", request.SolverInvocationCount);
            AddCount(errors, SectorQuietActivityEventErrorCode.RngMutationClaim,
                "mutation.map14Rng", request.Map14RngDrawCount);
            AddCount(errors, SectorQuietActivityEventErrorCode.SolverMutationClaim,
                "mutation.retry", request.RetryCount);
            AddCount(errors, SectorQuietActivityEventErrorCode.TileMutationClaim,
                "mutation.tile", request.TileWriteCount);
        }

        private static void ValidateActivity(
            SectorActivityEventPlacementRequest request,
            ICollection<SectorQuietActivityEventError> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var projection in request.ActivityOpportunities)
            {
                if (projection == null || projection.Authority == null ||
                    string.IsNullOrEmpty(projection.OpportunityId))
                {
                    Add(errors, SectorQuietActivityEventErrorCode.MissingActivityAuthority,
                        "activityOpportunity", "Activity public opportunity is required.");
                    continue;
                }
                if (!ids.Add(projection.OpportunityId))
                    Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication,
                        projection.OpportunityId, "Activity opportunity ID is duplicated.");
                if (projection.MarkerCoordinate.X < 0 || projection.MarkerCoordinate.X >= 48 ||
                    projection.MarkerCoordinate.Y < 0 || projection.MarkerCoordinate.Y >= 32)
                    Add(errors, SectorQuietActivityEventErrorCode.ActivityOpportunityOutOfBounds,
                        projection.OpportunityId, "Activity marker coordinate is outside 48x32.");
                if (!request.QuietFillPlan.TryGetCell(
                        projection.SectorCoordinate, projection.MarkerCoordinate, out var markerCell) ||
                    !markerCell.ActivityEligible || markerCell.ProtectedNoWrite ||
                    markerCell.ReservedNoWrite || markerCell.PatternRendered)
                    Add(errors, SectorQuietActivityEventErrorCode.ActivityOpportunityOverlapsProtected,
                        projection.OpportunityId, "Activity marker must use eligible unprotected Quiet/Buffer evidence.");
                if (projection.Authority.Clearance == null ||
                    projection.Authority.Clearance.Coordinates.Any(coordinate =>
                        !request.QuietFillPlan.TryGetCell(projection.SectorCoordinate, coordinate, out var cell) ||
                        !cell.ActivityEligible || cell.ProtectedNoWrite || cell.ReservedNoWrite || cell.PatternRendered))
                    Add(errors, SectorQuietActivityEventErrorCode.ActivityOpportunityOverlapsProtected,
                        projection.OpportunityId, "Activity clearance overlaps no-write evidence.");
                if (request.ActivityCandidateIndex.GetCandidates(projection.OpportunityId).Count == 0)
                    Add(errors, SectorQuietActivityEventErrorCode.ActivityFrequencyRejected,
                        projection.OpportunityId, "MAP12 compatibility published no candidate.");
                if (string.IsNullOrEmpty(projection.RemovalSafetyIdentity) ||
                    !string.Equals(projection.RemovalSafetyIdentity,
                        projection.Authority.RemovalSafetyDigest, StringComparison.Ordinal))
                    Add(errors, SectorQuietActivityEventErrorCode.ActivityRemovalSafetyMissing,
                        projection.OpportunityId, "Removal-safety identity must be preserved.");
            }

            var projectedIds = new HashSet<string>(request.ActivityOpportunities.Select(value => value.OpportunityId), StringComparer.Ordinal);
            if (request.ActivityFrequencyPlan.Decisions.Any(value => !projectedIds.Contains(value.OpportunityId)))
                Add(errors, SectorQuietActivityEventErrorCode.SectorMismatch,
                    "activity.decisions", "MAP12 selected a decision outside projected opportunities.");
            foreach (var decision in request.ActivityFrequencyPlan.Decisions.Where(value => value.Strength == ActivityStrengthClass.Strong))
            {
                var policy = request.ActivityFrequencyPlan.Policy;
                if (decision.WorldStrongAfter > policy.MaxStrongPerWorld ||
                    decision.PatchStrongAfter > policy.MaxStrongPerPatch ||
                    decision.SectorStrongAfter > policy.MaxStrongPerSector)
                    Add(errors, SectorQuietActivityEventErrorCode.ActivityStrongCapViolation,
                        decision.OpportunityId, "MAP12 Strong cap evidence exceeded policy.");
            }
        }

        private static void ValidateEvents(
            SectorActivityEventPlacementRequest request,
            ICollection<SectorQuietActivityEventError> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var decisionIds = new HashSet<string>(request.EventAssignmentPlan.Decisions
                .Select(value => value.OpportunityId), StringComparer.Ordinal);
            foreach (var projection in request.EventOpportunities)
            {
                if (projection == null || projection.Authority == null ||
                    string.IsNullOrEmpty(projection.OpportunityId))
                {
                    Add(errors, SectorQuietActivityEventErrorCode.MissingEventAuthority,
                        "eventOpportunity", "Event public opportunity is required.");
                    continue;
                }
                if (!ids.Add(projection.OpportunityId))
                    Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication,
                        projection.OpportunityId, "Event opportunity ID is duplicated.");
                if (projection.MarkerCoordinate.X < 0 || projection.MarkerCoordinate.X >= 48 ||
                    projection.MarkerCoordinate.Y < 0 || projection.MarkerCoordinate.Y >= 32)
                    Add(errors, SectorQuietActivityEventErrorCode.EventOpportunityOutOfBounds,
                        projection.OpportunityId, "Event marker coordinate is outside 48x32.");
                if (!request.QuietFillPlan.TryGetCell(
                        projection.SectorCoordinate, projection.MarkerCoordinate, out var cell) ||
                    !cell.EventEligible || cell.ProtectedNoWrite || cell.ReservedNoWrite || cell.PatternRendered)
                    Add(errors, SectorQuietActivityEventErrorCode.EventOpportunityOverlapsProtected,
                        projection.OpportunityId, "Event marker must use eligible non-owning evidence.");
                var candidates = request.EventCandidateIndex.GetCandidates(projection.OpportunityId);
                if (candidates.Count(value => value.IsEmpty) != 1)
                    Add(errors, SectorQuietActivityEventErrorCode.MissingEmptyEvent,
                        projection.OpportunityId, "Exactly one MAP12 Empty candidate is required.");
                if (candidates.Count(value => !value.IsEmpty) == 0 || !decisionIds.Contains(projection.OpportunityId))
                    Add(errors, SectorQuietActivityEventErrorCode.EventAssignmentRejected,
                        projection.OpportunityId, "MAP12 Event assignment authority is incomplete.");
                if (!string.Equals(projection.Authority.ActivityFrequencyPlanDigest,
                        request.ActivityFrequencyPlan.CanonicalDigest, StringComparison.Ordinal))
                    Add(errors, SectorQuietActivityEventErrorCode.NonCanonicalPublication,
                        projection.OpportunityId, "Event opportunity must cite the consumed Activity plan digest.");
            }
            if (request.EventAssignmentPlan.Decisions.Count != request.EventOpportunities.Count)
                Add(errors, SectorQuietActivityEventErrorCode.SectorMismatch,
                    "event.decisions", "Every Event opportunity must publish one assigned or explicit Empty decision.");
        }

        private static SectorQuietActivityEventBuildResult Failure(
            IEnumerable<SectorQuietActivityEventError> errors) =>
            new SectorQuietActivityEventBuildResult(null, string.Empty, errors);

        private static void Add(
            ICollection<SectorQuietActivityEventError> errors,
            SectorQuietActivityEventErrorCode code,
            string subject,
            string detail) => errors.Add(new SectorQuietActivityEventError(code, subject, detail));

        private static void AddCount(
            ICollection<SectorQuietActivityEventError> errors,
            SectorQuietActivityEventErrorCode code,
            string subject,
            int count)
        {
            if (count != 0) Add(errors, code, subject, count.ToString(CultureInfo.InvariantCulture));
        }
    }
}
