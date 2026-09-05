using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedFailureBundleSourceKind
    {
        Reference = 0,
        FocusedFixture = 1,
    }

    public enum GeneratedFailureBundlePassDecision
    {
        Pass = 0,
        Fail = 1,
    }

    public sealed class GeneratedFailureBundleSeedIdentity
    {
        public GeneratedFailureBundleSeedIdentity(ulong worldSeed,
            GeneratedFailureBundleSourceKind sourceKind, string contentHash)
        {
            WorldSeed = worldSeed;
            SourceKind = sourceKind;
            ContentHash = Normalize(contentHash);
        }

        public ulong WorldSeed { get; }
        public GeneratedFailureBundleSourceKind SourceKind { get; }
        public string ContentHash { get; }
        public bool IsValid => Enum.IsDefined(typeof(GeneratedFailureBundleSourceKind), SourceKind) &&
            BakingCanonicalDigest.IsLowerHexSha256(ContentHash);
        public string StableToken => Join("SEED", Number(WorldSeed), Number((int)SourceKind), ContentHash);

        public static GeneratedFailureBundleSeedIdentity FromSaveManifest(ulong worldSeed,
            GeneratedFailureBundleSourceKind sourceKind, GeneratedSaveManifestHeader header) =>
            new GeneratedFailureBundleSeedIdentity(worldSeed, sourceKind,
                header == null ? string.Empty : BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP19_08_SAVE_MANIFEST_CONTENT", header.StableToken,
                }));

        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        internal static string Number(ulong value) => value.ToString(CultureInfo.InvariantCulture);
        internal static string Join(params string[] values) => string.Join("|", values);
    }

    public sealed class GeneratedFailureBundleVersionIdentity
    {
        public GeneratedFailureBundleVersionIdentity(string bundleSchemaVersion,
            string generatorVersion, string dataVersion)
        {
            BundleSchemaVersion = GeneratedFailureBundleSeedIdentity.Normalize(bundleSchemaVersion);
            GeneratorVersion = GeneratedFailureBundleSeedIdentity.Normalize(generatorVersion);
            DataVersion = GeneratedFailureBundleSeedIdentity.Normalize(dataVersion);
        }

        public string BundleSchemaVersion { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public bool IsValid => IsToken(BundleSchemaVersion) && IsToken(GeneratorVersion) &&
            IsToken(DataVersion);
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join("VERSION",
            BundleSchemaVersion, GeneratorVersion, DataVersion);

        public static GeneratedFailureBundleVersionIdentity FromSaveManifest(
            GeneratedSaveManifestVersion version) => new GeneratedFailureBundleVersionIdentity(
                GeneratedFailureBundleManifest.SchemaVersion,
                version == null ? string.Empty : version.GeneratorVersion,
                version == null ? string.Empty : version.DataVersion);

        internal static bool IsToken(string value) => !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0;
    }

    public sealed class GeneratedFailureBundlePassSnapshot : IComparable<GeneratedFailureBundlePassSnapshot>
    {
        public GeneratedFailureBundlePassSnapshot(string passId,
            GeneratedFailureBundlePassDecision decision, string detail, string sourceDigest)
        {
            PassId = GeneratedFailureBundleSeedIdentity.Normalize(passId);
            Decision = decision;
            Detail = GeneratedFailureBundleSeedIdentity.Normalize(detail);
            SourceDigest = GeneratedFailureBundleSeedIdentity.Normalize(sourceDigest);
        }

        public string PassId { get; }
        public GeneratedFailureBundlePassDecision Decision { get; }
        public string Detail { get; }
        public string SourceDigest { get; }
        public bool IsValid => GeneratedFailureBundleVersionIdentity.IsToken(PassId) &&
            Enum.IsDefined(typeof(GeneratedFailureBundlePassDecision), Decision) &&
            GeneratedFailureBundleVersionIdentity.IsToken(Detail) &&
            BakingCanonicalDigest.IsLowerHexSha256(SourceDigest);
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join("PASS", PassId,
            GeneratedFailureBundleSeedIdentity.Number((int)Decision), Detail, SourceDigest);
        public int CompareTo(GeneratedFailureBundlePassSnapshot other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class GeneratedFailureBundleFailureRecord : IComparable<GeneratedFailureBundleFailureRecord>
    {
        public GeneratedFailureBundleFailureRecord(string owner, string reason, string bundleOrRequestId,
            string offendingKey, string expected, string actual, string sourceDigest, string outputPath = "",
            string failedRuleId = "")
        {
            Owner = N(owner);
            Reason = N(reason);
            FailedRuleId = N(string.IsNullOrWhiteSpace(failedRuleId) ? reason : failedRuleId);
            BundleOrRequestId = N(bundleOrRequestId);
            OffendingKey = N(offendingKey);
            Expected = N(expected);
            Actual = N(actual);
            SourceDigest = N(sourceDigest);
            OutputPath = N(outputPath);
        }

        public string Owner { get; }
        public string Reason { get; }
        public string FailedRuleId { get; }
        public string BundleOrRequestId { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public string OutputPath { get; }
        public bool IsComplete => GeneratedFailureBundleVersionIdentity.IsToken(Owner) &&
            GeneratedFailureBundleVersionIdentity.IsToken(Reason) &&
            GeneratedFailureBundleVersionIdentity.IsToken(FailedRuleId) &&
            GeneratedFailureBundleVersionIdentity.IsToken(BundleOrRequestId) &&
            GeneratedFailureBundleVersionIdentity.IsToken(OffendingKey) &&
            GeneratedFailureBundleVersionIdentity.IsToken(Expected) &&
            GeneratedFailureBundleVersionIdentity.IsToken(Actual) &&
            BakingCanonicalDigest.IsLowerHexSha256(SourceDigest);
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join("FAILURE", Owner, Reason,
            FailedRuleId,
            BundleOrRequestId, OffendingKey, Expected, Actual, SourceDigest, OutputPath);
        public int CompareTo(GeneratedFailureBundleFailureRecord other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public sealed class GeneratedFailureBundleCoordinateRecord : IComparable<GeneratedFailureBundleCoordinateRecord>
    {
        public GeneratedFailureBundleCoordinateRecord(int sectorX, int sectorY, int sliceX, int sliceY,
            int cellX, int cellY, string nodeId, string edgeId, string markerId, string scenarioId)
        {
            SectorX = sectorX;
            SectorY = sectorY;
            SliceX = sliceX;
            SliceY = sliceY;
            CellX = cellX;
            CellY = cellY;
            NodeId = N(nodeId);
            EdgeId = N(edgeId);
            MarkerId = N(markerId);
            ScenarioId = N(scenarioId);
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public int SliceX { get; }
        public int SliceY { get; }
        public int CellX { get; }
        public int CellY { get; }
        public string NodeId { get; }
        public string EdgeId { get; }
        public string MarkerId { get; }
        public string ScenarioId { get; }
        public bool IsValid => SectorX >= 0 && SectorY >= 0 && SliceX >= 0 && SliceY >= 0 &&
            CellX >= 0 && CellY >= 0 && new[] { NodeId, EdgeId, MarkerId, ScenarioId }
                .All(GeneratedFailureBundleVersionIdentity.IsToken);
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join("COORDINATE",
            Num(SectorX), Num(SectorY), Num(SliceX), Num(SliceY), Num(CellX), Num(CellY),
            NodeId, EdgeId, MarkerId, ScenarioId);
        public int CompareTo(GeneratedFailureBundleCoordinateRecord other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
        private static string Num(int value) => GeneratedFailureBundleSeedIdentity.Number(value);
    }

    public sealed class GeneratedFailureBundleProvenanceRecord : IComparable<GeneratedFailureBundleProvenanceRecord>
    {
        public GeneratedFailureBundleProvenanceRecord(string owner, string key, string value, string sourceDigest)
        {
            Owner = N(owner);
            Key = N(key);
            Value = N(value);
            SourceDigest = N(sourceDigest);
        }

        public string Owner { get; }
        public string Key { get; }
        public string Value { get; }
        public string SourceDigest { get; }
        public bool IsValid => GeneratedFailureBundleVersionIdentity.IsToken(Owner) &&
            GeneratedFailureBundleVersionIdentity.IsToken(Key) &&
            GeneratedFailureBundleVersionIdentity.IsToken(Value) &&
            BakingCanonicalDigest.IsLowerHexSha256(SourceDigest);
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join("PROVENANCE", Owner, Key,
            Value, SourceDigest);
        public int CompareTo(GeneratedFailureBundleProvenanceRecord other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public sealed class GeneratedFailureBundleAttachmentReference : IComparable<GeneratedFailureBundleAttachmentReference>
    {
        public GeneratedFailureBundleAttachmentReference(string role, string path, string coordinateFocus,
            string expectedLaterProducer)
        {
            Role = N(role);
            Path = N(path);
            CoordinateFocus = N(coordinateFocus);
            ExpectedLaterProducer = N(expectedLaterProducer);
        }

        public string Role { get; }
        public string Path { get; }
        public string CoordinateFocus { get; }
        public string ExpectedLaterProducer { get; }
        public bool IsValid => GeneratedFailureBundleVersionIdentity.IsToken(Role) &&
            GeneratedFailureBundleVersionIdentity.IsToken(Path) &&
            GeneratedFailureBundleVersionIdentity.IsToken(CoordinateFocus) &&
            GeneratedFailureBundleVersionIdentity.IsToken(ExpectedLaterProducer);
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join("ATTACHMENT", Role, Path,
            CoordinateFocus, ExpectedLaterProducer);
        public int CompareTo(GeneratedFailureBundleAttachmentReference other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public sealed class GeneratedFailureBundleUpstreamDigest : IComparable<GeneratedFailureBundleUpstreamDigest>
    {
        public GeneratedFailureBundleUpstreamDigest(string phaseId, string digest)
        {
            PhaseId = GeneratedFailureBundleSeedIdentity.Normalize(phaseId);
            Digest = GeneratedFailureBundleSeedIdentity.Normalize(digest);
        }

        public string PhaseId { get; }
        public string Digest { get; }
        public bool IsValid => GeneratedFailureBundleVersionIdentity.IsToken(PhaseId) &&
            BakingCanonicalDigest.IsLowerHexSha256(Digest);
        public string StableToken => GeneratedFailureBundleSeedIdentity.Join("UPSTREAM", PhaseId, Digest);
        public int CompareTo(GeneratedFailureBundleUpstreamDigest other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class GeneratedFailureBundleManifest
    {
        public const string SchemaVersion = "MAP19_FAILURE_BUNDLE_V1";
        private readonly ReadOnlyCollection<GeneratedFailureBundlePassSnapshot> passes;
        private readonly ReadOnlyCollection<GeneratedFailureBundleFailureRecord> failures;
        private readonly ReadOnlyCollection<GeneratedFailureBundleCoordinateRecord> coordinates;
        private readonly ReadOnlyCollection<GeneratedFailureBundleProvenanceRecord> provenance;
        private readonly ReadOnlyCollection<GeneratedFailureBundleAttachmentReference> attachments;
        private readonly ReadOnlyCollection<GeneratedFailureBundleUpstreamDigest> upstream;

        public GeneratedFailureBundleManifest(string bundleId,
            GeneratedFailureBundleSeedIdentity seedIdentity,
            GeneratedFailureBundleVersionIdentity versionIdentity,
            string validationPhaseId, string failingTaskId,
            IEnumerable<GeneratedFailureBundlePassSnapshot> passSnapshots,
            IEnumerable<GeneratedFailureBundleFailureRecord> failureRecords,
            IEnumerable<GeneratedFailureBundleCoordinateRecord> coordinateRecords,
            IEnumerable<GeneratedFailureBundleProvenanceRecord> provenanceRecords,
            IEnumerable<GeneratedFailureBundleAttachmentReference> attachmentReferences,
            IEnumerable<GeneratedFailureBundleUpstreamDigest> upstreamDigests,
            string createdAt = "")
        {
            BundleId = N(bundleId);
            SeedIdentity = seedIdentity;
            VersionIdentity = versionIdentity;
            ValidationPhaseId = N(validationPhaseId);
            FailingTaskId = N(failingTaskId);
            CreatedAt = N(createdAt);
            passes = Read(passSnapshots);
            failures = Read(failureRecords);
            coordinates = Read(coordinateRecords);
            provenance = Read(provenanceRecords);
            attachments = Read(attachmentReferences);
            upstream = Read(upstreamDigests);
            ManifestDigest = GeneratedFailureBundleDigest.Manifest(this);
        }

        public string BundleId { get; }
        public GeneratedFailureBundleSeedIdentity SeedIdentity { get; }
        public GeneratedFailureBundleVersionIdentity VersionIdentity { get; }
        public string ValidationPhaseId { get; }
        public string FailingTaskId { get; }
        public string CreatedAt { get; }
        public IReadOnlyList<GeneratedFailureBundlePassSnapshot> PassSnapshots => passes;
        public IReadOnlyList<GeneratedFailureBundleFailureRecord> FailureRecords => failures;
        public IReadOnlyList<GeneratedFailureBundleCoordinateRecord> CoordinateRecords => coordinates;
        public IReadOnlyList<GeneratedFailureBundleProvenanceRecord> ProvenanceRecords => provenance;
        public IReadOnlyList<GeneratedFailureBundleAttachmentReference> AttachmentReferences => attachments;
        public IReadOnlyList<GeneratedFailureBundleUpstreamDigest> UpstreamDigests => upstream;
        public string ManifestDigest { get; }
        public GeneratedFailureBundlePassDecision PassDecision => failures.Count == 0
            ? GeneratedFailureBundlePassDecision.Pass : GeneratedFailureBundlePassDecision.Fail;

        internal IEnumerable<string> CanonicalLines()
        {
            yield return "SCHEMA|" + SchemaVersion;
            yield return "BUNDLE|" + BundleId;
            yield return SeedIdentity == null ? "SEED|MISSING" : SeedIdentity.StableToken;
            yield return VersionIdentity == null ? "VERSION|MISSING" : VersionIdentity.StableToken;
            yield return "PHASE|" + ValidationPhaseId;
            yield return "TASK|" + FailingTaskId;
            yield return "DECISION|" + GeneratedFailureBundleSeedIdentity.Number((int)PassDecision);
            foreach (var value in passes) yield return value.StableToken;
            foreach (var value in failures) yield return value.StableToken;
            foreach (var value in coordinates) yield return value.StableToken;
            foreach (var value in provenance) yield return value.StableToken;
            foreach (var value in attachments) yield return value.StableToken;
            foreach (var value in upstream) yield return value.StableToken;
        }

        private static ReadOnlyCollection<T> Read<T>(IEnumerable<T> source) where T : class,
            IComparable<T> => new ReadOnlyCollection<T>((source ?? Array.Empty<T>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
        private static string N(string value) => GeneratedFailureBundleSeedIdentity.Normalize(value);
    }

    public static class GeneratedFailureBundleDigest
    {
        public static string Schema => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            GeneratedFailureBundleManifest.SchemaVersion,
            "bundle-id|schema-version|generator-version|data-version|world-seed|source-kind|content-hash",
            "phase|task|decision|owner|reason|key|expected|actual|coordinates|provenance|attachments|created-at-excluded",
        });

        public static string Manifest(GeneratedFailureBundleManifest manifest) =>
            BakingCanonicalDigest.HashCanonicalLines(manifest == null
                ? new[] { "MISSING_MANIFEST" } : manifest.CanonicalLines());

        public static string Payload(GeneratedFailureBundlePayload payload) =>
            BakingCanonicalDigest.HashCanonicalLines(payload == null ? new[] { "MISSING_PAYLOAD" } :
                new[] { payload.Manifest.ManifestDigest, payload.PassSnapshotsCsv, payload.FailuresCsv,
                    payload.CoordinatesCsv, payload.ProvenanceCsv, payload.ScreenshotReferencesCsv });
    }

    public sealed class GeneratedFailureBundlePayload
    {
        internal GeneratedFailureBundlePayload(GeneratedFailureBundleManifest manifest,
            string manifestJson, string passSnapshotsCsv, string failuresCsv, string coordinatesCsv,
            string provenanceCsv, string screenshotReferencesCsv)
        {
            Manifest = manifest;
            ManifestJson = manifestJson;
            PassSnapshotsCsv = passSnapshotsCsv;
            FailuresCsv = failuresCsv;
            CoordinatesCsv = coordinatesCsv;
            ProvenanceCsv = provenanceCsv;
            ScreenshotReferencesCsv = screenshotReferencesCsv;
            PayloadDigest = GeneratedFailureBundleDigest.Payload(this);
        }

        public GeneratedFailureBundleManifest Manifest { get; }
        public string ManifestJson { get; }
        public string PassSnapshotsCsv { get; }
        public string FailuresCsv { get; }
        public string CoordinatesCsv { get; }
        public string ProvenanceCsv { get; }
        public string ScreenshotReferencesCsv { get; }
        public string PayloadDigest { get; }
    }

    public sealed class GeneratedFailureBundleCreationResult
    {
        internal GeneratedFailureBundleCreationResult(GeneratedFailureBundlePayload payload,
            IEnumerable<GeneratedFailureBundleFailureRecord> sourceFailures)
        {
            Payload = payload;
            Failures = new ReadOnlyCollection<GeneratedFailureBundleFailureRecord>((sourceFailures ??
                Array.Empty<GeneratedFailureBundleFailureRecord>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Payload != null && Failures.Count == 0;
        public GeneratedFailureBundlePayload Payload { get; }
        public IReadOnlyList<GeneratedFailureBundleFailureRecord> Failures { get; }
        public string SuccessManifestDigest => Success ? Payload.Manifest.ManifestDigest : string.Empty;
        public string SuccessPayloadDigest => Success ? Payload.PayloadDigest : string.Empty;
    }

    public static class GeneratedFailureBundleFactory
    {
        public static GeneratedFailureBundleCreationResult Create(GeneratedFailureBundleManifest manifest)
        {
            var validation = Validate(manifest).ToArray();
            if (validation.Length != 0)
                return new GeneratedFailureBundleCreationResult(null, validation);
            return new GeneratedFailureBundleCreationResult(Serialize(manifest),
                Array.Empty<GeneratedFailureBundleFailureRecord>());
        }

        private static IEnumerable<GeneratedFailureBundleFailureRecord> Validate(
            GeneratedFailureBundleManifest manifest)
        {
            var source = manifest == null ? "MISSING" : manifest.ManifestDigest;
            if (manifest == null)
            {
                yield return Failure("Bundle", "MISSING_MANIFEST", "MISSING", "manifest",
                    "NON_NULL", "NULL", source);
                yield break;
            }
            if (!GeneratedFailureBundleVersionIdentity.IsToken(manifest.BundleId))
                yield return Failure("Bundle", "MISSING_BUNDLE_ID", manifest.BundleId, "bundleId",
                    "STABLE_TOKEN", "MISSING", source);
            if (manifest.SeedIdentity == null || !manifest.SeedIdentity.IsValid)
                yield return Failure("Identity", "INVALID_SEED_OR_HASH", manifest.BundleId, "seedIdentity",
                    "SEED_AND_LOWER_SHA256", "INVALID", source);
            if (manifest.VersionIdentity == null || !manifest.VersionIdentity.IsValid ||
                !string.Equals(manifest.VersionIdentity.BundleSchemaVersion,
                    GeneratedFailureBundleManifest.SchemaVersion, StringComparison.Ordinal))
                yield return Failure("Identity", "INVALID_VERSION_IDENTITY", manifest.BundleId,
                    "versionIdentity", GeneratedFailureBundleManifest.SchemaVersion, "INVALID", source);
            if (!GeneratedFailureBundleVersionIdentity.IsToken(manifest.ValidationPhaseId))
                yield return Failure("Bundle", "MISSING_PHASE", manifest.BundleId, "validationPhaseId",
                    "STABLE_TOKEN", "MISSING", source);
            if (!GeneratedFailureBundleVersionIdentity.IsToken(manifest.FailingTaskId))
                yield return Failure("Bundle", "MISSING_FAILING_TASK", manifest.BundleId, "failingTaskId",
                    "STABLE_TOKEN", "MISSING", source);
            if (manifest.PassSnapshots.Count == 0)
                yield return Failure("Bundle", "MISSING_PASS_SNAPSHOTS", manifest.BundleId, "passes",
                    "AT_LEAST_ONE", "0", source);
            foreach (var value in manifest.PassSnapshots.Where(value => !value.IsValid))
                yield return Failure("Bundle", "INVALID_PASS_SNAPSHOT", manifest.BundleId, "pass",
                    "VALID_PASS_SNAPSHOT", value.StableToken, source);
            foreach (var value in manifest.FailureRecords.Where(value => !value.IsComplete))
                yield return Failure("Bundle", "INVALID_FAILURE_OWNER_OR_REASON", manifest.BundleId,
                    value.OffendingKey, "COMPLETE_FAILURE", value.StableToken, source);
            foreach (var value in manifest.CoordinateRecords.Where(value => !value.IsValid))
                yield return Failure("Bundle", "INVALID_COORDINATE", manifest.BundleId, "coordinate",
                    "VALID_COORDINATE", value.StableToken, source);
            foreach (var value in manifest.ProvenanceRecords.Where(value => !value.IsValid))
                yield return Failure("Bundle", "INVALID_PROVENANCE", manifest.BundleId, "provenance",
                    "VALID_PROVENANCE", value.StableToken, source);
            foreach (var value in manifest.AttachmentReferences.Where(value => !value.IsValid))
                yield return Failure("Bundle", "INVALID_ATTACHMENT", manifest.BundleId, "attachment",
                    "VALID_REFERENCE", value.StableToken, source);
            if (manifest.AttachmentReferences.Count == 0)
                yield return Failure("Bundle", "MISSING_SCREENSHOT_REFERENCE_SLOT", manifest.BundleId,
                    "attachments", "AT_LEAST_ONE", "0", source);
            var expected = GeneratedValidationChainSnapshot.RequiredPhaseIds;
            if (manifest.UpstreamDigests.Count != expected.Count || expected.Any(phase =>
                    !manifest.UpstreamDigests.Any(value => string.Equals(value.PhaseId, phase,
                        StringComparison.Ordinal) && value.IsValid)))
                yield return Failure("Bundle", "INVALID_UPSTREAM_CHAIN", manifest.BundleId, "upstream",
                    string.Join(",", expected), string.Join(",", manifest.UpstreamDigests.Select(v => v.PhaseId)), source);
        }

        private static GeneratedFailureBundlePayload Serialize(GeneratedFailureBundleManifest manifest)
        {
            var json = new StringBuilder();
            json.Append('{').Append("\"bundleId\":\"").Append(Json(manifest.BundleId))
                .Append("\",\"schemaVersion\":\"").Append(Json(manifest.VersionIdentity.BundleSchemaVersion))
                .Append("\",\"generatorVersion\":\"").Append(Json(manifest.VersionIdentity.GeneratorVersion))
                .Append("\",\"dataVersion\":\"").Append(Json(manifest.VersionIdentity.DataVersion))
                .Append("\",\"worldSeed\":\"").Append(manifest.SeedIdentity.WorldSeed.ToString(CultureInfo.InvariantCulture))
                .Append("\",\"sourceKind\":\"").Append(manifest.SeedIdentity.SourceKind)
                .Append("\",\"contentHash\":\"").Append(manifest.SeedIdentity.ContentHash)
                .Append("\",\"validationPhaseId\":\"").Append(Json(manifest.ValidationPhaseId))
                .Append("\",\"failingTaskId\":\"").Append(Json(manifest.FailingTaskId))
                .Append("\",\"passDecision\":\"").Append(manifest.PassDecision)
                .Append("\",\"createdAt\":\"").Append(Json(manifest.CreatedAt))
                .Append("\",\"canonicalBundleDigest\":\"").Append(manifest.ManifestDigest).Append("\"}");
            return new GeneratedFailureBundlePayload(manifest, json.ToString(),
                Csv("pass_id,decision,detail,source_digest", manifest.PassSnapshots.Select(value =>
                    Row(value.PassId, value.Decision.ToString(), value.Detail, value.SourceDigest))),
                Csv("owner,reason,failed_rule_id,bundle_or_request_id,offending_key,expected,actual,source_digest,output_path",
                    manifest.FailureRecords.Select(value => Row(value.Owner, value.Reason,
                        value.FailedRuleId, value.BundleOrRequestId, value.OffendingKey, value.Expected, value.Actual,
                        value.SourceDigest, value.OutputPath))),
                Csv("sector_x,sector_y,slice_x,slice_y,cell_x,cell_y,node_id,edge_id,marker_id,scenario_id",
                    manifest.CoordinateRecords.Select(value => Row(Num(value.SectorX), Num(value.SectorY),
                        Num(value.SliceX), Num(value.SliceY), Num(value.CellX), Num(value.CellY),
                        value.NodeId, value.EdgeId, value.MarkerId, value.ScenarioId))),
                Csv("owner,key,value,source_digest", manifest.ProvenanceRecords.Select(value =>
                    Row(value.Owner, value.Key, value.Value, value.SourceDigest))),
                Csv("role,path,coordinate_focus,expected_later_producer", manifest.AttachmentReferences.Select(value =>
                    Row(value.Role, value.Path, value.CoordinateFocus, value.ExpectedLaterProducer))));
        }

        private static GeneratedFailureBundleFailureRecord Failure(string owner, string reason, string id,
            string key, string expected, string actual, string digest) =>
            new GeneratedFailureBundleFailureRecord(owner, reason, string.IsNullOrEmpty(id) ? "MISSING" : id,
                key, expected, actual, BakingCanonicalDigest.IsLowerHexSha256(digest) ? digest :
                    BakingCanonicalDigest.HashCanonicalLines(new[] { digest ?? "MISSING" }));
        private static string Csv(string header, IEnumerable<string> rows) =>
            string.Join("\n", new[] { header }.Concat(rows)) + "\n";
        private static string Row(params string[] values) => string.Join(",", values.Select(CsvValue));
        private static string CsvValue(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        private static string Json(string value) => (value ?? string.Empty).Replace("\\", "\\\\")
            .Replace("\"", "\\\"").Replace("\n", "\\n");
        private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
