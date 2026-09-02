using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedMicroChunkSliceBuilder
    {
        private static readonly GeneratedMicroChunkSocketSide[] SocketSides =
        {
            GeneratedMicroChunkSocketSide.Left,
            GeneratedMicroChunkSocketSide.Right,
            GeneratedMicroChunkSocketSide.Down,
            GeneratedMicroChunkSocketSide.Up,
        };

        public static GeneratedMicroChunkSliceResult Build(
            SectorFinalCanvasLayerPlan canvasPlan,
            SectorCanvasProtectionDensityReport protectionDensityReport,
            SectorFinalRouteRecoveryReport routeRecoveryReport,
            SectorPatternChunkPartition partition) => Build(
                GeneratedMicroChunkSliceBuildRequest.FromAuthorities(
                    canvasPlan, protectionDensityReport, routeRecoveryReport, partition));

        public static GeneratedMicroChunkSliceResult Build(
            GeneratedMicroChunkSliceBuildRequest request)
        {
            if (request == null)
                return Failure(null, GeneratedMicroChunkSliceFailureCode.MissingRequest,
                    "REQUEST", "Generated MicroChunk slice build request is required.");

            var failures = new List<GeneratedMicroChunkSliceFailure>();
            ValidateAuthorities(request, failures);
            ValidateCellSources(request, failures);
            ValidateForcedSockets(request, failures);
            ValidateNonOwnership(request, failures);
            if (failures.Count > 0)
                return new GeneratedMicroChunkSliceResult(request, null, failures);

            var forced = new HashSet<SectorTileCoordinate>(request.ForcedSocketCoordinates);
            var slices = new List<GeneratedMicroChunkSliceRecord>();
            foreach (var slot in request.Partition.ChunkSlots.OrderBy(value => value))
            {
                var cells = request.CellSources.Where(value =>
                        value.Address.ChunkIndex == slot.Index)
                    .Select(value => new GeneratedMicroChunkCell(value))
                    .OrderBy(value => value).ToArray();
                var bands = SocketSides.SelectMany(side =>
                        BuildBands(side, cells, forced))
                    .OrderBy(value => value).ToArray();
                var traversal = BuildTraversal(cells, bands);
                var sideSignatures = SocketSides.Select(side =>
                {
                    var edge = EdgeCells(side, cells);
                    var sideBands = bands.Where(value => value.Side == side);
                    return new GeneratedMicroChunkSocketSignature(side,
                        GeneratedMicroChunkSliceDigest.ComputeSideSignature(
                            side, edge, sideBands));
                }).ToArray();
                var signature = new GeneratedMicroChunkSocketSignature(null,
                    GeneratedMicroChunkSliceDigest.ComputeSliceSignature(
                        cells, bands, sideSignatures, traversal));
                slices.Add(new GeneratedMicroChunkSliceRecord(
                    new GeneratedMicroChunkSliceId(request.CanvasPlan.Request.SectorId, slot.Index),
                    slot, cells, bands, sideSignatures, traversal, signature));
            }

            var sliceSet = new GeneratedMicroChunkSliceSet(request, slices);
            ValidateSliceSet(sliceSet, failures);
            return failures.Count == 0
                ? new GeneratedMicroChunkSliceResult(request, sliceSet, failures)
                : new GeneratedMicroChunkSliceResult(request, null, failures);
        }

        private static void ValidateAuthorities(
            GeneratedMicroChunkSliceBuildRequest request,
            ICollection<GeneratedMicroChunkSliceFailure> failures)
        {
            var canvas = request.CanvasPlan;
            if (canvas == null)
            {
                Add(failures, GeneratedMicroChunkSliceFailureCode.MissingCanvasPlan,
                    "MAP16_01", "Successful final canvas layer plan is required.");
            }
            else
            {
                if (canvas.Request == null || canvas.Request.Width != GeneratedMicroChunkSliceSet.SectorWidth ||
                    canvas.Request.Height != GeneratedMicroChunkSliceSet.SectorHeight ||
                    canvas.ObservedCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                    canvas.UniqueCoordinateCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                    canvas.OutOfBoundsCellCount != 0 || canvas.MissingLayerKindCount != 0)
                    Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidCanvasPlan,
                        "MAP16_01", "Canvas must be a complete 48x32, 1536-cell accepted plan.");
                ValidateDigest(canvas.InputDigest, "MAP16_01_INPUT", failures);
                ValidateDigest(canvas.OutputDigest, "MAP16_01_OUTPUT", failures);
            }

            var density = request.ProtectionDensityReport;
            if (density == null)
            {
                Add(failures, GeneratedMicroChunkSliceFailureCode.MissingProtectionDensityReport,
                    "MAP16_02", "Successful protection-density report is required.");
            }
            else
            {
                if (canvas == null || !ReferenceEquals(density.SourcePlan, canvas))
                    Add(failures, GeneratedMicroChunkSliceFailureCode.SourceMismatch,
                        "MAP16_02", "Protection-density report must reference the supplied canvas plan.");
                if (density.ObservedCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                    density.UniqueCoordinateCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                    density.OutOfBoundsCellCount != 0 || density.MissingLayerKindCount != 0 ||
                    density.ProtectionIntrusionCount != 0 || density.DensityBudgetViolationCount != 0 ||
                    density.UnownedAirViolationCount != 0 || density.CleanupProjection == null ||
                    !density.CleanupProjection.IsSafe)
                    Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidProtectionDensityReport,
                        "MAP16_02", "Protection-density report must retain accepted coverage and safety.");
                ValidateDigest(density.InputDigest, "MAP16_02_INPUT", failures);
                ValidateDigest(density.OutputDigest, "MAP16_02_OUTPUT", failures);
            }

            var route = request.RouteRecoveryReport;
            if (route == null)
            {
                Add(failures, GeneratedMicroChunkSliceFailureCode.MissingRouteRecoveryReport,
                    "MAP16_03", "Successful route-recovery report is required.");
            }
            else
            {
                if (canvas == null || density == null ||
                    !ReferenceEquals(route.SourceCanvasPlan, canvas) ||
                    !ReferenceEquals(route.SourceProtectionDensityReport, density))
                    Add(failures, GeneratedMicroChunkSliceFailureCode.SourceMismatch,
                        "MAP16_03", "Route-recovery report must reference the supplied MAP16_01/02 authorities.");
                if (route.Width != GeneratedMicroChunkSliceSet.SectorWidth ||
                    route.Height != GeneratedMicroChunkSliceSet.SectorHeight ||
                    route.ObservedCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                    route.UniqueCoordinateCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                    route.BaseRouteWitnessMissingCount != 0 ||
                    route.ExternalSocketWitnessMissingCount != 0 ||
                    route.BoundaryApertureWitnessMissingCount != 0 ||
                    route.SpecialEntranceWitnessMissingCount != 0 ||
                    route.HighFailureSampleMissingCount != 0 ||
                    route.RecoveryWitnessMissingCount != 0 || route.BlockedCellCrossingCount != 0 ||
                    route.StaticSoftlockCandidateCount != 0)
                    Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidRouteRecoveryReport,
                        "MAP16_03", "Route-recovery report must retain accepted witnesses and zero blockers.");
                ValidateDigest(route.InputDigest, "MAP16_03_INPUT", failures);
                ValidateDigest(route.OutputDigest, "MAP16_03_OUTPUT", failures);
            }

            var partition = request.Partition;
            if (partition == null)
            {
                Add(failures, GeneratedMicroChunkSliceFailureCode.MissingPartition,
                    "MAP16_04", "Successful pattern/chunk partition is required.");
            }
            else
            {
                if (canvas == null || density == null || route == null ||
                    !ReferenceEquals(partition.SourceCanvasPlan, canvas) ||
                    !ReferenceEquals(partition.SourceProtectionDensityReport, density) ||
                    !ReferenceEquals(partition.SourceRouteRecoveryReport, route))
                    Add(failures, GeneratedMicroChunkSliceFailureCode.SourceMismatch,
                        "MAP16_04", "Partition must reference the supplied MAP16_01/02/03 authorities.");
                if (partition.ChunkSlots.Count != GeneratedMicroChunkSliceSet.ChunkCount ||
                    partition.TileAssignmentCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                    partition.CoverageCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                    partition.ChunkSlots.Any(value =>
                        value.TileCount != GeneratedMicroChunkSliceSet.MicroChunkCellCount) ||
                    partition.DuplicateTileAssignmentCount != 0 ||
                    partition.MissingTileAssignmentCount != 0 ||
                    partition.OutOfBoundsTileAssignmentCount != 0 ||
                    partition.ChunkIndexMismatchCount != 0 ||
                    partition.TileRoundTripMismatchCount != 0 ||
                    partition.RotationRequestCount != 0 ||
                    partition.MissingWitnessProjectionCount != 0)
                    Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidPartition,
                        "MAP16_04", "Partition must retain 16 complete 12x8 slots and exact witness projection.");
                ValidateDigest(partition.InputDigest, "MAP16_04_INPUT", failures);
                ValidateDigest(partition.OutputDigest, "MAP16_04_OUTPUT", failures);
            }

            ValidateDigest(request.CanonicalDigest, "SLICE_INPUT", failures);
            if (request.RotateNinetyDegrees)
                Add(failures, GeneratedMicroChunkSliceFailureCode.RotationForbidden,
                    "ROTATION", "A 12x8 Generated MicroChunk slice cannot be rotated 90 degrees.");
        }

        private static void ValidateCellSources(
            GeneratedMicroChunkSliceBuildRequest request,
            ICollection<GeneratedMicroChunkSliceFailure> failures)
        {
            var sources = request.CellSources;
            if (sources.Count != GeneratedMicroChunkSliceSet.SectorCellCount ||
                request.NullCellSourceCount != 0)
                Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidCellCount,
                    "CELLS", "Exactly 1536 non-null cell sources are required; observed " +
                    Number(sources.Count) + ".");

            var addressed = sources.Where(value => value.Address != null).ToArray();
            if (addressed.Length != sources.Count || addressed.Any(value =>
                    value.Address.SectorCoordinate == null ||
                    value.Address.LocalTileCoordinate == null ||
                    !value.Address.SectorCoordinate.IsInBounds ||
                    !value.Address.LocalTileCoordinate.IsInBounds))
                Add(failures, GeneratedMicroChunkSliceFailureCode.OutOfBoundsCoordinate,
                    "CELLS", "Every source requires in-bounds sector and local coordinates.");

            var unique = addressed.Select(value => value.Address.SectorCoordinate).Distinct().Count();
            if (unique != addressed.Length)
                Add(failures, GeneratedMicroChunkSliceFailureCode.DuplicateCoordinate,
                    "CELLS", "Cell source sector coordinates must be unique.");
            if (MissingSectorCoordinateCount(addressed) != 0)
                Add(failures, GeneratedMicroChunkSliceFailureCode.MissingCoordinate,
                    "CELLS", "Cell source coverage must include every 48x32 sector coordinate.");

            if (request.CanvasPlan == null || request.Partition == null) return;
            var canvas = request.CanvasPlan.Cells.ToDictionary(value => value.Coordinate);
            var partitionAddresses = new HashSet<PatternChunkCellAddress>(request.Partition.TileAddresses);
            var projectionGroups = request.Partition.WitnessProjections.GroupBy(value =>
                    value.Address.SectorCoordinate)
                .ToDictionary(group => group.Key, group => group.Select(value =>
                    new GeneratedMicroChunkWitnessMembership(
                        value.WitnessKind, value.SourceStableId, value.PathIndex).StableToken)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray());

            foreach (var source in addressed)
            {
                var subject = source.Address.SectorCoordinate.ToString();
                if (!partitionAddresses.Contains(source.Address))
                    Add(failures, GeneratedMicroChunkSliceFailureCode.SourceMismatch,
                        subject, "Cell address must come from the supplied MAP16_04 partition.");
                if (source.NullLayerCount != 0 || source.Layers.Count !=
                    GeneratedMicroChunkSliceSet.LayerKindsPerCell ||
                    source.Layers.Select(value => value.Layer).Distinct().Count() !=
                    GeneratedMicroChunkSliceSet.LayerKindsPerCell)
                    Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidLayerCount,
                        subject, "Every generated cell requires exactly seven unique layer records.");
                if (source.Layers.Any(value => value.SourceOwner == FinalCanvasSourceOwner.Unknown))
                    Add(failures, GeneratedMicroChunkSliceFailureCode.MissingSourceOwner,
                        subject, "Every layer record requires a source owner.");
                if (source.Layers.Any(value => string.IsNullOrEmpty(value.ProvenanceId) ||
                                               string.IsNullOrEmpty(value.ClaimId) ||
                                               string.IsNullOrEmpty(value.SourceCellToken)))
                    Add(failures, GeneratedMicroChunkSliceFailureCode.MissingProvenance,
                        subject, "Every layer record requires provenance, claim, and source-cell identity.");

                FinalCanvasCell canvasCell;
                if (!canvas.TryGetValue(new FinalCanvasCellCoordinate(
                        source.Address.SectorCoordinate.X, source.Address.SectorCoordinate.Y),
                    out canvasCell))
                {
                    Add(failures, GeneratedMicroChunkSliceFailureCode.MissingCoordinate,
                        subject, "Source coordinate is absent from the final canvas.");
                    continue;
                }
                if (!LayersMatch(source.Layers, canvasCell))
                    Add(failures, GeneratedMicroChunkSliceFailureCode.LayerCopyMismatch,
                        subject, "Layer/source/protection/provenance must match the final canvas winners.");

                string[] expected;
                if (!projectionGroups.TryGetValue(source.Address.SectorCoordinate, out expected))
                    expected = Array.Empty<string>();
                var observed = source.WitnessMemberships.Select(value => value.StableToken)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                if (!expected.SequenceEqual(observed, StringComparer.Ordinal))
                    Add(failures, GeneratedMicroChunkSliceFailureCode.WitnessCopyMismatch,
                        subject, "Route/recovery memberships must match MAP16_04 projections.");
            }
        }

        private static void ValidateForcedSockets(
            GeneratedMicroChunkSliceBuildRequest request,
            ICollection<GeneratedMicroChunkSliceFailure> failures)
        {
            if (request.NullForcedSocketCoordinateCount != 0)
                Add(failures, GeneratedMicroChunkSliceFailureCode.OutOfBoundsCoordinate,
                    "FORCED_SOCKET", "Forced socket probe coordinates cannot be null.");
            var sources = request.CellSources.Where(value => value.Address != null)
                .GroupBy(value => value.Address.SectorCoordinate)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (var coordinate in request.ForcedSocketCoordinates)
            {
                GeneratedMicroChunkCellSource source;
                if (!coordinate.IsInBounds || !sources.TryGetValue(coordinate, out source))
                {
                    Add(failures, GeneratedMicroChunkSliceFailureCode.OutOfBoundsCoordinate,
                        coordinate.ToString(), "Forced socket probe must reference a generated cell.");
                    continue;
                }
                var local = source.Address.LocalTileCoordinate;
                if (local.X != 0 && local.X != GeneratedMicroChunkSliceSet.MicroChunkWidth - 1 &&
                    local.Y != 0 && local.Y != GeneratedMicroChunkSliceSet.MicroChunkHeight - 1)
                {
                    Add(failures, GeneratedMicroChunkSliceFailureCode.BlockedSocketCell,
                        coordinate.ToString(), "Socket probe must reference a slice edge cell.");
                    continue;
                }
                if (!new GeneratedMicroChunkCell(source).IsPassable)
                    Add(failures, GeneratedMicroChunkSliceFailureCode.BlockedSocketCell,
                        coordinate.ToString(), "Socket bands cannot include a blocked edge cell.");
            }
        }

        private static void ValidateNonOwnership(
            GeneratedMicroChunkSliceBuildRequest request,
            ICollection<GeneratedMicroChunkSliceFailure> failures)
        {
            if (request.ForbiddenOperationCount != 0)
                Add(failures, GeneratedMicroChunkSliceFailureCode.ForbiddenOperation,
                    "NON_OWNERSHIP", "Slice building cannot project marker slots, create spawn ids, write files/assets/Tilemaps, mutate Unity objects, simulate physics, rerender, reroll, carve, widen, spawn, approve seeds, or run regression.");
        }

        private static GeneratedMicroChunkSocketBand[] BuildBands(
            GeneratedMicroChunkSocketSide side,
            IEnumerable<GeneratedMicroChunkCell> sourceCells,
            ISet<SectorTileCoordinate> forced)
        {
            var edge = EdgeCells(side, sourceCells).ToArray();
            var bands = new List<GeneratedMicroChunkSocketBand>();
            var current = new List<GeneratedMicroChunkCell>();
            foreach (var cell in edge)
            {
                if (cell.IsPassable || forced.Contains(cell.SectorCoordinate))
                {
                    current.Add(cell);
                    continue;
                }
                if (current.Count > 0)
                {
                    bands.Add(new GeneratedMicroChunkSocketBand(side, current));
                    current.Clear();
                }
            }
            if (current.Count > 0)
                bands.Add(new GeneratedMicroChunkSocketBand(side, current));
            return bands.ToArray();
        }

        private static IEnumerable<GeneratedMicroChunkCell> EdgeCells(
            GeneratedMicroChunkSocketSide side,
            IEnumerable<GeneratedMicroChunkCell> cells) => cells.Where(value =>
                side == GeneratedMicroChunkSocketSide.Left ? value.LocalCoordinate.X == 0 :
                side == GeneratedMicroChunkSocketSide.Right
                    ? value.LocalCoordinate.X == GeneratedMicroChunkSliceSet.MicroChunkWidth - 1 :
                side == GeneratedMicroChunkSocketSide.Down ? value.LocalCoordinate.Y == 0 :
                    value.LocalCoordinate.Y == GeneratedMicroChunkSliceSet.MicroChunkHeight - 1)
                .OrderBy(value => GeneratedMicroChunkSocketBand.EdgePositionStatic(side, value));

        private static GeneratedMicroChunkTraversalSummary BuildTraversal(
            IReadOnlyCollection<GeneratedMicroChunkCell> cells,
            IReadOnlyCollection<GeneratedMicroChunkSocketBand> bands) =>
            new GeneratedMicroChunkTraversalSummary(
                cells.Count(value => value.IsPassable),
                cells.Count(value => !value.IsPassable),
                cells.Count(value => value.WitnessMembershipCount > 0),
                cells.Sum(value => value.WitnessMembershipCount),
                bands.Select(value => value.Side).Distinct().Count(),
                ConnectedPassableComponentCount(cells),
                bands.Count,
                bands.Count(value => value.TouchesPassableComponent));

        private static int ConnectedPassableComponentCount(
            IEnumerable<GeneratedMicroChunkCell> sourceCells)
        {
            var passable = sourceCells.Where(value => value.IsPassable)
                .ToDictionary(value => value.LocalCoordinate);
            var visited = new HashSet<MicroChunkLocalTileCoordinate>();
            var components = 0;
            foreach (var start in passable.Keys.OrderBy(value => value))
            {
                if (!visited.Add(start)) continue;
                components++;
                var queue = new Queue<MicroChunkLocalTileCoordinate>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var neighbor in Neighbors(current))
                    {
                        if (!passable.ContainsKey(neighbor) || !visited.Add(neighbor)) continue;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return components;
        }

        private static IEnumerable<MicroChunkLocalTileCoordinate> Neighbors(
            MicroChunkLocalTileCoordinate coordinate)
        {
            var candidates = new[]
            {
                new MicroChunkLocalTileCoordinate(coordinate.X - 1, coordinate.Y),
                new MicroChunkLocalTileCoordinate(coordinate.X + 1, coordinate.Y),
                new MicroChunkLocalTileCoordinate(coordinate.X, coordinate.Y - 1),
                new MicroChunkLocalTileCoordinate(coordinate.X, coordinate.Y + 1),
            };
            return candidates.Where(value => value.IsInBounds);
        }

        private static void ValidateSliceSet(
            GeneratedMicroChunkSliceSet set,
            ICollection<GeneratedMicroChunkSliceFailure> failures)
        {
            if (set.SliceCount != GeneratedMicroChunkSliceSet.ChunkCount ||
                set.Slices.Any(value => value.CellCount != GeneratedMicroChunkSliceSet.MicroChunkCellCount))
                Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidCellCount,
                    "SLICES", "Exactly 16 slices with 96 cells each are required.");
            if (set.TotalCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                set.UniqueSectorCellCount != GeneratedMicroChunkSliceSet.SectorCellCount ||
                set.DuplicateSectorCellCount != 0 || set.MissingSectorCellCount != 0 ||
                set.OutOfBoundsSectorCellCount != 0)
                Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidCellCount,
                    "COVERAGE", "Generated slices must cover all 1536 sector cells exactly once.");
            if (set.TotalLayerRecordCount != GeneratedMicroChunkSliceSet.SectorCellCount *
                    GeneratedMicroChunkSliceSet.LayerKindsPerCell ||
                set.LayerRecordsWithSourceOwnerCount != set.TotalLayerRecordCount ||
                set.LayerRecordsWithProvenanceCount != set.TotalLayerRecordCount)
                Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidLayerCount,
                    "LAYERS", "All 10752 layer records require source owner and provenance.");
            if (set.WitnessMembershipCount != set.SourcePartition.WitnessProjections.Count)
                Add(failures, GeneratedMicroChunkSliceFailureCode.WitnessCopyMismatch,
                    "WITNESSES", "Every MAP16_04 witness projection must be copied once.");
            if (set.SocketBandsOnBlockedCellsCount != 0)
                Add(failures, GeneratedMicroChunkSliceFailureCode.BlockedSocketCell,
                    "SOCKETS", "Socket bands cannot contain blocked cells.");
            if (set.SocketSideSignatureCount != set.SliceCount * SocketSides.Length ||
                set.InvalidSideSignatureCount != 0 || set.InvalidSliceSignatureCount != 0)
                Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidSocketSignature,
                    "SIGNATURES", "Every slice and side requires a lower-hex SHA-256 signature.");
            if (set.MissingTraversalSummaryCount != 0 ||
                set.MissingPassableComponentSummaryCount != 0 ||
                set.Slices.Any(value => !value.TraversalSummary.EverySocketBandTouchesPassableComponent))
                Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidTraversalSummary,
                    "TRAVERSAL", "Every slice requires a complete static traversal summary.");
            if (set.RotationRequestCount != 0)
                Add(failures, GeneratedMicroChunkSliceFailureCode.RotationForbidden,
                    "ROTATION", "Successful slices cannot rotate 12x8 coordinates.");
            ValidateDigest(set.InputDigest, "SLICE_SET_INPUT", failures);
            ValidateDigest(set.OutputDigest, "SLICE_SET_OUTPUT", failures);
        }

        private static bool LayersMatch(
            IReadOnlyList<GeneratedMicroChunkLayerRecord> observed,
            FinalCanvasCell cell)
        {
            if (observed.Count != cell.Winners.Count) return false;
            var winners = cell.Winners.OrderBy(value => value.Layer).ToArray();
            var records = observed.OrderBy(value => value.Layer).ToArray();
            for (var index = 0; index < winners.Length; index++)
            {
                var winner = winners[index];
                var record = records[index];
                if (record.Layer != winner.Layer || record.CellKind != winner.CellKind ||
                    record.SourceOwner != winner.SourceOwner ||
                    record.ProvenanceId != winner.ProvenanceId ||
                    record.Protection != winner.Protection ||
                    record.IsProtected != winner.IsProtected || record.ClaimId != winner.ClaimId ||
                    record.SourceCellToken != cell.StableToken) return false;
            }
            return true;
        }

        private static int MissingSectorCoordinateCount(
            IEnumerable<GeneratedMicroChunkCellSource> sources)
        {
            var set = new HashSet<SectorTileCoordinate>(sources.Select(value =>
                value.Address.SectorCoordinate).Where(value => value.IsInBounds));
            var count = 0;
            for (var y = 0; y < GeneratedMicroChunkSliceSet.SectorHeight; y++)
            for (var x = 0; x < GeneratedMicroChunkSliceSet.SectorWidth; x++)
                if (!set.Contains(new SectorTileCoordinate(x, y))) count++;
            return count;
        }

        private static void ValidateDigest(
            string digest,
            string subject,
            ICollection<GeneratedMicroChunkSliceFailure> failures)
        {
            if (!GeneratedMicroChunkSliceDigest.IsLowerHexSha256(digest))
                Add(failures, GeneratedMicroChunkSliceFailureCode.InvalidDigest,
                    subject, "Digest must be lower-hex SHA-256.");
        }

        private static GeneratedMicroChunkSliceResult Failure(
            GeneratedMicroChunkSliceBuildRequest request,
            GeneratedMicroChunkSliceFailureCode code,
            string subject,
            string reason) => new GeneratedMicroChunkSliceResult(request, null, new[]
        {
            new GeneratedMicroChunkSliceFailure(code, subject, reason),
        });

        private static void Add(
            ICollection<GeneratedMicroChunkSliceFailure> failures,
            GeneratedMicroChunkSliceFailureCode code,
            string subject,
            string reason) => failures.Add(new GeneratedMicroChunkSliceFailure(
                code, subject, reason));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
