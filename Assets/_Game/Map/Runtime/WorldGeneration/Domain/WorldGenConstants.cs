namespace StarNight.Map.WorldGeneration.Domain
{
    public static class WorldGenConstants
    {
        public const int WorldWidthTiles = 624;
        public const int WorldHeightTiles = 416;
        public const int SectorWidthTiles = 48;
        public const int SectorHeightTiles = 32;
        public const int MicroChunkWidthTiles = 12;
        public const int MicroChunkHeightTiles = 8;

        public const int SectorColumns = WorldWidthTiles / SectorWidthTiles;
        public const int SectorRows = WorldHeightTiles / SectorHeightTiles;
        public const int SectorCount = SectorColumns * SectorRows;

        public const int MicroChunkColumnsPerSector = SectorWidthTiles / MicroChunkWidthTiles;
        public const int MicroChunkRowsPerSector = SectorHeightTiles / MicroChunkHeightTiles;
        public const int MicroChunksPerSector = MicroChunkColumnsPerSector * MicroChunkRowsPerSector;
        public const int TilesPerMicroChunk = MicroChunkWidthTiles * MicroChunkHeightTiles;
        public const int TilesPerSector = SectorWidthTiles * SectorHeightTiles;
        public const int WorldTileCount = WorldWidthTiles * WorldHeightTiles;
    }
}
