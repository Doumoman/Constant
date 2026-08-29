using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Activities.Authoring;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.EventOverlays.Authoring;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using StarNight.MapAuthoring.WorldGeneration.Import;

namespace StarNight.MapAuthoring.WorldGeneration.Activities
{
    public enum ActivityPreviewState
    {
        Static = 1,
        Active = 2,
        Removed = 3,
    }

    public sealed class ActivityEventPreviewRequest
    {
        public ActivityEventPreviewRequest(
            string activityId,
            string eventOverlayId = "",
            string expectedAggregateDigest = "",
            string expectedEventSourceOwnerId = "",
            string expectedEventSourceSlotKind = "")
        {
            ActivityId = activityId ?? string.Empty;
            EventOverlayId = eventOverlayId ?? string.Empty;
            ExpectedAggregateDigest = expectedAggregateDigest ?? string.Empty;
            ExpectedEventSourceOwnerId = expectedEventSourceOwnerId ?? string.Empty;
            ExpectedEventSourceSlotKind = expectedEventSourceSlotKind ?? string.Empty;
        }

        public string ActivityId { get; }
        public string EventOverlayId { get; }
        public string ExpectedAggregateDigest { get; }
        public string ExpectedEventSourceOwnerId { get; }
        public string ExpectedEventSourceSlotKind { get; }
    }

    public sealed class ActivityEventPreviewCell
    {
        internal ActivityEventPreviewCell(
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            ClusterChunkCoord owningChunk,
            string occupancy,
            bool protectedOpen,
            bool baselineRoute)
        {
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            OwningChunk = owningChunk;
            Occupancy = occupancy ?? string.Empty;
            ProtectedOpen = protectedOpen;
            BaselineRoute = baselineRoute;
        }

        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterChunkCoord OwningChunk { get; }
        public string Occupancy { get; }
        public bool ProtectedOpen { get; }
        public bool BaselineRoute { get; }
    }

    public sealed class ActivityEventPreviewMarker
    {
        internal ActivityEventPreviewMarker(
            string identity,
            string token,
            string label,
            string sourceKind,
            string sourceOwnerId,
            string sourceSlotKind,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            string operation,
            string payloadId)
        {
            Identity = identity ?? string.Empty;
            Token = token ?? string.Empty;
            Label = label ?? string.Empty;
            SourceKind = sourceKind ?? string.Empty;
            SourceOwnerId = sourceOwnerId ?? string.Empty;
            SourceSlotKind = sourceSlotKind ?? string.Empty;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            Operation = operation ?? string.Empty;
            PayloadId = payloadId ?? string.Empty;
        }

        public string Identity { get; }
        public string Token { get; }
        public string Label { get; }
        public string SourceKind { get; }
        public string SourceOwnerId { get; }
        public string SourceSlotKind { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public string Operation { get; }
        public string PayloadId { get; }
    }

    public sealed class ActivityEventPreviewRouteWitness
    {
        internal ActivityEventPreviewRouteWitness(
            string identity,
            string token,
            LocalTileCoord sourceStart,
            LocalTileCoord sourceEnd,
            LocalTileCoord compiledStart,
            LocalTileCoord compiledEnd,
            string detail)
        {
            Identity = identity ?? string.Empty;
            Token = token ?? string.Empty;
            SourceStart = sourceStart;
            SourceEnd = sourceEnd;
            CompiledStart = compiledStart;
            CompiledEnd = compiledEnd;
            Detail = detail ?? string.Empty;
        }

        public string Identity { get; }
        public string Token { get; }
        public LocalTileCoord SourceStart { get; }
        public LocalTileCoord SourceEnd { get; }
        public LocalTileCoord CompiledStart { get; }
        public LocalTileCoord CompiledEnd { get; }
        public string Detail { get; }
    }

    public sealed class ActivityStatePreviewSnapshot
    {
        private readonly ReadOnlyCollection<ActivityEventPreviewCell> cells;
        private readonly ReadOnlyCollection<ActivityEventPreviewMarker> activityMarkers;
        private readonly ReadOnlyCollection<ActivityEventPreviewMarker> eventMarkers;
        private readonly ReadOnlyCollection<ActivityEventPreviewRouteWitness> routeWitnesses;

        internal ActivityStatePreviewSnapshot(
            ActivityPreviewState state,
            ActivityStructureId activityId,
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            string underlyingDigest,
            string cellDigest,
            string routeDigest,
            string accessDigest,
            string protectionDigest,
            IEnumerable<ActivityEventPreviewCell> sourceCells,
            IEnumerable<ActivityEventPreviewMarker> sourceActivityMarkers,
            IEnumerable<ActivityEventPreviewMarker> sourceEventMarkers,
            IEnumerable<ActivityEventPreviewRouteWitness> sourceRouteWitnesses,
            int cueObservationOrdinal,
            int activationBoundaryOrdinal,
            int safePocketProofCount,
            int recoveryProofCount,
            int exitPreservationProofCount,
            int rewardPreservationProofCount,
            int residualMarkerCount,
            int tileDeltaCount,
            int colliderDeltaCount,
            int rngDrawCount,
            string stableDigest)
        {
            State = state;
            ActivityId = activityId;
            ClusterId = clusterId;
            VariantId = variantId;
            UnderlyingDigest = underlyingDigest ?? string.Empty;
            CellDigest = cellDigest ?? string.Empty;
            RouteDigest = routeDigest ?? string.Empty;
            AccessDigest = accessDigest ?? string.Empty;
            ProtectionDigest = protectionDigest ?? string.Empty;
            cells = Freeze(sourceCells, CompareCells);
            activityMarkers = Freeze(sourceActivityMarkers, CompareMarkers);
            eventMarkers = Freeze(sourceEventMarkers, CompareMarkers);
            routeWitnesses = Freeze(sourceRouteWitnesses, CompareRoutes);
            CueObservationOrdinal = cueObservationOrdinal;
            ActivationBoundaryOrdinal = activationBoundaryOrdinal;
            SafePocketProofCount = safePocketProofCount;
            RecoveryProofCount = recoveryProofCount;
            ExitPreservationProofCount = exitPreservationProofCount;
            RewardPreservationProofCount = rewardPreservationProofCount;
            ResidualMarkerCount = residualMarkerCount;
            TileDeltaCount = tileDeltaCount;
            ColliderDeltaCount = colliderDeltaCount;
            RngDrawCount = rngDrawCount;
            StableDigest = stableDigest ?? string.Empty;
        }

