#if LEGACY_DISABLED
using StarNight.Stage.Data;

namespace StarNight.Stage.Maru
{
    public static class MaruTimeline
    {
        public const float AccessibilityTimeMultiplier = 1.25f;

        public static float GetMultiplier(bool extendTime)
        {
            return extendTime ? AccessibilityTimeMultiplier : 1f;
        }

        public static BellPhase Evaluate(StageDefinition definition, float elapsedTime, bool extendTime)
        {
            if (definition == null || definition.kind == StageKind.Boss)
            {
                return BellPhase.None;
            }

            float multiplier = GetMultiplier(extendTime);
            if (elapsedTime >= definition.maruSpawnTime * multiplier)
            {
                return BellPhase.Maru;
            }
            if (elapsedTime >= definition.bell2Time * multiplier)
            {
                return BellPhase.Second;
            }
            return elapsedTime >= definition.bell1Time * multiplier ? BellPhase.First : BellPhase.None;
        }

        public static float GetRemainingSeconds(StageDefinition definition, float elapsedTime, bool extendTime)
        {
            if (definition == null || definition.kind == StageKind.Boss)
            {
                return 0f;
            }
            return UnityEngine.Mathf.Max(0f, definition.maruSpawnTime * GetMultiplier(extendTime) - elapsedTime);
        }
    }
}

#endif
