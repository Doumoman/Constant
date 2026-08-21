#if LEGACY_DISABLED
namespace StarNight.Core.State
{
    public static class FeatureFlag
    {
#if STAR_NIGHT_NEW_STAGE_ARCHITECTURE
        private const bool DefaultNewStageArchitecture = true;
#else
        private const bool DefaultNewStageArchitecture = false;
#endif

        private static bool newStageArchitecture = DefaultNewStageArchitecture;

        public static bool NewStageArchitecture => newStageArchitecture;

        public static void SetNewStageArchitecture(bool enabled)
        {
            newStageArchitecture = enabled;
        }

        public static void ResetNewStageArchitecture()
        {
            newStageArchitecture = DefaultNewStageArchitecture;
        }
    }
}

#endif
