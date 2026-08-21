using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class BiomePatchOverlayPatchRow
    {
        internal BiomePatchOverlayPatchRow(
            BiomePatchId patchId,
            string biomeId,
            BiomePatchRole role,
            int size,
            int perimeter,
            int compactnessPermille,
            int seedCount,
            int coreSiteCellCount)
        {
            if (!patchId.IsValid) throw new ArgumentException("Patch ID must be valid.", nameof(patchId));
            if (string.IsNullOrEmpty(biomeId)) throw new ArgumentException("Biome ID is required.", nameof(biomeId));
            BiomePatchRoleTokenCodec.ToToken(role);
            if (size < 1) throw new ArgumentOutOfRangeException(nameof(size));
            if (perimeter < 1) throw new ArgumentOutOfRangeException(nameof(perimeter));
            if (compactnessPermille < 1 || compactnessPermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(compactnessPermille));
            if (seedCount < 1) throw new ArgumentOutOfRangeException(nameof(seedCount));
            if (coreSiteCellCount < 0) throw new ArgumentOutOfRangeException(nameof(coreSiteCellCount));

            PatchId = patchId;
            BiomeId = biomeId;
            Role = role;
            Size = size;
            Perimeter = perimeter;
            CompactnessPermille = compactnessPermille;
            SeedCount = seedCount;
            CoreSiteCellCount = coreSiteCellCount;
        }

        public BiomePatchId PatchId { get; }
        public string BiomeId { get; }
        public BiomePatchRole Role { get; }
        public int Size { get; }
        public int Perimeter { get; }
        public int CompactnessPermille { get; }
        public int SeedCount { get; }
        public int CoreSiteCellCount { get; }
    }

    public sealed class BiomePatchOverlayCell
    {
        internal BiomePatchOverlayCell(
            int index,
            SectorCoord coordinate,
            bool isAssigned,
            string primaryBiomeId,
            BiomePatchId? patchId,
            BiomePatchRole? role,
            int patchSize,
            int perimeter,
            int compactnessPermille,
            bool isSeed,
            bool isCoreSiteCell,
            bool borderLeft,
            bool borderRight,
            bool borderUp,
            bool borderDown)
        {
            if (index < 0 || index >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (coordinate != WorldGridIndex.ToCoordinate(index))
                throw new ArgumentException("Cell index and coordinate must match the world grid.", nameof(coordinate));

            if (isAssigned)
            {
                if (string.IsNullOrEmpty(primaryBiomeId))
                    throw new ArgumentException("Assigned cells require a biome ID.", nameof(primaryBiomeId));
                if (!patchId.HasValue || !patchId.Value.IsValid)
                    throw new ArgumentException("Assigned cells require a patch ID.", nameof(patchId));
                if (!role.HasValue)
                    throw new ArgumentException("Assigned cells require a patch role.", nameof(role));
                BiomePatchRoleTokenCodec.ToToken(role.Value);
                if (patchSize < 1) throw new ArgumentOutOfRangeException(nameof(patchSize));
                if (perimeter < 1) throw new ArgumentOutOfRangeException(nameof(perimeter));
                if (compactnessPermille < 1 || compactnessPermille > 1000)
                    throw new ArgumentOutOfRangeException(nameof(compactnessPermille));
            }
            else if (!string.IsNullOrEmpty(primaryBiomeId) || patchId.HasValue || role.HasValue ||
                     patchSize != 0 || perimeter != 0 || compactnessPermille != 0 ||
                     isSeed || isCoreSiteCell)
            {
                throw new ArgumentException("Unassigned cells must contain neutral patch state.");
            }

            Index = index;
            Coordinate = coordinate;
            IsAssigned = isAssigned;
            PrimaryBiomeId = primaryBiomeId ?? string.Empty;
            PatchId = patchId;
            Role = role;
            PatchSize = patchSize;
            Perimeter = perimeter;
            CompactnessPermille = compactnessPermille;
            IsSeed = isSeed;
            IsCoreSiteCell = isCoreSiteCell;
            BorderLeft = borderLeft;
            BorderRight = borderRight;
            BorderUp = borderUp;
            BorderDown = borderDown;
            RoleToken = role.HasValue ? BiomePatchRoleTokenCodec.ToToken(role.Value) : string.Empty;
            RoleGlyph = role.HasValue ? BiomePatchOverlayGui.GetRoleGlyph(role.Value) : string.Empty;
            CellLabel = CreateLabel();
            Tooltip = CreateTooltip();
        }

        public int Index { get; }
        public SectorCoord Coordinate { get; }
        public bool IsAssigned { get; }
        public string PrimaryBiomeId { get; }
        public BiomePatchId? PatchId { get; }
        public BiomePatchRole? Role { get; }
        public int PatchSize { get; }
        public int Perimeter { get; }
        public int CompactnessPermille { get; }
        public bool IsSeed { get; }
        public bool IsCoreSiteCell { get; }
        public bool BorderLeft { get; }
        public bool BorderRight { get; }
        public bool BorderUp { get; }
        public bool BorderDown { get; }
        public string RoleToken { get; }
        public string RoleGlyph { get; }
        public string CellLabel { get; }
        public string Tooltip { get; }

        private string CreateLabel()
        {
            if (!IsAssigned)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1}\n--",
                    Coordinate.X,
                    Coordinate.Y);
            }

            var marker = IsCoreSiteCell ? "*" : IsSeed ? "+" : string.Empty;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1}\n{2}{3}",
                Coordinate.X,
                Coordinate.Y,
                RoleGlyph,
                marker);
        }

        private string CreateTooltip()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Sector: {0} / Index {1}\n" +
                "Biome: {2}\n" +
                "PatchId: {3}\n" +
                "Role: {4}\n" +
                "Size/Perimeter/Compactness: {5} / {6} / {7}\n" +
                "Seed/CoreSite: {8} / {9}\n" +
                "Boundary L/R/U/D: {10}/{11}/{12}/{13}",
                Coordinate,
                Index,
                IsAssigned ? PrimaryBiomeId : "NONE",
                PatchId.HasValue ? PatchId.Value.Value : "NONE",
                RoleToken.Length == 0 ? "NONE" : RoleToken,
                PatchSize,
                Perimeter,
                CompactnessPermille,
                YesNo(IsSeed),
                YesNo(IsCoreSiteCell),
                BoundaryToken(BorderLeft, "L"),
                BoundaryToken(BorderRight, "R"),
                BoundaryToken(BorderUp, "U"),
                BoundaryToken(BorderDown, "D"));
        }

        private static string YesNo(bool value)
        {
            return value ? "YES" : "NO";
        }

        private static string BoundaryToken(bool value, string token)
        {
            return value ? token : "-";
        }
    }
}
