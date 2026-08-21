#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using StarNight.Interaction.State;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.Bomb
{
    public interface IBombSpawnWorld
    {
        bool CanSpawn(Vector2 worldPosition, float cellSize, Collider2D[] playerColliders);
    }

    public sealed class PhysicsBombSpawnWorld : IBombSpawnWorld
    {
        public bool CanSpawn(Vector2 worldPosition, float cellSize, Collider2D[] playerColliders)
        {
            Vector2 size = Vector2.one * Mathf.Max(0.01f, cellSize) * 0.82f;
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(worldPosition, size, 0f);
            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider2D overlap = overlaps[index];
                if (overlap == null)
                {
                    continue;
                }

                int layer = overlap.gameObject.layer;
                if (layer == LayerMask.NameToLayer("UnbreakableBoundary"))
                {
                    return false;
                }

                MapElementInstance element = overlap.GetComponentInParent<MapElementInstance>();
                if (element != null
                    && element.Definition?.CommonProfile?.Kind == CommonElementKind.UnbreakableBlock)
                {
                    return false;
                }

                if (playerColliders == null)
                {
                    continue;
                }
                for (int playerIndex = 0; playerIndex < playerColliders.Length; playerIndex++)
                {
                    if (playerColliders[playerIndex] == overlap)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class BombActionController : MonoBehaviour, IPlayerBombActionExecutor
    {
        [SerializeField] private BombDefinition definition;
        [SerializeField] private BombRuntime bombPrefab;
        [SerializeField] private BombInventoryState inventory;
        [SerializeField] private BombExplosionDispatcher explosionDispatcher;
        [SerializeField] private PlayerActionLock actionLock;
        [SerializeField] private InteractionProbe interactionProbe;
        [SerializeField] private Transform playerFeet;
        [SerializeField] private Collider2D[] playerColliders;
        [SerializeField] private Vector2 gridOrigin;

        private IBombSpawnWorld spawnWorld;
        private int facingSign = 1;

        private void Awake()
        {
            ResolveDependencies();
        }

        public bool TryPlaceBomb(PlayerActionContext context)
        {
            ResolveDependencies();
            if (definition == null || bombPrefab == null || inventory == null
                || !inventory.HasBomb
                || actionLock != null && actionLock.State == PlayerActionState.RoomTransitionLocked)
            {
                return false;
            }

            if (interactionProbe != null)
            {
                facingSign = interactionProbe.FacingSign;
            }
            BombLaunchSolution launch = definition.ResolveLaunch(context, facingSign);
            Vector2 feetPosition = playerFeet != null ? playerFeet.position : transform.position;
            Vector2Int feetCell = WorldToCell(feetPosition, definition.CellSize);
            Vector2Int spawnCell = feetCell + Vector2Int.right * launch.HorizontalSign;
            Vector2 spawnPosition = gridOrigin + (Vector2)spawnCell * definition.CellSize;
            IBombSpawnWorld world = spawnWorld ?? new PhysicsBombSpawnWorld();
            if (!world.CanSpawn(spawnPosition, definition.CellSize, playerColliders))
            {
                return false;
            }

            PlayerActionState previousState = actionLock != null ? actionLock.State : PlayerActionState.Free;
            if (actionLock != null && !actionLock.TryAcquire(context.ActionId, PlayerActionState.Placing))
            {
                return false;
            }

            BombRuntime created = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
            bool armed = created != null && created.Arm(
                definition,
                gameObject,
                launch.Velocity,
                context.ActionId,
                explosionDispatcher);
            bool consumed = armed && inventory.TryConsume();
            if (!consumed && created != null)
            {
                Destroy(created.gameObject);
            }

            actionLock?.TryRelease(context.ActionId, previousState);
            return consumed;
        }

        public void ConfigureForTests(
            BombDefinition configuredDefinition,
            BombRuntime configuredPrefab,
            BombInventoryState configuredInventory,
            PlayerActionLock configuredLock,
            IBombSpawnWorld configuredWorld,
            Transform configuredFeet = null)
        {
            definition = configuredDefinition;
            bombPrefab = configuredPrefab;
            inventory = configuredInventory;
            actionLock = configuredLock;
            spawnWorld = configuredWorld;
            playerFeet = configuredFeet;
        }

        private void ResolveDependencies()
        {
            if (inventory == null)
            {
                inventory = GetComponent<BombInventoryState>();
            }
            if (actionLock == null)
            {
                actionLock = GetComponent<PlayerActionLock>();
            }
            if (interactionProbe == null)
            {
                interactionProbe = GetComponent<InteractionProbe>();
            }
            if (playerColliders == null || playerColliders.Length == 0)
            {
                playerColliders = GetComponentsInChildren<Collider2D>(true);
            }
        }

        private Vector2Int WorldToCell(Vector2 worldPosition, float cellSize)
        {
            Vector2 local = (worldPosition - gridOrigin) / Mathf.Max(0.01f, cellSize);
            return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
        }
    }
}

#endif