        public ActivityPreviewState State { get; }
        public ActivityStructureId ActivityId { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public string UnderlyingDigest { get; }
        public string CellDigest { get; }
        public string RouteDigest { get; }
        public string AccessDigest { get; }
        public string ProtectionDigest { get; }
        public IReadOnlyList<ActivityEventPreviewCell> Cells => cells;
        public IReadOnlyList<ActivityEventPreviewMarker> ActivityMarkers => activityMarkers;
        public IReadOnlyList<ActivityEventPreviewMarker> EventMarkers => eventMarkers;
        public IReadOnlyList<ActivityEventPreviewRouteWitness> RouteWitnesses => routeWitnesses;
        public int ActivityMarkerCount => activityMarkers.Count;
        public int EventMarkerCount => eventMarkers.Count;
        public int MarkerCount => activityMarkers.Count + eventMarkers.Count;
        public int CueObservationOrdinal { get; }
        public int ActivationBoundaryOrdinal { get; }
        public int SafePocketProofCount { get; }
        public int RecoveryProofCount { get; }
        public int ExitPreservationProofCount { get; }
        public int RewardPreservationProofCount { get; }
        public int ResidualMarkerCount { get; }
        public int TileDeltaCount { get; }
        public int ColliderDeltaCount { get; }
        public int RngDrawCount { get; }
        public string StableDigest { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, Comparison<T> comparison)
        {
            var copy = (source ?? Array.Empty<T>()).ToArray();
            Array.Sort(copy, comparison);
            return new ReadOnlyCollection<T>(copy);
        }

        private static int CompareCells(ActivityEventPreviewCell left, ActivityEventPreviewCell right)
        {
            var comparison = left.CompiledCoordinate.Y.CompareTo(right.CompiledCoordinate.Y);
            return comparison != 0 ? comparison : left.CompiledCoordinate.X.CompareTo(right.CompiledCoordinate.X);
        }

        private static int CompareMarkers(ActivityEventPreviewMarker left, ActivityEventPreviewMarker right) =>
            string.Compare(left.Identity, right.Identity, StringComparison.Ordinal);

        private static int CompareRoutes(ActivityEventPreviewRouteWitness left, ActivityEventPreviewRouteWitness right) =>
            string.Compare(left.Identity, right.Identity, StringComparison.Ordinal);
    }

    public sealed class EventOverlayPreviewSnapshot
    {
        private readonly ReadOnlyCollection<ActivityEventPreviewMarker> markers;

        internal EventOverlayPreviewSnapshot(
            string eventId,
            bool explicitEmpty,
            string kind,
            int weight,
            int minimumProgressionGap,
            IEnumerable<ActivityEventPreviewMarker> sourceMarkers,
            string sourceOwnerSummary,
            string operationSummary,
            string payloadSummary,
            string contractDigest,
            string candidateIndexDigest,
            string stableDigest)
        {
            EventId = eventId ?? string.Empty;
            ExplicitEmpty = explicitEmpty;
            Kind = kind ?? string.Empty;
            Weight = weight;
            MinimumProgressionGap = minimumProgressionGap;
            markers = new ReadOnlyCollection<ActivityEventPreviewMarker>((sourceMarkers ?? Array.Empty<ActivityEventPreviewMarker>())
                .OrderBy(value => value.Identity, StringComparer.Ordinal).ToArray());
            SourceOwnerSummary = sourceOwnerSummary ?? string.Empty;
            OperationSummary = operationSummary ?? string.Empty;
            PayloadSummary = payloadSummary ?? string.Empty;
            ContractDigest = contractDigest ?? string.Empty;
            CandidateIndexDigest = candidateIndexDigest ?? string.Empty;
            StableDigest = stableDigest ?? string.Empty;
        }

        public string EventId { get; }
        public bool ExplicitEmpty { get; }
        public string Kind { get; }
        public int Weight { get; }
        public int MinimumProgressionGap { get; }
        public IReadOnlyList<ActivityEventPreviewMarker> Markers => markers;
        public int MarkerCount => markers.Count;
        public string SourceOwnerSummary { get; }
        public string OperationSummary { get; }
        public string PayloadSummary { get; }
        public string ContractDigest { get; }
        public string CandidateIndexDigest { get; }
        public string StableDigest { get; }
    }

    public sealed class ActivityEventComparisonSnapshot
    {
        internal ActivityEventComparisonSnapshot(
            int staticToActiveMarkerDelta,
            int activeToRemovedMarkerDelta,
            int staticToActiveCellDelta,
            int activeToRemovedCellDelta,
            int routeDelta,
            int accessDelta,
            int protectionDelta,
            int geometryDelta,
            string stableDigest)
        {
            StaticToActiveMarkerDelta = staticToActiveMarkerDelta;
            ActiveToRemovedMarkerDelta = activeToRemovedMarkerDelta;
            StaticToActiveCellDelta = staticToActiveCellDelta;
            ActiveToRemovedCellDelta = activeToRemovedCellDelta;
            RouteDelta = routeDelta;
            AccessDelta = accessDelta;
            ProtectionDelta = protectionDelta;
            GeometryDelta = geometryDelta;
            StableDigest = stableDigest ?? string.Empty;
        }

        public int StaticToActiveMarkerDelta { get; }
        public int ActiveToRemovedMarkerDelta { get; }
        public int StaticToActiveCellDelta { get; }
        public int ActiveToRemovedCellDelta { get; }
        public int RouteDelta { get; }
        public int AccessDelta { get; }
        public int ProtectionDelta { get; }
        public int GeometryDelta { get; }
        public bool MarkerOnly => StaticToActiveCellDelta == 0 && ActiveToRemovedCellDelta == 0 &&
                                  RouteDelta == 0 && AccessDelta == 0 && ProtectionDelta == 0 && GeometryDelta == 0;
        public string StableDigest { get; }
    }

    public enum ActivityEventPreviewBuildErrorCode
    {
        MissingRequest = 1,
        ImportFailed = 2,
        ActivityNotFound = 3,
        EventNotFound = 4,
        DigestMismatch = 5,
        SourceMismatch = 6,
        CompileFailed = 7,
        NonCanonicalPublication = 8,
    }

