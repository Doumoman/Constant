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
    public sealed class ActivityRemovalSafetyCompileRequest
    {
        private readonly ReadOnlyCollection<ActivityCueObservationEvidence> cueEvidence;
        private readonly ReadOnlyCollection<ActivityCriticalTargetEvidence> criticalTargetEvidence;

        public ActivityRemovalSafetyCompileRequest(
            TerrainClusterContract sourceContract,
            ActivityStructureContract activity,
            ActivityShellCanvas activityShell,
            TerrainClusterLocalCanvas localCanvas,
            TerrainClusterRoleSocketContract roleSocketContract,
            TerrainClusterTraversalCompilation traversalCompilation,
            TerrainClusterRouteWitnessReport routeWitnessReport,
            TerrainClusterPatternRenderReport patternRenderReport,
            string expectedActivityShellDigest,
            IEnumerable<ActivityCueObservationEvidence> cueEvidence,
            ActivityOverlayRemovalIntent removalIntent,
            IEnumerable<ActivityCriticalTargetEvidence> criticalTargetEvidence)
        {
            SourceContract = sourceContract;
            Activity = activity;
            ActivityShell = activityShell;
            LocalCanvas = localCanvas;
            RoleSocketContract = roleSocketContract;
            TraversalCompilation = traversalCompilation;
            RouteWitnessReport = routeWitnessReport;
            PatternRenderReport = patternRenderReport;
            ExpectedActivityShellDigest = expectedActivityShellDigest ?? string.Empty;
            this.cueEvidence = new ReadOnlyCollection<ActivityCueObservationEvidence>(
                (cueEvidence ?? Array.Empty<ActivityCueObservationEvidence>())
                    .OrderBy(value => value == null ? string.Empty : value.CueId, StringComparer.Ordinal)
                    .ToArray());
            RemovalIntent = removalIntent;
            this.criticalTargetEvidence = new ReadOnlyCollection<ActivityCriticalTargetEvidence>(
                (criticalTargetEvidence ?? Array.Empty<ActivityCriticalTargetEvidence>())
                    .OrderBy(value => value == null ? 0 : (int)value.Kind)
                    .ThenBy(value => value == null ? string.Empty : value.TargetId, StringComparer.Ordinal)
                    .ToArray());
        }

        public TerrainClusterContract SourceContract { get; }
        public ActivityStructureContract Activity { get; }
        public ActivityShellCanvas ActivityShell { get; }
        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public TerrainClusterRoleSocketContract RoleSocketContract { get; }
        public TerrainClusterTraversalCompilation TraversalCompilation { get; }
        public TerrainClusterRouteWitnessReport RouteWitnessReport { get; }
        public TerrainClusterPatternRenderReport PatternRenderReport { get; }
        public string ExpectedActivityShellDigest { get; }
        public IReadOnlyList<ActivityCueObservationEvidence> CueEvidence => cueEvidence;
        public ActivityOverlayRemovalIntent RemovalIntent { get; }
        public IReadOnlyList<ActivityCriticalTargetEvidence> CriticalTargetEvidence => criticalTargetEvidence;
    }

    public enum ActivityRemovalSafetyCompileErrorCode
    {
        MissingInput = 1,
        IdentityMismatch = 2,
        ArtifactDigestMismatch = 3,
        MissingCueEvidence = 4,
        InvalidCueEvidence = 5,
        CueNotBeforeActivation = 6,
        CueOutOfRange = 7,
        CueOccluded = 8,
        InvalidObservationCoordinate = 9,
        InvalidActiveSnapshot = 10,
        ResidualOverlay = 11,
        StaticShellChanged = 12,
        TraversalChanged = 13,
        AccessChanged = 14,
        WorkingCanvasChanged = 15,
        InvalidSafePocket = 16,
        UnsafePocketOverlap = 17,
        InvalidRecoveryEvidence = 18,
        RecoveryDurationOutOfRange = 19,
        MissingCriticalTarget = 20,
        ExitDestructionDeclared = 21,
        RewardDestructionDeclared = 22,
        PermanentMutationDeclared = 23,
        NonCanonicalPublication = 24,
    }

    public sealed class ActivityRemovalSafetyCompileError :
        IEquatable<ActivityRemovalSafetyCompileError>,
        IComparable<ActivityRemovalSafetyCompileError>
    {
        public ActivityRemovalSafetyCompileError(
            ActivityRemovalSafetyCompileErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public ActivityRemovalSafetyCompileErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(ActivityRemovalSafetyCompileError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(ActivityRemovalSafetyCompileError other)
        {
            return other != null && Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as ActivityRemovalSafetyCompileError);

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

    public sealed class ActivityRemovalSafetyCompileResult
    {
        private readonly ReadOnlyCollection<ActivityRemovalSafetyCompileError> errors;

        internal ActivityRemovalSafetyCompileResult(
            ActivityRemovalSafetyProof proof,
            IEnumerable<ActivityRemovalSafetyCompileError> errors)
        {
            var copy = (errors ?? Array.Empty<ActivityRemovalSafetyCompileError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<ActivityRemovalSafetyCompileError>(copy);
            Proof = copy.Length == 0 ? proof : null;
        }

        public bool IsSuccess => Proof != null && errors.Count == 0;
        public ActivityRemovalSafetyProof Proof { get; }
        public ActivityOverlaySnapshot ActiveSnapshot => Proof == null ? null : Proof.ActiveSnapshot;
        public ActivityOverlaySnapshot RemovedSnapshot => Proof == null ? null : Proof.RemovedSnapshot;
        public IReadOnlyList<ActivityCueObservationProof> CueProofs =>
            Proof == null ? Array.Empty<ActivityCueObservationProof>() : Proof.CueProofs;
        public IReadOnlyList<ActivitySafePocketProof> SafePocketProofs =>
            Proof == null ? Array.Empty<ActivitySafePocketProof>() : Proof.SafePocketProofs;
        public IReadOnlyList<ActivityRecoverySafetyProof> RecoveryProofs =>
            Proof == null ? Array.Empty<ActivityRecoverySafetyProof>() : Proof.RecoveryProofs;
        public IReadOnlyList<ActivityCriticalPreservationProof> CriticalTargetProofs =>
            Proof == null ? Array.Empty<ActivityCriticalPreservationProof>() : Proof.CriticalTargetProofs;
        public IReadOnlyList<ActivityRemovalSafetyCompileError> Errors => errors;
        public string CanonicalDigest => Proof == null ? string.Empty : Proof.CanonicalDigest;
    }

    public static class ActivityRemovalSafetyCompiler
    {
        public const string RulesetVersion = "MAP12_02_ACTIVITY_REMOVAL_CUE_SAFETY_PROOF_V1";

        public static ActivityRemovalSafetyCompileResult Compile(
            ActivityRemovalSafetyCompileRequest request)
        {
            var errors = new List<ActivityRemovalSafetyCompileError>();
            if (!ValidateRequiredInputs(request, errors)) return Failure(errors);

            ValidateArtifactChain(request, errors);
            var staticShellDigest = ComputeStaticShellDigest(request.RouteWitnessReport.StaticShell);
            var overlayIdentities = BuildOverlayIdentities(request.ActivityShell, errors);
            ValidateRemovalIntent(request, overlayIdentities, staticShellDigest, errors);

            var cueProofs = BuildCueProofs(request, errors);
            var safePocketProofs = BuildSafePocketProofs(request, errors);
            var recoveryProofs = BuildRecoveryProofs(request, errors);
            var criticalProofs = BuildCriticalProofs(request, errors);
            ValidateSafetyContract(request.Activity.RemovalSafety, errors);

            if (cueProofs.Count != request.ActivityShell.CueBindings.Count ||
                safePocketProofs.Count != request.Activity.RemovalSafety.SafePocketTiles.Count ||
                recoveryProofs.Count != request.Activity.RemovalSafety.RecoveryTiles.Count ||
                criticalProofs.Count != 2)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.NonCanonicalPublication,
                    "publication.counts",
                    Number(cueProofs.Count) + "|" + Number(safePocketProofs.Count) + "|" +
                    Number(recoveryProofs.Count) + "|" + Number(criticalProofs.Count));
            }
            if (errors.Count != 0) return Failure(errors);

            var safety = request.Activity.RemovalSafety;
            var activeSnapshot = new ActivityOverlaySnapshot(
                ActivityOverlaySnapshotKind.Active,
                overlayIdentities,
                staticShellDigest,
                request.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest,
                request.TraversalCompilation.CanonicalDigest,
                request.RouteWitnessReport.CanonicalDigest,
                safety.RouteTypeBeforeRemoval,
                safety.AccessClassBeforeRemoval);
            var removedSnapshot = new ActivityOverlaySnapshot(
                ActivityOverlaySnapshotKind.Removed,
                Array.Empty<string>(),
                staticShellDigest,
                request.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest,
                request.TraversalCompilation.CanonicalDigest,
                request.RouteWitnessReport.CanonicalDigest,
                safety.RouteTypeAfterRemoval,
                safety.AccessClassAfterRemoval);
            var digest = ComputeDigest(
                request, activeSnapshot, removedSnapshot, cueProofs,
                safePocketProofs, recoveryProofs, criticalProofs);
            var proof = new ActivityRemovalSafetyProof(
                request.Activity.Id,
                request.SourceContract.Id,
                request.Activity.CompatibleSpineVariantId,
                request.ActivityShell.CanonicalDigest,
                activeSnapshot,
                removedSnapshot,
                cueProofs,
                safePocketProofs,
                recoveryProofs,
                criticalProofs,
                digest);
            return new ActivityRemovalSafetyCompileResult(proof, errors);
        }

        private static bool ValidateRequiredInputs(
            ActivityRemovalSafetyCompileRequest request,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            if (request == null)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "request", "Compile request is required.");
                return false;
            }
            if (request.SourceContract == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "sourceContract", "TerrainCluster contract is required.");
            if (request.Activity == null || request.Activity.RemovalSafety == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "activity", "Validated Activity contract and removal safety are required.");
            if (request.ActivityShell == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "activityShell", "Successful Activity shell is required.");
            if (request.LocalCanvas == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "localCanvas", "Local Canvas is required.");
            if (request.RoleSocketContract == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "roleSocket", "Role/socket contract is required.");
            if (request.TraversalCompilation == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "traversal", "Traversal compilation is required.");
            if (request.RouteWitnessReport == null || request.RouteWitnessReport.StaticShell == null ||
                request.RouteWitnessReport.BaselineRoute == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "routeWitness", "Route witness, baseline and Static Shell are required.");
            if (request.PatternRenderReport == null || request.PatternRenderReport.FinalWorkingCanvas == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "patternRender", "Final working Canvas is required.");
            if (request.RemovalIntent == null)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingInput,
                    "removalIntent", "Removal intent is required.");
            return errors.Count == 0;
        }

        private static void ValidateArtifactChain(
            ActivityRemovalSafetyCompileRequest request,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            var sourceValidation = TerrainClusterContractValidator.Validate(request.SourceContract);
            var activityValidation = ActivityContractValidator.Validate(request.Activity, request.SourceContract);
            if (!sourceValidation.IsValid || !activityValidation.IsValid)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.IdentityMismatch,
                    "validatedInput", "Source TerrainCluster and Activity must both validate.");
            }

            var clusterId = request.SourceContract.Id;
            var variantId = request.Activity.CompatibleSpineVariantId;
            if (request.Activity.TerrainClusterId != clusterId ||
                request.ActivityShell.ActivityId != request.Activity.Id ||
                request.ActivityShell.ClusterId != clusterId ||
                request.ActivityShell.VariantId != variantId ||
                request.LocalCanvas.ClusterId != clusterId ||
                request.RoleSocketContract.ClusterId != clusterId ||
                request.TraversalCompilation.ClusterId != clusterId ||
                request.RouteWitnessReport.ClusterId != clusterId ||
                request.PatternRenderReport.FinalWorkingCanvas.ClusterId != clusterId)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.IdentityMismatch,
                    "artifact.identity", request.Activity.Id.Value + "|" + clusterId.Value + "|" + variantId.Value);
            }

            if (!string.Equals(request.ExpectedActivityShellDigest,
                    request.ActivityShell.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.ActivityShell.ActivityDigest,
                    activityValidation.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.ActivityShell.SourceContractDigest,
                    sourceValidation.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.ActivityShell.LocalCanvasDigest,
                    request.LocalCanvas.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.ActivityShell.RoleSocketContractDigest,
                    request.RoleSocketContract.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.ActivityShell.TraversalCompilationDigest,
                    request.TraversalCompilation.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.ActivityShell.RouteWitnessDigest,
                    request.RouteWitnessReport.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.ActivityShell.PatternRenderDigest,
                    request.PatternRenderReport.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.ActivityShell.WorkingCanvasDigest,
                    request.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest, StringComparison.Ordinal))
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.ArtifactDigestMismatch,
                    "artifact.digestChain", "Activity shell and MAP11 artifact digests must match exactly.");
            }

            CompiledClusterSpineVariant baseline;
            if (!request.TraversalCompilation.TryGetVariant(variantId, out baseline) ||
                request.RouteWitnessReport.BaselineRoute.VariantId != variantId ||
                request.RouteWitnessReport.StaticShell.ActiveTileCount !=
                request.PatternRenderReport.FinalWorkingCanvas.CoordinateCount)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.IdentityMismatch,
                    "artifact.baseline", variantId.Value);
            }
        }

        private static IReadOnlyList<string> BuildOverlayIdentities(
            ActivityShellCanvas shell,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            var identities = new List<string>();
            identities.AddRange(shell.Zones.Select(value => "ZONE|" + Number((int)value.Kind)));
            identities.AddRange(shell.Slots.Select(value => "SLOT|" + value.SlotId.Value));
            identities.AddRange(shell.CueBindings.Select(value =>
                "CUE|" + Number((int)value.CueKind) + "|" + value.SlotId.Value));
            identities.AddRange(shell.MechanismBindings.Select(value =>
                "MECHANISM|" + value.MechanismNodeId + "|" + value.SlotId.Value));
            identities.AddRange(shell.ProgressionBindings.Select(value =>
                "PROGRESSION|" + value.ProgressionNodeId));
            identities.Sort(StringComparer.Ordinal);
            if (identities.Any(string.IsNullOrEmpty) || identities.Distinct(StringComparer.Ordinal).Count() != identities.Count)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidActiveSnapshot,
                    "activeSnapshot.identities", "Overlay identities must be non-empty and unique.");
            }
            return identities;
        }

        private static void ValidateRemovalIntent(
            ActivityRemovalSafetyCompileRequest request,
            IReadOnlyList<string> overlayIdentities,
            string staticShellDigest,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            var intent = request.RemovalIntent;
            if (intent.RemovedOverlayIdentities.Distinct(StringComparer.Ordinal).Count() !=
                intent.RemovedOverlayIdentities.Count ||
                !intent.RemovedOverlayIdentities.SequenceEqual(overlayIdentities, StringComparer.Ordinal))
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidActiveSnapshot,
                    "removalIntent.identities", "Removal must name the exact canonical active overlay set.");
            }
            if (intent.ResidualOverlayIdentities.Count != 0)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.ResidualOverlay,
                    "removalIntent.residual", string.Join(",", intent.ResidualOverlayIdentities));
            }
            if (intent.PermanentTileMutationDeclared)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.PermanentMutationDeclared,
                    "removalIntent.permanentTileMutation", "Permanent tile mutation is forbidden.");
            if (intent.MandatoryExitDestructionDeclared)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.ExitDestructionDeclared,
                    "removalIntent.exit", "Mandatory Exit destruction is forbidden.");
            if (intent.RewardDestructionDeclared)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.RewardDestructionDeclared,
                    "removalIntent.reward", "Reward destruction is forbidden.");

            ValidateOptionalDigest(intent.StaticShellDigestAfterRemovalDeclaration,
                staticShellDigest, ActivityRemovalSafetyCompileErrorCode.StaticShellChanged,
                "removalIntent.staticShell", errors);
            ValidateOptionalDigest(intent.WorkingCanvasDigestAfterRemovalDeclaration,
                request.PatternRenderReport.FinalWorkingCanvas.CanonicalDigest,
                ActivityRemovalSafetyCompileErrorCode.WorkingCanvasChanged,
                "removalIntent.workingCanvas", errors);
            ValidateOptionalDigest(intent.TraversalDigestAfterRemovalDeclaration,
                request.TraversalCompilation.CanonicalDigest,
                ActivityRemovalSafetyCompileErrorCode.TraversalChanged,
                "removalIntent.traversal", errors);
            ValidateOptionalDigest(intent.RouteWitnessDigestAfterRemovalDeclaration,
                request.RouteWitnessReport.CanonicalDigest,
                ActivityRemovalSafetyCompileErrorCode.TraversalChanged,
                "removalIntent.routeWitness", errors);

            var safety = request.Activity.RemovalSafety;
            if (intent.RouteTypeAfterRemovalDeclaration.HasValue &&
                intent.RouteTypeAfterRemovalDeclaration.Value != safety.RouteTypeBeforeRemoval)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.AccessChanged,
                    "removalIntent.routeType", Number(intent.RouteTypeAfterRemovalDeclaration.Value));
            }
            if (intent.AccessClassAfterRemovalDeclaration.HasValue &&
                intent.AccessClassAfterRemovalDeclaration.Value != safety.AccessClassBeforeRemoval)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.AccessChanged,
                    "removalIntent.accessClass", intent.AccessClassAfterRemovalDeclaration.Value.ToString());
            }
        }

        private static List<ActivityCueObservationProof> BuildCueProofs(
            ActivityRemovalSafetyCompileRequest request,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            var proofs = new List<ActivityCueObservationProof>();
            var evidenceByKey = new Dictionary<string, ActivityCueObservationEvidence>(StringComparer.Ordinal);
            foreach (var evidence in request.CueEvidence)
            {
                if (evidence == null || string.IsNullOrEmpty(evidence.CueId))
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidCueEvidence,
                        "cueEvidence", "Cue evidence and Cue ID are required.");
                    continue;
                }
                var key = CueKey(evidence.CueKind, evidence.SlotId);
                if (evidenceByKey.ContainsKey(key) ||
                    request.CueEvidence.Count(value => value != null &&
                        string.Equals(value.CueId, evidence.CueId, StringComparison.Ordinal)) != 1)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidCueEvidence,
                        "cueEvidence[" + evidence.CueId + "]", "Cue evidence identity must be unique.");
                    continue;
                }
                evidenceByKey.Add(key, evidence);
            }

            var baseline = request.RouteWitnessReport.BaselineRoute;
            CompiledClusterSpineVariant variant;
            request.TraversalCompilation.TryGetVariant(baseline.VariantId, out variant);
            foreach (var binding in request.ActivityShell.CueBindings)
            {
                var key = CueKey(binding.CueKind, binding.SlotId);
                ActivityCueObservationEvidence evidence;
                if (!evidenceByKey.TryGetValue(key, out evidence))
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingCueEvidence,
                        "cues[" + key + "]", "Every Activity cue requires one observation evidence item.");
                    continue;
                }

                var errorCount = errors.Count;
                var observationOrdinal = IndexOfEdge(baseline.OrderedEdges,
                    evidence.BaselineWitnessObservationEdgeId);
                var activationOrdinal = IndexOfEdge(baseline.OrderedEdges,
                    evidence.ActivationBoundaryEdgeId);
                if (observationOrdinal < 0 || activationOrdinal < 0 || variant == null)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidCueEvidence,
                        "cues[" + evidence.CueId + "].edges",
                        evidence.BaselineWitnessObservationEdgeId + "|" + evidence.ActivationBoundaryEdgeId);
                    continue;
                }
                if (observationOrdinal >= activationOrdinal)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.CueNotBeforeActivation,
                        "cues[" + evidence.CueId + "].ordinal",
                        Number(observationOrdinal) + ">=" + Number(activationOrdinal));
                }

                CompiledTraversalEdge sourceEdge;
                LocalTileCoord observationCompiled;
                if (!variant.TryGetEdge(evidence.BaselineWitnessObservationEdgeId, out sourceEdge) ||
                    !TryResolveObservationCoordinate(sourceEdge, evidence.ObservationSourceCoordinate,
                        out observationCompiled))
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidObservationCoordinate,
                        "cues[" + evidence.CueId + "].observation",
                        Coordinate(evidence.ObservationSourceCoordinate));
                    continue;
                }

                TerrainClusterPatternWorkingCell observationCell;
                if (!request.PatternRenderReport.FinalWorkingCanvas.TryGetCell(
                        observationCompiled, out observationCell) || observationCell.Solid)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidObservationCoordinate,
                        "cues[" + evidence.CueId + "].observationAir", Coordinate(observationCompiled));
                }

                var cueCoordinate = binding.ProjectedSlot.CompiledCoordinate;
                var distance = Manhattan(observationCompiled, cueCoordinate);
                if (evidence.MaximumObservationDistanceTiles <= 0 ||
                    distance > evidence.MaximumObservationDistanceTiles)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.CueOutOfRange,
                        "cues[" + evidence.CueId + "].distance",
                        Number(distance) + ">" + Number(evidence.MaximumObservationDistanceTiles));
                }

                var distanceOnly = evidence.CueKind == ActivityCueKind.Audio ||
                                   evidence.CueKind == ActivityCueKind.Environment;
                var supercover = distanceOnly
                    ? Array.Empty<LocalTileCoord>()
                    : GridSupercover(observationCompiled, cueCoordinate).ToArray();
                if (!distanceOnly)
                {
                    var occluded = supercover.Where(value =>
                    {
                        TerrainClusterPatternWorkingCell cell;
                        return !request.PatternRenderReport.FinalWorkingCanvas.TryGetCell(value, out cell) || cell.Solid;
                    }).ToArray();
                    if (occluded.Length != 0)
                    {
                        Add(errors, ActivityRemovalSafetyCompileErrorCode.CueOccluded,
                            "cues[" + evidence.CueId + "].supercover",
                            string.Join(",", occluded.Select(Coordinate)));
                    }
                }

                if (errors.Count == errorCount)
                {
                    proofs.Add(new ActivityCueObservationProof(
                        evidence, observationCompiled, cueCoordinate,
                        observationOrdinal, activationOrdinal, distance,
                        distanceOnly, supercover));
                }
            }
            foreach (var extra in evidenceByKey.Keys.Where(key =>
                         request.ActivityShell.CueBindings.All(value =>
                             !string.Equals(CueKey(value.CueKind, value.SlotId), key, StringComparison.Ordinal))))
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidCueEvidence,
                    "cueEvidence.extra[" + extra + "]", "Evidence does not match an Activity shell cue binding.");
            }
            return proofs;
        }

        private static List<ActivitySafePocketProof> BuildSafePocketProofs(
            ActivityRemovalSafetyCompileRequest request,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            var proofs = new List<ActivitySafePocketProof>();
            var coordinates = request.Activity.RemovalSafety.SafePocketTiles;
            if (coordinates.Count == 0 || coordinates.Distinct().Count() != coordinates.Count)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidSafePocket,
                    "safePocket", "SafePocket coordinates must be non-empty and unique.");
            }
            var unsafeCoordinates = request.ActivityShell.Slots
                .Where(value => value.Semantic == ActivitySlotSemanticKind.DeviceAnchor ||
                                value.Semantic == ActivitySlotSemanticKind.ProjectileEmitter ||
                                value.Semantic == ActivitySlotSemanticKind.ChaseOrHazardSpawn)
                .Select(value => value.SourceCoordinate).ToHashSet();
            foreach (var source in coordinates)
            {
                var errorCount = errors.Count;
                LocalTileCoord compiled;
                TerrainClusterPatternWorkingCell cell;
                if (!request.LocalCanvas.TryGetCompiledTile(source, out compiled) ||
                    !request.PatternRenderReport.FinalWorkingCanvas.TryGetCell(compiled, out cell) || cell.Solid)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidSafePocket,
                        "safePocket[" + Coordinate(source) + "]", "SafePocket must remain active Air.");
                    continue;
                }
                if (unsafeCoordinates.Contains(source))
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.UnsafePocketOverlap,
                        "safePocket[" + Coordinate(source) + "]", "SafePocket overlaps a Core device/hazard/projectile slot.");
                }

                string witnessKind;
                string witnessId;
                if (!TryFindOpenWitness(request.RouteWitnessReport, compiled, out witnessKind, out witnessId))
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidSafePocket,
                        "safePocket[" + Coordinate(source) + "].witness",
                        "SafePocket must connect to published baseline or recovery open evidence.");
                }
                if (errors.Count == errorCount)
                    proofs.Add(new ActivitySafePocketProof(source, compiled, witnessKind, witnessId));
            }
            return proofs;
        }

        private static List<ActivityRecoverySafetyProof> BuildRecoveryProofs(
            ActivityRemovalSafetyCompileRequest request,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            var proofs = new List<ActivityRecoverySafetyProof>();
            var coordinates = request.Activity.RemovalSafety.RecoveryTiles;
            if (coordinates.Count == 0 || coordinates.Distinct().Count() != coordinates.Count)
            {
                Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidRecoveryEvidence,
                    "recovery", "Recovery coordinates must be non-empty and unique.");
            }
            foreach (var source in coordinates)
            {
                var errorCount = errors.Count;
                LocalTileCoord compiled;
                TerrainClusterPatternWorkingCell cell;
                if (!request.LocalCanvas.TryGetCompiledTile(source, out compiled) ||
                    !request.PatternRenderReport.FinalWorkingCanvas.TryGetCell(compiled, out cell) || cell.Solid)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidRecoveryEvidence,
                        "recovery[" + Coordinate(source) + "]", "Recovery coordinate must remain active Air.");
                    continue;
                }
                var witness = request.RouteWitnessReport.RecoveryRoutes.FirstOrDefault(value =>
                    value.CompiledCoordinates.Contains(compiled) || value.CoveredProtectedTiles.Contains(compiled));
                if (witness == null || !UsesSourceEdgesOnly(request.TraversalCompilation, witness))
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.InvalidRecoveryEvidence,
                        "recovery[" + Coordinate(source) + "].witness",
                        "Recovery must be explained by source-authored recovery witness edges.");
                    continue;
                }
                if (witness.TotalEstimatedDurationMilliseconds < 2000 ||
                    witness.TotalEstimatedDurationMilliseconds > 5000)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.RecoveryDurationOutOfRange,
                        "recovery[" + Coordinate(source) + "].duration",
                        Number(witness.TotalEstimatedDurationMilliseconds));
                }
                if (errors.Count == errorCount)
                    proofs.Add(new ActivityRecoverySafetyProof(source, compiled, witness));
            }
            return proofs;
        }

        private static List<ActivityCriticalPreservationProof> BuildCriticalProofs(
            ActivityRemovalSafetyCompileRequest request,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            var proofs = new List<ActivityCriticalPreservationProof>();
            foreach (var kind in new[]
                     {
                         ActivityCriticalTargetKind.MandatoryExit,
                         ActivityCriticalTargetKind.Reward,
                     })
            {
                var matches = request.CriticalTargetEvidence
                    .Where(value => value != null && value.Kind == kind).ToArray();
                if (matches.Length != 1)
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingCriticalTarget,
                        "critical[" + kind + "]", "Exactly one critical-target evidence item is required.");
                    continue;
                }
                var evidence = matches[0];
                LocalTileCoord expectedSource;
                string expectedTarget;
                string expectedBinding;
                string expectedNode;
                if (kind == ActivityCriticalTargetKind.MandatoryExit)
                {
                    ProjectedClusterPort exit;
                    if (!request.RoleSocketContract.TryGetPrimaryPort(ClusterPortKind.Exit, out exit))
                    {
                        Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingCriticalTarget,
                            "critical[MandatoryExit].port", "Primary Exit port is required.");
                        continue;
                    }
                    expectedSource = exit.SourceCoordinate;
                    expectedTarget = exit.PortId;
                    expectedBinding = exit.RoleAnchorId;
                    expectedNode = request.RouteWitnessReport.BaselineRoute.ExitNodeId;
                }
                else
                {
                    var rewards = request.ActivityShell.Slots
                        .Where(value => value.Semantic == ActivitySlotSemanticKind.RewardAnchor).ToArray();
                    var bindings = request.ActivityShell.ProgressionBindings
                        .Where(value => value.Phase == ProgressionPhaseKind.Reward).ToArray();
                    if (rewards.Length != 1 || bindings.Length != 1)
                    {
                        Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingCriticalTarget,
                            "critical[Reward].binding", "One Reward slot and progression binding are required.");
                        continue;
                    }
                    expectedSource = rewards[0].SourceCoordinate;
                    expectedTarget = rewards[0].SlotId.Value;
                    expectedBinding = bindings[0].ProgressionNodeId;
                    expectedNode = string.Empty;
                }

                if (!string.Equals(evidence.TargetId, expectedTarget, StringComparison.Ordinal) ||
                    evidence.SourceCoordinate != expectedSource ||
                    !string.Equals(evidence.RoleOrBindingId, expectedBinding, StringComparison.Ordinal) ||
                    !string.Equals(evidence.TraversalNodeId, expectedNode, StringComparison.Ordinal))
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingCriticalTarget,
                        "critical[" + kind + "].identity",
                        evidence.TargetId + "|" + Coordinate(evidence.SourceCoordinate) + "|" +
                        evidence.RoleOrBindingId + "|" + evidence.TraversalNodeId);
                    continue;
                }

                LocalTileCoord compiled;
                TerrainClusterPatternWorkingCell cell;
                if (!request.LocalCanvas.TryGetCompiledTile(expectedSource, out compiled) ||
                    !request.PatternRenderReport.FinalWorkingCanvas.TryGetCell(compiled, out cell))
                {
                    Add(errors, ActivityRemovalSafetyCompileErrorCode.MissingCriticalTarget,
                        "critical[" + kind + "].coordinate", Coordinate(expectedSource));
                    continue;
                }
                proofs.Add(new ActivityCriticalPreservationProof(
                    evidence, compiled, ComputeWorkingCellIdentity(cell)));
            }
            return proofs;
        }

        private static void ValidateSafetyContract(
            ActivityRemovalSafety safety,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            if (safety.PermanentSolidMutationAllowed || safety.PermanentSolidWriteTiles.Count != 0)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.PermanentMutationDeclared,
                    "activity.removalSafety.permanentSolid", "Permanent Solid mutation/write must be forbidden.");
            if (safety.MandatoryExitDestructionAllowed)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.ExitDestructionDeclared,
                    "activity.removalSafety.exit", "Mandatory Exit destruction must be forbidden.");
            if (!safety.PreserveStaticTraversal ||
                !string.Equals(safety.TraversalDigestBeforeRemoval,
                    safety.TraversalDigestAfterRemoval, StringComparison.Ordinal))
                Add(errors, ActivityRemovalSafetyCompileErrorCode.TraversalChanged,
                    "activity.removalSafety.traversal", "Static traversal identity must be preserved.");
            if (!safety.PreserveAccessClass || safety.RouteTypeBeforeRemoval != safety.RouteTypeAfterRemoval ||
                safety.AccessClassBeforeRemoval != safety.AccessClassAfterRemoval)
                Add(errors, ActivityRemovalSafetyCompileErrorCode.AccessChanged,
                    "activity.removalSafety.access", "Route type and access class must be preserved.");
        }

        private static bool TryResolveObservationCoordinate(
            CompiledTraversalEdge edge,
            LocalTileCoord source,
            out LocalTileCoord compiled)
        {
            foreach (var tile in edge.Envelope.Centerline.Concat(edge.Envelope.Clearance)
                         .Concat(edge.Envelope.Landing))
            {
                if (tile.SourceCoordinate == source)
                {
                    compiled = tile.CompiledCoordinate;
                    return true;
                }
            }
            compiled = default(LocalTileCoord);
            return false;
        }

        private static bool TryFindOpenWitness(
            TerrainClusterRouteWitnessReport report,
            LocalTileCoord compiled,
            out string witnessKind,
            out string witnessId)
        {
            if (report.BaselineRoute.CompiledCoordinates.Contains(compiled) ||
                report.BaselineRoute.CoveredProtectedTiles.Contains(compiled))
            {
                var edge = report.BaselineRoute.OrderedEdges.FirstOrDefault(value =>
                    value.CompiledStartCoordinate == compiled || value.CompiledEndCoordinate == compiled);
                witnessKind = "BaselineRoute";
                witnessId = report.BaselineRoute.VariantId.Value + "/" +
                            (edge == null ? "PATH" : edge.EdgeId);
                return true;
            }
            var recovery = report.RecoveryRoutes.FirstOrDefault(value =>
                value.CompiledCoordinates.Contains(compiled) || value.CoveredProtectedTiles.Contains(compiled));
            if (recovery != null)
            {
                witnessKind = "RecoveryRoute";
                witnessId = recovery.HighRouteId + "/" + recovery.FailureNodeId;
                return true;
            }
            witnessKind = string.Empty;
            witnessId = string.Empty;
            return false;
        }

        private static bool UsesSourceEdgesOnly(
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRecoveryRouteWitness witness)
        {
            if (witness.OrderedEdges.Count == 0) return false;
            foreach (var edge in witness.OrderedEdges)
            {
                CompiledClusterSpineVariant variant;
                CompiledTraversalEdge source;
                if (!traversal.TryGetVariant(edge.VariantId, out variant) ||
                    !variant.TryGetEdge(edge.EdgeId, out source) ||
                    !string.Equals(source.FromNodeId, edge.FromNodeId, StringComparison.Ordinal) ||
                    !string.Equals(source.ToNodeId, edge.ToNodeId, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static IEnumerable<LocalTileCoord> GridSupercover(
            LocalTileCoord start,
            LocalTileCoord end)
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

        private static int IndexOfEdge(
            IReadOnlyList<TerrainClusterRouteWitnessEdge> edges,
            string edgeId)
        {
            for (var index = 0; index < edges.Count; index++)
            {
                if (string.Equals(edges[index].EdgeId, edgeId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private static int Manhattan(LocalTileCoord left, LocalTileCoord right)
        {
            return Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
        }

        private static string ComputeStaticShellDigest(TerrainClusterStaticShell shell)
        {
            var material = new StringBuilder();
            Append(material, "STATIC_SHELL", shell.ClusterId.Value,
                shell.LocalCanvasDigest, shell.TraversalCompilationDigest);
            foreach (var cell in shell.Cells)
            {
                Append(material, "CELL", Coordinate(cell.CompiledCoordinate),
                    Number((int)cell.Occupancy), cell.IsProtectedOpen ? "1" : "0");
                foreach (var provenance in cell.Provenance)
                {
                    Append(material, "PROVENANCE", provenance.VariantId.Value,
                        provenance.EdgeId, Number((int)provenance.EnvelopeSetKind),
                        Coordinate(provenance.SourceCoordinate), Coordinate(provenance.CompiledCoordinate));
                }
            }
            return Sha256(material.ToString());
        }

        private static string ComputeWorkingCellIdentity(TerrainClusterPatternWorkingCell cell)
        {
            var material = new StringBuilder();
            Append(material, "WORKING_CELL", Coordinate(cell.Coordinate), cell.Solid ? "1" : "0",
                cell.SurfaceId, cell.AffordanceId, cell.MaterialId, cell.HazardId, cell.MarkerId,
                Number((int)cell.StaticShellCell.Occupancy));
            return Sha256(material.ToString());
        }

        private static string ComputeDigest(
            ActivityRemovalSafetyCompileRequest request,
            ActivityOverlaySnapshot active,
            ActivityOverlaySnapshot removed,
            IEnumerable<ActivityCueObservationProof> cues,
            IEnumerable<ActivitySafePocketProof> safePockets,
            IEnumerable<ActivityRecoverySafetyProof> recoveries,
            IEnumerable<ActivityCriticalPreservationProof> critical)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", RulesetVersion);
            Append(material, "IDENTITY", request.Activity.Id.Value,
                request.SourceContract.Id.Value, request.Activity.CompatibleSpineVariantId.Value);
            Append(material, "ACTIVITY_SHELL", request.ActivityShell.CanonicalDigest);
            Append(material, "STATIC_SHELL", active.StaticShellDigest, removed.StaticShellDigest);
            Append(material, "WORKING_CANVAS", active.WorkingCanvasDigest, removed.WorkingCanvasDigest);
            Append(material, "TRAVERSAL", active.TraversalDigest, removed.TraversalDigest);
            Append(material, "ROUTE", active.RouteWitnessDigest, removed.RouteWitnessDigest);
            Append(material, "ACCESS", Number(active.RouteType), Number((int)active.AccessClass),
                Number(removed.RouteType), Number((int)removed.AccessClass));
            foreach (var identity in active.OverlayIdentities)
                Append(material, "ACTIVE_OVERLAY", identity);
            Append(material, "REMOVED_OVERLAY_COUNT", Number(removed.OverlayIdentities.Count));
            foreach (var cue in cues.OrderBy(value => value.CueId, StringComparer.Ordinal))
            {
                Append(material, "CUE", cue.CueId, Number((int)cue.CueKind), cue.SlotId.Value,
                    cue.BaselineWitnessObservationEdgeId, cue.ActivationBoundaryEdgeId,
                    Coordinate(cue.ObservationSourceCoordinate), Coordinate(cue.ObservationCompiledCoordinate),
                    Coordinate(cue.CueCompiledCoordinate), Number(cue.MaximumObservationDistanceTiles),
                    Number(cue.DistanceTiles), cue.UsesDistanceOnly ? "1" : "0",
                    string.Join(",", cue.SupercoverCoordinates.Select(Coordinate)));
            }
            foreach (var pocket in safePockets.OrderBy(value => value.SourceCoordinate.Y)
                         .ThenBy(value => value.SourceCoordinate.X))
                Append(material, "SAFE_POCKET", Coordinate(pocket.SourceCoordinate),
                    Coordinate(pocket.CompiledCoordinate), pocket.WitnessKind, pocket.WitnessId);
            foreach (var recovery in recoveries.OrderBy(value => value.SourceCoordinate.Y)
                         .ThenBy(value => value.SourceCoordinate.X))
                Append(material, "RECOVERY", Coordinate(recovery.SourceCoordinate),
                    Coordinate(recovery.CompiledCoordinate), recovery.HighRouteId, recovery.FailureNodeId,
                    recovery.TargetBaselineNodeId, Number(recovery.EstimatedDurationMilliseconds),
                    string.Join(",", recovery.SourceEdgeIds));
            foreach (var target in critical.OrderBy(value => value.Kind))
                Append(material, "CRITICAL", Number((int)target.Kind), target.TargetId,
                    Coordinate(target.SourceCoordinate), Coordinate(target.CompiledCoordinate),
                    target.RoleOrBindingId, target.TraversalNodeId,
                    target.UnderlyingIdentityDigestBeforeRemoval,
                    target.UnderlyingIdentityDigestAfterRemoval);
            Append(material, "SIDE_EFFECTS", "0", "0", "0", "0");
            return Sha256(material.ToString());
        }

        private static void ValidateOptionalDigest(
            string declaration,
            string actual,
            ActivityRemovalSafetyCompileErrorCode code,
            string path,
            ICollection<ActivityRemovalSafetyCompileError> errors)
        {
            if (!string.IsNullOrEmpty(declaration) &&
                !string.Equals(declaration, actual, StringComparison.Ordinal))
                Add(errors, code, path, declaration + "!=" + actual);
        }

        private static ActivityRemovalSafetyCompileResult Failure(
            IEnumerable<ActivityRemovalSafetyCompileError> errors)
        {
            return new ActivityRemovalSafetyCompileResult(null, errors);
        }

        private static void Add(
            ICollection<ActivityRemovalSafetyCompileError> errors,
            ActivityRemovalSafetyCompileErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new ActivityRemovalSafetyCompileError(code, path, detail));
        }

        private static string CueKey(ActivityCueKind kind, ActivitySlotId slotId)
        {
            return Number((int)kind) + "|" + slotId.Value;
        }

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
