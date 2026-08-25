using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceRootMillBoundaryValidator
    {
        public MoonpalaceRootMillBoundaryContentReport Validate(
            MoonpalaceRootMillBoundaryAuthoringData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var issues = new List<string>();
            var expectedCandidateIds = MoonpalaceRootMillBoundaryAuthoringContract.CandidateIds;
            var expectedMicrochunkIds = MoonpalaceRootMillBoundaryAuthoringContract.MicrochunkIds;
            var candidates = data.Candidates.Where(row => row != null).ToList();
            var microchunks = data.Microchunks.Where(row => row != null).ToList();
            var tiles = data.Tiles.Where(row => row != null).ToList();
            var sockets = data.Sockets.Where(row => row != null).ToList();

            if (candidates.Count != data.Candidates.Count) issues.Add("Candidate row cannot be null.");
            if (microchunks.Count != data.Microchunks.Count) issues.Add("Microchunk row cannot be null.");
            if (tiles.Count != data.Tiles.Count) issues.Add("Tile row cannot be null.");
            if (sockets.Count != data.Sockets.Count) issues.Add("Socket row cannot be null.");

            var candidateIds = candidates.Select(row => row.CandidateId)
                .Where(value => value != null)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var microchunkIds = microchunks.Select(row => row.MicrochunkId)
                .Where(value => value != null)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            RequireExactSet(candidateIds, expectedCandidateIds, "candidate", issues);
            RequireExactSet(microchunkIds, expectedMicrochunkIds, "microchunk", issues);

            var expectedMatrix = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in MoonpalaceRootMillBoundaryAuthoringContract.ProfileIds)
            {
                expectedMatrix.Add(profile + "|HORIZONTAL");
                expectedMatrix.Add(profile + "|VERTICAL");
            }

            var actualMatrix = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < candidates.Count; index++)
            {
                var row = candidates[index];
                var orientationToken = OrientationToken(row.Orientation);
                actualMatrix.Add((row.ProfileId ?? string.Empty) + "|" + orientationToken);
                var expectedIndex = IndexOf(expectedCandidateIds, row.CandidateId);
                if (expectedIndex < 0) continue;
                var expectedOrientation = expectedIndex % 2 == 0
                    ? MoonpalaceBoundaryOrientation.Horizontal
                    : MoonpalaceBoundaryOrientation.Vertical;
                var expectedProfile = MoonpalaceRootMillBoundaryAuthoringContract.ProfileIds[expectedIndex / 2];
                var expectedSignature = expectedOrientation == MoonpalaceBoundaryOrientation.Horizontal
                    ? MoonpalaceRootMillBoundaryAuthoringContract.HorizontalEdgeSignatureId
                    : MoonpalaceRootMillBoundaryAuthoringContract.VerticalEdgeSignatureId;

                Require(row.MicrochunkId == expectedMicrochunkIds[expectedIndex], row.CandidateId + ": microchunk mismatch.", issues);
                Require(row.BiomeAId == MoonpalaceRootMillBoundaryAuthoringContract.BiomeAId, row.CandidateId + ": biome A mismatch.", issues);
                Require(row.BiomeBId == MoonpalaceRootMillBoundaryAuthoringContract.BiomeBId, row.CandidateId + ": biome B mismatch.", issues);
                Require(row.ProfileId == expectedProfile, row.CandidateId + ": profile mismatch.", issues);
                Require(row.Orientation == expectedOrientation, row.CandidateId + ": orientation mismatch.", issues);
                Require(row.RouteType == 1, row.CandidateId + ": route type must be 1.", issues);
                Require(row.EntryEdgeSignatureId == expectedSignature, row.CandidateId + ": entry signature mismatch.", issues);
                Require(row.ExitEdgeSignatureId == expectedSignature, row.CandidateId + ": exit signature mismatch.", issues);
                Require(row.Weight > 0, row.CandidateId + ": weight must be positive.", issues);
                Require(row.Reversible, row.CandidateId + ": reversible must be true.", issues);
                Require(row.Active, row.CandidateId + ": active must be true.", issues);
                Require(row.MandatoryAllowed, row.CandidateId + ": mandatory route must be allowed.", issues);
                Require(row.ToolRequirement == MoonpalaceRootMillBoundaryAuthoringContract.NoToolRequirement,
                    row.CandidateId + ": tool requirement must be NONE.", issues);
            }

            foreach (var row in microchunks)
            {
                if (!MoonpalaceRootMillBoundaryAuthoringContract.IsOwnedMicrochunk(row.MicrochunkId)) continue;
                Require(row.WidthTiles == 12 && row.HeightTiles == 8, row.MicrochunkId + ": dimensions must be 12x8.", issues);
                Require(row.UsageClass == "BOUNDARY", row.MicrochunkId + ": usage class must be BOUNDARY.", issues);
                Require(row.BiomeIds == "BIO_CASSIA_ROOT|BIO_ABANDONED_MILL", row.MicrochunkId + ": biome IDs mismatch.", issues);
                Require(row.RouteRoles != null && row.RouteRoles.Contains("BOUNDARY"), row.MicrochunkId + ": boundary role missing.", issues);
                Require(row.TileDataComplete, row.MicrochunkId + ": tile data must be complete.", issues);
                Require(row.Active, row.MicrochunkId + ": active must be true.", issues);
            }

            var rowsPerMicrochunk = new Dictionary<string, int>(StringComparer.Ordinal);
            var warningCategoryCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var microchunkId in expectedMicrochunkIds)
            {
                var ownedTiles = tiles.Where(row => row.MicrochunkId == microchunkId).ToList();
                rowsPerMicrochunk[microchunkId] = ownedTiles.Count;
                Require(ownedTiles.Count == MoonpalaceRootMillBoundaryAuthoringContract.CellsPerMicrochunk,
                    microchunkId + ": tile row count must be 96.", issues);
                var coordinateCount = ownedTiles.Select(row => row.LocalY * 12 + row.LocalX).Distinct().Count();
                Require(ownedTiles.All(row => row.LocalX >= 0 && row.LocalX < 12 && row.LocalY >= 0 && row.LocalY < 8),
                    microchunkId + ": tile coordinate out of range.", issues);
                Require(coordinateCount == MoonpalaceRootMillBoundaryAuthoringContract.CellsPerMicrochunk,
                    microchunkId + ": tile coordinates must cover 12x8 exactly.", issues);

                var tileEvidence = ownedTiles.Any(row => row.GroundCode == "G_CASSIA_WOOD") &&
                                   ownedTiles.Any(row => row.GroundCode == "G_MILL_METAL");
                var backgroundEvidence = ownedTiles.Any(row => row.DecorBackCode == "DB_ROOT") &&
                                         ownedTiles.Any(row => row.DecorBackCode == "DB_MILL");
                var warningCount = (tileEvidence ? 1 : 0) + (backgroundEvidence ? 1 : 0);
                warningCategoryCounts[microchunkId] = warningCount;
                Require(warningCount >= 2, microchunkId + ": Tile and Background warning evidence required.", issues);
                Require(ownedTiles.Any(row => row.MarkerCode == "M_ROUTE_MAIN"), microchunkId + ": route marker missing.", issues);
                Require(ownedTiles.Any(row => row.MarkerCode == "M_SOCKET"), microchunkId + ": socket marker missing.", issues);
            }

            var horizontalSocketShapeValid = true;
            var verticalSocketShapeValid = true;
            var mandatoryAllowed = candidates.All(row => row.MandatoryAllowed) && sockets.All(row => row.MandatoryAllowed);
            var toolRequirementNone = candidates.All(row => row.ToolRequirement == "NONE") &&
                                      sockets.All(row => row.ToolRequirement == "NONE");
            for (var index = 0; index < expectedMicrochunkIds.Count; index++)
            {
                var microchunkId = expectedMicrochunkIds[index];
                var ownedSockets = sockets.Where(row => row.MicrochunkId == microchunkId).ToList();
                Require(ownedSockets.Count == 2, microchunkId + ": exactly two sockets required.", issues);
                var horizontal = index % 2 == 0;
                var expectedSides = horizontal ? new[] { "L", "R" } : new[] { "D", "U" };
                var expectedTraversal = horizontal ? "WALK" : "CLIMB";
                var expectedSignature = horizontal
                    ? MoonpalaceRootMillBoundaryAuthoringContract.HorizontalEdgeSignatureId
                    : MoonpalaceRootMillBoundaryAuthoringContract.VerticalEdgeSignatureId;
                var shapeValid = ownedSockets.Select(row => row.Side).OrderBy(value => value, StringComparer.Ordinal)
                                     .SequenceEqual(expectedSides) &&
                                 ownedSockets.All(row => row.TraversalKind == expectedTraversal &&
                                                         row.EdgeSignatureId == expectedSignature &&
                                                         row.RouteLayer == "MANDATORY" &&
                                                         row.MinimumSafeTiles >= 2);
                if (horizontal) horizontalSocketShapeValid &= shapeValid;
                else verticalSocketShapeValid &= shapeValid;
                Require(shapeValid, microchunkId + ": socket shape mismatch.", issues);
            }

            Require(data.GeneratedCsvCreated == 0, "Generated CSV files must not be created.", issues);
            Require(data.ExistingRowsModified == 0, "Existing rows must not be modified.", issues);
            Require(data.OtherPairRowsModified == 0, "Other pair rows must not be modified.", issues);
            Require(data.CraterRootRowsModified == 0, "Crater/Root rows must not be modified.", issues);
            Require(data.CraterMillRowsModified == 0, "Crater/Mill rows must not be modified.", issues);
            Require(data.CraterDoughRowsModified == 0, "Crater/Dough rows must not be modified.", issues);

            var matrixComplete = actualMatrix.SetEquals(expectedMatrix) && candidates.Count == expectedMatrix.Count;
            Require(matrixComplete, "Profile/orientation matrix must be complete.", issues);
            issues.Sort(StringComparer.Ordinal);
            return new MoonpalaceRootMillBoundaryContentReport(
                candidateIds,
                microchunkIds,
                candidates.Count,
                matrixComplete,
                tiles.Count,
                rowsPerMicrochunk,
                sockets.Count,
                horizontalSocketShapeValid,
                verticalSocketShapeValid,
                mandatoryAllowed,
                toolRequirementNone,
                warningCategoryCounts,
                data.GeneratedCsvCreated,
                data.ExistingRowsModified,
                data.OtherPairRowsModified,
                data.CraterRootRowsModified,
                data.CraterMillRowsModified,
                data.CraterDoughRowsModified,
                issues);
        }

        private static int IndexOf(IReadOnlyList<string> values, string value)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], value, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private static string OrientationToken(MoonpalaceBoundaryOrientation orientation)
        {
            if (orientation == MoonpalaceBoundaryOrientation.Horizontal) return "HORIZONTAL";
            if (orientation == MoonpalaceBoundaryOrientation.Vertical) return "VERTICAL";
            return "INVALID";
        }

        private static void RequireExactSet(
            IEnumerable<string> actual,
            IEnumerable<string> expected,
            string label,
            ICollection<string> issues)
        {
            var actualValues = actual.ToArray();
            var expectedValues = expected.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (actualValues.Length != actualValues.Distinct(StringComparer.Ordinal).Count())
            {
                issues.Add("Duplicate " + label + " ID.");
            }
            if (!actualValues.SequenceEqual(expectedValues))
            {
                issues.Add("Exact " + label + " ID set mismatch.");
            }
        }

        private static void Require(bool condition, string issue, ICollection<string> issues)
        {
            if (!condition) issues.Add(issue);
        }
    }
}
