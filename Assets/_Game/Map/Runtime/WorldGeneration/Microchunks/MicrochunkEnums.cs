namespace StarNight.Map.WorldGeneration.Microchunks
{
    public enum MicrochunkUsageClass
    {
        Traversal,
        Boundary,
        Filler,
        Special,
        Village,
        Adapter
    }

    public enum MicrochunkTransform
    {
        R0,
        MirrorX,
        MirrorY,
        R180
    }

    public enum MicrochunkSide
    {
        Left,
        Right,
        Up,
        Down
    }

    public enum MicrochunkTraversalKind
    {
        Walk,
        Drop,
        Climb,
        OptionalBreak,
        Hidden,
        Decoration
    }

    public enum MicrochunkRouteLayer
    {
        Mandatory,
        Optional,
        Both
    }

    public enum MicrochunkSlotCategory
    {
        Resource,
        MapElement,
        Enemy,
        Reward,
        Npc,
        ShopItem,
        EventTrigger,
        SpecialItem,
        Decoration
    }

    public enum MicrochunkToolRequirement
    {
        None,
        Pickaxe,
        Shovel,
        Rope,
        Explosive,
        Environment
    }

    public enum MicrochunkObjectOrientation
    {
        None
    }

    public enum MicrochunkTileLayer
    {
        GroundSolid,
        OneWay,
        Breakable,
        Hazard,
        Liquid,
        DecorationBack,
        DecorationFront,
        Marker
    }
}
