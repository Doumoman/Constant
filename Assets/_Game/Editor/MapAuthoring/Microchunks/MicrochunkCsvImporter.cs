using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvImporter
    {
        private static readonly string[] CatalogHeaders = { "microchunk_id", "tile_data_complete" };
        private static readonly string[] TileHeaders =
        {
            "microchunk_id", "local_x", "local_y", "ground_code", "one_way_code",
            "breakable_code", "hazard_code", "liquid_code", "decor_back_code",
            "decor_front_code", "marker_code"
        };
        private static readonly string[] SocketHeaders =
        {
            "microchunk_id", "socket_id", "side", "band_id", "traversal_kind",
            "mandatory_allowed", "tool_requirement", "edge_signature_id"
        };
        private static readonly string[] BandHeaders =
        {
            "band_id", "axis", "min_local_coord", "max_local_coord", "minimum_clearance_tiles"
        };
        private static readonly string[] SlotHeaders =
        {
            "microchunk_id", "slot_id", "local_x", "local_y", "slot_category",
            "allowed_pool_id", "required", "orientation", "visible_from_route",
            "forbidden_radius_tiles", "required_marker_code"
        };
        private static readonly string[] EdgeSignatureHeaders =
        {
            "edge_signature_id", "axis", "band_id", "traversal_kind", "ground_entry_height",
            "clearance_width", "clearance_height", "tool_requirement", "mandatory_allowed",
            "tags", "notes"
        };

        public MicrochunkCsvImportResult Import(
            MicrochunkCsvImportSource source,
            MicrochunkCsvImportRequest request)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var issues = new List<MicrochunkCsvImportIssue>();
            var editorState = new MicrochunkSocketAndSlotEditorViewModel();
            var catalogTable = ReadTable(
                source.CatalogBytes,
                MicrochunkCsvImportSource.CatalogFileName,
                CatalogHeaders,
                true,
                request.SelectedMicrochunkId,
                issues);
            var tileTable = ReadTable(
                source.TileCellBytes,
                MicrochunkCsvImportSource.TileCellsFileName,
                TileHeaders,
                true,
                request.SelectedMicrochunkId,
                issues);
            var socketTable = ReadTable(
                source.SocketBytes,
                MicrochunkCsvImportSource.SocketsFileName,
                SocketHeaders,
                false,
                request.SelectedMicrochunkId,
                issues);
            var bandTable = ReadTable(
                source.SocketBandBytes,
                MicrochunkCsvImportSource.SocketBandsFileName,
                BandHeaders,
                false,
                request.SelectedMicrochunkId,
                issues);
            var slotTable = ReadTable(
                source.ObjectSlotBytes,
                MicrochunkCsvImportSource.ObjectSlotsFileName,
                SlotHeaders,
                false,
                request.SelectedMicrochunkId,
                issues);
            var variantTable = ReadTable(
                source.VariantBytes,
                MicrochunkCsvImportSource.VariantsFileName,
                new[] { "microchunk_id" },
                false,
                request.SelectedMicrochunkId,
                issues);
            var tileCodeTable = ReadTable(
                source.TileCodeBytes,
                MicrochunkCsvImportSource.TileCodesFileName,
                Array.Empty<string>(),
                false,
                request.SelectedMicrochunkId,
                issues);
            var poolTable = ReadTable(
                source.ObjectSlotPoolBytes,
                MicrochunkCsvImportSource.ObjectSlotPoolsFileName,
                Array.Empty<string>(),
                false,
                request.SelectedMicrochunkId,
                issues);
            var edgeTable = ReadTable(
                source.EdgeSignatureBytes,
                MicrochunkCsvImportSource.EdgeSignaturesFileName,
                EdgeSignatureHeaders,
                false,
                request.SelectedMicrochunkId,
                issues);

            var selectedCatalogRows = SelectRows(catalogTable, request.SelectedMicrochunkId);
            if (selectedCatalogRows.Count == 0)
            {
                AddIssue(
                    issues,
                    MicrochunkCsvImportSource.CatalogFileName,
                    request.SelectedMicrochunkId,
                    0,
                    "microchunk_id",
                    "CATALOG_ROW_MISSING",
                    "The selected microchunk catalog row was not found.");
                return Result(request, null, editorState, issues, Array.Empty<MicrochunkCsvVariantMetadata>(),
                    CollectReferences(tileCodeTable, poolTable, edgeTable), null);
            }
            if (selectedCatalogRows.Count > 1)
            {
                foreach (var duplicate in selectedCatalogRows.Skip(1))
                {
                    AddIssue(
                        issues,
                        MicrochunkCsvImportSource.CatalogFileName,
                        request.SelectedMicrochunkId,
                        duplicate.RowNumber,
                        "microchunk_id",
                        "CATALOG_ROW_DUPLICATE",
                        "The selected microchunk catalog row is duplicated.");
                }
                return Result(request, null, editorState, issues, Array.Empty<MicrochunkCsvVariantMetadata>(),
                    CollectReferences(tileCodeTable, poolTable, edgeTable), null);
            }

            var catalogRow = selectedCatalogRows[0];
            if (!TryBoolean(
                    catalogRow,
                    "tile_data_complete",
                    MicrochunkCsvImportSource.CatalogFileName,
                    request.SelectedMicrochunkId,
                    issues,
                    out var tileDataComplete))
            {
                return Result(request, null, editorState, issues, Array.Empty<MicrochunkCsvVariantMetadata>(),
                    CollectReferences(tileCodeTable, poolTable, edgeTable), null);
            }

            var catalog = new MicrochunkCsvCatalogMetadata(
                request.SelectedMicrochunkId,
                catalogRow.RowNumber,
                tileDataComplete,
                catalogRow.Fields);

            ImportTiles(tileTable, request, catalog, editorState.Grid.State, issues);
            var bandReferences = ImportSockets(socketTable, request, editorState.SocketAuthoring, issues);
            ImportBands(bandTable, request, bandReferences, editorState.SocketAuthoring, issues);
            ImportObjectSlots(slotTable, request, editorState.ObjectSlotAuthoring, issues);
            var variants = ImportVariants(variantTable, request);
            var references = CollectReferences(tileCodeTable, poolTable, edgeTable);

            MicrochunkCsvImportValidationFeedback feedback = null;
            try
            {
                var signatures = ImportEdgeSignatures(
                    edgeTable,
                    request,
                    editorState,
                    issues);
                feedback = new MicrochunkCsvImportValidationFeedback(
                    editorState.Grid.ValidateTileLayers(),
                    editorState.Grid.ValidateCoverage(),
                    editorState.ValidateSocketEdges(signatures),
                    editorState.ValidateObjectSlots(editorState.CreateAuthoringSlotPolicy()));
            }
            catch (Exception exception)
            {
                AddIssue(
                    issues,
                    MicrochunkCsvImportSource.CatalogFileName,
                    request.SelectedMicrochunkId,
                    catalogRow.RowNumber,
                    string.Empty,
                    "VALIDATION_FEEDBACK_UNAVAILABLE",
                    exception.Message);
            }

            return Result(request, catalog, editorState, issues, variants, references, feedback);
        }

        private static void ImportTiles(
            ParsedTable table,
            MicrochunkCsvImportRequest request,
            MicrochunkCsvCatalogMetadata catalog,
            MicrochunkAuthoringGridState grid,
            ICollection<MicrochunkCsvImportIssue> issues)
        {
            var selected = SelectRows(table, request.SelectedMicrochunkId);
            var importedCoordinates = new HashSet<MicrochunkLocalCoord>();
            foreach (var row in selected.OrderBy(value => value.RowNumber))
            {
                if (!TryInteger(row, "local_x", table.FileName, request.SelectedMicrochunkId, issues, out var x) ||
                    !TryInteger(row, "local_y", table.FileName, request.SelectedMicrochunkId, issues, out var y))
                {
                    continue;
                }
                if (!MicrochunkLocalCoord.TryCreate(x, y, out var coordinate))
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                        x < 0 || x >= MicrochunkConstants.WidthTiles ? "local_x" : "local_y",
                        "TILE_COORDINATE_OUT_OF_RANGE",
                        "Tile coordinate must be inside the fixed 12 x 8 microchunk grid.");
                    continue;
                }
                if (!importedCoordinates.Add(coordinate))
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                        "local_x", "TILE_COORDINATE_DUPLICATE",
                        "Tile coordinates must be unique for the selected microchunk.");
                    continue;
                }

                var columns = new[]
                {
                    "ground_code", "one_way_code", "breakable_code", "hazard_code",
                    "liquid_code", "decor_back_code", "decor_front_code", "marker_code"
                };
                for (var index = 0; index < columns.Length; index++)
                {
                    var value = row[columns[index]];
                    if (value.Length == 0) value = MicrochunkAuthoringGridCell.EmptyTileCode;
                    if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                    {
                        AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                            columns[index], "TILE_CODE_NOT_CANONICAL",
                            "Tile codes cannot contain surrounding whitespace.");
                        value = MicrochunkAuthoringGridCell.EmptyTileCode;
                    }
                    grid.PaintCell(x, y, MicrochunkAuthoringGridLayer.At(index), value);
                }
            }

            if (!catalog.TileDataComplete)
            {
                AddIssue(
                    issues,
                    MicrochunkCsvImportSource.CatalogFileName,
                    request.SelectedMicrochunkId,
                    catalog.SourceRowNumber,
                    "tile_data_complete",
                    "TILE_DATA_INCOMPLETE",
                    "The editor grid was hydrated to 96 cells; absent source cells remain exact NONE.",
                    MicrochunkCsvImportIssueSeverity.Warning);
            }

            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
            {
                var coordinate = new MicrochunkLocalCoord(x, y);
                if (importedCoordinates.Contains(coordinate)) continue;
                AddIssue(
                    issues,
                    table.FileName,
                    request.SelectedMicrochunkId,
                    0,
                    "local_x",
                    "TILE_CELL_MISSING_" + coordinate.RowMajorIndex.ToString("D2", CultureInfo.InvariantCulture),
                    "Missing source tile cell at (" + x + "," + y + ").",
                    catalog.TileDataComplete
                        ? MicrochunkCsvImportIssueSeverity.Error
                        : MicrochunkCsvImportIssueSeverity.Warning);
            }
        }

        private static List<BandReference> ImportSockets(
            ParsedTable table,
            MicrochunkCsvImportRequest request,
            MicrochunkSocketAuthoringCollection collection,
            ICollection<MicrochunkCsvImportIssue> issues)
        {
            var references = new List<BandReference>();
            foreach (var row in SelectRows(table, request.SelectedMicrochunkId)
                         .OrderBy(value => value["socket_id"], StringComparer.Ordinal)
                         .ThenBy(value => value.RowNumber))
            {
                if (!TryBoolean(row, "mandatory_allowed", table.FileName,
                        request.SelectedMicrochunkId, issues, out var mandatory))
                {
                    continue;
                }
                try
                {
                    var authoring = new MicrochunkSocketAuthoringRow(
                        row["socket_id"], row["side"], row["band_id"], row["traversal_kind"],
                        row["edge_signature_id"], mandatory, row["tool_requirement"]);
                    collection.AddSocket(authoring);
                    references.Add(new BandReference(authoring.BandId, authoring.SideToken, row.RowNumber));
                }
                catch (Exception exception)
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                        "socket_id", "SOCKET_ROW_INVALID", exception.Message);
                }
            }
            return references;
        }

        private static void ImportBands(
            ParsedTable table,
            MicrochunkCsvImportRequest request,
            IEnumerable<BandReference> references,
            MicrochunkSocketAuthoringCollection collection,
            ICollection<MicrochunkCsvImportIssue> issues)
        {
            foreach (var group in references
                         .GroupBy(value => value.BandId, StringComparer.Ordinal)
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                var rows = table.Rows
                    .Where(row => string.Equals(row["band_id"], group.Key, StringComparison.Ordinal))
                    .OrderBy(row => row.RowNumber)
                    .ToList();
                if (rows.Count == 0)
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, 0, "band_id",
                        "SOCKET_BAND_MISSING", "Referenced socket band was not found: " + group.Key);
                    continue;
                }
                if (rows.Count > 1)
                {
                    foreach (var duplicate in rows.Skip(1))
                    {
                        AddIssue(issues, table.FileName, request.SelectedMicrochunkId, duplicate.RowNumber,
                            "band_id", "SOCKET_BAND_DUPLICATE",
                            "Referenced socket band is duplicated: " + group.Key);
                    }
                    continue;
                }

                var row = rows[0];
                if (!MicrochunkSocketBandDefinition.TryParseAxisToken(row["axis"], out var axis) ||
                    axis == MicrochunkEdgeAxis.Solid)
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                        "axis", "SOCKET_BAND_AXIS_INVALID", "Socket band axis must be horizontal or vertical.");
                    continue;
                }
                var orderedReferences = group
                    .OrderBy(value => SideOrder(value.SideToken))
                    .ThenBy(value => value.RowNumber)
                    .ToList();
                var compatible = true;
                foreach (var reference in orderedReferences)
                {
                    var expected = MicrochunkSocketBandAuthoringRow.ToRuntimeAxis(reference.SideToken);
                    if (expected == axis) continue;
                    compatible = false;
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                        "axis", "SOCKET_BAND_SIDE_INCOMPATIBLE",
                        "Band axis is incompatible with socket side " + reference.SideToken + ".");
                }
                if (!compatible) continue;
                if (!TryInteger(row, "min_local_coord", table.FileName, request.SelectedMicrochunkId,
                        issues, out var minimum) ||
                    !TryInteger(row, "max_local_coord", table.FileName, request.SelectedMicrochunkId,
                        issues, out var maximum) ||
                    !TryNonNegativeInteger(row, "minimum_clearance_tiles", table.FileName,
                        request.SelectedMicrochunkId, issues, out var clearance))
                {
                    continue;
                }
                try
                {
                    collection.AddBand(new MicrochunkSocketBandAuthoringRow(
                        group.Key, orderedReferences[0].SideToken, minimum, maximum, clearance));
                }
                catch (Exception exception)
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                        "min_local_coord", "SOCKET_BAND_RANGE_INVALID", exception.Message);
                }
            }
        }

        private static void ImportObjectSlots(
            ParsedTable table,
            MicrochunkCsvImportRequest request,
            MicrochunkObjectSlotAuthoringCollection collection,
            ICollection<MicrochunkCsvImportIssue> issues)
        {
            foreach (var row in SelectRows(table, request.SelectedMicrochunkId)
                         .OrderBy(value => value["slot_id"], StringComparer.Ordinal)
                         .ThenBy(value => value.RowNumber))
            {
                if (!TryInteger(row, "local_x", table.FileName, request.SelectedMicrochunkId, issues, out var x) ||
                    !TryInteger(row, "local_y", table.FileName, request.SelectedMicrochunkId, issues, out var y) ||
                    !TryNonNegativeInteger(row, "forbidden_radius_tiles", table.FileName,
                        request.SelectedMicrochunkId, issues, out var radius) ||
                    !TryBoolean(row, "required", table.FileName,
                        request.SelectedMicrochunkId, issues, out var required) ||
                    !TryBoolean(row, "visible_from_route", table.FileName,
                        request.SelectedMicrochunkId, issues, out var visible))
                {
                    continue;
                }
                try
                {
                    collection.Add(new MicrochunkObjectSlotAuthoringRow(
                        row["slot_id"], x, y, row["slot_category"], row["allowed_pool_id"],
                        row["orientation"], radius, required, visible, row["required_marker_code"]));
                }
                catch (Exception exception)
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                        "slot_id", "OBJECT_SLOT_ROW_INVALID", exception.Message);
                }
            }
        }

        private static IReadOnlyList<MicrochunkCsvVariantMetadata> ImportVariants(
            ParsedTable table,
            MicrochunkCsvImportRequest request)
        {
            return new ReadOnlyCollection<MicrochunkCsvVariantMetadata>(
                SelectRows(table, request.SelectedMicrochunkId)
                    .Select(row => new MicrochunkCsvVariantMetadata(
                        request.SelectedMicrochunkId, row.RowNumber, row.Fields))
                    .OrderBy(value => CanonicalFieldSignature(value.Fields), StringComparer.Ordinal)
                    .ThenBy(value => value.SourceRowNumber)
                    .ToList());
        }

        private static IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> ImportEdgeSignatures(
            ParsedTable table,
            MicrochunkCsvImportRequest request,
            MicrochunkSocketAndSlotEditorViewModel editorState,
            ICollection<MicrochunkCsvImportIssue> issues)
        {
            if (table.Rows.Count == 0) return editorState.CreateAuthoringSignatureLookup();

            var result = new SortedDictionary<string, MicrochunkEdgeSignatureDefinition>(StringComparer.Ordinal);
            foreach (var socket in editorState.SocketAuthoring.Sockets
                         .GroupBy(value => value.EdgeSignatureId, StringComparer.Ordinal)
                         .OrderBy(group => group.Key, StringComparer.Ordinal)
                         .Select(group => group.First()))
            {
                var rows = table.Rows
                    .Where(row => string.Equals(
                        row["edge_signature_id"], socket.EdgeSignatureId, StringComparison.Ordinal))
                    .OrderBy(row => row.RowNumber)
                    .ToList();
                if (rows.Count == 0)
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, 0,
                        "edge_signature_id", "EDGE_SIGNATURE_MISSING",
                        "Referenced edge signature was not found: " + socket.EdgeSignatureId);
                    continue;
                }
                if (rows.Count > 1)
                {
                    foreach (var duplicate in rows.Skip(1))
                    {
                        AddIssue(issues, table.FileName, request.SelectedMicrochunkId, duplicate.RowNumber,
                            "edge_signature_id", "EDGE_SIGNATURE_DUPLICATE",
                            "Referenced edge signature is duplicated: " + socket.EdgeSignatureId);
                    }
                    continue;
                }
                var row = rows[0];
                try
                {
                    if (!TryNonNegativeInteger(row, "ground_entry_height", table.FileName,
                            request.SelectedMicrochunkId, issues, out var groundHeight) ||
                        !TryNonNegativeInteger(row, "clearance_width", table.FileName,
                            request.SelectedMicrochunkId, issues, out var clearanceWidth) ||
                        !TryNonNegativeInteger(row, "clearance_height", table.FileName,
                            request.SelectedMicrochunkId, issues, out var clearanceHeight) ||
                        !TryBoolean(row, "mandatory_allowed", table.FileName,
                            request.SelectedMicrochunkId, issues, out var mandatory))
                    {
                        continue;
                    }
                    result.Add(socket.EdgeSignatureId, new MicrochunkEdgeSignatureDefinition(
                        socket.EdgeSignatureId,
                        row["axis"],
                        row["band_id"],
                        MicrochunkSocketAuthoringRow.ParseTraversalKind(row["traversal_kind"]),
                        groundHeight,
                        clearanceWidth,
                        clearanceHeight,
                        MicrochunkSocketAuthoringRow.ParseToolRequirement(row["tool_requirement"]),
                        mandatory,
                        SplitTokens(row["tags"]),
                        row["notes"]));
                }
                catch (Exception exception)
                {
                    AddIssue(issues, table.FileName, request.SelectedMicrochunkId, row.RowNumber,
                        "edge_signature_id", "EDGE_SIGNATURE_ROW_INVALID", exception.Message);
                }
            }

            foreach (var socket in editorState.SocketAuthoring.Sockets)
            {
                if (!result.ContainsKey(socket.EdgeSignatureId))
                {
                    var fallback = editorState.CreateAuthoringSignatureLookup();
                    if (fallback.TryGetValue(socket.EdgeSignatureId, out var value))
                    {
                        result[socket.EdgeSignatureId] = value;
                    }
                }
            }
            return new ReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition>(result);
        }

        private static IReadOnlyList<MicrochunkCsvReferenceMetadata> CollectReferences(
            params ParsedTable[] tables)
        {
            var rows = new List<MicrochunkCsvReferenceMetadata>();
            foreach (var table in tables)
            foreach (var row in table.Rows)
            {
                rows.Add(new MicrochunkCsvReferenceMetadata(table.FileName, row.RowNumber, row.Fields));
            }
            return new ReadOnlyCollection<MicrochunkCsvReferenceMetadata>(rows
                .OrderBy(value => value.FileName, StringComparer.Ordinal)
                .ThenBy(value => value.SourceRowNumber)
                .ToList());
        }

        private static ParsedTable ReadTable(
            byte[] bytes,
            string fileName,
            IReadOnlyList<string> requiredHeaders,
            bool requiredTable,
            string selectedId,
            ICollection<MicrochunkCsvImportIssue> issues)
        {
            if (bytes.Length == 0)
            {
                if (requiredTable)
                {
                    AddIssue(issues, fileName, selectedId, 0, string.Empty,
                        "CSV_SOURCE_EMPTY", "Required CSV source snapshot is empty.");
                }
                return new ParsedTable(fileName, Array.Empty<ParsedRow>());
            }

            var read = new Rfc4180CsvReader().Read(bytes, fileName);
            if (!read.Success)
            {
                foreach (var error in read.Errors.Select(value => value.ToString())
                             .OrderBy(value => value, StringComparer.Ordinal))
                {
                    AddIssue(issues, fileName, selectedId, 0, string.Empty, "CSV_SYNTAX", error);
                }
                return new ParsedTable(fileName, Array.Empty<ParsedRow>());
            }
            if (read.Records.Count == 0)
            {
                AddIssue(issues, fileName, selectedId, 0, string.Empty,
                    "CSV_HEADER_MISSING", "CSV table does not contain a header record.");
                return new ParsedTable(fileName, Array.Empty<ParsedRow>());
            }

            var headerRecord = read.Records[0];
            var headers = headerRecord.Fields.Select(field => field.Value).ToList();
            if (headers.Count != headers.Distinct(StringComparer.Ordinal).Count())
            {
                AddIssue(issues, fileName, selectedId, headerRecord.StartLocation.PhysicalLine,
                    string.Empty, "CSV_HEADER_DUPLICATE", "CSV header names must be unique.");
                return new ParsedTable(fileName, Array.Empty<ParsedRow>());
            }
            foreach (var required in requiredHeaders)
            {
                if (headers.Contains(required, StringComparer.Ordinal)) continue;
                AddIssue(issues, fileName, selectedId, headerRecord.StartLocation.PhysicalLine,
                    required, "CSV_HEADER_REQUIRED_COLUMN_MISSING",
                    "Required CSV column was not found: " + required);
            }
            if (requiredHeaders.Any(required => !headers.Contains(required, StringComparer.Ordinal)))
            {
                return new ParsedTable(fileName, Array.Empty<ParsedRow>());
            }

            var parsed = new List<ParsedRow>();
            for (var index = 1; index < read.Records.Count; index++)
            {
                var record = read.Records[index];
                if (record.Fields.Count != headers.Count)
                {
                    AddIssue(issues, fileName, selectedId, record.StartLocation.PhysicalLine,
                        string.Empty, "CSV_COLUMN_COUNT_MISMATCH",
                        "CSV row field count does not match its header.");
                    continue;
                }
                var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
                for (var fieldIndex = 0; fieldIndex < headers.Count; fieldIndex++)
                {
                    values.Add(headers[fieldIndex], record.Fields[fieldIndex].Value);
                }
                parsed.Add(new ParsedRow(record.StartLocation.PhysicalLine, values));
            }
            return new ParsedTable(fileName, parsed);
        }

        private static List<ParsedRow> SelectRows(ParsedTable table, string selectedId)
        {
            if (!table.Rows.Any() || !table.Rows[0].Fields.ContainsKey("microchunk_id"))
            {
                return new List<ParsedRow>();
            }
            return table.Rows
                .Where(row => string.Equals(row["microchunk_id"], selectedId, StringComparison.Ordinal))
                .OrderBy(row => row.RowNumber)
                .ToList();
        }

        private static bool TryInteger(
            ParsedRow row,
            string column,
            string fileName,
            string selectedId,
            ICollection<MicrochunkCsvImportIssue> issues,
            out int value)
        {
            if (int.TryParse(row[column], NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
            AddIssue(issues, fileName, selectedId, row.RowNumber, column,
                "INTEGER_INVALID", "Expected a canonical invariant integer.");
            return false;
        }

        private static bool TryNonNegativeInteger(
            ParsedRow row,
            string column,
            string fileName,
            string selectedId,
            ICollection<MicrochunkCsvImportIssue> issues,
            out int value)
        {
            if (TryInteger(row, column, fileName, selectedId, issues, out value) && value >= 0)
            {
                return true;
            }
            if (value < 0)
            {
                AddIssue(issues, fileName, selectedId, row.RowNumber, column,
                    "INTEGER_NEGATIVE", "Expected a non-negative integer.");
            }
            return false;
        }

        private static bool TryBoolean(
            ParsedRow row,
            string column,
            string fileName,
            string selectedId,
            ICollection<MicrochunkCsvImportIssue> issues,
            out bool value)
        {
            if (row[column] == "0")
            {
                value = false;
                return true;
            }
            if (row[column] == "1")
            {
                value = true;
                return true;
            }
            value = false;
            AddIssue(issues, fileName, selectedId, row.RowNumber, column,
                "BOOLEAN_INVALID", "Boolean values must be exactly 0 or 1.");
            return false;
        }

        private static IEnumerable<string> SplitTokens(string value)
        {
            return string.IsNullOrEmpty(value)
                ? Array.Empty<string>()
                : value.Split('|').Where(token => token.Length > 0);
        }

        private static int SideOrder(string side)
        {
            switch (side)
            {
                case "L": return 0;
                case "R": return 1;
                case "D": return 2;
                case "U": return 3;
                default: return int.MaxValue;
            }
        }

        private static string CanonicalFieldSignature(IReadOnlyDictionary<string, string> fields)
        {
            return string.Join("\n", fields.OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => value.Key + "=" + value.Value));
        }

        private static MicrochunkCsvImportResult Result(
            MicrochunkCsvImportRequest request,
            MicrochunkCsvCatalogMetadata catalog,
            MicrochunkSocketAndSlotEditorViewModel editorState,
            IEnumerable<MicrochunkCsvImportIssue> issues,
            IEnumerable<MicrochunkCsvVariantMetadata> variants,
            IEnumerable<MicrochunkCsvReferenceMetadata> references,
            MicrochunkCsvImportValidationFeedback feedback)
        {
            return new MicrochunkCsvImportResult(
                request, catalog, editorState, issues, variants, references, feedback);
        }

        private static void AddIssue(
            ICollection<MicrochunkCsvImportIssue> issues,
            string fileName,
            string selectedId,
            int rowNumber,
            string column,
            string code,
            string message,
            MicrochunkCsvImportIssueSeverity severity = MicrochunkCsvImportIssueSeverity.Error)
        {
            issues.Add(new MicrochunkCsvImportIssue(
                fileName, selectedId, rowNumber, column, code, message, severity));
        }

        private sealed class ParsedTable
        {
            public string FileName { get; }
            public IReadOnlyList<ParsedRow> Rows { get; }

            public ParsedTable(string fileName, IEnumerable<ParsedRow> rows)
            {
                FileName = fileName;
                Rows = new ReadOnlyCollection<ParsedRow>(rows.ToList());
            }
        }

        private sealed class ParsedRow
        {
            public int RowNumber { get; }
            public IReadOnlyDictionary<string, string> Fields { get; }
            public string this[string name] =>
                Fields.TryGetValue(name, out var value) ? value : string.Empty;

            public ParsedRow(int rowNumber, IDictionary<string, string> fields)
            {
                RowNumber = rowNumber;
                Fields = new ReadOnlyDictionary<string, string>(
                    new SortedDictionary<string, string>(fields, StringComparer.Ordinal));
            }
        }

        private sealed class BandReference
        {
            public string BandId { get; }
            public string SideToken { get; }
            public int RowNumber { get; }

            public BandReference(string bandId, string sideToken, int rowNumber)
            {
                BandId = bandId;
                SideToken = sideToken;
                RowNumber = rowNumber;
            }
        }
    }
}
