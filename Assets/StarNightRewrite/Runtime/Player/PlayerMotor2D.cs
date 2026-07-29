using System;
using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [Header("Horizontal")]
        [SerializeField]
        private float maxRunSpeed = 5.5f;

        [SerializeField]
        private float groundAcceleration = 35f;

        [SerializeField]
        private float groundDeceleration = 45f;

        [SerializeField, Range(0f, 1f)]
        private float airControlRatio = 0.75f;

        [Header("Jump")]
        [SerializeField]
        private float jumpHeight = 2.8f;

        [SerializeField]
        private float jumpBuffer = 0.12f;

        [SerializeField]
        private float coyoteTime = 0.12f;

        [SerializeField, Range(0.1f, 1f)]
        private float earlyReleaseMultiplier = 0.45f;

        [Header("Collision")]
        [SerializeField]
        private LayerMask groundMask;

        [SerializeField]
        private float groundProbeDistance = 0.08f;

        [SerializeField]
        private float fallSpeedCap = 12f;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private PlayerInputReader input;
        private PlayerJumpAssist jumpAssist;
        private ContactFilter2D groundFilter;
        private bool jumpCutRequested;
        private float supportVelocityX;

        public event Action<bool> GroundedChanged;

        public bool IsGrounded { get; private set; }
        public int FacingSign { get; private set; } = 1;
        public Vector2 Position =>
            body != null ? body.position : (Vector2)transform.position;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            input.JumpPressed += OnJumpPressed;
            input.JumpReleased += OnJumpReleased;
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.JumpPressed -= OnJumpPressed;
                input.JumpReleased -= OnJumpReleased;
            }
        }

        private void FixedUpdate()
        {
            EnsureInitialized();
            UpdateGrounded();
            jumpAssist.Tick(Time.fixedDeltaTime, IsGrounded);

            Vector2 velocity = body.linearVelocity;
            float moveInput = Mathf.Clamp(input.MoveX, -1f, 1f);
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                FacingSign = moveInput > 0f ? 1 : -1;
            }

            float targetSpeed = moveInput * maxRunSpeed + supportVelocityX;
            float acceleration = Mathf.Abs(moveInput) > 0.01f
                ? groundAcceleration
                : groundDeceleration;
            if (!IsGrounded)
            {
                acceleration *= airControlRatio;
            }

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                targetSpeed,
                acceleration * Time.fixedDeltaTime);

            if (jumpAssist.TryConsumeJump())
            {
                float gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
                velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
                jumpAssist.ClearCoyoteTime();
                SetGrounded(false);
            }

            if (jumpCutRequested && velocity.y > 0f)
            {
                velocity.y *= earlyReleaseMultiplier;
            }

            jumpCutRequested = false;
            velocity.y = Mathf.Max(velocity.y, -fallSpeedCap);
            body.linearVelocity = velocity;
        }

        public void Teleport(Vector2 worldPosition)
        {
            EnsureInitialized();
            body.position = worldPosition;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        private void UpdateGrounded()
        {
            int hitCount = capsule.Cast(
                Vector2.down,
                groundFilter,
                groundHits,
                groundProbeDistance,
                true);

            bool grounded = false;
            supportVelocityX = 0f;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = groundHits[index];
                if (hit.normal.y < 0.45f)
                {
                    continue;
                }

                grounded = true;
                if (hit.rigidbody != null)
                {
                    supportVelocityX = hit.rigidbody.linearVelocity.x;
                }

                break;
            }

            SetGrounded(grounded);
        }

        private void SetGrounded(bool grounded)
        {
            if (IsGrounded == grounded)
            {
                return;
            }

            IsGrounded = grounded;
            GroundedChanged?.Invoke(grounded);
        }

        private void OnJumpPressed()
        {
            jumpAssist.BufferJump();
        }

        private void OnJumpReleased()
        {
            jumpCutRequested = true;
        }

        private void EnsureInitialized()
        {
            if (body != null &&
                capsule != null &&
                input != null &&
                jumpAssist != null)
            {
                return;
            }

            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            input = GetComponent<PlayerInputReader>();
            jumpAssist = new PlayerJumpAssist(coyoteTime, jumpBuffer);

            if (groundMask.value == 0)
            {
                groundMask = LayerMask.GetMask("Ground", "MovingPlatform");
            }

            groundFilter = new ContactFilter2D
            {
                useTriggers = false
            };
            groundFilter.SetLayerMask(groundMask);

            body.freezeRotation = true;
            body.gravityScale = 3.2f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }
}
