using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public static class TerrainClusterRouteWitnessCompiler
    {
        private const int MinimumRecoveryMilliseconds = 2000;
        private const int MaximumRecoveryMilliseconds = 5000;
        private static readonly Regex HighRouteIdPattern =
            new Regex("^HIGH_ROUTE_[A-Z0-9_]+$", RegexOptions.CultureInvariant);
        private static readonly Regex BenefitIdPattern =
            new Regex("^BENEFIT_[A-Z0-9_]+$", RegexOptions.CultureInvariant);

        public static TerrainClusterRouteWitnessCompileResult Compile(
            TerrainClusterRouteWitnessCompileRequest request)
        {
            var errors = new List<TerrainClusterRouteWitnessCompileError>();
            if (request == null)
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.MissingInput,
                    "request", "Request is required.");
                return Failure(errors);
            }

            if (request.LocalCanvas == null) Add(errors,
                TerrainClusterRouteWitnessCompileErrorCode.MissingInput, "request.localCanvas", "Local Canvas is required.");
            if (request.RoleSocketContract == null) Add(errors,
                TerrainClusterRouteWitnessCompileErrorCode.MissingInput, "request.roleSocketContract", "Role/socket contract is required.");
            if (request.TraversalCompilation == null) Add(errors,
                TerrainClusterRouteWitnessCompileErrorCode.MissingInput, "request.traversalCompilation", "Traversal compilation is required.");
            if (request.Intent == null) Add(errors,
                TerrainClusterRouteWitnessCompileErrorCode.MissingInput, "request.intent", "Route witness intent is required.");
            if (errors.Count != 0) return Failure(errors);

            ValidateArtifacts(request, errors);
            var shell = BuildShell(request.LocalCanvas, request.TraversalCompilation, errors);
            var durations = ValidateDurations(request.TraversalCompilation, request.Intent, errors);

            CompiledClusterSpineVariant baselineVariant;
            var baseline = CompileBaseline(request, shell, durations, errors, out baselineVariant);
            var highRoutes = new List<TerrainClusterHighRouteWitness>();
            var recoveryRoutes = new List<TerrainClusterRecoveryRouteWitness>();
            if (baseline != null && baselineVariant != null)
            {
                CompileHighAndRecovery(request, shell, durations, baselineVariant, baseline,
                    highRoutes, recoveryRoutes, errors);
            }

            if (errors.Count != 0) return Failure(errors);
            if (shell == null || baseline == null || highRoutes.Count == 0)
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.NonCanonicalPublication,
                    "publication", "Complete shell, baseline, high, and recovery publication is required.");
                return Failure(errors);
            }

            var ruleset = request.Intent.EdgeDurationEvidence[0].RulesetId;
            var digest = ComputeDigest(request, shell, baseline, highRoutes, recoveryRoutes, ruleset);
            var report = new TerrainClusterRouteWitnessReport(
                request.LocalCanvas.ClusterId,
                ruleset,
                request.TraversalCompilation.CanonicalDigest,
                shell,
                baseline,
                highRoutes,
                recoveryRoutes,
                digest);
            return new TerrainClusterRouteWitnessCompileResult(report, errors);
        }

        private static void ValidateArtifacts(
            TerrainClusterRouteWitnessCompileRequest request,
            ICollection<TerrainClusterRouteWitnessCompileError> errors)
        {
            var canvas = request.LocalCanvas;
            var role = request.RoleSocketContract;
            var traversal = request.TraversalCompilation;
            if (canvas.ClusterId != role.ClusterId || canvas.ClusterId != traversal.ClusterId ||
                canvas.Transform != role.Transform || canvas.Transform != traversal.Transform ||
                !string.Equals(canvas.CanonicalDigest, role.LocalCanvasDigest, StringComparison.Ordinal) ||
                !string.Equals(canvas.CanonicalDigest, traversal.LocalCanvasDigest, StringComparison.Ordinal) ||
                !string.Equals(role.SourceContractDigest, traversal.SourceContractDigest, StringComparison.Ordinal) ||
                !string.Equals(role.CanonicalDigest, traversal.RoleSocketContractDigest, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.ArtifactIdentityMismatch,
                    "request.artifacts", "MAP11_01 through MAP11_03 artifacts do not share one identity chain.");
            }

            if (!string.Equals(request.LocalCanvasCanonicalDigest, canvas.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.RoleSocketContractCanonicalDigest, role.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.TraversalCompilationCanonicalDigest, traversal.CanonicalDigest, StringComparison.Ordinal))
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.ArtifactDigestMismatch,
                    "request.artifactDigests", "Provided artifact digest does not match the supplied artifact.");
            }
        }

        private static TerrainClusterStaticShell BuildShell(
            TerrainClusterLocalCanvas canvas,
            TerrainClusterTraversalCompilation traversal,
            ICollection<TerrainClusterRouteWitnessCompileError> errors)
        {
            var builders = new Dictionary<LocalTileCoord, ShellCellBuilder>();
            foreach (var tile in canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active))
            {
                if (builders.ContainsKey(tile.Coordinate))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.ShellCoverageMismatch,
                        CoordinatePath(tile.Coordinate), "Active tile was published more than once.");
                    continue;
                }
                builders.Add(tile.Coordinate, new ShellCellBuilder(tile.OwningChunk));
            }

            foreach (var variant in traversal.Variants)
            foreach (var edge in variant.Edges)
            foreach (var tile in edge.Envelope.AllTiles)
            {
                ShellCellBuilder builder;
                if (!builders.TryGetValue(tile.CompiledCoordinate, out builder))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.ShellCoverageMismatch,
                        variant.VariantId.Value + "/" + edge.EdgeId + "/" + CoordinatePath(tile.CompiledCoordinate),
                        "Traversal requirement is outside the active Local Canvas.");
                    continue;
                }

                var solid = tile.SetKind == CompiledTraversalEnvelopeSetKind.Floor;
                builder.RequiresSolid |= solid;
                builder.RequiresAir |= !solid;
                builder.Provenance.Add(new TerrainClusterStaticShellProvenance(
                    variant.VariantId, edge.EdgeId, tile.SetKind,
                    tile.SourceCoordinate, tile.CompiledCoordinate));
            }

            foreach (var pair in builders.Where(value => value.Value.RequiresAir && value.Value.RequiresSolid))
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.StaticShellConflict,
                    CoordinatePath(pair.Key), "The same active tile is required as both Solid and Air.");
            }

            var cells = builders.Select(pair => new TerrainClusterStaticShellCell(
                pair.Key,
                pair.Value.OwningChunk,
                pair.Value.RequiresSolid ? TerrainClusterShellOccupancy.Solid : TerrainClusterShellOccupancy.Air,
                pair.Value.RequiresAir,
                pair.Value.Provenance)).ToArray();
            if (cells.Length != builders.Count || cells.Select(value => value.CompiledCoordinate).Distinct().Count() != builders.Count)
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.ShellCoverageMismatch,
                    "shell.cells", "Static shell must publish every active tile exactly once.");
            }
            return new TerrainClusterStaticShell(
                canvas.ClusterId, canvas.CanonicalDigest, traversal.CanonicalDigest, cells);
        }

        private static Dictionary<string, TraversalEdgeDurationEvidence> ValidateDurations(
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessIntent intent,
            ICollection<TerrainClusterRouteWitnessCompileError> errors)
        {
            var actualEdges = traversal.Edges.ToDictionary(
                value => EdgeKey(value.VariantId, value.EdgeId), StringComparer.Ordinal);
            var durations = new Dictionary<string, TraversalEdgeDurationEvidence>(StringComparer.Ordinal);
            var rulesets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var evidence in intent.EdgeDurationEvidence)
            {
                if (evidence == null)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidDurationEvidence,
                        "intent.edgeDurationEvidence", "Null duration evidence is not allowed.");
                    continue;
                }
                var key = EdgeKey(evidence.VariantId, evidence.EdgeId);
                if (evidence.EstimatedDurationMilliseconds <= 0 || string.IsNullOrEmpty(evidence.EdgeId) ||
                    string.IsNullOrEmpty(evidence.VariantId.Value) || string.IsNullOrEmpty(evidence.RulesetId))
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidDurationEvidence,
                        "intent.edgeDurationEvidence/" + key, "Duration must be integer > 0 with variant, edge, and ruleset provenance.");
                if (!actualEdges.ContainsKey(key))
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidDurationEvidence,
                        "intent.edgeDurationEvidence/" + key, "Duration evidence references an unknown compiled edge.");
                if (durations.ContainsKey(key))
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidDurationEvidence,
                        "intent.edgeDurationEvidence/" + key, "Duration evidence identity is duplicated.");
                else durations.Add(key, evidence);
                rulesets.Add(evidence.RulesetId);
            }
            foreach (var key in actualEdges.Keys.Where(value => !durations.ContainsKey(value)).OrderBy(value => value, StringComparer.Ordinal))
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidDurationEvidence,
                    "intent.edgeDurationEvidence/" + key, "Compiled edge has no duration evidence.");
            if (rulesets.Count != 1)
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidDurationEvidence,
                    "intent.edgeDurationEvidence.rulesetId", "Exactly one stable timing ruleset is required.");
            return durations;
        }

        private static TerrainClusterBaselineRouteWitness CompileBaseline(
            TerrainClusterRouteWitnessCompileRequest request,
            TerrainClusterStaticShell shell,
            IDictionary<string, TraversalEdgeDurationEvidence> durations,
            ICollection<TerrainClusterRouteWitnessCompileError> errors,
            out CompiledClusterSpineVariant baselineVariant)
        {
            baselineVariant = null;
            var baselines = request.TraversalCompilation.Variants.Where(value => value.IsBaseline).ToArray();
            if (baselines.Length != 1 || baselines[0].VariantId != request.Intent.BaselineVariantId)
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidBaselineVariant,
                    "intent.baselineVariantId", "Intent must bind the exact single source baseline variant.");
                return null;
            }
            var selectedBaseline = baselines[0];
            baselineVariant = selectedBaseline;

            ProjectedClusterPort entryPort;
            ProjectedClusterPort exitPort;
            if (!request.RoleSocketContract.TryGetPrimaryPort(ClusterPortKind.Entry, out entryPort) ||
                !request.RoleSocketContract.TryGetPrimaryPort(ClusterPortKind.Exit, out exitPort))
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.DisconnectedBaselinePath,
                    "baseline.ports", "Entry and Exit primary ports are required.");
                return null;
            }
            var links = request.RoleSocketContract.RoleSpineLinks.Where(value => value.VariantId == selectedBaseline.VariantId).ToArray();
            var entryLinks = links.Where(value => value.RoleAnchorId == entryPort.RoleAnchorId &&
                value.RoleKind == ClusterRoleKind.Entry && value.ConnectionKind == ProjectedRoleConnectionKind.EntryPort).ToArray();
            var exitLinks = links.Where(value => value.RoleAnchorId == exitPort.RoleAnchorId &&
                value.RoleKind == ClusterRoleKind.Exit && value.ConnectionKind == ProjectedRoleConnectionKind.ExitPort).ToArray();
            if (entryLinks.Length != 1 || exitLinks.Length != 1)
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.DisconnectedBaselinePath,
                    "baseline.portRoleNodeChain", "Exact Entry port-role-node and Exit node-role-port chains are required.");
                return null;
            }

            var path = FindMinimumEdgePath(baselineVariant, entryLinks[0].TraversalNodeId, exitLinks[0].TraversalNodeId);
            if (path == null)
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.MissingBaselinePath,
                    "baseline.path", "No directed Entry to Exit path exists.");
                return null;
            }
            if (!ValidateShellPath(shell, baselineVariant, path.Edges, path.NodeIds, errors, "baseline")) return null;

            var requiredRoles = new[] { ClusterRoleKind.BuildUp, ClusterRoleKind.Core, ClusterRoleKind.Recovery };
            var preserved = new List<ClusterRoleKind>();
            var lastIndex = -1;
            foreach (var roleKind in requiredRoles)
            {
                var roleLinks = links.Where(value => value.RoleKind == roleKind).ToArray();
                var indexes = roleLinks.Select(value => IndexOf(path.NodeIds, value.TraversalNodeId)).Where(value => value >= 0).ToArray();
                if (roleLinks.Length == 0 || indexes.Length == 0 || indexes.Min() <= lastIndex)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.DisconnectedBaselinePath,
                        "baseline.roles/" + roleKind, "Mandatory BuildUp/Core/Recovery role evidence is missing or out of source order.");
                }
                else
                {
                    lastIndex = indexes.Min();
                    preserved.Add(roleKind);
                }
            }
            if (preserved.Count != requiredRoles.Length) return null;

            var witnessEdges = ToWitnessEdges(path.Edges, durations);
            return new TerrainClusterBaselineRouteWitness(
                baselineVariant.VariantId,
                entryPort.PortId, entryPort.RoleAnchorId, entryLinks[0].TraversalNodeId,
                exitLinks[0].TraversalNodeId, exitPort.RoleAnchorId, exitPort.PortId,
                path.NodeIds,
                witnessEdges,
                Coordinates(baselineVariant, path.NodeIds),
                CoveredProtectedTiles(baselineVariant, path.NodeIds, path.Edges),
                preserved);
        }

        private static void CompileHighAndRecovery(
            TerrainClusterRouteWitnessCompileRequest request,
            TerrainClusterStaticShell shell,
            IDictionary<string, TraversalEdgeDurationEvidence> durations,
            CompiledClusterSpineVariant baselineVariant,
            TerrainClusterBaselineRouteWitness baseline,
            ICollection<TerrainClusterHighRouteWitness> highRoutes,
            ICollection<TerrainClusterRecoveryRouteWitness> recoveryRoutes,
            ICollection<TerrainClusterRouteWitnessCompileError> errors)
        {
            if (request.Intent.HighRoutes.Count == 0)
            {
                Add(errors, TerrainClusterRouteWitnessCompileErrorCode.MissingHighRoute,
                    "intent.highRoutes", "At least one authored high route is required.");
                return;
            }
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in request.Intent.HighRoutes)
            {
                if (definition == null)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.MissingHighRoute,
                        "intent.highRoutes", "Null high route definition is not allowed.");
                    continue;
                }
                var root = "intent.highRoutes/" + definition.HighRouteId;
                var definitionValid = true;
                if (!HighRouteIdPattern.IsMatch(definition.HighRouteId) || !seenIds.Add(definition.HighRouteId))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidHighRouteId,
                        root, "High route ID must be unique and match HIGH_ROUTE_[A-Z0-9_]+.");
                    definitionValid = false;
                }
                CompiledClusterSpineVariant variant;
                if (!request.TraversalCompilation.TryGetVariant(definition.VariantId, out variant) || variant.IsBaseline)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidHighRoutePath,
                        root + "/variantId", "High route must reference a compiled non-baseline variant.");
                    continue;
                }

                var pathEdges = new List<CompiledTraversalEdge>();
                var duplicateEdge = definition.OrderedEdgeIds.GroupBy(value => value, StringComparer.Ordinal).Any(group => group.Count() != 1);
                if (definition.OrderedEdgeIds.Count == 0 || duplicateEdge)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidHighRoutePath,
                        root + "/orderedEdgeIds", "Ordered edge list must be non-empty and contain no duplicate.");
                    definitionValid = false;
                }
                foreach (var edgeId in definition.OrderedEdgeIds)
                {
                    CompiledTraversalEdge edge;
                    if (!variant.TryGetEdge(edgeId, out edge))
                    {
                        Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidHighRoutePath,
                            root + "/orderedEdgeIds/" + edgeId, "High route edge is outside the selected compiled variant.");
                        definitionValid = false;
                    }
                    else pathEdges.Add(edge);
                }
                var nodeIds = pathEdges.Count == 0 ? new List<string>() :
                    new List<string> { pathEdges[0].FromNodeId };
                foreach (var edge in pathEdges)
                {
                    if (nodeIds[nodeIds.Count - 1] != edge.FromNodeId)
                    {
                        Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidHighRoutePath,
                            root + "/orderedEdgeIds", "High route edges are not a contiguous directed path.");
                        definitionValid = false;
                    }
                    nodeIds.Add(edge.ToNodeId);
                }
                if (nodeIds.Count == 0 || nodeIds[0] != definition.BaseDivergenceNodeId ||
                    nodeIds[nodeIds.Count - 1] != definition.BaseRejoinNodeId ||
                    !baseline.OrderedNodeIds.Contains(definition.BaseDivergenceNodeId) ||
                    !baseline.OrderedNodeIds.Contains(definition.BaseRejoinNodeId) ||
                    IndexOf(baseline.OrderedNodeIds, definition.BaseDivergenceNodeId) >= IndexOf(baseline.OrderedNodeIds, definition.BaseRejoinNodeId))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidHighRoutePath,
                        root + "/baselineJoin", "High route must diverge from and rejoin the ordered baseline path.");
                    definitionValid = false;
                }
                if (!nodeIds.Contains(definition.HighPointNodeId))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidHighPoint,
                        root + "/highPointNodeId", "Authored high point must be on the high route path.");
                    definitionValid = false;
                }
                var benefits = definition.BenefitIds.Distinct(StringComparer.Ordinal).ToArray();
                if (benefits.Length < 2 || benefits.Any(value => !BenefitIdPattern.IsMatch(value)))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InsufficientHighRouteBenefits,
                        root + "/benefitIds", "At least two distinct BENEFIT_[A-Z0-9_]+ IDs are required.");
                    definitionValid = false;
                }
                if (definition.FailureNodeIds.Count == 0 || definition.FailureNodeIds.Distinct(StringComparer.Ordinal).Count() != definition.FailureNodeIds.Count ||
                    definition.FailureNodeIds.Any(value => !nodeIds.Contains(value) || value == baseline.EntryNodeId || value == baseline.ExitNodeId))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.InvalidFailureNode,
                        root + "/failureNodeIds", "Every unique failure node must be on the high path and cannot be Entry or Exit.");
                    definitionValid = false;
                }

                var baseEdges = BaselineSubpathEdges(baseline, definition.BaseDivergenceNodeId, definition.BaseRejoinNodeId);
                if (definition.OrderedEdgeIds.SequenceEqual(baseEdges, StringComparer.Ordinal) &&
                    nodeIds.SequenceEqual(BaselineSubpathNodes(baseline, definition.BaseDivergenceNodeId, definition.BaseRejoinNodeId), StringComparer.Ordinal))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.HighRouteNotDistinct,
                        root, "High route is structurally identical to the baseline subpath.");
                    definitionValid = false;
                }
                if (!ValidateShellPath(shell, variant, pathEdges, nodeIds, errors, root)) definitionValid = false;
                if (!definitionValid) continue;

                var high = new TerrainClusterHighRouteWitness(
                    definition, nodeIds, ToWitnessEdges(pathEdges, durations),
                    CoveredProtectedTiles(variant, nodeIds, pathEdges));
                highRoutes.Add(high);
                CompileRecoveries(request, shell, durations, variant, baseline, high,
                    recoveryRoutes, errors);
            }
        }

        private static void CompileRecoveries(
            TerrainClusterRouteWitnessCompileRequest request,
            TerrainClusterStaticShell shell,
            IDictionary<string, TraversalEdgeDurationEvidence> durations,
            CompiledClusterSpineVariant variant,
            TerrainClusterBaselineRouteWitness baseline,
            TerrainClusterHighRouteWitness high,
            ICollection<TerrainClusterRecoveryRouteWitness> recoveryRoutes,
            ICollection<TerrainClusterRouteWitnessCompileError> errors)
        {
            var baselineTargets = new HashSet<string>(baseline.OrderedNodeIds, StringComparer.Ordinal);
            var recoveryRoleTargets = new HashSet<string>(request.RoleSocketContract.RoleSpineLinks
                .Where(value => value.VariantId == variant.VariantId && value.RoleKind == ClusterRoleKind.Recovery && baselineTargets.Contains(value.TraversalNodeId))
                .Select(value => value.TraversalNodeId), StringComparer.Ordinal);
            foreach (var failureNodeId in high.FailureNodeIds)
            {
                var preferredTargets = recoveryRoleTargets.Count == 0 ? baselineTargets : recoveryRoleTargets;
                var path = FindMinimumDurationPath(variant, failureNodeId, preferredTargets, durations);
                if (path == null && recoveryRoleTargets.Count != 0)
                    path = FindMinimumDurationPath(variant, failureNodeId, baselineTargets, durations);
                var root = "recovery/" + high.HighRouteId + "/" + failureNodeId;
                if (path == null || path.Edges.Count == 0)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.MissingRecoveryPath,
                        root, "Failure node has no directed recovery path to the baseline.");
                    continue;
                }
                var target = path.NodeIds[path.NodeIds.Count - 1];
                if (!baselineTargets.Contains(target))
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.RecoveryTargetMismatch,
                        root, "Recovery target is not a baseline path node.");
                    continue;
                }
                if (path.DurationMilliseconds < MinimumRecoveryMilliseconds)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.RecoveryTooShort,
                        root, "Recovery duration is below 2000 ms inclusive gate.");
                    continue;
                }
                if (path.DurationMilliseconds > MaximumRecoveryMilliseconds)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.RecoveryTooLong,
                        root, "Recovery duration exceeds 5000 ms inclusive gate.");
                    continue;
                }
                if (!ValidateShellPath(shell, variant, path.Edges, path.NodeIds, errors, root)) continue;
                recoveryRoutes.Add(new TerrainClusterRecoveryRouteWitness(
                    high.HighRouteId,
                    failureNodeId,
                    target,
                    recoveryRoleTargets.Contains(target),
                    path.NodeIds,
                    ToWitnessEdges(path.Edges, durations),
                    Coordinates(variant, path.NodeIds),
                    CoveredProtectedTiles(variant, path.NodeIds, path.Edges)));
            }
        }

        private static PathCandidate FindMinimumEdgePath(
            CompiledClusterSpineVariant variant, string start, string target)
        {
            var frontier = new List<PathCandidate> { new PathCandidate(start) };
            while (frontier.Count != 0)
            {
                frontier.Sort(CompareByEdgeCount);
                var current = frontier[0]; frontier.RemoveAt(0);
                if (current.CurrentNodeId == target) return current;
                foreach (var edge in variant.Edges.Where(value => value.FromNodeId == current.CurrentNodeId)
                    .OrderBy(value => value.EdgeId, StringComparer.Ordinal))
                    if (!current.NodeIds.Contains(edge.ToNodeId)) frontier.Add(current.Append(edge, 0));
            }
            return null;
        }

        private static PathCandidate FindMinimumDurationPath(
            CompiledClusterSpineVariant variant,
            string start,
            ISet<string> targets,
            IDictionary<string, TraversalEdgeDurationEvidence> durations)
        {
            var frontier = new List<PathCandidate> { new PathCandidate(start) };
            while (frontier.Count != 0)
            {
                frontier.Sort(CompareByDuration);
                var current = frontier[0]; frontier.RemoveAt(0);
                if (current.Edges.Count != 0 && targets.Contains(current.CurrentNodeId)) return current;
                foreach (var edge in variant.Edges.Where(value => value.FromNodeId == current.CurrentNodeId)
                    .OrderBy(value => value.EdgeId, StringComparer.Ordinal))
                {
                    TraversalEdgeDurationEvidence evidence;
                    if (current.NodeIds.Contains(edge.ToNodeId) || !durations.TryGetValue(EdgeKey(edge.VariantId, edge.EdgeId), out evidence)) continue;
                    frontier.Add(current.Append(edge, evidence.EstimatedDurationMilliseconds));
                }
            }
            return null;
        }

        private static int CompareByEdgeCount(PathCandidate left, PathCandidate right)
        {
            var comparison = left.Edges.Count.CompareTo(right.Edges.Count);
            return comparison != 0 ? comparison : CompareEdgeSequences(left.Edges, right.Edges);
        }

        private static int CompareByDuration(PathCandidate left, PathCandidate right)
        {
            var comparison = left.DurationMilliseconds.CompareTo(right.DurationMilliseconds);
            if (comparison != 0) return comparison;
            comparison = left.Edges.Count.CompareTo(right.Edges.Count);
            return comparison != 0 ? comparison : CompareEdgeSequences(left.Edges, right.Edges);
        }

        private static int CompareEdgeSequences(IReadOnlyList<CompiledTraversalEdge> left, IReadOnlyList<CompiledTraversalEdge> right)
        {
            for (var index = 0; index < Math.Min(left.Count, right.Count); index++)
            {
                var comparison = string.Compare(left[index].EdgeId, right[index].EdgeId, StringComparison.Ordinal);
                if (comparison != 0) return comparison;
            }
            return left.Count.CompareTo(right.Count);
        }

        private static bool ValidateShellPath(
            TerrainClusterStaticShell shell,
            CompiledClusterSpineVariant variant,
            IEnumerable<CompiledTraversalEdge> edges,
            IEnumerable<string> nodeIds,
            ICollection<TerrainClusterRouteWitnessCompileError> errors,
            string path)
        {
            if (shell == null) return false;
            var valid = true;
            foreach (var nodeId in nodeIds)
            {
                CompiledTraversalNode node;
                TerrainClusterStaticShellCell cell;
                if (!variant.TryGetNode(nodeId, out node) || !shell.TryGetCell(node.CompiledCoordinate, out cell) ||
                    cell.Occupancy != TerrainClusterShellOccupancy.Air)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.ShellRouteMismatch,
                        path + "/nodes/" + nodeId, "Route node must resolve to an explicit Air shell cell.");
                    valid = false;
                }
            }
            foreach (var edge in edges)
            foreach (var tile in edge.Envelope.AllTiles)
            {
                TerrainClusterStaticShellCell cell;
                var expected = tile.SetKind == CompiledTraversalEnvelopeSetKind.Floor
                    ? TerrainClusterShellOccupancy.Solid : TerrainClusterShellOccupancy.Air;
                if (!shell.TryGetCell(tile.CompiledCoordinate, out cell) || cell.Occupancy != expected)
                {
                    Add(errors, TerrainClusterRouteWitnessCompileErrorCode.ShellRouteMismatch,
                        path + "/edges/" + edge.EdgeId + "/" + tile.SetKind,
                        "Route envelope does not match Static Shell occupancy.");
                    valid = false;
                }
            }
            return valid;
        }

        private static TerrainClusterRouteWitnessEdge[] ToWitnessEdges(
            IEnumerable<CompiledTraversalEdge> edges,
            IDictionary<string, TraversalEdgeDurationEvidence> durations)
        {
            return edges.Select(edge =>
            {
                TraversalEdgeDurationEvidence evidence;
                durations.TryGetValue(EdgeKey(edge.VariantId, edge.EdgeId), out evidence);
                return new TerrainClusterRouteWitnessEdge(edge,
                    evidence == null ? 0 : evidence.EstimatedDurationMilliseconds);
            }).ToArray();
        }

        private static LocalTileCoord[] Coordinates(CompiledClusterSpineVariant variant, IEnumerable<string> nodeIds)
        {
            return nodeIds.Select(value => { CompiledTraversalNode node; variant.TryGetNode(value, out node); return node.CompiledCoordinate; }).ToArray();
        }

        private static LocalTileCoord[] CoveredProtectedTiles(
            CompiledClusterSpineVariant variant,
            IEnumerable<string> nodeIds,
            IEnumerable<CompiledTraversalEdge> edges)
        {
            var nodes = new HashSet<string>(nodeIds, StringComparer.Ordinal);
            var edgeIds = new HashSet<string>(edges.Select(value => value.EdgeId), StringComparer.Ordinal);
            return variant.ProtectedTiles.Where(tile => tile.Provenance.Any(value =>
                    nodes.Contains(value.NodeId) || edgeIds.Contains(value.EdgeId)))
                .Select(value => value.CompiledCoordinate).Distinct()
                .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray();
        }

        private static string[] BaselineSubpathEdges(TerrainClusterBaselineRouteWitness baseline, string start, string end)
        {
            var first = IndexOf(baseline.OrderedNodeIds, start);
            var last = IndexOf(baseline.OrderedNodeIds, end);
            if (first < 0 || last <= first) return Array.Empty<string>();
            return baseline.OrderedEdges.Skip(first).Take(last - first).Select(value => value.EdgeId).ToArray();
        }

        private static string[] BaselineSubpathNodes(TerrainClusterBaselineRouteWitness baseline, string start, string end)
        {
            var first = IndexOf(baseline.OrderedNodeIds, start);
            var last = IndexOf(baseline.OrderedNodeIds, end);
            if (first < 0 || last <= first) return Array.Empty<string>();
            return baseline.OrderedNodeIds.Skip(first).Take(last - first + 1).ToArray();
        }

        private static int IndexOf(IReadOnlyList<string> values, string target)
        {
            for (var index = 0; index < values.Count; index++)
                if (string.Equals(values[index], target, StringComparison.Ordinal)) return index;
            return -1;
        }

        private static string ComputeDigest(
            TerrainClusterRouteWitnessCompileRequest request,
            TerrainClusterStaticShell shell,
            TerrainClusterBaselineRouteWitness baseline,
            IEnumerable<TerrainClusterHighRouteWitness> highRoutes,
            IEnumerable<TerrainClusterRecoveryRouteWitness> recoveryRoutes,
            string ruleset)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", ruleset);
            Append(material, "TRAVERSAL", request.TraversalCompilation.CanonicalDigest);
            Append(material, "BASELINE_INTENT", request.Intent.BaselineVariantId.Value);
            foreach (var cell in shell.Cells)
            {
                Append(material, "SHELL", Coordinate(cell.CompiledCoordinate),
                    Coordinate(cell.OwningChunk), ((int)cell.Occupancy).ToString(CultureInfo.InvariantCulture), cell.IsProtectedOpen ? "1" : "0");
                foreach (var provenance in cell.Provenance)
                    Append(material, "SHELL_PROVENANCE", provenance.VariantId.Value, provenance.EdgeId,
                        ((int)provenance.EnvelopeSetKind).ToString(CultureInfo.InvariantCulture),
                        Coordinate(provenance.SourceCoordinate), Coordinate(provenance.CompiledCoordinate));
            }
            foreach (var evidence in request.Intent.EdgeDurationEvidence.OrderBy(value => value.StableIdentity, StringComparer.Ordinal))
                Append(material, "DURATION", evidence.VariantId.Value, evidence.EdgeId,
                    evidence.EstimatedDurationMilliseconds.ToString(CultureInfo.InvariantCulture), evidence.RulesetId);
            Append(material, "BASELINE_CHAIN", baseline.EntryPortId, baseline.EntryRoleAnchorId,
                baseline.EntryNodeId, baseline.ExitNodeId, baseline.ExitRoleAnchorId, baseline.ExitPortId,
                string.Join(",", baseline.PreservedMandatoryRoles.Select(value => ((int)value).ToString(CultureInfo.InvariantCulture))));
            AppendPath(material, "BASELINE", baseline.OrderedNodeIds, baseline.OrderedEdges);
            Append(material, "BASELINE_PROTECTION", string.Join(",", baseline.CoveredProtectedTiles.Select(Coordinate)));
            foreach (var high in highRoutes.OrderBy(value => value.HighRouteId, StringComparer.Ordinal))
            {
                Append(material, "HIGH", high.HighRouteId, high.VariantId.Value, high.BaseDivergenceNodeId,
                    high.BaseRejoinNodeId, high.HighPointNodeId, string.Join(",", high.BenefitIds), string.Join(",", high.FailureNodeIds));
                AppendPath(material, "HIGH_PATH/" + high.HighRouteId, high.OrderedNodeIds, high.OrderedEdges);
                Append(material, "HIGH_PROTECTION/" + high.HighRouteId,
                    string.Join(",", high.CoveredProtectedTiles.Select(Coordinate)));
            }
            foreach (var recovery in recoveryRoutes.OrderBy(value => value.HighRouteId, StringComparer.Ordinal).ThenBy(value => value.FailureNodeId, StringComparer.Ordinal))
            {
                Append(material, "RECOVERY", recovery.HighRouteId, recovery.FailureNodeId,
                    recovery.TargetBaselineNodeId, recovery.TargetsRecoveryRole ? "1" : "0",
                    recovery.TotalEstimatedDurationMilliseconds.ToString(CultureInfo.InvariantCulture));
                AppendPath(material, "RECOVERY_PATH/" + recovery.HighRouteId + "/" + recovery.FailureNodeId,
                    recovery.OrderedNodeIds, recovery.OrderedEdges);
                Append(material, "RECOVERY_COORDINATES/" + recovery.HighRouteId + "/" + recovery.FailureNodeId,
                    string.Join(",", recovery.CompiledCoordinates.Select(Coordinate)));
                Append(material, "RECOVERY_PROTECTION/" + recovery.HighRouteId + "/" + recovery.FailureNodeId,
                    string.Join(",", recovery.CoveredProtectedTiles.Select(Coordinate)));
            }
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(material.ToString())).Select(value => value.ToString("x2")));
        }

        private static void AppendPath(StringBuilder material, string kind, IEnumerable<string> nodes, IEnumerable<TerrainClusterRouteWitnessEdge> edges)
        {
            Append(material, kind + "_NODES", string.Join(",", nodes));
            foreach (var edge in edges) Append(material, kind + "_EDGE", edge.VariantId.Value, edge.EdgeId,
                edge.FromNodeId, edge.ToNodeId, ((int)edge.MovementKind).ToString(CultureInfo.InvariantCulture),
                Coordinate(edge.CompiledStartCoordinate), Coordinate(edge.CompiledEndCoordinate),
                edge.EstimatedDurationMilliseconds.ToString(CultureInfo.InvariantCulture));
        }

        private static void Append(StringBuilder material, params string[] fields)
        {
            if (material.Length != 0) material.Append('\n');
            material.Append(string.Join("|", fields));
        }

        private static string Coordinate(LocalTileCoord value)
        {
            return value.X.ToString(CultureInfo.InvariantCulture) + "," + value.Y.ToString(CultureInfo.InvariantCulture);
        }

        private static string Coordinate(ClusterChunkCoord value)
        {
            return value.X.ToString(CultureInfo.InvariantCulture) + "," + value.Y.ToString(CultureInfo.InvariantCulture);
        }

        private static string CoordinatePath(LocalTileCoord value) { return "tile/" + Coordinate(value); }
        private static string EdgeKey(SpineVariantId variantId, string edgeId) { return variantId.Value + "/" + (edgeId ?? string.Empty); }

        private static void Add(ICollection<TerrainClusterRouteWitnessCompileError> errors,
            TerrainClusterRouteWitnessCompileErrorCode code, string path, string detail)
        {
            errors.Add(new TerrainClusterRouteWitnessCompileError(code, path, detail));
        }

        private static TerrainClusterRouteWitnessCompileResult Failure(IEnumerable<TerrainClusterRouteWitnessCompileError> errors)
        {
            return new TerrainClusterRouteWitnessCompileResult(null, errors);
        }

        private sealed class ShellCellBuilder
        {
            public ShellCellBuilder(ClusterChunkCoord owningChunk)
            {
                OwningChunk = owningChunk;
                Provenance = new List<TerrainClusterStaticShellProvenance>();
            }
            public ClusterChunkCoord OwningChunk { get; }
            public bool RequiresAir { get; set; }
            public bool RequiresSolid { get; set; }
            public List<TerrainClusterStaticShellProvenance> Provenance { get; }
        }

        private sealed class PathCandidate
        {
            public PathCandidate(string start)
            {
                NodeIds = new List<string> { start };
                Edges = new List<CompiledTraversalEdge>();
            }
            private PathCandidate(IEnumerable<string> nodes, IEnumerable<CompiledTraversalEdge> edges, int duration)
            {
                NodeIds = new List<string>(nodes);
                Edges = new List<CompiledTraversalEdge>(edges);
                DurationMilliseconds = duration;
            }
            public List<string> NodeIds { get; }
            public List<CompiledTraversalEdge> Edges { get; }
            public int DurationMilliseconds { get; }
            public string CurrentNodeId => NodeIds[NodeIds.Count - 1];
            public PathCandidate Append(CompiledTraversalEdge edge, int duration)
            {
                return new PathCandidate(NodeIds.Concat(new[] { edge.ToNodeId }),
                    Edges.Concat(new[] { edge }), DurationMilliseconds + duration);
            }
        }
    }
}
