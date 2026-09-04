using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedTilemapSeamKind
    {
        MicroPattern = 1,
        MicroChunk = 2,
    }

    public enum GeneratedTilemapSeamOrientation
    {
        Vertical = 1,
        Horizontal = 2,
    }

    public enum GeneratedTilemapSeamExposureKind
    {
        ApprovedContinuous = 1,
        ApprovedMaterialTransition = 2,
        SocketOpening = 3,
        ProtectedRoute = 4,
        UnapprovedSolidAirDiscontinuity = 5,
        UnapprovedHazardProtectionDiscontinuity = 6,
        UnapprovedProvenanceBreak = 7,
        MissingNeighbor = 8,
        OutOfBoundsNeighbor = 9,
    }

    public sealed class GeneratedTilemapSeamCoordinate :
        IComparable<GeneratedTilemapSeamCoordinate>
    {
        public GeneratedTilemapSeamCoordinate(
            GeneratedTilemapSeamKind seamKind,
            GeneratedTilemapSeamOrientation orientation,
            int boundaryCoordinate,
            int firstX,
            int firstY,
            int secondX,
            int secondY)
        {
            SeamKind = seamKind;
            Orientation = orientation;
            BoundaryCoordinate = boundaryCoordinate;
            FirstX = firstX;
            FirstY = firstY;
            SecondX = secondX;
            SecondY = secondY;
            StableToken = string.Join("|", new[]
            {
                "SEAM_COORD", Number((int)SeamKind), Number((int)Orientation),
                Number(BoundaryCoordinate), Number(FirstX), Number(FirstY),
                Number(SecondX), Number(SecondY),
            });
        }

        public GeneratedTilemapSeamKind SeamKind { get; }
        public GeneratedTilemapSeamOrientation Orientation { get; }
        public int BoundaryCoordinate { get; }
        public int FirstX { get; }
        public int FirstY { get; }
        public int SecondX { get; }
        public int SecondY { get; }
        public bool IsMicroChunkBoundary => Orientation == GeneratedTilemapSeamOrientation.Vertical
            ? BoundaryCoordinate > 0 && BoundaryCoordinate %
                GeneratedTerrainGeometrySnapshot.CanonicalMicroChunkWidth == 0
            : BoundaryCoordinate > 0 && BoundaryCoordinate %
                GeneratedTerrainGeometrySnapshot.CanonicalMicroChunkHeight == 0;
        public string StableToken { get; }
        public int CompareTo(GeneratedTilemapSeamCoordinate other)
        {
            if (other == null) return -1;
            var comparison = SeamKind.CompareTo(other.SeamKind);
            if (comparison != 0) return comparison;
            comparison = Orientation.CompareTo(other.Orientation);
            if (comparison != 0) return comparison;
            comparison = BoundaryCoordinate.CompareTo(other.BoundaryCoordinate);
            if (comparison != 0) return comparison;
            comparison = FirstY.CompareTo(other.FirstY);
            return comparison != 0 ? comparison : FirstX.CompareTo(other.FirstX);
        }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTilemapSeamExposure :
        IComparable<GeneratedTilemapSeamExposure>
    {
        public GeneratedTilemapSeamExposure(
            GeneratedTilemapSeamCoordinate coordinate,
            GeneratedTilemapSeamExposureKind exposureKind,
            string firstMaterialToken,
            string secondMaterialToken,
            string firstTileToken,
            string secondTileToken,
            string firstProvenanceToken,
            string secondProvenanceToken)
        {
            Coordinate = coordinate;
            ExposureKind = exposureKind;
            FirstMaterialToken = firstMaterialToken ?? string.Empty;
            SecondMaterialToken = secondMaterialToken ?? string.Empty;
            FirstTileToken = firstTileToken ?? string.Empty;
            SecondTileToken = secondTileToken ?? string.Empty;
            FirstProvenanceToken = firstProvenanceToken ?? string.Empty;
            SecondProvenanceToken = secondProvenanceToken ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "SEAM_EXPOSURE", Coordinate == null ? "MISSING" : Coordinate.StableToken,
                Number((int)ExposureKind), ExposureKind.ToString().ToUpperInvariant(),
                FirstMaterialToken, SecondMaterialToken, FirstTileToken, SecondTileToken,
                FirstProvenanceToken, SecondProvenanceToken,
            });
        }

        public GeneratedTilemapSeamCoordinate Coordinate { get; }
        public GeneratedTilemapSeamExposureKind ExposureKind { get; }
        public string FirstMaterialToken { get; }
        public string SecondMaterialToken { get; }
        public string FirstTileToken { get; }
        public string SecondTileToken { get; }
        public string FirstProvenanceToken { get; }
        public string SecondProvenanceToken { get; }
        public bool IsApproved => ExposureKind == GeneratedTilemapSeamExposureKind.ApprovedContinuous ||
            ExposureKind == GeneratedTilemapSeamExposureKind.ApprovedMaterialTransition ||
            ExposureKind == GeneratedTilemapSeamExposureKind.SocketOpening ||
            ExposureKind == GeneratedTilemapSeamExposureKind.ProtectedRoute;
        public bool IsForbidden => ExposureKind ==
                GeneratedTilemapSeamExposureKind.UnapprovedSolidAirDiscontinuity ||
            ExposureKind == GeneratedTilemapSeamExposureKind.UnapprovedHazardProtectionDiscontinuity ||
            ExposureKind == GeneratedTilemapSeamExposureKind.UnapprovedProvenanceBreak;
        public string StableToken { get; }
        public int CompareTo(GeneratedTilemapSeamExposure other) => other == null
            ? -1 : Coordinate.CompareTo(other.Coordinate);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedTilemapSeamReport
    {
        private readonly ReadOnlyCollection<GeneratedTilemapSeamExposure> exposures;

        internal GeneratedTilemapSeamReport(IEnumerable<GeneratedTilemapSeamExposure> sourceExposures)
        {
            exposures = new ReadOnlyCollection<GeneratedTilemapSeamExposure>((sourceExposures ??
                Array.Empty<GeneratedTilemapSeamExposure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            OutputDigest = GeneratedTilemapSeamDigest.Compute(this);
        }

        public const string PolicyVersion = "MAP17_02_TILEMAP_SEAM_VALIDATION_V1";
        public IReadOnlyList<GeneratedTilemapSeamExposure> Exposures => exposures;
        public int MicroPatternSeamPairCount => exposures.Count(value => value.Coordinate != null &&
            value.Coordinate.SeamKind == GeneratedTilemapSeamKind.MicroPattern);
        public int MicroChunkSeamPairCount => exposures.Count(value => value.Coordinate != null &&
            value.Coordinate.SeamKind == GeneratedTilemapSeamKind.MicroChunk);
        public int MicroPatternOnlySeamPairCount => exposures.Count(value => value.Coordinate != null &&
            value.Coordinate.SeamKind == GeneratedTilemapSeamKind.MicroPattern &&
            !value.Coordinate.IsMicroChunkBoundary);
        public int ApprovedPairCount => exposures.Count(value => value.IsApproved);
        public int ApprovedContinuousPairCount => Count(GeneratedTilemapSeamExposureKind.ApprovedContinuous);
        public int ApprovedMaterialTransitionPairCount =>
            Count(GeneratedTilemapSeamExposureKind.ApprovedMaterialTransition);
        public int SocketOpeningPairCount => Count(GeneratedTilemapSeamExposureKind.SocketOpening);
        public int ProtectedRoutePairCount => Count(GeneratedTilemapSeamExposureKind.ProtectedRoute);
        public int UnapprovedPairCount => exposures.Count(value => value.IsForbidden);
        public int MissingNeighborPairCount => Count(GeneratedTilemapSeamExposureKind.MissingNeighbor);
        public int OutOfBoundsNeighborPairCount => Count(GeneratedTilemapSeamExposureKind.OutOfBoundsNeighbor);
        public int ForbiddenSolidAirPairCount =>
            Count(GeneratedTilemapSeamExposureKind.UnapprovedSolidAirDiscontinuity);
        public int ForbiddenHazardProtectionPairCount =>
            Count(GeneratedTilemapSeamExposureKind.UnapprovedHazardProtectionDiscontinuity);
        public int ForbiddenProvenancePairCount =>
            Count(GeneratedTilemapSeamExposureKind.UnapprovedProvenanceBreak);
        public string OutputDigest { get; }
        private int Count(GeneratedTilemapSeamExposureKind kind) =>
            exposures.Count(value => value.ExposureKind == kind);
    }

    public static class GeneratedTilemapSeamValidator
    {
        private static readonly GeneratedTilemapLayerId[] RequiredLayers =
            (GeneratedTilemapLayerId[])Enum.GetValues(typeof(GeneratedTilemapLayerId));

        public static GeneratedTilemapSeamReport BuildReport(
            IEnumerable<GeneratedTilemapCellBakeRecord> sourceRecords,
            GeneratedTerrainGeometrySnapshot geometry)
        {
            var records = (sourceRecords ?? Array.Empty<GeneratedTilemapCellBakeRecord>())
                .Where(value => value != null).ToArray();
            var lookup = records.GroupBy(value => Key(value.LayerId, value.SectorLocalIndex))
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var coordinates = EnumerateCoordinates(geometry).ToArray();
            var exposures = coordinates.Select(coordinate =>
                BuildExposure(coordinate, lookup, geometry)).ToArray();
            return new GeneratedTilemapSeamReport(exposures);
        }

        public static GeneratedTilemapSeamReport CreateReport(
            IEnumerable<GeneratedTilemapSeamExposure> exposures) =>
            new GeneratedTilemapSeamReport(exposures);

        public static IReadOnlyList<GeneratedTilemapBakeFailure> ValidateExposures(
            IEnumerable<GeneratedTilemapSeamExposure> sourceExposures)
        {
            var failures = new List<GeneratedTilemapBakeFailure>();
            foreach (var exposure in (sourceExposures ?? Array.Empty<GeneratedTilemapSeamExposure>())
                .Where(value => value != null))
            {
                if (exposure.IsForbidden)
                    failures.Add(new GeneratedTilemapBakeFailure(
                        GeneratedTilemapBakeFailureCode.ForbiddenSeamExposure,
                        exposure.Coordinate == null ? "MISSING" : exposure.Coordinate.StableToken,
                        exposure.ExposureKind.ToString()));
                else if (exposure.ExposureKind == GeneratedTilemapSeamExposureKind.MissingNeighbor)
                    failures.Add(new GeneratedTilemapBakeFailure(
                        GeneratedTilemapBakeFailureCode.MissingSeamNeighbor,
                        exposure.Coordinate == null ? "MISSING" : exposure.Coordinate.StableToken,
                        "A required seam neighbor is missing."));
                else if (exposure.ExposureKind == GeneratedTilemapSeamExposureKind.OutOfBoundsNeighbor)
                    failures.Add(new GeneratedTilemapBakeFailure(
                        GeneratedTilemapBakeFailureCode.OutOfBoundsSeamNeighbor,
                        exposure.Coordinate == null ? "MISSING" : exposure.Coordinate.StableToken,
                        "A seam neighbor lies outside the canonical sector."));
            }
            return new ReadOnlyCollection<GeneratedTilemapBakeFailure>(failures.Distinct()
                .OrderBy(value => value).ToArray());
        }

        private static IEnumerable<GeneratedTilemapSeamCoordinate> EnumerateCoordinates(
            GeneratedTerrainGeometrySnapshot geometry)
        {
            if (geometry == null) yield break;
            for (var boundaryX = geometry.MicroPatternWidth;
                boundaryX < geometry.SectorWidth; boundaryX += geometry.MicroPatternWidth)
                for (var y = 0; y < geometry.SectorHeight; y++)
                    yield return Coordinate(GeneratedTilemapSeamKind.MicroPattern,
                        GeneratedTilemapSeamOrientation.Vertical, boundaryX,
                        boundaryX - 1, y, boundaryX, y);
            for (var boundaryY = geometry.MicroPatternHeight;
                boundaryY < geometry.SectorHeight; boundaryY += geometry.MicroPatternHeight)
                for (var x = 0; x < geometry.SectorWidth; x++)
                    yield return Coordinate(GeneratedTilemapSeamKind.MicroPattern,
                        GeneratedTilemapSeamOrientation.Horizontal, boundaryY,
                        x, boundaryY - 1, x, boundaryY);
            for (var boundaryX = geometry.MicroChunkWidth;
                boundaryX < geometry.SectorWidth; boundaryX += geometry.MicroChunkWidth)
                for (var y = 0; y < geometry.SectorHeight; y++)
                    yield return Coordinate(GeneratedTilemapSeamKind.MicroChunk,
                        GeneratedTilemapSeamOrientation.Vertical, boundaryX,
                        boundaryX - 1, y, boundaryX, y);
            for (var boundaryY = geometry.MicroChunkHeight;
                boundaryY < geometry.SectorHeight; boundaryY += geometry.MicroChunkHeight)
                for (var x = 0; x < geometry.SectorWidth; x++)
                    yield return Coordinate(GeneratedTilemapSeamKind.MicroChunk,
                        GeneratedTilemapSeamOrientation.Horizontal, boundaryY,
                        x, boundaryY - 1, x, boundaryY);
        }

        private static GeneratedTilemapSeamExposure BuildExposure(
            GeneratedTilemapSeamCoordinate coordinate,
            IDictionary<string, GeneratedTilemapCellBakeRecord> lookup,
            GeneratedTerrainGeometrySnapshot geometry)
        {
            if (coordinate.FirstX < 0 || coordinate.FirstX >= geometry.SectorWidth ||
                coordinate.FirstY < 0 || coordinate.FirstY >= geometry.SectorHeight ||
                coordinate.SecondX < 0 || coordinate.SecondX >= geometry.SectorWidth ||
                coordinate.SecondY < 0 || coordinate.SecondY >= geometry.SectorHeight)
                return Exposure(coordinate, GeneratedTilemapSeamExposureKind.OutOfBoundsNeighbor,
                    null, null);
            var firstIndex = coordinate.FirstY * geometry.SectorWidth + coordinate.FirstX;
            var secondIndex = coordinate.SecondY * geometry.SectorWidth + coordinate.SecondX;
            var first = Profile(firstIndex, lookup);
            var second = Profile(secondIndex, lookup);
            if (first == null || second == null)
                return Exposure(coordinate, GeneratedTilemapSeamExposureKind.MissingNeighbor,
                    first, second);

            var hasProvenance = first.Records.All(HasProvenance) && second.Records.All(HasProvenance);
            var solidTransition = IsSolid(first.Terrain.CellKind) != IsSolid(second.Terrain.CellKind);
            var hazardProtectionTransition = first.Hazard.CellKind != second.Hazard.CellKind &&
                first.Protection.CellKind != second.Protection.CellKind;
            GeneratedTilemapSeamExposureKind kind;
            if (!hasProvenance)
                kind = solidTransition
                    ? GeneratedTilemapSeamExposureKind.UnapprovedSolidAirDiscontinuity
                    : hazardProtectionTransition
                        ? GeneratedTilemapSeamExposureKind.UnapprovedHazardProtectionDiscontinuity
                        : GeneratedTilemapSeamExposureKind.UnapprovedProvenanceBreak;
            else if (first.Records.Any(value => value.IsProtected) ||
                     second.Records.Any(value => value.IsProtected))
                kind = GeneratedTilemapSeamExposureKind.ProtectedRoute;
            else if (coordinate.SeamKind == GeneratedTilemapSeamKind.MicroChunk &&
                     IsOpen(first.Terrain.CellKind) && IsOpen(second.Terrain.CellKind))
                kind = GeneratedTilemapSeamExposureKind.SocketOpening;
            else if (first.SemanticToken == second.SemanticToken)
                kind = GeneratedTilemapSeamExposureKind.ApprovedContinuous;
            else
                kind = GeneratedTilemapSeamExposureKind.ApprovedMaterialTransition;
            return Exposure(coordinate, kind, first, second);
        }

        private static CellProfile Profile(
            int index,
            IDictionary<string, GeneratedTilemapCellBakeRecord> lookup)
        {
            var records = new List<GeneratedTilemapCellBakeRecord>();
            foreach (var layer in RequiredLayers)
            {
                GeneratedTilemapCellBakeRecord record;
                if (!lookup.TryGetValue(Key(layer, index), out record)) return null;
                records.Add(record);
            }
            return new CellProfile(records);
        }

        private static GeneratedTilemapSeamExposure Exposure(
            GeneratedTilemapSeamCoordinate coordinate,
            GeneratedTilemapSeamExposureKind kind,
            CellProfile first,
            CellProfile second) => new GeneratedTilemapSeamExposure(
                coordinate, kind,
                first == null ? string.Empty : first.Material.StableToken,
                second == null ? string.Empty : second.Material.StableToken,
                first == null ? string.Empty : first.Terrain.TileCode.Value,
                second == null ? string.Empty : second.Terrain.TileCode.Value,
                first == null ? string.Empty : first.ProvenanceToken,
                second == null ? string.Empty : second.ProvenanceToken);

        private static GeneratedTilemapSeamCoordinate Coordinate(
            GeneratedTilemapSeamKind kind,
            GeneratedTilemapSeamOrientation orientation,
            int boundary,
            int firstX,
            int firstY,
            int secondX,
            int secondY) => new GeneratedTilemapSeamCoordinate(kind, orientation, boundary,
                firstX, firstY, secondX, secondY);

        private static bool HasProvenance(GeneratedTilemapCellBakeRecord record) =>
            record != null && !string.IsNullOrEmpty(record.PlacementId) &&
            !string.IsNullOrEmpty(record.ProvenanceId) &&
            !string.IsNullOrEmpty(record.SourceCellToken) &&
            !string.IsNullOrEmpty(record.SourceLayerStableToken);
        private static bool IsSolid(FinalCanvasCellKind kind) =>
            kind == FinalCanvasCellKind.Solid || kind == FinalCanvasCellKind.Ground ||
            kind == FinalCanvasCellKind.Blocked;
        private static bool IsOpen(FinalCanvasCellKind kind) =>
            kind == FinalCanvasCellKind.Air || kind == FinalCanvasCellKind.Traversable ||
            kind == FinalCanvasCellKind.ProtectedOpen;
        private static string Key(GeneratedTilemapLayerId layer, int index) =>
            Number((int)layer) + "|" + Number(index);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private sealed class CellProfile
        {
            public CellProfile(IEnumerable<GeneratedTilemapCellBakeRecord> records)
            {
                Records = records.OrderBy(value => value).ToArray();
                Terrain = Records.Single(value => value.LayerId == GeneratedTilemapLayerId.Terrain);
                Material = Records.Single(value => value.LayerId == GeneratedTilemapLayerId.Material);
                Hazard = Records.Single(value => value.LayerId == GeneratedTilemapLayerId.Hazard);
                Protection = Records.Single(value => value.LayerId == GeneratedTilemapLayerId.Protection);
                SemanticToken = string.Join("/", Records.Select(value => string.Join(":", new[]
                {
                    Number((int)value.LayerId), value.CellKind.ToString(),
                    value.TileCode == null ? "MISSING" : value.TileCode.Value,
                    value.SourceOwner.ToString(), value.Protection.ToString(),
                })));
                ProvenanceToken = BakingCanonicalDigest.HashCanonicalLines(
                    Records.Select(value => value.ProvenanceId + "|" + value.SourceCellToken));
            }

            public GeneratedTilemapCellBakeRecord[] Records { get; }
            public GeneratedTilemapCellBakeRecord Terrain { get; }
            public GeneratedTilemapCellBakeRecord Material { get; }
            public GeneratedTilemapCellBakeRecord Hazard { get; }
            public GeneratedTilemapCellBakeRecord Protection { get; }
            public string SemanticToken { get; }
            public string ProvenanceToken { get; }
        }
    }

    public static class GeneratedTilemapSeamDigest
    {
        public static string Compute(GeneratedTilemapSeamReport report)
        {
            if (report == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedTilemapSeamReport.PolicyVersion,
                "COUNTS|" + Number(report.MicroPatternSeamPairCount) + "|" +
                    Number(report.MicroChunkSeamPairCount) + "|" +
                    Number(report.MicroPatternOnlySeamPairCount) + "|" +
                    Number(report.ApprovedPairCount) + "|" + Number(report.UnapprovedPairCount) + "|" +
                    Number(report.MissingNeighborPairCount) + "|" +
                    Number(report.OutOfBoundsNeighborPairCount),
            };
            lines.AddRange(report.Exposures.OrderBy(value => value)
                .Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
