#if LEGACY_DISABLED
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RopeClimbController : MonoBehaviour,
        IPlayerRopeMovementHandler,
        IPlayerMovementOverride
    {
        public const float ContactGraceSeconds = 0.12f;
        public const float JumpExitVerticalSpeed = 6.5f;

        [SerializeField] private RopeDefinition definition;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerHandSlot handSlot;
        [SerializeField] private PlayerActionLock actionLock;

        private RopeSegmentRuntime nearbySegment;
        private float lastContactAt = float.NegativeInfinity;
        private float horizontalInput;
        private float verticalInput;
        private float ropeColumnX;

        public bool IsRopeClimbing { get; private set; }
        public bool IsMovementOverrideActive => IsRopeClimbing;

        private void Awake() => ResolveDependencies();

        private void Update()
        {
            if (!IsRopeClimbing)
            {
                if (nearbySegment != null
                    && nearbySegment.IsAttached
                    && Time.time - lastContactAt <= ContactGraceSeconds
                    && Mathf.Abs(verticalInput) > 0.5f)
                {
                    TryBeginClimb(nearbySegment);
                }
                return;
            }

            if (nearbySegment == null
                || !nearbySegment.IsAttached
                || Time.time - lastContactAt > ContactGraceSeconds)
            {
                EndClimb(false);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            RopeSegmentRuntime segment = other != null ? other.GetComponentInParent<RopeSegmentRuntime>() : null;
            if (segment == null || !segment.IsAttached)
            {
                return;
            }
            nearbySegment = segment;
            lastContactAt = Time.time;
        }

        public void SetRopeInput(float horizontal, float vertical)
        {
            horizontalInput = Mathf.Clamp(horizontal, -1f, 1f);
            verticalInput = Mathf.Clamp(vertical, -1f, 1f);
        }

        public bool TryJumpExit()
        {
            if (!IsRopeClimbing)
            {
                return false;
            }
            EndClimb(true);
            return true;
        }

        public bool TryBeginClimb(RopeSegmentRuntime segment)
        {
            ResolveDependencies();
            if (segment == null || !segment.IsAttached
                || handSlot != null && !handSlot.CanClimbRope
                || actionLock != null && !actionLock.AllowsGameplayActions)
            {
                return false;
            }

            nearbySegment = segment;
            lastContactAt = Time.time;
            ropeColumnX = segment.transform.position.x;
            IsRopeClimbing = true;
            body.linearVelocity = Vector2.zero;
            actionLock?.SetState(PlayerActionState.RopeClimbing);
            return true;
        }

        public void ApplyMovementOverride(Rigidbody2D targetBody, float fixedDeltaTime)
        {
            if (!IsRopeClimbing || targetBody == null)
            {
                return;
            }
            float climbSpeed = definition != null
                ? definition.ClimbCellsPerSecond
                : RopeDefinition.ApprovedClimbCellsPerSecond;
            float swing = definition != null ? definition.SwingCells : RopeDefinition.ApprovedSwingCells;
            float targetX = ropeColumnX + horizontalInput * swing;
            float xSpeed = (targetX - targetBody.position.x) / Mathf.Max(0.001f, fixedDeltaTime);
            targetBody.linearVelocity = new Vector2(
                Mathf.Clamp(xSpeed, -climbSpeed, climbSpeed),
                verticalInput * climbSpeed);
        }

        public void ConfigureForTests(
            RopeDefinition configuredDefinition,
            Rigidbody2D configuredBody,
            PlayerHandSlot configuredHandSlot,
            PlayerActionLock configuredLock)
        {
            definition = configuredDefinition;
            body = configuredBody;
            handSlot = configuredHandSlot;
            actionLock = configuredLock;
        }

        private void EndClimb(bool jump)
        {
            IsRopeClimbing = false;
            if (body != null)
            {
                body.linearVelocity = jump
                    ? new Vector2(horizontalInput * 4f, JumpExitVerticalSpeed)
                    : Vector2.zero;
            }
            actionLock?.SetState(handSlot != null && !handSlot.IsEmpty
                ? PlayerActionState.Carrying
                : PlayerActionState.Free);
            if (jump)
            {
                nearbySegment = null;
                lastContactAt = float.NegativeInfinity;
            }
        }

        private void ResolveDependencies()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
            if (handSlot == null)
            {
                handSlot = GetComponent<PlayerHandSlot>();
            }
            if (actionLock == null)
            {
                actionLock = GetComponent<PlayerActionLock>();
            }
        }
    }
}

#endif
