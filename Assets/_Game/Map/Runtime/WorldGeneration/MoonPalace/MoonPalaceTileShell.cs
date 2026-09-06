using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public enum MoonPalaceCollisionKind
    {
        Solid = 1,
        OneWay = 2,
        PassThrough = 3,
        Hazard = 4,
        Decor = 5,
        Background = 6,
        MissingData = 7,
    }

    public enum MoonPalaceTileRole
    {
        Ground = 1,
        Wall = 2,
        Ceiling = 3,
        OneWayPlatform = 4,
        SlopeOrStep = 5,
        Hazard = 6,
        Decor = 7,
        Background = 8,
        BoundaryBlend = 9,
        DebugMissing = 10,
    }

    public enum MoonPalaceAssetReferenceKind
    {
        MissingData = 1,
        FallbackToken = 2,
    }

    public sealed class MoonPalaceTileShellRecord
    {
        public MoonPalaceTileShellRecord(string tileCode, MoonPalaceTileRole tileRole,
            string layerToken, MoonPalaceCollisionKind collisionKind, string materialToken,
            string footstepAudioToken, string impactAudioToken, string backgroundToken,
            IEnumerable<string> biomeAllowlist,
            MoonPalaceAssetReferenceKind assetReferenceKind, string assetReference,
            string fallbackTileCode, string missingReason, int renderPriority)
        {
            TileCode = MoonPalaceProfileValidation.Require(tileCode, nameof(tileCode));
            if (!Enum.IsDefined(typeof(MoonPalaceTileRole), tileRole))
                throw new ArgumentOutOfRangeException(nameof(tileRole));
            if (!Enum.IsDefined(typeof(MoonPalaceCollisionKind), collisionKind))
                throw new ArgumentOutOfRangeException(nameof(collisionKind));
            if (!Enum.IsDefined(typeof(MoonPalaceAssetReferenceKind), assetReferenceKind))
                throw new ArgumentOutOfRangeException(nameof(assetReferenceKind));
            TileRole = tileRole;
            LayerToken = MoonPalaceProfileValidation.Require(layerToken, nameof(layerToken));
            CollisionKind = collisionKind;
            MaterialToken = MoonPalaceProfileValidation.Require(materialToken,
                nameof(materialToken));
            FootstepAudioToken = MoonPalaceProfileValidation.Require(footstepAudioToken,
                nameof(footstepAudioToken));
            ImpactAudioToken = MoonPalaceProfileValidation.Require(impactAudioToken,
                nameof(impactAudioToken));
            BackgroundToken = MoonPalaceProfileValidation.Require(backgroundToken,
                nameof(backgroundToken));
            var orderedBiomes = (biomeAllowlist ?? throw new ArgumentNullException(
                    nameof(biomeAllowlist)))
                .Select(value => MoonPalaceProfileValidation.Require(value,
                    nameof(biomeAllowlist)))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            if (orderedBiomes.Length == 0)
                throw new ArgumentException("At least one biome is required.",
                    nameof(biomeAllowlist));
            BiomeAllowlist = new ReadOnlyCollection<string>(orderedBiomes);
            AssetReferenceKind = assetReferenceKind;
            AssetReference = MoonPalaceProfileValidation.Clean(assetReference);
            FallbackTileCode = MoonPalaceProfileValidation.Require(fallbackTileCode,
                nameof(fallbackTileCode));
            MissingReason = MoonPalaceProfileValidation.Require(missingReason,
                nameof(missingReason));
            RenderPriority = renderPriority;
            if (assetReferenceKind == MoonPalaceAssetReferenceKind.MissingData &&
                AssetReference.Length != 0)
                throw new ArgumentException("MissingData must not claim an imported asset.",
                    nameof(assetReference));
            if (collisionKind == MoonPalaceCollisionKind.MissingData &&
                tileRole != MoonPalaceTileRole.DebugMissing)
                throw new ArgumentException("MissingData collision is reserved for DebugMissing.");
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MOONPALACE_TILE_SHELL_RECORD_V1", CanonicalLine });
        }

        public string TileCode { get; }
        public MoonPalaceTileRole TileRole { get; }
        public string LayerToken { get; }
        public MoonPalaceCollisionKind CollisionKind { get; }
        public string MaterialToken { get; }
        public string FootstepAudioToken { get; }
        public string ImpactAudioToken { get; }
        public string BackgroundToken { get; }
        public IReadOnlyList<string> BiomeAllowlist { get; }
        public MoonPalaceAssetReferenceKind AssetReferenceKind { get; }
        public string AssetReference { get; }
        public string FallbackTileCode { get; }
        public string MissingReason { get; }
        public int RenderPriority { get; }
        public string CanonicalDigest { get; }

        public string CanonicalLine => MoonPalaceCanonical.Join(TileCode,
            TileRole.ToString(), LayerToken, CollisionKind.ToString(), MaterialToken,
            FootstepAudioToken, ImpactAudioToken, BackgroundToken,
            string.Join(";", BiomeAllowlist), AssetReferenceKind.ToString(),
            AssetReference, FallbackTileCode, MissingReason,
            RenderPriority.ToString(CultureInfo.InvariantCulture));
    }

    public sealed class MoonPalaceTileShell
    {
        public const string SchemaVersion = "map21_01.moonpalace_tile_shell.v1";
        private static readonly MoonPalaceTileRole[] RequiredRoles =
            (MoonPalaceTileRole[])Enum.GetValues(typeof(MoonPalaceTileRole));
        private readonly ReadOnlyCollection<MoonPalaceTileShellRecord> records;

        public MoonPalaceTileShell(IEnumerable<MoonPalaceTileShellRecord> sourceRecords,
            string createdUtc)
        {
            var source = (sourceRecords ?? throw new ArgumentNullException(nameof(sourceRecords)))
                .Where(value => value != null).ToArray();
            if (source.GroupBy(value => value.TileCode, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("Tile codes must be unique.", nameof(sourceRecords));
            var ordered = source.OrderBy(value => value.TileCode,
                StringComparer.Ordinal).ToArray();
            var roles = new HashSet<MoonPalaceTileRole>(ordered.Select(value => value.TileRole));
            if (RequiredRoles.Any(role => !roles.Contains(role)))
                throw new ArgumentException("All minimum MoonPalace tile roles are required.",
                    nameof(sourceRecords));
            records = new ReadOnlyCollection<MoonPalaceTileShellRecord>(ordered);
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                SchemaVersion, MoonPalaceProfilePreconditions.TaskId,
                MoonPalaceProfilePreconditions.SourceMap20PhaseExitDigest,
                "created_utc_excluded=true",
            }.Concat(records.Select(value => value.CanonicalLine)));
        }

        public IReadOnlyList<MoonPalaceTileShellRecord> Records => records;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(
            MoonPalaceTileShellDocument.From(this));
    }

    [Serializable]
    internal sealed class MoonPalaceTileShellRecordDocument
    {
        public string tile_code;
        public string tile_role;
        public string layer_token;
        public string collision_kind;
        public string material_token;
        public string footstep_audio_token;
        public string impact_audio_token;
        public string background_token;
        public string[] biome_allowlist;
        public string asset_reference_kind;
        public string asset_reference;
        public string fallback_tile_code;
        public string missing_reason;
        public int render_priority;
        public string canonical_digest;

        public static MoonPalaceTileShellRecordDocument From(
            MoonPalaceTileShellRecord value) => new MoonPalaceTileShellRecordDocument
        {
            tile_code = value.TileCode,
            tile_role = value.TileRole.ToString(),
            layer_token = value.LayerToken,
            collision_kind = value.CollisionKind.ToString(),
            material_token = value.MaterialToken,
            footstep_audio_token = value.FootstepAudioToken,
            impact_audio_token = value.ImpactAudioToken,
            background_token = value.BackgroundToken,
            biome_allowlist = value.BiomeAllowlist.ToArray(),
            asset_reference_kind = value.AssetReferenceKind.ToString(),
            asset_reference = value.AssetReference,
            fallback_tile_code = value.FallbackTileCode,
            missing_reason = value.MissingReason,
            render_priority = value.RenderPriority,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceTileShellDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_phase_exit_digest;
        public MoonPalaceTileShellRecordDocument[] tile_records;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static MoonPalaceTileShellDocument From(MoonPalaceTileShell value) =>
            new MoonPalaceTileShellDocument
            {
                schema_version = MoonPalaceTileShell.SchemaVersion,
                task_id = MoonPalaceProfilePreconditions.TaskId,
                source_MAP20_phase_exit_digest =
                    MoonPalaceProfilePreconditions.SourceMap20PhaseExitDigest,
                tile_records = value.Records.Select(
                    MoonPalaceTileShellRecordDocument.From).ToArray(),
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CanonicalDigest,
            };
    }
}
