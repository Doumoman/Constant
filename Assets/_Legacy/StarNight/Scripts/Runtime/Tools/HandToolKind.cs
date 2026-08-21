#if LEGACY_DISABLED
namespace StarNight.Tools
{
    public enum P3ToolKind
    {
        Rope = 1,
        Bomb = 2,
        Pickaxe = 3,
        Shovel = 4,
        WateringCan = 5,
        Pestle = 6,
        Grapple = 7,
        WindUmbrella = 8
    }

    public enum HandToolKind
    {
        None = 0,
        Pickaxe = 1,
        Shovel = 2,
        WateringCan = 3,
        Pestle = 4,
        Grapple = 5,
        WindUmbrella = 6
    }

    public static class ToolKindConversion
    {
        public static P3ToolKind ToP3ToolKind(this HandToolKind kind)
        {
            switch (kind)
            {
                case HandToolKind.Pickaxe:
                    return P3ToolKind.Pickaxe;
                case HandToolKind.Shovel:
                    return P3ToolKind.Shovel;
                case HandToolKind.WateringCan:
                    return P3ToolKind.WateringCan;
                case HandToolKind.Pestle:
                    return P3ToolKind.Pestle;
                case HandToolKind.Grapple:
                    return P3ToolKind.Grapple;
                case HandToolKind.WindUmbrella:
                    return P3ToolKind.WindUmbrella;
                default:
                    return 0;
            }
        }
    }
}

#endif
