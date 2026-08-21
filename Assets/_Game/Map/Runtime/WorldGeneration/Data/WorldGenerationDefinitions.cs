using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    internal static class WorldRouteDefinitionValueReader
    {
        public static CsvParsedField Field(
            CsvParsedRecord record,
            int index,
            string columnName)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (index < 0 || index >= record.Fields.Count)
            {
                throw new InvalidOperationException(
                    "Parsed record does not contain column " + columnName + ".");
            }

            var field = record.Fields[index];
            if (field == null ||
                !string.Equals(field.Schema.ColumnName, columnName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Parsed field order does not match column " + columnName + ".");
            }

            return field;
        }

        public static string String(CsvParsedRecord record, int index, string columnName)
        {
            return Field(record, index, columnName).Value.StringValue;
        }

        public static int Int(CsvParsedRecord record, int index, string columnName)
        {
            return Field(record, index, columnName).Value.IntValue;
        }

        public static ulong ULong(CsvParsedRecord record, int index, string columnName)
        {
            return Field(record, index, columnName).Value.ULongValue;
        }

        public static float Float(CsvParsedRecord record, int index, string columnName)
        {
            return Field(record, index, columnName).Value.FloatValue;
        }

        public static bool Bool(CsvParsedRecord record, int index, string columnName)
        {
            return Field(record, index, columnName).Value.BoolValue;
        }

        public static CsvHexValue Hex(CsvParsedRecord record, int index, string columnName)
        {
            return Field(record, index, columnName).Value.HexValue;
        }

        public static DateTimeOffset DateTime(
            CsvParsedRecord record,
            int index,
            string columnName)
        {
            return Field(record, index, columnName).Value.DateTimeValue;
        }

        public static IReadOnlyList<string> StringList(
            CsvParsedRecord record,
            int index,
            string columnName)
        {
            var source = Field(record, index, columnName).Value.StringListValue;
            return new ReadOnlyCollection<string>(new List<string>(source));
        }

        public static IReadOnlyList<int> IntList(
            CsvParsedRecord record,
            int index,
            string columnName)
        {
            var source = Field(record, index, columnName).Value.IntListValue;
            return new ReadOnlyCollection<int>(new List<int>(source));
        }
    }

    public sealed class WorldProfileDefinition
    {
        internal WorldProfileDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            WorldProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "world_profile_id");
            DisplayNameKo = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "display_name_ko");
            WidthTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "width_tiles");
            HeightTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "height_tiles");
            SectorWidthTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "sector_width_tiles");
            SectorHeightTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "sector_height_tiles");
            SectorCols = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "sector_cols");
            SectorRows = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "sector_rows");
            MicroWidthTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "micro_width_tiles");
            MicroHeightTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "micro_height_tiles");
            MicroColsPerSector = WorldRouteDefinitionValueReader.Int(sourceRecord, 10, "micro_cols_per_sector");
            MicroRowsPerSector = WorldRouteDefinitionValueReader.Int(sourceRecord, 11, "micro_rows_per_sector");
            MinCompletionDistanceTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 12, "min_completion_distance_tiles");
            MaxShortestCompletionDistanceTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 13, "max_shortest_completion_distance_tiles");
            NormalCompletionMinTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 14, "normal_completion_min_tiles");
            NormalCompletionMaxTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 15, "normal_completion_max_tiles");
            OptionalCompletionMaxTiles = WorldRouteDefinitionValueReader.Int(sourceRecord, 16, "optional_completion_max_tiles");
            MaxRevisitRatio = WorldRouteDefinitionValueReader.Float(sourceRecord, 17, "max_revisit_ratio");
            RequiredVillageCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 18, "required_village_count");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 19, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 20, "notes");
        }

        public string WorldProfileId { get; }
        public string DisplayNameKo { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public int SectorWidthTiles { get; }
        public int SectorHeightTiles { get; }
        public int SectorCols { get; }
        public int SectorRows { get; }
        public int MicroWidthTiles { get; }
        public int MicroHeightTiles { get; }
        public int MicroColsPerSector { get; }
        public int MicroRowsPerSector { get; }
        public int MinCompletionDistanceTiles { get; }
        public int MaxShortestCompletionDistanceTiles { get; }
        public int NormalCompletionMinTiles { get; }
        public int NormalCompletionMaxTiles { get; }
        public int OptionalCompletionMaxTiles { get; }
        public float MaxRevisitRatio { get; }
        public int RequiredVillageCount { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class GenerationProfileDefinition
    {
        internal GenerationProfileDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            GenerationProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "generation_profile_id");
            WorldProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 1, "world_profile_id");
            MandatorySectorMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 2, "mandatory_sector_min");
            MandatorySectorMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 3, "mandatory_sector_max");
            Type0SectorMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 4, "type0_sector_min");
            Type0SectorMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 5, "type0_sector_max");
            ReservedSectorMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 6, "reserved_sector_min");
            ReservedSectorMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 7, "reserved_sector_max");
            InactiveSectorMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "inactive_sector_min");
            InactiveSectorMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 9, "inactive_sector_max");
            StartEdgeRingMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 10, "start_edge_ring_min");
            StartEdgeRingMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 11, "start_edge_ring_max");
            MandatoryLoopMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 12, "mandatory_loop_min");
            MandatoryLoopMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 13, "mandatory_loop_max");
            OptionalRegionDepthMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 14, "optional_region_depth_min");
            OptionalRegionDepthMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 15, "optional_region_depth_max");
            OptionalRegionCountMin = WorldRouteDefinitionValueReader.Int(sourceRecord, 16, "optional_region_count_min");
            OptionalRegionCountMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 17, "optional_region_count_max");
            SiteReservationRetryMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 18, "site_reservation_retry_max");
            BiomeRetryMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 19, "biome_retry_max");
            RouteRetryMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 20, "route_retry_max");
            SectorSolveRetryMax = WorldRouteDefinitionValueReader.Int(sourceRecord, 21, "sector_solve_retry_max");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 22, "active");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 23, "notes");
        }

        public string GenerationProfileId { get; }
        public string WorldProfileId { get; }
        public int MandatorySectorMin { get; }
        public int MandatorySectorMax { get; }
        public int Type0SectorMin { get; }
        public int Type0SectorMax { get; }
        public int ReservedSectorMin { get; }
        public int ReservedSectorMax { get; }
        public int InactiveSectorMin { get; }
        public int InactiveSectorMax { get; }
        public int StartEdgeRingMin { get; }
        public int StartEdgeRingMax { get; }
        public int MandatoryLoopMin { get; }
        public int MandatoryLoopMax { get; }
        public int OptionalRegionDepthMin { get; }
        public int OptionalRegionDepthMax { get; }
        public int OptionalRegionCountMin { get; }
        public int OptionalRegionCountMax { get; }
        public int SiteReservationRetryMax { get; }
        public int BiomeRetryMax { get; }
        public int RouteRetryMax { get; }
        public int SectorSolveRetryMax { get; }
        public bool Active { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class GenerationPassDefinition
    {
        internal GenerationPassDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            GenerationProfileId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "generation_profile_id");
            PassOrder = WorldRouteDefinitionValueReader.Int(sourceRecord, 1, "pass_order");
            PassId = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "pass_id");
            ClassName = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "class_name");
            RngStreamId = WorldRouteDefinitionValueReader.String(sourceRecord, 4, "rng_stream_id");
            InputArtifacts = WorldRouteDefinitionValueReader.StringList(sourceRecord, 5, "input_artifacts");
            OutputArtifacts = WorldRouteDefinitionValueReader.StringList(sourceRecord, 6, "output_artifacts");
            FailurePolicy = WorldRouteDefinitionValueReader.String(sourceRecord, 7, "failure_policy");
            MaxRetryCount = WorldRouteDefinitionValueReader.Int(sourceRecord, 8, "max_retry_count");
            Enabled = WorldRouteDefinitionValueReader.Bool(sourceRecord, 9, "enabled");
            Notes = WorldRouteDefinitionValueReader.String(sourceRecord, 10, "notes");
        }

        public string GenerationProfileId { get; }
        public int PassOrder { get; }
        public string PassId { get; }
        public string ClassName { get; }
        public string RngStreamId { get; }
        public IReadOnlyList<string> InputArtifacts { get; }
        public IReadOnlyList<string> OutputArtifacts { get; }
        public string FailurePolicy { get; }
        public int MaxRetryCount { get; }
        public bool Enabled { get; }
        public string Notes { get; }
        public CsvParsedRecord SourceRecord { get; }
    }

    public sealed class RngStreamDefinition
    {
        internal RngStreamDefinition(CsvParsedRecord sourceRecord)
        {
            SourceRecord = sourceRecord ?? throw new ArgumentNullException(nameof(sourceRecord));
            RngStreamId = WorldRouteDefinitionValueReader.String(sourceRecord, 0, "rng_stream_id");
            SaltHex = WorldRouteDefinitionValueReader.Hex(sourceRecord, 1, "salt_hex");
            ResetScope = WorldRouteDefinitionValueReader.String(sourceRecord, 2, "reset_scope");
            DescriptionKo = WorldRouteDefinitionValueReader.String(sourceRecord, 3, "description_ko");
            Active = WorldRouteDefinitionValueReader.Bool(sourceRecord, 4, "active");
        }

        public string RngStreamId { get; }
        public CsvHexValue SaltHex { get; }
        public string ResetScope { get; }
        public string DescriptionKo { get; }
        public bool Active { get; }
        public CsvParsedRecord SourceRecord { get; }
    }
}
