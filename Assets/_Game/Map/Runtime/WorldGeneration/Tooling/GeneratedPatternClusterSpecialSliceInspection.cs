using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public enum GeneratedPatternCandidateState
    {
        MissingData = 0,
        Accepted = 1,
        Rejected = 2,
    }

    public sealed class GeneratedPatternDetailRecord
    {
        public GeneratedPatternDetailRecord(int sectorX, int sectorY, int patternZoneX,
            int patternZoneY, int patternLocalX, int patternLocalY, string patternId,
            string biomeProfileId, int candidateOrdinal, string transformToken,
            GeneratedPatternCandidateState state, string rejectionReason, int addSolidCount,
            int carveAirCount, int protectedMaskOverlapCount, int affectedCellCount,
            string sourceDigest, string missingDataMarker)
        {
            ValidateSector(sectorX, sectorY);
            if (patternLocalX < 0 || patternLocalX >= MicroPatternDefinition.RequiredWidth)
                throw new ArgumentOutOfRangeException(nameof(patternLocalX));
            if (patternLocalY < 0 || patternLocalY >= MicroPatternDefinition.RequiredHeight)
                throw new ArgumentOutOfRangeException(nameof(patternLocalY));
            if (candidateOrdinal < 0 || addSolidCount < 0 || carveAirCount < 0 ||
                protectedMaskOverlapCount < 0 || affectedCellCount < 0)
                throw new ArgumentOutOfRangeException(nameof(candidateOrdinal));
            SectorX = sectorX;
            SectorY = sectorY;
            PatternZoneX = patternZoneX;
            PatternZoneY = patternZoneY;
            PatternLocalX = patternLocalX;
            PatternLocalY = patternLocalY;
            PatternId = patternId ?? string.Empty;
            BiomeProfileId = biomeProfileId ?? string.Empty;
            CandidateOrdinal = candidateOrdinal;
            TransformToken = transformToken ?? string.Empty;
            State = state;
            RejectionReason = rejectionReason ?? string.Empty;
            AddSolidCount = addSolidCount;
            CarveAirCount = carveAirCount;
            ProtectedMaskOverlapCount = protectedMaskOverlapCount;
            AffectedCellCount = affectedCellCount;
            SourceDigest = sourceDigest ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public int PatternZoneX { get; }
        public int PatternZoneY { get; }
        public int PatternLocalX { get; }
        public int PatternLocalY { get; }
        public int PatternWidth => MicroPatternDefinition.RequiredWidth;
        public int PatternHeight => MicroPatternDefinition.RequiredHeight;
        public string PatternId { get; }
        public string BiomeProfileId { get; }
        public int CandidateOrdinal { get; }
        public string TransformToken { get; }
        public GeneratedPatternCandidateState State { get; }
        public string RejectionReason { get; }
        public int AddSolidCount { get; }
        public int CarveAirCount { get; }
        public int ProtectedMaskOverlapCount { get; }
        public int AffectedCellCount { get; }
        public string SourceDigest { get; }
        public string MissingDataMarker { get; }
        public bool IsMissingData => State == GeneratedPatternCandidateState.MissingData ||
                                     !string.IsNullOrEmpty(MissingDataMarker);

        public static GeneratedPatternDetailRecord Missing(int sectorX, int sectorY,
            int selectedCellX, int selectedCellY, string reason) =>
            new GeneratedPatternDetailRecord(sectorX, sectorY,
                selectedCellX / MicroPatternDefinition.RequiredWidth,
                selectedCellY / MicroPatternDefinition.RequiredHeight,
                selectedCellX % MicroPatternDefinition.RequiredWidth,
                selectedCellY % MicroPatternDefinition.RequiredHeight,
                "MissingData", "MissingData", 0, "MissingData",
                GeneratedPatternCandidateState.MissingData, reason, 0, 0, 0, 0,
                string.Empty, reason);

        public static GeneratedPatternDetailRecord Projection(int sectorX, int sectorY,
            int patternZoneX, int patternZoneY, int patternLocalX, int patternLocalY,
            string patternId, string biomeProfileId, int candidateOrdinal,
            string transformToken, bool accepted, string rejectionReason,
            int addSolidCount, int carveAirCount, int protectedMaskOverlapCount,
            int affectedCellCount, string sourceDigest) =>
            new GeneratedPatternDetailRecord(sectorX, sectorY, patternZoneX, patternZoneY,
                patternLocalX, patternLocalY, patternId, biomeProfileId, candidateOrdinal,
                transformToken, accepted ? GeneratedPatternCandidateState.Accepted :
                    GeneratedPatternCandidateState.Rejected,
                accepted ? string.Empty : rejectionReason, addSolidCount, carveAirCount,
                protectedMaskOverlapCount, affectedCellCount, sourceDigest, string.Empty);

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            Number(SectorX), Number(SectorY), Number(PatternZoneX), Number(PatternZoneY),
            Number(PatternLocalX), Number(PatternLocalY), PatternId, BiomeProfileId,
            Number(CandidateOrdinal), TransformToken, State.ToString(), RejectionReason,
            Number(AddSolidCount), Number(CarveAirCount), Number(ProtectedMaskOverlapCount),
            Number(AffectedCellCount), SourceDigest, MissingDataMarker);

        internal static void ValidateSector(int sectorX, int sectorY)
        {
            if (sectorX < 0 || sectorX >= GeneratedWorldOverlayLayerCatalog.WorldWidthSectors)
                throw new ArgumentOutOfRangeException(nameof(sectorX));
            if (sectorY < 0 || sectorY >= GeneratedWorldOverlayLayerCatalog.WorldHeightSectors)
                throw new ArgumentOutOfRangeException(nameof(sectorY));
        }

        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedClusterDetailRecord
    {
        public GeneratedClusterDetailRecord(int sectorX, int sectorY, string clusterId,
            string clusterRole, string spineVariantId, int footprintMinX, int footprintMinY,
            int footprintMaxX, int footprintMaxY, int footprintCellCount, int pathNodeCount,
            int pathEdgeCount, int socketBindingCount, int activitySlotBindingCount,
            int specialSiteBindingCount, string ownerProvenanceDigest, string missingDataMarker)
        {
            GeneratedPatternDetailRecord.ValidateSector(sectorX, sectorY);
            if (footprintCellCount < 0 || pathNodeCount < 0 || pathEdgeCount < 0 ||
                socketBindingCount < 0 || activitySlotBindingCount < 0 ||
                specialSiteBindingCount < 0)
                throw new ArgumentOutOfRangeException(nameof(footprintCellCount));
            SectorX = sectorX;
            SectorY = sectorY;
            ClusterId = clusterId ?? string.Empty;
            ClusterRole = clusterRole ?? string.Empty;
            SpineVariantId = spineVariantId ?? string.Empty;
            FootprintMinX = footprintMinX;
            FootprintMinY = footprintMinY;
            FootprintMaxX = footprintMaxX;
            FootprintMaxY = footprintMaxY;
            FootprintCellCount = footprintCellCount;
            PathNodeCount = pathNodeCount;
            PathEdgeCount = pathEdgeCount;
            SocketBindingCount = socketBindingCount;
            ActivitySlotBindingCount = activitySlotBindingCount;
            SpecialSiteBindingCount = specialSiteBindingCount;
            OwnerProvenanceDigest = ownerProvenanceDigest ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public string ClusterId { get; }
        public string ClusterRole { get; }
        public string SpineVariantId { get; }
        public int FootprintMinX { get; }
        public int FootprintMinY { get; }
        public int FootprintMaxX { get; }
        public int FootprintMaxY { get; }
        public int FootprintCellCount { get; }
        public int PathNodeCount { get; }
        public int PathEdgeCount { get; }
        public int SocketBindingCount { get; }
        public int ActivitySlotBindingCount { get; }
        public int SpecialSiteBindingCount { get; }
        public string OwnerProvenanceDigest { get; }
        public string MissingDataMarker { get; }
        public bool IsMissingData => !string.IsNullOrEmpty(MissingDataMarker);

        public static GeneratedClusterDetailRecord Missing(int sectorX, int sectorY,
            string reason) => new GeneratedClusterDetailRecord(sectorX, sectorY,
                "MissingData", "MissingData", "MissingData", -1, -1, -1, -1,
                0, 0, 0, 0, 0, 0, string.Empty, reason);

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            GeneratedPatternDetailRecord.Number(SectorX), GeneratedPatternDetailRecord.Number(SectorY),
            ClusterId, ClusterRole, SpineVariantId, GeneratedPatternDetailRecord.Number(FootprintMinX),
            GeneratedPatternDetailRecord.Number(FootprintMinY),
            GeneratedPatternDetailRecord.Number(FootprintMaxX),
            GeneratedPatternDetailRecord.Number(FootprintMaxY),
            GeneratedPatternDetailRecord.Number(FootprintCellCount),
            GeneratedPatternDetailRecord.Number(PathNodeCount),
            GeneratedPatternDetailRecord.Number(PathEdgeCount),
            GeneratedPatternDetailRecord.Number(SocketBindingCount),
            GeneratedPatternDetailRecord.Number(ActivitySlotBindingCount),
            GeneratedPatternDetailRecord.Number(SpecialSiteBindingCount),
            OwnerProvenanceDigest, MissingDataMarker);
    }

    public enum GeneratedSpecialDetailState
    {
        Absent = 0,
        EmptyValid = 1,
        Present = 2,
    }

    public sealed class GeneratedSpecialDetailRecord
    {
        public GeneratedSpecialDetailRecord(int sectorX, int sectorY,
            GeneratedSpecialDetailState state, string specialRegionId, string siteKind,
            string regionCategory, int footprintMinX, int footprintMinY, int footprintMaxX,
            int footprintMaxY, int footprintCellCount, int entryMarkerCount,
            int returnMarkerCount, int fixedShellMarkerCount, int facilityMarkerCount,
            int requiredRewardMarkerCount, int optionalMarkerCount, string siteBindingDigest,
            string missingDataMarker)
        {
            GeneratedPatternDetailRecord.ValidateSector(sectorX, sectorY);
            if (footprintCellCount < 0 || entryMarkerCount < 0 || returnMarkerCount < 0 ||
                fixedShellMarkerCount < 0 || facilityMarkerCount < 0 ||
                requiredRewardMarkerCount < 0 || optionalMarkerCount < 0)
                throw new ArgumentOutOfRangeException(nameof(footprintCellCount));
            SectorX = sectorX;
            SectorY = sectorY;
            State = state;
            SpecialRegionId = specialRegionId ?? string.Empty;
            SiteKind = siteKind ?? string.Empty;
            RegionCategory = regionCategory ?? string.Empty;
            FootprintMinX = footprintMinX;
            FootprintMinY = footprintMinY;
            FootprintMaxX = footprintMaxX;
            FootprintMaxY = footprintMaxY;
            FootprintCellCount = footprintCellCount;
            EntryMarkerCount = entryMarkerCount;
            ReturnMarkerCount = returnMarkerCount;
            FixedShellMarkerCount = fixedShellMarkerCount;
            FacilityMarkerCount = facilityMarkerCount;
            RequiredRewardMarkerCount = requiredRewardMarkerCount;
            OptionalMarkerCount = optionalMarkerCount;
            SiteBindingDigest = siteBindingDigest ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public GeneratedSpecialDetailState State { get; }
        public string SpecialRegionId { get; }
        public string SiteKind { get; }
        public string RegionCategory { get; }
        public int FootprintMinX { get; }
        public int FootprintMinY { get; }
        public int FootprintMaxX { get; }
        public int FootprintMaxY { get; }
        public int FootprintCellCount { get; }
        public int EntryMarkerCount { get; }
        public int ReturnMarkerCount { get; }
        public int FixedShellMarkerCount { get; }
        public int FacilityMarkerCount { get; }
        public int RequiredRewardMarkerCount { get; }
        public int OptionalMarkerCount { get; }
        public string SiteBindingDigest { get; }
        public string MissingDataMarker { get; }
        public bool IsAbsentData => State == GeneratedSpecialDetailState.Absent;
        public bool IsEmptyValidData => State == GeneratedSpecialDetailState.EmptyValid;
        public bool IsMissingData => IsAbsentData && !string.IsNullOrEmpty(MissingDataMarker);

        public static GeneratedSpecialDetailRecord Absent(int sectorX, int sectorY,
            string reason) => new GeneratedSpecialDetailRecord(sectorX, sectorY,
                GeneratedSpecialDetailState.Absent, string.Empty, string.Empty, string.Empty,
                -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, string.Empty, reason);

        public static GeneratedSpecialDetailRecord EmptyValid(int sectorX, int sectorY,
            string sourceDigest) => new GeneratedSpecialDetailRecord(sectorX, sectorY,
                GeneratedSpecialDetailState.EmptyValid, string.Empty, string.Empty, string.Empty,
                -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, sourceDigest, string.Empty);

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            GeneratedPatternDetailRecord.Number(SectorX), GeneratedPatternDetailRecord.Number(SectorY),
            State.ToString(), SpecialRegionId, SiteKind, RegionCategory,
            GeneratedPatternDetailRecord.Number(FootprintMinX),
            GeneratedPatternDetailRecord.Number(FootprintMinY),
            GeneratedPatternDetailRecord.Number(FootprintMaxX),
            GeneratedPatternDetailRecord.Number(FootprintMaxY),
            GeneratedPatternDetailRecord.Number(FootprintCellCount),
            GeneratedPatternDetailRecord.Number(EntryMarkerCount),
            GeneratedPatternDetailRecord.Number(ReturnMarkerCount),
            GeneratedPatternDetailRecord.Number(FixedShellMarkerCount),
            GeneratedPatternDetailRecord.Number(FacilityMarkerCount),
            GeneratedPatternDetailRecord.Number(RequiredRewardMarkerCount),
            GeneratedPatternDetailRecord.Number(OptionalMarkerCount), SiteBindingDigest,
            MissingDataMarker);
    }

    public sealed class GeneratedSliceCellDetailRecord
    {
        public GeneratedSliceCellDetailRecord(int sliceIndex, int cellLocalX, int cellLocalY,
            int sectorLocalX, int sectorLocalY, bool hasWorldCoordinate, int worldX, int worldY,
            string owner, string provenanceId, string missingDataMarker)
        {
            if (sliceIndex < 0 || sliceIndex >= GeneratedMicroChunkSliceSet.ChunkCount)
                throw new ArgumentOutOfRangeException(nameof(sliceIndex));
            if (cellLocalX < 0 || cellLocalX >= GeneratedMicroChunkSliceSet.MicroChunkWidth)
                throw new ArgumentOutOfRangeException(nameof(cellLocalX));
            if (cellLocalY < 0 || cellLocalY >= GeneratedMicroChunkSliceSet.MicroChunkHeight)
                throw new ArgumentOutOfRangeException(nameof(cellLocalY));
            if (sectorLocalX < 0 || sectorLocalX >= GeneratedMicroChunkSliceSet.SectorWidth)
                throw new ArgumentOutOfRangeException(nameof(sectorLocalX));
            if (sectorLocalY < 0 || sectorLocalY >= GeneratedMicroChunkSliceSet.SectorHeight)
                throw new ArgumentOutOfRangeException(nameof(sectorLocalY));
            SliceIndex = sliceIndex;
            CellLocalX = cellLocalX;
            CellLocalY = cellLocalY;
            SectorLocalX = sectorLocalX;
            SectorLocalY = sectorLocalY;
            HasWorldCoordinate = hasWorldCoordinate;
            WorldX = worldX;
            WorldY = worldY;
            Owner = owner ?? string.Empty;
            ProvenanceId = provenanceId ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public int SliceIndex { get; }
        public int CellLocalX { get; }
        public int CellLocalY { get; }
        public int SectorLocalX { get; }
        public int SectorLocalY { get; }
        public bool HasWorldCoordinate { get; }
        public int WorldX { get; }
        public int WorldY { get; }
        public string Owner { get; }
        public string ProvenanceId { get; }
        public string MissingDataMarker { get; }
        public int RowMajorIndex => CellLocalY * GeneratedMicroChunkSliceSet.MicroChunkWidth + CellLocalX;
        public bool IsMissingData => !string.IsNullOrEmpty(MissingDataMarker);
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            GeneratedPatternDetailRecord.Number(SliceIndex),
            GeneratedPatternDetailRecord.Number(CellLocalX),
            GeneratedPatternDetailRecord.Number(CellLocalY),
            GeneratedPatternDetailRecord.Number(SectorLocalX),
            GeneratedPatternDetailRecord.Number(SectorLocalY),
            HasWorldCoordinate ? "true" : "false",
            GeneratedPatternDetailRecord.Number(WorldX), GeneratedPatternDetailRecord.Number(WorldY),
            Owner, ProvenanceId, MissingDataMarker);
    }

    public sealed class GeneratedSliceSocketBandRecord
    {
        public GeneratedSliceSocketBandRecord(string sideToken, int start, int length,
            string signatureDigest, string missingDataMarker)
        {
            SideToken = sideToken ?? string.Empty;
            Start = start;
            Length = length;
            SignatureDigest = signatureDigest ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public string SideToken { get; }
        public int Start { get; }
        public int Length { get; }
        public string SignatureDigest { get; }
        public string MissingDataMarker { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(SideToken,
            GeneratedPatternDetailRecord.Number(Start), GeneratedPatternDetailRecord.Number(Length),
            SignatureDigest, MissingDataMarker);
    }

    public sealed class GeneratedSliceMarkerSlotRecord
    {
        public GeneratedSliceMarkerSlotRecord(string slotKind, int localX, int localY,
            string stableId, string missingDataMarker)
        {
            SlotKind = slotKind ?? string.Empty;
            LocalX = localX;
            LocalY = localY;
            StableId = stableId ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public string SlotKind { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string StableId { get; }
        public string MissingDataMarker { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(SlotKind,
            GeneratedPatternDetailRecord.Number(LocalX), GeneratedPatternDetailRecord.Number(LocalY),
            StableId, MissingDataMarker);
    }

    public sealed class GeneratedSliceProvenanceRecord
    {
        public GeneratedSliceProvenanceRecord(string owner, string provenanceId,
            string sourceDigest, string missingDataMarker)
        {
            Owner = owner ?? string.Empty;
            ProvenanceId = provenanceId ?? string.Empty;
            SourceDigest = sourceDigest ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public string Owner { get; }
        public string ProvenanceId { get; }
        public string SourceDigest { get; }
        public string MissingDataMarker { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            Owner, ProvenanceId, SourceDigest, MissingDataMarker);
    }

    public sealed class GeneratedSliceDetailRecord
    {
        private readonly ReadOnlyCollection<GeneratedSliceCellDetailRecord> cells;
        private readonly ReadOnlyCollection<GeneratedSliceSocketBandRecord> socketBands;
        private readonly ReadOnlyCollection<GeneratedSliceMarkerSlotRecord> markerSlots;
        private readonly ReadOnlyCollection<GeneratedSliceProvenanceRecord> provenanceRecords;

        public GeneratedSliceDetailRecord(int sectorX, int sectorY, int sliceIndex,
            int chunkX, int chunkY, IEnumerable<GeneratedSliceCellDetailRecord> sourceCells,
            IEnumerable<GeneratedSliceSocketBandRecord> sourceSocketBands,
            IEnumerable<GeneratedSliceMarkerSlotRecord> sourceMarkerSlots,
            IEnumerable<GeneratedSliceProvenanceRecord> sourceProvenance,
            string ownerProvenanceDigest, string missingDataMarker)
        {
            GeneratedPatternDetailRecord.ValidateSector(sectorX, sectorY);
            if (sliceIndex < 0 || sliceIndex >= GeneratedMicroChunkSliceSet.ChunkCount)
                throw new ArgumentOutOfRangeException(nameof(sliceIndex));
            if (chunkX < 0 || chunkX >= GeneratedMicroChunkSliceSet.ChunkGridWidth)
                throw new ArgumentOutOfRangeException(nameof(chunkX));
            if (chunkY < 0 || chunkY >= GeneratedMicroChunkSliceSet.ChunkGridHeight)
                throw new ArgumentOutOfRangeException(nameof(chunkY));
            var orderedCells = (sourceCells ?? Array.Empty<GeneratedSliceCellDetailRecord>())
                .Where(value => value != null).OrderBy(value => value.RowMajorIndex).ToArray();
            if (orderedCells.Length != GeneratedMicroChunkSliceSet.MicroChunkCellCount ||
                orderedCells.Select(value => value.RowMajorIndex).Distinct().Count() !=
                    GeneratedMicroChunkSliceSet.MicroChunkCellCount ||
                orderedCells.Any(value => value.SliceIndex != sliceIndex))
                throw new ArgumentException("A detail slice requires every unique local cell.",
                    nameof(sourceCells));
            SectorX = sectorX;
            SectorY = sectorY;
            SliceIndex = sliceIndex;
            ChunkX = chunkX;
            ChunkY = chunkY;
            cells = new ReadOnlyCollection<GeneratedSliceCellDetailRecord>(orderedCells);
            socketBands = new ReadOnlyCollection<GeneratedSliceSocketBandRecord>(
                (sourceSocketBands ?? Array.Empty<GeneratedSliceSocketBandRecord>())
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray());
            markerSlots = new ReadOnlyCollection<GeneratedSliceMarkerSlotRecord>(
                (sourceMarkerSlots ?? Array.Empty<GeneratedSliceMarkerSlotRecord>())
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray());
            provenanceRecords = new ReadOnlyCollection<GeneratedSliceProvenanceRecord>(
                (sourceProvenance ?? Array.Empty<GeneratedSliceProvenanceRecord>())
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray());
            OwnerProvenanceDigest = ownerProvenanceDigest ?? string.Empty;
            MissingDataMarker = missingDataMarker ?? string.Empty;
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public int SliceIndex { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public int Width => GeneratedMicroChunkSliceSet.MicroChunkWidth;
        public int Height => GeneratedMicroChunkSliceSet.MicroChunkHeight;
        public int CellCount => cells.Count;
        public IReadOnlyList<GeneratedSliceCellDetailRecord> Cells => cells;
        public IReadOnlyList<GeneratedSliceSocketBandRecord> SocketBands => socketBands;
        public IReadOnlyList<GeneratedSliceMarkerSlotRecord> MarkerSlots => markerSlots;
        public IReadOnlyList<GeneratedSliceProvenanceRecord> ProvenanceRecords => provenanceRecords;
        public string OwnerProvenanceDigest { get; }
        public string MissingDataMarker { get; }
        public bool IsMissingData => !string.IsNullOrEmpty(MissingDataMarker);

        public static GeneratedSliceDetailRecord Missing(int sectorX, int sectorY,
            int sliceIndex, string reason)
        {
            var chunkX = sliceIndex % GeneratedMicroChunkSliceSet.ChunkGridWidth;
            var chunkY = sliceIndex / GeneratedMicroChunkSliceSet.ChunkGridWidth;
            var cells = new List<GeneratedSliceCellDetailRecord>(
                GeneratedMicroChunkSliceSet.MicroChunkCellCount);
            for (var localY = 0; localY < GeneratedMicroChunkSliceSet.MicroChunkHeight; localY++)
            for (var localX = 0; localX < GeneratedMicroChunkSliceSet.MicroChunkWidth; localX++)
            {
                var sectorLocalX = chunkX * GeneratedMicroChunkSliceSet.MicroChunkWidth + localX;
                var sectorLocalY = chunkY * GeneratedMicroChunkSliceSet.MicroChunkHeight + localY;
                cells.Add(new GeneratedSliceCellDetailRecord(sliceIndex, localX, localY,
                    sectorLocalX, sectorLocalY, true,
                    sectorX * GeneratedMicroChunkSliceSet.SectorWidth + sectorLocalX,
                    sectorY * GeneratedMicroChunkSliceSet.SectorHeight + sectorLocalY,
                    "MissingData", "MissingData", reason));
            }
            var socketBands = Enum.GetValues(typeof(GeneratedMicroChunkSocketSide))
                .Cast<GeneratedMicroChunkSocketSide>().Select(side =>
                    new GeneratedSliceSocketBandRecord(side.ToString(), -1, 0,
                        string.Empty, reason));
            return new GeneratedSliceDetailRecord(sectorX, sectorY, sliceIndex, chunkX, chunkY,
                cells, socketBands,
                new[] { new GeneratedSliceMarkerSlotRecord("MissingData", -1, -1,
                    string.Empty, reason) },
                new[] { new GeneratedSliceProvenanceRecord("MissingData", "MissingData",
                    string.Empty, reason) }, string.Empty, reason);
        }

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            GeneratedPatternDetailRecord.Number(SectorX), GeneratedPatternDetailRecord.Number(SectorY),
            GeneratedPatternDetailRecord.Number(SliceIndex),
            GeneratedPatternDetailRecord.Number(ChunkX), GeneratedPatternDetailRecord.Number(ChunkY),
            GeneratedPatternDetailRecord.Number(Width), GeneratedPatternDetailRecord.Number(Height),
            string.Join("|", cells.Select(value => value.CanonicalLine)),
            string.Join("|", socketBands.Select(value => value.CanonicalLine)),
            string.Join("|", markerSlots.Select(value => value.CanonicalLine)),
            string.Join("|", provenanceRecords.Select(value => value.CanonicalLine)),
            OwnerProvenanceDigest, MissingDataMarker);
    }

    /// <summary>Immutable combined records for the four detail views.</summary>
    public sealed class GeneratedPatternClusterSpecialSliceInspection
    {
        public const string SchemaVersion = "map20_03.pattern_cluster_special_slice.v1";
        public const string TaskId = GeneratedDetailInspectionSnapshot.TaskId;

        private readonly ReadOnlyCollection<GeneratedPatternDetailRecord> patternRecords;
        private readonly ReadOnlyCollection<GeneratedClusterDetailRecord> clusterRecords;
        private readonly ReadOnlyCollection<GeneratedSpecialDetailRecord> specialRecords;
        private readonly ReadOnlyCollection<GeneratedSliceDetailRecord> sliceRecords;
        private readonly ReadOnlyCollection<GeneratedDetailMissingDataRecord> missingDataRecords;

        public GeneratedPatternClusterSpecialSliceInspection(
            IEnumerable<GeneratedPatternDetailRecord> patterns,
            IEnumerable<GeneratedClusterDetailRecord> clusters,
            IEnumerable<GeneratedSpecialDetailRecord> specials,
            IEnumerable<GeneratedSliceDetailRecord> slices,
            IEnumerable<GeneratedDetailMissingDataRecord> missingData, string createdUtc)
        {
            patternRecords = Ordered(patterns, value => value.CanonicalLine);
            clusterRecords = Ordered(clusters, value => value.CanonicalLine);
            specialRecords = Ordered(specials, value => value.CanonicalLine);
            var orderedSlices = (slices ?? Array.Empty<GeneratedSliceDetailRecord>())
                .Where(value => value != null).OrderBy(value => value.SliceIndex).ToArray();
            if (orderedSlices.Length != GeneratedMicroChunkSliceSet.ChunkCount ||
                orderedSlices.Select(value => value.SliceIndex).Distinct().Count() !=
                    GeneratedMicroChunkSliceSet.ChunkCount)
                throw new ArgumentException("The detail sample requires every generated slice.",
                    nameof(slices));
            sliceRecords = new ReadOnlyCollection<GeneratedSliceDetailRecord>(orderedSlices);
            missingDataRecords = new ReadOnlyCollection<GeneratedDetailMissingDataRecord>(
                (missingData ?? Array.Empty<GeneratedDetailMissingDataRecord>())
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray());
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public IReadOnlyList<GeneratedPatternDetailRecord> PatternRecords => patternRecords;
        public IReadOnlyList<GeneratedClusterDetailRecord> ClusterRecords => clusterRecords;
        public IReadOnlyList<GeneratedSpecialDetailRecord> SpecialRecords => specialRecords;
        public IReadOnlyList<GeneratedSliceDetailRecord> SliceRecords => sliceRecords;
        public IReadOnlyList<GeneratedDetailMissingDataRecord> MissingDataRecords => missingDataRecords;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }

        public IReadOnlyList<GeneratedDetailTabSummary> CreateTabSummaries()
        {
            var contributions = new[]
            {
                BakingCanonicalDigest.HashCanonicalLines(patternRecords.Select(value => value.CanonicalLine)),
                BakingCanonicalDigest.HashCanonicalLines(clusterRecords.Select(value => value.CanonicalLine)),
                BakingCanonicalDigest.HashCanonicalLines(specialRecords.Select(value => value.CanonicalLine)),
                BakingCanonicalDigest.HashCanonicalLines(sliceRecords.Select(value => value.CanonicalLine)),
            };
            return new ReadOnlyCollection<GeneratedDetailTabSummary>(new[]
            {
                new GeneratedDetailTabSummary(GeneratedDetailTabCatalog.Tabs[0].Token,
                    patternRecords.Count, patternRecords.Count(value => value.IsMissingData),
                    contributions[0]),
                new GeneratedDetailTabSummary(GeneratedDetailTabCatalog.Tabs[1].Token,
                    clusterRecords.Count, clusterRecords.Count(value => value.IsMissingData),
                    contributions[1]),
                new GeneratedDetailTabSummary(GeneratedDetailTabCatalog.Tabs[2].Token,
                    specialRecords.Count, specialRecords.Count(value => value.IsMissingData),
                    contributions[2]),
                new GeneratedDetailTabSummary(GeneratedDetailTabCatalog.Tabs[3].Token,
                    sliceRecords.Count, sliceRecords.Count(value => value.IsMissingData),
                    contributions[3]),
            });
        }

        public string Serialize() => GeneratedInspectionCanonical.ToJson(ToDocument());

        public static GeneratedPatternClusterSpecialSliceInspection CreateMissingDataSample(
            int sectorX, int sectorY, int selectedCellX, int selectedCellY, string createdUtc)
        {
            const string reason =
                "MAP20_02 samples contain inspection markers but no detailed source records.";
            var slices = Enumerable.Range(0, GeneratedMicroChunkSliceSet.ChunkCount)
                .Select(index => GeneratedSliceDetailRecord.Missing(
                    sectorX, sectorY, index, reason)).ToArray();
            var tabs = GeneratedDetailTabCatalog.Tabs;
            return new GeneratedPatternClusterSpecialSliceInspection(
                new[] { GeneratedPatternDetailRecord.Missing(
                    sectorX, sectorY, selectedCellX, selectedCellY, reason) },
                new[] { GeneratedClusterDetailRecord.Missing(sectorX, sectorY, reason) },
                new[] { GeneratedSpecialDetailRecord.Absent(sectorX, sectorY, reason) },
                slices,
                new[]
                {
                    new GeneratedDetailMissingDataRecord(tabs[0].Token, "pattern:selected",
                        "source_digest", reason),
                    new GeneratedDetailMissingDataRecord(tabs[1].Token, "cluster:selected",
                        "owner_provenance_digest", reason),
                    new GeneratedDetailMissingDataRecord(tabs[2].Token, "special:selected",
                        "site_binding_digest", reason),
                    new GeneratedDetailMissingDataRecord(tabs[3].Token, "slice:all",
                        "owner_provenance_digest", reason),
                }, createdUtc);
        }

        private string ComputeCanonicalDigest()
        {
            var lines = new List<string> { SchemaVersion, TaskId, "created_utc_excluded=true" };
            lines.AddRange(patternRecords.Select(value => "pattern=" + value.CanonicalLine));
            lines.AddRange(clusterRecords.Select(value => "cluster=" + value.CanonicalLine));
            lines.AddRange(specialRecords.Select(value => "special=" + value.CanonicalLine));
            lines.AddRange(sliceRecords.Select(value => "slice=" + value.CanonicalLine));
            lines.AddRange(missingDataRecords.Select(value => "missing=" + value.CanonicalLine));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private GeneratedPatternClusterSpecialSliceDocument ToDocument() =>
            new GeneratedPatternClusterSpecialSliceDocument
            {
                schema_version = SchemaVersion,
                task_id = TaskId,
                pattern_records = patternRecords.Select(GeneratedPatternDetailDocument.From).ToArray(),
                cluster_records = clusterRecords.Select(GeneratedClusterDetailDocument.From).ToArray(),
                special_records = specialRecords.Select(GeneratedSpecialDetailDocument.From).ToArray(),
                slice_records = sliceRecords.Select(GeneratedSliceDetailDocument.From).ToArray(),
                missing_data_records = missingDataRecords.Select(
                    GeneratedDetailMissingDataDocument.From).ToArray(),
                canonical_digest = CanonicalDigest,
                created_utc = CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
            };

        private static ReadOnlyCollection<T> Ordered<T>(IEnumerable<T> source,
            Func<T, string> key) where T : class => new ReadOnlyCollection<T>(
                (source ?? Array.Empty<T>()).Where(value => value != null)
                .OrderBy(key, StringComparer.Ordinal).ToArray());
    }

    [Serializable]
    internal sealed class GeneratedPatternClusterSpecialSliceDocument
    {
        public string schema_version;
        public string task_id;
        public GeneratedPatternDetailDocument[] pattern_records;
        public GeneratedClusterDetailDocument[] cluster_records;
        public GeneratedSpecialDetailDocument[] special_records;
        public GeneratedSliceDetailDocument[] slice_records;
        public GeneratedDetailMissingDataDocument[] missing_data_records;
        public string canonical_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
    }

    [Serializable]
    internal sealed class GeneratedPatternDetailDocument
    {
        public GeneratedDetailCoordinateDocument sector_coordinate;
        public GeneratedDetailCoordinateDocument pattern_zone_coordinate;
        public GeneratedDetailCoordinateDocument pattern_local_coordinate;
        public int pattern_width;
        public int pattern_height;
        public string pattern_id;
        public string biome_profile_id;
        public int candidate_ordinal;
        public string transform_token;
        public string candidate_state;
        public string rejection_reason;
        public int add_solid_count;
        public int carve_air_count;
        public int protected_mask_overlap_count;
        public int affected_cell_count;
        public string source_digest;
        public string missing_data_marker;

        public static GeneratedPatternDetailDocument From(GeneratedPatternDetailRecord value) =>
            new GeneratedPatternDetailDocument
            {
                sector_coordinate = Coordinate(value.SectorX, value.SectorY),
                pattern_zone_coordinate = Coordinate(value.PatternZoneX, value.PatternZoneY),
                pattern_local_coordinate = Coordinate(value.PatternLocalX, value.PatternLocalY),
                pattern_width = value.PatternWidth,
                pattern_height = value.PatternHeight,
                pattern_id = value.PatternId,
                biome_profile_id = value.BiomeProfileId,
                candidate_ordinal = value.CandidateOrdinal,
                transform_token = value.TransformToken,
                candidate_state = value.State.ToString(),
                rejection_reason = value.RejectionReason,
                add_solid_count = value.AddSolidCount,
                carve_air_count = value.CarveAirCount,
                protected_mask_overlap_count = value.ProtectedMaskOverlapCount,
                affected_cell_count = value.AffectedCellCount,
                source_digest = value.SourceDigest,
                missing_data_marker = value.MissingDataMarker,
            };

        internal static GeneratedDetailCoordinateDocument Coordinate(int x, int y) =>
            new GeneratedDetailCoordinateDocument { x = x, y = y };
    }

    [Serializable]
    internal sealed class GeneratedClusterDetailDocument
    {
        public GeneratedDetailCoordinateDocument sector_coordinate;
        public string cluster_id;
        public string cluster_role;
        public string spine_variant_id;
        public GeneratedDetailBoundsDocument footprint_bounds;
        public int footprint_cell_count;
        public int route_path_node_count;
        public int route_path_edge_count;
        public int socket_binding_count;
        public int activity_slot_binding_count;
        public int special_site_binding_count;
        public string owner_provenance_digest;
        public string missing_data_marker;

        public static GeneratedClusterDetailDocument From(GeneratedClusterDetailRecord value) =>
            new GeneratedClusterDetailDocument
            {
                sector_coordinate = GeneratedPatternDetailDocument.Coordinate(
                    value.SectorX, value.SectorY),
                cluster_id = value.ClusterId,
                cluster_role = value.ClusterRole,
                spine_variant_id = value.SpineVariantId,
                footprint_bounds = GeneratedDetailBoundsDocument.From(value.FootprintMinX,
                    value.FootprintMinY, value.FootprintMaxX, value.FootprintMaxY),
                footprint_cell_count = value.FootprintCellCount,
                route_path_node_count = value.PathNodeCount,
                route_path_edge_count = value.PathEdgeCount,
                socket_binding_count = value.SocketBindingCount,
                activity_slot_binding_count = value.ActivitySlotBindingCount,
                special_site_binding_count = value.SpecialSiteBindingCount,
                owner_provenance_digest = value.OwnerProvenanceDigest,
                missing_data_marker = value.MissingDataMarker,
            };
    }

    [Serializable]
    internal sealed class GeneratedSpecialDetailDocument
    {
        public GeneratedDetailCoordinateDocument sector_coordinate;
        public string data_state;
        public string special_region_id;
        public string site_kind;
        public string region_category;
        public GeneratedDetailBoundsDocument footprint_bounds;
        public int footprint_cell_count;
        public int entry_marker_count;
        public int return_marker_count;
        public int fixed_shell_marker_count;
        public int facility_marker_count;
        public int required_reward_marker_count;
        public int optional_marker_count;
        public string site_binding_digest;
        public string missing_data_marker;

        public static GeneratedSpecialDetailDocument From(GeneratedSpecialDetailRecord value) =>
            new GeneratedSpecialDetailDocument
            {
                sector_coordinate = GeneratedPatternDetailDocument.Coordinate(
                    value.SectorX, value.SectorY),
                data_state = value.State.ToString(),
                special_region_id = value.SpecialRegionId,
                site_kind = value.SiteKind,
                region_category = value.RegionCategory,
                footprint_bounds = GeneratedDetailBoundsDocument.From(value.FootprintMinX,
                    value.FootprintMinY, value.FootprintMaxX, value.FootprintMaxY),
                footprint_cell_count = value.FootprintCellCount,
                entry_marker_count = value.EntryMarkerCount,
                return_marker_count = value.ReturnMarkerCount,
                fixed_shell_marker_count = value.FixedShellMarkerCount,
                facility_marker_count = value.FacilityMarkerCount,
                required_reward_marker_count = value.RequiredRewardMarkerCount,
                optional_marker_count = value.OptionalMarkerCount,
                site_binding_digest = value.SiteBindingDigest,
                missing_data_marker = value.MissingDataMarker,
            };
    }

    [Serializable]
    internal sealed class GeneratedDetailBoundsDocument
    {
        public int min_x;
        public int min_y;
        public int max_x;
        public int max_y;

        public static GeneratedDetailBoundsDocument From(int minX, int minY, int maxX, int maxY) =>
            new GeneratedDetailBoundsDocument
                { min_x = minX, min_y = minY, max_x = maxX, max_y = maxY };
    }

    [Serializable]
    internal sealed class GeneratedSliceDetailDocument
    {
        public GeneratedDetailCoordinateDocument sector_coordinate;
        public int slice_index;
        public GeneratedDetailCoordinateDocument chunk_coordinate;
        public int slice_width;
        public int slice_height;
        public int slice_cell_count;
        public GeneratedSliceCellDetailDocument[] cell_records;
        public GeneratedSliceSocketBandDocument[] socket_band_records;
        public GeneratedSliceMarkerSlotDocument[] marker_slot_records;
        public GeneratedSliceProvenanceDocument[] provenance_records;
        public string owner_provenance_digest;
        public string missing_data_marker;

        public static GeneratedSliceDetailDocument From(GeneratedSliceDetailRecord value) =>
            new GeneratedSliceDetailDocument
            {
                sector_coordinate = GeneratedPatternDetailDocument.Coordinate(
                    value.SectorX, value.SectorY),
                slice_index = value.SliceIndex,
                chunk_coordinate = GeneratedPatternDetailDocument.Coordinate(
                    value.ChunkX, value.ChunkY),
                slice_width = value.Width,
                slice_height = value.Height,
                slice_cell_count = value.CellCount,
                cell_records = value.Cells.Select(GeneratedSliceCellDetailDocument.From).ToArray(),
                socket_band_records = value.SocketBands.Select(
                    GeneratedSliceSocketBandDocument.From).ToArray(),
                marker_slot_records = value.MarkerSlots.Select(
                    GeneratedSliceMarkerSlotDocument.From).ToArray(),
                provenance_records = value.ProvenanceRecords.Select(
                    GeneratedSliceProvenanceDocument.From).ToArray(),
                owner_provenance_digest = value.OwnerProvenanceDigest,
                missing_data_marker = value.MissingDataMarker,
            };
    }

    [Serializable]
    internal sealed class GeneratedSliceCellDetailDocument
    {
        public GeneratedDetailCoordinateDocument cell_local_coordinate;
        public GeneratedDetailCoordinateDocument sector_local_coordinate;
        public bool has_world_coordinate;
        public GeneratedDetailCoordinateDocument world_coordinate;
        public string owner;
        public string provenance_id;
        public string missing_data_marker;

        public static GeneratedSliceCellDetailDocument From(GeneratedSliceCellDetailRecord value) =>
            new GeneratedSliceCellDetailDocument
            {
                cell_local_coordinate = GeneratedPatternDetailDocument.Coordinate(
                    value.CellLocalX, value.CellLocalY),
                sector_local_coordinate = GeneratedPatternDetailDocument.Coordinate(
                    value.SectorLocalX, value.SectorLocalY),
                has_world_coordinate = value.HasWorldCoordinate,
                world_coordinate = GeneratedPatternDetailDocument.Coordinate(
                    value.WorldX, value.WorldY),
                owner = value.Owner,
                provenance_id = value.ProvenanceId,
                missing_data_marker = value.MissingDataMarker,
            };
    }

    [Serializable]
    internal sealed class GeneratedSliceSocketBandDocument
    {
        public string side_token;
        public int start;
        public int length;
        public string signature_digest;
        public string missing_data_marker;

        public static GeneratedSliceSocketBandDocument From(GeneratedSliceSocketBandRecord value) =>
            new GeneratedSliceSocketBandDocument
            {
                side_token = value.SideToken, start = value.Start, length = value.Length,
                signature_digest = value.SignatureDigest,
                missing_data_marker = value.MissingDataMarker,
            };
    }

    [Serializable]
    internal sealed class GeneratedSliceMarkerSlotDocument
    {
        public string slot_kind;
        public GeneratedDetailCoordinateDocument local_coordinate;
        public string stable_id;
        public string missing_data_marker;

        public static GeneratedSliceMarkerSlotDocument From(GeneratedSliceMarkerSlotRecord value) =>
            new GeneratedSliceMarkerSlotDocument
            {
                slot_kind = value.SlotKind,
                local_coordinate = GeneratedPatternDetailDocument.Coordinate(
                    value.LocalX, value.LocalY),
                stable_id = value.StableId,
                missing_data_marker = value.MissingDataMarker,
            };
    }

    [Serializable]
    internal sealed class GeneratedSliceProvenanceDocument
    {
        public string owner;
        public string provenance_id;
        public string source_digest;
        public string missing_data_marker;

        public static GeneratedSliceProvenanceDocument From(GeneratedSliceProvenanceRecord value) =>
            new GeneratedSliceProvenanceDocument
            {
                owner = value.Owner,
                provenance_id = value.ProvenanceId,
                source_digest = value.SourceDigest,
                missing_data_marker = value.MissingDataMarker,
            };
    }
}
