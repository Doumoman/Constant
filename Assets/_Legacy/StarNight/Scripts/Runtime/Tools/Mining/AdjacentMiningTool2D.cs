#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using StarNight.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Tools.Mining
{
    [DisallowMultipleComponent]
    public abstract class AdjacentMiningTool2D : MonoBehaviour
    {
        public const float DefaultShortWindupSeconds = 0.14f;

        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private TileMutationService tileMutationService;
        [SerializeField, Min(1)] private int maximumDurability;
        [SerializeField, Min(0)] private int remainingDurability;
        [SerializeField, Min(0f)]
        private float windupSeconds = DefaultShortWindupSeconds;
        [SerializeField] private MiningToolState state;

        private GridPos pendingTarget;
        private float remainingWindup;

        public event Action<MiningUseResult> UseResolved;

        public GridWorld GridWorld => gridWorld;
        public TileMutationService TileMutationService => tileMutationService;
        public int MaximumDurability => maximumDurability;
        public int RemainingDurability => remainingDurability;
        public float WindupSeconds => windupSeconds;
        public float RemainingWindup => remainingWindup;
        public MiningToolState State => state;
        public bool IsDepleted => remainingDurability <= 0;

        protected abstract int GddDefaultDurability { get; }
        protected abstract TileBreakMethod BreakMethod { get; }

        protected virtual void Reset()
        {
            maximumDurability = GddDefaultDurability;
            remainingDurability = maximumDurability;
            windupSeconds = DefaultShortWindupSeconds;
        }

        protected virtual void Awake()
        {
            ResolveDependencies();
            if (maximumDurability <= 0)
            {
                maximumDurability = GddDefaultDurability;
            }

            if (remainingDurability <= 0)
            {
                remainingDurability = maximumDurability;
            }
        }

        protected virtual void OnValidate()
        {
            if (maximumDurability <= 0)
            {
                maximumDurability = GddDefaultDurability;
            }

            remainingDurability = Mathf.Clamp(
                remainingDurability,
                0,
                maximumDurability);
            windupSeconds = Mathf.Max(0f, windupSeconds);
        }

        private void FixedUpdate()
        {
            if (state != MiningToolState.WindingUp)
            {
                return;
            }

            remainingWindup = Mathf.Max(
                0f,
                remainingWindup - Time.fixedDeltaTime);
            if (remainingWindup > 0f)
            {
                return;
            }

            state = MiningToolState.Ready;
            MiningUseResult result = TryQueueMutation(pendingTarget, false);
            UseResolved?.Invoke(result);
        }

        public void Configure(
            GridWorld world,
            TileMutationService mutationService,
            int configuredMaximumDurability,
            float configuredWindupSeconds = DefaultShortWindupSeconds)
        {
            gridWorld = world;
            tileMutationService = mutationService;
            maximumDurability = Mathf.Max(1, configuredMaximumDurability);
            remainingDurability = maximumDurability;
            windupSeconds = Mathf.Max(0f, configuredWindupSeconds);
            state = MiningToolState.Ready;
            remainingWindup = 0f;
        }

        public void Refill()
        {
            remainingDurability = maximumDurability;
        }

        public bool TryBeginUse(
            Vector2 worldOrigin,
            Vector2 aim,
            int fallbackHorizontalFacing,
            out MiningUseResult result)
        {
            ResolveDependencies();
            if (gridWorld == null)
            {
                result = Failure(
                    default,
                    MiningUseFailure.MissingGridWorld);
                UseResolved?.Invoke(result);
                return false;
            }

            GridPos originCell = gridWorld.WorldToCell(worldOrigin);
            MiningTargetResolver.TryResolveAdjacent(
                originCell,
                aim,
                fallbackHorizontalFacing,
                out GridPos targetCell,
                out Vector2Int direction);
            return TryBeginUseFromCell(
                originCell,
                direction,
                out result);
        }

        public bool TryBeginUseFromCell(
            GridPos originCell,
            Vector2Int direction,
            out MiningUseResult result)
        {
            ResolveDependencies();
            if (state == MiningToolState.WindingUp)
            {
                result = Failure(
                    originCell,
                    MiningUseFailure.Busy);
                UseResolved?.Invoke(result);
                return false;
            }

            if (!MiningTargetResolver.IsCardinalUnit(direction))
            {
                result = Failure(
                    originCell,
                    MiningUseFailure.InvalidDirection);
                UseResolved?.Invoke(result);
                return false;
            }

            GridPos targetCell = new GridPos(
                originCell.X + direction.x,
                originCell.Y + direction.y);
            MiningUseFailure validation = ValidateTarget(targetCell);
            if (validation != MiningUseFailure.None)
            {
                result = Failure(targetCell, validation);
                UseResolved?.Invoke(result);
                return false;
            }

            pendingTarget = targetCell;
            remainingWindup = windupSeconds;
            state = MiningToolState.WindingUp;
            result = new MiningUseResult(
                targetCell,
                MiningUseFailure.None,
                0,
                remainingDurability);

            if (remainingWindup <= 0f)
            {
                state = MiningToolState.Ready;
                result = TryQueueMutation(pendingTarget, false);
                UseResolved?.Invoke(result);
                return result.Queued;
            }

            return true;
        }

        public MiningUseResult TryUseImmediatelyForTests(
            GridPos originCell,
            Vector2Int direction,
            bool flushMutation = false)
        {
            ResolveDependencies();
            if (state == MiningToolState.WindingUp)
            {
                return Failure(originCell, MiningUseFailure.Busy);
            }

            if (!MiningTargetResolver.IsCardinalUnit(direction))
            {
                return Failure(
                    originCell,
                    MiningUseFailure.InvalidDirection);
            }

            GridPos targetCell = new GridPos(
                originCell.X + direction.x,
                originCell.Y + direction.y);
            return TryQueueMutation(targetCell, flushMutation);
        }

        public bool CancelUse()
        {
            if (state != MiningToolState.WindingUp)
            {
                return false;
            }

            state = MiningToolState.Ready;
            remainingWindup = 0f;
            return true;
        }

        protected abstract bool IsAllowedTerrain(TileDefinition definition);

        protected virtual bool HasNonTerrainTarget(GridPos targetCell)
        {
            return false;
        }

        protected virtual bool TryUseNonTerrainTarget(GridPos targetCell)
        {
            return false;
        }

        private MiningUseResult TryQueueMutation(
            GridPos targetCell,
            bool flushMutation)
        {
            MiningUseFailure validation = ValidateTarget(targetCell);
            if (validation != MiningUseFailure.None)
            {
                return Failure(targetCell, validation);
            }

            if (HasNonTerrainTarget(targetCell))
            {
                if (!TryUseNonTerrainTarget(targetCell))
                {
                    return Failure(
                        targetCell,
                        MiningUseFailure.MutationRejected);
                }

                remainingDurability = Mathf.Max(
                    0,
                    remainingDurability - 1);
                return new MiningUseResult(
                    targetCell,
                    MiningUseFailure.None,
                    0,
                    remainingDurability,
                    true);
            }

            long sequence = tileMutationService.EnqueueDestroy(
                targetCell,
                BreakMethod,
                this);
            remainingDurability = Mathf.Max(0, remainingDurability - 1);

            if (flushMutation)
            {
                TileMutationBatchReport report =
                    tileMutationService.FlushPending();
                TileMutationRecord record = FindRecord(report, sequence);
                if (record == null || !record.Committed)
                {
                    remainingDurability = Mathf.Min(
                        maximumDurability,
                        remainingDurability + 1);
                    return Failure(
                        targetCell,
                        MiningUseFailure.MutationRejected);
                }
            }

            return new MiningUseResult(
                targetCell,
                MiningUseFailure.None,
                sequence,
                remainingDurability);
        }

        private MiningUseFailure ValidateTarget(GridPos targetCell)
        {
            if (remainingDurability <= 0)
            {
                return MiningUseFailure.NoDurability;
            }

            if (gridWorld == null)
            {
                return MiningUseFailure.MissingGridWorld;
            }

            if (!gridWorld.IsWithinBounds(targetCell))
            {
                return MiningUseFailure.TargetOutOfBounds;
            }

            if (HasNonTerrainTarget(targetCell))
            {
                return MiningUseFailure.None;
            }

            if (tileMutationService == null)
            {
                return MiningUseFailure.MissingMutationService;
            }

            Tilemap terrain = gridWorld.TerrainTilemap;
            if (terrain == null)
            {
                return MiningUseFailure.MissingTerrainTilemap;
            }

            if (tileMutationService.IsProtectedCell(targetCell)
                || targetCell == tileMutationService.RequiredStart
                || targetCell == tileMutationService.RequiredExit)
            {
                return MiningUseFailure.ProtectedTile;
            }

            TileBase tile = terrain.GetTile(
                new Vector3Int(targetCell.X, targetCell.Y, 0));
            if (tile == null)
            {
                return MiningUseFailure.NoTile;
            }

            if (!tileMutationService.TryGetDefinition(
                    tile,
                    out TileDefinition definition))
            {
                return MiningUseFailure.UndefinedTile;
            }

            if (definition.IsProtected)
            {
                return MiningUseFailure.ProtectedTile;
            }

            return IsAllowedTerrain(definition)
                && definition.CanBreak(BreakMethod)
                ? MiningUseFailure.None
                : MiningUseFailure.WrongTerrain;
        }

        private MiningUseResult Failure(
            GridPos targetCell,
            MiningUseFailure failure)
        {
            return new MiningUseResult(
                targetCell,
                failure,
                0,
                remainingDurability);
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
                tileMutationService =
                    GetComponentInParent<TileMutationService>();
                if (tileMutationService == null)
                {
                    tileMutationService =
                        FindFirstObjectByType<TileMutationService>();
                }
            }
        }

        private static TileMutationRecord FindRecord(
            TileMutationBatchReport report,
            long sequence)
        {
            if (report == null || report.Records == null)
            {
                return null;
            }

            for (int index = 0; index < report.Records.Count; index++)
            {
                TileMutationRecord record = report.Records[index];
                if (record.Request.Sequence == sequence)
                {
                    return record;
                }
            }

            return null;
        }
    }
}

#endif
