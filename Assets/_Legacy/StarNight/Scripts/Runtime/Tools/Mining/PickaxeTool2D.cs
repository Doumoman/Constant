#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using StarNight.Tiles;
using UnityEngine;

namespace StarNight.Tools.Mining
{
    public sealed class PickaxeTool2D : AdjacentMiningTool2D
    {
        public const int DefaultDurability = 10;
        public const float DefaultEnemyStunSeconds = 0.65f;

        [SerializeField] private LayerMask stunnableQueryMask = ~0;
        [SerializeField, Min(0.05f)]
        private float enemyStunSeconds = DefaultEnemyStunSeconds;
        [SerializeField, Range(0.25f, 1f)]
        private float targetCellFill = 0.86f;

        protected override int GddDefaultDurability => DefaultDurability;
        protected override TileBreakMethod BreakMethod =>
            TileBreakMethod.Pickaxe;

        protected override bool IsAllowedTerrain(TileDefinition definition)
        {
            return definition != null
                && (definition.MaterialKind == TileMaterialKind.Stone
                    || definition.MaterialKind == TileMaterialKind.CrackedWall);
        }

        protected override bool HasNonTerrainTarget(GridPos targetCell)
        {
            return TryFindStunnable(targetCell, out _);
        }

        protected override bool TryUseNonTerrainTarget(GridPos targetCell)
        {
            if (!TryFindStunnable(
                    targetCell,
                    out IPickaxeStunnable stunnable))
            {
                return false;
            }

            PickaxeStunContext context = new PickaxeStunContext(
                this,
                targetCell,
                enemyStunSeconds);
            return stunnable.TryReceivePickaxeStun(context);
        }

        private bool TryFindStunnable(
            GridPos targetCell,
            out IPickaxeStunnable stunnable)
        {
            stunnable = null;
            if (GridWorld == null)
            {
                return false;
            }

            Vector2 center = GridWorld.CellToWorldCenter(targetCell);
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                center,
                Vector2.one * targetCellFill,
                0f,
                stunnableQueryMask);
            Array.Sort(
                overlaps,
                (left, right) =>
                    left.GetInstanceID().CompareTo(right.GetInstanceID()));
            for (int colliderIndex = 0;
                colliderIndex < overlaps.Length;
                colliderIndex++)
            {
                MonoBehaviour[] behaviours =
                    overlaps[colliderIndex]
                        .GetComponentsInParent<MonoBehaviour>(true);
                for (int behaviourIndex = 0;
                    behaviourIndex < behaviours.Length;
                    behaviourIndex++)
                {
                    if (behaviours[behaviourIndex]
                            is IPickaxeStunnable candidate
                        && candidate.PickaxeStunTargetObject != null
                        && candidate.CanReceivePickaxeStun)
                    {
                        stunnable = candidate;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

#endif
