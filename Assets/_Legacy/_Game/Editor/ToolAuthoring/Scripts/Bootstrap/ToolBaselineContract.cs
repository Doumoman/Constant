#if LEGACY_DISABLED
namespace StarNight.ToolAuthoring.Editor
{
    public static class ToolBaselineContract
    {
        public const string Milestone = "TOOL-00";
        public const string CoreStandardVersion = "v2.1";
        public const string MapHarnessVersion = "v1.0";
        public const string ToolHarnessVersion = "v1.0";

        public const string InputActionAssetPath =
            "Assets/_Game/Interaction/Data/Resources/Input/StarNightControls.inputactions";
        public const string InputActionBackupPath =
            "Assets/_Game/Editor/ToolAuthoring/Baseline/StarNightControls_TOOL-00_Backup.json";
        public const string BaselineAssetPath =
            "Assets/_Game/Editor/ToolAuthoring/Baseline/TOOL-00_Baseline.asset";
        public const string ReportAssetPath =
            "Assets/_Game/Editor/ToolAuthoring/Reports/TOOL-00_Baseline.json";

        public static readonly string[] RequiredLayers =
        {
            "Player",
            "PlayerSensor",
            "TerrainSolid",
            "TerrainOneWay",
            "UnbreakableBoundary",
            "PortalBoundary",
            "DynamicObject",
            "HeldObject",
            "ToolHitbox",
            "PlayerProjectile",
            "Enemy",
            "EnemyProjectile",
            "Hazard",
            "Interaction",
            "Rope",
            "HookLine",
            "WaterEffect",
            "VoidRecovery",
            "EditorOnly"
        };

        public static readonly string[] RequiredUnityTags =
        {
            "CriticalCarry",
            "UmbrellaDeflectable"
        };

        public static readonly string[] LegacyToolCodePaths =
        {
            "Assets/StarNight/Scripts/Runtime/Tools",
            "Assets/StarNight/Scripts/Runtime/Explosions",
            "Assets/StarNight/Scripts/Editor/P3StarwindToolGardenBuilder.cs"
        };
    }
}

#endif
