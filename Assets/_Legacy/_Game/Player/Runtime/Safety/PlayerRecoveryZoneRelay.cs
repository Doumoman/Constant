#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Player.Safety
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerRecoveryZoneRelay : MonoBehaviour
    {
        [SerializeField] private PlayerRecoveryCause recoveryCause = PlayerRecoveryCause.VoidRecoveryZone;

        public PlayerRecoveryCause RecoveryCause => recoveryCause;

        private void Awake()
        {
            Collider2D zone = GetComponent<Collider2D>();
            zone.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerOutOfBoundsGuard guard = other.GetComponentInParent<PlayerOutOfBoundsGuard>();
            if (guard != null)
            {
                guard.Recover(recoveryCause);
            }
        }

        public void Configure(PlayerRecoveryCause cause)
        {
            recoveryCause = cause;
            Collider2D zone = GetComponent<Collider2D>();
            if (zone != null)
            {
                zone.isTrigger = true;
            }
        }
    }
}

#endif
