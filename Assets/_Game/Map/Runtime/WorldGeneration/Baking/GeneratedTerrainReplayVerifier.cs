using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedTerrainReplayResult
    {
        private readonly ReadOnlyCollection<GeneratedTerrainExportFailure> failures;
        private readonly ReadOnlyCollection<GeneratedTerrainExportFile> files;

        internal GeneratedTerrainReplayResult(
            IEnumerable<GeneratedTerrainExportFailure> sourceFailures,
            IEnumerable<GeneratedTerrainExportFile> sourceFiles,
            string manifestDigest,
            string replayDigest)
        {
            failures = new ReadOnlyCollection<GeneratedTerrainExportFailure>((sourceFailures ??
                Array.Empty<GeneratedTerrainExportFailure>()).OrderBy(value => value).ToArray());
            files = new ReadOnlyCollection<GeneratedTerrainExportFile>((sourceFiles ??
                Array.Empty<GeneratedTerrainExportFile>()).OrderBy(value =>
                    GeneratedTerrainCsvExporter.FileOrder(value.FileName)).ToArray());
            ManifestDigest = manifestDigest ?? string.Empty;
            ReplayDigest = replayDigest ?? string.Empty;
        }

        public bool Success => failures.Count == 0 && files.Count ==
            GeneratedTerrainCsvExporter.RequiredFileNames.Count;
        public IReadOnlyList<GeneratedTerrainExportFailure> Failures => failures;
        public IReadOnlyList<GeneratedTerrainExportFile> Files => files;
        public string ManifestDigest { get; }
        public string ReplayDigest { get; }
    }

    public static class GeneratedTerrainReplayVerifier
    {
        public static GeneratedTerrainReplayResult Verify(string directory)
        {
            var failures = new List<GeneratedTerrainExportFailure>();
            string fullPath;
            try
            {
                fullPath = string.IsNullOrWhiteSpace(directory)
                    ? string.Empty : Path.GetFullPath(directory);
            }
            catch (Exception exception)
            {
                failures.Add(Failure(GeneratedTerrainExportFailureCode.InvalidOutputDirectory,
                    directory, exception.GetType().Name));
                return Result(failures);
            }
            if (string.IsNullOrEmpty(fullPath) || !Directory.Exists(fullPath))
            {
                failures.Add(Failure(GeneratedTerrainExportFailureCode.InvalidOutputDirectory,
                    fullPath, "The replay directory does not exist."));
                return Result(failures);
            }

            string[] paths;
            try { paths = Directory.GetFiles(fullPath, "*", SearchOption.TopDirectoryOnly); }
            catch (Exception exception)
            {
                failures.Add(Failure(GeneratedTerrainExportFailureCode.InvalidOutputDirectory,
                    fullPath, exception.GetType().Name));
                return Result(failures);
            }
            var groups = paths.GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var group in groups.Where(value => value.Count() > 1))
                failures.Add(Failure(GeneratedTerrainExportFailureCode.DuplicateFile,
                    group.Key, "File names must be unique ignoring case."));
            foreach (var required in GeneratedTerrainCsvExporter.RequiredFileNames)
                if (!groups.Any(value => string.Equals(value.Key, required,
                    StringComparison.OrdinalIgnoreCase)))
                    failures.Add(Failure(GeneratedTerrainExportFailureCode.MissingFile,
                        required, "A required CSV file is missing."));
            foreach (var group in groups.Where(value => !GeneratedTerrainCsvExporter.RequiredFileNames
                         .Any(required => string.Equals(required, value.Key,
                             StringComparison.OrdinalIgnoreCase))))
                failures.Add(Failure(GeneratedTerrainExportFailureCode.ExtraFile,
                    group.Key, "The replay directory contains an unexpected file."));
            if (failures.Count > 0) return Result(failures);

            var files = new Dictionary<string, ParsedFile>(StringComparer.Ordinal);
            foreach (var required in GeneratedTerrainCsvExporter.RequiredFileNames)
            {
                var path = groups.Single(value => string.Equals(value.Key, required,
                    StringComparison.OrdinalIgnoreCase)).Single();
                try
                {
                    var payload = File.ReadAllText(path, new UTF8Encoding(false, true));
                    var expectedHeader = Header(required);
                    List<string[]> records;
                    var reason = string.Empty;
                    if (payload.Length == 0 || payload[0] == '\ufeff' ||
                        payload.IndexOf('\r') >= 0 || !payload.EndsWith("\n", StringComparison.Ordinal) ||
                        !TryParse(payload, out records, out reason))
                    {
                        failures.Add(Failure(GeneratedTerrainExportFailureCode.MalformedCsv,
                            required, string.IsNullOrEmpty(reason)
                                ? "CSV must be UTF-8 without BOM and use LF with a final newline."
                                : reason));
                        continue;
                    }
                    if (records.Count == 0 || !records[0].SequenceEqual(expectedHeader.Split(',')))
                    {
                        failures.Add(Failure(GeneratedTerrainExportFailureCode.HeaderMismatch,
                            required, "The fixed CSV header does not match."));
                        continue;
                    }
                    var fieldCount = records[0].Length;
                    if (records.Skip(1).Any(value => value.Length != fieldCount))
                    {
                        failures.Add(Failure(GeneratedTerrainExportFailureCode.MalformedCsv,
                            required, "A CSV row has the wrong field count."));
                        continue;
                    }
                    files.Add(required, new ParsedFile(required, expectedHeader, payload,
                        records.Skip(1).ToArray()));
                }
                catch (Exception exception)
                {
                    failures.Add(Failure(GeneratedTerrainExportFailureCode.MalformedCsv,
                        required, exception.GetType().Name + ": " + exception.Message));
                }
            }
            if (failures.Count > 0) return Result(failures, files.Values);

            var manifest = files[GeneratedTerrainCsvExporter.ManifestFileName];
            if (manifest.Rows.Count != 1)
            {
                failures.Add(Failure(GeneratedTerrainExportFailureCode.RowCountMismatch,
                    manifest.FileName, "The manifest must contain exactly one data row."));
                return Result(failures, files.Values);
            }
            var row = manifest.Rows[0];
            if (!ManifestConstantsAreValid(row))
                failures.Add(Failure(GeneratedTerrainExportFailureCode.ManifestDigestMismatch,
                    manifest.FileName, "Manifest identity or geometry is invalid."));

            var sourceSliceDigest = row[2];
            var sourceSlotDigest = row[3];
            var expected = new[]
            {
                new ManifestEntry(GeneratedTerrainCsvExporter.PlanFileName, row[14], row[15]),
                new ManifestEntry(GeneratedTerrainCsvExporter.SlicesFileName, row[16], row[17]),
                new ManifestEntry(GeneratedTerrainCsvExporter.CellsFileName, row[18], row[19]),
                new ManifestEntry(GeneratedTerrainCsvExporter.SocketsFileName, row[20], row[21]),
                new ManifestEntry(GeneratedTerrainCsvExporter.SlotsFileName, row[22], row[23]),
            };
            foreach (var entry in expected)
            {
                int count;
                if (!int.TryParse(entry.RowCount, NumberStyles.None,
                        CultureInfo.InvariantCulture, out count) || count != files[entry.FileName].Rows.Count)
                    failures.Add(Failure(GeneratedTerrainExportFailureCode.RowCountMismatch,
                        entry.FileName, "The manifest row count does not match the CSV file."));
                if (!GeneratedTerrainExportDigest.IsLowerHexSha256(entry.Digest) ||
                    !string.Equals(entry.Digest, files[entry.FileName].ExportFile.PayloadDigest,
                        StringComparison.Ordinal))
                    failures.Add(Failure(GeneratedTerrainExportFailureCode.PayloadDigestMismatch,
                        entry.FileName, "The manifest payload digest does not match the CSV file."));
            }

            var planRows = files[GeneratedTerrainCsvExporter.PlanFileName].Rows;
            if (planRows.Count != 1 || planRows[0][1] != sourceSliceDigest ||
                planRows[0][2] != sourceSlotDigest)
                failures.Add(Failure(GeneratedTerrainExportFailureCode.ManifestDigestMismatch,
                    GeneratedTerrainCsvExporter.PlanFileName,
                    "The plan source digests do not match the manifest."));
            if (files[GeneratedTerrainCsvExporter.SlicesFileName].Rows.Count !=
                    GeneratedMicroChunkSliceSet.ChunkCount ||
                files[GeneratedTerrainCsvExporter.CellsFileName].Rows.Count !=
                    GeneratedMicroChunkSliceSet.SectorCellCount ||
                files[GeneratedTerrainCsvExporter.SocketsFileName].Rows.Count !=
                    GeneratedMicroChunkSliceSet.ChunkCount * 4)
                failures.Add(Failure(GeneratedTerrainExportFailureCode.RowCountMismatch,
                    "terrain", "The terrain CSV cardinalities are incomplete."));

            var dataFiles = expected.Select(value => files[value.FileName].ExportFile).ToArray();
            var replayDigest = GeneratedTerrainCsvExporter.ComputePacketDigest(
                sourceSliceDigest, sourceSlotDigest, dataFiles);
            if (!GeneratedTerrainExportDigest.IsLowerHexSha256(row[24]) ||
                !string.Equals(row[24], replayDigest, StringComparison.Ordinal))
                failures.Add(Failure(GeneratedTerrainExportFailureCode.PacketDigestMismatch,
                    manifest.FileName, "The recomputed packet digest does not match the manifest."));
            var manifestDigest = manifest.ExportFile.PayloadDigest;
            if (!GeneratedTerrainExportDigest.IsLowerHexSha256(manifestDigest))
                failures.Add(Failure(GeneratedTerrainExportFailureCode.ManifestDigestMismatch,
                    manifest.FileName, "The manifest digest is invalid."));
            return new GeneratedTerrainReplayResult(failures,
                GeneratedTerrainCsvExporter.RequiredFileNames.Select(value => files[value].ExportFile),
                manifestDigest, failures.Count == 0 ? replayDigest : string.Empty);
        }

        private static bool ManifestConstantsAreValid(string[] row)
        {
            if (row == null || row.Length != 25) return false;
            var expected = new[] { "48", "32", "1536", "4", "4", "16", "12", "8", "96", "4" };
            return row[0] == GeneratedTerrainExportPacket.FormatVersion &&
                row[1] == GeneratedTerrainExportPacket.TaskId &&
                GeneratedTerrainExportDigest.IsLowerHexSha256(row[2]) &&
                GeneratedTerrainExportDigest.IsLowerHexSha256(row[3]) &&
                row.Skip(4).Take(10).SequenceEqual(expected);
        }

        private static bool TryParse(
            string payload, out List<string[]> records, out string reason)
        {
            records = new List<string[]>();
            reason = string.Empty;
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            var afterQuote = false;
            for (var index = 0; index < payload.Length; index++)
            {
                var character = payload[index];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (index + 1 < payload.Length && payload[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else { quoted = false; afterQuote = true; }
                    }
                    else field.Append(character);
                    continue;
                }
                if (afterQuote && character != ',' && character != '\n')
                {
                    reason = "Unexpected character after a quoted field.";
                    return false;
                }
                if (character == '"')
                {
                    if (field.Length != 0 || afterQuote)
                    {
                        reason = "A quote appeared inside an unquoted field.";
                        return false;
                    }
                    quoted = true;
                }
                else if (character == ',')
                {
                    row.Add(field.ToString()); field.Length = 0; afterQuote = false;
                }
                else if (character == '\n')
                {
                    row.Add(field.ToString()); field.Length = 0; afterQuote = false;
                    records.Add(row.ToArray()); row.Clear();
                }
                else
                {
                    if (afterQuote)
                    {
                        reason = "Unexpected text after a quoted field.";
                        return false;
                    }
                    field.Append(character);
                }
            }
            if (quoted || row.Count != 0 || field.Length != 0)
            {
                reason = quoted ? "An escaped field was not closed." : "CSV did not end at a row boundary.";
                return false;
            }
            return true;
        }

        private static string Header(string fileName)
        {
            if (fileName == GeneratedTerrainCsvExporter.ManifestFileName)
                return GeneratedTerrainCsvExporter.ManifestHeader;
            if (fileName == GeneratedTerrainCsvExporter.PlanFileName)
                return GeneratedTerrainCsvExporter.PlanHeader;
            if (fileName == GeneratedTerrainCsvExporter.SlicesFileName)
                return GeneratedTerrainCsvExporter.SlicesHeader;
            if (fileName == GeneratedTerrainCsvExporter.CellsFileName)
                return GeneratedTerrainCsvExporter.CellsHeader;
            if (fileName == GeneratedTerrainCsvExporter.SocketsFileName)
                return GeneratedTerrainCsvExporter.SocketsHeader;
            return GeneratedTerrainCsvExporter.SlotsHeader;
        }

        private static GeneratedTerrainReplayResult Result(
            IEnumerable<GeneratedTerrainExportFailure> failures,
            IEnumerable<ParsedFile> files = null) => new GeneratedTerrainReplayResult(
                failures, (files ?? Array.Empty<ParsedFile>()).Select(value => value.ExportFile),
                string.Empty, string.Empty);

        private static GeneratedTerrainExportFailure Failure(
            GeneratedTerrainExportFailureCode code, string subject, string reason) =>
            new GeneratedTerrainExportFailure(code, subject ?? string.Empty, reason);

        private sealed class ParsedFile
        {
            public ParsedFile(string fileName, string header, string payload,
                IReadOnlyList<string[]> rows)
            {
                FileName = fileName;
                Rows = rows;
                ExportFile = new GeneratedTerrainExportFile(fileName, header, rows.Count, payload);
            }
            public string FileName { get; }
            public IReadOnlyList<string[]> Rows { get; }
            public GeneratedTerrainExportFile ExportFile { get; }
        }

        private sealed class ManifestEntry
        {
            public ManifestEntry(string fileName, string rowCount, string digest)
            {
                FileName = fileName; RowCount = rowCount; Digest = digest;
            }
            public string FileName { get; }
            public string RowCount { get; }
            public string Digest { get; }
        }
    }
}
