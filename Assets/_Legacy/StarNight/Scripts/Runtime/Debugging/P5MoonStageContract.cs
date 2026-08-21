#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Rooms;
using StarNight.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Debugging
{
    [Serializable]
    public struct P5FixedRoomPlacement
    {
        [SerializeField] private string roomId;
        [SerializeField] private Vector2Int origin;
        [SerializeField] private Vector2Int size;
        [SerializeField] private RoomTemplate2D instance;

        public string RoomId => roomId;
        public Vector2Int Origin => origin;
        public Vector2Int Size => size;
        public RoomTemplate2D Instance => instance;

        public P5FixedRoomPlacement(
            string id,
            Vector2Int roomOrigin,
            Vector2Int roomSize,
            RoomTemplate2D roomInstance)
        {
            roomId = id;
            origin = roomOrigin;
            size = roomSize;
            instance = roomInstance;
        }
    }

    [DisallowMultipleComponent]
    public sealed class P5MoonStageContract : MonoBehaviour
    {
        public const string LayoutId =
            "P5_MoonPalace_1-1_CraterWorkshop_Fixed_v1";
        public const int Width = 102;
        public const int Height = 18;
        public const int RequiredRoomCount = 7;
        public const int RequiredGuaranteedGoldCount = 4;
        public const int RequiredShopPedestalCount = 3;
        public const int RequiredBellCueCount = 3;
        public const int NoProceduralSeed = -1;
        public const float ExitHoldSeconds = 0.5f;
        public const float FirstBellSeconds = 140f;
        public const float SecondBellSeconds = 185f;
        public const float ArrivalBellSeconds = 215f;

        public static readonly Vector2Int StartCell = new Vector2Int(2, 1);
        public static readonly Vector2Int ExitSafeCell =
            new Vector2Int(100, 1);
        public static readonly Vector2Int ExitFrameCell =
            new Vector2Int(101, 1);
        public static readonly Vector2Int RabbitCell =
            new Vector2Int(51, 1);
        public static readonly Vector2Int MortarCell =
            new Vector2Int(54, 2);
        public static readonly Vector2Int PestleSilhouetteCell =
            new Vector2Int(54, 3);
        public static readonly Vector2Int StoryPestleCell =
            new Vector2Int(86, 4);
        public static readonly Vector2Int MoonCakeRewardCell =
            new Vector2Int(56, 1);

        public static readonly Vector2 PlayerSpawn =
            new Vector2(2.5f, 1.45f);

        public static readonly Vector2Int[] GuaranteedGoldCells =
        {
            new Vector2Int(7, 1),
            new Vector2Int(16, 1),
            new Vector2Int(21, 1),
            new Vector2Int(29, 1)
        };

        public static readonly string[] RequiredRoomIds =
        {
            "P4_Moon_Small_CrescentStep_01",
            "P4_Moon_Small_SoftSoilDip_01",
            "P4_Moon_Wide_RollingDoughLane_01",
            "P4_Moon_Small_PestlePost_01",
            "P4_Moon_Corridor_MomoShop_01",
            "P4_Moon_Small_CrackedShelf_01",
            "P4_Moon_Standard_ThreeBeatSteps_01"
        };

        public static readonly Vector2Int[] RequiredRoomOrigins =
        {
            new Vector2Int(0, 0),
            new Vector2Int(12, 0),
            new Vector2Int(24, 0),
            new Vector2Int(48, 0),
            new Vector2Int(60, 0),
            new Vector2Int(78, 0),
            new Vector2Int(90, 0)
        };

        public static readonly Vector2Int[] RequiredRoomSizes =
        {
            new Vector2Int(12, 8),
            new Vector2Int(12, 8),
            new Vector2Int(24, 8),
            new Vector2Int(12, 8),
            new Vector2Int(18, 5),
            new Vector2Int(12, 8),
            new Vector2Int(12, 8)
        };

        [Header("Fixed handmade layout")]
        [SerializeField] private string layoutId = LayoutId;
        [SerializeField] private bool fixedHandmadeLayout = true;
        [SerializeField] private int proceduralSeed = NoProceduralSeed;
        [SerializeField] private P5FixedRoomPlacement[] roomPlacements =
            Array.Empty<P5FixedRoomPlacement>();

        [Header("Global 1 x 1 world")]
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private GridLayout gridLayout;
        [SerializeField] private Tilemap terrain;
        [SerializeField] private Tilemap oneWay;
        [SerializeField] private Tilemap fixture;
        [SerializeField] private Tilemap hazard;
        [SerializeField] private Tilemap decoration;
        [SerializeField] private Tilemap logic;

        [Header("Core route")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform entry;
        [SerializeField] private Transform exit;
        [SerializeField] private Transform optionalReward;
        [SerializeField] private Transform rabbitReturn;

        [Header("Moon rabbit event")]
        [SerializeField] private Transform moonRabbit;
        [SerializeField] private Transform emptyMortar;
        [SerializeField] private Transform pestleSilhouette;
        [SerializeField] private Transform storyPestle;
        [SerializeField] private Transform moonCakeReward;

        [Header("Economy and pressure")]
        [SerializeField] private Transform[] guaranteedGold =
            Array.Empty<Transform>();
        [SerializeField] private Transform[] shopPedestals =
            Array.Empty<Transform>();
        [SerializeField] private Transform[] bellCues =
            Array.Empty<Transform>();

        [Header("Guidance and presentation")]
        [SerializeField] private Transform globalMoonBackdrop;
        [SerializeField] private Transform entryGuidance;
        [SerializeField] private Transform firstReachBacktrackCue;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Light directionalLight;

        [Header("Runtime services")]
        [SerializeField] private MonoBehaviour coreLoop;
        [SerializeField] private MonoBehaviour runState;
        [SerializeField] private MonoBehaviour rabbitEvent;
        [SerializeField] private MonoBehaviour momoShop;
        [SerializeField] private MonoBehaviour stageExit;
        [SerializeField] private MonoBehaviour bellClock;
        [SerializeField] private MonoBehaviour telemetry;

        [Header("Validation")]
        [SerializeField, TextArea(3, 14)] private string lastValidation =
            "Not validated.";

        public string FixedLayoutId => layoutId;
        public bool IsFixedHandmadeLayout => fixedHandmadeLayout;
        public int ProceduralSeed => proceduralSeed;
        public IReadOnlyList<P5FixedRoomPlacement> RoomPlacements =>
            roomPlacements;
        public GridWorld GridWorld => gridWorld;
        public GridLayout GridLayout => gridLayout;
        public Tilemap Terrain => terrain;
        public Tilemap OneWay => oneWay;
        public Tilemap Fixture => fixture;
        public Tilemap Hazard => hazard;
        public Tilemap Decoration => decoration;
        public Tilemap Logic => logic;
        public Transform Player => player;
        public Transform Entry => entry;
        public Transform Exit => exit;
        public Transform OptionalReward => optionalReward;
        public Transform RabbitReturn => rabbitReturn;
        public Transform MoonRabbit => moonRabbit;
        public Transform EmptyMortar => emptyMortar;
        public Transform PestleSilhouette => pestleSilhouette;
        public Transform StoryPestle => storyPestle;
        public Transform MoonCakeReward => moonCakeReward;
        public IReadOnlyList<Transform> GuaranteedGold => guaranteedGold;
        public IReadOnlyList<Transform> ShopPedestals => shopPedestals;
        public IReadOnlyList<Transform> BellCues => bellCues;
        public Transform GlobalMoonBackdrop => globalMoonBackdrop;
        public Transform EntryGuidance => entryGuidance;
        public Transform FirstReachBacktrackCue => firstReachBacktrackCue;
        public Camera StageCamera => stageCamera;
        public Light DirectionalLight => directionalLight;
        public MonoBehaviour CoreLoop => coreLoop;
        public MonoBehaviour RunState => runState;
        public MonoBehaviour RabbitEvent => rabbitEvent;
        public MonoBehaviour MomoShop => momoShop;
        public MonoBehaviour StageExit => stageExit;
        public MonoBehaviour BellClock => bellClock;
        public MonoBehaviour Telemetry => telemetry;
        public string LastValidation => lastValidation;
        public bool ValidationPassed => lastValidation == "PASS";

        public void ConfigureLayout(
            P5FixedRoomPlacement[] fixedRooms,
            GridWorld world,
            GridLayout layout,
            Tilemap terrainLayer,
            Tilemap oneWayLayer,
            Tilemap fixtureLayer,
            Tilemap hazardLayer,
            Tilemap decorationLayer,
            Tilemap logicLayer)
        {
            layoutId = LayoutId;
            fixedHandmadeLayout = true;
            proceduralSeed = NoProceduralSeed;
            roomPlacements = fixedRooms ?? Array.Empty<P5FixedRoomPlacement>();
            gridWorld = world;
            gridLayout = layout;
            terrain = terrainLayer;
            oneWay = oneWayLayer;
            fixture = fixtureLayer;
            hazard = hazardLayer;
            decoration = decorationLayer;
            logic = logicLayer;
        }

        public void ConfigureContent(
            Transform playerTransform,
            Transform entryTransform,
            Transform exitTransform,
            Transform optionalRewardTransform,
            Transform rabbitReturnTransform,
            Transform rabbitTransform,
            Transform mortarTransform,
            Transform silhouetteTransform,
            Transform storyPestleTransform,
            Transform moonCakeTransform,
            Transform[] gold,
            Transform[] pedestals,
            Transform[] bells)
        {
            player = playerTransform;
            entry = entryTransform;
            exit = exitTransform;
            optionalReward = optionalRewardTransform;
            rabbitReturn = rabbitReturnTransform;
            moonRabbit = rabbitTransform;
            emptyMortar = mortarTransform;
            pestleSilhouette = silhouetteTransform;
            storyPestle = storyPestleTransform;
            moonCakeReward = moonCakeTransform;
            guaranteedGold = gold ?? Array.Empty<Transform>();
            shopPedestals = pedestals ?? Array.Empty<Transform>();
            bellCues = bells ?? Array.Empty<Transform>();
        }

        public void ConfigurePresentation(
            Transform backdrop,
            Transform guidance,
            Transform backtrackCue,
            Camera cameraComponent,
            Light mainLight)
        {
            globalMoonBackdrop = backdrop;
            entryGuidance = guidance;
            firstReachBacktrackCue = backtrackCue;
            stageCamera = cameraComponent;
            directionalLight = mainLight;
        }

        public void ConfigureRuntime(
            MonoBehaviour stageCoreLoop,
            MonoBehaviour stageRunState,
            MonoBehaviour localRabbitEvent,
            MonoBehaviour shop,
            MonoBehaviour exitController,
            MonoBehaviour maruBellClock,
            MonoBehaviour sliceTelemetry)
        {
            coreLoop = stageCoreLoop;
            runState = stageRunState;
            rabbitEvent = localRabbitEvent;
            momoShop = shop;
            stageExit = exitController;
            bellClock = maruBellClock;
            telemetry = sliceTelemetry;
        }

        [ContextMenu("Validate P5 Moon Palace 1-1")]
        public bool RefreshValidation()
        {
            List<string> issues = new List<string>();
            ValidateIdentity(issues);
            ValidateWorld(issues);
            ValidateRooms(issues);
            ValidateRoute(issues);
            ValidateContent(issues);
            ValidatePresentation(issues);
            ValidateRuntime(issues);
            lastValidation = issues.Count == 0
                ? "PASS"
                : string.Join("\n", issues);
            return issues.Count == 0;
        }

        private void ValidateIdentity(List<string> issues)
        {
            if (!fixedHandmadeLayout
                || proceduralSeed != NoProceduralSeed
                || layoutId != LayoutId)
            {
                issues.Add(
                    "Stage must use the approved fixed handmade layout with no seed.");
            }
        }

        private void ValidateWorld(List<string> issues)
        {
            if (gridWorld == null
                || gridLayout == null
                || terrain == null
                || oneWay == null
                || fixture == null
                || hazard == null
                || decoration == null
                || logic == null)
            {
                issues.Add("Global GridWorld and all six Tilemap layers are required.");
                return;
            }

            if (gridWorld.Origin != Vector2Int.zero
                || gridWorld.Size != new Vector2Int(Width, Height))
            {
                issues.Add($"GridWorld must be exactly {Width} x {Height} cells.");
            }

            Transform expectedParent = gridLayout.transform;
            Tilemap[] layers =
            {
                terrain,
                oneWay,
                fixture,
                hazard,
                decoration,
                logic
            };
            for (int index = 0; index < layers.Length; index++)
            {
                if (layers[index].transform.parent != expectedParent)
                {
                    issues.Add("All P5 Tilemaps must belong to one global Grid.");
                    break;
                }
            }

            TilemapCollider2D oneWayCollider =
                oneWay.GetComponent<TilemapCollider2D>();
            PlatformEffector2D effector =
                oneWay.GetComponent<PlatformEffector2D>();
            if (oneWayCollider == null
                || !oneWayCollider.usedByEffector
                || effector == null
                || !effector.useOneWay)
            {
                issues.Add(
                    "Global OneWay must use TilemapCollider2D.usedByEffector and PlatformEffector2D.useOneWay.");
            }

            if (terrain.GetUsedTilesCount() == 0
                || oneWay.GetUsedTilesCount() == 0)
            {
                issues.Add("The P4 room sequence was not baked into global Tilemaps.");
            }
        }

        private void ValidateRooms(List<string> issues)
        {
            if (roomPlacements == null
                || roomPlacements.Length != RequiredRoomCount)
            {
                issues.Add($"Exactly {RequiredRoomCount} fixed room placements are required.");
                return;
            }

            for (int index = 0; index < RequiredRoomCount; index++)
            {
                P5FixedRoomPlacement placement = roomPlacements[index];
                if (placement.RoomId != RequiredRoomIds[index]
                    || placement.Origin != RequiredRoomOrigins[index]
                    || placement.Size != RequiredRoomSizes[index]
                    || placement.Instance == null)
                {
                    issues.Add($"Fixed room placement {index:00} does not match the P5 layout contract.");
                }
            }
        }

        private void ValidateRoute(List<string> issues)
        {
            if (terrain == null || oneWay == null)
            {
                return;
            }

            RectInt bounds = new RectInt(Vector2Int.zero, new Vector2Int(Width, Height));
            bool IsSolid(GridPos cell)
            {
                Vector3Int position = new Vector3Int(cell.X, cell.Y, 0);
                return terrain.HasTile(position) || oneWay.HasTile(position);
            }

            GridPos start = new GridPos(StartCell.x, StartCell.y);
            GridPos stageExitCell =
                new GridPos(ExitSafeCell.x, ExitSafeCell.y);
            GridPos reward =
                new GridPos(StoryPestleCell.x, StoryPestleCell.y);
            GridPos rabbit =
                new GridPos(RabbitCell.x, RabbitCell.y);

            if (!GridReachabilityValidator.CanReach(
                    bounds,
                    start,
                    stageExitCell,
                    IsSolid))
            {
                issues.Add("Movement-and-jump Entry to Exit route is not reachable.");
            }

            if (!GridReachabilityValidator.CanReach(
                    bounds,
                    stageExitCell,
                    reward,
                    IsSolid)
                || !GridReachabilityValidator.CanReach(
                    bounds,
                    reward,
                    rabbit,
                    IsSolid))
            {
                issues.Add("Optional pestle backtrack route is not returnable.");
            }
        }

        private void ValidateContent(List<string> issues)
        {
            if (player == null
                || player.GetComponent<PlayerMotor2D>() == null
                || entry == null
                || exit == null
                || optionalReward == null
                || rabbitReturn == null)
            {
                issues.Add("Player, Entry, Exit, and optional-return route anchors are required.");
            }

            if (moonRabbit == null
                || emptyMortar == null
                || pestleSilhouette == null
                || storyPestle == null
                || moonCakeReward == null)
            {
                issues.Add("The complete Moon Rabbit pestle event hierarchy is required.");
            }
            else if (!HaveSameSpriteSignature(pestleSilhouette, storyPestle))
            {
                issues.Add("Pestle silhouette must use the exact story-pestle sprite signature.");
            }

            if (guaranteedGold == null
                || guaranteedGold.Length != RequiredGuaranteedGoldCount)
            {
                issues.Add(
                    $"Exactly {RequiredGuaranteedGoldCount} guaranteed gold pickups are required.");
            }

            if (shopPedestals == null
                || shopPedestals.Length != RequiredShopPedestalCount)
            {
                issues.Add(
                    $"Exactly {RequiredShopPedestalCount} physical shop pedestals are required.");
            }

            if (bellCues == null || bellCues.Length != RequiredBellCueCount)
            {
                issues.Add(
                    $"Exactly {RequiredBellCueCount} Maru bell visual tiers are required.");
            }
        }

        private void ValidatePresentation(List<string> issues)
        {
            if (globalMoonBackdrop == null
                || entryGuidance == null
                || firstReachBacktrackCue == null)
            {
                issues.Add(
                    "Global Moon backdrop, entry guidance, and first-reach backtrack cue are required.");
            }

            if (stageCamera == null
                || !stageCamera.orthographic
                || !stageCamera.CompareTag("MainCamera"))
            {
                issues.Add("An orthographic Main Camera is required.");
            }

            if (directionalLight == null
                || directionalLight.type != LightType.Directional)
            {
                issues.Add("A main Directional Light is required.");
            }
        }

        private void ValidateRuntime(List<string> issues)
        {
            if (coreLoop == null
                || runState == null
                || rabbitEvent == null
                || momoShop == null
                || stageExit == null
                || bellClock == null
                || telemetry == null)
            {
                issues.Add("All P5 core-loop runtime services must be connected.");
            }
        }

        private static bool HaveSameSpriteSignature(
            Transform first,
            Transform second)
        {
            SpriteRenderer[] firstRenderers =
                first.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer[] secondRenderers =
                second.GetComponentsInChildren<SpriteRenderer>(true);
            List<Sprite> firstSprites = CollectSprites(firstRenderers);
            List<Sprite> secondSprites = CollectSprites(secondRenderers);
            if (firstSprites.Count == 0
                || firstSprites.Count != secondSprites.Count)
            {
                return false;
            }

            for (int index = 0; index < firstSprites.Count; index++)
            {
                if (firstSprites[index] != secondSprites[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static List<Sprite> CollectSprites(SpriteRenderer[] renderers)
        {
            List<Sprite> sprites = new List<Sprite>(renderers.Length);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].sprite != null)
                {
                    sprites.Add(renderers[index].sprite);
                }
            }

            sprites.Sort(
                (left, right) =>
                    string.CompareOrdinal(left.name, right.name));
            return sprites;
        }
    }
}

#endif
