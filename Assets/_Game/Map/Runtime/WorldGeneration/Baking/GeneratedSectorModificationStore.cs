using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedSectorModificationFailureCode
    {
        MissingStorage = 1,
        MissingRecord = 2,
        InvalidAuthority = 3,
        InvalidLocalIndex = 4,
        InvalidLayer = 5,
        CrossSectorMismatch = 6,
        UnknownTarget = 7,
        StaleDigest = 8,
        InvalidPayload = 9,
        InvalidRevision = 10,
        ConflictingMutation = 11,
        MissingHandle = 12,
        InvalidHandleState = 13,
        HandleTransitionFailed = 14,
    }

    public sealed class GeneratedSectorModificationFailure :
        IEquatable<GeneratedSectorModificationFailure>,
        IComparable<GeneratedSectorModificationFailure>
    {
        public GeneratedSectorModificationFailure(
            GeneratedSectorModificationFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedSectorModificationFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;
        public int CompareTo(GeneratedSectorModificationFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedSectorModificationFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorModificationFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedSectorModificationResult
    {
        private readonly ReadOnlyCollection<GeneratedSectorModificationFailure> failures;

        internal GeneratedSectorModificationResult(
            GeneratedSectorModificationStorage storage,
            GeneratedModifiedSectorSnapshot sectorSnapshot,
            GeneratedSectorModificationApplyPlan applyPlan,
            bool wasIdempotent,
            IEnumerable<GeneratedSectorModificationFailure> sourceFailures)
        {
            Storage = storage;
            SectorSnapshot = sectorSnapshot;
            ApplyPlan = applyPlan;
            WasIdempotent = wasIdempotent;
            failures = new ReadOnlyCollection<GeneratedSectorModificationFailure>((sourceFailures ??
                Array.Empty<GeneratedSectorModificationFailure>()).Distinct()
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Storage != null && failures.Count == 0 &&
            (ApplyPlan == null || ApplyPlan.SleepingModifiedHandle != null);
        public GeneratedSectorModificationStorage Storage { get; }
        public GeneratedModifiedSectorSnapshot SectorSnapshot { get; }
        public GeneratedSectorModificationApplyPlan ApplyPlan { get; }
        public bool WasIdempotent { get; }
        public IReadOnlyList<GeneratedSectorModificationFailure> Failures => failures;
    }

    public sealed class GeneratedSectorModificationStore
    {
        public const string SchemaVersion = "MAP17_05_SECTOR_MODIFICATION_V1";
        public const string DownstreamOwner =
            "MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY";
        public const bool OpensDownstreamTask = false;
        public const int PopulationStableSpawnIdCount = 0;

        public GeneratedSectorModificationStore(GeneratedSectorModificationAuthority authority)
        {
            Authority = authority;
        }

        public GeneratedSectorModificationAuthority Authority { get; }

        public GeneratedSectorModificationStorage CreateEmpty() =>
            new GeneratedSectorModificationStorage(Array.Empty<GeneratedModifiedSectorSnapshot>());

        public GeneratedSectorModificationRecord Author(
            GeneratedSectorModificationStorage storage,
            GeneratedSectorModificationTarget target,
            GeneratedSectorModificationKind kind,
            GeneratedSectorModificationPayload payload,
            int? revision = null,
            GeneratedSectorModificationBaseDigests baseDigests = null)
        {
            var snapshot = storage == null || target == null
                ? null : storage.Find(target.Sector);
            var nextRevision = revision ?? (snapshot == null ? 1 : snapshot.DirtyRevision + 1);
            var stableId = new GeneratedSectorModificationStableId(
                Authority == null ? string.Empty : Authority.SeedIdentity,
                Authority == null ? string.Empty : Authority.GeneratorVersion,
                Authority == null ? string.Empty : Authority.DataVersion,
                target, kind, SchemaVersion);
            return new GeneratedSectorModificationRecord(stableId, target, kind,
                nextRevision, payload, baseDigests ??
                (Authority == null ? null : Authority.BaseDigests));
        }

        public GeneratedSectorModificationResult Add(
            GeneratedSectorModificationStorage storage,
            GeneratedSectorModificationRecord record)
        {
            var failures = Validate(storage, record);
            if (failures.Count != 0) return Failed(storage, failures);

            var existingSector = storage.Find(record.Target.Sector);
            var currentRecords = existingSector == null
                ? new List<GeneratedSectorModificationRecord>()
                : existingSector.Records.ToList();
            var currentRevision = existingSector == null ? 0 : existingSector.DirtyRevision;
            var existingRecord = currentRecords.SingleOrDefault(value => value.Id.Equals(record.Id));
            if (existingRecord != null && string.Equals(existingRecord.StableToken,
                record.StableToken, StringComparison.Ordinal))
                return new GeneratedSectorModificationResult(storage, existingSector,
                    null, true, Array.Empty<GeneratedSectorModificationFailure>());

            if (record.Revision != currentRevision + 1)
            {
                AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidRevision,
                    "revision", "A semantic mutation must advance the sector dirty revision by one.");
                return Failed(storage, failures);
            }

            if (existingRecord != null)
            {
                if (record.Revision <= existingRecord.Revision)
                {
                    AddFailure(failures, GeneratedSectorModificationFailureCode.ConflictingMutation,
                        record.Id.Value, "A same-target mutation cannot silently overwrite its revision.");
                    return Failed(storage, failures);
                }
                currentRecords.Remove(existingRecord);
            }
            currentRecords.Add(record);

            var set = new GeneratedSectorModificationSet(record.Target.Sector,
                record.Revision, Authority.BaseDigests, currentRecords);
            var modified = new GeneratedModifiedSectorSnapshot(set,
                GeneratedSectorRuntimeState.Active,
                GeneratedSectorRuntimeState.SleepingModified);
            var nextSectors = storage.ModifiedSectors
                .Where(value => !value.Sector.Equals(record.Target.Sector))
                .Concat(new[] { modified });
            var next = new GeneratedSectorModificationStorage(nextSectors);
            return new GeneratedSectorModificationResult(next, modified, null, false,
                Array.Empty<GeneratedSectorModificationFailure>());
        }

        public GeneratedSectorModificationResult Replace(
            GeneratedSectorModificationStorage storage,
            GeneratedSectorModificationRecord record) => Add(storage, record);

        public GeneratedSectorModificationResult Merge(
            GeneratedSectorModificationStorage storage,
            IEnumerable<GeneratedSectorModificationRecord> records)
        {
            if (storage == null)
                return Failed(null, new[] { Failure(
                    GeneratedSectorModificationFailureCode.MissingStorage,
                    "storage", "A storage snapshot is required.") });
            var source = records == null
                ? Array.Empty<GeneratedSectorModificationRecord>() : records.ToArray();
            if (source.Any(value => value == null))
                return Failed(storage, new[] { Failure(
                    GeneratedSectorModificationFailureCode.MissingRecord,
                    "records", "Merge records cannot contain null.") });

            var candidate = storage;
            foreach (var record in source.OrderBy(value => value.Revision)
                         .ThenBy(value => value.Id == null ? string.Empty : value.Id.Value,
                             StringComparer.Ordinal))
            {
                var result = Add(candidate, record);
                if (!result.Success)
                    return Failed(storage, result.Failures);
                candidate = result.Storage;
            }
            return new GeneratedSectorModificationResult(candidate,
                candidate.Find(Authority.Sector), null, false,
                Array.Empty<GeneratedSectorModificationFailure>());
        }

        public GeneratedSectorModificationResult Compact(
            GeneratedSectorModificationStorage storage)
        {
            if (storage == null)
                return Failed(null, new[] { Failure(
                    GeneratedSectorModificationFailureCode.MissingStorage,
                    "storage", "A storage snapshot is required.") });
            var compacted = storage.ModifiedSectors.Select(value =>
            {
                var finalRecords = value.Records.GroupBy(record => record.Id.Value,
                        StringComparer.Ordinal)
                    .Select(group => group.OrderByDescending(record => record.Revision).First())
                    .OrderBy(record => record).ToArray();
                return new GeneratedModifiedSectorSnapshot(
                    new GeneratedSectorModificationSet(value.Sector, value.DirtyRevision,
                        value.BaseDigests, finalRecords), value.SourceHandleState,
                    value.TargetHandleState);
            }).ToArray();
            var next = new GeneratedSectorModificationStorage(compacted);
            return new GeneratedSectorModificationResult(next,
                next.Find(Authority == null ? null : Authority.Sector), null,
                string.Equals(next.Digest, storage.Digest, StringComparison.Ordinal),
                Array.Empty<GeneratedSectorModificationFailure>());
        }

        public GeneratedSectorModificationResult Query(
            GeneratedSectorModificationStorage storage,
            GeneratedSectorCoordinate sector)
        {
            if (storage == null)
                return Failed(null, new[] { Failure(
                    GeneratedSectorModificationFailureCode.MissingStorage,
                    "storage", "A storage snapshot is required.") });
            return new GeneratedSectorModificationResult(storage, storage.Find(sector),
                null, false, Array.Empty<GeneratedSectorModificationFailure>());
        }

        public GeneratedSectorModificationResult BuildApplyPlan(
            GeneratedSectorModificationStorage storage,
            GeneratedSectorCoordinate sector,
            GeneratedSectorRuntimeHandle handle)
        {
            var failures = new List<GeneratedSectorModificationFailure>();
            if (storage == null)
                AddFailure(failures, GeneratedSectorModificationFailureCode.MissingStorage,
                    "storage", "A storage snapshot is required.");
            if (Authority == null || !Authority.IsValid)
                AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidAuthority,
                    "authority", "A valid source authority is required.");
            var snapshot = storage == null ? null : storage.Find(sector);
            if (snapshot == null)
                AddFailure(failures, GeneratedSectorModificationFailureCode.UnknownTarget,
                    "sector", "The requested sector has no modifications.");
            if (handle == null)
                AddFailure(failures, GeneratedSectorModificationFailureCode.MissingHandle,
                    "handle", "A runtime handle is required.");
            else
            {
                if (sector == null || !handle.Sector.Equals(sector.ToRuntimeCoordinate()))
                    AddFailure(failures, GeneratedSectorModificationFailureCode.CrossSectorMismatch,
                        "handle.sector", "The runtime handle must belong to the modified sector.");
                if (handle.State != GeneratedSectorRuntimeState.Active &&
                    handle.State != GeneratedSectorRuntimeState.SleepingModified)
                    AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidHandleState,
                        "handle.state", "Only Active or already SleepingModified handles may receive modifications.");
                if (Authority != null && !string.Equals(handle.BakeDigest,
                    Authority.BaseDigests.BakeDigest, StringComparison.Ordinal))
                    AddFailure(failures, GeneratedSectorModificationFailureCode.StaleDigest,
                        "handle.bakeDigest", "The runtime handle bake digest is stale.");
            }
            if (failures.Count != 0) return Failed(storage, failures);

            GeneratedSectorRuntimeHandle sleeping;
            if (handle.State == GeneratedSectorRuntimeState.SleepingModified)
            {
                if (handle.MutationRevision != snapshot.DirtyRevision)
                {
                    AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidRevision,
                        "handle.mutationRevision", "SleepingModified revision must equal storage revision.");
                    return Failed(storage, failures);
                }
                sleeping = handle;
            }
            else
            {
                var transition = GeneratedSectorRuntimeHandleLifecycle.Transition(
                    new GeneratedSectorRuntimeTransitionRequest(handle,
                        GeneratedSectorRuntimeState.SleepingModified, handle.Sector,
                        GeneratedColliderCacheSnapshot.Empty,
                        mutationRevision: snapshot.DirtyRevision,
                        dirtyReason: "SECTOR_MODIFICATION_STORAGE"));
                if (!transition.Success)
                {
                    AddFailure(failures,
                        GeneratedSectorModificationFailureCode.HandleTransitionFailed,
                        "handle", string.Join(";", transition.Failures.Select(value => value.StableToken)));
                    return Failed(storage, failures);
                }
                sleeping = transition.Handle;
            }

            var commands = snapshot.Records.OrderBy(value => value)
                .Select((record, index) =>
                    new GeneratedSectorModificationApplyCommand(index, record)).ToArray();
            var plan = new GeneratedSectorModificationApplyPlan(storage, snapshot,
                commands, handle, sleeping, Authority.SourceRecordDigest);
            return new GeneratedSectorModificationResult(storage, snapshot, plan,
                false, Array.Empty<GeneratedSectorModificationFailure>());
        }

        private List<GeneratedSectorModificationFailure> Validate(
            GeneratedSectorModificationStorage storage,
            GeneratedSectorModificationRecord record)
        {
            var failures = new List<GeneratedSectorModificationFailure>();
            if (storage == null)
                AddFailure(failures, GeneratedSectorModificationFailureCode.MissingStorage,
                    "storage", "A storage snapshot is required.");
            if (Authority == null || !Authority.IsValid)
                AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidAuthority,
                    "authority", "A valid source authority is required.");
            if (record == null)
            {
                AddFailure(failures, GeneratedSectorModificationFailureCode.MissingRecord,
                    "record", "A modification record is required.");
                return failures;
            }
            if (record.Target == null || record.Target.LocalIndex == null ||
                !record.Target.LocalIndex.IsValid)
                AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidLocalIndex,
                    "target.localIndex", "Local index must be in 0..1535.");
            if (record.Target == null || !record.Target.IsLayerValid)
                AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidLayer,
                    "target.layerId", "Layer id must identify one of the seven logical layers.");
            if (record.Target == null || Authority == null || record.Target.Sector == null ||
                !record.Target.Sector.Equals(Authority.Sector))
                AddFailure(failures, GeneratedSectorModificationFailureCode.CrossSectorMismatch,
                    "target.sector", "Target sector must equal the source authority sector.");
            if (Authority != null && !Authority.ContainsTarget(record.Target))
                AddFailure(failures, GeneratedSectorModificationFailureCode.UnknownTarget,
                    "target", "Target provenance does not exist in logical bake records.");
            if (Authority == null || record.BaseDigests == null ||
                !record.BaseDigests.Equals(Authority.BaseDigests))
                AddFailure(failures, GeneratedSectorModificationFailureCode.StaleDigest,
                    "baseDigests", "Bake, cache, or window base digest is stale.");
            if (record.Id == null || !record.Id.IsValid || record.Payload == null ||
                !record.Payload.IsValidFor(record.Kind))
                AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidPayload,
                    "record", "Stable identity and kind-specific payload must be valid.");
            if (record.Revision <= 0)
                AddFailure(failures, GeneratedSectorModificationFailureCode.InvalidRevision,
                    "revision", "Revision must be positive.");
            return failures;
        }

        private static GeneratedSectorModificationResult Failed(
            GeneratedSectorModificationStorage source,
            IEnumerable<GeneratedSectorModificationFailure> failures) =>
            new GeneratedSectorModificationResult(source, null, null, false, failures);

        private static GeneratedSectorModificationFailure Failure(
            GeneratedSectorModificationFailureCode code,
            string subject,
            string reason) => new GeneratedSectorModificationFailure(code, subject, reason);

        private static void AddFailure(
            ICollection<GeneratedSectorModificationFailure> failures,
            GeneratedSectorModificationFailureCode code,
            string subject,
            string reason) => failures.Add(Failure(code, subject, reason));
    }
}
