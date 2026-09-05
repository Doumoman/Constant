using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public enum GeneratedTerrainRunScope
    {
        Pattern = 0,
        Sector = 1,
        OneRing = 2,
        World = 3,
    }

    public static class GeneratedTerrainRunScopeCatalog
    {
        private static readonly ReadOnlyCollection<GeneratedTerrainRunScope> scopes =
            new ReadOnlyCollection<GeneratedTerrainRunScope>(new[]
            {
                GeneratedTerrainRunScope.Pattern,
                GeneratedTerrainRunScope.Sector,
                GeneratedTerrainRunScope.OneRing,
                GeneratedTerrainRunScope.World,
            });

        public static IReadOnlyList<GeneratedTerrainRunScope> Scopes => scopes;

        public static IReadOnlyList<string> Tokens => new ReadOnlyCollection<string>(
            scopes.Select(Token).ToArray());

        public static string Token(GeneratedTerrainRunScope scope)
        {
            if (!scopes.Contains(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
            return scope.ToString();
        }

        public static IReadOnlyList<WorldSectorCoordinate> ResolveSectorCoordinates(
            GeneratedTerrainRunScope scope, int centerX, int centerY)
        {
            if (scope == GeneratedTerrainRunScope.Pattern)
                return new ReadOnlyCollection<WorldSectorCoordinate>(Array.Empty<WorldSectorCoordinate>());

            if (scope == GeneratedTerrainRunScope.World)
            {
                var world = new List<WorldSectorCoordinate>();
                for (var y = 0; y < WorldGenConstants.SectorRows; y++)
                for (var x = 0; x < WorldGenConstants.SectorColumns; x++)
                    world.Add(new WorldSectorCoordinate(x, y));
                return new ReadOnlyCollection<WorldSectorCoordinate>(world);
            }

            var center = new WorldSectorCoordinate(centerX, centerY);
            if (!center.IsInBounds) throw new ArgumentOutOfRangeException(nameof(centerX),
                "Sector coordinate must be inside the 13x13 world.");
            if (scope == GeneratedTerrainRunScope.Sector)
                return new ReadOnlyCollection<WorldSectorCoordinate>(new[] { center });
            if (scope != GeneratedTerrainRunScope.OneRing)
                throw new ArgumentOutOfRangeException(nameof(scope));

            var oneRing = new List<WorldSectorCoordinate>();
            for (var dy = -WorldRollbackScope.Radius; dy <= WorldRollbackScope.Radius; dy++)
            for (var dx = -WorldRollbackScope.Radius; dx <= WorldRollbackScope.Radius; dx++)
            {
                var coordinate = new WorldSectorCoordinate(centerX + dx, centerY + dy);
                if (coordinate.IsInBounds) oneRing.Add(coordinate);
            }

            if (oneRing.Count > WorldRollbackScope.MaximumSectorCount)
                throw new InvalidOperationException("MAP15 Moore 1-ring cannot exceed nine sectors.");
            return new ReadOnlyCollection<WorldSectorCoordinate>(oneRing.OrderBy(value => value).ToArray());
        }

        public static IReadOnlyList<string> ResolveOutputBoundaries(
            GeneratedTerrainRunScope scope, string runId, string patternId, int centerX, int centerY)
        {
            var result = new List<string>();
            switch (scope)
            {
                case GeneratedTerrainRunScope.Pattern:
                    result.Add("patterns/" + SafeSegment(string.IsNullOrWhiteSpace(patternId)
                        ? "auto"
                        : patternId));
                    break;
                case GeneratedTerrainRunScope.Sector:
                case GeneratedTerrainRunScope.OneRing:
                    result.AddRange(ResolveSectorCoordinates(scope, centerX, centerY)
                        .Select(SectorBoundary));
                    break;
                case GeneratedTerrainRunScope.World:
                    result.Add("worlds/" + SafeSegment(runId));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope));
            }
            return new ReadOnlyCollection<string>(result.OrderBy(value => value,
                StringComparer.Ordinal).ToArray());
        }

        private static string SectorBoundary(WorldSectorCoordinate coordinate) =>
            string.Format(CultureInfo.InvariantCulture, "sectors/x{0:D2}_y{1:D2}",
                coordinate.X, coordinate.Y);

        private static string SafeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Path segment is required.");
            var trimmed = value.Trim();
            if (trimmed == "." || trimmed == ".." || trimmed.IndexOfAny(new[]
                { '/', '\\', ':', '*', '?', '\"', '<', '>', '|' }) >= 0)
                throw new ArgumentException("Path segment contains an unsafe character.");
            return trimmed;
        }
    }
}
