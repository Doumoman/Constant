#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RopeClimber2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField, Min(0f)] private float climbSpeed = 4f;
        [SerializeField, Range(0f, 1f)] private float horizontalDamping = 0.8f;
        [SerializeField, Range(0f, 0.5f)] private float inputDeadZone = 0.1f;

        private readonly HashSet<RopeSegment2D> overlappingSegments =
            new HashSet<RopeSegment2D>();

        private float climbInput;
        private float originalGravityScale;
        private bool gravityCaptured;
        private bool isClimbing;

        public Rigidbody2D Body => body;
        public bool IsClimbing => isClimbing;
        public int OverlappingSegmentCount => overlappingSegments.Count;
        public float ClimbSpeed => climbSpeed;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            CaptureGravity();
        }

        private void OnDisable()
        {
            StopClimbing();
            overlappingSegments.Clear();
        }

        private void FixedUpdate()
        {
            RemoveInvalidSegments();
            bool shouldClimb = overlappingSegments.Count > 0
                && Mathf.Abs(climbInput) > inputDeadZone;
            if (!shouldClimb)
            {
                StopClimbing();
                return;
            }

            CaptureGravity();
            isClimbing = true;
            body.gravityScale = 0f;
            Vector2 velocity = body.linearVelocity;
            velocity.x *= Mathf.Clamp01(horizontalDamping);
            velocity.y = Mathf.Clamp(climbInput, -1f, 1f) * climbSpeed;
            body.linearVelocity = velocity;
        }

        public void Configure(
            Rigidbody2D configuredBody,
            float configuredClimbSpeed = 4f,
            float configuredHorizontalDamping = 0.8f)
        {
            body = configuredBody != null
                ? configuredBody
                : GetComponent<Rigidbody2D>();
            climbSpeed = Mathf.Max(0f, configuredClimbSpeed);
            horizontalDamping = Mathf.Clamp01(configuredHorizontalDamping);
            gravityCaptured = false;
            CaptureGravity();
        }

        public void SetClimbInput(float verticalInput)
        {
            climbInput = Mathf.Clamp(verticalInput, -1f, 1f);
        }

        public bool AttachSegmentForTests(RopeSegment2D segment)
        {
            return segment != null
                && segment.IsClimbable
                && overlappingSegments.Add(segment);
        }

        public bool DetachSegmentForTests(RopeSegment2D segment)
        {
            return segment != null && overlappingSegments.Remove(segment);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            RopeSegment2D segment = other.GetComponent<RopeSegment2D>();
            if (segment != null && segment.IsClimbable)
            {
                overlappingSegments.Add(segment);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            RopeSegment2D segment = other.GetComponent<RopeSegment2D>();
            if (segment != null)
            {
                overlappingSegments.Remove(segment);
            }
        }

        private void CaptureGravity()
        {
            if (body == null || gravityCaptured)
            {
                return;
            }

            originalGravityScale = body.gravityScale;
            gravityCaptured = true;
        }

        private void StopClimbing()
        {
            if (!isClimbing)
            {
                return;
            }

            isClimbing = false;
            if (body != null && gravityCaptured)
            {
                body.gravityScale = originalGravityScale;
            }
        }

        private void RemoveInvalidSegments()
        {
            overlappingSegments.RemoveWhere(
                segment => segment == null || !segment.IsClimbable);
        }
    }
}

#endif
