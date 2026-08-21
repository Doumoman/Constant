#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Generation.P6
{
    public enum P6StageSlot { X1 = 1, X2 = 2, X3 = 3 }
    public enum P6StageArchetype { Traverse, Descent, Ascent, Circuit, Fork, Chase }

    [Flags]
    public enum P6SocketSides
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Bottom = 1 << 2,
        Top = 1 << 3,
        All = Left | Right | Bottom | Top
    }

    public enum P6EdgeKind { Mst, Additional }
    public enum P6GenerationFailure
    {
        None,
        InvalidRequest,
        CatalogInsufficient,
        PackingFailed,
        GraphFailed,
        CorridorFailed,
        ValidationFailed,
        AttemptsExhausted
    }

    [Serializable]
    public sealed class P6RoomPrefabDescriptor
    {
        public P6RoomPrefabDescriptor(
            string prefabId,
            RoomArchetype archetype,
            RoomRole supportedRoles,
            RoomRegion region,
            Vector2Int macroSize,
            P6SocketSides socketSides,
            bool mainRouteAllowed,
            int selectionWeight = 1,
            int maxUses = 2)
            : this(
                prefabId,
                archetype,
                supportedRoles,
                region,
                macroSize,
                socketSides,
                mainRouteAllowed,
                selectionWeight,
                maxUses,
                null)
        {
        }

        public P6RoomPrefabDescriptor(
            string prefabId,
            RoomArchetype archetype,
            RoomRole supportedRoles,
            RoomRegion region,
            Vector2Int macroSize,
            P6SocketSides socketSides,
            bool mainRouteAllowed,
            int selectionWeight,
            int maxUses,
            P6RoomTraversalProof traversalProof)
        {
            PrefabId = prefabId ?? string.Empty;
            Archetype = archetype;
            SupportedRoles = supportedRoles;
            Region = region;
            MacroSize = macroSize;
            SocketSides = socketSides;
            MainRouteAllowed = mainRouteAllowed;
            SelectionWeight = Math.Max(1, selectionWeight);
            MaxUses = Mathf.Clamp(maxUses, 1, 2);
            TraversalProof = traversalProof;
        }

        public string PrefabId { get; }
        public RoomArchetype Archetype { get; }
        public RoomRole SupportedRoles { get; }
        public RoomRegion Region { get; }
        public Vector2Int MacroSize { get; }
        public P6SocketSides SocketSides { get; }
        public bool MainRouteAllowed { get; }
        public int SelectionWeight { get; }
        public int MaxUses { get; }
        public P6RoomTraversalProof TraversalProof { get; }
        public bool HasValidatedTraversalProof =>
            TraversalProof != null && TraversalProof.Passed;
        public bool Supports(RoomRole role) =>
            role == RoomRole.None || (SupportedRoles & role) == role;
    }

    public sealed class P6GenerationRequest
    {
        public P6GenerationRequest(
            int seed,
            P6StageSlot stageSlot,
            P6StageArchetype? archetypeOverride,
            RoomRegion region,
            IReadOnlyList<P6RoomPrefabDescriptor> prefabs,
            Vector2Int canvasSize,
            int minRooms = 9,
            int maxRooms = 14,
            int maxAttempts = 96)
        {
            Seed = seed;
            StageSlot = stageSlot;
            ArchetypeOverride = archetypeOverride;
            Region = region;
            Prefabs = prefabs ?? Array.Empty<P6RoomPrefabDescriptor>();
            CanvasSize = canvasSize;
            MinRooms = minRooms;
            MaxRooms = maxRooms;
            MaxAttempts = maxAttempts;
        }

        public int Seed { get; }
        public P6StageSlot StageSlot { get; }
        public P6StageArchetype? ArchetypeOverride { get; }
        public RoomRegion Region { get; }
        public IReadOnlyList<P6RoomPrefabDescriptor> Prefabs { get; }
        public Vector2Int CanvasSize { get; }
        public int MinRooms { get; }
        public int MaxRooms { get; }
        public int MaxAttempts { get; }
        public bool RequiresClosedLoop => StageSlot == P6StageSlot.X2;

        public static P6GenerationRequest CreateMoonPalace(
            int seed,
            P6StageSlot stageSlot,
            IReadOnlyList<P6RoomPrefabDescriptor> prefabs,
            P6StageArchetype? archetypeOverride = null)
        {
            return new P6GenerationRequest(
                seed,
                stageSlot,
                archetypeOverride,
                RoomRegion.MoonPalace,
                prefabs,
                P6MoonPalaceProfile.CanvasSize);
        }
    }

    public sealed class P6RoomNode
    {
        internal P6RoomNode(
            int id, RectInt bounds, P6RoomPrefabDescriptor prefab,
            RoomRole role, bool onMainPath)
        {
            Id = id;
            MacroBounds = bounds;
            PrefabId = prefab.PrefabId;
            Archetype = prefab.Archetype;
            SupportedRoles = prefab.SupportedRoles;
            Region = prefab.Region;
            SocketSides = prefab.SocketSides;
            MainRouteAllowed = prefab.MainRouteAllowed;
            TraversalProof = prefab.TraversalProof;
            Role = role;
            OnMainPath = onMainPath;
        }

        public int Id { get; }
        public RectInt MacroBounds { get; }
        public string PrefabId { get; }
        public RoomArchetype Archetype { get; }
        public RoomRole SupportedRoles { get; }
        public RoomRegion Region { get; }
        public P6SocketSides SocketSides { get; }
        public bool MainRouteAllowed { get; }
        public P6RoomTraversalProof TraversalProof { get; }
        public bool HasValidatedTraversalProof =>
            TraversalProof != null && TraversalProof.Passed;
        public RoomRole Role { get; }
        public bool OnMainPath { get; }
        public int Degree { get; internal set; }
    }

    public sealed class P6CandidateEdge
    {
        internal P6CandidateEdge(int first, int second, int weight)
        {
            FirstNodeId = Math.Min(first, second);
            SecondNodeId = Math.Max(first, second);
            Weight = weight;
        }

        public int FirstNodeId { get; }
        public int SecondNodeId { get; }
        public int Weight { get; }
    }

    public sealed class P6GraphEdge
    {
        internal P6GraphEdge(
            int first, int second,
            int firstAccessId, int secondAccessId,
            int weight, P6EdgeKind kind,
            IReadOnlyList<Vector2Int> corridorCells)
        {
            if (first <= second)
            {
                FirstNodeId = first;
                SecondNodeId = second;
                FirstAccessId = firstAccessId;
                SecondAccessId = secondAccessId;
            }
            else
            {
                FirstNodeId = second;
                SecondNodeId = first;
                FirstAccessId = secondAccessId;
                SecondAccessId = firstAccessId;
            }
            Weight = weight;
            Kind = kind;
            CorridorCells = corridorCells ?? Array.Empty<Vector2Int>();
        }

        public int FirstNodeId { get; }
        public int SecondNodeId { get; }
        public int FirstAccessId { get; }
        public int SecondAccessId { get; }
        public int Weight { get; }
        public P6EdgeKind Kind { get; }
        public IReadOnlyList<Vector2Int> CorridorCells { get; }
    }

    public sealed class P6RoomAccess
    {
        internal P6RoomAccess(
            int accessId,
            int nodeId,
            P6SocketTraversalProof socket,
            Vector2Int socketCell,
            Vector2Int portalCell)
        {
            AccessId = accessId;
            NodeId = nodeId;
            SocketId = socket != null ? socket.SocketId : string.Empty;
            SocketSide = socket != null
                ? ToP6Side(socket.Side) : P6SocketSides.None;
            CellOffset = socket != null
                ? socket.CellOffset : Vector2Int.zero;
            OpeningSize = socket != null
                ? socket.OpeningSize : Vector2Int.zero;
            TraversalType = socket != null
                ? socket.TraversalType : RoomTraversalType.ExitOnly;
            MainRouteAllowed = socket != null && socket.MainRouteAllowed;
            ValidationAnchor = socket != null
                ? socket.ValidationAnchor : Vector2Int.zero;
            SocketCell = socketCell;
            PortalCell = portalCell;
        }

        public int AccessId { get; }
        public int NodeId { get; }
        public string SocketId { get; }
        public P6SocketSides SocketSide { get; }
        public Vector2Int CellOffset { get; }
        public Vector2Int OpeningSize { get; }
        public RoomTraversalType TraversalType { get; }
        public bool MainRouteAllowed { get; }
        public Vector2Int ValidationAnchor { get; }
        public Vector2Int SocketCell { get; }
        public Vector2Int PortalCell { get; }
        public bool IsToolFreeMainRouteAccess =>
            MainRouteAllowed
            && OpeningSize == Vector2Int.one
            && RoomSocketRules.IsMainRouteTraversal(TraversalType);

        private static P6SocketSides ToP6Side(RoomSocketSide side)
        {
            switch (side)
            {
                case RoomSocketSide.Left: return P6SocketSides.Left;
                case RoomSocketSide.Right: return P6SocketSides.Right;
                case RoomSocketSide.Bottom: return P6SocketSides.Bottom;
                case RoomSocketSide.Top: return P6SocketSides.Top;
                default: return P6SocketSides.None;
            }
        }
    }

    public sealed class P6CorridorNetwork
    {
        internal P6CorridorNetwork(
            int routingScale,
            RectInt routingBounds,
            IReadOnlyList<Vector2Int> cells,
            IReadOnlyList<Vector2Int> junctions,
            IReadOnlyList<P6RoomAccess> accesses)
        {
            RoutingScale = routingScale;
            RoutingBounds = routingBounds;
            Cells = cells ?? Array.Empty<Vector2Int>();
            JunctionCells = junctions ?? Array.Empty<Vector2Int>();
            RoomAccesses = accesses ?? Array.Empty<P6RoomAccess>();
        }

        public int RoutingScale { get; }
        public RectInt RoutingBounds { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }
        public IReadOnlyList<Vector2Int> JunctionCells { get; }
        public IReadOnlyList<P6RoomAccess> RoomAccesses { get; }
    }

    public sealed class P6RoomGraphPlan
    {
        internal P6RoomGraphPlan(
            int seed, P6StageSlot slot, P6StageArchetype archetype,
            RectInt canvas, IReadOnlyList<P6RoomNode> rooms,
            IReadOnlyList<P6CandidateEdge> candidates,
            IReadOnlyList<P6GraphEdge> mst,
            IReadOnlyList<P6GraphEdge> additional,
            IReadOnlyList<int> mainPath, int start, int exit,
            P6CorridorNetwork corridors)
        {
            Seed = seed;
            StageSlot = slot;
            Archetype = archetype;
            CanvasBounds = canvas;
            Rooms = rooms;
            CandidateEdges = candidates;
            MstEdges = mst;
            AdditionalEdges = additional;
            MainPathNodeIds = mainPath;
            StartNodeId = start;
            ExitNodeId = exit;
            CorridorNetwork = corridors;
            var all = new List<P6GraphEdge>(mst.Count + additional.Count);
            all.AddRange(mst);
            all.AddRange(additional);
            Edges = all;
        }

        public int Seed { get; }
        public P6StageSlot StageSlot { get; }
        public P6StageArchetype Archetype { get; }
        public RectInt CanvasBounds { get; }
        public IReadOnlyList<P6RoomNode> Rooms { get; }
        public IReadOnlyList<P6CandidateEdge> CandidateEdges { get; }
        public IReadOnlyList<P6GraphEdge> MstEdges { get; }
        public IReadOnlyList<P6GraphEdge> AdditionalEdges { get; }
        public IReadOnlyList<P6GraphEdge> Edges { get; }
        public IReadOnlyList<int> MainPathNodeIds { get; }
        public int StartNodeId { get; }
        public int ExitNodeId { get; }
        public P6CorridorNetwork CorridorNetwork { get; }
        public bool HasClosedLoop => Edges.Count >= Rooms.Count;
    }

    public sealed class P6ValidationReport
    {
        private readonly List<string> issues = new List<string>();
        public IReadOnlyList<string> Issues => issues;
        public bool Passed => issues.Count == 0;
        public bool StartExitReachable { get; internal set; }
        public bool OptionalRoomsReturnable { get; internal set; }
        public bool ClosedLoopSatisfied { get; internal set; }
        public bool MainPathLengthSatisfied { get; internal set; }
        public bool PrefabUsageSatisfied { get; internal set; }
        public bool PackingSatisfied { get; internal set; }
        public bool CorridorNetworkConnected { get; internal set; }
        public bool SocketClaimsUnique { get; internal set; }
        public bool OneCellOpeningsSatisfied { get; internal set; }
        public bool JumpEnvelopeSatisfied { get; internal set; }
        public bool ToolFreeMainPathSatisfied { get; internal set; }
        public bool ExitBlockProofSatisfied { get; internal set; }
        public bool PhysicalTraversalSatisfied { get; internal set; }
        public bool RiskyChoiceSatisfied { get; internal set; }
        public bool X3RoleSequenceSatisfied { get; internal set; }
        public bool LandmarkPrioritySatisfied { get; internal set; }

        internal void Add(string issue)
        {
            if (!string.IsNullOrEmpty(issue)) issues.Add(issue);
        }

        public override string ToString() =>
            Passed ? "PASS" : string.Join(Environment.NewLine, issues);
    }

    public sealed class P6GenerationResult
    {
        internal P6GenerationResult(
            int requestedSeed, int acceptedSeed, int attempts,
            P6RoomGraphPlan plan, P6ValidationReport validation,
            P6GenerationFailure failure, string reason, string fingerprint)
        {
            RequestedSeed = requestedSeed;
            AcceptedSeed = acceptedSeed;
            Attempts = attempts;
            Plan = plan;
            Validation = validation;
            Failure = failure;
            FailureReason = reason ?? string.Empty;
            Fingerprint = fingerprint ?? string.Empty;
        }

        public int RequestedSeed { get; }
        public int AcceptedSeed { get; }
        public int Attempts { get; }
        public bool Accepted => Plan != null && Validation != null && Validation.Passed;
        public P6RoomGraphPlan Plan { get; }
        public P6ValidationReport Validation { get; }
        public P6GenerationFailure Failure { get; }
        public string FailureReason { get; }
        public string Fingerprint { get; }
    }
}

#endif