    public sealed class ActivityEventPreviewBuildError : IComparable<ActivityEventPreviewBuildError>
    {
        internal ActivityEventPreviewBuildError(ActivityEventPreviewBuildErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public ActivityEventPreviewBuildErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(ActivityEventPreviewBuildError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class ActivityEventPreviewBuildResult
    {
        private readonly ReadOnlyCollection<ActivityEventPreviewBuildError> errors;

        internal ActivityEventPreviewBuildResult(
            ActivityStatePreviewSnapshot staticSnapshot,
            ActivityStatePreviewSnapshot activeSnapshot,
            ActivityStatePreviewSnapshot removedSnapshot,
            EventOverlayPreviewSnapshot eventSnapshot,
            ActivityEventComparisonSnapshot comparison,
            string aggregateDigest,
            string activityCatalogDigest,
            string eventCatalogDigest,
            string stableDigest,
            IEnumerable<ActivityEventPreviewBuildError> sourceErrors)
        {
            var copy = (sourceErrors ?? Array.Empty<ActivityEventPreviewBuildError>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<ActivityEventPreviewBuildError>(copy);
            AggregateDigest = aggregateDigest ?? string.Empty;
            ActivityCatalogDigest = activityCatalogDigest ?? string.Empty;
            EventCatalogDigest = eventCatalogDigest ?? string.Empty;
            StableDigest = stableDigest ?? string.Empty;
            if (copy.Length == 0)
            {
                StaticSnapshot = staticSnapshot;
                ActiveSnapshot = activeSnapshot;
                RemovedSnapshot = removedSnapshot;
                EventSnapshot = eventSnapshot;
                Comparison = comparison;
            }
        }

        public bool Success => errors.Count == 0 && StaticSnapshot != null && ActiveSnapshot != null &&
                               RemovedSnapshot != null && EventSnapshot != null && Comparison != null &&
                               StableDigest.Length != 0;
        public ActivityStatePreviewSnapshot StaticSnapshot { get; }
        public ActivityStatePreviewSnapshot ActiveSnapshot { get; }
        public ActivityStatePreviewSnapshot RemovedSnapshot { get; }
        public EventOverlayPreviewSnapshot EventSnapshot { get; }
        public ActivityEventComparisonSnapshot Comparison { get; }
        public string AggregateDigest { get; }
        public string ActivityCatalogDigest { get; }
        public string EventCatalogDigest { get; }
        public string StableDigest { get; }
        public IReadOnlyList<ActivityEventPreviewBuildError> Errors => errors;
    }

    public sealed class ActivityEventPreviewModel
    {
        public const string ApprovedAggregateDigest = "46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b";
        public const string ApprovedActivityCatalogDigest = "3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a";
        public const string ApprovedEventCatalogDigest = "2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0";

        private static readonly ReadOnlyCollection<string> activityIds = new ReadOnlyCollection<string>(new[]
        {
            "ACT_CRATER_BOULDER_CHAIN", "ACT_CRATER_RICOCHET_MINE", "ACT_DOUGH_TIME_TRIAL",
            "ACT_MARU_REWIND_ANOMALY", "ACT_MILL_ESCORT_CART", "ACT_MILL_GEAR_GRID",
            "ACT_MILL_PESTLE_WORKSHOP",
        });

        private static readonly ReadOnlyCollection<string> eventIds = new ReadOnlyCollection<string>(new[]
        {
            "EVT_METEOR_FALL", "EVT_WANDERING_MERCHANT", "EVT_RARE_CREATURE",
            "EVT_MARU_INTERVENTION", "EVT_EMPTY",
        });

        public IReadOnlyList<string> ActivityIds => activityIds;
        public IReadOnlyList<string> EventIds => eventIds;

        public ActivityEventPreviewBuildResult Build(ActivityEventPreviewRequest request)
        {
            var errors = new List<ActivityEventPreviewBuildError>();
            var terrain = new TerrainClusterCsvImporterV2().Import();
            if (!terrain.Success)
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.ImportFailed, "terrainCatalog",
                    string.Join(";", terrain.Errors.Select(value => value.ToString()))));
            var patterns = new MicroPatternCsvImporterV2().Import();
            if (!patterns.Success || !patterns.Published)
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.ImportFailed, "microPatternCatalog",
                    string.Join(";", patterns.Errors.Select(value => value.ToString()))));
            if (errors.Count != 0) return Failure(errors);
            var content = new ActivityEventCsvImporterV2().Import(terrain.Catalog);
            if (!content.Success || !content.Published)
            {
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.ImportFailed, "activityEventCatalog",
                    string.Join(";", content.Errors.Select(value => value.ToString()))));
                return Failure(errors);
            }
            return Build(request, terrain.Catalog, terrain.StableDigest,
                patterns.Catalog, patterns.StableDigest, content);
        }

