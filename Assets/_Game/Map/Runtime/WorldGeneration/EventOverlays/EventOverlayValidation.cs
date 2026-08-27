using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.EventOverlays
{
    public enum EventOverlayValidationErrorCode
    {
        MissingInput = 1,
        InvalidId = 2,
        InvalidShellReference = 3,
        InvalidOverlayKind = 4,
        InvalidMarker = 5,
        InvalidMarkerOperation = 6,
        NonMarkerMutation = 7,
        NonEmptyWithoutAssignment = 8,
        EmptyWithAssignment = 9,
    }

    public sealed class EventOverlayValidationError :
        IEquatable<EventOverlayValidationError>, IComparable<EventOverlayValidationError>
    {
        public EventOverlayValidationError(EventOverlayValidationErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public EventOverlayValidationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(EventOverlayValidationError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(EventOverlayValidationError other)
        {
            return other != null && Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as EventOverlayValidationError);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class EventOverlayValidationResult
    {
        private readonly ReadOnlyCollection<EventOverlayValidationError> errors;

        internal EventOverlayValidationResult(
            EventOverlayContract contract,
            IEnumerable<EventOverlayValidationError> errors,
            string canonicalDigest)
        {
            var copy = errors.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<EventOverlayValidationError>(copy);
            Contract = copy.Length == 0 ? contract : null;
            CanonicalDigest = copy.Length == 0 ? canonicalDigest ?? string.Empty : string.Empty;
        }

        public bool IsValid => Contract != null && errors.Count == 0;
        public EventOverlayContract Contract { get; }
        public IReadOnlyList<EventOverlayValidationError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class EventOverlayValidator
    {
        public static EventOverlayValidationResult Validate(
            EventOverlayContract contract,
            TerrainClusterContract staticShell,
            ActivityStructureContract activity,
            IEnumerable<EventMarkerId> knownMarkerIds,
            EventOverlayRemovalEvidence removalEvidence)
        {
            var errors = new List<EventOverlayValidationError>();
            if (contract == null)
            {
                Add(errors, EventOverlayValidationErrorCode.MissingInput, "contract", "Contract is required.");
                return new EventOverlayValidationResult(null, errors, string.Empty);
            }

            if (!IsStableId(contract.Id.Value, "EVT_"))
                Add(errors, EventOverlayValidationErrorCode.InvalidId, "id", contract.Id.Value);
            if (contract.Kind < EventOverlayKind.Npc || contract.Kind > EventOverlayKind.Empty)
                Add(errors, EventOverlayValidationErrorCode.InvalidOverlayKind, "kind", contract.Kind.ToString());

            var shellValidation = ValidateShell(contract, staticShell, errors);
            var activityValidation = ValidateActivityReference(contract, staticShell, activity, errors);
            ValidateAssignments(contract, knownMarkerIds, errors);
            ValidateRemovalEvidence(contract, shellValidation, activityValidation, removalEvidence, errors);

            return errors.Count == 0
                ? new EventOverlayValidationResult(contract, errors, EventOverlayCanonicalDigest.Compute(contract))
                : new EventOverlayValidationResult(null, errors, string.Empty);
        }

        private static TerrainClusterValidationResult ValidateShell(
            EventOverlayContract contract,
            TerrainClusterContract staticShell,
            ICollection<EventOverlayValidationError> errors)
        {
            if (staticShell == null)
            {
                Add(errors, EventOverlayValidationErrorCode.InvalidShellReference, "staticShell", "TerrainCluster is required.");
                return null;
            }

            var allowlist = staticShell.Footprint != null && staticShell.Footprint.ActiveChunks.Count == 6
                ? new[] { staticShell.Id }
                : Array.Empty<TerrainClusterId>();
            var result = TerrainClusterContractValidator.Validate(staticShell, allowlist);
            if (!result.IsValid || contract.TerrainClusterId != staticShell.Id)
                Add(errors, EventOverlayValidationErrorCode.InvalidShellReference, "staticShell.id",
                    contract.TerrainClusterId.Value + "|" + staticShell.Id.Value);
            return result;
        }

        private static ActivityValidationResult ValidateActivityReference(
            EventOverlayContract contract,
            TerrainClusterContract staticShell,
            ActivityStructureContract activity,
            ICollection<EventOverlayValidationError> errors)
        {
            if (!contract.ActivityStructureId.HasValue)
            {
                if (activity != null)
                    Add(errors, EventOverlayValidationErrorCode.InvalidShellReference, "activity",
                        "An unreferenced ActivityStructure cannot be supplied.");
                return null;
            }

            if (activity == null || activity.Id != contract.ActivityStructureId.Value)
            {
                Add(errors, EventOverlayValidationErrorCode.InvalidShellReference, "activity.id",
                    contract.ActivityStructureId.Value.Value);
                return null;
            }

            var result = ActivityContractValidator.Validate(activity, staticShell);
            if (!result.IsValid)
                Add(errors, EventOverlayValidationErrorCode.InvalidShellReference, "activity.contract",
                    contract.ActivityStructureId.Value.Value);
            return result;
        }

        private static void ValidateAssignments(
            EventOverlayContract contract,
            IEnumerable<EventMarkerId> knownMarkerIds,
            ICollection<EventOverlayValidationError> errors)
        {
            if (contract.Kind == EventOverlayKind.Empty && contract.Assignments.Count != 0)
                Add(errors, EventOverlayValidationErrorCode.EmptyWithAssignment, "assignments",
                    "Empty overlay must have zero assignments.");
            if (contract.Kind != EventOverlayKind.Empty && contract.Assignments.Count == 0)
                Add(errors, EventOverlayValidationErrorCode.NonEmptyWithoutAssignment, "assignments",
                    "Non-empty overlay requires at least one assignment.");

            var known = new HashSet<EventMarkerId>(knownMarkerIds ?? Array.Empty<EventMarkerId>());
            var targets = new HashSet<EventMarkerId>();
            foreach (var assignment in contract.Assignments)
            {
                if (assignment == null)
                {
                    Add(errors, EventOverlayValidationErrorCode.InvalidMarker, "assignments", "Assignment is required.");
                    continue;
                }

                var path = "assignments[" + assignment.TargetMarkerId.Value + "]";
                if (!IsStableId(assignment.TargetMarkerId.Value, "MARKER_") ||
                    !known.Contains(assignment.TargetMarkerId) || !targets.Add(assignment.TargetMarkerId))
                {
                    Add(errors, EventOverlayValidationErrorCode.InvalidMarker, path,
                        "A unique existing TerrainCluster/Activity marker is required.");
                }

                if (!IsStableToken(assignment.PayloadId) || !IsCompatible(contract.Kind, assignment.Operation))
                    Add(errors, EventOverlayValidationErrorCode.InvalidMarkerOperation, path,
                        assignment.Operation + "|" + assignment.PayloadId);
            }
        }

        private static void ValidateRemovalEvidence(
            EventOverlayContract contract,
            TerrainClusterValidationResult shellValidation,
            ActivityValidationResult activityValidation,
            EventOverlayRemovalEvidence evidence,
            ICollection<EventOverlayValidationError> errors)
        {
            if (evidence == null)
            {
                Add(errors, EventOverlayValidationErrorCode.MissingInput, "removalEvidence", "Removal evidence is required.");
                return;
            }

            if (evidence.DeclaresNonMarkerMutation)
                Add(errors, EventOverlayValidationErrorCode.NonMarkerMutation, "removalEvidence.nonMarkerMutation",
                    "EventOverlay can only alter marker assignments.");

            var expectedShell = shellValidation != null && shellValidation.IsValid
                ? shellValidation.CanonicalDigest
                : string.Empty;
            if (!IsSha256(evidence.StaticShellDigestBeforeRemoval) ||
                evidence.StaticShellDigestBeforeRemoval != evidence.StaticShellDigestAfterRemoval ||
                evidence.StaticShellDigestBeforeRemoval != expectedShell ||
                !IsSha256(evidence.MandatoryPathDigestBeforeRemoval) ||
                evidence.MandatoryPathDigestBeforeRemoval != evidence.MandatoryPathDigestAfterRemoval ||
                !AccessClassTokenCodec.IsPublished(evidence.AccessClassBeforeRemoval) ||
                evidence.AccessClassBeforeRemoval != evidence.AccessClassAfterRemoval)
            {
                Add(errors, EventOverlayValidationErrorCode.NonMarkerMutation, "removalEvidence.staticIdentity",
                    "Static shell, mandatory path, and access must remain identical.");
            }

            if (contract.ActivityStructureId.HasValue)
            {
                var expectedActivity = activityValidation != null && activityValidation.IsValid
                    ? activityValidation.CanonicalDigest
                    : string.Empty;
                if (!IsSha256(evidence.ActivityRemovalSafetyDigestBeforeRemoval) ||
                    evidence.ActivityRemovalSafetyDigestBeforeRemoval != evidence.ActivityRemovalSafetyDigestAfterRemoval ||
                    evidence.ActivityRemovalSafetyDigestBeforeRemoval != expectedActivity)
                {
                    Add(errors, EventOverlayValidationErrorCode.NonMarkerMutation, "removalEvidence.activityIdentity",
                        "Activity removal safety must remain identical.");
                }
            }
            else if (!string.IsNullOrEmpty(evidence.ActivityRemovalSafetyDigestBeforeRemoval) ||
                     !string.IsNullOrEmpty(evidence.ActivityRemovalSafetyDigestAfterRemoval))
            {
                Add(errors, EventOverlayValidationErrorCode.InvalidShellReference, "removalEvidence.activityIdentity",
                    "Activity evidence requires an ActivityStructure reference.");
            }
        }

        private static bool IsCompatible(EventOverlayKind kind, EventMarkerOperation operation)
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

        private static bool IsStableId(string value, string prefix)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.Ordinal) || value.Length <= prefix.Length)
                return false;
            return value.All(character => (character >= 'A' && character <= 'Z') ||
                                          (character >= '0' && character <= '9') || character == '_');
        }

        private static bool IsStableToken(string value)
        {
            return !string.IsNullOrEmpty(value) && value[0] >= 'A' && value[0] <= 'Z' &&
                   value.All(character => (character >= 'A' && character <= 'Z') ||
                                          (character >= '0' && character <= '9') || character == '_');
        }

        private static bool IsSha256(string value)
        {
            return value != null && value.Length == 64 && value.All(character =>
                (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));
        }

        private static void Add(
            ICollection<EventOverlayValidationError> errors,
            EventOverlayValidationErrorCode code,
            string path,
            string detail) => errors.Add(new EventOverlayValidationError(code, path, detail));
    }
}
