using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkPreviewBuilder
    {
        public MicrochunkPreviewReport Build(MicrochunkPreviewRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var issues = new List<MicrochunkPreviewIssue>();
            AddInputDiagnostics(request, issues);

            MicrochunkDefinition sourceDefinition;
            IReadOnlyDictionary<string, MicrochunkSocketBandDefinition> bandsById;
            try
            {
                sourceDefinition = ProjectDefinition(request);
                bandsById = request.EditorState.ProjectBandsById();
            }
            catch (Exception exception)
            {
                issues.Add(Issue(
                    request,
                    MicrochunkPreviewIssueCategory.Transform,
                    "EDITOR_STATE_PROJECTION_FAILED",
                    exception.Message,
                    null,
                    null,
                    0));
                return new MicrochunkPreviewReport(
                    request.SelectedMicrochunkId,
                    Array.Empty<MicrochunkPreviewTransformReport>(),
                    issues);
            }

            var transforms = new List<MicrochunkPreviewTransformReport>();
            foreach (var transform in request.SelectedTransforms)
            {
                try
                {
                    transforms.Add(BuildTransform(request, sourceDefinition, bandsById, transform, issues));
                }
                catch (Exception exception)
                {
                    issues.Add(Issue(
                        request,
                        MicrochunkPreviewIssueCategory.Transform,
                        "TRANSFORM_PREVIEW_FAILED",
                        exception.Message,
                        transform,
                        null,
                        0));
                }
            }

            return new MicrochunkPreviewReport(request.SelectedMicrochunkId, transforms, issues);
        }

        private static MicrochunkDefinition ProjectDefinition(MicrochunkPreviewRequest request)
        {
            return new MicrochunkDefinition(
                new MicrochunkId(request.SelectedMicrochunkId),
                "Editor Transform Preview: " + request.SelectedMicrochunkId,
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                Array.Empty<string>(),
                Array.Empty<string>(),
                request.SelectedTransforms,
                1,
                0,
                0,
                0,
                request.EditorState.Grid.State.CellCount == MicrochunkConstants.CellCount,
                "PREFAB_MC_GRAY",
                true,
                "Detached in-memory preview projection only.",
                request.EditorState.Grid.ProjectTileCells(),
                request.EditorState.ProjectSockets(),
                request.EditorState.ProjectObjectSlots());
        }

        private static MicrochunkPreviewTransformReport BuildTransform(
            MicrochunkPreviewRequest request,
            MicrochunkDefinition sourceDefinition,
            IReadOnlyDictionary<string, MicrochunkSocketBandDefinition> bandsById,
            MicrochunkTransform transform,
            ICollection<MicrochunkPreviewIssue> issues)
        {
            var definition = MicrochunkTransformer.Transform(sourceDefinition, transform).Definition;
            var options = request.ValidationOptions;

            MicrochunkTileLayerRuleResult tileResult = null;
            if (options.ValidateTileLayers)
            {
                try
                {
                    tileResult = MicrochunkTileLayerRules.ValidateDefinition(definition);
                    AddTileIssues(request, transform, tileResult, issues);
                }
                catch (Exception exception)
                {
                    issues.Add(Issue(request, MicrochunkPreviewIssueCategory.TileLayer,
                        "TILE_LAYER_VALIDATION_FAILED", exception.Message, transform, null, 0));
                }
            }

            Microchunk96CellValidationResult coverageResult = null;
            if (options.ValidateCoverage || options.ValidateReachability)
            {
                try
                {
                    coverageResult = new Microchunk96CellValidator().ValidateDefinition(definition);
                    if (options.ValidateCoverage)
                        AddCoverageIssues(request, transform, coverageResult, issues);
                }
                catch (Exception exception)
                {
                    issues.Add(Issue(request, MicrochunkPreviewIssueCategory.Coverage,
                        "COVERAGE_VALIDATION_FAILED", exception.Message, transform, null, 0));
                }
            }

            MicrochunkSocketEdgeValidationResult socketResult = null;
            if (options.ValidateSocketEdges)
            {
                try
                {
                    socketResult = MicrochunkSocketEdgeValidator.ValidateDefinition(
                        definition,
                        bandsById,
                        request.SignaturesById);
                    AddSocketIssues(request, transform, socketResult, issues);
                }
                catch (Exception exception)
                {
                    issues.Add(Issue(request, MicrochunkPreviewIssueCategory.SocketEdge,
                        "SOCKET_EDGE_VALIDATION_FAILED", exception.Message, transform, null, 0));
                }
            }

            MicrochunkObjectSlotValidationResult objectSlotResult = null;
            if (options.ValidateObjectSlots)
            {
                try
                {
                    objectSlotResult = MicrochunkObjectSlotValidator.ValidateDefinition(
                        definition,
                        request.ObjectSlotPolicy);
                    AddObjectSlotIssues(request, transform, objectSlotResult, issues);
                }
                catch (Exception exception)
                {
                    issues.Add(Issue(request, MicrochunkPreviewIssueCategory.ObjectSlot,
                        "OBJECT_SLOT_VALIDATION_FAILED", exception.Message, transform, null, 0));
                }
            }

            MicrochunkReachabilityResult reachabilityResult = null;
            if (options.ValidateReachability)
            {
                try
                {
                    reachabilityResult = new MicrochunkReachabilityProbe().ValidateDefinition(
                        definition,
                        bandsById.Values,
                        request.ReachabilityPolicy,
                        coverageResult);
                    AddReachabilityIssues(request, transform, reachabilityResult, issues);
                }
                catch (Exception exception)
                {
                    issues.Add(Issue(request, MicrochunkPreviewIssueCategory.Reachability,
                        "REACHABILITY_VALIDATION_FAILED", exception.Message, transform, null, 0));
                }
            }

            var overlays = BuildOverlays(request, transform, definition, reachabilityResult);
            return new MicrochunkPreviewTransformReport(
                transform,
                definition,
                overlays,
                tileResult,
                options.ValidateCoverage ? coverageResult : null,
                socketResult,
                objectSlotResult,
                reachabilityResult);
        }

        private static IReadOnlyList<MicrochunkPreviewCellOverlay> BuildOverlays(
            MicrochunkPreviewRequest request,
            MicrochunkTransform transform,
            MicrochunkDefinition definition,
            MicrochunkReachabilityResult reachability)
        {
            var cells = definition.TileCells.ToDictionary(value => value.Coordinate);
            var nodes = reachability == null
                ? new HashSet<MicrochunkLocalCoord>()
                : new HashSet<MicrochunkLocalCoord>(reachability.Nodes.Select(value => value.Coordinate));
            var reachable = ReachableCoordinates(reachability, nodes);
            var witnessCoordinates = reachability == null
                ? new HashSet<MicrochunkLocalCoord>()
                : new HashSet<MicrochunkLocalCoord>(
                    reachability.PathWitnesses.SelectMany(value => value.Coordinates));

            var socketIdsByCoordinate = new Dictionary<MicrochunkLocalCoord, List<string>>();
            var entryCoordinates = new HashSet<MicrochunkLocalCoord>();
            var exitCoordinates = new HashSet<MicrochunkLocalCoord>();
            if (reachability != null)
            {
                foreach (var pair in reachability.SocketEntries)
                foreach (var coordinate in pair.Value)
                {
                    if (!socketIdsByCoordinate.TryGetValue(coordinate, out var ids))
                    {
                        ids = new List<string>();
                        socketIdsByCoordinate.Add(coordinate, ids);
                    }
                    ids.Add(pair.Key);
                    entryCoordinates.Add(coordinate);
                }

                foreach (var witness in reachability.PathWitnesses)
                {
                    if (reachability.SocketEntries.TryGetValue(witness.SourceSocketId, out var sourceEntries))
                        foreach (var coordinate in sourceEntries) entryCoordinates.Add(coordinate);
                    if (reachability.SocketEntries.TryGetValue(witness.TargetSocketId, out var targetEntries))
                        foreach (var coordinate in targetEntries) exitCoordinates.Add(coordinate);
                }
            }

            var slotIdsByCoordinate = definition.ObjectSlots
                .GroupBy(value => value.Anchor)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(value => value.SlotId).OrderBy(value => value, StringComparer.Ordinal).ToList());

            var overlays = new List<MicrochunkPreviewCellOverlay>(MicrochunkConstants.CellCount);
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
            {
                var coordinate = new MicrochunkLocalCoord(x, y);
                cells.TryGetValue(coordinate, out var cell);
                var blocked = cell != null && IsBlockedSolid(cell);
                var isReachable = reachability != null && reachable.Contains(coordinate);
                var isWitness = witnessCoordinates.Contains(coordinate);
                var isEntry = entryCoordinates.Contains(coordinate);
                var isExit = exitCoordinates.Contains(coordinate);
                var state = ResolveState(
                    request.ShowReachabilityOverlay && reachability != null,
                    blocked,
                    isReachable,
                    isWitness,
                    isEntry,
                    isExit);

                socketIdsByCoordinate.TryGetValue(coordinate, out var socketIds);
                slotIdsByCoordinate.TryGetValue(coordinate, out var slotIds);
                overlays.Add(new MicrochunkPreviewCellOverlay(
                    transform,
                    coordinate,
                    request.ShowTileOverlay ? cell : null,
                    request.ShowSocketOverlay && socketIds != null ? socketIds : Enumerable.Empty<string>(),
                    request.ShowObjectSlotOverlay && slotIds != null ? slotIds : Enumerable.Empty<string>(),
                    state,
                    isReachable,
                    isWitness,
                    request.ShowSocketOverlay && isEntry,
                    request.ShowSocketOverlay && isExit,
                    blocked));
            }

            return new ReadOnlyCollection<MicrochunkPreviewCellOverlay>(overlays);
        }

        private static HashSet<MicrochunkLocalCoord> ReachableCoordinates(
            MicrochunkReachabilityResult result,
            HashSet<MicrochunkLocalCoord> nodes)
        {
            if (result == null) return new HashSet<MicrochunkLocalCoord>();
            var starts = result.SocketEntries.Values.SelectMany(value => value)
                .Where(nodes.Contains)
                .Distinct()
                .OrderBy(value => value.RowMajorIndex)
                .ToList();
            if (starts.Count == 0) return new HashSet<MicrochunkLocalCoord>(nodes);

            var outgoing = result.Edges
                .GroupBy(value => value.SourceCoordinate)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(value => value.TargetCoordinate)
                        .Distinct()
                        .OrderBy(value => value.RowMajorIndex)
                        .ToList());
            var reached = new HashSet<MicrochunkLocalCoord>(starts);
            var queue = new Queue<MicrochunkLocalCoord>(starts);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!outgoing.TryGetValue(current, out var targets)) continue;
                foreach (var target in targets)
                {
                    if (reached.Add(target)) queue.Enqueue(target);
                }
            }
            return reached;
        }

        private static bool IsBlockedSolid(MicrochunkTileCell cell)
        {
            var occupancy = MicrochunkTileLayerOccupancy.FromCell(cell);
            return occupancy.IsOccupied(MicrochunkTileLayer.GroundSolid) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Breakable) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Hazard) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Liquid);
        }

        private static MicrochunkPreviewReachabilityState ResolveState(
            bool enabled,
            bool blocked,
            bool reachable,
            bool witness,
            bool socketEntry,
            bool socketExit)
        {
            if (!enabled) return MicrochunkPreviewReachabilityState.Disabled;
            if (blocked) return MicrochunkPreviewReachabilityState.BlockedSolid;
            if (socketExit) return MicrochunkPreviewReachabilityState.SocketExit;
            if (socketEntry) return MicrochunkPreviewReachabilityState.SocketEntry;
            if (witness) return MicrochunkPreviewReachabilityState.PathWitness;
            return reachable
                ? MicrochunkPreviewReachabilityState.Reachable
                : MicrochunkPreviewReachabilityState.Unreachable;
        }

        private static void AddTileIssues(
            MicrochunkPreviewRequest request,
            MicrochunkTransform transform,
            MicrochunkTileLayerRuleResult result,
            ICollection<MicrochunkPreviewIssue> issues)
        {
            for (var index = 0; index < result.Violations.Count; index++)
            {
                var violation = result.Violations[index];
                issues.Add(Issue(
                    request,
                    MicrochunkPreviewIssueCategory.TileLayer,
                    violation.Reason,
                    violation.FirstLayer + "=" + violation.FirstCode + ", " +
                    violation.SecondLayer + "=" + violation.SecondCode,
                    transform,
                    violation.Coordinate,
                    index));
            }
        }

        private static void AddCoverageIssues(
            MicrochunkPreviewRequest request,
            MicrochunkTransform transform,
            Microchunk96CellValidationResult result,
            ICollection<MicrochunkPreviewIssue> issues)
        {
            for (var index = 0; index < result.Violations.Count; index++)
            {
                var violation = result.Violations[index];
                issues.Add(Issue(
                    request,
                    MicrochunkPreviewIssueCategory.Coverage,
                    violation.Reason,
                    "96-cell coverage validation failed.",
                    transform,
                    violation.Coordinate,
                    index));
            }
        }

        private static void AddSocketIssues(
            MicrochunkPreviewRequest request,
            MicrochunkTransform transform,
            MicrochunkSocketEdgeValidationResult result,
            ICollection<MicrochunkPreviewIssue> issues)
        {
            for (var index = 0; index < result.Violations.Count; index++)
            {
                var violation = result.Violations[index];
                issues.Add(Issue(
                    request,
                    MicrochunkPreviewIssueCategory.SocketEdge,
                    violation.Reason,
                    "Socket " + violation.SocketId + " on " + violation.Side + ".",
                    transform,
                    violation.Coordinate,
                    index));
            }
        }

        private static void AddObjectSlotIssues(
            MicrochunkPreviewRequest request,
            MicrochunkTransform transform,
            MicrochunkObjectSlotValidationResult result,
            ICollection<MicrochunkPreviewIssue> issues)
        {
            for (var index = 0; index < result.Violations.Count; index++)
            {
                var violation = result.Violations[index];
                issues.Add(Issue(
                    request,
                    MicrochunkPreviewIssueCategory.ObjectSlot,
                    violation.Reason,
                    "Object slot " + violation.SlotId + ".",
                    transform,
                    violation.Coordinate,
                    index));
            }
        }

        private static void AddReachabilityIssues(
            MicrochunkPreviewRequest request,
            MicrochunkTransform transform,
            MicrochunkReachabilityResult result,
            ICollection<MicrochunkPreviewIssue> issues)
        {
            for (var index = 0; index < result.Violations.Count; index++)
            {
                var violation = result.Violations[index];
                var socketPair = string.IsNullOrEmpty(violation.PairedSocketId)
                    ? violation.SocketId
                    : violation.SocketId + " -> " + violation.PairedSocketId;
                issues.Add(Issue(
                    request,
                    MicrochunkPreviewIssueCategory.Reachability,
                    violation.Reason,
                    "Reachability failed for " + socketPair + ".",
                    transform,
                    violation.Coordinate,
                    index));
            }
        }

        private static void AddInputDiagnostics(
            MicrochunkPreviewRequest request,
            ICollection<MicrochunkPreviewIssue> issues)
        {
            for (var index = 0; index < request.ImportIssues.Count; index++)
            {
                var issue = request.ImportIssues[index];
                issues.Add(new MicrochunkPreviewIssue(
                    issue.IsError ? MicrochunkPreviewIssueSeverity.Error : MicrochunkPreviewIssueSeverity.Warning,
                    0,
                    MicrochunkPreviewIssueCategory.Import,
                    issue.Code,
                    issue.FileName + ":" + issue.RowNumber + ":" + issue.ColumnName + ": " + issue.Message,
                    request.SelectedMicrochunkId,
                    null,
                    null,
                    index));
            }
            for (var index = 0; index < request.ExportIssues.Count; index++)
            {
                var issue = request.ExportIssues[index];
                issues.Add(new MicrochunkPreviewIssue(
                    issue.IsError ? MicrochunkPreviewIssueSeverity.Error : MicrochunkPreviewIssueSeverity.Warning,
                    0,
                    MicrochunkPreviewIssueCategory.Export,
                    issue.Code,
                    issue.FileName + ":" + issue.ColumnName + ": " + issue.Message,
                    request.SelectedMicrochunkId,
                    null,
                    null,
                    index));
            }
        }

        private static MicrochunkPreviewIssue Issue(
            MicrochunkPreviewRequest request,
            MicrochunkPreviewIssueCategory category,
            string code,
            string message,
            MicrochunkTransform? transform,
            MicrochunkLocalCoord? coordinate,
            int sourceOrder)
        {
            return new MicrochunkPreviewIssue(
                MicrochunkPreviewIssueSeverity.Error,
                0,
                category,
                code,
                message,
                request.SelectedMicrochunkId,
                transform,
                coordinate,
                sourceOrder);
        }
    }
}
