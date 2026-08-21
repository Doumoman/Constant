#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Rooms
{
    [DisallowMultipleComponent]
    public sealed class RoomTemplate2D : MonoBehaviour
    {
        public static readonly Vector2Int MacroCellSize = new Vector2Int(12, 8);

        [Header("Identity")]
        [SerializeField] private string roomId = "Room";
        [SerializeField] private RoomArchetype archetype;
        [SerializeField] private RoomRegion region = RoomRegion.MoonPalace;
        [SerializeField] private RoomRole roles = RoomRole.Main;
        [SerializeField] private string coreActionSentence = "기본 이동으로 방을 통과한다.";

        [Header("Footprint")]
        [SerializeField] private Vector2Int macroSize = Vector2Int.one;
        [SerializeField] private Vector2Int logicalSize = new Vector2Int(12, 8);
        [SerializeField] private RoomValidationProfile validationProfile;

        [Header("Budgets and checks")]
        [SerializeField, Min(0)] private int hazardBudget;
        [SerializeField, Min(0)] private int enemyBudget;
        [SerializeField, Min(0)] private int introducedRuleCount = 1;
        [SerializeField, Min(0)] private int landmarkCount;
        [SerializeField, Min(1)] private int movementLayerCount = 1;
        [SerializeField, Min(0f)] private float expectedTraversalSeconds = 10f;
        [SerializeField] private bool rewardVisibleFromMainRoute;
        [SerializeField] private bool exitBlockProof = true;

        [Header("Layers")]
        [SerializeField] private UnityEngine.Grid grid;
        [SerializeField] private Tilemap terrain;
        [SerializeField] private Tilemap oneWay;
        [SerializeField] private Tilemap fixture;
        [SerializeField] private Tilemap hazard;
        [SerializeField] private Tilemap decoration;
        [SerializeField] private Tilemap logic;
        [SerializeField] private RoomSocket2D[] sockets = Array.Empty<RoomSocket2D>();
        [SerializeField] private RoomContentSlot2D[] contentSlots =
            Array.Empty<RoomContentSlot2D>();

        public string RoomId => roomId;
        public RoomArchetype Archetype => archetype;
        public RoomRegion Region => region;
        public RoomRole Roles => roles;
        public string CoreActionSentence => coreActionSentence;
        public Vector2Int MacroSize => macroSize;
        public Vector2Int LogicalSize => logicalSize;
        public RoomValidationProfile ValidationProfile => validationProfile;
        public int HazardBudget => hazardBudget;
        public int EnemyBudget => enemyBudget;
        public int IntroducedRuleCount => introducedRuleCount;
        public int LandmarkCount => landmarkCount;
        public int MovementLayerCount => movementLayerCount;
        public float ExpectedTraversalSeconds => expectedTraversalSeconds;
        public bool RewardVisibleFromMainRoute => rewardVisibleFromMainRoute;
        public bool ExitBlockProof => exitBlockProof;
        public UnityEngine.Grid Grid => grid;
        public Tilemap Terrain => terrain;
        public Tilemap OneWay => oneWay;
        public Tilemap Fixture => fixture;
        public Tilemap Hazard => hazard;
        public Tilemap Decoration => decoration;
        public Tilemap Logic => logic;
        public IReadOnlyList<RoomSocket2D> Sockets => sockets;
        public IReadOnlyList<RoomContentSlot2D> ContentSlots => contentSlots;
        public RectInt CellBounds => new RectInt(Vector2Int.zero, logicalSize);

        public void Configure(
            string id,
            RoomArchetype roomArchetype,
            RoomRegion roomRegion,
            RoomRole roomRoles,
            string coreAction,
            Vector2Int roomMacroSize,
            Vector2Int roomLogicalSize,
            RoomValidationProfile profile,
            int roomHazardBudget,
            int roomEnemyBudget,
            int newRuleCount,
            int roomLandmarkCount,
            int roomMovementLayerCount,
            float traversalSeconds,
            bool rewardPreVisible,
            bool cannotBlockExit,
            UnityEngine.Grid roomGrid,
            Tilemap terrainLayer,
            Tilemap oneWayLayer,
            Tilemap fixtureLayer,
            Tilemap hazardLayer,
            Tilemap decorationLayer,
            Tilemap logicLayer,
            RoomSocket2D[] roomSockets,
            RoomContentSlot2D[] slots)
        {
            roomId = id;
            archetype = roomArchetype;
            region = roomRegion;
            roles = roomRoles;
            coreActionSentence = coreAction;
            macroSize = roomMacroSize;
            logicalSize = roomLogicalSize;
            validationProfile = profile;
            hazardBudget = Mathf.Max(0, roomHazardBudget);
            enemyBudget = Mathf.Max(0, roomEnemyBudget);
            introducedRuleCount = Mathf.Max(0, newRuleCount);
            landmarkCount = Mathf.Max(0, roomLandmarkCount);
            movementLayerCount = Mathf.Max(1, roomMovementLayerCount);
            expectedTraversalSeconds = Mathf.Max(0f, traversalSeconds);
            rewardVisibleFromMainRoute = rewardPreVisible;
            exitBlockProof = cannotBlockExit;
            grid = roomGrid;
            terrain = terrainLayer;
            oneWay = oneWayLayer;
            fixture = fixtureLayer;
            hazard = hazardLayer;
            decoration = decorationLayer;
            logic = logicLayer;
            sockets = roomSockets ?? Array.Empty<RoomSocket2D>();
            contentSlots = slots ?? Array.Empty<RoomContentSlot2D>();
        }

        public bool HasRole(RoomRole role)
        {
            return (roles & role) == role;
        }

        public int CountSlots(RoomContentSlotKind kind)
        {
            int count = 0;
            for (int index = 0; index < contentSlots.Length; index++)
            {
                if (contentSlots[index] != null && contentSlots[index].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        public IEnumerable<RoomContentSlot2D> EnumerateSlots(RoomContentSlotKind kind)
        {
            for (int index = 0; index < contentSlots.Length; index++)
            {
                RoomContentSlot2D slot = contentSlots[index];
                if (slot != null && slot.Kind == kind)
                {
                    yield return slot;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.35f, 0.8f, 1f, 0.45f);
            Vector3 center = transform.TransformPoint(
                new Vector3(logicalSize.x * 0.5f, logicalSize.y * 0.5f, 0f));
            Gizmos.DrawWireCube(
                center,
                new Vector3(logicalSize.x, logicalSize.y, 0.1f));
        }
    }
}

#endif
