using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class BiomePatchOverlaySnapshot
    {
        private readonly IReadOnlyList<BiomePatchOverlayCell> cells;
        private readonly IReadOnlyList<BiomePatchOverlayPatchRow> patches;

        private BiomePatchOverlaySnapshot(
            ulong worldSeed,
            IReadOnlyList<BiomePatchOverlayCell> sourceCells,
            IReadOnlyList<BiomePatchOverlayPatchRow> sourcePatches,
            int assignedCount,
            int unassignedCount,
            int coreCount,
            int satelliteCount,
            int intrusionCount,
            int passedValidationRuleCount)
        {
            WorldSeed = worldSeed;
            cells = sourceCells;
            patches = sourcePatches;
            AssignedCount = assignedCount;
            UnassignedCount = unassignedCount;
            CoreCount = coreCount;
            SatelliteCount = satelliteCount;
            IntrusionCount = intrusionCount;
            PassedValidationRuleCount = passedValidationRuleCount;
        }

        public ulong WorldSeed { get; }
        public IReadOnlyList<BiomePatchOverlayCell> Cells => cells;
        public IReadOnlyList<BiomePatchOverlayPatchRow> Patches => patches;
        public int AssignedCount { get; }
        public int UnassignedCount { get; }
        public int CoreCount { get; }
        public int SatelliteCount { get; }
        public int IntrusionCount { get; }
        public int PassedValidationRuleCount { get; }

        public BiomePatchOverlayCell GetCell(int index)
        {
            if (!TryGetCell(index, out var cell))
                throw new ArgumentOutOfRangeException(nameof(index));
            return cell;
        }

        public BiomePatchOverlayCell GetCell(SectorCoord coordinate)
        {
            return GetCell(WorldGridIndex.ToIndex(coordinate));
        }

        public bool TryGetCell(int index, out BiomePatchOverlayCell cell)
        {
            if (index < 0 || index >= cells.Count)
            {
                cell = null;
                return false;
            }

            cell = cells[index];
            return true;
        }

        public static BiomePatchOverlaySnapshot Create(
            BiomePatchValidationPublication publication)
        {
            if (publication == null) throw new ArgumentNullException(nameof(publication));

            var sourceExport = publication.SourceExport;
            var sourceSnapshot = publication.Snapshot;
            var sourceWorld = publication.WorldWithBiomeAssignments;
            var diagnostics = publication.Diagnostics;
            if (sourceExport == null || sourceSnapshot == null || sourceWorld == null || diagnostics == null ||
                sourceExport.SourceCleanup == null || sourceExport.SourceWorld == null ||
                sourceExport.WorldWithBiomeAssignments == null)
                throw new ArgumentException("The approved publication source chain is incomplete.", nameof(publication));
            if (!ReferenceEquals(sourceSnapshot, sourceExport.SourceCleanup.Snapshot) ||
                !ReferenceEquals(sourceWorld, sourceExport.WorldWithBiomeAssignments))
                throw new ArgumentException("The publication source chain identity is inconsistent.", nameof(publication));

            ValidateApprovedDiagnostics(diagnostics, sourceSnapshot.Patches.Count);
            ValidateExactSourceCounts(publication);

            var patchLookup = new Dictionary<BiomePatchId, BiomePatch>();
            foreach (var patch in sourceSnapshot.Patches)
            {
                if (patch == null || !patchLookup.TryAdd(patch.Id, patch))
                    throw new ArgumentException("Patch identities must be non-null and unique.", nameof(publication));
            }

            var rowLookup = new Dictionary<BiomePatchId, GeneratedBiomePatchRow>();
            foreach (var row in publication.PatchRows)
            {
                if (row == null || !rowLookup.TryAdd(row.PatchInstanceId, row))
                    throw new ArgumentException("Exported patch-row identities must be non-null and unique.", nameof(publication));
            }

            var coreSiteCellsByPatch = CreateCoreSiteCellLookup(sourceSnapshot, patchLookup);
            var patchRows = new List<BiomePatchOverlayPatchRow>(patchLookup.Count);
            var patchProjection = new Dictionary<BiomePatchId, BiomePatchOverlayPatchRow>();
            var coreCount = 0;
            var satelliteCount = 0;
            var intrusionCount = 0;
            var patchSectorSum = 0;
            foreach (var patch in sourceSnapshot.Patches)
            {
                if (!rowLookup.TryGetValue(patch.Id, out var exportedRow))
                    throw new ArgumentException("Every patch requires one exported row.", nameof(publication));
                BiomePatchOverlayGui.GetBiomeColor(patch.BiomeId);
                BiomePatchOverlayGui.GetRoleGlyph(patch.Role);
                var perimeter = ComputePerimeter(patch);
                var compactness = ComputeCompactness(patch.SectorCount, perimeter);
                ValidatePatchAgainstExport(
                    sourceSnapshot,
                    patch,
                    exportedRow,
                    perimeter,
                    coreSiteCellsByPatch[patch.Id]);

                var projected = new BiomePatchOverlayPatchRow(
                    patch.Id,
                    patch.BiomeId,
                    patch.Role,
                    patch.SectorCount,
                    perimeter,
                    compactness,
                    patch.Seeds.Count,
                    coreSiteCellsByPatch[patch.Id].Count);
                patchRows.Add(projected);
                patchProjection.Add(patch.Id, projected);
                checked { patchSectorSum += patch.SectorCount; }
                switch (patch.Role)
                {
                    case BiomePatchRole.Core: coreCount++; break;
                    case BiomePatchRole.Satellite: satelliteCount++; break;
                    case BiomePatchRole.Intrusion: intrusionCount++; break;
                    default: throw new ArgumentOutOfRangeException(nameof(publication));
                }
            }
            patchRows.Sort((left, right) => left.PatchId.CompareTo(right.PatchId));

            if (rowLookup.Count != patchLookup.Count || patchSectorSum != 165 ||
                diagnostics.CorePatchCount != coreCount ||
                diagnostics.SatellitePatchCount != satelliteCount ||
                diagnostics.IntrusionPatchCount != intrusionCount ||
                coreCount + satelliteCount + intrusionCount != patchLookup.Count)
                throw new ArgumentException("The publication does not contain the exact viable patch set.", nameof(publication));

            var copiedCells = new List<BiomePatchOverlayCell>(WorldGenConstants.SectorCount);
            var assigned = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var ownership = sourceSnapshot.GetSector(index);
                var worldCell = sourceWorld.Cells[index];
                var sourceWorldCell = sourceExport.SourceWorld.Cells[index];
                ValidateCellSourceIdentity(index, ownership, worldCell, sourceWorldCell);

                if (!ownership.IsAssigned)
                {
                    copiedCells.Add(new BiomePatchOverlayCell(
                        index,
                        ownership.Sector,
                        false,
                        string.Empty,
                        null,
                        null,
                        0,
                        0,
                        0,
                        false,
                        false,
                        HasDifferentNeighbor(sourceSnapshot, index, WorldGridIndex.GetLeftIndex(index)),
                        HasDifferentNeighbor(sourceSnapshot, index, WorldGridIndex.GetRightIndex(index)),
                        HasDifferentNeighbor(sourceSnapshot, index, WorldGridIndex.GetUpIndex(index)),
                        HasDifferentNeighbor(sourceSnapshot, index, WorldGridIndex.GetDownIndex(index))));
                    continue;
                }

                if (!ownership.PatchId.HasValue ||
                    !patchLookup.TryGetValue(ownership.PatchId.Value, out var patch) ||
                    !patchProjection.TryGetValue(ownership.PatchId.Value, out var projectedPatch))
                    throw new ArgumentException("Assigned ownership has no exact patch projection.", nameof(publication));

                var isSeed = ContainsSeed(patch, index);
                var isCoreSiteCell = coreSiteCellsByPatch[patch.Id].Contains(index);
                copiedCells.Add(new BiomePatchOverlayCell(
                    index,
                    ownership.Sector,
                    true,
                    ownership.PrimaryBiomeId,
                    patch.Id,
                    patch.Role,
                    projectedPatch.Size,
                    projectedPatch.Perimeter,
                    projectedPatch.CompactnessPermille,
                    isSeed,
                    isCoreSiteCell,
                    HasDifferentNeighbor(sourceSnapshot, index, WorldGridIndex.GetLeftIndex(index)),
                    HasDifferentNeighbor(sourceSnapshot, index, WorldGridIndex.GetRightIndex(index)),
                    HasDifferentNeighbor(sourceSnapshot, index, WorldGridIndex.GetUpIndex(index)),
                    HasDifferentNeighbor(sourceSnapshot, index, WorldGridIndex.GetDownIndex(index))));
                assigned++;
            }

            if (assigned != 165 || copiedCells.Count != WorldGenConstants.SectorCount ||
                sourceSnapshot.AssignedSectorCount != assigned || sourceSnapshot.UnassignedSectorCount != 4 ||
                sourceExport.AssignedSectorCount != assigned || sourceExport.UnassignedSectorCount != 4)
                throw new ArgumentException("The overlay requires exact 165/4 ownership conservation.", nameof(publication));

            return new BiomePatchOverlaySnapshot(
                sourceSnapshot.Seed,
                new ReadOnlyCollection<BiomePatchOverlayCell>(copiedCells),
                new ReadOnlyCollection<BiomePatchOverlayPatchRow>(patchRows),
                assigned,
                WorldGenConstants.SectorCount - assigned,
                coreCount,
                satelliteCount,
                intrusionCount,
                15);
        }

        private static void ValidateApprovedDiagnostics(
            BiomePatchValidationDiagnostics diagnostics,
            int expectedPatchCount)
        {
            var passed = 0;
            for (var index = 0; index < diagnostics.RuleResults.Count; index++)
            {
                var result = diagnostics.RuleResults[index];
                if (result == null || (int)result.Rule != index)
                    throw new ArgumentException("Validation rules must use the exact canonical order.", nameof(diagnostics));
                if (result.Passed) passed++;
            }

            if (diagnostics.RuleResults.Count != 15 || passed != 15 || diagnostics.Violations.Count != 0 ||
                diagnostics.PatchCount != expectedPatchCount ||
                diagnostics.CorePatchCount + diagnostics.SatellitePatchCount + diagnostics.IntrusionPatchCount != expectedPatchCount ||
                diagnostics.AssignedSectorCount != 165 || diagnostics.UnassignedSectorCount != 4 ||
                diagnostics.PatchSectorSum != 165 || diagnostics.RequiredBiomeCount != 4 ||
                diagnostics.CoreBindingCount != 4 || diagnostics.DisconnectedPatchCount != 0 ||
                diagnostics.OverlapCount != 0 || diagnostics.OrphanCount != 0 ||
                diagnostics.UnassignedNonReservedCount != 0 || diagnostics.SiteMisownershipCount != 0 ||
                diagnostics.IntrusionInvalidCount != 0 || diagnostics.PatchCsvRowCount != expectedPatchCount ||
                diagnostics.WorldCsvRowCount != 169 || diagnostics.RngDrawCount != 0 ||
                diagnostics.SourceMutationCount != 0)
                throw new ArgumentException("Only an exact approved 15/15 validation publication can be projected.", nameof(diagnostics));
        }

        private static void ValidateExactSourceCounts(BiomePatchValidationPublication publication)
        {
            var snapshot = publication.Snapshot;
            var export = publication.SourceExport;
            if (snapshot.Seed != publication.Diagnostics.WorldSeed ||
                snapshot.Seed != export.SourceWorld.Seed ||
                snapshot.Seed != export.WorldWithBiomeAssignments.Seed ||
                snapshot.Sectors.Count != 169 || snapshot.SiteBindings.Count != 4 ||
                publication.PatchRows.Count != snapshot.Patches.Count ||
                export.PatchRows.Count != snapshot.Patches.Count ||
                export.PatchRowCount != snapshot.Patches.Count ||
                export.WorldSectorRowCount != 169 || publication.WorldWithBiomeAssignments.Cells.Count != 169 ||
                export.SourceWorld.Cells.Count != 169)
                throw new ArgumentException("Publication source counts or seed identity are inconsistent.", nameof(publication));

            var exportedRows = new Dictionary<BiomePatchId, GeneratedBiomePatchRow>();
            foreach (var row in export.PatchRows)
                if (row == null || !exportedRows.TryAdd(row.PatchInstanceId, row))
                    throw new ArgumentException("Export patch rows must have unique identities.", nameof(publication));
            foreach (var row in publication.PatchRows)
                if (row == null || !exportedRows.TryGetValue(row.PatchInstanceId, out var exportedRow) ||
                    !PatchRowsMatch(row, exportedRow))
                    throw new ArgumentException("Validation and export patch rows must match exactly.", nameof(publication));
        }

        private static Dictionary<BiomePatchId, HashSet<int>> CreateCoreSiteCellLookup(
            BiomePatchSnapshot snapshot,
            IReadOnlyDictionary<BiomePatchId, BiomePatch> patches)
        {
            var result = new Dictionary<BiomePatchId, HashSet<int>>();
            foreach (var patch in patches.Values) result.Add(patch.Id, new HashSet<int>());
            foreach (var binding in snapshot.SiteBindings)
            {
                if (binding == null || !patches.TryGetValue(binding.PatchId, out var patch) ||
                    patch.Role != BiomePatchRole.Core ||
                    !string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal))
                    throw new ArgumentException("Core site bindings must match their patches.", nameof(snapshot));
                foreach (var sectorIndex in binding.OccupiedSectorIndices)
                    if (!result[patch.Id].Add(sectorIndex))
                        throw new ArgumentException("Core site cells must be unique within a patch.", nameof(snapshot));
            }
            return result;
        }

        private static void ValidatePatchAgainstExport(
            BiomePatchSnapshot snapshot,
            BiomePatch patch,
            GeneratedBiomePatchRow row,
            int perimeter,
            IReadOnlyCollection<int> coreSiteCells)
        {
            var seedIndex = int.MaxValue;
            var seedSet = new HashSet<int>();
            foreach (var seed in patch.Seeds)
            {
                if (seed == null || seed.Role != patch.Role || !patch.ContainsSector(seed.SectorIndex) ||
                    !seedSet.Add(seed.SectorIndex))
                    throw new ArgumentException("Patch seeds are inconsistent.", nameof(snapshot));
                if (seed.SectorIndex < seedIndex) seedIndex = seed.SectorIndex;
            }
            if (seedIndex == int.MaxValue)
                throw new ArgumentException("Every patch requires a seed.", nameof(snapshot));

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            foreach (var sectorIndex in patch.SectorIndices)
            {
                var coordinate = WorldGridIndex.ToCoordinate(sectorIndex);
                if (coordinate.X < minX) minX = coordinate.X;
                if (coordinate.Y < minY) minY = coordinate.Y;
                if (coordinate.X > maxX) maxX = coordinate.X;
                if (coordinate.Y > maxY) maxY = coordinate.Y;
            }
            var seedCoordinate = WorldGridIndex.ToCoordinate(seedIndex);
            if (row.Seed != snapshot.Seed || row.PatchInstanceId != patch.Id ||
                !string.Equals(row.BiomeId, patch.BiomeId, StringComparison.Ordinal) ||
                row.PatchRole != patch.Role || row.SeedSectorX != seedCoordinate.X ||
                row.SeedSectorY != seedCoordinate.Y || row.SectorCount != patch.SectorCount ||
                row.MinX != minX || row.MinY != minY || row.MaxX != maxX || row.MaxY != maxY ||
                row.PerimeterEdges != perimeter)
                throw new ArgumentException("Patch export values do not match the source patch.", nameof(row));

            var expectedSites = new List<SiteReservationId>();
            foreach (var binding in snapshot.SiteBindings)
                if (binding.PatchId == patch.Id) expectedSites.Add(binding.SiteReservationId);
            expectedSites.Sort();
            if (expectedSites.Count != row.SpecialMapInstanceIds.Count)
                throw new ArgumentException("Patch special-site counts do not match.", nameof(row));
            for (var index = 0; index < expectedSites.Count; index++)
                if (expectedSites[index] != row.SpecialMapInstanceIds[index])
                    throw new ArgumentException("Patch special-site identities do not match.", nameof(row));
            if (coreSiteCells.Count != expectedSites.Count)
                throw new ArgumentException("Each Core binding must project one site cell.", nameof(snapshot));
        }

        private static void ValidateCellSourceIdentity(
            int index,
            BiomeSectorOwnership ownership,
            SectorCell worldCell,
            SectorCell sourceWorldCell)
        {
            var coordinate = WorldGridIndex.ToCoordinate(index);
            if (ownership == null || ownership.SectorIndex != index || ownership.Sector != coordinate ||
                worldCell == null || worldCell.Index != index || worldCell.Coordinate != coordinate ||
                sourceWorldCell == null || sourceWorldCell.Index != index || sourceWorldCell.Coordinate != coordinate)
                throw new ArgumentException("Ownership/export/world indices must match exact row-major identity.");
            if (!string.Equals(worldCell.PrimaryBiomeId, ownership.PrimaryBiomeId, StringComparison.Ordinal) ||
                !string.Equals(worldCell.SecondaryBiomeId, ownership.SecondaryBiomeId, StringComparison.Ordinal) ||
                !string.IsNullOrEmpty(ownership.SecondaryBiomeId))
                throw new ArgumentException("World and ownership biome values must match with no secondary biome.");
            var expectedPatchId = ownership.PatchId.HasValue ? ownership.PatchId.Value.Value : string.Empty;
            if (!string.Equals(worldCell.PatchId, expectedPatchId, StringComparison.Ordinal))
                throw new ArgumentException("World and ownership patch IDs must match.");
        }

        private static bool HasDifferentNeighbor(
            BiomePatchSnapshot snapshot,
            int index,
            int neighborIndex)
        {
            if (neighborIndex < 0) return true;
            var current = snapshot.GetSector(index).PatchId;
            var neighbor = snapshot.GetSector(neighborIndex).PatchId;
            return current.HasValue != neighbor.HasValue ||
                   (current.HasValue && current.Value != neighbor.Value);
        }

        private static int ComputePerimeter(BiomePatch patch)
        {
            var perimeter = 0;
            foreach (var sectorIndex in patch.SectorIndices)
            {
                var coordinate = WorldGridIndex.ToCoordinate(sectorIndex);
                if (coordinate.X == 0 || !patch.ContainsSector(sectorIndex - 1)) perimeter++;
                if (coordinate.X == WorldGenConstants.SectorColumns - 1 || !patch.ContainsSector(sectorIndex + 1)) perimeter++;
                if (coordinate.Y == WorldGenConstants.SectorRows - 1 || !patch.ContainsSector(sectorIndex + WorldGenConstants.SectorColumns)) perimeter++;
                if (coordinate.Y == 0 || !patch.ContainsSector(sectorIndex - WorldGenConstants.SectorColumns)) perimeter++;
            }
            return perimeter;
        }

        private static int ComputeCompactness(int size, int perimeter)
        {
            int value;
            checked { value = 16000 * size / (perimeter * perimeter); }
            if (value < 1 || value > 1000)
                throw new ArgumentException("Patch compactness must be in the range 1..1000.");
            return value;
        }

        private static bool ContainsSeed(BiomePatch patch, int sectorIndex)
        {
            foreach (var seed in patch.Seeds)
                if (seed.SectorIndex == sectorIndex) return true;
            return false;
        }

        private static bool PatchRowsMatch(GeneratedBiomePatchRow left, GeneratedBiomePatchRow right)
        {
            if (left == null || right == null || left.Seed != right.Seed ||
                left.PatchInstanceId != right.PatchInstanceId ||
                !string.Equals(left.BiomeId, right.BiomeId, StringComparison.Ordinal) ||
                left.PatchRole != right.PatchRole || left.SeedSectorX != right.SeedSectorX ||
                left.SeedSectorY != right.SeedSectorY || left.SectorCount != right.SectorCount ||
                left.MinX != right.MinX || left.MinY != right.MinY || left.MaxX != right.MaxX ||
                left.MaxY != right.MaxY || left.PerimeterEdges != right.PerimeterEdges ||
                left.SpecialMapInstanceIds.Count != right.SpecialMapInstanceIds.Count)
                return false;
            for (var index = 0; index < left.SpecialMapInstanceIds.Count; index++)
                if (left.SpecialMapInstanceIds[index] != right.SpecialMapInstanceIds[index]) return false;
            return true;
        }
    }
}
