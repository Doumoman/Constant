using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum SpecialRegionPersistenceCheckpoint
    {
        Initial = 1,
        Active = 2,
        Interrupted = 3,
        Failed = 4,
        Regenerated = 5,
        Claimed = 6,
        Revisited = 7,
    }

    public enum SpecialRegionRequiredResourceState
    {
        Available = 1,
        TemporarilyUnavailable = 2,
        Claimed = 3,
        PermanentlyUnavailable = 4,
    }

    public sealed class SpecialRegionPersistenceCheckpointEvidence
    {
        public SpecialRegionPersistenceCheckpointEvidence(
            SpecialRegionId regionId,
            SpecialRegionSlotId slotId,
            SpecialPersistenceKey persistenceKey,
            SpecialPersistenceScope persistenceScope,
            SpecialRegionPersistenceCheckpoint checkpoint,
            SpecialRegionRequiredResourceState state,
            string sourceDigest)
        {
            RegionId = regionId;
            SlotId = slotId;
            PersistenceKey = persistenceKey;
            PersistenceScope = persistenceScope;
            Checkpoint = checkpoint;
            State = state;
            SourceDigest = sourceDigest ?? string.Empty;
        }

        public SpecialRegionId RegionId { get; }
        public SpecialRegionSlotId SlotId { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public SpecialPersistenceScope PersistenceScope { get; }
        public SpecialRegionPersistenceCheckpoint Checkpoint { get; }
        public SpecialRegionRequiredResourceState State { get; }
        public string SourceDigest { get; }
    }

    public sealed class SpecialRegionRequiredResourceSafetyProof
    {
        private readonly ReadOnlyCollection<SpecialRegionPersistenceCheckpointEvidence> evidence;

        internal SpecialRegionRequiredResourceSafetyProof(
            SpecialRegionId regionId,
            SpecialRegionSlotId slotId,
            SpecialPersistenceKey persistenceKey,
            string sourceDigest,
            IEnumerable<SpecialRegionPersistenceCheckpointEvidence> evidence)
        {
            RegionId = regionId;
            SlotId = slotId;
            PersistenceKey = persistenceKey;
            PersistenceScope = SpecialPersistenceScope.Reward;
            SourceDigest = sourceDigest ?? string.Empty;
            this.evidence = new ReadOnlyCollection<SpecialRegionPersistenceCheckpointEvidence>(
                (evidence ?? Array.Empty<SpecialRegionPersistenceCheckpointEvidence>())
                .Where(value => value != null).OrderBy(value => value.Checkpoint).ToArray());
            CanonicalDigest = SpecialRegionPersistenceSafetyCanonicalDigest.ComputeProof(this);
        }

        public SpecialRegionId RegionId { get; }
        public SpecialRegionSlotId SlotId { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public SpecialPersistenceScope PersistenceScope { get; }
        public string SourceDigest { get; }
        public IReadOnlyList<SpecialRegionPersistenceCheckpointEvidence> Evidence => evidence;
        public string CanonicalDigest { get; }
        public bool InitialAvailable => StateAt(SpecialRegionPersistenceCheckpoint.Initial) ==
                                        SpecialRegionRequiredResourceState.Available;
        public bool RecoveryBranchesAvailable =>
            StateAt(SpecialRegionPersistenceCheckpoint.Interrupted) == SpecialRegionRequiredResourceState.Available &&
            StateAt(SpecialRegionPersistenceCheckpoint.Failed) == SpecialRegionRequiredResourceState.Available &&
            StateAt(SpecialRegionPersistenceCheckpoint.Regenerated) == SpecialRegionRequiredResourceState.Available;
        public bool ClaimStable =>
            StateAt(SpecialRegionPersistenceCheckpoint.Claimed) == SpecialRegionRequiredResourceState.Claimed &&
            StateAt(SpecialRegionPersistenceCheckpoint.Revisited) == SpecialRegionRequiredResourceState.Claimed;
        public int PermanentlyUnavailableCount => evidence.Count(value =>
            value.State == SpecialRegionRequiredResourceState.PermanentlyUnavailable);
        public int DuplicateRewardRiskCount => 0;
        public int RewardGrantCount => 0;
        public int SaveWriteCount => 0;
        public bool IsSafe => evidence.Count == 7 && InitialAvailable && RecoveryBranchesAvailable &&
                              ClaimStable && PermanentlyUnavailableCount == 0;

        private SpecialRegionRequiredResourceState StateAt(SpecialRegionPersistenceCheckpoint checkpoint)
            => evidence.Single(value => value.Checkpoint == checkpoint).State;
    }

    public sealed class SpecialRegionPersistenceSafetyCompileRequest
    {
        private readonly ReadOnlyCollection<SpecialRegionPersistenceCheckpointEvidence> evidence;

        public SpecialRegionPersistenceSafetyCompileRequest(
            SpecialRegionFixedSlotLayerPlan layerPlan,
            string expectedLayerDigest,
            IEnumerable<SpecialRegionPersistenceCheckpointEvidence> evidence)
        {
            LayerPlan = layerPlan;
            ExpectedLayerDigest = expectedLayerDigest ?? string.Empty;
            var supplied = evidence == null
                ? Array.Empty<SpecialRegionPersistenceCheckpointEvidence>()
                : evidence.ToArray();
            this.evidence = new ReadOnlyCollection<SpecialRegionPersistenceCheckpointEvidence>(
                supplied.Where(value => value != null).ToArray());
            SuppliedNullEvidenceCount = supplied.Count(value => value == null);
        }

        public SpecialRegionFixedSlotLayerPlan LayerPlan { get; }
        public string ExpectedLayerDigest { get; }
        public IReadOnlyList<SpecialRegionPersistenceCheckpointEvidence> Evidence => evidence;
        internal int SuppliedNullEvidenceCount { get; }
    }

    public enum SpecialRegionPersistenceSafetyErrorCode
    {
        MissingInput = 1,
        PersistenceKeyMismatch = 2,
        PersistenceScopeMismatch = 3,
        MissingRequiredReward = 4,
        MissingCheckpoint = 5,
        InvalidCheckpointState = 6,
        RequiredResourcePermanentlyLost = 7,
        ClaimRollback = 8,
        DuplicateRewardRisk = 9,
        NonCanonicalPublication = 10,
    }

    public sealed class SpecialRegionPersistenceSafetyError :
        IEquatable<SpecialRegionPersistenceSafetyError>, IComparable<SpecialRegionPersistenceSafetyError>
    {
        public SpecialRegionPersistenceSafetyError(
            SpecialRegionPersistenceSafetyErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SpecialRegionPersistenceSafetyErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(SpecialRegionPersistenceSafetyError other)
        {
            if (other == null) return -1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SpecialRegionPersistenceSafetyError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as SpecialRegionPersistenceSafetyError);

        public override int GetHashCode()
        {
            unchecked
            {
                var value = (int)Code;
                value = (value * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (value * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class SpecialRegionPersistenceSafetyResult
    {
        private readonly ReadOnlyCollection<SpecialRegionRequiredResourceSafetyProof> proofs;
        private readonly ReadOnlyCollection<SpecialRegionPersistenceSafetyError> errors;

        internal SpecialRegionPersistenceSafetyResult(
            string layerDigest,
            IEnumerable<SpecialRegionRequiredResourceSafetyProof> proofs,
            IEnumerable<SpecialRegionPersistenceSafetyError> errors)
        {
            var errorValues = (errors ?? Array.Empty<SpecialRegionPersistenceSafetyError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<SpecialRegionPersistenceSafetyError>(errorValues);
            LayerDigest = errorValues.Length == 0 ? layerDigest ?? string.Empty : string.Empty;
            var proofValues = errorValues.Length == 0
                ? (proofs ?? Array.Empty<SpecialRegionRequiredResourceSafetyProof>())
                    .Where(value => value != null).OrderBy(value => value.SlotId).ToArray()
                : Array.Empty<SpecialRegionRequiredResourceSafetyProof>();
            this.proofs = new ReadOnlyCollection<SpecialRegionRequiredResourceSafetyProof>(proofValues);
            CanonicalDigest = errorValues.Length == 0
                ? SpecialRegionPersistenceSafetyCanonicalDigest.Compute(LayerDigest, proofValues)
                : string.Empty;
        }

        public bool Succeeded => errors.Count == 0 && LayerDigest.Length != 0 &&
                                 proofs.All(value => value.IsSafe);
        public bool AggregateSafetyPublished => Succeeded;
        public string LayerDigest { get; }
        public IReadOnlyList<SpecialRegionRequiredResourceSafetyProof> Proofs => proofs;
        public IReadOnlyList<SpecialRegionPersistenceSafetyError> Errors => errors;
        public string CanonicalDigest { get; }
        public int RewardGrantCount => 0;
        public int InventoryMutationCount => 0;
        public int SaveWriteCount => 0;
    }

    public static class SpecialRegionPersistenceSafetyCompiler
    {
        private static readonly SpecialRegionPersistenceCheckpoint[] RequiredCheckpoints =
        {
            SpecialRegionPersistenceCheckpoint.Initial,
            SpecialRegionPersistenceCheckpoint.Active,
            SpecialRegionPersistenceCheckpoint.Interrupted,
            SpecialRegionPersistenceCheckpoint.Failed,
            SpecialRegionPersistenceCheckpoint.Regenerated,
            SpecialRegionPersistenceCheckpoint.Claimed,
            SpecialRegionPersistenceCheckpoint.Revisited,
        };

        public static SpecialRegionPersistenceSafetyResult Compile(
            SpecialRegionPersistenceSafetyCompileRequest request)
        {
            if (request == null)
                return Failure(SpecialRegionPersistenceSafetyErrorCode.MissingInput, "request");
            var errors = new List<SpecialRegionPersistenceSafetyError>();
            if (request.LayerPlan == null)
                Add(errors, SpecialRegionPersistenceSafetyErrorCode.MissingInput,
                    "layerPlan", "A successful fixed-slot layer plan is required.");
            else
            {
                var digest = SpecialRegionFixedSlotLayerCanonicalDigest.Compute(request.LayerPlan);
                if (!EqualsDigest(digest, request.LayerPlan.CanonicalDigest) ||
                    !EqualsDigest(digest, request.ExpectedLayerDigest))
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.NonCanonicalPublication,
                        "layerPlan", "Expected, published, and recomputed layer digests must match.");
            }
            if (request.SuppliedNullEvidenceCount != 0)
                Add(errors, SpecialRegionPersistenceSafetyErrorCode.NonCanonicalPublication,
                    "evidence", "Null checkpoint evidence is not canonical.");
            if (request.LayerPlan == null)
                return new SpecialRegionPersistenceSafetyResult(string.Empty, null, errors);

            var rewards = request.LayerPlan.ReplaceableSlots
                .Where(value => value.Kind == SpecialRegionSlotKind.Reward).ToArray();
            var required = rewards.Where(value => value.Required).ToArray();
            if (request.LayerPlan.RegionKind == SpecialRegionKind.CoreResource && required.Length == 0)
                Add(errors, SpecialRegionPersistenceSafetyErrorCode.MissingRequiredReward,
                    "requiredRewards", "CoreResource regions require at least one required Reward slot.");

            ValidateSuppliedEvidenceTargets(request, rewards, errors);
            var proofs = new List<SpecialRegionRequiredResourceSafetyProof>();
            foreach (var slot in required)
            {
                ValidateRequiredRewardIdentity(request.LayerPlan.RegionId, slot, errors);
                var slotEvidence = request.Evidence.Where(value => value.SlotId == slot.SlotId).ToArray();
                ValidateEvidenceIdentity(request.LayerPlan.RegionId, slot, slotEvidence, errors);
                ValidateCheckpointSet(slot, slotEvidence, errors);
                ValidateCheckpointStates(slot, slotEvidence, errors);
                if (!errors.Any(value => value.Path.StartsWith(
                        "requiredRewards/" + slot.SlotId.Value, StringComparison.Ordinal)))
                {
                    proofs.Add(new SpecialRegionRequiredResourceSafetyProof(
                        request.LayerPlan.RegionId, slot.SlotId, slot.PersistenceKey,
                        slot.IdentityDigest, slotEvidence));
                }
            }

            return new SpecialRegionPersistenceSafetyResult(
                errors.Count == 0 ? request.LayerPlan.CanonicalDigest : string.Empty,
                proofs,
                errors);
        }

        private static void ValidateSuppliedEvidenceTargets(
            SpecialRegionPersistenceSafetyCompileRequest request,
            IEnumerable<SpecialRegionReplaceableSlotBinding> rewards,
            ICollection<SpecialRegionPersistenceSafetyError> errors)
        {
            var byId = rewards.ToDictionary(value => value.SlotId);
            foreach (var item in request.Evidence)
            {
                if (!byId.TryGetValue(item.SlotId, out var reward))
                {
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.PersistenceKeyMismatch,
                        "evidence/" + item.SlotId.Value,
                        "Checkpoint evidence must reference a Reward slot from the layer plan.");
                    continue;
                }
                if (!reward.Required)
                {
                    if (item.RegionId != request.LayerPlan.RegionId ||
                        item.PersistenceKey != reward.PersistenceKey ||
                        item.PersistenceScope != reward.PersistenceScope ||
                        !string.Equals(item.SourceDigest, reward.IdentityDigest, StringComparison.Ordinal))
                        Add(errors, SpecialRegionPersistenceSafetyErrorCode.PersistenceKeyMismatch,
                            "evidence/" + item.SlotId.Value,
                            "Optional Reward evidence may only preserve authored provenance.");
                }
            }
        }

        private static void ValidateRequiredRewardIdentity(
            SpecialRegionId regionId,
            SpecialRegionReplaceableSlotBinding slot,
            ICollection<SpecialRegionPersistenceSafetyError> errors)
        {
            var path = "requiredRewards/" + slot.SlotId.Value;
            var expected = SpecialPersistenceKey.ForSlot(
                regionId, SpecialPersistenceScope.Reward, slot.SlotId);
            if (!SpecialRegionValidator.IsStableId(slot.PersistenceKey.Value, "SR_STATE_") ||
                slot.PersistenceKey != expected)
                Add(errors, SpecialRegionPersistenceSafetyErrorCode.PersistenceKeyMismatch,
                    path, "Required Reward key must be stable and deterministically bound to the slot.");
            if (slot.PersistenceScope != SpecialPersistenceScope.Reward)
                Add(errors, SpecialRegionPersistenceSafetyErrorCode.PersistenceScopeMismatch,
                    path, "Required Reward scope must be exactly Reward.");
        }

        private static void ValidateEvidenceIdentity(
            SpecialRegionId regionId,
            SpecialRegionReplaceableSlotBinding slot,
            IEnumerable<SpecialRegionPersistenceCheckpointEvidence> evidence,
            ICollection<SpecialRegionPersistenceSafetyError> errors)
        {
            var path = "requiredRewards/" + slot.SlotId.Value + "/identity";
            foreach (var item in evidence)
            {
                if (item.RegionId != regionId || item.SlotId != slot.SlotId ||
                    item.PersistenceKey != slot.PersistenceKey ||
                    !string.Equals(item.SourceDigest, slot.IdentityDigest, StringComparison.Ordinal))
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.PersistenceKeyMismatch,
                        path, "Every checkpoint must preserve region, slot, key, and source digest.");
                if (item.PersistenceScope != SpecialPersistenceScope.Reward)
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.PersistenceScopeMismatch,
                        path, "Every required Reward checkpoint must preserve Reward scope.");
            }
        }

        private static void ValidateCheckpointSet(
            SpecialRegionReplaceableSlotBinding slot,
            IEnumerable<SpecialRegionPersistenceCheckpointEvidence> evidence,
            ICollection<SpecialRegionPersistenceSafetyError> errors)
        {
            var path = "requiredRewards/" + slot.SlotId.Value + "/checkpoints";
            var values = evidence.ToArray();
            foreach (var checkpoint in RequiredCheckpoints)
            {
                var count = values.Count(value => value.Checkpoint == checkpoint);
                if (count == 0)
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.MissingCheckpoint,
                        path + "/" + checkpoint, "Required checkpoint is missing.");
                else if (count != 1)
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.NonCanonicalPublication,
                        path + "/" + checkpoint, "Each checkpoint must be published exactly once.");
            }
            foreach (var item in values.Where(value => !Enum.IsDefined(
                         typeof(SpecialRegionPersistenceCheckpoint), value.Checkpoint)))
                Add(errors, SpecialRegionPersistenceSafetyErrorCode.NonCanonicalPublication,
                    path, "Unknown checkpoint values are forbidden.");
        }

        private static void ValidateCheckpointStates(
            SpecialRegionReplaceableSlotBinding slot,
            IEnumerable<SpecialRegionPersistenceCheckpointEvidence> evidence,
            ICollection<SpecialRegionPersistenceSafetyError> errors)
        {
            var path = "requiredRewards/" + slot.SlotId.Value + "/states";
            foreach (var item in evidence)
            {
                if (!Enum.IsDefined(typeof(SpecialRegionRequiredResourceState), item.State))
                {
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.InvalidCheckpointState,
                        path + "/" + item.Checkpoint, "Unknown resource state.");
                    continue;
                }
                if (item.Checkpoint <= SpecialRegionPersistenceCheckpoint.Regenerated &&
                    item.State == SpecialRegionRequiredResourceState.PermanentlyUnavailable)
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.RequiredResourcePermanentlyLost,
                        path + "/" + item.Checkpoint, "Required Reward may not be permanently lost before claim.");

                var valid = false;
                switch (item.Checkpoint)
                {
                    case SpecialRegionPersistenceCheckpoint.Initial:
                        valid = item.State == SpecialRegionRequiredResourceState.Available;
                        break;
                    case SpecialRegionPersistenceCheckpoint.Active:
                        valid = item.State == SpecialRegionRequiredResourceState.Available ||
                                item.State == SpecialRegionRequiredResourceState.TemporarilyUnavailable;
                        break;
                    case SpecialRegionPersistenceCheckpoint.Interrupted:
                    case SpecialRegionPersistenceCheckpoint.Failed:
                    case SpecialRegionPersistenceCheckpoint.Regenerated:
                        valid = item.State == SpecialRegionRequiredResourceState.Available;
                        break;
                    case SpecialRegionPersistenceCheckpoint.Claimed:
                    case SpecialRegionPersistenceCheckpoint.Revisited:
                        valid = item.State == SpecialRegionRequiredResourceState.Claimed;
                        if (!valid)
                        {
                            Add(errors, SpecialRegionPersistenceSafetyErrorCode.ClaimRollback,
                                path + "/" + item.Checkpoint,
                                "Claimed state may not roll back on claim or revisit.");
                            if (item.State == SpecialRegionRequiredResourceState.Available ||
                                item.State == SpecialRegionRequiredResourceState.TemporarilyUnavailable)
                                Add(errors, SpecialRegionPersistenceSafetyErrorCode.DuplicateRewardRisk,
                                    path + "/" + item.Checkpoint,
                                    "Available state after claim could duplicate the required Reward.");
                        }
                        break;
                }
                if (!valid)
                    Add(errors, SpecialRegionPersistenceSafetyErrorCode.InvalidCheckpointState,
                        path + "/" + item.Checkpoint,
                        "Checkpoint state violates the required Reward lifecycle contract.");
            }
        }

        private static bool EqualsDigest(string left, string right)
            => !string.IsNullOrEmpty(left) &&
               string.Equals(left, right, StringComparison.Ordinal);

        private static SpecialRegionPersistenceSafetyResult Failure(
            SpecialRegionPersistenceSafetyErrorCode code,
            string path)
            => new SpecialRegionPersistenceSafetyResult(string.Empty, null, new[]
            {
                new SpecialRegionPersistenceSafetyError(code, path, "Required input is missing."),
            });

        private static void Add(
            ICollection<SpecialRegionPersistenceSafetyError> errors,
            SpecialRegionPersistenceSafetyErrorCode code,
            string path,
            string detail)
            => errors.Add(new SpecialRegionPersistenceSafetyError(code, path, detail));
    }

    public static class SpecialRegionPersistenceSafetyCanonicalDigest
    {
        public static string Compute(
            string layerDigest,
            IEnumerable<SpecialRegionRequiredResourceSafetyProof> proofs)
        {
            if (proofs == null) throw new ArgumentNullException(nameof(proofs));
            var value = new StringBuilder();
            Append(value, "layer", layerDigest);
            foreach (var proof in proofs.Where(item => item != null).OrderBy(item => item.SlotId))
                Append(value, "proof", proof.SlotId.Value + "/" + proof.CanonicalDigest);
            return Sha256(value.ToString());
        }

        public static string ComputeProof(SpecialRegionRequiredResourceSafetyProof proof)
        {
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            var value = new StringBuilder();
            Append(value, "region", proof.RegionId.Value);
            Append(value, "slot", proof.SlotId.Value);
            Append(value, "key", proof.PersistenceKey.Value);
            Append(value, "scope", Number((int)proof.PersistenceScope));
            Append(value, "source", proof.SourceDigest);
            foreach (var item in proof.Evidence.OrderBy(evidence => evidence.Checkpoint))
                Append(value, "checkpoint", Number((int)item.Checkpoint) + "/" +
                    Number((int)item.State) + "/" + item.RegionId.Value + "/" +
                    item.SlotId.Value + "/" + item.PersistenceKey.Value + "/" +
                    Number((int)item.PersistenceScope) + "/" + item.SourceDigest);
            return Sha256(value.ToString());
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static void Append(StringBuilder value, string name, string field)
            => value.Append(name).Append('=').Append(field ?? string.Empty).Append('\n');

        private static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(material))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
