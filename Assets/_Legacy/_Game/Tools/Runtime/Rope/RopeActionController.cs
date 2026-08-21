#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using StarNight.Interaction.State;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeActionController : MonoBehaviour, IPlayerRopeActionExecutor
    {
        [SerializeField] private RopeDefinition definition;
        [SerializeField] private RopeInventoryState inventory;
        [SerializeField] private RopeInstallationRuntime installationPrefab;
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField] private GameObject ceilingAnchorPrefab;
        [SerializeField] private GameObject starKnotPrefab;
        [SerializeField] private PlayerActionLock actionLock;
        [SerializeField] private ProjectPhysicsProfile physicsProfile;
        [SerializeField] private Transform playerHead;
        [SerializeField] private RectInt roomBounds;
        [SerializeField] private Vector2 gridOrigin;

        private readonly RopePlacementResolver resolver = new RopePlacementResolver();
        private IRopePlacementWorld placementWorldOverride;

        private void Awake() => ResolveDependencies();

        public bool TryPlaceRope(PlayerActionContext context)
        {
            ResolveDependencies();
            if (definition == null || inventory == null || installationPrefab == null || segmentPrefab == null
                || actionLock != null && actionLock.State == PlayerActionState.RoomTransitionLocked)
            {
                return false;
            }

            Vector2 headWorld = playerHead != null ? playerHead.position : (Vector2)transform.position + Vector2.up;
            Vector2Int headCell = WorldToCell(headWorld);
            if (RopeInstallationRegistry.FindInColumn(headCell.x, roomBounds) != null)
            {
                return true;
            }
            if (!inventory.HasRope)
            {
                return false;
            }

            IRopePlacementWorld world = placementWorldOverride
                ?? new PhysicsRopePlacementWorld(roomBounds, gridOrigin, definition.CellSize, physicsProfile);
            if (!resolver.TryResolve(headCell, definition, world, out RopePlacementPlan plan, out _))
            {
                return false;
            }

            PlayerActionState previousState = actionLock != null ? actionLock.State : PlayerActionState.Free;
            if (actionLock != null && !actionLock.TryAcquire(context.ActionId, PlayerActionState.Placing))
            {
                return false;
            }

            RopeInstallationRuntime created = Instantiate(installationPrefab);
            bool initialized = created != null && created.Initialize(
                definition,
                plan,
                segmentPrefab,
                ceilingAnchorPrefab,
                starKnotPrefab,
                gridOrigin,
                context.ActionId);
            bool consumed = initialized && inventory.TryConsume();
            if (!consumed && created != null)
            {
                Destroy(created.gameObject);
            }
            actionLock?.TryRelease(context.ActionId, previousState);
            return consumed;
        }

        public void ConfigureForTests(
            RopeDefinition configuredDefinition,
            RopeInventoryState configuredInventory,
            RopeInstallationRuntime configuredInstallationPrefab,
            GameObject configuredSegmentPrefab,
            PlayerActionLock configuredLock,
            IRopePlacementWorld configuredWorld,
            Transform configuredHead = null)
        {
            definition = configuredDefinition;
            inventory = configuredInventory;
            installationPrefab = configuredInstallationPrefab;
            segmentPrefab = configuredSegmentPrefab;
            actionLock = configuredLock;
            placementWorldOverride = configuredWorld;
            playerHead = configuredHead;
        }

        private void ResolveDependencies()
        {
            if (inventory == null)
            {
                inventory = GetComponent<RopeInventoryState>();
            }
            if (actionLock == null)
            {
                actionLock = GetComponent<PlayerActionLock>();
            }
        }

        private Vector2Int WorldToCell(Vector2 world)
        {
            Vector2 local = (world - gridOrigin) / Mathf.Max(0.01f, definition.CellSize);
            return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
        }
    }
}

#endif
