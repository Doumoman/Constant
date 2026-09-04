using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedSaveManifestSerializer
    {
        public static GeneratedSaveManifestResult Serialize(GeneratedWorldSaveManifest manifest)
        {
            var failures = GeneratedSaveManifestService.ValidateManifest(manifest).ToArray();
            if (failures.Length != 0)
                return new GeneratedSaveManifestResult(manifest, null, failures);

            var header = manifest.Header;
            var lines = new List<string>
            {
                "FORMAT|GENERATED_SAVE_MANIFEST|1",
                string.Join("|", new[]
                {
                    "HEADER", Encode(header.Version.SchemaVersion),
                    Encode(header.SeedIdentity), Encode(header.Version.GeneratorVersion),
                    Encode(header.Version.DataVersion), header.GeometryDigest,
                    header.PlacementDigest, header.BakeDigest, header.CacheDigest,
                    header.WindowHandleDigest, header.StorageDigest,
                    Number(header.ModifiedSectorCount),
                }),
            };

            foreach (var entry in manifest.ModifiedSectorEntries.OrderBy(value => value))
            {
                lines.Add(string.Join("|", new[]
                {
                    "ENTRY", Number(entry.Sector.X), Number(entry.Sector.Y),
                    Number(entry.DirtyRevision), entry.BaseDigests.GeometryDigest,
                    entry.BaseDigests.BakeDigest, entry.BaseDigests.CacheDigest,
                    entry.BaseDigests.WindowDigest, entry.BaseDigests.WindowDiffDigest,
                    entry.BaseDigests.TransitionPlanDigest,
                    entry.ModificationSetDigest, Number(entry.RecordCount),
                }));
                foreach (var record in entry.Records.OrderBy(value => value))
                    lines.Add(SerializeRecord(record));
            }
            lines.Add("MANIFEST_DIGEST|" + manifest.Digest);
            var payload = new GeneratedSaveManifestPayload(string.Join("\n", lines));
            return new GeneratedSaveManifestResult(manifest, payload,
                Array.Empty<GeneratedSaveManifestValidationFailure>());
        }

        public static GeneratedSaveManifestResult Parse(GeneratedSaveManifestPayload payload)
        {
            if (payload == null)
                return Failed(null, null, Code.InvalidPayload, "serializer", "payload",
                    "present", "missing", "A canonical payload is required.");
            if (!payload.DigestMatches)
                return Failed(null, payload, Code.PayloadHashMismatch, "serializer",
                    "payloadDigest", payload.ComputedDigest, payload.Digest,
                    "Declared payload digest does not match canonical text.");

            try
            {
                var lines = BakingCanonicalDigest.NormalizeLineEndingsToLf(payload.CanonicalText)
                    .Split(new[] { '\n' }, StringSplitOptions.None);
                if (lines.Length < 3 || !string.Equals(lines[0],
                    "FORMAT|GENERATED_SAVE_MANIFEST|1", StringComparison.Ordinal))
                    return Failed(null, payload, Code.InvalidPayload, "serializer", "format",
                        "FORMAT|GENERATED_SAVE_MANIFEST|1",
                        lines.Length == 0 ? "missing" : lines[0],
                        "Manifest format line is missing or unsupported.");

                GeneratedSaveManifestHeader header = null;
                var entryRows = new List<EntryRow>();
                var recordRows = new List<GeneratedSaveManifestRecordPayload>();
                string declaredManifestDigest = null;
                for (var index = 1; index < lines.Length; index++)
                {
                    var parts = lines[index].Split('|');
                    var kind = parts.Length == 0 ? string.Empty : parts[0];
                    switch (kind)
                    {
                        case "HEADER":
                            if (header != null || parts.Length != 12)
                                return Failed(null, payload, Code.InvalidPayload,
                                    "serializer", "HEADER", "one 12-field line",
                                    lines[index], "Header is duplicate or malformed.");
                            header = ParseHeader(parts);
                            break;
                        case "ENTRY":
                            if (parts.Length != 12)
                                return Failed(null, payload, Code.InvalidPayload,
                                    "serializer", "ENTRY", "12 fields", lines[index],
                                    "Modified sector entry is malformed.");
                            var entry = ParseEntry(parts);
                            if (entryRows.Any(value => value.Sector.Equals(entry.Sector)))
                                return Failed(null, payload, Code.DuplicateSectorEntry,
                                    "serializer", entry.Sector.ToString(), "unique", "duplicate",
                                    "Modified sector coordinate is duplicated.");
                            entryRows.Add(entry);
                            break;
                        case "RECORD":
                            if (parts.Length != 26)
                                return Failed(null, payload, Code.InvalidPayload,
                                    "serializer", "RECORD", "26 fields", lines[index],
                                    "Modification record payload is malformed.");
                            recordRows.Add(ParseRecord(parts));
                            break;
                        case "MANIFEST_DIGEST":
                            if (parts.Length != 2 || declaredManifestDigest != null)
                                return Failed(null, payload, Code.InvalidPayload,
                                    "serializer", "MANIFEST_DIGEST", "one digest",
                                    lines[index], "Manifest digest line is malformed.");
                            declaredManifestDigest = parts[1];
                            break;
                        default:
                            return Failed(null, payload, Code.UnknownField,
                                "serializer", kind, "known canonical field", kind,
                                "Unknown canonical field is rejected.");
                    }
                }

                if (header == null || declaredManifestDigest == null)
                    return Failed(null, payload, Code.InvalidPayload, "serializer",
                        "requiredFields", "HEADER and MANIFEST_DIGEST", "missing",
                        "Canonical payload is incomplete.");

                var entries = new List<GeneratedModifiedSectorManifestEntry>();
                foreach (var row in entryRows.OrderBy(value => value.Sector))
                {
                    var records = recordRows.Where(value => value.Sector != null &&
                            value.Sector.Equals(row.Sector)).OrderBy(value => value).ToArray();
                    if (records.Length != row.RecordCount)
                        return Failed(null, payload, Code.ModifiedSectorCountMismatch,
                            "serializer", row.Sector.ToString(), Number(row.RecordCount),
                            Number(records.Length), "Entry record count does not match payload.");
                    if (records.Select(value => value.StableId).Distinct(
                        StringComparer.Ordinal).Count() != records.Length)
                        return Failed(null, payload, Code.DuplicateRecordId,
                            "serializer", row.Sector.ToString(), "unique record ids",
                            "duplicate", "Modification stable id is duplicated.");

                    var reconstructed = records.Select(value =>
                        value.ToModificationRecord(header)).ToArray();
                    for (var recordIndex = 0; recordIndex < records.Length; recordIndex++)
                    {
                        if (!string.Equals(records[recordIndex].StableId,
                            reconstructed[recordIndex].Id.Value, StringComparison.Ordinal) ||
                            !string.Equals(records[recordIndex].SourceDigest,
                                reconstructed[recordIndex].SourceDigest, StringComparison.Ordinal))
                            return Failed(null, payload, Code.RecordHashMismatch,
                                "serializer", records[recordIndex].StableId,
                                reconstructed[recordIndex].Id.Value,
                                records[recordIndex].StableId,
                                "Record identity or source digest does not reconstruct.");
                    }
                    var set = new GeneratedSectorModificationSet(row.Sector,
                        row.DirtyRevision, row.BaseDigests, reconstructed);
                    if (!string.Equals(set.Digest, row.ModificationSetDigest,
                        StringComparison.Ordinal))
                        return Failed(null, payload, Code.ModificationSetHashMismatch,
                            "serializer", row.Sector.ToString(), row.ModificationSetDigest,
                            set.Digest, "Modification set digest does not reconstruct.");
                    entries.Add(new GeneratedModifiedSectorManifestEntry(row.Sector,
                        row.DirtyRevision, row.BaseDigests, row.ModificationSetDigest, records));
                }

                if (recordRows.Any(record => record.Sector == null ||
                    entryRows.All(row => !row.Sector.Equals(record.Sector))))
                    return Failed(null, payload, Code.MissingEntry, "serializer", "record.sector",
                        "matching ENTRY", "missing", "Record has no modified sector entry.");

                var manifest = new GeneratedWorldSaveManifest(header, entries);
                if (!string.Equals(manifest.Digest, declaredManifestDigest,
                    StringComparison.Ordinal))
                    return Failed(null, payload, Code.ManifestHashMismatch,
                        "serializer", "manifestDigest", manifest.Digest,
                        declaredManifestDigest, "Manifest digest does not match parsed content.");
                var validation = GeneratedSaveManifestService.ValidateManifest(manifest).ToArray();
                return new GeneratedSaveManifestResult(validation.Length == 0 ? manifest : null,
                    payload, validation);
            }
            catch (Exception exception)
            {
                return Failed(null, payload, Code.InvalidPayload, "serializer", "parse",
                    "valid canonical values", exception.GetType().Name,
                    "Canonical payload parsing failed atomically.");
            }
        }

        private static string SerializeRecord(GeneratedSaveManifestRecordPayload value) =>
            string.Join("|", new[]
            {
                "RECORD", Number(value.Sector.X), Number(value.Sector.Y),
                Encode(value.StableId), Number(value.LocalIndex), Number(value.LayerId),
                Encode(value.SourceProvenanceToken), Encode(value.SlotReference),
                Number((int)value.Kind), Number(value.Revision), Encode(value.OldTileCode),
                Encode(value.OldSourceToken), Encode(value.NewTileCode),
                Encode(value.NewSourceToken), Encode(value.StateKey), Encode(value.StateValue),
                Bool(value.LogicalRemoved), Bool(value.Collected), Bool(value.Consumed),
                value.BaseDigests.GeometryDigest, value.BaseDigests.BakeDigest,
                value.BaseDigests.CacheDigest, value.BaseDigests.WindowDigest,
                value.BaseDigests.WindowDiffDigest, value.BaseDigests.TransitionPlanDigest,
                value.SourceDigest,
            });

        private static GeneratedSaveManifestHeader ParseHeader(string[] parts) =>
            new GeneratedSaveManifestHeader(
                new GeneratedSaveManifestVersion(Decode(parts[1]), Decode(parts[3]),
                    Decode(parts[4])), Decode(parts[2]), parts[5], parts[6], parts[7],
                parts[8], parts[9], parts[10], Integer(parts[11]));

        private static EntryRow ParseEntry(string[] parts) => new EntryRow(
            new GeneratedSectorCoordinate(Integer(parts[1]), Integer(parts[2])),
            Integer(parts[3]), new GeneratedSectorModificationBaseDigests(
                parts[4], parts[5], parts[6], parts[7], parts[8], parts[9]),
            parts[10], Integer(parts[11]));

        private static GeneratedSaveManifestRecordPayload ParseRecord(string[] parts) =>
            new GeneratedSaveManifestRecordPayload(Decode(parts[3]),
                new GeneratedSectorCoordinate(Integer(parts[1]), Integer(parts[2])),
                Integer(parts[4]), Integer(parts[5]), Decode(parts[6]), Decode(parts[7]),
                (GeneratedSectorModificationKind)Integer(parts[8]), Integer(parts[9]),
                Decode(parts[10]), Decode(parts[11]), Decode(parts[12]), Decode(parts[13]),
                Decode(parts[14]), Decode(parts[15]), Boolean(parts[16]),
                Boolean(parts[17]), Boolean(parts[18]),
                new GeneratedSectorModificationBaseDigests(parts[19], parts[20], parts[21],
                    parts[22], parts[23], parts[24]), parts[25]);

        private static string Encode(string value) => Convert.ToBase64String(
            BakingCanonicalDigest.Utf8NoBomEncoding.GetBytes(value ?? string.Empty));
        private static string Decode(string value) =>
            BakingCanonicalDigest.Utf8NoBomEncoding.GetString(Convert.FromBase64String(value));
        private static string Bool(bool value) => value ? "1" : "0";
        private static bool Boolean(string value)
        {
            if (value == "1") return true;
            if (value == "0") return false;
            throw new FormatException("Boolean canonical field must be 0 or 1.");
        }
        private static int Integer(string value) =>
            int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static GeneratedSaveManifestResult Failed(
            GeneratedWorldSaveManifest manifest,
            GeneratedSaveManifestPayload payload,
            GeneratedSaveManifestValidationFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => new GeneratedSaveManifestResult(manifest, payload, new[]
            {
                new GeneratedSaveManifestValidationFailure(code, owner, key,
                    expected, actual, reason),
            });

        private sealed class EntryRow
        {
            public EntryRow(
                GeneratedSectorCoordinate sector,
                int dirtyRevision,
                GeneratedSectorModificationBaseDigests baseDigests,
                string modificationSetDigest,
                int recordCount)
            {
                Sector = sector;
                DirtyRevision = dirtyRevision;
                BaseDigests = baseDigests;
                ModificationSetDigest = modificationSetDigest;
                RecordCount = recordCount;
            }
            public GeneratedSectorCoordinate Sector { get; }
            public int DirtyRevision { get; }
            public GeneratedSectorModificationBaseDigests BaseDigests { get; }
            public string ModificationSetDigest { get; }
            public int RecordCount { get; }
        }

        private static class Code
        {
            public const GeneratedSaveManifestValidationFailureCode InvalidPayload =
                GeneratedSaveManifestValidationFailureCode.InvalidPayload;
            public const GeneratedSaveManifestValidationFailureCode PayloadHashMismatch =
                GeneratedSaveManifestValidationFailureCode.PayloadHashMismatch;
            public const GeneratedSaveManifestValidationFailureCode DuplicateSectorEntry =
                GeneratedSaveManifestValidationFailureCode.DuplicateSectorEntry;
            public const GeneratedSaveManifestValidationFailureCode DuplicateRecordId =
                GeneratedSaveManifestValidationFailureCode.DuplicateRecordId;
            public const GeneratedSaveManifestValidationFailureCode ModifiedSectorCountMismatch =
                GeneratedSaveManifestValidationFailureCode.ModifiedSectorCountMismatch;
            public const GeneratedSaveManifestValidationFailureCode UnknownField =
                GeneratedSaveManifestValidationFailureCode.UnknownField;
            public const GeneratedSaveManifestValidationFailureCode RecordHashMismatch =
                GeneratedSaveManifestValidationFailureCode.RecordHashMismatch;
            public const GeneratedSaveManifestValidationFailureCode ModificationSetHashMismatch =
                GeneratedSaveManifestValidationFailureCode.ModificationSetHashMismatch;
            public const GeneratedSaveManifestValidationFailureCode MissingEntry =
                GeneratedSaveManifestValidationFailureCode.MissingEntry;
            public const GeneratedSaveManifestValidationFailureCode ManifestHashMismatch =
                GeneratedSaveManifestValidationFailureCode.ManifestHashMismatch;
        }
    }
}
