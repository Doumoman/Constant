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
    public sealed class VillageReservationSelectorTests
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string VillageProfileId = "VIL_MOON_PRIMARY";
        private const string VillageId = "SITE_PRIMARY_VILLAGE";
        private const string Layout5Id = "VLAY_STANDARD_5_A";
        private const string Layout6Id = "VLAY_STANDARD_6_A";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";
        private const string CraterBiomeId = "BIO_MOON_CRATER";
        private const string CassiaBiomeId = "BIO_CASSIA_ROOT";
        private const string MillBiomeId = "BIO_ABANDONED_MILL";
        private const string DoughBiomeId = "BIO_MOON_DOUGH";

        private static readonly FileSpec[] BiomeSpecs = CreateBiomeSpecs();
        private static readonly FileSpec[] SpecialSpecs = CreateSpecialSpecs();
        private static readonly StarterData Starter = BuildStarterData();
        private static readonly CoreCapacityApproval StandardApproval = BuildStandardApproval();

        public static IEnumerable BucketRollCases()
        {
            for (var roll = 0; roll < 100; roll++)
                yield return new TestCaseData(
                    roll, roll < 20 ? 0 : roll < 70 ? 1 : 2)
                    .SetName("BucketRoll_" + roll);
        }

        public static IEnumerable ReservationSeedCases()
        {
            for (var seed = 0; seed < WorldGenConstants.SectorCount; seed++)
                yield return new TestCaseData(seed).SetName("ReservationCandidateSeed_" + seed);
        }

        public static IEnumerable ErrorCodeCases()
        {
            foreach (VillageReservationErrorCode value in
                     Enum.GetValues(typeof(VillageReservationErrorCode)))
                yield return new TestCaseData(value, (int)value);
        }

        public static IEnumerable CandidateRejectionCases()
        {
            foreach (VillageCandidateRejectionReason value in
                     Enum.GetValues(typeof(VillageCandidateRejectionReason)))
                yield return new TestCaseData(value, (int)value);
        }

        public static IEnumerable StatusCases()
        {
            foreach (VillageReservationStatus value in
                     Enum.GetValues(typeof(VillageReservationStatus)))
                yield return new TestCaseData(value, (int)value);
        }

        public static IEnumerable FullStarterSeeds()
        {
            yield return new TestCaseData(0UL);
            yield return new TestCaseData(4660UL);
            yield return new TestCaseData(ulong.MaxValue);
        }

        [TestCaseSource(nameof(BucketRollCases))]
        public void DistanceBuckets_ExhaustiveRollMapping(int roll, int expectedOrdinal)
        {
            Assert.That(VillageDistanceBucketCatalog.TryParse(
                "2-3:20|4-6:50|7-10:30", out var catalog, out var error), Is.True, error);
            var bucket = catalog.SelectByRoll(roll);

            Assert.That(bucket.BucketOrdinal, Is.EqualTo(expectedOrdinal));
            Assert.That(roll, Is.InRange(bucket.RollMinInclusive, bucket.RollMaxInclusive));
            Assert.That(bucket.Contains(bucket.MinDistanceInclusive), Is.True);
            Assert.That(bucket.Contains(bucket.MaxDistanceInclusive), Is.True);
        }

        [TestCaseSource(nameof(ReservationSeedCases))]
        public void Reserve_SelectedCandidateMaintainsWorldAndRectangleContracts(int seed)
        {
            var result = Reserve(StandardApproval, new DeterministicRngStream((ulong)seed + 1UL));

            Assert.That(result.Succeeded, Is.True, Format(result));
            var candidate = result.Approval.Village.Candidate;
            Assert.That(candidate.OriginIndex, Is.EqualTo(WorldGridIndex.ToIndex(candidate.Origin)));
            Assert.That(candidate.OccupiedSectorIndices, Is.Ordered.And.Unique);
            Assert.That(candidate.OccupiedSectorIndices, Has.Count.EqualTo(
                candidate.FootprintWidthSectors * candidate.FootprintHeightSectors));
            Assert.That(candidate.EntryExteriorSectorIndex, Is.InRange(0, WorldGenConstants.SectorCount - 1));
            Assert.That(candidate.OccupiedSectorIndices.Contains(
                candidate.EntryExteriorSectorIndex), Is.False);
        }

        [TestCaseSource(nameof(ErrorCodeCases))]
        public void ErrorCode_UsesFrozenOrdinal(VillageReservationErrorCode value, int ordinal)
        {
            Assert.That((int)value, Is.EqualTo(ordinal));
        }

        [TestCaseSource(nameof(CandidateRejectionCases))]
        public void CandidateRejection_UsesFrozenOrdinal(
            VillageCandidateRejectionReason value,
            int ordinal)
        {
            Assert.That((int)value, Is.EqualTo(ordinal));
        }

        [TestCaseSource(nameof(StatusCases))]
        public void Status_UsesFrozenOrdinal(VillageReservationStatus value, int ordinal)
        {
            Assert.That((int)value, Is.EqualTo(ordinal));
        }

        [Test]
        public void ReservationRejection_UsesFrozenOrdinal()
        {
            Assert.That((int)VillageReservationRejectionReason.SelectedBucketHasNoViableCandidate,
                Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" 2-3:20|4-6:50|7-10:30")]
        [TestCase("2-3:20|4-6:50|7-10:30 ")]
        [TestCase("2 -3:20|4-6:50|7-10:30")]
        [TestCase("+2-3:20|4-6:50|7-10:30")]
        [TestCase("-2-3:20|4-6:50|7-10:30")]
        [TestCase("02-3:20|4-6:50|7-10:30")]
        [TestCase("2-03:20|4-6:50|7-10:30")]
        [TestCase("2-3:020|4-6:50|7-10:30")]
        [TestCase("2-3:20|4-6:50|7-10:30|")]
        [TestCase("2-3:20||4-6:50|7-10:30")]
        [TestCase("2-3:20|4-6:50")]
        [TestCase("2-3:20|3-6:50|7-10:30")]
        [TestCase("2-3:20|5-6:50|7-10:30")]
        [TestCase("2-3:20|4-6:40|7-10:40")]
        [TestCase("2-4:20|5-6:50|7-10:30")]
        [TestCase("1-3:20|4-6:50|7-10:30")]
        [TestCase("2-3:21|4-6:49|7-10:30")]
        [TestCase("2-3:0|4-6:70|7-10:30")]
        [TestCase("3-2:20|4-6:50|7-10:30")]
        [TestCase("2-3:2147483648|4-6:50|7-10:30")]
        [TestCase("2-3:20|4-2147483648:50|7-10:30")]
        [TestCase("2-3:+20|4-6:50|7-10:30")]
        [TestCase("2-3:20.0|4-6:50|7-10:30")]
        public void DistanceBuckets_RejectNonCanonicalVectors(string value)
        {
            Assert.That(VillageDistanceBucketCatalog.TryParse(
                value, out var catalog, out var error), Is.False);
            Assert.That(catalog, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void DistanceBuckets_PublishExactFrozenCatalog()
        {
            Assert.That(VillageDistanceBucketCatalog.TryParse(
                "2-3:20|4-6:50|7-10:30", out var catalog, out var error), Is.True, error);
            Assert.That(catalog.TotalWeight, Is.EqualTo(100));
            Assert.That(catalog.Buckets.Select(item => new[]
            {
                item.BucketOrdinal, item.MinDistanceInclusive, item.MaxDistanceInclusive,
                item.Weight, item.RollMinInclusive, item.RollMaxInclusive
            }), Is.EqualTo(new[]
            {
                new[] { 0, 2, 3, 20, 0, 19 },
                new[] { 1, 4, 6, 50, 20, 69 },
                new[] { 2, 7, 10, 30, 70, 99 }
            }));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.SelectByRoll(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.SelectByRoll(100));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<VillageDistanceBucket>)catalog.Buckets).Clear());
        }

        [Test]
        public void Reserve_AccumulatesMissingInputsWithoutRngDraw()
        {
            var rng = new DeterministicRngStream(10);

            var result = new VillageReservationSelector().Reserve(
                null, null, null, null, null, rng);

            Assert.That(result.Status, Is.EqualTo(VillageReservationStatus.InvalidInput));
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Approval, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Rejections, Is.Empty);
            Assert.That(rng.DrawCount, Is.Zero);
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                VillageReservationErrorCode.MissingCoreCapacityApproval));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                VillageReservationErrorCode.MissingVillageProfile));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                VillageReservationErrorCode.MissingVillageSpecialMap));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                VillageReservationErrorCode.MissingEntrySockets));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                VillageReservationErrorCode.MissingLayouts));
            Assert.That(result.Errors.Select(item => item.Code), Is.Ordered);
        }

        [Test]
        public void Reserve_MissingRngIsStructuralAndPublishesNoPartialOutput()
        {
            var result = new VillageReservationSelector().Reserve(
                StandardApproval, Starter.Profile, Starter.Village,
                Starter.EntrySockets, Starter.Layouts, null);

            Assert.That(result.Status, Is.EqualTo(VillageReservationStatus.InvalidInput));
            Assert.That(result.Errors.Select(item => item.Code), Is.EqualTo(
                new[] { VillageReservationErrorCode.MissingSiteRng }));
            Assert.That(result.Approval, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
        }

        [Test]
        public void Reserve_PublishesExactStarterCandidateConservationAndRngSchedule()
        {
            var rng = new DeterministicRngStream(4660);
            var before = rng.DrawCount;

            var result = Reserve(StandardApproval, rng);

            Assert.That(result.Succeeded, Is.True, Format(result));
            Assert.That(result.Diagnostics.RawEntryEvaluationCount, Is.EqualTo(676));
            Assert.That(result.Diagnostics.SourceCandidateCount, Is.EqualTo(624));
            Assert.That(result.Diagnostics.Layouts.Sum(item => item.EntryOutsideWorldCount),
                Is.EqualTo(52));
            Assert.That(result.Diagnostics.RngMethodCallCount, Is.EqualTo(3));
            Assert.That(result.Diagnostics.RngDrawCountBefore, Is.EqualTo(before));
            Assert.That(result.Diagnostics.RngDrawCountAfter, Is.EqualTo(rng.DrawCount));
            Assert.That(rng.DrawCount - before, Is.EqualTo(3));
            Assert.That(result.Diagnostics.Layouts.All(item =>
                item.EntryOutsideWorldCount + item.SourceCandidateCount ==
                item.RawEntryEvaluationCount), Is.True);
            Assert.That(result.Diagnostics.Layouts.All(item =>
                item.FootprintOverlapCount + item.ProtectedCoreWitnessCount +
                item.BlocksExistingEntryApproachCount + item.EntryApproachOccupiedCount +
                item.OtherSiteDistanceTooSmallCount +
                item.StartDistanceOutsideSelectedBucketCount + item.ViableCandidateCount ==
                item.SourceCandidateCount), Is.True);
        }

        [Test]
        public void Reserve_PreservesFrozenDefinitionIdentityAndWeights()
        {
            var result = Reserve(StandardApproval, new DeterministicRngStream(20));

            Assert.That(result.Succeeded, Is.True, Format(result));
            Assert.That(result.Approval.Village.Profile, Is.SameAs(Starter.Profile));
            Assert.That(result.Approval.Village.SpecialMap, Is.SameAs(Starter.Village));
            Assert.That(result.Approval.Village.EntryTemplate, Is.SameAs(Starter.EntrySockets[0]));
            Assert.That(result.Diagnostics.Layouts.Select(item => item.LayoutId),
                Is.EqualTo(new[] { Layout5Id, Layout6Id }));
            Assert.That(result.Diagnostics.Layouts.Select(item => item.SelectionWeight),
                Is.EqualTo(new[] { 100, 70 }));
            Assert.That(result.Approval.ExistingSiteCount, Is.EqualTo(6));
            Assert.That(result.Approval.CapacityWitnessCount, Is.EqualTo(4));
            Assert.That(result.Approval.TotalSelectedSiteCount, Is.EqualTo(7));
            Assert.That(result.Approval.CoreCapacityApproval, Is.SameAs(StandardApproval));
        }

        [Test]
        public void Reserve_ReversedCallerCollectionsAreCanonicalAndMutationIsolated()
        {
            var layouts = Starter.Layouts.Reverse().ToList();
            var entries = Starter.EntrySockets.Reverse().ToList();
            var first = new VillageReservationSelector().Reserve(
                StandardApproval, Starter.Profile, Starter.Village,
                entries, layouts, new DeterministicRngStream(123));
            layouts.Clear();
            entries.Clear();
            var second = Reserve(StandardApproval, new DeterministicRngStream(123));

            Assert.That(Snapshot(first), Is.EqualTo(Snapshot(second)));
            Assert.That(first.Diagnostics.Layouts, Has.Count.EqualTo(2));
        }

        [Test]
        public void Reserve_CultureAndReusedSelectorAreDeterministicForOneHundredRuns()
        {
            var selector = new VillageReservationSelector();
            var expected = Snapshot(Reserve(StandardApproval, new DeterministicRngStream(999)));
            var original = System.Threading.Thread.CurrentThread.CurrentCulture;
            var originalUi = System.Threading.Thread.CurrentThread.CurrentUICulture;
            try
            {
                for (var run = 0; run < 100; run++)
                {
                    var culture = run % 2 == 0 ? new CultureInfo("en-US") : new CultureInfo("tr-TR");
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    var actual = selector.Reserve(
                        StandardApproval, Starter.Profile, Starter.Village,
                        Starter.EntrySockets, Starter.Layouts,
                        new DeterministicRngStream(999));
                    Assert.That(Snapshot(actual), Is.EqualTo(expected), "run " + run);
                }
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = original;
                System.Threading.Thread.CurrentThread.CurrentUICulture = originalUi;
            }
        }

        [TestCaseSource(nameof(FullStarterSeeds))]
        public void FullStarter_ContinuedSiteStreamCompletesSixPlusOneWithExactWitnesses(ulong seed)
        {
            var rng = WorldSiteStream(seed);
            var search = new SiteReservationBacktracker().Search(
                Starter.FullGroups, Starter.Policy, SiteCandidateCostWeights.Default,
                SiteReservationSearchLimits.Default, rng);
            Assert.That(search.Succeeded, Is.True,
                string.Join("\n", search.Errors.Select(item => item.Message)));
            Assert.That(rng.DrawCount, Is.EqualTo(3156));
            var capacity = new CoreCapacityFloodChecker().Check(
                search.SelectionPlan, Requirements(search.SelectionPlan));
            Assert.That(capacity.Succeeded, Is.True, Format(capacity));
            var before = rng.DrawCount;

            var result = Reserve(capacity.Approval, rng);

            Assert.That(result.Succeeded, Is.True, Format(result));
            Assert.That(result.Approval.TotalSelectedSiteCount, Is.EqualTo(7));
            Assert.That(result.Approval.CapacityWitnessCount, Is.EqualTo(4));
            Assert.That(result.Approval.CoreCapacityApproval.TotalWitnessSectorCount, Is.EqualTo(20));
            Assert.That(result.Approval.Village.Candidate.OccupiedSectorIndices, Has.Count.EqualTo(1));
            Assert.That(result.Approval.Village.Candidate.OccupiedSectorIndices.Any(index =>
                search.SelectionPlan.SelectedPlacements.Any(placement => placement.OccupiedSectors.Any(
                    sector => WorldGridIndex.ToIndex(sector) == index))), Is.False);
            Assert.That(result.Approval.Village.Candidate.OccupiedSectorIndices.Any(index =>
                capacity.Approval.Witnesses.Any(witness => witness.ContainsWitnessSector(index))), Is.False);
            Assert.That(result.Diagnostics.RngDrawCountBefore, Is.EqualTo(before));
            Assert.That(rng.DrawCount - before, Is.EqualTo(3));
        }

        [Test]
        public void Result_SortsDeduplicatesAndProtectsErrorSnapshot()
        {
            var error = new VillageReservationError(
                VillageReservationErrorCode.InvalidLayout, Layout5Id, -1, "stable");
            var result = new VillageReservationResult(
                VillageReservationStatus.InvalidInput, null, null,
                Array.Empty<VillageReservationRejection>(), new[] { error, error });

            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(() => ((IList<VillageReservationError>)result.Errors).Add(error),
                Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void PublicSurface_IsImmutableAndHasNoUnityOrLaterTaskDependencies()
        {
            var types = new[]
            {
                typeof(VillageDistanceBucket), typeof(VillageDistanceBucketCatalog),
                typeof(VillageReservationCandidate), typeof(VillageLayoutCandidateDiagnostics),
                typeof(VillageReservationDiagnostics), typeof(VillageReservationRejection),
                typeof(VillageReservationError), typeof(VillageReservationSelection),
                typeof(VillageReservationApproval), typeof(VillageReservationResult),
                typeof(VillageReservationSelector)
            };
            foreach (var type in types)
            {
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.Static).Where(property => property.SetMethod != null &&
                    property.SetMethod.IsPublic), Is.Empty, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(field => !field.IsLiteral), Is.Empty, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
                Assert.That(type.FullName, Does.Not.Contain("Unity"));
            }
            Assert.That(types.Select(type => type.Assembly.GetName().Name).Distinct(),
                Is.EqualTo(new[] { "Game.Map.Runtime" }));
        }

        private static VillageReservationResult Reserve(
            CoreCapacityApproval approval,
            DeterministicRngStream rng) => new VillageReservationSelector().Reserve(
                approval, Starter.Profile, Starter.Village,
                Starter.EntrySockets, Starter.Layouts, rng);

        private static CoreCapacityApproval BuildStandardApproval()
        {
            var plan = SearchPlan(new[]
            {
                StartGroup(Option(SiteReservationKind.Start, WorldId, 0, 0)),
                SpecialGroup(BossId, SiteReservationKind.Boss,
                    Option(SiteReservationKind.Boss, BossId, 4, 0)),
                SpecialGroup(ForgeId, SiteReservationKind.Forge,
                    Option(SiteReservationKind.Forge, ForgeId, 2, 4)),
                SpecialGroup(CassiaId, SiteReservationKind.CoreResource,
                    Option(SiteReservationKind.CoreResource, CassiaId, 5, 4)),
                SpecialGroup(YeastId, SiteReservationKind.CoreResource,
                    Option(SiteReservationKind.CoreResource, YeastId, 8, 1)),
                SpecialGroup(MeteorId, SiteReservationKind.CoreResource,
                    Option(SiteReservationKind.CoreResource, MeteorId, 8, 8))
            });
            var result = new CoreCapacityFloodChecker().Check(plan, Requirements(plan));
            if (!result.Succeeded) throw new InvalidOperationException(Format(result));
            return result.Approval;
        }

        private static SiteReservationSelectionPlan SearchPlan(
            IEnumerable<SiteReservationSearchGroup> groups)
        {
            var result = new SiteReservationBacktracker().Search(
                groups, Starter.Policy, SiteCandidateCostWeights.Default,
                SiteReservationSearchLimits.Default, new DeterministicRngStream(10));
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("\n",
                    result.Errors.Select(item => item.Code + ":" + item.Message)));
            return result.SelectionPlan;
        }

        private static IReadOnlyList<CoreCapacityRequirement> Requirements(
            SiteReservationSelectionPlan plan)
        {
            var result = new List<CoreCapacityRequirement>();
            foreach (var index in new[] { 2, 3, 4, 5 })
            {
                var placement = plan.SelectedPlacements[index];
                var key = SitePlacementKey.FromPlacement(placement);
                var special = Starter.SpecialDefinitions.SpecialMaps[key.SourceDefinitionId];
                var biome = Starter.Biomes[special.PrimaryBiomeId];
                result.Add(new CoreCapacityRequirement(
                    key, placement, special, biome, Starter.CoreRules[biome.BiomeId]));
            }
            return new ReadOnlyCollection<CoreCapacityRequirement>(result);
        }

        private static SiteReservationSearchGroup StartGroup(
            params SiteReservationSearchOption[] options) => new SiteReservationSearchGroup(
                Key(SiteReservationKind.Start, WorldId), null, null, null, options);

        private static SiteReservationSearchGroup SpecialGroup(
            string sourceId,
            SiteReservationKind kind,
            params SiteReservationSearchOption[] options)
        {
            var special = Starter.SpecialDefinitions.SpecialMaps[sourceId];
            var biome = Starter.Biomes[special.PrimaryBiomeId];
            return new SiteReservationSearchGroup(
                Key(kind, sourceId), special, biome, Starter.CoreRules[biome.BiomeId], options);
        }

        private static SiteReservationSearchOption Option(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y) => new SiteReservationSearchOption(Single(kind, sourceId, x, y), -1);

        private static FootprintPlacement Single(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y)
        {
            var origin = new SectorCoord(x, y);
            var candidate = new SiteOriginCandidate(
                kind, sourceId, 0, origin, WorldGridIndex.ToIndex(origin), EdgeRing(origin), 0);
            var footprint = new SiteFootprint(1, 1, SiteFootprintTransform.R0,
                new[] { new SiteFootprintCell(0, 0, "CELL", string.Empty, string.Empty,
                    Array.Empty<SiteEntrySide>()) });
            return new FootprintPlacement(candidate, footprint,
                new[] { origin }, Array.Empty<FootprintPlacementEntry>());
        }

        private static IReadOnlyList<SiteReservationSearchGroup> BuildFullGroups(StarterData data)
        {
            var solver = new FootprintPlacementSolver();
            var startOptions = new List<SiteReservationSearchOption>();
            var ordinal = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var origin = WorldGridIndex.ToCoordinate(index);
                var ring = EdgeRing(origin);
                if (ring > 1) continue;
                var candidate = new SiteOriginCandidate(
                    SiteReservationKind.Start, WorldId, 0, origin, index, ring, ordinal++);
                var placement = solver.SolveStart(candidate, FootprintPlacementBlockers.Empty);
                if (!placement.Succeeded) throw new InvalidOperationException("Start solve failed.");
                startOptions.Add(new SiteReservationSearchOption(placement.Placement, -1));
            }
            var groups = new List<SiteReservationSearchGroup>
            {
                new SiteReservationSearchGroup(Key(SiteReservationKind.Start, WorldId),
                    null, null, null, startOptions)
            };
            AddFullGroup(groups, data, solver, BossId, SiteReservationKind.Boss);
            AddFullGroup(groups, data, solver, ForgeId, SiteReservationKind.Forge);
            AddFullGroup(groups, data, solver, CassiaId, SiteReservationKind.CoreResource);
            AddFullGroup(groups, data, solver, YeastId, SiteReservationKind.CoreResource);
            AddFullGroup(groups, data, solver, MeteorId, SiteReservationKind.CoreResource);
            return new ReadOnlyCollection<SiteReservationSearchGroup>(groups);
        }

        private static void AddFullGroup(
            ICollection<SiteReservationSearchGroup> groups,
            StarterData data,
            FootprintPlacementSolver solver,
            string sourceId,
            SiteReservationKind kind)
        {
            var options = new List<SiteReservationSearchOption>();
            var special = data.SpecialDefinitions.SpecialMaps[sourceId];
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
                    var placement = solver.SolveSpecialSite(candidate, transform, special,
                        cells, entries, FootprintPlacementBlockers.Empty);
                    if (placement.Succeeded)
                        options.Add(new SiteReservationSearchOption(placement.Placement, -1));
                }
            }
            var biome = data.Biomes[special.PrimaryBiomeId];
            groups.Add(new SiteReservationSearchGroup(
                Key(kind, sourceId), special, biome, data.CoreRules[biome.BiomeId],
                options.ToArray()));
        }

        private static SitePlacementKey Key(SiteReservationKind kind, string sourceId) =>
            new SitePlacementKey(kind, sourceId, 0);

        private static int EdgeRing(SectorCoord coordinate) => Math.Min(
            Math.Min(coordinate.X, WorldGenConstants.SectorColumns - 1 - coordinate.X),
            Math.Min(coordinate.Y, WorldGenConstants.SectorRows - 1 - coordinate.Y));

        private static StarterData BuildStarterData()
        {
            var biomeRows = new List<string[]>
            {
                BiomeRow(CraterBiomeId, 0, 7), BiomeRow(CassiaBiomeId, 2, 12),
                BiomeRow(MillBiomeId, 1, 11), BiomeRow(DoughBiomeId, 0, 7)
            };
            var patchRows = new List<string[]>
            {
                PatchRow("PATCH_CRATER_CORE", CraterBiomeId, 5, 18, true),
                PatchRow("PATCH_ROOT_CORE", CassiaBiomeId, 5, 18, false),
                PatchRow("PATCH_MILL_CORE", MillBiomeId, 4, 14, false),
                PatchRow("PATCH_DOUGH_CORE", DoughBiomeId, 5, 18, true)
            };
            var biomeResult = new BiomeBoundaryDefinitionBuilder().Build(
                BiomeSpecs.Select(spec => BuildBiomeSource(spec,
                    spec.FileName == "biome_types.csv" ? biomeRows :
                    spec.FileName == "biome_patch_rules.csv" ? patchRows : null)));
            if (!biomeResult.Success)
                throw new InvalidOperationException(string.Join("\n", biomeResult.Errors));

            var rowsByFile = SpecialRows();
            var specialResult = new SpecialVillageDefinitionBuilder().Build(
                SpecialSpecs.Select(spec => BuildSpecialSource(spec,
                    rowsByFile.TryGetValue(spec.FileName, out var rows) ? rows : null)));
            if (!specialResult.Success)
                throw new InvalidOperationException(string.Join("\n", specialResult.Errors));
            var policyResult = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(
                WorldId, specialResult.DefinitionSet.SpecialMaps.Values);
            if (!policyResult.Succeeded)
                throw new InvalidOperationException(string.Join("\n", policyResult.Errors));

            var result = new StarterData(
                biomeResult.DefinitionSet.BiomeTypes,
                biomeResult.DefinitionSet.BiomePatchRules.Values.ToDictionary(
                    item => item.BiomeId, StringComparer.Ordinal),
                specialResult.DefinitionSet, policyResult.Policy);
            result.FullGroups = BuildFullGroupsFor(result);
            return result;
        }

        private static IReadOnlyList<SiteReservationSearchGroup> BuildFullGroupsFor(StarterData data)
        {
            return BuildFullGroups(data);
        }

        private static Dictionary<string, IReadOnlyList<string[]>> SpecialRows() =>
            new Dictionary<string, IReadOnlyList<string[]>>(StringComparer.Ordinal)
            {
                { "special_map_catalog.csv", new[]
                    {
                        SpecialRow(BossId, "BOSS", MillBiomeId, 2, 1, 4, 2, "FIXED"),
                        SpecialRow(ForgeId, "FORGE", MillBiomeId, 1, 1, 2, 2, "FIXED"),
                        SpecialRow(CassiaId, "CORE_RESOURCE", CassiaBiomeId, 1, 1, 2, 3, "FIXED"),
                        SpecialRow(YeastId, "CORE_RESOURCE", DoughBiomeId, 1, 1, 2, 3, "FIXED"),
                        SpecialRow(MeteorId, "CORE_RESOURCE", CraterBiomeId, 1, 1, 2, 3, "FIXED"),
                        SpecialRow(VillageId, "VILLAGE", string.Empty, 1, 1, 0, 2, "VILLAGE_LAYOUT")
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
                        EntryRow(YeastId), EntryRow(MeteorId), EntryRow(VillageId)
                    }
                },
                { "village_layout_catalog.csv", new[]
                    {
                        LayoutRow(Layout5Id, 5, 100), LayoutRow(Layout6Id, 6, 70)
                    }
                },
                { "village_profiles.csv", new[] { ProfileRow() } }
            };

        private static string[] BiomeRow(string id, int minimumY, int maximumY) => new[]
        {
            id, "Biome", "STAGE_MOON", "1", "1", "169", "1",
            minimumY.ToString(CultureInfo.InvariantCulture),
            maximumY.ToString(CultureInfo.InvariantCulture), "1.0", "THEME_MOON",
            "AUDIO_MOON", "MICRO_MOON", "RECIPE_MOON", "RESOURCE_MOON",
            "ELEMENT_MOON", "SITE_REQUIRED", "1", "test"
        };

        private static string[] PatchRow(
            string ruleId, string biomeId, int minimum, int maximum, bool edge) => new[]
        {
            ruleId, biomeId, "CORE", minimum.ToString(CultureInfo.InvariantCulture),
            maximum.ToString(CultureInfo.InvariantCulture), "1", "1", "1", "1.0",
            edge ? "1" : "0", "1", "1", "1.0", "1.0", "1.0", "1.0",
            "1.0", "1.0", "1", "test"
        };

        private static string[] SpecialRow(
            string id, string role, string biome, int width, int height,
            int startDistance, int otherDistance, string mode) => new[]
        {
            id, "Site", role, biome, width.ToString(CultureInfo.InvariantCulture),
            height.ToString(CultureInfo.InvariantCulture), "1",
            startDistance.ToString(CultureInfo.InvariantCulture),
            otherDistance.ToString(CultureInfo.InvariantCulture),
            "1|2|3", "0", role == "VILLAGE" ? string.Empty : "REWARD_NONE",
            mode, "1", "test"
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

        private static string[] LayoutRow(string id, int facilities, int weight) => new[]
        {
            id, "Layout", "1", "1", facilities.ToString(CultureInfo.InvariantCulture),
            "L|R", weight.ToString(CultureInfo.InvariantCulture), "1", "test"
        };

        private static string[] ProfileRow() => new[]
        {
            VillageProfileId, "Village", WorldId, "5", "6",
            "FACILITY_FIXED", "FACILITY_OPTIONAL", Layout5Id + "|" + Layout6Id,
            "2-3:20|4-6:50|7-10:30", "2", "1", "test"
        };

        private static BiomeBoundaryDefinitionSource BuildBiomeSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var catalog = new CsvSchemaCatalogBuilder().Build(SchemaRows(spec));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(BuildCsv(spec,
                    rows ?? new[] { StandardRow(spec) })), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!parsed.Success) throw new InvalidOperationException(string.Join("\n", parsed.Errors));
            return new BiomeBoundaryDefinitionSource(schema, parsed);
        }

        private static SpecialVillageDefinitionSource BuildSpecialSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var catalog = new CsvSchemaCatalogBuilder().Build(SchemaRows(spec));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(BuildCsv(spec,
                    rows ?? new[] { StandardRow(spec) })), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!parsed.Success) throw new InvalidOperationException(string.Join("\n", parsed.Errors));
            return new SpecialVillageDefinitionSource(schema, parsed);
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
            File("special_map_catalog.csv", 1, "special_map_id:ID", "display_name_ko:STRING", "site_role:ENUM:BOSS|FORGE|CORE_RESOURCE|VILLAGE", "primary_biome_id:ID", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "required_count:INT", "min_graph_distance_from_start:INT", "min_graph_distance_to_other_core_sites:INT", "allowed_entry_route_types:INT_LIST", "requires_tool:BOOL", "mandatory_reward_id:ID", "generation_mode:ENUM:FIXED|VILLAGE_LAYOUT", "active:BOOL", "notes:STRING"),
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

        private static FileSpec File(
            string name,
            int primaryKeyCount,
            params string[] definitions) => new FileSpec(
                name, primaryKeyCount, definitions.Select(definition =>
                {
                    var parts = definition.Split(':');
                    var allowed = parts.Length > 2 ? parts[2] :
                        (parts[1] == "ENUM" || parts[1] == "ENUM_LIST"
                            ? "ENUM_A|ENUM_B" : string.Empty);
                    return new ColumnSpec(parts[0], parts[1], allowed);
                }).ToArray());

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
            foreach (var item in bytes) target.Add(item);
        }

        private static void AppendU64(ICollection<byte> target, ulong value)
        {
            for (var shift = 56; shift >= 0; shift -= 8) target.Add((byte)(value >> shift));
        }

        private static string Snapshot(VillageReservationResult result)
        {
            if (!result.Succeeded) return Format(result);
            var selection = result.Approval.Village;
            var candidate = selection.Candidate;
            return result.Status + "|" + selection.DistanceBucket.BucketOrdinal + "|" +
                   selection.Layout.VillageLayoutId + "|" + candidate.OriginIndex + "|" +
                   candidate.EntrySide + "|" + candidate.CandidateOrdinal + "|" +
                   result.Diagnostics.BucketRoll + "|" + result.Diagnostics.LayoutRoll + "|" +
                   result.Diagnostics.CandidateRoll + "|" + result.Diagnostics.RngDrawCountAfter;
        }

        private static string Format(VillageReservationResult result) =>
            string.Join("\n", result.Errors.Select(item => item.Code + ":" + item.Message)
                .Concat(result.Rejections.Select(item => item.Reason + ":" + item.Message)));

        private static string Format(CoreCapacityFloodResult result) =>
            string.Join("\n", result.Errors.Select(item => item.Code + ":" + item.Message)
                .Concat(result.Rejections.Select(item => item.Reason + ":" + item.Message)));

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
            public SiteDistancePolicy Policy { get; }
            public IReadOnlyList<SiteReservationSearchGroup> FullGroups { get; set; }
            public VillageProfileDefinition Profile =>
                SpecialDefinitions.VillageProfiles[VillageProfileId];
            public SpecialMapDefinition Village => SpecialDefinitions.SpecialMaps[VillageId];
            public IReadOnlyList<SpecialMapEntrySocketDefinition> EntrySockets =>
                SpecialDefinitions.GetSpecialMapEntrySockets(VillageId);
            public IReadOnlyList<VillageLayoutDefinition> Layouts =>
                new[]
                {
                    SpecialDefinitions.VillageLayouts[Layout5Id],
                    SpecialDefinitions.VillageLayouts[Layout6Id]
                };
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
