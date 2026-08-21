#if LEGACY_DISABLED
using System;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    public enum RegionId
    {
        Common,
        Moon,
        Bridge,
        Palace,
        Post,
        Sun,
        Polaris,
    }

    public enum RoomRole
    {
        Start,
        Main,
        Branch,
        Secret,
        Rest,
        Exit,
        Boss,
    }

    public enum TraversalType
    {
        Walk,
        Drop,
        Climb,
        Portal,
    }

    public enum StageLayoutMode
    {
        Graph,
        Room,
        ElementSlots,
        Simulation,
    }

    public enum RoomEdgeType
    {
        PortalPair,
        SecretGate,
    }

    [Serializable]
    public class RoomEdge
    {
        public string EdgeId;
        public string FromNodeId;
        public string ToNodeId;
        public bool Bidirectional = true;
        public string FromSocket;
        public string ToSocket;
        public RoomEdgeType EdgeType = RoomEdgeType.PortalPair;
        public string Condition;

        public bool IsValid => !string.IsNullOrWhiteSpace(EdgeId) &&
                               !string.IsNullOrWhiteSpace(FromNodeId) &&
                               !string.IsNullOrWhiteSpace(ToNodeId) &&
                               !string.Equals(FromNodeId, ToNodeId, StringComparison.Ordinal);

        public string ConnectionGuid { get => EdgeId; set => EdgeId = value; }
        public string SourceNodeGuid { get => FromNodeId; set => FromNodeId = value; }
        public string TargetNodeGuid { get => ToNodeId; set => ToNodeId = value; }
        public string SourceSocketGuid { get => FromSocket; set => FromSocket = value; }
        public string TargetSocketGuid { get => ToSocket; set => ToSocket = value; }
    }

    public enum SocketCompatibility
    {
        Compatible,
        MissingSocket,
        SameRoom,
        DirectionMismatch,
        TraversalMismatch,
        OpeningSizeMismatch,
        FloorHeightMismatch,
        SecretTypeMismatch,
    }

    [Serializable]
    public sealed class RoomBudget
    {
        [Min(0)] public int MaxElements = 24;
        [Min(0)] public int MaxDynamicBodies = 8;
        [Min(0)] public int MaxHazards = 8;
        [Min(0)] public int MaxSignals = 8;
    }

    [Serializable]
    public sealed class RoomGeometryHash
    {
        public string Value;
    }

    [Serializable]
    public sealed class RoomSocketDefinition
    {
        public string SocketGuid;
        public CardinalDirection Side;
        public Vector2Int LocalCell;
        public Vector2Int OpeningSizeCells = Vector2Int.one;
        public TraversalType Traversal = TraversalType.Walk;
        public bool MainRouteAllowed = true;
        public bool SecretOnly;
        public int FloorHeightCell;
    }

    [Serializable]
    public sealed class RoomNodeSnapshot
    {
        public string NodeGuid;
        public RoomTemplate Template;
        public Vector2Int PositionCells;
        public RoomRole Role;
        public bool Locked;
        public bool MainRoute;
    }

    [Serializable]
    public sealed class RoomConnectionSnapshot
    {
        public string ConnectionGuid;
        public string SourceNodeGuid;
        public string SourceSocketGuid;
        public string TargetNodeGuid;
        public string TargetSocketGuid;
        public bool OneWay;
        public bool Secret;
        public bool MaruRoute;
        public TraversalType Traversal;
    }

    public static class RoomSizeCatalog
    {
        public static readonly Vector2Int Micro = new Vector2Int(12, 8);
        public static readonly Vector2Int Wide = new Vector2Int(24, 8);
        public static readonly Vector2Int Tall = new Vector2Int(12, 16);
        public static readonly Vector2Int Large = new Vector2Int(24, 16);
        public static readonly Vector2Int LongHall = new Vector2Int(36, 6);
        public static readonly Vector2Int DeepShaft = new Vector2Int(8, 24);
    }
}

#endif
