using System;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SeedReplayRecorder
    {
        public SeedReplayBundle Record(
            WorldGenerationExecutionResult execution,
            ContentVersionHash contentVersionHash,
            string generatorBuildId)
        {
            if (execution == null) throw new ArgumentNullException(nameof(execution));
            if (contentVersionHash == null) throw new ArgumentNullException(nameof(contentVersionHash));
            if (generatorBuildId == null) throw new ArgumentNullException(nameof(generatorBuildId));
            if (generatorBuildId.Length == 0)
                throw new ArgumentException("Generator build identifier must be non-empty.", nameof(generatorBuildId));
            if (!TryGetGridCheckpoint(execution, out var record, out var grid))
                throw new ArgumentException("Execution is not an exact successful PASS_GRID checkpoint.", nameof(execution));
            if (record.DurationMilliseconds > int.MaxValue)
                throw new ArgumentException("Execution duration exceeds the seed manifest Int32 range.", nameof(execution));

            var manifest = new SeedManifest(
                record.WorldProfileId,
                record.WorldSeed,
                contentVersionHash.Hex,
                record.GenerationProfileId,
                generatorBuildId,
                false,
                record.StartedUtc,
                (int)record.DurationMilliseconds,
                record.RetryCountTotal,
                Array.Empty<string>(),
                SeedManifest.GridCheckpointNotes);
            var manifestBytes = SeedManifestCsvSerializer.Serialize(manifest);
            var sectorsBytes = GeneratedWorldDataCsvSerializer.Serialize(grid.WorldData);
            return new SeedReplayBundle(
                manifest,
                SeedReplayBundle.GetRelativeDirectory(manifest.WorldProfileId, manifest.Seed),
                manifestBytes,
                sectorsBytes);
        }

        internal static bool TryGetGridCheckpoint(
            WorldGenerationExecutionResult execution,
            out WorldGenerationExecutionRecord record,
            out GridInitializationResult grid)
        {
            record = null;
            grid = null;
            if (execution == null || execution.Result == null || execution.ExecutionRecord == null)
                return false;
            record = execution.ExecutionRecord;
            if (!execution.Result.Succeeded || !record.Succeeded ||
                !string.Equals(record.InclusivePassId, GridInitializationPass.PassId, StringComparison.Ordinal) ||
                !string.Equals(execution.Result.LastCompletedPassId, GridInitializationPass.PassId, StringComparison.Ordinal) ||
                !string.Equals(record.LastCompletedPassId, GridInitializationPass.PassId, StringComparison.Ordinal) ||
                record.PassCount != 1 || record.Passes.Count != 1 ||
                record.AttemptCount != 1 || record.RetryCountTotal != 0 ||
                string.IsNullOrEmpty(record.WorldProfileId) || string.IsNullOrEmpty(record.GenerationProfileId))
                return false;

            var pass = record.Passes[0];
            if (pass == null ||
                !string.Equals(pass.PassId, GridInitializationPass.PassId, StringComparison.Ordinal) ||
                !pass.Succeeded || pass.Terminal ||
                pass.WorldSeed != record.WorldSeed ||
                pass.AttemptCount != 1 || pass.RetryCount != 0 || pass.Attempts.Count != 1)
                return false;
            var attempt = pass.Attempts[0];
            if (attempt == null || !attempt.Succeeded ||
                attempt.WorldSeed != record.WorldSeed || attempt.AttemptOrdinal != 0)
                return false;
            if (execution.Result.Artifacts.Count != 1 ||
                execution.Result.Artifacts.ArtifactIds.Count != 1 ||
                !string.Equals(execution.Result.Artifacts.ArtifactIds[0], GridInitializationPass.OutputArtifactId, StringComparison.Ordinal) ||
                !execution.Result.Artifacts.TryGet<GridInitializationResult>(GridInitializationPass.OutputArtifactId, out grid) ||
                grid == null || grid.WorldData == null || grid.WorldData.Seed != record.WorldSeed)
                return false;
            return IsExactNeutralGrid(grid);
        }

        private static bool IsExactNeutralGrid(GridInitializationResult grid)
        {
            if (grid.WorldData.Cells.Count != WorldGenConstants.SectorCount ||
                grid.Neighbors.Count != WorldGenConstants.SectorCount)
                return false;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var cell = grid.WorldData.Cells[index];
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (cell == null || cell.Index != index || cell.Coordinate != coordinate ||
                    cell.Role != GeneratedSectorRole.Unassigned ||
                    cell.PrimaryBiomeId.Length != 0 || cell.SecondaryBiomeId.Length != 0 ||
                    cell.PatchId.Length != 0 || cell.RouteMaskId.Length != 0 ||
                    cell.SpecialSiteInstanceId.Length != 0 || cell.BoundaryProfileId.Length != 0 ||
                    cell.SectorRecipeId.Length != 0 || cell.ReservationId.Length != 0 ||
                    cell.ShortestDistanceFromStart != -1 || cell.MandatoryGraphNode)
                    return false;

                var neighbors = grid.Neighbors[index];
                if (neighbors == null || neighbors.Index != index ||
                    neighbors.LeftIndex != WorldGridIndex.GetLeftIndex(index) ||
                    neighbors.RightIndex != WorldGridIndex.GetRightIndex(index) ||
                    neighbors.UpIndex != WorldGridIndex.GetUpIndex(index) ||
                    neighbors.DownIndex != WorldGridIndex.GetDownIndex(index))
                    return false;
            }
            return true;
        }
    }
}
