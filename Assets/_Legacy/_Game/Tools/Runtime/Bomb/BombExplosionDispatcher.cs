#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Interaction.State;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.Bomb
{
    public readonly struct BombDamageEvent
    {
        public BombDamageEvent(int damage, Vector2 knockback, GameObject source, int explosionId)
        {
            Damage = Mathf.Clamp(damage, 0, 1);
            Knockback = knockback;
            Source = source;
            ExplosionId = explosionId;
        }

        public int Damage { get; }
        public Vector2 Knockback { get; }
        public GameObject Source { get; }
        public int ExplosionId { get; }
    }

    public interface IBombDamageReceiver
    {
        bool ReceiveBombDamage(BombDamageEvent damageEvent);
    }

    public interface IBombProtectedTarget { }

    public readonly struct BombExplosionReport
    {
        public BombExplosionReport(int cells, int reactions, int damagedEntities, int chainedBombs)
        {
            Cells = cells;
            Reactions = reactions;
            DamagedEntities = damagedEntities;
            ChainedBombs = chainedBombs;
        }

        public int Cells { get; }
        public int Reactions { get; }
        public int DamagedEntities { get; }
        public int ChainedBombs { get; }
    }

    public interface IBombExplosionWorld
    {
        BombExplosionReport Dispatch(BombRuntime source, int explosionId);
    }

    [DisallowMultipleComponent]
    public sealed class BombExplosionDispatcher : MonoBehaviour, IBombExplosionWorld
    {
        private static readonly Vector2Int[] CellOffsets = BuildApprovedOffsets();

        [SerializeField] private ProjectPhysicsProfile physicsProfile;
        [SerializeField] private Vector2 gridOrigin;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;

        public static IReadOnlyList<Vector2Int> ApprovedCellOffsets => CellOffsets;

        public BombExplosionReport Dispatch(BombRuntime source, int explosionId)
        {
            if (source == null)
            {
                return default;
            }

            BombDefinition definition = source.Definition;
            float size = definition != null ? definition.CellSize : cellSize;
            Vector2Int centerCell = WorldToCell(source.transform.position, size);
            Vector2 center = CellToWorld(centerCell, size);
            int affectMask = physicsProfile != null
                ? physicsProfile.BombAffectMask.value
                : LayerMask.GetMask("TerrainSolid", "DynamicObject", "Enemy", "Hazard", "Rope");
            int occlusionMask = LayerMask.GetMask("UnbreakableBoundary");
            int protectedMask = LayerMask.GetMask("UnbreakableBoundary", "PortalBoundary");
            var processedReactions = new HashSet<int>();
            var processedDamage = new HashSet<int>();
            var processedBombs = new HashSet<int>();
            int reactions = 0;
            int damaged = 0;
            int chained = 0;

            List<Vector2Int> propagatedCells = BuildPropagationCells(centerCell, size, affectMask);
            for (int cellIndex = 0; cellIndex < propagatedCells.Count; cellIndex++)
            {
                Vector2Int targetCell = propagatedCells[cellIndex];
                Vector2 targetWorld = CellToWorld(targetCell, size);
                Collider2D[] colliders = Physics2D.OverlapBoxAll(
                    targetWorld,
                    Vector2.one * size * 0.92f,
                    0f,
                    affectMask);
                Array.Sort(colliders, CompareColliderIds);
                for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    Collider2D targetCollider = colliders[colliderIndex];
                    if (targetCollider == null
                        || ((1 << targetCollider.gameObject.layer) & protectedMask) != 0
                        || HasProtectedMarker(targetCollider))
                    {
                        continue;
                    }

                    BombRuntime otherBomb = targetCollider.GetComponentInParent<BombRuntime>();
                    if (otherBomb != null && otherBomb != source
                        && processedBombs.Add(otherBomb.GetInstanceID())
                        && otherBomb.ReduceFuseForChain())
                    {
                        chained++;
                    }

                    MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>(true);
                    for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                    {
                        MonoBehaviour behaviour = behaviours[behaviourIndex];
                        if (behaviour is IToolReactionReceiver reactionReceiver
                            && processedReactions.Add(behaviour.GetInstanceID()))
                        {
                            Vector2Int direction = Cardinal(targetWorld - center);
                            ToolReactionResult result = reactionReceiver.TryReact(new ToolReactionContext
                            {
                                ActionId = explosionId,
                                Tags = ToolTag.Bomb | ToolTag.HeavyImpact,
                                OriginCell = new GridCell(centerCell),
                                TargetCell = new GridCell(targetCell),
                                Direction = direction,
                                Magnitude = definition != null
                                    ? definition.KnockbackCellsPerSecond
                                    : BombDefinition.ApprovedKnockbackCellsPerSecond,
                                Source = source.gameObject,
                                Instigator = source.Instigator,
                            });
                            if (result.Accepted)
                            {
                                reactions++;
                            }
                        }

                        if (behaviour is IBombDamageReceiver damageReceiver
                            && processedDamage.Add(behaviour.GetInstanceID())
                            && !Physics2D.Linecast(center, behaviour.transform.position, occlusionMask))
                        {
                            Vector2 delta = (Vector2)behaviour.transform.position - center;
                            Vector2 knockbackDirection = delta.sqrMagnitude > 0.0001f
                                ? delta.normalized
                                : Vector2.up;
                            float knockback = definition != null
                                ? definition.KnockbackCellsPerSecond
                                : BombDefinition.ApprovedKnockbackCellsPerSecond;
                            int damage = definition != null
                                ? definition.EntityDamage
                                : BombDefinition.ApprovedEntityDamage;
                            if (damageReceiver.ReceiveBombDamage(new BombDamageEvent(
                                damage,
                                knockbackDirection * knockback,
                                source.gameObject,
                                explosionId)))
                            {
                                damaged++;
                            }
                        }
                    }
                }
            }

            return new BombExplosionReport(propagatedCells.Count, reactions, damaged, chained);
        }

        public void ConfigureForTests(ProjectPhysicsProfile profile, Vector2 origin, float size = 1f)
        {
            physicsProfile = profile;
            gridOrigin = origin;
            cellSize = Mathf.Max(0.01f, size);
        }

        private bool HasProtectedMarker(Collider2D collider)
        {
            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IBombProtectedTarget)
                {
                    return true;
                }

                if (behaviours[index] is IMapExplosionProtected)
                {
                    return true;
                }

                if (behaviours[index] is MapElementInstance element
                    && element.Definition?.CommonProfile?.Kind == CommonElementKind.UnbreakableBlock)
                {
                    return true;
                }
            }
            return false;
        }

        private Vector2Int WorldToCell(Vector2 world, float size)
        {
            Vector2 local = (world - gridOrigin) / Mathf.Max(0.01f, size);
            return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
        }

        private Vector2 CellToWorld(Vector2Int cell, float size) => gridOrigin + (Vector2)cell * size;

        private List<Vector2Int> BuildPropagationCells(Vector2Int centerCell, float size, int affectMask)
        {
            IReadOnlyList<SoftSoilExplosionCell> trace = SoftSoilContract.TraceExplosion(
                centerCell,
                cell => CellContainsSoftSoil(cell, size, affectMask));
            var result = new List<Vector2Int>(trace.Count);
            for (int index = 0; index < trace.Count; index++)
            {
                result.Add(trace[index].Cell);
            }
            return result;
        }

        private bool CellContainsSoftSoil(Vector2Int cell, float size, int affectMask)
        {
            Collider2D[] colliders = Physics2D.OverlapBoxAll(
                CellToWorld(cell, size),
                Vector2.one * size * 0.92f,
                0f,
                affectMask);
            for (int index = 0; index < colliders.Length; index++)
            {
                MapElementInstance element = colliders[index] != null
                    ? colliders[index].GetComponentInParent<MapElementInstance>()
                    : null;
                if (SoftSoilContract.IsSoftSoil(element?.Definition)) return true;
            }
            return false;
        }

        private static Vector2Int[] BuildApprovedOffsets()
        {
            var result = new List<Vector2Int>();
            for (int y = -SoftSoilContract.ExplosionOriginEnergy; y <= SoftSoilContract.ExplosionOriginEnergy; y++)
            {
                for (int x = -SoftSoilContract.ExplosionOriginEnergy; x <= SoftSoilContract.ExplosionOriginEnergy; x++)
                {
                    int cost = Mathf.Abs(x) + Mathf.Abs(y);
                    if (cost <= SoftSoilContract.ExplosionOriginEnergy) result.Add(new Vector2Int(x, y));
                }
            }
            return result.ToArray();
        }

        private static Vector2Int Cardinal(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.0001f)
            {
                return Vector2Int.up;
            }
            return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? (delta.x < 0f ? Vector2Int.left : Vector2Int.right)
                : (delta.y < 0f ? Vector2Int.down : Vector2Int.up);
        }

        private static int CompareColliderIds(Collider2D left, Collider2D right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }
            if (left == null)
            {
                return 1;
            }
            if (right == null)
            {
                return -1;
            }
            return left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

    }
}

#endif
