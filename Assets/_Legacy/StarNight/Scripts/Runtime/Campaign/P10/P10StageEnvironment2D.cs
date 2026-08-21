#if LEGACY_DISABLED
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Tiles;
using StarNight.Tools.Pestle;
using StarNight.Tools.Rope;
using StarNight.Tools.Water;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10StageEnvironment2D : MonoBehaviour
    {
        [SerializeField] private P10StageId stageId;
        [SerializeField] private GameObject environmentRoot;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Transform entryAnchor;
        [SerializeField] private Transform exitAnchor;
        [SerializeField] private Transform maruAnchor;
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private TileMutationService mutationService;
        [SerializeField] private ExplosionService2D explosionService;
        [SerializeField] private RopeInstaller2D ropeInstaller;
        [SerializeField] private WaterInteractionRegistry2D waterRegistry;
        [SerializeField] private PestleInteractionRegistry2D pestleRegistry;
        [SerializeField] private Transform spawnedConsumablesRoot;
        [SerializeField] private Transform installedRopesRoot;
        [SerializeField] private Transform[] themeMotifs =
            System.Array.Empty<Transform>();

        public P10StageId StageId => stageId;
        public GameObject EnvironmentRoot => environmentRoot;
        public GridWorld GridWorld => gridWorld;
        public Transform EntryAnchor => entryAnchor;
        public Transform ExitAnchor => exitAnchor;
        public Transform MaruAnchor =>
            maruAnchor != null
                ? maruAnchor
                : exitAnchor != null
                    ? exitAnchor
                    : entryAnchor;
        public Transform CameraAnchor => cameraAnchor;
        public TileMutationService MutationService =>
            mutationService;
        public ExplosionService2D ExplosionService =>
            explosionService;
        public RopeInstaller2D RopeInstaller => ropeInstaller;
        public WaterInteractionRegistry2D WaterRegistry =>
            waterRegistry;
        public PestleInteractionRegistry2D PestleRegistry =>
            pestleRegistry;
        public Transform SpawnedConsumablesRoot =>
            spawnedConsumablesRoot;
        public Transform InstalledRopesRoot => installedRopesRoot;
        public int ThemeMotifCount => themeMotifs != null
            ? themeMotifs.Length
            : 0;
        public bool ThemePresentationReady =>
            environmentRoot != null
            && gridWorld != null
            && entryAnchor != null
            && exitAnchor != null
            && cameraAnchor != null
            && ThemeMotifCount >= 3;
        public int TraversalForceZoneCount =>
            environmentRoot != null
                ? environmentRoot.GetComponentsInChildren<
                    P10TraversalForceZone2D>(true).Length
                : 0;
        public int SwayingPlatformCount =>
            environmentRoot != null
                ? environmentRoot.GetComponentsInChildren<
                    P10SwayingPlatform2D>(true).Length
                : 0;
        public int TimedPlatformCount =>
            environmentRoot != null
                ? environmentRoot.GetComponentsInChildren<
                    P10TimedPlatform2D>(true).Length
                : 0;
        public int FloodgateCount =>
            environmentRoot != null
                ? environmentRoot.GetComponentsInChildren<
                    P10Floodgate2D>(true).Length
                : 0;
        public bool MagpieTraversalReady =>
            TraversalForceZoneCount >= 2
            && SwayingPlatformCount >= 1
            && TimedPlatformCount >= 2;
        public bool DragonTraversalReady =>
            TraversalForceZoneCount >= 2
            && FloodgateCount >= 2;

        public void Configure(
            P10StageId id,
            GameObject root,
            GridWorld world,
            Transform entry,
            Transform exit,
            Transform maruSpawn,
            Transform cameraTarget,
            TileMutationService mutations,
            ExplosionService2D explosions,
            RopeInstaller2D ropes,
            WaterInteractionRegistry2D water,
            PestleInteractionRegistry2D pestle,
            Transform spawnedConsumables,
            Transform installedRopes,
            Transform[] motifs)
        {
            stageId = id;
            environmentRoot = root;
            gridWorld = world;
            entryAnchor = entry;
            exitAnchor = exit;
            maruAnchor = maruSpawn;
            cameraAnchor = cameraTarget;
            mutationService = mutations;
            explosionService = explosions;
            ropeInstaller = ropes;
            waterRegistry = water;
            pestleRegistry = pestle;
            spawnedConsumablesRoot = spawnedConsumables;
            installedRopesRoot = installedRopes;
            themeMotifs = motifs ?? System.Array.Empty<Transform>();
        }

        public void SetEnvironmentActive(bool active)
        {
            if (environmentRoot != null)
            {
                environmentRoot.SetActive(active);
            }
        }
    }
}

#endif
