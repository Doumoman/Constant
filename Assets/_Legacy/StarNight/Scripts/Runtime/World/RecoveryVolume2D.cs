#if LEGACY_DISABLED
using StarNight.Player;
using UnityEngine;

namespace StarNight.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class RecoveryVolume2D : MonoBehaviour
    {
        [SerializeField] private RecoveryReason reason = RecoveryReason.Fall;

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerRecovery recovery = other.GetComponentInParent<PlayerRecovery>();
            if (recovery != null)
            {
                recovery.Recover(reason);
            }
        }
    }
}

#endif
