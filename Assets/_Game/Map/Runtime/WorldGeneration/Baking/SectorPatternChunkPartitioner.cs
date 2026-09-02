using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class SectorPatternChunkPartitioner
    {
        public static PatternChunkPartitionResult Partition(
            SectorFinalCanvasLayerPlan canvasPlan,
            SectorCanvasProtectionDensityReport protectionDensityReport,
            SectorFinalRouteRecoveryReport routeRecoveryReport) => Partition(
                PatternChunkPartitionRequest.FromAuthorities(
                    canvasPlan, protectionDensityReport, routeRecoveryReport));

        public static PatternChunkPartitionResult Partition(PatternChunkPartitionRequest request)
        {
            if (request == null)
                return Failure(null, PatternChunkPartitionFailureCode.MissingRequest,
                    "REQUEST", "Pattern chunk partition request is required.");

            var failures = new List<PatternChunkPartitionFailure>();
            ValidateAuthorities(request, failures);
            ValidateConstants(request, failures);
            ValidateCoordinates(request, failures);
            ValidateForbiddenOperations(request, failures);
            if (failures.Count > 0)
                return new PatternChunkPartitionResult(request, null, failures);

            var tileAddresses = request.TileCoordinates.Select(CreateTileAddress)
                .OrderBy(value => value).ToArray();
            var patternAddresses = request.PatternCoordinates.Select(CreatePatternAddress)
                .OrderBy(value => value).ToArray();
            var witnessProjections = ProjectWitnesses(
                request.RouteRecoveryReport, tileAddresses, failures);
            if (failures.Count > 0)
                return new PatternChunkPartitionResult(request, null, failures);

            var slots = Enumerable.Range(0, SectorPatternChunkPartition.ChunkCount)
                .Select(index =>
                {
                    var coordinate = new MicroChunkCoordinate(
                        index % SectorPatternChunkPartition.ChunkGridWidth,
                        index / SectorPatternChunkPartition.ChunkGridWidth);
                    return new MicroChunkSlot(
                        coordinate,
                        tileAddresses.Where(value => value.ChunkIndex == index),
                        patternAddresses.Where(value => value.ChunkIndex == index));
                }).ToArray();
            var partition = new SectorPatternChunkPartition(
                request, slots, tileAddresses, patternAddresses, witnessProjections);
            ValidatePartition(partition, failures);
            return failures.Count == 0
                ? new PatternChunkPartitionResult(request, partition, failures)
                : new PatternChunkPartitionResult(request, null, failures);
        }

        private static void ValidateAuthorities(
            PatternChunkPartitionRequest request,
            ICollection<PatternChunkPartitionFailure> failures)
        {
            var canvas = request.CanvasPlan;
            if (canvas == null)
            {
                Add(failures, PatternChunkPartitionFailureCode.MissingCanvasPlan,
                    "MAP16_01", "Successful final canvas layer plan is required.");
            }
            else
            {
                if (canvas.Request == null ||
                    canvas.Request.Width != SectorPatternChunkPartition.SectorWidth ||
                    canvas.Request.Height != SectorPatternChunkPartition.SectorHeight ||
                    canvas.ObservedCellCount != SectorPatternChunkPartition.SectorCellCount ||
                    canvas.UniqueCoordinateCount != SectorPatternChunkPartition.SectorCellCount ||
                    canvas.OutOfBoundsCellCount != 0 || canvas.MissingLayerKindCount != 0)
                    Add(failures, PatternChunkPartitionFailureCode.InvalidCanvasPlan,
                        "MAP16_01", "Canvas must be a complete 48x32, 1536-cell accepted plan.");
                if (!PatternChunkPartitionDigest.IsLowerHexSha256(canvas.InputDigest) ||
                    !PatternChunkPartitionDigest.IsLowerHexSha256(canvas.OutputDigest))
                    Add(failures, PatternChunkPartitionFailureCode.InvalidDigest,
                        "MAP16_01", "Canvas input/output digests must be lower-hex SHA-256.");
            }

            var density = request.ProtectionDensityReport;
            if (density == null)
            {
                Add(failures, PatternChunkPartitionFailureCode.MissingProtectionDensityReport,
                    "MAP16_02", "Successful protection-density report is required.");
            }
            else
            {
                if (canvas == null || !ReferenceEquals(density.SourcePlan, canvas))
                    Add(failures, PatternChunkPartitionFailureCode.SourceMismatch,
                        "MAP16_02", "Protection-density report must reference the supplied canvas plan.");
                if (density.ObservedCellCount != SectorPatternChunkPartition.SectorCellCount ||
                    density.UniqueCoordinateCount != SectorPatternChunkPartition.SectorCellCount ||
                    density.OutOfBoundsCellCount != 0 || density.MissingLayerKindCount != 0 ||
                    density.ProtectionIntrusionCount != 0 || density.DensityBudgetViolationCount != 0 ||
                    density.UnownedAirViolationCount != 0 || density.CleanupProjection == null ||
                    !density.CleanupProjection.IsSafe)
                    Add(failures, PatternChunkPartitionFailureCode.InvalidProtectionDensityReport,
                        "MAP16_02", "Protection-density report must retain accepted coverage and safety.");
                if (!PatternChunkPartitionDigest.IsLowerHexSha256(density.InputDigest) ||
                    !PatternChunkPartitionDigest.IsLowerHexSha256(density.OutputDigest))
                    Add(failures, PatternChunkPartitionFailureCode.InvalidDigest,
                        "MAP16_02", "Protection-density digests must be lower-hex SHA-256.");
            }

            var route = request.RouteRecoveryReport;
            if (route == null)
            {
                Add(failures, PatternChunkPartitionFailureCode.MissingRouteRecoveryReport,
                    "MAP16_03", "Successful final route-recovery report is required.");
            }
            else
            {
                if (canvas == null || density == null ||
                    !ReferenceEquals(route.SourceCanvasPlan, canvas) ||
                    !ReferenceEquals(route.SourceProtectionDensityReport, density))
                    Add(failures, PatternChunkPartitionFailureCode.SourceMismatch,
                        "MAP16_03", "Route-recovery report must reference the supplied MAP16_01/02 authorities.");
                if (route.Width != SectorPatternChunkPartition.SectorWidth ||
                    route.Height != SectorPatternChunkPartition.SectorHeight ||
                    route.ObservedCellCount != SectorPatternChunkPartition.SectorCellCount ||
                    route.UniqueCoordinateCount != SectorPatternChunkPartition.SectorCellCount ||
                    route.BaseRouteWitnessMissingCount != 0 ||
                    route.ExternalSocketWitnessMissingCount != 0 ||
                    route.BoundaryApertureWitnessMissingCount != 0 ||
                    route.SpecialEntranceWitnessMissingCount != 0 ||
                    route.HighFailureSampleMissingCount != 0 ||
                    route.RecoveryWitnessMissingCount != 0 ||
                    route.BlockedCellCrossingCount != 0 ||
                    route.StaticSoftlockCandidateCount != 0)
                    Add(failures, PatternChunkPartitionFailureCode.InvalidRouteRecoveryReport,
                        "MAP16_03", "Route-recovery report must retain all accepted witnesses and zero blockers.");
                if (!PatternChunkPartitionDigest.IsLowerHexSha256(route.InputDigest) ||
                    !PatternChunkPartitionDigest.IsLowerHexSha256(route.OutputDigest))
                    Add(failures, PatternChunkPartitionFailureCode.InvalidDigest,
                        "MAP16_03", "Route-recovery digests must be lower-hex SHA-256.");
            }

            if (!PatternChunkPartitionDigest.IsLowerHexSha256(request.CanonicalDigest))
                Add(failures, PatternChunkPartitionFailureCode.InvalidDigest,
                    "INPUT", "Partition input digest must be lower-hex SHA-256.");
        }

        private static void ValidateConstants(
            PatternChunkPartitionRequest request,
            ICollection<PatternChunkPartitionFailure> failures)
        {
            if (request.SectorWidth != SectorPatternChunkPartition.SectorWidth ||
                request.SectorHeight != SectorPatternChunkPartition.SectorHeight ||
                request.MicroPatternWidth != SectorPatternChunkPartition.MicroPatternWidth ||
                request.MicroPatternHeight != SectorPatternChunkPartition.MicroPatternHeight ||
                request.MicroChunkWidth != SectorPatternChunkPartition.MicroChunkWidth ||
                request.MicroChunkHeight != SectorPatternChunkPartition.MicroChunkHeight)
                Add(failures, PatternChunkPartitionFailureCode.InvalidDimensions,
                    "CONSTANTS", "Required dimensions are sector 48x32, pattern 4x4, and chunk 12x8.");

            if (request.SectorWidth <= 0 || request.SectorHeight <= 0 ||
                request.MicroPatternWidth <= 0 || request.MicroPatternHeight <= 0 ||
                request.MicroChunkWidth <= 0 || request.MicroChunkHeight <= 0 ||
                request.SectorWidth % request.MicroChunkWidth != 0 ||
                request.SectorHeight % request.MicroChunkHeight != 0 ||
                request.MicroChunkWidth % request.MicroPatternWidth != 0 ||
                request.MicroChunkHeight % request.MicroPatternHeight != 0)
                Add(failures, PatternChunkPartitionFailureCode.NonDivisibleConstants,
                    "CONSTANTS", "Sector and MicroChunk dimensions must divide exactly by their child grids.");

            if (request.RotateNinetyDegrees ||
                (request.MicroChunkWidth == SectorPatternChunkPartition.MicroChunkHeight &&
                 request.MicroChunkHeight == SectorPatternChunkPartition.MicroChunkWidth))
                Add(failures, PatternChunkPartitionFailureCode.RotationForbidden,
                    "ROTATION", "A 12x8 MicroChunk cannot be rotated by 90 degrees.");
        }

        private static void ValidateCoordinates(
            PatternChunkPartitionRequest request,
            ICollection<PatternChunkPartitionFailure> failures)
        {
            var tiles = request.TileCoordinates;
            var tileUniqueCount = tiles.Distinct().Count();
            if (tiles.Count != SectorPatternChunkPartition.SectorCellCount)
                Add(failures, PatternChunkPartitionFailureCode.InvalidCellCount,
                    "TILES", "Tile coordinate count must be 1536; observed " + Number(tiles.Count) + ".");
            if (request.NullTileCoordinateCount > 0 || tiles.Any(value => !value.IsInBounds))
                Add(failures, PatternChunkPartitionFailureCode.OutOfBoundsTileCoordinate,
                    "TILES", "Tile coordinates must be non-null and inside 48x32.");
            if (tileUniqueCount != tiles.Count)
                Add(failures, PatternChunkPartitionFailureCode.DuplicateTileCoordinate,
                    "TILES", "Tile coordinate authority contains duplicates.");
            if (MissingTileCount(tiles) != 0)
                Add(failures, PatternChunkPartitionFailureCode.MissingTileCoordinate,
                    "TILES", "Tile coordinate authority has missing sector coordinates.");

            var patterns = request.PatternCoordinates;
            var patternUniqueCount = patterns.Distinct().Count();
            if (patterns.Count != SectorPatternChunkPartition.SectorPatternCellCount)
                Add(failures, PatternChunkPartitionFailureCode.InvalidCellCount,
                    "PATTERNS", "Pattern coordinate count must be 96; observed " + Number(patterns.Count) + ".");
            if (request.NullPatternCoordinateCount > 0 || patterns.Any(value => !value.IsInBounds))
                Add(failures, PatternChunkPartitionFailureCode.OutOfBoundsPatternCoordinate,
                    "PATTERNS", "Pattern coordinates must be non-null and inside 12x8.");
            if (patternUniqueCount != patterns.Count)
                Add(failures, PatternChunkPartitionFailureCode.DuplicatePatternCoordinate,
                    "PATTERNS", "Pattern coordinate authority contains duplicates.");
            if (MissingPatternCount(patterns) != 0)
                Add(failures, PatternChunkPartitionFailureCode.MissingPatternCoordinate,
                    "PATTERNS", "Pattern coordinate authority has missing sector pattern coordinates.");

            if (request.RouteRecoveryReport != null)
            {
                var witnessCoordinates = request.RouteRecoveryReport.Witnesses
                    .SelectMany(value => value.Path)
                    .Concat(request.RouteRecoveryReport.RecoveryWitnesses
                        .SelectMany(value => value.Path));
                if (witnessCoordinates.Any(value => value == null || !value.IsInBounds))
                    Add(failures, PatternChunkPartitionFailureCode.OutOfBoundsTileCoordinate,
                        "WITNESSES", "Every route/recovery witness coordinate must be inside 48x32.");
            }
        }

        private static void ValidateForbiddenOperations(
            PatternChunkPartitionRequest request,
            ICollection<PatternChunkPartitionFailure> failures)
        {
            if (request.ForbiddenOperationCount != 0)
                Add(failures, PatternChunkPartitionFailureCode.ForbiddenOperation,
                    "NON_OWNERSHIP", "Partitioning cannot copy layers, build slices/sockets, mutate Unity objects, write files, reroll, carve, widen, spawn, approve seeds, or run regression.");
        }

        private static PatternChunkCellAddress CreateTileAddress(SectorTileCoordinate coordinate)
        {
            var chunk = new MicroChunkCoordinate(
                coordinate.X / SectorPatternChunkPartition.MicroChunkWidth,
                coordinate.Y / SectorPatternChunkPartition.MicroChunkHeight);
            var localTile = new MicroChunkLocalTileCoordinate(
                coordinate.X % SectorPatternChunkPartition.MicroChunkWidth,
                coordinate.Y % SectorPatternChunkPartition.MicroChunkHeight);
            var pattern = new MicroPatternCoordinate(
                coordinate.X / SectorPatternChunkPartition.MicroPatternWidth,
                coordinate.Y / SectorPatternChunkPartition.MicroPatternHeight);
            var localPatternCell = new MicroPatternLocalCellCoordinate(
                coordinate.X % SectorPatternChunkPartition.MicroPatternWidth,
                coordinate.Y % SectorPatternChunkPartition.MicroPatternHeight);
            return new PatternChunkCellAddress(
                coordinate, chunk, localTile, pattern, localPatternCell);
        }

        private static PatternChunkPatternAddress CreatePatternAddress(
            MicroPatternCoordinate coordinate)
        {
            var chunk = new MicroChunkCoordinate(
                coordinate.X / SectorPatternChunkPartition.ChunkPatternGridWidth,
                coordinate.Y / SectorPatternChunkPartition.ChunkPatternGridHeight);
            var local = new MicroChunkLocalPatternCoordinate(
                coordinate.X % SectorPatternChunkPartition.ChunkPatternGridWidth,
                coordinate.Y % SectorPatternChunkPartition.ChunkPatternGridHeight);
            return new PatternChunkPatternAddress(coordinate, chunk, local);
        }

        private static RouteRecoveryWitnessChunkProjection[] ProjectWitnesses(
            SectorFinalRouteRecoveryReport report,
            IReadOnlyList<PatternChunkCellAddress> addresses,
            ICollection<PatternChunkPartitionFailure> failures)
        {
            var byCoordinate = addresses.ToDictionary(value => value.SectorCoordinate);
            var projections = new List<RouteRecoveryWitnessChunkProjection>();
            foreach (var witness in report.Witnesses.OrderBy(value => value))
            {
                for (var index = 0; index < witness.Path.Count; index++)
                    AddProjection("ROUTE_" + witness.Kind.ToString().ToUpperInvariant(),
                        witness.StableId, index, witness.Path[index], byCoordinate,
                        projections, failures);
            }
            foreach (var witness in report.RecoveryWitnesses.OrderBy(value => value))
            {
                for (var index = 0; index < witness.Path.Count; index++)
                    AddProjection("RECOVERY_" + witness.Kind.ToString().ToUpperInvariant(),
                        witness.StableId, index, witness.Path[index], byCoordinate,
                        projections, failures);
            }
            return projections.OrderBy(value => value).ToArray();
        }

        private static void AddProjection(
            string kind,
            string stableId,
            int pathIndex,
            FinalCanvasCellCoordinate coordinate,
            IReadOnlyDictionary<SectorTileCoordinate, PatternChunkCellAddress> byCoordinate,
            ICollection<RouteRecoveryWitnessChunkProjection> projections,
            ICollection<PatternChunkPartitionFailure> failures)
        {
            var key = coordinate == null
                ? null
                : new SectorTileCoordinate(coordinate.X, coordinate.Y);
            PatternChunkCellAddress address;
            if (key == null || !byCoordinate.TryGetValue(key, out address))
            {
                Add(failures, PatternChunkPartitionFailureCode.MissingTileCoordinate,
                    stableId, "Witness coordinate is absent from the partition authority.");
                return;
            }
            projections.Add(new RouteRecoveryWitnessChunkProjection(
                kind, stableId, pathIndex, address));
        }

        private static void ValidatePartition(
            SectorPatternChunkPartition partition,
            ICollection<PatternChunkPartitionFailure> failures)
        {
            if (partition.ChunkSlots.Count != SectorPatternChunkPartition.ChunkCount ||
                partition.ChunkSlots.Any(value =>
                    value.TileCount != SectorPatternChunkPartition.ChunkCellCount ||
                    value.PatternCount != SectorPatternChunkPartition.ChunkPatternCellCount))
                Add(failures, PatternChunkPartitionFailureCode.InvalidCellCount,
                    "CHUNKS", "Partition must publish 16 slots with 96 tiles and 6 patterns each.");
            if (partition.ChunkIndexMismatchCount != 0)
                Add(failures, PatternChunkPartitionFailureCode.ChunkIndexMismatch,
                    "CHUNKS", "Chunk index must equal chunkY * 4 + chunkX.");
            if (partition.TileAssignmentCount != SectorPatternChunkPartition.SectorCellCount ||
                partition.CoverageCount != SectorPatternChunkPartition.SectorCellCount ||
                partition.DuplicateTileAssignmentCount != 0 ||
                partition.MissingTileAssignmentCount != 0 ||
                partition.OutOfBoundsTileAssignmentCount != 0)
                Add(failures, PatternChunkPartitionFailureCode.InvalidCellCount,
                    "TILES", "Partition tile coverage must be unique, complete, and in bounds.");
            if (partition.PatternAssignmentCount != SectorPatternChunkPartition.SectorPatternCellCount ||
                partition.PatternCoverageCount != SectorPatternChunkPartition.SectorPatternCellCount ||
                partition.DuplicatePatternAssignmentCount != 0 ||
                partition.MissingPatternAssignmentCount != 0 ||
                partition.OutOfBoundsPatternAssignmentCount != 0)
                Add(failures, PatternChunkPartitionFailureCode.InvalidCellCount,
                    "PATTERNS", "Partition pattern coverage must be unique, complete, and in bounds.");
            if (partition.TileRoundTripMismatchCount != 0 ||
                partition.LocalPatternCellRoundTripMismatchCount != 0)
                Add(failures, PatternChunkPartitionFailureCode.TileRoundTripMismatch,
                    "TILES", "Tile/chunk/local-pattern-cell round-trip must be exact.");
            if (partition.PatternRoundTripMismatchCount != 0)
                Add(failures, PatternChunkPartitionFailureCode.PatternRoundTripMismatch,
                    "PATTERNS", "Pattern/chunk/local-pattern round-trip must be exact.");
            if (partition.RotationRequestCount != 0)
                Add(failures, PatternChunkPartitionFailureCode.RotationForbidden,
                    "ROTATION", "Successful partition cannot rotate a 12x8 chunk.");
            if (partition.WitnessProjections.Count != ExpectedWitnessCoordinateCount(
                    partition.SourceRouteRecoveryReport))
                Add(failures, PatternChunkPartitionFailureCode.MissingTileCoordinate,
                    "WITNESSES", "Every route/recovery witness path coordinate must be projected.");
            if (!PatternChunkPartitionDigest.IsLowerHexSha256(partition.InputDigest) ||
                !PatternChunkPartitionDigest.IsLowerHexSha256(partition.OutputDigest))
                Add(failures, PatternChunkPartitionFailureCode.InvalidDigest,
                    "PARTITION", "Partition input/output digests must be lower-hex SHA-256.");
        }

        private static int MissingTileCount(IEnumerable<SectorTileCoordinate> source)
        {
            var set = new HashSet<SectorTileCoordinate>(source.Where(value => value.IsInBounds));
            var count = 0;
            for (var y = 0; y < SectorPatternChunkPartition.SectorHeight; y++)
            for (var x = 0; x < SectorPatternChunkPartition.SectorWidth; x++)
                if (!set.Contains(new SectorTileCoordinate(x, y))) count++;
            return count;
        }

        private static int MissingPatternCount(IEnumerable<MicroPatternCoordinate> source)
        {
            var set = new HashSet<MicroPatternCoordinate>(source.Where(value => value.IsInBounds));
            var count = 0;
            for (var y = 0; y < SectorPatternChunkPartition.SectorPatternGridHeight; y++)
            for (var x = 0; x < SectorPatternChunkPartition.SectorPatternGridWidth; x++)
                if (!set.Contains(new MicroPatternCoordinate(x, y))) count++;
            return count;
        }

        private static int ExpectedWitnessCoordinateCount(SectorFinalRouteRecoveryReport report) =>
            report == null ? 0 : report.Witnesses.Sum(value => value.Path.Count) +
                                 report.RecoveryWitnesses.Sum(value => value.Path.Count);

        private static PatternChunkPartitionResult Failure(
            PatternChunkPartitionRequest request,
            PatternChunkPartitionFailureCode code,
            string subject,
            string reason) => new PatternChunkPartitionResult(request, null, new[]
        {
            new PatternChunkPartitionFailure(code, subject, reason),
        });

        private static void Add(
            ICollection<PatternChunkPartitionFailure> failures,
            PatternChunkPartitionFailureCode code,
            string subject,
            string reason) => failures.Add(new PatternChunkPartitionFailure(
                code, subject, reason));

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }
}
