#if LEGACY_DISABLED
using System;

namespace StarNight.Tiles
{
    [Flags]
    public enum TileBreakMethod
    {
        None = 0,
        Bomb = 1 << 0,
        Pickaxe = 1 << 1,
        Shovel = 1 << 2,
        System = 1 << 3
    }

    public enum TileMaterialKind
    {
        Stone,
        Dirt,
        CrackedWall,
        Wood,
        Fixture,
        ReinforcedWall,
        ExitFrame,
        RequiredEventCore,
        Sand,
        Ash,
        ThinFloor
    }
}

#endif
