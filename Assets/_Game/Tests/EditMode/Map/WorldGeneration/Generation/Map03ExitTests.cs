using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class Map03ExitTests
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string GenerationId = "GEN_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";
        private const string VillageId = "SITE_PRIMARY_VILLAGE";
        private const string VillageProfileId = "VIL_MOON_PRIMARY";
        private const string Layout5Id = "VLAY_STANDARD_5_A";
        private const string Layout6Id = "VLAY_STANDARD_6_A";
        private const string CraterBiomeId = "BIO_MOON_CRATER";
        private const string CassiaBiomeId = "BIO_CASSIA_ROOT";
        private const string MillBiomeId = "BIO_ABANDONED_MILL";
        private const string DoughBiomeId = "BIO_MOON_DOUGH";

        public static IEnumerable LockedContractCases()
        {
            for (var index = 0; index < 96; index++)
                yield return new TestCaseData(index).SetName(
                    "LockedContract_" + index.ToString("D2", CultureInfo.InvariantCulture));
        }

        [TestCaseSource(nameof(LockedContractCases))]
        public void LockedIdentityAndPublicContractSmoke(int index)
        {
            var ids = new[]
            {
                WorldId, BossId, ForgeId, CassiaId, YeastId, MeteorId, VillageId,
                VillageProfileId, Layout5Id, Layout6Id, CraterBiomeId, CassiaBiomeId,
                MillBiomeId, DoughBiomeId
            };
            Assert.That(ids[index % ids.Length], Is.Not.Empty);
            Assert.That(WorldGenConstants.SectorColumns, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorRows, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorCount, Is.EqualTo(169));
            Assert.That(Enum.GetValues(typeof(SiteReservationKind)).Length, Is.EqualTo(5));
            Assert.That(Enum.GetValues(typeof(SiteFootprintTransform)).Length, Is.EqualTo(4));
            Assert.That(typeof(SiteReservationBacktracker).Assembly.GetName().Name,
                Is.EqualTo("Game.Map.Runtime"));
        }

        [Test]
        public void PreparedStarterFixture_PublishesExactPhaseMatrix()
        {
            var fixture = Prepare();

            Assert.That(fixture.Catalog.TotalCandidateCount, Is.EqualTo(933));
            Assert.That(fixture.TransformEvaluationCount, Is.EqualTo(3468));
            Assert.That(fixture.Groups.Sum(item => item.OptionCount), Is.EqualTo(3156));
            Assert.That(fixture.SourceRejectionCount, Is.EqualTo(312));
            Assert.That(fixture.Groups, Has.Count.EqualTo(6));
            Assert.That(fixture.Policy.Constraints, Has.Count.EqualTo(15));
            Assert.That(fixture.Groups.Select(item => item.Key.SourceDefinitionId), Is.EqualTo(
                new[] { WorldId, BossId, ForgeId, CassiaId, YeastId, MeteorId }));
        }

        [Test]
        public void VillageSchedule_TenThousandSeeds_MeetsReducedDistributionGate()
        {
            var fixture = Prepare();
            var watch = Stopwatch.StartNew();
            var rolls = new byte[10000];
            var counts = new int[3];
            var outside = 0;
            for (var seed = 0; seed < rolls.Length; seed++)
            {
                var rng = fixture.RngFactory.Create(
                    WorldGenerationRngStreams.WorldSiteStreamId, (ulong)seed,
                    RngStreamScope.World(0));
                for (var draw = 0; draw < 3156; draw++) rng.NextUInt64();
                var roll = rng.NextInt(100);
                rolls[seed] = (byte)roll;
                if (roll < 0 || roll > 99) outside++;
                else counts[roll < 20 ? 0 : roll < 70 ? 1 : 2]++;
            }
            watch.Stop();

            var expected = new[] { 2000.0, 5000.0, 3000.0 };
            var chiSquare = counts.Select((count, i) =>
                (count - expected[i]) * (count - expected[i]) / expected[i]).Sum();
            var digest = Sha256(rolls);
            var percentages = counts.Select(value => value / 100.0).ToArray();
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "MAP03_EXIT_10K_SCHEDULE near={0} middle={1} far={2} near_pct={3:F6} " +
                "middle_pct={4:F6} far_pct={5:F6} chi_square={6:F9} outside={7} " +
                "digest={8} elapsed_ms={9}", counts[0], counts[1], counts[2],
                percentages[0], percentages[1], percentages[2], chiSquare, outside,
                digest, watch.ElapsedMilliseconds));

            Assert.That(counts.Sum(), Is.EqualTo(10000));
            Assert.That(outside, Is.Zero);
            Assert.That(counts[0], Is.InRange(1925, 2075));
            Assert.That(counts[1], Is.InRange(4925, 5075));
            Assert.That(counts[2], Is.InRange(2925, 3075));
            Assert.That(Math.Abs(percentages[0] - 20.0), Is.LessThanOrEqualTo(0.75));
            Assert.That(Math.Abs(percentages[1] - 50.0), Is.LessThanOrEqualTo(0.75));
            Assert.That(Math.Abs(percentages[2] - 30.0), Is.LessThanOrEqualTo(0.75));
            Assert.That(chiSquare, Is.LessThanOrEqualTo(13.815511));
        }

        [Test]
        public void FullPipeline_OneThousandSeeds_ResolvesWithinEightFreshAttempts()
        {
            var fixture = Prepare();
            var watch = Stopwatch.StartNew();
            var histogram = new int[8];
            var finalBuckets = new int[3];
            var terminal = new int[6];
            var reasonCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var records = new StringBuilder(4000000);
            var initialRetries = 0;
            var retriedWorlds = 0;
            var totalAttempts = 0;
            var resolved = 0;
            var unresolved = 0;
            var invalid = 0;
            var maximumAttempt = 0;
            for (var seed = 0; seed < 1000; seed++)
            {
                AttemptRecord completed = null;
                for (var attempt = 0; attempt < 8; attempt++)
                {
                    totalAttempts++;
                    var record = RunAttempt(fixture, (ulong)seed, attempt, null, false);
                    CountReason(reasonCounts, record);
                    if (record.Succeeded)
                    {
                        completed = record;
                        histogram[attempt]++;
                        maximumAttempt = Math.Max(maximumAttempt, attempt);
                        break;
                    }
                    terminal[record.TerminalOrdinal]++;
                    if (record.InvalidInput)
                    {
                        invalid++;
                        break;
                    }
                    Assert.That(record.RetryRequired, Is.True, record.Terminal);
                    if (attempt == 0) initialRetries++;
                }
                if (completed == null)
                {
                    unresolved++;
                    continue;
                }
                resolved++;
                if (completed.AttemptOrdinal > 0) retriedWorlds++;
                finalBuckets[completed.Village.Diagnostics.SelectedBucket.BucketOrdinal]++;
                AssertSuccessfulRecord(completed, (ulong)seed);
                records.Append(completed.Canonical).Append('\n');
            }
            watch.Stop();
            var digest = Sha256(Encoding.UTF8.GetBytes(records.ToString()));
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "MAP03_EXIT_1K_FULL initial_retry={0} retried={1} resolved={2} unresolved={3} " +
                "invalid={4} attempts={5} max_attempt={6} histogram={7} buckets={8}/{9}/{10} " +
                "terminal={11} reasons={12} digest={13} elapsed_ms={14}",
                initialRetries, retriedWorlds, resolved, unresolved, invalid, totalAttempts,
                maximumAttempt, string.Join(",", histogram), finalBuckets[0], finalBuckets[1],
                finalBuckets[2], string.Join(",", terminal), FormatReasons(reasonCounts), digest,
                watch.ElapsedMilliseconds));

            Assert.That(initialRetries, Is.LessThanOrEqualTo(50));
            Assert.That(invalid, Is.Zero);
            Assert.That(unresolved, Is.Zero);
            Assert.That(resolved, Is.EqualTo(1000));
            Assert.That(finalBuckets.Sum(), Is.EqualTo(1000));
            Assert.That(finalBuckets[0], Is.InRange(180, 220));
            Assert.That(finalBuckets[1], Is.InRange(480, 520));
            Assert.That(finalBuckets[2], Is.InRange(280, 320));
            Assert.That(totalAttempts - 1000, Is.EqualTo(terminal.Sum()));
        }

        [Test]
        public void Determinism_FreshReusedReverseCultureAndAttemptDomainsRemainExact()
        {
            var fixture = Prepare();
            var fresh = new StringBuilder();
            var reused = new StringBuilder();
            var reverse = new StringBuilder();
            var services = new Services();
            for (var seed = 0; seed < 102; seed++)
            {
                fresh.Append(RunToSuccess(fixture, (ulong)seed, null, false).Canonical).Append('\n');
                reused.Append(RunToSuccess(fixture, (ulong)seed, services, false).Canonical).Append('\n');
                reverse.Append(RunToSuccess(fixture, (ulong)seed, services, true).Canonical).Append('\n');
            }
            Assert.That(Sha256(Encoding.UTF8.GetBytes(reused.ToString())),
                Is.EqualTo(Sha256(Encoding.UTF8.GetBytes(fresh.ToString()))));
            Assert.That(Sha256(Encoding.UTF8.GetBytes(reverse.ToString())),
                Is.EqualTo(Sha256(Encoding.UTF8.GetBytes(fresh.ToString()))));

            var original = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                var en = RunToSuccess(fixture, 4660, services, false).Canonical;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                Assert.That(RunToSuccess(fixture, 4660, services, false).Canonical, Is.EqualTo(en));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
                CultureInfo.CurrentUICulture = originalUi;
            }

            var expected = RunAttempt(fixture, 4660, 0, null, false).Canonical;
            for (var run = 0; run < 100; run++)
                Assert.That(RunAttempt(fixture, 4660, 0, services, false).Canonical,
                    Is.EqualTo(expected), "repeat " + run);

            var site0 = fixture.RngFactory.Create(WorldGenerationRngStreams.WorldSiteStreamId,
                4660, RngStreamScope.World(0)).NextUInt64();
            var site1 = fixture.RngFactory.Create(WorldGenerationRngStreams.WorldSiteStreamId,
                4660, RngStreamScope.World(1)).NextUInt64();
            Assert.That(site1, Is.Not.EqualTo(site0));
            foreach (var streamId in new[]
            {
                WorldGenerationRngStreams.BiomePatchStreamId,
                WorldGenerationRngStreams.RouteStreamId,
                WorldGenerationRngStreams.Type0StreamId,
                WorldGenerationRngStreams.SectorRecipeStreamId,
                WorldGenerationRngStreams.PopulationStreamId
            })
            {
                var left = fixture.RngFactory.Create(streamId, 4660, Scope(streamId)).NextUInt64();
                var right = fixture.RngFactory.Create(streamId, 4660, Scope(streamId)).NextUInt64();
                Assert.That(right, Is.EqualTo(left), streamId);
            }
            Assert.That(fixture.Groups.Sum(item => item.OptionCount), Is.EqualTo(3156));
        }

        [TestCase(0UL)]
        [TestCase(4660UL)]
        [TestCase(ulong.MaxValue)]
        public void BoundarySeeds_CompleteOnAttemptZeroWithExactPublication(ulong seed)
        {
            var record = RunAttempt(Prepare(), seed, 0, null, false);
            Assert.That(record.Succeeded, Is.True, record.Terminal);
            AssertSuccessfulRecord(record, seed);
        }

        [Test]
        public void RetryAndInvalidInputClassifications_AreAtomicAndFrozen()
        {
            var fixture = Prepare();
            var noSolutionGroups = ImpossibleGroups(fixture);
            var noSolution = new SiteReservationBacktracker().Search(
                noSolutionGroups, fixture.Policy, SiteCandidateCostWeights.Default,
                SiteReservationSearchLimits.Default,
                fixture.RngFactory.Create(WorldGenerationRngStreams.WorldSiteStreamId, 0,
                    RngStreamScope.World()));
            Assert.That(noSolution.Status, Is.EqualTo(SiteReservationSearchStatus.NoSolution));
            Assert.That(noSolution.RetryRequired, Is.True);
            Assert.That(noSolution.SelectionPlan, Is.Null);

            var limit = new SiteReservationBacktracker().Search(
                noSolutionGroups, fixture.Policy, SiteCandidateCostWeights.Default,
                new SiteReservationSearchLimits(1),
                fixture.RngFactory.Create(WorldGenerationRngStreams.WorldSiteStreamId, 0,
                    RngStreamScope.World()));
            Assert.That(limit.Status,
                Is.EqualTo(SiteReservationSearchStatus.FailedCombinationLimitReached));
            Assert.That(limit.RetryRequired, Is.True);
            Assert.That(limit.SelectionPlan, Is.Null);
            Assert.That(limit.Diagnostics.FailedCombinationCount, Is.EqualTo(1));

            var successful = RunAttempt(fixture, 0, 0, null, false);
            Assert.That(successful.Succeeded, Is.True, successful.Terminal);
            var pressured = Requirements(fixture, successful.Search.SelectionPlan).ToArray();
            pressured[0] = new CoreCapacityRequirement(
                pressured[0].Key, pressured[0].Placement, pressured[0].SpecialMap,
                pressured[0].PrimaryBiome,
                Patch("PATCH_PRESSURE_CORE", pressured[0].PrimaryBiome.BiomeId, 169, 169, true));
            var capacity = new CoreCapacityFloodChecker().Check(
                successful.Search.SelectionPlan, pressured);
            Assert.That(capacity.Status, Is.EqualTo(CoreCapacityFloodStatus.CapacityRejected));
            Assert.That(capacity.RetryRequired, Is.True);
            Assert.That(capacity.Approval, Is.Null);
            Assert.That(capacity.Rejections, Is.Not.Empty);

            var selectedBucket = successful.Village.Diagnostics.SelectedBucket;
            var rejectedVillage = new VillageReservationResult(
                VillageReservationStatus.ReservationRejected, null,
                successful.Village.Diagnostics,
                new[]
                {
                    new VillageReservationRejection(
                        VillageReservationRejectionReason.SelectedBucketHasNoViableCandidate,
                        selectedBucket.BucketOrdinal, selectedBucket.MinDistanceInclusive,
                        selectedBucket.MaxDistanceInclusive,
                        successful.Village.Diagnostics.SourceCandidateCount, 0,
                        "Synthetic exit classification rejection.")
                },
                Array.Empty<VillageReservationError>());
            Assert.That(rejectedVillage.RetryRequired, Is.True);
            Assert.That(rejectedVillage.Approval, Is.Null);
            Assert.That(rejectedVillage.Rejections, Has.Count.EqualTo(1));

            var completedDiagnostics = successful.Validation.Diagnostics;
            var violation = new SiteReservationValidationViolation(
                SiteReservationValidationViolationCode.FootprintOutsideWorld,
                SiteReservationValidationRule.WorldBounds, BossId, string.Empty,
                0, 1, 0, "Synthetic exit classification violation.");
            var rejectedRules = completedDiagnostics.Rules.Select(rule =>
                rule.Rule == SiteReservationValidationRule.WorldBounds
                    ? new SiteReservationRuleResult(
                        rule.Rule, false, 1, rule.MeasuredCount, rule.ExpectedCount,
                        "Synthetic exit classification failure.")
                    : rule).ToArray();
            var rejectedDiagnostics = new SiteReservationValidationDiagnostics(
                rejectedRules, completedDiagnostics.ReservationCount,
                completedDiagnostics.ReservedSectorCount,
                completedDiagnostics.UnreservedSectorCount,
                completedDiagnostics.EntryAnchorCount,
                completedDiagnostics.RequiredEntryCount,
                completedDiagnostics.NonVillageDistanceConstraintCount,
                completedDiagnostics.VillageDistanceCheckCount,
                completedDiagnostics.CoreClusterCheckCount,
                completedDiagnostics.CoreWitnessCount,
                completedDiagnostics.CoreWitnessSectorCount,
                completedDiagnostics.CoreSeedCount, 1);
            var rejectedValidation = new SiteReservationValidationResult(
                SiteReservationValidationStatus.ValidationRejected, null,
                rejectedDiagnostics, new[] { violation },
                Array.Empty<SiteReservationValidationError>());
            Assert.That(rejectedValidation.Status,
                Is.EqualTo(SiteReservationValidationStatus.ValidationRejected));
            Assert.That(rejectedValidation.RetryRequired, Is.True);
            Assert.That(rejectedValidation.Publication, Is.Null);
            Assert.That(rejectedValidation.Violations, Is.Not.Empty);

            var invalidSearch = new SiteReservationBacktracker().Search(null, null, null, null, null);
            var invalidCapacity = new CoreCapacityFloodChecker().Check(null, null);
            var invalidVillage = new VillageReservationSelector().Reserve(
                null, null, null, null, null, null);
            var invalidValidation = new SiteReservationValidator().ValidateAndPublish(
                0, null, null, null, null);
            Assert.That(new[]
            {
                invalidSearch.RetryRequired, invalidCapacity.RetryRequired,
                invalidVillage.RetryRequired, invalidValidation.RetryRequired
            }, Is.All.False);
            Assert.That(invalidSearch.SelectionPlan, Is.Null);
            Assert.That(invalidCapacity.Approval, Is.Null);
            Assert.That(invalidVillage.Approval, Is.Null);
            Assert.That(invalidValidation.Publication, Is.Null);
        }

        private static PreparedFixture Prepare()
        {
            var worldDefinitions = BuildWorldDefinitions();
            var world = worldDefinitions.WorldProfiles[WorldId];
            var generation = worldDefinitions.GenerationProfiles[GenerationId];
            var biomeDefinitions = BuildBiomeDefinitions();
            var biomes = biomeDefinitions.BiomeTypes;
            var rules = biomeDefinitions.BiomePatchRules.Values.ToDictionary(
                item => item.BiomeId, StringComparer.Ordinal);
            var specialDefinitions = BuildSpecialDefinitions(false);
            var maps = specialDefinitions.SpecialMaps.Values.ToArray();
            var cells = specialDefinitions.SpecialMapFootprintCells.ToArray();
            var entries = specialDefinitions.SpecialMapEntrySockets.ToArray();
            var fixedMaps = maps.Where(item => item.SpecialMapId != VillageId).ToArray();
            var catalogResult = new SiteCandidateEnumerator().Enumerate(
                new GridInitializationPass().Execute(0), world, generation, maps);
            if (!catalogResult.Succeeded)
                throw new InvalidOperationException(string.Join("\n",
                    catalogResult.Errors.Select(item => item.ErrorCode + ":" + item.Message)));
            var policyResult = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(WorldId, maps);
            if (!policyResult.Succeeded)
                throw new InvalidOperationException(string.Join("\n",
                    policyResult.Errors.Select(item => item.Code + ":" + item.Message)));

            var solver = new FootprintPlacementSolver();
            var groups = new List<SiteReservationSearchGroup>();
            var evaluations = 0;
            foreach (var candidateGroup in catalogResult.Catalog.Groups)
            {
                var options = new List<SiteReservationSearchOption>();
                if (candidateGroup.Kind == SiteReservationKind.Start)
                {
                    foreach (var candidate in candidateGroup.Candidates)
                    {
                        evaluations++;
                        var placed = solver.SolveStart(candidate, FootprintPlacementBlockers.Empty);
                        if (placed.Succeeded)
                            options.Add(new SiteReservationSearchOption(placed.Placement, -1));
                    }
                    groups.Add(new SiteReservationSearchGroup(
                        new SitePlacementKey(candidateGroup.Kind,
                            candidateGroup.SourceDefinitionId, 0), null, null, null, options));
                    continue;
                }
                var special = fixedMaps.Single(item =>
                    item.SpecialMapId == candidateGroup.SourceDefinitionId);
                foreach (var candidate in candidateGroup.Candidates)
                foreach (SiteFootprintTransform transform in
                         Enum.GetValues(typeof(SiteFootprintTransform)))
                {
                    evaluations++;
                    var placed = solver.SolveSpecialSite(candidate, transform, special,
                        cells.Where(item => item.SpecialMapId == special.SpecialMapId),
                        entries.Where(item => item.SpecialMapId == special.SpecialMapId),
                        FootprintPlacementBlockers.Empty);
                    if (placed.Succeeded)
                        options.Add(new SiteReservationSearchOption(placed.Placement, -1));
                }
                var biome = biomes[special.PrimaryBiomeId];
                groups.Add(new SiteReservationSearchGroup(
                    new SitePlacementKey(candidateGroup.Kind, special.SpecialMapId, 0),
                    special, biome, rules[biome.BiomeId], options));
            }
            var layouts = new[]
            {
                specialDefinitions.VillageLayouts[Layout5Id],
                specialDefinitions.VillageLayouts[Layout6Id]
            };
            return new PreparedFixture(
                world, generation, catalogResult.Catalog, groups, policyResult.Policy,
                maps, cells, entries, biomes, rules,
                specialDefinitions.VillageProfiles[VillageProfileId], layouts,
                new DeterministicRngStreamFactory(worldDefinitions), evaluations,
                evaluations - groups.Sum(item => item.OptionCount));
        }

        private static AttemptRecord RunToSuccess(
            PreparedFixture fixture, ulong seed, Services services, bool reverse)
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var record = RunAttempt(fixture, seed, attempt, services, reverse);
                if (record.Succeeded) return record;
                if (!record.RetryRequired) break;
            }
            throw new AssertionException("The full attempt pipeline did not resolve seed " + seed);
        }

        private static AttemptRecord RunAttempt(
            PreparedFixture fixture,
            ulong seed,
            int attemptOrdinal,
            Services services,
            bool reverse)
        {
            services = services ?? new Services();
            var rng = fixture.RngFactory.Create(WorldGenerationRngStreams.WorldSiteStreamId,
                seed, RngStreamScope.World(attemptOrdinal));
            IEnumerable<SiteReservationSearchGroup> groups = fixture.Groups;
            if (reverse)
                groups = fixture.Groups.Reverse().Select(item => new SiteReservationSearchGroup(
                    item.Key, item.SpecialMap, item.PrimaryBiome, item.CorePatchRule,
                    item.Options.Reverse())).ToArray();
            var search = services.Search.Search(groups, fixture.Policy,
                SiteCandidateCostWeights.Default, SiteReservationSearchLimits.Default, rng);
            if (!search.Succeeded)
                return AttemptRecord.Failure(seed, attemptOrdinal, search, null, null, null);
            var capacity = services.Capacity.Check(
                search.SelectionPlan, Requirements(fixture, search.SelectionPlan));
            if (!capacity.Succeeded)
                return AttemptRecord.Failure(seed, attemptOrdinal, search, capacity, null, null);
            var village = services.Village.Reserve(
                capacity.Approval, fixture.VillageProfile, fixture.Village,
                fixture.VillageEntries, fixture.Layouts, rng);
            if (!village.Succeeded)
                return AttemptRecord.Failure(seed, attemptOrdinal, search, capacity, village, null);

            var bridge = fixture.RngFactory.Create(WorldGenerationRngStreams.WorldSiteStreamId,
                seed, RngStreamScope.World(attemptOrdinal));
            for (var draw = 0; draw < 3156; draw++) bridge.NextUInt64();
            var expectedRoll = bridge.NextInt(100);
            Assert.That(village.Diagnostics.BucketRoll, Is.EqualTo(expectedRoll));
            Assert.That(expectedRoll, Is.InRange(
                village.Diagnostics.SelectedBucket.RollMinInclusive,
                village.Diagnostics.SelectedBucket.RollMaxInclusive));

            var validation = services.Validation.ValidateAndPublish(
                seed, village.Approval,
                reverse ? fixture.Maps.Reverse() : fixture.Maps,
                reverse ? fixture.Cells.Reverse() : fixture.Cells,
                reverse ? fixture.Entries.Reverse() : fixture.Entries);
            return validation.Succeeded
                ? AttemptRecord.Success(seed, attemptOrdinal, search, capacity, village, validation)
                : AttemptRecord.Failure(seed, attemptOrdinal, search, capacity, village, validation);
        }

        private static IReadOnlyList<CoreCapacityRequirement> Requirements(
            PreparedFixture fixture, SiteReservationSelectionPlan plan)
        {
            var result = new List<CoreCapacityRequirement>();
            for (var index = 2; index <= 5; index++)
            {
                var placement = plan.SelectedPlacements[index];
                var key = SitePlacementKey.FromPlacement(placement);
                var special = fixture.Maps.Single(item =>
                    item.SpecialMapId == key.SourceDefinitionId);
                var biome = fixture.Biomes[special.PrimaryBiomeId];
                result.Add(new CoreCapacityRequirement(
                    key, placement, special, biome, fixture.CoreRules[biome.BiomeId]));
            }
            return new ReadOnlyCollection<CoreCapacityRequirement>(result);
        }

        private static void AssertSuccessfulRecord(AttemptRecord record, ulong seed)
        {
            Assert.That(record.Search.Diagnostics.TotalSourceOptionCount, Is.EqualTo(3156));
            Assert.That(record.Search.Diagnostics.TieBreakDrawCount, Is.EqualTo(3156));
            Assert.That(record.Search.SelectionPlan.SelectedCount, Is.EqualTo(6));
            Assert.That(record.Capacity.Approval.CapacitySiteCount, Is.EqualTo(4));
            Assert.That(record.Capacity.Approval.TotalWitnessSectorCount, Is.EqualTo(20));
            Assert.That(record.Capacity.Approval.Witnesses.All(item =>
                item.WitnessSectorIndices.Count == 5), Is.True);
            Assert.That(record.Village.Approval.TotalSelectedSiteCount, Is.EqualTo(7));
            Assert.That(record.Village.Diagnostics.RngMethodCallCount, Is.EqualTo(3));
            Assert.That(record.Validation.Diagnostics.Rules.Count, Is.EqualTo(6));
            Assert.That(record.Validation.Diagnostics.Rules.All(item => item.Passed), Is.True);
            Assert.That(record.Validation.Violations, Is.Empty);
            Assert.That(record.Validation.Errors, Is.Empty);
            Assert.That(record.Validation.Publication.Snapshot.Seed, Is.EqualTo(seed));
            Assert.That(record.Validation.Publication.ReservationCount, Is.EqualTo(7));
            Assert.That(record.Validation.Publication.ReservedSectorCount, Is.EqualTo(8));
            Assert.That(record.Validation.Publication.Snapshot.Sectors.Count(item =>
                !item.IsReserved), Is.EqualTo(161));
            Assert.That(record.Validation.Publication.EntryAnchorCount, Is.EqualTo(6));
            Assert.That(record.Validation.Publication.CoreSeedCount, Is.EqualTo(4));
            Assert.That(record.Validation.Publication.Snapshot.Reservations.Select(item =>
                item.SourceDefinitionId), Is.EqualTo(new[]
            {
                WorldId, BossId, ForgeId, CassiaId, YeastId, MeteorId, VillageId
            }));
        }

        private static IReadOnlyList<SiteReservationSearchGroup> ImpossibleGroups(
            PreparedFixture fixture)
        {
            var start = fixture.Groups[0];
            var boss = fixture.Groups[1];
            var bossAtEdge = boss.Options.First(item => item.Placement.OccupiedSectors.Any(
                sector => start.Options.Any(option => option.Placement.Candidate.Origin == sector)));
            var blockedStarts = start.Options.Where(option =>
                bossAtEdge.Placement.OccupiedSectors.Contains(
                    option.Placement.Candidate.Origin)).Take(2).ToArray();
            var result = new List<SiteReservationSearchGroup>
            {
                new SiteReservationSearchGroup(start.Key, null, null, null,
                    blockedStarts),
                new SiteReservationSearchGroup(boss.Key, boss.SpecialMap, boss.PrimaryBiome,
                    boss.CorePatchRule, new[] { bossAtEdge })
            };
            result.AddRange(fixture.Groups.Skip(2).Select(item =>
                new SiteReservationSearchGroup(item.Key, item.SpecialMap, item.PrimaryBiome,
                    item.CorePatchRule, item.Options.Take(1))));
            return result;
        }

        private static void CountReason(IDictionary<string, int> counts, AttemptRecord record)
        {
            IEnumerable<string> values = Array.Empty<string>();
            if (record.Capacity != null && !record.Capacity.Succeeded)
                values = record.Capacity.Rejections.Select(item => "CAPACITY:" + item.Reason);
            else if (record.Village != null && !record.Village.Succeeded)
                values = record.Village.Rejections.Select(item => "VILLAGE:" + item.Reason);
            else if (record.Validation != null && !record.Validation.Succeeded)
                values = record.Validation.Violations.Select(item => "VALIDATION:" + item.Code);
            foreach (var value in values)
                counts[value] = counts.TryGetValue(value, out var count) ? count + 1 : 1;
        }

        private static string FormatReasons(IEnumerable<KeyValuePair<string, int>> counts) =>
            string.Join(",", counts.Select(item => item.Key + "=" + item.Value));

        private static RngStreamScope Scope(string streamId)
        {
            if (streamId == WorldGenerationRngStreams.SectorRecipeStreamId)
                return RngStreamScope.Sector(new SectorCoord(0, 0));
            if (streamId == WorldGenerationRngStreams.PopulationStreamId)
                return RngStreamScope.Spawn("SPAWN_EXIT");
            return RngStreamScope.Pass("PASS_EXIT");
        }

        private static WorldRouteDefinitionSet BuildWorldDefinitions()
        {
            var worldRow = new[]
            {
                WorldId, "Moon Palace", "624", "416", "48", "32", "13", "13",
                "12", "8", "4", "4", "0", "0", "0", "0", "0", "0.25", "0", "1", "exit"
            };
            var generationRow = new[]
            {
                GenerationId, WorldId, "0", "0", "0", "0", "0", "0", "0", "0",
                "0", "1", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "1", "exit"
            };
            var rngRows = new[]
            {
                new[] { WorldGenerationRngStreams.WorldSiteStreamId, "0xA13C9E0B2F1044D1", "WORLD", "exit", "1" },
                new[] { WorldGenerationRngStreams.BiomePatchStreamId, "0xB24DAF1C302155E2", "PASS", "exit", "1" },
                new[] { WorldGenerationRngStreams.RouteStreamId, "0xC35EB02D413266F3", "PASS", "exit", "1" },
                new[] { WorldGenerationRngStreams.Type0StreamId, "0xD46FC13E52437704", "PASS", "exit", "1" },
                new[] { WorldGenerationRngStreams.SectorRecipeStreamId, "0xE570D24F63548815", "SECTOR", "exit", "1" },
                new[] { WorldGenerationRngStreams.PopulationStreamId, "0xF681E35074659926", "SPAWN", "exit", "1" }
            };
            var sources = WorldSpecs().Select(spec => BuildWorldSource(spec,
                spec.FileName == "world_profiles.csv" ? new[] { worldRow } :
                spec.FileName == "generation_profiles.csv" ? new[] { generationRow } :
                spec.FileName == "rng_streams.csv" ? rngRows : null));
            var result = new WorldRouteDefinitionBuilder().Build(sources);
            if (!result.Success)
                throw new InvalidOperationException(string.Join("\n", result.Errors));
            return result.DefinitionSet;
        }

        private static BiomeBoundaryDefinitionSet BuildBiomeDefinitions(
            string pressureId = null,
            string pressureBiome = null,
            int pressureMinimum = 0,
            int pressureMaximum = 0,
            bool pressureEdge = false)
        {
            var biomeRows = new[]
            {
                BiomeRow(CraterBiomeId, 0, 7), BiomeRow(CassiaBiomeId, 2, 12),
                BiomeRow(MillBiomeId, 1, 11), BiomeRow(DoughBiomeId, 0, 7)
            };
            var patchRows = new[]
            {
                PatchRow("PATCH_CRATER_CORE", CraterBiomeId, 5, 18, true),
                PatchRow("PATCH_ROOT_CORE", CassiaBiomeId, 5, 18, false),
                PatchRow("PATCH_MILL_CORE", MillBiomeId, 4, 14, false),
                PatchRow("PATCH_DOUGH_CORE", DoughBiomeId, 5, 18, true)
            }.ToList();
            if (pressureBiome != null)
            {
                patchRows.RemoveAll(row => row[1] == pressureBiome);
                patchRows.Add(PatchRow(pressureId, pressureBiome,
                    pressureMinimum, pressureMaximum, pressureEdge));
            }
            var sources = BiomeSpecs().Select(spec => BuildBiomeSource(spec,
                spec.FileName == "biome_types.csv" ? biomeRows :
                spec.FileName == "biome_patch_rules.csv" ? patchRows : null));
            var result = new BiomeBoundaryDefinitionBuilder().Build(sources);
            if (!result.Success)
                throw new InvalidOperationException(string.Join("\n", result.Errors));
            return result.DefinitionSet;
        }

        private static BiomePatchRuleDefinition Patch(
            string id, string biomeId, int minimum, int maximum, bool edge) =>
            BuildBiomeDefinitions(id, biomeId, minimum, maximum, edge)
                .BiomePatchRules.Values.Single(item => item.BiomeId == biomeId);

        private static SpecialVillageDefinitionSet BuildSpecialDefinitions(bool changedBossCell)
        {
            var rows = new Dictionary<string, IReadOnlyList<string[]>>(StringComparer.Ordinal)
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
                        FootprintRow(BossId, MillBiomeId, 0, 0, "ENTRY", changedBossCell ? "R" : "L"),
                        FootprintRow(BossId, MillBiomeId, 1, 0, "ARENA", "R"),
                        FootprintRow(ForgeId, MillBiomeId, 0, 0, "CORE", "L"),
                        FootprintRow(CassiaId, CassiaBiomeId, 0, 0, "CORE", "L"),
                        FootprintRow(YeastId, DoughBiomeId, 0, 0, "CORE", "L"),
                        FootprintRow(MeteorId, CraterBiomeId, 0, 0, "CORE", "L"),
                        FootprintRow(VillageId, string.Empty, 0, 0, "CORE", "L|R")
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
            var sources = SpecialSpecs().Select(spec => BuildSpecialSource(spec,
                rows.TryGetValue(spec.FileName, out var values) ? values : null));
            var result = new SpecialVillageDefinitionBuilder().Build(sources);
            if (!result.Success)
                throw new InvalidOperationException(string.Join("\n", result.Errors));
            return result.DefinitionSet;
        }

        private static string[] BiomeRow(string id, int minimumY, int maximumY) => new[]
        {
            id, "Biome", "STAGE_MOON", "1", "1", "169", "1",
            minimumY.ToString(CultureInfo.InvariantCulture),
            maximumY.ToString(CultureInfo.InvariantCulture), "1.0", "THEME_MOON",
            "AUDIO_MOON", "MICRO_MOON", "RECIPE_MOON", "RESOURCE_MOON",
            "ELEMENT_MOON", "SITE_REQUIRED", "1", "exit"
        };

        private static string[] PatchRow(
            string id, string biome, int minimum, int maximum, bool edge) => new[]
        {
            id, biome, "CORE", minimum.ToString(CultureInfo.InvariantCulture),
            maximum.ToString(CultureInfo.InvariantCulture), "1", "1", "1", "1.0",
            edge ? "1" : "0", "1", "1", "1.0", "1.0", "1.0", "1.0",
            "1.0", "1.0", "1", "exit"
        };

        private static string[] SpecialRow(
            string id, string role, string biome, int width, int height,
            int startDistance, int otherDistance, string mode) => new[]
        {
            id, "Site", role, biome, width.ToString(CultureInfo.InvariantCulture),
            height.ToString(CultureInfo.InvariantCulture), "1",
            startDistance.ToString(CultureInfo.InvariantCulture),
            otherDistance.ToString(CultureInfo.InvariantCulture), "1|2|3", "0",
            role == "VILLAGE" ? string.Empty : "REWARD_NONE", mode, "1", "exit"
        };

        private static string[] FootprintRow(
            string id, string biome, int x, int y, string role, string sides) => new[]
        {
            id, x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture),
            role, biome, "RECIPE_FIXED", sides, "exit"
        };

        private static string[] EntryRow(string id) => new[]
        {
            id, "ENTRY_L", "0", "0", "L", "1|2|3", "1", "1", "exit"
        };

        private static string[] LayoutRow(string id, int facilities, int weight) => new[]
        {
            id, "Layout", "1", "1", facilities.ToString(CultureInfo.InvariantCulture),
            "L|R", weight.ToString(CultureInfo.InvariantCulture), "1", "exit"
        };

        private static string[] ProfileRow() => new[]
        {
            VillageProfileId, "Village", WorldId, "5", "6", "FACILITY_FIXED",
            "FACILITY_OPTIONAL", Layout5Id + "|" + Layout6Id,
            "2-3:20|4-6:50|7-10:30", "2", "1", "exit"
        };

        private static WorldRouteDefinitionSource BuildWorldSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var parsed = Parse(spec, rows);
            return new WorldRouteDefinitionSource(parsed.Schema, parsed.Result);
        }

        private static BiomeBoundaryDefinitionSource BuildBiomeSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var parsed = Parse(spec, rows);
            return new BiomeBoundaryDefinitionSource(parsed.Schema, parsed.Result);
        }

        private static SpecialVillageDefinitionSource BuildSpecialSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var parsed = Parse(spec, rows);
            return new SpecialVillageDefinitionSource(parsed.Schema, parsed.Result);
        }

        private static ParsedSource Parse(FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var catalog = new CsvSchemaCatalogBuilder().Build(SchemaRows(spec));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(BuildCsv(spec,
                    rows ?? new[] { StandardRow(spec) })), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!parsed.Success)
                throw new InvalidOperationException(string.Join("\n", parsed.Errors.Select(error =>
                    error.SourceName + ":" + error.ColumnName + ":" + error.ErrorCode + ":" +
                    error.EffectiveValue + ":" + error.Message)));
            return new ParsedSource(schema, parsed);
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
                default: throw new ArgumentOutOfRangeException(column.DataType);
            }
        }).ToArray();

        private static string CsvCell(string value) =>
            value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static FileSpec[] WorldSpecs() => new[]
        {
            File("world_profiles.csv", 1, "world_profile_id:ID", "display_name_ko:STRING", "width_tiles:INT", "height_tiles:INT", "sector_width_tiles:INT", "sector_height_tiles:INT", "sector_cols:INT", "sector_rows:INT", "micro_width_tiles:INT", "micro_height_tiles:INT", "micro_cols_per_sector:INT", "micro_rows_per_sector:INT", "min_completion_distance_tiles:INT", "max_shortest_completion_distance_tiles:INT", "normal_completion_min_tiles:INT", "normal_completion_max_tiles:INT", "optional_completion_max_tiles:INT", "max_revisit_ratio:FLOAT", "required_village_count:INT", "active:BOOL", "notes:STRING"),
            File("generation_profiles.csv", 1, "generation_profile_id:ID", "world_profile_id:ID", "mandatory_sector_min:INT", "mandatory_sector_max:INT", "type0_sector_min:INT", "type0_sector_max:INT", "reserved_sector_min:INT", "reserved_sector_max:INT", "inactive_sector_min:INT", "inactive_sector_max:INT", "start_edge_ring_min:INT", "start_edge_ring_max:INT", "mandatory_loop_min:INT", "mandatory_loop_max:INT", "optional_region_depth_min:INT", "optional_region_depth_max:INT", "optional_region_count_min:INT", "optional_region_count_max:INT", "site_reservation_retry_max:INT", "biome_retry_max:INT", "route_retry_max:INT", "sector_solve_retry_max:INT", "active:BOOL", "notes:STRING"),
            File("generation_passes.csv", 3, "generation_profile_id:ID", "pass_order:INT", "pass_id:ID", "class_name:STRING", "rng_stream_id:ID", "input_artifacts:ID_LIST", "output_artifacts:ID_LIST", "failure_policy:ENUM", "max_retry_count:INT", "enabled:BOOL", "notes:STRING"),
            File("rng_streams.csv", 1, "rng_stream_id:ID", "salt_hex:HEX", "reset_scope:ENUM:WORLD|PASS|SECTOR|SPAWN", "description_ko:STRING", "active:BOOL"),
            File("sector_route_masks.csv", 1, "route_mask_id:ID", "route_type:INT", "open_l:BOOL", "open_r:BOOL", "open_u:BOOL", "open_d:BOOL", "mandatory_allowed:BOOL", "description_ko:STRING", "active:BOOL"),
            File("socket_band_definitions.csv", 1, "band_id:ID", "axis:ENUM", "min_local_coord:INT", "max_local_coord:INT", "recommended_center:FLOAT", "minimum_clearance_tiles:INT", "description_ko:STRING"),
            File("edge_signatures.csv", 1, "edge_signature_id:ID", "axis:ENUM", "band_id:ID", "traversal_kind:ENUM", "ground_entry_height:INT", "clearance_width:INT", "clearance_height:INT", "tool_requirement:ENUM", "mandatory_allowed:BOOL", "tags:ID_LIST", "notes:STRING"),
            File("edge_signature_compatibility.csv", 2, "signature_a:ID", "signature_b:ID", "compatible:BOOL", "adapter_microchunk_pool_id:ID", "notes:STRING"),
            File("sector_recipe_catalog.csv", 1, "sector_recipe_id:ID", "display_name_ko:STRING", "route_type:INT", "route_mask_id:ID", "primary_biome_id:ID", "secondary_biome_id:ID", "boundary_profile_id:ID", "recipe_kind:ENUM", "microchunk_budget_profile_id:ID", "selection_weight:INT", "supports_special_entry:BOOL", "supports_village_entry:BOOL", "active:BOOL", "notes:STRING"),
            File("sector_recipe_cells.csv", 3, "sector_recipe_id:ID", "chunk_x:INT", "chunk_y:INT", "cell_role:ENUM", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_usage_class:ENUM_LIST", "required_route_roles:ID_LIST", "required_biome_ids:ID_LIST", "required_signature_l:ID", "required_signature_r:ID", "required_signature_u:ID", "required_signature_d:ID", "transform_policy:ENUM_LIST:R0|MIRROR_X|MIRROR_Y|R180", "notes:STRING"),
            File("sector_recipe_paths.csv", 3, "sector_recipe_id:ID", "path_id:ID", "path_order:INT", "chunk_x:INT", "chunk_y:INT", "enter_side:ENUM", "exit_side:ENUM", "mandatory:BOOL", "traversal_kind:ENUM", "max_jump_tiles:INT", "notes:STRING"),
            File("sector_external_sockets.csv", 2, "sector_recipe_id:ID", "socket_id:ID", "side:ENUM", "edge_chunk_index:INT", "band_id:ID", "traversal_kind:ENUM", "mandatory_allowed:BOOL", "edge_signature_id:ID", "notes:STRING"),
            File("sector_recipe_pool_entries.csv", 3, "sector_recipe_pool_id:ID", "entry_order:INT", "sector_recipe_id:ID", "weight:INT", "min_repeat_distance_sectors:INT", "required_patch_role:ENUM", "active:BOOL")
        };

        private static FileSpec[] BiomeSpecs() => new[]
        {
            File("biome_types.csv", 1, "biome_id:ID", "display_name_ko:STRING", "stage_id:ID", "required:BOOL", "min_patch_count:INT", "max_patch_count:INT", "min_core_patch_count:INT", "preferred_altitude_min_sector_y:INT", "preferred_altitude_max_sector_y:INT", "growth_weight:FLOAT", "tile_theme_id:ID", "audio_profile_id:ID", "microchunk_pool_prefix:ID", "sector_recipe_pool_prefix:ID", "common_resource_pool_id:ID", "map_element_pool_id:ID", "required_special_map_ids:ID_LIST", "active:BOOL", "notes:STRING"),
            File("biome_patch_rules.csv", 1, "patch_rule_id:ID", "biome_id:ID", "patch_role:ENUM:CORE|OUTER", "min_sector_count:INT", "max_sector_count:INT", "min_seed_distance:INT", "seed_count_min:INT", "seed_count_max:INT", "seed_weight:FLOAT", "can_touch_world_edge:BOOL", "buffer_ring_sectors:INT", "allow_single_sector:BOOL", "max_world_share:FLOAT", "distance_weight:FLOAT", "altitude_weight:FLOAT", "noise_weight:FLOAT", "compactness_weight:FLOAT", "branchiness_target:FLOAT", "active:BOOL", "notes:STRING"),
            File("biome_boundary_profiles.csv", 1, "boundary_profile_id:ID", "display_name_ko:STRING", "boundary_type:ENUM:WALL", "allowed_orientations:ENUM_LIST:L|R|U|D", "width_microchunks_min:INT", "width_microchunks_max:INT", "warning_microchunks_min:INT", "mandatory_route_allowed:BOOL", "tool_requirement:ENUM:NONE", "hard_border:BOOL", "active:BOOL", "notes:STRING"),
            File("biome_boundary_pair_rules.csv", 1, "boundary_pair_rule_id:ID", "biome_a_id:ID", "biome_b_id:ID", "allowed_boundary_profile_ids:ID_LIST", "boundary_profile_weights:INT_LIST", "default_boundary_profile_id:ID", "transition_resource_pool_id:ID", "transition_element_pool_id:ID", "min_shared_edge_count:INT", "active:BOOL", "notes:STRING"),
            File("boundary_chunk_catalog.csv", 1, "boundary_chunk_id:ID", "microchunk_id:ID", "biome_a_id:ID", "biome_b_id:ID", "boundary_profile_id:ID", "orientation:ENUM:L|R|U|D", "route_type:INT", "entry_edge_signature_id:ID", "exit_edge_signature_id:ID", "weight:INT", "reversible:BOOL", "active:BOOL", "notes:STRING")
        };

        private static FileSpec[] SpecialSpecs() => new[]
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
            string name, int primaryKeyCount, params string[] definitions) =>
            new FileSpec(name, primaryKeyCount, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                var allowed = parts.Length > 2 ? parts[2] :
                    (parts[1] == "ENUM" || parts[1] == "ENUM_LIST"
                        ? "ENUM_A|ENUM_B" : string.Empty);
                return new ColumnSpec(parts[0], parts[1], allowed);
            }).ToArray());

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes).Select(item =>
                    item.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private sealed class ParsedSource
        {
            public ParsedSource(CsvFileSchema schema, CsvScalarAndListParseResult result)
            {
                Schema = schema;
                Result = result;
            }
            public CsvFileSchema Schema { get; }
            public CsvScalarAndListParseResult Result { get; }
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, int primaryKeyCount, IReadOnlyList<ColumnSpec> columns)
            {
                FileName = fileName;
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

        private sealed class PreparedFixture
        {
            public PreparedFixture(
                WorldProfileDefinition world,
                GenerationProfileDefinition generation,
                SiteCandidateCatalog catalog,
                IReadOnlyList<SiteReservationSearchGroup> groups,
                SiteDistancePolicy policy,
                IReadOnlyList<SpecialMapDefinition> maps,
                IReadOnlyList<SpecialMapFootprintCellDefinition> cells,
                IReadOnlyList<SpecialMapEntrySocketDefinition> entries,
                IReadOnlyDictionary<string, BiomeTypeDefinition> biomes,
                IReadOnlyDictionary<string, BiomePatchRuleDefinition> coreRules,
                VillageProfileDefinition villageProfile,
                IReadOnlyList<VillageLayoutDefinition> layouts,
                DeterministicRngStreamFactory rngFactory,
                int transformEvaluationCount,
                int sourceRejectionCount)
            {
                World = world;
                Generation = generation;
                Catalog = catalog;
                Groups = groups;
                Policy = policy;
                Maps = maps;
                Cells = cells;
                Entries = entries;
                Biomes = biomes;
                CoreRules = coreRules;
                VillageProfile = villageProfile;
                Layouts = layouts;
                RngFactory = rngFactory;
                TransformEvaluationCount = transformEvaluationCount;
                SourceRejectionCount = sourceRejectionCount;
            }
            public WorldProfileDefinition World { get; }
            public GenerationProfileDefinition Generation { get; }
            public SiteCandidateCatalog Catalog { get; }
            public IReadOnlyList<SiteReservationSearchGroup> Groups { get; }
            public SiteDistancePolicy Policy { get; }
            public IReadOnlyList<SpecialMapDefinition> Maps { get; }
            public IReadOnlyList<SpecialMapFootprintCellDefinition> Cells { get; }
            public IReadOnlyList<SpecialMapEntrySocketDefinition> Entries { get; }
            public IReadOnlyDictionary<string, BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyDictionary<string, BiomePatchRuleDefinition> CoreRules { get; }
            public VillageProfileDefinition VillageProfile { get; }
            public IReadOnlyList<VillageLayoutDefinition> Layouts { get; }
            public DeterministicRngStreamFactory RngFactory { get; }
            public int TransformEvaluationCount { get; }
            public int SourceRejectionCount { get; }
            public SpecialMapDefinition Village => Maps.Single(item => item.SpecialMapId == VillageId);
            public IReadOnlyList<SpecialMapEntrySocketDefinition> VillageEntries =>
                Entries.Where(item => item.SpecialMapId == VillageId).ToArray();
        }

        private sealed class Services
        {
            public SiteReservationBacktracker Search { get; } = new SiteReservationBacktracker();
            public CoreCapacityFloodChecker Capacity { get; } = new CoreCapacityFloodChecker();
            public VillageReservationSelector Village { get; } = new VillageReservationSelector();
            public SiteReservationValidator Validation { get; } = new SiteReservationValidator();
        }

        private sealed class AttemptRecord
        {
            private AttemptRecord(
                ulong seed, int attemptOrdinal, SiteReservationSearchResult search,
                CoreCapacityFloodResult capacity, VillageReservationResult village,
                SiteReservationValidationResult validation)
            {
                Seed = seed;
                AttemptOrdinal = attemptOrdinal;
                Search = search;
                Capacity = capacity;
                Village = village;
                Validation = validation;
                if (search == null || !search.Succeeded)
                {
                    Terminal = search == null ? "InvalidInput" : "Search" + search.Status;
                    TerminalOrdinal = search != null && search.Status == SiteReservationSearchStatus.InvalidInput ? 5 : 0;
                    RetryRequired = search != null && search.RetryRequired;
                    InvalidInput = search == null || search.Status == SiteReservationSearchStatus.InvalidInput;
                }
                else if (capacity == null || !capacity.Succeeded)
                {
                    Terminal = capacity == null ? "InvalidInput" : "Capacity" + capacity.Status;
                    TerminalOrdinal = capacity != null && capacity.Status == CoreCapacityFloodStatus.InvalidInput ? 5 : 1;
                    RetryRequired = capacity != null && capacity.RetryRequired;
                    InvalidInput = capacity == null || capacity.Status == CoreCapacityFloodStatus.InvalidInput;
                }
                else if (village == null || !village.Succeeded)
                {
                    Terminal = village == null ? "InvalidInput" : "Village" + village.Status;
                    TerminalOrdinal = village != null && village.Status == VillageReservationStatus.InvalidInput ? 5 : 2;
                    RetryRequired = village != null && village.RetryRequired;
                    InvalidInput = village == null || village.Status == VillageReservationStatus.InvalidInput;
                }
                else if (validation == null || !validation.Succeeded)
                {
                    Terminal = validation == null ? "InvalidInput" :
                        "Validation" + validation.Status + ":" + string.Join(",", validation.Errors.Select(
                            item => item.Code + "/" + item.DefinitionId + "/" + item.Message));
                    TerminalOrdinal = validation != null && validation.Status == SiteReservationValidationStatus.InvalidInput ? 5 : 3;
                    RetryRequired = validation != null && validation.RetryRequired;
                    InvalidInput = validation == null || validation.Status == SiteReservationValidationStatus.InvalidInput;
                }
                else
                {
                    Terminal = "Completed";
                    TerminalOrdinal = 4;
                    RetryRequired = false;
                    InvalidInput = false;
                    Succeeded = true;
                    Canonical = BuildCanonical();
                }
            }

            public ulong Seed { get; }
            public int AttemptOrdinal { get; }
            public SiteReservationSearchResult Search { get; }
            public CoreCapacityFloodResult Capacity { get; }
            public VillageReservationResult Village { get; }
            public SiteReservationValidationResult Validation { get; }
            public string Terminal { get; }
            public int TerminalOrdinal { get; }
            public bool RetryRequired { get; }
            public bool InvalidInput { get; }
            public bool Succeeded { get; }
            public string Canonical { get; }

            public static AttemptRecord Success(
                ulong seed, int attempt, SiteReservationSearchResult search,
                CoreCapacityFloodResult capacity, VillageReservationResult village,
                SiteReservationValidationResult validation) =>
                new AttemptRecord(seed, attempt, search, capacity, village, validation);

            public static AttemptRecord Failure(
                ulong seed, int attempt, SiteReservationSearchResult search,
                CoreCapacityFloodResult capacity, VillageReservationResult village,
                SiteReservationValidationResult validation) =>
                new AttemptRecord(seed, attempt, search, capacity, village, validation);

            private string BuildCanonical()
            {
                var snapshot = Validation.Publication.Snapshot;
                return Seed.ToString(CultureInfo.InvariantCulture) + "|" +
                       AttemptOrdinal.ToString(CultureInfo.InvariantCulture) + "|" +
                       string.Join(",", Search.SelectionPlan.SelectedPlacements.Select(item =>
                           item.Candidate.SourceDefinitionId + ":" + item.Candidate.OriginIndex + ":" +
                           (int)item.Footprint.Transform)) + "|" +
                       string.Join(",", Capacity.Approval.Witnesses.Select(item =>
                           item.Key.SourceDefinitionId + ":" +
                           string.Join(".", item.WitnessSectorIndices))) + "|" +
                       Village.Diagnostics.BucketRoll + ":" +
                       Village.Approval.Village.DistanceBucket.BucketOrdinal + ":" +
                       Village.Approval.Village.Layout.VillageLayoutId + ":" +
                       Village.Approval.Village.Candidate.OriginIndex + "|" +
                       string.Join(",", snapshot.Reservations.Select(item => item.ReservationId.Value)) + "|" +
                       string.Join(",", snapshot.Sectors.Where(item => item.IsReserved).Select(item => item.Index)) + "|" +
                       string.Join(",", snapshot.EntryAnchors.Select(item =>
                           item.ReservationId.Value + ":" + item.EntrySocketId + ":" + (int)item.Side)) + "|" +
                       string.Join(",", snapshot.CoreBiomeSeeds.Select(item =>
                           item.SourceReservationId.Value + ":" + item.BiomeId + ":" +
                           item.CorePatchRuleId + ":" + item.SeedSector)) + "|" +
                       string.Join(",", Validation.Diagnostics.Rules.Select(item =>
                           (int)item.Rule + ":" + item.Passed + ":" + item.ViolationCount));
            }
        }
    }
}
