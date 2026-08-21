#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerInputAdapter input;
        [SerializeField] private GroundProbe2D groundProbe;
        [SerializeField] private P1MovementTuning tuning;

        private JumpGraceState jumpGrace;
        private bool initialized;
        private bool grounded;
        private int jumpCount;
        private bool controlLocked;

        public event Action Jumped;
        public event Action<bool> GroundedChanged;

        public Rigidbody2D Body => body;
        public PlayerInputAdapter Input => input;
        public P1MovementTuning Tuning => tuning;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public bool IsGrounded => grounded;
        public int JumpCount => jumpCount;
        public float CoyoteRemaining => jumpGrace.CoyoteRemaining;
        public float BufferRemaining => jumpGrace.BufferRemaining;
        public bool ControlLocked => controlLocked;

        public void Configure(
            Rigidbody2D targetBody,
            PlayerInputAdapter inputAdapter,
            GroundProbe2D probe,
            P1MovementTuning movementTuning)
        {
            body = targetBody;
            input = inputAdapter;
            groundProbe = probe;
            tuning = movementTuning;
            Initialize();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (input == null)
            {
                input = GetComponent<PlayerInputAdapter>();
            }

            if (groundProbe == null)
            {
                groundProbe = GetComponent<GroundProbe2D>();
            }

            Initialize();
        }

        private void FixedUpdate()
        {
            SimulateFixedStep(Time.fixedDeltaTime);
        }

        public void SimulateFixedStep(float deltaTime)
        {
            if (!initialized || body == null || tuning == null)
            {
                return;
            }

            if (controlLocked)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                return;
            }

            bool previousGrounded = grounded;
            bool probeGrounded = groundProbe != null && groundProbe.Refresh();
            grounded = probeGrounded && body.linearVelocity.y <= 0.05f;
            if (grounded != previousGrounded)
            {
                GroundedChanged?.Invoke(grounded);
            }

            jumpGrace.Tick(deltaTime, grounded);
            if (input != null && input.ConsumeJumpPressed())
            {
                jumpGrace.BufferJump();
            }

            Vector2 velocity = body.linearVelocity;
            float moveX = input != null ? Mathf.Clamp(input.Move.x, -1f, 1f) : 0f;
            float targetSpeed = moveX * tuning.MaxRunSpeed;
            float acceleration = Mathf.Abs(moveX) > 0.001f
                ? tuning.GroundAcceleration
                : tuning.GroundDeceleration;

            if (!grounded)
            {
                acceleration *= tuning.AirControl;
            }

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                targetSpeed,
                acceleration * deltaTime);

            if (jumpGrace.TryConsume())
            {
                velocity.y = tuning.JumpSpeed;
                grounded = false;
                jumpCount++;
                Jumped?.Invoke();
            }
            else if (grounded && velocity.y <= 0f)
            {
                velocity.y = 0f;
            }
            else
            {
                float gravityMultiplier = velocity.y > 0f && input != null && !input.JumpHeld
                    ? tuning.JumpReleaseGravityMultiplier
                    : 1f;
                velocity.y = Mathf.Max(
                    velocity.y - tuning.Gravity * gravityMultiplier * deltaTime,
                    -tuning.MaxFallSpeed);
            }

            body.linearVelocity = velocity;
        }

        public void ResetMotionAfterRecovery()
        {
            jumpGrace.Reset();
            grounded = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        public void SetControlLocked(bool locked)
        {
            controlLocked = locked;
            if (!locked)
            {
                return;
            }

            jumpGrace.Reset();
            grounded = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        private void Initialize()
        {
            if (body == null || tuning == null)
            {
                initialized = false;
                return;
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            jumpGrace = new JumpGraceState(tuning.CoyoteTime, tuning.JumpBufferTime);
            initialized = true;
        }
    }
}

#endif
