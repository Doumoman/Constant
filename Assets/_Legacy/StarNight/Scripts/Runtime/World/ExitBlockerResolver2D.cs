#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Objects;
using StarNight.Tiles;
using UnityEngine;

namespace StarNight.World
{
    [DefaultExecutionOrder(31000)]
    [DisallowMultipleComponent]
    public sealed class ExitBlockerResolver2D : MonoBehaviour
    {
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private TileMutationService tileMutationService;
        [SerializeField] private ExplosionService2D explosionService;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private Vector2Int requiredStart = new Vector2Int(2, 1);
        [SerializeField] private Vector2Int requiredExit = new Vector2Int(44, 1);
        [SerializeField] private Vector2Int[] protectedExitCells;
        [SerializeField, Min(0f)] private float settledSpeed = 0.25f;
        [SerializeField, Min(1)] private int settledFixedSteps = 3;
        [SerializeField, Min(1)] private int maximumWaitFixedSteps = 90;

        private readonly HashSet<GridPos> protectedCells = new HashSet<GridPos>();
        private readonly List<CarryableObject2D> objects =
            new List<CarryableObject2D>();
        private readonly Dictionary<CarryableObject2D, GridPos> objectCells =
            new Dictionary<CarryableObject2D, GridPos>();
        private readonly HashSet<GridPos> occupiedCells = new HashSet<GridPos>();

        private bool subscribed;
        private bool resolutionRequested;
        private int stableStepCount;
        private int waitedStepCount;

        public event Action<int> ResolutionCompleted;

        public bool ResolutionRequested => resolutionRequested;
        public int LastRelocatedCount { get; private set; }
        public bool ExitRouteReachable { get; private set; }

        public void Configure(
            GridWorld world,
            TileMutationService mutationService,
            ExplosionService2D targetExplosionService,
            Collider2D targetPlayerCollider,
            GridPos start,
            GridPos exit,
            GridPos[] exitProtectedCells)
        {
            Unsubscribe();
            gridWorld = world;
            tileMutationService = mutationService;
            explosionService = targetExplosionService;
            playerCollider = targetPlayerCollider;
            requiredStart = new Vector2Int(start.X, start.Y);
            requiredExit = new Vector2Int(exit.X, exit.Y);
            protectedExitCells = ToVectorArray(exitProtectedCells);
            RebuildProtectedCells();
            Subscribe();
        }

        private void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (tileMutationService == null)
            {
                tileMutationService = FindFirstObjectByType<TileMutationService>();
            }

            if (explosionService == null)
            {
                explosionService = FindFirstObjectByType<ExplosionService2D>();
            }

            RebuildProtectedCells();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void FixedUpdate()
        {
            if (!resolutionRequested)
            {
                return;
            }

            waitedStepCount++;
            if (AreRelevantBodiesSettled())
            {
                stableStepCount++;
            }
            else
            {
                stableStepCount = 0;
            }

            if (stableStepCount < settledFixedSteps
                && waitedStepCount < maximumWaitFixedSteps)
            {
                return;
            }

            ResolveNow();
        }

        public void RequestResolution()
        {
            resolutionRequested = true;
            stableStepCount = 0;
            waitedStepCount = 0;
        }

