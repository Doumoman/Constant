using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class SiteReservationBacktrackerTests
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";
        private const string CraterBiomeId = "BIO_MOON_CRATER";
        private const string CassiaBiomeId = "BIO_CASSIA_ROOT";
        private const string MillBiomeId = "BIO_ABANDONED_MILL";
        private const string DoughBiomeId = "BIO_MOON_DOUGH";
        private const ulong KnownSiteInitialState = 0x60D4B46EBF6EF00DUL;

        private static readonly FileSpec[] BiomeSpecs = CreateBiomeSpecs();
        private static readonly FileSpec[] SpecialSpecs = CreateSpecialSpecs();
        private static readonly StarterData Starter = BuildStarterData();

        public static IEnumerable ValidLimitCases()
        {
            for (var value = 1; value <= 200; value++)
                yield return new TestCaseData(value).SetName("Limits_Valid_" + value);
        }

        public static IEnumerable SearchErrorCodeCases()
        {
            foreach (SiteReservationSearchErrorCode value in
                     Enum.GetValues(typeof(SiteReservationSearchErrorCode)))
                yield return new TestCaseData(value, (int)value);
        }

        public static IEnumerable RejectionReasonCases()
        {
            foreach (SiteReservationRejectionReason value in
                     Enum.GetValues(typeof(SiteReservationRejectionReason)))
                yield return new TestCaseData(value, (int)value);
        }

        public static IEnumerable FullStarterSeedCases()
        {
            yield return new TestCaseData(0UL).SetName("FullStarter_Seed_0");
            yield return new TestCaseData(4660UL).SetName("FullStarter_Seed_4660");
            yield return new TestCaseData(ulong.MaxValue).SetName("FullStarter_Seed_Max");
        }

        [TestCaseSource(nameof(ValidLimitCases))]
        public void Limits_AcceptsEveryProductionValue(int value)
        {
            Assert.That(new SiteReservationSearchLimits(value).MaxFailedCombinations,
                Is.EqualTo(value));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(201)]
        [TestCase(int.MaxValue)]
        public void Limits_RejectsValuesOutsideOneThroughTwoHundred(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteReservationSearchLimits(value));
        }

        [Test]
        public void Limits_DefaultIsExactAndImmutable()
        {
            Assert.That(SiteReservationSearchLimits.Default.MaxFailedCombinations, Is.EqualTo(200));
            Assert.That(SiteReservationSearchLimits.ProductionMaximum, Is.EqualTo(200));
            Assert.That(PublicSetters(typeof(SiteReservationSearchLimits)), Is.Empty);
        }

        [TestCaseSource(nameof(SearchErrorCodeCases))]
        public void SearchErrorCode_UsesFrozenOrdinalOrder(
            SiteReservationSearchErrorCode value,
            int ordinal)
        {
            Assert.That((int)value, Is.EqualTo(ordinal));
            Assert.That(new SiteReservationSearchError(
                value, string.Empty, string.Empty, -1, "stable").Code, Is.EqualTo(value));
        }

        [TestCaseSource(nameof(RejectionReasonCases))]
        public void RejectionReason_UsesFrozenOrdinalOrder(
            SiteReservationRejectionReason value,
            int ordinal)
        {
            Assert.That((int)value, Is.EqualTo(ordinal));
        }

        [Test]
        public void SearchOption_ValidatesCapacityAndPreservesPlacement()
        {
            var placement = Single(SiteReservationKind.Start, WorldId, 0, 0);
            var option = new SiteReservationSearchOption(placement, -1);

            Assert.That(option.Placement, Is.SameAs(placement));
            Assert.That(option.FutureCoreAvailableSectorCount, Is.EqualTo(-1));
            Assert.Throws<ArgumentNullException>(() => new SiteReservationSearchOption(null, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteReservationSearchOption(placement, -2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteReservationSearchOption(placement, 170));
        }

        [Test]
        public void SearchGroup_CopiesCanonicalizesAndProtectsOptions()
        {
            var later = Option(SiteReservationKind.Start, WorldId, 4, 0);
            var earlier = Option(SiteReservationKind.Start, WorldId, 0, 0);
            var caller = new List<SiteReservationSearchOption> { later, earlier };
            var group = new SiteReservationSearchGroup(
                Key(SiteReservationKind.Start, WorldId), null, null, null, caller);
            caller.Clear();

            Assert.That(group.Options.Select(item => item.Placement.Candidate.OriginIndex),
                Is.EqualTo(new[] { 0, 4 }));
            Assert.That(group.OptionCount, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SiteReservationSearchOption>)group.Options).Clear());
        }

        [Test]
        public void SearchGroup_RejectsIdentityDuplicateVillageAndInvalidStartInputs()
        {
            var start = Option(SiteReservationKind.Start, WorldId, 0, 0);
            Assert.Throws<ArgumentException>(() => new SiteReservationSearchGroup(
                Key(SiteReservationKind.Start, WorldId), null, null, null,
                new[] { start, start }));
            Assert.Throws<ArgumentException>(() => new SiteReservationSearchGroup(
                Key(SiteReservationKind.Start, WorldId), Starter.SpecialMaps[BossId], null, null,
                new[] { start }));
            Assert.Throws<ArgumentException>(() => new SiteReservationSearchGroup(
                Key(SiteReservationKind.Village, "VILLAGE_TEST"), null, null, null,
                Array.Empty<SiteReservationSearchOption>()));
        }

        [Test]
        public void ConflictDetector_ReportsExactCanonicalCollisionReasons()
        {
            var selected = WithEntry(SiteReservationKind.Start, WorldId, 4, 4, SiteEntrySide.R);
            var overlap = Single(SiteReservationKind.Boss, BossId, 4, 4);
            var blocksEntry = Single(SiteReservationKind.Boss, BossId, 5, 4);
            var candidateEntry = WithEntry(SiteReservationKind.Boss, BossId, 3, 4, SiteEntrySide.R);
            var detector = new SitePlacementConflictDetector();

            Assert.That(detector.Evaluate(overlap, new[] { selected }),
                Is.EqualTo(new[] { SiteReservationRejectionReason.FootprintOverlap }));
            Assert.That(detector.Evaluate(blocksEntry, new[] { selected }),
                Is.EqualTo(new[] { SiteReservationRejectionReason.BlocksExistingEntryApproach }));
            Assert.That(detector.Evaluate(candidateEntry, new[] { selected }),
                Is.EqualTo(new[] { SiteReservationRejectionReason.EntryApproachOccupied }));
        }

        [Test]
        public void ConflictDetector_AllowsSharedEntryExteriorAndOrdinaryAdjacency()
        {
            var first = WithEntry(SiteReservationKind.Start, WorldId, 4, 4, SiteEntrySide.R);
            var second = WithEntry(SiteReservationKind.Boss, BossId, 6, 4, SiteEntrySide.L);
            var adjacent = Single(SiteReservationKind.Boss, BossId, 4, 5);
            var detector = new SitePlacementConflictDetector();

            Assert.That(detector.Evaluate(second, new[] { first }), Is.Empty);
            Assert.That(detector.Evaluate(adjacent, new[] { first }), Is.Empty);
        }

        [Test]
        public void Search_InvalidDependenciesDoNotConsumeRng()
        {
            var fresh = new DeterministicRngStream(1);
            var backtracker = new SiteReservationBacktracker();
            var missingGroups = backtracker.Search(null, null, null, null, fresh);

            Assert.That(missingGroups.Status, Is.EqualTo(SiteReservationSearchStatus.InvalidInput));
            Assert.That(missingGroups.Errors.Select(error => error.Code), Does.Contain(
                SiteReservationSearchErrorCode.MissingGroups));
            Assert.That(missingGroups.Errors.Select(error => error.Code), Does.Contain(
                SiteReservationSearchErrorCode.MissingDistancePolicy));
            Assert.That(fresh.DrawCount, Is.Zero);

            var consumed = new DeterministicRngStream(2);
            consumed.NextUInt64();
            var invalid = backtracker.Search(StandardGroups(), Starter.Policy,
                SiteCandidateCostWeights.Default, SiteReservationSearchLimits.Default, consumed);
            Assert.That(invalid.Status, Is.EqualTo(SiteReservationSearchStatus.InvalidInput));
            Assert.That(invalid.Errors.Single().Code,
                Is.EqualTo(SiteReservationSearchErrorCode.SiteRngAlreadyConsumed));
            Assert.That(consumed.DrawCount, Is.EqualTo(1));
        }

        [Test]
        public void Search_CompletedPlanHasExactOrderPolicyAndDiagnostics()
        {
            var groups = StandardGroups();
            var rng = new DeterministicRngStream(10);
            var result = Search(groups, rng);

            Assert.That(result.Status, Is.EqualTo(SiteReservationSearchStatus.Completed));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.SelectionPlan.SelectedCount, Is.EqualTo(6));
            Assert.That(result.SelectionPlan.Steps.Select(step => step.Depth),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(result.SelectionPlan.Steps.Select(step => step.Key.SourceDefinitionId),
                Is.EqualTo(new[] { WorldId, BossId, ForgeId, CassiaId, YeastId, MeteorId }));
            Assert.That(result.SelectionPlan.Steps.Sum(step =>
                step.IncrementalCost.DistanceConstraintCountChecked), Is.EqualTo(15));
            Assert.That(result.Diagnostics.TotalSourceOptionCount, Is.EqualTo(6));
            Assert.That(result.Diagnostics.TieBreakDrawCount, Is.EqualTo(6));
            Assert.That(result.Diagnostics.RngDrawCountAfter, Is.EqualTo(6));
            Assert.That(result.Diagnostics.DeepestSelectedDepth, Is.EqualTo(6));
            Assert.That(result.Diagnostics.FailedCombinationCount,
                Is.EqualTo(result.Diagnostics.BacktrackCount));

            var index = new SiteDistanceIndexBuilder().Build(
                result.SelectionPlan.SelectedPlacements);
            Assert.That(index.Succeeded, Is.True);
            Assert.That(index.Index.PlacementCount, Is.EqualTo(6));
            Assert.That(index.Index.PairCount, Is.EqualTo(15));
            Assert.That(index.Index.Evaluate(Starter.Policy).Satisfied, Is.True);
        }

        [Test]
        public void Search_KnownTieBreakBacktracksToPreviousSelectionAndSucceeds()
        {
            var groups = StandardGroups();
            groups[0] = StartGroup(
                Option(SiteReservationKind.Start, WorldId, 0, 0),
                Option(SiteReservationKind.Start, WorldId, 4, 0));
            var rng = new DeterministicRngStream(KnownSiteInitialState);
            var result = Search(groups, rng);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.SelectionPlan.Steps[0].Option.Placement.Candidate.OriginIndex,
                Is.EqualTo(0));
            Assert.That(result.Diagnostics.FailedCombinationCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.BacktrackCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Groups[0].BacktrackPopCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Groups[1].GetReasonCount(
                SiteReservationRejectionReason.FootprintOverlap), Is.EqualTo(1));
            Assert.That(result.Diagnostics.TieBreakDrawCount, Is.EqualTo(7));
            Assert.That(new DeterministicRngStream(KnownSiteInitialState).NextUInt64(),
                Is.EqualTo(0xF627BD56683B33FCUL));
        }

        [Test]
        public void Search_CustomLimitStopsImmediatelyAfterFirstPopWithoutPartialPlan()
        {
            var groups = StandardGroups();
            groups[0] = StartGroup(
                Option(SiteReservationKind.Start, WorldId, 0, 0),
                Option(SiteReservationKind.Start, WorldId, 4, 0));
            var result = new SiteReservationBacktracker().Search(
                groups,
                Starter.Policy,
                SiteCandidateCostWeights.Default,
                new SiteReservationSearchLimits(1),
                new DeterministicRngStream(KnownSiteInitialState));

            Assert.That(result.Status,
                Is.EqualTo(SiteReservationSearchStatus.FailedCombinationLimitReached));
            Assert.That(result.RetryRequired, Is.True);
            Assert.That(result.SelectionPlan, Is.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Diagnostics.FailedCombinationCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.BacktrackCount, Is.EqualTo(1));
        }

        [Test]
        public void Search_ExhaustionReturnsNoSolutionAndNoPartialPlan()
        {
            var groups = StandardGroups();
            groups[0] = StartGroup(
                Option(SiteReservationKind.Start, WorldId, 0, 0),
                Option(SiteReservationKind.Start, WorldId, 4, 0));
            groups[1] = SpecialGroup(BossId, SiteReservationKind.Boss,
                new SiteReservationSearchOption(
                    Sparse(SiteReservationKind.Boss, BossId, 0, 0, 5, 1,
                        new SectorCoord(0, 0), new SectorCoord(4, 0)), -1));
            var result = Search(groups, new DeterministicRngStream(4));

            Assert.That(result.Status, Is.EqualTo(SiteReservationSearchStatus.NoSolution));
            Assert.That(result.RetryRequired, Is.True);
            Assert.That(result.SelectionPlan, Is.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Diagnostics.BacktrackCount, Is.EqualTo(2));
        }

        [Test]
        public void Search_GroupAndOptionReversalDoesNotChangeOutput()
        {
            var baseline = StandardGroups();
            baseline[0] = StartGroup(
                Option(SiteReservationKind.Start, WorldId, 0, 0),
                Option(SiteReservationKind.Start, WorldId, 0, 4));
            var reversed = baseline.AsEnumerable().Reverse().Select(group =>
                new SiteReservationSearchGroup(group.Key, group.SpecialMap, group.PrimaryBiome,
                    group.CorePatchRule, group.Options.Reverse())).ToArray();

            var first = Search(baseline, new DeterministicRngStream(77));
            var second = Search(reversed, new DeterministicRngStream(77));
            Assert.That(Snapshot(second), Is.EqualTo(Snapshot(first)));
        }

        [Test]
        public void Search_IsCultureIndependentAndConsumesNoOtherStreams()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                var en = Search(StandardGroups(), new DeterministicRngStream(91));
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var tr = Search(StandardGroups(), new DeterministicRngStream(91));
                Assert.That(Snapshot(tr), Is.EqualTo(Snapshot(en)));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }

            var others = Enumerable.Range(1, 5).Select(index =>
                new DeterministicRngStream((ulong)index)).ToArray();
            var result = Search(StandardGroups(), new DeterministicRngStream(92));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(others.Select(stream => stream.DrawCount), Is.All.Zero);
        }

        [TestCaseSource(nameof(FullStarterSeedCases))]
        public void FullStarter_ThreeSeedsCompleteWithExactOptionDrawAndPolicyCounts(ulong seed)
        {
            var result = Search(Starter.FullGroups, WorldSiteStream(seed));

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Diagnostics.Groups.Select(group => group.SourceOptionCount),
                Is.EqualTo(new[] { 88, 572, 624, 624, 624, 624 }));
            Assert.That(result.Diagnostics.TotalSourceOptionCount, Is.EqualTo(3156));
            Assert.That(result.Diagnostics.TieBreakDrawCount, Is.EqualTo(3156));
            Assert.That(result.Diagnostics.RngDrawCountBefore, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCountAfter, Is.EqualTo(3156));
            Assert.That(result.SelectionPlan.SelectedCount, Is.EqualTo(6));
            Assert.That(result.SelectionPlan.Steps.All(step =>
                step.IncrementalCost.HardConstraintsSatisfied), Is.True);
            Assert.That(result.SelectionPlan.Steps.Last().IncrementalCost.CoreClusterUnits,
                Is.Zero);
            Assert.That(new SiteDistanceIndexBuilder().Build(
                result.SelectionPlan.SelectedPlacements).Index.Evaluate(Starter.Policy).Satisfied,
                Is.True);
        }

        [Test]
        public void FullStarter_FreshAndReusedBacktrackerAreIdenticalAcrossOneHundredRuns()
        {
            var expected = Snapshot(Search(Starter.FullGroups, WorldSiteStream(4660UL)));
            var reused = new SiteReservationBacktracker();
            for (var run = 0; run < 100; run++)
            {
                var result = reused.Search(
                    Starter.FullGroups,
                    Starter.Policy,
                    SiteCandidateCostWeights.Default,
                    SiteReservationSearchLimits.Default,
                    WorldSiteStream(4660UL));
                Assert.That(Snapshot(result), Is.EqualTo(expected), "run " + run);
            }
        }

        [Test]
        public void PublicSurface_IsImmutableAndHasNoUnityLifecycleOrMutableStatics()
        {
            var types = new[]
            {
                typeof(SiteReservationSearchOption), typeof(SiteReservationSearchGroup),
                typeof(SiteReservationSearchLimits), typeof(SitePlacementConflictDetector),
                typeof(SiteReservationGroupDiagnostics), typeof(SiteReservationSearchDiagnostics),
                typeof(SiteReservationSelectionStep), typeof(SiteReservationSelectionPlan),
                typeof(SiteReservationSearchError), typeof(SiteReservationSearchResult),
                typeof(SiteReservationBacktracker)
            };
            foreach (var type in types)
            {
                Assert.That(PublicSetters(type), Is.Empty, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(field => !field.IsLiteral), Is.Empty, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
                Assert.That(type.FullName, Does.Not.Contain("UnityEditor"));
                Assert.That(type.BaseType == null ||
                    !string.Equals(type.BaseType.FullName, "UnityEngine.Object", StringComparison.Ordinal),
                    Is.True, type.FullName);
            }
        }

        private static SiteReservationSearchResult Search(
            IEnumerable<SiteReservationSearchGroup> groups,
            DeterministicRngStream rng) => new SiteReservationBacktracker().Search(
                groups, Starter.Policy, SiteCandidateCostWeights.Default,
                SiteReservationSearchLimits.Default, rng);

        private static List<SiteReservationSearchGroup> StandardGroups() => new List<SiteReservationSearchGroup>
        {
            StartGroup(Option(SiteReservationKind.Start, WorldId, 0, 0)),
            SpecialGroup(BossId, SiteReservationKind.Boss,
                Option(SiteReservationKind.Boss, BossId, 4, 0)),
            SpecialGroup(ForgeId, SiteReservationKind.Forge,
                Option(SiteReservationKind.Forge, ForgeId, 0, 4)),
            SpecialGroup(CassiaId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, CassiaId, 4, 4)),
            SpecialGroup(YeastId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, YeastId, 8, 0)),
            SpecialGroup(MeteorId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, MeteorId, 8, 8))
        };

        private static SiteReservationSearchGroup StartGroup(
            params SiteReservationSearchOption[] options) => new SiteReservationSearchGroup(
                Key(SiteReservationKind.Start, WorldId), null, null, null, options);

        private static SiteReservationSearchGroup SpecialGroup(
            string sourceId,
            SiteReservationKind kind,
            params SiteReservationSearchOption[] options)
        {
            var special = Starter.SpecialMaps[sourceId];
            var biome = Starter.Biomes[special.PrimaryBiomeId];
            return new SiteReservationSearchGroup(
                Key(kind, sourceId), special, biome,
                Starter.CoreRules[biome.BiomeId], options);
        }

        private static SitePlacementKey Key(SiteReservationKind kind, string sourceId) =>
            new SitePlacementKey(kind, sourceId, 0);

        private static SiteReservationSearchOption Option(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int capacity = -1) => new SiteReservationSearchOption(
                Single(kind, sourceId, x, y), capacity);

        private static FootprintPlacement Single(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y) => Sparse(kind, sourceId, x, y, 1, 1, new SectorCoord(0, 0));

        private static FootprintPlacement Sparse(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int width,
            int height,
            params SectorCoord[] localCells)
        {
            var origin = new SectorCoord(x, y);
            var candidate = new SiteOriginCandidate(
                kind, sourceId, 0, origin, WorldGridIndex.ToIndex(origin), EdgeRing(origin), 0);
            var footprint = new SiteFootprint(width, height, SiteFootprintTransform.R0,
                localCells.Select(cell => new SiteFootprintCell(
                    cell.X, cell.Y, "CELL", string.Empty, string.Empty,
                    Array.Empty<SiteEntrySide>())));
            return new FootprintPlacement(
                candidate,
                footprint,
                localCells.Select(cell => new SectorCoord(x + cell.X, y + cell.Y)),
                Array.Empty<FootprintPlacementEntry>());
        }

        private static FootprintPlacement WithEntry(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            SiteEntrySide side)
        {
            var placement = Single(kind, sourceId, x, y);
            SiteReservationTokenCodec.GetDelta(side, out var deltaX, out var deltaY);
            var sector = new SectorCoord(x, y);
            var entry = new FootprintPlacementEntry(
                "ENTRY_TEST", 0, 0, sector, side,
                new SectorCoord(x + deltaX, y + deltaY), new[] { 1 }, true, true);
            return new FootprintPlacement(
                placement.Candidate, placement.Footprint, placement.OccupiedSectors,
                new[] { entry });
        }

        private static int EdgeRing(SectorCoord coordinate) => Math.Min(
            Math.Min(coordinate.X, WorldGenConstants.SectorColumns - 1 - coordinate.X),
            Math.Min(coordinate.Y, WorldGenConstants.SectorRows - 1 - coordinate.Y));

        private static string Snapshot(SiteReservationSearchResult result)
        {
            var plan = result.SelectionPlan == null
                ? "-"
                : string.Join(",", result.SelectionPlan.Steps.Select(step =>
                    step.Key.SourceDefinitionId + ":" +
                    step.Option.Placement.Candidate.OriginIndex + ":" +
                    (int)step.Option.Placement.Footprint.Transform + ":" +
                    step.RandomTieBreak + ":" + step.IncrementalCost.TotalCost));
            var diagnostics = result.Diagnostics;
            return result.Status + "|" + plan + "|" + diagnostics.TotalSourceOptionCount + "|" +
                   diagnostics.CandidateEvaluationCount + "|" + diagnostics.SelectionPushCount + "|" +
                   diagnostics.FailedCombinationCount + "|" + diagnostics.DeepestSelectedDepth + "|" +
                   diagnostics.TieBreakDrawCount + "|" + diagnostics.RngDrawCountAfter;
        }

        private static string FormatErrors(SiteReservationSearchResult result) =>
            string.Join("\n", result.Errors.Select(error => error.Code + ":" + error.Message));

        private static IEnumerable<PropertyInfo> PublicSetters(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(property => property.SetMethod != null && property.SetMethod.IsPublic);

        private static DeterministicRngStream WorldSiteStream(ulong worldSeed) =>
            new DeterministicRngStream(WorldSiteInitialState(worldSeed));

        private static ulong WorldSiteInitialState(ulong worldSeed)
        {
            var material = new List<byte>();
            material.AddRange(Encoding.ASCII.GetBytes("STARNIGHT_MAP_RNG_V1"));
            AppendU64(material, worldSeed);
            material.AddRange(HexBytes("A13C9E0B2F1044D1"));
            AppendUtf8(material, "RNG_WORLD_SITE");
            AppendUtf8(material, "WORLD");
            AppendUtf8(material, string.Empty);
            AppendU64(material, 0);
            using (var sha = SHA256.Create())
            {
                var digest = sha.ComputeHash(material.ToArray());
                return ((ulong)digest[0] << 56) | ((ulong)digest[1] << 48) |
                       ((ulong)digest[2] << 40) | ((ulong)digest[3] << 32) |
                       ((ulong)digest[4] << 24) | ((ulong)digest[5] << 16) |
                       ((ulong)digest[6] << 8) | digest[7];
            }
        }

        private static byte[] HexBytes(string value)
        {
            var result = new byte[value.Length / 2];
            for (var index = 0; index < result.Length; index++)
                result[index] = byte.Parse(value.Substring(index * 2, 2),
                    NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return result;
        }

        private static void AppendUtf8(ICollection<byte> target, string value)
        {
            var bytes = new UTF8Encoding(false, true).GetBytes(value);
            AppendU64(target, (ulong)bytes.Length);
            foreach (var valueByte in bytes) target.Add(valueByte);
        }

        private static void AppendU64(ICollection<byte> target, ulong value)
        {
            for (var shift = 56; shift >= 0; shift -= 8) target.Add((byte)(value >> shift));
        }

        private static StarterData BuildStarterData()
        {
            var biomeRows = new List<string[]>
            {
                BiomeRow(CraterBiomeId, 0, 7), BiomeRow(CassiaBiomeId, 2, 12),
                BiomeRow(MillBiomeId, 1, 11), BiomeRow(DoughBiomeId, 0, 7)
            };
            var patchRows = new List<string[]>
            {
                PatchRow(CraterBiomeId, 5, true), PatchRow(CassiaBiomeId, 5, false),
                PatchRow(MillBiomeId, 4, false), PatchRow(DoughBiomeId, 5, true)
            };
            var biomeResult = new BiomeBoundaryDefinitionBuilder().Build(
                BiomeSpecs.Select(spec => BuildBiomeSource(spec,
                    spec.FileName == "biome_types.csv" ? biomeRows :
                    spec.FileName == "biome_patch_rules.csv" ? patchRows : null)));
            if (!biomeResult.Success)
                throw new InvalidOperationException(string.Join("\n", biomeResult.Errors));

            var specialRows = SpecialRows();
            var specialResult = new SpecialVillageDefinitionBuilder().Build(
                SpecialSpecs.Select(spec => BuildSpecialSource(spec,
                    specialRows.TryGetValue(spec.FileName, out var rows) ? rows : null)));
            if (!specialResult.Success)
                throw new InvalidOperationException(string.Join("\n", specialResult.Errors));
            var policyResult = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(
                WorldId, specialResult.DefinitionSet.SpecialMaps.Values);
            if (!policyResult.Succeeded)
                throw new InvalidOperationException(string.Join("\n", policyResult.Errors));

            var coreRules = biomeResult.DefinitionSet.BiomePatchRules.Values
                .ToDictionary(rule => rule.BiomeId, StringComparer.Ordinal);
            var starter = new StarterData(
                biomeResult.DefinitionSet.BiomeTypes,
                coreRules,
                specialResult.DefinitionSet,
                policyResult.Policy);
            starter.SetFullGroups(BuildFullGroups(starter));
            return starter;
        }

        private static IReadOnlyList<SiteReservationSearchGroup> BuildFullGroups(StarterData data)
        {
            var solver = new FootprintPlacementSolver();
            var startOptions = new List<SiteReservationSearchOption>();
            var startOrdinal = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var origin = WorldGridIndex.ToCoordinate(index);
                var ring = EdgeRing(origin);
                if (ring > 1) continue;
                var candidate = new SiteOriginCandidate(
                    SiteReservationKind.Start, WorldId, 0, origin, index, ring, startOrdinal++);
                var result = solver.SolveStart(candidate, FootprintPlacementBlockers.Empty);
                if (!result.Succeeded) throw new InvalidOperationException("Start placement failed.");
                startOptions.Add(new SiteReservationSearchOption(result.Placement, -1));
            }

            var groups = new List<SiteReservationSearchGroup>
            {
                new SiteReservationSearchGroup(Key(SiteReservationKind.Start, WorldId),
                    null, null, null, startOptions)
            };
            AddFullSpecialGroup(groups, data, solver, BossId, SiteReservationKind.Boss);
            AddFullSpecialGroup(groups, data, solver, ForgeId, SiteReservationKind.Forge);
            AddFullSpecialGroup(groups, data, solver, CassiaId, SiteReservationKind.CoreResource);
            AddFullSpecialGroup(groups, data, solver, YeastId, SiteReservationKind.CoreResource);
            AddFullSpecialGroup(groups, data, solver, MeteorId, SiteReservationKind.CoreResource);
            return new ReadOnlyCollection<SiteReservationSearchGroup>(groups);
        }

        private static void AddFullSpecialGroup(
            ICollection<SiteReservationSearchGroup> groups,
            StarterData data,
            FootprintPlacementSolver solver,
            string sourceId,
            SiteReservationKind kind)
        {
            var options = new List<SiteReservationSearchOption>();
            var special = data.SpecialMaps[sourceId];
            var cells = data.SpecialDefinitions.GetSpecialMapFootprintCells(sourceId);
            var entries = data.SpecialDefinitions.GetSpecialMapEntrySockets(sourceId);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var origin = WorldGridIndex.ToCoordinate(index);
                var candidate = new SiteOriginCandidate(
                    kind, sourceId, 0, origin, index, EdgeRing(origin), index);
                foreach (SiteFootprintTransform transform in
                         Enum.GetValues(typeof(SiteFootprintTransform)))
                {
                    var result = solver.SolveSpecialSite(candidate, transform, special, cells,
                        entries, FootprintPlacementBlockers.Empty);
                    if (result.Succeeded)
                        options.Add(new SiteReservationSearchOption(result.Placement, -1));
                }
            }
            var biome = data.Biomes[special.PrimaryBiomeId];
            groups.Add(new SiteReservationSearchGroup(
                Key(kind, sourceId), special, biome, data.CoreRules[biome.BiomeId], options));
        }

        private static Dictionary<string, IReadOnlyList<string[]>> SpecialRows()
        {
            return new Dictionary<string, IReadOnlyList<string[]>>(StringComparer.Ordinal)
            {
                { "special_map_catalog.csv", new[]
                    {
                        SpecialRow(BossId, "BOSS", MillBiomeId, 2, 1, 4, 2),
                        SpecialRow(ForgeId, "FORGE", MillBiomeId, 1, 1, 2, 2),
                        SpecialRow(CassiaId, "CORE_RESOURCE", CassiaBiomeId, 1, 1, 2, 3),
                        SpecialRow(YeastId, "CORE_RESOURCE", DoughBiomeId, 1, 1, 2, 3),
                        SpecialRow(MeteorId, "CORE_RESOURCE", CraterBiomeId, 1, 1, 2, 3)
                    }
                },
                { "special_map_footprint_cells.csv", new[]
                    {
                        FootprintRow(BossId, MillBiomeId, 0, 0, "ENTRY", "L"),
                        FootprintRow(BossId, MillBiomeId, 1, 0, "ARENA", "R"),
                        FootprintRow(ForgeId, MillBiomeId, 0, 0, "CORE", "L"),
                        FootprintRow(CassiaId, CassiaBiomeId, 0, 0, "CORE", "L"),
                        FootprintRow(YeastId, DoughBiomeId, 0, 0, "CORE", "L"),
                        FootprintRow(MeteorId, CraterBiomeId, 0, 0, "CORE", "L")
                    }
                },
                { "special_map_entry_sockets.csv", new[]
                    {
                        EntryRow(BossId), EntryRow(ForgeId), EntryRow(CassiaId),
                        EntryRow(YeastId), EntryRow(MeteorId)
                    }
                }
            };
        }

        private static string[] BiomeRow(string id, int minimumY, int maximumY) => new[]
        {
            id, "Biome", "STAGE_MOON", "1", "1", "169", "1",
            minimumY.ToString(CultureInfo.InvariantCulture),
            maximumY.ToString(CultureInfo.InvariantCulture), "1.0", "THEME_MOON",
            "AUDIO_MOON", "MICRO_MOON", "RECIPE_MOON", "RESOURCE_MOON",
            "ELEMENT_MOON", "SITE_REQUIRED", "1", "test"
        };

        private static string[] PatchRow(string id, int minimum, bool edge) => new[]
        {
            "RULE_CORE_" + id, id, "CORE", minimum.ToString(CultureInfo.InvariantCulture),
            "169", "1", "1", "1", "1.0", edge ? "1" : "0", "1", "1",
            "1.0", "1.0", "1.0", "1.0", "1.0", "1.0", "1", "test"
        };

        private static string[] SpecialRow(
            string id, string role, string biome, int width, int height,
            int startDistance, int otherDistance) => new[]
        {
            id, "Site", role, biome, width.ToString(CultureInfo.InvariantCulture),
            height.ToString(CultureInfo.InvariantCulture), "1",
            startDistance.ToString(CultureInfo.InvariantCulture),
            otherDistance.ToString(CultureInfo.InvariantCulture),
            "1|2|3", "0", "REWARD_NONE", "FIXED", "1", "test"
        };

        private static string[] FootprintRow(
            string id, string biome, int x, int y, string role, string sides) => new[]
        {
            id, x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture),
            role, biome, "RECIPE_FIXED", sides, "test"
        };

        private static string[] EntryRow(string id) => new[]
        {
            id, "ENTRY_L", "0", "0", "L", "1|2|3", "1", "1", "test"
        };

        private static BiomeBoundaryDefinitionSource BuildBiomeSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var catalog = new CsvSchemaCatalogBuilder().Build(SchemaRows(spec));
            if (!catalog.Success) throw new InvalidOperationException(string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var csv = BuildCsv(spec, rows ?? new[] { StandardRow(spec) });
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            if (!validation.Success)
                throw new InvalidOperationException(string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            if (!keys.Success) throw new InvalidOperationException("Primary-key index failed.");
            var result = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            return new BiomeBoundaryDefinitionSource(schema, result);
        }

        private static SpecialVillageDefinitionSource BuildSpecialSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var catalog = new CsvSchemaCatalogBuilder().Build(SchemaRows(spec));
            if (!catalog.Success) throw new InvalidOperationException(string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var csv = BuildCsv(spec, rows ?? new[] { StandardRow(spec) });
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            if (!validation.Success)
                throw new InvalidOperationException(string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            if (!keys.Success) throw new InvalidOperationException("Primary-key index failed.");
            var result = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            return new SpecialVillageDefinitionSource(schema, result);
        }

        private static IEnumerable<CsvSchemaDictionaryRow> SchemaRows(FileSpec spec) =>
            spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName, (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name, column.DataType, index < spec.PrimaryKeyCount ? "1" : "0",
                index < spec.PrimaryKeyCount
                    ? (index + 1).ToString(CultureInfo.InvariantCulture) : string.Empty,
                string.Empty, column.AllowedValues, string.Empty, string.Empty, index + 2));

        private static string BuildCsv(FileSpec spec, IEnumerable<string[]> rows)
        {
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in rows) csv += "\n" + string.Join(",", row.Select(CsvCell));
            return csv;
        }

        private static string[] StandardRow(FileSpec spec) => spec.Columns.Select((column, index) =>
        {
            var allowed = column.AllowedValues.Split(
                new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if ((column.DataType == "ENUM" || column.DataType == "ENUM_LIST") && allowed.Length > 0)
                return allowed[0];
            switch (column.DataType)
            {
                case "STRING": return "TEXT_" + (index + 1);
                case "ID": return "ID_" + (index + 1);
                case "INT": return (index + 1).ToString(CultureInfo.InvariantCulture);
                case "FLOAT": return "0.25";
                case "BOOL": return "0";
                case "ID_LIST": return "LIST_A|LIST_B";
                case "ENUM_LIST": return "L";
                case "INT_LIST": return "1|2";
                default: throw new ArgumentOutOfRangeException(nameof(column.DataType));
            }
        }).ToArray();

        private static string CsvCell(string value) =>
            value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static FileSpec[] CreateBiomeSpecs() => new[]
        {
            File("biome_types.csv", 1, "biome_id:ID", "display_name_ko:STRING", "stage_id:ID", "required:BOOL", "min_patch_count:INT", "max_patch_count:INT", "min_core_patch_count:INT", "preferred_altitude_min_sector_y:INT", "preferred_altitude_max_sector_y:INT", "growth_weight:FLOAT", "tile_theme_id:ID", "audio_profile_id:ID", "microchunk_pool_prefix:ID", "sector_recipe_pool_prefix:ID", "common_resource_pool_id:ID", "map_element_pool_id:ID", "required_special_map_ids:ID_LIST", "active:BOOL", "notes:STRING"),
            File("biome_patch_rules.csv", 1, "patch_rule_id:ID", "biome_id:ID", "patch_role:ENUM:CORE|OUTER", "min_sector_count:INT", "max_sector_count:INT", "min_seed_distance:INT", "seed_count_min:INT", "seed_count_max:INT", "seed_weight:FLOAT", "can_touch_world_edge:BOOL", "buffer_ring_sectors:INT", "allow_single_sector:BOOL", "max_world_share:FLOAT", "distance_weight:FLOAT", "altitude_weight:FLOAT", "noise_weight:FLOAT", "compactness_weight:FLOAT", "branchiness_target:FLOAT", "active:BOOL", "notes:STRING"),
            File("biome_boundary_profiles.csv", 1, "boundary_profile_id:ID", "display_name_ko:STRING", "boundary_type:ENUM:WALL", "allowed_orientations:ENUM_LIST:L|R|U|D", "width_microchunks_min:INT", "width_microchunks_max:INT", "warning_microchunks_min:INT", "mandatory_route_allowed:BOOL", "tool_requirement:ENUM:NONE", "hard_border:BOOL", "active:BOOL", "notes:STRING"),
            File("biome_boundary_pair_rules.csv", 1, "boundary_pair_rule_id:ID", "biome_a_id:ID", "biome_b_id:ID", "allowed_boundary_profile_ids:ID_LIST", "boundary_profile_weights:INT_LIST", "default_boundary_profile_id:ID", "transition_resource_pool_id:ID", "transition_element_pool_id:ID", "min_shared_edge_count:INT", "active:BOOL", "notes:STRING"),
            File("boundary_chunk_catalog.csv", 1, "boundary_chunk_id:ID", "microchunk_id:ID", "biome_a_id:ID", "biome_b_id:ID", "boundary_profile_id:ID", "orientation:ENUM:L|R|U|D", "route_type:INT", "entry_edge_signature_id:ID", "exit_edge_signature_id:ID", "weight:INT", "reversible:BOOL", "active:BOOL", "notes:STRING")
        };

        private static FileSpec[] CreateSpecialSpecs() => new[]
        {
            File("event_activation_routes.csv", 1, "event_route_id:ID", "special_map_id:ID", "event_id:ID", "mandatory:BOOL", "allowed_sector_types:INT_LIST", "requires_tool:BOOL", "requires_consumable:BOOL", "min_safe_tiles_before_trigger:INT", "return_path_required:BOOL", "trigger_slot_id:ID", "notes:STRING"),
            File("special_map_catalog.csv", 1, "special_map_id:ID", "display_name_ko:STRING", "site_role:ENUM:BOSS|FORGE|CORE_RESOURCE|VILLAGE", "primary_biome_id:ID", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "required_count:INT", "min_graph_distance_from_start:INT", "min_graph_distance_to_other_core_sites:INT", "allowed_entry_route_types:INT_LIST", "requires_tool:BOOL", "mandatory_reward_id:ID", "generation_mode:ENUM:FIXED|GENERATED", "active:BOOL", "notes:STRING"),
            File("special_map_entry_sockets.csv", 2, "special_map_id:ID", "entry_socket_id:ID", "local_sector_x:INT", "local_sector_y:INT", "side:ENUM:L|R|U|D", "allowed_route_types:INT_LIST", "required:BOOL", "return_path_required:BOOL", "notes:STRING"),
            File("special_map_footprint_cells.csv", 3, "special_map_id:ID", "local_sector_x:INT", "local_sector_y:INT", "local_role:ENUM:ENTRY|ARENA|CORE", "required_primary_biome_id:ID", "fixed_sector_recipe_id:ID", "required_open_sides:ENUM_LIST:L|R|U|D", "notes:STRING"),
            File("special_map_rewards.csv", 2, "special_map_id:ID", "reward_order:INT", "reward_id:ID", "reward_kind:ENUM:ITEM", "mandatory:BOOL", "slot_id:ID", "quantity_min:INT", "quantity_max:INT", "notes:STRING"),
            File("shop_archetypes.csv", 1, "shop_archetype_id:ID", "display_name_ko:STRING", "shop_type:ENUM:GENERAL", "item_slot_count_min:INT", "item_slot_count_max:INT", "base_price_multiplier:FLOAT", "allows_reputation_reward:BOOL", "active:BOOL", "notes:STRING"),
            File("shop_inventory_rules.csv", 2, "shop_archetype_id:ID", "slot_index:INT", "spawn_pool_id:ID", "guaranteed:BOOL", "quantity_min:INT", "quantity_max:INT", "price_min_gold:INT", "price_max_gold:INT", "required_favor_tier:INT", "active:BOOL", "notes:STRING"),
            File("shopkeeper_species.csv", 1, "species_id:ID", "display_name_ko:STRING", "prefab_id:ID", "dialogue_style_id:ID", "animation_set_id:ID", "selection_weight:INT", "allowed_biome_ids:ID_LIST", "active:BOOL", "notes:STRING"),
            File("village_facilities.csv", 1, "facility_id:ID", "display_name_ko:STRING", "facility_group:ENUM:SHOP", "fixed:BOOL", "selection_weight:INT", "prefab_id:ID", "shop_archetype_id:ID", "evacuated_prefab_id:ID", "active:BOOL", "notes:STRING"),
            File("village_layout_catalog.csv", 1, "village_layout_id:ID", "display_name_ko:STRING", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "target_facility_count:INT", "entry_sides:ENUM_LIST:L|R|U|D", "selection_weight:INT", "active:BOOL", "notes:STRING"),
            File("village_layout_cells.csv", 3, "village_layout_id:ID", "local_chunk_x:INT", "local_chunk_y:INT", "cell_role:ENUM:CORE", "facility_slot_id:ID", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_entry_side:ENUM:L|R|U|D", "notes:STRING"),
            File("village_profiles.csv", 1, "village_profile_id:ID", "display_name_ko:STRING", "world_profile_id:ID", "facility_count_min:INT", "facility_count_max:INT", "fixed_facility_ids:ID_LIST", "optional_facility_ids:ID_LIST", "allowed_layout_ids:ID_LIST", "start_distance_buckets:STRING", "maximum_sector_count:INT", "active:BOOL", "notes:STRING")
        };

        private static FileSpec File(string name, int primaryKeyCount, params string[] definitions) =>
            new FileSpec(name, primaryKeyCount, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                var allowed = parts.Length > 2 ? parts[2] :
                    (parts[1] == "ENUM" || parts[1] == "ENUM_LIST"
                        ? "ENUM_A|ENUM_B" : string.Empty);
                return new ColumnSpec(parts[0], parts[1], allowed);
            }).ToArray());

        private sealed class StarterData
        {
            public StarterData(
                IReadOnlyDictionary<string, BiomeTypeDefinition> biomes,
                IReadOnlyDictionary<string, BiomePatchRuleDefinition> coreRules,
                SpecialVillageDefinitionSet specialDefinitions,
                SiteDistancePolicy policy)
            {
                Biomes = biomes;
                CoreRules = coreRules;
                SpecialDefinitions = specialDefinitions;
                Policy = policy;
            }

            public IReadOnlyDictionary<string, BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyDictionary<string, BiomePatchRuleDefinition> CoreRules { get; }
            public SpecialVillageDefinitionSet SpecialDefinitions { get; }
            public IReadOnlyDictionary<string, SpecialMapDefinition> SpecialMaps =>
                SpecialDefinitions.SpecialMaps;
            public SiteDistancePolicy Policy { get; }
            public IReadOnlyList<SiteReservationSearchGroup> FullGroups { get; private set; }
            public void SetFullGroups(IReadOnlyList<SiteReservationSearchGroup> groups) =>
                FullGroups = groups;
        }

        private sealed class FileSpec
        {
            public FileSpec(string name, int primaryKeyCount, IReadOnlyList<ColumnSpec> columns)
            {
                FileName = name;
                PrimaryKeyCount = primaryKeyCount;
                Columns = columns;
            }
            public string FileName { get; }
            public int PrimaryKeyCount { get; }
            public IReadOnlyList<ColumnSpec> Columns { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string name, string dataType, string allowedValues)
            {
                Name = name;
                DataType = dataType;
                AllowedValues = allowedValues;
            }
            public string Name { get; }
            public string DataType { get; }
            public string AllowedValues { get; }
        }
    }
}
