#if LEGACY_DISABLED
using System;
using StarNight.Core.Flow;
using StarNight.Core.Player;
using StarNight.Core.State;
using StarNight.Player.Motor;
using UnityEngine;

namespace StarNight.Player.Safety
{
    public enum PlayerRecoveryCause
    {
        RoomBounds,
        VoidRecoveryZone,
        HardFailSafePlane
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor2D))]
    public sealed class PlayerOutOfBoundsGuard : MonoBehaviour
    {
        public const float RequiredRecoveryInvulnerabilitySeconds =
            PlayerGridContract.VoidRecoveryInvulnerabilitySeconds;

        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private Rect roomBounds = new Rect(-12f, -5f, 24f, 12f);
        [SerializeField] private float horizontalMargin = 0.5f;
        [SerializeField] private float belowRoomThreshold = 1f;
        [SerializeField] private bool logRecoveries = true;

        private float recoveryInvulnerabilityRemaining;
        [SerializeField] private SafeCellState safeCellState;
        private int hardFailSafeTriggerCount;

        public event Action<Vector2> Recovered;
        public event Action HardFailSafeTriggered;

        public Rect RoomBounds => roomBounds;
        public Vector2 LastSafePosition => safeCellState.PlayerCenter;
        public Vector2Int LastSafeCell => safeCellState.Cell;
        public SafeCellState CurrentSafeCell => safeCellState;
        public bool HasSafePosition => safeCellState.IsValid;
        public bool IsRecoveryInvulnerable => recoveryInvulnerabilityRemaining > 0f;
        public float RecoveryInvulnerabilityRemaining => recoveryInvulnerabilityRemaining;
        public PlayerRecoveryCause LastRecoveryCause { get; private set; }
        public int HardFailSafeTriggerCount => hardFailSafeTriggerCount;

        private void Awake()
        {
            if (motor == null)
            {
                motor = GetComponent<PlayerMotor2D>();
            }

            SetSafePosition(transform.position);
        }

        private void FixedUpdate()
        {
            recoveryInvulnerabilityRemaining = Mathf.Max(
                0f,
                recoveryInvulnerabilityRemaining - Time.fixedDeltaTime);

            Vector2 position = motor.Body.position;
            if (IsOutsideRecoveryBoundary(position))
            {
                Recover();
                return;
            }

        }

        public void Configure(Rect bounds, Vector2 initialSafePosition, bool shouldLogRecoveries = false)
        {
            roomBounds = bounds;
            logRecoveries = shouldLogRecoveries;
            SetSafePosition(initialSafePosition);
        }

        public void SetSafePosition(Vector2 position)
        {
            SetSafeCell(SafeCellState.FromPlayerCenter(position));
        }

        public void SetSafeCell(SafeCellState state)
        {
            if (!state.IsValid)
            {
                throw new ArgumentException("SafeCell state must be valid.", nameof(state));
            }

            safeCellState = state;
        }

        public bool IsOutsideRecoveryBoundary(Vector2 position)
        {
            return position.x < roomBounds.xMin - horizontalMargin ||
                   position.x > roomBounds.xMax + horizontalMargin ||
                   position.y > roomBounds.yMax + horizontalMargin ||
                   position.y < roomBounds.yMin - belowRoomThreshold;
        }

        public void Recover()
        {
            Recover(PlayerRecoveryCause.RoomBounds);
        }

        public void Recover(PlayerRecoveryCause cause)
        {
            if (!safeCellState.IsValid)
            {
                SetSafePosition(roomBounds.center);
            }

            LastRecoveryCause = cause;
            motor.SnapTo(safeCellState.PlayerCenter);
            recoveryInvulnerabilityRemaining = RequiredRecoveryInvulnerabilitySeconds;
            ApplyFallDamage();
            Recovered?.Invoke(safeCellState.PlayerCenter);

            if (cause == PlayerRecoveryCause.HardFailSafePlane)
            {
                hardFailSafeTriggerCount++;
                HardFailSafeTriggered?.Invoke();
                Debug.LogError(
                    $"HardFailSafePlane activated. QA failure; recovered player to SafeCell {safeCellState.Cell}.",
                    this);
                return;
            }

            if (logRecoveries)
            {
                Debug.LogWarning(
                    $"PlayerOutOfBoundsGuard recovered the player to SafeCell {safeCellState.Cell} ({cause}).",
                    this);
            }
        }

        private static void ApplyFallDamage()
        {
            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap == null || bootstrap.Settings?.accessibility?.removeFallDamage == true)
            {
                return;
            }

            if (!bootstrap.Services.TryGet(out RunManager runManager) || runManager.Current == null)
            {
                return;
            }

            RunState run = runManager.Current;
            run.health = Mathf.Max(0, run.health - 1);
            if (run.health == 0 && run.phase == RunPhase.Running)
            {
                run.phase = RunPhase.Failed;
            }
        }
    }
}

#endif
