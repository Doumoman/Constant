#if LEGACY_DISABLED
using StarNight.Grid;
using StarNight.Player;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P1GridLabTelemetry : MonoBehaviour
    {
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private SafeCellTracker safeCellTracker;
        [SerializeField] private PlayerRecovery recovery;
        [SerializeField] private Rect tunnelBounds = new Rect(3f, 1f, 7f, 1f);

        private float sessionSeconds;
        private float stallSeconds;
        private bool stallCounted;
        private int entrySide;
        private float previousX;

        public int TunnelFailureCount { get; private set; }
        public int TunnelPassageCount { get; private set; }
        public float SessionSeconds => sessionSeconds;
        public bool TenMinuteSessionComplete => sessionSeconds >= 600f;

        public void Configure(
            GridWorld world,
            PlayerMotor2D playerMotor,
            SafeCellTracker tracker,
            PlayerRecovery playerRecovery)
        {
            gridWorld = world;
            motor = playerMotor;
            safeCellTracker = tracker;
            recovery = playerRecovery;
            previousX = motor != null ? motor.transform.position.x : 0f;
        }

        private void Update()
        {
            sessionSeconds += Time.unscaledDeltaTime;
            if (motor == null)
            {
                return;
            }

            Vector2 position = motor.Body.position;
            bool inside = tunnelBounds.Contains(position);
            bool wasLeft = previousX < tunnelBounds.xMin;
            bool wasRight = previousX > tunnelBounds.xMax;

            if (inside && entrySide == 0)
            {
                if (wasLeft)
                {
                    entrySide = -1;
                }
                else if (wasRight)
                {
                    entrySide = 1;
                }
            }
            else if (!inside && entrySide != 0)
            {
                bool exitedOpposite = entrySide < 0
                    ? position.x > tunnelBounds.xMax
                    : position.x < tunnelBounds.xMin;
                if (exitedOpposite)
                {
                    TunnelPassageCount++;
                }

                if (position.x < tunnelBounds.xMin || position.x > tunnelBounds.xMax)
                {
                    entrySide = 0;
                }
            }

            float moveIntent = motor.Input != null ? Mathf.Abs(motor.Input.Move.x) : 0f;
            if (inside && moveIntent > 0.5f && Mathf.Abs(motor.Velocity.x) < 0.15f)
            {
                stallSeconds += Time.unscaledDeltaTime;
                if (stallSeconds >= 0.25f && !stallCounted)
                {
                    TunnelFailureCount++;
                    stallCounted = true;
                }
            }
            else
            {
                stallSeconds = 0f;
                stallCounted = false;
            }

            previousX = position.x;
        }

        private void OnGUI()
        {
            if (motor == null || gridWorld == null)
            {
                return;
            }

            GridPos cell = gridWorld.WorldToCell(motor.Body.position);
            string safe = safeCellTracker != null && safeCellTracker.HasSafeCell
                ? safeCellTracker.LastSafeCell.ToString()
                : "none";
            int health = recovery != null ? recovery.CurrentHealth : 0;

            GUI.Box(new Rect(14f, 14f, 335f, 190f), string.Empty);
            GUILayout.BeginArea(new Rect(28f, 24f, 310f, 170f));
            GUILayout.Label("P1 GRID LAB 30 x 18");
            GUILayout.Label("Move: A/D or Stick   Jump: Space / South");
            GUILayout.Label($"Cell {cell}   Velocity {motor.Velocity.x:0.00}, {motor.Velocity.y:0.00}");
            GUILayout.Label($"Grounded {motor.IsGrounded}   Safe {safe}   HP {health}/4");
            GUILayout.Label($"Session {sessionSeconds / 60f:00}:{sessionSeconds % 60f:00} / 10:00");
            GUILayout.Label($"1-cell passages {TunnelPassageCount}   failures {TunnelFailureCount}");
            GUILayout.Label("Gate: 10:00 comfort + 0 tunnel failures");
            GUILayout.EndArea();
        }
    }
}

#endif
