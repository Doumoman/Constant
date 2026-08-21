using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum WorldGenerationFailurePolicy
    {
        FailWorld,
        RetryPass,
        RetryScope,
        ReportOnly
    }

    public static class WorldGenerationFailurePolicyToken
    {
        public static WorldGenerationFailurePolicy Parse(string token)
        {
            switch (token)
            {
                case "FAIL_WORLD": return WorldGenerationFailurePolicy.FailWorld;
                case "RETRY_PASS": return WorldGenerationFailurePolicy.RetryPass;
                case "RETRY_SCOPE": return WorldGenerationFailurePolicy.RetryScope;
                case "REPORT_ONLY": return WorldGenerationFailurePolicy.ReportOnly;
                default: throw new ArgumentException("Unknown world-generation failure policy token.", nameof(token));
            }
        }

        public static string Format(WorldGenerationFailurePolicy policy)
        {
            switch (policy)
            {
                case WorldGenerationFailurePolicy.FailWorld: return "FAIL_WORLD";
                case WorldGenerationFailurePolicy.RetryPass: return "RETRY_PASS";
                case WorldGenerationFailurePolicy.RetryScope: return "RETRY_SCOPE";
                case WorldGenerationFailurePolicy.ReportOnly: return "REPORT_ONLY";
                default: throw new ArgumentOutOfRangeException(nameof(policy));
            }
        }
    }
}
