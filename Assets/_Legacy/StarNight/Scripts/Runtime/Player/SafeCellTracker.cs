#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Player
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class SafeCellTracker : MonoBehaviour
    {
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CapsuleCollider2D capsule;
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private P1MovementTuning tuning;

        private GridPos candidateCell;
        private bool hasCandidate;
        private float candidateDwell;
        private GridPos lastSafeCell;
        private Vector2 lastSafePosition;
        private bool hasSafeCell;

        public event Action<GridPos, Vector2> SafeCellChanged;

        public bool HasSafeCell => hasSafeCell;
        public GridPos LastSafeCell => lastSafeCell;
        public Vector2 LastSafePosition => lastSafePosition;
        public float CandidateDwell => candidateDwell;

        public void Configure(
            GridWorld world,
            Rigidbody2D targetBody,
            CapsuleCollider2D targetCapsule,
            PlayerMotor2D playerMotor,
            P1MovementTuning movementTuning)
        {
            gridWorld = world;
            body = targetBody;
            capsule = targetCapsule;
            motor = playerMotor;
            tuning = movementTuning;
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

            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider2D>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor2D>();
            }

            if (tuning == null && motor != null)
            {
                tuning = motor.Tuning;
            }
        }

        private void Start()
        {
            if (!hasSafeCell && body != null)
            {
                SetSpawnFallback(body.position);
            }
        }

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (gridWorld == null || body == null || capsule == null || motor == null || tuning == null)
            {
                ResetCandidate();
                return;
            }

            GridPos cell = gridWorld.WorldToCell(body.position);
            bool stable = motor.IsGrounded
                && Mathf.Abs(body.linearVelocity.x) <= 0.35f
                && Mathf.Abs(body.linearVelocity.y) <= 0.15f
                && gridWorld.CanStandAt(cell, capsule.bounds.size);

            if (!stable)
            {
                ResetCandidate();
                return;
            }

            if (!hasCandidate || candidateCell != cell)
            {
                candidateCell = cell;
                hasCandidate = true;
                candidateDwell = 0f;
            }

            candidateDwell += Mathf.Max(0f, deltaTime);
            if (candidateDwell + 0.0001f < tuning.SafeCellDwellTime)
            {
                return;
            }

            Vector2 center = gridWorld.CellToWorldCenter(cell);
            Vector2 standingPosition = new Vector2(
                center.x,
                cell.Y + tuning.ColliderSize.y * 0.5f);
            Record(cell, standingPosition);
        }

        public void SetSpawnFallback(Vector2 spawnPosition)
        {
            if (gridWorld != null)
            {
                GridPos cell = gridWorld.WorldToCell(spawnPosition);
                Vector2 center = gridWorld.CellToWorldCenter(cell);
                spawnPosition = new Vector2(center.x, cell.Y + (tuning != null ? tuning.ColliderSize.y * 0.5f : 0.45f));
                Record(cell, spawnPosition);
            }
            else
            {
                lastSafeCell = new GridPos(
                    Mathf.FloorToInt(spawnPosition.x),
                    Mathf.FloorToInt(spawnPosition.y));
                lastSafePosition = spawnPosition;
                hasSafeCell = true;
            }
        }

        private void Record(GridPos cell, Vector2 position)
        {
            bool changed = !hasSafeCell || lastSafeCell != cell || (lastSafePosition - position).sqrMagnitude > 0.0001f;
            lastSafeCell = cell;
            lastSafePosition = position;
            hasSafeCell = true;

            if (changed)
            {
                SafeCellChanged?.Invoke(cell, position);
            }
        }

        private void ResetCandidate()
        {
            hasCandidate = false;
            candidateDwell = 0f;
        }
    }
}

#endif
