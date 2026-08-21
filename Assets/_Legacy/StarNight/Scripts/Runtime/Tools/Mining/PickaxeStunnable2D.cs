#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Tools.Mining
{
    [DisallowMultipleComponent]
    public sealed class PickaxeStunnable2D : MonoBehaviour, IPickaxeStunnable
    {
        [SerializeField] private bool acceptsStun = true;
        [SerializeField, Min(0f)] private float stunRemaining;

        public event Action<PickaxeStunContext> Stunned;

        public UnityEngine.Object PickaxeStunTargetObject => this;
        public bool CanReceivePickaxeStun =>
            acceptsStun && isActiveAndEnabled;
        public bool IsStunned => stunRemaining > 0f;
        public float StunRemaining => stunRemaining;
        public int StunCount { get; private set; }

        private void FixedUpdate()
        {
            stunRemaining = Mathf.Max(
                0f,
                stunRemaining - Time.fixedDeltaTime);
        }

        public bool TryReceivePickaxeStun(PickaxeStunContext context)
        {
            if (!CanReceivePickaxeStun)
            {
                return false;
            }

            stunRemaining = Mathf.Max(
                stunRemaining,
                context.DurationSeconds);
            StunCount++;
            Stunned?.Invoke(context);
            return true;
        }

        public void SetAcceptsStun(bool accepts)
        {
            acceptsStun = accepts;
        }
    }
}

#endif
