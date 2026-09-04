using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Population
{
    public sealed class GeneratedContentSlotSourceRecord :
        IComparable<GeneratedContentSlotSourceRecord>
    {
        public GeneratedContentSlotSourceRecord(
            GeneratedContentSlotAddress address,
            bool availableForMandatoryUniquePreplacement)
        {
            Address = address;
            AvailableForMandatoryUniquePreplacement = availableForMandatoryUniquePreplacement;
            StableToken = "CONTENT_SLOT_SOURCE|" +
                (address == null ? "MISSING" : address.CanonicalLine) + "|" +
                (availableForMandatoryUniquePreplacement ? "MANDATORY_UNIQUE=1" :
                    "MANDATORY_UNIQUE=0");
        }

        public GeneratedContentSlotAddress Address { get; }
        public bool AvailableForMandatoryUniquePreplacement { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedContentSlotSourceRecord other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);

        public static GeneratedContentSlotSourceRecord FromMarkerSlot(
            string worldSeed,
            string generatorVersion,
            string dataVersion,
            GeneratedSectorCoordinate sector,
            GeneratedMarkerSlot slot,
            GeneratedContentSlotCategory category,
            GeneratedContentPoolKey poolKey,
            bool availableForMandatoryUniquePreplacement)
        {
            var cell = slot == null ? null : slot.CellReference;
            var provenance = slot == null ? null : slot.Provenance;
            var sectorLocalIndex = cell == null ? -1 :
                cell.SectorY * GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth + cell.SectorX;
            var sliceLocalIndex = cell == null ? -1 :
                cell.LocalY * GeneratedMicroChunkSliceSet.MicroChunkWidth + cell.LocalX;
            var address = new GeneratedContentSlotAddress(
                worldSeed, generatorVersion, dataVersion, sector,
                cell == null ? -1 : cell.ChunkIndex,
                sectorLocalIndex,
                sliceLocalIndex,
                slot == null ? (GeneratedMarkerSlotOwner)0 : slot.Owner,
                provenance == null ? string.Empty : provenance.SourceOwnerToken + "/" +
                    provenance.SourceTaskId,
                provenance == null ? string.Empty : provenance.StableToken,
                slot == null || slot.Id == null ? string.Empty : slot.Id.Value,
                category, poolKey);
            return new GeneratedContentSlotSourceRecord(address,
                availableForMandatoryUniquePreplacement);
        }
    }

    public sealed class GeneratedContentSlotIndexRequest
    {
        private readonly ReadOnlyCollection<GeneratedContentSlotSourceRecord> sources;

        public GeneratedContentSlotIndexRequest(
            IEnumerable<GeneratedContentSlotSourceRecord> sourceRecords,
            string map17AuditDigest,
            string map17PhaseExitVerdict,
            bool map18HandoffApproved,
            int map17WarningCount,
            string expectedIndexDigest = null,
            bool attemptedActualPlacementOrSpawn = false)
        {
            var raw = (sourceRecords ?? Array.Empty<GeneratedContentSlotSourceRecord>()).ToArray();
            sources = new ReadOnlyCollection<GeneratedContentSlotSourceRecord>(raw);
            NullSourceRecordCount = raw.Count(value => value == null);
            Map17AuditDigest = GeneratedContentPoolKey.Normalize(map17AuditDigest);
            Map17PhaseExitVerdict = GeneratedContentPoolKey.Normalize(map17PhaseExitVerdict);
            Map18HandoffApproved = map18HandoffApproved;
            Map17WarningCount = map17WarningCount;
            ExpectedIndexDigest = GeneratedContentPoolKey.Normalize(expectedIndexDigest);
            AttemptedActualPlacementOrSpawn = attemptedActualPlacementOrSpawn;
        }

        public IReadOnlyList<GeneratedContentSlotSourceRecord> Sources => sources;
        public int NullSourceRecordCount { get; }
        public string Map17AuditDigest { get; }
        public string Map17PhaseExitVerdict { get; }
        public bool Map18HandoffApproved { get; }
        public int Map17WarningCount { get; }
        public string ExpectedIndexDigest { get; }
        public bool AttemptedActualPlacementOrSpawn { get; }
    }

    public enum GeneratedContentSlotIndexFailureCode
    {
        MissingRequest = 1,
        MissingUpstreamSlotSource = 2,
        OutOfBoundsSectorCoordinate = 3,
        OutOfBoundsSliceIndex = 4,
        OutOfBoundsSectorLocalIndex = 5,
        OutOfBoundsSliceLocalIndex = 6,
        InvalidCategory = 7,
        InvalidPoolKey = 8,
        InvalidSourceOwner = 9,
        MissingSourceSlotOrProvenance = 10,
        SourceCoordinateMappingMismatch = 11,
        DuplicateContentSlotAddress = 12,
        DuplicateStableSpawnId = 13,
        ReservationKeyCollision = 14,
        UpstreamHandoffMismatch = 15,
        UnstableOrderOrDigestMismatch = 16,
        AttemptedActualPlacementOrSpawn = 17,
    }

    public sealed class GeneratedContentSlotIndexFailure :
        IComparable<GeneratedContentSlotIndexFailure>
    {
        public GeneratedContentSlotIndexFailure(
            GeneratedContentSlotIndexFailureCode code,
            string owner,
            string offendingKey,
            string expected,
            string actual,
            string reason)
        {
            Code = code;
            Owner = owner ?? string.Empty;
            OffendingKey = offendingKey ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            Reason = reason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                Code.ToString().ToUpperInvariant(), Owner, OffendingKey,
                Expected, Actual, Reason,
            });
        }

        public GeneratedContentSlotIndexFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedContentSlotIndexFailure other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedContentSlotIndexResult
    {
        private readonly ReadOnlyCollection<GeneratedContentSlotIndexFailure> failures;

        internal GeneratedContentSlotIndexResult(
            GeneratedContentSlotIndex index,
            IEnumerable<GeneratedContentSlotIndexFailure> sourceFailures,
            int sourceRecordCount)
        {
            Index = index;
            failures = new ReadOnlyCollection<GeneratedContentSlotIndexFailure>((sourceFailures ??
                Array.Empty<GeneratedContentSlotIndexFailure>()).OrderBy(value => value).ToArray());
            SourceRecordCount = sourceRecordCount;
        }

        public bool Success => Index != null && failures.Count == 0;
        public GeneratedContentSlotIndex Index { get; }
        public IReadOnlyList<GeneratedContentSlotIndexFailure> Failures => failures;
        public int SourceRecordCount { get; }
        public int PartialEntryCount => Success ? Index.Count : 0;
        public int PartialMutationCount => 0;
        public int RetryLoopCount => 0;
    }

    public static class GeneratedContentSlotIndexBuilder
    {
        public const string ExpectedMap17AuditDigest =
            "8b4849bf11ac6807a9e8a9d699a166eaa61e5c600454e410bae1ad47480545a0";
        public const string ExpectedMap17PhaseExitVerdict = "PASS";
        public const int ExpectedMap17WarningCount = 2;

        public static GeneratedContentSlotIndexResult Build(
            GeneratedContentSlotIndexRequest request)
        {
            var failures = new List<GeneratedContentSlotIndexFailure>();
            if (request == null)
            {
                failures.Add(Failure(GeneratedContentSlotIndexFailureCode.MissingRequest,
                    "MAP18_01", "REQUEST", "PRESENT", "MISSING", "Request is required."));
                return Result(null, failures, 0);
            }

            ValidateHandoff(request, failures);
            if (request.AttemptedActualPlacementOrSpawn)
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.AttemptedActualPlacementOrSpawn,
                    "MAP18_02_OR_LATER", "SIDE_EFFECT_REQUEST", "FALSE", "TRUE",
                    "MAP18_01 only builds immutable slot identities."));
            if (request.Sources.Count == 0 || request.Sources.All(value => value == null))
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.MissingUpstreamSlotSource,
                    "MAP16_06_GENERATED_MARKER_SLOT_POLICY_V1", "SOURCE_RECORDS", ">0", "0",
                    "Projected marker/slot/provenance input is required."));
            if (request.NullSourceRecordCount > 0)
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.MissingUpstreamSlotSource,
                    "MAP16_06_GENERATED_MARKER_SLOT_POLICY_V1", "NULL_SOURCE_RECORDS", "0",
                    Number(request.NullSourceRecordCount), "Null source records are forbidden."));

            var sources = request.Sources.Where(value => value != null).OrderBy(value => value).ToArray();
            foreach (var source in sources) ValidateSource(source, failures);
            if (failures.Count > 0) return Result(null, failures, request.Sources.Count);

            var entries = sources.Select(value => new GeneratedContentSlotIndexEntry(
                value.Address, GeneratedStableSpawnIdFactory.Create(value.Address),
                value.AvailableForMandatoryUniquePreplacement)).OrderBy(value => value).ToArray();

            foreach (var group in entries.GroupBy(value => value.Address.CanonicalLine,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.DuplicateContentSlotAddress,
                    "MAP18_01", group.Key, "UNIQUE", Number(group.Count()),
                    "Duplicate content slot address."));
            foreach (var group in entries.GroupBy(value => value.StableSpawnId.Value,
                         StringComparer.Ordinal).Where(value => value.Select(entry =>
                             entry.Address.CanonicalLine).Distinct(StringComparer.Ordinal).Count() > 1))
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.DuplicateStableSpawnId,
                    GeneratedStableSpawnIdFactory.Namespace, group.Key, "ONE_ADDRESS",
                    Number(group.Count()), "Stable spawn ID collision across different addresses."));
            foreach (var group in entries.GroupBy(value => value.ReservationKey,
                         StringComparer.Ordinal).Where(value => value.Select(entry =>
                             entry.Address.CanonicalLine).Distinct(StringComparer.Ordinal).Count() > 1))
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.ReservationKeyCollision,
                    "CONTENT_SLOT_RESERVATION_V1", group.Key, "ONE_ADDRESS",
                    Number(group.Count()), "Physical source slot has competing addresses."));
            if (failures.Count > 0) return Result(null, failures, request.Sources.Count);

            var index = new GeneratedContentSlotIndex(entries, request.Map17AuditDigest,
                request.Map17PhaseExitVerdict, request.Map18HandoffApproved,
                request.Map17WarningCount);
            if (!string.IsNullOrEmpty(request.ExpectedIndexDigest) &&
                !string.Equals(request.ExpectedIndexDigest, index.Digest, StringComparison.Ordinal))
            {
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.UnstableOrderOrDigestMismatch,
                    "MAP18_01", "INDEX_DIGEST", request.ExpectedIndexDigest, index.Digest,
                    "Computed stable index digest differs from the required digest."));
                return Result(null, failures, request.Sources.Count);
            }
            return Result(index, failures, request.Sources.Count);
        }

        private static void ValidateHandoff(
            GeneratedContentSlotIndexRequest request,
            ICollection<GeneratedContentSlotIndexFailure> failures)
        {
            if (!string.Equals(request.Map17AuditDigest, ExpectedMap17AuditDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(request.Map17PhaseExitVerdict, ExpectedMap17PhaseExitVerdict,
                    StringComparison.Ordinal) || !request.Map18HandoffApproved ||
                request.Map17WarningCount != ExpectedMap17WarningCount)
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.UpstreamHandoffMismatch,
                    "MAP17_08_MAP17_RUNTIME_EXIT_AUDIT", "HANDOFF",
                    ExpectedMap17AuditDigest + "|PASS|YES|2",
                    request.Map17AuditDigest + "|" + request.Map17PhaseExitVerdict + "|" +
                    (request.Map18HandoffApproved ? "YES" : "NO") + "|" +
                    Number(request.Map17WarningCount),
                    "MAP17 exit evidence must match the reviewed PASS handoff."));
        }

        private static void ValidateSource(
            GeneratedContentSlotSourceRecord source,
            ICollection<GeneratedContentSlotIndexFailure> failures)
        {
            var address = source.Address;
            if (address == null)
            {
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.MissingUpstreamSlotSource,
                    "MAP16_06_GENERATED_MARKER_SLOT_POLICY_V1", "ADDRESS", "PRESENT",
                    "MISSING", "Source address is required."));
                return;
            }
            if (address.Sector == null || !address.Sector.IsInWorld)
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.OutOfBoundsSectorCoordinate,
                    "WORLD_13X13", address.CanonicalLine, "x/y=0..12",
                    address.Sector == null ? "MISSING" : address.Sector.ToString(),
                    "Sector coordinate is outside the generated world."));
            if (address.SliceIndex < 0 || address.SliceIndex >= GeneratedMicroChunkSliceSet.ChunkCount)
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.OutOfBoundsSliceIndex,
                    "SECTOR_4X4_SLICES", address.CanonicalLine, "0..15",
                    Number(address.SliceIndex), "Slice index is outside the sector."));
            var local = new GeneratedSectorLocalCellIndex(address.SectorLocalIndex);
            if (!local.IsValid)
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.OutOfBoundsSectorLocalIndex,
                    "SECTOR_48X32", address.CanonicalLine, "0..1535",
                    Number(address.SectorLocalIndex), "Sector-local cell index is invalid."));
            if (address.SliceLocalIndex < -1 ||
                address.SliceLocalIndex >= GeneratedMicroChunkSliceSet.MicroChunkCellCount)
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.OutOfBoundsSliceLocalIndex,
                    "SLICE_12X8", address.CanonicalLine, "-1 or 0..95",
                    Number(address.SliceLocalIndex), "Slice-local cell index is invalid."));
            if (local.IsValid && address.SliceLocalIndex >= 0 &&
                address.SliceLocalIndex < GeneratedMicroChunkSliceSet.MicroChunkCellCount &&
                address.SliceIndex >= 0 && address.SliceIndex < GeneratedMicroChunkSliceSet.ChunkCount)
            {
                var expectedSlice = (local.SectorLocalY / GeneratedMicroChunkSliceSet.MicroChunkHeight) * 4 +
                    local.SectorLocalX / GeneratedMicroChunkSliceSet.MicroChunkWidth;
                var expectedSliceLocal = (local.SectorLocalY % GeneratedMicroChunkSliceSet.MicroChunkHeight) *
                    GeneratedMicroChunkSliceSet.MicroChunkWidth +
                    local.SectorLocalX % GeneratedMicroChunkSliceSet.MicroChunkWidth;
                if (address.SliceIndex != expectedSlice || address.SliceLocalIndex != expectedSliceLocal)
                    failures.Add(Failure(
                        GeneratedContentSlotIndexFailureCode.SourceCoordinateMappingMismatch,
                        "MAP16_SLICE_PROJECTION", address.CanonicalLine,
                        Number(expectedSlice) + "/" + Number(expectedSliceLocal),
                        Number(address.SliceIndex) + "/" + Number(address.SliceLocalIndex),
                        "Sector-local and slice-local coordinates disagree."));
            }
            if (!Enum.IsDefined(typeof(GeneratedContentSlotCategory), address.Category))
                failures.Add(Failure(GeneratedContentSlotIndexFailureCode.InvalidCategory,
                    "MAP18_01", address.CanonicalLine, "DEFINED", Number((int)address.Category),
                    "Content slot category is invalid."));
            if (address.PoolKey == null || !address.PoolKey.IsValid)
                failures.Add(Failure(GeneratedContentSlotIndexFailureCode.InvalidPoolKey,
                    "MAP18_02_OR_LATER", address.CanonicalLine, "NAMESPACE@VERSION",
                    address.PoolKey == null ? "MISSING" : address.PoolKey.ToString(),
                    "Pool key is invalid; no roll is performed."));
            if (!Enum.IsDefined(typeof(GeneratedMarkerSlotOwner), address.SourceOwnerKind))
                failures.Add(Failure(GeneratedContentSlotIndexFailureCode.InvalidSourceOwner,
                    "MAP16_06_GENERATED_MARKER_SLOT_POLICY_V1", address.CanonicalLine,
                    "DEFINED", Number((int)address.SourceOwnerKind), "Source owner is invalid."));
            if (!GeneratedContentPoolKey.IsSingleLine(address.WorldSeed) ||
                !GeneratedContentPoolKey.IsSingleLine(address.GeneratorVersion) ||
                !GeneratedContentPoolKey.IsSingleLine(address.DataVersion) ||
                !GeneratedContentPoolKey.IsSingleLine(address.SourceOwnerId) ||
                !GeneratedContentPoolKey.IsSingleLine(address.SourceProvenanceToken) ||
                !GeneratedContentPoolKey.IsSingleLine(address.SourceSlotId))
                failures.Add(Failure(
                    GeneratedContentSlotIndexFailureCode.MissingSourceSlotOrProvenance,
                    "MAP16_06_GENERATED_MARKER_SLOT_POLICY_V1", address.CanonicalLine,
                    "NON_EMPTY_SINGLE_LINE_IDENTITIES", "MISSING_OR_MULTILINE",
                    "Seed, versions, source slot, owner, and provenance must be stable identifiers."));
        }

        private static GeneratedContentSlotIndexFailure Failure(
            GeneratedContentSlotIndexFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => new GeneratedContentSlotIndexFailure(
                code, owner, key, expected, actual, reason);
        private static GeneratedContentSlotIndexResult Result(
            GeneratedContentSlotIndex index,
            IEnumerable<GeneratedContentSlotIndexFailure> failures,
            int sourceCount) => new GeneratedContentSlotIndexResult(index, failures, sourceCount);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public static class GeneratedContentSlotIndexDigest
    {
        public static string Compute(GeneratedContentSlotIndex index)
        {
            if (index == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedContentSlotIndex.PolicyVersion,
                "UPSTREAM|" + index.Map17AuditDigest + "|" + index.Map17PhaseExitVerdict + "|" +
                    (index.Map18HandoffApproved ? "YES" : "NO") + "|WARNINGS=" +
                    index.Map17WarningCount.ToString(CultureInfo.InvariantCulture),
                "COUNTS|" + string.Join("|", new[]
                {
                    index.Count.ToString(CultureInfo.InvariantCulture),
                    index.UniqueAddressCount.ToString(CultureInfo.InvariantCulture),
                    index.UniqueReservationKeyCount.ToString(CultureInfo.InvariantCulture),
                    index.UniqueStableSpawnIdCount.ToString(CultureInfo.InvariantCulture),
                    index.CategoryCount.ToString(CultureInfo.InvariantCulture),
                    index.PoolKeyCount.ToString(CultureInfo.InvariantCulture),
                    index.SourceOwnerKindCount.ToString(CultureInfo.InvariantCulture),
                }),
                "STABLE_ID_SET|" + index.StableIdSetDigest,
                "DOWNSTREAM|" + GeneratedContentSlotIndex.DownstreamOwner + "|0",
            };
            lines.AddRange(index.Entries.Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }
    }
}
