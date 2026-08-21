using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct RngStreamScope
    {
        public RngResetScope ResetScope { get; }
        public string Identity { get; }
        public int AttemptOrdinal { get; }

        public RngStreamScope(
            RngResetScope resetScope,
            string identity,
            int attemptOrdinal = 0)
        {
            RngResetScopeToken.Format(resetScope);
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (attemptOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            }

            if (resetScope == RngResetScope.World)
            {
                if (identity.Length != 0)
                {
                    throw new ArgumentException("WORLD scope identity must be empty.", nameof(identity));
                }
            }
            else if (identity.Length == 0)
            {
                throw new ArgumentException("Non-WORLD scope identity must be non-empty.", nameof(identity));
            }

            ResetScope = resetScope;
            Identity = identity;
            AttemptOrdinal = attemptOrdinal;
        }

        public static RngStreamScope World(int attemptOrdinal = 0)
        {
            return new RngStreamScope(RngResetScope.World, string.Empty, attemptOrdinal);
        }

        public static RngStreamScope Pass(string passId, int attemptOrdinal = 0)
        {
            return new RngStreamScope(RngResetScope.Pass, passId, attemptOrdinal);
        }

        public static RngStreamScope Sector(SectorCoord coordinate, int attemptOrdinal = 0)
        {
            if (!WorldCoordinateUtility.IsValid(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate));
            }

            return new RngStreamScope(
                RngResetScope.Sector,
                string.Format(CultureInfo.InvariantCulture, "{0},{1}", coordinate.X, coordinate.Y),
                attemptOrdinal);
        }

        public static RngStreamScope Patch(string patchId, int attemptOrdinal = 0)
        {
            return new RngStreamScope(RngResetScope.Patch, patchId, attemptOrdinal);
        }

        public static RngStreamScope Site(string siteId, int attemptOrdinal = 0)
        {
            return new RngStreamScope(RngResetScope.Site, siteId, attemptOrdinal);
        }

        public static RngStreamScope Spawn(string spawnScopeId, int attemptOrdinal = 0)
        {
            return new RngStreamScope(RngResetScope.Spawn, spawnScopeId, attemptOrdinal);
        }

        internal void Validate()
        {
            _ = new RngStreamScope(ResetScope, Identity, AttemptOrdinal);
        }
    }
}
