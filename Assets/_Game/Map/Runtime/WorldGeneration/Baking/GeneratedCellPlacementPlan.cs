using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedSectorIndexCoordinate :
        IEquatable<GeneratedSectorIndexCoordinate>, IComparable<GeneratedSectorIndexCoordinate>
    {
        public GeneratedSectorIndexCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns &&
                                  Y >= 0 && Y < GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorRows;
        public int RowMajorIndex => Y * GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns + X;
        public int CompareTo(GeneratedSectorIndexCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }
        public bool Equals(GeneratedSectorIndexCoordinate other) => other != null &&
            X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorIndexCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() => Number(X) + "," + Number(Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedCellPlacementCoordinate :
        IEquatable<GeneratedCellPlacementCoordinate>, IComparable<GeneratedCellPlacementCoordinate>
    {
        public GeneratedCellPlacementCoordinate(
            GeneratedSectorIndexCoordinate sectorIndex,
            int sliceIndex,
            int sliceLocalX,
            int sliceLocalY,
            int microChunkLocalX,
            int microChunkLocalY,
            int sectorLocalX,
            int sectorLocalY,
            int worldX,
            int worldY)
        {
            SectorIndex = sectorIndex;
            SliceIndex = sliceIndex;
            SliceLocalX = sliceLocalX;
            SliceLocalY = sliceLocalY;
            MicroChunkLocalX = microChunkLocalX;
            MicroChunkLocalY = microChunkLocalY;
            SectorLocalX = sectorLocalX;
            SectorLocalY = sectorLocalY;
            WorldX = worldX;
            WorldY = worldY;
        }

        public GeneratedSectorIndexCoordinate SectorIndex { get; }
        public int SliceIndex { get; }
        public int SliceLocalX { get; }
        public int SliceLocalY { get; }
        public int MicroChunkLocalX { get; }
        public int MicroChunkLocalY { get; }
        public int SectorLocalX { get; }
        public int SectorLocalY { get; }
        public int WorldX { get; }
        public int WorldY { get; }
        public int SectorRowMajorIndex => SectorLocalY *
            GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth + SectorLocalX;
        public int WorldRowMajorIndex => WorldY *
            GeneratedTerrainGeometrySnapshot.CanonicalWorldWidth + WorldX;
        public string StableToken => string.Join("|", new[]
        {
            "COORD", SectorIndex == null ? "MISSING" : SectorIndex.ToString(),
            Number(SliceIndex), Number(SliceLocalX), Number(SliceLocalY),
            Number(MicroChunkLocalX), Number(MicroChunkLocalY),
            Number(SectorLocalX), Number(SectorLocalY), Number(WorldX), Number(WorldY),
        });

        public bool IsValid(GeneratedTerrainGeometrySnapshot geometry)
        {
            if (geometry == null || SectorIndex == null || !SectorIndex.IsInBounds ||
                SliceIndex < 0 || SliceIndex >= geometry.ChunkCount ||
                SliceLocalX < 0 || SliceLocalX >= geometry.MicroChunkWidth ||
                SliceLocalY < 0 || SliceLocalY >= geometry.MicroChunkHeight ||
                MicroChunkLocalX != SliceLocalX || MicroChunkLocalY != SliceLocalY)
                return false;
            var chunkX = SliceIndex % geometry.ChunkGridWidth;
            var chunkY = SliceIndex / geometry.ChunkGridWidth;
            return SectorLocalX == chunkX * geometry.MicroChunkWidth + SliceLocalX &&
                   SectorLocalY == chunkY * geometry.MicroChunkHeight + SliceLocalY &&
                   SectorLocalX >= 0 && SectorLocalX < geometry.SectorWidth &&
                   SectorLocalY >= 0 && SectorLocalY < geometry.SectorHeight &&
                   WorldX == SectorIndex.X * geometry.SectorWidth + SectorLocalX &&
                   WorldY == SectorIndex.Y * geometry.SectorHeight + SectorLocalY &&
                   WorldX >= 0 && WorldX < geometry.WorldWidth &&
                   WorldY >= 0 && WorldY < geometry.WorldHeight;
        }

        public int CompareTo(GeneratedCellPlacementCoordinate other)
        {
            if (other == null) return -1;
            var comparison = WorldY.CompareTo(other.WorldY);
            return comparison != 0 ? comparison : WorldX.CompareTo(other.WorldX);
        }
        public bool Equals(GeneratedCellPlacementCoordinate other) => other != null &&
            WorldX == other.WorldX && WorldY == other.WorldY &&
            SliceIndex == other.SliceIndex && SectorLocalX == other.SectorLocalX &&
            SectorLocalY == other.SectorLocalY;
        public override bool Equals(object obj) => Equals(obj as GeneratedCellPlacementCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (WorldX * 397) ^ WorldY; }
        }
        public override string ToString() => StableToken;
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedCellPlacementId :
        IEquatable<GeneratedCellPlacementId>, IComparable<GeneratedCellPlacementId>
    {
        public GeneratedCellPlacementId(string sourceSectorId, GeneratedCellPlacementCoordinate coordinate)
        {
            SourceSectorId = sourceSectorId ?? string.Empty;
            Coordinate = coordinate;
            Value = coordinate == null ? string.Empty : string.Join(":", new[]
            {
                SourceSectorId, "SECTOR", Number(coordinate.SectorIndex.X, "D2") + "," +
                    Number(coordinate.SectorIndex.Y, "D2"),
                "CELL", Number(coordinate.SectorLocalX, "D2") + "," +
                    Number(coordinate.SectorLocalY, "D2"),
            });
        }

        public string SourceSectorId { get; }
        public GeneratedCellPlacementCoordinate Coordinate { get; }
        public string Value { get; }
        public bool IsValid => GeneratedTerrainAssetKey.IsValid(SourceSectorId) &&
            !string.IsNullOrEmpty(Value) && Coordinate != null;
        public int CompareTo(GeneratedCellPlacementId other) => other == null
            ? -1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(GeneratedCellPlacementId other) => other != null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedCellPlacementId);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        private static string Number(int value, string format) =>
            value.ToString(format, CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedCellPlacementLayer : IComparable<GeneratedCellPlacementLayer>
    {
        internal GeneratedCellPlacementLayer(
            GeneratedMicroChunkLayerRecord source,
            GeneratedTerrainTileCode tileCode,
            string resolvedAssetKey)
        {
            Source = source;
            TileCode = tileCode;
            ResolvedAssetKey = resolvedAssetKey ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "PLACEMENT_LAYER", ((int)source.Layer).ToString(CultureInfo.InvariantCulture),
                source.Layer.ToString().ToUpperInvariant(), source.CellKind.ToString().ToUpperInvariant(),
                source.SourceOwner.ToString().ToUpperInvariant(),
                source.Protection.ToString().ToUpperInvariant(), source.IsProtected ? "1" : "0",
                source.ProvenanceId, source.ClaimId, source.SourceCellToken,
                tileCode.Value, ResolvedAssetKey, source.StableToken,
            });
        }

        public GeneratedMicroChunkLayerRecord Source { get; }
        public FinalCanvasLayerKind Layer => Source.Layer;
        public FinalCanvasCellKind CellKind => Source.CellKind;
        public FinalCanvasSourceOwner SourceOwner => Source.SourceOwner;
        public FinalCanvasProtectionKind Protection => Source.Protection;
        public bool IsProtected => Source.IsProtected;
        public string ProvenanceId => Source.ProvenanceId;
        public string ClaimId => Source.ClaimId;
        public string SourceCellToken => Source.SourceCellToken;
        public string SourceLayerStableToken => Source.StableToken;
        public GeneratedTerrainTileCode TileCode { get; }
        public string ResolvedAssetKey { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedCellPlacementLayer other) => other == null
            ? -1 : Layer.CompareTo(other.Layer);
    }

    public sealed class GeneratedCellPlacementSlotReference :
        IComparable<GeneratedCellPlacementSlotReference>
    {
        internal GeneratedCellPlacementSlotReference(
            GeneratedMarkerSlot source,
            GeneratedTerrainPrefabId prefabId,
            string resolvedAssetKey)
        {
            Source = source;
            PrefabId = prefabId;
            ResolvedAssetKey = resolvedAssetKey ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "PLACEMENT_SLOT", source.Id.Value, prefabId.Value, ResolvedAssetKey,
                source.SourceKey, source.Provenance.StableToken, source.StableToken,
            });
        }

        public GeneratedMarkerSlot Source { get; }
        public string SlotId => Source.Id.Value;
        public GeneratedTerrainPrefabId PrefabId { get; }
        public string ResolvedAssetKey { get; }
        public string SourceKey => Source.SourceKey;
        public string SourceProvenanceToken => Source.Provenance.StableToken;
        public string StableToken { get; }
        public int CompareTo(GeneratedCellPlacementSlotReference other) => other == null
            ? -1 : string.Compare(SlotId, other.SlotId, StringComparison.Ordinal);
    }

    public sealed class GeneratedCellPlacementSocketReference :
        IComparable<GeneratedCellPlacementSocketReference>
    {
        private readonly ReadOnlyCollection<string> bandTokens;

        internal GeneratedCellPlacementSocketReference(
            GeneratedMicroChunkSliceRecord slice,
            GeneratedMicroChunkSocketSignature signature)
        {
            SourceSliceId = slice.Id.Value;
            ChunkIndex = slice.ChunkIndex;
            Side = signature.Side.Value;
            SideSignature = signature.Digest;
            SliceSignature = slice.Signature.Digest;
            TraversalToken = slice.TraversalSummary.StableToken;
            bandTokens = new ReadOnlyCollection<string>(slice.Bands(Side)
                .Select(value => value.StableToken).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            StableToken = string.Join("|", new[]
            {
                "PLACEMENT_SOCKET", SourceSliceId, Number(ChunkIndex),
                Side.ToString().ToUpperInvariant(), SideSignature, SliceSignature,
                TraversalToken,
            }.Concat(bandTokens));
        }

        public string SourceSliceId { get; }
        public int ChunkIndex { get; }
        public GeneratedMicroChunkSocketSide Side { get; }
        public string SideSignature { get; }
        public string SliceSignature { get; }
        public string TraversalToken { get; }
        public IReadOnlyList<string> BandTokens => bandTokens;
        public string StableToken { get; }
        public int CompareTo(GeneratedCellPlacementSocketReference other)
        {
            if (other == null) return -1;
            var comparison = ChunkIndex.CompareTo(other.ChunkIndex);
            return comparison != 0 ? comparison : Side.CompareTo(other.Side);
        }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedCellPlacementRecord : IComparable<GeneratedCellPlacementRecord>
    {
        private readonly ReadOnlyCollection<GeneratedCellPlacementLayer> layers;
        private readonly ReadOnlyCollection<GeneratedCellPlacementSlotReference> slotReferences;

        internal GeneratedCellPlacementRecord(
            GeneratedCellPlacementId id,
            GeneratedCellPlacementCoordinate coordinate,
            IEnumerable<GeneratedCellPlacementLayer> sourceLayers,
            IEnumerable<GeneratedCellPlacementSlotReference> sourceSlots)
        {
            Id = id;
            Coordinate = coordinate;
            layers = new ReadOnlyCollection<GeneratedCellPlacementLayer>((sourceLayers ??
                Array.Empty<GeneratedCellPlacementLayer>()).OrderBy(value => value).ToArray());
            slotReferences = new ReadOnlyCollection<GeneratedCellPlacementSlotReference>((sourceSlots ??
                Array.Empty<GeneratedCellPlacementSlotReference>()).OrderBy(value => value).ToArray());
            StableToken = string.Join("|", new[]
            {
                "PLACEMENT", Id.Value, Coordinate.StableToken,
            }.Concat(layers.Select(value => value.StableToken))
             .Concat(slotReferences.Select(value => value.StableToken)));
        }

        public GeneratedCellPlacementId Id { get; }
        public GeneratedCellPlacementCoordinate Coordinate { get; }
        public IReadOnlyList<GeneratedCellPlacementLayer> Layers => layers;
        public IReadOnlyList<GeneratedCellPlacementSlotReference> SlotReferences => slotReferences;
        public int LayerCount => layers.Count;
        public int SlotReferenceCount => slotReferences.Count;
        public string StableToken { get; }
        public int CompareTo(GeneratedCellPlacementRecord other) => other == null
            ? -1 : Coordinate.CompareTo(other.Coordinate);
    }

    public sealed class GeneratedCellPlacementRequest
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkSliceRecord> slices;
        private readonly ReadOnlyCollection<GeneratedMarkerSlot> slots;

        public GeneratedCellPlacementRequest(
            GeneratedSectorIndexCoordinate sectorIndex,
            GeneratedTerrainGeometrySnapshot geometry,
            string expectedGeometryDigest,
            string map16ExitAuditDigest,
            GeneratedMicroChunkSliceSet sliceSet,
            GeneratedMicroChunkMarkerSlotSet slotSet,
            GeneratedTerrainExportPacket exportPacket,
            GeneratedTerrainAssetRegistrySnapshot assetRegistry,
            IEnumerable<GeneratedMicroChunkSliceRecord> sourceSlices = null,
            IEnumerable<GeneratedMarkerSlot> sourceSlots = null)
        {
            SectorIndex = sectorIndex;
            Geometry = geometry;
            ExpectedGeometryDigest = expectedGeometryDigest ?? string.Empty;
            Map16ExitAuditDigest = map16ExitAuditDigest ?? string.Empty;
            SliceSet = sliceSet;
            SlotSet = slotSet;
            ExportPacket = exportPacket;
            AssetRegistry = assetRegistry;
            var rawSlices = (sourceSlices ?? (sliceSet == null
                ? Array.Empty<GeneratedMicroChunkSliceRecord>() : sliceSet.Slices)).ToArray();
            var rawSlots = (sourceSlots ?? (slotSet == null
                ? Array.Empty<GeneratedMarkerSlot>() : slotSet.Slots)).ToArray();
            NullSliceCount = rawSlices.Count(value => value == null);
            NullSlotCount = rawSlots.Count(value => value == null);
            slices = new ReadOnlyCollection<GeneratedMicroChunkSliceRecord>(rawSlices
                .Where(value => value != null).OrderBy(value => value).ToArray());
            slots = new ReadOnlyCollection<GeneratedMarkerSlot>(rawSlots
                .Where(value => value != null).OrderBy(value => value).ToArray());
            CanonicalDigest = GeneratedCellPlacementDigest.ComputeInput(this);
        }

        public GeneratedSectorIndexCoordinate SectorIndex { get; }
        public GeneratedTerrainGeometrySnapshot Geometry { get; }
        public string ExpectedGeometryDigest { get; }
        public string Map16ExitAuditDigest { get; }
        public GeneratedMicroChunkSliceSet SliceSet { get; }
        public GeneratedMicroChunkMarkerSlotSet SlotSet { get; }
        public GeneratedTerrainExportPacket ExportPacket { get; }
        public GeneratedTerrainAssetRegistrySnapshot AssetRegistry { get; }
        public IReadOnlyList<GeneratedMicroChunkSliceRecord> SourceSlices => slices;
        public IReadOnlyList<GeneratedMarkerSlot> SourceSlots => slots;
        public int NullSliceCount { get; }
        public int NullSlotCount { get; }
        public string CanonicalDigest { get; }
    }

    public enum GeneratedCellPlacementFailureCode
    {
        MissingRequest = 1,
        MissingGeometry = 2,
        StaleGeometry = 3,
        InvalidSectorIndex = 4,
        MissingSliceSet = 5,
        InvalidSliceSet = 6,
        DuplicateCoordinate = 7,
        MissingCoordinate = 8,
        OutOfBoundsCoordinate = 9,
        MissingSlotSet = 10,
        InvalidSlotSet = 11,
        MissingExportPacket = 12,
        StaleExportPacket = 13,
        MissingAssetRegistry = 14,
        InvalidAssetRegistry = 15,
        MissingTileCode = 16,
        MissingPrefabId = 17,
        DuplicateAssetReference = 18,
        InvalidAssetId = 19,
        LayerProjectionMismatch = 20,
        SlotProjectionMismatch = 21,
        InvalidDigest = 22,
    }

    public sealed class GeneratedCellPlacementFailure :
        IEquatable<GeneratedCellPlacementFailure>, IComparable<GeneratedCellPlacementFailure>
    {
        public GeneratedCellPlacementFailure(
            GeneratedCellPlacementFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedCellPlacementFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;
        public int CompareTo(GeneratedCellPlacementFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedCellPlacementFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedCellPlacementFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedCellPlacementPlan
    {
        private readonly ReadOnlyCollection<GeneratedCellPlacementRecord> records;
        private readonly ReadOnlyCollection<GeneratedCellPlacementSocketReference> socketReferences;

        internal GeneratedCellPlacementPlan(
            GeneratedCellPlacementRequest request,
            GeneratedTerrainAssetResolution assetResolution,
            IEnumerable<GeneratedCellPlacementRecord> sourceRecords,
            IEnumerable<GeneratedCellPlacementSocketReference> sourceSockets)
        {
            Request = request;
            AssetResolution = assetResolution;
            records = new ReadOnlyCollection<GeneratedCellPlacementRecord>((sourceRecords ??
                Array.Empty<GeneratedCellPlacementRecord>()).OrderBy(value => value).ToArray());
            socketReferences = new ReadOnlyCollection<GeneratedCellPlacementSocketReference>((sourceSockets ??
                Array.Empty<GeneratedCellPlacementSocketReference>()).OrderBy(value => value).ToArray());
            OutputDigest = GeneratedCellPlacementDigest.ComputeOutput(this);
        }

        public const string PolicyVersion = "MAP17_01_GENERATED_CELL_PLACEMENT_V1";
        public const string DownstreamOwner = "MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION";
        public const bool OpensDownstreamTask = false;
        public const string ReplayVerifierContract = "GeneratedTerrainReplayVerifier";

        public GeneratedCellPlacementRequest Request { get; }
        public GeneratedTerrainAssetResolution AssetResolution { get; }
        public IReadOnlyList<GeneratedCellPlacementRecord> Records => records;
        public IReadOnlyList<GeneratedCellPlacementSocketReference> SocketReferences => socketReferences;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int PlacedCellCount => records.Count;
        public int PlacedLayerReferenceCount => records.Sum(value => value.LayerCount);
        public int CellPlacementIdUniqueCount => records.Select(value => value.Id.Value)
            .Distinct(StringComparer.Ordinal).Count();
        public int UniqueSectorCoordinateCount => records.Select(value => string.Join(",", new[]
            { Number(value.Coordinate.SectorLocalX), Number(value.Coordinate.SectorLocalY) }))
            .Distinct(StringComparer.Ordinal).Count();
        public int DuplicateSectorCoordinateCount => PlacedCellCount - UniqueSectorCoordinateCount;
        public int MissingSectorCoordinateCount => GeneratedTerrainGeometrySnapshot.CanonicalSectorCellCount -
            UniqueSectorCoordinateCount;
        public int OutOfBoundsCoordinateCount => records.Count(value =>
            !value.Coordinate.IsValid(Request.Geometry));
        public int SocketReferenceCount => socketReferences.Count;
        public int SlotReferenceCount => records.Sum(value => value.SlotReferenceCount);
        public int SourceProvenanceReferenceCount => records.SelectMany(value => value.Layers)
            .Count(value => !string.IsNullOrEmpty(value.ProvenanceId));
        public int TilemapBakeCount => 0;
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
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedWorldPlacementProjection
    {
        internal GeneratedWorldPlacementProjection(
            GeneratedCellPlacementPlan sourcePlan,
            int sectorCount,
            int cellCount,
            int uniqueCellCount,
            int outOfBoundsCellCount,
            string digest)
        {
            SourcePlan = sourcePlan;
            SectorCount = sectorCount;
            CellCount = cellCount;
            UniqueCellCount = uniqueCellCount;
            OutOfBoundsCellCount = outOfBoundsCellCount;
            Digest = digest ?? string.Empty;
        }

        public GeneratedCellPlacementPlan SourcePlan { get; }
        public bool Success => SourcePlan != null &&
            SectorCount == GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount &&
            CellCount == GeneratedTerrainGeometrySnapshot.CanonicalWorldCellCount &&
            UniqueCellCount == CellCount && OutOfBoundsCellCount == 0 &&
            BakingCanonicalDigest.IsLowerHexSha256(Digest);
        public int SectorCount { get; }
        public int CellCount { get; }
        public int UniqueCellCount { get; }
        public int DuplicateCellCount => CellCount - UniqueCellCount;
        public int MissingCellCount => GeneratedTerrainGeometrySnapshot.CanonicalWorldCellCount - UniqueCellCount;
        public int OutOfBoundsCellCount { get; }
        public string Digest { get; }
        public int TilemapBakeCount => 0;
    }

    public sealed class GeneratedCellPlacementResult
    {
        private readonly ReadOnlyCollection<GeneratedCellPlacementFailure> failures;

        internal GeneratedCellPlacementResult(
            GeneratedCellPlacementRequest request,
            GeneratedCellPlacementPlan plan,
            GeneratedTerrainAssetResolution assetResolution,
            IEnumerable<GeneratedCellPlacementFailure> sourceFailures)
        {
            Request = request;
            Plan = plan;
            AssetResolution = assetResolution;
            failures = new ReadOnlyCollection<GeneratedCellPlacementFailure>((sourceFailures ??
                Array.Empty<GeneratedCellPlacementFailure>()).Distinct()
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0 &&
            AssetResolution != null && AssetResolution.Success;
        public GeneratedCellPlacementRequest Request { get; }
        public GeneratedCellPlacementPlan Plan { get; }
        public GeneratedTerrainAssetResolution AssetResolution { get; }
        public IReadOnlyList<GeneratedCellPlacementFailure> Failures => failures;
        public string InputDigest => Plan == null
            ? (Request == null ? string.Empty : Request.CanonicalDigest)
            : Plan.InputDigest;
        public string OutputDigest => Plan == null ? string.Empty : Plan.OutputDigest;
    }

    public static class GeneratedCellPlacementDigest
    {
        public static string ComputeGeometry(GeneratedTerrainGeometrySnapshot geometry) => geometry == null
            ? string.Empty : BakingCanonicalDigest.HashCanonicalLines(geometry.CanonicalLines);

        public static string ComputeInput(GeneratedCellPlacementRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedCellPlacementPlan.PolicyVersion,
                "SECTOR_INDEX|" + (request.SectorIndex == null ? "MISSING" : request.SectorIndex.ToString()),
                "GEOMETRY|" + ComputeGeometry(request.Geometry) + "|" + request.ExpectedGeometryDigest,
                "MAP16_EXIT|" + request.Map16ExitAuditDigest,
                "SLICES|" + (request.SliceSet == null ? string.Empty : request.SliceSet.InputDigest) + "|" +
                    (request.SliceSet == null ? string.Empty : request.SliceSet.OutputDigest),
                "SLOTS|" + (request.SlotSet == null ? string.Empty : request.SlotSet.InputDigest) + "|" +
                    (request.SlotSet == null ? string.Empty : request.SlotSet.OutputDigest),
                "EXPORT|" + (request.ExportPacket == null ? string.Empty : request.ExportPacket.ManifestDigest) + "|" +
                    (request.ExportPacket == null ? string.Empty : request.ExportPacket.PacketDigest),
                "REGISTRY|" + (request.AssetRegistry == null ? string.Empty : request.AssetRegistry.Digest),
                "NULLS|" + Number(request.NullSliceCount) + "|" + Number(request.NullSlotCount),
            };
            lines.AddRange(request.SourceSlices.Select(value => "SLICE|" + value.Id.Value + "|" +
                value.Signature.Digest));
            lines.AddRange(request.SourceSlots.Select(value => "SLOT|" + value.Id.Value + "|" +
                value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputeOutput(GeneratedCellPlacementPlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedCellPlacementPlan.PolicyVersion,
                "INPUT|" + plan.InputDigest,
                "REPLAY_VERIFIER|" + GeneratedCellPlacementPlan.ReplayVerifierContract,
                "COUNTS|" + Number(plan.PlacedCellCount) + "|" +
                    Number(plan.PlacedLayerReferenceCount) + "|" + Number(plan.SocketReferenceCount) + "|" +
                    Number(plan.SlotReferenceCount) + "|" + Number(plan.SourceProvenanceReferenceCount),
                "VALIDATION|" + Number(plan.DuplicateSectorCoordinateCount) + "|" +
                    Number(plan.MissingSectorCoordinateCount) + "|" + Number(plan.OutOfBoundsCoordinateCount),
                "DOWNSTREAM|" + GeneratedCellPlacementPlan.DownstreamOwner + "|0",
                "MUTATIONS|0|0|0|0|0|0|0|0|0|0|0|0",
            };
            lines.AddRange(plan.Records.OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange(plan.SocketReferences.OrderBy(value => value).Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
