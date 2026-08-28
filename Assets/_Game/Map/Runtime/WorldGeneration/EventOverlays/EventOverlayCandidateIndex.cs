using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.EventOverlays
{
    public sealed class EventOverlayCandidateIndex
    {
        private readonly ReadOnlyCollection<EventOverlayCandidate> candidates;
        private readonly ReadOnlyCollection<EventOverlayCompatibilityRejection> rejections;
        private readonly ReadOnlyDictionary<string, IReadOnlyList<EventOverlayCandidate>> byOpportunity;

        internal EventOverlayCandidateIndex(
            IEnumerable<EventOverlayCandidate> candidates,
            IEnumerable<EventOverlayCompatibilityRejection> rejections,
            string activityFrequencyPlanDigest,
            string canonicalDigest)
        {
            var candidateCopy = (candidates ?? Array.Empty<EventOverlayCandidate>())
                .OrderBy(value => value.OpportunityId, StringComparer.Ordinal)
                .ThenBy(value => value.EventId.Value, StringComparer.Ordinal)
                .ThenBy(value => value.CandidateKey, StringComparer.Ordinal).ToArray();
            var rejectionCopy = (rejections ?? Array.Empty<EventOverlayCompatibilityRejection>())
                .OrderBy(value => value.OpportunityId, StringComparer.Ordinal)
                .ThenBy(value => value.EventId.Value, StringComparer.Ordinal)
                .ThenBy(value => value.Code)
                .ThenBy(value => value.Path, StringComparer.Ordinal)
                .ThenBy(value => value.Detail, StringComparer.Ordinal).ToArray();
            this.candidates = new ReadOnlyCollection<EventOverlayCandidate>(candidateCopy);
            this.rejections = new ReadOnlyCollection<EventOverlayCompatibilityRejection>(rejectionCopy);
            var lookup = new SortedDictionary<string, IReadOnlyList<EventOverlayCandidate>>(StringComparer.Ordinal);
            foreach (var group in candidateCopy.GroupBy(value => value.OpportunityId, StringComparer.Ordinal))
                lookup[group.Key] = new ReadOnlyCollection<EventOverlayCandidate>(group.ToArray());
            byOpportunity = new ReadOnlyDictionary<string, IReadOnlyList<EventOverlayCandidate>>(lookup);
            ActivityFrequencyPlanDigest = activityFrequencyPlanDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public IReadOnlyList<EventOverlayCandidate> Candidates => candidates;
        public IReadOnlyList<EventOverlayCompatibilityRejection> Rejections => rejections;
        public int CandidateCount => candidates.Count;
        public int RejectionCount => rejections.Count;
        public int RngStreamCreationCount => 0;
        public int RngDrawCount => 0;
        public string ActivityFrequencyPlanDigest { get; }
        public string CanonicalDigest { get; }

        public IReadOnlyList<EventOverlayCandidate> GetCandidates(string opportunityId)
            => opportunityId != null && byOpportunity.TryGetValue(opportunityId, out var values)
                ? values : Array.Empty<EventOverlayCandidate>();
    }

    public sealed class EventOverlayCandidateIndexRequest
    {
        private readonly ReadOnlyCollection<EventOverlayAssignmentProfile> profiles;
        private readonly ReadOnlyCollection<EventOverlayOpportunity> opportunities;

        public EventOverlayCandidateIndexRequest(
            IEnumerable<EventOverlayAssignmentProfile> profiles,
            IEnumerable<EventOverlayOpportunity> opportunities,
            ActivityFrequencyPlan activityFrequencyPlan)
            : this(profiles, opportunities, activityFrequencyPlan == null ? string.Empty : activityFrequencyPlan.CanonicalDigest)
        {
            ActivityFrequencyPlan = activityFrequencyPlan;
        }

        public EventOverlayCandidateIndexRequest(
            IEnumerable<EventOverlayAssignmentProfile> profiles,
            IEnumerable<EventOverlayOpportunity> opportunities,
            string expectedActivityFrequencyPlanDigest)
        {
            this.profiles = new ReadOnlyCollection<EventOverlayAssignmentProfile>(
                profiles == null ? Array.Empty<EventOverlayAssignmentProfile>() : profiles.ToArray());
            this.opportunities = new ReadOnlyCollection<EventOverlayOpportunity>(
                opportunities == null ? Array.Empty<EventOverlayOpportunity>() : opportunities.ToArray());
            ExpectedActivityFrequencyPlanDigest = expectedActivityFrequencyPlanDigest ?? string.Empty;
        }

        public IReadOnlyList<EventOverlayAssignmentProfile> Profiles => profiles;
        public IReadOnlyList<EventOverlayOpportunity> Opportunities => opportunities;
        public ActivityFrequencyPlan ActivityFrequencyPlan { get; }
        public string ExpectedActivityFrequencyPlanDigest { get; }
    }

    public sealed class EventOverlayCandidateIndexResult
    {
        private readonly ReadOnlyCollection<EventOverlayAssignmentError> errors;

        internal EventOverlayCandidateIndexResult(
            EventOverlayCandidateIndex index,
            IEnumerable<EventOverlayAssignmentError> errors)
        {
            Index = index;
            this.errors = new ReadOnlyCollection<EventOverlayAssignmentError>(
                EventOverlayAssignmentCanonical.SortErrors(errors).ToArray());
        }

        public bool Success => Index != null && errors.Count == 0;
        public EventOverlayCandidateIndex Index { get; }
        public IReadOnlyList<EventOverlayAssignmentError> Errors => errors;
        public int RngStreamCreationCount => 0;
        public int RngDrawCount => 0;
    }

    public static class EventOverlayCandidateIndexCompiler
    {
        private const string RulesetVersion = "MAP12_04_EVENT_CANDIDATE_INDEX_V1";

        public static EventOverlayCandidateIndexResult Compile(EventOverlayCandidateIndexRequest request)
        {
            var errors = new List<EventOverlayAssignmentError>();
            if (request == null)
            {
                Add(errors, EventOverlayAssignmentErrorCode.MissingInput, "request", "Compile request is required.");
                return Failure(errors);
            }
            if (request.Profiles.Count == 0)
                Add(errors, EventOverlayAssignmentErrorCode.MissingInput, "profiles", "At least one Event profile is required.");
            if (request.Opportunities.Count == 0)
                Add(errors, EventOverlayAssignmentErrorCode.MissingInput, "opportunities", "At least one marker opportunity is required.");
            ValidateDigest(request.ExpectedActivityFrequencyPlanDigest, "activityFrequencyPlan.digest", errors,
                EventOverlayAssignmentErrorCode.ArtifactDigestMismatch);
            if (request.ActivityFrequencyPlan != null && !string.Equals(
                    request.ActivityFrequencyPlan.CanonicalDigest,
                    request.ExpectedActivityFrequencyPlanDigest, StringComparison.Ordinal))
                Add(errors, EventOverlayAssignmentErrorCode.ArtifactDigestMismatch,
                    "activityFrequencyPlan.digest", "The supplied MAP12_03 plan digest changed.");

            for (var index = 0; index < request.Profiles.Count; index++)
                ValidateProfile(request.Profiles[index], "profiles[" + Number(index) + "]", errors);
            var ordinalSet = new HashSet<int>();
            var opportunityIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < request.Opportunities.Count; index++)
                ValidateOpportunity(request.Opportunities[index], request, "opportunities[" + Number(index) + "]",
                    ordinalSet, opportunityIds, errors);
            if (errors.Count != 0) return Failure(errors);

            var candidates = new List<EventOverlayCandidate>();
            var rejections = new List<EventOverlayCompatibilityRejection>();
            foreach (var opportunity in request.Opportunities.OrderBy(value => value.OpportunityId, StringComparer.Ordinal))
            foreach (var profile in request.Profiles.OrderBy(value => value.Contract.Id.Value, StringComparer.Ordinal))
            {
                var pairRejections = Evaluate(opportunity, profile).ToArray();
                if (pairRejections.Length == 0)
                    candidates.Add(new EventOverlayCandidate(opportunity, profile, CandidateKey(opportunity, profile)));
                else
                    rejections.AddRange(pairRejections);
            }

            foreach (var opportunity in request.Opportunities.OrderBy(value => value.OpportunityId, StringComparer.Ordinal))
            {
                var values = candidates.Where(value => value.OpportunityId == opportunity.OpportunityId).ToArray();
                var emptyCount = values.Count(value => value.IsEmpty);
                if (emptyCount == 0)
                    Add(errors, EventOverlayAssignmentErrorCode.MissingEmptyVariant,
                        "opportunities/" + opportunity.OpportunityId, "Exactly one compatible Empty variant is required.");
                else if (emptyCount > 1)
                    Add(errors, EventOverlayAssignmentErrorCode.DuplicateEmptyVariant,
                        "opportunities/" + opportunity.OpportunityId, "More than one compatible Empty variant was found.");
                if (!values.Any(value => !value.IsEmpty))
                    Add(errors, EventOverlayAssignmentErrorCode.InvalidOpportunity,
                        "opportunities/" + opportunity.OpportunityId, "At least one compatible non-empty Event is required.");
            }

            var duplicateKeys = candidates.GroupBy(value => value.CandidateKey, StringComparer.Ordinal)
                .Where(group => group.Count() != 1).Select(group => group.Key).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (duplicateKeys.Length != 0)
                Add(errors, EventOverlayAssignmentErrorCode.NonCanonicalPublication, "candidates",
                    "Duplicate candidate keys: " + string.Join(",", duplicateKeys));
            if (errors.Count != 0) return Failure(errors);

            var digest = ComputeDigest(request, candidates, rejections);
            return new EventOverlayCandidateIndexResult(
                new EventOverlayCandidateIndex(candidates, rejections,
                    request.ExpectedActivityFrequencyPlanDigest, digest),
                Array.Empty<EventOverlayAssignmentError>());
        }

        private static void ValidateProfile(
            EventOverlayAssignmentProfile profile,
            string path,
            ICollection<EventOverlayAssignmentError> errors)
        {
            if (profile == null || profile.Contract == null)
            {
                Add(errors, EventOverlayAssignmentErrorCode.InvalidProfile, path, "Profile and EventOverlayContract are required.");
                return;
            }
            var contract = profile.Contract;
            if (!EventOverlayAssignmentCanonical.IsId(contract.Id.Value, "EVT_") ||
                !EventOverlayAssignmentCanonical.IsId(contract.TerrainClusterId.Value, "TC_"))
                Add(errors, EventOverlayAssignmentErrorCode.InvalidProfile, path + ".identity", "Stable Event and TerrainCluster IDs are required.");
            ValidateDigest(profile.ContractDigest, path + ".contractDigest", errors,
                EventOverlayAssignmentErrorCode.ArtifactDigestMismatch);
            if (EventOverlayAssignmentCanonical.IsDigest(profile.ContractDigest) &&
                !string.Equals(EventOverlayCanonicalDigest.Compute(contract), profile.ContractDigest, StringComparison.Ordinal))
                Add(errors, EventOverlayAssignmentErrorCode.ArtifactDigestMismatch, path + ".contractDigest",
                    "Contract digest does not match the supplied EventOverlayContract.");

            var assignments = contract.Assignments ?? Array.Empty<EventMarkerAssignment>();
            var duplicate = assignments.GroupBy(value => value == null ? string.Empty : value.TargetMarkerId.Value, StringComparer.Ordinal)
                .Any(group => group.Key.Length == 0 || group.Count() != 1);
            if (duplicate)
                Add(errors, EventOverlayAssignmentErrorCode.DuplicateMarker, path + ".assignments",
                    "Marker assignment identities must be non-empty and unique.");
            if (assignments.Any(value => value == null || !OperationMatches(contract.Kind, value.Operation)))
                Add(errors, EventOverlayAssignmentErrorCode.InvalidMarkerOperation, path + ".assignments",
                    "Every operation must match the exact Event kind matrix.");

            if (contract.Kind == EventOverlayKind.Empty)
            {
                if (assignments.Count != 0 || profile.Weight != 0 || profile.MinimumProgressionGap != 0)
                    Add(errors, EventOverlayAssignmentErrorCode.InvalidProfile, path + ".empty",
                        "Empty requires zero assignments, weight, and cooldown.");
            }
            else if (assignments.Count == 0 || profile.Weight < 1 || profile.Weight > 10000 || profile.MinimumProgressionGap < 0)
            {
                Add(errors, EventOverlayAssignmentErrorCode.InvalidProfile, path + ".selection",
                    "Non-empty Events require assignments, weight 1..10000, and a non-negative cooldown.");
            }
            if (profile.CompatibleBiomes.Count == 0 || profile.CompatibleBiomes.Any(value => !value.IsDefined) ||
                profile.CompatiblePacingRoles.Count == 0 || profile.CompatiblePacingRoles.Any(value => value == PacingRole.None) ||
                profile.CompatibleAccessClasses.Count == 0 || profile.CompatibleAccessClasses.Any(value => value == AccessClass.Unspecified))
                Add(errors, EventOverlayAssignmentErrorCode.InvalidProfile, path + ".compatibility",
                    "Biome, pacing, and access compatibility sets must be published and non-empty.");
            if (profile.ReferencedActivityId.HasValue &&
                !EventOverlayAssignmentCanonical.IsId(profile.ReferencedActivityId.Value.Value, "ACT_"))
                Add(errors, EventOverlayAssignmentErrorCode.InvalidProfile, path + ".activityId", "Referenced Activity ID is invalid.");
            if (contract.ActivityStructureId != profile.ReferencedActivityId)
                Add(errors, EventOverlayAssignmentErrorCode.IdentityMismatch, path + ".activityId",
                    "Profile Activity identity must match the EventOverlayContract.");
        }

        private static void ValidateOpportunity(
            EventOverlayOpportunity opportunity,
            EventOverlayCandidateIndexRequest request,
            string path,
            ISet<int> ordinals,
            ISet<string> ids,
            ICollection<EventOverlayAssignmentError> errors)
        {
            if (opportunity == null)
            {
                Add(errors, EventOverlayAssignmentErrorCode.InvalidOpportunity, path, "Opportunity cannot be null.");
                return;
            }
            if (!EventOverlayAssignmentCanonical.IsId(opportunity.OpportunityId, "EVENT_OPP_") ||
                !ids.Add(opportunity.OpportunityId) || !opportunity.PatchId.IsValid ||
                !opportunity.Biome.IsDefined || opportunity.PacingRole == PacingRole.None ||
                opportunity.AccessClass == AccessClass.Unspecified ||
                !EventOverlayAssignmentCanonical.IsId(opportunity.TerrainClusterId.Value, "TC_") ||
                opportunity.ProgressionOrdinal < 0 || !ordinals.Add(opportunity.ProgressionOrdinal))
                Add(errors, EventOverlayAssignmentErrorCode.InvalidOpportunity, path + ".identity",
                    "Opportunity IDs, ownership, compatibility, and non-negative progression ordinals must be unique and valid.");
            if (!string.Equals(opportunity.ActivityFrequencyPlanDigest,
                    request.ExpectedActivityFrequencyPlanDigest, StringComparison.Ordinal))
                Add(errors, EventOverlayAssignmentErrorCode.ArtifactDigestMismatch, path + ".activityFrequencyPlanDigest",
                    "Opportunity does not reference the exact MAP12_03 plan.");
            if (opportunity.SelectedActivityId.HasValue &&
                !EventOverlayAssignmentCanonical.IsId(opportunity.SelectedActivityId.Value.Value, "ACT_"))
                Add(errors, EventOverlayAssignmentErrorCode.IdentityMismatch, path + ".selectedActivityId", "Selected Activity ID is invalid.");

            if (opportunity.Markers.Count == 0)
                Add(errors, EventOverlayAssignmentErrorCode.MissingMarker, path + ".markers", "At least one marker target is required.");
            var markerIds = new HashSet<string>(StringComparer.Ordinal);
            for (var markerIndex = 0; markerIndex < opportunity.Markers.Count; markerIndex++)
            {
                var marker = opportunity.Markers[markerIndex];
                var markerPath = path + ".markers[" + Number(markerIndex) + "]";
                if (marker == null || !EventOverlayAssignmentCanonical.IsId(marker.MarkerId.Value, "MARKER_") ||
                    !markerIds.Add(marker == null ? string.Empty : marker.MarkerId.Value))
                {
                    Add(errors, EventOverlayAssignmentErrorCode.DuplicateMarker, markerPath,
                        "Marker evidence must contain unique stable marker IDs.");
                    continue;
                }
                if (!Enum.IsDefined(typeof(EventMarkerTargetSourceKind), marker.SourceKind) ||
                    !EventOverlayAssignmentCanonical.IsToken(marker.SourceOwnerId) || string.IsNullOrEmpty(marker.OwningSlotKind))
                    Add(errors, EventOverlayAssignmentErrorCode.InvalidOpportunity, markerPath + ".source", "Marker source evidence is invalid.");
                if (marker.SourceCoordinate != marker.CompiledCoordinate ||
                    !string.Equals(marker.UnderlyingCanvasValueBefore, marker.UnderlyingCanvasValueAfter, StringComparison.Ordinal) ||
                    !SameDigest(marker.StaticShellDigestBefore, marker.StaticShellDigestAfter) ||
                    !SameDigest(marker.ProtectionDigestBefore, marker.ProtectionDigestAfter))
                    Add(errors, EventOverlayAssignmentErrorCode.NonMarkerMutation, markerPath,
                        "Coordinates, Canvas value, Static Shell, and protection evidence must remain unchanged.");
                if (marker.HasNonMarkerMutation)
                    Add(errors, EventOverlayAssignmentErrorCode.NonMarkerMutation, markerPath + ".mutationCounts",
                        "Geometry/collision/route/access/pacing/envelope writes must all be zero.");
                if (!string.Equals(marker.PersistenceDigestBefore, marker.PersistenceDigestAfter, StringComparison.Ordinal))
                    Add(errors, EventOverlayAssignmentErrorCode.PersistenceProvenanceMismatch, markerPath + ".persistence",
                        "Persistence provenance must remain unchanged.");
            }
            ValidateSpecial(opportunity, path, errors);
        }

        private static void ValidateSpecial(
            EventOverlayOpportunity opportunity,
            string path,
            ICollection<EventOverlayAssignmentError> errors)
        {
            if (!Enum.IsDefined(typeof(EventSpecialOverlapKind), opportunity.SpecialOverlapKind))
            {
                Add(errors, EventOverlayAssignmentErrorCode.InvalidSpecialOverlap, path + ".special", "Unknown overlap kind.");
                return;
            }
            if (opportunity.SpecialOverlapKind == EventSpecialOverlapKind.None)
            {
                if (opportunity.SpecialRegion != null || opportunity.SpecialRegionSlotId.Value.Length != 0 ||
                    opportunity.SpecialRegionDigest.Length != 0)
                    Add(errors, EventOverlayAssignmentErrorCode.InvalidSpecialOverlap, path + ".special",
                        "No-overlap opportunities cannot carry SpecialRegion ownership.");
                return;
            }
            var region = opportunity.SpecialRegion;
            if (region == null || !EventOverlayAssignmentCanonical.IsDigest(opportunity.SpecialRegionDigest) ||
                !string.Equals(opportunity.SpecialRegionDigest, region == null ? string.Empty : SpecialRegionCanonicalDigest.Compute(region),
                    StringComparison.Ordinal))
            {
                Add(errors, EventOverlayAssignmentErrorCode.InvalidSpecialOverlap, path + ".special.digest",
                    "Replaceable overlap requires the exact SpecialRegion contract and digest.");
                return;
            }
            var slots = region.Slots.Where(value => value != null && value.Id == opportunity.SpecialRegionSlotId).ToArray();
            if (slots.Length != 1)
            {
                Add(errors, EventOverlayAssignmentErrorCode.InvalidSpecialOverlap, path + ".special.slot",
                    "The exact replaceable slot must resolve once.");
                return;
            }
            var slot = slots[0];
            if (slot.Kind == SpecialRegionSlotKind.Facility || slot.Kind == SpecialRegionSlotKind.Enemy ||
                slot.Kind == SpecialRegionSlotKind.Entry || slot.Kind == SpecialRegionSlotKind.Return)
                Add(errors, EventOverlayAssignmentErrorCode.InvalidSpecialOverlap, path + ".special.slot.kind",
                    "Only Npc, Reward, and Event replaceable slots may host an Event overlay.");
            if (region.FixedShell.Any(value => value != null && value.SectorOffset == slot.SectorOffset && value.Tile == slot.Tile))
                Add(errors, EventOverlayAssignmentErrorCode.FixedShellOverlap, path + ".special.slot",
                    "Replaceable slot overlaps the Fixed Shell.");
            var marker = opportunity.Markers.SingleOrDefault(value => value != null &&
                value.SourceKind == EventMarkerTargetSourceKind.SpecialRegion &&
                string.Equals(value.SourceOwnerId, region.Id.Value, StringComparison.Ordinal) &&
                value.CompiledCoordinate == slot.Tile);
            if (marker == null)
                Add(errors, EventOverlayAssignmentErrorCode.InvalidSpecialOverlap, path + ".special.marker",
                    "Special marker must preserve exact region, slot, and coordinate ownership.");
            else if (slot.PersistenceKey != marker.PersistenceKey ||
                     (marker.PersistenceKey.Value.Length != 0 &&
                      string.Equals(marker.PersistenceKey.Value, "SR_STATE_" + opportunity.OpportunityId, StringComparison.Ordinal)))
                Add(errors, EventOverlayAssignmentErrorCode.PersistenceProvenanceMismatch, path + ".special.persistence",
                    "The Event overlay cannot become the persistence-key owner.");
            if (region.Ports.Any(value => value != null && value.SectorOffset == slot.SectorOffset && value.Tile == slot.Tile))
                Add(errors, EventOverlayAssignmentErrorCode.InvalidSpecialOverlap, path + ".special.port",
                    "Event assignment cannot mutate or occupy an Entry/Return port coordinate.");
        }

        private static IEnumerable<EventOverlayCompatibilityRejection> Evaluate(
            EventOverlayOpportunity opportunity,
            EventOverlayAssignmentProfile profile)
        {
            var result = new List<EventOverlayCompatibilityRejection>();
            void Reject(EventOverlayCompatibilityRejectionCode code, string path, string detail)
                => result.Add(new EventOverlayCompatibilityRejection(
                    opportunity.OpportunityId, profile.Contract.Id, code, path, detail));

            if (opportunity.TerrainClusterId != profile.Contract.TerrainClusterId)
                Reject(EventOverlayCompatibilityRejectionCode.TerrainClusterMismatch, "terrainClusterId", opportunity.TerrainClusterId.Value);
            if (!profile.CompatibleBiomes.Contains(opportunity.Biome))
                Reject(EventOverlayCompatibilityRejectionCode.BiomeMismatch, "biome", opportunity.Biome.CanonicalId);
            if (!profile.CompatiblePacingRoles.Contains(opportunity.PacingRole))
                Reject(EventOverlayCompatibilityRejectionCode.PacingRoleMismatch, "pacingRole", Number((int)opportunity.PacingRole));
            if (!profile.CompatibleAccessClasses.Contains(opportunity.AccessClass))
                Reject(EventOverlayCompatibilityRejectionCode.AccessClassMismatch, "accessClass", Number((int)opportunity.AccessClass));
            if (profile.ReferencedActivityId.HasValue &&
                (!opportunity.SelectedActivityId.HasValue || opportunity.SelectedActivityId.Value != profile.ReferencedActivityId.Value))
                Reject(EventOverlayCompatibilityRejectionCode.ActivityMismatch, "selectedActivityId",
                    opportunity.SelectedActivityId.HasValue ? opportunity.SelectedActivityId.Value.Value : string.Empty);

            var markerLookup = opportunity.Markers.Where(value => value != null)
                .GroupBy(value => value.MarkerId.Value, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            foreach (var assignment in profile.Contract.Assignments)
            {
                if (!markerLookup.TryGetValue(assignment.TargetMarkerId.Value, out var evidence))
                    Reject(EventOverlayCompatibilityRejectionCode.MissingMarker,
                        "assignments/" + assignment.TargetMarkerId.Value, "Marker target does not exist in the opportunity.");
                else if (evidence.Length != 1)
                    Reject(EventOverlayCompatibilityRejectionCode.DuplicateMarker,
                        "assignments/" + assignment.TargetMarkerId.Value, "Marker target evidence is not unique.");
                if (!OperationMatches(profile.Contract.Kind, assignment.Operation))
                    Reject(EventOverlayCompatibilityRejectionCode.InvalidMarkerOperation,
                        "assignments/" + assignment.TargetMarkerId.Value, assignment.Operation.ToString());
            }

            if (opportunity.SpecialOverlapKind == EventSpecialOverlapKind.ReplaceableSlot && opportunity.SpecialRegion != null)
            {
                var slot = opportunity.SpecialRegion.Slots.SingleOrDefault(value => value != null && value.Id == opportunity.SpecialRegionSlotId);
                if (slot != null && !SpecialKindMatches(profile.Contract, slot.Kind))
                    Reject(EventOverlayCompatibilityRejectionCode.InvalidSpecialOverlap,
                        "special.slot.kind", profile.Contract.Kind + "->" + slot.Kind);
            }
            return result;
        }

        private static bool SpecialKindMatches(EventOverlayContract contract, SpecialRegionSlotKind slotKind)
        {
            switch (contract.Kind)
            {
                case EventOverlayKind.Npc:
                    return slotKind == SpecialRegionSlotKind.Npc && contract.Assignments.All(value => value.Operation == EventMarkerOperation.SpawnNpc);
                case EventOverlayKind.Reward:
                    return slotKind == SpecialRegionSlotKind.Reward && contract.Assignments.All(value => value.Operation == EventMarkerOperation.SpawnReward);
                case EventOverlayKind.State:
                case EventOverlayKind.Cosmetic:
                    return slotKind == SpecialRegionSlotKind.Event && contract.Assignments.All(value =>
                        value.Operation == EventMarkerOperation.EnableMarker ||
                        value.Operation == EventMarkerOperation.DisableMarker ||
                        value.Operation == EventMarkerOperation.SetState);
                case EventOverlayKind.Empty:
                    return true;
                default:
                    return false;
            }
        }

        private static bool OperationMatches(EventOverlayKind kind, EventMarkerOperation operation)
        {
            switch (kind)
            {
                case EventOverlayKind.Npc: return operation == EventMarkerOperation.SpawnNpc;
                case EventOverlayKind.Reward: return operation == EventMarkerOperation.SpawnReward;
                case EventOverlayKind.State: return operation == EventMarkerOperation.SetState;
                case EventOverlayKind.Cosmetic:
                    return operation == EventMarkerOperation.EnableMarker || operation == EventMarkerOperation.DisableMarker;
                case EventOverlayKind.Empty: return false;
                default: return false;
            }
        }

        private static string CandidateKey(EventOverlayOpportunity opportunity, EventOverlayAssignmentProfile profile)
            => opportunity.OpportunityId + "|" + profile.Contract.Id.Value + "|" + profile.ContractDigest;

        private static string ComputeDigest(
            EventOverlayCandidateIndexRequest request,
            IEnumerable<EventOverlayCandidate> candidates,
            IEnumerable<EventOverlayCompatibilityRejection> rejections)
        {
            var material = new StringBuilder();
            EventOverlayAssignmentCanonical.Append(material, "RULESET", RulesetVersion);
            EventOverlayAssignmentCanonical.Append(material, "ACTIVITY_PLAN", request.ExpectedActivityFrequencyPlanDigest);
            foreach (var profile in request.Profiles.OrderBy(value => value.Contract.Id.Value, StringComparer.Ordinal))
                EventOverlayAssignmentCanonical.Append(material, "PROFILE", profile.Contract.Id.Value,
                    Number((int)profile.Contract.Kind), profile.ContractDigest, Number(profile.Weight),
                    Number(profile.MinimumProgressionGap),
                    string.Join(",", profile.CompatibleBiomes.Select(value => value.CanonicalId)),
                    string.Join(",", profile.CompatiblePacingRoles.Select(value => Number((int)value))),
                    string.Join(",", profile.CompatibleAccessClasses.Select(value => Number((int)value))),
                    profile.ReferencedActivityId.HasValue ? profile.ReferencedActivityId.Value.Value : string.Empty);
            foreach (var opportunity in request.Opportunities.OrderBy(value => value.OpportunityId, StringComparer.Ordinal))
            {
                EventOverlayAssignmentCanonical.Append(material, "OPPORTUNITY", opportunity.OpportunityId,
                    Number(opportunity.Sector.X), Number(opportunity.Sector.Y), opportunity.PatchId.Value,
                    Number(opportunity.ProgressionOrdinal), opportunity.Biome.CanonicalId,
                    Number((int)opportunity.PacingRole), Number((int)opportunity.AccessClass),
                    opportunity.TerrainClusterId.Value,
                    opportunity.SelectedActivityId.HasValue ? opportunity.SelectedActivityId.Value.Value : string.Empty,
                    opportunity.ActivityFrequencyPlanDigest, Number((int)opportunity.SpecialOverlapKind),
                    opportunity.SpecialRegion == null ? string.Empty : opportunity.SpecialRegion.Id.Value,
                    opportunity.SpecialRegionDigest, opportunity.SpecialRegionSlotId.Value);
                foreach (var marker in opportunity.Markers)
                    EventOverlayAssignmentCanonical.Append(material, "MARKER", opportunity.OpportunityId,
                        marker.MarkerId.Value, Number((int)marker.SourceKind), marker.SourceOwnerId,
                        Number(marker.SourceCoordinate.X), Number(marker.SourceCoordinate.Y),
                        Number(marker.CompiledCoordinate.X), Number(marker.CompiledCoordinate.Y), marker.OwningSlotKind,
                        marker.UnderlyingCanvasValueBefore, marker.UnderlyingCanvasValueAfter,
                        marker.StaticShellDigestBefore, marker.StaticShellDigestAfter,
                        marker.ProtectionDigestBefore, marker.ProtectionDigestAfter,
                        marker.PersistenceKey.Value, marker.PersistenceDigestBefore, marker.PersistenceDigestAfter,
                        Number(marker.GeometryMutationCount), Number(marker.CollisionMutationCount),
                        Number(marker.RouteMutationCount), Number(marker.AccessMutationCount),
                        Number(marker.PacingMutationCount), Number(marker.EnvelopeMutationCount));
            }
            foreach (var candidate in candidates.OrderBy(value => value.CandidateKey, StringComparer.Ordinal))
                EventOverlayAssignmentCanonical.Append(material, "CANDIDATE", candidate.CandidateKey);
            foreach (var rejection in rejections.OrderBy(value => value.OpportunityId, StringComparer.Ordinal)
                         .ThenBy(value => value.EventId.Value, StringComparer.Ordinal).ThenBy(value => value.Code))
                EventOverlayAssignmentCanonical.Append(material, "REJECTION", rejection.OpportunityId,
                    rejection.EventId.Value, Number((int)rejection.Code), rejection.Path, rejection.Detail);
            return EventOverlayAssignmentCanonical.Sha256(material.ToString());
        }

        private static bool SameDigest(string before, string after)
            => EventOverlayAssignmentCanonical.IsDigest(before) && string.Equals(before, after, StringComparison.Ordinal);

        private static void ValidateDigest(string value, string path,
            ICollection<EventOverlayAssignmentError> errors, EventOverlayAssignmentErrorCode code)
        {
            if (!EventOverlayAssignmentCanonical.IsDigest(value)) Add(errors, code, path, "A lowercase SHA-256 digest is required.");
        }

        private static EventOverlayCandidateIndexResult Failure(IEnumerable<EventOverlayAssignmentError> errors)
            => new EventOverlayCandidateIndexResult(null, errors);

        private static void Add(ICollection<EventOverlayAssignmentError> errors,
            EventOverlayAssignmentErrorCode code, string path, string detail)
            => errors.Add(new EventOverlayAssignmentError(code, path, detail));

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    internal static class EventOverlayAssignmentCanonical
    {
        public static IEnumerable<EventOverlayAssignmentError> SortErrors(IEnumerable<EventOverlayAssignmentError> errors)
            => (errors ?? Array.Empty<EventOverlayAssignmentError>()).Where(value => value != null)
                .OrderBy(value => value.Code).ThenBy(value => value.Path, StringComparer.Ordinal)
                .ThenBy(value => value.Detail, StringComparer.Ordinal);

        public static bool IsDigest(string value)
            => value != null && value.Length == 64 && value.All(character =>
                (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));

        public static bool IsId(string value, string prefix)
            => value != null && value.StartsWith(prefix, StringComparison.Ordinal) && IsToken(value);

        public static bool IsToken(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            foreach (var character in value)
                if ((character < 'A' || character > 'Z') && (character < '0' || character > '9') && character != '_')
                    return false;
            return true;
        }

        public static void Append(StringBuilder builder, params string[] values)
        {
            foreach (var value in values)
            {
                var normalized = value ?? string.Empty;
                builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(normalized).Append('|');
            }
            builder.Append('\n');
        }

        public static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }
    }
}
