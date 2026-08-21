#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Player
{
    public enum RecoveryReason
    {
        Fall,
        Crush,
        OutOfBounds,
        Test
    }

    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class PlayerRecovery : MonoBehaviour
    {
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private SafeCellTracker safeCellTracker;
        [SerializeField] private P1MovementTuning tuning;

        private float recoveryCooldown;

        public event Action<RecoveryReason, Vector2> Recovered;
        public event Action<int, int> Damaged;
        public event Action HealthDepleted;

        public int CurrentHealth { get; private set; }
        public int RecoveryCount { get; private set; }
        public RecoveryReason LastReason { get; private set; }

        public void Configure(
            GridWorld world,
            Rigidbody2D targetBody,
            PlayerMotor2D playerMotor,
            SafeCellTracker tracker,
            P1MovementTuning movementTuning)
        {
            gridWorld = world;
            body = targetBody;
            motor = playerMotor;
            safeCellTracker = tracker;
            tuning = movementTuning;
            CurrentHealth = tuning != null ? tuning.MaxHealth : 4;
        }

        private void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor2D>();
            }

            if (safeCellTracker == null)
            {
                safeCellTracker = GetComponent<SafeCellTracker>();
            }

            if (tuning == null && motor != null)
            {
                tuning = motor.Tuning;
            }

            CurrentHealth = tuning != null ? tuning.MaxHealth : 4;
        }

        private void FixedUpdate()
        {
            recoveryCooldown = Mathf.Max(0f, recoveryCooldown - Time.fixedDeltaTime);
            if (gridWorld == null || body == null || tuning == null || recoveryCooldown > 0f)
            {
                return;
            }

            float threshold = gridWorld.WorldBounds.yMin - tuning.RecoveryBelowWorldMargin;
            if (body.position.y < threshold)
            {
                Recover(RecoveryReason.OutOfBounds);
            }
        }

        public bool Recover(RecoveryReason reason)
        {
            if (body == null || safeCellTracker == null || !safeCellTracker.HasSafeCell || recoveryCooldown > 0f)
            {
                return false;
            }

            ApplyDamage(1);
            LastReason = reason;
            RecoveryCount++;
            recoveryCooldown = 0.20f;

            Vector2 recoveryPosition = safeCellTracker.LastSafePosition;
            body.position = recoveryPosition;
            transform.position = new Vector3(
                recoveryPosition.x,
                recoveryPosition.y,
                transform.position.z);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            motor?.ResetMotionAfterRecovery();
            Physics2D.SyncTransforms();

            Recovered?.Invoke(reason, body.position);
            return true;
        }

        public void ResetHealthForTests()
        {
            CurrentHealth = tuning != null ? tuning.MaxHealth : 4;
            RecoveryCount = 0;
            recoveryCooldown = 0f;
        }

        public int ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return 0;
            }

            int before = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            int applied = before - CurrentHealth;
            Damaged?.Invoke(applied, CurrentHealth);
            if (CurrentHealth == 0)
            {
                HealthDepleted?.Invoke();
            }

            return applied;
        }
    }
}

#endif
