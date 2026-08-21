#if LEGACY_DISABLED
using StarNight.Map;

namespace StarNight.Stage.Secrets
{
    public enum SecretGateType
    {
        CrackedWall,
        DirtSeal,
        ThinFloor,
        MechanismSeal,
        BlindPanel,
    }

    public enum SecretGateToolFamily
    {
        HeavyImpact,
        Shovel,
        PestleOrHeavyImpact,
        ContextInteraction,
        PanelInteraction,
    }

    public static class SecretGateContract
    {
        public static SecretGateToolFamily ResolveToolFamily(SecretGateType type)
        {
            return type switch
            {
                SecretGateType.DirtSeal => SecretGateToolFamily.Shovel,
                SecretGateType.ThinFloor => SecretGateToolFamily.PestleOrHeavyImpact,
                SecretGateType.MechanismSeal => SecretGateToolFamily.ContextInteraction,
                SecretGateType.BlindPanel => SecretGateToolFamily.PanelInteraction,
                _ => SecretGateToolFamily.HeavyImpact,
            };
        }

        public static bool OpensFromTool(SecretGateType type, ToolTag tags)
        {
            return type switch
            {
                SecretGateType.CrackedWall =>
                    (tags & (ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.HeavyImpact)) != 0,
                SecretGateType.DirtSeal => (tags & ToolTag.Shovel) != 0,
                SecretGateType.ThinFloor =>
                    (tags & (ToolTag.Pound | ToolTag.HeavyImpact | ToolTag.Bomb)) != 0,
                _ => false,
            };
        }

        public static bool DiscoversBlindPanel(ToolTag tags)
        {
            return (tags & (ToolTag.Bomb | ToolTag.HeavyImpact)) != 0;
        }

        public static bool OpensFromContext(SecretGateType type, bool blindPanelDiscovered)
        {
            return type == SecretGateType.MechanismSeal
                || type == SecretGateType.BlindPanel && blindPanelDiscovered;
        }

        public static bool ShouldConsumeToolResource(ToolTag tags)
        {
            return (tags & (ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Shovel | ToolTag.Pound)) != 0;
        }
    }
}

#endif
