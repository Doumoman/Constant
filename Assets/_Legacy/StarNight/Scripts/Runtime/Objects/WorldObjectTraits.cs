#if LEGACY_DISABLED
using System;

namespace StarNight.Objects
{
    [Flags]
    public enum WorldObjectTraits
    {
        None = 0,
        Carryable = 1 << 0,
        Heavy = 1 << 1,
        Breakable = 1 << 2,
        Sacred = 1 << 3,
        Pullable = 1 << 4,
        Flammable = 1 << 5,
        Growable = 1 << 6,
        Compressible = 1 << 7,
        Buoyant = 1 << 8,
        Conductive = 1 << 9
    }
}

#endif
