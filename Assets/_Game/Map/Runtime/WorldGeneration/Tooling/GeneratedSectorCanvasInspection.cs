using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public sealed class GeneratedSectorCanvasLayerDefinition
    {
        internal GeneratedSectorCanvasLayerDefinition(string token, string shortLabel,
            string ownerCategory)
        {
            Token = token;
            ShortLabel = shortLabel;
            OwnerCategory = ownerCategory;
        }

        public string Token { get; }
        public string ShortLabel { get; }
        public string OwnerCategory { get; }
        public bool EnabledByDefault => true;
        public bool IsReadOnly => true;
    }

    /// <summary>Single production catalog for the fixed sector inspection canvas.</summary>
    public static class GeneratedSectorCanvasLayerCatalog
    {
        private static readonly ReadOnlyCollection<GeneratedSectorCanvasLayerDefinition> layers =
            new ReadOnlyCollection<GeneratedSectorCanvasLayerDefinition>(new[]
            {
                new GeneratedSectorCanvasLayerDefinition("Owner", "OWN", "CanvasOwnership"),
                new GeneratedSectorCanvasLayerDefinition("Spine", "SPN", "Traversal"),
                new GeneratedSectorCanvasLayerDefinition("Envelope", "ENV", "TraversalProtection"),
                new GeneratedSectorCanvasLayerDefinition("Density", "DEN", "DensityValidation"),
            });

        public static IReadOnlyList<GeneratedSectorCanvasLayerDefinition> Layers => layers;
        public static IReadOnlyList<string> Tokens => new ReadOnlyCollection<string>(
            layers.Select(value => value.Token).ToArray());
        public static int Width => WorldGenConstants.SectorWidthTiles;
        public static int Height => WorldGenConstants.SectorHeightTiles;
        public static int CellCount => WorldGenConstants.TilesPerSector;

        public static int IndexOf(string token)
        {
            for (var index = 0; index < layers.Count; index++)
                if (string.Equals(layers[index].Token, token, StringComparison.Ordinal)) return index;
            return -1;
        }

        internal static string[] NormalizeTokens(IEnumerable<string> tokens)
        {
            var requested = new HashSet<string>(tokens ?? layers.Select(value => value.Token),
                StringComparer.Ordinal);
            if (requested.Any(value => IndexOf(value) < 0))
                throw new ArgumentException("Enabled sector canvas tokens must come from the catalog.",
                    nameof(tokens));
            return layers.Where(value => requested.Contains(value.Token))
                .Select(value => value.Token).ToArray();
        }
    }

    public sealed class GeneratedSectorCanvasCellRecord
    {
        public GeneratedSectorCanvasCellRecord(int sectorX, int sectorY, int localX, int localY,
            bool hasWorldCellCoordinate, int worldX, int worldY, string ownerLayer,
            string sourceOwner, string provenanceId, bool spineMarker, string spineMovementKind,
            bool envelopeMarker, string envelopeRelation, string densityMarker,
            string validationMarker, string missingDataMarker)
        {
            if (sectorX < 0 || sectorX >= WorldGenConstants.SectorColumns)
                throw new ArgumentOutOfRangeException(nameof(sectorX));
            if (sectorY < 0 || sectorY >= WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(nameof(sectorY));
            if (localX < 0 || localX >= WorldGenConstants.SectorWidthTiles)
                throw new ArgumentOutOfRangeException(nameof(localX));
            if (localY < 0 || localY >= WorldGenConstants.SectorHeightTiles)
                throw new ArgumentOutOfRangeException(nameof(localY));
            SectorX = sectorX;
            SectorY = sectorY;
            LocalX = localX;
            LocalY = localY;
            HasWorldCellCoordinate = hasWorldCellCoordinate;
            WorldX = worldX;
            WorldY = worldY;
            OwnerLayer = ownerLayer ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
            ProvenanceId = provenanceId ?? string.Empty;
            SpineMarker = spineMarker;
            SpineMovementKind = spineMovementKind ?? string.Empty;
            EnvelopeMarker = envelopeMarker;
            EnvelopeRelation = envelopeRelation ?? string.Empty;
            DensityMarker = densityMarker ?? string.Empty;
            ValidationMarker = validationMarker ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public bool HasWorldCellCoordinate { get; }
        public int WorldX { get; }
        public int WorldY { get; }
        public string OwnerLayer { get; }
        public string SourceOwner { get; }
        public string ProvenanceId { get; }
        public bool SpineMarker { get; }
        public string SpineMovementKind { get; }
        public bool EnvelopeMarker { get; }
        public string EnvelopeRelation { get; }
        public string DensityMarker { get; }
        public string ValidationMarker { get; }
        public string MissingDataMarker { get; }
        public bool IsMissingData => !string.IsNullOrEmpty(MissingDataMarker);
        public int RowMajorIndex => LocalY * WorldGenConstants.SectorWidthTiles + LocalX;

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            SectorX.ToString(CultureInfo.InvariantCulture),
            SectorY.ToString(CultureInfo.InvariantCulture),
            LocalX.ToString(CultureInfo.InvariantCulture),
            LocalY.ToString(CultureInfo.InvariantCulture),
            HasWorldCellCoordinate ? "true" : "false",
            WorldX.ToString(CultureInfo.InvariantCulture),
            WorldY.ToString(CultureInfo.InvariantCulture), OwnerLayer, SourceOwner, ProvenanceId,
            SpineMarker ? "true" : "false", SpineMovementKind,
            EnvelopeMarker ? "true" : "false", EnvelopeRelation, DensityMarker,
            ValidationMarker, MissingDataMarker);

        public static GeneratedSectorCanvasCellRecord Missing(int sectorX, int sectorY,
            int localX, int localY, string reason)
        {
            var worldX = sectorX * WorldGenConstants.SectorWidthTiles + localX;
            var worldY = sectorY * WorldGenConstants.SectorHeightTiles + localY;
            return new GeneratedSectorCanvasCellRecord(sectorX, sectorY, localX, localY,
                true, worldX, worldY, "MissingData", "MissingData", "MissingData",
                false, "MissingData", false, "MissingData", "MissingData",
                "MissingData", reason);
        }
    }

    public sealed class GeneratedSectorCanvasMissingDataRecord
    {
        public GeneratedSectorCanvasMissingDataRecord(string layerToken, string reason)
        {
            if (GeneratedSectorCanvasLayerCatalog.IndexOf(layerToken) < 0)
                throw new ArgumentException("Unknown sector canvas layer token.", nameof(layerToken));
            LayerToken = layerToken;
            Reason = reason ?? string.Empty;
        }

        public string LayerToken { get; }
        public string Reason { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(LayerToken, Reason);
    }

    /// <summary>An immutable fixed-size cell inspection surface with no scene objects.</summary>
    public sealed class GeneratedSectorCanvasInspection
    {
        public const string SchemaVersion = "map20_02.sector_canvas_inspection.v1";
        public const string TaskId = "MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR";

        private readonly ReadOnlyCollection<string> enabledCanvasLayerTokens;
        private readonly ReadOnlyCollection<GeneratedSectorCanvasCellRecord> cellRecords;
        private readonly ReadOnlyCollection<GeneratedSectorCanvasMissingDataRecord> missingDataRecords;

        public GeneratedSectorCanvasInspection(int sectorX, int sectorY,
            IEnumerable<string> enabledTokens, IEnumerable<GeneratedSectorCanvasCellRecord> cells,
            IEnumerable<GeneratedSectorCanvasMissingDataRecord> missingData,
            int selectedLocalX, int selectedLocalY, string createdUtc)
        {
            if (sectorX < 0 || sectorX >= WorldGenConstants.SectorColumns)
                throw new ArgumentOutOfRangeException(nameof(sectorX));
            if (sectorY < 0 || sectorY >= WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(nameof(sectorY));
            if (selectedLocalX < 0 || selectedLocalX >= WorldGenConstants.SectorWidthTiles)
                throw new ArgumentOutOfRangeException(nameof(selectedLocalX));
            if (selectedLocalY < 0 || selectedLocalY >= WorldGenConstants.SectorHeightTiles)
                throw new ArgumentOutOfRangeException(nameof(selectedLocalY));

            SectorX = sectorX;
            SectorY = sectorY;
            CreatedUtc = createdUtc ?? string.Empty;
            enabledCanvasLayerTokens = new ReadOnlyCollection<string>(
                GeneratedSectorCanvasLayerCatalog.NormalizeTokens(enabledTokens));
            var supplied = (cells ?? Array.Empty<GeneratedSectorCanvasCellRecord>())
                .Where(value => value != null).ToArray();
            if (supplied.Any(value => value.SectorX != sectorX || value.SectorY != sectorY))
                throw new ArgumentException("All canvas cells must belong to the selected sector.",
                    nameof(cells));
            if (supplied.GroupBy(value => value.RowMajorIndex).Any(group => group.Count() != 1))
                throw new ArgumentException("Canvas cell coordinates must be unique.", nameof(cells));
            if (supplied.Length != WorldGenConstants.TilesPerSector)
                throw new ArgumentException("The inspection canvas requires every sector cell.",
                    nameof(cells));
            var ordered = supplied.OrderBy(value => value.RowMajorIndex).ToArray();
            for (var index = 0; index < ordered.Length; index++)
                if (ordered[index].RowMajorIndex != index)
                    throw new ArgumentException("The inspection canvas has a missing cell.", nameof(cells));
            cellRecords = new ReadOnlyCollection<GeneratedSectorCanvasCellRecord>(ordered);
            missingDataRecords = new ReadOnlyCollection<GeneratedSectorCanvasMissingDataRecord>(
                (missingData ?? Array.Empty<GeneratedSectorCanvasMissingDataRecord>())
                .Where(value => value != null).OrderBy(value =>
                    GeneratedSectorCanvasLayerCatalog.IndexOf(value.LayerToken)).ThenBy(
                        value => value.CanonicalLine, StringComparer.Ordinal).ToArray());
            SelectedCellRecord = cellRecords[
                selectedLocalY * WorldGenConstants.SectorWidthTiles + selectedLocalX];
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public int Width => WorldGenConstants.SectorWidthTiles;
        public int Height => WorldGenConstants.SectorHeightTiles;
        public int CellCount => WorldGenConstants.TilesPerSector;
        public IReadOnlyList<string> EnabledCanvasLayerTokens => enabledCanvasLayerTokens;
        public GeneratedSectorCanvasCellRecord SelectedCellRecord { get; }
        public IReadOnlyList<GeneratedSectorCanvasCellRecord> CellRecords => cellRecords;
        public IReadOnlyList<GeneratedSectorCanvasMissingDataRecord> MissingDataRecords => missingDataRecords;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }

        public GeneratedSectorCanvasCellRecord Cell(int localX, int localY)
        {
            if (localX < 0 || localX >= WorldGenConstants.SectorWidthTiles ||
                localY < 0 || localY >= WorldGenConstants.SectorHeightTiles)
                throw new ArgumentOutOfRangeException(nameof(localX));
            return cellRecords[localY * WorldGenConstants.SectorWidthTiles + localX];
        }

        public GeneratedSectorCanvasInspection SelectCell(int localX, int localY) =>
            new GeneratedSectorCanvasInspection(SectorX, SectorY, enabledCanvasLayerTokens,
                cellRecords, missingDataRecords, localX, localY, CreatedUtc);

        public string Serialize() => GeneratedInspectionCanonical.ToJson(ToDocument());

        public static GeneratedSectorCanvasInspection CreateMissingDataSample(int sectorX,
            int sectorY, string createdUtc)
        {
            const string reason =
                "MAP20_01 run artifact has no published sector-canvas facts; inspection remains read-only.";
            var cells = new List<GeneratedSectorCanvasCellRecord>(WorldGenConstants.TilesPerSector);
            for (var y = 0; y < WorldGenConstants.SectorHeightTiles; y++)
            for (var x = 0; x < WorldGenConstants.SectorWidthTiles; x++)
                cells.Add(GeneratedSectorCanvasCellRecord.Missing(sectorX, sectorY, x, y, reason));
            var missing = GeneratedSectorCanvasLayerCatalog.Tokens.Select(token =>
                new GeneratedSectorCanvasMissingDataRecord(token, reason));
            return new GeneratedSectorCanvasInspection(sectorX, sectorY,
                GeneratedSectorCanvasLayerCatalog.Tokens, cells, missing,
                WorldGenConstants.SectorWidthTiles / 2,
                WorldGenConstants.SectorHeightTiles / 2, createdUtc);
        }

        private string ComputeCanonicalDigest()
        {
            var lines = new List<string>
            {
                SchemaVersion,
                TaskId,
                SectorX.ToString(CultureInfo.InvariantCulture),
                SectorY.ToString(CultureInfo.InvariantCulture),
                Width.ToString(CultureInfo.InvariantCulture),
                Height.ToString(CultureInfo.InvariantCulture),
                CellCount.ToString(CultureInfo.InvariantCulture),
                string.Join("|", enabledCanvasLayerTokens),
                SelectedCellRecord.RowMajorIndex.ToString(CultureInfo.InvariantCulture),
                "created_utc_excluded=true",
            };
            lines.AddRange(cellRecords.Select(value => "cell=" + value.CanonicalLine));
            lines.AddRange(missingDataRecords.Select(value => "missing=" + value.CanonicalLine));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private GeneratedSectorCanvasInspectionDocument ToDocument() =>
            new GeneratedSectorCanvasInspectionDocument
            {
                schema_version = SchemaVersion,
                task_id = TaskId,
                sector_coordinate = new GeneratedCoordinateDocument
                    { x = SectorX, y = SectorY },
                sector_width = Width,
                sector_height = Height,
                cell_count = CellCount,
                enabled_canvas_layer_tokens = enabledCanvasLayerTokens.ToArray(),
                selected_cell_record = GeneratedSectorCanvasCellDocument.From(SelectedCellRecord),
                cell_records = cellRecords.Select(GeneratedSectorCanvasCellDocument.From).ToArray(),
                missing_data_records = missingDataRecords.Select(
                    GeneratedSectorCanvasMissingDataDocument.From).ToArray(),
                canonical_digest = CanonicalDigest,
                created_utc = CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
            };
    }

    [Serializable]
    internal sealed class GeneratedCoordinateDocument
    {
        public int x;
        public int y;
    }

    [Serializable]
    internal sealed class GeneratedSectorCanvasInspectionDocument
    {
        public string schema_version;
        public string task_id;
        public GeneratedCoordinateDocument sector_coordinate;
        public int sector_width;
        public int sector_height;
        public int cell_count;
        public string[] enabled_canvas_layer_tokens;
        public GeneratedSectorCanvasCellDocument selected_cell_record;
        public GeneratedSectorCanvasCellDocument[] cell_records;
        public GeneratedSectorCanvasMissingDataDocument[] missing_data_records;
        public string canonical_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
    }

    [Serializable]
    internal sealed class GeneratedSectorCanvasCellDocument
    {
        public GeneratedCoordinateDocument sector_coordinate;
        public GeneratedCoordinateDocument local_cell_coordinate;
        public bool has_world_cell_coordinate;
        public GeneratedCoordinateDocument world_cell_coordinate;
        public string owner_layer;
        public string source_owner;
        public string provenance_id;
        public bool spine_marker;
        public string spine_movement_kind;
        public bool envelope_marker;
        public string envelope_relation;
        public string density_marker;
        public string validation_marker;
        public string missing_data_marker;

        public static GeneratedSectorCanvasCellDocument From(GeneratedSectorCanvasCellRecord value) =>
            new GeneratedSectorCanvasCellDocument
            {
                sector_coordinate = new GeneratedCoordinateDocument
                    { x = value.SectorX, y = value.SectorY },
                local_cell_coordinate = new GeneratedCoordinateDocument
                    { x = value.LocalX, y = value.LocalY },
                has_world_cell_coordinate = value.HasWorldCellCoordinate,
                world_cell_coordinate = new GeneratedCoordinateDocument
                    { x = value.WorldX, y = value.WorldY },
                owner_layer = value.OwnerLayer,
                source_owner = value.SourceOwner,
                provenance_id = value.ProvenanceId,
                spine_marker = value.SpineMarker,
                spine_movement_kind = value.SpineMovementKind,
                envelope_marker = value.EnvelopeMarker,
                envelope_relation = value.EnvelopeRelation,
                density_marker = value.DensityMarker,
                validation_marker = value.ValidationMarker,
                missing_data_marker = value.MissingDataMarker,
            };
    }

    [Serializable]
    internal sealed class GeneratedSectorCanvasMissingDataDocument
    {
        public string layer_token;
        public string reason;

        public static GeneratedSectorCanvasMissingDataDocument From(
            GeneratedSectorCanvasMissingDataRecord value) =>
            new GeneratedSectorCanvasMissingDataDocument
                { layer_token = value.LayerToken, reason = value.Reason };
    }
}
