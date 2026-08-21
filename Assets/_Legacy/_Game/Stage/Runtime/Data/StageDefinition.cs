#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stage.Data
{
    public enum StageKind
    {
        Introduction,
        Exploration,
        Boss,
        Challenge,
    }

    public enum GenerationMode
    {
        Fixed,
        Procedural,
    }

    public enum ConnectionCondition
    {
        Always,
        RequiredFlag,
        RequiredItem,
        BossResolved,
    }

    [Serializable]
    public sealed class StageConnection
    {
        public string connectionId;
        public StageDefinition target;
        public ConnectionCondition condition;
        public string requiredFlag;
        public string requiredItem;
        public bool visibleWhenLocked;
    }

    [CreateAssetMenu(menuName = "Star Night/Stage Definition", fileName = "StageDefinition")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string stageId;
        public string displayNameKey;
        public string objectiveKey;
        public string sceneName;
        public string regionId;
        public StageKind kind;
        public GenerationMode generationMode;
        [Min(1)] public int minRooms = 2;
        [Min(1)] public int maxRooms = 2;
        [Min(0f)] public float bell1Time = 120f;
        [Min(0f)] public float bell2Time = 165f;
        [Min(0f)] public float maruSpawnTime = 195f;
        public string startRoomRole;
        public string exitRoomRole;
        public string[] requiredRoomRoles = Array.Empty<string>();
        public string[] optionalRoomRoles = Array.Empty<string>();
        [Min(0)] public int mainPathEdgeMin;
        [Min(0)] public int mainPathEdgeMax;
        public bool allowShop;
        public bool allowReturnStatue;
        public bool allowRecordCasket;
        public string nextStageId;
        public string introYarnNode;
        public string exitYarnNode;
        public StageConnection[] connections = Array.Empty<StageConnection>();
        public RegionArtProfile artProfile;

        public bool HasScene => !string.IsNullOrWhiteSpace(sceneName);
    }
}

#endif
