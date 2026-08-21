#if LEGACY_DISABLED
using System;

namespace StarNight.Core.State
{
    public static class StageArchitecturePaths
    {
        public const string GameRoot = "Assets/_Game";
        public const string CoreRoot = GameRoot + "/Core";
        public const string GlobalDataRoot = GameRoot + "/Data/Global";
        public const string StageDataRoot = GameRoot + "/Data/Stages";
        public const string LegacyDataRoot = GameRoot + "/Data/Legacy";
        public const string CoreValidationEditorRoot = GameRoot + "/Editor/CoreValidation";
        public const string StageAuthoringEditorRoot = GameRoot + "/Editor/StageAuthoring";
        public const string StageAuthoringScenesRoot = StageAuthoringEditorRoot + "/Scenes";
        public const string EditorTestsRoot = GameRoot + "/Editor/Tests";

        public static readonly string[] RequiredCoreFolders =
        {
            "Grid",
            "Player",
            "Inventory",
            "Tools",
            "Rooms",
            "Camera",
            "Streaming",
            "Secrets",
            "Maru"
        };

        public static readonly string[] ApprovedStageCsvFileNames =
        {
            "microchunk_pattern_library.csv",
            "room_role_recipe.csv",
            "room_graph_template.csv",
            "room_placement_profile.csv",
            "special_content_pool.csv",
            "secret_dimension_library.csv"
        };

        public static readonly string[] RequiredGlobalDataFileNames =
        {
            "item_master.csv",
            "MapElementCatalog.asset",
            "ToolReactionCatalog.asset",
            "SpecialContentDefinitionCatalog.asset",
            "SecretDimensionDefinitionCatalog.asset"
        };

        public static bool IsApprovedStageCsvFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            foreach (string approvedName in ApprovedStageCsvFileNames)
            {
                if (string.Equals(fileName, approvedName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanRuntimeReadDataPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalized = path.Replace('\\', '/').TrimEnd('/');
            string legacyRoot = LegacyDataRoot.TrimEnd('/');
            return !string.Equals(normalized, legacyRoot, StringComparison.OrdinalIgnoreCase)
                && !normalized.StartsWith(legacyRoot + "/", StringComparison.OrdinalIgnoreCase);
        }
    }
}

#endif
