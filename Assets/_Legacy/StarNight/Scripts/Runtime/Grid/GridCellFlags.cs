#if LEGACY_DISABLED
using System;

namespace StarNight.Grid
{
    [Flags]
    public enum GridCellFlags
    {
        None = 0,
        Solid = 1 << 0,
        Hazard = 1 << 1,
        Occupied = 1 << 2,
        SafeCandidate = 1 << 3
    }
}

#endif
