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
    public sealed class CoreCapacityFloodCheckerTests
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

        private static readonly FileSpec[] BiomeSpecs = CreateBiomeSpecs();
        private static readonly FileSpec[] SpecialSpecs = CreateSpecialSpecs();
        private static readonly StarterData Starter = BuildStarterData(1, true);
        private static readonly StarterData ZeroBuffer = BuildStarterData(0, false);
        private static readonly StarterData WideBuffer = BuildStarterData(2, false);
        private static readonly StarterData DisjointPressure = BuildStarterData(0, false, 42);
        private static readonly StarterData ConnectedPressure = BuildStarterData(0, false, 169);

        public static IEnumerable WorldIndexCases()
        {
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                yield return new TestCaseData(index).SetName("WorldGrid_Cardinal_" + index);
        }

        public static IEnumerable ErrorCodeCases()
        {
            yield return new TestCaseData(CoreCapacityFloodErrorCode.MissingSelectionPlan, 0);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.InvalidSelectionPlan, 1);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.MissingRequirements, 2);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.NullRequirement, 3);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.DuplicateRequirementKey, 4);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.MissingRequiredRequirement, 5);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.UnexpectedRequirement, 6);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.InvalidRequirement, 7);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.PlacementNotSelected, 8);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.PlacementIdentityMismatch, 9);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.MissingSpecialMap, 10);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.InvalidSpecialMap, 11);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.MissingPrimaryBiome, 12);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.InvalidPrimaryBiome, 13);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.MissingCorePatchRule, 14);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.InvalidCorePatchRule, 15);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.DefinitionIdentityMismatch, 16);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.InvalidFootprint, 17);
            yield return new TestCaseData(CoreCapacityFloodErrorCode.InternalInvariantViolation, 18);
        }

        public static IEnumerable RejectionReasonCases()
        {
            yield return new TestCaseData(CoreCapacityFloodRejectionReason.BufferOutsideWorld, 0);
            yield return new TestCaseData(
                CoreCapacityFloodRejectionReason.BufferBlockedBySelectedFootprint, 1);
            yield return new TestCaseData(CoreCapacityFloodRejectionReason.MandatoryBufferOverlap, 2);
            yield return new TestCaseData(
                CoreCapacityFloodRejectionReason.InsufficientConnectedCapacity, 3);
            yield return new TestCaseData(
                CoreCapacityFloodRejectionReason.InsufficientDisjointCapacity, 4);
        }

        public static IEnumerable StatusCases()
        {
            yield return new TestCaseData(CoreCapacityFloodStatus.Completed, 0);
            yield return new TestCaseData(CoreCapacityFloodStatus.CapacityRejected, 1);
            yield return new TestCaseData(CoreCapacityFloodStatus.InvalidInput, 2);
        }

        public static IEnumerable FullStarterSeeds()
        {
            yield return new TestCaseData(0UL).SetName("FullStarter_Seed_0");
            yield return new TestCaseData(4660UL).SetName("FullStarter_Seed_4660");
            yield return new TestCaseData(ulong.MaxValue).SetName("FullStarter_Seed_Max");
        }

        [TestCaseSource(nameof(WorldIndexCases))]
        public void WorldGrid_UsesExactIndexAndCardinalNeighbors(int index)
        {
            var coordinate = WorldGridIndex.ToCoordinate(index);
            Assert.That(WorldGridIndex.ToIndex(coordinate), Is.EqualTo(index));
            var neighbors = new[]
            {
                WorldGridIndex.GetLeftIndex(index), WorldGridIndex.GetRightIndex(index),
                WorldGridIndex.GetUpIndex(index), WorldGridIndex.GetDownIndex(index)
            }.Where(value => value >= 0).ToArray();
            Assert.That(neighbors, Is.Unique);
            Assert.That(neighbors.All(value =>
            {
                var other = WorldGridIndex.ToCoordinate(value);
                return Math.Abs(other.X - coordinate.X) + Math.Abs(other.Y - coordinate.Y) == 1;
            }), Is.True);
        }

        [TestCaseSource(nameof(ErrorCodeCases))]
        public void ErrorCode_UsesFrozenOrdinal(CoreCapacityFloodErrorCode value, int ordinal)
        {
            Assert.That((int)value, Is.EqualTo(ordinal));
            Assert.That(new CoreCapacityFloodError(
                value, string.Empty, string.Empty, string.Empty, -1, "stable").Code,
                Is.EqualTo(value));
        }

        [TestCaseSource(nameof(RejectionReasonCases))]
        public void RejectionReason_UsesFrozenOrdinal(
            CoreCapacityFloodRejectionReason value,
            int ordinal)
        {
            Assert.That((int)value, Is.EqualTo(ordinal));
        }

        [TestCaseSource(nameof(StatusCases))]
        public void Status_UsesFrozenOrdinal(CoreCapacityFloodStatus value, int ordinal)
        {
            Assert.That((int)value, Is.EqualTo(ordinal));
        }

        [Test]
        public void Requirement_PreservesEnvelopeReferences()
        {
            var plan = StandardPlan();
            var placement = plan.SelectedPlacements[2];
            var special = Starter.SpecialMaps[ForgeId];
            var biome = Starter.Biomes[special.PrimaryBiomeId];
            var rule = Starter.CoreRules[biome.BiomeId];
            var requirement = new CoreCapacityRequirement(
                Key(SiteReservationKind.Forge, ForgeId), placement, special, biome, rule);

            Assert.That(requirement.Placement, Is.SameAs(placement));
            Assert.That(requirement.SpecialMap, Is.SameAs(special));
            Assert.That(requirement.PrimaryBiome, Is.SameAs(biome));
            Assert.That(requirement.CorePatchRule, Is.SameAs(rule));
        }

        [Test]
        public void Check_AccumulatesMissingStructuralInputs()
        {
            var result = new CoreCapacityFloodChecker().Check(null, null);

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.InvalidInput));
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Approval, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Rejections, Is.Empty);
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(
                CoreCapacityFloodErrorCode.MissingSelectionPlan));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(
                CoreCapacityFloodErrorCode.MissingRequirements));
        }

        [Test]
        public void Check_AccumulatesSortedNullDuplicateMissingAndUnexpectedRequirements()
        {
            var plan = StandardPlan();
            var requirements = Requirements(plan, Starter).Take(3).ToList();
            requirements.Add(requirements[0]);
            requirements.Add(null);
            requirements.Add(new CoreCapacityRequirement(
                Key(SiteReservationKind.Village, "SITE_TEST_VILLAGE"),
                null, null, null, null));

            var result = new CoreCapacityFloodChecker().Check(plan, requirements);

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.InvalidInput));
            Assert.That(result.Approval, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Rejections, Is.Empty);
            var codes = result.Errors.Select(error => error.Code).ToArray();
            Assert.That(codes, Does.Contain(CoreCapacityFloodErrorCode.NullRequirement));
            Assert.That(codes, Does.Contain(CoreCapacityFloodErrorCode.DuplicateRequirementKey));
            Assert.That(codes, Does.Contain(CoreCapacityFloodErrorCode.MissingRequiredRequirement));
            Assert.That(codes, Does.Contain(CoreCapacityFloodErrorCode.UnexpectedRequirement));
            Assert.That(codes, Is.Ordered);
        }

        [Test]
        public void Check_RejectsEquivalentKeyWithDifferentSelectedPlacement()
        {
            var plan = StandardPlan();
            var requirements = Requirements(plan, Starter).ToList();
            var original = requirements[0];
            requirements[0] = new CoreCapacityRequirement(
                original.Key, requirements[1].Placement,
                original.SpecialMap, original.PrimaryBiome, original.CorePatchRule);

            var result = new CoreCapacityFloodChecker().Check(plan, requirements);

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.InvalidInput));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(
                CoreCapacityFloodErrorCode.PlacementIdentityMismatch));
            Assert.That(result.Approval, Is.Null);
        }

        [Test]
        public void Check_ZeroBufferIncludesFootprintAndUsesRuleMinimum()
        {
            var plan = StandardPlan();
            var result = new CoreCapacityFloodChecker().Check(
                plan, Requirements(plan, ZeroBuffer));

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.Completed),
                FormatErrors(result));
            Assert.That(result.Approval.Witnesses.Select(item =>
                item.MandatoryBufferSectorIndices.Count), Is.All.EqualTo(1));
            Assert.That(result.Approval.Witnesses.Select(item =>
                item.RequiredWitnessSectorCount), Is.EqualTo(new[] { 4, 5, 5, 5 }));
            Assert.That(result.Approval.Witnesses.Select(item =>
                item.AdditionalClaimedSectorCount), Is.EqualTo(new[] { 3, 4, 4, 4 }));
            Assert.That(result.Approval.TotalWitnessSectorCount, Is.EqualTo(19));
        }

        [Test]
        public void Check_EdgeTouchTruncatesBufferAndAddsConnectedCapacity()
        {
            var plan = EdgeTouchPlan();
            var result = new CoreCapacityFloodChecker().Check(
                plan, Requirements(plan, Starter));

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.Completed),
                FormatErrors(result));
            Assert.That(result.Approval.TryGetWitness(
                Key(SiteReservationKind.CoreResource, YeastId), out var witness), Is.True);
            Assert.That(witness.CanTouchWorldEdge, Is.True);
            Assert.That(witness.MandatoryBufferSectorIndices.Count, Is.EqualTo(4));
            Assert.That(witness.RequiredWitnessSectorCount, Is.EqualTo(5));
            Assert.That(witness.AdditionalClaimedSectorCount, Is.EqualTo(1));
            Assert.That(WitnessIsConnected(witness), Is.True);
        }

        [Test]
        public void Check_IndependentCapacityShortfallUsesReachableSectorCount()
        {
            var plan = StandardPlan();
            var result = new CoreCapacityFloodChecker().Check(
                plan, Requirements(plan, ConnectedPressure));

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.CapacityRejected));
            Assert.That(result.Rejections, Has.Some.Property("Reason").EqualTo(
                CoreCapacityFloodRejectionReason.InsufficientConnectedCapacity));
            Assert.That(result.Approval, Is.Null);
            foreach (var site in result.Diagnostics.Sites)
            {
                Assert.That(site.WitnessSectorCount, Is.Zero);
                Assert.That(site.AvailableConnectedSectorCount,
                    Is.LessThan(site.RequiredWitnessSectorCount));
                Assert.That(site.CapacityShortfall, Is.EqualTo(
                    site.RequiredWitnessSectorCount - site.AvailableConnectedSectorCount));
            }
        }

        [Test]
        public void Check_DisjointCapacityFailurePublishesNoPartialApproval()
        {
            var plan = StandardPlan();
            var result = new CoreCapacityFloodChecker().Check(
                plan, Requirements(plan, DisjointPressure));

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.CapacityRejected));
            Assert.That(result.RetryRequired, Is.True);
            Assert.That(result.Approval, Is.Null);
            Assert.That(result.Errors, Is.Empty);
            var rejection = result.Rejections.Single(item => item.Reason ==
                CoreCapacityFloodRejectionReason.InsufficientDisjointCapacity);
            var failedIndex = result.Diagnostics.Sites.Select((site, index) => new { site, index })
                .Single(item => item.site.Key == rejection.Key).index;
            Assert.That(result.Diagnostics.Sites.All(site =>
                site.AvailableConnectedSectorCount >= site.RequiredWitnessSectorCount), Is.True);
            Assert.That(result.Diagnostics.Sites.Take(failedIndex).All(site =>
                site.WitnessSectorCount == site.RequiredWitnessSectorCount), Is.True);
            Assert.That(result.Diagnostics.Sites[failedIndex].WitnessSectorCount,
                Is.LessThan(result.Diagnostics.Sites[failedIndex].RequiredWitnessSectorCount));
            Assert.That(result.Diagnostics.Sites.Skip(failedIndex + 1).All(site =>
                site.WitnessSectorCount == site.MandatoryBufferSectorCount), Is.True);
            Assert.That(result.Diagnostics.Sites[failedIndex].CapacityShortfall,
                Is.EqualTo(rejection.Shortfall));
        }

        [Test]
        public void Result_SortsDeduplicatesAndProtectsErrorSnapshot()
        {
            var error = new CoreCapacityFloodError(
                CoreCapacityFloodErrorCode.InvalidRequirement,
                ForgeId, MillBiomeId, "PATCH_MILL_CORE", -1, "stable");
            var result = new CoreCapacityFloodResult(
                CoreCapacityFloodStatus.InvalidInput, null, null,
                Array.Empty<CoreCapacityFloodRejection>(), new[] { error, error });

            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(() => ((IList<CoreCapacityFloodError>)result.Errors).Add(error),
                Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void Check_CompletedPlanPublishesExactDisjointWitnesses()
        {
            var plan = StandardPlan();
            var result = new CoreCapacityFloodChecker().Check(
                plan, Requirements(plan, Starter));

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.Completed),
                FormatErrors(result));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Rejections, Is.Empty);
            Assert.That(result.Approval.SelectionPlan, Is.SameAs(plan));
            Assert.That(result.Approval.CapacitySiteCount, Is.EqualTo(4));
            Assert.That(result.Approval.TotalWitnessSectorCount, Is.EqualTo(20));
            Assert.That(result.Approval.Witnesses.Select(witness => witness.Key.SourceDefinitionId),
                Is.EqualTo(new[] { ForgeId, CassiaId, YeastId, MeteorId }));
            Assert.That(result.Approval.Witnesses.Select(witness =>
                witness.RequiredWitnessSectorCount), Is.EqualTo(new[] { 5, 5, 5, 5 }));
            Assert.That(result.Approval.Witnesses.Select(witness =>
                witness.MinimumCoreSectorCount), Is.EqualTo(new[] { 4, 5, 5, 5 }));
            Assert.That(result.Approval.Witnesses.All(WitnessIsConnected), Is.True);

            var claimed = new HashSet<int>();
            foreach (var witness in result.Approval.Witnesses)
            {
                Assert.That(witness.FootprintSectorIndices,
                    Is.SubsetOf(witness.MandatoryBufferSectorIndices));
                Assert.That(witness.MandatoryBufferSectorIndices,
                    Is.SubsetOf(witness.WitnessSectorIndices));
                Assert.That(witness.WitnessSectorIndices.Count, Is.EqualTo(5));
                foreach (var sector in witness.WitnessSectorIndices)
                    Assert.That(claimed.Add(sector), Is.True, "cross-witness overlap " + sector);
            }
            Assert.That(result.Diagnostics.SelectedPlacementCount, Is.EqualTo(6));
            Assert.That(result.Diagnostics.CapacitySiteCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.TotalWitnessSectorCount, Is.EqualTo(20));
        }

        [Test]
        public void Check_ReversedRequirementListHasIdenticalOutput()
        {
            var plan = StandardPlan();
            var requirements = Requirements(plan, Starter);
            var checker = new CoreCapacityFloodChecker();
            var first = checker.Check(plan, requirements);
            var second = checker.Check(plan, requirements.Reverse().ToArray());

            Assert.That(Snapshot(second), Is.EqualTo(Snapshot(first)));
        }

        [Test]
        public void Check_CultureAndCallerCollectionMutationDoNotChangeSnapshots()
        {
            var plan = StandardPlan();
            var requirements = Requirements(plan, Starter).ToList();
            var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var originalUiCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                var first = new CoreCapacityFloodChecker().Check(plan, requirements);
                var expected = Snapshot(first);
                requirements.Reverse();
                System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("tr-TR");
                var second = new CoreCapacityFloodChecker().Check(plan, requirements);
                requirements.Clear();

                Assert.That(Snapshot(second), Is.EqualTo(expected));
                Assert.That(Snapshot(first), Is.EqualTo(expected));
                Assert.That(first.Approval.Witnesses, Has.Count.EqualTo(4));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void Check_WideMandatoryBuffersReportBothOverlapOwners()
        {
            var plan = StandardPlan();
            var result = new CoreCapacityFloodChecker().Check(
                plan, Requirements(plan, WideBuffer));

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.CapacityRejected));
            Assert.That(result.RetryRequired, Is.True);
            Assert.That(result.Approval, Is.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Rejections.Select(item => item.Reason), Does.Contain(
                CoreCapacityFloodRejectionReason.MandatoryBufferOverlap));
            var overlapKeys = result.Rejections.Where(item =>
                    item.Reason == CoreCapacityFloodRejectionReason.MandatoryBufferOverlap)
                .Select(item => item.Key.SourceDefinitionId).Distinct().ToArray();
            Assert.That(overlapKeys.Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Check_NonEdgeCoreBufferOutsideWorldRequiresRetry()
        {
            var plan = EdgePlan();
            var result = new CoreCapacityFloodChecker().Check(
                plan, Requirements(plan, Starter));

            Assert.That(result.Status, Is.EqualTo(CoreCapacityFloodStatus.CapacityRejected));
            Assert.That(result.RetryRequired, Is.True);
            Assert.That(result.Rejections.Any(item =>
                item.Key.SourceDefinitionId == ForgeId &&
                item.Reason == CoreCapacityFloodRejectionReason.BufferOutsideWorld), Is.True);
            Assert.That(result.Approval, Is.Null);
        }

        [TestCaseSource(nameof(FullStarterSeeds))]
        public void FullStarter_ThreeSeedsApproveFourWitnessesWithoutRngDraws(ulong seed)
        {
            var rng = WorldSiteStream(seed);
            var search = new SiteReservationBacktracker().Search(
                Starter.FullGroups, Starter.Policy, SiteCandidateCostWeights.Default,
                SiteReservationSearchLimits.Default, rng);
            Assert.That(search.Succeeded, Is.True,
                string.Join("\n", search.Errors.Select(error => error.Message)));
            var before = rng.DrawCount;

            var result = new CoreCapacityFloodChecker().Check(
                search.SelectionPlan, Requirements(search.SelectionPlan, Starter));

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Approval.CapacitySiteCount, Is.EqualTo(4));
            Assert.That(result.Approval.TotalWitnessSectorCount, Is.EqualTo(20));
            Assert.That(result.Approval.Witnesses.Select(item => item.WitnessSectorIndices.Count),
                Is.All.EqualTo(5));
            var checkedExteriorCount = 0;
            foreach (var witness in result.Approval.Witnesses)
            {
                var placement = search.SelectionPlan.SelectedPlacements.First(item =>
                    SitePlacementKey.FromPlacement(item) == witness.Key);
                foreach (var entry in placement.Entries)
                {
                    var exterior = WorldGridIndex.ToIndex(entry.ExteriorSector);
                    var blockedByOther = search.SelectionPlan.SelectedPlacements.Any(other =>
                        SitePlacementKey.FromPlacement(other) != witness.Key &&
                        other.OccupiedSectors.Any(cell => WorldGridIndex.ToIndex(cell) == exterior));
                    if (blockedByOther) continue;
                    checkedExteriorCount++;
                    Assert.That(witness.ReachableSectorIndices, Does.Contain(exterior));
                }
            }
            Assert.That(checkedExteriorCount, Is.GreaterThan(0));
            Assert.That(rng.DrawCount, Is.EqualTo(before));
            Assert.That(before, Is.EqualTo(3156));
        }

        [Test]
        public void Check_FreshAndReusedCheckerAreIdenticalAcrossOneHundredRuns()
        {
            var plan = StandardPlan();
            var requirements = Requirements(plan, Starter);
            var expected = Snapshot(new CoreCapacityFloodChecker().Check(plan, requirements));
            var reused = new CoreCapacityFloodChecker();
            for (var run = 0; run < 100; run++)
                Assert.That(Snapshot(reused.Check(plan, requirements)),
                    Is.EqualTo(expected), "run " + run);
        }

        [Test]
        public void PublicSurface_IsImmutableAndHasNoUnityOrLaterTaskDependencies()
        {
            var types = new[]
            {
                typeof(CoreCapacityRequirement), typeof(CoreCapacityFloodWitness),
                typeof(CoreCapacityApproval), typeof(CoreCapacitySiteDiagnostics),
                typeof(CoreCapacityFloodDiagnostics), typeof(CoreCapacityFloodRejection),
                typeof(CoreCapacityFloodError), typeof(CoreCapacityFloodResult),
                typeof(CoreCapacityFloodChecker)
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
            var assemblyNames = types.Select(type => type.Assembly.GetName().Name).Distinct();
            Assert.That(assemblyNames, Is.EqualTo(new[] { "Game.Map.Runtime" }));
        }

        private static SiteReservationSelectionPlan StandardPlan() => SearchPlan(new[]
        {
            StartGroup(Option(SiteReservationKind.Start, WorldId, 0, 0)),
            SpecialGroup(Starter, BossId, SiteReservationKind.Boss,
                Option(SiteReservationKind.Boss, BossId, 4, 0)),
            SpecialGroup(Starter, ForgeId, SiteReservationKind.Forge,
                Option(SiteReservationKind.Forge, ForgeId, 2, 4)),
            SpecialGroup(Starter, CassiaId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, CassiaId, 5, 4)),
            SpecialGroup(Starter, YeastId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, YeastId, 8, 1)),
            SpecialGroup(Starter, MeteorId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, MeteorId, 8, 8))
        });

        private static SiteReservationSelectionPlan EdgePlan() => SearchPlan(new[]
        {
            StartGroup(Option(SiteReservationKind.Start, WorldId, 0, 0)),
            SpecialGroup(Starter, BossId, SiteReservationKind.Boss,
                Option(SiteReservationKind.Boss, BossId, 4, 0)),
            SpecialGroup(Starter, ForgeId, SiteReservationKind.Forge,
                Option(SiteReservationKind.Forge, ForgeId, 0, 4)),
            SpecialGroup(Starter, CassiaId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, CassiaId, 4, 4)),
            SpecialGroup(Starter, YeastId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, YeastId, 8, 0)),
            SpecialGroup(Starter, MeteorId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, MeteorId, 8, 8))
        });

        private static SiteReservationSelectionPlan EdgeTouchPlan() => SearchPlan(new[]
        {
            StartGroup(Option(SiteReservationKind.Start, WorldId, 0, 0)),
            SpecialGroup(Starter, BossId, SiteReservationKind.Boss,
                Option(SiteReservationKind.Boss, BossId, 4, 0)),
            SpecialGroup(Starter, ForgeId, SiteReservationKind.Forge,
                Option(SiteReservationKind.Forge, ForgeId, 2, 4)),
            SpecialGroup(Starter, CassiaId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, CassiaId, 5, 4)),
            SpecialGroup(Starter, YeastId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, YeastId, 0, 8)),
            SpecialGroup(Starter, MeteorId, SiteReservationKind.CoreResource,
                Option(SiteReservationKind.CoreResource, MeteorId, 8, 8))
        });

        private static SiteReservationSelectionPlan SearchPlan(
            IEnumerable<SiteReservationSearchGroup> groups)
        {
            var result = new SiteReservationBacktracker().Search(
                groups, Starter.Policy, SiteCandidateCostWeights.Default,
                SiteReservationSearchLimits.Default, new DeterministicRngStream(10));
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("\n",
                    result.Errors.Select(error => error.Code + ":" + error.Message)));
            return result.SelectionPlan;
        }

        private static IReadOnlyList<CoreCapacityRequirement> Requirements(
            SiteReservationSelectionPlan plan,
            StarterData data)
        {
            var result = new List<CoreCapacityRequirement>();
            foreach (var index in new[] { 2, 3, 4, 5 })
            {
                var placement = plan.SelectedPlacements[index];
                var key = SitePlacementKey.FromPlacement(placement);
                var special = data.SpecialMaps[key.SourceDefinitionId];
                var biome = data.Biomes[special.PrimaryBiomeId];
                result.Add(new CoreCapacityRequirement(
                    key, placement, special, biome, data.CoreRules[biome.BiomeId]));
            }
            return new ReadOnlyCollection<CoreCapacityRequirement>(result);
        }

        private static SiteReservationSearchGroup StartGroup(
            params SiteReservationSearchOption[] options) => new SiteReservationSearchGroup(
                Key(SiteReservationKind.Start, WorldId), null, null, null, options);

        private static SiteReservationSearchGroup SpecialGroup(
            StarterData data,
            string sourceId,
            SiteReservationKind kind,
            params SiteReservationSearchOption[] options)
        {
            var special = data.SpecialMaps[sourceId];
            var biome = data.Biomes[special.PrimaryBiomeId];
            return new SiteReservationSearchGroup(
                Key(kind, sourceId), special, biome, data.CoreRules[biome.BiomeId], options);
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

        private static SitePlacementKey Key(SiteReservationKind kind, string sourceId) =>
            new SitePlacementKey(kind, sourceId, 0);

        private static int EdgeRing(SectorCoord coordinate) => Math.Min(
            Math.Min(coordinate.X, WorldGenConstants.SectorColumns - 1 - coordinate.X),
            Math.Min(coordinate.Y, WorldGenConstants.SectorRows - 1 - coordinate.Y));

        private static bool WitnessIsConnected(CoreCapacityFloodWitness witness)
        {
            var sectors = new HashSet<int>(witness.WitnessSectorIndices);
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(witness.WitnessSectorIndices[0]);
            visited.Add(witness.WitnessSectorIndices[0]);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in new[]
                         {
                             WorldGridIndex.GetLeftIndex(current),
                             WorldGridIndex.GetRightIndex(current),
                             WorldGridIndex.GetUpIndex(current),
                             WorldGridIndex.GetDownIndex(current)
                         })
                    if (neighbor >= 0 && sectors.Contains(neighbor) && visited.Add(neighbor))
                        queue.Enqueue(neighbor);
            }
            return visited.Count == sectors.Count;
        }

        private static string Snapshot(CoreCapacityFloodResult result)
        {
            var witnesses = result.Approval == null ? "-" : string.Join(";",
                result.Approval.Witnesses.Select(item => item.Key.SourceDefinitionId + ":" +
                    string.Join(",", item.WitnessSectorIndices)));
            var rejections = string.Join(";", result.Rejections.Select(item =>
                item.Reason + ":" + item.Key.SourceDefinitionId + ":" + item.SectorIndex));
            var errors = string.Join(";", result.Errors.Select(item =>
                item.Code + ":" + item.SiteSourceDefinitionId + ":" + item.SectorIndex));
            return result.Status + "|" + witnesses + "|" + rejections + "|" + errors + "|" +
                   (result.Diagnostics == null ? "-" :
                       result.Diagnostics.TotalFloodVisitedSectorCount + ":" +
                       result.Diagnostics.TotalWitnessSectorCount);
        }

        private static string FormatErrors(CoreCapacityFloodResult result) =>
            string.Join("\n", result.Errors.Select(error => error.Code + ":" + error.Message)
                .Concat(result.Rejections.Select(rejection =>
                    rejection.Reason + ":" + rejection.Message)));

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

        private static StarterData BuildStarterData(
            int buffer,
            bool buildFullGroups,
            int minimumOverride = -1)
        {
            var biomeRows = new List<string[]>
            {
                BiomeRow(CraterBiomeId, 0, 7), BiomeRow(CassiaBiomeId, 2, 12),
                BiomeRow(MillBiomeId, 1, 11), BiomeRow(DoughBiomeId, 0, 7)
            };
            var craterMinimum = minimumOverride >= 1 ? minimumOverride : 5;
            var cassiaMinimum = minimumOverride >= 1 ? minimumOverride : 5;
            var millMinimum = minimumOverride >= 1 ? minimumOverride : 4;
            var doughMinimum = minimumOverride >= 1 ? minimumOverride : 5;
            var craterMaximum = minimumOverride >= 1 ? WorldGenConstants.SectorCount : 18;
            var cassiaMaximum = minimumOverride >= 1 ? WorldGenConstants.SectorCount : 18;
            var millMaximum = minimumOverride >= 1 ? WorldGenConstants.SectorCount : 14;
            var doughMaximum = minimumOverride >= 1 ? WorldGenConstants.SectorCount : 18;
            var patchRows = new List<string[]>
            {
                PatchRow("PATCH_CRATER_CORE", CraterBiomeId,
                    craterMinimum, craterMaximum, true, buffer),
                PatchRow("PATCH_ROOT_CORE", CassiaBiomeId,
                    cassiaMinimum, cassiaMaximum, false, buffer),
                PatchRow("PATCH_MILL_CORE", MillBiomeId,
                    millMinimum, millMaximum, false, buffer),
                PatchRow("PATCH_DOUGH_CORE", DoughBiomeId,
                    doughMinimum, doughMaximum, true, buffer)
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
                    rule => rule.BiomeId, StringComparer.Ordinal),
                specialResult.DefinitionSet,
                policyResult.Policy);
            if (buildFullGroups) result.SetFullGroups(BuildFullGroups(result));
            return result;
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
                    var placement = solver.SolveSpecialSite(candidate, transform, special,
                        cells, entries, FootprintPlacementBlockers.Empty);
                    if (placement.Succeeded)
                        options.Add(new SiteReservationSearchOption(placement.Placement, -1));
                }
            }
            groups.Add(SpecialGroup(data, sourceId, kind, options.ToArray()));
        }

        private static Dictionary<string, IReadOnlyList<string[]>> SpecialRows() =>
            new Dictionary<string, IReadOnlyList<string[]>>(StringComparer.Ordinal)
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

        private static string[] BiomeRow(string id, int minimumY, int maximumY) => new[]
        {
            id, "Biome", "STAGE_MOON", "1", "1", "169", "1",
            minimumY.ToString(CultureInfo.InvariantCulture),
            maximumY.ToString(CultureInfo.InvariantCulture), "1.0", "THEME_MOON",
            "AUDIO_MOON", "MICRO_MOON", "RECIPE_MOON", "RESOURCE_MOON",
            "ELEMENT_MOON", "SITE_REQUIRED", "1", "test"
        };

        private static string[] PatchRow(
            string ruleId, string biomeId, int minimum, int maximum, bool edge, int buffer) =>
            new[]
            {
                ruleId, biomeId, "CORE", minimum.ToString(CultureInfo.InvariantCulture),
                maximum.ToString(CultureInfo.InvariantCulture), "1", "1", "1", "1.0",
                edge ? "1" : "0", buffer.ToString(CultureInfo.InvariantCulture), "1",
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
