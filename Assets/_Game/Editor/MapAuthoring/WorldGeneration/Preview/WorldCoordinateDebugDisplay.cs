using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.Preview
{
    public static class WorldCoordinateDebugDisplay
    {
        public static string Format(float worldX, float worldY)
        {
            if (float.IsNaN(worldX) ||
                float.IsInfinity(worldX) ||
                float.IsNaN(worldY) ||
                float.IsInfinity(worldY))
            {
                return "World: UNAVAILABLE\n" +
                       "Sector: -\n" +
                       "MicroChunk: -\n" +
                       "Local: -";
            }

            var tileX = Mathf.FloorToInt(worldX);
            var tileY = Mathf.FloorToInt(worldY);
            if (!WorldCoordinateUtility.TryCreateWorldTile(tileX, tileY, out var worldTile))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "World: OUTSIDE ({0}, {1})\nSector: -\nMicroChunk: -\nLocal: -",
                    tileX,
                    tileY);
            }

            if (!WorldCoordinateUtility.TryFromWorld(
                    worldTile,
                    out var sector,
                    out var microChunk,
                    out var localTile))
            {
                return "World: UNAVAILABLE\n" +
                       "Sector: -\n" +
                       "MicroChunk: -\n" +
                       "Local: -";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "World: {0}\nSector: {1}\nMicroChunk: {2}\nLocal: {3}",
                worldTile,
                sector,
                microChunk,
                localTile);
        }
    }
}
