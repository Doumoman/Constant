#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Tiles;
using UnityEngine;

namespace StarNight.Objects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class FallingObject2D : MonoBehaviour
    {
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private TileMutationService tileMutationService;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D objectCollider;
        [SerializeField, Min(0f)] private float gravityScale = 2.25f;
        [SerializeField, Min(0f)] private float minimumCrushSpeed = 2f;
        [SerializeField, Min(0f)] private float settleSpeed = 0.10f;
        [SerializeField, Min(0f)] private float settleDuration = 0.20f;

        private float settleTimer;
        private bool subscribed;

        public event Action<FallingObject2D> BeganFalling;
        public event Action<FallingObject2D> BecameSupported;
        public event Action<FallingObject2D, PlayerRecovery> PlayerCrushed;

        public Rigidbody2D Body => body;
        public Collider2D ObjectCollider => objectCollider;
        public bool IsFalling { get; private set; }
        public bool IsSupported => !IsFalling;
        public GridPos SupportCell
        {
            get
            {
                if (gridWorld == null)
                {
                    Vector2 position = body != null ? body.position : (Vector2)transform.position;
                    return new GridPos(
                        Mathf.FloorToInt(position.x),
                        Mathf.FloorToInt(position.y) - 1);
                }

                Vector2 positionInWorld = body != null
                    ? body.position
                    : (Vector2)transform.position;
                GridPos occupiedCell = gridWorld.WorldToCell(positionInWorld);
                return new GridPos(occupiedCell.X, occupiedCell.Y - 1);
            }
        }

        public void Configure(
            GridWorld world,
            TileMutationService mutationService,
            Rigidbody2D targetBody,
            Collider2D targetCollider)
        {
            Unsubscribe();
            gridWorld = world;
            tileMutationService = mutationService;
            body = targetBody;
            objectCollider = targetCollider;
            InitializeSupportedState();
            Subscribe();
        }

        public void Configure(GridWorld world, TileMutationService mutationService)
        {
            Configure(
                world,
                mutationService,
                GetComponent<Rigidbody2D>(),
                GetComponent<Collider2D>());
        }

        private void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (tileMutationService == null)
            {
                tileMutationService = FindFirstObjectByType<TileMutationService>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (objectCollider == null)
            {
                objectCollider = GetComponent<Collider2D>();
            }

            InitializeSupportedState();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            ReevaluateSupport();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void FixedUpdate()
        {
            if (!IsFalling || body == null || gridWorld == null)
            {
                return;
            }

            if (body.linearVelocity.sqrMagnitude > settleSpeed * settleSpeed
                || !gridWorld.IsSolid(SupportCell))
            {
                settleTimer = 0f;
                return;
            }

            settleTimer += Time.fixedDeltaTime;
            if (settleTimer >= settleDuration)
            {
                SetSupported();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryCrushPlayer(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryCrushPlayer(collision);
        }

        public bool ReevaluateSupport()
        {
            if (gridWorld == null || body == null)
            {
                return false;
            }

            bool hasSupport = gridWorld.IsSolid(SupportCell);
            if (!hasSupport)
            {
                BeginFalling();
            }

            return hasSupport;
        }

        public bool BeginFalling()
        {
            if (IsFalling || body == null)
            {
                return false;
            }

            IsFalling = true;
            settleTimer = 0f;
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = gravityScale;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.WakeUp();
            BeganFalling?.Invoke(this);
            return true;
        }

        public void SetSupported()
        {
            if (body == null)
            {
                return;
            }

            bool wasFalling = IsFalling;
            IsFalling = false;
            settleTimer = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Static;

            if (wasFalling)
            {
                BecameSupported?.Invoke(this);
            }
        }

        private void InitializeSupportedState()
        {
            if (body == null)
            {
                return;
            }

            IsFalling = false;
            settleTimer = 0f;
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Static;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void HandleMutationBatchCommitted(TileMutationBatchReport _)
        {
            ReevaluateSupport();
        }

        private void Subscribe()
        {
            if (subscribed || tileMutationService == null)
            {
                return;
            }

            tileMutationService.BatchCommitted += HandleMutationBatchCommitted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || tileMutationService == null)
            {
                subscribed = false;
                return;
            }

            tileMutationService.BatchCommitted -= HandleMutationBatchCommitted;
            subscribed = false;
        }

        private void TryCrushPlayer(Collision2D collision)
        {
            if (!IsFalling || collision == null || body == null)
            {
                return;
            }

            PlayerRecovery recovery =
                collision.collider.GetComponentInParent<PlayerRecovery>();
            if (recovery == null
                || transform.position.y <= recovery.transform.position.y + 0.15f)
            {
                return;
            }

            float impactSpeed = Mathf.Max(
                -body.linearVelocity.y,
                Mathf.Abs(collision.relativeVelocity.y));
            if (impactSpeed < minimumCrushSpeed)
            {
                return;
            }

            if (recovery.Recover(RecoveryReason.Crush))
            {
                PlayerCrushed?.Invoke(this, recovery);
            }
        }
    }
}

#endif
