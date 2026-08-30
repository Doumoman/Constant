using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public sealed class SectorPlannerGrayboxFixture : IComparable<SectorPlannerGrayboxFixture>
    {
        private readonly ReadOnlyCollection<SectorCoord> neighbors;
        private readonly ReadOnlyCollection<string> coverageTags;
        private readonly ReadOnlyCollection<string> sourceTaskIds;
        private readonly ReadOnlyCollection<string> sourceDigests;
        private readonly ReadOnlyCollection<string> boundaryIdentities;
        private readonly ReadOnlyCollection<string> specialRegionIdentities;

        internal SectorPlannerGrayboxFixture(
            string fixtureId,
            SectorPlannerGrayboxFixtureKind kind,
            SectorCoord centerSector,
            IEnumerable<SectorCoord> sourceNeighbors,
            IEnumerable<string> sourceCoverageTags,
            IEnumerable<string> taskIds,
            IEnumerable<string> digests,
            int expectedRouteType,
            string expectedAccessClass,
            string expectedPacingRole,
            string expectedBiomeId,
            IEnumerable<string> sourceBoundaryIdentities,
            IEnumerable<string> sourceSpecialRegionIdentities,
            string expectedOwnershipPlaneSummary,
            string expectedRetrySummary,
            string debugExportDigest)
        {
            FixtureId = fixtureId ?? string.Empty;
            Kind = kind;
            CenterSector = centerSector;
            neighbors = new ReadOnlyCollection<SectorCoord>((sourceNeighbors ?? Array.Empty<SectorCoord>())
                .Distinct().OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
            coverageTags = Copy(sourceCoverageTags);
            sourceTaskIds = Copy(taskIds);
            sourceDigests = Copy(digests);
            ExpectedRouteType = expectedRouteType;
            ExpectedAccessClass = expectedAccessClass ?? string.Empty;
            ExpectedPacingRole = expectedPacingRole ?? string.Empty;
            ExpectedBiomeId = expectedBiomeId ?? string.Empty;
            boundaryIdentities = Copy(sourceBoundaryIdentities);
            specialRegionIdentities = Copy(sourceSpecialRegionIdentities);
            ExpectedOwnershipPlaneSummary = expectedOwnershipPlaneSummary ?? string.Empty;
            ExpectedRetrySummary = expectedRetrySummary ?? string.Empty;
            DebugExportDigest = debugExportDigest ?? string.Empty;
            CanonicalDigest = SectorPlannerDebugCanonicalDigest.Hash(Material(this));
        }

        public string FixtureId { get; }
        public SectorPlannerGrayboxFixtureKind Kind { get; }
        public SectorCoord CenterSector { get; }
        public IReadOnlyList<SectorCoord> NeighborSectors => neighbors;
        public IReadOnlyList<string> CoverageTags => coverageTags;
        public IReadOnlyList<string> SourceTaskIds => sourceTaskIds;
        public IReadOnlyList<string> SourceDigests => sourceDigests;
        public int ExpectedRouteType { get; }
        public string ExpectedAccessClass { get; }
        public string ExpectedPacingRole { get; }
        public string ExpectedBiomeId { get; }
        public IReadOnlyList<string> ExpectedBoundaryIdentities => boundaryIdentities;
        public IReadOnlyList<string> ExpectedSpecialRegionIdentities => specialRegionIdentities;
        public string ExpectedOwnershipPlaneSummary { get; }
        public string ExpectedRetrySummary { get; }
        public string DebugExportDigest { get; }
        public string CanonicalDigest { get; }
        public bool UsesPrivateData => false;
        public int SceneAssetCount => 0;
        public int PrefabAssetCount => 0;
        public int GameObjectCount => 0;
        public int TilemapCount => 0;

        public int CompareTo(SectorPlannerGrayboxFixture other)
        {
            if (other == null) return -1;
            return string.Compare(FixtureId, other.FixtureId, StringComparison.Ordinal);
        }

        private static ReadOnlyCollection<string> Copy(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());

        private static string Material(SectorPlannerGrayboxFixture value) => string.Join("\n", new[]
        {
            value.FixtureId, value.Kind.ToString(), value.CenterSector.X.ToString(CultureInfo.InvariantCulture),
            value.CenterSector.Y.ToString(CultureInfo.InvariantCulture),
            string.Join(";", value.NeighborSectors.Select(item => item.X + "," + item.Y)),
            string.Join(";", value.CoverageTags), string.Join(";", value.SourceTaskIds), string.Join(";", value.SourceDigests),
            value.ExpectedRouteType.ToString(CultureInfo.InvariantCulture), value.ExpectedAccessClass, value.ExpectedPacingRole,
            value.ExpectedBiomeId, string.Join(";", value.ExpectedBoundaryIdentities), string.Join(";", value.ExpectedSpecialRegionIdentities),
            value.ExpectedOwnershipPlaneSummary, value.ExpectedRetrySummary, value.DebugExportDigest,
        });
    }

    public sealed class SectorPlannerGrayboxCoverageAudit
    {
        private readonly ReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> required;
        private readonly ReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> covered;
        private readonly ReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> missing;
        private readonly ReadOnlyDictionary<string, IReadOnlyList<string>> coveredByFixtureKind;
        private readonly ReadOnlyCollection<string> zeroCountEvidence;

        internal SectorPlannerGrayboxCoverageAudit(
            IDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> sourceRequired,
            IDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> sourceCovered,
            IDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> sourceMissing,
            IDictionary<string, IReadOnlyList<string>> sourceCoveredByFixtureKind,
            IEnumerable<string> sourceZeroCountEvidence,
            int oneSectorFixtureCount,
            int threeSectorFixtureCount,
            int failureRingFixtureCount,
            SectorPlannerDebugSection coverageSection)
        {
            required = Copy(sourceRequired);
            covered = Copy(sourceCovered);
            missing = Copy(sourceMissing);
            var fixtureCoverage = new SortedDictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (var pair in sourceCoveredByFixtureKind ?? new Dictionary<string, IReadOnlyList<string>>())
            {
                fixtureCoverage[pair.Key] = new ReadOnlyCollection<string>((pair.Value ?? Array.Empty<string>())
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            }
            coveredByFixtureKind = new ReadOnlyDictionary<string, IReadOnlyList<string>>(fixtureCoverage);
            zeroCountEvidence = new ReadOnlyCollection<string>((sourceZeroCountEvidence ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            OneSectorFixtureCount = oneSectorFixtureCount;
            ThreeSectorFixtureCount = threeSectorFixtureCount;
            FailureRingFixtureCount = failureRingFixtureCount;
            CoverageSection = coverageSection;
            CanonicalDigest = SectorPlannerDebugCanonicalDigest.Hash(string.Join("\n", new[]
            {
                DictionaryMaterial(required), DictionaryMaterial(covered), DictionaryMaterial(missing),
                string.Join("\n", coveredByFixtureKind.Select(value => value.Key + "=" + string.Join(",", value.Value))),
                string.Join("\n", zeroCountEvidence), OneSectorFixtureCount.ToString(CultureInfo.InvariantCulture),
                ThreeSectorFixtureCount.ToString(CultureInfo.InvariantCulture), FailureRingFixtureCount.ToString(CultureInfo.InvariantCulture),
                coverageSection.CanonicalDigest,
            }));
        }

        public IReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> Required => required;
        public IReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> Covered => covered;
        public IReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> Missing => missing;
        public IReadOnlyList<string> ZeroCountEvidence => zeroCountEvidence;
        public int OneSectorFixtureCount { get; }
        public int ThreeSectorFixtureCount { get; }
        public int FailureRingFixtureCount { get; }
        public SectorPlannerDebugSection CoverageSection { get; }
        public string CanonicalDigest { get; }
        public int TotalMissingCount => missing.Values.Sum(value => value.Count);

        public IReadOnlyList<string> RequiredFor(SectorPlannerGrayboxCoverageKind kind) => required[kind];
        public IReadOnlyList<string> CoveredFor(SectorPlannerGrayboxCoverageKind kind) => covered[kind];
        public IReadOnlyList<string> MissingFor(SectorPlannerGrayboxCoverageKind kind) => missing[kind];
        public IReadOnlyList<string> CoveredIn(SectorPlannerGrayboxCoverageKind kind, SectorPlannerGrayboxFixtureKind fixtureKind)
        {
            var key = kind + "|" + fixtureKind;
            return coveredByFixtureKind.TryGetValue(key, out var values) ? values : Array.Empty<string>();
        }

        private static ReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> Copy(
            IDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> source)
        {
            var result = new SortedDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>>();
            foreach (SectorPlannerGrayboxCoverageKind kind in Enum.GetValues(typeof(SectorPlannerGrayboxCoverageKind)))
            {
                IReadOnlyList<string> values = null;
                if (source != null) source.TryGetValue(kind, out values);
                result[kind] = new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            }
            return new ReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>>(result);
        }

        private static string DictionaryMaterial(IEnumerable<KeyValuePair<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>>> values) =>
            string.Join("\n", values.Select(value => value.Key + "=" + string.Join(",", value.Value)));
    }

    public static class SectorPlannerGrayboxFixtureCatalogBuilder
    {
        private static readonly string[] TaskIds =
        {
            "MAP14_01", "MAP14_02", "MAP14_03", "MAP14_04", "MAP14_05",
            "MAP14_06", "MAP14_07", "MAP14_08", "MAP14_09",
        };

        public static SectorPlannerDebugExportResult Build(
            SectorPlannerDebugExportRequest request,
            SectorPlannerDebugExport debugExport,
            SectorPlannerFailureRingSnapshot failureRing,
            IEnumerable<SectorPlannerSectorSnapshot> sourceCoverageSectors = null)
        {
            var errors = new List<SectorPlannerDebugExportError>();
            if (request == null)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingInput, "request", "A debug export request is required.");
            if (debugExport == null)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingInput, "debugExport", "A successful in-memory debug export is required.");
            if (failureRing == null)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.FailureRingFixtureMissing, "failureRing", "A failure 1-ring snapshot is required.");
            var retry = request?.RetryPlan;
            var ownership = retry?.Request?.OwnershipPlan;
            var input = ownership?.Request?.Input;
            if (retry == null) SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingRetryPlan, "retryPlan", "MAP14_08 retry plan is required.");
            if (ownership == null && retry != null) SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingOwnershipPlan, "ownershipPlan", "MAP14_07 ownership plan is required.");
            if (input == null && ownership != null) SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.MissingPlannerInput, "plannerInput", "MAP14_01 planner input is required.");
            if (errors.Count != 0) return SectorPlannerDebugExporter.Failed(errors);

            var sectors = (sourceCoverageSectors ?? input.Sectors).Where(value => value != null)
                .OrderBy(value => value.SectorIndex).ToArray();
            foreach (var duplicate in sectors.GroupBy(value => value.SectorIndex).Where(value => value.Count() > 1))
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.DuplicateFixtureId,
                    duplicate.Key.ToString(CultureInfo.InvariantCulture), "Coverage sector indices must be unique.");
            foreach (var sector in sectors.Where(value => !WorldInside(value.Coordinate)))
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.FixtureOutOfBounds,
                    sector.SectorIndex.ToString(CultureInfo.InvariantCulture), "Fixture sectors must remain inside the 13x13 world.");
            if (errors.Count != 0) return SectorPlannerDebugExporter.Failed(errors);

            var assignments = ownership.Request.Assignments.ToDictionary(value => value.Coordinate);
            var ownershipSummary = string.Join(";", Enum.GetValues(typeof(SectorCanvasOwnershipPlane)).Cast<SectorCanvasOwnershipPlane>()
                .Select(value => value + "=" + ownership.CountOwned(value).ToString(CultureInfo.InvariantCulture)));
            var retrySummary = "terminal=" + retry.TerminalDecision + ";" + string.Join(";",
                Enum.GetValues(typeof(SectorPlannerRetryStage)).Cast<SectorPlannerRetryStage>()
                    .Select(value => value + "=" + retry.Count(value).ToString(CultureInfo.InvariantCulture)));
            var digests = SourceDigests(ownership, retry, debugExport);
            var globalTags = OwnershipTags(ownership).Concat(RetryTags(retry)).ToArray();
            var fixtures = new List<SectorPlannerGrayboxFixture>();
            foreach (var sector in sectors)
            {
                assignments.TryGetValue(sector.Coordinate, out var assignment);
                var localTags = CoverageTags(sector, assignment).Concat(globalTags).ToArray();
                fixtures.Add(Fixture("ONE", SectorPlannerGrayboxFixtureKind.OneSector, sector,
                    Array.Empty<SectorCoord>(), localTags, assignment, ownershipSummary, retrySummary, digests, debugExport.CanonicalDigest));
                var adjacent = sectors.Where(value => value.SectorIndex != sector.SectorIndex && MooreAdjacent(sector.Coordinate, value.Coordinate))
                    .OrderBy(value => Distance(value.Coordinate, sector.Coordinate)).ThenBy(value => value.SectorIndex).Take(2).ToArray();
                if (adjacent.Length != 2)
                {
                    SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.ThreeSectorAdjacencyBroken,
                        sector.SectorIndex.ToString(CultureInfo.InvariantCulture), "Each ThreeSector fixture requires two available Moore-adjacent public sectors.");
                }
                else
                {
                    fixtures.Add(Fixture("THREE", SectorPlannerGrayboxFixtureKind.ThreeSector, sector,
                        adjacent.Select(value => value.Coordinate), localTags, assignment, ownershipSummary, retrySummary, digests, debugExport.CanonicalDigest));
                }
            }
            if (failureRing != null)
            {
                var center = sectors.FirstOrDefault(value => value.Coordinate == failureRing.CenterSector.SectorCoordinate);
                if (center == null)
                    SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.RingCenterMissing, "failureFixture", "Failure-ring center must exist in graybox coverage sectors.");
                else
                {
                    assignments.TryGetValue(center.Coordinate, out var assignment);
                    fixtures.Add(Fixture("FAILURE", SectorPlannerGrayboxFixtureKind.FailureOneRing, center,
                        failureRing.RingSectors.Select(value => value.SectorCoordinate),
                        CoverageTags(center, assignment).Concat(globalTags), assignment,
                        ownershipSummary, retrySummary, digests, debugExport.CanonicalDigest));
                }
            }
            foreach (var duplicate in fixtures.GroupBy(value => value.FixtureId, StringComparer.Ordinal).Where(value => value.Count() > 1))
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.DuplicateFixtureId, duplicate.Key, "Fixture IDs must be unique.");
            if (errors.Count != 0) return SectorPlannerDebugExporter.Failed(errors);

            var required = RequiredCoverage(sectors, assignments.Values, ownership, retry);
            var covered = new Dictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>>();
            var missing = new Dictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>>();
            var coveredByFixture = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (SectorPlannerGrayboxCoverageKind kind in Enum.GetValues(typeof(SectorPlannerGrayboxCoverageKind)))
            {
                var prefix = kind + ":";
                var values = fixtures.SelectMany(value => value.CoverageTags).Where(value => value.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(value => value.Substring(prefix.Length)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                covered[kind] = values;
                missing[kind] = required[kind].Except(values, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                foreach (SectorPlannerGrayboxFixtureKind fixtureKind in Enum.GetValues(typeof(SectorPlannerGrayboxFixtureKind)))
                {
                    coveredByFixture[kind + "|" + fixtureKind] = fixtures.Where(value => value.Kind == fixtureKind)
                        .SelectMany(value => value.CoverageTags).Where(value => value.StartsWith(prefix, StringComparison.Ordinal))
                        .Select(value => value.Substring(prefix.Length)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                }
            }

            ValidateCoverage(required, coveredByFixture, missing, errors);
            if (fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.OneSector) == 0)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.OneSectorFixtureMissing, "OneSector", "At least one OneSector fixture is required.");
            if (fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.ThreeSector) == 0)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.ThreeSectorFixtureMissing, "ThreeSector", "At least one ThreeSector fixture is required.");
            if (fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.FailureOneRing) == 0)
                SectorPlannerDebugExporter.Add(errors, SectorPlannerDebugExportErrorCode.FailureRingFixtureMissing, "FailureOneRing", "Exactly one FailureOneRing fixture is required.");
            if (errors.Count != 0) return SectorPlannerDebugExporter.Failed(errors);

            var zeroEvidence = Enum.GetValues(typeof(SectorPlannerRetryStage)).Cast<SectorPlannerRetryStage>()
                .Where(value => retry.Count(value) == 0).Select(value => "RETRY_STAGE_" + value + "=0").ToArray();
            var facts = new List<SectorPlannerDebugFact>();
            foreach (SectorPlannerGrayboxCoverageKind kind in Enum.GetValues(typeof(SectorPlannerGrayboxCoverageKind)))
            {
                facts.Add(new SectorPlannerDebugFact(kind + "_REQUIRED", required[kind].Count.ToString(CultureInfo.InvariantCulture)));
                facts.Add(new SectorPlannerDebugFact(kind + "_COVERED", covered[kind].Count.ToString(CultureInfo.InvariantCulture)));
                facts.Add(new SectorPlannerDebugFact(kind + "_MISSING", missing[kind].Count.ToString(CultureInfo.InvariantCulture)));
            }
            facts.Add(new SectorPlannerDebugFact("ONE_SECTOR_FIXTURES", fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.OneSector).ToString(CultureInfo.InvariantCulture)));
            facts.Add(new SectorPlannerDebugFact("THREE_SECTOR_FIXTURES", fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.ThreeSector).ToString(CultureInfo.InvariantCulture)));
            facts.Add(new SectorPlannerDebugFact("FAILURE_RING_FIXTURES", fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.FailureOneRing).ToString(CultureInfo.InvariantCulture)));
            var section = SectorPlannerDebugExporter.Section("MAP14_09_GRAYBOX_COVERAGE",
                SectorPlannerDebugSectionKind.GrayboxCoverage, "MAP14_09", debugExport.CanonicalDigest,
                "One-sector, three-sector, and failure-ring descriptor coverage.", facts, Array.Empty<SectorPlannerDebugToken>());
            var audit = new SectorPlannerGrayboxCoverageAudit(required, covered, missing, coveredByFixture, zeroEvidence,
                fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.OneSector),
                fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.ThreeSector),
                fixtures.Count(value => value.Kind == SectorPlannerGrayboxFixtureKind.FailureOneRing), section);
            return new SectorPlannerDebugExportResult(debugExport, failureRing, fixtures, audit, errors);
        }

        private static SectorPlannerGrayboxFixture Fixture(
            string prefix,
            SectorPlannerGrayboxFixtureKind kind,
            SectorPlannerSectorSnapshot sector,
            IEnumerable<SectorCoord> neighbors,
            IEnumerable<string> coverageTags,
            SectorPacingAssignment assignment,
            string ownershipSummary,
            string retrySummary,
            IEnumerable<string> digests,
            string debugDigest)
        {
            var id = "MAP14_09_" + prefix + "_" + sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture);
            var boundary = sector.Boundaries.Select(value => value.PairId + ":" + value.CandidateId);
            var special = SpecialIdentities(sector);
            return new SectorPlannerGrayboxFixture(id, kind, sector.Coordinate, neighbors, coverageTags,
                TaskIds, digests, sector.Route.RouteType, sector.Route.AccessClass.ToString(),
                assignment == null ? string.Empty : assignment.PrimaryRole.ToString(), sector.Biome.BiomeId,
                boundary, special, ownershipSummary, retrySummary, debugDigest);
        }

        private static IReadOnlyList<string> SourceDigests(
            SectorCanvasOwnershipPlan ownership, SectorPlannerRetryPlan retry, SectorPlannerDebugExport export) => new[]
        {
            ownership.PlannerInputDigestBefore, ownership.PacingAssignmentDigestBefore, ownership.FixedAnchorPlanDigestBefore,
            ownership.ClusterPlacementPlanDigestBefore, ownership.SpineEnvelopePlanDigestBefore,
            ownership.RolePatternPlanDigestBefore, ownership.PatternRenderPlanDigestBefore,
            ownership.QuietActivityEventPlanDigestBefore, ownership.CanonicalDigest, retry.CanonicalDigest, export.CanonicalDigest,
        };

        private static IEnumerable<string> CoverageTags(SectorPlannerSectorSnapshot sector, SectorPacingAssignment assignment)
        {
            yield return Tag(SectorPlannerGrayboxCoverageKind.RouteType, "TYPE_" + sector.Route.RouteType.ToString(CultureInfo.InvariantCulture));
            if (sector.Boundaries.Count != 0) yield return Tag(SectorPlannerGrayboxCoverageKind.RouteType, "BOUNDARY");
            if (sector.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.None || sector.OptionalRegions.Count != 0)
                yield return Tag(SectorPlannerGrayboxCoverageKind.RouteType, "SPECIAL");
            yield return Tag(SectorPlannerGrayboxCoverageKind.Biome, sector.Biome.BiomeId);
            foreach (var boundary in sector.Boundaries) yield return Tag(SectorPlannerGrayboxCoverageKind.BoundaryPair, boundary.PairId);
            foreach (var special in SpecialIdentities(sector)) yield return Tag(SectorPlannerGrayboxCoverageKind.SpecialRegion, SpecialKindToken(special));
            if (assignment != null) yield return Tag(SectorPlannerGrayboxCoverageKind.PacingRole, assignment.PrimaryRole.ToString());
            yield return Tag(SectorPlannerGrayboxCoverageKind.AccessClass, sector.Route.AccessClass.ToString());
        }

        private static IEnumerable<string> OwnershipTags(SectorCanvasOwnershipPlan ownership) =>
            Enum.GetValues(typeof(SectorCanvasOwnershipPlane)).Cast<SectorCanvasOwnershipPlane>()
                .Select(value => Tag(SectorPlannerGrayboxCoverageKind.OwnershipPlane,
                    value + "=" + ownership.CountOwned(value).ToString(CultureInfo.InvariantCulture)));

        private static IEnumerable<string> RetryTags(SectorPlannerRetryPlan retry)
        {
            foreach (SectorPlannerRetryStage stage in Enum.GetValues(typeof(SectorPlannerRetryStage)))
                yield return Tag(SectorPlannerGrayboxCoverageKind.RetryStage,
                    "STAGE_" + stage + "=" + retry.Count(stage).ToString(CultureInfo.InvariantCulture));
            yield return Tag(SectorPlannerGrayboxCoverageKind.RetryStage, "TERMINAL_" + retry.TerminalDecision);
        }

        private static Dictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> RequiredCoverage(
            IEnumerable<SectorPlannerSectorSnapshot> sectors,
            IEnumerable<SectorPacingAssignment> assignments,
            SectorCanvasOwnershipPlan ownership,
            SectorPlannerRetryPlan retry)
        {
            var values = sectors.ToArray();
            var required = new Dictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>>
            {
                { SectorPlannerGrayboxCoverageKind.RouteType, new[] { "TYPE_0", "TYPE_1", "TYPE_2", "TYPE_3", "TYPE_4", "BOUNDARY", "SPECIAL" } },
                { SectorPlannerGrayboxCoverageKind.Biome, values.Select(value => value.Biome.BiomeId).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray() },
                { SectorPlannerGrayboxCoverageKind.BoundaryPair, MoonpalaceBoundaryCoverageRequirement.Canonical.Select(value => value.PairRuleId).OrderBy(value => value, StringComparer.Ordinal).ToArray() },
                { SectorPlannerGrayboxCoverageKind.SpecialRegion, new[] { "Village", "CoreResource", "Forge", "Boss", "Merchant", "Maru" } },
                { SectorPlannerGrayboxCoverageKind.PacingRole, assignments.Select(value => value.PrimaryRole.ToString()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray() },
                { SectorPlannerGrayboxCoverageKind.AccessClass, values.Select(value => value.Route.AccessClass.ToString()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray() },
                { SectorPlannerGrayboxCoverageKind.OwnershipPlane, Enum.GetValues(typeof(SectorCanvasOwnershipPlane)).Cast<SectorCanvasOwnershipPlane>().Select(value => value + "=" + ownership.CountOwned(value).ToString(CultureInfo.InvariantCulture)).ToArray() },
                { SectorPlannerGrayboxCoverageKind.RetryStage, Enum.GetValues(typeof(SectorPlannerRetryStage)).Cast<SectorPlannerRetryStage>().Select(value => "STAGE_" + value + "=" + retry.Count(value).ToString(CultureInfo.InvariantCulture)).Concat(new[] { "TERMINAL_" + retry.TerminalDecision }).ToArray() },
            };
            return required;
        }

        private static void ValidateCoverage(
            IReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> required,
            IReadOnlyDictionary<string, IReadOnlyList<string>> coveredByFixture,
            IReadOnlyDictionary<SectorPlannerGrayboxCoverageKind, IReadOnlyList<string>> missing,
            ICollection<SectorPlannerDebugExportError> errors)
        {
            var oneAndThree = new[]
            {
                SectorPlannerGrayboxCoverageKind.RouteType, SectorPlannerGrayboxCoverageKind.Biome,
                SectorPlannerGrayboxCoverageKind.BoundaryPair, SectorPlannerGrayboxCoverageKind.SpecialRegion,
                SectorPlannerGrayboxCoverageKind.PacingRole, SectorPlannerGrayboxCoverageKind.AccessClass,
            };
            foreach (var kind in oneAndThree)
            {
                var one = coveredByFixture[kind + "|" + SectorPlannerGrayboxFixtureKind.OneSector];
                var three = coveredByFixture[kind + "|" + SectorPlannerGrayboxFixtureKind.ThreeSector];
                foreach (var value in required[kind].Where(value => !one.Contains(value, StringComparer.Ordinal) || !three.Contains(value, StringComparer.Ordinal)))
                    AddCoverageError(kind, value, errors);
            }
            foreach (var pair in missing.Where(value => value.Value.Count != 0))
                foreach (var value in pair.Value) AddCoverageError(pair.Key, value, errors);
        }

        private static void AddCoverageError(
            SectorPlannerGrayboxCoverageKind kind, string value, ICollection<SectorPlannerDebugExportError> errors)
        {
            var code = kind == SectorPlannerGrayboxCoverageKind.RouteType ? SectorPlannerDebugExportErrorCode.CoverageMissingRouteType :
                kind == SectorPlannerGrayboxCoverageKind.Biome ? SectorPlannerDebugExportErrorCode.CoverageMissingBiome :
                kind == SectorPlannerGrayboxCoverageKind.BoundaryPair ? SectorPlannerDebugExportErrorCode.CoverageMissingBoundaryPair :
                kind == SectorPlannerGrayboxCoverageKind.SpecialRegion ? SectorPlannerDebugExportErrorCode.CoverageMissingSpecialRegion :
                kind == SectorPlannerGrayboxCoverageKind.OwnershipPlane ? SectorPlannerDebugExportErrorCode.CoverageMissingOwnershipPlane :
                kind == SectorPlannerGrayboxCoverageKind.RetryStage ? SectorPlannerDebugExportErrorCode.CoverageMissingRetryStage :
                SectorPlannerDebugExportErrorCode.MissingInput;
            SectorPlannerDebugExporter.Add(errors, code, kind + ":" + value, "Required public graybox coverage is missing.");
        }

        private static IEnumerable<string> SpecialIdentities(SectorPlannerSectorSnapshot sector)
        {
            if (sector.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.None)
                yield return sector.SpecialRegion.Kind + ":" + sector.SpecialRegion.Binding + ":" + sector.SpecialRegion.RegionId;
            foreach (var value in sector.OptionalRegions)
                yield return value.Kind + ":DeferredOptionalLocal:" + value.RegionId;
        }

        private static string SpecialKindToken(string identity)
        {
            var index = identity.IndexOf(':');
            return index < 0 ? identity : identity.Substring(0, index);
        }

        private static bool MooreAdjacent(SectorCoord left, SectorCoord right)
        {
            var dx = Math.Abs(left.X - right.X);
            var dy = Math.Abs(left.Y - right.Y);
            return dx <= 1 && dy <= 1 && (dx != 0 || dy != 0);
        }

        private static int Distance(SectorCoord value, SectorCoord center) =>
            Math.Abs(value.X - center.X) + Math.Abs(value.Y - center.Y);

        private static bool WorldInside(SectorCoord value) => value.X >= 0 && value.X < 13 && value.Y >= 0 && value.Y < 13;
        private static string Tag(SectorPlannerGrayboxCoverageKind kind, string value) => kind + ":" + (value ?? string.Empty);
    }
}
