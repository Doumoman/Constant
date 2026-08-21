using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum BiomePatchRole
    {
        Core,
        Satellite,
        Intrusion
    }

    public static class BiomePatchRoleTokenCodec
    {
        public static bool TryParse(string token, out BiomePatchRole value)
        {
            switch (token)
            {
                case "CORE": value = BiomePatchRole.Core; return true;
                case "SATELLITE": value = BiomePatchRole.Satellite; return true;
                case "INTRUSION": value = BiomePatchRole.Intrusion; return true;
                default: value = default(BiomePatchRole); return false;
            }
        }

        public static string ToToken(BiomePatchRole value)
        {
            switch (value)
            {
                case BiomePatchRole.Core: return "CORE";
                case BiomePatchRole.Satellite: return "SATELLITE";
                case BiomePatchRole.Intrusion: return "INTRUSION";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    internal static class BiomePatchModelValidation
    {
        public static bool IsDefined(BiomePatchRole role)
        {
            return role == BiomePatchRole.Core ||
                   role == BiomePatchRole.Satellite ||
                   role == BiomePatchRole.Intrusion;
        }

        public static void ValidateGridIdentity(int sectorIndex, SectorCoord sector)
        {
            if (sectorIndex < 0 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (sector != WorldGridIndex.ToCoordinate(sectorIndex))
                throw new ArgumentException("Sector index and coordinate must match the world grid.", nameof(sector));
        }
    }
}
