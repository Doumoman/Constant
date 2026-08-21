#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Player.Motor
{
    public readonly struct PlayerTraversalResult
    {
        public PlayerTraversalResult(bool landed, Vector2 finalCenter, int simulatedSteps)
        {
            Landed = landed;
            FinalCenter = finalCenter;
            SimulatedSteps = simulatedSteps;
        }

        public bool Landed { get; }
        public Vector2 FinalCenter { get; }
        public int SimulatedSteps { get; }
    }

    /// <summary>
    /// Uses the same 1/60 second motion contract as PlayerMotor2D and sweeps the
    /// player capsule for every simulated step. Map validation must use this
    /// instead of accepting a route from horizontal distance alone.
    /// </summary>
    public static class PlayerTraversalSolver2D
    {
        private const int DefaultMaximumSteps = 120;
        private const float LandingNormalThreshold = 0.5f;

        public static PlayerTraversalResult SimulateFullSpeedJump(
            PhysicsScene2D physicsScene,
            Vector2 startCenter,
            int horizontalDirection,
            int collisionLayerMask,
            PlayerMotionSettings settings = null,
            int maximumSteps = DefaultMaximumSteps)
        {
            settings ??= new PlayerMotionSettings();
            float step = PlayerMotionSettings.RequiredFixedDeltaTime;
            Vector2 position = startCenter;
            Vector2 velocity = new Vector2(
                Mathf.Sign(horizontalDirection) * settings.maximumMoveSpeed,
                settings.baseJumpVelocity);

            for (int index = 0; index < Mathf.Max(1, maximumSteps); index++)
            {
                Vector2 delta = velocity * step;
                float distance = delta.magnitude;
                if (distance > 0.0001f)
                {
                    Vector2 direction = delta / distance;
                    RaycastHit2D hit = physicsScene.CapsuleCast(
                        position,
                        new Vector2(PlayerMotor2D.ColliderWidth, PlayerMotor2D.ColliderHeight),
                        CapsuleDirection2D.Vertical,
                        0f,
                        direction,
                        distance,
                        collisionLayerMask);

                    if (hit.collider != null)
                    {
                        position += direction * Mathf.Max(0f, hit.distance - settings.wallSkin);
                        if (velocity.y <= 0f && hit.normal.y >= LandingNormalThreshold)
                        {
                            return new PlayerTraversalResult(true, position, index + 1);
                        }

                        if (Mathf.Abs(hit.normal.x) >= LandingNormalThreshold)
                        {
                            velocity.x = 0f;
                        }

                        if (Mathf.Abs(hit.normal.y) >= LandingNormalThreshold)
                        {
                            velocity.y = 0f;
                        }
                    }
                    else
                    {
                        position += delta;
                    }
                }

                velocity.y = Mathf.Max(
                    velocity.y - settings.gravity * step,
                    -settings.maximumFallSpeed);
            }

            return new PlayerTraversalResult(false, position, Mathf.Max(1, maximumSteps));
        }
    }
}

#endif
