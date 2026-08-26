using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCoverageValidator
    {
        public const string ExpectedAuthoringManifestSha256 =
            "f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb";
        public const string ExpectedPreviousTaskSha256 =
            "67f2852a01e19d61a78160e6cae79c77b4103ccf2d378e98c7e08becfcb3fda5";
        public const string HorizontalEdgeSignatureId = "EDGE_H_MID_WALK";
        public const string VerticalEdgeSignatureId = "EDGE_V_CENTER_CLIMB";

        public sealed class SourceChain
        {
            public SourceChain(
                string authoringManifestSha256,
                string previousTaskSha256,
                int generatedCsvCount,
                int authoringMutationCount)
            {
                AuthoringManifestSha256 = authoringManifestSha256;
                PreviousTaskSha256 = previousTaskSha256;
                GeneratedCsvCount = generatedCsvCount;
                AuthoringMutationCount = authoringMutationCount;
            }

            public string AuthoringManifestSha256 { get; }
            public string PreviousTaskSha256 { get; }
            public int GeneratedCsvCount { get; }
            public int AuthoringMutationCount { get; }
        }

        public MoonpalaceBoundaryCoverageReport Validate(
            IEnumerable<MoonpalaceBoundaryCoverageRequirement> requirements,
            IEnumerable<MoonpalaceBoundaryCoverageCandidateEvidence> candidates,
            SourceChain sourceChain)
        {
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (sourceChain == null) throw new ArgumentNullException(nameof(sourceChain));

            var requirementRows = requirements.ToArray();
            var candidateRows = candidates.ToArray();
            var validRequirements = requirementRows.Where(value => value != null).ToArray();
            var validCandidates = candidateRows.Where(value => value != null).ToArray();
            var globalIssues = new List<MoonpalaceBoundaryCoverageIssue>();

            if (validRequirements.Length != requirementRows.Length)
            {
                globalIssues.Add(GlobalIssue(
                    MoonpalaceBoundaryCoverageIssueCode.UnexpectedPair,
                    "Pair requirement rows cannot be null."));
            }
            if (validCandidates.Length != candidateRows.Length)
            {
                globalIssues.Add(GlobalIssue(
                    MoonpalaceBoundaryCoverageIssueCode.InvalidCandidate,
                    "Candidate evidence rows cannot be null."));
            }

            ValidateSourceChain(sourceChain, globalIssues);

            foreach (var requirement in validRequirements)
            {
                if (!MoonpalaceBoundaryCoverageRequirement.TryGetCanonical(requirement.PairRuleId, out _))
                {
                    globalIssues.Add(new MoonpalaceBoundaryCoverageIssue(
                        MoonpalaceBoundaryCoverageIssueCode.UnexpectedPair,
                        int.MaxValue,
                        requirement.PairRuleId,
                        InvalidOrientation,
                        int.MaxValue,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        "Unexpected pair requirement."));
                }
            }

            foreach (var candidate in validCandidates)
            {
                if (!MoonpalaceBoundaryCoverageRequirement.TryGetCanonical(candidate.PairRuleId, out _))
                {
                    globalIssues.Add(new MoonpalaceBoundaryCoverageIssue(
                        MoonpalaceBoundaryCoverageIssueCode.UnexpectedPair,
                        int.MaxValue,
                        candidate.PairRuleId,
                        candidate.Orientation,
                        int.MaxValue,
                        candidate.ProfileId,
                        candidate.CandidateId,
                        candidate.MicrochunkId,
                        "Candidate belongs to an unexpected pair."));
                }
            }

            var duplicateCandidateIds = new HashSet<string>(
                validCandidates.Where(value => !string.IsNullOrEmpty(value.CandidateId))
                    .GroupBy(value => value.CandidateId, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key),
                StringComparer.Ordinal);
            var duplicateMicrochunkIds = new HashSet<string>(
                validCandidates.Where(value => !string.IsNullOrEmpty(value.MicrochunkId))
                    .GroupBy(value => value.MicrochunkId, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key),
                StringComparer.Ordinal);

            var pairReports = new List<MoonpalaceBoundaryCoveragePairReport>();
            foreach (var expected in MoonpalaceBoundaryCoverageRequirement.Canonical)
            {
                var pairIssues = new List<MoonpalaceBoundaryCoverageIssue>();
                var actualRequirements = validRequirements.Where(value =>
                    string.Equals(value.PairRuleId, expected.PairRuleId, StringComparison.Ordinal)).ToArray();
                ValidateRequirement(expected, actualRequirements, pairIssues);

                var pairCandidates = validCandidates.Where(value =>
                    string.Equals(value.PairRuleId, expected.PairRuleId, StringComparison.Ordinal)).ToArray();
                var orientationCoverage = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    { "HORIZONTAL", pairCandidates.Count(value => value.Orientation == MoonpalaceBoundaryOrientation.Horizontal) },
                    { "VERTICAL", pairCandidates.Count(value => value.Orientation == MoonpalaceBoundaryOrientation.Vertical) },
                };
                var profileCoverage = expected.AllowedProfileIds.ToDictionary(
                    profile => profile,
                    profile => pairCandidates.Count(value => string.Equals(value.ProfileId, profile, StringComparison.Ordinal)),
                    StringComparer.Ordinal);

                ValidatePair(
                    expected,
                    pairCandidates,
                    duplicateCandidateIds,
                    duplicateMicrochunkIds,
                    orientationCoverage,
                    profileCoverage,
                    pairIssues);
                pairIssues.Sort();
                pairReports.Add(new MoonpalaceBoundaryCoveragePairReport(
                    expected,
                    pairCandidates.Length,
                    pairCandidates.Select(value => value.MicrochunkId)
                        .Where(value => !string.IsNullOrEmpty(value))
                        .Distinct(StringComparer.Ordinal).Count(),
                    pairCandidates.Sum(value => value.TileCells.Count),
                    pairCandidates.Sum(value => value.Sockets.Count),
                    orientationCoverage,
                    profileCoverage,
                    pairIssues));
            }

            var issues = pairReports.SelectMany(value => value.Issues).Concat(globalIssues).OrderBy(value => value).ToArray();
            var totalOrientationCoverage = new Dictionary<string, int>(StringComparer.Ordinal);
            var totalProfileCoverage = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pairReport in pairReports)
            {
                foreach (var value in pairReport.OrientationCoverage)
                {
                    totalOrientationCoverage.Add(pairReport.PairRuleId + "|" + value.Key, value.Value);
                }
                foreach (var value in pairReport.ProfileCoverage)
                {
                    totalProfileCoverage.Add(pairReport.PairRuleId + "|" + value.Key, value.Value);
                }
            }

            return new MoonpalaceBoundaryCoverageReport(
                pairReports,
                validCandidates.Length,
                validCandidates.Select(value => value.MicrochunkId)
                    .Where(value => !string.IsNullOrEmpty(value))
                    .Distinct(StringComparer.Ordinal).Count(),
                validCandidates.Sum(value => value.TileCells.Count),
                validCandidates.Sum(value => value.Sockets.Count),
                totalOrientationCoverage,
                totalProfileCoverage,
                sourceChain.GeneratedCsvCount,
                sourceChain.AuthoringManifestSha256,
                issues);
        }

        private static void ValidateSourceChain(
            SourceChain sourceChain,
            ICollection<MoonpalaceBoundaryCoverageIssue> issues)
        {
            if (sourceChain.GeneratedCsvCount != 0)
            {
                issues.Add(GlobalIssue(
                    MoonpalaceBoundaryCoverageIssueCode.GeneratedCsvPresent,
                    "Generated CSV count must remain zero."));
            }
            if (sourceChain.AuthoringMutationCount != 0)
            {
                issues.Add(GlobalIssue(
                    MoonpalaceBoundaryCoverageIssueCode.AuthoringMutationDetected,
                    "Authoring content must remain byte-stable during coverage validation."));
            }
            if (!string.Equals(sourceChain.AuthoringManifestSha256, ExpectedAuthoringManifestSha256, StringComparison.Ordinal) ||
                !string.Equals(sourceChain.PreviousTaskSha256, ExpectedPreviousTaskSha256, StringComparison.Ordinal))
            {
                issues.Add(GlobalIssue(
                    MoonpalaceBoundaryCoverageIssueCode.InvalidSourceChain,
                    "Authoring manifest or previous installed Task SHA-256 does not match the approved source chain."));
            }
        }

        private static void ValidateRequirement(
            MoonpalaceBoundaryCoverageRequirement expected,
            IReadOnlyList<MoonpalaceBoundaryCoverageRequirement> actualRows,
            ICollection<MoonpalaceBoundaryCoverageIssue> issues)
        {
            if (actualRows.Count == 0)
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.MissingPair,
                    "Required pair record is missing."));
                return;
            }
            if (actualRows.Count > 1)
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.DuplicatePair,
                    "Pair record must be unique."));
            }

            var actual = actualRows[0];
            if (!actual.Active)
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.InactivePair,
                    "Pair record must be active."));
            }
            if (actual.PairOrder != expected.PairOrder ||
                !string.Equals(actual.BiomeAId, expected.BiomeAId, StringComparison.Ordinal) ||
                !string.Equals(actual.BiomeBId, expected.BiomeBId, StringComparison.Ordinal) ||
                string.Equals(actual.BiomeAId, actual.BiomeBId, StringComparison.Ordinal))
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.UnexpectedPair,
                    "Pair biomes or canonical order do not match."));
            }

            foreach (var profile in expected.AllowedProfileIds.Where(profile =>
                         !actual.AllowedProfileIds.Contains(profile, StringComparer.Ordinal)))
            {
                issues.Add(ProfileIssue(expected, MoonpalaceBoundaryCoverageIssueCode.MissingProfile, profile,
                    "Required profile is missing from the pair rule."));
            }
            foreach (var profile in actual.AllowedProfileIds.Where(profile =>
                         !expected.AllowedProfileIds.Contains(profile, StringComparer.Ordinal)))
            {
                issues.Add(ProfileIssue(expected, MoonpalaceBoundaryCoverageIssueCode.UnexpectedProfile, profile,
                    "Pair rule contains an unexpected profile."));
            }
            if (!actual.AllowedProfileIds.SequenceEqual(expected.AllowedProfileIds) ||
                !actual.ProfileWeights.SequenceEqual(expected.ProfileWeights) ||
                !string.Equals(actual.DefaultProfileId, expected.DefaultProfileId, StringComparison.Ordinal) ||
                actual.ExpectedCandidateCount != expected.ExpectedCandidateCount ||
                actual.ExpectedMicrochunkCount != expected.ExpectedMicrochunkCount ||
                actual.ExpectedTileRowCount != expected.ExpectedTileRowCount ||
                actual.ExpectedSocketRowCount != expected.ExpectedSocketRowCount)
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.UnexpectedProfile,
                    "Profile order, weights, default, or expected row totals do not match."));
            }
        }

        private static void ValidatePair(
            MoonpalaceBoundaryCoverageRequirement expected,
            IReadOnlyList<MoonpalaceBoundaryCoverageCandidateEvidence> candidates,
            ISet<string> duplicateCandidateIds,
            ISet<string> duplicateMicrochunkIds,
            IReadOnlyDictionary<string, int> orientationCoverage,
            IReadOnlyDictionary<string, int> profileCoverage,
            ICollection<MoonpalaceBoundaryCoverageIssue> issues)
        {
            if (orientationCoverage["HORIZONTAL"] == 0)
            {
                issues.Add(OrientationIssue(expected, MoonpalaceBoundaryCoverageIssueCode.MissingOrientation,
                    MoonpalaceBoundaryOrientation.Horizontal, "Horizontal coverage is missing."));
            }
            if (orientationCoverage["VERTICAL"] == 0)
            {
                issues.Add(OrientationIssue(expected, MoonpalaceBoundaryCoverageIssueCode.MissingOrientation,
                    MoonpalaceBoundaryOrientation.Vertical, "Vertical coverage is missing."));
            }

            foreach (var profile in expected.AllowedProfileIds)
            {
                if (profileCoverage[profile] == 0)
                {
                    issues.Add(ProfileIssue(expected, MoonpalaceBoundaryCoverageIssueCode.MissingProfile, profile,
                        "Authored candidate coverage for this profile is missing."));
                }
            }

            foreach (var candidate in candidates)
            {
                ValidateCandidate(expected, candidate, duplicateCandidateIds, duplicateMicrochunkIds, issues);
            }

            foreach (var matrixKey in expected.ExpectedMatrix)
            {
                var count = candidates.Count(value =>
                    string.Equals(MoonpalaceBoundaryCoverageRequirement.MatrixKey(value.ProfileId, value.Orientation),
                        matrixKey, StringComparison.Ordinal));
                if (count == 0)
                {
                    var parts = matrixKey.Split('|');
                    var orientation = parts[1] == "HORIZONTAL"
                        ? MoonpalaceBoundaryOrientation.Horizontal
                        : MoonpalaceBoundaryOrientation.Vertical;
                    issues.Add(new MoonpalaceBoundaryCoverageIssue(
                        MoonpalaceBoundaryCoverageIssueCode.MissingCandidate,
                        expected.PairOrder,
                        expected.PairRuleId,
                        orientation,
                        expected.GetProfileOrder(parts[0]),
                        parts[0],
                        string.Empty,
                        string.Empty,
                        "Expected profile/orientation candidate is missing."));
                }
                else if (count > 1)
                {
                    var parts = matrixKey.Split('|');
                    var orientation = parts[1] == "HORIZONTAL"
                        ? MoonpalaceBoundaryOrientation.Horizontal
                        : MoonpalaceBoundaryOrientation.Vertical;
                    issues.Add(new MoonpalaceBoundaryCoverageIssue(
                        MoonpalaceBoundaryCoverageIssueCode.DuplicateCandidate,
                        expected.PairOrder,
                        expected.PairRuleId,
                        orientation,
                        expected.GetProfileOrder(parts[0]),
                        parts[0],
                        string.Empty,
                        string.Empty,
                        "Profile/orientation matrix contains duplicate candidates."));
                }
            }

            if (candidates.Count != expected.ExpectedCandidateCount)
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.MissingCandidate,
                    "Pair candidate total does not match the required matrix."));
            }
            var microchunkCount = candidates.Select(value => value.MicrochunkId)
                .Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).Count();
            if (microchunkCount != expected.ExpectedMicrochunkCount)
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.MissingMicrochunk,
                    "Pair backing microchunk total does not match."));
            }
            if (candidates.Sum(value => value.TileCells.Count) != expected.ExpectedTileRowCount)
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.InvalidTileCoverage,
                    "Pair tile row total does not match."));
            }
            if (candidates.Sum(value => value.Sockets.Count) != expected.ExpectedSocketRowCount)
            {
                issues.Add(PairIssue(expected, MoonpalaceBoundaryCoverageIssueCode.MissingSocket,
                    "Pair socket row total does not match."));
            }
        }

        private static void ValidateCandidate(
            MoonpalaceBoundaryCoverageRequirement expected,
            MoonpalaceBoundaryCoverageCandidateEvidence candidate,
            ISet<string> duplicateCandidateIds,
            ISet<string> duplicateMicrochunkIds,
            ICollection<MoonpalaceBoundaryCoverageIssue> issues)
        {
            var profileOrder = expected.GetProfileOrder(candidate.ProfileId);
            if (duplicateCandidateIds.Contains(candidate.CandidateId))
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.DuplicateCandidate, profileOrder,
                    "Candidate ID must be globally unique."));
            }
            if (string.IsNullOrEmpty(candidate.MicrochunkId))
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.MissingMicrochunk, profileOrder,
                    "Backing microchunk ID is missing."));
            }
            else if (duplicateMicrochunkIds.Contains(candidate.MicrochunkId))
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.DuplicateMicrochunk, profileOrder,
                    "Backing microchunk ID must be globally unique."));
            }

            if (!expected.AllowedProfileIds.Contains(candidate.ProfileId, StringComparer.Ordinal))
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.UnexpectedProfile, profileOrder,
                    "Candidate profile is not allowed for the pair."));
            }
            if (!expected.Allows(candidate.ProfileId, candidate.Orientation))
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.InvalidProfileOrientation, profileOrder,
                    "Candidate profile/orientation combination is invalid."));
            }

            var expectedSignature = candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                ? HorizontalEdgeSignatureId
                : candidate.Orientation == MoonpalaceBoundaryOrientation.Vertical
                    ? VerticalEdgeSignatureId
                    : string.Empty;
            if (string.IsNullOrEmpty(candidate.CandidateId) || !candidate.Active || candidate.Weight <= 0 ||
                !candidate.Reversible || candidate.RouteType != 1 || !candidate.MandatoryAllowed ||
                !string.Equals(candidate.BiomeAId, expected.BiomeAId, StringComparison.Ordinal) ||
                !string.Equals(candidate.BiomeBId, expected.BiomeBId, StringComparison.Ordinal) ||
                !string.Equals(candidate.EntryEdgeSignatureId, expectedSignature, StringComparison.Ordinal) ||
                !string.Equals(candidate.ExitEdgeSignatureId, expectedSignature, StringComparison.Ordinal))
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.InvalidCandidate, profileOrder,
                    "Candidate identity, pair, weight, reversible, route, or edge signature is invalid."));
            }
            if (!string.Equals(candidate.ToolRequirement, "NONE", StringComparison.Ordinal))
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.ToolRequired, profileOrder,
                    "Mandatory boundary candidate requires a tool."));
            }

            var expectedBiomeIds = expected.BiomeAId + "|" + expected.BiomeBId;
            if (candidate.WidthTiles != 12 || candidate.HeightTiles != 8 ||
                !string.Equals(candidate.UsageClass, "BOUNDARY", StringComparison.Ordinal) ||
                !string.Equals(candidate.MicrochunkBiomeIds, expectedBiomeIds, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(candidate.RouteRoles) || !candidate.RouteRoles.Contains("BOUNDARY") ||
                !candidate.TileDataComplete || !candidate.MicrochunkActive)
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.MissingMicrochunk, profileOrder,
                    "Backing microchunk contract is invalid."));
            }

            var tileCoverageValid = candidate.TileCells.Count == 96 &&
                                    candidate.TileCells.All(value => value.LocalX >= 0 && value.LocalX < 12 &&
                                                                     value.LocalY >= 0 && value.LocalY < 8) &&
                                    candidate.TileCells.Select(value => value.CoordinateKey)
                                        .Distinct().Count() == 96;
            if (!tileCoverageValid)
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.InvalidTileCoverage, profileOrder,
                    "Backing microchunk must contain 96 unique in-range local cells."));
            }

            GetBiomeEvidence(expected.BiomeAId, out var foregroundA, out var backgroundA);
            GetBiomeEvidence(expected.BiomeBId, out var foregroundB, out var backgroundB);
            var warningCategories = candidate.WarningMarkerCategoryCount(
                foregroundA, foregroundB, backgroundA, backgroundB);
            var routeEvidence = candidate.TileCells.Any(value => value.MarkerCode == "M_ROUTE_MAIN");
            var socketEvidence = candidate.TileCells.Any(value => value.MarkerCode == "M_SOCKET");
            if (warningCategories < 2 || !routeEvidence || !socketEvidence)
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.MissingWarningEvidence, profileOrder,
                    "Both biome tile/background evidence and route/socket markers are required."));
            }

            ValidateSockets(expected, candidate, profileOrder, expectedSignature, issues);
        }

        private static void ValidateSockets(
            MoonpalaceBoundaryCoverageRequirement expected,
            MoonpalaceBoundaryCoverageCandidateEvidence candidate,
            int profileOrder,
            string expectedSignature,
            ICollection<MoonpalaceBoundaryCoverageIssue> issues)
        {
            if (candidate.Sockets.Count < 2)
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.MissingSocket, profileOrder,
                    "Exactly two boundary sockets are required."));
            }

            var expectedSides = candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                ? new[] { "L", "R" }
                : new[] { "D", "U" };
            var expectedTraversal = candidate.Orientation == MoonpalaceBoundaryOrientation.Horizontal
                ? "WALK"
                : "CLIMB";
            var socketShapeValid = candidate.Sockets.Count == 2 &&
                                   candidate.Sockets.Select(value => value.SocketId)
                                       .Distinct(StringComparer.Ordinal).Count() == 2 &&
                                   candidate.Sockets.Select(value => value.Side)
                                       .OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(expectedSides) &&
                                   candidate.Sockets.All(value =>
                                       string.Equals(value.TraversalKind, expectedTraversal, StringComparison.Ordinal) &&
                                       value.MandatoryAllowed &&
                                       string.Equals(value.EdgeSignatureId, expectedSignature, StringComparison.Ordinal) &&
                                       string.Equals(value.RouteLayer, "MANDATORY", StringComparison.Ordinal) &&
                                       value.MinimumSafeTiles >= 2);
            if (!socketShapeValid)
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.InvalidSocket, profileOrder,
                    "Socket side, traversal, signature, route layer, or safety contract is invalid."));
            }
            if (candidate.Sockets.Any(value =>
                    !string.Equals(value.ToolRequirement, "NONE", StringComparison.Ordinal)))
            {
                issues.Add(CandidateIssue(expected, candidate,
                    MoonpalaceBoundaryCoverageIssueCode.ToolRequired, profileOrder,
                    "Mandatory boundary socket requires a tool."));
            }
        }

        private static void GetBiomeEvidence(string biomeId, out string foreground, out string background)
        {
            switch (biomeId)
            {
                case "BIO_MOON_CRATER":
                    foreground = "G_MOON_ROCK";
                    background = "DB_CRATER";
                    return;
                case "BIO_CASSIA_ROOT":
                    foreground = "G_CASSIA_WOOD";
                    background = "DB_ROOT";
                    return;
                case "BIO_ABANDONED_MILL":
                    foreground = "G_MILL_METAL";
                    background = "DB_MILL";
                    return;
                case "BIO_MOON_DOUGH":
                    foreground = "G_DOUGH_SOLID";
                    background = "DB_DOUGH";
                    return;
                default:
                    foreground = string.Empty;
                    background = string.Empty;
                    return;
            }
        }

        private static MoonpalaceBoundaryCoverageIssue GlobalIssue(
            MoonpalaceBoundaryCoverageIssueCode code,
            string message)
        {
            return new MoonpalaceBoundaryCoverageIssue(
                code, int.MaxValue, string.Empty, InvalidOrientation, int.MaxValue,
                string.Empty, string.Empty, string.Empty, message);
        }

        private static MoonpalaceBoundaryCoverageIssue PairIssue(
            MoonpalaceBoundaryCoverageRequirement requirement,
            MoonpalaceBoundaryCoverageIssueCode code,
            string message)
        {
            return new MoonpalaceBoundaryCoverageIssue(
                code, requirement.PairOrder, requirement.PairRuleId, InvalidOrientation, int.MaxValue,
                string.Empty, string.Empty, string.Empty, message);
        }

        private static MoonpalaceBoundaryCoverageIssue OrientationIssue(
            MoonpalaceBoundaryCoverageRequirement requirement,
            MoonpalaceBoundaryCoverageIssueCode code,
            MoonpalaceBoundaryOrientation orientation,
            string message)
        {
            return new MoonpalaceBoundaryCoverageIssue(
                code, requirement.PairOrder, requirement.PairRuleId, orientation, int.MaxValue,
                string.Empty, string.Empty, string.Empty, message);
        }

        private static MoonpalaceBoundaryCoverageIssue ProfileIssue(
            MoonpalaceBoundaryCoverageRequirement requirement,
            MoonpalaceBoundaryCoverageIssueCode code,
            string profileId,
            string message)
        {
            return new MoonpalaceBoundaryCoverageIssue(
                code, requirement.PairOrder, requirement.PairRuleId, InvalidOrientation,
                requirement.GetProfileOrder(profileId), profileId, string.Empty, string.Empty, message);
        }

        private static MoonpalaceBoundaryCoverageIssue CandidateIssue(
            MoonpalaceBoundaryCoverageRequirement requirement,
            MoonpalaceBoundaryCoverageCandidateEvidence candidate,
            MoonpalaceBoundaryCoverageIssueCode code,
            int profileOrder,
            string message)
        {
            return new MoonpalaceBoundaryCoverageIssue(
                code, requirement.PairOrder, requirement.PairRuleId, candidate.Orientation,
                profileOrder, candidate.ProfileId, candidate.CandidateId, candidate.MicrochunkId, message);
        }

        private static MoonpalaceBoundaryOrientation InvalidOrientation =>
            (MoonpalaceBoundaryOrientation)(-1);
    }
}
