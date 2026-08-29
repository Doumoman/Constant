using System;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public readonly struct SpecialRegionAuthoredCoordinate : IEquatable<SpecialRegionAuthoredCoordinate>
    {
        public SpecialRegionAuthoredCoordinate(
            SpecialRegionSectorOffset sectorOffset,
            LocalTileCoord localTile,
            SiteEntrySide? side = null)
        {
            SectorOffset = sectorOffset;
            LocalTile = localTile;
            Side = side;
        }

        public SpecialRegionSectorOffset SectorOffset { get; }
        public LocalTileCoord LocalTile { get; }
        public SiteEntrySide? Side { get; }
        public SpecialRegionSectorOffset SourceSectorOffset => SectorOffset;
        public LocalTileCoord SourceLocalTile => LocalTile;
        public SiteEntrySide? SourceSide => Side;
        public bool HasSide => Side.HasValue;

        public bool Equals(SpecialRegionAuthoredCoordinate other)
            => SectorOffset == other.SectorOffset && LocalTile == other.LocalTile && Side == other.Side;

        public override bool Equals(object obj)
            => obj is SpecialRegionAuthoredCoordinate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = SectorOffset.GetHashCode();
                hash = (hash * 397) ^ LocalTile.GetHashCode();
                return (hash * 397) ^ (Side.HasValue ? (int)Side.Value + 1 : 0);
            }
        }

        public static bool operator ==(
            SpecialRegionAuthoredCoordinate left,
            SpecialRegionAuthoredCoordinate right) => left.Equals(right);

        public static bool operator !=(
            SpecialRegionAuthoredCoordinate left,
            SpecialRegionAuthoredCoordinate right) => !left.Equals(right);
    }

    public readonly struct SpecialRegionPlacedCoordinate : IEquatable<SpecialRegionPlacedCoordinate>
    {
        public SpecialRegionPlacedCoordinate(
            SpecialRegionSectorOffset sectorOffset,
            SectorCoord worldSector,
            LocalTileCoord localTile,
            LocalTileCoord regionTile,
            SiteEntrySide? side = null)
        {
            SectorOffset = sectorOffset;
            WorldSector = worldSector;
            LocalTile = localTile;
            RegionTile = regionTile;
            Side = side;
        }

        public SpecialRegionSectorOffset SectorOffset { get; }
        public SectorCoord WorldSector { get; }
        public LocalTileCoord LocalTile { get; }
        public LocalTileCoord RegionTile { get; }
        public SiteEntrySide? Side { get; }
        public SpecialRegionSectorOffset TransformedSectorOffset => SectorOffset;
        public LocalTileCoord TransformedLocalTile => LocalTile;
        public SiteEntrySide? TransformedSide => Side;
        public bool HasSide => Side.HasValue;

        public bool Equals(SpecialRegionPlacedCoordinate other)
            => SectorOffset == other.SectorOffset && WorldSector == other.WorldSector &&
               LocalTile == other.LocalTile && RegionTile == other.RegionTile && Side == other.Side;

        public override bool Equals(object obj)
            => obj is SpecialRegionPlacedCoordinate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = SectorOffset.GetHashCode();
                hash = (hash * 397) ^ WorldSector.GetHashCode();
                hash = (hash * 397) ^ LocalTile.GetHashCode();
                hash = (hash * 397) ^ RegionTile.GetHashCode();
                return (hash * 397) ^ (Side.HasValue ? (int)Side.Value + 1 : 0);
            }
        }

        public static bool operator ==(
            SpecialRegionPlacedCoordinate left,
            SpecialRegionPlacedCoordinate right) => left.Equals(right);

        public static bool operator !=(
            SpecialRegionPlacedCoordinate left,
            SpecialRegionPlacedCoordinate right) => !left.Equals(right);
    }

    public static class SpecialRegionSiteCoordinateTransformer
    {
        public static bool TryProject(
            SiteReservation reservation,
            SpecialRegionAuthoredCoordinate source,
            out SpecialRegionPlacedCoordinate placed)
        {
            placed = default(SpecialRegionPlacedCoordinate);
            return reservation != null && TryProject(
                reservation.Footprint.Width,
                reservation.Footprint.Height,
                reservation.Footprint.Transform,
                reservation.Origin,
                source,
                out placed);
        }

        public static bool TryProject(
            int width,
            int height,
            SiteFootprintTransform transform,
            SectorCoord origin,
            SpecialRegionAuthoredCoordinate source,
            out SpecialRegionPlacedCoordinate placed)
        {
            placed = default(SpecialRegionPlacedCoordinate);
            if (!IsValidDimensions(width, height) || !IsWorldSector(origin) ||
                !IsLocalTile(source.LocalTile) || !IsOptionalSide(source.Side) ||
                !SiteFootprintTransformer.TryTransformCoordinate(
                    width, height, transform, source.SectorOffset.X, source.SectorOffset.Y,
                    out var sectorX, out var sectorY))
                return false;

            if (!TryTransformTile(transform, source.LocalTile, out var tile) ||
                !TryTransformSide(transform, source.Side, out var side) ||
                !TryAdd(origin.X, sectorX, out var worldX) ||
                !TryAdd(origin.Y, sectorY, out var worldY) ||
                !IsWorldSector(new SectorCoord(worldX, worldY)) ||
                !TryRegionTile(sectorX, sectorY, tile, out var regionTile))
                return false;

            placed = new SpecialRegionPlacedCoordinate(
                new SpecialRegionSectorOffset(sectorX, sectorY),
                new SectorCoord(worldX, worldY), tile, regionTile, side);
            return true;
        }

        public static bool TryUnproject(
            SiteReservation reservation,
            SpecialRegionPlacedCoordinate placed,
            out SpecialRegionAuthoredCoordinate source)
        {
            source = default(SpecialRegionAuthoredCoordinate);
            return reservation != null && TryUnproject(
                reservation.Footprint.Width,
                reservation.Footprint.Height,
                reservation.Footprint.Transform,
                reservation.Origin,
                placed,
                out source);
        }

        public static bool TryUnproject(
            int width,
            int height,
            SiteFootprintTransform transform,
            SectorCoord origin,
            SpecialRegionPlacedCoordinate placed,
            out SpecialRegionAuthoredCoordinate source)
        {
            source = default(SpecialRegionAuthoredCoordinate);
            if (!IsValidDimensions(width, height) || !IsWorldSector(origin) ||
                !IsLocalTile(placed.LocalTile) || !IsOptionalSide(placed.Side) ||
                !SiteFootprintTransformer.TryTransformCoordinate(
                    width, height, transform, placed.SectorOffset.X, placed.SectorOffset.Y,
                    out var sourceSectorX, out var sourceSectorY) ||
                !TryTransformTile(transform, placed.LocalTile, out var sourceTile) ||
                !TryTransformSide(transform, placed.Side, out var sourceSide))
                return false;

            if (!TryAdd(origin.X, placed.SectorOffset.X, out var worldX) ||
                !TryAdd(origin.Y, placed.SectorOffset.Y, out var worldY) ||
                placed.WorldSector != new SectorCoord(worldX, worldY) ||
                !TryRegionTile(
                    placed.SectorOffset.X, placed.SectorOffset.Y, placed.LocalTile,
                    out var expectedRegionTile) || placed.RegionTile != expectedRegionTile)
                return false;

            source = new SpecialRegionAuthoredCoordinate(
                new SpecialRegionSectorOffset(sourceSectorX, sourceSectorY), sourceTile, sourceSide);
            return true;
        }

        private static bool TryTransformTile(
            SiteFootprintTransform transform,
            LocalTileCoord source,
            out LocalTileCoord transformed)
        {
            transformed = default(LocalTileCoord);
            if (!IsLocalTile(source)) return false;
            switch (transform)
            {
                case SiteFootprintTransform.R0:
                    transformed = source;
                    return true;
                case SiteFootprintTransform.MirrorX:
                    transformed = new LocalTileCoord(WorldGenConstants.SectorWidthTiles - 1 - source.X, source.Y);
                    return true;
                case SiteFootprintTransform.MirrorY:
                    transformed = new LocalTileCoord(source.X, WorldGenConstants.SectorHeightTiles - 1 - source.Y);
                    return true;
                case SiteFootprintTransform.R180:
                    transformed = new LocalTileCoord(
                        WorldGenConstants.SectorWidthTiles - 1 - source.X,
                        WorldGenConstants.SectorHeightTiles - 1 - source.Y);
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryTransformSide(
            SiteFootprintTransform transform,
            SiteEntrySide? source,
            out SiteEntrySide? transformed)
        {
            transformed = null;
            if (!source.HasValue) return IsDefined(transform);
            if (!SiteFootprintTransformer.TryTransformSide(transform, source.Value, out var side)) return false;
            transformed = side;
            return true;
        }

        private static bool TryRegionTile(
            int sectorX,
            int sectorY,
            LocalTileCoord localTile,
            out LocalTileCoord regionTile)
        {
            regionTile = default(LocalTileCoord);
            var x = ((long)sectorX * WorldGenConstants.SectorWidthTiles) + localTile.X;
            var y = ((long)sectorY * WorldGenConstants.SectorHeightTiles) + localTile.Y;
            if (x < 0 || x > int.MaxValue || y < 0 || y > int.MaxValue) return false;
            regionTile = new LocalTileCoord((int)x, (int)y);
            return true;
        }

        private static bool TryAdd(int left, int right, out int value)
        {
            var sum = (long)left + right;
            if (sum < int.MinValue || sum > int.MaxValue)
            {
                value = 0;
                return false;
            }
            value = (int)sum;
            return true;
        }

        private static bool IsValidDimensions(int width, int height)
            => width > 0 && height > 0 &&
               width <= WorldGenConstants.SectorColumns && height <= WorldGenConstants.SectorRows;

        private static bool IsLocalTile(LocalTileCoord tile)
            => tile.X >= 0 && tile.X < WorldGenConstants.SectorWidthTiles &&
               tile.Y >= 0 && tile.Y < WorldGenConstants.SectorHeightTiles;

        private static bool IsWorldSector(SectorCoord sector)
            => sector.X >= 0 && sector.X < WorldGenConstants.SectorColumns &&
               sector.Y >= 0 && sector.Y < WorldGenConstants.SectorRows;

        private static bool IsOptionalSide(SiteEntrySide? side)
            => !side.HasValue || side.Value == SiteEntrySide.L || side.Value == SiteEntrySide.R ||
               side.Value == SiteEntrySide.U || side.Value == SiteEntrySide.D;

        private static bool IsDefined(SiteFootprintTransform transform)
            => transform == SiteFootprintTransform.R0 || transform == SiteFootprintTransform.MirrorX ||
               transform == SiteFootprintTransform.MirrorY || transform == SiteFootprintTransform.R180;
    }
}
