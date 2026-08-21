using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class CorePatchGrowerTests
    {
        public static IEnumerable CanonicalSeeds()
        {
            for (var index = 0; index < 100; index++)
            {
                var seed = index == 0 ? 0UL : index == 1 ? 4660UL :
                    index == 2 ? ulong.MaxValue : (ulong)index;
                yield return new TestCaseData(seed).SetName(
                    "Grow_CanonicalWorld_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [TestCaseSource(nameof(CanonicalSeeds))]
        public void Grow_CanonicalWorldPublishesExactStarter(ulong seed)
        {
            var fixture = BuildFixture(seed, new FixtureOptions());
            var result = Grow(fixture);

            Assert.That(result.Status, Is.EqualTo(CorePatchGrowthStatus.Completed), FormatErrors(result));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Publication.AssignedSectorCount, Is.EqualTo(20));
            Assert.That(result.Publication.UnassignedSectorCount, Is.EqualTo(149));
            Assert.That(result.Publication.Snapshot.Patches.Select(item => item.SectorCount),
                Is.EqualTo(new[] { 5, 5, 5, 5 }));
            Assert.That(result.Diagnostics.InitialAssignedSectorCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.MandatoryAddedSectorCount, Is.EqualTo(16));
            Assert.That(result.Diagnostics.SupplementalAddedSectorCount, Is.Zero);
            Assert.That(result.Diagnostics.TotalAddedSectorCount, Is.EqualTo(16));
            Assert.That(result.Diagnostics.FinalAssignedSectorCount, Is.EqualTo(20));
            Assert.That(result.Diagnostics.FinalUnassignedSectorCount, Is.EqualTo(149));
            Assert.That(result.Diagnostics.ReservationIntrusionCount, Is.Zero);
            Assert.That(result.Diagnostics.CrossPatchOverlapCount, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(result.Publication.Snapshot.IsComplete, Is.False);
        }

        [Test]
        public void Grow_RecordsUseExactCanonicalMappingAndOrder()
        {
            var result = Grow(BuildFixture(0, new FixtureOptions()));
            Assert.That(result.Diagnostics.Records.Select(item => item.SourceReservationId.Value),
                Is.EqualTo(new[]
                {
                    "RSV_02_SITE_MOON_SEAL_FORGE", "RSV_03_SITE_CASSIA_SAP_HEART",
                    "RSV_04_SITE_DEEP_STAR_YEAST", "RSV_05_SITE_MOON_CORE_METEOR"
                }));
            Assert.That(result.Diagnostics.Records.Select(item => item.PatchId.Value),
                Is.EqualTo(new[]
                {
                    "PATCHINST_CORE_RSV_02_SITE_MOON_SEAL_FORGE",
                    "PATCHINST_CORE_RSV_03_SITE_CASSIA_SAP_HEART",
                    "PATCHINST_CORE_RSV_04_SITE_DEEP_STAR_YEAST",
                    "PATCHINST_CORE_RSV_05_SITE_MOON_CORE_METEOR"
                }));
            Assert.That(result.Diagnostics.Records.Select(item => item.BiomeId),
                Is.EqualTo(new[]
                {
                    "BIO_ABANDONED_MILL", "BIO_CASSIA_ROOT", "BIO_MOON_DOUGH", "BIO_MOON_CRATER"
                }));
            Assert.That(result.Diagnostics.Records.Select(item => item.CorePatchRuleId),
                Is.EqualTo(new[]
                {
                    "PATCH_MILL_CORE", "PATCH_ROOT_CORE", "PATCH_DOUGH_CORE", "PATCH_CRATER_CORE"
                }));
        }

        [Test]
        public void Grow_PreservesSourceReferencesSeedsAndBindings()
        {
            var fixture = BuildFixture(4660, new FixtureOptions());
            var result = Grow(fixture);
            Assert.That(result.Publication.SourceInitialization, Is.SameAs(fixture.Initialization));
            Assert.That(result.Publication.SourceSiteSnapshot, Is.SameAs(fixture.Source));
            Assert.That(result.Publication.Snapshot.Seed, Is.EqualTo(fixture.Source.Seed));
            Assert.That(result.Publication.CorePatchCount, Is.EqualTo(4));
            Assert.That(result.Publication.CoreSeedCount, Is.EqualTo(4));
            Assert.That(result.Publication.CoreSiteBindingCount, Is.EqualTo(4));
            foreach (var inputPatch in fixture.Initialization.Snapshot.Patches)
            {
                Assert.That(result.Publication.Snapshot.TryGetPatch(inputPatch.Id, out var output), Is.True);
                Assert.That(output.Seeds, Is.EqualTo(inputPatch.Seeds));
            }
        }

        [Test]
        public void Grow_DoesNotMutateSourceOrInputSnapshot()
        {
            var fixture = BuildFixture(0, new FixtureOptions());
            var before = LogicalInitialization(fixture.Initialization);
            var result = Grow(fixture);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(LogicalInitialization(fixture.Initialization), Is.EqualTo(before));
            Assert.That(fixture.Initialization.AssignedSectorCount, Is.EqualTo(4));
            Assert.That(fixture.Initialization.Snapshot.Patches.Select(item => item.SectorCount),
                Is.EqualTo(new[] { 1, 1, 1, 1 }));
        }

        [TestCase(0, 1, 0)]
        [TestCase(1, 5, 4)]
        [TestCase(2, 13, 12)]
        public void Grow_BufferRadiusUsesFullManhattanUnion(
            int radius,
            int expectedTarget,
            int expectedAdded)
        {
            var options = MinimalRules();
            options.Rules[ForgeId].Buffer = radius;
            options.Rules[ForgeId].Maximum = 14;
            var result = Grow(BuildFixture(0, options));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var record = Record(result, ForgeId);
            Assert.That(record.MandatoryBufferSectorCount, Is.EqualTo(expectedTarget));
            Assert.That(record.TargetSectorCount, Is.EqualTo(expectedTarget));
            Assert.That(record.MandatoryAddedSectorCount, Is.EqualTo(expectedAdded));
            Assert.That(record.SupplementalAddedSectorCount, Is.Zero);
            Assert.That(record.MandatoryBufferSectorIndices, Has.None.EqualTo(0));
        }

        [Test]
        public void Grow_MultiCellFootprintUsesUnionNotOriginOrBoundingBox()
        {
            var options = new FixtureOptions { ForgeWidth = 2 };
            var result = Grow(BuildFixture(0, options));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var record = Record(result, ForgeId);
            Assert.That(record.FootprintSectorIndices, Is.EqualTo(new[] { 28, 29 }));
            Assert.That(record.MandatoryBufferSectorIndices,
                Is.EqualTo(new[] { 15, 16, 27, 28, 29, 30, 41, 42 }));
            Assert.That(record.MandatoryBufferSectorCount, Is.EqualTo(8));
            Assert.That(record.FinalSectorCount, Is.EqualTo(8));
            Assert.That(record.MandatoryBufferSectorIndices, Has.None.EqualTo(14));
        }

        [Test]
        public void Grow_EdgeForbiddenReturnsAtomicRetry()
        {
            var options = new FixtureOptions();
            options.Positions[ForgeId] = new SectorCoord(0, 6);
            var fixture = BuildFixture(0, options);
            var before = LogicalInitialization(fixture.Initialization);
            var result = Grow(fixture);
            AssertRetry(result, CorePatchGrowthErrorCode.BufferOutsideWorld);
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics.Records, Is.Empty);
            Assert.That(result.Diagnostics.TotalAddedSectorCount, Is.Zero);
            Assert.That(result.Diagnostics.FinalAssignedSectorCount, Is.EqualTo(4));
            Assert.That(LogicalInitialization(fixture.Initialization), Is.EqualTo(before));
        }

        [Test]
        public void Grow_EdgeAllowedUsesTruncatedBufferThenMinimumSupplement()
        {
            var options = new FixtureOptions();
            options.Positions[YeastId] = new SectorCoord(0, 6);
            var result = Grow(BuildFixture(0, options));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var record = Record(result, YeastId);
            Assert.That(record.OutsideTheoreticalBufferCount, Is.EqualTo(1));
            Assert.That(record.MandatoryBufferSectorCount, Is.EqualTo(4));
            Assert.That(record.TargetSectorCount, Is.EqualTo(5));
            Assert.That(record.MandatoryAddedSectorCount, Is.EqualTo(3));
            Assert.That(record.SupplementalAddedSectorCount, Is.EqualTo(1));
        }

        [TestCase(StartId)]
        [TestCase(BossId)]
        [TestCase(VillageId)]
        public void Grow_NonCoreReservationIsHardBlocker(string blockerId)
        {
            var options = new FixtureOptions();
            options.Positions[blockerId] = new SectorCoord(3, 2);
            var result = Grow(BuildFixture(0, options));
            AssertRetry(result, CorePatchGrowthErrorCode.BufferBlockedByReservation);
            Assert.That(result.Errors.Any(item =>
                item.OtherSourceReservationId.Value == blockerId && item.SectorIndex == 29), Is.True);
        }

        [Test]
        public void Grow_OtherCoreReservationIsHardBlocker()
        {
            var options = new FixtureOptions();
            options.Positions[CassiaId] = new SectorCoord(3, 2);
            var result = Grow(BuildFixture(0, options));
            AssertRetry(result,
                CorePatchGrowthErrorCode.BufferBlockedByReservation,
                CorePatchGrowthErrorCode.MandatoryBufferConflict);
        }

        [Test]
        public void Grow_UnreservedExteriorSectorRemainsEligible()
        {
            var result = Grow(BuildFixture(0, new FixtureOptions()));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var record = Record(result, ForgeId);
            Assert.That(record.FinalSectorIndices, Does.Contain(29));
            Assert.That(result.Publication.SourceSiteSnapshot.GetSector(29).IsReserved, Is.False);
        }

        [Test]
        public void Grow_CrossCoreMandatoryConflictEmitsBothCanonicalSides()
        {
            var options = new FixtureOptions();
            options.Positions[ForgeId] = new SectorCoord(4, 4);
            options.Positions[CassiaId] = new SectorCoord(6, 4);
            var result = Grow(BuildFixture(0, options));
            AssertRetry(result, CorePatchGrowthErrorCode.MandatoryBufferConflict);
            var conflicts = result.Errors.Where(item =>
                item.Code == CorePatchGrowthErrorCode.MandatoryBufferConflict && item.SectorIndex == 57).ToArray();
            Assert.That(conflicts, Has.Length.EqualTo(2));
            Assert.That(conflicts.Select(item => item.SourceReservationId.Value),
                Is.EqualTo(new[] { ForgeId, CassiaId }));
        }

        [Test]
        public void Grow_TargetIsMaximumOfBufferAndMinimumButNeverMaximumFill()
        {
            var options = MinimalRules();
            options.Rules[ForgeId].Minimum = 6;
            options.Rules[ForgeId].Maximum = 14;
            var result = Grow(BuildFixture(0, options));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var record = Record(result, ForgeId);
            Assert.That(record.MandatoryBufferSectorCount, Is.EqualTo(1));
            Assert.That(record.TargetSectorCount, Is.EqualTo(6));
            Assert.That(record.FinalSectorCount, Is.EqualTo(6));
            Assert.That(record.FinalSectorCount, Is.LessThan(record.MaximumSectorCount));
            Assert.That(record.SupplementalAddedSectorCount, Is.EqualTo(5));
        }

        [Test]
        public void Grow_TargetAboveMaximumIsStructuralInvalidInput()
        {
            var options = MinimalRules();
            options.Rules[ForgeId].Buffer = 2;
            options.Rules[ForgeId].Maximum = 10;
            var result = Grow(BuildFixture(0, options));
            AssertInvalid(result, CorePatchGrowthErrorCode.TargetExceedsMaximum);
        }

        [Test]
        public void Grow_SupplementUsesPerimeterSectorTieBreakAndOneClaimPerPatchRound()
        {
            var options = new FixtureOptions();
            options.Positions[YeastId] = new SectorCoord(0, 6);
            options.Positions[MeteorId] = new SectorCoord(12, 6);
            var result = Grow(BuildFixture(0, options));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var yeast = Record(result, YeastId);
            var meteor = Record(result, MeteorId);
            Assert.That(yeast.AddedSectorIndices.Except(yeast.MandatoryBufferSectorIndices),
                Is.EqualTo(new[] { 66 }));
            Assert.That(meteor.AddedSectorIndices.Except(meteor.MandatoryBufferSectorIndices),
                Is.EqualTo(new[] { 76 }));
            Assert.That(yeast.SupplementalAddedSectorCount, Is.EqualTo(1));
            Assert.That(meteor.SupplementalAddedSectorCount, Is.EqualTo(1));
            Assert.That(yeast.GrowthRoundCount, Is.EqualTo(1));
            Assert.That(meteor.GrowthRoundCount, Is.EqualTo(1));
        }

        [Test]
        public void Grow_InsufficientFrontierRequiresRetryWithoutPartialPublication()
        {
            var options = new FixtureOptions();
            options.Positions[YeastId] = new SectorCoord(0, 6);
            options.ReservationMutation = reservations =>
            {
                Replace(reservations, 0, CreateSparseReservation2D(
                    0, StartId, "WORLD_MOONPALACE_V1", SiteReservationKind.Start,
                    string.Empty, new SectorCoord(0, 4), 3, 5,
                    new[]
                    {
                        new SectorCoord(0, 0), new SectorCoord(1, 1),
                        new SectorCoord(2, 2), new SectorCoord(1, 3),
                        new SectorCoord(0, 4)
                    }));
            };
            var fixture = BuildFixture(0, options);
            var before = LogicalInitialization(fixture.Initialization);
            var result = Grow(fixture);
            AssertRetry(result, CorePatchGrowthErrorCode.InsufficientUnreservedCapacity);
            Assert.That(result.Diagnostics.Records, Is.Empty);
            Assert.That(result.Diagnostics.TotalAddedSectorCount, Is.Zero);
            Assert.That(LogicalInitialization(fixture.Initialization), Is.EqualTo(before));
        }

        [Test]
        public void Grow_NullInputsAccumulateStableStructuralErrors()
        {
            var result = new CorePatchGrower().Grow(null, null, null);
            AssertInvalid(result,
                CorePatchGrowthErrorCode.MissingInitialization,
                CorePatchGrowthErrorCode.MissingBiomeTypes,
                CorePatchGrowthErrorCode.MissingPatchRules);
        }

        [Test]
        public void Grow_NullAndDuplicateDefinitionsAccumulateSortAndDedupe()
        {
            var fixture = BuildFixture(0, new FixtureOptions());
            var biomes = fixture.Definitions.Biomes.Concat(new[]
            {
                null, fixture.Definitions.Biomes[0], fixture.Definitions.Biomes[0]
            }).Reverse();
            var rules = fixture.Definitions.Rules.Concat(new[]
            {
                null, fixture.Definitions.Rules[0], fixture.Definitions.Rules[0]
            }).Reverse();
            var result = new CorePatchGrower().Grow(fixture.Initialization, biomes, rules);
            AssertInvalid(result,
                CorePatchGrowthErrorCode.NullDefinition,
                CorePatchGrowthErrorCode.DuplicateDefinitionId);
            Assert.That(result.Errors, Is.Ordered.Using<CorePatchGrowthError>(
                Comparer<CorePatchGrowthError>.Create(CompareErrors)));
            Assert.That(result.Errors.Count(item => item.Code == CorePatchGrowthErrorCode.NullDefinition),
                Is.EqualTo(1));
        }

        [Test]
        public void Grow_MissingRequiredDefinitionsIsAtomicInvalidInput()
        {
            var fixture = BuildFixture(0, new FixtureOptions());
            var result = new CorePatchGrower().Grow(
                fixture.Initialization,
                fixture.Definitions.Biomes.Where(item => item.BiomeId != "BIO_CASSIA_ROOT"),
                fixture.Definitions.Rules.Where(item => item.PatchRuleId != "PATCH_DOUGH_CORE"));
            AssertInvalid(result,
                CorePatchGrowthErrorCode.MissingBiomeDefinition,
                CorePatchGrowthErrorCode.MissingCorePatchRule);
        }

        [Test]
        public void Grow_InvalidRuleAndSeedIdentityAreRejected()
        {
            var fixture = BuildFixture(0, new FixtureOptions());
            var invalidOptions = new FixtureOptions();
            invalidOptions.Rules[CassiaId].Minimum = 6;
            invalidOptions.Rules[CassiaId].Role = "SATELLITE";
            invalidOptions.Rules[CassiaId].BiomeIdOverride = "BIO_MOON_DOUGH";
            var invalidDefinitions = BuildDefinitions(invalidOptions);
            var result = new CorePatchGrower().Grow(
                fixture.Initialization,
                invalidDefinitions.Biomes,
                invalidDefinitions.Rules);
            AssertInvalid(result,
                CorePatchGrowthErrorCode.InvalidCorePatchRule,
                CorePatchGrowthErrorCode.DefinitionIdentityMismatch);
        }

        [Test]
        public void Grow_IsStableAcrossInputOrderCultureAndInstanceReuse()
        {
            var fixture = BuildFixture(ulong.MaxValue, new FixtureOptions());
            var previous = CultureInfo.CurrentCulture;
            try
            {
                var grower = new CorePatchGrower();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var first = grower.Grow(
                    fixture.Initialization,
                    fixture.Definitions.Biomes.Reverse(),
                    fixture.Definitions.Rules.Reverse());
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                var second = grower.Grow(
                    fixture.Initialization,
                    fixture.Definitions.Biomes,
                    fixture.Definitions.Rules);
                Assert.That(LogicalResult(first), Is.EqualTo(LogicalResult(second)));
                Assert.That(LogicalResult(second), Is.EqualTo(LogicalResult(Grow(fixture))));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void Grow_DefensivelyIsolatesCallerCollectionsAndReturnedLists()
        {
            var fixture = BuildFixture(0, new FixtureOptions());
            var biomes = fixture.Definitions.Biomes.ToArray();
            var rules = fixture.Definitions.Rules.ToArray();
            var result = new CorePatchGrower().Grow(fixture.Initialization, biomes, rules);
            var before = LogicalResult(result);
            Array.Reverse(biomes);
            Array.Clear(rules, 0, rules.Length);
            Assert.That(LogicalResult(result), Is.EqualTo(before));
            Assert.That(result.Diagnostics.Records, Is.InstanceOf<ReadOnlyCollection<CorePatchGrowthRecord>>());
            Assert.That(result.Diagnostics.Records[0].FinalSectorIndices,
                Is.InstanceOf<ReadOnlyCollection<int>>());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)result.Diagnostics.Records[0].FinalSectorIndices).Add(168));
        }

        [Test]
        public void Grow_PublicSurfaceIsImmutableAndDependencyBoundaryIsExact()
        {
            var types = new[]
            {
                typeof(CorePatchGrowthError), typeof(CorePatchGrowthRecord),
                typeof(CorePatchGrowthDiagnostics), typeof(CorePatchGrowthPublication),
                typeof(CorePatchGrowthResult), typeof(CorePatchGrower)
            };
            foreach (var type in types)
            {
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty, type.Name);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Any(property => property.SetMethod != null && property.SetMethod.IsPublic), Is.False, type.Name);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Any(field => !field.IsLiteral && !field.IsInitOnly), Is.False, type.Name);
            }
            var method = typeof(CorePatchGrower).GetMethod("Grow");
            Assert.That(method, Is.Not.Null);
            Assert.That(method.GetParameters().Select(item => item.ParameterType), Is.EqualTo(new[]
            {
                typeof(CorePatchInitializationPublication),
                typeof(IEnumerable<BiomeTypeDefinition>),
                typeof(IEnumerable<BiomePatchRuleDefinition>)
            }));
            var dependencyNames = method.GetParameters().Select(item => item.ParameterType.FullName ?? string.Empty)
                .Concat(new[] { method.ReturnType.FullName ?? string.Empty });
            Assert.That(dependencyNames.Any(name =>
                name.Contains("CoreCapacity") || name.Contains("Random") ||
                name.Contains("UnityEngine") || name.Contains("UnityEditor") ||
                name.Contains("DateTime") || name.Contains("System.IO")), Is.False);
        }

        [Test]
        public void Grow_FrozenEnumsHaveExactOrder()
        {
            Assert.That(Enum.GetNames(typeof(CorePatchGrowthStatus)),
                Is.EqualTo(new[] { "Completed", "InvalidInput", "RetryRequired" }));
            Assert.That(Enum.GetNames(typeof(CorePatchGrowthErrorCode)), Is.EqualTo(new[]
            {
                "MissingInitialization", "InvalidInitialization", "MissingSourceSiteSnapshot",
                "InvalidSourceSiteSnapshot", "MissingBiomeTypes", "MissingPatchRules",
                "NullDefinition", "DuplicateDefinitionId", "MissingBiomeDefinition",
                "MissingCorePatchRule", "InvalidBiomeDefinition", "InvalidCorePatchRule",
                "DefinitionIdentityMismatch", "MissingCorePatch", "MissingCoreBinding",
                "InvalidCorePatch", "InvalidCoreBinding", "InvalidCoreSeed", "InvalidOwnership",
                "UnexpectedAssignedSector", "TargetExceedsMaximum", "InternalInvariantViolation",
                "BufferOutsideWorld", "BufferBlockedByReservation", "MandatoryBufferConflict",
                "InsufficientUnreservedCapacity"
            }));
        }

        private static CorePatchGrowthResult Grow(GrowthFixture fixture)
        {
            return new CorePatchGrower().Grow(
                fixture.Initialization,
                fixture.Definitions.Biomes,
                fixture.Definitions.Rules);
        }

        private static GrowthFixture BuildFixture(ulong seed, FixtureOptions options)
        {
            var definitions = BuildDefinitions(options);
            var initializationDefinitions = BuildDefinitions(new FixtureOptions());
            var source = BuildSourceSnapshot(seed, options);
            var initialized = new CorePatchSeedInitializer().Initialize(
                source, initializationDefinitions.Biomes, initializationDefinitions.Rules);
            if (!initialized.Succeeded)
                throw new InvalidOperationException(string.Join("\n", initialized.Errors.Select(item =>
                    item.Code + ":" + item.Message)));
            return new GrowthFixture(source, initialized.Publication, definitions);
        }

        private static SiteReservationSnapshot BuildSourceSnapshot(ulong seed, FixtureOptions options)
        {
            var reservations = new List<SiteReservation>
            {
                CreateReservation(0, StartId, "WORLD_MOONPALACE_V1", SiteReservationKind.Start,
                    string.Empty, options.Positions[StartId], 1),
                CreateReservation(1, BossId, "SITE_MOON_BOSS_VAULT", SiteReservationKind.Boss,
                    "BIO_ABANDONED_MILL", options.Positions[BossId], 1),
                CreateReservation(2, ForgeId, "SITE_MOON_SEAL_FORGE", SiteReservationKind.Forge,
                    "BIO_ABANDONED_MILL", options.Positions[ForgeId], options.ForgeWidth),
                CreateReservation(3, CassiaId, "SITE_CASSIA_SAP_HEART", SiteReservationKind.CoreResource,
                    "BIO_CASSIA_ROOT", options.Positions[CassiaId], 1),
                CreateReservation(4, YeastId, "SITE_DEEP_STAR_YEAST", SiteReservationKind.CoreResource,
                    "BIO_MOON_DOUGH", options.Positions[YeastId], 1),
                CreateReservation(5, MeteorId, "SITE_MOON_CORE_METEOR", SiteReservationKind.CoreResource,
                    "BIO_MOON_CRATER", options.Positions[MeteorId], 1),
                CreateReservation(6, VillageId, "SITE_PRIMARY_VILLAGE", SiteReservationKind.Village,
                    string.Empty, options.Positions[VillageId], 1)
            };
            options.ReservationMutation?.Invoke(reservations);

            var byId = reservations.ToDictionary(item => item.ReservationId.Value, StringComparer.Ordinal);
            var seeds = new List<CoreBiomeSeed>
            {
                Seed(byId[ForgeId], "BIO_ABANDONED_MILL", "PATCH_MILL_CORE", new RuleSpec(4, 14, 1, false)),
                Seed(byId[CassiaId], "BIO_CASSIA_ROOT", "PATCH_ROOT_CORE", new RuleSpec(5, 18, 1, false)),
                Seed(byId[YeastId], "BIO_MOON_DOUGH", "PATCH_DOUGH_CORE", new RuleSpec(5, 18, 1, true)),
                Seed(byId[MeteorId], "BIO_MOON_CRATER", "PATCH_CRATER_CORE", new RuleSpec(5, 18, 1, true))
            };
            return new SiteReservationSnapshot(seed, reservations, CreateSectors(reservations), seeds);
        }

        private static CoreBiomeSeed Seed(
            SiteReservation reservation,
            string biomeId,
            string ruleId,
            RuleSpec rule)
        {
            return new CoreBiomeSeed(
                reservation.ReservationId,
                biomeId,
                ruleId,
                reservation.OccupiedSectors.OrderBy(WorldGridIndex.ToIndex).First(),
                rule.Minimum,
                rule.Buffer);
        }

        private static SiteReservation CreateReservation(
            int order,
            string reservationId,
            string sourceId,
            SiteReservationKind kind,
            string biomeId,
            SectorCoord origin,
            int width)
        {
            return CreateSparseReservation(
                order, reservationId, sourceId, kind, biomeId, origin, width,
                Enumerable.Range(0, width));
        }

        private static SiteReservation CreateSparseReservation(
            int order,
            string reservationId,
            string sourceId,
            SiteReservationKind kind,
            string biomeId,
            SectorCoord origin,
            int width,
            IEnumerable<int> occupiedLocalX)
        {
            var cells = occupiedLocalX.Select(localX => new SiteFootprintCell(
                localX,
                0,
                kind == SiteReservationKind.Start ? "START" : "CORE",
                biomeId,
                string.Empty,
                Array.Empty<SiteEntrySide>()));
            return new SiteReservation(
                new SiteReservationId(reservationId),
                kind,
                sourceId,
                origin,
                new SiteFootprint(width, 1, SiteFootprintTransform.R0, cells),
                biomeId,
                order,
                Array.Empty<SiteEntryAnchor>());
        }

        private static SiteReservation CreateSparseReservation2D(
            int order,
            string reservationId,
            string sourceId,
            SiteReservationKind kind,
            string biomeId,
            SectorCoord origin,
            int width,
            int height,
            IEnumerable<SectorCoord> occupiedLocalCells)
        {
            var cells = occupiedLocalCells.Select(local => new SiteFootprintCell(
                local.X,
                local.Y,
                kind == SiteReservationKind.Start ? "START" : "CORE",
                biomeId,
                string.Empty,
                Array.Empty<SiteEntrySide>()));
            return new SiteReservation(
                new SiteReservationId(reservationId),
                kind,
                sourceId,
                origin,
                new SiteFootprint(width, height, SiteFootprintTransform.R0, cells),
                biomeId,
                order,
                Array.Empty<SiteEntryAnchor>());
        }

        private static void Replace(List<SiteReservation> values, int index, SiteReservation value)
        {
            values[index] = value;
        }

        private static List<SectorReservation> CreateSectors(IEnumerable<SiteReservation> reservations)
        {
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var reservation in reservations)
                foreach (var coordinate in reservation.OccupiedSectors)
                {
                    if (!reservation.TryGetFootprintCell(coordinate, out var cell))
                        throw new InvalidOperationException("Footprint lookup failed.");
                    occupied.Add(coordinate, Tuple.Create(reservation, cell));
                }

            var result = new List<SectorReservation>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (occupied.TryGetValue(coordinate, out var binding))
                    result.Add(SectorReservation.CreateReserved(
                        index, coordinate, binding.Item1.ReservationId, binding.Item1.Kind,
                        binding.Item2.LocalX, binding.Item2.LocalY, binding.Item2.LocalRole));
                else
                    result.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            return result;
        }

        private static FixtureOptions MinimalRules()
        {
            var options = new FixtureOptions();
            foreach (var rule in options.Rules.Values)
            {
                rule.Minimum = 1;
                rule.Buffer = 0;
            }
            return options;
        }

        private static DefinitionFixture BuildDefinitions(FixtureOptions options)
        {
            var specs = CreateSpecs();
            var biomeRows = new[]
            {
                BiomeRow("BIO_ABANDONED_MILL"), BiomeRow("BIO_CASSIA_ROOT"),
                BiomeRow("BIO_MOON_DOUGH"), BiomeRow("BIO_MOON_CRATER")
            };
            var patchRows = new[]
            {
                PatchRow("PATCH_MILL_CORE", "BIO_ABANDONED_MILL", options.Rules[ForgeId]),
                PatchRow("PATCH_ROOT_CORE", "BIO_CASSIA_ROOT", options.Rules[CassiaId]),
                PatchRow("PATCH_DOUGH_CORE", "BIO_MOON_DOUGH", options.Rules[YeastId]),
                PatchRow("PATCH_CRATER_CORE", "BIO_MOON_CRATER", options.Rules[MeteorId])
            };
            var sources = specs.Select(spec => BuildDefinitionSource(
                spec,
                spec.FileName == "biome_types.csv" ? biomeRows :
                spec.FileName == "biome_patch_rules.csv" ? patchRows : Array.Empty<string[]>())).ToArray();
            var result = new BiomeBoundaryDefinitionBuilder().Build(sources);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            return new DefinitionFixture(
                result.DefinitionSet.BiomeTypes.Values.ToArray(),
                result.DefinitionSet.BiomePatchRules.Values.ToArray());
        }

        private static string[] BiomeRow(string biomeId)
        {
            return new[]
            {
                biomeId, "NAME", "STAGE_MOON", "1", "1", "4", "1", "0", "12", "1",
                "THEME", "AUDIO", "MICRO", "RECIPE", "RESOURCE", "ELEMENT", "SITE_REQUIRED",
                "1", string.Empty
            };
        }

        private static string[] PatchRow(string ruleId, string biomeId, RuleSpec spec)
        {
            return new[]
            {
                ruleId, spec.BiomeIdOverride ?? biomeId, spec.Role,
                spec.Minimum.ToString(CultureInfo.InvariantCulture),
                spec.Maximum.ToString(CultureInfo.InvariantCulture),
                "1", "1", "1", "1", spec.CanTouchEdge ? "1" : "0",
                spec.Buffer.ToString(CultureInfo.InvariantCulture),
                "0", "0.35", "1", "1", "1", "1", "0.5", spec.Active ? "1" : "0", string.Empty
            };
        }

        private static BiomeBoundaryDefinitionSource BuildDefinitionSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var schemaRows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName,
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                column.DataType,
                index == 0 ? "1" : "0",
                index == 0 ? "1" : string.Empty,
                string.Empty,
                column.AllowedValues,
                string.Empty,
                string.Empty,
                index + 2));
            var catalog = new CsvSchemaCatalogBuilder().Build(schemaRows);
            if (!catalog.Success) throw new InvalidOperationException(string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in rows) csv += "\n" + string.Join(",", row.Select(CsvCell));
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            if (!validation.Success) throw new InvalidOperationException(string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            if (!keys.Success) throw new InvalidOperationException("Primary-key fixture failed.");
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!parsed.Success) throw new InvalidOperationException(string.Join("\n", parsed.Errors));
            return new BiomeBoundaryDefinitionSource(schema, parsed);
        }

        private static FileSpec[] CreateSpecs()
        {
            return new[]
            {
                File("biome_types.csv", "biome_id:ID", "display_name_ko:STRING", "stage_id:ID", "required:BOOL", "min_patch_count:INT", "max_patch_count:INT", "min_core_patch_count:INT", "preferred_altitude_min_sector_y:INT", "preferred_altitude_max_sector_y:INT", "growth_weight:FLOAT", "tile_theme_id:ID", "audio_profile_id:ID", "microchunk_pool_prefix:ID", "sector_recipe_pool_prefix:ID", "common_resource_pool_id:ID", "map_element_pool_id:ID", "required_special_map_ids:ID_LIST", "active:BOOL", "notes:STRING"),
                File("biome_patch_rules.csv", "patch_rule_id:ID", "biome_id:ID", "patch_role:ENUM", "min_sector_count:INT", "max_sector_count:INT", "min_seed_distance:INT", "seed_count_min:INT", "seed_count_max:INT", "seed_weight:FLOAT", "can_touch_world_edge:BOOL", "buffer_ring_sectors:INT", "allow_single_sector:BOOL", "max_world_share:FLOAT", "distance_weight:FLOAT", "altitude_weight:FLOAT", "noise_weight:FLOAT", "compactness_weight:FLOAT", "branchiness_target:FLOAT", "active:BOOL", "notes:STRING"),
                File("biome_boundary_profiles.csv", "boundary_profile_id:ID", "display_name_ko:STRING", "boundary_type:ENUM", "allowed_orientations:ENUM_LIST", "width_microchunks_min:INT", "width_microchunks_max:INT", "warning_microchunks_min:INT", "mandatory_route_allowed:BOOL", "tool_requirement:ENUM", "hard_border:BOOL", "active:BOOL", "notes:STRING"),
                File("biome_boundary_pair_rules.csv", "boundary_pair_rule_id:ID", "biome_a_id:ID", "biome_b_id:ID", "allowed_boundary_profile_ids:ID_LIST", "boundary_profile_weights:INT_LIST", "default_boundary_profile_id:ID", "transition_resource_pool_id:ID", "transition_element_pool_id:ID", "min_shared_edge_count:INT", "active:BOOL", "notes:STRING"),
                File("boundary_chunk_catalog.csv", "boundary_chunk_id:ID", "microchunk_id:ID", "biome_a_id:ID", "biome_b_id:ID", "boundary_profile_id:ID", "orientation:ENUM", "route_type:INT", "entry_edge_signature_id:ID", "exit_edge_signature_id:ID", "weight:INT", "reversible:BOOL", "active:BOOL", "notes:STRING")
            };
        }

        private static FileSpec File(string fileName, params string[] definitions)
        {
            return new FileSpec(fileName, definitions.Select(value =>
            {
                var parts = value.Split(':');
                var allowed = parts[0] == "patch_role" ? "CORE|SATELLITE|INTRUSION" :
                    parts[1] == "ENUM" || parts[1] == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty;
                return new ColumnSpec(parts[0], parts[1], allowed);
            }).ToArray());
        }

        private static string CsvCell(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static CorePatchGrowthRecord Record(CorePatchGrowthResult result, string sourceId)
        {
            return result.Diagnostics.Records.Single(item => item.SourceReservationId.Value == sourceId);
        }

        private static void AssertInvalid(
            CorePatchGrowthResult result,
            params CorePatchGrowthErrorCode[] codes)
        {
            Assert.That(result.Status, Is.EqualTo(CorePatchGrowthStatus.InvalidInput));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors, Is.Not.Empty);
            foreach (var code in codes) Assert.That(result.Errors.Select(item => item.Code), Does.Contain(code));
        }

        private static void AssertRetry(
            CorePatchGrowthResult result,
            params CorePatchGrowthErrorCode[] codes)
        {
            Assert.That(result.Status, Is.EqualTo(CorePatchGrowthStatus.RetryRequired), FormatErrors(result));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RetryRequired, Is.True);
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Not.Null);
            foreach (var code in codes) Assert.That(result.Errors.Select(item => item.Code), Does.Contain(code));
        }

        private static string FormatErrors(CorePatchGrowthResult result)
        {
            return string.Join("\n", result.Errors.Select(item =>
                item.Code + ":" + item.SourceReservationId.Value + ":" + item.SectorIndex + ":" + item.Message));
        }

        private static int CompareErrors(CorePatchGrowthError left, CorePatchGrowthError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = left.SourceReservationId.CompareTo(right.SourceReservationId);
            if (value != 0) return value;
            value = left.OtherSourceReservationId.CompareTo(right.OtherSourceReservationId);
            if (value != 0) return value;
            value = left.PatchId.CompareTo(right.PatchId);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static string LogicalInitialization(CorePatchInitializationPublication publication)
        {
            return publication.SourceSiteSnapshot.Seed + "|" +
                   string.Join(";", publication.Snapshot.Patches.Select(item =>
                       item.Id.Value + ":" + string.Join(",", item.SectorIndices))) + "|" +
                   string.Join(";", publication.Snapshot.Sectors.Where(item => item.IsAssigned).Select(item =>
                       item.SectorIndex + ":" + item.PatchId.Value.Value));
        }

        private static string LogicalResult(CorePatchGrowthResult result)
        {
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            return result.Status + "|" + result.Diagnostics.TotalAddedSectorCount + "|" +
                   string.Join(";", result.Diagnostics.Records.Select(item =>
                       item.SourceReservationId.Value + ":" + string.Join(",", item.FinalSectorIndices))) + "|" +
                   string.Join(";", result.Publication.Snapshot.Sectors.Where(item => item.IsAssigned).Select(item =>
                       item.SectorIndex + ":" + item.PrimaryBiomeId + ":" + item.PatchId.Value.Value));
        }

        private const string StartId = "RSV_00_WORLD_MOONPALACE_V1";
        private const string BossId = "RSV_01_SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "RSV_02_SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "RSV_03_SITE_CASSIA_SAP_HEART";
        private const string YeastId = "RSV_04_SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "RSV_05_SITE_MOON_CORE_METEOR";
        private const string VillageId = "RSV_06_SITE_PRIMARY_VILLAGE";

        private sealed class FixtureOptions
        {
            public FixtureOptions()
            {
                Positions = new Dictionary<string, SectorCoord>(StringComparer.Ordinal)
                {
                    [StartId] = new SectorCoord(0, 0),
                    [BossId] = new SectorCoord(12, 12),
                    [ForgeId] = new SectorCoord(2, 2),
                    [CassiaId] = new SectorCoord(8, 2),
                    [YeastId] = new SectorCoord(2, 8),
                    [MeteorId] = new SectorCoord(8, 8),
                    [VillageId] = new SectorCoord(0, 12)
                };
                Rules = new Dictionary<string, RuleSpec>(StringComparer.Ordinal)
                {
                    [ForgeId] = new RuleSpec(4, 14, 1, false),
                    [CassiaId] = new RuleSpec(5, 18, 1, false),
                    [YeastId] = new RuleSpec(5, 18, 1, true),
                    [MeteorId] = new RuleSpec(5, 18, 1, true)
                };
            }

            public Dictionary<string, SectorCoord> Positions { get; }
            public Dictionary<string, RuleSpec> Rules { get; }
            public int ForgeWidth { get; set; } = 1;
            public Action<List<SiteReservation>> ReservationMutation { get; set; }
        }

        private sealed class RuleSpec
        {
            public RuleSpec(int minimum, int maximum, int buffer, bool canTouchEdge)
            {
                Minimum = minimum;
                Maximum = maximum;
                Buffer = buffer;
                CanTouchEdge = canTouchEdge;
                Role = "CORE";
                Active = true;
            }

            public int Minimum { get; set; }
            public int Maximum { get; set; }
            public int Buffer { get; set; }
            public bool CanTouchEdge { get; set; }
            public string Role { get; set; }
            public bool Active { get; set; }
            public string BiomeIdOverride { get; set; }
        }

        private sealed class GrowthFixture
        {
            public GrowthFixture(
                SiteReservationSnapshot source,
                CorePatchInitializationPublication initialization,
                DefinitionFixture definitions)
            {
                Source = source;
                Initialization = initialization;
                Definitions = definitions;
            }

            public SiteReservationSnapshot Source { get; }
            public CorePatchInitializationPublication Initialization { get; }
            public DefinitionFixture Definitions { get; }
        }

        private sealed class DefinitionFixture
        {
            public DefinitionFixture(
                IReadOnlyList<BiomeTypeDefinition> biomes,
                IReadOnlyList<BiomePatchRuleDefinition> rules)
            {
                Biomes = biomes;
                Rules = rules;
            }

            public IReadOnlyList<BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> Rules { get; }
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, IReadOnlyList<ColumnSpec> columns)
            {
                FileName = fileName;
                Columns = columns;
            }

            public string FileName { get; }
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