        public int ResolveNow()
        {
            resolutionRequested = false;
            stableStepCount = 0;
            waitedStepCount = 0;
            LastRelocatedCount = 0;

            if (gridWorld == null)
            {
                ExitRouteReachable = false;
                ResolutionCompleted?.Invoke(0);
                return 0;
            }

            GatherObjectCells();
            ExitRouteReachable = HasRoute(occupiedCells);
            if (ExitRouteReachable)
            {
                ResolutionCompleted?.Invoke(0);
                return 0;
            }

            SortObjectsForResolution();
            for (int index = 0;
                 index < objects.Count && !ExitRouteReachable;
                 index++)
            {
                CarryableObject2D blocker = objects[index];
                if (!objectCells.TryGetValue(blocker, out GridPos originalCell))
                {
                    continue;
                }

                occupiedCells.Remove(originalCell);
                if (!TryFindRelocationCell(
                        originalCell,
                        occupiedCells,
                        out GridPos destination))
                {
                    destination = FindFallbackCell(blocker, occupiedCells);
                }

                if (destination == originalCell
                    || !IsValidDestination(destination, occupiedCells, false))
                {
                    occupiedCells.Add(originalCell);
                    continue;
                }

                Relocate(blocker, destination);
                objectCells[blocker] = destination;
                occupiedCells.Add(destination);
                LastRelocatedCount++;
                ExitRouteReachable = HasRoute(occupiedCells);
            }

            Physics2D.SyncTransforms();
            ExitRouteReachable = HasRoute(occupiedCells);
            ResolutionCompleted?.Invoke(LastRelocatedCount);
            return LastRelocatedCount;
        }

        private void GatherObjectCells()
        {
            objects.Clear();
            objectCells.Clear();
            occupiedCells.Clear();

            foreach (CarryableObject2D candidate in CarryableObject2D.ActiveObjects)
            {
                if (candidate == null
                    || candidate.IsHeld
                    || candidate.Body == null
                    || !candidate.Body.simulated)
                {
                    continue;
                }

                GridPos cell = gridWorld.WorldToCell(candidate.Body.position);
                if (!gridWorld.IsWithinBounds(cell))
                {
                    continue;
                }

                objects.Add(candidate);
                objectCells[candidate] = cell;
                occupiedCells.Add(cell);
            }
        }

        private void SortObjectsForResolution()
        {
            objects.Sort((left, right) =>
            {
                GridPos leftCell = objectCells[left];
                GridPos rightCell = objectCells[right];
                int leftDistance = Manhattan(leftCell, RequiredExit);
                int rightDistance = Manhattan(rightCell, RequiredExit);
                int order = leftDistance.CompareTo(rightDistance);
                if (order != 0)
                {
                    return order;
                }

                order = leftCell.Y.CompareTo(rightCell.Y);
                return order != 0
                    ? order
                    : leftCell.X.CompareTo(rightCell.X);
            });
        }

        private bool TryFindRelocationCell(
            GridPos origin,
            HashSet<GridPos> otherOccupiedCells,
            out GridPos destination)
        {
            if (TryFindRelocationCell(
                    origin,
                    otherOccupiedCells,
                    true,
                    out destination))
            {
                return true;
            }

            return TryFindRelocationCell(
                origin,
                otherOccupiedCells,
                false,
                out destination);
        }

        private bool TryFindRelocationCell(
            GridPos origin,
            HashSet<GridPos> otherOccupiedCells,
            bool requireCurrentRoute,
            out GridPos destination)
        {
            RectInt bounds = gridWorld.CellBounds;
            int maximumRadius = bounds.width + bounds.height;
            for (int radius = 1; radius <= maximumRadius; radius++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        GridPos candidate = new GridPos(x, y);
                        if (Manhattan(origin, candidate) != radius
                            || !IsValidDestination(
                                candidate,
                                otherOccupiedCells,
                                requireCurrentRoute))
                        {
                            continue;
                        }

                        destination = candidate;
                        return true;
                    }
                }
            }

