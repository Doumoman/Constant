#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Interaction.Targeting
{
    public sealed class InteractionTargetSelector
    {
        private const float TieEpsilon = 0.0001f;

        public InteractionCandidate Select(
            IReadOnlyList<InteractionCandidate> candidates,
            Vector2 origin,
            Vector2 facing,
            ContextReceiverQuery query,
            InteractionCandidate previous)
        {
            Vector2 facingDirection = facing.sqrMagnitude > TieEpsilon
                ? facing.normalized
                : Vector2.right;
            InteractionCandidate best = null;
            float bestFacingDot = float.NegativeInfinity;
            float bestDistanceSquared = float.PositiveInfinity;

            for (int index = 0; index < candidates.Count; index++)
            {
                InteractionCandidate candidate = candidates[index];
                if (candidate == null || !candidate.IsSelectable(query))
                {
                    continue;
                }

                Vector2 offset = candidate.AnchorPosition - origin;
                float distanceSquared = offset.sqrMagnitude;
                float facingDot = distanceSquared <= TieEpsilon
                    ? 1f
                    : Vector2.Dot(facingDirection, offset.normalized);

                if (best == null
                    || IsBetter(
                        candidate,
                        facingDot,
                        distanceSquared,
                        best,
                        bestFacingDot,
                        bestDistanceSquared,
                        previous))
                {
                    best = candidate;
                    bestFacingDot = facingDot;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return best;
        }

        private static bool IsBetter(
            InteractionCandidate candidate,
            float facingDot,
            float distanceSquared,
            InteractionCandidate best,
            float bestFacingDot,
            float bestDistanceSquared,
            InteractionCandidate previous)
        {
            if (candidate.Priority != best.Priority)
            {
                return candidate.Priority > best.Priority;
            }

            if (Mathf.Abs(facingDot - bestFacingDot) > TieEpsilon)
            {
                return facingDot > bestFacingDot;
            }

            if (Mathf.Abs(distanceSquared - bestDistanceSquared) > TieEpsilon)
            {
                return distanceSquared < bestDistanceSquared;
            }

            bool candidateWasPrevious = candidate == previous;
            bool bestWasPrevious = best == previous;
            if (candidateWasPrevious != bestWasPrevious)
            {
                return candidateWasPrevious;
            }

            return candidate.StableRuntimeId < best.StableRuntimeId;
        }
    }
}

#endif
