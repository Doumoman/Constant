#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stage.Secrets
{
    public static class SecretDimensionRuntimeContract
    {
        public const float ReturnMaruBiteImmunitySeconds = 0.8f;
        public const bool BellAndMaruTimeContinues = true;
    }

    [Serializable]
    public readonly struct SecretDimensionPlan : IEquatable<SecretDimensionPlan>
    {
        public SecretDimensionPlan(string secretId, int seed)
        {
            SecretId = secretId ?? string.Empty;
            Seed = seed;
        }

        public string SecretId { get; }
        public int Seed { get; }

        public bool Equals(SecretDimensionPlan other)
        {
            return Seed == other.Seed && string.Equals(SecretId, other.SecretId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SecretDimensionPlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SecretId, Seed);
        }
    }

    [DisallowMultipleComponent]
    public sealed class SecretReturnMaruBiteImmunity : MonoBehaviour
    {
        private float immuneUntilUnscaledTime;

        public bool IsActive => RemainingSeconds > 0f;
        public float RemainingSeconds => Mathf.Max(0f, immuneUntilUnscaledTime - Time.unscaledTime);

        public void Grant(float seconds = SecretDimensionRuntimeContract.ReturnMaruBiteImmunitySeconds)
        {
            immuneUntilUnscaledTime = Mathf.Max(
                immuneUntilUnscaledTime,
                Time.unscaledTime + Mathf.Max(0f, seconds));
        }
    }
}

#endif
