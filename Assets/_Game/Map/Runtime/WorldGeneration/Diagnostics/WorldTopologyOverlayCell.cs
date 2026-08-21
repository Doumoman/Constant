using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class WorldTopologyOverlayCell
    {
        public WorldTopologyOverlayCell(
            SectorCell sourceCell,
            SectorNeighborIndices sourceNeighbors)
        {
            if (sourceCell == null)
            {
                throw new ArgumentNullException(nameof(sourceCell));
            }

            if (sourceNeighbors == null)
            {
                throw new ArgumentNullException(nameof(sourceNeighbors));
            }

            var index = sourceCell.Index;
            var expectedCoordinate = WorldGridIndex.ToCoordinate(index);
            if (sourceCell.Coordinate != expectedCoordinate)
            {
                throw new ArgumentException(
                    "The source cell coordinate does not match its grid index.",
                    nameof(sourceCell));
            }

            if (sourceNeighbors.Index != index ||
                sourceNeighbors.LeftIndex != WorldGridIndex.GetLeftIndex(index) ||
                sourceNeighbors.RightIndex != WorldGridIndex.GetRightIndex(index) ||
                sourceNeighbors.UpIndex != WorldGridIndex.GetUpIndex(index) ||
                sourceNeighbors.DownIndex != WorldGridIndex.GetDownIndex(index))
            {
                throw new ArgumentException(
                    "The source neighbors do not match the frozen grid topology.",
                    nameof(sourceNeighbors));
            }

            var roleIdentity = GetRoleIdentity(sourceCell.Role);
            Index = index;
            Coordinate = sourceCell.Coordinate;
            Role = sourceCell.Role;
            WorldTileMinX = Coordinate.X * WorldGenConstants.SectorWidthTiles;
            WorldTileMaxX = WorldTileMinX + WorldGenConstants.SectorWidthTiles - 1;
            WorldTileMinY = Coordinate.Y * WorldGenConstants.SectorHeightTiles;
            WorldTileMaxY = WorldTileMinY + WorldGenConstants.SectorHeightTiles - 1;
            LeftIndex = sourceNeighbors.LeftIndex;
            RightIndex = sourceNeighbors.RightIndex;
            UpIndex = sourceNeighbors.UpIndex;
            DownIndex = sourceNeighbors.DownIndex;
            RoleToken = roleIdentity.Token;
            RoleGlyph = roleIdentity.Glyph;
            CellLabel = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1}\n{2}",
                Coordinate.X,
                Coordinate.Y,
                RoleGlyph);
            Tooltip = string.Format(
                CultureInfo.InvariantCulture,
                "Sector: {0} / Index {1}\n" +
                "World Tiles: X {2}..{3} / Y {4}..{5}\n" +
                "Role: {6}\n" +
                "Neighbors: L={7} R={8} U={9} D={10}",
                Coordinate,
                Index,
                WorldTileMinX,
                WorldTileMaxX,
                WorldTileMinY,
                WorldTileMaxY,
                RoleToken,
                LeftIndex,
                RightIndex,
                UpIndex,
                DownIndex);
        }

        public int Index { get; }
        public SectorCoord Coordinate { get; }
        public GeneratedSectorRole Role { get; }
        public int WorldTileMinX { get; }
        public int WorldTileMaxX { get; }
        public int WorldTileMinY { get; }
        public int WorldTileMaxY { get; }
        public int LeftIndex { get; }
        public int RightIndex { get; }
        public int UpIndex { get; }
        public int DownIndex { get; }
        public string RoleToken { get; }
        public string RoleGlyph { get; }
        public string CellLabel { get; }
        public string Tooltip { get; }

        private static RoleIdentity GetRoleIdentity(GeneratedSectorRole role)
        {
            switch (role)
            {
                case GeneratedSectorRole.Unassigned:
                    return new RoleIdentity("UNASSIGNED", "U");
                case GeneratedSectorRole.Mandatory:
                    return new RoleIdentity("MANDATORY", "M");
                case GeneratedSectorRole.Type0:
                    return new RoleIdentity("TYPE0", "0");
                case GeneratedSectorRole.ReservedSite:
                    return new RoleIdentity("RESERVED_SITE", "S");
                case GeneratedSectorRole.InactiveBuffer:
                    return new RoleIdentity("INACTIVE_BUFFER", "X");
                default:
                    throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        private readonly struct RoleIdentity
        {
            public RoleIdentity(string token, string glyph)
            {
                Token = token;
                Glyph = glyph;
            }

            public string Token { get; }
            public string Glyph { get; }
        }
    }
}
