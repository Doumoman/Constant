using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum RngResetScope
    {
        World,
        Pass,
        Sector,
        Patch,
        Site,
        Spawn
    }

    public static class RngResetScopeToken
    {
        public static string Format(RngResetScope scope)
        {
            switch (scope)
            {
                case RngResetScope.World:
                    return "WORLD";
                case RngResetScope.Pass:
                    return "PASS";
                case RngResetScope.Sector:
                    return "SECTOR";
                case RngResetScope.Patch:
                    return "PATCH";
                case RngResetScope.Site:
                    return "SITE";
                case RngResetScope.Spawn:
                    return "SPAWN";
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Undefined RNG reset scope.");
            }
        }

        public static RngResetScope Parse(string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            switch (token)
            {
                case "WORLD":
                    return RngResetScope.World;
                case "PASS":
                    return RngResetScope.Pass;
                case "SECTOR":
                    return RngResetScope.Sector;
                case "PATCH":
                    return RngResetScope.Patch;
                case "SITE":
                    return RngResetScope.Site;
                case "SPAWN":
                    return RngResetScope.Spawn;
                default:
                    throw new ArgumentException("Unknown RNG reset scope token.", nameof(token));
            }
        }
    }
}
