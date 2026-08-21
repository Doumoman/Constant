using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchExporter
    {
        public BiomePatchExportResult Export(
            PatchCleanupResult cleanupResult,
            GeneratedWorldData sourceWorld)
        {
            var errors = new List<BiomePatchExportError>();
            ValidateInputs(cleanupResult, sourceWorld, errors);
            if (errors.Count != 0) return BiomePatchExportResult.Invalid(errors);

            var snapshot = cleanupResult.Publication.Snapshot;
            List<GeneratedBiomePatchRow> rows;
            GeneratedWorldData world;
            try
            {
                rows = BuildPatchRows(snapshot);
                world = BuildWorld(snapshot, sourceWorld);
            }
            catch (Exception)
            {
                Add(errors, BiomePatchExportErrorCode.InternalInvariantViolation,
                    "Export artifact construction failed.");
                return BiomePatchExportResult.Invalid(errors);
            }

            byte[] patchBytes;
            byte[] worldBytes;
            try
            {
                patchBytes = GeneratedBiomePatchCsvSerializer.Serialize(rows);
                worldBytes = GeneratedWorldDataCsvSerializer.Serialize(world);
            }
            catch (Exception)
            {
                Add(errors, BiomePatchExportErrorCode.SerializationFailure,
                    "CSV serialization failed.");
                return BiomePatchExportResult.Invalid(errors);
            }

            if (!ValidateArtifacts(snapshot, world, rows, patchBytes, worldBytes))
            {
                Add(errors, BiomePatchExportErrorCode.InternalInvariantViolation,
                    "Export artifact cross-check failed.");
                return BiomePatchExportResult.Invalid(errors);
            }

            var publication = new BiomePatchExportPublication(
                cleanupResult.Publication,
                sourceWorld,
                world,
                rows,
                patchBytes,
                worldBytes,
                snapshot.AssignedSectorCount,
                snapshot.UnassignedSectorCount);
            return BiomePatchExportResult.Completed(publication);
        }

        private static void ValidateInputs(
            PatchCleanupResult cleanupResult,
            GeneratedWorldData sourceWorld,
            ICollection<BiomePatchExportError> errors)
        {
            if (cleanupResult == null)
            {
                Add(errors, BiomePatchExportErrorCode.MissingCleanupResult,
                    "Cleanup result is required.");
            }
            else
            {
                if (cleanupResult.Status != PatchCleanupStatus.Completed)
                    Add(errors, BiomePatchExportErrorCode.CleanupNotCompleted,
                        "Cleanup result must be completed.");
                if (cleanupResult.Publication == null)
                    Add(errors, BiomePatchExportErrorCode.MissingCleanupPublication,
                        "Cleanup publication is required.");
                if (cleanupResult.Diagnostics == null)
                    Add(errors, BiomePatchExportErrorCode.MissingCleanupDiagnostics,
                        "Cleanup diagnostics are required.");
            }

            if (sourceWorld == null)
                Add(errors, BiomePatchExportErrorCode.MissingSourceWorld,
                    "Source world is required.");

            var snapshot = cleanupResult == null || cleanupResult.Publication == null
                ? null
                : cleanupResult.Publication.Snapshot;
            if (snapshot == null)
            {
                if (cleanupResult != null && cleanupResult.Publication != null)
                    Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                        "Cleanup snapshot is required.");
            }
            else
            {
                ValidateSnapshot(snapshot, errors);
            }

            if (sourceWorld != null)
                ValidateSourceWorld(sourceWorld, errors);

            if (snapshot == null || sourceWorld == null) return;
            if (snapshot.Seed != sourceWorld.Seed)
                Add(errors, BiomePatchExportErrorCode.SeedMismatch,
                    "Cleanup and world seeds must match.");

            if (snapshot.Sectors.Count != WorldGenConstants.SectorCount ||
                sourceWorld.Cells.Count != WorldGenConstants.SectorCount)
                return;

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var ownership = snapshot.Sectors[index];
                var cell = sourceWorld.Cells[index];
                var primary = ownership.IsAssigned ? ownership.PrimaryBiomeId : string.Empty;
                var secondary = ownership.IsAssigned ? ownership.SecondaryBiomeId : string.Empty;
                var patchId = ownership.IsAssigned && ownership.PatchId.HasValue
                    ? ownership.PatchId.Value.Value
                    : string.Empty;
                if (!IsEmptyOrExact(cell.PrimaryBiomeId, primary) ||
                    !IsEmptyOrExact(cell.SecondaryBiomeId, secondary) ||
                    !IsEmptyOrExact(cell.PatchId, patchId))
                {
                    Add(errors, BiomePatchExportErrorCode.ConflictingExistingBiomeAssignment,
                        "Source world contains a conflicting biome assignment.",
                        patchId, index);
                }
            }
        }

        private static void ValidateSnapshot(
            BiomePatchSnapshot snapshot,
            ICollection<BiomePatchExportError> errors)
        {
            if (snapshot.Sectors.Count != WorldGenConstants.SectorCount)
            {
                Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                    "Patch snapshot must contain exactly 169 ownership rows.",
                    string.Empty, -1, WorldGenConstants.SectorCount, snapshot.Sectors.Count);
                return;
            }
            if (snapshot.Patches.Count == 0 || snapshot.AssignedSectorCount == 0 ||
                snapshot.AssignedSectorCount + snapshot.UnassignedSectorCount != WorldGenConstants.SectorCount)
            {
                Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                    "Patch snapshot counts are invalid.");
            }

            var patchById = new Dictionary<BiomePatchId, BiomePatch>();
            foreach (var patch in snapshot.Patches)
            {
                if (patch == null || !patch.Id.IsValid || patch.SectorCount == 0 ||
                    !patchById.TryAdd(patch.Id, patch))
                {
                    Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                        "Patch identity or membership is invalid.");
                    continue;
                }
                foreach (var seed in patch.Seeds)
                    if (seed == null || !patch.ContainsSector(seed.SectorIndex))
                        Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                            "Patch seed does not belong to its patch.", patch.Id.Value,
                            seed == null ? -1 : seed.SectorIndex);
            }

            var assigned = 0;
            for (var index = 0; index < snapshot.Sectors.Count; index++)
            {
                var ownership = snapshot.Sectors[index];
                if (ownership == null || ownership.SectorIndex != index ||
                    ownership.Sector.X != index % WorldGenConstants.SectorColumns ||
                    ownership.Sector.Y != index / WorldGenConstants.SectorColumns)
                {
                    Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                        "Ownership index and coordinate must be row-major.", string.Empty, index);
                    continue;
                }
                if (!ownership.IsAssigned)
                {
                    if (ownership.PrimaryBiomeId.Length != 0 || ownership.SecondaryBiomeId.Length != 0 ||
                        ownership.PatchId.HasValue)
                        Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                            "Unassigned ownership must be empty.", string.Empty, index);
                    continue;
                }

                assigned++;
                if (!ownership.PatchId.HasValue ||
                    !patchById.TryGetValue(ownership.PatchId.Value, out var patch) ||
                    !patch.ContainsSector(index) ||
                    !string.Equals(ownership.PrimaryBiomeId, patch.BiomeId, StringComparison.Ordinal))
                    Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                        "Ownership does not match its patch.",
                        ownership.PatchId.HasValue ? ownership.PatchId.Value.Value : string.Empty,
                        index);
            }
            if (assigned != snapshot.AssignedSectorCount)
                Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                    "Assigned-sector count does not match ownership rows.",
                    string.Empty, -1, snapshot.AssignedSectorCount, assigned);

            foreach (var patch in snapshot.Patches)
                foreach (var sectorIndex in patch.SectorIndices)
                    if (sectorIndex < 0 || sectorIndex >= snapshot.Sectors.Count ||
                        !snapshot.Sectors[sectorIndex].IsAssigned ||
                        !snapshot.Sectors[sectorIndex].PatchId.HasValue ||
                        snapshot.Sectors[sectorIndex].PatchId.Value != patch.Id)
                        Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                            "Patch membership has no matching ownership.", patch.Id.Value, sectorIndex);

            foreach (var binding in snapshot.SiteBindings)
            {
                if (binding == null || !patchById.TryGetValue(binding.PatchId, out var patch) ||
                    patch.Role != BiomePatchRole.Core ||
                    !string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal))
                    Add(errors, BiomePatchExportErrorCode.InvalidPatchSnapshot,
                        "Site binding does not match a Core patch.",
                        binding == null ? string.Empty : binding.SiteReservationId.Value);
            }
        }

        private static void ValidateSourceWorld(
            GeneratedWorldData sourceWorld,
            ICollection<BiomePatchExportError> errors)
        {
            if (sourceWorld.Cells.Count != WorldGenConstants.SectorCount)
            {
                Add(errors, BiomePatchExportErrorCode.InvalidSourceWorld,
                    "Source world must contain exactly 169 sectors.",
                    string.Empty, -1, WorldGenConstants.SectorCount, sourceWorld.Cells.Count);
                return;
            }
            for (var index = 0; index < sourceWorld.Cells.Count; index++)
            {
                var cell = sourceWorld.Cells[index];
                if (cell == null || cell.Index != index ||
                    cell.Coordinate.X != index % WorldGenConstants.SectorColumns ||
                    cell.Coordinate.Y != index / WorldGenConstants.SectorColumns)
                    Add(errors, BiomePatchExportErrorCode.InvalidSourceWorld,
                        "Source world index and coordinate must be row-major.", string.Empty, index);
            }
        }

        private static List<GeneratedBiomePatchRow> BuildPatchRows(BiomePatchSnapshot snapshot)
        {
            var result = new List<GeneratedBiomePatchRow>(snapshot.Patches.Count);
            foreach (var patch in snapshot.Patches)
            {
                var minX = int.MaxValue;
                var minY = int.MaxValue;
                var maxX = int.MinValue;
                var maxY = int.MinValue;
                var sectors = new HashSet<int>(patch.SectorIndices);
                var perimeter = 0;
                foreach (var sectorIndex in patch.SectorIndices)
                {
                    var x = sectorIndex % WorldGenConstants.SectorColumns;
                    var y = sectorIndex / WorldGenConstants.SectorColumns;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                    if (x == 0 || !sectors.Contains(sectorIndex - 1)) perimeter++;
                    if (x == WorldGenConstants.SectorColumns - 1 || !sectors.Contains(sectorIndex + 1)) perimeter++;
                    if (y == 0 || !sectors.Contains(sectorIndex - WorldGenConstants.SectorColumns)) perimeter++;
                    if (y == WorldGenConstants.SectorRows - 1 || !sectors.Contains(sectorIndex + WorldGenConstants.SectorColumns)) perimeter++;
                }

                var representative = patch.Seeds[0];
                for (var index = 1; index < patch.Seeds.Count; index++)
                    if (patch.Seeds[index].SectorIndex < representative.SectorIndex)
                        representative = patch.Seeds[index];

                var siteIds = new List<SiteReservationId>();
                if (patch.Role == BiomePatchRole.Core)
                {
                    foreach (var binding in snapshot.SiteBindings)
                        if (binding.PatchId == patch.Id && !siteIds.Contains(binding.SiteReservationId))
                            siteIds.Add(binding.SiteReservationId);
                    siteIds.Sort();
                }

                result.Add(new GeneratedBiomePatchRow(
                    snapshot.Seed,
                    patch.Id,
                    patch.BiomeId,
                    patch.Role,
                    representative.Sector.X,
                    representative.Sector.Y,
                    patch.SectorCount,
                    minX,
                    minY,
                    maxX,
                    maxY,
                    perimeter,
                    siteIds));
            }
            result.Sort((left, right) => left.PatchInstanceId.CompareTo(right.PatchInstanceId));
            return result;
        }

        private static GeneratedWorldData BuildWorld(
            BiomePatchSnapshot snapshot,
            GeneratedWorldData sourceWorld)
        {
            var cells = new List<SectorCell>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var source = sourceWorld.Cells[index];
                var ownership = snapshot.Sectors[index];
                cells.Add(new SectorCell(
                    source.Index,
                    source.Coordinate,
                    source.Role,
                    ownership.IsAssigned ? ownership.PrimaryBiomeId : string.Empty,
                    ownership.IsAssigned ? ownership.SecondaryBiomeId : string.Empty,
                    ownership.IsAssigned ? ownership.PatchId.Value.Value : string.Empty,
                    source.RouteMaskId,
                    source.SpecialSiteInstanceId,
                    source.BoundaryProfileId,
                    source.SectorRecipeId,
                    source.ReservationId,
                    source.ShortestDistanceFromStart,
                    source.MandatoryGraphNode));
            }
            return new GeneratedWorldData(sourceWorld.Seed, cells);
        }

        private static bool ValidateArtifacts(
            BiomePatchSnapshot snapshot,
            GeneratedWorldData world,
            IReadOnlyList<GeneratedBiomePatchRow> rows,
            byte[] patchBytes,
            byte[] worldBytes)
        {
            if (rows.Count != snapshot.Patches.Count || world.Cells.Count != WorldGenConstants.SectorCount)
                return false;
            var sectorSum = 0;
            var ids = new HashSet<BiomePatchId>();
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (row.Seed != snapshot.Seed || !ids.Add(row.PatchInstanceId)) return false;
                if (index > 0 && row.PatchInstanceId.CompareTo(rows[index - 1].PatchInstanceId) <= 0) return false;
                if (!snapshot.TryGetPatch(row.PatchInstanceId, out var patch) ||
                    patch.SectorCount != row.SectorCount ||
                    !string.Equals(patch.BiomeId, row.BiomeId, StringComparison.Ordinal) ||
                    patch.Role != row.PatchRole)
                    return false;
                sectorSum += row.SectorCount;
            }
            if (sectorSum != snapshot.AssignedSectorCount) return false;

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var ownership = snapshot.Sectors[index];
                var cell = world.Cells[index];
                if (cell.Index != index || cell.Coordinate != ownership.Sector) return false;
                if (ownership.IsAssigned)
                {
                    if (!string.Equals(cell.PrimaryBiomeId, ownership.PrimaryBiomeId, StringComparison.Ordinal) ||
                        !string.Equals(cell.SecondaryBiomeId, ownership.SecondaryBiomeId, StringComparison.Ordinal) ||
                        !string.Equals(cell.PatchId, ownership.PatchId.Value.Value, StringComparison.Ordinal))
                        return false;
                }
                else if (cell.PrimaryBiomeId.Length != 0 || cell.SecondaryBiomeId.Length != 0 || cell.PatchId.Length != 0)
                    return false;
            }

            return HasExactCsvShape(patchBytes, GeneratedBiomePatchCsvSerializer.Header, rows.Count + 1) &&
                   HasExactCsvShape(worldBytes, GeneratedWorldDataCsvSerializer.Header,
                       WorldGenConstants.SectorCount + 1);
        }

        private static bool HasExactCsvShape(byte[] bytes, string header, int recordCount)
        {
            if (bytes == null || bytes.Length < 5 || bytes[0] != 0xEF || bytes[1] != 0xBB || bytes[2] != 0xBF)
                return false;
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (!text.StartsWith(header + "\r\n", StringComparison.Ordinal) ||
                !text.EndsWith("\r\n", StringComparison.Ordinal) ||
                text.EndsWith("\r\n\r\n", StringComparison.Ordinal))
                return false;
            var count = 0;
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n')
                {
                    if (index == 0 || text[index - 1] != '\r') return false;
                    count++;
                }
                else if (text[index] == '\r' && (index + 1 >= text.Length || text[index + 1] != '\n'))
                    return false;
            }
            return count == recordCount;
        }

        private static bool IsEmptyOrExact(string actual, string expected)
        {
            return actual.Length == 0 || string.Equals(actual, expected, StringComparison.Ordinal);
        }

        private static void Add(
            ICollection<BiomePatchExportError> errors,
            BiomePatchExportErrorCode code,
            string message,
            string definitionId = "",
            int sectorIndex = -1,
            int requiredCount = -1,
            int availableCount = -1)
        {
            errors.Add(new BiomePatchExportError(
                code, definitionId, sectorIndex, requiredCount, availableCount, message));
        }
    }
}
