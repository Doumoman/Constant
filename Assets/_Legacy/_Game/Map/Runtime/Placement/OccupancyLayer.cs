#if LEGACY_DISABLED
using System;
using System.Collections.Generic;

namespace StarNight.Map.Placement
{
    [Flags]
    public enum OccupancyLayer
    {
        None = 0,
        Terrain = 1 << 0,
        OneWay = 1 << 1,
        Fixture = 1 << 2,
        Hazard = 1 << 3,
        Dynamic = 1 << 4,
        Logic = 1 << 5,
        Decoration = 1 << 6,
    }

    public static class OccupancyRules
    {
        public const OccupancyLayer PhysicalLayers =
            OccupancyLayer.Terrain |
            OccupancyLayer.OneWay |
            OccupancyLayer.Fixture |
            OccupancyLayer.Hazard |
            OccupancyLayer.Dynamic;

        public const OccupancyLayer ClearanceBlockingLayers =
            OccupancyLayer.Terrain |
            OccupancyLayer.Fixture |
            OccupancyLayer.Dynamic;

        public static bool CanOverlap(OccupancyLayer existing, OccupancyLayer incoming)
        {
            foreach (var existingLayer in EnumerateLayers(existing))
            {
                foreach (var incomingLayer in EnumerateLayers(incoming))
                {
                    if (!CanPairOverlap(existingLayer, incomingLayer))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool CanPairOverlap(OccupancyLayer first, OccupancyLayer second)
        {
            if (first == OccupancyLayer.Logic || second == OccupancyLayer.Logic ||
                first == OccupancyLayer.Decoration || second == OccupancyLayer.Decoration)
            {
                return true;
            }

            if ((first == OccupancyLayer.Terrain && second == OccupancyLayer.Fixture) ||
                (first == OccupancyLayer.Fixture && second == OccupancyLayer.Terrain) ||
                (first == OccupancyLayer.Terrain && second == OccupancyLayer.Hazard) ||
                (first == OccupancyLayer.Hazard && second == OccupancyLayer.Terrain))
            {
                return true;
            }

            return false;
        }

        private static IEnumerable<OccupancyLayer> EnumerateLayers(OccupancyLayer mask)
        {
            for (var bit = 0; bit < 7; bit++)
            {
                var layer = (OccupancyLayer)(1 << bit);
                if ((mask & layer) != 0)
                {
                    yield return layer;
                }
            }
        }
    }
}

#endif
