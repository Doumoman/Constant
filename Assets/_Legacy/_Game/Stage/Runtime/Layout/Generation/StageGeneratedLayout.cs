#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    public enum GeneratedRouteKind
    {
        MainRoute,
        Branch,
        Secret,
        Loop,
    }

    public enum GeneratedElementSlotKind
    {
        Threat,
        Utility,
        Event,
        Shop,
    }

    [Serializable]
    public sealed class GeneratedElementSlot
    {
        public string SlotGuid;
        public GeneratedElementSlotKind Kind;
        public Vector2Int LocalCell;
        public string ContentId;
    }

    [Serializable]
    public sealed class StageGeneratedRoom
    {
        public string NodeGuid;
        public RoomTemplate Template;
        public Vector2Int PositionCells;
        public RoomRole Role;
        public bool Locked;
        public bool MainRoute;
        public List<GeneratedElementSlot> ElementSlots = new List<GeneratedElementSlot>();
    }

    [Serializable]
    public sealed class StageGeneratedConnection : RoomEdge
    {
        public GeneratedRouteKind RouteKind;
        public bool RequiresCorridor => false;
    }

    [Serializable]
    public sealed class StageLockedRoom
    {
        public string NodeGuid;
        public RoomTemplate Template;
        public Vector2Int PositionCells;
        public RoomRole Role;
        public bool MainRoute;
    }

    [Serializable]
    public sealed class StageGeneratedLayout
    {
        public string StageId;
        public int Seed;
        public int RerollNonce;
        public LayoutFamily Family;
        public List<StageGeneratedRoom> Rooms = new List<StageGeneratedRoom>();
        public List<StageGeneratedConnection> Connections = new List<StageGeneratedConnection>();
        public string ValidationHash;
        public int ErrorCount;
        public int EstimatedRoomMoves;
        public bool HasValidMainRoute;
    }
}

#endif
