using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedSaveManifestVersion :
        IEquatable<GeneratedSaveManifestVersion>
    {
        public GeneratedSaveManifestVersion(
            string schemaVersion,
            string generatorVersion,
            string dataVersion)
        {
            SchemaVersion = Normalize(schemaVersion);
            GeneratorVersion = Normalize(generatorVersion);
            DataVersion = Normalize(dataVersion);
        }

        public string SchemaVersion { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public bool IsSupported => string.Equals(SchemaVersion,
            GeneratedSaveManifestService.SchemaVersion, StringComparison.Ordinal) &&
            !string.IsNullOrEmpty(GeneratorVersion) && !string.IsNullOrEmpty(DataVersion);
        public string StableToken => string.Join("|", new[]
        {
            "SAVE_MANIFEST_VERSION", SchemaVersion, GeneratorVersion, DataVersion,
        });
        public bool Equals(GeneratedSaveManifestVersion other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedSaveManifestVersion);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public sealed class GeneratedSaveManifestHeader
    {
        public GeneratedSaveManifestHeader(
            GeneratedSaveManifestVersion version,
            string seedIdentity,
            string geometryDigest,
            string placementDigest,
            string bakeDigest,
            string cacheDigest,
            string windowHandleDigest,
            string storageDigest,
            int modifiedSectorCount)
        {
            Version = version;
            SeedIdentity = Normalize(seedIdentity);
            GeometryDigest = geometryDigest ?? string.Empty;
            PlacementDigest = placementDigest ?? string.Empty;
            BakeDigest = bakeDigest ?? string.Empty;
            CacheDigest = cacheDigest ?? string.Empty;
            WindowHandleDigest = windowHandleDigest ?? string.Empty;
            StorageDigest = storageDigest ?? string.Empty;
            ModifiedSectorCount = modifiedSectorCount;
        }

        public GeneratedSaveManifestVersion Version { get; }
        public string SeedIdentity { get; }
        public string GeometryDigest { get; }
        public string PlacementDigest { get; }
        public string BakeDigest { get; }
        public string CacheDigest { get; }
        public string WindowHandleDigest { get; }
        public string StorageDigest { get; }
        public int ModifiedSectorCount { get; }
        public int PublishedFieldCount => 10;
        public bool IsValid => Version != null && Version.IsSupported &&
            !string.IsNullOrEmpty(SeedIdentity) && ModifiedSectorCount >= 0 &&
            ModifiedSectorCount <= GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount &&
            BakingCanonicalDigest.IsLowerHexSha256(GeometryDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(PlacementDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(BakeDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(CacheDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(WindowHandleDigest) &&
            BakingCanonicalDigest.IsLowerHexSha256(StorageDigest);
        public string StableToken => string.Join("|", new[]
        {
            "SAVE_MANIFEST_HEADER", Version == null ? "MISSING" : Version.StableToken,
            SeedIdentity, GeometryDigest, PlacementDigest, BakeDigest, CacheDigest,
            WindowHandleDigest, StorageDigest, Number(ModifiedSectorCount),
        });
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedSaveManifestRecordPayload :
        IComparable<GeneratedSaveManifestRecordPayload>
    {
        public GeneratedSaveManifestRecordPayload(GeneratedSectorModificationRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            StableId = record.Id.Value;
            Sector = record.Target.Sector;
            LocalIndex = record.Target.LocalIndex.Value;
            LayerId = record.Target.LayerId;
            SourceProvenanceToken = record.Target.SourceProvenanceToken;
            SlotReference = record.Target.SlotReference;
            Kind = record.Kind;
            Revision = record.Revision;
            OldTileCode = record.Payload.OldTileCode;
            OldSourceToken = record.Payload.OldSourceToken;
            NewTileCode = record.Payload.NewTileCode;
            NewSourceToken = record.Payload.NewSourceToken;
            StateKey = record.Payload.StateKey;
            StateValue = record.Payload.StateValue;
            LogicalRemoved = record.Payload.LogicalRemoved;
            Collected = record.Payload.Collected;
            Consumed = record.Payload.Consumed;
            BaseDigests = record.BaseDigests;
            SourceDigest = record.SourceDigest;
            StableToken = BuildStableToken();
        }

        public GeneratedSaveManifestRecordPayload(
            string stableId,
            GeneratedSectorCoordinate sector,
            int localIndex,
            int layerId,
            string sourceProvenanceToken,
            string slotReference,
            GeneratedSectorModificationKind kind,
            int revision,
            string oldTileCode,
            string oldSourceToken,
            string newTileCode,
            string newSourceToken,
            string stateKey,
            string stateValue,
            bool logicalRemoved,
            bool collected,
            bool consumed,
            GeneratedSectorModificationBaseDigests baseDigests,
            string sourceDigest)
        {
            StableId = stableId ?? string.Empty;
            Sector = sector;
            LocalIndex = localIndex;
            LayerId = layerId;
            SourceProvenanceToken = Normalize(sourceProvenanceToken);
            SlotReference = Normalize(slotReference);
            Kind = kind;
            Revision = revision;
            OldTileCode = Normalize(oldTileCode);
            OldSourceToken = Normalize(oldSourceToken);
            NewTileCode = Normalize(newTileCode);
            NewSourceToken = Normalize(newSourceToken);
            StateKey = Normalize(stateKey);
            StateValue = Normalize(stateValue);
            LogicalRemoved = logicalRemoved;
            Collected = collected;
            Consumed = consumed;
            BaseDigests = baseDigests;
            SourceDigest = sourceDigest ?? string.Empty;
            StableToken = BuildStableToken();
        }

        public string StableId { get; }
        public GeneratedSectorCoordinate Sector { get; }
        public int LocalIndex { get; }
        public int LayerId { get; }
        public string SourceProvenanceToken { get; }
        public string SlotReference { get; }
        public GeneratedSectorModificationKind Kind { get; }
        public int Revision { get; }
        public string OldTileCode { get; }
        public string OldSourceToken { get; }
        public string NewTileCode { get; }
        public string NewSourceToken { get; }
        public string StateKey { get; }
        public string StateValue { get; }
        public bool LogicalRemoved { get; }
        public bool Collected { get; }
        public bool Consumed { get; }
        public GeneratedSectorModificationBaseDigests BaseDigests { get; }
        public string SourceDigest { get; }
        public string StableToken { get; }

        public GeneratedSectorModificationRecord ToModificationRecord(
            GeneratedSaveManifestHeader header)
        {
            var target = new GeneratedSectorModificationTarget(Sector,
                new GeneratedSectorLocalCellIndex(LocalIndex), LayerId,
                SourceProvenanceToken, SlotReference);
            var payload = new GeneratedSectorModificationPayload(OldTileCode,
                OldSourceToken, NewTileCode, NewSourceToken, StateKey, StateValue,
                LogicalRemoved, Collected, Consumed);
            var id = new GeneratedSectorModificationStableId(
                header == null ? string.Empty : header.SeedIdentity,
                header == null || header.Version == null
                    ? string.Empty : header.Version.GeneratorVersion,
                header == null || header.Version == null
                    ? string.Empty : header.Version.DataVersion,
                target, Kind, GeneratedSectorModificationStore.SchemaVersion);
            return new GeneratedSectorModificationRecord(id, target, Kind,
                Revision, payload, BaseDigests);
        }

        public int CompareTo(GeneratedSaveManifestRecordPayload other) => other == null
            ? -1 : string.Compare(StableId, other.StableId, StringComparison.Ordinal);

        private string BuildStableToken() => string.Join("|", new[]
        {
            "SAVE_MANIFEST_RECORD", StableId,
            Sector == null ? "MISSING" : Sector.ToString(), Number(LocalIndex),
            Number(LayerId), SourceProvenanceToken, SlotReference,
            Kind.ToString().ToUpperInvariant(), Number(Revision),
            OldTileCode, OldSourceToken, NewTileCode, NewSourceToken,
            StateKey, StateValue, LogicalRemoved ? "1" : "0",
            Collected ? "1" : "0", Consumed ? "1" : "0",
            GeneratedSectorModificationDigest.ComputeBase(BaseDigests), SourceDigest,
        });
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedModifiedSectorManifestEntry :
        IComparable<GeneratedModifiedSectorManifestEntry>
    {
        private readonly ReadOnlyCollection<GeneratedSaveManifestRecordPayload> records;

        public GeneratedModifiedSectorManifestEntry(
            GeneratedSectorCoordinate sector,
            int dirtyRevision,
            GeneratedSectorModificationBaseDigests baseDigests,
            string modificationSetDigest,
            IEnumerable<GeneratedSaveManifestRecordPayload> sourceRecords)
        {
            Sector = sector;
            DirtyRevision = dirtyRevision;
            BaseDigests = baseDigests;
            ModificationSetDigest = modificationSetDigest ?? string.Empty;
            records = new ReadOnlyCollection<GeneratedSaveManifestRecordPayload>((sourceRecords ??
                Array.Empty<GeneratedSaveManifestRecordPayload>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            StableToken = string.Join("|", new[]
            {
                "MODIFIED_SECTOR_MANIFEST_ENTRY",
                Sector == null ? "MISSING" : Sector.ToString(),
                Number(DirtyRevision), GeneratedSectorModificationDigest.ComputeBase(BaseDigests),
                ModificationSetDigest, Number(records.Count),
            });
        }

        public GeneratedSectorCoordinate Sector { get; }
        public int DirtyRevision { get; }
        public GeneratedSectorModificationBaseDigests BaseDigests { get; }
        public string ModificationSetDigest { get; }
        public IReadOnlyList<GeneratedSaveManifestRecordPayload> Records => records;
        public int RecordCount => records.Count;
        public int DuplicateRecordIdCount => records.Count -
            records.Select(value => value.StableId).Distinct(StringComparer.Ordinal).Count();
        public string StableToken { get; }
        public int CompareTo(GeneratedModifiedSectorManifestEntry other) => other == null
            ? -1 : Sector.CompareTo(other.Sector);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedWorldSaveManifest
    {
        private readonly ReadOnlyCollection<GeneratedModifiedSectorManifestEntry> entries;

        public GeneratedWorldSaveManifest(
            GeneratedSaveManifestHeader header,
            IEnumerable<GeneratedModifiedSectorManifestEntry> sourceEntries)
        {
            Header = header;
            entries = new ReadOnlyCollection<GeneratedModifiedSectorManifestEntry>((sourceEntries ??
                Array.Empty<GeneratedModifiedSectorManifestEntry>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            Digest = GeneratedSaveManifestDigest.ComputeManifest(this);
        }

        public GeneratedSaveManifestHeader Header { get; }
        public IReadOnlyList<GeneratedModifiedSectorManifestEntry> ModifiedSectorEntries => entries;
        public int ModifiedSectorCount => entries.Count;
        public int UnmodifiedSectorCount => Math.Max(0,
            GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount - entries.Count);
        public int ModificationRecordCount => entries.Sum(value => value.RecordCount);
        public int DuplicateSectorEntryCount => entries.Count - entries
            .Select(value => value.Sector).Distinct().Count();
        public int FullTileDataEntryCount => 0;
        public int UnityObjectIdCount => 0;
        public int FilePathCount => 0;
        public int TimestampCount => 0;
        public int FrameCountValueCount => 0;
        public int PopulationStableSpawnIdCount => 0;
        public string Digest { get; }
        public GeneratedModifiedSectorManifestEntry Find(GeneratedSectorCoordinate sector) =>
            sector == null ? null : entries.FirstOrDefault(value => value.Sector.Equals(sector));
    }

    public sealed class GeneratedSaveManifestPayload
    {
        public GeneratedSaveManifestPayload(string canonicalText, string declaredDigest = null)
        {
            CanonicalText = BakingCanonicalDigest.NormalizeLineEndingsToLf(canonicalText ?? string.Empty);
            Bytes = new ReadOnlyCollection<byte>(
                BakingCanonicalDigest.Utf8NoBomEncoding.GetBytes(CanonicalText));
            ComputedDigest = GeneratedSaveManifestDigest.ComputePayload(CanonicalText);
            Digest = declaredDigest ?? ComputedDigest;
        }

        public string CanonicalText { get; }
        public IReadOnlyList<byte> Bytes { get; }
        public string ComputedDigest { get; }
        public string Digest { get; }
        public bool DigestMatches => string.Equals(Digest, ComputedDigest, StringComparison.Ordinal);
        public bool IsUtf8WithoutBom => Bytes.Count < 3 ||
            !(Bytes[0] == 0xef && Bytes[1] == 0xbb && Bytes[2] == 0xbf);
        public int DiskReadCount => 0;
        public int DiskWriteCount => 0;
    }

    public enum GeneratedSaveManifestValidationFailureCode
    {
        MissingManifest = 1,
        InvalidHeader = 2,
        UnsupportedVersion = 3,
        ModifiedSectorCountMismatch = 4,
        DuplicateSectorEntry = 5,
        DuplicateRecordId = 6,
        InvalidSector = 7,
        UnmodifiedSectorEntry = 8,
        UnknownField = 9,
        InvalidPayload = 10,
        PayloadHashMismatch = 11,
        ManifestHashMismatch = 12,
        RecordHashMismatch = 13,
        ModificationSetHashMismatch = 14,
        SeedMismatch = 15,
        GeneratorVersionMismatch = 16,
        DataVersionMismatch = 17,
        GeometryDigestMismatch = 18,
        PlacementDigestMismatch = 19,
        BakeDigestMismatch = 20,
        CacheDigestMismatch = 21,
        WindowHandleDigestMismatch = 22,
        StorageDigestMismatch = 23,
        MissingTarget = 24,
        MissingEntry = 25,
    }

    public sealed class GeneratedSaveManifestValidationFailure :
        IEquatable<GeneratedSaveManifestValidationFailure>,
        IComparable<GeneratedSaveManifestValidationFailure>
    {
        public GeneratedSaveManifestValidationFailure(
            GeneratedSaveManifestValidationFailureCode code,
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
        }

        public GeneratedSaveManifestValidationFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken => string.Join("|", new[]
        {
            Code.ToString().ToUpperInvariant(), Owner, OffendingKey,
            Expected, Actual, Reason,
        });
        public int CompareTo(GeneratedSaveManifestValidationFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            return string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedSaveManifestValidationFailure other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            Equals(obj as GeneratedSaveManifestValidationFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedSaveManifestResult
    {
        private readonly ReadOnlyCollection<GeneratedSaveManifestValidationFailure> failures;

        internal GeneratedSaveManifestResult(
            GeneratedWorldSaveManifest manifest,
            GeneratedSaveManifestPayload payload,
            IEnumerable<GeneratedSaveManifestValidationFailure> sourceFailures)
        {
            Manifest = manifest;
            Payload = payload;
            failures = new ReadOnlyCollection<GeneratedSaveManifestValidationFailure>(
                (sourceFailures ?? Array.Empty<GeneratedSaveManifestValidationFailure>())
                .Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Manifest != null && failures.Count == 0;
        public GeneratedWorldSaveManifest Manifest { get; }
        public GeneratedSaveManifestPayload Payload { get; }
        public IReadOnlyList<GeneratedSaveManifestValidationFailure> Failures => failures;
    }

    public static class GeneratedSaveManifestDigest
    {
        public static string ComputeManifest(GeneratedWorldSaveManifest manifest)
        {
            if (manifest == null) return string.Empty;
            var lines = new List<string>
            {
                manifest.Header == null ? "HEADER|MISSING" : manifest.Header.StableToken,
                "COUNTS|" + Number(manifest.ModifiedSectorCount) + "|" +
                    Number(manifest.UnmodifiedSectorCount) + "|" +
                    Number(manifest.ModificationRecordCount),
                "EXCLUDED|0|0|0|0|0|0",
            };
            foreach (var entry in manifest.ModifiedSectorEntries.OrderBy(value => value))
            {
                lines.Add(entry.StableToken);
                lines.AddRange(entry.Records.OrderBy(value => value)
                    .Select(value => value.StableToken));
            }
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputePayload(string canonicalText) =>
            BakingCanonicalDigest.HashCanonicalText(canonicalText ?? string.Empty);

        public static string ComputeRegenerationApply(
            GeneratedSectorRegenerationApplyPlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "REGENERATION_APPLY|" + GeneratedSaveManifestService.SchemaVersion,
                "MANIFEST|" + plan.Manifest.Digest,
                "ENTRY|" + plan.Entry.StableToken,
                "SOURCE|" + plan.SourceLogicalRecordDigest,
                "OUTPUT|" + plan.OutputModificationSet.Digest,
                "COUNTS|" + Number(plan.CommandCount) + "|" +
                    Number(plan.InputInPlaceMutationCount),
                "NO_SIDE_EFFECTS|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0",
            };
            lines.AddRange(plan.Commands.OrderBy(value => value)
                .Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
