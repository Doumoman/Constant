using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Activities
{
    public enum ActivityValidationErrorCode
    {
        MissingInput = 1,
        InvalidId = 2,
        InvalidShellReference = 3,
        InvalidSlot = 4,
        MissingCueOrTrigger = 5,
        InvalidCue = 6,
        InvalidGraphKind = 7,
        DuplicateNodeOrEdge = 8,
        MissingReference = 9,
        InvalidMechanismRelation = 10,
        UnreachableMechanismNode = 11,
        MissingProgressionPhase = 12,
        InvalidProgressionOrder = 13,
        InvalidFailureOrReset = 14,
        NoRecoveryOrExit = 15,
        InvalidRemovalSafety = 16,
        ProtectedMutation = 17,
    }

    public sealed class ActivityValidationError :
        IEquatable<ActivityValidationError>, IComparable<ActivityValidationError>
    {
        public ActivityValidationError(ActivityValidationErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public ActivityValidationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(ActivityValidationError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(ActivityValidationError other)
        {
            return other != null && Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as ActivityValidationError);
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

    public sealed class ActivityValidationResult
    {
        private readonly ReadOnlyCollection<ActivityValidationError> errors;

        internal ActivityValidationResult(
            ActivityStructureContract contract,
            IEnumerable<ActivityValidationError> errors,
            string canonicalDigest)
        {
            var copy = errors.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<ActivityValidationError>(copy);
            Contract = copy.Length == 0 ? contract : null;
            CanonicalDigest = copy.Length == 0 ? canonicalDigest ?? string.Empty : string.Empty;
        }

        public bool IsValid => Contract != null && errors.Count == 0;
        public ActivityStructureContract Contract { get; }
        public IReadOnlyList<ActivityValidationError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class ActivityContractValidator
    {
        private static readonly ProgressionPhaseKind[] RequiredSuccessPhases =
        {
            ProgressionPhaseKind.Cue,
            ProgressionPhaseKind.Activation,
            ProgressionPhaseKind.Core,
            ProgressionPhaseKind.Reward,
            ProgressionPhaseKind.Recovery,
            ProgressionPhaseKind.Exit,
        };

        public static ActivityValidationResult Validate(
            ActivityStructureContract contract,
            TerrainClusterContract staticShell)
        {
            var errors = new List<ActivityValidationError>();
            if (contract == null)
            {
                Add(errors, ActivityValidationErrorCode.MissingInput, "contract", "Contract is required.");
                return new ActivityValidationResult(null, errors, string.Empty);
            }

            ValidateIdentity(contract, errors);
            var shellValidation = ValidateShell(contract, staticShell, errors);
            var slots = ValidateSlots(contract, staticShell, errors);
            ValidateCues(contract, slots, errors);
            ValidateMechanism(contract.MechanismGraph, slots, errors);
            ValidateProgression(contract.ProgressionGraph, errors);
            ValidateRemoval(contract, staticShell, shellValidation, errors);

            return errors.Count == 0
                ? new ActivityValidationResult(contract, errors, ActivityCanonicalDigest.Compute(contract))
                : new ActivityValidationResult(null, errors, string.Empty);
        }

        private static void ValidateIdentity(
            ActivityStructureContract contract,
            ICollection<ActivityValidationError> errors)
        {
            if (!IsStableId(contract.Id.Value, "ACT_"))
                Add(errors, ActivityValidationErrorCode.InvalidId, "id", contract.Id.Value);

            if (contract.CompatiblePacingRoles.Count == 0 ||
                contract.CompatiblePacingRoles.Any(value => !PacingRoleTokenCodec.IsPublished(value)) ||
                contract.CompatiblePacingRoles.Distinct().Count() != contract.CompatiblePacingRoles.Count)
            {
                Add(errors, ActivityValidationErrorCode.InvalidShellReference, "compatibility.pacing",
                    "Published unique compatibility-only pacing roles are required.");
            }

            if (contract.CompatibleAccessClasses.Count == 0 ||
                contract.CompatibleAccessClasses.Any(value => !AccessClassTokenCodec.IsPublished(value)) ||
                contract.CompatibleAccessClasses.Distinct().Count() != contract.CompatibleAccessClasses.Count)
            {
                Add(errors, ActivityValidationErrorCode.InvalidShellReference, "compatibility.access",
                    "Published unique compatibility-only access classes are required.");
            }
        }

        private static TerrainClusterValidationResult ValidateShell(
            ActivityStructureContract contract,
            TerrainClusterContract staticShell,
            ICollection<ActivityValidationError> errors)
        {
            if (staticShell == null)
            {
                Add(errors, ActivityValidationErrorCode.InvalidShellReference, "staticShell", "TerrainCluster is required.");
                return null;
            }

            var allowlist = staticShell.Footprint != null && staticShell.Footprint.ActiveChunks.Count == 6
                ? new[] { staticShell.Id }
                : Array.Empty<TerrainClusterId>();
            var result = TerrainClusterContractValidator.Validate(staticShell, allowlist);
            if (!result.IsValid || contract.TerrainClusterId != staticShell.Id)
            {
                Add(errors, ActivityValidationErrorCode.InvalidShellReference, "staticShell.id",
                    contract.TerrainClusterId.Value + "|" + staticShell.Id.Value);
            }

            if (staticShell.Traversal == null ||
                !staticShell.Traversal.Variants.Any(value => value != null && value.Id == contract.CompatibleSpineVariantId))
            {
                Add(errors, ActivityValidationErrorCode.InvalidShellReference, "staticShell.compatibleSpine",
                    contract.CompatibleSpineVariantId.Value);
            }

            return result;
        }

        private static Dictionary<ActivitySlotId, ActivitySlot> ValidateSlots(
            ActivityStructureContract contract,
            TerrainClusterContract staticShell,
            ICollection<ActivityValidationError> errors)
        {
            var slots = new Dictionary<ActivitySlotId, ActivitySlot>();
            foreach (var slot in contract.Slots)
            {
                if (slot == null)
                {
                    Add(errors, ActivityValidationErrorCode.InvalidSlot, "slots", "Slot is required.");
                    continue;
                }

                var path = "slots[" + slot.Id.Value + "]";
                if (!IsStableId(slot.Id.Value, "SLOT_") || !IsStableId(slot.MarkerId, "MARKER_") ||
                    !IsDefinedSlotKind(slot.Kind) || !IsInsideFootprint(slot.Tile, staticShell))
                {
                    Add(errors, ActivityValidationErrorCode.InvalidSlot, path,
                        "Stable slot/marker IDs, exact kind, and an active-footprint tile are required.");
                }

                if (!slots.ContainsKey(slot.Id)) slots.Add(slot.Id, slot);
                else Add(errors, ActivityValidationErrorCode.InvalidSlot, path, "Slot ID occurs more than once.");
            }

            foreach (var kind in new[] { ActivitySlotKind.Cue, ActivitySlotKind.Trigger, ActivitySlotKind.Recovery })
            {
                if (contract.Slots.Count(value => value != null && value.Kind == kind) == 0)
                    Add(errors, ActivityValidationErrorCode.MissingCueOrTrigger, "slots.required[" + kind + "]",
                        "At least one slot is required.");
            }

            var duplicateMarkers = contract.Slots.Where(value => value != null)
                .GroupBy(value => value.MarkerId, StringComparer.Ordinal).Where(group => group.Count() > 1);
            foreach (var duplicate in duplicateMarkers)
                Add(errors, ActivityValidationErrorCode.InvalidSlot, "slots.marker[" + duplicate.Key + "]",
                    "Marker ID occurs more than once.");
            return slots;
        }

        private static void ValidateCues(
            ActivityStructureContract contract,
            IReadOnlyDictionary<ActivitySlotId, ActivitySlot> slots,
            ICollection<ActivityValidationError> errors)
        {
            if (contract.Cues.Count == 0)
                Add(errors, ActivityValidationErrorCode.MissingCueOrTrigger, "cues", "At least one cue is required.");

            var pairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cue in contract.Cues)
            {
                if (cue == null)
                {
                    Add(errors, ActivityValidationErrorCode.InvalidCue, "cues", "Cue is required.");
                    continue;
                }

                ActivitySlot slot;
                var pair = ((int)cue.Kind).ToString(CultureInfo.InvariantCulture) + "|" + cue.SlotId.Value;
                if (!IsDefinedCueKind(cue.Kind) || !slots.TryGetValue(cue.SlotId, out slot) ||
                    slot.Kind != ActivitySlotKind.Cue || !cue.DetectableBeforeActivation || !pairs.Add(pair))
                {
                    Add(errors, ActivityValidationErrorCode.InvalidCue, "cues[" + pair + "]",
                        "Cue kind/slot must be unique and detectable before activation.");
                }
            }
        }

        private static void ValidateMechanism(
            MechanismGraph graph,
            IReadOnlyDictionary<ActivitySlotId, ActivitySlot> slots,
            ICollection<ActivityValidationError> errors)
        {
            if (graph == null)
            {
                Add(errors, ActivityValidationErrorCode.MissingInput, "mechanism", "MechanismGraph is required.");
                return;
            }

            if (graph.GraphKind != TraversalGraphKind.Mechanism)
                Add(errors, ActivityValidationErrorCode.InvalidGraphKind, "mechanism.graphKind", graph.GraphKind.ToString());

            var nodes = new Dictionary<string, MechanismNode>(StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
            {
                if (node == null)
                {
                    Add(errors, ActivityValidationErrorCode.MissingInput, "mechanism.nodes", "Node is required.");
                    continue;
                }

                var path = "mechanism.nodes[" + node.NodeId + "]";
                if (node.GraphKind != TraversalGraphKind.Mechanism)
                    Add(errors, ActivityValidationErrorCode.InvalidGraphKind, path, node.GraphKind.ToString());
                if (!IsStableId(node.NodeId, "MECH_") || !IsDefinedMechanismNodeKind(node.Kind))
                    Add(errors, ActivityValidationErrorCode.InvalidId, path, node.NodeId);
                if (nodes.ContainsKey(node.NodeId))
                    Add(errors, ActivityValidationErrorCode.DuplicateNodeOrEdge, path, "Node ID occurs more than once.");
                else nodes.Add(node.NodeId, node);

                ActivitySlot slot;
                if (!slots.TryGetValue(node.SlotId, out slot) || !IsCompatible(node.Kind, slot.Kind))
                    Add(errors, ActivityValidationErrorCode.MissingReference, path + ".slot", node.SlotId.Value);
            }

            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            var validEdges = new List<MechanismEdge>();
            foreach (var edge in graph.Edges)
            {
                if (edge == null)
                {
                    Add(errors, ActivityValidationErrorCode.MissingInput, "mechanism.edges", "Edge is required.");
                    continue;
                }

                var path = "mechanism.edges[" + edge.EdgeId + "]";
                if (edge.GraphKind != TraversalGraphKind.Mechanism)
                    Add(errors, ActivityValidationErrorCode.InvalidGraphKind, path, edge.GraphKind.ToString());
                if (!IsStableId(edge.EdgeId, "MECH_EDGE_") || !edgeIds.Add(edge.EdgeId))
                    Add(errors, ActivityValidationErrorCode.DuplicateNodeOrEdge, path, edge.EdgeId);
                MechanismNode from;
                MechanismNode to;
                if (!nodes.TryGetValue(edge.FromNodeId, out from) || !nodes.TryGetValue(edge.ToNodeId, out to) ||
                    string.Equals(edge.FromNodeId, edge.ToNodeId, StringComparison.Ordinal))
                {
                    Add(errors, ActivityValidationErrorCode.MissingReference, path,
                        edge.FromNodeId + "->" + edge.ToNodeId);
                    continue;
                }

                validEdges.Add(edge);
                if (!IsDefinedMechanismRelation(edge.Relation) || !IsCompatible(edge.Relation, from.Kind, to.Kind))
                    Add(errors, ActivityValidationErrorCode.InvalidMechanismRelation, path, edge.Relation.ToString());
            }

            var triggers = nodes.Values.Where(value => value.Kind == MechanismNodeKind.Trigger).ToArray();
            if (triggers.Length != 1)
            {
                Add(errors, ActivityValidationErrorCode.MissingCueOrTrigger, "mechanism.trigger",
                    "Expected exactly one Trigger node; actual " + Number(triggers.Length) + ".");
                return;
            }

            var reachable = Reachable(triggers[0].NodeId, validEdges.Select(value =>
                new DirectedEdge(value.FromNodeId, value.ToNodeId)));
            foreach (var node in nodes.Values.Where(value => !reachable.Contains(value.NodeId)))
                Add(errors, ActivityValidationErrorCode.UnreachableMechanismNode,
                    "mechanism.nodes[" + node.NodeId + "]", "Node is not Trigger-reachable.");
        }

        private static void ValidateProgression(
            ProgressionGraph graph,
            ICollection<ActivityValidationError> errors)
        {
            if (graph == null)
            {
                Add(errors, ActivityValidationErrorCode.MissingInput, "progression", "ProgressionGraph is required.");
                return;
            }

            if (graph.GraphKind != TraversalGraphKind.Progression)
                Add(errors, ActivityValidationErrorCode.InvalidGraphKind, "progression.graphKind", graph.GraphKind.ToString());

            var nodes = new Dictionary<string, ProgressionNode>(StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
            {
                if (node == null)
                {
                    Add(errors, ActivityValidationErrorCode.MissingInput, "progression.nodes", "Node is required.");
                    continue;
                }

                var path = "progression.nodes[" + node.NodeId + "]";
                if (node.GraphKind != TraversalGraphKind.Progression)
                    Add(errors, ActivityValidationErrorCode.InvalidGraphKind, path, node.GraphKind.ToString());
                if (!IsStableId(node.NodeId, "PROG_") || !IsDefinedProgressionPhase(node.Phase))
                    Add(errors, ActivityValidationErrorCode.InvalidId, path, node.NodeId);
                if (nodes.ContainsKey(node.NodeId))
                    Add(errors, ActivityValidationErrorCode.DuplicateNodeOrEdge, path, "Node ID occurs more than once.");
                else nodes.Add(node.NodeId, node);
            }

            foreach (var phase in RequiredSuccessPhases)
            {
                if (nodes.Values.All(value => value.Phase != phase))
                    Add(errors, ActivityValidationErrorCode.MissingProgressionPhase,
                        "progression.phases[" + phase + "]", "Required phase is absent.");
            }

            ProgressionNode start;
            ProgressionNode terminal;
            if (!nodes.TryGetValue(graph.StartNodeId, out start) || start.Phase != ProgressionPhaseKind.Cue)
                Add(errors, ActivityValidationErrorCode.InvalidProgressionOrder, "progression.start", graph.StartNodeId);
            if (!nodes.TryGetValue(graph.TerminalNodeId, out terminal) || terminal.Phase != ProgressionPhaseKind.Exit)
                Add(errors, ActivityValidationErrorCode.InvalidProgressionOrder, "progression.terminal", graph.TerminalNodeId);

            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            var validEdges = new List<ProgressionEdge>();
            foreach (var edge in graph.Edges)
            {
                if (edge == null)
                {
                    Add(errors, ActivityValidationErrorCode.MissingInput, "progression.edges", "Edge is required.");
                    continue;
                }

                var path = "progression.edges[" + edge.EdgeId + "]";
                if (edge.GraphKind != TraversalGraphKind.Progression)
                    Add(errors, ActivityValidationErrorCode.InvalidGraphKind, path, edge.GraphKind.ToString());
                if (!IsStableId(edge.EdgeId, "PROG_EDGE_") || !edgeIds.Add(edge.EdgeId))
                    Add(errors, ActivityValidationErrorCode.DuplicateNodeOrEdge, path, edge.EdgeId);
                ProgressionNode from;
                ProgressionNode to;
                if (!nodes.TryGetValue(edge.FromNodeId, out from) || !nodes.TryGetValue(edge.ToNodeId, out to) ||
                    string.Equals(edge.FromNodeId, edge.ToNodeId, StringComparison.Ordinal))
                {
                    Add(errors, ActivityValidationErrorCode.MissingReference, path,
                        edge.FromNodeId + "->" + edge.ToNodeId);
                    continue;
                }

                validEdges.Add(edge);
                if (!IsDefinedProgressionEdge(edge.Kind))
                    Add(errors, ActivityValidationErrorCode.InvalidProgressionOrder, path, edge.Kind.ToString());
                if (edge.Kind == ProgressionEdgeKind.Failure &&
                    to.Phase != ProgressionPhaseKind.Recovery && to.Phase != ProgressionPhaseKind.Reset)
                    Add(errors, ActivityValidationErrorCode.InvalidFailureOrReset, path, "Failure must target Recovery or Reset.");
                if (edge.Kind == ProgressionEdgeKind.Reset &&
                    to.Phase != ProgressionPhaseKind.Activation && to.Phase != ProgressionPhaseKind.Core)
                    Add(errors, ActivityValidationErrorCode.InvalidFailureOrReset, path, "Reset must target Activation or Core.");
                if (from.Phase == ProgressionPhaseKind.Exit)
                    Add(errors, ActivityValidationErrorCode.InvalidFailureOrReset, path, "Exit cannot have outgoing edges.");
                if (edge.Kind == ProgressionEdgeKind.Exit && to.Phase != ProgressionPhaseKind.Exit)
                    Add(errors, ActivityValidationErrorCode.InvalidProgressionOrder, path, "Exit edge must target Exit.");
            }

            if (start != null && terminal != null && !HasOrderedSuccessPath(graph, nodes, validEdges))
                Add(errors, ActivityValidationErrorCode.InvalidProgressionOrder, "progression.successPath",
                    "Cue->Activation->Core->Reward->Recovery->Exit is required.");

            if (start != null)
            {
                var directed = validEdges.Select(value => new DirectedEdge(value.FromNodeId, value.ToNodeId)).ToArray();
                var reachable = Reachable(start.NodeId, directed);
                foreach (var node in nodes.Values.Where(value => !reachable.Contains(value.NodeId)))
                    Add(errors, ActivityValidationErrorCode.NoRecoveryOrExit,
                        "progression.nodes[" + node.NodeId + "]", "Node is unreachable from Cue.");
                foreach (var node in nodes.Values.Where(value => reachable.Contains(value.NodeId)))
                {
                    var exits = Reachable(node.NodeId, directed);
                    if (!exits.Any(id => nodes.ContainsKey(id) &&
                        (nodes[id].Phase == ProgressionPhaseKind.Recovery || nodes[id].Phase == ProgressionPhaseKind.Exit)))
                    {
                        Add(errors, ActivityValidationErrorCode.NoRecoveryOrExit,
                            "progression.nodes[" + node.NodeId + "]", "No Recovery or Exit is reachable.");
                    }
                }
            }
        }

        private static void ValidateRemoval(
            ActivityStructureContract contract,
            TerrainClusterContract staticShell,
            TerrainClusterValidationResult shellValidation,
            ICollection<ActivityValidationError> errors)
        {
            var safety = contract.RemovalSafety;
            if (safety == null)
            {
                Add(errors, ActivityValidationErrorCode.MissingInput, "removalSafety", "Removal safety is required.");
                return;
            }

            SpineVariant baseline = null;
            if (staticShell != null && staticShell.Traversal != null)
                baseline = staticShell.Traversal.Variants.FirstOrDefault(value =>
                    value != null && value.Id == safety.BaselineSpineVariantId && value.IsBaseline);
            if (baseline == null)
                Add(errors, ActivityValidationErrorCode.InvalidRemovalSafety, "removalSafety.baselineSpine",
                    safety.BaselineSpineVariantId.Value);
            else
            {
                if (!baseline.Nodes.Any(value => value != null && value.NodeId == safety.EntryTraversalNodeId) ||
                    !baseline.Nodes.Any(value => value != null && value.NodeId == safety.ExitTraversalNodeId))
                    Add(errors, ActivityValidationErrorCode.InvalidRemovalSafety, "removalSafety.entryExit",
                        safety.EntryTraversalNodeId + "->" + safety.ExitTraversalNodeId);

                var entry = staticShell.RoleAnchors.FirstOrDefault(value => value != null && value.Role == ClusterRoleKind.Entry);
                var exit = staticShell.RoleAnchors.FirstOrDefault(value => value != null && value.Role == ClusterRoleKind.Exit);
                if (entry == null || exit == null || entry.TraversalNodeId != safety.EntryTraversalNodeId ||
                    exit.TraversalNodeId != safety.ExitTraversalNodeId)
                    Add(errors, ActivityValidationErrorCode.InvalidRemovalSafety, "removalSafety.primaryEntryExit",
                        safety.EntryTraversalNodeId + "->" + safety.ExitTraversalNodeId);
            }

            ValidateSafetyTiles(safety.SafePocketTiles, "removalSafety.safePocket", staticShell, errors);
            ValidateSafetyTiles(safety.RecoveryTiles, "removalSafety.recovery", staticShell, errors);
            if (!safety.PreserveStaticTraversal || !safety.PreserveAccessClass ||
                safety.PermanentSolidMutationAllowed || safety.MandatoryExitDestructionAllowed ||
                safety.RouteTypeBeforeRemoval < 0 || safety.RouteTypeBeforeRemoval > 4 ||
                safety.RouteTypeBeforeRemoval != safety.RouteTypeAfterRemoval ||
                !AccessClassTokenCodec.IsPublished(safety.AccessClassBeforeRemoval) ||
                safety.AccessClassBeforeRemoval != safety.AccessClassAfterRemoval ||
                !contract.CompatibleAccessClasses.Contains(safety.AccessClassBeforeRemoval) ||
                safety.PermanentSolidWriteTiles.Count != 0)
            {
                Add(errors, ActivityValidationErrorCode.InvalidRemovalSafety, "removalSafety.identity",
                    "Static traversal, RouteType, AccessClass, and mandatory Exit must be preserved without permanent solids.");
            }

            var expectedDigest = shellValidation != null && shellValidation.IsValid
                ? shellValidation.CanonicalDigest
                : string.Empty;
            if (!IsSha256(safety.TraversalDigestBeforeRemoval) ||
                safety.TraversalDigestBeforeRemoval != safety.TraversalDigestAfterRemoval ||
                safety.TraversalDigestBeforeRemoval != expectedDigest)
            {
                Add(errors, ActivityValidationErrorCode.InvalidRemovalSafety, "removalSafety.traversalDigest",
                    safety.TraversalDigestBeforeRemoval + "|" + safety.TraversalDigestAfterRemoval);
            }

            if (baseline != null)
            {
                var protectedTiles = new HashSet<LocalTileCoord>(baseline.Edges.Where(value => value != null && value.Envelope != null)
                    .SelectMany(value => value.Envelope.ProtectedTiles));
                foreach (var tile in safety.PermanentSolidWriteTiles.Where(protectedTiles.Contains))
                    Add(errors, ActivityValidationErrorCode.ProtectedMutation,
                        "removalSafety.permanentSolidWrites[" + Coordinate(tile) + "]",
                        "Permanent writes cannot touch TerrainCluster protected envelopes.");
            }
        }

        private static void ValidateSafetyTiles(
            IReadOnlyList<LocalTileCoord> tiles,
            string path,
            TerrainClusterContract shell,
            ICollection<ActivityValidationError> errors)
        {
            if (tiles.Count == 0 || tiles.Distinct().Count() != tiles.Count ||
                tiles.Any(value => !IsInsideFootprint(value, shell)))
                Add(errors, ActivityValidationErrorCode.InvalidRemovalSafety, path,
                    "A non-empty unique active-footprint tile set is required.");
        }

        private static bool HasOrderedSuccessPath(
            ProgressionGraph graph,
            IReadOnlyDictionary<string, ProgressionNode> nodes,
            IEnumerable<ProgressionEdge> edges)
        {
            var usable = edges.Where(value => value.Kind == ProgressionEdgeKind.Advance || value.Kind == ProgressionEdgeKind.Exit)
                .GroupBy(value => value.FromNodeId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            var queue = new Queue<ProgressionState>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            queue.Enqueue(new ProgressionState(graph.StartNodeId, 0));
            while (queue.Count != 0)
            {
                var state = queue.Dequeue();
                var key = state.NodeId + "|" + Number(state.PhaseIndex);
                if (!visited.Add(key)) continue;
                if (state.NodeId == graph.TerminalNodeId && state.PhaseIndex == RequiredSuccessPhases.Length - 1)
                    return true;
                ProgressionEdge[] outgoing;
                if (!usable.TryGetValue(state.NodeId, out outgoing)) continue;
                foreach (var edge in outgoing)
                {
                    ProgressionNode target;
                    if (!nodes.TryGetValue(edge.ToNodeId, out target)) continue;
                    var next = state.PhaseIndex + 1;
                    if (next < RequiredSuccessPhases.Length && target.Phase == RequiredSuccessPhases[next])
                        queue.Enqueue(new ProgressionState(target.NodeId, next));
                }
            }
            return false;
        }

        private static HashSet<string> Reachable(string start, IEnumerable<DirectedEdge> edges)
        {
            var adjacency = edges.GroupBy(value => value.From, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Select(value => value.To).ToArray(), StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal) { start };
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                string[] next;
                if (!adjacency.TryGetValue(current, out next)) continue;
                foreach (var value in next) if (visited.Add(value)) queue.Enqueue(value);
            }
            return visited;
        }

        private static bool IsInsideFootprint(LocalTileCoord tile, TerrainClusterContract shell)
        {
            if (shell == null || shell.Footprint == null || tile.X < 0 || tile.Y < 0) return false;
            var chunk = new ClusterChunkCoord(
                tile.X / WorldGenConstants.MicroChunkWidthTiles,
                tile.Y / WorldGenConstants.MicroChunkHeightTiles);
            return shell.Footprint.ActiveChunks.Contains(chunk);
        }

        private static bool IsCompatible(MechanismNodeKind node, ActivitySlotKind slot)
        {
            switch (node)
            {
                case MechanismNodeKind.CueEmitter: return slot == ActivitySlotKind.Cue;
                case MechanismNodeKind.Trigger: return slot == ActivitySlotKind.Trigger;
                case MechanismNodeKind.Device: return slot == ActivitySlotKind.Device;
                case MechanismNodeKind.Hazard: return slot == ActivitySlotKind.Hazard;
                case MechanismNodeKind.ProjectileEmitter: return slot == ActivitySlotKind.Projectile;
                case MechanismNodeKind.RewardEmitter: return slot == ActivitySlotKind.Reward;
                case MechanismNodeKind.RecoveryController: return slot == ActivitySlotKind.Recovery;
                case MechanismNodeKind.ResetController: return slot == ActivitySlotKind.Reset;
                default: return false;
            }
        }

        private static bool IsCompatible(
            MechanismRelationKind relation,
            MechanismNodeKind from,
            MechanismNodeKind to)
        {
            switch (relation)
            {
                case MechanismRelationKind.Activates:
                    return from == MechanismNodeKind.Trigger || from == MechanismNodeKind.CueEmitter;
                case MechanismRelationKind.Drives:
                    return from == MechanismNodeKind.Device &&
                           (to == MechanismNodeKind.Hazard || to == MechanismNodeKind.ProjectileEmitter ||
                            to == MechanismNodeKind.RewardEmitter);
                case MechanismRelationKind.Emits:
                    return from == MechanismNodeKind.Device || from == MechanismNodeKind.ProjectileEmitter;
                case MechanismRelationKind.Enables:
                case MechanismRelationKind.Disables:
                    return from == MechanismNodeKind.Trigger || from == MechanismNodeKind.Device;
                case MechanismRelationKind.Resets:
                    return from == MechanismNodeKind.ResetController || to == MechanismNodeKind.ResetController;
                default: return false;
            }
        }

        private static bool IsStableId(string value, string prefix)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.Ordinal) || value.Length <= prefix.Length)
                return false;
            return value.All(character => (character >= 'A' && character <= 'Z') ||
                                          (character >= '0' && character <= '9') || character == '_');
        }

        private static bool IsSha256(string value)
        {
            return value != null && value.Length == 64 && value.All(character =>
                (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));
        }

        private static bool IsDefinedSlotKind(ActivitySlotKind value) => value >= ActivitySlotKind.Cue && value <= ActivitySlotKind.Npc;
        private static bool IsDefinedCueKind(ActivityCueKind value) => value >= ActivityCueKind.Visual && value <= ActivityCueKind.Motion;
        private static bool IsDefinedMechanismNodeKind(MechanismNodeKind value) => value >= MechanismNodeKind.CueEmitter && value <= MechanismNodeKind.ResetController;
        private static bool IsDefinedMechanismRelation(MechanismRelationKind value) => value >= MechanismRelationKind.Activates && value <= MechanismRelationKind.Resets;
        private static bool IsDefinedProgressionPhase(ProgressionPhaseKind value) => value >= ProgressionPhaseKind.Cue && value <= ProgressionPhaseKind.Exit;
        private static bool IsDefinedProgressionEdge(ProgressionEdgeKind value) => value >= ProgressionEdgeKind.Advance && value <= ProgressionEdgeKind.Exit;
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Coordinate(LocalTileCoord value) => Number(value.X) + "," + Number(value.Y);

        private static void Add(
            ICollection<ActivityValidationError> errors,
            ActivityValidationErrorCode code,
            string path,
            string detail) => errors.Add(new ActivityValidationError(code, path, detail));

        private sealed class DirectedEdge
        {
            public DirectedEdge(string from, string to) { From = from; To = to; }
            public string From { get; }
            public string To { get; }
        }

        private sealed class ProgressionState
        {
            public ProgressionState(string nodeId, int phaseIndex) { NodeId = nodeId; PhaseIndex = phaseIndex; }
            public string NodeId { get; }
            public int PhaseIndex { get; }
        }
    }
}
