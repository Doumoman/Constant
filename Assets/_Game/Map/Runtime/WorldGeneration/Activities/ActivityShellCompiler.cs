using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Activities
{
    public sealed class ActivityShellCompileRequest
    {
        private readonly ReadOnlyCollection<ActivityShellZoneDefinition> zones;
        private readonly ReadOnlyCollection<ActivitySlotProjectionIntent> slotIntents;

        public ActivityShellCompileRequest(
            TerrainClusterContract sourceContract,
            string expectedSourceContractDigest,
            ActivityStructureContract activity,
            string expectedActivityDigest,
            TerrainClusterLocalCanvas localCanvas,
            string expectedLocalCanvasDigest,
            TerrainClusterRoleSocketContract roleSocketContract,
            string expectedRoleSocketContractDigest,
            TerrainClusterTraversalCompilation traversalCompilation,
            string expectedTraversalCompilationDigest,
            TerrainClusterRouteWitnessReport routeWitnessReport,
            string expectedRouteWitnessDigest,
            TerrainClusterPatternRenderReport patternRenderReport,
            string expectedPatternRenderDigest,
            string expectedWorkingCanvasDigest,
            IEnumerable<ActivityShellZoneDefinition> zones,
            IEnumerable<ActivitySlotProjectionIntent> slotIntents)
        {
            SourceContract = sourceContract;
            ExpectedSourceContractDigest = expectedSourceContractDigest ?? string.Empty;
            Activity = activity;
            ExpectedActivityDigest = expectedActivityDigest ?? string.Empty;
            LocalCanvas = localCanvas;
            ExpectedLocalCanvasDigest = expectedLocalCanvasDigest ?? string.Empty;
            RoleSocketContract = roleSocketContract;
            ExpectedRoleSocketContractDigest = expectedRoleSocketContractDigest ?? string.Empty;
            TraversalCompilation = traversalCompilation;
            ExpectedTraversalCompilationDigest = expectedTraversalCompilationDigest ?? string.Empty;
            RouteWitnessReport = routeWitnessReport;
            ExpectedRouteWitnessDigest = expectedRouteWitnessDigest ?? string.Empty;
            PatternRenderReport = patternRenderReport;
            ExpectedPatternRenderDigest = expectedPatternRenderDigest ?? string.Empty;
            ExpectedWorkingCanvasDigest = expectedWorkingCanvasDigest ?? string.Empty;
            var zoneCopy = (zones ?? Array.Empty<ActivityShellZoneDefinition>()).ToArray();
            Array.Sort(zoneCopy, CompareZones);
            this.zones = new ReadOnlyCollection<ActivityShellZoneDefinition>(zoneCopy);
            var intentCopy = (slotIntents ?? Array.Empty<ActivitySlotProjectionIntent>()).ToArray();
            Array.Sort(intentCopy, CompareIntents);
            this.slotIntents = new ReadOnlyCollection<ActivitySlotProjectionIntent>(intentCopy);
        }

        public TerrainClusterContract SourceContract { get; }
        public string ExpectedSourceContractDigest { get; }
        public ActivityStructureContract Activity { get; }
        public string ExpectedActivityDigest { get; }
        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public string ExpectedLocalCanvasDigest { get; }
        public TerrainClusterRoleSocketContract RoleSocketContract { get; }
        public string ExpectedRoleSocketContractDigest { get; }
        public TerrainClusterTraversalCompilation TraversalCompilation { get; }
        public string ExpectedTraversalCompilationDigest { get; }
        public TerrainClusterRouteWitnessReport RouteWitnessReport { get; }
        public string ExpectedRouteWitnessDigest { get; }
        public TerrainClusterPatternRenderReport PatternRenderReport { get; }
        public string ExpectedPatternRenderDigest { get; }
        public string ExpectedWorkingCanvasDigest { get; }
        public IReadOnlyList<ActivityShellZoneDefinition> Zones => zones;
        public IReadOnlyList<ActivitySlotProjectionIntent> SlotIntents => slotIntents;

        private static int CompareZones(ActivityShellZoneDefinition left, ActivityShellZoneDefinition right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return left.Kind.CompareTo(right.Kind);
        }

        private static int CompareIntents(ActivitySlotProjectionIntent left, ActivitySlotProjectionIntent right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            var comparison = left.SlotId.CompareTo(right.SlotId);
            return comparison != 0 ? comparison : left.Semantic.CompareTo(right.Semantic);
        }
    }

    public enum ActivityShellCompileErrorCode
    {
        MissingInput = 1,
        InvalidActivityContract = 2,
        IdentityMismatch = 3,
        ArtifactDigestMismatch = 4,
        MissingZone = 5,
        InvalidZone = 6,
        DuplicateZoneCoordinate = 7,
        MissingSlotIntent = 8,
        DuplicateSlotIntent = 9,
        UnknownSlot = 10,
        SlotSemanticMismatch = 11,
        SlotOutsideActiveCanvas = 12,
        SlotOutsideRequiredZone = 13,
        MissingGraphSlotBinding = 14,
        WorkingCanvasMismatch = 15,
        ProtectedEvidenceMismatch = 16,
        NonCanonicalPublication = 17,
    }

    public sealed class ActivityShellCompileError :
        IEquatable<ActivityShellCompileError>,
        IComparable<ActivityShellCompileError>
    {
        public ActivityShellCompileError(
            ActivityShellCompileErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public ActivityShellCompileErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(ActivityShellCompileError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(ActivityShellCompileError other)
        {
            return other != null && Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as ActivityShellCompileError);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class ActivityShellCompileResult
    {
        private readonly ReadOnlyCollection<ActivityShellCompileError> errors;

        internal ActivityShellCompileResult(
            ActivityShellCanvas canvas,
            IEnumerable<ActivityShellCompileError> errors)
        {
            var copy = (errors ?? Array.Empty<ActivityShellCompileError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<ActivityShellCompileError>(copy);
            Canvas = copy.Length == 0 ? canvas : null;
        }

        public bool IsSuccess => Canvas != null && errors.Count == 0;
        public ActivityShellCanvas Canvas { get; }
        public IReadOnlyList<ActivityShellCompileError> Errors => errors;
        public IReadOnlyList<ActivityShellZoneDefinition> Zones =>
            Canvas == null ? Array.Empty<ActivityShellZoneDefinition>() : Canvas.Zones;
        public IReadOnlyList<ProjectedActivityShellCell> ZoneCells =>
            Canvas == null ? Array.Empty<ProjectedActivityShellCell>() : Canvas.ZoneCells;
        public IReadOnlyList<ProjectedActivitySlot> Slots =>
            Canvas == null ? Array.Empty<ProjectedActivitySlot>() : Canvas.Slots;
        public IReadOnlyList<ActivityCueSlotBinding> CueBindings =>
            Canvas == null ? Array.Empty<ActivityCueSlotBinding>() : Canvas.CueBindings;
        public IReadOnlyList<ActivityMechanismSlotBinding> MechanismBindings =>
            Canvas == null ? Array.Empty<ActivityMechanismSlotBinding>() : Canvas.MechanismBindings;
        public IReadOnlyList<ActivityProgressionShellBinding> ProgressionBindings =>
            Canvas == null ? Array.Empty<ActivityProgressionShellBinding>() : Canvas.ProgressionBindings;
        public string CanonicalDigest => Canvas == null ? string.Empty : Canvas.CanonicalDigest;
    }

    public static class ActivityShellCompiler
    {
        public const string RulesetVersion = "MAP12_01_ACTIVITY_SHELL_SLOT_COMPILER_V1";

        private static readonly ActivityShellZoneKind[] RequiredZones =
        {
            ActivityShellZoneKind.Cue,
            ActivityShellZoneKind.Core,
            ActivityShellZoneKind.Reward,
            ActivityShellZoneKind.Recovery,
        };

        public static ActivityShellCompileResult Compile(ActivityShellCompileRequest request)
        {
            var errors = new List<ActivityShellCompileError>();
            if (!ValidateRequiredInputs(request, errors)) return Failure(errors);

            var sourceValidation = TerrainClusterContractValidator.Validate(request.SourceContract);
            if (!sourceValidation.IsValid)
            {
                foreach (var sourceError in sourceValidation.Errors)
                {
                    Add(errors, ActivityShellCompileErrorCode.IdentityMismatch,
                        "sourceContract/" + sourceError.Path, sourceError.ToString());
                }
            }
            var sourceDigest = sourceValidation.IsValid ? sourceValidation.CanonicalDigest : string.Empty;
            ValidateDigest(errors, "sourceContract", request.ExpectedSourceContractDigest, sourceDigest);

            var activityValidation = ActivityContractValidator.Validate(request.Activity, request.SourceContract);
            if (!activityValidation.IsValid)
            {
                foreach (var activityError in activityValidation.Errors)
                {
                    Add(errors, ActivityShellCompileErrorCode.InvalidActivityContract,
                        "activity/" + activityError.Path, activityError.ToString());
                }
            }
            var activityDigest = activityValidation.IsValid ? activityValidation.CanonicalDigest : string.Empty;
            ValidateDigest(errors, "activity", request.ExpectedActivityDigest, activityDigest);
            ValidateArtifactChain(request, sourceDigest, errors);

            var protectedByCoordinate = request.TraversalCompilation.ProtectedTiles
                .ToDictionary(value => value.CompiledCoordinate);
            ValidateWorkingCanvas(request, protectedByCoordinate, errors);

            var projectedZoneCells = new List<ProjectedActivityShellCell>();
            var projectedZoneByIdentity = new Dictionary<string, ProjectedActivityShellCell>(StringComparer.Ordinal);
            var zoneCoordinates = BuildZones(
                request, protectedByCoordinate, projectedZoneCells, projectedZoneByIdentity, errors);
            var projectedSlots = BuildSlots(
                request, protectedByCoordinate, zoneCoordinates, projectedZoneByIdentity, errors);
            var projectedById = projectedSlots
                .GroupBy(value => value.SlotId)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single());
            var cueBindings = BuildCueBindings(request.Activity, projectedById, errors);
            var mechanismBindings = BuildMechanismBindings(request.Activity, projectedById, errors);
            var progressionBindings = BuildProgressionBindings(
                request.Activity, request.RouteWitnessReport, projectedById, errors);

            ValidatePublication(
                request, projectedZoneCells, projectedSlots, cueBindings,
                mechanismBindings, progressionBindings, errors);
            if (errors.Count != 0) return Failure(errors);

            var canonicalDigest = ComputeDigest(
                request, activityDigest, sourceDigest, projectedZoneCells, projectedSlots,
                cueBindings, mechanismBindings, progressionBindings);
            var canvas = new ActivityShellCanvas(
                request.Activity.Id,
                request.SourceContract.Id,
                request.Activity.CompatibleSpineVariantId,
                activityDigest,
                sourceDigest,
                request.LocalCanvas.CanonicalDigest,
                request.RoleSocketContract.CanonicalDigest,
                request.TraversalCompilation.CanonicalDigest,
                request.RouteWitnessReport.CanonicalDigest,
                request.PatternRenderReport.CanonicalDigest,
                request.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest,
                request.Zones,
                projectedZoneCells,
                projectedSlots,
                cueBindings,
                mechanismBindings,
                progressionBindings,
                canonicalDigest);
            return new ActivityShellCompileResult(canvas, errors);
        }

        private static bool ValidateRequiredInputs(
            ActivityShellCompileRequest request,
            ICollection<ActivityShellCompileError> errors)
        {
            if (request == null)
            {
                Add(errors, ActivityShellCompileErrorCode.MissingInput, "request", "Compile request is required.");
                return false;
            }

            if (request.SourceContract == null)
                Add(errors, ActivityShellCompileErrorCode.MissingInput, "sourceContract", "TerrainCluster contract is required.");
            if (request.Activity == null)
                Add(errors, ActivityShellCompileErrorCode.MissingInput, "activity", "Activity contract is required.");
            if (request.LocalCanvas == null)
                Add(errors, ActivityShellCompileErrorCode.MissingInput, "localCanvas", "Local Canvas is required.");
            if (request.RoleSocketContract == null)
                Add(errors, ActivityShellCompileErrorCode.MissingInput, "roleSocket", "Role/socket contract is required.");
            if (request.TraversalCompilation == null)
                Add(errors, ActivityShellCompileErrorCode.MissingInput, "traversal", "Traversal compilation is required.");
            if (request.RouteWitnessReport == null || request.RouteWitnessReport.StaticShell == null)
                Add(errors, ActivityShellCompileErrorCode.MissingInput, "routeWitness", "Route witness and Static Shell are required.");
            if (request.PatternRenderReport == null || request.PatternRenderReport.FinalWorkingCanvas == null)
                Add(errors, ActivityShellCompileErrorCode.MissingInput, "patternRender", "Final pattern working Canvas is required.");
            return errors.Count == 0;
        }

        private static void ValidateArtifactChain(
            ActivityShellCompileRequest request,
            string sourceDigest,
            ICollection<ActivityShellCompileError> errors)
        {
            var clusterId = request.SourceContract.Id;
            if (request.Activity.TerrainClusterId != clusterId ||
                request.LocalCanvas.ClusterId != clusterId ||
                request.RoleSocketContract.ClusterId != clusterId ||
                request.TraversalCompilation.ClusterId != clusterId ||
                request.RouteWitnessReport.ClusterId != clusterId ||
                request.RouteWitnessReport.StaticShell.ClusterId != clusterId ||
                request.PatternRenderReport.FinalWorkingCanvas.ClusterId != clusterId)
            {
                Add(errors, ActivityShellCompileErrorCode.IdentityMismatch, "clusterId",
                    request.Activity.TerrainClusterId.Value + "|" + request.LocalCanvas.ClusterId.Value + "|" +
                    request.RoleSocketContract.ClusterId.Value + "|" + request.TraversalCompilation.ClusterId.Value + "|" +
                    request.RouteWitnessReport.ClusterId.Value + "|" + request.PatternRenderReport.FinalWorkingCanvas.ClusterId.Value);
            }

            CompiledClusterSpineVariant variant;
            if (!request.TraversalCompilation.TryGetVariant(request.Activity.CompatibleSpineVariantId, out variant))
            {
                Add(errors, ActivityShellCompileErrorCode.IdentityMismatch, "variant",
                    request.Activity.CompatibleSpineVariantId.Value);
            }
            if (request.RouteWitnessReport.BaselineRoute == null ||
                request.RouteWitnessReport.BaselineRoute.VariantId != request.Activity.CompatibleSpineVariantId)
            {
                Add(errors, ActivityShellCompileErrorCode.IdentityMismatch, "routeWitness.baselineVariant",
                    request.Activity.CompatibleSpineVariantId.Value);
            }
            if (request.RouteWitnessReport.RecoveryRoutes.Count == 0)
            {
                Add(errors, ActivityShellCompileErrorCode.IdentityMismatch, "routeWitness.recovery",
                    "At least one approved recovery witness is required.");
            }

            ValidateDigest(errors, "localCanvas", request.ExpectedLocalCanvasDigest, request.LocalCanvas.CanonicalDigest);
            ValidateDigest(errors, "roleSocket", request.ExpectedRoleSocketContractDigest, request.RoleSocketContract.CanonicalDigest);
            ValidateDigest(errors, "traversal", request.ExpectedTraversalCompilationDigest, request.TraversalCompilation.CanonicalDigest);
            ValidateDigest(errors, "routeWitness", request.ExpectedRouteWitnessDigest, request.RouteWitnessReport.CanonicalDigest);
            ValidateDigest(errors, "patternRender", request.ExpectedPatternRenderDigest, request.PatternRenderReport.CanonicalDigest);
            ValidateDigest(errors, "workingCanvas", request.ExpectedWorkingCanvasDigest,
                request.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest);
            ValidateDigest(errors, "roleSocket.sourceContract", sourceDigest, request.RoleSocketContract.SourceContractDigest);
            ValidateDigest(errors, "roleSocket.localCanvas", request.LocalCanvas.CanonicalDigest,
                request.RoleSocketContract.LocalCanvasDigest);
            ValidateDigest(errors, "traversal.sourceContract", sourceDigest,
                request.TraversalCompilation.SourceContractDigest);
            ValidateDigest(errors, "traversal.localCanvas", request.LocalCanvas.CanonicalDigest,
                request.TraversalCompilation.LocalCanvasDigest);
            ValidateDigest(errors, "traversal.roleSocket", request.RoleSocketContract.CanonicalDigest,
                request.TraversalCompilation.RoleSocketContractDigest);
            ValidateDigest(errors, "routeWitness.traversal", request.TraversalCompilation.CanonicalDigest,
                request.RouteWitnessReport.TraversalCompilationDigest);
            ValidateDigest(errors, "staticShell.localCanvas", request.LocalCanvas.CanonicalDigest,
                request.RouteWitnessReport.StaticShell.LocalCanvasDigest);
            ValidateDigest(errors, "staticShell.traversal", request.TraversalCompilation.CanonicalDigest,
                request.RouteWitnessReport.StaticShell.TraversalCompilationDigest);
        }

        private static void ValidateWorkingCanvas(
            ActivityShellCompileRequest request,
            IReadOnlyDictionary<LocalTileCoord, ClusterTraversalProtectedTile> protectedByCoordinate,
            ICollection<ActivityShellCompileError> errors)
        {
            var active = request.LocalCanvas.TileCells
                .Where(value => value.State == ClusterChunkMaskState.Active)
                .Select(value => value.Coordinate)
                .ToHashSet();
            var working = request.PatternRenderReport.FinalWorkingCanvas;
            if (working.CoordinateCount != active.Count ||
                working.Cells.Any(value => !active.Contains(value.Coordinate)) ||
                active.Any(value => !working.TryGetCell(value, out _)) ||
                request.RouteWitnessReport.StaticShell.ActiveTileCount != active.Count)
            {
                Add(errors, ActivityShellCompileErrorCode.WorkingCanvasMismatch, "workingCanvas.coverage",
                    "Final working Canvas and Static Shell must cover the exact active Local Canvas.");
            }

            foreach (var item in protectedByCoordinate)
            {
                TerrainClusterStaticShellCell shellCell;
                TerrainClusterPatternWorkingCell workingCell;
                if (!active.Contains(item.Key) ||
                    !request.RouteWitnessReport.StaticShell.TryGetCell(item.Key, out shellCell) ||
                    !working.TryGetCell(item.Key, out workingCell) ||
                    workingCell.StaticShellCell.CompiledCoordinate != item.Key)
                {
                    Add(errors, ActivityShellCompileErrorCode.ProtectedEvidenceMismatch,
                        "protected[" + Coordinate(item.Key) + "]",
                        "Protected traversal evidence must resolve through Static Shell and working Canvas.");
                }
            }

            foreach (var shellCell in request.RouteWitnessReport.StaticShell.Cells.Where(value => value.IsProtectedOpen))
            {
                if (!protectedByCoordinate.ContainsKey(shellCell.CompiledCoordinate))
                {
                    Add(errors, ActivityShellCompileErrorCode.ProtectedEvidenceMismatch,
                        "staticShell.protected[" + Coordinate(shellCell.CompiledCoordinate) + "]",
                        "Protected-open evidence must originate from traversal protection.");
                }
            }
        }

        private static Dictionary<ActivityShellZoneKind, HashSet<LocalTileCoord>> BuildZones(
            ActivityShellCompileRequest request,
            IReadOnlyDictionary<LocalTileCoord, ClusterTraversalProtectedTile> protectedByCoordinate,
            ICollection<ProjectedActivityShellCell> projectedCells,
            IDictionary<string, ProjectedActivityShellCell> projectedByIdentity,
            ICollection<ActivityShellCompileError> errors)
        {
            var coordinates = new Dictionary<ActivityShellZoneKind, HashSet<LocalTileCoord>>();
            foreach (var required in RequiredZones)
            {
                var matches = request.Zones.Where(value => value != null && value.Kind == required).ToArray();
                if (matches.Length != 1 || matches[0].SourceCoordinates.Count == 0)
                {
                    Add(errors, ActivityShellCompileErrorCode.MissingZone, "zones[" + required + "]",
                        "Exactly one non-empty zone is required.");
                    coordinates[required] = new HashSet<LocalTileCoord>();
                    continue;
                }

                var definition = matches[0];
                var unique = new HashSet<LocalTileCoord>();
                coordinates[required] = unique;
                foreach (var sourceCoordinate in definition.SourceCoordinates)
                {
                    if (!unique.Add(sourceCoordinate))
                    {
                        Add(errors, ActivityShellCompileErrorCode.DuplicateZoneCoordinate,
                            "zones[" + required + "][" + Coordinate(sourceCoordinate) + "]",
                            "A coordinate occurs more than once in the same zone.");
                        continue;
                    }

                    ProjectedActivityShellCell projected;
                    if (!TryProjectZoneCell(request, required, sourceCoordinate,
                            protectedByCoordinate, out projected, errors))
                    {
                        continue;
                    }
                    projectedCells.Add(projected);
                    projectedByIdentity[ZoneIdentity(required, sourceCoordinate)] = projected;
                }
            }

            foreach (var invalid in request.Zones.Where(value => value == null || !IsDefinedZone(value.Kind)))
            {
                Add(errors, ActivityShellCompileErrorCode.InvalidZone, "zones",
                    invalid == null ? "Zone definition is required." : invalid.Kind.ToString());
            }
            foreach (var duplicate in request.Zones.Where(value => value != null && IsDefinedZone(value.Kind))
                         .GroupBy(value => value.Kind).Where(group => group.Count() > 1))
            {
                Add(errors, ActivityShellCompileErrorCode.InvalidZone, "zones[" + duplicate.Key + "]",
                    "Zone kind occurs more than once.");
            }
            return coordinates;
        }

        private static bool TryProjectZoneCell(
            ActivityShellCompileRequest request,
            ActivityShellZoneKind zone,
            LocalTileCoord sourceCoordinate,
            IReadOnlyDictionary<LocalTileCoord, ClusterTraversalProtectedTile> protectedByCoordinate,
            out ProjectedActivityShellCell projected,
            ICollection<ActivityShellCompileError> errors)
        {
            projected = null;
            LocalTileCoord compiledCoordinate;
            CompiledClusterLocalTileCell localCell;
            TerrainClusterPatternWorkingCell workingCell;
            TerrainClusterStaticShellCell staticShellCell;
            if (!request.LocalCanvas.TryGetCompiledTile(sourceCoordinate, out compiledCoordinate) ||
                !request.LocalCanvas.TryGetTileCell(compiledCoordinate, out localCell) ||
                localCell.State != ClusterChunkMaskState.Active)
            {
                Add(errors, ActivityShellCompileErrorCode.InvalidZone,
                    "zones[" + zone + "][" + Coordinate(sourceCoordinate) + "]",
                    "Zone coordinate must resolve to an active Local Canvas tile.");
                return false;
            }
            if (!request.PatternRenderReport.FinalWorkingCanvas.TryGetCell(compiledCoordinate, out workingCell) ||
                !request.RouteWitnessReport.StaticShell.TryGetCell(compiledCoordinate, out staticShellCell))
            {
                Add(errors, ActivityShellCompileErrorCode.WorkingCanvasMismatch,
                    "zones[" + zone + "][" + Coordinate(sourceCoordinate) + "]",
                    "Projected coordinate is absent from the final working Canvas or Static Shell.");
                return false;
            }

            ClusterTraversalProtectedTile protectedTile;
            var isProtected = protectedByCoordinate.TryGetValue(compiledCoordinate, out protectedTile);
            projected = new ProjectedActivityShellCell(
                zone,
                sourceCoordinate,
                compiledCoordinate,
                localCell.OwningChunk,
                Occupancy(workingCell.Solid),
                staticShellCell.Occupancy,
                isProtected,
                isProtected ? protectedTile.Provenance : null);
            return true;
        }

        private static List<ProjectedActivitySlot> BuildSlots(
            ActivityShellCompileRequest request,
            IReadOnlyDictionary<LocalTileCoord, ClusterTraversalProtectedTile> protectedByCoordinate,
            IReadOnlyDictionary<ActivityShellZoneKind, HashSet<LocalTileCoord>> zoneCoordinates,
            IReadOnlyDictionary<string, ProjectedActivityShellCell> projectedZoneByIdentity,
            ICollection<ActivityShellCompileError> errors)
        {
            var slots = request.Activity.Slots.Where(value => value != null)
                .ToDictionary(value => value.Id);
            var intents = new Dictionary<ActivitySlotId, ActivitySlotProjectionIntent>();
            foreach (var intent in request.SlotIntents)
            {
                if (intent == null)
                {
                    Add(errors, ActivityShellCompileErrorCode.MissingSlotIntent, "slotIntents",
                        "Slot intent is required.");
                    continue;
                }
                if (!slots.ContainsKey(intent.SlotId))
                {
                    Add(errors, ActivityShellCompileErrorCode.UnknownSlot,
                        "slotIntents[" + intent.SlotId.Value + "]", "Intent references an unknown Activity slot.");
                    continue;
                }
                if (intents.ContainsKey(intent.SlotId))
                {
                    Add(errors, ActivityShellCompileErrorCode.DuplicateSlotIntent,
                        "slotIntents[" + intent.SlotId.Value + "]", "Slot intent occurs more than once.");
                    continue;
                }
                intents.Add(intent.SlotId, intent);
            }

            var projected = new List<ProjectedActivitySlot>();
            foreach (var slot in request.Activity.Slots.Where(value => value != null).OrderBy(value => value.Id))
            {
                ActivitySlotProjectionIntent intent;
                if (!intents.TryGetValue(slot.Id, out intent))
                {
                    Add(errors, ActivityShellCompileErrorCode.MissingSlotIntent,
                        "slots[" + slot.Id.Value + "]", "Every Activity slot requires exactly one projection intent.");
                    continue;
                }

                ActivitySlotSemanticKind expectedSemantic;
                ActivityShellZoneKind requiredZone;
                if (!TryGetExpected(slot.Kind, out expectedSemantic, out requiredZone) ||
                    intent.Semantic != expectedSemantic)
                {
                    Add(errors, ActivityShellCompileErrorCode.SlotSemanticMismatch,
                        "slots[" + slot.Id.Value + "]", slot.Kind + "|" + intent.Semantic);
                    continue;
                }

                LocalTileCoord compiledCoordinate;
                CompiledClusterLocalTileCell localCell;
                TerrainClusterPatternWorkingCell workingCell;
                if (!request.LocalCanvas.TryGetCompiledTile(slot.Tile, out compiledCoordinate) ||
                    !request.LocalCanvas.TryGetTileCell(compiledCoordinate, out localCell) ||
                    localCell.State != ClusterChunkMaskState.Active ||
                    !request.PatternRenderReport.FinalWorkingCanvas.TryGetCell(compiledCoordinate, out workingCell))
                {
                    Add(errors, ActivityShellCompileErrorCode.SlotOutsideActiveCanvas,
                        "slots[" + slot.Id.Value + "]", Coordinate(slot.Tile));
                    continue;
                }

                HashSet<LocalTileCoord> requiredCoordinates;
                ProjectedActivityShellCell zoneCell;
                if (!zoneCoordinates.TryGetValue(requiredZone, out requiredCoordinates) ||
                    !requiredCoordinates.Contains(slot.Tile) ||
                    !projectedZoneByIdentity.TryGetValue(ZoneIdentity(requiredZone, slot.Tile), out zoneCell))
                {
                    Add(errors, ActivityShellCompileErrorCode.SlotOutsideRequiredZone,
                        "slots[" + slot.Id.Value + "]", requiredZone + "|" + Coordinate(slot.Tile));
                    continue;
                }

                ClusterTraversalProtectedTile protectedTile;
                var isProtected = protectedByCoordinate.TryGetValue(compiledCoordinate, out protectedTile);
                projected.Add(new ProjectedActivitySlot(
                    slot.Id,
                    slot.Kind,
                    intent.Semantic,
                    requiredZone,
                    slot.MarkerId,
                    slot.Tile,
                    compiledCoordinate,
                    localCell.OwningChunk,
                    Occupancy(workingCell.Solid),
                    isProtected,
                    isProtected ? protectedTile.Provenance : null));
            }
            return projected;
        }

        private static List<ActivityCueSlotBinding> BuildCueBindings(
            ActivityStructureContract activity,
            IReadOnlyDictionary<ActivitySlotId, ProjectedActivitySlot> projectedById,
            ICollection<ActivityShellCompileError> errors)
        {
            var bindings = new List<ActivityCueSlotBinding>();
            foreach (var cue in activity.Cues.Where(value => value != null))
            {
                ProjectedActivitySlot slot;
                if (!projectedById.TryGetValue(cue.SlotId, out slot) ||
                    slot.Semantic != ActivitySlotSemanticKind.CueMarker)
                {
                    Add(errors, ActivityShellCompileErrorCode.MissingGraphSlotBinding,
                        "cues[" + cue.Kind + "/" + cue.SlotId.Value + "]",
                        "Activity cue must bind to a projected CueMarker slot.");
                    continue;
                }
                bindings.Add(new ActivityCueSlotBinding(cue.Kind, cue.SlotId, slot));
            }
            return bindings;
        }

        private static List<ActivityMechanismSlotBinding> BuildMechanismBindings(
            ActivityStructureContract activity,
            IReadOnlyDictionary<ActivitySlotId, ProjectedActivitySlot> projectedById,
            ICollection<ActivityShellCompileError> errors)
        {
            var bindings = new List<ActivityMechanismSlotBinding>();
            if (activity.MechanismGraph == null) return bindings;
            foreach (var node in activity.MechanismGraph.Nodes.Where(value => value != null))
            {
                ProjectedActivitySlot slot;
                if (!projectedById.TryGetValue(node.SlotId, out slot) ||
                    !IsMechanismSemanticCompatible(node.Kind, slot.Semantic))
                {
                    Add(errors, ActivityShellCompileErrorCode.MissingGraphSlotBinding,
                        "mechanism[" + node.NodeId + "]", node.SlotId.Value);
                    continue;
                }
                bindings.Add(new ActivityMechanismSlotBinding(node.NodeId, node.Kind, node.SlotId, slot));
            }
            return bindings;
        }

        private static List<ActivityProgressionShellBinding> BuildProgressionBindings(
            ActivityStructureContract activity,
            TerrainClusterRouteWitnessReport routeWitness,
            IReadOnlyDictionary<ActivitySlotId, ProjectedActivitySlot> projectedById,
            ICollection<ActivityShellCompileError> errors)
        {
            var bindings = new List<ActivityProgressionShellBinding>();
            if (activity.ProgressionGraph == null) return bindings;
            foreach (var node in activity.ProgressionGraph.Nodes.Where(value => value != null))
            {
                ActivityShellZoneKind? zone = null;
                ActivitySlotId slotId = default(ActivitySlotId);
                var traversalNodeId = string.Empty;
                switch (node.Phase)
                {
                    case ProgressionPhaseKind.Cue:
                        zone = ActivityShellZoneKind.Cue;
                        break;
                    case ProgressionPhaseKind.Activation:
                        zone = ActivityShellZoneKind.Core;
                        slotId = FindSlot(projectedById, ActivitySlotSemanticKind.PressurePlateTrigger);
                        break;
                    case ProgressionPhaseKind.Core:
                        zone = ActivityShellZoneKind.Core;
                        break;
                    case ProgressionPhaseKind.Reward:
                        zone = ActivityShellZoneKind.Reward;
                        break;
                    case ProgressionPhaseKind.Recovery:
                        zone = ActivityShellZoneKind.Recovery;
                        break;
                    case ProgressionPhaseKind.Reset:
                        zone = ActivityShellZoneKind.Recovery;
                        slotId = FindSlot(projectedById, ActivitySlotSemanticKind.ResetAnchor);
                        break;
                    case ProgressionPhaseKind.Exit:
                        traversalNodeId = routeWitness.BaselineRoute == null
                            ? string.Empty
                            : routeWitness.BaselineRoute.ExitNodeId;
                        break;
                    default:
                        Add(errors, ActivityShellCompileErrorCode.MissingGraphSlotBinding,
                            "progression[" + node.NodeId + "]", node.Phase.ToString());
                        continue;
                }

                if ((node.Phase == ProgressionPhaseKind.Activation || node.Phase == ProgressionPhaseKind.Reset) &&
                    string.IsNullOrEmpty(slotId.Value))
                {
                    Add(errors, ActivityShellCompileErrorCode.MissingGraphSlotBinding,
                        "progression[" + node.NodeId + "]", "Required projected semantic slot is missing.");
                    continue;
                }
                if (node.Phase == ProgressionPhaseKind.Exit && string.IsNullOrEmpty(traversalNodeId))
                {
                    Add(errors, ActivityShellCompileErrorCode.MissingGraphSlotBinding,
                        "progression[" + node.NodeId + "]", "TerrainCluster Exit witness is missing.");
                    continue;
                }
                bindings.Add(new ActivityProgressionShellBinding(
                    node.NodeId, node.Phase, zone, slotId, traversalNodeId));
            }
            return bindings;
        }

        private static void ValidatePublication(
            ActivityShellCompileRequest request,
            IReadOnlyCollection<ProjectedActivityShellCell> zoneCells,
            IReadOnlyCollection<ProjectedActivitySlot> slots,
            IReadOnlyCollection<ActivityCueSlotBinding> cueBindings,
            IReadOnlyCollection<ActivityMechanismSlotBinding> mechanismBindings,
            IReadOnlyCollection<ActivityProgressionShellBinding> progressionBindings,
            ICollection<ActivityShellCompileError> errors)
        {
            if (zoneCells.Count != request.Zones.Where(value => value != null)
                    .Sum(value => value.SourceCoordinates.Count) ||
                slots.Count != request.Activity.Slots.Count ||
                cueBindings.Count != request.Activity.Cues.Count ||
                request.Activity.MechanismGraph == null ||
                mechanismBindings.Count != request.Activity.MechanismGraph.Nodes.Count ||
                request.Activity.ProgressionGraph == null ||
                progressionBindings.Count != request.Activity.ProgressionGraph.Nodes.Count)
            {
                Add(errors, ActivityShellCompileErrorCode.NonCanonicalPublication, "publication.counts",
                    Number(zoneCells.Count) + "|" + Number(slots.Count) + "|" + Number(cueBindings.Count) + "|" +
                    Number(mechanismBindings.Count) + "|" + Number(progressionBindings.Count));
            }
            if (slots.Select(value => value.SlotId).Distinct().Count() != slots.Count)
            {
                Add(errors, ActivityShellCompileErrorCode.NonCanonicalPublication, "publication.slots",
                    "Projected slot IDs must be unique.");
            }
        }

        private static string ComputeDigest(
            ActivityShellCompileRequest request,
            string activityDigest,
            string sourceDigest,
            IEnumerable<ProjectedActivityShellCell> zoneCells,
            IEnumerable<ProjectedActivitySlot> slots,
            IEnumerable<ActivityCueSlotBinding> cueBindings,
            IEnumerable<ActivityMechanismSlotBinding> mechanismBindings,
            IEnumerable<ActivityProgressionShellBinding> progressionBindings)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", RulesetVersion);
            Append(material, "IDENTITY", request.Activity.Id.Value, request.SourceContract.Id.Value,
                request.Activity.CompatibleSpineVariantId.Value);
            Append(material, "ACTIVITY", activityDigest);
            Append(material, "SOURCE", sourceDigest);
            Append(material, "LOCAL_CANVAS", request.LocalCanvas.CanonicalDigest);
            Append(material, "ROLE_SOCKET", request.RoleSocketContract.CanonicalDigest);
            Append(material, "TRAVERSAL", request.TraversalCompilation.CanonicalDigest);
            Append(material, "ROUTE_WITNESS", request.RouteWitnessReport.CanonicalDigest);
            Append(material, "PATTERN_RENDER", request.PatternRenderReport.CanonicalDigest);
            Append(material, "WORKING_CANVAS", request.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest);
            foreach (var zone in request.Zones.OrderBy(value => value.Kind))
            {
                Append(material, "ZONE", Number((int)zone.Kind),
                    string.Join(",", zone.SourceCoordinates.Select(Coordinate)));
            }
            foreach (var cell in zoneCells.OrderBy(value => value.ZoneKind)
                         .ThenBy(value => value.CompiledCoordinate.Y)
                         .ThenBy(value => value.CompiledCoordinate.X))
            {
                Append(material, "ZONE_CELL", Number((int)cell.ZoneKind), Coordinate(cell.SourceCoordinate),
                    Coordinate(cell.CompiledCoordinate), Chunk(cell.OwningCompiledChunk),
                    Number((int)cell.Occupancy), Number((int)cell.StaticShellOccupancy),
                    cell.IsAbsoluteProtected ? "1" : "0");
                foreach (var provenance in cell.ProtectedProvenance)
                {
                    Append(material, "PROTECTION", Number((int)provenance.SourceKind),
                        provenance.VariantId.Value, provenance.NodeId, provenance.EdgeId,
                        provenance.EnvelopeSetKind.HasValue ? Number((int)provenance.EnvelopeSetKind.Value) : string.Empty,
                        Coordinate(provenance.SourceCoordinate), Coordinate(provenance.CompiledCoordinate),
                        provenance.IsMandatory ? "1" : "0");
                }
            }
            foreach (var slot in slots.OrderBy(value => value.SlotId))
            {
                Append(material, "SLOT", slot.SlotId.Value, Number((int)slot.SlotKind),
                    Number((int)slot.Semantic), Number((int)slot.RequiredZone), slot.MarkerId,
                    Coordinate(slot.SourceCoordinate), Coordinate(slot.CompiledCoordinate),
                    Chunk(slot.OwningCompiledChunk), Number((int)slot.Occupancy),
                    slot.IsAbsoluteProtected ? "1" : "0");
            }
            foreach (var binding in cueBindings.OrderBy(value => value.CueKind).ThenBy(value => value.SlotId))
                Append(material, "CUE_BINDING", Number((int)binding.CueKind), binding.SlotId.Value);
            foreach (var binding in mechanismBindings.OrderBy(value => value.MechanismNodeId, StringComparer.Ordinal))
                Append(material, "MECHANISM_BINDING", binding.MechanismNodeId,
                    Number((int)binding.MechanismNodeKind), binding.SlotId.Value);
            foreach (var binding in progressionBindings.OrderBy(value => value.ProgressionNodeId, StringComparer.Ordinal))
                Append(material, "PROGRESSION_BINDING", binding.ProgressionNodeId,
                    Number((int)binding.Phase), binding.ZoneKind.HasValue ? Number((int)binding.ZoneKind.Value) : string.Empty,
                    binding.SlotId.Value, binding.TraversalNodeId);
            Append(material, "SIDE_EFFECTS", "0", "0", "0", "0");
            return Sha256(material.ToString());
        }

        private static bool TryGetExpected(
            ActivitySlotKind kind,
            out ActivitySlotSemanticKind semantic,
            out ActivityShellZoneKind zone)
        {
            switch (kind)
            {
                case ActivitySlotKind.Cue:
                    semantic = ActivitySlotSemanticKind.CueMarker;
                    zone = ActivityShellZoneKind.Cue;
                    return true;
                case ActivitySlotKind.Trigger:
                    semantic = ActivitySlotSemanticKind.PressurePlateTrigger;
                    zone = ActivityShellZoneKind.Core;
                    return true;
                case ActivitySlotKind.Device:
                    semantic = ActivitySlotSemanticKind.DeviceAnchor;
                    zone = ActivityShellZoneKind.Core;
                    return true;
                case ActivitySlotKind.Hazard:
                    semantic = ActivitySlotSemanticKind.ChaseOrHazardSpawn;
                    zone = ActivityShellZoneKind.Core;
                    return true;
                case ActivitySlotKind.Projectile:
                    semantic = ActivitySlotSemanticKind.ProjectileEmitter;
                    zone = ActivityShellZoneKind.Core;
                    return true;
                case ActivitySlotKind.Reward:
                    semantic = ActivitySlotSemanticKind.RewardAnchor;
                    zone = ActivityShellZoneKind.Reward;
                    return true;
                case ActivitySlotKind.Recovery:
                    semantic = ActivitySlotSemanticKind.RecoveryAnchor;
                    zone = ActivityShellZoneKind.Recovery;
                    return true;
                case ActivitySlotKind.Reset:
                    semantic = ActivitySlotSemanticKind.ResetAnchor;
                    zone = ActivityShellZoneKind.Recovery;
                    return true;
                case ActivitySlotKind.Npc:
                    semantic = ActivitySlotSemanticKind.NpcAnchor;
                    zone = ActivityShellZoneKind.Core;
                    return true;
                default:
                    semantic = default(ActivitySlotSemanticKind);
                    zone = default(ActivityShellZoneKind);
                    return false;
            }
        }

        private static bool IsMechanismSemanticCompatible(
            MechanismNodeKind nodeKind,
            ActivitySlotSemanticKind semantic)
        {
            switch (nodeKind)
            {
                case MechanismNodeKind.CueEmitter: return semantic == ActivitySlotSemanticKind.CueMarker;
                case MechanismNodeKind.Trigger: return semantic == ActivitySlotSemanticKind.PressurePlateTrigger;
                case MechanismNodeKind.Device: return semantic == ActivitySlotSemanticKind.DeviceAnchor;
                case MechanismNodeKind.Hazard: return semantic == ActivitySlotSemanticKind.ChaseOrHazardSpawn;
                case MechanismNodeKind.ProjectileEmitter: return semantic == ActivitySlotSemanticKind.ProjectileEmitter;
                case MechanismNodeKind.RewardEmitter: return semantic == ActivitySlotSemanticKind.RewardAnchor;
                case MechanismNodeKind.RecoveryController: return semantic == ActivitySlotSemanticKind.RecoveryAnchor;
                case MechanismNodeKind.ResetController: return semantic == ActivitySlotSemanticKind.ResetAnchor;
                default: return false;
            }
        }

        private static ActivitySlotId FindSlot(
            IReadOnlyDictionary<ActivitySlotId, ProjectedActivitySlot> slots,
            ActivitySlotSemanticKind semantic)
        {
            var match = slots.Values.FirstOrDefault(value => value.Semantic == semantic);
            return match == null ? default(ActivitySlotId) : match.SlotId;
        }

        private static bool IsDefinedZone(ActivityShellZoneKind value)
        {
            return value >= ActivityShellZoneKind.Cue && value <= ActivityShellZoneKind.Recovery;
        }

        private static TerrainClusterShellOccupancy Occupancy(bool solid)
        {
            return solid ? TerrainClusterShellOccupancy.Solid : TerrainClusterShellOccupancy.Air;
        }

        private static void ValidateDigest(
            ICollection<ActivityShellCompileError> errors,
            string path,
            string expected,
            string actual)
        {
            if (!string.Equals(expected ?? string.Empty, actual ?? string.Empty, StringComparison.Ordinal))
            {
                Add(errors, ActivityShellCompileErrorCode.ArtifactDigestMismatch,
                    path, (expected ?? string.Empty) + "!=" + (actual ?? string.Empty));
            }
        }

        private static ActivityShellCompileResult Failure(IEnumerable<ActivityShellCompileError> errors)
        {
            return new ActivityShellCompileResult(null, errors);
        }

        private static void Add(
            ICollection<ActivityShellCompileError> errors,
            ActivityShellCompileErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new ActivityShellCompileError(code, path, detail));
        }

        private static string ZoneIdentity(ActivityShellZoneKind zone, LocalTileCoord source)
        {
            return Number((int)zone) + "|" + Coordinate(source);
        }

        private static string Chunk(ClusterChunkCoord value) => Number(value.X) + "," + Number(value.Y);
        private static string Coordinate(LocalTileCoord value) => Number(value.X) + "," + Number(value.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

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
    }
}
