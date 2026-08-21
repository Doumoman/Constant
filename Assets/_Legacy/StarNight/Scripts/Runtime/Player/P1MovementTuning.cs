#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Player
{
    [CreateAssetMenu(menuName = "StarNight/P1 Movement Tuning", fileName = "P1_MovementTuning")]
    public sealed class P1MovementTuning : ScriptableObject
    {
        [Header("Body")]
        [SerializeField] private Vector2 colliderSize = new Vector2(0.72f, 0.90f);

        [Header("Horizontal")]
        [SerializeField] private float maxRunSpeed = 3.75f;
        [SerializeField] private float groundAcceleration = 30f;
        [SerializeField] private float groundDeceleration = 40f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.75f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2.2f;
        [SerializeField] private float gravity = 24f;
        [SerializeField] private float jumpReleaseGravityMultiplier = 2.4f;
        [SerializeField] private float maxFallSpeed = 18f;
        [SerializeField] private float coyoteTime = 0.10f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private float groundProbeDistance = 0.08f;

        [Header("Safe Cell")]
        [SerializeField] private float safeCellDwellTime = 0.30f;
        [SerializeField] private float recoveryBelowWorldMargin = 1.0f;
        [SerializeField] private int maxHealth = 4;

        public Vector2 ColliderSize => colliderSize;
        public float MaxRunSpeed => maxRunSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirControl => airControl;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float JumpReleaseGravityMultiplier => jumpReleaseGravityMultiplier;
        public float MaxFallSpeed => maxFallSpeed;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float GroundProbeDistance => groundProbeDistance;
        public float SafeCellDwellTime => safeCellDwellTime;
        public float RecoveryBelowWorldMargin => recoveryBelowWorldMargin;
        public int MaxHealth => maxHealth;
        public float JumpSpeed => Mathf.Sqrt(2f * gravity * jumpHeight);
    }
}

#endif
