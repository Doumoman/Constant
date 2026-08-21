#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using StarNight.Core.Player;
using UnityEngine;

namespace StarNight.Player.Motor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour, IPlayerMovementInputSink, IPlayerSpecialJumpExecutor
    {
        public const float ColliderWidth = PlayerGridContract.ColliderWidth;
        public const float ColliderHeight = PlayerGridContract.ColliderHeight;

        [SerializeField] private PlayerMotionSettings settings = new PlayerMotionSettings();
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundMask = (1 << 6) | (1 << 7);

        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private float moveInput;
        private float lookInput;
        private float coyoteRemaining;
        private float jumpBufferRemaining;
        private bool jumpHeld;
        private bool jumpCutRequested;
        private bool jumpCutApplied;
        private Collider2D groundCollider;
        private IPlayerMovementOverride movementOverride;
        private IPlayerAirMovementModifier airMovementModifier;

        public PlayerMotionSettings Settings => settings;
        public Rigidbody2D Body => body;
        public CapsuleCollider2D Capsule => capsule;
        public bool IsGrounded { get; private set; }
        public int Facing { get; private set; } = 1;
        public float LookInput => lookInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            if (groundCheck == null)
            {
                Transform candidate = transform.Find("GroundCheck");
                groundCheck = candidate != null ? candidate : transform;
            }

            ApplyRequiredPhysicsConfiguration();
            ResolveMovementOverride();
        }

        private void FixedUpdate()
        {
            float step = Time.fixedDeltaTime;
            ResolveMovementOverride();
            if (movementOverride != null && movementOverride.IsMovementOverrideActive)
            {
                IsGrounded = false;
                coyoteRemaining = 0f;
                jumpBufferRemaining = 0f;
                movementOverride.ApplyMovementOverride(body, step);
                return;
            }

            IsGrounded = CheckGrounded();
            if (IsGrounded)
            {
                ResolveGroundPenetration();
            }
            coyoteRemaining = IsGrounded ? settings.coyoteTime : Mathf.Max(0f, coyoteRemaining - step);
            jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - step);

            ResolveAirMovementModifier();
            Vector2 velocity = body.linearVelocity;
            float maximumHorizontalSpeed = !IsGrounded
                && airMovementModifier != null
                && airMovementModifier.IsAirMovementModifierActive
                    ? Mathf.Min(settings.maximumMoveSpeed, airMovementModifier.MaximumHorizontalSpeed)
                    : settings.maximumMoveSpeed;
            float targetHorizontal = moveInput * maximumHorizontalSpeed * ResolveMovementSpeedMultiplier();
            float horizontalRate = IsGrounded
                ? (Mathf.Abs(moveInput) > 0.001f ? settings.groundAcceleration : settings.groundDeceleration)
                : settings.airAcceleration;
            if (!IsGrounded
                && airMovementModifier != null
                && airMovementModifier.IsAirMovementModifierActive)
            {
                horizontalRate *= Mathf.Clamp01(airMovementModifier.AirAccelerationMultiplier);
            }
            velocity.x = Mathf.MoveTowards(velocity.x, targetHorizontal, horizontalRate * step);

            bool launchedThisStep = false;
            if (jumpBufferRemaining > 0f && coyoteRemaining > 0f)
            {
                velocity.y = settings.CalculateDiscreteJumpVelocity(step);
                jumpBufferRemaining = 0f;
                coyoteRemaining = 0f;
                IsGrounded = false;
                launchedThisStep = true;
                jumpCutApplied = false;
                jumpCutRequested = !jumpHeld;
            }

            if (!launchedThisStep)
            {
                if (IsGrounded && velocity.y < 0f)
                {
                    velocity.y = 0f;
                }
                else
                {
                    float maximumFallSpeed = !IsGrounded
                        && airMovementModifier != null
                        && airMovementModifier.IsAirMovementModifierActive
                            ? Mathf.Min(settings.maximumFallSpeed, airMovementModifier.MaximumFallSpeed)
                            : settings.maximumFallSpeed;
                    velocity.y = Mathf.Max(velocity.y - settings.Gravity * step, -maximumFallSpeed);
                }
            }

            if (jumpCutRequested && !jumpCutApplied && velocity.y > 0f)
            {
                velocity.y *= settings.jumpCutMultiplier;
                jumpCutApplied = true;
                jumpCutRequested = false;
            }

            body.linearVelocity = velocity;
        }

        public void SetMoveInput(float horizontal)
        {
            moveInput = Mathf.Clamp(horizontal, -1f, 1f);
            if (moveInput > 0.01f)
            {
                Facing = 1;
            }
            else if (moveInput < -0.01f)
            {
                Facing = -1;
            }
        }

        public void SetLookInput(float vertical)
        {
            lookInput = Mathf.Clamp(vertical, -1f, 1f);
        }

        public void SetJumpHeld(bool held)
        {
            if (jumpHeld && !held)
            {
                jumpCutRequested = true;
            }

            jumpHeld = held;
        }

        public void QueueJump()
        {
            jumpBufferRemaining = settings.jumpBufferTime;
            jumpHeld = true;
            jumpCutRequested = false;
        }

        public void ReleaseJump()
        {
            jumpHeld = false;
            jumpCutRequested = true;
        }

        public bool TryLaunchSpecialJump(float verticalVelocity, float requiredHeadClearance)
        {
            ResolveMovementOverride();
            bool canUseGroundJump = IsGrounded || coyoteRemaining > 0f;
            if (!canUseGroundJump
                || verticalVelocity <= 0f
                || movementOverride != null && movementOverride.IsMovementOverrideActive
                || !HasHeadClearance(requiredHeadClearance))
            {
                return false;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.y = verticalVelocity;
            body.linearVelocity = velocity;
            jumpBufferRemaining = 0f;
            coyoteRemaining = 0f;
            IsGrounded = false;
            jumpHeld = true;
            jumpCutRequested = false;
            jumpCutApplied = false;
            return true;
        }

        public void ClearBufferedInput()
        {
            moveInput = 0f;
            lookInput = 0f;
            jumpBufferRemaining = 0f;
            jumpHeld = false;
            jumpCutRequested = false;
        }

        public void SnapTo(Vector2 position)
        {
            body.position = position;
            transform.position = position;
            body.linearVelocity = Vector2.zero;
            coyoteRemaining = 0f;
            ClearBufferedInput();
            Physics2D.SyncTransforms();
        }

        public void ConfigureForTests(LayerMask collisionMask, PlayerMotionSettings motionSettings = null)
        {
            groundMask = collisionMask;
            if (motionSettings != null)
            {
                settings = motionSettings;
            }

            ApplyRequiredPhysicsConfiguration();
        }

        private void ApplyRequiredPhysicsConfiguration()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider2D>();
            }

            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(ColliderWidth, ColliderHeight);
        }

        private void ResolveMovementOverride()
        {
            movementOverride = null;
            IPlayerMovementOverride fallback = null;
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPlayerMovementOverride candidate)
                {
                    fallback ??= candidate;
                    if (candidate.IsMovementOverrideActive)
                    {
                        movementOverride = candidate;
                        return;
                    }
                }
            }
            movementOverride = fallback;
        }

        private float ResolveMovementSpeedMultiplier()
        {
            float multiplier = 1f;
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPlayerMovementSpeedModifier modifier)
                {
                    multiplier = Mathf.Min(multiplier, Mathf.Clamp01(modifier.MovementSpeedMultiplier));
                }
            }
            return multiplier;
        }

        private void ResolveAirMovementModifier()
        {
            airMovementModifier = null;
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPlayerAirMovementModifier candidate
                    && candidate.IsAirMovementModifierActive)
                {
                    airMovementModifier = candidate;
                    return;
                }
            }
        }

        private bool CheckGrounded()
        {
            Bounds bounds = capsule.bounds;
            Vector2 sensorCenter = groundCheck != transform
                ? groundCheck.position
                : new Vector2(bounds.center.x, bounds.min.y - settings.groundCheckDepth * 0.5f);
            Vector2 sensorSize = new Vector2(
                Mathf.Max(0.05f, bounds.size.x - settings.wallSkin * 2f),
                settings.groundCheckDepth);
            Collider2D hit = Physics2D.OverlapBox(sensorCenter, sensorSize, 0f, groundMask);
            groundCollider = hit != capsule ? hit : null;
            return groundCollider != null;
        }

        private bool HasHeadClearance(float distance)
        {
            if (distance <= 0f)
            {
                return true;
            }

            Bounds bounds = capsule.bounds;
            float width = Mathf.Max(0.05f, bounds.size.x - settings.wallSkin * 2f);
            Vector2 center = new Vector2(bounds.center.x, bounds.max.y + distance * 0.5f);
            Collider2D hit = Physics2D.OverlapBox(center, new Vector2(width, distance), 0f, groundMask);
            return hit == null || hit == capsule;
        }

        private void ResolveGroundPenetration()
        {
            ColliderDistance2D separation = Physics2D.Distance(capsule, groundCollider);
            if (!separation.isValid || !separation.isOverlapped || separation.normal.y < 0.5f)
            {
                return;
            }

            Vector2 correctedPosition = body.position + separation.normal * (-separation.distance + 0.001f);
            body.position = correctedPosition;
            transform.position = correctedPosition;
            Physics2D.SyncTransforms();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CapsuleCollider2D currentCapsule = GetComponent<CapsuleCollider2D>();
            if (currentCapsule != null)
            {
                currentCapsule.direction = CapsuleDirection2D.Vertical;
                currentCapsule.size = new Vector2(ColliderWidth, ColliderHeight);
            }
        }
#endif
    }
}

#endif