        public ActivityEventPreviewBuildResult Build(
            ActivityEventPreviewRequest request,
            TerrainClusterAuthoringCatalog terrainCatalog,
            string terrainCatalogDigest,
            MicroPatternAuthoringCatalog microPatternCatalog,
            string microPatternCatalogDigest,
            ActivityEventCsvImportResult content)
        {
            var errors = new List<ActivityEventPreviewBuildError>();
            if (request == null)
            {
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.MissingRequest, "request", "Preview request is required."));
                return Failure(errors);
            }
            if (terrainCatalog == null || microPatternCatalog == null || content == null ||
                !content.Success || !content.Published || content.ActivityCatalog == null || content.EventCatalog == null)
            {
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.ImportFailed, "catalogs",
                    "Published TerrainCluster, MicroPattern, Activity, and Event catalogs are required."));
                return Failure(errors);
            }
            if (request.ExpectedAggregateDigest.Length != 0 &&
                !string.Equals(request.ExpectedAggregateDigest, content.AggregateStableDigest, StringComparison.Ordinal))
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.DigestMismatch, "request.expectedAggregateDigest",
                    request.ExpectedAggregateDigest));
            if (!string.Equals(content.ActivityCatalog.StableDigest, ApprovedActivityCatalogDigest, StringComparison.Ordinal) ||
                !string.Equals(content.EventCatalog.StableDigest, ApprovedEventCatalogDigest, StringComparison.Ordinal))
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.DigestMismatch, "catalogs",
                    content.ActivityCatalog.StableDigest + "|" + content.EventCatalog.StableDigest));
            if (!content.ActivityCatalog.TryGet(new ActivityStructureId(request.ActivityId), out var activity))
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.ActivityNotFound, "request.activityId", request.ActivityId));
            EventOverlayAuthoringEntry selectedEvent = null;
            if (request.EventOverlayId.Length != 0 &&
                !content.EventCatalog.TryGet(new EventOverlayId(request.EventOverlayId), out selectedEvent))
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.EventNotFound, "request.eventOverlayId", request.EventOverlayId));
            if (errors.Count != 0) return Failure(errors);
            if (!terrainCatalog.TryGet(activity.Contract.TerrainClusterId, out var terrain))
            {
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch, "activity.terrainClusterId",
                    activity.Contract.TerrainClusterId.Value));
                return Failure(errors);
            }

            try
            {
                return BuildCompiled(request, activity, selectedEvent, terrain, terrainCatalog,
                    terrainCatalogDigest, microPatternCatalog, microPatternCatalogDigest, content, errors);
            }
            catch (Exception exception)
            {
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.CompileFailed, "exception",
                    exception.GetType().Name + ": " + exception.Message));
                return Failure(errors);
            }
        }

        private static ActivityEventPreviewBuildResult BuildCompiled(
            ActivityEventPreviewRequest request,
            ActivityAuthoringEntry activity,
            EventOverlayAuthoringEntry selectedEvent,
            TerrainClusterAuthoringEntry terrain,
            TerrainClusterAuthoringCatalog terrainCatalog,
            string terrainCatalogDigest,
            MicroPatternAuthoringCatalog microPatternCatalog,
            string microPatternCatalogDigest,
            ActivityEventCsvImportResult content,
            ICollection<ActivityEventPreviewBuildError> errors)
        {
            var sourceValidation = TerrainClusterContractValidator.Validate(terrain.Contract);
            if (!sourceValidation.IsValid)
                return CompileFailure("terrain.contract", sourceValidation.Errors.Select(value => value.ToString()), errors);
            var footprint = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(terrain.Contract, ClusterFootprintTransform.R0));
            if (!footprint.IsSuccess) return CompileFailure("terrain.footprint", footprint.Errors.Select(value => value.ToString()), errors);
            var sourceEntry = terrain.Contract.Ports.Single(value => value.IsPrimary && value.Kind == ClusterPortKind.Entry);
            var sourceExit = terrain.Contract.Ports.Single(value => value.IsPrimary && value.Kind == ClusterPortKind.Exit);
            var role = TerrainClusterRoleSocketCompiler.Compile(new TerrainClusterRoleSocketCompileRequest(
                terrain.Contract, sourceValidation.CanonicalDigest, footprint.LocalCanvas, footprint.CanonicalDigest,
                new[]
                {
                    new ClusterSectorSocketEvidence("SR_PREVIEW_ENTRY", "SOCKET_PREVIEW_ENTRY",
                        sourceEntry.OutwardSide, 2, true, ClusterPortKind.Entry),
                    new ClusterSectorSocketEvidence("SR_PREVIEW_EXIT", "SOCKET_PREVIEW_EXIT",
                        sourceExit.OutwardSide, 3, true, ClusterPortKind.Exit),
                }));
            if (!role.IsSuccess) return CompileFailure("terrain.roleSocket", role.Errors.Select(value => value.ToString()), errors);
            var traversal = TerrainClusterTraversalCompiler.Compile(new TerrainClusterTraversalCompileRequest(
                terrain.Contract, sourceValidation.CanonicalDigest, footprint.LocalCanvas, footprint.CanonicalDigest,
                role.Contract, role.CanonicalDigest));
            if (!traversal.IsSuccess) return CompileFailure("terrain.traversal", traversal.Errors.Select(value => value.ToString()), errors);
            var witness = TerrainClusterRouteWitnessCompiler.Compile(new TerrainClusterRouteWitnessCompileRequest(
                footprint.LocalCanvas, footprint.CanonicalDigest, role.Contract, role.CanonicalDigest,
                traversal.Compilation, traversal.CanonicalDigest, terrain.RouteIntent));
            if (!witness.IsSuccess) return CompileFailure("terrain.routeWitness", witness.Errors.Select(value => value.ToString()), errors);
            var pattern = TerrainClusterPatternRenderer.Render(new TerrainClusterPatternRenderRequest(
                footprint.LocalCanvas, footprint.CanonicalDigest,
                traversal.Compilation, traversal.CanonicalDigest,
                witness.Report, witness.CanonicalDigest,
                microPatternCatalog, microPatternCatalogDigest,
                Array.Empty<TerrainClusterPatternZoneCell>(),
                Array.Empty<TerrainClusterPatternPlacementIntent>()));
            if (!pattern.Success) return CompileFailure("terrain.pattern", pattern.Errors.Select(value => value.ToString()), errors);

            var activityValidation = ActivityContractValidator.Validate(activity.Contract, terrain.Contract);
            if (!activityValidation.IsValid || !string.Equals(activityValidation.CanonicalDigest,
                    activity.PlacementProfile.ActivityDigest, StringComparison.Ordinal))
                return CompileFailure("activity.contract", activityValidation.Errors.Select(value => value.ToString())
                    .Concat(new[] { activityValidation.CanonicalDigest }), errors);
            var shell = ActivityShellCompiler.Compile(new ActivityShellCompileRequest(
                terrain.Contract, sourceValidation.CanonicalDigest,
                activity.Contract, activityValidation.CanonicalDigest,
                footprint.LocalCanvas, footprint.CanonicalDigest,
                role.Contract, role.CanonicalDigest,
                traversal.Compilation, traversal.CanonicalDigest,
                witness.Report, witness.CanonicalDigest,
                pattern.Report, pattern.CanonicalDigest, pattern.Report.FinalWorkingCanvas.CanonicalDigest,
                ActivityZones(activity.Contract.Slots),
                activity.Contract.Slots.Select(value => new ActivitySlotProjectionIntent(value.Id, SlotSemantic(value.Kind)))));
            if (!shell.IsSuccess) return CompileFailure("activity.shell", shell.Errors.Select(value => value.ToString()), errors);

            var cueEvidence = activity.Contract.Cues.Select(cue => BuildCueEvidence(
                activity, cue, footprint.LocalCanvas, traversal.Compilation,
                witness.Report, pattern.Report.FinalWorkingCanvas)).ToArray();
            if (cueEvidence.Any(value => value == null))
            {
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.CompileFailed, "activity.cueEvidence",
                    "A clear pre-activation cue observation witness was not found."));
                return Failure(errors);
            }
            if (!role.Contract.TryGetPrimaryPort(ClusterPortKind.Exit, out var projectedExit))
            {
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.CompileFailed, "activity.exit", "Primary Exit is required."));
                return Failure(errors);
            }
            var reward = shell.Canvas.Slots.Single(value => value.Semantic == ActivitySlotSemanticKind.RewardAnchor);
            var rewardBinding = shell.Canvas.ProgressionBindings.Single(value => value.Phase == ProgressionPhaseKind.Reward);
            var critical = new[]
            {
                new ActivityCriticalTargetEvidence(ActivityCriticalTargetKind.MandatoryExit,
                    projectedExit.PortId, projectedExit.SourceCoordinate, projectedExit.RoleAnchorId,
                    witness.Report.BaselineRoute.ExitNodeId),
                new ActivityCriticalTargetEvidence(ActivityCriticalTargetKind.Reward,
                    reward.SlotId.Value, reward.SourceCoordinate, rewardBinding.ProgressionNodeId, string.Empty),
            };
            var removal = ActivityRemovalSafetyCompiler.Compile(new ActivityRemovalSafetyCompileRequest(
                terrain.Contract, activity.Contract, shell.Canvas, footprint.LocalCanvas,
                role.Contract, traversal.Compilation, witness.Report, pattern.Report,
                shell.CanonicalDigest, cueEvidence,
                new ActivityOverlayRemovalIntent(OverlayIdentities(shell.Canvas)), critical));
            if (!removal.IsSuccess) return CompileFailure("activity.removal", removal.Errors.Select(value => value.ToString()), errors);

            var activityIndex = CompileActivityIndex(activity.PlacementProfile, terrainCatalogDigest);
            if (!activityIndex.Success) return CompileFailure("activity.candidateIndex",
                activityIndex.Errors.Select(value => value.Code + "|" + value.Path + "|" + value.Detail), errors);
            var eventIndex = CompileReferenceEventIndex(content.EventCatalog, sourceValidation.CanonicalDigest,
                activityIndex.Index.CanonicalDigest);
            if (!eventIndex.Success) return CompileFailure("event.candidateIndex",
                eventIndex.Errors.Select(value => value.Code + "|" + value.Path + "|" + value.Detail), errors);

            var eventSnapshot = BuildEventSnapshot(request, selectedEvent, activity, terrain,
                terrainCatalog, footprint.LocalCanvas, sourceValidation.CanonicalDigest,
                eventIndex.Index.CanonicalDigest, content, errors);
            if (errors.Count != 0) return Failure(errors);

            var cells = BuildCells(footprint.LocalCanvas, traversal.Compilation,
                witness.Report, pattern.Report.FinalWorkingCanvas);
            var routes = BuildRoutes(footprint.LocalCanvas, role.Contract, traversal.Compilation, witness.Report);
            var activityMarkers = BuildActivityMarkers(activity, shell.Canvas, removal);
            var eventMarkers = eventSnapshot.Markers;
            var cellDigest = DigestCells(cells);
            var protectionDigest = DigestStrings(traversal.Compilation.ProtectedTiles
                .Select(value => Coordinate(value.CompiledCoordinate)));
            var accessDigest = Sha256("ACCESS|" + Number(activity.Contract.RemovalSafety.RouteTypeBeforeRemoval) + "|" +
                                      Number((int)activity.Contract.RemovalSafety.AccessClassBeforeRemoval));
            var underlyingDigest = DigestStrings(new[]
            {
                terrainCatalogDigest, sourceValidation.CanonicalDigest, footprint.CanonicalDigest,
                role.CanonicalDigest, traversal.CanonicalDigest, witness.CanonicalDigest,
                pattern.CanonicalDigest, cellDigest, protectionDigest, accessDigest,
            });
            var staticSnapshot = BuildState(ActivityPreviewState.Static, activity, underlyingDigest,
                cellDigest, witness.CanonicalDigest, accessDigest, protectionDigest, cells,
                Array.Empty<ActivityEventPreviewMarker>(), Array.Empty<ActivityEventPreviewMarker>(), routes,
                removal);
            var activeSnapshot = BuildState(ActivityPreviewState.Active, activity, underlyingDigest,
                cellDigest, witness.CanonicalDigest, accessDigest, protectionDigest, cells,
                activityMarkers, eventMarkers, routes, removal);
            var removedSnapshot = BuildState(ActivityPreviewState.Removed, activity, underlyingDigest,
                cellDigest, witness.CanonicalDigest, accessDigest, protectionDigest, cells,
                Array.Empty<ActivityEventPreviewMarker>(), Array.Empty<ActivityEventPreviewMarker>(), routes,
                removal);
            var comparisonMaterial = "COMPARE|" + staticSnapshot.StableDigest + "|" + activeSnapshot.StableDigest +
                                     "|" + removedSnapshot.StableDigest + "|" + Number(activeSnapshot.MarkerCount);
            var comparison = new ActivityEventComparisonSnapshot(
                activeSnapshot.MarkerCount, -activeSnapshot.MarkerCount, 0, 0, 0, 0, 0, 0,
                Sha256(comparisonMaterial));
            var stableDigest = DigestStrings(new[]
            {
                content.AggregateStableDigest, content.ActivityCatalog.StableDigest,
                content.EventCatalog.StableDigest, terrainCatalogDigest, microPatternCatalogDigest,
                activityIndex.Index.CanonicalDigest, eventIndex.Index.CanonicalDigest,
                staticSnapshot.StableDigest, activeSnapshot.StableDigest,
                removedSnapshot.StableDigest, eventSnapshot.StableDigest, comparison.StableDigest,
            });
            return new ActivityEventPreviewBuildResult(staticSnapshot, activeSnapshot, removedSnapshot,
                eventSnapshot, comparison, content.AggregateStableDigest,
                content.ActivityCatalog.StableDigest, content.EventCatalog.StableDigest,
                stableDigest, errors);
        }

        private static ActivityStatePreviewSnapshot BuildState(
            ActivityPreviewState state,
            ActivityAuthoringEntry activity,
            string underlyingDigest,
            string cellDigest,
            string routeDigest,
            string accessDigest,
            string protectionDigest,
            IEnumerable<ActivityEventPreviewCell> cells,
            IEnumerable<ActivityEventPreviewMarker> activityMarkers,
            IEnumerable<ActivityEventPreviewMarker> eventMarkers,
            IEnumerable<ActivityEventPreviewRouteWitness> routes,
            ActivityRemovalSafetyCompileResult removal)
        {
            var activityCopy = activityMarkers.ToArray();
            var eventCopy = eventMarkers.ToArray();
            var cue = removal.CueProofs.Single();
            var exitProofs = removal.CriticalTargetProofs.Count(value => value.Kind == ActivityCriticalTargetKind.MandatoryExit);
            var rewardProofs = removal.CriticalTargetProofs.Count(value => value.Kind == ActivityCriticalTargetKind.Reward);
            var material = string.Join("|", new[]
            {
                "STATE", Number((int)state), activity.Id.Value, underlyingDigest, cellDigest, routeDigest,
                accessDigest, protectionDigest,
                string.Join(",", activityCopy.OrderBy(value => value.Identity, StringComparer.Ordinal).Select(value => value.Identity)),
                string.Join(",", eventCopy.OrderBy(value => value.Identity, StringComparer.Ordinal).Select(value => value.Identity)),
                Number(cue.ObservationEdgeOrdinal), Number(cue.ActivationBoundaryEdgeOrdinal),
                Number(removal.SafePocketProofs.Count), Number(removal.RecoveryProofs.Count),
                Number(exitProofs), Number(rewardProofs),
            });
            return new ActivityStatePreviewSnapshot(state, activity.Id,
                activity.Contract.TerrainClusterId, activity.Contract.CompatibleSpineVariantId,
                underlyingDigest, cellDigest, routeDigest, accessDigest, protectionDigest,
                cells, activityCopy, eventCopy, routes,
                cue.ObservationEdgeOrdinal, cue.ActivationBoundaryEdgeOrdinal,
                removal.SafePocketProofs.Count, removal.RecoveryProofs.Count,
                exitProofs, rewardProofs, 0, 0, 0, 0, Sha256(material));
        }

        private static EventOverlayPreviewSnapshot BuildEventSnapshot(
            ActivityEventPreviewRequest request,
            EventOverlayAuthoringEntry selectedEvent,
            ActivityAuthoringEntry activity,
            TerrainClusterAuthoringEntry terrain,
            TerrainClusterAuthoringCatalog terrainCatalog,
            TerrainClusterLocalCanvas localCanvas,
            string sourceDigest,
            string candidateIndexDigest,
            ActivityEventCsvImportResult content,
            ICollection<ActivityEventPreviewBuildError> errors)
        {
            if (selectedEvent == null)
                return new EventOverlayPreviewSnapshot("NONE", false, "None", 0, 0,
                    Array.Empty<ActivityEventPreviewMarker>(), string.Empty, string.Empty, string.Empty,
                    string.Empty, candidateIndexDigest, Sha256("EVENT|NONE|" + candidateIndexDigest));

            if (selectedEvent.Contract.Kind != EventOverlayKind.Empty)
            {
                if (selectedEvent.Contract.TerrainClusterId != terrain.Id)
                    errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch, "event.terrainClusterId",
                        selectedEvent.Contract.TerrainClusterId.Value + "!=" + terrain.Id.Value));
                if (selectedEvent.Contract.ActivityStructureId.HasValue &&
                    selectedEvent.Contract.ActivityStructureId.Value != activity.Id)
                    errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch, "event.activityId",
                        selectedEvent.Contract.ActivityStructureId.Value.Value + "!=" + activity.Id.Value));
            }
            if (!terrainCatalog.TryGet(selectedEvent.Contract.TerrainClusterId, out var eventTerrain))
            {
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch, "event.sourceTerrain",
                    selectedEvent.Contract.TerrainClusterId.Value));
                return null;
            }
            ActivityStructureContract referencedActivity = null;
            if (selectedEvent.Contract.ActivityStructureId.HasValue)
            {
                if (!content.ActivityCatalog.TryGet(selectedEvent.Contract.ActivityStructureId.Value, out var referenced))
                    errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch, "event.sourceActivity",
                        selectedEvent.Contract.ActivityStructureId.Value.Value));
                else referencedActivity = referenced.Contract;
            }
            var validation = EventOverlayValidator.Validate(selectedEvent.Contract, eventTerrain.Contract,
                referencedActivity, selectedEvent.MarkerTargets.Select(value => value.MarkerId),
                selectedEvent.RemovalEvidence);
            if (!validation.IsValid || !string.Equals(validation.CanonicalDigest,
                    selectedEvent.Profile.ContractDigest, StringComparison.Ordinal))
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.CompileFailed, "event.validation",
                    string.Join(";", validation.Errors.Select(value => value.ToString()))));

            var markers = new List<ActivityEventPreviewMarker>();
            foreach (var target in selectedEvent.MarkerTargets.OrderBy(value => value.MarkerId))
            {
                if (request.ExpectedEventSourceOwnerId.Length != 0 &&
                    !string.Equals(request.ExpectedEventSourceOwnerId, target.SourceOwnerId, StringComparison.Ordinal))
                    errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch, "event.marker.sourceOwnerId",
                        request.ExpectedEventSourceOwnerId + "!=" + target.SourceOwnerId));
                if (request.ExpectedEventSourceSlotKind.Length != 0 &&
                    !string.Equals(request.ExpectedEventSourceSlotKind, target.SourceSlotKind, StringComparison.Ordinal))
                    errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch, "event.marker.sourceSlotKind",
                        request.ExpectedEventSourceSlotKind + "!=" + target.SourceSlotKind));
                if (!ValidateMarkerAuthority(target, activity, terrain))
                    errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch,
                        "event.marker/" + target.MarkerId.Value,
                        target.SourceKind + "|" + target.SourceOwnerId + "|" + target.SourceSlotKind));
                if (!localCanvas.TryGetCompiledTile(target.Coordinate, out var compiled))
                {
                    errors.Add(Error(ActivityEventPreviewBuildErrorCode.SourceMismatch,
                        "event.marker.coordinate", Coordinate(target.Coordinate)));
                    continue;
                }
                markers.Add(new ActivityEventPreviewMarker(
                    "EVENT|" + selectedEvent.Id.Value + "|" + target.MarkerId.Value,
                    "EV", selectedEvent.Id.Value, target.SourceKind, target.SourceOwnerId,
                    target.SourceSlotKind, target.Coordinate, compiled,
                    target.Operation.ToString(), target.PayloadId));
            }
            if (selectedEvent.Contract.Kind == EventOverlayKind.Empty &&
                (markers.Count != 0 || selectedEvent.Profile.Weight != 0 || selectedEvent.Profile.MinimumProgressionGap != 0))
                errors.Add(Error(ActivityEventPreviewBuildErrorCode.NonCanonicalPublication, "event.empty",
                    "Explicit Empty requires marker/weight/gap 0/0/0."));
            if (errors.Count != 0) return null;
            var sourceOwners = string.Join(",", markers.Select(value =>
                    value.SourceKind + ":" + value.SourceOwnerId + ":" + value.SourceSlotKind)
                .OrderBy(value => value, StringComparer.Ordinal));
            var operations = string.Join(",", markers.Select(value => value.Operation)
                .OrderBy(value => value, StringComparer.Ordinal));
            var payloads = string.Join(",", markers.Select(value => value.PayloadId)
                .OrderBy(value => value, StringComparer.Ordinal));
            var material = string.Join("|", new[]
            {
                "EVENT", selectedEvent.Id.Value, Number((int)selectedEvent.Contract.Kind),
                Number(selectedEvent.Profile.Weight), Number(selectedEvent.Profile.MinimumProgressionGap),
                selectedEvent.Profile.ContractDigest, candidateIndexDigest, sourceDigest,
                sourceOwners, operations, payloads,
            });
            return new EventOverlayPreviewSnapshot(selectedEvent.Id.Value,
                selectedEvent.Contract.Kind == EventOverlayKind.Empty,
                selectedEvent.Contract.Kind == EventOverlayKind.Empty ? "Explicit Empty" : selectedEvent.Contract.Kind.ToString(),
                selectedEvent.Profile.Weight, selectedEvent.Profile.MinimumProgressionGap,
                markers, sourceOwners, operations, payloads,
                selectedEvent.Profile.ContractDigest, candidateIndexDigest, Sha256(material));
        }

        private static bool ValidateMarkerAuthority(
            EventMarkerAuthoringTarget target,
            ActivityAuthoringEntry activity,
            TerrainClusterAuthoringEntry terrain)
        {
            if (string.Equals(target.SourceKind, "TERRAIN_CLUSTER", StringComparison.Ordinal))
                return string.Equals(target.SourceOwnerId, terrain.Id.Value, StringComparison.Ordinal) &&
                       string.Equals(target.SourceSlotKind, "CORE", StringComparison.Ordinal);
            if (string.Equals(target.SourceKind, "ACTIVITY", StringComparison.Ordinal))
                return string.Equals(target.SourceOwnerId, activity.Id.Value, StringComparison.Ordinal) &&
                       activity.Contract.Slots.Any(value => value.MarkerId == target.MarkerId.Value &&
                           string.Equals(value.Kind.ToString(), target.SourceSlotKind, StringComparison.Ordinal) &&
                           value.Tile == target.Coordinate);
            return string.Equals(target.SourceKind, "SPECIAL_REGION", StringComparison.Ordinal) &&
                   target.SourceOwnerId.Length != 0 && target.SourceSlotKind.Length != 0;
        }

        private static ActivityCandidateIndexCompileResult CompileActivityIndex(
            ActivityPlacementProfile profile,
            string sourceDigest)
        {
            var patchId = new BiomePatchId("PATCH_MAP12_06");
            var indices = Enumerable.Range(0, WorldGenConstants.SectorCount).ToArray();
            var biome = profile.AllowedBiomes.First();
            var patch = new BiomePatch(patchId, BiomeToken(biome), "PATCH_RULE_MAP12_06",
                BiomePatchRole.Satellite,
                new[] { new BiomePatchSeed(0, WorldGridIndex.ToCoordinate(0), BiomePatchRole.Satellite, null) },
                indices);
            var ownership = indices.Select(index => new BiomeSectorOwnership(index,
                WorldGridIndex.ToCoordinate(index), BiomeToken(biome), string.Empty, patchId)).ToArray();
            var snapshot = new BiomePatchSnapshot(12, new[] { patch }, ownership,
                Array.Empty<BiomePatchSiteBinding>());
            var coordinates = (from y in Enumerable.Range(0, profile.RequiredOpenClearanceHeight)
                               from x in Enumerable.Range(0, profile.RequiredOpenClearanceWidth)
                               select new LocalTileCoord(x, y)).ToArray();
            var opportunity = new ActivityPlacementOpportunity(
                "ACTIVITY_OPP_MAP12_06", WorldGridIndex.ToCoordinate(0), patchId,
                biome, profile.TerrainClusterId, profile.SpineVariantId,
                profile.AllowedPacingRoles.First(), profile.AllowedAccessClasses.First(),
                profile.MinimumActiveChunkCount,
                new ActivityPlacementClearanceEvidence(new LocalTileCoord(0, 0),
                    profile.RequiredOpenClearanceWidth, profile.RequiredOpenClearanceHeight,
                    coordinates, coordinates, Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>()),
                sourceDigest, sourceDigest, sourceDigest,
                profile.ShellDigest, profile.RemovalSafetyDigest);
            return ActivityCandidateIndexCompiler.Compile(new ActivityCandidateIndexCompileRequest(
                new[] { profile }, new[] { opportunity }, snapshot,
                sourceDigest, sourceDigest, sourceDigest));
        }

        private static EventOverlayCandidateIndexResult CompileReferenceEventIndex(
            EventOverlayAuthoringCatalog catalog,
            string sourceDigest,
            string activityPlanDigest)
        {
            catalog.TryGet(new EventOverlayId("EVT_EMPTY"), out var empty);
            catalog.TryGet(new EventOverlayId("EVT_METEOR_FALL"), out var meteor);
            var target = meteor.MarkerTargets.Single();
            var marker = new EventMarkerTargetEvidence(target.MarkerId,
                EventMarkerTargetSourceKind.TerrainCluster, target.SourceOwnerId,
                target.Coordinate, target.Coordinate, target.SourceSlotKind,
                "AIR", "AIR", sourceDigest, sourceDigest, sourceDigest, sourceDigest,
                default(SpecialPersistenceKey), string.Empty, string.Empty);
            var opportunity = new EventOverlayOpportunity(
                "EVENT_OPP_MAP12_06", WorldGridIndex.ToCoordinate(0), new BiomePatchId("PATCH_MAP12_06"),
                0, meteor.Profile.CompatibleBiomes.First(), meteor.Profile.CompatiblePacingRoles.First(),
                meteor.Profile.CompatibleAccessClasses.First(), meteor.Contract.TerrainClusterId,
                null, activityPlanDigest, new[] { marker });
            return EventOverlayCandidateIndexCompiler.Compile(new EventOverlayCandidateIndexRequest(
                new[] { empty.Profile, meteor.Profile }, new[] { opportunity }, activityPlanDigest));
        }

        private static IReadOnlyList<ActivityEventPreviewCell> BuildCells(
            TerrainClusterLocalCanvas canvas,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport witness,
            TerrainClusterPatternWorkingCanvas working)
        {
            var protectedSet = traversal.ProtectedTiles.Select(value => value.CompiledCoordinate).ToHashSet();
            var baseline = witness.BaselineRoute.CompiledCoordinates
                .Concat(witness.BaselineRoute.CoveredProtectedTiles).ToHashSet();
            var result = new List<ActivityEventPreviewCell>();
            foreach (var cell in working.Cells.OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X))
            {
                if (!canvas.TryGetSourceTile(cell.Coordinate, out var source) ||
                    !canvas.TryGetTileCell(cell.Coordinate, out var local)) continue;
                result.Add(new ActivityEventPreviewCell(source, cell.Coordinate, local.OwningChunk,
                    cell.Solid ? "SOLID" : "AIR", protectedSet.Contains(cell.Coordinate),
                    baseline.Contains(cell.Coordinate)));
            }
            return result;
        }

        private static IReadOnlyList<ActivityEventPreviewRouteWitness> BuildRoutes(
            TerrainClusterLocalCanvas canvas,
            TerrainClusterRoleSocketContract role,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport witness)
        {
            var result = new List<ActivityEventPreviewRouteWitness>();
            foreach (var port in role.Ports.Where(value => value.Kind == ClusterPortKind.Entry || value.Kind == ClusterPortKind.Exit))
            {
                var token = port.Kind == ClusterPortKind.Entry ? "EN" : "EX";
                result.Add(new ActivityEventPreviewRouteWitness("PORT|" + token, token,
                    port.SourceCoordinate, port.SourceCoordinate, port.CompiledCoordinate,
                    port.CompiledCoordinate, port.PortId + "|" + port.RoleAnchorId));
            }
            foreach (var edge in witness.BaselineRoute.OrderedEdges)
            {
                canvas.TryGetSourceTile(edge.CompiledStartCoordinate, out var sourceStart);
                canvas.TryGetSourceTile(edge.CompiledEndCoordinate, out var sourceEnd);
                result.Add(new ActivityEventPreviewRouteWitness("BASE|" + edge.EdgeId, "B",
                    sourceStart, sourceEnd, edge.CompiledStartCoordinate, edge.CompiledEndCoordinate,
                    edge.MovementKind + "|" + Number(edge.EstimatedDurationMilliseconds)));
            }
            foreach (var tile in traversal.ProtectedTiles)
            {
                canvas.TryGetSourceTile(tile.CompiledCoordinate, out var source);
                result.Add(new ActivityEventPreviewRouteWitness("PROTECTED|" + Coordinate(tile.CompiledCoordinate),
                    "AP", source, source, tile.CompiledCoordinate, tile.CompiledCoordinate,
                    string.Join(",", tile.Provenance.Select(value => value.EdgeId).OrderBy(value => value, StringComparer.Ordinal))));
            }
            return result;
        }

        private static IReadOnlyList<ActivityEventPreviewMarker> BuildActivityMarkers(
            ActivityAuthoringEntry activity,
            ActivityShellCanvas shell,
            ActivityRemovalSafetyCompileResult removal)
        {
            var markers = shell.Slots.Select(value => new ActivityEventPreviewMarker(
                "ACTIVITY|" + value.SlotId.Value, MarkerToken(value.Semantic), value.SlotId.Value,
                "ACTIVITY", activity.Id.Value, value.SlotKind.ToString(), value.SourceCoordinate,
                value.CompiledCoordinate, string.Empty, string.Empty)).ToList();
            foreach (var proof in removal.SafePocketProofs)
                markers.Add(new ActivityEventPreviewMarker(
                    "ACTIVITY|SAFE_POCKET|" + Coordinate(proof.SourceCoordinate), "SP", "SafePocket",
                    "ACTIVITY", activity.Id.Value, "SafePocket", proof.SourceCoordinate,
                    proof.CompiledCoordinate, string.Empty, string.Empty));
            return markers;
        }

        private static ActivityCueObservationEvidence BuildCueEvidence(
            ActivityAuthoringEntry activity,
            ActivityCue cue,
            TerrainClusterLocalCanvas localCanvas,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport witness,
            TerrainClusterPatternWorkingCanvas working)
        {
            var slot = activity.Contract.Slots.Single(value => value.Id == cue.SlotId);
            if (!localCanvas.TryGetCompiledTile(slot.Tile, out var cueCompiled) ||
                !traversal.TryGetVariant(activity.Contract.CompatibleSpineVariantId, out var baseline)) return null;
            var ordered = witness.BaselineRoute.OrderedEdges;
            for (var index = 0; index < ordered.Count - 1; index++)
            {
                if (!baseline.TryGetEdge(ordered[index].EdgeId, out var edge)) continue;
                foreach (var tile in edge.Envelope.Centerline.Concat(edge.Envelope.Clearance)
                             .Concat(edge.Envelope.Landing)
                             .OrderBy(value => value.SourceCoordinate.Y).ThenBy(value => value.SourceCoordinate.X))
                {
                    if (!working.TryGetCell(tile.CompiledCoordinate, out var cell) || cell.Solid ||
                        !GridSupercover(tile.CompiledCoordinate, cueCompiled).All(coordinate =>
                        {
                            return working.TryGetCell(coordinate, out var lineCell) && !lineCell.Solid;
                        })) continue;
                    var distance = Math.Abs(tile.CompiledCoordinate.X - cueCompiled.X) +
                                   Math.Abs(tile.CompiledCoordinate.Y - cueCompiled.Y);
                    return new ActivityCueObservationEvidence(
                        "CUE_PREVIEW_" + activity.Id.Value, cue.Kind, cue.SlotId,
                        ordered[index].EdgeId, ordered[index + 1].EdgeId,
                        tile.SourceCoordinate, Math.Max(1, distance));
                }
            }
            return null;
        }

        private static ActivityShellZoneDefinition[] ActivityZones(IEnumerable<ActivitySlot> source)
        {
            var slots = source.ToArray();
            return new[]
            {
                new ActivityShellZoneDefinition(ActivityShellZoneKind.Cue,
                    slots.Where(value => value.Kind == ActivitySlotKind.Cue).Select(value => value.Tile)),
                new ActivityShellZoneDefinition(ActivityShellZoneKind.Core,
                    slots.Where(value => value.Kind == ActivitySlotKind.Cue || value.Kind == ActivitySlotKind.Trigger ||
                                         value.Kind == ActivitySlotKind.Device || value.Kind == ActivitySlotKind.Hazard ||
                                         value.Kind == ActivitySlotKind.Projectile || value.Kind == ActivitySlotKind.Npc)
                        .Select(value => value.Tile)),
                new ActivityShellZoneDefinition(ActivityShellZoneKind.Reward,
                    slots.Where(value => value.Kind == ActivitySlotKind.Reward).Select(value => value.Tile)),
                new ActivityShellZoneDefinition(ActivityShellZoneKind.Recovery,
                    slots.Where(value => value.Kind == ActivitySlotKind.Recovery || value.Kind == ActivitySlotKind.Reset)
                        .Select(value => value.Tile)),
            };
        }

        private static ActivitySlotSemanticKind SlotSemantic(ActivitySlotKind kind)
        {
            switch (kind)
            {
                case ActivitySlotKind.Cue: return ActivitySlotSemanticKind.CueMarker;
                case ActivitySlotKind.Trigger: return ActivitySlotSemanticKind.PressurePlateTrigger;
                case ActivitySlotKind.Device: return ActivitySlotSemanticKind.DeviceAnchor;
                case ActivitySlotKind.Hazard: return ActivitySlotSemanticKind.ChaseOrHazardSpawn;
                case ActivitySlotKind.Projectile: return ActivitySlotSemanticKind.ProjectileEmitter;
                case ActivitySlotKind.Reward: return ActivitySlotSemanticKind.RewardAnchor;
                case ActivitySlotKind.Recovery: return ActivitySlotSemanticKind.RecoveryAnchor;
                case ActivitySlotKind.Reset: return ActivitySlotSemanticKind.ResetAnchor;
                case ActivitySlotKind.Npc: return ActivitySlotSemanticKind.NpcAnchor;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static string MarkerToken(ActivitySlotSemanticKind kind)
        {
            switch (kind)
            {
                case ActivitySlotSemanticKind.CueMarker: return "C";
                case ActivitySlotSemanticKind.PressurePlateTrigger: return "T";
                case ActivitySlotSemanticKind.DeviceAnchor: return "D";
                case ActivitySlotSemanticKind.ChaseOrHazardSpawn: return "H";
                case ActivitySlotSemanticKind.ProjectileEmitter: return "P";
                case ActivitySlotSemanticKind.NpcAnchor: return "N";
                case ActivitySlotSemanticKind.RewardAnchor: return "RW";
                case ActivitySlotSemanticKind.RecoveryAnchor: return "RC";
                case ActivitySlotSemanticKind.ResetAnchor: return "RS";
                default: return "?";
            }
        }

        private static IReadOnlyList<string> OverlayIdentities(ActivityShellCanvas shell)
        {
            return shell.Zones.Select(value => "ZONE|" + Number((int)value.Kind))
                .Concat(shell.Slots.Select(value => "SLOT|" + value.SlotId.Value))
                .Concat(shell.CueBindings.Select(value => "CUE|" + Number((int)value.CueKind) + "|" + value.SlotId.Value))
                .Concat(shell.MechanismBindings.Select(value => "MECHANISM|" + value.MechanismNodeId + "|" + value.SlotId.Value))
                .Concat(shell.ProgressionBindings.Select(value => "PROGRESSION|" + value.ProgressionNodeId))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static IEnumerable<LocalTileCoord> GridSupercover(LocalTileCoord start, LocalTileCoord end)
        {
            var x = start.X;
            var y = start.Y;
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var nx = Math.Abs(dx);
            var ny = Math.Abs(dy);
            var signX = Math.Sign(dx);
            var signY = Math.Sign(dy);
            var ix = 0;
            var iy = 0;
            yield return new LocalTileCoord(x, y);
            while (ix < nx || iy < ny)
            {
                var xDecision = (1 + (2 * ix)) * ny;
                var yDecision = (1 + (2 * iy)) * nx;
                if (xDecision == yDecision)
                {
                    x += signX;
                    y += signY;
                    ix++;
                    iy++;
                }
                else if (xDecision < yDecision)
                {
                    x += signX;
                    ix++;
                }
                else
                {
                    y += signY;
                    iy++;
                }
                yield return new LocalTileCoord(x, y);
            }
        }

        private static string DigestCells(IEnumerable<ActivityEventPreviewCell> cells) =>
            DigestStrings(cells.OrderBy(value => value.CompiledCoordinate.Y)
                .ThenBy(value => value.CompiledCoordinate.X)
                .Select(value => Coordinate(value.SourceCoordinate) + "|" + Coordinate(value.CompiledCoordinate) +
                                 "|" + value.Occupancy + "|" + Bool(value.ProtectedOpen) + "|" + Bool(value.BaselineRoute)));

        private static string DigestStrings(IEnumerable<string> values) => Sha256(
            string.Join("\n", (values ?? Array.Empty<string>()).Select(value => value ?? string.Empty)));

        private static string BiomeToken(MoonpalaceBiomeId biome)
        {
            switch (biome.CanonicalId)
            {
                case "MoonCrater": return "BIO_MOON_CRATER";
                case "CassiaRoot": return "BIO_CASSIA_ROOT";
                case "AbandonedMill": return "BIO_ABANDONED_MILL";
                case "MoonDough": return "BIO_MOON_DOUGH";
                default: return biome.CanonicalId;
            }
        }

        private static ActivityEventPreviewBuildResult CompileFailure(
            string path,
            IEnumerable<string> details,
            ICollection<ActivityEventPreviewBuildError> errors)
        {
            errors.Add(Error(ActivityEventPreviewBuildErrorCode.CompileFailed, path,
                string.Join(";", details ?? Array.Empty<string>())));
            return Failure(errors);
        }

        private static ActivityEventPreviewBuildError Error(
            ActivityEventPreviewBuildErrorCode code,
            string path,
            string detail) => new ActivityEventPreviewBuildError(code, path, detail);

        private static ActivityEventPreviewBuildResult Failure(
            IEnumerable<ActivityEventPreviewBuildError> errors) =>
            new ActivityEventPreviewBuildResult(null, null, null, null, null,
                string.Empty, string.Empty, string.Empty, string.Empty, errors);

        private static string Coordinate(LocalTileCoord value) =>
            Number(value.X) + "," + Number(value.Y);

        private static string Bool(bool value) => value ? "1" : "0";
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Sha256(string material)
        {
            using (var algorithm = SHA256.Create())
            {
                var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty));
                return string.Concat(bytes.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }
}
