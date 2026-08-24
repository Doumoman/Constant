using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Data;
using UnityEngine;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvExporter
    {
        private static readonly FileContract CatalogContract = new FileContract(
            MicrochunkCsvImportSource.CatalogFileName,
            "MicroChunk",
            new[] { "microchunk_id" },
            Array.Empty<string>(),
            new[] { "microchunk_id", "tile_data_complete" });

        private static readonly FileContract TileContract = new FileContract(
            MicrochunkCsvImportSource.TileCellsFileName,
            "MicroChunk",
            new[] { "microchunk_id", "local_x", "local_y" },
            new[] { "local_x", "local_y" },
            new[]
            {
                "microchunk_id", "local_x", "local_y", "ground_code", "one_way_code",
                "breakable_code", "hazard_code", "liquid_code", "decor_back_code",
                "decor_front_code", "marker_code"
            });

        private static readonly FileContract SocketContract = new FileContract(
            MicrochunkCsvImportSource.SocketsFileName,
            "MicroChunk",
            new[] { "microchunk_id", "socket_id" },
            Array.Empty<string>(),
            new[]
            {
                "microchunk_id", "socket_id", "side", "band_id", "traversal_kind",
                "mandatory_allowed", "tool_requirement", "edge_signature_id"
            });

        private static readonly FileContract BandContract = new FileContract(
            MicrochunkCsvImportSource.SocketBandsFileName,
            "Route",
            new[] { "band_id" },
            Array.Empty<string>(),
            new[]
            {
                "band_id", "axis", "min_local_coord", "max_local_coord",
                "minimum_clearance_tiles"
            });

        private static readonly FileContract SlotContract = new FileContract(
            MicrochunkCsvImportSource.ObjectSlotsFileName,
            "MicroChunk",
            new[] { "microchunk_id", "slot_id" },
            Array.Empty<string>(),
            new[]
            {
                "microchunk_id", "slot_id", "local_x", "local_y", "slot_category",
                "allowed_pool_id", "required", "orientation", "visible_from_route",
                "forbidden_radius_tiles", "required_marker_code"
            });

        private static readonly FileContract VariantContract = new FileContract(
            MicrochunkCsvImportSource.VariantsFileName,
            "MicroChunk",
            new[] { "microchunk_id", "variant_id" },
            Array.Empty<string>(),
            new[] { "microchunk_id", "variant_id" });

        public MicrochunkCsvExportPlan BuildPlan(
            MicrochunkCsvImportSource source,
            MicrochunkCsvExportRequest request)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var issues = new List<MicrochunkCsvExportIssue>();
            var catalog = ReadTable(source.CatalogBytes, CatalogContract, request.SelectedMicrochunkId, issues);
            var tiles = ReadTable(source.TileCellBytes, TileContract, request.SelectedMicrochunkId, issues);
            var sockets = ReadTable(source.SocketBytes, SocketContract, request.SelectedMicrochunkId, issues);
            var bands = ReadTable(source.SocketBandBytes, BandContract, request.SelectedMicrochunkId, issues);
            var slots = ReadTable(source.ObjectSlotBytes, SlotContract, request.SelectedMicrochunkId, issues);
            var variants = ReadTable(source.VariantBytes, VariantContract, request.SelectedMicrochunkId, issues);

            var selectedCatalog = catalog.Rows.Where(row => IsSelected(row, request)).ToList();
            if (selectedCatalog.Count == 0 && !request.AllowNewCatalogRow)
            {
                AddIssue(
                    issues,
                    CatalogContract.FileName,
                    request.SelectedMicrochunkId,
                    "microchunk_id",
                    "CATALOG_ROW_MISSING",
                    "The selected catalog row is missing and creation was not explicitly allowed.");
            }
            if (selectedCatalog.Count > 1)
            {
                AddIssue(
                    issues,
                    CatalogContract.FileName,
                    request.SelectedMicrochunkId,
                    "microchunk_id",
                    "CATALOG_ROW_DUPLICATE",
                    "The selected catalog row is duplicated.");
            }

            MicrochunkCsvImportValidationFeedback feedback = null;
            try
            {
                feedback = new MicrochunkCsvImportValidationFeedback(
                    request.EditorState.Grid.ValidateTileLayers(),
                    request.EditorState.Grid.ValidateCoverage(),
                    request.EditorState.ValidateSocketEdges(
                        request.EditorState.CreateAuthoringSignatureLookup()),
                    request.EditorState.ValidateObjectSlots(
                        request.EditorState.CreateAuthoringSlotPolicy()));
            }
            catch (Exception exception)
            {
                AddIssue(
                    issues,
                    CatalogContract.FileName,
                    request.SelectedMicrochunkId,
                    string.Empty,
                    "VALIDATION_PREFLIGHT_UNAVAILABLE",
                    exception.Message);
            }

            if (issues.Any(value => value.IsError))
            {
                return new MicrochunkCsvExportPlan(
                    request,
                    Array.Empty<MicrochunkCsvExportFilePlan>(),
                    issues,
                    feedback);
            }

            var plans = new List<MicrochunkCsvExportFilePlan>
            {
                BuildCatalogPlan(catalog, request),
                BuildTilePlan(tiles, request),
                BuildSocketPlan(sockets, request),
                BuildBandPlan(bands, request, issues),
                BuildSlotPlan(slots, request),
                BuildVariantPlan(variants, request)
            };
            return new MicrochunkCsvExportPlan(request, plans, issues, feedback);
        }

        public MicrochunkCsvExportResult ApplyPlan(
            MicrochunkCsvExportPlan plan,
            string authoringRoot,
            string simulateFailureBeforeFileName = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (string.IsNullOrWhiteSpace(authoringRoot))
            {
                throw new ArgumentException("Authoring root is required.", nameof(authoringRoot));
            }

            var issues = new List<MicrochunkCsvExportIssue>(plan.Issues);
            if (!plan.Success)
            {
                AddIssue(
                    issues,
                    CatalogContract.FileName,
                    plan.Request.SelectedMicrochunkId,
                    string.Empty,
                    "EXPORT_PLAN_INVALID",
                    "A plan containing errors cannot be applied.");
                return new MicrochunkCsvExportResult(plan, false, 0, issues);
            }

            var root = Path.GetFullPath(authoringRoot).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            var rootPrefix = root + Path.DirectorySeparatorChar;
            var operations = new List<ApplyOperation>();
            try
            {
                foreach (var file in plan.Files.Where(value => value.HasChanges))
                {
                    var target = Path.GetFullPath(Path.Combine(root, file.RelativeDirectory, file.FileName));
                    if (!target.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Export target escaped the Authoring root.");
                    }
                    if (!File.Exists(target))
                    {
                        throw new FileNotFoundException("Export target does not exist.", target);
                    }

                    var original = File.ReadAllBytes(target);
                    if (!string.Equals(Sha256(original), file.BeforeSha256, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Export target changed after plan generation: " + file.FileName);
                    }

                    var token = Guid.NewGuid().ToString("N");
                    var temporary = target + ".map07_11.tmp." + token;
                    var backup = target + ".map07_11.bak." + token;
                    File.WriteAllBytes(temporary, file.AfterBytes);
                    operations.Add(new ApplyOperation(file, target, temporary, backup, original));
                }

                foreach (var operation in operations)
                {
                    if (string.Equals(
                            operation.File.FileName,
                            simulateFailureBeforeFileName,
                            StringComparison.Ordinal))
                    {
                        throw new IOException(
                            "Simulated atomic export failure before " + operation.File.FileName + ".");
                    }

                    File.Replace(operation.TemporaryPath, operation.TargetPath, operation.BackupPath, true);
                    operation.Replaced = true;
                }

                foreach (var operation in operations)
                {
                    var actual = Sha256(File.ReadAllBytes(operation.TargetPath));
                    if (!string.Equals(actual, operation.File.AfterSha256, StringComparison.Ordinal))
                    {
                        throw new IOException("Export verification failed for " + operation.File.FileName + ".");
                    }
                }

                Cleanup(operations);
                return new MicrochunkCsvExportResult(plan, true, operations.Count, issues);
            }
            catch (Exception exception)
            {
                for (var index = operations.Count - 1; index >= 0; index--)
                {
                    var operation = operations[index];
                    if (!operation.Replaced) continue;
                    try
                    {
                        File.WriteAllBytes(operation.TargetPath, operation.OriginalBytes);
                    }
                    catch (Exception rollbackException)
                    {
                        AddIssue(
                            issues,
                            operation.File.FileName,
                            plan.Request.SelectedMicrochunkId,
                            string.Empty,
                            "ATOMIC_ROLLBACK_FAILED",
                            rollbackException.Message);
                    }
                }

                Cleanup(operations);
                AddIssue(
                    issues,
                    CatalogContract.FileName,
                    plan.Request.SelectedMicrochunkId,
                    string.Empty,
                    "ATOMIC_APPLY_FAILED",
                    exception.Message);
                return new MicrochunkCsvExportResult(plan, false, 0, issues);
            }
        }

        public MicrochunkCsvExportResult ApplyProjectAuthoringPlan(MicrochunkCsvExportPlan plan)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var authoringRoot = Path.Combine(
                projectRoot,
                MicrochunkCsvImportSource.AuthoringRoot.Replace('/', Path.DirectorySeparatorChar));
            return ApplyPlan(plan, authoringRoot);
        }

        private static MicrochunkCsvExportFilePlan BuildCatalogPlan(
            CsvTable table,
            MicrochunkCsvExportRequest request)
        {
            var selected = table.Rows.Where(row => IsSelected(row, request)).ToList();
            var row = NewRow(table, selected.FirstOrDefault(), table.Rows.Count);
            if (request.Catalog != null)
            {
                OverlayKnownFields(row, request.Catalog.Fields);
            }
            row["microchunk_id"] = request.SelectedMicrochunkId;
            row["tile_data_complete"] = "1";
            return ReplaceSelected(table, request, new[] { row }, CatalogContract);
        }

        private static MicrochunkCsvExportFilePlan BuildTilePlan(
            CsvTable table,
            MicrochunkCsvExportRequest request)
        {
            var selectedByCoordinate = table.Rows
                .Where(row => IsSelected(row, request))
                .GroupBy(row => row["local_x"] + "\n" + row["local_y"], StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var rows = new List<CsvRow>();
            foreach (var cell in request.EditorState.Grid.State.Cells)
            {
                var x = cell.Coordinate.X.ToString(CultureInfo.InvariantCulture);
                var y = cell.Coordinate.Y.ToString(CultureInfo.InvariantCulture);
                selectedByCoordinate.TryGetValue(x + "\n" + y, out var original);
                var row = NewRow(table, original, table.Rows.Count + rows.Count);
                row["microchunk_id"] = request.SelectedMicrochunkId;
                row["local_x"] = x;
                row["local_y"] = y;
                row["ground_code"] = cell.GetTileCode(StarNight.Map.WorldGeneration.Microchunks.MicrochunkTileLayer.GroundSolid);
                row["one_way_code"] = cell.GetTileCode(StarNight.Map.WorldGeneration.Microchunks.MicrochunkTileLayer.OneWay);
                row["breakable_code"] = cell.GetTileCode(StarNight.Map.WorldGeneration.Microchunks.MicrochunkTileLayer.Breakable);
                row["hazard_code"] = cell.GetTileCode(StarNight.Map.WorldGeneration.Microchunks.MicrochunkTileLayer.Hazard);
                row["liquid_code"] = cell.GetTileCode(StarNight.Map.WorldGeneration.Microchunks.MicrochunkTileLayer.Liquid);
                row["decor_back_code"] = cell.GetTileCode(StarNight.Map.WorldGeneration.Microchunks.MicrochunkTileLayer.DecorationBack);
                row["decor_front_code"] = cell.GetTileCode(StarNight.Map.WorldGeneration.Microchunks.MicrochunkTileLayer.DecorationFront);
                row["marker_code"] = cell.GetTileCode(StarNight.Map.WorldGeneration.Microchunks.MicrochunkTileLayer.Marker);
                rows.Add(row);
            }

            return ReplaceSelected(table, request, rows, TileContract);
        }

        private static MicrochunkCsvExportFilePlan BuildSocketPlan(
            CsvTable table,
            MicrochunkCsvExportRequest request)
        {
            var selectedById = SelectedByKey(table, request, "socket_id");
            var bands = request.EditorState.SocketAuthoring.Bands.ToDictionary(
                value => value.BandId,
                StringComparer.Ordinal);
            var rows = new List<CsvRow>();
            foreach (var socket in request.EditorState.SocketAuthoring.Sockets
                         .OrderBy(value => value.SocketId, StringComparer.Ordinal))
            {
                selectedById.TryGetValue(socket.SocketId, out var original);
                var row = NewRow(table, original, table.Rows.Count + rows.Count);
                row["microchunk_id"] = request.SelectedMicrochunkId;
                row["socket_id"] = socket.SocketId;
                row["side"] = socket.SideToken;
                row["band_id"] = socket.BandId;
                row["traversal_kind"] = socket.TraversalKindToken;
                SetIfPresent(row, "direction", "BIDIRECTIONAL");
                row["mandatory_allowed"] = BooleanToken(socket.MandatoryAllowed);
                row["tool_requirement"] = socket.ToolRequirementToken;
                row["edge_signature_id"] = socket.EdgeSignatureId;
                SetIfPresent(row, "route_layer", socket.MandatoryAllowed ? "BOTH" : "OPTIONAL");
                SetIfPresent(
                    row,
                    "minimum_safe_tiles",
                    bands.TryGetValue(socket.BandId, out var band)
                        ? band.MinimumClearanceTiles.ToString(CultureInfo.InvariantCulture)
                        : "0");
                rows.Add(row);
            }

            return ReplaceSelected(table, request, rows, SocketContract);
        }

        private static MicrochunkCsvExportFilePlan BuildBandPlan(
            CsvTable table,
            MicrochunkCsvExportRequest request,
            ICollection<MicrochunkCsvExportIssue> issues)
        {
            if (!table.Headers.Contains("microchunk_id", StringComparer.Ordinal))
            {
                if (request.EditorState.SocketAuthoring.Bands.Count > 0)
                {
                    AddIssue(
                        issues,
                        BandContract.FileName,
                        request.SelectedMicrochunkId,
                        "band_id",
                        "SOCKET_BAND_NON_OWNED_GLOBAL_SCHEMA",
                        "The schema has global-only socket bands; shared rows were left byte-identical.",
                        MicrochunkCsvExportIssueSeverity.Warning);
                }

                return Unchanged(table, BandContract);
            }

            var selectedById = SelectedByKey(table, request, "band_id");
            var rows = new List<CsvRow>();
            foreach (var band in request.EditorState.SocketAuthoring.Bands
                         .OrderBy(value => value.BandId, StringComparer.Ordinal))
            {
                selectedById.TryGetValue(band.BandId, out var original);
                var row = NewRow(table, original, table.Rows.Count + rows.Count);
                row["microchunk_id"] = request.SelectedMicrochunkId;
                row["band_id"] = band.BandId;
                row["axis"] = band.SideToken == "L" || band.SideToken == "R"
                    ? "HORIZONTAL_EDGE"
                    : "VERTICAL_EDGE";
                row["min_local_coord"] = band.InclusiveStart.ToString(CultureInfo.InvariantCulture);
                row["max_local_coord"] = band.InclusiveEnd.ToString(CultureInfo.InvariantCulture);
                SetIfPresent(
                    row,
                    "recommended_center",
                    ((band.InclusiveStart + band.InclusiveEnd) / 2.0)
                    .ToString("0.0############", CultureInfo.InvariantCulture));
                row["minimum_clearance_tiles"] =
                    band.MinimumClearanceTiles.ToString(CultureInfo.InvariantCulture);
                rows.Add(row);
            }

            var ownedContract = new FileContract(
                BandContract.FileName,
                BandContract.RelativeDirectory,
                new[] { "microchunk_id", "band_id" },
                Array.Empty<string>(),
                BandContract.RequiredHeaders.Concat(new[] { "microchunk_id" }));
            return ReplaceSelected(table, request, rows, ownedContract);
        }

        private static MicrochunkCsvExportFilePlan BuildSlotPlan(
            CsvTable table,
            MicrochunkCsvExportRequest request)
        {
            var selectedById = SelectedByKey(table, request, "slot_id");
            var rows = new List<CsvRow>();
            foreach (var slot in request.EditorState.ObjectSlotAuthoring.Rows
                         .OrderBy(value => value.SlotId, StringComparer.Ordinal))
            {
                selectedById.TryGetValue(slot.SlotId, out var original);
                var row = NewRow(table, original, table.Rows.Count + rows.Count);
                row["microchunk_id"] = request.SelectedMicrochunkId;
                row["slot_id"] = slot.SlotId;
                row["local_x"] = slot.Anchor.X.ToString(CultureInfo.InvariantCulture);
                row["local_y"] = slot.Anchor.Y.ToString(CultureInfo.InvariantCulture);
                row["slot_category"] = slot.CategoryToken;
                row["allowed_pool_id"] = slot.PoolId;
                row["required"] = BooleanToken(slot.Required);
                row["orientation"] = slot.OrientationToken;
                row["visible_from_route"] = BooleanToken(slot.VisibleFromRoute);
                row["forbidden_radius_tiles"] =
                    slot.SafetyRadiusTiles.ToString(CultureInfo.InvariantCulture);
                row["required_marker_code"] = slot.RequiredMarkerCode;
                rows.Add(row);
            }

            return ReplaceSelected(table, request, rows, SlotContract);
        }

        private static MicrochunkCsvExportFilePlan BuildVariantPlan(
            CsvTable table,
            MicrochunkCsvExportRequest request)
        {
            var rows = new List<CsvRow>();
            foreach (var variant in request.Variants)
            {
                var row = NewRow(table, null, table.Rows.Count + rows.Count);
                OverlayKnownFields(row, variant.Fields);
                row["microchunk_id"] = request.SelectedMicrochunkId;
                rows.Add(row);
            }

            return ReplaceSelected(table, request, rows, VariantContract);
        }

        private static MicrochunkCsvExportFilePlan ReplaceSelected(
            CsvTable table,
            MicrochunkCsvExportRequest request,
            IEnumerable<CsvRow> inserted,
            FileContract contract)
        {
            var removed = table.Rows.Where(row => IsSelected(row, request)).ToList();
            var insertedRows = inserted.ToList();
            var finalRows = table.Rows.Where(row => !IsSelected(row, request))
                .Concat(insertedRows)
                .ToList();
            finalRows.Sort((left, right) => CompareRows(left, right, contract));
            var after = Serialize(table.Headers, finalRows);
            return new MicrochunkCsvExportFilePlan(
                contract.FileName,
                contract.RelativeDirectory,
                table.Headers,
                removed.Count,
                insertedRows.Count,
                finalRows.Select(row => PrimaryKeySignature(row, contract)),
                Sha256(table.SourceBytes),
                Sha256(after),
                after);
        }

        private static MicrochunkCsvExportFilePlan Unchanged(CsvTable table, FileContract contract)
        {
            return new MicrochunkCsvExportFilePlan(
                contract.FileName,
                contract.RelativeDirectory,
                table.Headers,
                0,
                0,
                table.Rows.Select(row => PrimaryKeySignature(row, contract)),
                Sha256(table.SourceBytes),
                Sha256(table.SourceBytes),
                table.SourceBytes);
        }

        private static CsvTable ReadTable(
            byte[] bytes,
            FileContract contract,
            string selectedId,
            ICollection<MicrochunkCsvExportIssue> issues)
        {
            if (bytes == null || bytes.Length == 0)
            {
                AddIssue(issues, contract.FileName, selectedId, string.Empty,
                    "CSV_SOURCE_EMPTY", "Export source CSV is empty.");
                return new CsvTable(contract.FileName, Array.Empty<string>(), Array.Empty<CsvRow>(),
                    bytes ?? Array.Empty<byte>());
            }

            var read = new Rfc4180CsvReader().Read(bytes, contract.FileName);
            if (!read.Success || read.Records.Count == 0)
            {
                var detail = read.Errors.Count == 0
                    ? "CSV header record is missing."
                    : string.Join(" | ", read.Errors.Select(value => value.ToString())
                        .OrderBy(value => value, StringComparer.Ordinal));
                AddIssue(issues, contract.FileName, selectedId, string.Empty,
                    "CSV_SYNTAX", detail);
                return new CsvTable(contract.FileName, Array.Empty<string>(), Array.Empty<CsvRow>(), bytes);
            }

            var headers = read.Records[0].Fields.Select(value => value.Value).ToList();
            if (headers.Count != headers.Distinct(StringComparer.Ordinal).Count())
            {
                AddIssue(issues, contract.FileName, selectedId, string.Empty,
                    "CSV_HEADER_DUPLICATE", "CSV header names must be unique.");
            }
            foreach (var required in contract.RequiredHeaders)
            {
                if (headers.Contains(required, StringComparer.Ordinal)) continue;
                AddIssue(issues, contract.FileName, selectedId, required,
                    "CSV_HEADER_REQUIRED_COLUMN_MISSING", "Required CSV column is missing.");
            }

            var rows = new List<CsvRow>();
            for (var index = 1; index < read.Records.Count; index++)
            {
                var record = read.Records[index];
                if (record.Fields.Count != headers.Count)
                {
                    AddIssue(issues, contract.FileName, selectedId, string.Empty,
                        "CSV_COLUMN_COUNT_MISMATCH", "CSV row field count does not match its header.");
                    continue;
                }

                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var fieldIndex = 0; fieldIndex < headers.Count; fieldIndex++)
                {
                    values.Add(headers[fieldIndex], record.Fields[fieldIndex].Value);
                }
                rows.Add(new CsvRow(values, index - 1));
            }

            return new CsvTable(contract.FileName, headers, rows, bytes);
        }

        private static CsvRow NewRow(CsvTable table, CsvRow source, int ordinal)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var header in table.Headers)
            {
                values.Add(header, source == null ? string.Empty : source[header]);
            }
            return new CsvRow(values, ordinal);
        }

        private static void OverlayKnownFields(CsvRow row, IReadOnlyDictionary<string, string> fields)
        {
            foreach (var field in fields)
            {
                if (row.Contains(field.Key)) row[field.Key] = field.Value ?? string.Empty;
            }
        }

        private static Dictionary<string, CsvRow> SelectedByKey(
            CsvTable table,
            MicrochunkCsvExportRequest request,
            string key)
        {
            return table.Rows.Where(row => IsSelected(row, request))
                .GroupBy(row => row[key], StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private static bool IsSelected(CsvRow row, MicrochunkCsvExportRequest request)
        {
            return row.Contains("microchunk_id") && string.Equals(
                row["microchunk_id"],
                request.SelectedMicrochunkId,
                StringComparison.Ordinal);
        }

        private static int CompareRows(CsvRow left, CsvRow right, FileContract contract)
        {
            foreach (var key in contract.PrimaryKeys)
            {
                int comparison;
                if (contract.NumericKeys.Contains(key, StringComparer.Ordinal) &&
                    int.TryParse(left[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var leftNumber) &&
                    int.TryParse(right[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rightNumber))
                {
                    comparison = leftNumber.CompareTo(rightNumber);
                }
                else
                {
                    comparison = string.Compare(left[key], right[key], StringComparison.Ordinal);
                }

                if (comparison != 0) return comparison;
            }

            return left.OriginalOrdinal.CompareTo(right.OriginalOrdinal);
        }

        private static string PrimaryKeySignature(CsvRow row, FileContract contract)
        {
            return string.Join("|", contract.PrimaryKeys.Select(key => key + "=" + row[key]));
        }

        private static byte[] Serialize(IReadOnlyList<string> headers, IEnumerable<CsvRow> rows)
        {
            var builder = new StringBuilder();
            AppendRecord(builder, headers);
            foreach (var row in rows)
            {
                AppendRecord(builder, headers.Select(header => row[header]));
            }

            var content = new UTF8Encoding(false, true).GetBytes(builder.ToString());
            var result = new byte[content.Length + 3];
            result[0] = 0xEF;
            result[1] = 0xBB;
            result[2] = 0xBF;
            Buffer.BlockCopy(content, 0, result, 3, content.Length);
            return result;
        }

        private static void AppendRecord(StringBuilder builder, IEnumerable<string> values)
        {
            var first = true;
            foreach (var value in values)
            {
                if (!first) builder.Append(',');
                first = false;
                builder.Append(Escape(value ?? string.Empty));
            }
            builder.Append("\r\n");
        }

        private static string Escape(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static string BooleanToken(bool value)
        {
            return value ? "1" : "0";
        }

        private static void SetIfPresent(CsvRow row, string column, string value)
        {
            if (row.Contains(column)) row[column] = value;
        }

        private static void Cleanup(IEnumerable<ApplyOperation> operations)
        {
            foreach (var operation in operations)
            {
                TryDelete(operation.TemporaryPath);
                TryDelete(operation.BackupPath);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup cannot hide the already verified export result.
            }
        }

        private static void AddIssue(
            ICollection<MicrochunkCsvExportIssue> issues,
            string fileName,
            string selectedId,
            string column,
            string code,
            string message,
            MicrochunkCsvExportIssueSeverity severity = MicrochunkCsvExportIssueSeverity.Error)
        {
            issues.Add(new MicrochunkCsvExportIssue(
                fileName, selectedId, column, code, message, severity));
        }

        private sealed class FileContract
        {
            public string FileName { get; }
            public string RelativeDirectory { get; }
            public IReadOnlyList<string> PrimaryKeys { get; }
            public IReadOnlyList<string> NumericKeys { get; }
            public IReadOnlyList<string> RequiredHeaders { get; }

            public FileContract(
                string fileName,
                string relativeDirectory,
                IEnumerable<string> primaryKeys,
                IEnumerable<string> numericKeys,
                IEnumerable<string> requiredHeaders)
            {
                FileName = fileName;
                RelativeDirectory = relativeDirectory;
                PrimaryKeys = new ReadOnlyCollection<string>(primaryKeys.ToList());
                NumericKeys = new ReadOnlyCollection<string>(numericKeys.ToList());
                RequiredHeaders = new ReadOnlyCollection<string>(requiredHeaders
                    .Distinct(StringComparer.Ordinal)
                    .ToList());
            }
        }

        private sealed class CsvTable
        {
            public string FileName { get; }
            public IReadOnlyList<string> Headers { get; }
            public IReadOnlyList<CsvRow> Rows { get; }
            public byte[] SourceBytes { get; }

            public CsvTable(
                string fileName,
                IEnumerable<string> headers,
                IEnumerable<CsvRow> rows,
                byte[] sourceBytes)
            {
                FileName = fileName;
                Headers = new ReadOnlyCollection<string>(headers.ToList());
                Rows = new ReadOnlyCollection<CsvRow>(rows.ToList());
                SourceBytes = (byte[])sourceBytes.Clone();
            }
        }

        private sealed class CsvRow
        {
            private readonly Dictionary<string, string> values;

            public int OriginalOrdinal { get; }
            public string this[string key]
            {
                get => values.TryGetValue(key, out var value) ? value : string.Empty;
                set
                {
                    if (!values.ContainsKey(key))
                    {
                        throw new KeyNotFoundException("CSV column was not found: " + key);
                    }
                    values[key] = value ?? string.Empty;
                }
            }

            public CsvRow(IDictionary<string, string> values, int originalOrdinal)
            {
                this.values = new Dictionary<string, string>(values, StringComparer.Ordinal);
                OriginalOrdinal = originalOrdinal;
            }

            public bool Contains(string key)
            {
                return values.ContainsKey(key);
            }
        }

        private sealed class ApplyOperation
        {
            public MicrochunkCsvExportFilePlan File { get; }
            public string TargetPath { get; }
            public string TemporaryPath { get; }
            public string BackupPath { get; }
            public byte[] OriginalBytes { get; }
            public bool Replaced { get; set; }

            public ApplyOperation(
                MicrochunkCsvExportFilePlan file,
                string targetPath,
                string temporaryPath,
                string backupPath,
                byte[] originalBytes)
            {
                File = file;
                TargetPath = targetPath;
                TemporaryPath = temporaryPath;
                BackupPath = backupPath;
                OriginalBytes = (byte[])originalBytes.Clone();
            }
        }
    }
}
