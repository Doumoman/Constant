using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class SectorTileCoordinate :
        IEquatable<SectorTileCoordinate>, IComparable<SectorTileCoordinate>
    {
        public SectorTileCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < SectorPatternChunkPartition.SectorWidth &&
                                  Y >= 0 && Y < SectorPatternChunkPartition.SectorHeight;
        public int RowMajorIndex => (Y * SectorPatternChunkPartition.SectorWidth) + X;

        public int CompareTo(SectorTileCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(SectorTileCoordinate other) =>
            other != null && X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as SectorTileCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0},{1}", X, Y);
    }

    public sealed class MicroPatternCoordinate :
        IEquatable<MicroPatternCoordinate>, IComparable<MicroPatternCoordinate>
    {
        public MicroPatternCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < SectorPatternChunkPartition.SectorPatternGridWidth &&
                                  Y >= 0 && Y < SectorPatternChunkPartition.SectorPatternGridHeight;
        public int RowMajorIndex => (Y * SectorPatternChunkPartition.SectorPatternGridWidth) + X;

        public int CompareTo(MicroPatternCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(MicroPatternCoordinate other) =>
            other != null && X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as MicroPatternCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0},{1}", X, Y);
    }

    public sealed class MicroPatternLocalCellCoordinate :
        IEquatable<MicroPatternLocalCellCoordinate>, IComparable<MicroPatternLocalCellCoordinate>
    {
        public MicroPatternLocalCellCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < SectorPatternChunkPartition.MicroPatternWidth &&
                                  Y >= 0 && Y < SectorPatternChunkPartition.MicroPatternHeight;

        public int CompareTo(MicroPatternLocalCellCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(MicroPatternLocalCellCoordinate other) =>
            other != null && X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as MicroPatternLocalCellCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0},{1}", X, Y);
    }

    public sealed class MicroChunkCoordinate :
        IEquatable<MicroChunkCoordinate>, IComparable<MicroChunkCoordinate>
    {
        public MicroChunkCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < SectorPatternChunkPartition.ChunkGridWidth &&
                                  Y >= 0 && Y < SectorPatternChunkPartition.ChunkGridHeight;
        public int Index => (Y * SectorPatternChunkPartition.ChunkGridWidth) + X;

        public int CompareTo(MicroChunkCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(MicroChunkCoordinate other) =>
            other != null && X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as MicroChunkCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0},{1}", X, Y);
    }

    public sealed class MicroChunkLocalTileCoordinate :
        IEquatable<MicroChunkLocalTileCoordinate>, IComparable<MicroChunkLocalTileCoordinate>
    {
        public MicroChunkLocalTileCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < SectorPatternChunkPartition.MicroChunkWidth &&
                                  Y >= 0 && Y < SectorPatternChunkPartition.MicroChunkHeight;

        public int CompareTo(MicroChunkLocalTileCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(MicroChunkLocalTileCoordinate other) =>
            other != null && X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as MicroChunkLocalTileCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0},{1}", X, Y);
    }

    public sealed class MicroChunkLocalPatternCoordinate :
        IEquatable<MicroChunkLocalPatternCoordinate>, IComparable<MicroChunkLocalPatternCoordinate>
    {
        public MicroChunkLocalPatternCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < SectorPatternChunkPartition.ChunkPatternGridWidth &&
                                  Y >= 0 && Y < SectorPatternChunkPartition.ChunkPatternGridHeight;

        public int CompareTo(MicroChunkLocalPatternCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(MicroChunkLocalPatternCoordinate other) =>
            other != null && X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as MicroChunkLocalPatternCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0},{1}", X, Y);
    }

    public sealed class PatternChunkCellAddress : IComparable<PatternChunkCellAddress>
    {
        internal PatternChunkCellAddress(
            SectorTileCoordinate sectorCoordinate,
            MicroChunkCoordinate chunkCoordinate,
            MicroChunkLocalTileCoordinate localTileCoordinate,
            MicroPatternCoordinate patternCoordinate,
            MicroPatternLocalCellCoordinate localPatternCellCoordinate)
        {
            SectorCoordinate = sectorCoordinate;
            ChunkCoordinate = chunkCoordinate;
            LocalTileCoordinate = localTileCoordinate;
            PatternCoordinate = patternCoordinate;
            LocalPatternCellCoordinate = localPatternCellCoordinate;
            SectorRoundTripCoordinate = new SectorTileCoordinate(
                (chunkCoordinate.X * SectorPatternChunkPartition.MicroChunkWidth) +
                localTileCoordinate.X,
                (chunkCoordinate.Y * SectorPatternChunkPartition.MicroChunkHeight) +
                localTileCoordinate.Y);
            PatternCellRoundTripCoordinate = new SectorTileCoordinate(
                (patternCoordinate.X * SectorPatternChunkPartition.MicroPatternWidth) +
                localPatternCellCoordinate.X,
                (patternCoordinate.Y * SectorPatternChunkPartition.MicroPatternHeight) +
                localPatternCellCoordinate.Y);
            StableToken = string.Join("|", new[]
            {
                "TILE", SectorCoordinate.ToString(), Number(ChunkIndex),
                ChunkCoordinate.ToString(), LocalTileCoordinate.ToString(),
                PatternCoordinate.ToString(), LocalPatternCellCoordinate.ToString(),
            });
        }

        public SectorTileCoordinate SectorCoordinate { get; }
        public MicroChunkCoordinate ChunkCoordinate { get; }
        public int ChunkIndex => ChunkCoordinate.Index;
        public MicroChunkLocalTileCoordinate LocalTileCoordinate { get; }
        public SectorTileCoordinate SectorRoundTripCoordinate { get; }
        public MicroPatternCoordinate PatternCoordinate { get; }
        public MicroPatternLocalCellCoordinate LocalPatternCellCoordinate { get; }
        public SectorTileCoordinate PatternCellRoundTripCoordinate { get; }
        public bool TileRoundTripMatches => SectorCoordinate.Equals(SectorRoundTripCoordinate);
        public bool PatternCellRoundTripMatches =>
            SectorCoordinate.Equals(PatternCellRoundTripCoordinate);
        public string StableToken { get; }

        public int CompareTo(PatternChunkCellAddress other) => other == null
            ? -1
            : SectorCoordinate.CompareTo(other.SectorCoordinate);

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class PatternChunkPatternAddress : IComparable<PatternChunkPatternAddress>
    {
        internal PatternChunkPatternAddress(
            MicroPatternCoordinate sectorPatternCoordinate,
            MicroChunkCoordinate chunkCoordinate,
            MicroChunkLocalPatternCoordinate localPatternCoordinate)
        {
            SectorPatternCoordinate = sectorPatternCoordinate;
            ChunkCoordinate = chunkCoordinate;
            LocalPatternCoordinate = localPatternCoordinate;
            SectorPatternRoundTripCoordinate = new MicroPatternCoordinate(
                (chunkCoordinate.X * SectorPatternChunkPartition.ChunkPatternGridWidth) +
                localPatternCoordinate.X,
                (chunkCoordinate.Y * SectorPatternChunkPartition.ChunkPatternGridHeight) +
                localPatternCoordinate.Y);
            StableToken = string.Join("|", new[]
            {
                "PATTERN", SectorPatternCoordinate.ToString(), Number(ChunkIndex),
                ChunkCoordinate.ToString(), LocalPatternCoordinate.ToString(),
            });
        }

        public MicroPatternCoordinate SectorPatternCoordinate { get; }
        public MicroChunkCoordinate ChunkCoordinate { get; }
        public int ChunkIndex => ChunkCoordinate.Index;
        public MicroChunkLocalPatternCoordinate LocalPatternCoordinate { get; }
        public MicroPatternCoordinate SectorPatternRoundTripCoordinate { get; }
        public bool RoundTripMatches =>
            SectorPatternCoordinate.Equals(SectorPatternRoundTripCoordinate);
        public string StableToken { get; }

        public int CompareTo(PatternChunkPatternAddress other) => other == null
            ? -1
            : SectorPatternCoordinate.CompareTo(other.SectorPatternCoordinate);

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class RouteRecoveryWitnessChunkProjection :
        IComparable<RouteRecoveryWitnessChunkProjection>
    {
        internal RouteRecoveryWitnessChunkProjection(
            string witnessKind,
            string sourceStableId,
            int pathIndex,
            PatternChunkCellAddress address)
        {
            WitnessKind = witnessKind ?? string.Empty;
            SourceStableId = sourceStableId ?? string.Empty;
            PathIndex = pathIndex;
            Address = address;
            StableToken = string.Join("|", new[]
            {
                "WITNESS", WitnessKind, SourceStableId,
                Address.SectorCoordinate.ToString(), Number(PathIndex),
                Number(Address.ChunkIndex), Address.LocalTileCoordinate.ToString(),
            });
        }

        public string WitnessKind { get; }
        public string SourceStableId { get; }
        public int PathIndex { get; }
        public PatternChunkCellAddress Address { get; }
        public string StableToken { get; }

        public int CompareTo(RouteRecoveryWitnessChunkProjection other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(WitnessKind, other.WitnessKind, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SourceStableId, other.SourceStableId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = Address.SectorCoordinate.CompareTo(other.Address.SectorCoordinate);
            return comparison != 0 ? comparison : PathIndex.CompareTo(other.PathIndex);
        }

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class MicroChunkSlot : IComparable<MicroChunkSlot>
    {
        private readonly ReadOnlyCollection<PatternChunkCellAddress> tileAddresses;
        private readonly ReadOnlyCollection<PatternChunkPatternAddress> patternAddresses;

        internal MicroChunkSlot(
            MicroChunkCoordinate coordinate,
            IEnumerable<PatternChunkCellAddress> sourceTileAddresses,
            IEnumerable<PatternChunkPatternAddress> sourcePatternAddresses)
        {
            Coordinate = coordinate;
            Index = coordinate.Index;
            Origin = new SectorTileCoordinate(
                coordinate.X * SectorPatternChunkPartition.MicroChunkWidth,
                coordinate.Y * SectorPatternChunkPartition.MicroChunkHeight);
            tileAddresses = ReadOnlySorted(sourceTileAddresses);
            patternAddresses = ReadOnlySorted(sourcePatternAddresses);
            StableToken = string.Join("|", new[]
            {
                "CHUNK", Number(Index), Coordinate.ToString(), Origin.ToString(),
                Number(Width), Number(Height), Number(tileAddresses.Count),
                Number(patternAddresses.Count), "ROTATION|0",
            });
        }

        public MicroChunkCoordinate Coordinate { get; }
        public int Index { get; }
        public int ChunkX => Coordinate.X;
        public int ChunkY => Coordinate.Y;
        public SectorTileCoordinate Origin { get; }
        public int MinX => Origin.X;
        public int MinY => Origin.Y;
        public int Width => SectorPatternChunkPartition.MicroChunkWidth;
        public int Height => SectorPatternChunkPartition.MicroChunkHeight;
        public int MaxXExclusive => MinX + Width;
        public int MaxYExclusive => MinY + Height;
        public IReadOnlyList<PatternChunkCellAddress> TileAddresses => tileAddresses;
        public IReadOnlyList<PatternChunkPatternAddress> PatternAddresses => patternAddresses;
        public int TileCount => tileAddresses.Count;
        public int PatternCount => patternAddresses.Count;
        public bool RotationAllowed => false;
        public string StableToken { get; }
        public int CompareTo(MicroChunkSlot other) =>
            other == null ? -1 : Index.CompareTo(other.Index);

        private static ReadOnlyCollection<T> ReadOnlySorted<T>(IEnumerable<T> source)
            where T : IComparable<T> => new ReadOnlyCollection<T>((source ?? Array.Empty<T>())
                .OrderBy(value => value).ToArray());
        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }

    public enum PatternChunkPartitionFailureCode
    {
        MissingRequest = 1,
        MissingCanvasPlan = 2,
        InvalidCanvasPlan = 3,
        MissingProtectionDensityReport = 4,
        InvalidProtectionDensityReport = 5,
        MissingRouteRecoveryReport = 6,
        InvalidRouteRecoveryReport = 7,
        SourceMismatch = 8,
        InvalidDimensions = 9,
        InvalidCellCount = 10,
        NonDivisibleConstants = 11,
        DuplicateTileCoordinate = 12,
        MissingTileCoordinate = 13,
        OutOfBoundsTileCoordinate = 14,
        DuplicatePatternCoordinate = 15,
        MissingPatternCoordinate = 16,
        OutOfBoundsPatternCoordinate = 17,
        ChunkIndexMismatch = 18,
        TileRoundTripMismatch = 19,
        PatternRoundTripMismatch = 20,
        RotationForbidden = 21,
        InvalidDigest = 22,
        ForbiddenOperation = 23,
    }

    public sealed class PatternChunkPartitionFailure :
        IComparable<PatternChunkPartitionFailure>, IEquatable<PatternChunkPartitionFailure>
    {
        public PatternChunkPartitionFailure(
            PatternChunkPartitionFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public PatternChunkPartitionFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }

        public int CompareTo(PatternChunkPartitionFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(PatternChunkPartitionFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as PatternChunkPartitionFailure);
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Code;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Subject);
                return (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason);
            }
        }
        public override string ToString() => Code + ":" + Subject + ":" + Reason;
    }

    public sealed class PatternChunkPartitionRequest
    {
        private readonly ReadOnlyCollection<SectorTileCoordinate> tileCoordinates;
        private readonly ReadOnlyCollection<MicroPatternCoordinate> patternCoordinates;

        public PatternChunkPartitionRequest(
            SectorFinalCanvasLayerPlan canvasPlan,
            SectorCanvasProtectionDensityReport protectionDensityReport,
            SectorFinalRouteRecoveryReport routeRecoveryReport,
            IEnumerable<SectorTileCoordinate> sourceTileCoordinates,
            IEnumerable<MicroPatternCoordinate> sourcePatternCoordinates,
            int sectorWidth = SectorPatternChunkPartition.SectorWidth,
            int sectorHeight = SectorPatternChunkPartition.SectorHeight,
            int microPatternWidth = SectorPatternChunkPartition.MicroPatternWidth,
            int microPatternHeight = SectorPatternChunkPartition.MicroPatternHeight,
            int microChunkWidth = SectorPatternChunkPartition.MicroChunkWidth,
            int microChunkHeight = SectorPatternChunkPartition.MicroChunkHeight,
            bool rotateNinetyDegrees = false,
            int layerCopyCount = 0,
            int sliceRecordCreationCount = 0,
            int socketDerivationCount = 0,
            int tilemapBakeCount = 0,
            int generatedFileWriteCount = 0,
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
            var rawTiles = (sourceTileCoordinates ?? Array.Empty<SectorTileCoordinate>()).ToArray();
            NullTileCoordinateCount = rawTiles.Count(value => value == null);
            tileCoordinates = new ReadOnlyCollection<SectorTileCoordinate>(rawTiles
                .Where(value => value != null).ToArray());
            var rawPatterns = (sourcePatternCoordinates ?? Array.Empty<MicroPatternCoordinate>()).ToArray();
            NullPatternCoordinateCount = rawPatterns.Count(value => value == null);
            patternCoordinates = new ReadOnlyCollection<MicroPatternCoordinate>(rawPatterns
                .Where(value => value != null).ToArray());
            SectorWidth = sectorWidth;
            SectorHeight = sectorHeight;
            MicroPatternWidth = microPatternWidth;
            MicroPatternHeight = microPatternHeight;
            MicroChunkWidth = microChunkWidth;
            MicroChunkHeight = microChunkHeight;
            RotateNinetyDegrees = rotateNinetyDegrees;
            LayerCopyCount = layerCopyCount;
            SliceRecordCreationCount = sliceRecordCreationCount;
            SocketDerivationCount = socketDerivationCount;
            TilemapBakeCount = tilemapBakeCount;
            GeneratedFileWriteCount = generatedFileWriteCount;
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
            CanonicalDigest = PatternChunkPartitionDigest.ComputeInput(this);
        }

        public SectorFinalCanvasLayerPlan CanvasPlan { get; }
        public SectorCanvasProtectionDensityReport ProtectionDensityReport { get; }
        public SectorFinalRouteRecoveryReport RouteRecoveryReport { get; }
        public IReadOnlyList<SectorTileCoordinate> TileCoordinates => tileCoordinates;
        public IReadOnlyList<MicroPatternCoordinate> PatternCoordinates => patternCoordinates;
        public int NullTileCoordinateCount { get; }
        public int NullPatternCoordinateCount { get; }
        public int SectorWidth { get; }
        public int SectorHeight { get; }
        public int MicroPatternWidth { get; }
        public int MicroPatternHeight { get; }
        public int MicroChunkWidth { get; }
        public int MicroChunkHeight { get; }
        public bool RotateNinetyDegrees { get; }
        public int LayerCopyCount { get; }
        public int SliceRecordCreationCount { get; }
        public int SocketDerivationCount { get; }
        public int TilemapBakeCount { get; }
        public int GeneratedFileWriteCount { get; }
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
        public int ForbiddenOperationCount => LayerCopyCount + SliceRecordCreationCount +
            SocketDerivationCount + TilemapBakeCount + GeneratedFileWriteCount +
            TilemapMutationCount + SceneMutationCount + PrefabMutationCount +
            GameObjectMutationCount + GameplaySpawnCount + PlayerPhysicsSimulationCount +
            SectorRerenderCount + SectorRerollCount + FallbackCarveCount +
            SilentWideningCount + FullRegressionCount + ProductionSeedApprovalCount;
        public string CanonicalDigest { get; }

        public static PatternChunkPartitionRequest FromAuthorities(
            SectorFinalCanvasLayerPlan canvasPlan,
            SectorCanvasProtectionDensityReport protectionDensityReport,
            SectorFinalRouteRecoveryReport routeRecoveryReport)
        {
            var tiles = canvasPlan == null
                ? Array.Empty<SectorTileCoordinate>()
                : canvasPlan.Cells.Select(value => new SectorTileCoordinate(
                    value.Coordinate.X, value.Coordinate.Y)).ToArray();
            var patterns = Enumerable.Range(0, SectorPatternChunkPartition.SectorPatternCellCount)
                .Select(index => new MicroPatternCoordinate(
                    index % SectorPatternChunkPartition.SectorPatternGridWidth,
                    index / SectorPatternChunkPartition.SectorPatternGridWidth)).ToArray();
            return new PatternChunkPartitionRequest(
                canvasPlan, protectionDensityReport, routeRecoveryReport, tiles, patterns);
        }
    }

    public sealed class SectorPatternChunkPartition
    {
        private readonly ReadOnlyCollection<MicroChunkSlot> chunkSlots;
        private readonly ReadOnlyCollection<PatternChunkCellAddress> tileAddresses;
        private readonly ReadOnlyCollection<PatternChunkPatternAddress> patternAddresses;
        private readonly ReadOnlyCollection<RouteRecoveryWitnessChunkProjection> witnessProjections;

        internal SectorPatternChunkPartition(
            PatternChunkPartitionRequest request,
            IEnumerable<MicroChunkSlot> sourceChunkSlots,
            IEnumerable<PatternChunkCellAddress> sourceTileAddresses,
            IEnumerable<PatternChunkPatternAddress> sourcePatternAddresses,
            IEnumerable<RouteRecoveryWitnessChunkProjection> sourceWitnessProjections)
        {
            Request = request;
            chunkSlots = new ReadOnlyCollection<MicroChunkSlot>((sourceChunkSlots ??
                Array.Empty<MicroChunkSlot>()).OrderBy(value => value).ToArray());
            tileAddresses = new ReadOnlyCollection<PatternChunkCellAddress>((sourceTileAddresses ??
                Array.Empty<PatternChunkCellAddress>()).OrderBy(value => value).ToArray());
            patternAddresses = new ReadOnlyCollection<PatternChunkPatternAddress>((sourcePatternAddresses ??
                Array.Empty<PatternChunkPatternAddress>()).OrderBy(value => value).ToArray());
            witnessProjections = new ReadOnlyCollection<RouteRecoveryWitnessChunkProjection>(
                (sourceWitnessProjections ?? Array.Empty<RouteRecoveryWitnessChunkProjection>())
                .OrderBy(value => value).ToArray());
            OutputDigest = PatternChunkPartitionDigest.ComputeOutput(this);
        }

        public const int SectorWidth = SectorFinalCanvasLayerPlan.SectorWidth;
        public const int SectorHeight = SectorFinalCanvasLayerPlan.SectorHeight;
        public const int SectorCellCount = SectorWidth * SectorHeight;
        public const int MicroPatternWidth = 4;
        public const int MicroPatternHeight = 4;
        public const int SectorPatternGridWidth = SectorWidth / MicroPatternWidth;
        public const int SectorPatternGridHeight = SectorHeight / MicroPatternHeight;
        public const int SectorPatternCellCount = SectorPatternGridWidth * SectorPatternGridHeight;
        public const int MicroChunkWidth = 12;
        public const int MicroChunkHeight = 8;
        public const int ChunkGridWidth = SectorWidth / MicroChunkWidth;
        public const int ChunkGridHeight = SectorHeight / MicroChunkHeight;
        public const int ChunkCount = ChunkGridWidth * ChunkGridHeight;
        public const int ChunkCellCount = MicroChunkWidth * MicroChunkHeight;
        public const int ChunkPatternGridWidth = MicroChunkWidth / MicroPatternWidth;
        public const int ChunkPatternGridHeight = MicroChunkHeight / MicroPatternHeight;
        public const int ChunkPatternCellCount = ChunkPatternGridWidth * ChunkPatternGridHeight;
        public const bool ChunkRotationAllowed = false;
        public const string PolicyVersion = "MAP16_04_PATTERN_CHUNK_PARTITION_POLICY_V1";
        public const string DownstreamOwner = "MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS";
        public const bool OpensDownstreamTask = false;

        public PatternChunkPartitionRequest Request { get; }
        public SectorFinalCanvasLayerPlan SourceCanvasPlan => Request.CanvasPlan;
        public SectorCanvasProtectionDensityReport SourceProtectionDensityReport =>
            Request.ProtectionDensityReport;
        public SectorFinalRouteRecoveryReport SourceRouteRecoveryReport =>
            Request.RouteRecoveryReport;
        public IReadOnlyList<MicroChunkSlot> ChunkSlots => chunkSlots;
        public IReadOnlyList<PatternChunkCellAddress> TileAddresses => tileAddresses;
        public IReadOnlyList<PatternChunkPatternAddress> PatternAddresses => patternAddresses;
        public IReadOnlyList<RouteRecoveryWitnessChunkProjection> WitnessProjections =>
            witnessProjections;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int CoverageCount => tileAddresses.Select(value => value.SectorCoordinate).Distinct().Count();
        public int TileAssignmentCount => tileAddresses.Count;
        public int DuplicateTileAssignmentCount => tileAddresses.Count - CoverageCount;
        public int MissingTileAssignmentCount => SectorCellCount - CoverageCount;
        public int OutOfBoundsTileAssignmentCount => tileAddresses.Count(value =>
            !value.SectorCoordinate.IsInBounds || !value.LocalTileCoordinate.IsInBounds);
        public int PatternCoverageCount => patternAddresses.Select(value =>
            value.SectorPatternCoordinate).Distinct().Count();
        public int PatternAssignmentCount => patternAddresses.Count;
        public int DuplicatePatternAssignmentCount => patternAddresses.Count - PatternCoverageCount;
        public int MissingPatternAssignmentCount => SectorPatternCellCount - PatternCoverageCount;
        public int OutOfBoundsPatternAssignmentCount => patternAddresses.Count(value =>
            !value.SectorPatternCoordinate.IsInBounds || !value.LocalPatternCoordinate.IsInBounds);
        public int ChunkIndexMismatchCount => chunkSlots.Count(value =>
            value.Index != (value.ChunkY * ChunkGridWidth) + value.ChunkX);
        public int TileRoundTripMismatchCount => tileAddresses.Count(value =>
            !value.TileRoundTripMatches);
        public int PatternRoundTripMismatchCount => patternAddresses.Count(value =>
            !value.RoundTripMatches);
        public int LocalPatternCellRoundTripMismatchCount => tileAddresses.Count(value =>
            !value.PatternCellRoundTripMatches);
        public int RotationRequestCount => Request.RotateNinetyDegrees ? 1 : 0;
        public int ExpectedWitnessProjectionCount => SourceRouteRecoveryReport == null ? 0 :
            SourceRouteRecoveryReport.Witnesses.Sum(value => value.Path.Count) +
            SourceRouteRecoveryReport.RecoveryWitnesses.Sum(value => value.Path.Count);
        public int MissingWitnessProjectionCount =>
            ExpectedWitnessProjectionCount - witnessProjections.Count;
        public int LayerCopyCount => Request.LayerCopyCount;
        public int SliceRecordCreationCount => Request.SliceRecordCreationCount;
        public int SocketDerivationCount => Request.SocketDerivationCount;
        public int TilemapBakeCount => Request.TilemapBakeCount;
        public int GeneratedFileWriteCount => Request.GeneratedFileWriteCount;
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

    public sealed class PatternChunkPartitionResult
    {
        private readonly ReadOnlyCollection<PatternChunkPartitionFailure> failures;

        internal PatternChunkPartitionResult(
            PatternChunkPartitionRequest request,
            SectorPatternChunkPartition partition,
            IEnumerable<PatternChunkPartitionFailure> sourceFailures)
        {
            Request = request;
            Partition = partition;
            failures = new ReadOnlyCollection<PatternChunkPartitionFailure>((sourceFailures ??
                Array.Empty<PatternChunkPartitionFailure>()).Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Partition != null && failures.Count == 0;
        public PatternChunkPartitionRequest Request { get; }
        public SectorPatternChunkPartition Partition { get; }
        public IReadOnlyList<PatternChunkPartitionFailure> Failures => failures;
        public string InputDigest => Partition == null
            ? (Request == null ? string.Empty : Request.CanonicalDigest)
            : Partition.InputDigest;
        public string OutputDigest => Partition == null ? string.Empty : Partition.OutputDigest;
    }

    public static class PatternChunkPartitionDigest
    {
        public static string ComputeInput(PatternChunkPartitionRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + SectorPatternChunkPartition.PolicyVersion,
                "CANVAS|" + Digest(request.CanvasPlan, true) + "|" + Digest(request.CanvasPlan, false),
                "DENSITY|" + Digest(request.ProtectionDensityReport, true) + "|" +
                    Digest(request.ProtectionDensityReport, false),
                "ROUTE|" + Digest(request.RouteRecoveryReport, true) + "|" +
                    Digest(request.RouteRecoveryReport, false),
                "CONSTANTS|" + string.Join("|", new[]
                {
                    Number(request.SectorWidth), Number(request.SectorHeight),
                    Number(request.MicroPatternWidth), Number(request.MicroPatternHeight),
                    Number(request.MicroChunkWidth), Number(request.MicroChunkHeight),
                    request.RotateNinetyDegrees ? "1" : "0",
                }),
                "OPERATIONS|" + string.Join("|", OperationCounts(request)),
                "NULLS|" + Number(request.NullTileCoordinateCount) + "|" +
                    Number(request.NullPatternCoordinateCount),
            };
            lines.AddRange(request.TileCoordinates.OrderBy(value => value)
                .Select(value => "INPUT_TILE|" + value));
            lines.AddRange(request.PatternCoordinates.OrderBy(value => value)
                .Select(value => "INPUT_PATTERN|" + value));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(SectorPatternChunkPartition partition)
        {
            if (partition == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + SectorPatternChunkPartition.PolicyVersion,
                "INPUT|" + partition.InputDigest,
                "COUNTS|" + string.Join("|", new[]
                {
                    Number(partition.ChunkSlots.Count), Number(partition.TileAssignmentCount),
                    Number(partition.PatternAssignmentCount), Number(partition.WitnessProjections.Count),
                    Number(partition.CoverageCount), Number(partition.PatternCoverageCount),
                }),
                "VALIDATION|" + string.Join("|", new[]
                {
                    Number(partition.DuplicateTileAssignmentCount),
                    Number(partition.MissingTileAssignmentCount),
                    Number(partition.OutOfBoundsTileAssignmentCount),
                    Number(partition.DuplicatePatternAssignmentCount),
                    Number(partition.MissingPatternAssignmentCount),
                    Number(partition.OutOfBoundsPatternAssignmentCount),
                    Number(partition.ChunkIndexMismatchCount),
                    Number(partition.TileRoundTripMismatchCount),
                    Number(partition.PatternRoundTripMismatchCount),
                    Number(partition.LocalPatternCellRoundTripMismatchCount),
                    Number(partition.RotationRequestCount),
                    Number(partition.MissingWitnessProjectionCount),
                }),
                "DOWNSTREAM|" + SectorPatternChunkPartition.DownstreamOwner + "|" +
                    (SectorPatternChunkPartition.OpensDownstreamTask ? "1" : "0"),
            };
            foreach (var slot in partition.ChunkSlots.OrderBy(value => value))
            {
                lines.Add(slot.StableToken);
                lines.AddRange(slot.TileAddresses.OrderBy(value => value)
                    .Select(value => value.StableToken));
                lines.AddRange(slot.PatternAddresses.OrderBy(value => value)
                    .Select(value => value.StableToken));
            }
            lines.AddRange(partition.WitnessProjections.OrderBy(value => value)
                .Select(value => value.StableToken));
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
        private static string[] OperationCounts(PatternChunkPartitionRequest request) => new[]
        {
            Number(request.LayerCopyCount), Number(request.SliceRecordCreationCount),
            Number(request.SocketDerivationCount), Number(request.TilemapBakeCount),
            Number(request.GeneratedFileWriteCount), Number(request.TilemapMutationCount),
            Number(request.SceneMutationCount), Number(request.PrefabMutationCount),
            Number(request.GameObjectMutationCount), Number(request.GameplaySpawnCount),
            Number(request.PlayerPhysicsSimulationCount), Number(request.SectorRerenderCount),
            Number(request.SectorRerollCount), Number(request.FallbackCarveCount),
            Number(request.SilentWideningCount), Number(request.FullRegressionCount),
            Number(request.ProductionSeedApprovalCount),
        };
        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }
}
