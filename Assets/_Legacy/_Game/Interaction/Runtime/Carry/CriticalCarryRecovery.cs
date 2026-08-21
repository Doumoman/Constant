#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Interaction.Carry
{
    [DisallowMultipleComponent]
    public sealed class CriticalObjectAnchor : MonoBehaviour
    {
        public Vector2 Position => transform.position;
    }

    [DisallowMultipleComponent]
    public sealed class CarryVoidRecoveryRelay : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            other?.GetComponentInParent<CarryObjectOutOfBoundsGuard>()?.NotifyEnteredVoid();
        }
    }
}

#endif
