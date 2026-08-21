using System;

namespace StarNight.Map.WorldGeneration.Domain
{
    public static class WorldCoordinateUtility
    {
        public static bool IsValid(WorldTileCoord coordinate)
        {
            return coordinate.X >= 0 &&
                   coordinate.X < WorldGenConstants.WorldWidthTiles &&
                   coordinate.Y >= 0 &&
                   coordinate.Y < WorldGenConstants.WorldHeightTiles;
        }

        public static bool IsValid(SectorCoord coordinate)
        {
            return coordinate.X >= 0 &&
                   coordinate.X < WorldGenConstants.SectorColumns &&
                   coordinate.Y >= 0 &&
                   coordinate.Y < WorldGenConstants.SectorRows;
        }

        public static bool IsValid(MicroChunkCoord coordinate)
        {
            return coordinate.X >= 0 &&
                   coordinate.X < WorldGenConstants.MicroChunkColumnsPerSector &&
                   coordinate.Y >= 0 &&
                   coordinate.Y < WorldGenConstants.MicroChunkRowsPerSector;
        }

        public static bool IsValid(LocalTileCoord coordinate)
        {
            return coordinate.X >= 0 &&
                   coordinate.X < WorldGenConstants.MicroChunkWidthTiles &&
                   coordinate.Y >= 0 &&
                   coordinate.Y < WorldGenConstants.MicroChunkHeightTiles;
        }

        public static bool TryCreateWorldTile(int x, int y, out WorldTileCoord coordinate)
        {
            coordinate = default;
            var candidate = new WorldTileCoord(x, y);
            if (!IsValid(candidate))
            {
                return false;
            }

            coordinate = candidate;
            return true;
        }

        public static bool TryCreateSector(int x, int y, out SectorCoord coordinate)
        {
            coordinate = default;
            var candidate = new SectorCoord(x, y);
            if (!IsValid(candidate))
            {
                return false;
            }

            coordinate = candidate;
            return true;
        }

        public static bool TryCreateMicroChunk(int x, int y, out MicroChunkCoord coordinate)
        {
            coordinate = default;
            var candidate = new MicroChunkCoord(x, y);
            if (!IsValid(candidate))
            {
                return false;
            }

            coordinate = candidate;
            return true;
        }

        public static bool TryCreateLocalTile(int x, int y, out LocalTileCoord coordinate)
        {
            coordinate = default;
            var candidate = new LocalTileCoord(x, y);
            if (!IsValid(candidate))
            {
                return false;
            }

            coordinate = candidate;
            return true;
        }

        public static bool TryToWorld(
            SectorCoord sector,
            MicroChunkCoord microChunk,
            LocalTileCoord localTile,
            out WorldTileCoord worldTile)
        {
            worldTile = default;
            if (!IsValid(sector) || !IsValid(microChunk) || !IsValid(localTile))
            {
                return false;
            }

            var candidate = ToWorld(sector, microChunk, localTile);
            if (!IsValid(candidate))
            {
                return false;
            }

            worldTile = candidate;
            return true;
        }

        public static WorldTileCoord ToWorld(
            SectorCoord sector,
            MicroChunkCoord microChunk,
            LocalTileCoord localTile)
        {
            if (!IsValid(sector))
            {
                throw new ArgumentOutOfRangeException(nameof(sector));
            }

            if (!IsValid(microChunk))
            {
                throw new ArgumentOutOfRangeException(nameof(microChunk));
            }

            if (!IsValid(localTile))
            {
                throw new ArgumentOutOfRangeException(nameof(localTile));
            }

            return new WorldTileCoord(
                sector.X * WorldGenConstants.SectorWidthTiles +
                microChunk.X * WorldGenConstants.MicroChunkWidthTiles +
                localTile.X,
                sector.Y * WorldGenConstants.SectorHeightTiles +
                microChunk.Y * WorldGenConstants.MicroChunkHeightTiles +
                localTile.Y);
        }

        public static bool TryFromWorld(
            WorldTileCoord worldTile,
            out SectorCoord sector,
            out MicroChunkCoord microChunk,
            out LocalTileCoord localTile)
        {
            sector = default;
            microChunk = default;
            localTile = default;
            if (!IsValid(worldTile))
            {
                return false;
            }

            var resolvedSector = new SectorCoord(
                worldTile.X / WorldGenConstants.SectorWidthTiles,
                worldTile.Y / WorldGenConstants.SectorHeightTiles);
            var resolvedMicroChunk = new MicroChunkCoord(
                worldTile.X % WorldGenConstants.SectorWidthTiles /
                WorldGenConstants.MicroChunkWidthTiles,
                worldTile.Y % WorldGenConstants.SectorHeightTiles /
                WorldGenConstants.MicroChunkHeightTiles);
            var resolvedLocalTile = new LocalTileCoord(
                worldTile.X % WorldGenConstants.MicroChunkWidthTiles,
                worldTile.Y % WorldGenConstants.MicroChunkHeightTiles);

            if (!IsValid(resolvedSector) ||
                !IsValid(resolvedMicroChunk) ||
                !IsValid(resolvedLocalTile))
            {
                return false;
            }

            sector = resolvedSector;
            microChunk = resolvedMicroChunk;
            localTile = resolvedLocalTile;
            return true;
        }

        public static SectorCoord ToSector(WorldTileCoord worldTile)
        {
            if (!TryFromWorld(worldTile, out var sector, out _, out _))
            {
                throw new ArgumentOutOfRangeException(nameof(worldTile));
            }

            return sector;
        }

        public static MicroChunkCoord ToMicroChunk(WorldTileCoord worldTile)
        {
            if (!TryFromWorld(worldTile, out _, out var microChunk, out _))
            {
                throw new ArgumentOutOfRangeException(nameof(worldTile));
            }

            return microChunk;
        }

        public static LocalTileCoord ToLocalTile(WorldTileCoord worldTile)
        {
            if (!TryFromWorld(worldTile, out _, out _, out var localTile))
            {
                throw new ArgumentOutOfRangeException(nameof(worldTile));
            }

            return localTile;
        }
    }
}
