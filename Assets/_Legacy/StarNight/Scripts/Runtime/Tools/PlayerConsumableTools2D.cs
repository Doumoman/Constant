#if LEGACY_DISABLED
using System;
using StarNight.Debugging;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Tools.Rope;
using UnityEngine;

namespace StarNight.Tools
{
    [DefaultExecutionOrder(-25)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class PlayerConsumableTools2D : MonoBehaviour
    {
        public const int DefaultRopeStock = 4;
        public const int DefaultBombStock = 4;

        [SerializeField] private PlayerInputAdapter input;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private RopeInstaller2D ropeInstaller;
        [SerializeField] private ExplosionService2D explosionService;
        [SerializeField] private Bomb2D bombPrefab;
        [SerializeField] private Transform spawnedBombParent;
        [SerializeField] private P3ToolDiscoveryTelemetry telemetry;
        [SerializeField, Range(0, 99)] private int ropeStock = DefaultRopeStock;
        [SerializeField, Range(0, 99)] private int bombStock = DefaultBombStock;
        [SerializeField, Min(0f)] private float bombHorizontalOffset = 0.85f;

        private float facingDirection = 1f;

        public event Action<int> RopeStockChanged;
        public event Action<int> BombStockChanged;
        public event Action<RopeInstallation2D> RopeInstalled;
        public event Action<Bomb2D> BombPlaced;

        public int RopeStock => ropeStock;
        public int BombStock => bombStock;

        public void Configure(
            PlayerInputAdapter inputAdapter,
            Rigidbody2D targetPlayerBody,
            GridWorld world,
            RopeInstaller2D targetRopeInstaller,
            ExplosionService2D targetExplosionService,
            Bomb2D targetBombPrefab,
            Transform targetBombParent,
            P3ToolDiscoveryTelemetry targetTelemetry,
            int initialRopes = DefaultRopeStock,
            int initialBombs = DefaultBombStock)
        {
            input = inputAdapter;
            playerBody = targetPlayerBody;
            gridWorld = world;
            ropeInstaller = targetRopeInstaller;
            explosionService = targetExplosionService;
            bombPrefab = targetBombPrefab;
            spawnedBombParent = targetBombParent;
            telemetry = targetTelemetry;
            SetRopeStock(initialRopes);
            SetBombStock(initialBombs);
        }

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputAdapter>();
            }

            if (playerBody == null)
            {
                playerBody = GetComponent<Rigidbody2D>();
            }

            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (ropeInstaller == null)
            {
                ropeInstaller = FindFirstObjectByType<RopeInstaller2D>();
            }

            if (explosionService == null)
            {
                explosionService = FindFirstObjectByType<ExplosionService2D>();
            }

            if (telemetry == null)
            {
                telemetry = FindFirstObjectByType<P3ToolDiscoveryTelemetry>();
            }
        }

        private void Start()
        {
            telemetry?.MarkSeen(P3ToolKind.Rope);
            telemetry?.MarkSeen(P3ToolKind.Bomb);
        }

        private void Update()
        {
            if (input == null)
            {
                return;
            }

            if (Mathf.Abs(input.Move.x) > 0.01f)
            {
                facingDirection = Mathf.Sign(input.Move.x);
            }

            if (input.ConsumeUseRopePressed())
            {
                TryUseRope();
            }

            if (input.ConsumeUseBombPressed())
            {
                TryUseBomb();
            }
        }

        public bool TryUseRope()
        {
            telemetry?.MarkUse(P3ToolKind.Rope);
            if (ropeStock <= 0 || ropeInstaller == null || gridWorld == null)
            {
                return false;
            }

            GridPos useCell = gridWorld.WorldToCell(PlayerPosition);
            if (!ropeInstaller.TryInstall(
                    useCell,
                    out RopeInstallation2D installation,
                    out _))
            {
                return false;
            }

            SetRopeStock(ropeStock - 1);
            telemetry?.MarkSuccess(P3ToolKind.Rope);
            RopeInstalled?.Invoke(installation);
            return true;
        }

        public bool TryUseBomb()
        {
            telemetry?.MarkUse(P3ToolKind.Bomb);
            if (bombStock <= 0
                || bombPrefab == null
                || explosionService == null
                || gridWorld == null)
            {
                return false;
            }

            Vector2 desired =
                PlayerPosition
                + Vector2.right * facingDirection * bombHorizontalOffset;
            GridPos targetCell = gridWorld.WorldToCell(desired);
            if (!gridWorld.IsWithinBounds(targetCell)
                || gridWorld.IsSolid(targetCell)
                || gridWorld.IsOccupied(targetCell))
            {
                targetCell = gridWorld.WorldToCell(PlayerPosition);
            }

            Vector2 spawnPosition = gridWorld.CellToWorldCenter(targetCell);
            Bomb2D bomb = Instantiate(
                bombPrefab,
                spawnPosition,
                Quaternion.identity,
                spawnedBombParent);
            bomb.gameObject.SetActive(true);
            bomb.Configure(
                explosionService,
                Bomb2D.DefaultFuseSeconds,
                false,
                true);
            if (bomb.Body != null && playerBody != null)
            {
                bomb.Body.linearVelocity = playerBody.linearVelocity;
            }

            if (!bomb.Arm())
            {
                Destroy(bomb.gameObject);
                return false;
            }

            SetBombStock(bombStock - 1);
            telemetry?.MarkSuccess(P3ToolKind.Bomb);
            BombPlaced?.Invoke(bomb);
            return true;
        }

        public void SetStockForTests(int ropes, int bombs)
        {
            SetRopeStock(ropes);
            SetBombStock(bombs);
        }

        public int AddRopes(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int before = ropeStock;
            SetRopeStock(ropeStock + amount);
            return ropeStock - before;
        }

        public int AddBombs(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int before = bombStock;
            SetBombStock(bombStock + amount);
            return bombStock - before;
        }

        private Vector2 PlayerPosition =>
            playerBody != null ? playerBody.position : (Vector2)transform.position;

        private void SetRopeStock(int value)
        {
            int next = Mathf.Clamp(value, 0, 99);
            if (next == ropeStock)
            {
                return;
            }

            ropeStock = next;
            RopeStockChanged?.Invoke(ropeStock);
        }

        private void SetBombStock(int value)
        {
            int next = Mathf.Clamp(value, 0, 99);
            if (next == bombStock)
            {
                return;
            }

            bombStock = next;
            BombStockChanged?.Invoke(bombStock);
        }
    }
}

#endif
