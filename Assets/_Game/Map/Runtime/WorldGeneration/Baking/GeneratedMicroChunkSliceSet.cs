using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedMicroChunkSliceId :
        IEquatable<GeneratedMicroChunkSliceId>, IComparable<GeneratedMicroChunkSliceId>
    {
        public GeneratedMicroChunkSliceId(string sectorId, int chunkIndex)
        {
            SectorId = sectorId ?? string.Empty;
            ChunkIndex = chunkIndex;
            Value = SectorId + ":CHUNK:" + chunkIndex.ToString("D2", CultureInfo.InvariantCulture);
        }

        public string SectorId { get; }
        public int ChunkIndex { get; }
        public string Value { get; }
        public int CompareTo(GeneratedMicroChunkSliceId other) => other == null
            ? -1 : ChunkIndex.CompareTo(other.ChunkIndex);
        public bool Equals(GeneratedMicroChunkSliceId other) => other != null &&
            SectorId == other.SectorId && ChunkIndex == other.ChunkIndex;
        public override bool Equals(object obj) => Equals(obj as GeneratedMicroChunkSliceId);
        public override int GetHashCode()
        {
            unchecked { return (StringComparer.Ordinal.GetHashCode(SectorId) * 397) ^ ChunkIndex; }
        }
        public override string ToString() => Value;
    }

    public sealed class GeneratedMicroChunkLayerRecord :
        IComparable<GeneratedMicroChunkLayerRecord>
    {
        public GeneratedMicroChunkLayerRecord(
            FinalCanvasLayerKind layer,
            FinalCanvasCellKind cellKind,
            FinalCanvasSourceOwner sourceOwner,
            string provenanceId,
            FinalCanvasProtectionKind protection,
            bool isProtected,
            string claimId,
            string sourceCellToken)
        {
            Layer = layer;
            CellKind = cellKind;
            SourceOwner = sourceOwner;
            ProvenanceId = provenanceId ?? string.Empty;
            Protection = protection;
            IsProtected = isProtected;
            ClaimId = claimId ?? string.Empty;
            SourceCellToken = sourceCellToken ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "LAYER", Layer.ToString().ToUpperInvariant(),
                CellKind.ToString().ToUpperInvariant(), SourceOwner.ToString().ToUpperInvariant(),
                Protection.ToString().ToUpperInvariant(), IsProtected ? "1" : "0",
                ProvenanceId, ClaimId, SourceCellToken,
            });
        }

        public FinalCanvasLayerKind Layer { get; }
        public FinalCanvasCellKind CellKind { get; }
        public FinalCanvasSourceOwner SourceOwner { get; }
        public string ProvenanceId { get; }
        public FinalCanvasProtectionKind Protection { get; }
        public bool IsProtected { get; }
        public string ClaimId { get; }
        public string SourceCellToken { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedMicroChunkLayerRecord other) =>
            other == null ? -1 : Layer.CompareTo(other.Layer);
    }

    public sealed class GeneratedMicroChunkWitnessMembership :
        IComparable<GeneratedMicroChunkWitnessMembership>
    {
        public GeneratedMicroChunkWitnessMembership(
            string witnessKind,
            string sourceStableId,
            int pathIndex)
        {
            WitnessKind = witnessKind ?? string.Empty;
            SourceStableId = sourceStableId ?? string.Empty;
            PathIndex = pathIndex;
            StableToken = string.Join("|", new[]
            {
                "WITNESS", WitnessKind, SourceStableId,
                PathIndex.ToString(CultureInfo.InvariantCulture),
            });
        }

        public string WitnessKind { get; }
        public string SourceStableId { get; }
        public int PathIndex { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedMicroChunkWitnessMembership other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(WitnessKind, other.WitnessKind, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SourceStableId, other.SourceStableId, StringComparison.Ordinal);
            return comparison != 0 ? comparison : PathIndex.CompareTo(other.PathIndex);
        }
    }

    public sealed class GeneratedMicroChunkCellSource :
        IComparable<GeneratedMicroChunkCellSource>
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkLayerRecord> layers;
        private readonly ReadOnlyCollection<GeneratedMicroChunkWitnessMembership> memberships;

        public GeneratedMicroChunkCellSource(
            PatternChunkCellAddress address,
            IEnumerable<GeneratedMicroChunkLayerRecord> sourceLayers,
            IEnumerable<GeneratedMicroChunkWitnessMembership> sourceMemberships)
        {
            Address = address;
            var rawLayers = (sourceLayers ?? Array.Empty<GeneratedMicroChunkLayerRecord>()).ToArray();
            NullLayerCount = rawLayers.Count(value => value == null);
            layers = new ReadOnlyCollection<GeneratedMicroChunkLayerRecord>(rawLayers
                .Where(value => value != null).OrderBy(value => value).ToArray());
            var rawMemberships = (sourceMemberships ??
                Array.Empty<GeneratedMicroChunkWitnessMembership>()).ToArray();
            NullMembershipCount = rawMemberships.Count(value => value == null);
            memberships = new ReadOnlyCollection<GeneratedMicroChunkWitnessMembership>(rawMemberships
                .Where(value => value != null).OrderBy(value => value).ToArray());
            StableToken = string.Join("|", new[]
            {
                "SOURCE_CELL", Address == null ? "MISSING" : Address.StableToken,
                "NULLS", Number(NullLayerCount), Number(NullMembershipCount),
            }.Concat(layers.Select(value => value.StableToken))
             .Concat(memberships.Select(value => value.StableToken)));
        }

        public PatternChunkCellAddress Address { get; }
        public IReadOnlyList<GeneratedMicroChunkLayerRecord> Layers => layers;
        public IReadOnlyList<GeneratedMicroChunkWitnessMembership> WitnessMemberships => memberships;
        public int NullLayerCount { get; }
        public int NullMembershipCount { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedMicroChunkCellSource other) => other == null
            ? -1
            : (Address == null
                ? (other.Address == null ? 0 : 1)
                : (other.Address == null ? -1 : Address.CompareTo(other.Address)));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public enum GeneratedMicroChunkSocketSide
    {
        Left = 1,
        Right = 2,
        Down = 3,
        Up = 4,
    }

    public sealed class GeneratedMicroChunkCell : IComparable<GeneratedMicroChunkCell>
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkLayerRecord> layers;
        private readonly ReadOnlyCollection<GeneratedMicroChunkWitnessMembership> memberships;

        internal GeneratedMicroChunkCell(GeneratedMicroChunkCellSource source)
        {
            SectorCoordinate = source.Address.SectorCoordinate;
            LocalCoordinate = source.Address.LocalTileCoordinate;
            ChunkIndex = source.Address.ChunkIndex;
            layers = new ReadOnlyCollection<GeneratedMicroChunkLayerRecord>(source.Layers
                .OrderBy(value => value).ToArray());
            memberships = new ReadOnlyCollection<GeneratedMicroChunkWitnessMembership>(
                source.WitnessMemberships.OrderBy(value => value).ToArray());
            var terrain = Layer(FinalCanvasLayerKind.Terrain);
            var affordance = Layer(FinalCanvasLayerKind.Affordance);
            var hazard = Layer(FinalCanvasLayerKind.Hazard);
            var protection = Layer(FinalCanvasLayerKind.Protection);
            IsSolid = terrain.CellKind == FinalCanvasCellKind.Solid;
            IsHazardBlocked = hazard.CellKind == FinalCanvasCellKind.Hazard;
            IsProtectionBlocked = protection.CellKind != FinalCanvasCellKind.None &&
                                  protection.CellKind != FinalCanvasCellKind.ProtectedOpen;
            var explicitlyOpen = terrain.CellKind == FinalCanvasCellKind.Air ||
                                 affordance.CellKind == FinalCanvasCellKind.Traversable ||
                                 protection.CellKind == FinalCanvasCellKind.ProtectedOpen;
            IsPassable = explicitlyOpen && !IsSolid && !IsHazardBlocked && !IsProtectionBlocked;
            StableToken = string.Join("|", new[]
            {
                "CELL", Number(ChunkIndex), LocalCoordinate.ToString(),
                SectorCoordinate.ToString(), IsPassable ? "OPEN" : "BLOCKED",
            }.Concat(layers.Select(value => value.StableToken))
             .Concat(memberships.Select(value => value.StableToken)));
        }

        public int ChunkIndex { get; }
        public SectorTileCoordinate SectorCoordinate { get; }
        public MicroChunkLocalTileCoordinate LocalCoordinate { get; }
        public IReadOnlyList<GeneratedMicroChunkLayerRecord> Layers => layers;
        public IReadOnlyList<GeneratedMicroChunkWitnessMembership> WitnessMemberships => memberships;
        public int LayerCount => layers.Count;
        public int WitnessMembershipCount => memberships.Count;
        public bool IsSolid { get; }
        public bool IsHazardBlocked { get; }
        public bool IsProtectionBlocked { get; }
        public bool IsPassable { get; }
        public bool IsProtected => layers.Any(value => value.IsProtected);
        public FinalCanvasProtectionKind ProtectionKind =>
            Layer(FinalCanvasLayerKind.Protection).Protection;
        public string StableToken { get; }
        public GeneratedMicroChunkLayerRecord Layer(FinalCanvasLayerKind kind) =>
            layers.Single(value => value.Layer == kind);
        public int CompareTo(GeneratedMicroChunkCell other) => other == null
            ? -1 : LocalCoordinate.CompareTo(other.LocalCoordinate);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedMicroChunkSocketBand :
        IComparable<GeneratedMicroChunkSocketBand>
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkCell> cells;
        private readonly ReadOnlyCollection<string> sourceEvidence;

        internal GeneratedMicroChunkSocketBand(
            GeneratedMicroChunkSocketSide side,
            IEnumerable<GeneratedMicroChunkCell> sourceCells)
        {
            Side = side;
            cells = new ReadOnlyCollection<GeneratedMicroChunkCell>((sourceCells ??
                Array.Empty<GeneratedMicroChunkCell>()).OrderBy(EdgePosition).ToArray());
            Start = cells.Count == 0 ? null : cells[0].LocalCoordinate;
            End = cells.Count == 0 ? null : cells[cells.Count - 1].LocalCoordinate;
            sourceEvidence = new ReadOnlyCollection<string>(cells.Select(CellEvidence)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            TouchesPassableComponent = cells.Count > 0 && cells.All(value => value.IsPassable);
            StableToken = string.Join("|", new[]
            {
                "BAND", Side.ToString().ToUpperInvariant(),
                Start == null ? "MISSING" : Start.ToString(),
                End == null ? "MISSING" : End.ToString(), Number(Length),
                TouchesPassableComponent ? "1" : "0",
            }.Concat(sourceEvidence));
        }

        public GeneratedMicroChunkSocketSide Side { get; }
        public MicroChunkLocalTileCoordinate Start { get; }
        public MicroChunkLocalTileCoordinate End { get; }
        public int Length => cells.Count;
        public IReadOnlyList<GeneratedMicroChunkCell> Cells => cells;
        public IReadOnlyList<string> SourceEvidence => sourceEvidence;
        public bool TouchesPassableComponent { get; }
        public string StableToken { get; }

        public int CompareTo(GeneratedMicroChunkSocketBand other)
        {
            if (other == null) return -1;
            var comparison = Side.CompareTo(other.Side);
            return comparison != 0 ? comparison : EdgePosition(cells.FirstOrDefault())
                .CompareTo(EdgePosition(other.cells.FirstOrDefault()));
        }

        private int EdgePosition(GeneratedMicroChunkCell value) => EdgePositionStatic(Side, value);
        internal static int EdgePositionStatic(
            GeneratedMicroChunkSocketSide side,
            GeneratedMicroChunkCell value) => value == null ? int.MaxValue :
                (side == GeneratedMicroChunkSocketSide.Left ||
                 side == GeneratedMicroChunkSocketSide.Right
                    ? value.LocalCoordinate.Y : value.LocalCoordinate.X);
        private static string CellEvidence(GeneratedMicroChunkCell cell) => string.Join("|", new[]
        {
            "EVIDENCE", cell.SectorCoordinate.ToString(),
            string.Join(",", cell.Layers.Select(value => value.SourceOwner.ToString().ToUpperInvariant())
                .Distinct().OrderBy(value => value, StringComparer.Ordinal)),
            string.Join(",", cell.Layers.Select(value => value.ProvenanceId)
                .Distinct().OrderBy(value => value, StringComparer.Ordinal)),
            string.Join(",", cell.WitnessMemberships.Select(value => value.SourceStableId)
                .Distinct().OrderBy(value => value, StringComparer.Ordinal)),
        });
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedMicroChunkSocketSignature :
        IComparable<GeneratedMicroChunkSocketSignature>
    {
        internal GeneratedMicroChunkSocketSignature(
            GeneratedMicroChunkSocketSide? side,
            string digest)
        {
            Side = side;
            Digest = digest ?? string.Empty;
        }

        public GeneratedMicroChunkSocketSide? Side { get; }
        public bool IsSliceSignature => !Side.HasValue;
        public string Digest { get; }
        public int CompareTo(GeneratedMicroChunkSocketSignature other)
        {
            if (other == null) return -1;
            if (!Side.HasValue) return other.Side.HasValue ? 1 : 0;
            if (!other.Side.HasValue) return -1;
            return Side.Value.CompareTo(other.Side.Value);
        }
    }

    public sealed class GeneratedMicroChunkTraversalSummary
    {
        internal GeneratedMicroChunkTraversalSummary(
            int passableCellCount,
            int blockedCellCount,
            int routeRecoveryWitnessCellCount,
            int routeRecoveryMembershipCount,
            int socketConnectedSideCount,
            int connectedPassableComponentCount,
            int socketBandCount,
            int socketBandsTouchingPassableComponentCount)
        {
            PassableCellCount = passableCellCount;
            BlockedCellCount = blockedCellCount;
            RouteRecoveryWitnessCellCount = routeRecoveryWitnessCellCount;
            RouteRecoveryMembershipCount = routeRecoveryMembershipCount;
            SocketConnectedSideCount = socketConnectedSideCount;
            ConnectedPassableComponentCount = connectedPassableComponentCount;
            SocketBandCount = socketBandCount;
            SocketBandsTouchingPassableComponentCount =
                socketBandsTouchingPassableComponentCount;
            StableToken = string.Join("|", new[]
            {
                "TRAVERSAL", Number(PassableCellCount), Number(BlockedCellCount),
                Number(RouteRecoveryWitnessCellCount), Number(RouteRecoveryMembershipCount),
                Number(SocketConnectedSideCount), Number(ConnectedPassableComponentCount),
                Number(SocketBandCount), Number(SocketBandsTouchingPassableComponentCount),
            });
        }

        public int PassableCellCount { get; }
        public int BlockedCellCount { get; }
        public int RouteRecoveryWitnessCellCount { get; }
        public int RouteRecoveryMembershipCount { get; }
        public int SocketConnectedSideCount { get; }
        public int ConnectedPassableComponentCount { get; }
        public int SocketBandCount { get; }
        public int SocketBandsTouchingPassableComponentCount { get; }
        public bool EverySocketBandTouchesPassableComponent =>
            SocketBandCount == SocketBandsTouchingPassableComponentCount;
        public string StableToken { get; }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedMicroChunkSliceRecord : IComparable<GeneratedMicroChunkSliceRecord>
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkCell> cells;
        private readonly ReadOnlyCollection<GeneratedMicroChunkSocketBand> socketBands;
        private readonly ReadOnlyCollection<GeneratedMicroChunkSocketSignature> sideSignatures;

        internal GeneratedMicroChunkSliceRecord(
            GeneratedMicroChunkSliceId id,
            MicroChunkSlot sourceSlot,
            IEnumerable<GeneratedMicroChunkCell> sourceCells,
            IEnumerable<GeneratedMicroChunkSocketBand> sourceSocketBands,
            IEnumerable<GeneratedMicroChunkSocketSignature> sourceSideSignatures,
            GeneratedMicroChunkTraversalSummary traversalSummary,
            GeneratedMicroChunkSocketSignature signature)
        {
            Id = id;
            SourceSlot = sourceSlot;
            cells = new ReadOnlyCollection<GeneratedMicroChunkCell>((sourceCells ??
                Array.Empty<GeneratedMicroChunkCell>()).OrderBy(value => value).ToArray());
            socketBands = new ReadOnlyCollection<GeneratedMicroChunkSocketBand>((sourceSocketBands ??
                Array.Empty<GeneratedMicroChunkSocketBand>()).OrderBy(value => value).ToArray());
            sideSignatures = new ReadOnlyCollection<GeneratedMicroChunkSocketSignature>(
                (sourceSideSignatures ?? Array.Empty<GeneratedMicroChunkSocketSignature>())
                .OrderBy(value => value).ToArray());
            TraversalSummary = traversalSummary;
            Signature = signature;
        }

        public GeneratedMicroChunkSliceId Id { get; }
        public MicroChunkSlot SourceSlot { get; }
        public int ChunkIndex => SourceSlot.Index;
        public int ChunkX => SourceSlot.ChunkX;
        public int ChunkY => SourceSlot.ChunkY;
        public SectorTileCoordinate SectorOrigin => SourceSlot.Origin;
        public int Width => GeneratedMicroChunkSliceSet.MicroChunkWidth;
        public int Height => GeneratedMicroChunkSliceSet.MicroChunkHeight;
        public IReadOnlyList<GeneratedMicroChunkCell> Cells => cells;
        public IReadOnlyList<GeneratedMicroChunkSocketBand> SocketBands => socketBands;
        public IReadOnlyList<GeneratedMicroChunkSocketSignature> SideSignatures => sideSignatures;
        public GeneratedMicroChunkTraversalSummary TraversalSummary { get; }
        public GeneratedMicroChunkSocketSignature Signature { get; }
        public int CellCount => cells.Count;
        public int LayerRecordCount => cells.Sum(value => value.LayerCount);
        public int CompareTo(GeneratedMicroChunkSliceRecord other) =>
            other == null ? -1 : ChunkIndex.CompareTo(other.ChunkIndex);
        public IReadOnlyList<GeneratedMicroChunkSocketBand> Bands(
            GeneratedMicroChunkSocketSide side) => new ReadOnlyCollection<GeneratedMicroChunkSocketBand>(
                socketBands.Where(value => value.Side == side).OrderBy(value => value).ToArray());
        public GeneratedMicroChunkSocketSignature SideSignature(
            GeneratedMicroChunkSocketSide side) => sideSignatures.Single(value => value.Side == side);
    }

    public sealed class GeneratedMicroChunkSliceBuildRequest
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkCellSource> cellSources;
        private readonly ReadOnlyCollection<SectorTileCoordinate> forcedSocketCoordinates;

        public GeneratedMicroChunkSliceBuildRequest(
            SectorFinalCanvasLayerPlan canvasPlan,
            SectorCanvasProtectionDensityReport protectionDensityReport,
            SectorFinalRouteRecoveryReport routeRecoveryReport,
            SectorPatternChunkPartition partition,
            IEnumerable<GeneratedMicroChunkCellSource> sourceCells,
            IEnumerable<SectorTileCoordinate> sourceForcedSocketCoordinates = null,
            bool rotateNinetyDegrees = false,
            int markerSlotRecordCount = 0,
            int stableSpawnIdCount = 0,
            int tilemapBakeCount = 0,
            int generatedFileWriteCount = 0,
            int generatedAssetWriteCount = 0,
            int tilemapMutationCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int gameplaySpawnCount = 0,
            int playerPhysicsSimulationCount = 0,
            int sectorRerenderCount = 0,
            int sectorRerollCount = 0,
            int fallbackCarveCount = 0,
            int silentWideningCount = 0,
            int fullRegressionCount = 0,
            int productionSeedApprovalCount = 0)
        {
            CanvasPlan = canvasPlan;
            ProtectionDensityReport = protectionDensityReport;
            RouteRecoveryReport = routeRecoveryReport;
            Partition = partition;
            var rawCells = (sourceCells ?? Array.Empty<GeneratedMicroChunkCellSource>()).ToArray();
            NullCellSourceCount = rawCells.Count(value => value == null);
            cellSources = new ReadOnlyCollection<GeneratedMicroChunkCellSource>(rawCells
                .Where(value => value != null).ToArray());
            var rawForced = (sourceForcedSocketCoordinates ??
                Array.Empty<SectorTileCoordinate>()).ToArray();
            NullForcedSocketCoordinateCount = rawForced.Count(value => value == null);
            forcedSocketCoordinates = new ReadOnlyCollection<SectorTileCoordinate>(rawForced
                .Where(value => value != null).OrderBy(value => value).ToArray());
            RotateNinetyDegrees = rotateNinetyDegrees;
            MarkerSlotRecordCount = markerSlotRecordCount;
            StableSpawnIdCount = stableSpawnIdCount;
            TilemapBakeCount = tilemapBakeCount;
            GeneratedFileWriteCount = generatedFileWriteCount;
            GeneratedAssetWriteCount = generatedAssetWriteCount;
            TilemapMutationCount = tilemapMutationCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            GameplaySpawnCount = gameplaySpawnCount;
            PlayerPhysicsSimulationCount = playerPhysicsSimulationCount;
            SectorRerenderCount = sectorRerenderCount;
            SectorRerollCount = sectorRerollCount;
            FallbackCarveCount = fallbackCarveCount;
            SilentWideningCount = silentWideningCount;
            FullRegressionCount = fullRegressionCount;
            ProductionSeedApprovalCount = productionSeedApprovalCount;
            CanonicalDigest = GeneratedMicroChunkSliceDigest.ComputeInput(this);
        }

        public SectorFinalCanvasLayerPlan CanvasPlan { get; }
        public SectorCanvasProtectionDensityReport ProtectionDensityReport { get; }
        public SectorFinalRouteRecoveryReport RouteRecoveryReport { get; }
        public SectorPatternChunkPartition Partition { get; }
        public IReadOnlyList<GeneratedMicroChunkCellSource> CellSources => cellSources;
        public IReadOnlyList<SectorTileCoordinate> ForcedSocketCoordinates => forcedSocketCoordinates;
        public int NullCellSourceCount { get; }
        public int NullForcedSocketCoordinateCount { get; }
        public bool RotateNinetyDegrees { get; }
        public int MarkerSlotRecordCount { get; }
        public int StableSpawnIdCount { get; }
        public int TilemapBakeCount { get; }
        public int GeneratedFileWriteCount { get; }
        public int GeneratedAssetWriteCount { get; }
        public int TilemapMutationCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int GameplaySpawnCount { get; }
        public int PlayerPhysicsSimulationCount { get; }
        public int SectorRerenderCount { get; }
        public int SectorRerollCount { get; }
        public int FallbackCarveCount { get; }
        public int SilentWideningCount { get; }
        public int FullRegressionCount { get; }
        public int ProductionSeedApprovalCount { get; }
        public int ForbiddenOperationCount => MarkerSlotRecordCount + StableSpawnIdCount +
            TilemapBakeCount + GeneratedFileWriteCount + GeneratedAssetWriteCount +
            TilemapMutationCount + SceneMutationCount + PrefabMutationCount +
            GameObjectMutationCount + GameplaySpawnCount + PlayerPhysicsSimulationCount +
            SectorRerenderCount + SectorRerollCount + FallbackCarveCount +
            SilentWideningCount + FullRegressionCount + ProductionSeedApprovalCount;
        public string CanonicalDigest { get; }

        public static GeneratedMicroChunkSliceBuildRequest FromAuthorities(
            SectorFinalCanvasLayerPlan canvasPlan,
            SectorCanvasProtectionDensityReport protectionDensityReport,
            SectorFinalRouteRecoveryReport routeRecoveryReport,
            SectorPatternChunkPartition partition)
        {
            var planCells = canvasPlan == null
                ? new Dictionary<FinalCanvasCellCoordinate, FinalCanvasCell>()
                : canvasPlan.Cells.ToDictionary(value => value.Coordinate);
            var projections = partition == null
                ? Array.Empty<RouteRecoveryWitnessChunkProjection>()
                : partition.WitnessProjections.ToArray();
            var sources = partition == null
                ? Array.Empty<GeneratedMicroChunkCellSource>()
                : partition.TileAddresses.OrderBy(value => value).Select(address =>
                {
                    FinalCanvasCell cell;
                    planCells.TryGetValue(new FinalCanvasCellCoordinate(
                        address.SectorCoordinate.X, address.SectorCoordinate.Y), out cell);
                    var layers = cell == null
                        ? Array.Empty<GeneratedMicroChunkLayerRecord>()
                        : cell.Winners.Select(value => new GeneratedMicroChunkLayerRecord(
                            value.Layer, value.CellKind, value.SourceOwner, value.ProvenanceId,
                            value.Protection, value.IsProtected, value.ClaimId, cell.StableToken)).ToArray();
                    var memberships = projections.Where(value =>
                        value.Address.SectorCoordinate.Equals(address.SectorCoordinate))
                        .Select(value => new GeneratedMicroChunkWitnessMembership(
                            value.WitnessKind, value.SourceStableId, value.PathIndex)).ToArray();
                    return new GeneratedMicroChunkCellSource(address, layers, memberships);
                }).ToArray();
            return new GeneratedMicroChunkSliceBuildRequest(
                canvasPlan, protectionDensityReport, routeRecoveryReport, partition, sources);
        }
    }

    public sealed class GeneratedMicroChunkSliceSet
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkSliceRecord> slices;

        internal GeneratedMicroChunkSliceSet(
            GeneratedMicroChunkSliceBuildRequest request,
            IEnumerable<GeneratedMicroChunkSliceRecord> sourceSlices)
        {
            Request = request;
            slices = new ReadOnlyCollection<GeneratedMicroChunkSliceRecord>((sourceSlices ??
                Array.Empty<GeneratedMicroChunkSliceRecord>()).OrderBy(value => value).ToArray());
            OutputDigest = GeneratedMicroChunkSliceDigest.ComputeOutput(this);
        }

        public const int SectorWidth = SectorPatternChunkPartition.SectorWidth;
        public const int SectorHeight = SectorPatternChunkPartition.SectorHeight;
        public const int SectorCellCount = SectorPatternChunkPartition.SectorCellCount;
        public const int MicroChunkWidth = SectorPatternChunkPartition.MicroChunkWidth;
        public const int MicroChunkHeight = SectorPatternChunkPartition.MicroChunkHeight;
        public const int MicroChunkCellCount = SectorPatternChunkPartition.ChunkCellCount;
        public const int ChunkGridWidth = SectorPatternChunkPartition.ChunkGridWidth;
        public const int ChunkGridHeight = SectorPatternChunkPartition.ChunkGridHeight;
        public const int ChunkCount = SectorPatternChunkPartition.ChunkCount;
        public const int MicroPatternWidth = SectorPatternChunkPartition.MicroPatternWidth;
        public const int MicroPatternHeight = SectorPatternChunkPartition.MicroPatternHeight;
        public const int ChunkPatternGridWidth = SectorPatternChunkPartition.ChunkPatternGridWidth;
        public const int ChunkPatternGridHeight = SectorPatternChunkPartition.ChunkPatternGridHeight;
        public const int LayerKindsPerCell = SectorFinalCanvasLayerPlan.RequiredLayerCount;
        public const bool ChunkRotationAllowed = false;
        public const string PolicyVersion = "MAP16_05_GENERATED_MICROCHUNK_SLICE_POLICY_V1";
        public const string DownstreamOwner = "MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE";
        public const bool OpensDownstreamTask = false;

        public GeneratedMicroChunkSliceBuildRequest Request { get; }
        public SectorFinalCanvasLayerPlan SourceCanvasPlan => Request.CanvasPlan;
        public SectorCanvasProtectionDensityReport SourceProtectionDensityReport =>
            Request.ProtectionDensityReport;
        public SectorFinalRouteRecoveryReport SourceRouteRecoveryReport => Request.RouteRecoveryReport;
        public SectorPatternChunkPartition SourcePartition => Request.Partition;
        public IReadOnlyList<GeneratedMicroChunkSliceRecord> Slices => slices;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int SliceCount => slices.Count;
        public int TotalCellCount => slices.Sum(value => value.CellCount);
        public int UniqueSectorCellCount => slices.SelectMany(value => value.Cells)
            .Select(value => value.SectorCoordinate).Distinct().Count();
        public int DuplicateSectorCellCount => TotalCellCount - UniqueSectorCellCount;
        public int MissingSectorCellCount => SectorCellCount - UniqueSectorCellCount;
        public int OutOfBoundsSectorCellCount => slices.SelectMany(value => value.Cells)
            .Count(value => !value.SectorCoordinate.IsInBounds || !value.LocalCoordinate.IsInBounds);
        public int TotalLayerRecordCount => slices.Sum(value => value.LayerRecordCount);
        public int LayerRecordsWithSourceOwnerCount => slices.SelectMany(value => value.Cells)
            .SelectMany(value => value.Layers).Count(value => value.SourceOwner != FinalCanvasSourceOwner.Unknown);
        public int LayerRecordsWithProvenanceCount => slices.SelectMany(value => value.Cells)
            .SelectMany(value => value.Layers).Count(value => !string.IsNullOrEmpty(value.ProvenanceId));
        public int WitnessMembershipCount => slices.SelectMany(value => value.Cells)
            .Sum(value => value.WitnessMembershipCount);
        public int WitnessMemberCellCount => slices.SelectMany(value => value.Cells)
            .Count(value => value.WitnessMembershipCount > 0);
        public int SocketBandCount => slices.Sum(value => value.SocketBands.Count);
        public int SocketSideSignatureCount => slices.Sum(value => value.SideSignatures.Count);
        public int SocketBandsOnBlockedCellsCount => slices.SelectMany(value => value.SocketBands)
            .Count(value => value.Cells.Any(cell => !cell.IsPassable));
        public int InvalidSideSignatureCount => slices.SelectMany(value => value.SideSignatures)
            .Count(value => !GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.Digest));
        public int InvalidSliceSignatureCount => slices.Count(value => value.Signature == null ||
            !GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.Signature.Digest));
        public int MissingTraversalSummaryCount => slices.Count(value => value.TraversalSummary == null);
        public int MissingPassableComponentSummaryCount => slices.Count(value =>
            value.TraversalSummary == null || value.TraversalSummary.ConnectedPassableComponentCount < 0);
        public int RotationRequestCount => Request.RotateNinetyDegrees ? 1 : 0;
        public int MarkerSlotRecordCount => Request.MarkerSlotRecordCount;
        public int StableSpawnIdCount => Request.StableSpawnIdCount;
        public int TilemapBakeCount => Request.TilemapBakeCount;
        public int GeneratedFileWriteCount => Request.GeneratedFileWriteCount;
        public int GeneratedAssetWriteCount => Request.GeneratedAssetWriteCount;
        public int TilemapMutationCount => Request.TilemapMutationCount;
        public int SceneMutationCount => Request.SceneMutationCount;
        public int PrefabMutationCount => Request.PrefabMutationCount;
        public int GameObjectMutationCount => Request.GameObjectMutationCount;
        public int GameplaySpawnCount => Request.GameplaySpawnCount;
        public int PlayerPhysicsSimulationCount => Request.PlayerPhysicsSimulationCount;
        public int SectorRerenderCount => Request.SectorRerenderCount;
        public int SectorRerollCount => Request.SectorRerollCount;
        public int FallbackCarveCount => Request.FallbackCarveCount;
        public int SilentWideningCount => Request.SilentWideningCount;
        public int FullRegressionCount => Request.FullRegressionCount;
        public int ProductionSeedApprovalCount => Request.ProductionSeedApprovalCount;
    }

    public enum GeneratedMicroChunkSliceFailureCode
    {
        MissingRequest = 1,
        MissingCanvasPlan = 2,
        InvalidCanvasPlan = 3,
        MissingProtectionDensityReport = 4,
        InvalidProtectionDensityReport = 5,
        MissingRouteRecoveryReport = 6,
        InvalidRouteRecoveryReport = 7,
        MissingPartition = 8,
        InvalidPartition = 9,
        SourceMismatch = 10,
        InvalidCellCount = 11,
        DuplicateCoordinate = 12,
        MissingCoordinate = 13,
        OutOfBoundsCoordinate = 14,
        InvalidLayerCount = 15,
        MissingSourceOwner = 16,
        MissingProvenance = 17,
        LayerCopyMismatch = 18,
        WitnessCopyMismatch = 19,
        BlockedSocketCell = 20,
        InvalidSocketSignature = 21,
        InvalidTraversalSummary = 22,
        RotationForbidden = 23,
        InvalidDigest = 24,
        ForbiddenOperation = 25,
    }

    public sealed class GeneratedMicroChunkSliceFailure :
        IComparable<GeneratedMicroChunkSliceFailure>, IEquatable<GeneratedMicroChunkSliceFailure>
    {
        public GeneratedMicroChunkSliceFailure(
            GeneratedMicroChunkSliceFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedMicroChunkSliceFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public int CompareTo(GeneratedMicroChunkSliceFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedMicroChunkSliceFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedMicroChunkSliceFailure);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Subject);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Reason);
            }
        }
        public override string ToString() => Code + ":" + Subject + ":" + Reason;
    }

    public sealed class GeneratedMicroChunkSliceResult
    {
        private readonly ReadOnlyCollection<GeneratedMicroChunkSliceFailure> failures;

        internal GeneratedMicroChunkSliceResult(
            GeneratedMicroChunkSliceBuildRequest request,
            GeneratedMicroChunkSliceSet sliceSet,
            IEnumerable<GeneratedMicroChunkSliceFailure> sourceFailures)
        {
            Request = request;
            SliceSet = sliceSet;
            failures = new ReadOnlyCollection<GeneratedMicroChunkSliceFailure>((sourceFailures ??
                Array.Empty<GeneratedMicroChunkSliceFailure>()).Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => SliceSet != null && failures.Count == 0;
        public GeneratedMicroChunkSliceBuildRequest Request { get; }
        public GeneratedMicroChunkSliceSet SliceSet { get; }
        public IReadOnlyList<GeneratedMicroChunkSliceFailure> Failures => failures;
        public string InputDigest => SliceSet == null
            ? (Request == null ? string.Empty : Request.CanonicalDigest)
            : SliceSet.InputDigest;
        public string OutputDigest => SliceSet == null ? string.Empty : SliceSet.OutputDigest;
    }

    public static class GeneratedMicroChunkSliceDigest
    {
        public static string ComputeInput(GeneratedMicroChunkSliceBuildRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedMicroChunkSliceSet.PolicyVersion,
                "CANVAS|" + Digest(request.CanvasPlan, true) + "|" + Digest(request.CanvasPlan, false),
                "DENSITY|" + Digest(request.ProtectionDensityReport, true) + "|" +
                    Digest(request.ProtectionDensityReport, false),
                "ROUTE|" + Digest(request.RouteRecoveryReport, true) + "|" +
                    Digest(request.RouteRecoveryReport, false),
                "PARTITION|" + Digest(request.Partition, true) + "|" + Digest(request.Partition, false),
                "CONSTANTS|48|32|1536|12|8|96|4|4|16|4|4|3|2|7|ROTATION|" +
                    (request.RotateNinetyDegrees ? "1" : "0"),
                "NULLS|" + Number(request.NullCellSourceCount) + "|" +
                    Number(request.NullForcedSocketCoordinateCount),
                "OPERATIONS|" + string.Join("|", OperationCounts(request)),
            };
            lines.AddRange(request.CellSources.OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange(request.ForcedSocketCoordinates.OrderBy(value => value)
                .Select(value => "FORCED_SOCKET|" + value));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(GeneratedMicroChunkSliceSet set)
        {
            if (set == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedMicroChunkSliceSet.PolicyVersion,
                "INPUT|" + set.InputDigest,
                "COUNTS|" + string.Join("|", new[]
                {
                    Number(set.SliceCount), Number(set.TotalCellCount),
                    Number(set.UniqueSectorCellCount), Number(set.TotalLayerRecordCount),
                    Number(set.WitnessMembershipCount), Number(set.SocketBandCount),
                    Number(set.SocketSideSignatureCount),
                }),
                "VALIDATION|" + string.Join("|", new[]
                {
                    Number(set.DuplicateSectorCellCount), Number(set.MissingSectorCellCount),
                    Number(set.OutOfBoundsSectorCellCount), Number(set.SocketBandsOnBlockedCellsCount),
                    Number(set.InvalidSideSignatureCount), Number(set.InvalidSliceSignatureCount),
                    Number(set.MissingTraversalSummaryCount),
                    Number(set.MissingPassableComponentSummaryCount), Number(set.RotationRequestCount),
                }),
                "DOWNSTREAM|" + GeneratedMicroChunkSliceSet.DownstreamOwner + "|" +
                    (GeneratedMicroChunkSliceSet.OpensDownstreamTask ? "1" : "0"),
            };
            foreach (var slice in set.Slices.OrderBy(value => value))
            {
                lines.Add("SLICE|" + slice.Id + "|" + slice.SectorOrigin + "|" +
                    slice.Signature.Digest);
                lines.AddRange(slice.Cells.OrderBy(value => value)
                    .Select(value => value.StableToken));
                lines.AddRange(slice.SocketBands.OrderBy(value => value)
                    .Select(value => value.StableToken));
                lines.AddRange(slice.SideSignatures.OrderBy(value => value)
                    .Select(value => "SIDE_SIGNATURE|" + value.Side.Value.ToString().ToUpperInvariant() +
                        "|" + value.Digest));
                lines.Add(slice.TraversalSummary.StableToken);
            }
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeSideSignature(
            GeneratedMicroChunkSocketSide side,
            IEnumerable<GeneratedMicroChunkCell> edgeCells,
            IEnumerable<GeneratedMicroChunkSocketBand> bands)
        {
            var lines = new List<string> { "SIDE|" + side.ToString().ToUpperInvariant() };
            lines.AddRange((edgeCells ?? Array.Empty<GeneratedMicroChunkCell>())
                .OrderBy(value => GeneratedMicroChunkSocketBand.EdgePositionStatic(side, value))
                .Select(value => "EDGE|" + value.LocalCoordinate + "|" +
                    (value.IsPassable ? "OPEN" : "BLOCKED") + "|" + value.StableToken));
            lines.AddRange((bands ?? Array.Empty<GeneratedMicroChunkSocketBand>())
                .OrderBy(value => value).Select(value => value.StableToken));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeSliceSignature(
            IEnumerable<GeneratedMicroChunkCell> cells,
            IEnumerable<GeneratedMicroChunkSocketBand> bands,
            IEnumerable<GeneratedMicroChunkSocketSignature> sideSignatures,
            GeneratedMicroChunkTraversalSummary traversal)
        {
            var lines = new List<string> { "SLICE_SIGNATURE" };
            lines.AddRange((cells ?? Array.Empty<GeneratedMicroChunkCell>()).OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange((bands ?? Array.Empty<GeneratedMicroChunkSocketBand>()).OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange((sideSignatures ?? Array.Empty<GeneratedMicroChunkSocketSignature>())
                .OrderBy(value => value).Select(value => value.Side.Value.ToString().ToUpperInvariant() +
                    "|" + value.Digest));
            lines.Add(traversal == null ? "MISSING_TRAVERSAL" : traversal.StableToken);
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string text)
        {
            var canonical = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(canonical))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        public static bool IsLowerHexSha256(string value) => value != null && value.Length == 64 &&
            value.All(character => (character >= '0' && character <= '9') ||
                                   (character >= 'a' && character <= 'f'));
        private static string Digest(SectorFinalCanvasLayerPlan value, bool input) =>
            value == null ? string.Empty : (input ? value.InputDigest : value.OutputDigest);
        private static string Digest(SectorCanvasProtectionDensityReport value, bool input) =>
            value == null ? string.Empty : (input ? value.InputDigest : value.OutputDigest);
        private static string Digest(SectorFinalRouteRecoveryReport value, bool input) =>
            value == null ? string.Empty : (input ? value.InputDigest : value.OutputDigest);
        private static string Digest(SectorPatternChunkPartition value, bool input) =>
            value == null ? string.Empty : (input ? value.InputDigest : value.OutputDigest);
        private static string[] OperationCounts(GeneratedMicroChunkSliceBuildRequest request) => new[]
        {
            Number(request.MarkerSlotRecordCount), Number(request.StableSpawnIdCount),
            Number(request.TilemapBakeCount), Number(request.GeneratedFileWriteCount),
            Number(request.GeneratedAssetWriteCount), Number(request.TilemapMutationCount),
            Number(request.SceneMutationCount), Number(request.PrefabMutationCount),
            Number(request.GameObjectMutationCount), Number(request.GameplaySpawnCount),
            Number(request.PlayerPhysicsSimulationCount), Number(request.SectorRerenderCount),
            Number(request.SectorRerollCount), Number(request.FallbackCarveCount),
            Number(request.SilentWideningCount), Number(request.FullRegressionCount),
            Number(request.ProductionSeedApprovalCount),
        };
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
