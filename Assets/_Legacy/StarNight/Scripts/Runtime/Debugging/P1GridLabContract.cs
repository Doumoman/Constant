#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Debugging
{
    public static class P1GridLabContract
    {
        public const int Width = 30;
        public const int Height = 18;
        public const int TunnelMinX = 3;
        public const int TunnelMaxXExclusive = 10;
        public const int TunnelFloorY = 0;
        public const int TunnelCeilingY = 2;
        public const int JumpGapMinX = 17;
        public const int JumpGapMaxXInclusive = 19;
        public const int JumpPlatformY = 3;
        public const int RecoveryPitMinX = 25;
        public const int RecoveryPitMaxXInclusive = 27;

        public static readonly Vector2 PlayerSpawn = new Vector2(2.5f, 1.45f);
        public static readonly Rect TunnelWorldBounds = new Rect(3f, 1f, 7f, 1f);
    }
}

#endif
