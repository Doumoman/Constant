#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    public static class SoftSoilContract
    {
        public const int ExplosionOriginEnergy = 2;
        public const int OrthogonalPropagationCost = 1;
        public const int DiagonalPropagationCost = 2;
        public const int ExplosionAbsorptionCost = 1;

        public static bool IsSoftSoil(MapElementDefinition definition)
        {
            return definition?.CommonProfile?.Kind == CommonElementKind.SoftSoil;
        }

        public static ToolTag ReduceImpactGrade(ToolTag tags)
        {
            if ((tags & ToolTag.HeavyImpact) != 0)
            {
                return (tags & ~ToolTag.HeavyImpact) | ToolTag.LightImpact;
            }
            if ((tags & ToolTag.LightImpact) != 0)
            {
                return tags & ~ToolTag.LightImpact;
            }
            return tags;
        }

        public static int PropagationCost(Vector2Int direction)
        {
            return direction.x != 0 && direction.y != 0
                ? DiagonalPropagationCost
                : OrthogonalPropagationCost;
        }

        public static IReadOnlyList<SoftSoilExplosionCell> TraceExplosion(
            Vector2Int origin,
            Func<Vector2Int, bool> containsSoftSoil,
            int originEnergy = ExplosionOriginEnergy)
        {
            containsSoftSoil ??= _ => false;
            var order = new List<Vector2Int>();
            var bestEnergy = new Dictionary<Vector2Int, int>();
            var queue = new Queue<SoftSoilExplosionCell>();
            int clampedOriginEnergy = Mathf.Max(0, originEnergy);
            bestEnergy.Add(origin, clampedOriginEnergy);
            order.Add(origin);
            queue.Enqueue(new SoftSoilExplosionCell(origin, clampedOriginEnergy, false));

            while (queue.Count > 0)
            {
                SoftSoilExplosionCell current = queue.Dequeue();
                if (bestEnergy.TryGetValue(current.Cell, out int knownEnergy) &&
                    current.RemainingEnergy < knownEnergy)
                {
                    continue;
                }

                for (int index = 0; index < PropagationDirections.Length; index++)
                {
                    Vector2Int direction = PropagationDirections[index];
                    int travelEnergy = current.RemainingEnergy - PropagationCost(direction);
                    if (travelEnergy < 0)
                    {
                        continue;
                    }

                    Vector2Int next = current.Cell + direction;
                    bool isSoftSoil = containsSoftSoil(next);
                    int remainingEnergy = travelEnergy - (isSoftSoil ? ExplosionAbsorptionCost : 0);
                    if (bestEnergy.TryGetValue(next, out int previousEnergy) &&
                        previousEnergy >= remainingEnergy)
                    {
                        continue;
                    }

                    if (!bestEnergy.ContainsKey(next))
                    {
                        order.Add(next);
                    }
                    bestEnergy[next] = remainingEnergy;
                    if (remainingEnergy >= 0)
                    {
                        queue.Enqueue(new SoftSoilExplosionCell(next, remainingEnergy, isSoftSoil));
                    }
                }
            }

            var result = new List<SoftSoilExplosionCell>(order.Count);
            for (int index = 0; index < order.Count; index++)
            {
                Vector2Int cell = order[index];
                result.Add(new SoftSoilExplosionCell(
                    cell,
                    bestEnergy[cell],
                    cell != origin && containsSoftSoil(cell)));
            }
            return result;
        }

        private static readonly Vector2Int[] PropagationDirections =
        {
            Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up,
            new Vector2Int(-1, -1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(1, 1),
        };
    }

    public readonly struct SoftSoilExplosionCell
    {
        public SoftSoilExplosionCell(Vector2Int cell, int remainingEnergy, bool isSoftSoil)
        {
            Cell = cell;
            RemainingEnergy = remainingEnergy;
            IsSoftSoil = isSoftSoil;
        }

        public Vector2Int Cell { get; }
        public int RemainingEnergy { get; }
        public bool IsSoftSoil { get; }
    }
}

#endif
