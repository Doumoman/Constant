#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Core.Player
{
    public static class PlayerGridContract
    {
        public const float CellSize = 1f;
        public const float ColliderWidth = 0.72f;
        public const float ColliderHeight = 0.92f;
        public const float GroundMoveSpeed = 6f;
        public const float AirMoveSpeed = 6f;
        public const float Gravity = 28f;
        public const float BaseJumpVelocity = 8.75f;
        public const float CoyoteTimeSeconds = 0.10f;
        public const float JumpBufferSeconds = 0.10f;
        public const float FixedDeltaTime = 1f / 60f;
        public const float VoidRecoveryInvulnerabilitySeconds = 0.8f;

        public static Vector2Int PlayerCenterToCell(Vector2 playerCenter)
        {
            float feetY = playerCenter.y - ColliderHeight * 0.5f;
            return new Vector2Int(
                Mathf.FloorToInt(playerCenter.x),
                Mathf.FloorToInt(feetY + 0.01f));
        }
    }
}

#endif
