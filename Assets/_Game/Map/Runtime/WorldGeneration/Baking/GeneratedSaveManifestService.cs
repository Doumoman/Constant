using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedSaveManifestService
    {
        public const string SchemaVersion = "MAP17_06_SAVE_MANIFEST_V1";
        public const string DownstreamOwner =
            "MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS";
        public const bool OpensDownstreamTask = false;

        public static GeneratedSaveManifestResult Build(
            GeneratedSectorModificationStorage storage,
            string seedIdentity,
            string generatorVersion,
            string dataVersion,
            string placementDigest,
            string windowHandleDigest)
        {
            if (storage == null || storage.ModifiedSectorCount == 0)
                return ManifestFailure(null, Code.MissingManifest, "service", "storage",
                    "at least one modified sector", "missing",
                    "Modification storage is required.");
            var baseDigests = storage.ModifiedSectors[0].BaseDigests;
            var version = new GeneratedSaveManifestVersion(SchemaVersion,
                generatorVersion, dataVersion);
            var header = new GeneratedSaveManifestHeader(version, seedIdentity,
                baseDigests.GeometryDigest, placementDigest, baseDigests.BakeDigest,
                baseDigests.CacheDigest, windowHandleDigest, storage.Digest,
                storage.ModifiedSectorCount);
            var entries = storage.ModifiedSectors.Select(snapshot =>
                new GeneratedModifiedSectorManifestEntry(snapshot.Sector,
                    snapshot.DirtyRevision, snapshot.BaseDigests,
                    snapshot.ModificationSet.Digest,
                    snapshot.Records.Select(record =>
                        new GeneratedSaveManifestRecordPayload(record)))).ToArray();
            return GeneratedSaveManifestSerializer.Serialize(
                new GeneratedWorldSaveManifest(header, entries));
        }

        public static string ComputeWindowHandleDigest(
            GeneratedSectorModificationBaseDigests baseDigests) =>
            baseDigests == null ? string.Empty : BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "WINDOW_HANDLE_BASE|" + baseDigests.WindowDigest + "|" +
                    baseDigests.WindowDiffDigest + "|" + baseDigests.TransitionPlanDigest,
            });

        public static string ComputeModificationSetDigest(
            GeneratedSaveManifestHeader header,
            GeneratedSectorCoordinate sector,
            int dirtyRevision,
            GeneratedSectorModificationBaseDigests baseDigests,
            IEnumerable<GeneratedSaveManifestRecordPayload> records)
        {
            var reconstructed = (records ?? Array.Empty<GeneratedSaveManifestRecordPayload>())
                .Where(value => value != null)
                .Select(value => value.ToModificationRecord(header)).ToArray();
            return new GeneratedSectorModificationSet(sector, dirtyRevision,
                baseDigests, reconstructed).Digest;
        }

        public static IReadOnlyList<GeneratedSaveManifestValidationFailure> ValidateManifest(
            GeneratedWorldSaveManifest manifest)
        {
            var failures = new List<GeneratedSaveManifestValidationFailure>();
            if (manifest == null)
            {
                Add(failures, Code.MissingManifest, "manifest", "manifest",
                    "present", "missing", "Manifest is required.");
                return failures;
            }
            var header = manifest.Header;
            if (header == null)
            {
                Add(failures, Code.InvalidHeader, "manifest", "header",
                    "present", "missing", "Manifest header is required.");
                return failures;
            }
            if (header.Version == null || !header.Version.IsSupported)
                Add(failures, Code.UnsupportedVersion, "manifest", "schemaVersion",
                    SchemaVersion, header.Version == null ? "missing" :
                        header.Version.SchemaVersion, "Manifest schema version is unsupported.");
            if (!header.IsValid)
                Add(failures, Code.InvalidHeader, "manifest", "header",
                    "valid seed/version/digests/count", header.StableToken,
                    "Manifest header is invalid.");
            if (header.ModifiedSectorCount != manifest.ModifiedSectorCount)
                Add(failures, Code.ModifiedSectorCountMismatch, "manifest",
                    "modifiedSectorCount", Number(header.ModifiedSectorCount),
                    Number(manifest.ModifiedSectorCount),
                    "Header modified sector count does not match entries.");
            if (manifest.DuplicateSectorEntryCount != 0)
                Add(failures, Code.DuplicateSectorEntry, "manifest", "sector",
                    "unique", Number(manifest.DuplicateSectorEntryCount),
                    "Modified sector entries must be unique.");

            foreach (var entry in manifest.ModifiedSectorEntries)
            {
                var key = entry.Sector == null ? "missing" : entry.Sector.ToString();
                if (entry.Sector == null || !entry.Sector.IsInWorld)
                    Add(failures, Code.InvalidSector, "manifest", key,
                        "sector inside 13x13 world", key,
                        "Modified sector coordinate is invalid.");
                if (entry.RecordCount == 0)
                    Add(failures, Code.UnmodifiedSectorEntry, "manifest", key,
                        "one or more records", "0",
                        "Unmodified sectors must not have manifest entries.");
                if (entry.DuplicateRecordIdCount != 0)
                    Add(failures, Code.DuplicateRecordId, "manifest", key,
                        "unique record ids", Number(entry.DuplicateRecordIdCount),
                        "Modification stable ids must be unique in a sector entry.");
                if (entry.BaseDigests == null || !entry.BaseDigests.IsValid)
                    Add(failures, Code.InvalidHeader, "manifest", key + ".baseDigests",
                        "valid lower-hex digests", "invalid",
                        "Modified sector base digests are invalid.");

                var records = entry.Records.Select(value =>
                    value.ToModificationRecord(header)).ToArray();
                for (var index = 0; index < entry.Records.Count; index++)
                {
                    var payload = entry.Records[index];
                    var record = records[index];
                    if (payload.Sector == null || entry.Sector == null ||
                        !payload.Sector.Equals(entry.Sector) ||
                        record.Target.LocalIndex == null || !record.Target.LocalIndex.IsValid ||
                        !record.Target.IsLayerValid || !record.Payload.IsValidFor(record.Kind))
                        Add(failures, Code.InvalidPayload, "manifest", payload.StableId,
                            "valid target and kind payload", payload.StableToken,
                            "Record payload is invalid.");
                    if (!string.Equals(payload.StableId, record.Id.Value,
                            StringComparison.Ordinal) ||
                        !string.Equals(payload.SourceDigest, record.SourceDigest,
                            StringComparison.Ordinal))
                        Add(failures, Code.RecordHashMismatch, "manifest", payload.StableId,
                            record.Id.Value, payload.StableId,
                            "Record stable id or source digest is stale.");
                }
                if (entry.BaseDigests != null)
                {
                    var set = new GeneratedSectorModificationSet(entry.Sector,
                        entry.DirtyRevision, entry.BaseDigests, records);
                    if (!string.Equals(set.Digest, entry.ModificationSetDigest,
                        StringComparison.Ordinal))
                        Add(failures, Code.ModificationSetHashMismatch, "manifest", key,
                            entry.ModificationSetDigest, set.Digest,
                            "Modification set digest does not match records.");
                }
            }
            return failures.Distinct().OrderBy(value => value).ToArray();
        }

        public static GeneratedSaveManifestResult ValidateUnmodifiedSectorRegeneration(
            GeneratedSectorRegenerationRequest request)
        {
            var failures = ValidateRequestHeader(request);
            if (request != null && request.Sector != null && request.Manifest != null &&
                request.Manifest.Find(request.Sector) != null)
                Add(failures, Code.UnmodifiedSectorEntry, "regeneration",
                    request.Sector.ToString(), "no manifest entry", "entry exists",
                    "Unmodified regeneration requires an omitted sector entry.");
            return new GeneratedSaveManifestResult(failures.Count == 0
                ? request.Manifest : null, null, failures);
        }

        public static GeneratedSectorRegenerationApplyResult PlanRegenerationApply(
            GeneratedSectorRegenerationRequest request)
        {
            var failures = ValidateRequestHeader(request);
            var entry = request == null ? null : request.Entry;
            if (entry == null)
                Add(failures, Code.MissingEntry, "regeneration", "sector",
                    "modified sector entry", "missing",
                    "Regeneration apply requires a modified sector entry.");
            var authority = request == null ? null : request.RegeneratedAuthority;
            if (authority == null || !authority.IsValid)
                Add(failures, Code.MissingTarget, "regeneration", "authority",
                    "valid regenerated base authority", "missing or invalid",
                    "Regenerated base authority is required.");

            GeneratedSectorModificationRecord[] records = null;
            if (entry != null && request != null && request.Manifest != null)
            {
                records = entry.Records.Select(value =>
                    value.ToModificationRecord(request.Manifest.Header)).ToArray();
                if (authority != null)
                {
                    foreach (var record in records.Where(value =>
                        !authority.ContainsTarget(value.Target)))
                        Add(failures, Code.MissingTarget, "regeneration",
                            record.Id.Value, "target in regenerated logical base", "missing",
                            "Manifest record target is absent from regenerated base sector.");
                }
                if (records.Any(value => value.BaseDigests == null || authority == null ||
                    !value.BaseDigests.Equals(authority.BaseDigests)))
                    Add(failures, Code.BakeDigestMismatch, "regeneration",
                        "record.baseDigests", authority == null ? "valid authority" :
                            GeneratedSectorModificationDigest.ComputeBase(authority.BaseDigests),
                        "stale", "Manifest record base digests are stale.");
            }

            if (failures.Count != 0)
                return new GeneratedSectorRegenerationApplyResult(null, failures);

            var outputSet = new GeneratedSectorModificationSet(entry.Sector,
                entry.DirtyRevision, entry.BaseDigests, records);
            if (!string.Equals(outputSet.Digest, entry.ModificationSetDigest,
                StringComparison.Ordinal))
            {
                Add(failures, Code.ModificationSetHashMismatch, "regeneration",
                    entry.Sector.ToString(), entry.ModificationSetDigest,
                    outputSet.Digest, "Regenerated modification set digest mismatches manifest.");
                return new GeneratedSectorRegenerationApplyResult(null, failures);
            }
            var commands = records.OrderBy(value => value)
                .Select((record, index) =>
                    new GeneratedSectorModificationApplyCommand(index, record)).ToArray();
            var plan = new GeneratedSectorRegenerationApplyPlan(request.Manifest,
                entry, commands, outputSet, authority.SourceRecordDigest);
            return new GeneratedSectorRegenerationApplyResult(plan,
                Array.Empty<GeneratedSaveManifestValidationFailure>());
        }

        private static List<GeneratedSaveManifestValidationFailure> ValidateRequestHeader(
            GeneratedSectorRegenerationRequest request)
        {
            var failures = new List<GeneratedSaveManifestValidationFailure>();
            if (request == null || request.Manifest == null)
            {
                Add(failures, Code.MissingManifest, "regeneration", "manifest",
                    "present", "missing", "Regeneration request needs a manifest.");
                return failures;
            }
            failures.AddRange(ValidateManifest(request.Manifest));
            if (request.Sector == null || !request.Sector.IsInWorld)
                Add(failures, Code.InvalidSector, "regeneration", "sector",
                    "inside 13x13 world", request.Sector == null ? "missing" :
                        request.Sector.ToString(), "Regeneration sector is invalid.");
            var header = request.Manifest.Header;
            Compare(failures, Code.SeedMismatch, "seed", header.SeedIdentity,
                request.SeedIdentity);
            Compare(failures, Code.GeneratorVersionMismatch, "generatorVersion",
                header.Version.GeneratorVersion, request.GeneratorVersion);
            Compare(failures, Code.DataVersionMismatch, "dataVersion",
                header.Version.DataVersion, request.DataVersion);
            Compare(failures, Code.GeometryDigestMismatch, "geometryDigest",
                header.GeometryDigest, request.GeometryDigest);
            Compare(failures, Code.PlacementDigestMismatch, "placementDigest",
                header.PlacementDigest, request.PlacementDigest);
            Compare(failures, Code.BakeDigestMismatch, "bakeDigest",
                header.BakeDigest, request.BakeDigest);
            Compare(failures, Code.CacheDigestMismatch, "cacheDigest",
                header.CacheDigest, request.CacheDigest);
            Compare(failures, Code.WindowHandleDigestMismatch, "windowHandleDigest",
                header.WindowHandleDigest, request.WindowHandleDigest);
            Compare(failures, Code.StorageDigestMismatch, "storageDigest",
                header.StorageDigest, request.StorageDigest);
            return failures;
        }

        private static void Compare(
            ICollection<GeneratedSaveManifestValidationFailure> failures,
            GeneratedSaveManifestValidationFailureCode code,
            string key,
            string expected,
            string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                Add(failures, code, "regeneration", key, expected, actual,
                    "Regenerated base identity or digest does not match manifest.");
        }

        private static GeneratedSaveManifestResult ManifestFailure(
            GeneratedWorldSaveManifest manifest,
            GeneratedSaveManifestValidationFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => new GeneratedSaveManifestResult(manifest, null, new[]
            {
                new GeneratedSaveManifestValidationFailure(code, owner, key,
                    expected, actual, reason),
            });

        private static void Add(
            ICollection<GeneratedSaveManifestValidationFailure> failures,
            GeneratedSaveManifestValidationFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => failures.Add(new GeneratedSaveManifestValidationFailure(
                code, owner, key, expected, actual, reason));

        private static string Number(int value) =>
            value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static class Code
        {
            public const GeneratedSaveManifestValidationFailureCode MissingManifest =
                GeneratedSaveManifestValidationFailureCode.MissingManifest;
            public const GeneratedSaveManifestValidationFailureCode InvalidHeader =
                GeneratedSaveManifestValidationFailureCode.InvalidHeader;
            public const GeneratedSaveManifestValidationFailureCode UnsupportedVersion =
                GeneratedSaveManifestValidationFailureCode.UnsupportedVersion;
            public const GeneratedSaveManifestValidationFailureCode ModifiedSectorCountMismatch =
                GeneratedSaveManifestValidationFailureCode.ModifiedSectorCountMismatch;
            public const GeneratedSaveManifestValidationFailureCode DuplicateSectorEntry =
                GeneratedSaveManifestValidationFailureCode.DuplicateSectorEntry;
            public const GeneratedSaveManifestValidationFailureCode DuplicateRecordId =
                GeneratedSaveManifestValidationFailureCode.DuplicateRecordId;
            public const GeneratedSaveManifestValidationFailureCode InvalidSector =
                GeneratedSaveManifestValidationFailureCode.InvalidSector;
            public const GeneratedSaveManifestValidationFailureCode UnmodifiedSectorEntry =
                GeneratedSaveManifestValidationFailureCode.UnmodifiedSectorEntry;
            public const GeneratedSaveManifestValidationFailureCode InvalidPayload =
                GeneratedSaveManifestValidationFailureCode.InvalidPayload;
            public const GeneratedSaveManifestValidationFailureCode RecordHashMismatch =
                GeneratedSaveManifestValidationFailureCode.RecordHashMismatch;
            public const GeneratedSaveManifestValidationFailureCode ModificationSetHashMismatch =
                GeneratedSaveManifestValidationFailureCode.ModificationSetHashMismatch;
            public const GeneratedSaveManifestValidationFailureCode SeedMismatch =
                GeneratedSaveManifestValidationFailureCode.SeedMismatch;
            public const GeneratedSaveManifestValidationFailureCode GeneratorVersionMismatch =
                GeneratedSaveManifestValidationFailureCode.GeneratorVersionMismatch;
            public const GeneratedSaveManifestValidationFailureCode DataVersionMismatch =
                GeneratedSaveManifestValidationFailureCode.DataVersionMismatch;
            public const GeneratedSaveManifestValidationFailureCode GeometryDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.GeometryDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode PlacementDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.PlacementDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode BakeDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.BakeDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode CacheDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.CacheDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode WindowHandleDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.WindowHandleDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode StorageDigestMismatch =
                GeneratedSaveManifestValidationFailureCode.StorageDigestMismatch;
            public const GeneratedSaveManifestValidationFailureCode MissingTarget =
                GeneratedSaveManifestValidationFailureCode.MissingTarget;
            public const GeneratedSaveManifestValidationFailureCode MissingEntry =
                GeneratedSaveManifestValidationFailureCode.MissingEntry;
        }
    }
}
