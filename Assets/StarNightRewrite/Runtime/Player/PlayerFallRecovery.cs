using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(PlayerMotor2D))]
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(SafeAnchorService))]
    [DisallowMultipleComponent]
    public sealed class PlayerFallRecovery : MonoBehaviour
    {
        [SerializeField]
        private float fallThreshold = -7f;

        [SerializeField]
        private float recoveryCooldown = 0.25f;

        private PlayerMotor2D motor;
        private PlayerHealth health;
        private SafeAnchorService safeAnchor;
        private float nextRecoveryTime;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor2D>();
            health = GetComponent<PlayerHealth>();
            safeAnchor = GetComponent<SafeAnchorService>();
        }

        private void Update()
        {
            if (motor.Position.y >= fallThreshold || Time.time < nextRecoveryTime)
            {
                return;
            }

            nextRecoveryTime = Time.time + recoveryCooldown;
            if (safeAnchor.Recover(motor))
            {
                health.TakeFallDamage();
            }
        }
    }
}
