#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using StarNight.Tiles;
using UnityEngine;

namespace StarNight.Explosions
{
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class ExplosionService2D : MonoBehaviour
    {
        public const int DefaultStartCount = ExplosionConstants.DefaultStartingBombCount;
        public const int DefaultHardCap = ExplosionConstants.DefaultChainHardCap;

        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private TileMutationService tileMutationService;
        [SerializeField, Min(1)] private int hardCap = DefaultHardCap;
        [SerializeField, Min(0f)] private float impulseStrength = 7f;
        [SerializeField] private LayerMask impulseLayers = ~0;

        private readonly List<Bomb2D> registeredBombs = new List<Bomb2D>();
        private readonly Queue<Bomb2D> pendingBombs = new Queue<Bomb2D>();
        private readonly HashSet<Bomb2D> queuedBombs = new HashSet<Bomb2D>();
        private readonly HashSet<Rigidbody2D> impulseBodies = new HashSet<Rigidbody2D>();
        private readonly List<Rigidbody2D> sortedImpulseBodies = new List<Rigidbody2D>();
        private readonly List<Bomb2D> chainCandidates = new List<Bomb2D>();
        private readonly HashSet<IExplosionReceiver2D> explosionReceivers =
            new HashSet<IExplosionReceiver2D>();
        private readonly List<IExplosionReceiver2D> sortedExplosionReceivers =
            new List<IExplosionReceiver2D>();

        private long nextRegistrationOrder = 1;
        private bool isProcessing;

        public event Action<ExplosionChainReport> ChainCompleted;

        public GridWorld GridWorld => gridWorld;
        public TileMutationService TileMutationService => tileMutationService;
        public int HardCap => hardCap;
        public float ImpulseStrength => impulseStrength;
        public LayerMask ImpulseLayers => impulseLayers;
        public int RegisteredBombCount => registeredBombs.Count;
        public int PendingBombCount => pendingBombs.Count;
        public ExplosionChainReport LastReport { get; private set; } = ExplosionChainReport.Empty;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void FixedUpdate()
        {
            if (!isProcessing && pendingBombs.Count > 0)
            {
                ProcessPendingInternal();
            }
        }

        private void OnValidate()
        {
            hardCap = Mathf.Max(1, hardCap);
            impulseStrength = Mathf.Max(0f, impulseStrength);
        }

        public void Configure(
            GridWorld world,
            TileMutationService mutationService,
            int chainHardCap = DefaultHardCap,
            float configuredImpulseStrength = 7f,
            int configuredImpulseLayers = ~0)
        {
            gridWorld = world;
            tileMutationService = mutationService;
            hardCap = Mathf.Max(1, chainHardCap);
            impulseStrength = Mathf.Max(0f, configuredImpulseStrength);
            impulseLayers = configuredImpulseLayers;
        }

        public void Register(Bomb2D bomb)
        {
            if (bomb == null || registeredBombs.Contains(bomb))
            {
                return;
            }

            bomb.RegistrationOrder = nextRegistrationOrder++;
            registeredBombs.Add(bomb);
        }

        public void Unregister(Bomb2D bomb)
        {
            if (bomb == null)
            {
                return;
            }

            registeredBombs.Remove(bomb);
        }

        public bool EnqueueDetonation(Bomb2D bomb)
        {
            if (bomb == null || queuedBombs.Contains(bomb))
            {
                return false;
            }

            Register(bomb);
            if (!bomb.TryMarkQueued())
            {
                return false;
            }

            queuedBombs.Add(bomb);
            pendingBombs.Enqueue(bomb);
            return true;
        }

        public ExplosionChainReport ProcessPendingForTests()
        {
            if (isProcessing || pendingBombs.Count == 0)
            {
                return ExplosionChainReport.Empty;
            }

            return ProcessPendingInternal();
        }

        private ExplosionChainReport ProcessPendingInternal()
        {
            isProcessing = true;
            int seedCount = pendingBombs.Count;
            int mutationRequestCount = 0;
            int impulsedBodyCount = 0;
            int suppressedBombCount = 0;
            List<int> processingOrder = new List<int>(
                Mathf.Min(pendingBombs.Count, hardCap));
            HashSet<Bomb2D> visited = new HashSet<Bomb2D>();

            try
            {
                while (pendingBombs.Count > 0 && processingOrder.Count < hardCap)
                {
                    Bomb2D bomb = pendingBombs.Dequeue();
                    queuedBombs.Remove(bomb);

                    if (bomb == null || !visited.Add(bomb) || bomb.State != BombState.Queued)
                    {
                        continue;
                    }

                    GridPos centerCell = GetBombCell(bomb);
                    processingOrder.Add(bomb.ChainId);
                    mutationRequestCount += EnqueueTileMutations(centerCell, bomb);
                    NotifyExplosionReceivers(centerCell, bomb);
                    impulsedBodyCount += ApplyImpulse(centerCell, bomb.Body);
                    QueueChainCandidates(centerCell, bomb);
                    bomb.MarkDetonated();
                }

                while (pendingBombs.Count > 0)
                {
                    Bomb2D suppressed = pendingBombs.Dequeue();
                    queuedBombs.Remove(suppressed);
                    if (suppressed == null || !visited.Add(suppressed))
                    {
                        continue;
                    }

                    suppressedBombCount++;
                    suppressed.MarkSuppressedBySafetyCap();
                }
            }
            finally
            {
                isProcessing = false;
            }

            LastReport = new ExplosionChainReport(
                seedCount,
                mutationRequestCount,
                impulsedBodyCount,
                hardCap,
                suppressedBombCount,
                processingOrder);

            ChainCompleted?.Invoke(LastReport);
            return LastReport;
        }

        private int EnqueueTileMutations(GridPos centerCell, Bomb2D source)
        {
            if (tileMutationService == null)
            {
                return 0;
            }

            int requestCount = 0;
            foreach (GridPos cell in ExplosionMask3x3.Enumerate(centerCell))
            {
                tileMutationService.EnqueueDestroy(
                    cell,
                    TileBreakMethod.Bomb,
                    source);
                requestCount++;
            }

            return requestCount;
        }

        private void QueueChainCandidates(GridPos centerCell, Bomb2D source)
        {
            chainCandidates.Clear();
            for (int index = registeredBombs.Count - 1; index >= 0; index--)
            {
                Bomb2D candidate = registeredBombs[index];
                if (candidate == null)
                {
                    registeredBombs.RemoveAt(index);
                    continue;
                }

                if (candidate == source
                    || !candidate.isActiveAndEnabled
                    || candidate.State == BombState.Queued
                    || candidate.State == BombState.Detonated
                    || candidate.State == BombState.SuppressedBySafetyCap)
                {
                    continue;
                }

                if (ExplosionMask3x3.Contains(centerCell, GetBombCell(candidate)))
                {
                    chainCandidates.Add(candidate);
                }
            }

            chainCandidates.Sort(CompareBombs);
            for (int index = 0; index < chainCandidates.Count; index++)
            {
                chainCandidates[index].TriggerChain(this);
            }
        }

        private int ApplyImpulse(GridPos centerCell, Rigidbody2D sourceBody)
        {
            if (impulseStrength <= 0f)
            {
                return 0;
            }

            Vector2 center = gridWorld != null
                ? gridWorld.CellToWorldCenter(centerCell)
                : new Vector2(centerCell.X + 0.5f, centerCell.Y + 0.5f);
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                center,
                new Vector2(3f, 3f),
                0f,
                impulseLayers);

            impulseBodies.Clear();
            for (int index = 0; index < overlaps.Length; index++)
            {
                Rigidbody2D body = overlaps[index].attachedRigidbody;
                if (body != null
                    && body != sourceBody
                    && body.bodyType == RigidbodyType2D.Dynamic
                    && body.simulated)
                {
                    impulseBodies.Add(body);
                }
            }

            sortedImpulseBodies.Clear();
            sortedImpulseBodies.AddRange(impulseBodies);
            sortedImpulseBodies.Sort(CompareBodies);

            float maxDistance = Mathf.Sqrt(4.5f);
            for (int index = 0; index < sortedImpulseBodies.Count; index++)
            {
                Rigidbody2D body = sortedImpulseBodies[index];
                Vector2 delta = body.worldCenterOfMass - center;
                float distance = delta.magnitude;
                Vector2 direction = distance > 0.001f ? delta / distance : Vector2.up;
                float falloff = Mathf.Clamp01(1f - (distance / maxDistance));
                body.AddForce(
                    direction * impulseStrength * Mathf.Max(0.2f, falloff),
                    ForceMode2D.Impulse);
            }

            return sortedImpulseBodies.Count;
        }

