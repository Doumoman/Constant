using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum OptionalAccessRequirement
    {
        None,
        Pickaxe,
        Shovel,
        Rope,
        Explosive,
        Environment
    }

    public enum OptionalAccessClueKind
    {
        BasicOpening,
        ToolSurface,
        EnvironmentDevice,
        ExplosiveRewardPreview,
        HiddenCrack,
        HiddenLight,
        HiddenSound
    }

    public enum OptionalAccessTraversalKind
    {
        OptionalBreak,
        Hidden
    }

    public static class OptionalAccessAssignmentEnums
    {
        public static bool TryParseRequirement(string token, out OptionalAccessRequirement value)
        {
            switch (token)
            {
                case "NONE": value = OptionalAccessRequirement.None; return true;
                case "PICKAXE": value = OptionalAccessRequirement.Pickaxe; return true;
                case "SHOVEL": value = OptionalAccessRequirement.Shovel; return true;
                case "ROPE": value = OptionalAccessRequirement.Rope; return true;
                case "EXPLOSIVE": value = OptionalAccessRequirement.Explosive; return true;
                case "ENVIRONMENT": value = OptionalAccessRequirement.Environment; return true;
                default: value = default(OptionalAccessRequirement); return false;
            }
        }

        public static string ToToken(OptionalAccessRequirement value)
        {
            switch (value)
            {
                case OptionalAccessRequirement.None: return "NONE";
                case OptionalAccessRequirement.Pickaxe: return "PICKAXE";
                case OptionalAccessRequirement.Shovel: return "SHOVEL";
                case OptionalAccessRequirement.Rope: return "ROPE";
                case OptionalAccessRequirement.Explosive: return "EXPLOSIVE";
                case OptionalAccessRequirement.Environment: return "ENVIRONMENT";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static bool TryParseClueKind(string token, out OptionalAccessClueKind value)
        {
            switch (token)
            {
                case "BASIC_OPENING": value = OptionalAccessClueKind.BasicOpening; return true;
                case "TOOL_SURFACE": value = OptionalAccessClueKind.ToolSurface; return true;
                case "ENVIRONMENT_DEVICE": value = OptionalAccessClueKind.EnvironmentDevice; return true;
                case "EXPLOSIVE_REWARD_PREVIEW": value = OptionalAccessClueKind.ExplosiveRewardPreview; return true;
                case "HIDDEN_CRACK": value = OptionalAccessClueKind.HiddenCrack; return true;
                case "HIDDEN_LIGHT": value = OptionalAccessClueKind.HiddenLight; return true;
                case "HIDDEN_SOUND": value = OptionalAccessClueKind.HiddenSound; return true;
                default: value = default(OptionalAccessClueKind); return false;
            }
        }

        public static string ToToken(OptionalAccessClueKind value)
        {
            switch (value)
            {
                case OptionalAccessClueKind.BasicOpening: return "BASIC_OPENING";
                case OptionalAccessClueKind.ToolSurface: return "TOOL_SURFACE";
                case OptionalAccessClueKind.EnvironmentDevice: return "ENVIRONMENT_DEVICE";
                case OptionalAccessClueKind.ExplosiveRewardPreview: return "EXPLOSIVE_REWARD_PREVIEW";
                case OptionalAccessClueKind.HiddenCrack: return "HIDDEN_CRACK";
                case OptionalAccessClueKind.HiddenLight: return "HIDDEN_LIGHT";
                case OptionalAccessClueKind.HiddenSound: return "HIDDEN_SOUND";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static bool TryParseTraversalKind(string token, out OptionalAccessTraversalKind value)
        {
            switch (token)
            {
                case "OPTIONAL_BREAK": value = OptionalAccessTraversalKind.OptionalBreak; return true;
                case "HIDDEN": value = OptionalAccessTraversalKind.Hidden; return true;
                default: value = default(OptionalAccessTraversalKind); return false;
            }
        }

        public static string ToToken(OptionalAccessTraversalKind value)
        {
            switch (value)
            {
                case OptionalAccessTraversalKind.OptionalBreak: return "OPTIONAL_BREAK";
                case OptionalAccessTraversalKind.Hidden: return "HIDDEN";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
}
