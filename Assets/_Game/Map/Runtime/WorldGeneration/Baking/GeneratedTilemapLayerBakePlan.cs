using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedTilemapLayerId
    {
        Terrain = 1,
        Affordance = 2,
        Material = 3,
        Hazard = 4,
        Marker = 5,
        Protection = 6,
        SourceOwner = 7,
    }

    public sealed class GeneratedTilemapCellBakeRecord :
        IComparable<GeneratedTilemapCellBakeRecord>
    {
        public GeneratedTilemapCellBakeRecord(
            GeneratedTilemapLayerId layerId,
            int sectorLocalX,
            int sectorLocalY,
            int sectorLocalIndex,
            string placementId,
            GeneratedTerrainTileCode tileCode,
            string resolvedAssetKey,
            FinalCanvasCellKind cellKind,
            FinalCanvasSourceOwner sourceOwner,
            FinalCanvasProtectionKind protection,
            bool isProtected,
            string provenanceId,
            string claimId,
            string sourceCellToken,
            string sourceLayerStableToken)
        {
            LayerId = layerId;
            SectorLocalX = sectorLocalX;
            SectorLocalY = sectorLocalY;
            SectorLocalIndex = sectorLocalIndex;
            PlacementId = placementId ?? string.Empty;
            TileCode = tileCode;
            ResolvedAssetKey = resolvedAssetKey ?? string.Empty;
            CellKind = cellKind;
            SourceOwner = sourceOwner;
            Protection = protection;
            IsProtected = isProtected;
            ProvenanceId = provenanceId ?? string.Empty;
            ClaimId = claimId ?? string.Empty;
            SourceCellToken = sourceCellToken ?? string.Empty;
            SourceLayerStableToken = sourceLayerStableToken ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "LAYER_CELL", Number((int)LayerId), LayerId.ToString().ToUpperInvariant(),
                Number(SectorLocalIndex), Number(SectorLocalX), Number(SectorLocalY), PlacementId,
                TileCode == null ? "MISSING" : TileCode.Value, ResolvedAssetKey,
                CellKind.ToString().ToUpperInvariant(), SourceOwner.ToString().ToUpperInvariant(),
                Protection.ToString().ToUpperInvariant(), IsProtected ? "1" : "0",
                ProvenanceId, ClaimId, SourceCellToken, SourceLayerStableToken,
            });
        }

        public GeneratedTilemapLayerId LayerId { get; }
        public int SectorLocalX { get; }
        public int SectorLocalY { get; }
        public int SectorLocalIndex { get; }
        public string PlacementId { get; }
        public GeneratedTerrainTileCode TileCode { get; }
        public string ResolvedAssetKey { get; }
        public FinalCanvasCellKind CellKind { get; }
        public FinalCanvasSourceOwner SourceOwner { get; }
        public FinalCanvasProtectionKind Protection { get; }
        public bool IsProtected { get; }
        public string ProvenanceId { get; }
        public string ClaimId { get; }
        public string SourceCellToken { get; }
        public string SourceLayerStableToken { get; }
        public bool IsOccupied => CellKind != FinalCanvasCellKind.None &&
            CellKind != FinalCanvasCellKind.Air && CellKind != FinalCanvasCellKind.Unknown;
        public string StableToken { get; }

        public bool IsCoordinateValid(GeneratedTerrainGeometrySnapshot geometry) => geometry != null &&
            SectorLocalX >= 0 && SectorLocalX < geometry.SectorWidth &&
            SectorLocalY >= 0 && SectorLocalY < geometry.SectorHeight &&
            SectorLocalIndex == SectorLocalY * geometry.SectorWidth + SectorLocalX;

        public int CompareTo(GeneratedTilemapCellBakeRecord other)
        {
            if (other == null) return -1;
            var comparison = LayerId.CompareTo(other.LayerId);
            return comparison != 0 ? comparison : SectorLocalIndex.CompareTo(other.SectorLocalIndex);
        }

        public static GeneratedTilemapCellBakeRecord FromPlacement(
            GeneratedCellPlacementRecord placement,
            GeneratedCellPlacementLayer layer)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            return new GeneratedTilemapCellBakeRecord(
                (GeneratedTilemapLayerId)(int)layer.Layer,
                placement.Coordinate.SectorLocalX,
                placement.Coordinate.SectorLocalY,
                placement.Coordinate.SectorRowMajorIndex,
                placement.Id.Value,
                layer.TileCode,
                layer.ResolvedAssetKey,
                layer.CellKind,
                layer.SourceOwner,
                layer.Protection,
                layer.IsProtected,
                layer.ProvenanceId,
                layer.ClaimId,
                layer.SourceCellToken,
                layer.SourceLayerStableToken);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTilemapLayerBuffer
    {
        private readonly ReadOnlyCollection<GeneratedTilemapCellBakeRecord> records;

        internal GeneratedTilemapLayerBuffer(
            GeneratedTilemapLayerId layerId,
            IEnumerable<GeneratedTilemapCellBakeRecord> sourceRecords)
        {
            LayerId = layerId;
            records = new ReadOnlyCollection<GeneratedTilemapCellBakeRecord>((sourceRecords ??
                Array.Empty<GeneratedTilemapCellBakeRecord>()).OrderBy(value => value).ToArray());
        }

        public GeneratedTilemapLayerId LayerId { get; }
        public IReadOnlyList<GeneratedTilemapCellBakeRecord> Records => records;
        public int RecordCount => records.Count;
        public int UniqueCellCount => records.Select(value => value.SectorLocalIndex).Distinct().Count();
        public int OccupiedCellCount => records.Count(value => value.IsOccupied);
        public string StableToken => "LAYER_BUFFER|" + Number((int)LayerId) + "|" +
            Number(RecordCount) + "|" + Number(UniqueCellCount);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTilemapBakeCommand : IComparable<GeneratedTilemapBakeCommand>
    {
        internal GeneratedTilemapBakeCommand(GeneratedTilemapCellBakeRecord record)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            StableToken = "BAKE_COMMAND|" + record.StableToken;
        }

        public GeneratedTilemapCellBakeRecord Record { get; }
        public GeneratedTilemapLayerId LayerId => Record.LayerId;
        public int SectorLocalIndex => Record.SectorLocalIndex;
        public int SectorLocalX => Record.SectorLocalX;
        public int SectorLocalY => Record.SectorLocalY;
        public GeneratedTerrainTileCode TileCode => Record.TileCode;
        public string ResolvedAssetKey => Record.ResolvedAssetKey;
        public bool IsOccupied => Record.IsOccupied;
        public string StableToken { get; }
        public int CompareTo(GeneratedTilemapBakeCommand other) => other == null
            ? -1 : Record.CompareTo(other.Record);
    }

    public sealed class GeneratedTilemapBakeRequest
    {
        private readonly ReadOnlyCollection<GeneratedTilemapCellBakeRecord> records;

        public GeneratedTilemapBakeRequest(
            GeneratedCellPlacementPlan placementPlan,
            string expectedPlacementDigest = null,
            GeneratedTerrainAssetRegistrySnapshot assetRegistry = null,
            IEnumerable<GeneratedTilemapCellBakeRecord> sourceRecords = null)
        {
            PlacementPlan = placementPlan;
            ExpectedPlacementDigest = expectedPlacementDigest ??
                (placementPlan == null ? string.Empty : placementPlan.OutputDigest);
            AssetRegistry = assetRegistry ?? (placementPlan == null || placementPlan.AssetResolution == null
                ? null : placementPlan.AssetResolution.Registry);
            var raw = (sourceRecords ?? Project(placementPlan)).ToArray();
            NullRecordCount = raw.Count(value => value == null);
            records = new ReadOnlyCollection<GeneratedTilemapCellBakeRecord>(raw
                .Where(value => value != null).OrderBy(value => value).ToArray());
            CanonicalDigest = GeneratedTilemapBakeDigest.ComputeInput(this);
        }

        public GeneratedCellPlacementPlan PlacementPlan { get; }
        public string ExpectedPlacementDigest { get; }
        public GeneratedTerrainAssetRegistrySnapshot AssetRegistry { get; }
        public IReadOnlyList<GeneratedTilemapCellBakeRecord> SourceRecords => records;
        public int NullRecordCount { get; }
        public string CanonicalDigest { get; }

        private static IEnumerable<GeneratedTilemapCellBakeRecord> Project(
            GeneratedCellPlacementPlan plan) => plan == null
                ? Array.Empty<GeneratedTilemapCellBakeRecord>()
                : plan.Records.SelectMany(placement => placement.Layers.Select(layer =>
                    GeneratedTilemapCellBakeRecord.FromPlacement(placement, layer)));
    }

    public enum GeneratedTilemapBakeFailureCode
    {
        MissingRequest = 1,
        MissingPlacementPlan = 2,
        StalePlacementInput = 3,
        InvalidLayerId = 4,
        OutOfBoundsLayerCell = 5,
        DuplicateLayerCell = 6,
        MissingLayerCell = 7,
        Overlap = 8,
        Gap = 9,
        PlacementCellMissing = 10,
        MissingTileCode = 11,
        MissingPrefabId = 12,
        InvalidProvenance = 13,
        ForbiddenSeamExposure = 14,
        MissingSeamNeighbor = 15,
        OutOfBoundsSeamNeighbor = 16,
        InvalidDigest = 17,
    }

    public sealed class GeneratedTilemapBakeFailure :
        IEquatable<GeneratedTilemapBakeFailure>, IComparable<GeneratedTilemapBakeFailure>
    {
        public GeneratedTilemapBakeFailure(
            GeneratedTilemapBakeFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedTilemapBakeFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;
        public int CompareTo(GeneratedTilemapBakeFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedTilemapBakeFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedTilemapBakeFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedTilemapBakePlan
    {
        private readonly ReadOnlyCollection<GeneratedTilemapLayerBuffer> layerBuffers;
        private readonly ReadOnlyCollection<GeneratedTilemapBakeCommand> commands;
        private readonly ReadOnlyCollection<GeneratedCellPlacementSocketReference> socketReferences;
        private readonly ReadOnlyCollection<GeneratedCellPlacementSlotReference> slotReferences;

        internal GeneratedTilemapBakePlan(
            GeneratedTilemapBakeRequest request,
            IEnumerable<GeneratedTilemapLayerBuffer> sourceBuffers,
            IEnumerable<GeneratedTilemapBakeCommand> sourceCommands,
            IEnumerable<GeneratedCellPlacementSocketReference> sourceSockets,
            IEnumerable<GeneratedCellPlacementSlotReference> sourceSlots,
            GeneratedTilemapSeamReport seamReport)
        {
            Request = request;
            layerBuffers = new ReadOnlyCollection<GeneratedTilemapLayerBuffer>((sourceBuffers ??
                Array.Empty<GeneratedTilemapLayerBuffer>()).OrderBy(value => value.LayerId).ToArray());
            commands = new ReadOnlyCollection<GeneratedTilemapBakeCommand>((sourceCommands ??
                Array.Empty<GeneratedTilemapBakeCommand>()).OrderBy(value => value).ToArray());
            socketReferences = new ReadOnlyCollection<GeneratedCellPlacementSocketReference>((sourceSockets ??
                Array.Empty<GeneratedCellPlacementSocketReference>()).OrderBy(value => value).ToArray());
            slotReferences = new ReadOnlyCollection<GeneratedCellPlacementSlotReference>((sourceSlots ??
                Array.Empty<GeneratedCellPlacementSlotReference>()).OrderBy(value => value).ToArray());
            SeamReport = seamReport;
            OutputDigest = GeneratedTilemapBakeDigest.ComputeOutput(this);
        }

        public const string PolicyVersion = "MAP17_02_LOGICAL_TILEMAP_BAKE_V1";
        public const string DownstreamOwner = "MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES";
        public const bool OpensDownstreamTask = false;

        public GeneratedTilemapBakeRequest Request { get; }
        public IReadOnlyList<GeneratedTilemapLayerBuffer> LayerBuffers => layerBuffers;
        public IReadOnlyList<GeneratedTilemapBakeCommand> Commands => commands;
        public IReadOnlyList<GeneratedCellPlacementSocketReference> SocketReferences => socketReferences;
        public IReadOnlyList<GeneratedCellPlacementSlotReference> SlotReferences => slotReferences;
        public GeneratedTilemapSeamReport SeamReport { get; }
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int LayerCount => layerBuffers.Count;
        public int TotalLayerRecordCount => layerBuffers.Sum(value => value.RecordCount);
        public int UniqueLayerCellKeyCount => layerBuffers.Sum(value => value.UniqueCellCount);
        public int SectorCellCoverageCount => layerBuffers.SelectMany(value => value.Records)
            .Select(value => value.SectorLocalIndex).Distinct().Count();
        public int MissingLayerCellCount => Math.Max(0,
            GeneratedTerrainGeometrySnapshot.CanonicalSectorLayerRecordCount - UniqueLayerCellKeyCount);
        public int DuplicateLayerCellCount => TotalLayerRecordCount - UniqueLayerCellKeyCount;
        public int OutOfBoundsLayerCellCount => layerBuffers.SelectMany(value => value.Records)
            .Count(value => !value.IsCoordinateValid(Request.PlacementPlan.Request.Geometry));
        public int CommandCount => commands.Count;
        public int SocketReferenceCount => socketReferences.Count;
        public int SlotReferenceCount => slotReferences.Count;
        public int TilemapComponentWriteCount => 0;
        public int TilemapSetTileCallCount => 0;
        public int TilemapSetTilesCallCount => 0;
        public int TilemapSetTilesBlockCallCount => 0;
        public int TilemapClearAllTilesCallCount => 0;
        public int TilemapCompressBoundsCallCount => 0;
        public int SceneTilemapBakeCount => 0;
        public int ColliderRebuildCount => 0;
        public int GameObjectInstantiationCount => 0;
        public int PrefabInstantiationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int StableSpawnIdCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int ProductionSeedApprovalCount => 0;
    }

    public sealed class GeneratedTilemapBakeResult
    {
        private readonly ReadOnlyCollection<GeneratedTilemapBakeFailure> failures;

        internal GeneratedTilemapBakeResult(
            GeneratedTilemapBakeRequest request,
            GeneratedTilemapBakePlan plan,
            IEnumerable<GeneratedTilemapBakeFailure> sourceFailures)
        {
            Request = request;
            Plan = plan;
            failures = new ReadOnlyCollection<GeneratedTilemapBakeFailure>((sourceFailures ??
                Array.Empty<GeneratedTilemapBakeFailure>()).Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public GeneratedTilemapBakeRequest Request { get; }
        public GeneratedTilemapBakePlan Plan { get; }
        public IReadOnlyList<GeneratedTilemapBakeFailure> Failures => failures;
        public string InputDigest => Plan == null
            ? (Request == null ? string.Empty : Request.CanonicalDigest) : Plan.InputDigest;
        public string OutputDigest => Plan == null ? string.Empty : Plan.OutputDigest;
    }

    public static class GeneratedTilemapBakeDigest
    {
        public static string ComputeInput(GeneratedTilemapBakeRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedTilemapBakePlan.PolicyVersion,
                "PLACEMENT|" + (request.PlacementPlan == null ? string.Empty :
                    request.PlacementPlan.OutputDigest) + "|" + request.ExpectedPlacementDigest,
                "REGISTRY|" + (request.AssetRegistry == null ? string.Empty : request.AssetRegistry.Digest),
                "NULLS|" + Number(request.NullRecordCount),
            };
            lines.AddRange(request.SourceRecords.OrderBy(value => value)
                .Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputeOutput(GeneratedTilemapBakePlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedTilemapBakePlan.PolicyVersion,
                "INPUT|" + plan.InputDigest,
                "COUNTS|" + Number(plan.LayerCount) + "|" + Number(plan.TotalLayerRecordCount) + "|" +
                    Number(plan.UniqueLayerCellKeyCount) + "|" + Number(plan.SectorCellCoverageCount) + "|" +
                    Number(plan.CommandCount) + "|" + Number(plan.SocketReferenceCount) + "|" +
                    Number(plan.SlotReferenceCount),
                "VALIDATION|" + Number(plan.MissingLayerCellCount) + "|" +
                    Number(plan.DuplicateLayerCellCount) + "|" + Number(plan.OutOfBoundsLayerCellCount),
                "SEAMS|" + (plan.SeamReport == null ? string.Empty : plan.SeamReport.OutputDigest),
                "DOWNSTREAM|" + GeneratedTilemapBakePlan.DownstreamOwner + "|0",
                "MUTATIONS|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0|0",
            };
            lines.AddRange(plan.LayerBuffers.OrderBy(value => value.LayerId)
                .Select(value => value.StableToken));
            lines.AddRange(plan.Commands.OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange(plan.SocketReferences.OrderBy(value => value)
                .Select(value => "SOCKET|" + value.StableToken));
            lines.AddRange(plan.SlotReferences.OrderBy(value => value)
                .Select(value => "SLOT|" + value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
