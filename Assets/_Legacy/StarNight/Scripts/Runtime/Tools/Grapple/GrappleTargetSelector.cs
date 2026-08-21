#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools.Grapple
{
    public enum GrappleTargetKind
    {
        None = 0,
        FixedTerrain = 1,
        Pullable = 2,
        BossHook = 3
    }

    public readonly struct GrappleTargetCandidate
    {
        public GrappleTargetCandidate(
            GrappleTargetKind kind,
            float distanceCells,
            int stableOrder,
            Vector2 point,
            Collider2D collider)
        {
            Kind = kind;
            DistanceCells = distanceCells;
            StableOrder = stableOrder;
            Point = point;
            Collider = collider;
        }

        public GrappleTargetKind Kind { get; }
        public float DistanceCells { get; }
        public int StableOrder { get; }
        public Vector2 Point { get; }
        public Collider2D Collider { get; }
    }

    public static class GrappleTargetSelector
    {
        private const float SameDistanceTolerance = 0.001f;

        public static bool TrySelect(
            IReadOnlyList<GrappleTargetCandidate> candidates,
            float maxRangeCells,
            out GrappleTargetCandidate selected)
        {
            selected = default;
            if (candidates == null || maxRangeCells <= 0f)
            {
                return false;
            }

            bool found = false;
            for (int index = 0; index < candidates.Count; index++)
            {
                GrappleTargetCandidate candidate = candidates[index];
                if (candidate.Kind == GrappleTargetKind.None
                    || candidate.DistanceCells < 0f
                    || candidate.DistanceCells > maxRangeCells)
                {
                    continue;
                }

                if (!found || Compare(candidate, selected) < 0)
                {
                    selected = candidate;
                    found = true;
                }
            }

            return found;
        }

        private static int Compare(
            GrappleTargetCandidate left,
            GrappleTargetCandidate right)
        {
            float distanceDifference = left.DistanceCells - right.DistanceCells;
            if (Mathf.Abs(distanceDifference) > SameDistanceTolerance)
            {
                return distanceDifference < 0f ? -1 : 1;
            }

            int kindOrder = GetKindPriority(left.Kind)
                .CompareTo(GetKindPriority(right.Kind));
            if (kindOrder != 0)
            {
                return kindOrder;
            }

            return left.StableOrder.CompareTo(right.StableOrder);
        }

        private static int GetKindPriority(GrappleTargetKind kind)
        {
            switch (kind)
            {
                case GrappleTargetKind.BossHook:
                    return 0;
                case GrappleTargetKind.Pullable:
                    return 1;
                case GrappleTargetKind.FixedTerrain:
                    return 2;
                default:
                    return int.MaxValue;
            }
        }
    }
}

#endif
