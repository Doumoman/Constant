#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    public enum LayoutFamily
    {
        LinearBend,
        VerticalSpine,
        TwinBranchMerge,
        BrokenSpiral,
        HubAndSpokes,
    }

    [Serializable]
    public sealed class RoomRoleRequirement
    {
        public RoomRole Role;
        [Min(0)] public int MinCount = 1;
        [Min(0)] public int MaxCount = 1;
    }

    [Serializable]
    public sealed class RoomSizeWeights
    {
        [Min(0)] public int Micro = 4;
        [Min(0)] public int Wide = 4;
        [Min(0)] public int Tall = 3;
        [Min(0)] public int Large = 2;
        [Min(0)] public int LongHall = 1;
        [Min(0)] public int DeepShaft = 1;
        [Min(0)] public int Boss;

        public int GetWeight(Vector2Int size)
        {
            if (size == RoomSizeCatalog.Micro) return Micro;
            if (size == RoomSizeCatalog.Wide) return Wide;
            if (size == RoomSizeCatalog.Tall) return Tall;
            if (size == RoomSizeCatalog.Large) return Large;
            if (size == RoomSizeCatalog.LongHall) return LongHall;
            if (size == RoomSizeCatalog.DeepShaft) return DeepShaft;
            return Boss;
        }
    }

    [Serializable]
    public sealed class StageElementBudget
    {
        [Min(0)] public int Threat = 12;
        [Min(0)] public int Utility = 10;
        [Min(0)] public int Event = 3;
        [Min(0)] public int Shop = 1;
        [Min(0)] public int MaxSlotsPerRoom = 3;
    }

    [Serializable]
    public sealed class GuaranteedEventRule
    {
        public string EventId;
        public RoomRole TargetRole = RoomRole.Branch;
        [Min(0)] public int MinimumCount = 1;
    }

    [CreateAssetMenu(menuName = "Star Night/Stage Layout/Stage Map Profile", fileName = "StageMapProfile")]
    public sealed class StageMapProfile : ScriptableObject
    {
        public string StageId;
        [Min(2)] public int MinRooms = 6;
        [Min(2)] public int MaxRooms = 9;
        public Vector2Int MainRouteLengthRange = new Vector2Int(4, 6);
        public Vector2Int BranchCountRange = new Vector2Int(1, 3);
        public Vector2Int LoopCountRange = new Vector2Int(0, 1);
        public List<RoomRoleRequirement> RequiredRoles = new List<RoomRoleRequirement>();
        public RoomSizeWeights SizeWeights = new RoomSizeWeights();
        public List<LayoutFamily> AllowedFamilies = new List<LayoutFamily>();
        public StageElementBudget Budget = new StageElementBudget();
        public List<GuaranteedEventRule> GuaranteedEvents = new List<GuaranteedEventRule>();
    }
}

#endif
