using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceMicroPatternPreconditions
    {
        public const string TaskId = "MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS";
        public const string SourceMap2101ResultDigest =
            "37de80ca52aa90a2ccc121f0b10ac2b75f54a8758381cf592a1413ac9e7d2eaf";
        public const string SourceMap2101TaskDigest =
            "efd5fedf4551f5427af95253ffe792c0b9d6b3b68a0868df167cee66101b9f89";
        public const string SourceMap2102HandoffDigest =
            "f7993186e63a73a76e0e54af05c0851a6ca9e9743b836877dfdb931a8dd8e2d2";
        public const string SourceBiomeProfileDigest =
            "631000d63f0f2de9c829e662dbb9ab05f3ad4ad5ada542307058515569b3a735";
        public const string SourceTileShellDigest =
            "04eefb76556b4874c4a60838149d523e084e0d315fc2d833a9458c9fc6ad2514";
    }

    public enum MoonPalaceProductionPatternRole
    {
        GeometrySilhouette = 1,
        SurfaceAffordance = 2,
        MaterialHazardMarker = 3,
    }

    public enum MoonPalaceProductionTagKind
    {
        Surface = 1,
        Material = 2,
        Affordance = 3,
        Hazard = 4,
        Marker = 5,
    }

    public enum MoonPalaceRuntimeBindingState
    {
        AuthoringOnly = 1,
        NeedsRuntimeBinding = 2,
        MissingData = 3,
    }

    public sealed class MoonPalaceProductionPatternCell
    {
        private static readonly HashSet<string> AllowedOperations = new HashSet<string>(
            new[] { "NO_CHANGE", "ADD_SOLID", "CARVE_AIR" }, StringComparer.Ordinal);

        public MoonPalaceProductionPatternCell(string patternId, int x, int y,
            string sourceOperation, string tileCode, MoonPalaceTileRole tileRole,
            MoonPalaceCollisionKind collisionKind, string materialToken,
            string fallbackTileCode, MoonPalaceAssetReferenceKind assetReferenceKind,
            string missingReason, string protectedMaskPolicy,
            IEnumerable<string> knownTileCodes)
        {
            PatternId = MoonPalaceProfileValidation.Require(patternId, nameof(patternId));
            if (x < 0 || x > 3) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y > 3) throw new ArgumentOutOfRangeException(nameof(y));
            X = x;
            Y = y;
            SourceOperation = MoonPalaceProfileValidation.Require(sourceOperation,
                nameof(sourceOperation));
            if (!AllowedOperations.Contains(SourceOperation))
                throw new ArgumentException("Unsupported starter source operation.",
                    nameof(sourceOperation));
            TileCode = MoonPalaceProfileValidation.Require(tileCode, nameof(tileCode));
            var known = new HashSet<string>(knownTileCodes ?? throw new ArgumentNullException(
                nameof(knownTileCodes)), StringComparer.Ordinal);
            if (!known.Contains(TileCode))
                throw new ArgumentException("Tile code is not present in MAP21_01 shell.",
                    nameof(tileCode));
            if (!Enum.IsDefined(typeof(MoonPalaceTileRole), tileRole))
                throw new ArgumentOutOfRangeException(nameof(tileRole));
            if (!Enum.IsDefined(typeof(MoonPalaceCollisionKind), collisionKind))
                throw new ArgumentOutOfRangeException(nameof(collisionKind));
            if (!Enum.IsDefined(typeof(MoonPalaceAssetReferenceKind), assetReferenceKind))
                throw new ArgumentOutOfRangeException(nameof(assetReferenceKind));
            TileRole = tileRole;
            CollisionKind = collisionKind;
            MaterialToken = MoonPalaceProfileValidation.Require(materialToken,
                nameof(materialToken));
            FallbackTileCode = MoonPalaceProfileValidation.Require(fallbackTileCode,
                nameof(fallbackTileCode));
            AssetReferenceKind = assetReferenceKind;
            MissingReason = MoonPalaceProfileValidation.Require(missingReason,
                nameof(missingReason));
            ProtectedMaskPolicy = MoonPalaceProfileValidation.Require(protectedMaskPolicy,
                nameof(protectedMaskPolicy));
            if (TileRole == MoonPalaceTileRole.OneWayPlatform &&
                CollisionKind != MoonPalaceCollisionKind.OneWay)
                throw new ArgumentException("OneWayPlatform must use OneWay collision.");
            if (AssetReferenceKind != MoonPalaceAssetReferenceKind.MissingData)
                throw new ArgumentException("MAP21_01 unresolved assets must remain MissingData.");
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_PRODUCTION_CELL_V1", CanonicalLine });
        }

        public string PatternId { get; }
        public int X { get; }
        public int Y { get; }
        public string SourceOperation { get; }
        public string TileCode { get; }
        public MoonPalaceTileRole TileRole { get; }
        public MoonPalaceCollisionKind CollisionKind { get; }
        public string MaterialToken { get; }
        public string FallbackTileCode { get; }
        public MoonPalaceAssetReferenceKind AssetReferenceKind { get; }
        public string MissingReason { get; }
        public string ProtectedMaskPolicy { get; }
        public string CanonicalDigest { get; }
        public string CoordinateToken => X.ToString(CultureInfo.InvariantCulture) + "," +
            Y.ToString(CultureInfo.InvariantCulture);
        public string CanonicalLine => MoonPalaceCanonical.Join(PatternId,
            X.ToString(CultureInfo.InvariantCulture), Y.ToString(CultureInfo.InvariantCulture),
            SourceOperation, TileCode, TileRole.ToString(), CollisionKind.ToString(),
            MaterialToken, FallbackTileCode, AssetReferenceKind.ToString(), MissingReason,
            ProtectedMaskPolicy);
    }

    public sealed class MoonPalaceProductionPatternTag
    {
        public MoonPalaceProductionPatternTag(string patternId, int x, int y,
            MoonPalaceProductionTagKind tagKind, string tagToken,
            MoonPalaceRuntimeBindingState runtimeBindingState, string riskLevel,
            int visualPriority)
        {
            PatternId = MoonPalaceProfileValidation.Require(patternId, nameof(patternId));
            if (x < 0 || x > 3) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y > 3) throw new ArgumentOutOfRangeException(nameof(y));
            if (!Enum.IsDefined(typeof(MoonPalaceProductionTagKind), tagKind))
                throw new ArgumentOutOfRangeException(nameof(tagKind));
            if (!Enum.IsDefined(typeof(MoonPalaceRuntimeBindingState), runtimeBindingState))
                throw new ArgumentOutOfRangeException(nameof(runtimeBindingState));
            if (visualPriority < 0) throw new ArgumentOutOfRangeException(nameof(visualPriority));
            X = x;
            Y = y;
            TagKind = tagKind;
            TagToken = MoonPalaceProfileValidation.Require(tagToken, nameof(tagToken));
            RuntimeBindingState = runtimeBindingState;
            RiskLevel = MoonPalaceProfileValidation.Require(riskLevel, nameof(riskLevel));
            VisualPriority = visualPriority;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_PRODUCTION_TAG_V1", CanonicalLine });
        }

        public string PatternId { get; }
        public int X { get; }
        public int Y { get; }
        public MoonPalaceProductionTagKind TagKind { get; }
        public string TagToken { get; }
        public MoonPalaceRuntimeBindingState RuntimeBindingState { get; }
        public string RiskLevel { get; }
        public int VisualPriority { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(PatternId,
            X.ToString(CultureInfo.InvariantCulture), Y.ToString(CultureInfo.InvariantCulture),
            TagKind.ToString(), TagToken, RuntimeBindingState.ToString(), RiskLevel,
            VisualPriority.ToString(CultureInfo.InvariantCulture));
    }

    public sealed class MoonPalaceProductionPatternCatalogRecord
    {
        public const string SchemaVersion = "map21_02.moonpalace_micropattern.v1";
        private readonly ReadOnlyCollection<string> allowedTransformTokens;

        public MoonPalaceProductionPatternCatalogRecord(string patternId,
            string sourceStarterPatternId, string sourceStarterDigest, string biomeId,
            MoonPalaceProductionPatternRole patternRole, string silhouetteFamily,
            IEnumerable<string> sourceAllowedTransformTokens, int selectionWeight,
            string tileShellDigest, string cellRecordDigest, string tagRecordDigest,
            string missingAssetPolicy, MoonPalaceRuntimeBindingState runtimeBindingState)
        {
            PatternId = MoonPalaceProfileValidation.Require(patternId, nameof(patternId));
            SourceStarterPatternId = MoonPalaceProfileValidation.Require(sourceStarterPatternId,
                nameof(sourceStarterPatternId));
            SourceStarterDigest = MoonPalaceProfileValidation.Digest(sourceStarterDigest,
                nameof(sourceStarterDigest));
            BiomeId = MoonPalaceProfileValidation.Require(biomeId, nameof(biomeId));
            if (!Enum.IsDefined(typeof(MoonPalaceProductionPatternRole), patternRole))
                throw new ArgumentOutOfRangeException(nameof(patternRole));
            PatternRole = patternRole;
            SilhouetteFamily = MoonPalaceProfileValidation.Require(silhouetteFamily,
                nameof(silhouetteFamily));
            var transforms = (sourceAllowedTransformTokens ?? throw new ArgumentNullException(
                    nameof(sourceAllowedTransformTokens)))
                .Select(value => MoonPalaceProfileValidation.Require(value,
                    nameof(sourceAllowedTransformTokens)))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            if (transforms.Length == 0)
                throw new ArgumentException("At least one transform token is required.",
                    nameof(sourceAllowedTransformTokens));
            allowedTransformTokens = new ReadOnlyCollection<string>(transforms);
            if (selectionWeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(selectionWeight));
            SelectionWeight = selectionWeight;
            TileShellDigest = MoonPalaceProfileValidation.Digest(tileShellDigest,
                nameof(tileShellDigest));
            if (!string.Equals(TileShellDigest,
                    MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                    StringComparison.Ordinal))
                throw new ArgumentException("MAP21_01 tile shell digest mismatch.",
                    nameof(tileShellDigest));
            CellRecordDigest = MoonPalaceProfileValidation.Digest(cellRecordDigest,
                nameof(cellRecordDigest));
            TagRecordDigest = MoonPalaceProfileValidation.Digest(tagRecordDigest,
                nameof(tagRecordDigest));
            MissingAssetPolicy = MoonPalaceProfileValidation.Require(missingAssetPolicy,
                nameof(missingAssetPolicy));
            if (!Enum.IsDefined(typeof(MoonPalaceRuntimeBindingState), runtimeBindingState))
                throw new ArgumentOutOfRangeException(nameof(runtimeBindingState));
            RuntimeBindingState = runtimeBindingState;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_PRODUCTION_PATTERN_V1", CanonicalLine });
        }

        public string PatternId { get; }
        public string SourceStarterPatternId { get; }
        public string SourceStarterDigest { get; }
        public string BiomeId { get; }
        public MoonPalaceProductionPatternRole PatternRole { get; }
        public string SilhouetteFamily { get; }
        public IReadOnlyList<string> AllowedTransformTokens => allowedTransformTokens;
        public int SelectionWeight { get; }
        public string TileShellDigest { get; }
        public string CellRecordDigest { get; }
        public string TagRecordDigest { get; }
        public string MissingAssetPolicy { get; }
        public MoonPalaceRuntimeBindingState RuntimeBindingState { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCanonical.Join(PatternId, SchemaVersion,
            SourceStarterPatternId, SourceStarterDigest, BiomeId, PatternRole.ToString(),
            SilhouetteFamily, string.Join("|", AllowedTransformTokens),
            SelectionWeight.ToString(CultureInfo.InvariantCulture), TileShellDigest,
            CellRecordDigest, TagRecordDigest, MissingAssetPolicy,
            RuntimeBindingState.ToString(), "created_utc_excluded=true");
    }

    public sealed class MoonPalaceMicroPatternProduction
    {
        public const string SchemaVersion = "map21_02.moonpalace_micropattern_production.v1";
        private static readonly ReadOnlyCollection<string> requiredPatternIds =
            new ReadOnlyCollection<string>(new[]
            {
                "MP_CRATER_BROKEN_SLOPE", "MP_CRATER_BOWL", "MP_CRATER_ROCK_SHELF",
                "MP_CRATER_GRIP_RIDGE", "MP_CRATER_DUST_PATCH", "MP_CRATER_METEOR_CUE",
                "MP_ROOT_ARCH", "MP_ROOT_VERTICAL_TUNNEL", "MP_ROOT_HOLLOW_POCKET",
                "MP_ROOT_CLIMB_VINES", "MP_ROOT_SAP_PATCH", "MP_ROOT_SPROUT_MARK",
                "MP_MILL_BROKEN_PILLAR", "MP_MILL_BEAM_OVERHANG",
                "MP_MILL_ORTHOGONAL_CARVE", "MP_MILL_BEAM_GRIP",
                "MP_MILL_RUST_PATCH", "MP_MILL_GEAR_SOCKET", "MP_DOUGH_BOUNCE_CUP",
                "MP_DOUGH_SOFT_POCKET", "MP_DOUGH_STICKY_SHELF",
                "MP_DOUGH_BOUNCE_STRIP", "MP_DOUGH_FERMENT_PATCH",
                "MP_DOUGH_RECOVERY_PAD",
            }.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        private static readonly ReadOnlyCollection<string> requiredBiomeIds =
            new ReadOnlyCollection<string>(new[]
                { "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough" });
        private readonly ReadOnlyCollection<MoonPalaceProductionPatternCatalogRecord> catalog;
        private readonly ReadOnlyCollection<MoonPalaceProductionPatternCell> cells;
        private readonly ReadOnlyCollection<MoonPalaceProductionPatternTag> tags;
        private readonly ReadOnlyCollection<string> preservedSilhouettePatternIds;

        public MoonPalaceMicroPatternProduction(
            IEnumerable<MoonPalaceProductionPatternCatalogRecord> sourceCatalog,
            IEnumerable<MoonPalaceProductionPatternCell> sourceCells,
            IEnumerable<MoonPalaceProductionPatternTag> sourceTags,
            IEnumerable<string> sourcePreservedSilhouettePatternIds,
            int tileShellRecordsRead, int tileCodeLookupsResolved,
            int missingFallbackCellCount, int unknownTileCodeReferences, string createdUtc)
        {
            var orderedCatalog = RequiredValues(sourceCatalog, nameof(sourceCatalog))
                .OrderBy(value => value.PatternId, StringComparer.Ordinal).ToArray();
            if (orderedCatalog.GroupBy(value => value.PatternId, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("Pattern ids must be unique.", nameof(sourceCatalog));
            if (!orderedCatalog.Select(value => value.PatternId).SequenceEqual(
                    requiredPatternIds, StringComparer.Ordinal))
                throw new ArgumentException("Exact MAP10 starter 24 ids are required.",
                    nameof(sourceCatalog));
            if (!orderedCatalog.Select(value => value.SourceStarterPatternId)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(requiredPatternIds, StringComparer.Ordinal))
                throw new ArgumentException("Exact starter source mappings are required.",
                    nameof(sourceCatalog));
            foreach (var biome in requiredBiomeIds)
                if (orderedCatalog.Count(value => value.BiomeId == biome) != 6)
                    throw new ArgumentException("Each MoonPalace biome requires six patterns.",
                        nameof(sourceCatalog));
            RequireRoleCount(orderedCatalog,
                MoonPalaceProductionPatternRole.GeometrySilhouette, 12);
            RequireRoleCount(orderedCatalog,
                MoonPalaceProductionPatternRole.SurfaceAffordance, 4);
            RequireRoleCount(orderedCatalog,
                MoonPalaceProductionPatternRole.MaterialHazardMarker, 8);

            var orderedCells = RequiredValues(sourceCells, nameof(sourceCells))
                .OrderBy(value => value.PatternId, StringComparer.Ordinal)
                .ThenBy(value => value.Y).ThenBy(value => value.X).ToArray();
            var knownPatterns = new HashSet<string>(requiredPatternIds, StringComparer.Ordinal);
            if (orderedCells.Any(value => !knownPatterns.Contains(value.PatternId)))
                throw new ArgumentException("Cell references an unknown pattern.",
                    nameof(sourceCells));
            foreach (var patternId in requiredPatternIds)
            {
                var group = orderedCells.Where(value => value.PatternId == patternId).ToArray();
                if (group.Length != 16 || group.Select(value => value.CoordinateToken)
                        .Distinct(StringComparer.Ordinal).Count() != 16)
                    throw new ArgumentException("Every pattern requires exact 4x4 coverage.",
                        nameof(sourceCells));
                var record = orderedCatalog.Single(value => value.PatternId == patternId);
                if (!string.Equals(record.CellRecordDigest, ComputeCellRecordDigest(group),
                        StringComparison.Ordinal))
                    throw new ArgumentException("Pattern cell digest mismatch.",
                        nameof(sourceCells));
            }

            var orderedTags = RequiredValues(sourceTags, nameof(sourceTags))
                .OrderBy(value => value.PatternId, StringComparer.Ordinal)
                .ThenBy(value => value.Y).ThenBy(value => value.X)
                .ThenBy(value => value.TagKind).ThenBy(value => value.TagToken,
                    StringComparer.Ordinal).ToArray();
            if (orderedTags.Any(value => !knownPatterns.Contains(value.PatternId)))
                throw new ArgumentException("Tag references an unknown pattern.",
                    nameof(sourceTags));
            foreach (var patternId in requiredPatternIds)
            {
                var group = orderedTags.Where(value => value.PatternId == patternId).ToArray();
                var record = orderedCatalog.Single(value => value.PatternId == patternId);
                if (!string.Equals(record.TagRecordDigest, ComputeTagRecordDigest(group),
                        StringComparison.Ordinal))
                    throw new ArgumentException("Pattern tag digest mismatch.",
                        nameof(sourceTags));
            }
            var requiredTagKinds = Enum.GetValues(typeof(MoonPalaceProductionTagKind))
                .Cast<MoonPalaceProductionTagKind>();
            if (requiredTagKinds.Any(kind => orderedTags.All(value => value.TagKind != kind)))
                throw new ArgumentException("All semantic tag kinds are required.",
                    nameof(sourceTags));

            var preserved = (sourcePreservedSilhouettePatternIds ??
                    throw new ArgumentNullException(nameof(sourcePreservedSilhouettePatternIds)))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            if (!preserved.SequenceEqual(requiredPatternIds, StringComparer.Ordinal))
                throw new ArgumentException("All starter silhouettes must be preserved.",
                    nameof(sourcePreservedSilhouettePatternIds));
            if (tileShellRecordsRead <= 0 || tileCodeLookupsResolved != orderedCells.Length ||
                missingFallbackCellCount != orderedCells.Length || unknownTileCodeReferences != 0)
                throw new ArgumentException("Tile shell mapping counters are inconsistent.");

            catalog = new ReadOnlyCollection<MoonPalaceProductionPatternCatalogRecord>(
                orderedCatalog);
            cells = new ReadOnlyCollection<MoonPalaceProductionPatternCell>(orderedCells);
            tags = new ReadOnlyCollection<MoonPalaceProductionPatternTag>(orderedTags);
            preservedSilhouettePatternIds = new ReadOnlyCollection<string>(preserved);
            TileShellRecordsRead = tileShellRecordsRead;
            TileCodeLookupsResolved = tileCodeLookupsResolved;
            MissingFallbackCellCount = missingFallbackCellCount;
            UnknownTileCodeReferences = unknownTileCodeReferences;
            CreatedUtc = createdUtc ?? string.Empty;
            PatternCatalogDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_PATTERN_CATALOG_SET_V1" }.Concat(
                catalog.Select(value => value.CanonicalLine)));
            PatternCellDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_PATTERN_CELL_SET_V1" }.Concat(
                cells.Select(value => value.CanonicalLine)));
            PatternTagDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_PATTERN_TAG_SET_V1" }.Concat(
                tags.Select(value => value.CanonicalLine)));
            CatalogManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MOONPALACE_PATTERN_CATALOG_MANIFEST_V1", SchemaVersion,
                MoonPalaceMicroPatternPreconditions.SourceMap2102HandoffDigest,
                MoonPalaceMicroPatternPreconditions.SourceBiomeProfileDigest,
                MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                PatternCatalogDigest, PatternTagDigest, "created_utc_excluded=true",
            });
            CellManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MOONPALACE_PATTERN_CELL_MANIFEST_V1", SchemaVersion,
                MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                PatternCellDigest, "created_utc_excluded=true",
            });
        }

        public static IReadOnlyList<string> RequiredPatternIds => requiredPatternIds;
        public static IReadOnlyList<string> RequiredBiomeIds => requiredBiomeIds;
        public IReadOnlyList<MoonPalaceProductionPatternCatalogRecord> Catalog => catalog;
        public IReadOnlyList<MoonPalaceProductionPatternCell> Cells => cells;
        public IReadOnlyList<MoonPalaceProductionPatternTag> Tags => tags;
        public IReadOnlyList<string> PreservedSilhouettePatternIds =>
            preservedSilhouettePatternIds;
        public int TileShellRecordsRead { get; }
        public int TileCodeLookupsResolved { get; }
        public int MissingFallbackCellCount { get; }
        public int UnknownTileCodeReferences { get; }
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string PatternCatalogDigest { get; }
        public string PatternCellDigest { get; }
        public string PatternTagDigest { get; }
        public string CatalogManifestDigest { get; }
        public string CellManifestDigest { get; }

        public string SerializeCatalogManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceMicroPatternCatalogManifestDocument.From(this));
        public string SerializeCellManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceMicroPatternCellManifestDocument.From(this));

        public string SerializeCatalogCsv()
        {
            var lines = new List<string>
            {
                "pattern_id,schema_version,source_starter_pattern_id,source_starter_digest,biome_id,pattern_role,silhouette_family,allowed_transform_tokens,selection_weight,tile_shell_digest,cell_record_digest,tag_record_digest,missing_asset_policy,runtime_binding_state,created_utc_excluded_from_canonical_digest,canonical_digest",
            };
            lines.AddRange(Catalog.Select(value => MoonPalaceProductionCsv.Join(
                value.PatternId, MoonPalaceProductionPatternCatalogRecord.SchemaVersion,
                value.SourceStarterPatternId, value.SourceStarterDigest, value.BiomeId,
                value.PatternRole.ToString(), value.SilhouetteFamily,
                string.Join("|", value.AllowedTransformTokens),
                value.SelectionWeight.ToString(CultureInfo.InvariantCulture),
                value.TileShellDigest, value.CellRecordDigest, value.TagRecordDigest,
                value.MissingAssetPolicy, value.RuntimeBindingState.ToString(), "true",
                value.CanonicalDigest)));
            return string.Join("\n", lines) + "\n";
        }

        public string SerializeCellCsv()
        {
            var lines = new List<string>
            {
                "pattern_id,x,y,source_operation,tile_code,tile_role,collision_kind,material_token,fallback_tile_code,asset_reference_kind,missing_reason,protected_mask_policy,canonical_digest",
            };
            lines.AddRange(Cells.Select(value => MoonPalaceProductionCsv.Join(value.PatternId,
                value.X.ToString(CultureInfo.InvariantCulture),
                value.Y.ToString(CultureInfo.InvariantCulture), value.SourceOperation,
                value.TileCode, value.TileRole.ToString(), value.CollisionKind.ToString(),
                value.MaterialToken, value.FallbackTileCode,
                value.AssetReferenceKind.ToString(), value.MissingReason,
                value.ProtectedMaskPolicy, value.CanonicalDigest)));
            return string.Join("\n", lines) + "\n";
        }

        public string SerializeTagCsv()
        {
            var lines = new List<string>
            {
                "pattern_id,x,y,tag_kind,tag_token,runtime_binding_state,risk_level,visual_priority,canonical_digest",
            };
            lines.AddRange(Tags.Select(value => MoonPalaceProductionCsv.Join(value.PatternId,
                value.X.ToString(CultureInfo.InvariantCulture),
                value.Y.ToString(CultureInfo.InvariantCulture), value.TagKind.ToString(),
                value.TagToken, value.RuntimeBindingState.ToString(), value.RiskLevel,
                value.VisualPriority.ToString(CultureInfo.InvariantCulture),
                value.CanonicalDigest)));
            return string.Join("\n", lines) + "\n";
        }

        public static string ComputeCellRecordDigest(
            IEnumerable<MoonPalaceProductionPatternCell> values) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_PATTERN_CELL_RECORDS_V1" }.Concat(
                (values ?? Array.Empty<MoonPalaceProductionPatternCell>())
                    .OrderBy(value => value.Y).ThenBy(value => value.X)
                    .Select(value => value.CanonicalLine)));

        public static string ComputeTagRecordDigest(
            IEnumerable<MoonPalaceProductionPatternTag> values) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_PATTERN_TAG_RECORDS_V1" }.Concat(
                (values ?? Array.Empty<MoonPalaceProductionPatternTag>())
                    .OrderBy(value => value.Y).ThenBy(value => value.X)
                    .ThenBy(value => value.TagKind).ThenBy(value => value.TagToken,
                        StringComparer.Ordinal).Select(value => value.CanonicalLine)));

        private static T[] RequiredValues<T>(IEnumerable<T> values, string parameterName)
            where T : class
        {
            var result = (values ?? throw new ArgumentNullException(parameterName)).ToArray();
            if (result.Any(value => value == null))
                throw new ArgumentException("Null records are not allowed.", parameterName);
            return result;
        }

        private static void RequireRoleCount(
            IEnumerable<MoonPalaceProductionPatternCatalogRecord> values,
            MoonPalaceProductionPatternRole role, int required)
        {
            if (values.Count(value => value.PatternRole == role) != required)
                throw new ArgumentException("Pattern role count mismatch: " + role);
        }
    }

    public sealed class MoonPalaceMicroPatternDigestManifest
    {
        public const string SchemaVersion = "map21_02.micropattern_digest_manifest.v1";

        public MoonPalaceMicroPatternDigestManifest(MoonPalaceMicroPatternProduction value,
            string map2103HandoffDigest, string createdUtc)
        {
            Production = value ?? throw new ArgumentNullException(nameof(value));
            Map2103HandoffDigest = MoonPalaceProfileValidation.Digest(map2103HandoffDigest,
                nameof(map2103HandoffDigest));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                SchemaVersion, MoonPalaceMicroPatternPreconditions.TaskId,
                MoonPalaceMicroPatternPreconditions.SourceMap2101ResultDigest,
                MoonPalaceMicroPatternPreconditions.SourceMap2101TaskDigest,
                MoonPalaceMicroPatternPreconditions.SourceMap2102HandoffDigest,
                MoonPalaceMicroPatternPreconditions.SourceBiomeProfileDigest,
                MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                Production.PatternCatalogDigest, Production.PatternCellDigest,
                Production.PatternTagDigest, Map2103HandoffDigest,
                "created_utc_excluded=true",
            });
        }

        public MoonPalaceMicroPatternProduction Production { get; }
        public string Map2103HandoffDigest { get; }
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(
            MoonPalaceMicroPatternDigestManifestDocument.From(this));
    }

    internal static class MoonPalaceProductionCsv
    {
        public static string Join(params string[] values) => string.Join(",",
            (values ?? Array.Empty<string>()).Select(Escape));

        private static string Escape(string value)
        {
            var text = value ?? string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? text
                : "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }

    [Serializable]
    internal sealed class MoonPalaceMicroPatternCatalogRecordDocument
    {
        public string pattern_id;
        public string schema_version;
        public string source_starter_pattern_id;
        public string source_starter_digest;
        public string biome_id;
        public string pattern_role;
        public string silhouette_family;
        public string[] allowed_transform_tokens;
        public int selection_weight;
        public string tile_shell_digest;
        public string cell_record_digest;
        public string tag_record_digest;
        public string missing_asset_policy;
        public string runtime_binding_state;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceMicroPatternCatalogRecordDocument From(
            MoonPalaceProductionPatternCatalogRecord value) =>
            new MoonPalaceMicroPatternCatalogRecordDocument
            {
                pattern_id = value.PatternId,
                schema_version = MoonPalaceProductionPatternCatalogRecord.SchemaVersion,
                source_starter_pattern_id = value.SourceStarterPatternId,
                source_starter_digest = value.SourceStarterDigest,
                biome_id = value.BiomeId,
                pattern_role = value.PatternRole.ToString(),
                silhouette_family = value.SilhouetteFamily,
                allowed_transform_tokens = value.AllowedTransformTokens.ToArray(),
                selection_weight = value.SelectionWeight,
                tile_shell_digest = value.TileShellDigest,
                cell_record_digest = value.CellRecordDigest,
                tag_record_digest = value.TagRecordDigest,
                missing_asset_policy = value.MissingAssetPolicy,
                runtime_binding_state = value.RuntimeBindingState.ToString(),
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceMicroPatternCellDocument
    {
        public string pattern_id;
        public int x;
        public int y;
        public string source_operation;
        public string tile_code;
        public string tile_role;
        public string collision_kind;
        public string material_token;
        public string fallback_tile_code;
        public string asset_reference_kind;
        public string missing_reason;
        public string protected_mask_policy;
        public string canonical_digest;

        public static MoonPalaceMicroPatternCellDocument From(
            MoonPalaceProductionPatternCell value) => new MoonPalaceMicroPatternCellDocument
        {
            pattern_id = value.PatternId,
            x = value.X,
            y = value.Y,
            source_operation = value.SourceOperation,
            tile_code = value.TileCode,
            tile_role = value.TileRole.ToString(),
            collision_kind = value.CollisionKind.ToString(),
            material_token = value.MaterialToken,
            fallback_tile_code = value.FallbackTileCode,
            asset_reference_kind = value.AssetReferenceKind.ToString(),
            missing_reason = value.MissingReason,
            protected_mask_policy = value.ProtectedMaskPolicy,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceMicroPatternTagDocument
    {
        public string pattern_id;
        public int x;
        public int y;
        public string tag_kind;
        public string tag_token;
        public string runtime_binding_state;
        public string risk_level;
        public int visual_priority;
        public string canonical_digest;

        public static MoonPalaceMicroPatternTagDocument From(
            MoonPalaceProductionPatternTag value) => new MoonPalaceMicroPatternTagDocument
        {
            pattern_id = value.PatternId,
            x = value.X,
            y = value.Y,
            tag_kind = value.TagKind.ToString(),
            tag_token = value.TagToken,
            runtime_binding_state = value.RuntimeBindingState.ToString(),
            risk_level = value.RiskLevel,
            visual_priority = value.VisualPriority,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceMicroPatternCatalogManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP21_02_handoff_digest;
        public string source_biome_profile_digest;
        public string source_tile_shell_digest;
        public MoonPalaceMicroPatternCatalogRecordDocument[] pattern_catalog;
        public MoonPalaceMicroPatternTagDocument[] tag_records;
        public string pattern_catalog_digest;
        public string pattern_tag_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceMicroPatternCatalogManifestDocument From(
            MoonPalaceMicroPatternProduction value) =>
            new MoonPalaceMicroPatternCatalogManifestDocument
            {
                schema_version = MoonPalaceMicroPatternProduction.SchemaVersion,
                task_id = MoonPalaceMicroPatternPreconditions.TaskId,
                source_MAP21_02_handoff_digest =
                    MoonPalaceMicroPatternPreconditions.SourceMap2102HandoffDigest,
                source_biome_profile_digest =
                    MoonPalaceMicroPatternPreconditions.SourceBiomeProfileDigest,
                source_tile_shell_digest =
                    MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                pattern_catalog = value.Catalog.Select(
                    MoonPalaceMicroPatternCatalogRecordDocument.From).ToArray(),
                tag_records = value.Tags.Select(
                    MoonPalaceMicroPatternTagDocument.From).ToArray(),
                pattern_catalog_digest = value.PatternCatalogDigest,
                pattern_tag_digest = value.PatternTagDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CatalogManifestDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceMicroPatternCellManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_tile_shell_digest;
        public MoonPalaceMicroPatternCellDocument[] cell_records;
        public string pattern_cell_digest;
        public int tile_shell_records_read;
        public int tile_code_lookups_resolved;
        public int tile_code_missing_data_fallback_records;
        public int unknown_tile_code_references;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceMicroPatternCellManifestDocument From(
            MoonPalaceMicroPatternProduction value) =>
            new MoonPalaceMicroPatternCellManifestDocument
            {
                schema_version = MoonPalaceMicroPatternProduction.SchemaVersion,
                task_id = MoonPalaceMicroPatternPreconditions.TaskId,
                source_tile_shell_digest =
                    MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                cell_records = value.Cells.Select(
                    MoonPalaceMicroPatternCellDocument.From).ToArray(),
                pattern_cell_digest = value.PatternCellDigest,
                tile_shell_records_read = value.TileShellRecordsRead,
                tile_code_lookups_resolved = value.TileCodeLookupsResolved,
                tile_code_missing_data_fallback_records = value.MissingFallbackCellCount,
                unknown_tile_code_references = value.UnknownTileCodeReferences,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CellManifestDigest,
            };
    }

    [Serializable]
    internal sealed class MoonPalaceMicroPatternDigestManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP21_01_result_digest;
        public string source_MAP21_01_task_digest;
        public string source_MAP21_02_handoff_digest;
        public string source_biome_profile_digest;
        public string source_tile_shell_digest;
        public string pattern_catalog_digest;
        public string pattern_cell_digest;
        public string pattern_tag_digest;
        public string MAP21_03_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceMicroPatternDigestManifestDocument From(
            MoonPalaceMicroPatternDigestManifest value) =>
            new MoonPalaceMicroPatternDigestManifestDocument
            {
                schema_version = MoonPalaceMicroPatternDigestManifest.SchemaVersion,
                task_id = MoonPalaceMicroPatternPreconditions.TaskId,
                source_MAP21_01_result_digest =
                    MoonPalaceMicroPatternPreconditions.SourceMap2101ResultDigest,
                source_MAP21_01_task_digest =
                    MoonPalaceMicroPatternPreconditions.SourceMap2101TaskDigest,
                source_MAP21_02_handoff_digest =
                    MoonPalaceMicroPatternPreconditions.SourceMap2102HandoffDigest,
                source_biome_profile_digest =
                    MoonPalaceMicroPatternPreconditions.SourceBiomeProfileDigest,
                source_tile_shell_digest =
                    MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                pattern_catalog_digest = value.Production.PatternCatalogDigest,
                pattern_cell_digest = value.Production.PatternCellDigest,
                pattern_tag_digest = value.Production.PatternTagDigest,
                MAP21_03_handoff_digest = value.Map2103HandoffDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CanonicalDigest,
            };
    }
}