            destination = origin;
            return false;
        }

        private GridPos FindFallbackCell(
            CarryableObject2D blocker,
            HashSet<GridPos> otherOccupiedCells)
        {
            GridPos home = gridWorld.WorldToCell(blocker.RespawnPosition);
            if (IsValidDestination(home, otherOccupiedCells, true))
            {
                return home;
            }

            return objectCells[blocker];
        }

        private bool IsValidDestination(
            GridPos cell,
            HashSet<GridPos> otherOccupiedCells,
            bool requireRoute)
        {
            if (protectedCells.Contains(cell)
                || otherOccupiedCells.Contains(cell)
                || OverlapsPlayer(cell)
                || !gridWorld.CanStandAt(cell, new Vector2(0.82f, 0.82f)))
            {
                return false;
            }

            if (!requireRoute)
            {
                HashSet<GridPos> candidateOnly = new HashSet<GridPos>
                {
                    cell
                };
                return HasRoute(candidateOnly);
            }

            otherOccupiedCells.Add(cell);
            bool hasRoute = HasRoute(otherOccupiedCells);
            otherOccupiedCells.Remove(cell);
            return hasRoute;
        }

        private bool HasRoute(HashSet<GridPos> dynamicOccupiedCells)
        {
            return GridReachabilityValidator.CanReach(
                gridWorld.CellBounds,
                RequiredStart,
                RequiredExit,
                cell => gridWorld.IsSolid(cell)
                    || (dynamicOccupiedCells != null
                        && dynamicOccupiedCells.Contains(cell)));
        }

        private bool AreRelevantBodiesSettled()
        {
            float maximumSpeedSquared = settledSpeed * settledSpeed;
            foreach (CarryableObject2D candidate in CarryableObject2D.ActiveObjects)
            {
                if (candidate == null
                    || candidate.IsHeld
                    || candidate.Body == null
                    || !candidate.Body.simulated)
                {
                    continue;
                }

                if (candidate.Body.linearVelocity.sqrMagnitude > maximumSpeedSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private void Relocate(CarryableObject2D blocker, GridPos destination)
        {
            gridWorld.ReleaseAll(blocker);
            Vector2 position = gridWorld.CellToWorldCenter(destination);
            Rigidbody2D body = blocker.Body;
            blocker.transform.position = new Vector3(
                position.x,
                position.y,
                blocker.transform.position.z);
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.WakeUp();
            gridWorld.TryOccupy(destination, blocker);
        }

        private bool OverlapsPlayer(GridPos cell)
        {
            if (playerCollider == null || !playerCollider.enabled)
            {
                return false;
            }

            Vector2 center = gridWorld.CellToWorldCenter(cell);
            Bounds bounds = playerCollider.bounds;
            Rect playerRect = new Rect(
                bounds.min.x,
                bounds.min.y,
                bounds.size.x,
                bounds.size.y);
            Rect cellRect = new Rect(
                center.x - 0.5f,
                center.y - 0.5f,
                1f,
                1f);
            return playerRect.Overlaps(cellRect);
        }

        private void HandleMutationCommitted(TileMutationBatchReport _)
        {
            RequestResolution();
        }

        private void HandleExplosionCompleted(ExplosionChainReport _)
        {
            RequestResolution();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (tileMutationService != null)
            {
                tileMutationService.BatchCommitted += HandleMutationCommitted;
            }

            if (explosionService != null)
            {
                explosionService.ChainCompleted += HandleExplosionCompleted;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (tileMutationService != null)
            {
                tileMutationService.BatchCommitted -= HandleMutationCommitted;
            }

            if (explosionService != null)
            {
                explosionService.ChainCompleted -= HandleExplosionCompleted;
            }

            subscribed = false;
        }

        private void RebuildProtectedCells()
        {
            protectedCells.Clear();
            if (protectedExitCells == null)
            {
                return;
            }

            for (int index = 0; index < protectedExitCells.Length; index++)
            {
                Vector2Int cell = protectedExitCells[index];
                protectedCells.Add(new GridPos(cell.x, cell.y));
            }
        }

        private GridPos RequiredStart =>
            new GridPos(requiredStart.x, requiredStart.y);

        private GridPos RequiredExit =>
            new GridPos(requiredExit.x, requiredExit.y);

        private static int Manhattan(GridPos left, GridPos right)
        {
            return Mathf.Abs(left.X - right.X) + Mathf.Abs(left.Y - right.Y);
        }

        private static Vector2Int[] ToVectorArray(GridPos[] cells)
        {
            if (cells == null)
            {
                return Array.Empty<Vector2Int>();
            }

            Vector2Int[] values = new Vector2Int[cells.Length];
            for (int index = 0; index < cells.Length; index++)
            {
                values[index] = new Vector2Int(cells[index].X, cells[index].Y);
            }

            return values;
        }
    }
}

#endif
