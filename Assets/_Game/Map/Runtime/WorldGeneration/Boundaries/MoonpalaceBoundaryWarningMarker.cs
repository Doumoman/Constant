using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    [Flags]
    public enum MoonpalaceBoundaryWarningMarker
    {
        None = 0,
        Tile = 1 << 0,
        Background = 1 << 1,
        Resource = 1 << 2,
        Audio = 1 << 3,
    }
}
