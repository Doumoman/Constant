using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public static class MicrochunkConstants
    {
        public const int WidthTiles = WorldGenConstants.MicroChunkWidthTiles;
        public const int HeightTiles = WorldGenConstants.MicroChunkHeightTiles;
        public const int CellCount = WidthTiles * HeightTiles;
        public const int LayerCount = 8;
    }
}
