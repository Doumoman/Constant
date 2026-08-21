using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class SatelliteSeedPlacerTests
    {
        private const ulong KnownWorldSeed = 0x0123456789ABCDEFUL;
        private Fixture fixture;

        public static IEnumerable CanonicalStates()
        {
            for (var index = 0; index < 100; index++)
            {
                var state = index == 0 ? 0UL : index == 1 ? ulong.MaxValue : (ulong)index;
                yield return new TestCaseData(state).SetName(
                    "Place_CanonicalState_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            fixture = BuildFixture(false);
        }

        [TestCaseSource(nameof(CanonicalStates))]
        public void Place_CanonicalStatesPreserveRangeConservationAndDistance(ulong initialState)
        {
            var result = Place(fixture, new DeterministicRngStream(initialState));

            Assert.That(result.Status, Is.EqualTo(SatelliteSeedPlacementStatus.Completed), FormatErrors(result));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Diagnostics.DesiredSatelliteSeedCount, Is.InRange(0, 11));
            Assert.That(result.Diagnostics.PlacedSatelliteSeedCount,
                Is.EqualTo(result.Diagnostics.DesiredSatelliteSeedCount));
            Assert.That(result.Publication.TotalPatchCount,
                Is.EqualTo(4 + result.Diagnostics.PlacedSatelliteSeedCount));
            Assert.That(result.Publication.AssignedSectorCount,
                Is.EqualTo(20 + result.Diagnostics.PlacedSatelliteSeedCount));
            Assert.That(result.Publication.UnassignedSectorCount,
                Is.EqualTo(149 - result.Diagnostics.PlacedSatelliteSeedCount));
            Assert.That(result.Publication.AssignedSectorCount + result.Publication.UnassignedSectorCount,
                Is.EqualTo(169));
            Assert.That(result.Diagnostics.Records.All(record =>
                record.SameBiomeDistance >= record.MinimumSeedDistance), Is.True);
            Assert.That(result.Diagnostics.ReservationIntrusionCount, Is.Zero);
            Assert.That(result.Diagnostics.PatchOverlapCount, Is.Zero);
        }

        [TestCase("BIO_MOON_CRATER", 0, "PATCHINST_SAT_BIO_MOON_CRATER_00")]
        [TestCase("BIO_MOON_CRATER", 1, "PATCHINST_SAT_BIO_MOON_CRATER_01")]
        [TestCase("BIO_ABANDONED_MILL", 99, "PATCHINST_SAT_BIO_ABANDONED_MILL_99")]
        public void PatchIdFactory_UsesExactGrammar(
            string biomeId,
            int ordinal,
            string expected)
        {
            var factory = new SatellitePatchIdFactory();
            Assert.That(factory.Create(biomeId, ordinal).Value, Is.EqualTo(expected));
            Assert.That(factory.TryCreate(biomeId, ordinal, out var value), Is.True);
            Assert.That(value.Value, Is.EqualTo(expected));
        }

        [TestCase(null, 0)]
        [TestCase("", 0)]
        [TestCase(" ", 0)]
        [TestCase("bio", 0)]
        [TestCase("BIO-A", 0)]
        [TestCase("BIO A", 0)]
        [TestCase("BIO_가", 0)]
        [TestCase("BIO_A", -1)]
        [TestCase("BIO_A", 100)]
        [TestCase(" BIO_A", 0)]
        [TestCase("BIO_A ", 0)]
        public void PatchIdFactory_RejectsInvalidIdentityWithoutNormalization(
            string biomeId,
            int ordinal)
        {
            var factory = new SatellitePatchIdFactory();
            Assert.That(factory.TryCreate(biomeId, ordinal, out var value), Is.False);
            Assert.That(value.IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => factory.Create(biomeId, ordinal));
        }

        [TestCase("en-US")]
        [TestCase("ar-SA")]
        public void PatchIdFactory_IsCultureInvariant(string cultureName)
        {
            WithCulture(cultureName, () =>
                Assert.That(new SatellitePatchIdFactory().Create("BIO_I", 9).Value,
                    Is.EqualTo("PATCHINST_SAT_BIO_I_09")));
        }

        [Test]
        public void Place_KnownFactoryVectorCountsFirstAndReportsExactStarter()
        {
            var streams = new WorldGenerationRngStreams(fixture.RouteDefinitions);
            var rng = streams.CreateBiomePatch(KnownWorldSeed, "PASS_BIOME", 0);
            Assert.That(rng.InitialState, Is.EqualTo(0x98BC23250806566BUL));

            var result = Place(fixture, rng);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Diagnostics.Rules.Select(value => value.PatchRuleId), Is.EqualTo(new[]
            {
                "PATCH_CRATER_SAT", "PATCH_DOUGH_SAT", "PATCH_MILL_SAT", "PATCH_ROOT_SAT"
            }));
            Assert.That(result.Diagnostics.Rules.Select(value => value.DesiredSeedCount),
                Is.EqualTo(new[] { 2, 0, 2, 3 }));
            Assert.That(result.Diagnostics.CountMethodCallCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.DesiredSatelliteSeedCount, Is.EqualTo(7));
            Assert.That(result.Diagnostics.PlacedSatelliteSeedCount, Is.EqualTo(7));
            Assert.That(result.Diagnostics.RawCandidateSectorCount, Is.EqualTo(145));
            Assert.That(result.Diagnostics.RngDrawCountBefore, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCountAfter, Is.EqualTo(rng.DrawCount));
            Assert.That(result.Publication.CorePatchCount, Is.EqualTo(4));
            Assert.That(result.Publication.SatellitePatchCount, Is.EqualTo(7));
            Assert.That(result.Publication.TotalPatchCount, Is.EqualTo(11));
            Assert.That(result.Publication.AssignedSectorCount, Is.EqualTo(27));
            Assert.That(result.Publication.UnassignedSectorCount, Is.EqualTo(142));
            TestContext.WriteLine("STARTER_EVIDENCE|initial=0x98BC23250806566B|rules=" +
                string.Join(",", result.Diagnostics.Rules.Select(value =>
                    value.PatchRuleId + ":count=" + value.DesiredSeedCount + ":attempts=" +
                    value.CandidateAttemptCount + ":edgeReject=" + value.EdgeRejectionCount +
                    ":distanceReject=" + value.DistanceRejectionCount)) + "|records=" +
                string.Join(",", result.Diagnostics.Records.Select(value =>
                    value.PatchId.Value + "@" + value.SectorIndex + "(" + value.Sector.X + "," +
                    value.Sector.Y + "):attempts=" + value.AttemptCount + ":distance=" +
                    value.SameBiomeDistance + "/" + value.MinimumSeedDistance + ":edgeReject=" +
                    value.EdgeRejectionCount + ":distanceReject=" + value.DistanceRejectionCount)) +
                "|raw=" + result.Diagnostics.RawCandidateSectorCount + "|rng=" +
                result.Diagnostics.RngDrawCountBefore + "->" + result.Diagnostics.RngDrawCountAfter);
        }

        [Test]
        public void Place_ZeroCountVectorCompletesWithNoCandidateCalls()
        {
            var state = FindState(new[] { 0, 0, 0, 0 });
            var result = Place(fixture, new DeterministicRngStream(state));

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Diagnostics.DesiredSatelliteSeedCount, Is.Zero);
            Assert.That(result.Diagnostics.PlacedSatelliteSeedCount, Is.Zero);
            Assert.That(result.Diagnostics.CandidateMethodCallCount, Is.Zero);
            Assert.That(result.Diagnostics.TotalRngMethodCallCount, Is.EqualTo(4));
            Assert.That(result.Publication.Snapshot, Is.Not.SameAs(fixture.Growth.Snapshot));
            Assert.That(result.Publication.AssignedSectorCount, Is.EqualTo(20));
            Assert.That(result.Publication.UnassignedSectorCount, Is.EqualTo(149));
        }

        [Test]
        public void Place_CountRollsOccurBeforeEveryCandidateCall()
        {
            var result = Place(fixture, new DeterministicRngStream(0));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Diagnostics.CountMethodCallCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.TotalRngMethodCallCount,
                Is.EqualTo(4 + result.Diagnostics.CandidateMethodCallCount));
            Assert.That(result.Diagnostics.Rules.All(rule => rule.CountRoll == rule.DesiredSeedCount), Is.True);
        }

        [Test]
        public void Place_PreservesGrowthSourceCoreObjectsSeedsBindingsAndOwnership()
        {
            var result = Place(fixture, new DeterministicRngStream(1));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Publication.SourceGrowth, Is.SameAs(fixture.Growth));
            Assert.That(result.Publication.SourceSiteSnapshot, Is.SameAs(fixture.Source));
            Assert.That(result.Publication.Snapshot.Seed, Is.EqualTo(fixture.Growth.Snapshot.Seed));
            foreach (var core in fixture.Growth.Snapshot.Patches)
            {
                Assert.That(result.Publication.Snapshot.TryGetPatch(core.Id, out var output), Is.True);
                Assert.That(output, Is.SameAs(core));
                Assert.That(output.Seeds, Is.SameAs(core.Seeds));
                foreach (var sectorIndex in core.SectorIndices)
                    Assert.That(result.Publication.Snapshot.GetSector(sectorIndex),
                        Is.SameAs(fixture.Growth.Snapshot.GetSector(sectorIndex)));
            }
            for (var index = 0; index < fixture.Growth.Snapshot.SiteBindings.Count; index++)
                Assert.That(result.Publication.Snapshot.SiteBindings[index],
                    Is.SameAs(fixture.Growth.Snapshot.SiteBindings[index]));
        }

        [Test]
        public void Place_SatellitePatchesAreOneCellSourceNullAndBindingFree()
        {
            var result = Place(fixture, new DeterministicRngStream(2));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            foreach (var record in result.Diagnostics.Records)
            {
                Assert.That(result.Publication.Snapshot.TryGetPatch(record.PatchId, out var patch), Is.True);
                Assert.That(patch.Role, Is.EqualTo(BiomePatchRole.Satellite));
                Assert.That(patch.SectorIndices, Is.EqualTo(new[] { record.SectorIndex }));
                Assert.That(patch.Seeds, Has.Count.EqualTo(1));
                Assert.That(patch.Seeds[0].Role, Is.EqualTo(BiomePatchRole.Satellite));
                Assert.That(patch.Seeds[0].SourceSiteReservationId, Is.Null);
                Assert.That(result.Publication.Snapshot.GetSector(record.SectorIndex).SecondaryBiomeId, Is.Empty);
                Assert.That(result.Publication.Snapshot.SiteBindings.Any(binding => binding.PatchId == patch.Id), Is.False);
            }
        }

        [Test]
        public void Place_ExcludesEveryReservationAndExistingOwnershipFromRawUniverse()
        {
            var result = Place(fixture, new DeterministicRngStream(3));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Diagnostics.RawCandidateSectorCount, Is.EqualTo(145));
            foreach (var record in result.Diagnostics.Records)
            {
                Assert.That(fixture.Source.GetSector(record.SectorIndex).IsReserved, Is.False);
                Assert.That(fixture.Growth.Snapshot.GetSector(record.SectorIndex).IsAssigned, Is.False);
            }
            Assert.That(result.Diagnostics.Records.Select(value => value.SectorIndex).Distinct().Count(),
                Is.EqualTo(result.Diagnostics.Records.Count));
        }

        [Test]
        public void Place_UnreservedEntryExteriorRemainsEligibleInRawUniverse()
        {
            var entryExterior = new SectorCoord(1, 0);
            var exteriorIndex = WorldGridIndex.ToIndex(entryExterior);
            Assert.That(fixture.Source.GetSector(exteriorIndex).IsReserved, Is.False);
            Assert.That(fixture.Growth.Snapshot.GetSector(exteriorIndex).IsAssigned, Is.False);

            var excluded = fixture.Source.Sectors.Count(value => value.IsReserved ||
                fixture.Growth.Snapshot.GetSector(value.Index).IsAssigned);
            Assert.That(WorldGenConstants.SectorCount - excluded, Is.EqualTo(145));
        }

        [TestCase("PATCH_CRATER_SAT")]
        [TestCase("PATCH_DOUGH_SAT")]
        [TestCase("PATCH_MILL_SAT")]
        [TestCase("PATCH_ROOT_SAT")]
        public void Place_EachRuleUsesAllCoreSectorsAndPriorSameBiomeSeeds(string ruleId)
        {
            var result = Place(fixture, new DeterministicRngStream(4));
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var records = result.Diagnostics.Records.Where(value => value.PatchRuleId == ruleId)
                .OrderBy(value => value.SatelliteOrdinal).ToArray();
            foreach (var record in records)
            {
                var sources = fixture.Growth.Snapshot.Patches
                    .Where(patch => patch.Role == BiomePatchRole.Core && patch.BiomeId == record.BiomeId)
                    .SelectMany(patch => patch.SectorIndices)
                    .Concat(records.Where(value => value.SatelliteOrdinal < record.SatelliteOrdinal)
                        .Select(value => value.SectorIndex));
                Assert.That(record.SameBiomeDistance,
                    Is.EqualTo(sources.Min(index => Manhattan(index, record.SectorIndex))));
            }
        }

        [Test]
        public void Place_DifferentBiomeAdjacencyDoesNotCauseRejection()
        {
            var observedAdjacent = false;
            for (ulong state = 0; state < 300 && !observedAdjacent; state++)
            {
                var result = Place(fixture, new DeterministicRngStream(state));
                var records = result.Diagnostics.Records;
                observedAdjacent = records.Any(first => records.Any(second =>
                    first.BiomeId != second.BiomeId && Manhattan(first.SectorIndex, second.SectorIndex) == 1));
            }
            Assert.That(observedAdjacent, Is.True);
        }

        [Test]
        public void Place_EdgeForbiddenRulesNeverAcceptWorldEdge()
        {
            for (ulong state = 0; state < 25; state++)
            {
                var result = Place(fixture, new DeterministicRngStream(state));
                foreach (var record in result.Diagnostics.Records.Where(value =>
                             value.PatchRuleId == "PATCH_MILL_SAT" || value.PatchRuleId == "PATCH_ROOT_SAT"))
                    Assert.That(IsWorldEdge(record.Sector), Is.False);
            }
        }

        [Test]
        public void Place_ExhaustionReturnsAtomicRollbackAndEmptyRecords()
        {
            var blocked = BuildFixture(true);
            var rng = new DeterministicRngStream(0x98BC23250806566BUL);
            var result = Place(blocked, rng);

            Assert.That(result.Status, Is.EqualTo(SatelliteSeedPlacementStatus.RetryRequired), FormatErrors(result));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RetryRequired, Is.True);
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics.Records, Is.Empty);
            Assert.That(result.Diagnostics.RawCandidateSectorCount, Is.Zero);
            Assert.That(result.Diagnostics.FinalPatchCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.FinalAssignedSectorCount, Is.EqualTo(20));
            Assert.That(result.Diagnostics.FinalUnassignedSectorCount, Is.EqualTo(149));
            Assert.That(result.Diagnostics.PlacedSatelliteSeedCount, Is.Zero);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(SatelliteSeedPlacementErrorCode.CandidateAttemptsExhausted));
        }

        [Test]
        public void Place_NullInputsAccumulateWithoutRngConsumption()
        {
            var rng = new DeterministicRngStream(0);
            var result = new SatelliteSeedPlacer().Place(null, null, null, null, rng);
            AssertInvalid(result,
                SatelliteSeedPlacementErrorCode.MissingGrowthPublication,
                SatelliteSeedPlacementErrorCode.MissingGenerationProfile,
                SatelliteSeedPlacementErrorCode.MissingBiomeTypes,
                SatelliteSeedPlacementErrorCode.MissingSatelliteRules);
            Assert.That(rng.DrawCount, Is.Zero);
        }

        [Test]
        public void Place_MissingRngIsStructuralInvalidInput()
        {
            var result = new SatelliteSeedPlacer().Place(
                fixture.Growth, fixture.Profile, fixture.Biomes, fixture.SatelliteRules, null);
            AssertInvalid(result, SatelliteSeedPlacementErrorCode.MissingBiomePatchRng);
        }

        [Test]
        public void Place_ConsumedRngIsRejectedWithoutAdditionalDraw()
        {
            var rng = new DeterministicRngStream(0);
            rng.NextUInt64();
            var result = Place(fixture, rng);
            AssertInvalid(result, SatelliteSeedPlacementErrorCode.InvalidBiomePatchRngState);
            Assert.That(rng.DrawCount, Is.EqualTo(1));
        }

        [Test]
        public void Place_MissingDefinitionsAreAccumulatedAndSorted()
        {
            var rng = new DeterministicRngStream(0);
            var result = new SatelliteSeedPlacer().Place(
                fixture.Growth,
                fixture.Profile,
                fixture.Biomes.Where(value => value.BiomeId != "BIO_CASSIA_ROOT"),
                fixture.SatelliteRules.Where(value => value.PatchRuleId != "PATCH_DOUGH_SAT"),
                rng);
            AssertInvalid(result,
                SatelliteSeedPlacementErrorCode.MissingBiomeDefinition,
                SatelliteSeedPlacementErrorCode.MissingSatelliteRule);
            Assert.That(result.Errors, Is.Ordered.Using<SatelliteSeedPlacementError>(
                Comparer<SatelliteSeedPlacementError>.Create(CompareErrors)));
            Assert.That(rng.DrawCount, Is.Zero);
        }

        [Test]
        public void Place_NullAndDuplicateDefinitionsDedupeStableErrors()
        {
            var rng = new DeterministicRngStream(0);
            var result = new SatelliteSeedPlacer().Place(
                fixture.Growth,
                fixture.Profile,
                fixture.Biomes.Concat(new[] { null, fixture.Biomes[0], fixture.Biomes[0] }).Reverse(),
                fixture.SatelliteRules.Concat(new[]
                {
                    null, fixture.SatelliteRules[0], fixture.SatelliteRules[0]
                }).Reverse(),
                rng);
            AssertInvalid(result,
                SatelliteSeedPlacementErrorCode.NullDefinition,
                SatelliteSeedPlacementErrorCode.DuplicateDefinitionId);
            Assert.That(result.Errors.Count(value => value.Code == SatelliteSeedPlacementErrorCode.NullDefinition),
                Is.EqualTo(1));
            Assert.That(rng.DrawCount, Is.Zero);
        }

        [Test]
        public void Place_IsStableAcrossDefinitionOrderCultureAndPlacerReuse()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                var placer = new SatelliteSeedPlacer();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var first = placer.Place(fixture.Growth, fixture.Profile,
                    fixture.Biomes.Reverse(), fixture.SatelliteRules.Reverse(),
                    new DeterministicRngStream(77));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                var second = placer.Place(fixture.Growth, fixture.Profile,
                    fixture.Biomes, fixture.SatelliteRules,
                    new DeterministicRngStream(77));
                Assert.That(Signature(first), Is.EqualTo(Signature(second)));
                Assert.That(Signature(second), Is.EqualTo(Signature(
                    Place(fixture, new DeterministicRngStream(77)))));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void Place_IndependentStreamActivityDoesNotAlterPlacement()
        {
            var unrelated = new DeterministicRngStream(99);
            for (var draw = 0; draw < 100; draw++) unrelated.NextUInt64();
            var first = Place(fixture, new DeterministicRngStream(123));
            var second = Place(fixture, new DeterministicRngStream(123));
            Assert.That(Signature(first), Is.EqualTo(Signature(second)));
        }

        [Test]
        public void Place_DefensivelyCopiesAndExposesReadOnlyEvidence()
        {
            var biomes = fixture.Biomes.ToArray();
            var rules = fixture.SatelliteRules.ToArray();
            var result = new SatelliteSeedPlacer().Place(
                fixture.Growth, fixture.Profile, biomes, rules, new DeterministicRngStream(5));
            var signature = Signature(result);
            Array.Clear(biomes, 0, biomes.Length);
            Array.Reverse(rules);
            Assert.That(Signature(result), Is.EqualTo(signature));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SatelliteSeedPlacementRecord>)result.Diagnostics.Records).Add(null));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SatelliteRulePlacementDiagnostics>)result.Diagnostics.Rules).Add(null));
        }

        [Test]
        public void Place_PublicApiAndRuntimeSurfaceAreImmutableAndExact()
        {
            var method = typeof(SatelliteSeedPlacer).GetMethod("Place");
            Assert.That(method, Is.Not.Null);
            Assert.That(method.GetParameters().Select(value => value.ParameterType), Is.EqualTo(new[]
            {
                typeof(CorePatchGrowthPublication), typeof(GenerationProfileDefinition),
                typeof(IEnumerable<BiomeTypeDefinition>), typeof(IEnumerable<BiomePatchRuleDefinition>),
                typeof(DeterministicRngStream)
            }));
            var types = new[]
            {
                typeof(SatellitePatchIdFactory), typeof(SatelliteSeedPlacementError),
                typeof(SatelliteSeedPlacementRecord), typeof(SatelliteRulePlacementDiagnostics),
                typeof(SatelliteSeedPlacementDiagnostics), typeof(SatelliteSeedPlacementPublication),
                typeof(SatelliteSeedPlacementResult), typeof(SatelliteSeedPlacer)
            };
            foreach (var type in types)
            {
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty, type.Name);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Any(property => property.SetMethod != null && property.SetMethod.IsPublic), Is.False, type.Name);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Any(field => !field.IsLiteral && !field.IsInitOnly), Is.False, type.Name);
                var surface = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(member => member.ToString()).ToArray();
                Assert.That(surface.Any(value => value.Contains("UnityEngine") || value.Contains("UnityEditor") ||
                    value.Contains("System.Random") || value.Contains("System.IO") || value.Contains("DateTime")),
                    Is.False, type.Name);
            }
        }

        [Test]
        public void FrozenStatusAndReasonEnumsHaveExactOrder()
        {
            Assert.That(Enum.GetNames(typeof(SatelliteSeedPlacementStatus)),
                Is.EqualTo(new[] { "Completed", "InvalidInput", "RetryRequired" }));
            Assert.That(Enum.GetNames(typeof(SatelliteSeedCandidateRejectionReason)),
                Is.EqualTo(new[] { "WorldEdgeForbidden", "SameBiomeDistanceTooSmall" }));
        }

        [Test]
        public void FrozenErrorCodesHaveExactOrder()
        {
            Assert.That(Enum.GetNames(typeof(SatelliteSeedPlacementErrorCode)), Is.EqualTo(new[]
            {
                "MissingGrowthPublication", "InvalidGrowthPublication", "MissingSourceSiteSnapshot",
                "InvalidSourceSiteSnapshot", "MissingGenerationProfile", "InvalidGenerationProfile",
                "MissingBiomeTypes", "MissingSatelliteRules", "NullDefinition", "DuplicateDefinitionId",
                "MissingBiomeDefinition", "UnexpectedBiomeDefinition", "MissingSatelliteRule",
                "UnexpectedSatelliteRule", "InvalidBiomeDefinition", "InvalidSatelliteRule",
                "DefinitionIdentityMismatch", "InvalidCorePatchState", "InvalidReservationState",
                "MissingBiomePatchRng", "InvalidBiomePatchRngState", "PatchCountLimitExceeded",
                "InternalInvariantViolation", "CandidateAttemptsExhausted"
            }));
        }

        private static SatelliteSeedPlacementResult Place(
            Fixture source,
            DeterministicRngStream rng)
        {
            return new SatelliteSeedPlacer().Place(
                source.Growth,
                source.Profile,
                source.Biomes,
                source.SatelliteRules,
                rng);
        }

        private static Fixture BuildFixture(bool blockAllCandidates)
        {
            var definitions = BuildBiomeDefinitions();
            var routeDefinitions = BuildRouteDefinitions();
            var source = BuildSourceSnapshot(blockAllCandidates);
            var initialization = new CorePatchSeedInitializer().Initialize(
                source,
                definitions.Biomes,
                definitions.CoreRules);
            if (!initialization.Succeeded)
                throw new InvalidOperationException(string.Join("\n", initialization.Errors.Select(value =>
                    value.Code + ":" + value.Message)));
            var growth = new CorePatchGrower().Grow(
                initialization.Publication,
                definitions.Biomes,
                definitions.CoreRules);
            if (!growth.Succeeded)
                throw new InvalidOperationException(string.Join("\n", growth.Errors.Select(value =>
                    value.Code + ":" + value.Message)));

            return new Fixture(
                source,
                growth.Publication,
                routeDefinitions,
                routeDefinitions.GenerationProfiles["GEN_MOONPALACE_V1"],
                definitions.Biomes,
                definitions.SatelliteRules);
        }

        private static SiteReservationSnapshot BuildSourceSnapshot(bool blockAllCandidates)
        {
            var forge = new SectorCoord(2, 2);
            var cassia = new SectorCoord(8, 2);
            var dough = new SectorCoord(2, 8);
            var crater = new SectorCoord(8, 8);
            var reservations = new List<SiteReservation>
            {
                blockAllCandidates
                    ? CreateBlockingStart(forge, cassia, dough, crater)
                    : CreateReservation(0, StartId, "WORLD_MOONPALACE_V1",
                        SiteReservationKind.Start, string.Empty, new SectorCoord(0, 0), 1),
                CreateReservation(1, BossId, "SITE_MOON_BOSS_VAULT",
                    SiteReservationKind.Boss, "BIO_ABANDONED_MILL", new SectorCoord(12, 12), 1),
                CreateReservation(2, ForgeId, "SITE_MOON_SEAL_FORGE",
                    SiteReservationKind.Forge, "BIO_ABANDONED_MILL", forge, 1),
                CreateReservation(3, CassiaId, "SITE_CASSIA_SAP_HEART",
                    SiteReservationKind.CoreResource, "BIO_CASSIA_ROOT", cassia, 1),
                CreateReservation(4, DoughId, "SITE_DEEP_STAR_YEAST",
                    SiteReservationKind.CoreResource, "BIO_MOON_DOUGH", dough, 1),
                CreateReservation(5, CraterId, "SITE_MOON_CORE_METEOR",
                    SiteReservationKind.CoreResource, "BIO_MOON_CRATER", crater, 1),
                CreateReservation(6, VillageId, "SITE_PRIMARY_VILLAGE",
                    SiteReservationKind.Village, string.Empty, new SectorCoord(0, 12), 2)
            };
            var byId = reservations.ToDictionary(value => value.ReservationId.Value, StringComparer.Ordinal);
            var seeds = new[]
            {
                CoreSeed(byId[ForgeId], "BIO_ABANDONED_MILL", "PATCH_MILL_CORE", 4),
                CoreSeed(byId[CassiaId], "BIO_CASSIA_ROOT", "PATCH_ROOT_CORE", 5),
                CoreSeed(byId[DoughId], "BIO_MOON_DOUGH", "PATCH_DOUGH_CORE", 5),
                CoreSeed(byId[CraterId], "BIO_MOON_CRATER", "PATCH_CRATER_CORE", 5)
            };
            return new SiteReservationSnapshot(
                KnownWorldSeed,
                reservations,
                CreateSectorReservations(reservations),
                seeds);
        }

        private static SiteReservation CreateBlockingStart(params SectorCoord[] coreOrigins)
        {
            var protectedIndices = new HashSet<int>();
            foreach (var origin in coreOrigins)
            {
                var center = WorldGridIndex.ToIndex(origin);
                protectedIndices.Add(center);
                foreach (var neighbor in new[]
                {
                    WorldGridIndex.GetLeftIndex(center), WorldGridIndex.GetRightIndex(center),
                    WorldGridIndex.GetUpIndex(center), WorldGridIndex.GetDownIndex(center)
                })
                    if (neighbor != SectorNeighborIndices.NoNeighbor) protectedIndices.Add(neighbor);
            }
            protectedIndices.Add(WorldGridIndex.ToIndex(new SectorCoord(12, 12)));
            protectedIndices.Add(WorldGridIndex.ToIndex(new SectorCoord(0, 12)));
            protectedIndices.Add(WorldGridIndex.ToIndex(new SectorCoord(1, 12)));

            var cells = Enumerable.Range(0, WorldGenConstants.SectorCount)
                .Where(index => !protectedIndices.Contains(index))
                .Select(index => WorldGridIndex.ToCoordinate(index))
                .Select(coord => new SiteFootprintCell(
                    coord.X, coord.Y, "START", string.Empty, string.Empty,
                    Array.Empty<SiteEntrySide>()));
            return new SiteReservation(
                new SiteReservationId(StartId),
                SiteReservationKind.Start,
                "WORLD_MOONPALACE_V1",
                new SectorCoord(0, 0),
                new SiteFootprint(13, 13, SiteFootprintTransform.R0, cells),
                string.Empty,
                0,
                Array.Empty<SiteEntryAnchor>());
        }

        private static SiteReservation CreateReservation(
            int order,
            string reservationId,
            string sourceDefinitionId,
            SiteReservationKind kind,
            string biomeId,
            SectorCoord origin,
            int width)
        {
            var cells = Enumerable.Range(0, width).Select(localX => new SiteFootprintCell(
                localX,
                0,
                kind == SiteReservationKind.Start ? "START" : "CORE",
                biomeId,
                string.Empty,
                Array.Empty<SiteEntrySide>()));
            return new SiteReservation(
                new SiteReservationId(reservationId),
                kind,
                sourceDefinitionId,
                origin,
                new SiteFootprint(width, 1, SiteFootprintTransform.R0, cells),
                biomeId,
                order,
                Array.Empty<SiteEntryAnchor>());
        }

        private static CoreBiomeSeed CoreSeed(
            SiteReservation reservation,
            string biomeId,
            string ruleId,
            int minimum)
        {
            return new CoreBiomeSeed(
                reservation.ReservationId,
                biomeId,
                ruleId,
                reservation.OccupiedSectors.OrderBy(WorldGridIndex.ToIndex).First(),
                minimum,
                1);
        }

        private static List<SectorReservation> CreateSectorReservations(
            IEnumerable<SiteReservation> reservations)
        {
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var reservation in reservations)
                foreach (var coordinate in reservation.OccupiedSectors)
                {
                    if (!reservation.TryGetFootprintCell(coordinate, out var cell))
                        throw new InvalidOperationException("Footprint lookup failed.");
                    occupied.Add(coordinate, Tuple.Create(reservation, cell));
                }

            var result = new List<SectorReservation>();
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

        private static BiomeDefinitions BuildBiomeDefinitions()
        {
            var specs = CreateBiomeFileSpecs();
            var biomeRows = new[]
            {
                BiomeRow("BIO_ABANDONED_MILL"), BiomeRow("BIO_CASSIA_ROOT"),
                BiomeRow("BIO_MOON_DOUGH"), BiomeRow("BIO_MOON_CRATER")
            };
            var patchRows = new[]
            {
                PatchRow("PATCH_MILL_CORE", "BIO_ABANDONED_MILL", "CORE", 4, 14, 1, 1, 1, 1, false),
                PatchRow("PATCH_ROOT_CORE", "BIO_CASSIA_ROOT", "CORE", 5, 18, 1, 1, 1, 1, false),
                PatchRow("PATCH_DOUGH_CORE", "BIO_MOON_DOUGH", "CORE", 5, 18, 1, 1, 1, 1, true),
                PatchRow("PATCH_CRATER_CORE", "BIO_MOON_CRATER", "CORE", 5, 18, 1, 1, 1, 1, true),
                PatchRow("PATCH_CRATER_SAT", "BIO_MOON_CRATER", "SATELLITE", 2, 16, 3, 0, 3, 70, true),
                PatchRow("PATCH_DOUGH_SAT", "BIO_MOON_DOUGH", "SATELLITE", 2, 14, 3, 0, 3, 70, true),
                PatchRow("PATCH_MILL_SAT", "BIO_ABANDONED_MILL", "SATELLITE", 2, 10, 3, 0, 2, 45, false),
                PatchRow("PATCH_ROOT_SAT", "BIO_CASSIA_ROOT", "SATELLITE", 2, 14, 3, 0, 3, 70, false)
            };
            var sources = specs.Select(spec => BuildBiomeSource(
                spec,
                spec.FileName == "biome_types.csv" ? biomeRows :
                spec.FileName == "biome_patch_rules.csv" ? patchRows : Array.Empty<string[]>())).ToArray();
            var result = new BiomeBoundaryDefinitionBuilder().Build(sources);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            var biomes = result.DefinitionSet.BiomeTypes.Values.ToArray();
            var rules = result.DefinitionSet.BiomePatchRules.Values.ToArray();
            return new BiomeDefinitions(
                biomes,
                rules.Where(value => value.PatchRole == "CORE").ToArray(),
                rules.Where(value => value.PatchRole == "SATELLITE").ToArray());
        }

        private static WorldRouteDefinitionSet BuildRouteDefinitions()
        {
            var profileSpec = File("generation_profiles.csv",
                "generation_profile_id:ID", "world_profile_id:ID", "mandatory_sector_min:INT",
                "mandatory_sector_max:INT", "type0_sector_min:INT", "type0_sector_max:INT",
                "reserved_sector_min:INT", "reserved_sector_max:INT", "inactive_sector_min:INT",
                "inactive_sector_max:INT", "start_edge_ring_min:INT", "start_edge_ring_max:INT",
                "mandatory_loop_min:INT", "mandatory_loop_max:INT", "optional_region_depth_min:INT",
                "optional_region_depth_max:INT", "optional_region_count_min:INT",
                "optional_region_count_max:INT", "site_reservation_retry_max:INT", "biome_retry_max:INT",
                "route_retry_max:INT", "sector_solve_retry_max:INT", "active:BOOL", "notes:STRING");
            var rngSpec = File("rng_streams.csv",
                "rng_stream_id:ID", "salt_hex:HEX", "reset_scope:ENUM",
                "description_ko:STRING", "active:BOOL");
            var profileRow = new[]
            {
                "GEN_MOONPALACE_V1", "WORLD_MOONPALACE_V1", "40", "100", "20", "80",
                "7", "30", "0", "100", "0", "1", "2", "4", "1", "4", "1", "8",
                "200", "100", "100", "8", "1", string.Empty
            };
            var rngRows = new[]
            {
                new[] { "RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD", "test", "1" },
                new[] { "RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS", "test", "1" },
                new[] { "RNG_ROUTE", "C00FEE12AB341901", "PASS", "test", "1" },
                new[] { "RNG_TYPE0", "D15EA5E007A4C883", "PASS", "test", "1" },
                new[] { "RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR", "test", "1" },
                new[] { "RNG_POPULATION", "F123456789ABCDEF", "SPAWN", "test", "1" }
            };
            var sources = new List<WorldRouteDefinitionSource>
            {
                BuildWorldRouteSource(profileSpec, new[] { profileRow }),
                BuildWorldRouteSource(rngSpec, rngRows)
            };
            var empty = Array.Empty<string[]>();
            sources.Add(BuildWorldRouteSource(File("world_profiles.csv",
                "world_profile_id:ID", "display_name_ko:STRING", "width_tiles:INT", "height_tiles:INT",
                "sector_width_tiles:INT", "sector_height_tiles:INT", "sector_cols:INT", "sector_rows:INT",
                "micro_width_tiles:INT", "micro_height_tiles:INT", "micro_cols_per_sector:INT",
                "micro_rows_per_sector:INT", "min_completion_distance_tiles:INT",
                "max_shortest_completion_distance_tiles:INT", "normal_completion_min_tiles:INT",
                "normal_completion_max_tiles:INT", "optional_completion_max_tiles:INT",
                "max_revisit_ratio:FLOAT", "required_village_count:INT", "active:BOOL", "notes:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("generation_passes.csv",
                "generation_profile_id:ID", "pass_order:INT", "pass_id:ID", "class_name:STRING",
                "rng_stream_id:ID", "input_artifacts:ID_LIST", "output_artifacts:ID_LIST",
                "failure_policy:ENUM", "max_retry_count:INT", "enabled:BOOL", "notes:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("sector_route_masks.csv",
                "route_mask_id:ID", "route_type:INT", "open_l:BOOL", "open_r:BOOL", "open_u:BOOL",
                "open_d:BOOL", "mandatory_allowed:BOOL", "description_ko:STRING", "active:BOOL"), empty));
            sources.Add(BuildWorldRouteSource(File("socket_band_definitions.csv",
                "band_id:ID", "axis:ENUM", "min_local_coord:INT", "max_local_coord:INT",
                "recommended_center:FLOAT", "minimum_clearance_tiles:INT", "description_ko:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("edge_signatures.csv",
                "edge_signature_id:ID", "axis:ENUM", "band_id:ID", "traversal_kind:ENUM",
                "ground_entry_height:INT", "clearance_width:INT", "clearance_height:INT",
                "tool_requirement:ENUM", "mandatory_allowed:BOOL", "tags:ID_LIST", "notes:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("edge_signature_compatibility.csv",
                "signature_a:ID", "signature_b:ID", "compatible:BOOL", "adapter_microchunk_pool_id:ID",
                "notes:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("sector_recipe_catalog.csv",
                "sector_recipe_id:ID", "display_name_ko:STRING", "route_type:INT", "route_mask_id:ID",
                "primary_biome_id:ID", "secondary_biome_id:ID", "boundary_profile_id:ID", "recipe_kind:ENUM",
                "microchunk_budget_profile_id:ID", "selection_weight:INT", "supports_special_entry:BOOL",
                "supports_village_entry:BOOL", "active:BOOL", "notes:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("sector_recipe_cells.csv",
                "sector_recipe_id:ID", "chunk_x:INT", "chunk_y:INT", "cell_role:ENUM",
                "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_usage_class:ENUM_LIST",
                "required_route_roles:ID_LIST", "required_biome_ids:ID_LIST", "required_signature_l:ID",
                "required_signature_r:ID", "required_signature_u:ID", "required_signature_d:ID",
                "transform_policy:ENUM_LIST", "notes:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("sector_recipe_paths.csv",
                "sector_recipe_id:ID", "path_id:ID", "path_order:INT", "chunk_x:INT", "chunk_y:INT",
                "enter_side:ENUM", "exit_side:ENUM", "mandatory:BOOL", "traversal_kind:ENUM",
                "max_jump_tiles:INT", "notes:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("sector_external_sockets.csv",
                "sector_recipe_id:ID", "socket_id:ID", "side:ENUM", "edge_chunk_index:INT", "band_id:ID",
                "traversal_kind:ENUM", "mandatory_allowed:BOOL", "edge_signature_id:ID", "notes:STRING"), empty));
            sources.Add(BuildWorldRouteSource(File("sector_recipe_pool_entries.csv",
                "sector_recipe_pool_id:ID", "entry_order:INT", "sector_recipe_id:ID", "weight:INT",
                "min_repeat_distance_sectors:INT", "required_patch_role:ENUM", "active:BOOL"), empty));

            var result = new WorldRouteDefinitionBuilder().Build(sources);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors.Select(error =>
                error.FileName + ":" + error.ErrorCode + ":" + error.ColumnOrder + ":" +
                error.ColumnName + ":" + error.Message)));
            return result.DefinitionSet;
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

        private static string[] PatchRow(
            string ruleId,
            string biomeId,
            string role,
            int minimum,
            int maximum,
            int distance,
            int countMinimum,
            int countMaximum,
            int weight,
            bool edge)
        {
            return new[]
            {
                ruleId, biomeId, role,
                minimum.ToString(CultureInfo.InvariantCulture),
                maximum.ToString(CultureInfo.InvariantCulture),
                distance.ToString(CultureInfo.InvariantCulture),
                countMinimum.ToString(CultureInfo.InvariantCulture),
                countMaximum.ToString(CultureInfo.InvariantCulture),
                weight.ToString(CultureInfo.InvariantCulture), edge ? "1" : "0",
                role == "CORE" ? "1" : "0", "0", "0.35", "1", "1", "1", "1", "0.5", "1", string.Empty
            };
        }

        private static FileSpec[] CreateBiomeFileSpecs()
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

        private static BiomeBoundaryDefinitionSource BuildBiomeSource(
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

        private static WorldRouteDefinitionSource BuildWorldRouteSource(
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
            return new WorldRouteDefinitionSource(schema, parsed);
        }

        private static FileSpec File(string fileName, params string[] definitions)
        {
            return new FileSpec(fileName, definitions.Select(value =>
            {
                var parts = value.Split(':');
                var allowed = parts[0] == "patch_role" ? "CORE|SATELLITE|INTRUSION" :
                    parts[0] == "reset_scope" ? "WORLD|PASS|SECTOR|PATCH|SITE|SPAWN" :
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

        private static ulong FindState(IReadOnlyList<int> desired)
        {
            for (ulong state = 0; state < 1000000; state++)
            {
                var rng = new DeterministicRngStream(state);
                if (rng.NextInt(0, 4) == desired[0] &&
                    rng.NextInt(0, 4) == desired[1] &&
                    rng.NextInt(0, 3) == desired[2] &&
                    rng.NextInt(0, 4) == desired[3])
                    return state;
            }
            throw new InvalidOperationException("Requested count vector was not found.");
        }

        private static int Manhattan(int left, int right)
        {
            var a = WorldGridIndex.ToCoordinate(left);
            var b = WorldGridIndex.ToCoordinate(right);
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private static bool IsWorldEdge(SectorCoord value)
        {
            return value.X == 0 || value.X == 12 || value.Y == 0 || value.Y == 12;
        }

        private static string Signature(SatelliteSeedPlacementResult result)
        {
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            return result.Status + "|" + result.Diagnostics.RngDrawCountAfter + "|" +
                   string.Join(";", result.Diagnostics.Rules.Select(value =>
                       value.PatchRuleId + ":" + value.DesiredSeedCount + ":" + value.CandidateAttemptCount)) + "|" +
                   string.Join(";", result.Diagnostics.Records.Select(value =>
                       value.PatchId.Value + ":" + value.SectorIndex + ":" + value.SameBiomeDistance));
        }

        private static string FormatErrors(SatelliteSeedPlacementResult result)
        {
            return string.Join("\n", result.Errors.Select(value =>
                value.Code + ":" + value.DefinitionId + ":" + value.BiomeId + ":" + value.Message));
        }

        private static void AssertInvalid(
            SatelliteSeedPlacementResult result,
            params SatelliteSeedPlacementErrorCode[] expected)
        {
            Assert.That(result.Status, Is.EqualTo(SatelliteSeedPlacementStatus.InvalidInput), FormatErrors(result));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            foreach (var code in expected)
                Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code));
        }

        private static int CompareErrors(
            SatelliteSeedPlacementError left,
            SatelliteSeedPlacementError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.BiomeId, right.BiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SatelliteOrdinal.CompareTo(right.SatelliteOrdinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.RequiredCount.CompareTo(right.RequiredCount);
            if (value != 0) return value;
            value = left.AvailableCount.CompareTo(right.AvailableCount);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static void WithCulture(string name, Action action)
        {
            var culture = CultureInfo.CurrentCulture;
            var ui = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
                action();
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = ui;
            }
        }

        private const string StartId = "RSV_00_WORLD_MOONPALACE_V1";
        private const string BossId = "RSV_01_SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "RSV_02_SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "RSV_03_SITE_CASSIA_SAP_HEART";
        private const string DoughId = "RSV_04_SITE_DEEP_STAR_YEAST";
        private const string CraterId = "RSV_05_SITE_MOON_CORE_METEOR";
        private const string VillageId = "RSV_06_SITE_PRIMARY_VILLAGE";

        private sealed class Fixture
        {
            public Fixture(
                SiteReservationSnapshot source,
                CorePatchGrowthPublication growth,
                WorldRouteDefinitionSet routeDefinitions,
                GenerationProfileDefinition profile,
                IReadOnlyList<BiomeTypeDefinition> biomes,
                IReadOnlyList<BiomePatchRuleDefinition> satelliteRules)
            {
                Source = source;
                Growth = growth;
                RouteDefinitions = routeDefinitions;
                Profile = profile;
                Biomes = biomes;
                SatelliteRules = satelliteRules;
            }

            public SiteReservationSnapshot Source { get; }
            public CorePatchGrowthPublication Growth { get; }
            public WorldRouteDefinitionSet RouteDefinitions { get; }
            public GenerationProfileDefinition Profile { get; }
            public IReadOnlyList<BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> SatelliteRules { get; }
        }

        private sealed class BiomeDefinitions
        {
            public BiomeDefinitions(
                IReadOnlyList<BiomeTypeDefinition> biomes,
                IReadOnlyList<BiomePatchRuleDefinition> coreRules,
                IReadOnlyList<BiomePatchRuleDefinition> satelliteRules)
            {
                Biomes = biomes;
                CoreRules = coreRules;
                SatelliteRules = satelliteRules;
            }
            public IReadOnlyList<BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> CoreRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> SatelliteRules { get; }
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
