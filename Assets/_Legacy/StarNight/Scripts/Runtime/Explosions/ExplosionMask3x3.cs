#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;

namespace StarNight.Explosions
{
    public static class ExplosionMask3x3
    {
        public const int CellCount = 9;

        private static readonly GridPos[] offsetArray =
        {
            new GridPos(-1, -1),
            new GridPos(0, -1),
            new GridPos(1, -1),
            new GridPos(-1, 0),
            new GridPos(0, 0),
            new GridPos(1, 0),
            new GridPos(-1, 1),
            new GridPos(0, 1),
            new GridPos(1, 1)
        };

        private static readonly IReadOnlyList<GridPos> readOnlyOffsets =
            Array.AsReadOnly(offsetArray);

        public static IReadOnlyList<GridPos> Offsets => readOnlyOffsets;

        public static IEnumerable<GridPos> Enumerate(GridPos center)
        {
            for (int index = 0; index < offsetArray.Length; index++)
            {
                yield return center + offsetArray[index];
            }
        }

        public static bool Contains(GridPos center, GridPos candidate)
        {
            return Math.Abs(candidate.X - center.X) <= 1
                && Math.Abs(candidate.Y - center.Y) <= 1;
        }
    }
}

#endif
