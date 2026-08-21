#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Interaction.Carry;

namespace StarNight.Interaction.Reactions
{
    public enum CarryImpactClass
    {
        None,
        LightImpact,
        HeavyImpact,
    }

    public static class ImpactClassifier
    {
        public static CarryImpactClass Classify(
            float mass,
            float relativeSpeed,
            CarryWeightClass weightClass,
            bool forceHeavyImpact = false)
        {
            if (forceHeavyImpact || weightClass == CarryWeightClass.Heavy && relativeSpeed >= 3f)
            {
                return CarryImpactClass.HeavyImpact;
            }

            float score = Math.Max(0f, mass) * Math.Max(0f, relativeSpeed);
            if (score < 2f)
            {
                return CarryImpactClass.None;
            }

            return score < 6f ? CarryImpactClass.LightImpact : CarryImpactClass.HeavyImpact;
        }
    }

    public sealed class ImpactActionDeduplicator
    {
        public const float DuplicateWindowSeconds = 0.15f;
        private const float TimeBoundaryEpsilon = 0.000001f;
        private readonly Dictionary<ImpactKey, float> lastAppliedAt = new Dictionary<ImpactKey, float>();

        public bool ShouldApply(long actionId, int targetRuntimeId, float now)
        {
            var key = new ImpactKey(actionId, targetRuntimeId);
            if (lastAppliedAt.TryGetValue(key, out float previous)
                && now - previous + TimeBoundaryEpsilon < DuplicateWindowSeconds)
            {
                return false;
            }

            lastAppliedAt[key] = now;
            return true;
        }

        public void Clear()
        {
            lastAppliedAt.Clear();
        }

        private readonly struct ImpactKey : IEquatable<ImpactKey>
        {
            private readonly long actionId;
            private readonly int targetRuntimeId;

            public ImpactKey(long actionId, int targetRuntimeId)
            {
                this.actionId = actionId;
                this.targetRuntimeId = targetRuntimeId;
            }

            public bool Equals(ImpactKey other)
            {
                return actionId == other.actionId && targetRuntimeId == other.targetRuntimeId;
            }

            public override bool Equals(object obj)
            {
                return obj is ImpactKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (actionId.GetHashCode() * 397) ^ targetRuntimeId;
                }
            }
        }
    }
}

#endif
