using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public sealed class TerrainClusterPatternRenderRequest
    {
        private readonly ReadOnlyCollection<TerrainClusterPatternZoneCell> authoredZones;
        private readonly ReadOnlyCollection<TerrainClusterPatternPlacementIntent> placements;

        public TerrainClusterPatternRenderRequest(
            TerrainClusterLocalCanvas localCanvas,
            string expectedLocalCanvasDigest,
            TerrainClusterTraversalCompilation traversalCompilation,
            string expectedTraversalCompilationDigest,
            TerrainClusterRouteWitnessReport routeWitnessReport,
            string expectedRouteWitnessDigest,
            MicroPatternAuthoringCatalog patternCatalog,
            string expectedPatternCatalogDigest,
            IEnumerable<TerrainClusterPatternZoneCell> authoredZones,
            IEnumerable<TerrainClusterPatternPlacementIntent> placements)
        {
            LocalCanvas = localCanvas;
            ExpectedLocalCanvasDigest = expectedLocalCanvasDigest ?? string.Empty;
            TraversalCompilation = traversalCompilation;
            ExpectedTraversalCompilationDigest = expectedTraversalCompilationDigest ?? string.Empty;
            RouteWitnessReport = routeWitnessReport;
            ExpectedRouteWitnessDigest = expectedRouteWitnessDigest ?? string.Empty;
            PatternCatalog = patternCatalog;
            ExpectedPatternCatalogDigest = expectedPatternCatalogDigest ?? string.Empty;
            this.authoredZones = new ReadOnlyCollection<TerrainClusterPatternZoneCell>(
                (authoredZones ?? Array.Empty<TerrainClusterPatternZoneCell>()).ToArray());
            this.placements = new ReadOnlyCollection<TerrainClusterPatternPlacementIntent>(
                (placements ?? Array.Empty<TerrainClusterPatternPlacementIntent>()).ToArray());
        }

        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public string ExpectedLocalCanvasDigest { get; }
        public TerrainClusterTraversalCompilation TraversalCompilation { get; }
        public string ExpectedTraversalCompilationDigest { get; }
        public TerrainClusterRouteWitnessReport RouteWitnessReport { get; }
        public string ExpectedRouteWitnessDigest { get; }
        public MicroPatternAuthoringCatalog PatternCatalog { get; }
        public string ExpectedPatternCatalogDigest { get; }
        public IReadOnlyList<TerrainClusterPatternZoneCell> AuthoredZones => authoredZones;
        public IReadOnlyList<TerrainClusterPatternPlacementIntent> Placements => placements;
    }

    public enum TerrainClusterPatternGeometryProvenanceKind
    {
        StaticShellAir = 1,
        StaticShellSolid = 2,
        GeometryCarveSubstrate = 3,
    }

    public sealed class TerrainClusterPatternWorkingCell
    {
        private readonly ReadOnlyCollection<TerrainClusterPatternGeometryProvenanceKind> geometryProvenance;

        internal TerrainClusterPatternWorkingCell(
            MicroPatternRenderCellState state,
            TerrainClusterStaticShellCell staticShellCell,
            IEnumerable<TerrainClusterPatternGeometryProvenanceKind> geometryProvenance)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            StaticShellCell = staticShellCell ?? throw new ArgumentNullException(nameof(staticShellCell));
            this.geometryProvenance = new ReadOnlyCollection<TerrainClusterPatternGeometryProvenanceKind>(
                (geometryProvenance ?? Array.Empty<TerrainClusterPatternGeometryProvenanceKind>())
                    .Distinct().OrderBy(value => value).ToArray());
        }

        public LocalTileCoord Coordinate => State.TargetCoordinate;
        public bool Solid => State.Solid;
        public string SurfaceId => State.SurfaceId;
        public string AffordanceId => State.AffordanceId;
        public string MaterialId => State.MaterialId;
        public string HazardId => State.HazardId;
        public string MarkerId => State.MarkerId;
        public MicroPatternRenderCellState State { get; }
        public TerrainClusterStaticShellCell StaticShellCell { get; }
        public IReadOnlyList<TerrainClusterPatternGeometryProvenanceKind> GeometryProvenance => geometryProvenance;
        public bool IsGeometryCarveSubstrate => geometryProvenance.Contains(TerrainClusterPatternGeometryProvenanceKind.GeometryCarveSubstrate);
    }

    public sealed class TerrainClusterPatternWorkingCanvas
    {
        private readonly ReadOnlyCollection<TerrainClusterPatternWorkingCell> cells;
        private readonly ReadOnlyDictionary<LocalTileCoord, TerrainClusterPatternWorkingCell> byCoordinate;

        internal TerrainClusterPatternWorkingCanvas(
            TerrainClusterId clusterId,
            IEnumerable<TerrainClusterPatternWorkingCell> cells,
            string canonicalDigest)
        {
            ClusterId = clusterId;
            var copy = (cells ?? Array.Empty<TerrainClusterPatternWorkingCell>())
                .Where(value => value != null)
                .OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X).ToArray();
            this.cells = new ReadOnlyCollection<TerrainClusterPatternWorkingCell>(copy);
            byCoordinate = new ReadOnlyDictionary<LocalTileCoord, TerrainClusterPatternWorkingCell>(
                copy.ToDictionary(value => value.Coordinate));
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public TerrainClusterId ClusterId { get; }
        public IReadOnlyList<TerrainClusterPatternWorkingCell> Cells => cells;
        public string CanonicalDigest { get; }
        public int CoordinateCount => cells.Count;
        public int GeometryCarveSubstrateCoordinateCount => cells.Count(value => value.IsGeometryCarveSubstrate);

        public bool TryGetCell(LocalTileCoord coordinate, out TerrainClusterPatternWorkingCell cell)
        {
            return byCoordinate.TryGetValue(coordinate, out cell);
        }
    }

    public sealed class TerrainClusterPatternRenderReport
    {
        private readonly ReadOnlyCollection<TerrainClusterPatternPlacementIntent> placements;
        private readonly ReadOnlyCollection<MicroPatternApplicationPlan> applicationPlans;

        internal TerrainClusterPatternRenderReport(
            PatternZoneMap zoneMap,
            IEnumerable<TerrainClusterPatternPlacementIntent> placements,
            IEnumerable<MicroPatternApplicationPlan> applicationPlans,
            string applicationPlanDigest,
            MicroPatternRenderDelta renderDelta,
            TerrainClusterPatternWorkingCanvas initialCanvas,
            TerrainClusterPatternWorkingCanvas finalCanvas,
            int map10TargetCoordinateCount,
            int untouchedFullCanvasCoordinateCount,
            int protectedWriteCount,
            int protectedValueChangeCount,
            string canonicalDigest)
        {
            ZoneMap = zoneMap;
            this.placements = new ReadOnlyCollection<TerrainClusterPatternPlacementIntent>(
                placements.OrderBy(value => value.PlacementId, StringComparer.Ordinal).ToArray());
            this.applicationPlans = new ReadOnlyCollection<MicroPatternApplicationPlan>(
                applicationPlans.OrderBy(value => value.StableDigest, StringComparer.Ordinal).ToArray());
            ApplicationPlanDigest = applicationPlanDigest ?? string.Empty;
            RenderDelta = renderDelta;
            InitialWorkingCanvas = initialCanvas;
            FinalWorkingCanvas = finalCanvas;
            Map10TargetCoordinateCount = map10TargetCoordinateCount;
            UntouchedFullCanvasCoordinateCount = untouchedFullCanvasCoordinateCount;
            ProtectedWriteCount = protectedWriteCount;
            ProtectedValueChangeCount = protectedValueChangeCount;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public PatternZoneMap ZoneMap { get; }
        public IReadOnlyList<TerrainClusterPatternPlacementIntent> Placements => placements;
        public IReadOnlyList<MicroPatternApplicationPlan> ApplicationPlans => applicationPlans;
        public string ApplicationPlanDigest { get; }
        public MicroPatternRenderDelta RenderDelta { get; }
        public TerrainClusterPatternWorkingCanvas InitialWorkingCanvas { get; }
        public TerrainClusterPatternWorkingCanvas FinalWorkingCanvas { get; }
        public int FullWorkingCanvasCoordinateCount => InitialWorkingCanvas.CoordinateCount;
        public int GeometryCarveSubstrateCoordinateCount => InitialWorkingCanvas.GeometryCarveSubstrateCoordinateCount;
        public int Map10TargetCoordinateCount { get; }
        public int UntouchedFullCanvasCoordinateCount { get; }
        public int RendererDeltaCoordinateCount => RenderDelta.Cells.Count;
        public int ProtectedWriteCount { get; }
        public int ProtectedValueChangeCount { get; }
        public string CanonicalDigest { get; }
    }

    public enum TerrainClusterPatternRenderErrorCode
    {
        MissingInput = 1,
        ArtifactIdentityMismatch = 2,
        ArtifactDigestMismatch = 3,
        InvalidZoneCoordinate = 4,
        ConflictingGeometryZone = 5,
        ProtectedZoneOverlap = 6,
        ProtectedEvidenceMismatch = 7,
        InvalidPlacementId = 8,
        DuplicatePlacementId = 9,
        UnknownPattern = 10,
        InvalidPlacement = 11,
        ApplicationPlanRejected = 12,
        UnauthorizedZoneOperation = 13,
        UnsupportedLayerOperation = 14,
        RenderConflict = 15,
        ProtectedWriteDetected = 16,
        WorkingCanvasCoverageMismatch = 17,
        NonCanonicalPublication = 18,
    }

    public sealed class TerrainClusterPatternRenderError :
        IEquatable<TerrainClusterPatternRenderError>,
        IComparable<TerrainClusterPatternRenderError>
    {
        private readonly ReadOnlyCollection<MicroPatternTransformError> transformErrors;
        private readonly ReadOnlyCollection<MicroPatternProtectedMaskError> protectedMaskErrors;
        private readonly ReadOnlyCollection<MicroPatternApplicationError> applicationErrors;
        private readonly ReadOnlyCollection<MicroPatternRenderError> renderErrors;

        public TerrainClusterPatternRenderError(
            TerrainClusterPatternRenderErrorCode code,
            string path,
            string detail)
            : this(code, path, detail, null, null, null, null)
        {
        }

        internal TerrainClusterPatternRenderError(
            TerrainClusterPatternRenderErrorCode code,
            string path,
            string detail,
            IEnumerable<MicroPatternTransformError> transformErrors,
            IEnumerable<MicroPatternProtectedMaskError> protectedMaskErrors,
            IEnumerable<MicroPatternApplicationError> applicationErrors,
            IEnumerable<MicroPatternRenderError> renderErrors)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
            this.transformErrors = new ReadOnlyCollection<MicroPatternTransformError>(
                (transformErrors ?? Array.Empty<MicroPatternTransformError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray());
            this.protectedMaskErrors = new ReadOnlyCollection<MicroPatternProtectedMaskError>(
                (protectedMaskErrors ?? Array.Empty<MicroPatternProtectedMaskError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray());
            this.applicationErrors = new ReadOnlyCollection<MicroPatternApplicationError>(
                (applicationErrors ?? Array.Empty<MicroPatternApplicationError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray());
            this.renderErrors = new ReadOnlyCollection<MicroPatternRenderError>(
                (renderErrors ?? Array.Empty<MicroPatternRenderError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray());
        }

        public TerrainClusterPatternRenderErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }
        public IReadOnlyList<MicroPatternTransformError> TransformErrors => transformErrors;
        public IReadOnlyList<MicroPatternProtectedMaskError> ProtectedMaskErrors => protectedMaskErrors;
        public IReadOnlyList<MicroPatternApplicationError> ApplicationErrors => applicationErrors;
        public IReadOnlyList<MicroPatternRenderError> RenderErrors => renderErrors;

        public int CompareTo(TerrainClusterPatternRenderError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterPatternRenderError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as TerrainClusterPatternRenderError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class TerrainClusterPatternRenderResult
    {
        private readonly ReadOnlyCollection<TerrainClusterPatternRenderError> errors;

        internal TerrainClusterPatternRenderResult(
            TerrainClusterPatternRenderReport report,
            IEnumerable<TerrainClusterPatternRenderError> errors)
        {
            var copy = (errors ?? Array.Empty<TerrainClusterPatternRenderError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<TerrainClusterPatternRenderError>(copy);
            Report = copy.Length == 0 ? report : null;
        }

        public bool Success => Report != null && errors.Count == 0;
        public TerrainClusterPatternRenderReport Report { get; }
        public PatternZoneMap ZoneMap => Report == null ? null : Report.ZoneMap;
        public IReadOnlyList<MicroPatternApplicationPlan> ApplicationPlans =>
            Report == null ? Array.Empty<MicroPatternApplicationPlan>() : Report.ApplicationPlans;
        public MicroPatternRenderDelta RenderDelta => Report == null ? null : Report.RenderDelta;
        public TerrainClusterPatternWorkingCanvas InitialWorkingCanvas => Report == null ? null : Report.InitialWorkingCanvas;
        public TerrainClusterPatternWorkingCanvas FinalWorkingCanvas => Report == null ? null : Report.FinalWorkingCanvas;
        public IReadOnlyList<TerrainClusterPatternRenderError> Errors => errors;
        public string CanonicalDigest => Report == null ? string.Empty : Report.CanonicalDigest;
    }

    public static class TerrainClusterPatternRenderer
    {
        public static TerrainClusterPatternRenderResult Render(TerrainClusterPatternRenderRequest request)
        {
            var errors = new List<TerrainClusterPatternRenderError>();
            ValidateArtifacts(request, errors);
            if (errors.Count != 0) return Failure(errors);

            var zoneBuild = TerrainClusterPatternZoneBuilder.Build(
                request.LocalCanvas, request.TraversalCompilation, request.RouteWitnessReport, request.AuthoredZones);
            errors.AddRange(zoneBuild.Errors);
            if (errors.Count != 0) return Failure(errors);

            var initialCanvas = BuildInitialCanvas(request.RouteWitnessReport.StaticShell, zoneBuild.Map);
            var activeCoordinates = request.LocalCanvas.TileCells
                .Where(value => value.State == ClusterChunkMaskState.Active)
                .Select(value => value.Coordinate).ToHashSet();
            if (initialCanvas.CoordinateCount != activeCoordinates.Count ||
                initialCanvas.Cells.Any(value => !activeCoordinates.Contains(value.Coordinate)))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.WorkingCanvasCoverageMismatch,
                    "workingCanvas", "Pre-render full working canvas must equal exact active Local Canvas union.");
                return Failure(errors);
            }

            var canonicalPlacements = request.Placements
                .Where(value => value != null)
                .OrderBy(value => value.PlacementId, StringComparer.Ordinal).ToArray();
            ValidatePlacementIds(request.Placements, errors);
            var plans = new List<MicroPatternApplicationPlan>();
            var renderRequests = new List<MicroPatternRenderRequest>();
            foreach (var intent in canonicalPlacements)
            {
                CompilePlacement(request.PatternCatalog, zoneBuild.Map, initialCanvas, intent,
                    plans, renderRequests, errors);
            }
            if (errors.Count != 0) return Failure(errors);

            var targetCoordinates = plans.SelectMany(value => value.Cells)
                .Select(value => value.TargetCoordinate).Distinct()
                .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray();
            foreach (var coordinate in targetCoordinates.Where(value => !activeCoordinates.Contains(value)))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.WorkingCanvasCoverageMismatch,
                    CoordinatePath(coordinate), "Application-plan coordinate is outside active full working canvas.");
            }
            if (errors.Count != 0) return Failure(errors);

            var targetStates = new List<MicroPatternRenderCellState>();
            foreach (var coordinate in targetCoordinates)
            {
                if (!initialCanvas.TryGetCell(coordinate, out var cell))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.WorkingCanvasCoverageMismatch,
                        CoordinatePath(coordinate), "Application-plan coordinate is missing from full working canvas.");
                }
                else targetStates.Add(cell.State);
            }
            if (errors.Count != 0) return Failure(errors);

            var renderResult = MicroPatternOrderedRenderer.Render(
                renderRequests, new MicroPatternRenderTarget(targetStates));
            if (!renderResult.Success)
            {
                errors.Add(new TerrainClusterPatternRenderError(
                    TerrainClusterPatternRenderErrorCode.RenderConflict,
                    "map10/render", "MAP10_03 ordered renderer rejected the atomic batch.",
                    null, null, null, renderResult.Errors));
                return Failure(errors);
            }

            var finalCanvas = ApplyDelta(initialCanvas, renderResult.Delta);
            if (finalCanvas.CoordinateCount != activeCoordinates.Count ||
                finalCanvas.Cells.Any(value => !activeCoordinates.Contains(value.Coordinate)))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.WorkingCanvasCoverageMismatch,
                    "workingCanvas/final", "Final full working canvas coverage changed.");
                return Failure(errors);
            }

            var protectedCoordinates = zoneBuild.Map.Cells
                .Where(value => value.HasKind(TerrainClusterPatternZoneKind.AbsoluteProtected))
                .Select(value => value.Coordinate).ToHashSet();
            var protectedWrites = renderResult.Delta.Writes.Count(value => protectedCoordinates.Contains(value.TargetCoordinate));
            var protectedChanges = renderResult.Delta.Cells.Count(value =>
                protectedCoordinates.Contains(value.TargetCoordinate) && !value.ValuesEqual);
            if (protectedWrites != 0 || protectedChanges != 0)
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.ProtectedWriteDetected,
                    "map10/delta", "AbsoluteProtected write/change counts must both be zero.");
                return Failure(errors);
            }

            var planDigest = ComputePlanDigest(plans);
            var reportDigest = ComputeReportDigest(request, zoneBuild.Map, canonicalPlacements, plans,
                planDigest, renderResult.Delta, initialCanvas, finalCanvas, targetCoordinates.Length,
                initialCanvas.CoordinateCount - targetCoordinates.Length, protectedWrites, protectedChanges);
            var report = new TerrainClusterPatternRenderReport(
                zoneBuild.Map, canonicalPlacements, plans, planDigest, renderResult.Delta,
                initialCanvas, finalCanvas, targetCoordinates.Length,
                initialCanvas.CoordinateCount - targetCoordinates.Length,
                protectedWrites, protectedChanges, reportDigest);
            return new TerrainClusterPatternRenderResult(report, errors);
        }

        private static void ValidateArtifacts(
            TerrainClusterPatternRenderRequest request,
            ICollection<TerrainClusterPatternRenderError> errors)
        {
            if (request == null)
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.MissingInput, "request", "Render request is required.");
                return;
            }
            if (request.LocalCanvas == null) Add(errors, TerrainClusterPatternRenderErrorCode.MissingInput, "localCanvas", "MAP11_01 Local Canvas is required.");
            if (request.TraversalCompilation == null) Add(errors, TerrainClusterPatternRenderErrorCode.MissingInput, "traversal", "MAP11_03 traversal compilation is required.");
            if (request.RouteWitnessReport == null) Add(errors, TerrainClusterPatternRenderErrorCode.MissingInput, "routeWitness", "MAP11_04 route witness is required.");
            if (request.PatternCatalog == null) Add(errors, TerrainClusterPatternRenderErrorCode.MissingInput, "patternCatalog", "MAP10 catalog is required.");
            if (errors.Count != 0) return;

            var ids = new[]
            {
                request.LocalCanvas.ClusterId,
                request.TraversalCompilation.ClusterId,
                request.RouteWitnessReport.ClusterId,
                request.RouteWitnessReport.StaticShell.ClusterId,
            };
            if (ids.Any(value => value != ids[0]))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.ArtifactIdentityMismatch,
                    "clusterId", string.Join("|", ids.Select(value => value.Value)));
            }
            ValidateDigest(errors, "localCanvas", request.ExpectedLocalCanvasDigest, request.LocalCanvas.CanonicalDigest);
            ValidateDigest(errors, "traversal", request.ExpectedTraversalCompilationDigest, request.TraversalCompilation.CanonicalDigest);
            ValidateDigest(errors, "routeWitness", request.ExpectedRouteWitnessDigest, request.RouteWitnessReport.CanonicalDigest);
            ValidateDigest(errors, "patternCatalog", request.ExpectedPatternCatalogDigest, request.PatternCatalog.StableDigest);
            ValidateDigest(errors, "routeWitness/traversal", request.RouteWitnessReport.TraversalCompilationDigest, request.TraversalCompilation.CanonicalDigest);
            ValidateDigest(errors, "staticShell/localCanvas", request.RouteWitnessReport.StaticShell.LocalCanvasDigest, request.LocalCanvas.CanonicalDigest);
        }

        private static void ValidateDigest(
            ICollection<TerrainClusterPatternRenderError> errors,
            string path,
            string expected,
            string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.ArtifactDigestMismatch,
                    path, (expected ?? string.Empty) + "!=" + (actual ?? string.Empty));
            }
        }

        private static void ValidatePlacementIds(
            IEnumerable<TerrainClusterPatternPlacementIntent> placements,
            ICollection<TerrainClusterPatternRenderError> errors)
        {
            var snapshot = placements == null ? Array.Empty<TerrainClusterPatternPlacementIntent>() : placements.ToArray();
            if (snapshot.Length == 0)
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.MissingInput,
                    "placements", "At least one caller-selected placement is required.");
                return;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < snapshot.Length; index++)
            {
                var intent = snapshot[index];
                if (intent == null)
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.MissingInput,
                        "placements[" + Number(index) + "]", "Placement intent is required.");
                    continue;
                }
                if (!IsPlacementId(intent.PlacementId))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.InvalidPlacementId,
                        "placements/" + intent.PlacementId, intent.PlacementId);
                }
                else if (!ids.Add(intent.PlacementId))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.DuplicatePlacementId,
                        "placements/" + intent.PlacementId, intent.PlacementId);
                }
            }
        }

        private static void CompilePlacement(
            MicroPatternAuthoringCatalog catalog,
            PatternZoneMap zoneMap,
            TerrainClusterPatternWorkingCanvas initialCanvas,
            TerrainClusterPatternPlacementIntent intent,
            ICollection<MicroPatternApplicationPlan> plans,
            ICollection<MicroPatternRenderRequest> renderRequests,
            ICollection<TerrainClusterPatternRenderError> errors)
        {
            var path = "placements/" + intent.PlacementId;
            if (!IsPlacementId(intent.PlacementId)) return;
            if (!catalog.TryGetDefinition(intent.PatternId, out var definition))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.UnknownPattern,
                    path + "/pattern", intent.PatternId.Value);
                return;
            }
            var definitionDigest = definition.ComputeStableDigest();
            if (!string.Equals(intent.ExpectedDefinitionDigest, definitionDigest, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterPatternRenderErrorCode.ArtifactDigestMismatch,
                    path + "/definitionDigest", intent.ExpectedDefinitionDigest + "!=" + definitionDigest);
                return;
            }

            var transformed = MicroPatternTransformer.Transform(definition, intent.Transform);
            if (!transformed.Success)
            {
                errors.Add(new TerrainClusterPatternRenderError(
                    TerrainClusterPatternRenderErrorCode.InvalidPlacement,
                    path + "/transform", "MAP10_02 transformer rejected placement.",
                    transformed.Errors, null, null, null));
                return;
            }

            var placement = new MicroPatternPlacement(intent.Origin);
            var protectedMask = MicroPatternProtectedMaskBuilder.Build(
                placement, zoneMap.MicroPatternProtectedCells);
            if (!protectedMask.Success)
            {
                errors.Add(new TerrainClusterPatternRenderError(
                    TerrainClusterPatternRenderErrorCode.InvalidPlacement,
                    path + "/protectedMask", "MAP10_02 protected-mask builder rejected placement.",
                    null, protectedMask.Errors, null, null));
                return;
            }

            var application = MicroPatternApplicationPlanner.Plan(
                transformed.Pattern, placement, zoneMap.MicroPatternProtectedCells);
            if (!application.Success)
            {
                errors.Add(new TerrainClusterPatternRenderError(
                    TerrainClusterPatternRenderErrorCode.ApplicationPlanRejected,
                    path + "/application", "MAP10_02 application planner rejected placement.",
                    null, null, application.Errors, null));
                return;
            }

            ValidatePlanPermissions(application.Plan, zoneMap, initialCanvas, path, errors);
            plans.Add(application.Plan);
            var requestId = new MicroPatternRenderRequestId("MPR_" + intent.PlacementId.Substring(4));
            renderRequests.Add(new MicroPatternRenderRequest(requestId, application.Plan));
        }

        private static void ValidatePlanPermissions(
            MicroPatternApplicationPlan plan,
            PatternZoneMap zoneMap,
            TerrainClusterPatternWorkingCanvas initialCanvas,
            string path,
            ICollection<TerrainClusterPatternRenderError> errors)
        {
            foreach (var cell in plan.Cells)
            {
                if (!initialCanvas.TryGetCell(cell.TargetCoordinate, out var initial))
                {
                    Add(errors, TerrainClusterPatternRenderErrorCode.InvalidPlacement,
                        path + "/" + Coordinate(cell.TargetCoordinate), "Plan target is outside active full working canvas.");
                    continue;
                }
                zoneMap.TryGetCell(cell.TargetCoordinate, out var zone);
                foreach (var instruction in cell.Instructions.Where(value => value.Operation != MicroPatternOperation.NoChange))
                {
                    if (zone != null && zone.HasKind(TerrainClusterPatternZoneKind.AbsoluteProtected))
                    {
                        Add(errors, TerrainClusterPatternRenderErrorCode.ProtectedWriteDetected,
                            path + "/" + Coordinate(cell.TargetCoordinate), instruction.Layer + "/" + instruction.Operation);
                        continue;
                    }
                    if (instruction.Layer == MicroPatternLayer.Surface ||
                        instruction.Layer == MicroPatternLayer.Material ||
                        instruction.Layer == MicroPatternLayer.Hazard)
                    {
                        Add(errors, TerrainClusterPatternRenderErrorCode.UnsupportedLayerOperation,
                            path + "/" + Coordinate(cell.TargetCoordinate), instruction.Layer + "/" + instruction.Operation);
                        continue;
                    }

                    var allowed = instruction.Layer == MicroPatternLayer.Geometry &&
                                  instruction.Operation == MicroPatternOperation.AddSolid &&
                                  zone != null && zone.HasKind(TerrainClusterPatternZoneKind.GeometryAdd) && !initial.Solid;
                    allowed |= instruction.Layer == MicroPatternLayer.Geometry &&
                               instruction.Operation == MicroPatternOperation.CarveAir &&
                               zone != null && zone.HasKind(TerrainClusterPatternZoneKind.GeometryCarve) && initial.Solid;
                    allowed |= instruction.Layer == MicroPatternLayer.Affordance &&
                               instruction.Operation == MicroPatternOperation.SetAffordance &&
                               zone != null && zone.HasKind(TerrainClusterPatternZoneKind.Affordance);
                    allowed |= instruction.Layer == MicroPatternLayer.Marker &&
                               instruction.Operation == MicroPatternOperation.SetMarker &&
                               zone != null && zone.HasKind(TerrainClusterPatternZoneKind.Marker);
                    if (!allowed)
                    {
                        Add(errors, TerrainClusterPatternRenderErrorCode.UnauthorizedZoneOperation,
                            path + "/" + Coordinate(cell.TargetCoordinate), instruction.Layer + "/" + instruction.Operation);
                    }
                }
            }
        }

        private static TerrainClusterPatternWorkingCanvas BuildInitialCanvas(
            TerrainClusterStaticShell shell,
            PatternZoneMap zoneMap)
        {
            var cells = new List<TerrainClusterPatternWorkingCell>();
            foreach (var shellCell in shell.Cells.OrderBy(value => value.CompiledCoordinate.Y).ThenBy(value => value.CompiledCoordinate.X))
            {
                var solid = shellCell.Occupancy == TerrainClusterShellOccupancy.Solid;
                var provenance = new List<TerrainClusterPatternGeometryProvenanceKind>
                {
                    solid ? TerrainClusterPatternGeometryProvenanceKind.StaticShellSolid : TerrainClusterPatternGeometryProvenanceKind.StaticShellAir,
                };
                var renderEvidence = new List<MicroPatternRenderSourceEvidence>
                {
                    new MicroPatternRenderSourceEvidence(MicroPatternLayer.Geometry, solid ? "TCSS_SOLID" : "TCSS_AIR"),
                };
                if (zoneMap.TryGetCell(shellCell.CompiledCoordinate, out var zone) &&
                    zone.HasKind(TerrainClusterPatternZoneKind.GeometryCarve))
                {
                    solid = true;
                    provenance.Add(TerrainClusterPatternGeometryProvenanceKind.GeometryCarveSubstrate);
                    renderEvidence.Add(new MicroPatternRenderSourceEvidence(
                        MicroPatternLayer.Geometry, "TCPS_GEOMETRY_CARVE_SUBSTRATE"));
                }
                var state = new MicroPatternRenderCellState(shellCell.CompiledCoordinate, solid,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, renderEvidence);
                cells.Add(new TerrainClusterPatternWorkingCell(state, shellCell, provenance));
            }
            return new TerrainClusterPatternWorkingCanvas(shell.ClusterId, cells, ComputeCanvasDigest(cells));
        }

        private static TerrainClusterPatternWorkingCanvas ApplyDelta(
            TerrainClusterPatternWorkingCanvas initial,
            MicroPatternRenderDelta delta)
        {
            var cells = initial.Cells.ToDictionary(value => value.Coordinate, value => value);
            foreach (var rendered in delta.Cells)
            {
                var before = cells[rendered.TargetCoordinate];
                cells[rendered.TargetCoordinate] = new TerrainClusterPatternWorkingCell(
                    rendered.After, before.StaticShellCell, before.GeometryProvenance);
            }
            return new TerrainClusterPatternWorkingCanvas(initial.ClusterId, cells.Values, ComputeCanvasDigest(cells.Values));
        }

        private static string ComputeCanvasDigest(IEnumerable<TerrainClusterPatternWorkingCell> cells)
        {
            var material = new StringBuilder();
            foreach (var cell in cells.OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X))
            {
                Append(material, "CELL", Number(cell.Coordinate.X), Number(cell.Coordinate.Y),
                    cell.Solid ? "SOLID" : "AIR", cell.SurfaceId, cell.AffordanceId,
                    cell.MaterialId, cell.HazardId, cell.MarkerId,
                    cell.StaticShellCell.Occupancy.ToString(),
                    string.Join(",", cell.GeometryProvenance.Select(value => value.ToString())));
                foreach (var source in cell.State.Provenance.OrderBy(value => value)) Append(material, "SOURCE", source.ToString());
                foreach (var source in cell.StaticShellCell.Provenance)
                {
                    Append(material, "SHELL", source.VariantId.Value, source.EdgeId,
                        source.EnvelopeSetKind.ToString(), Coordinate(source.SourceCoordinate), Coordinate(source.CompiledCoordinate));
                }
            }
            return Sha256(material.ToString());
        }

        private static string ComputePlanDigest(IEnumerable<MicroPatternApplicationPlan> plans)
        {
            var material = new StringBuilder();
            foreach (var plan in plans.OrderBy(value => value.StableDigest, StringComparer.Ordinal))
                Append(material, "PLAN", plan.StableDigest);
            return Sha256(material.ToString());
        }

        private static string ComputeReportDigest(
            TerrainClusterPatternRenderRequest request,
            PatternZoneMap zoneMap,
            IEnumerable<TerrainClusterPatternPlacementIntent> placements,
            IEnumerable<MicroPatternApplicationPlan> plans,
            string planDigest,
            MicroPatternRenderDelta delta,
            TerrainClusterPatternWorkingCanvas initial,
            TerrainClusterPatternWorkingCanvas final,
            int targetCount,
            int untouchedCount,
            int protectedWrites,
            int protectedChanges)
        {
            var material = new StringBuilder();
            Append(material, "CLUSTER", request.LocalCanvas.ClusterId.Value);
            Append(material, "LOCAL_CANVAS", request.LocalCanvas.CanonicalDigest);
            Append(material, "TRAVERSAL", request.TraversalCompilation.CanonicalDigest);
            Append(material, "ROUTE_WITNESS", request.RouteWitnessReport.CanonicalDigest);
            Append(material, "CATALOG", request.PatternCatalog.StableDigest);
            Append(material, "ZONES", zoneMap.CanonicalDigest);
            foreach (var placement in placements.OrderBy(value => value.PlacementId, StringComparer.Ordinal))
                Append(material, "PLACEMENT", placement.ApplicationIdentity, placement.ExpectedDefinitionDigest);
            foreach (var plan in plans.OrderBy(value => value.StableDigest, StringComparer.Ordinal)) Append(material, "PLAN", plan.StableDigest);
            Append(material, "PLAN_DIGEST", planDigest);
            Append(material, "MAP10_RENDER", delta.StableDigest);
            Append(material, "INITIAL", initial.CanonicalDigest);
            Append(material, "FINAL", final.CanonicalDigest);
            Append(material, "COUNTS", Number(initial.CoordinateCount), Number(targetCount), Number(untouchedCount),
                Number(delta.Cells.Count), Number(protectedWrites), Number(protectedChanges));
            return Sha256(material.ToString());
        }

        private static TerrainClusterPatternRenderResult Failure(IEnumerable<TerrainClusterPatternRenderError> errors)
        {
            return new TerrainClusterPatternRenderResult(null, errors);
        }

        private static bool IsPlacementId(string value)
        {
            if (value == null || !value.StartsWith("TCP_", StringComparison.Ordinal) || value.Length <= 4) return false;
            for (var index = 4; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') && character != '_') return false;
            }
            return true;
        }

        private static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void Append(StringBuilder target, params string[] fields)
        {
            foreach (var field in fields)
            {
                var value = field ?? string.Empty;
                target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
                target.Append(':');
                target.Append(value);
            }
            target.Append('\n');
        }

        private static void Add(
            ICollection<TerrainClusterPatternRenderError> errors,
            TerrainClusterPatternRenderErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new TerrainClusterPatternRenderError(code, path, detail));
        }

        private static string CoordinatePath(LocalTileCoord value) => "workingCanvas[" + Coordinate(value) + "]";
        private static string Coordinate(LocalTileCoord value) => Number(value.X) + "," + Number(value.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
