#if LEGACY_DISABLED
using System;
using StarNight.Core.Player;
using UnityEngine;

namespace StarNight.Player.Motor
{
    [Serializable]
    public sealed class PlayerMotionSettings
    {
        public const float RequiredFixedDeltaTime = PlayerGridContract.FixedDeltaTime;

        [Min(0f)] public float maximumMoveSpeed = PlayerGridContract.GroundMoveSpeed;
        [Min(0f)] public float groundAcceleration = 48f;
        [Min(0f)] public float groundDeceleration = 60f;
        [Min(0f)] public float airAcceleration = 28f;
        [Min(0.01f)] public float gravity = PlayerGridContract.Gravity;
        [Min(0.01f)] public float baseJumpVelocity = PlayerGridContract.BaseJumpVelocity;
        [Min(0f)] public float coyoteTime = PlayerGridContract.CoyoteTimeSeconds;
        [Min(0f)] public float jumpBufferTime = PlayerGridContract.JumpBufferSeconds;
        [Min(0.01f)] public float maximumFallSpeed = 12f;
        [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.55f;
        [Min(0f)] public float groundCheckDepth = 0.08f;
        [Min(0f)] public float wallSkin = 0.04f;

        public float Gravity => gravity;
        public float BaseJumpApex => baseJumpVelocity * baseJumpVelocity / (2f * gravity);
        public float SameHeightAirTime => 2f * baseJumpVelocity / gravity;
        public float FullSpeedHorizontalDistance => maximumMoveSpeed * SameHeightAirTime;

        public float CalculateDiscreteJumpVelocity(float fixedDeltaTime)
        {
            _ = fixedDeltaTime;
            return baseJumpVelocity;
        }

        public float PredictDiscreteApexHeight(float fixedDeltaTime)
        {
            float step = Mathf.Max(0.0001f, fixedDeltaTime);
            float velocity = baseJumpVelocity;
            float height = 0f;
            while (velocity > 0f)
            {
                height += velocity * step;
                velocity -= gravity * step;
            }

            return height;
        }
    }
}

#endif
