#if LEGACY_DISABLED
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Maru.P8;
using StarNight.Tiles;
using StarNight.Tools.Pestle;
using StarNight.Tools.Rope;
using StarNight.Tools.Water;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DisallowMultipleComponent]
    public sealed class P12StageEnvironment2D : MonoBehaviour
    {
        [SerializeField] private P12StageId stageId;
        [SerializeField] private GameObject environmentRoot;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Transform entryAnchor;
        [SerializeField] private Transform exitAnchor;
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private Transform maruEntryAnchor;
        [SerializeField] private TileMutationService mutationService;
        [SerializeField] private ExplosionService2D explosionService;
        [SerializeField] private RopeInstaller2D ropeInstaller;
        [SerializeField] private WaterInteractionRegistry2D waterRegistry;
        [SerializeField] private PestleInteractionRegistry2D pestleRegistry;
        [SerializeField] private Transform spawnedConsumablesRoot;
        [SerializeField] private Transform installedRopesRoot;
        [SerializeField] private P8MaruRoomGraph2D maruRoomGraph;
        [SerializeField] private P8ReturnPile2D returnPile;

        public P12StageId StageId => stageId;
        public GameObject EnvironmentRoot => environmentRoot;
        public GridWorld GridWorld => gridWorld;
        public Transform EntryAnchor => entryAnchor;
        public Transform ExitAnchor => exitAnchor;
        public Transform CameraAnchor => cameraAnchor;
        public Transform MaruEntryAnchor => maruEntryAnchor;
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
        public P8MaruRoomGraph2D MaruRoomGraph => maruRoomGraph;
        public P8ReturnPile2D ReturnPile => returnPile;
        public bool MaruRoutingReady =>
            maruEntryAnchor != null
            && maruRoomGraph != null
            && maruRoomGraph.NodeCount > 0
            && returnPile != null;

        public void Configure(
            P12StageId id,
            GameObject root,
            GridWorld world,
            Transform entry,
            Transform exit,
            Transform cameraTarget,
            Transform maruEntry,
            TileMutationService mutations,
            ExplosionService2D explosions,
            RopeInstaller2D ropes,
            WaterInteractionRegistry2D water,
            PestleInteractionRegistry2D pestle,
            Transform spawnedConsumables,
            Transform installedRopes,
            P8MaruRoomGraph2D roomGraph,
            P8ReturnPile2D stageReturnPile)
        {
            stageId = id;
            environmentRoot = root;
            gridWorld = world;
            entryAnchor = entry;
            exitAnchor = exit;
            cameraAnchor = cameraTarget;
            maruEntryAnchor = maruEntry;
            mutationService = mutations;
            explosionService = explosions;
            ropeInstaller = ropes;
            waterRegistry = water;
            pestleRegistry = pestle;
            spawnedConsumablesRoot = spawnedConsumables;
            installedRopesRoot = installedRopes;
            maruRoomGraph = roomGraph;
            returnPile = stageReturnPile;
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