        private void NotifyExplosionReceivers(
            GridPos centerCell,
            Bomb2D source)
        {
            Vector2 center = gridWorld != null
                ? gridWorld.CellToWorldCenter(centerCell)
                : new Vector2(
                    centerCell.X + 0.5f,
                    centerCell.Y + 0.5f);
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                center,
                new Vector2(3f, 3f),
                0f,
                impulseLayers);
            explosionReceivers.Clear();
            for (int index = 0; index < overlaps.Length; index++)
            {
                MonoBehaviour[] behaviours =
                    overlaps[index].GetComponentsInParent<MonoBehaviour>(
                        true);
                for (int behaviourIndex = 0;
                     behaviourIndex < behaviours.Length;
                     behaviourIndex++)
                {
                    if (behaviours[behaviourIndex]
                        is IExplosionReceiver2D receiver)
                    {
                        explosionReceivers.Add(receiver);
                    }
                }
            }

            sortedExplosionReceivers.Clear();
            sortedExplosionReceivers.AddRange(explosionReceivers);
            sortedExplosionReceivers.Sort(
                (left, right) =>
                    ((Component)left).GetInstanceID().CompareTo(
                        ((Component)right).GetInstanceID()));
            var hit = new ExplosionHit2D(centerCell, center, source);
            for (int index = 0;
                 index < sortedExplosionReceivers.Count;
                 index++)
            {
                sortedExplosionReceivers[index].ReceiveExplosion(hit);
            }
        }

        private GridPos GetBombCell(Bomb2D bomb)
        {
            Vector2 position = bomb.transform.position;
            if (gridWorld != null)
            {
                return gridWorld.WorldToCell(position);
            }

            return new GridPos(
                Mathf.FloorToInt(position.x),
                Mathf.FloorToInt(position.y));
        }

        private void ResolveDependencies()
        {
            if (gridWorld == null)
            {
                gridWorld = GetComponentInParent<GridWorld>();
                if (gridWorld == null)
                {
                    gridWorld = FindFirstObjectByType<GridWorld>();
                }
            }

            if (tileMutationService == null)
            {
                tileMutationService = GetComponentInParent<TileMutationService>();
                if (tileMutationService == null)
                {
                    tileMutationService = FindFirstObjectByType<TileMutationService>();
                }
            }
        }

        private static int CompareBombs(Bomb2D left, Bomb2D right)
        {
            int order = left.ChainId.CompareTo(right.ChainId);
            if (order != 0)
            {
                return order;
            }

            order = left.RegistrationOrder.CompareTo(right.RegistrationOrder);
            return order != 0 ? order : left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        private static int CompareBodies(Rigidbody2D left, Rigidbody2D right)
        {
            return left.GetInstanceID().CompareTo(right.GetInstanceID());
        }
    }
}

#endif
