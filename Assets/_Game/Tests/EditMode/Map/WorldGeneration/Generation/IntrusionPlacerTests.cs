using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class IntrusionPlacerTests
    {
        private const ulong ViableWorldSeed = 0x0123456789ABCDF9UL;
        private const int ViableAttempt = 24;
        private Fixture fixture;

        public static IEnumerable<TestCaseData> IdCases
        {
            get
            {
                for (var index = 0; index < 150; index++)
                    yield return new TestCaseData(index).SetName(
                        "IdFactory_CanonicalAndCultureStable_" +
                        index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            fixture = BuildFixture(ViableWorldSeed, ViableAttempt);
        }

        [TestCaseSource(nameof(IdCases))]
        public void IdFactory_CanonicalAndCultureStable(int value)
        {
            var ordinal = value % 100;
            var biome = (value & 1) == 0 ? "BIO_CASSIA_ROOT" : "BIO_ABANDONED_MILL";
            var expected = "PATCHINST_INTR_" + biome + "_" +
                           ordinal.ToString("D2", CultureInfo.InvariantCulture);
            var factory = new IntrusionPatchIdFactory();
            Assert.That(factory.Create(biome, ordinal).Value, Is.EqualTo(expected));
            Assert.That(factory.TryCreate(biome, ordinal, out var parsed), Is.True);
            Assert.That(parsed.Value, Is.EqualTo(expected));
        }

        [Test]
        public void FrozenEnumsAndIdInvalidsHaveExactOrder()
        {
            CollectionAssert.AreEqual(new[] { "Completed", "InvalidInput", "RetryRequired" },
                Enum.GetNames(typeof(IntrusionPlacementStatus)));
            CollectionAssert.AreEqual(new[]
            {
                "MissingGrowthResult", "GrowthNotCompleted", "MissingGrowthPublication",
                "MissingGrowthDiagnostics", "InvalidGrowthPublication", "InvalidSourceSiteSnapshot",
                "MissingGenerationProfile", "InvalidGenerationProfile", "MissingBiomeTypes",
                "MissingPatchRules", "MissingBoundaryProfiles", "MissingBoundaryPairRules",
                "NullDefinition", "DuplicateDefinitionId", "MissingBiomeDefinition",
                "UnexpectedBiomeDefinition", "MissingPatchRule", "UnexpectedPatchRule",
                "MissingBoundaryProfile", "UnexpectedBoundaryProfile", "MissingBoundaryPairRule",
                "UnexpectedBoundaryPairRule", "InvalidBiomeDefinition", "InvalidPatchRule",
                "InvalidBoundaryProfile", "InvalidBoundaryPairRule", "DefinitionIdentityMismatch",
                "InvalidPatchState", "InvalidReservationState", "MissingBiomePatchRng",
                "InvalidBiomePatchRngState", "InternalInvariantViolation", "NoLegalIntrusionCandidate"
            }, Enum.GetNames(typeof(IntrusionPlacementErrorCode)));
            CollectionAssert.AreEqual(new[]
            {
                "ReservedSector", "WorldEdgeForbidden", "ProtectedSeedSector",
                "ProtectedSiteBindingSector", "SameBiomeHost", "DisallowedBoundaryPair",
                "MissingIntruderSharedEdge", "DonorBelowMinimum", "DonorDisconnected",
                "IntrusionSeedDistanceTooSmall", "BiomeShareExceeded", "IntrusionShareExceeded"
            }, Enum.GetNames(typeof(IntrusionCandidateRejectionReason)));

            var factory = new IntrusionPatchIdFactory();
            Assert.That(factory.TryCreate(null, 0, out _), Is.False);
            Assert.That(factory.TryCreate("bio_root", 0, out _), Is.False);
            Assert.That(factory.TryCreate("BIO_ROOT", -1, out _), Is.False);
            Assert.That(factory.TryCreate("BIO_ROOT", 100, out _), Is.False);
            Assert.Throws<ArgumentException>(() => factory.Create("BIO_ROOT", 100));
        }

        [Test]
        public void Place_NullInputsAccumulateSortedErrorsWithoutRngConsumption()
        {
            var rng = new DeterministicRngStream(123UL);
            var result = new IntrusionPlacer().Place(null, null, null, null, null, null, rng);
            Assert.That(result.Status, Is.EqualTo(IntrusionPlacementStatus.InvalidInput));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(6));
            Assert.That(result.Errors.Select(value => value.Code), Is.Ordered);
            Assert.That(result.Errors.Any(value => value.Code == IntrusionPlacementErrorCode.MissingGrowthResult), Is.True);
            Assert.That(result.Errors.Any(value => value.Code == IntrusionPlacementErrorCode.MissingBoundaryProfiles), Is.True);
            Assert.That(rng.DrawCount, Is.Zero);
        }

        [Test]
        public void Place_ViableFactoryAttemptMatchesCountFirstVectorAndTransfersAtomically()
        {
            var sourceSignature = SnapshotSignature(fixture.Growth.Publication.Snapshot);
            var sourceP01Signature = ReservationSignature(fixture.Growth.Publication.SourceSiteSnapshot);
            var result = Place(fixture, fixture.ContinuedRng,
                fixture.Definitions.Biomes, fixture.Definitions.AllRules,
                fixture.Definitions.Profiles, fixture.Definitions.Pairs);

            Assert.That(result.Status, Is.EqualTo(IntrusionPlacementStatus.Completed));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Diagnostics.RngDrawCountBefore, Is.EqualTo(1907UL));
            Assert.That(result.Diagnostics.Rules.Single(value => value.PatchRuleId == "PATCH_MILL_INTRUSION").DesiredIntrusionCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Rules.Single(value => value.PatchRuleId == "PATCH_ROOT_INTRUSION").DesiredIntrusionCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics.CountMethodCallCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics.CandidateMethodCallCount, Is.EqualTo(3));
            Assert.That(result.Diagnostics.TotalRngMethodCallCount, Is.EqualTo(5));
            Assert.That(result.Diagnostics.RngDrawCountAfter, Is.EqualTo(1912UL));
            Assert.That(result.Publication.TotalPatchCount, Is.EqualTo(17));
            Assert.That(result.Publication.CorePatchCount, Is.EqualTo(4));
            Assert.That(result.Publication.IntrusionPatchCount, Is.EqualTo(3));
            Assert.That(result.Publication.AssignedSectorCount, Is.EqualTo(165));
            Assert.That(result.Publication.UnassignedSectorCount, Is.EqualTo(4));
            Assert.That(result.Publication.Intrusions.All(value => value.BoundaryProfileId == "BOUND_TUNNEL"), Is.True);
            Assert.That(result.Publication.Intrusions.All(value => value.DonorSizeAfter >= 2), Is.True);
            Assert.That(result.Publication.Intrusions.All(value => value.SharedIntruderEdgeCount >= 1), Is.True);
            Assert.That(result.Diagnostics.DonorMinimumViolationCount, Is.Zero);
            Assert.That(result.Diagnostics.DonorDisconnectCount, Is.Zero);
            Assert.That(result.Diagnostics.ProtectedCellTransferCount, Is.Zero);
            Assert.That(result.Diagnostics.DisallowedPairCount, Is.Zero);
            Assert.That(result.Diagnostics.ReservationIntrusionCount, Is.Zero);
            Assert.That(result.Diagnostics.PatchOverlapCount, Is.Zero);

            foreach (var record in result.Publication.Intrusions)
            {
                var patch = result.Publication.Snapshot.Patches.Single(value => value.Id == record.IntrusionPatchId);
                Assert.That(patch.Role, Is.EqualTo(BiomePatchRole.Intrusion));
                Assert.That(patch.SectorCount, Is.EqualTo(1));
                Assert.That(patch.Seeds.Count, Is.EqualTo(1));
                Assert.That(patch.Seeds[0].SourceSiteReservationId.HasValue, Is.False);
                Assert.That(fixture.Growth.Publication.SourceSiteSnapshot.GetSector(record.SectorIndex).IsReserved, Is.False);
                Assert.That(IsWorldEdge(record.Coordinate), Is.False);
            }

            Assert.That(SnapshotSignature(fixture.Growth.Publication.Snapshot), Is.EqualTo(sourceSignature));
            Assert.That(ReservationSignature(fixture.Growth.Publication.SourceSiteSnapshot), Is.EqualTo(sourceP01Signature));
            Debug.Log("MAP04_06_VIABLE " + string.Join(";", result.Publication.Intrusions.Select(record =>
                string.Format(CultureInfo.InvariantCulture,
                    "seq={0},rule={1},cand={2},roll={3},sector={4},coord={5},{6},host={7},donor={8},size={9}->{10},pair={11},anchor={12},dist={13}",
                    record.Sequence, record.IntrusionRuleId, record.CandidateCountBeforeDraw,
                    record.CandidateRoll, record.SectorIndex, record.Coordinate.X, record.Coordinate.Y,
                    record.HostBiomeId, record.DonorPatchId, record.DonorSizeBefore,
                    record.DonorSizeAfter, record.BoundaryPairRuleId,
                    record.AnchorSectorIndex, record.SameRuleNearestIntrusionDistance))));
        }

        [Test]
        public void Place_KnownRawCountOutputsMatchFrozenVector()
        {
            var probe = fixture.RngStreams.CreateBiomePatch(ViableWorldSeed, "PASS_BIOME", ViableAttempt);
            for (ulong draw = 0; draw < 1907UL; draw++) probe.NextUInt64();
            Assert.That(probe.NextUInt64(), Is.EqualTo(0xCB8386606F087EA4UL));
            Assert.That(probe.NextUInt64(), Is.EqualTo(0x9018672136A34305UL));
            Assert.That(probe.DrawCount, Is.EqualTo(1909UL));
        }

        [Test]
        public void Place_ZeroDesiredCountsPublishesInputEquivalentSnapshotWithTwoCountCalls()
        {
            var expectedDraws = fixture.Growth.Diagnostics.RngDrawCountAfter;
            var seed = FindInitialStateForCounts(expectedDraws, 0, 0);
            var rng = new DeterministicRngStream(seed);
            for (ulong draw = 0; draw < expectedDraws; draw++) rng.NextUInt64();
            var sourceSignature = SnapshotSignature(fixture.Growth.Publication.Snapshot);
            var result = Place(fixture, rng,
                fixture.Definitions.Biomes, fixture.Definitions.AllRules,
                fixture.Definitions.Profiles, fixture.Definitions.Pairs);

            Assert.That(result.Status, Is.EqualTo(IntrusionPlacementStatus.Completed));
            Assert.That(result.Diagnostics.DesiredIntrusionCount, Is.Zero);
            Assert.That(result.Diagnostics.PlacedIntrusionCount, Is.Zero);
            Assert.That(result.Diagnostics.CountMethodCallCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics.CandidateMethodCallCount, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCountAfter, Is.EqualTo(expectedDraws + 2));
            Assert.That(result.Publication.Intrusions, Is.Empty);
            Assert.That(SnapshotSignature(result.Publication.Snapshot), Is.EqualTo(sourceSignature));
            Assert.That(SnapshotSignature(fixture.Growth.Publication.Snapshot), Is.EqualTo(sourceSignature));
        }

        [Test]
        public void Place_ShuffledDefinitionsAndFreshPlacersAreDeterministic()
        {
            var firstRng = CreateContinuedRng(fixture);
            var secondRng = CreateContinuedRng(fixture);
            var first = Place(fixture, firstRng,
                fixture.Definitions.Biomes, fixture.Definitions.AllRules,
                fixture.Definitions.Profiles, fixture.Definitions.Pairs);
            var second = Place(fixture, secondRng,
                fixture.Definitions.Biomes.Reverse(), fixture.Definitions.AllRules.Reverse(),
                fixture.Definitions.Profiles.Reverse(), fixture.Definitions.Pairs.Reverse());

            Assert.That(first.Status, Is.EqualTo(second.Status));
            Assert.That(ResultSignature(first), Is.EqualTo(ResultSignature(second)));
            Assert.That(firstRng.DrawCount, Is.EqualTo(secondRng.DrawCount));
        }

        private static IntrusionPlacementResult Place(
            Fixture source,
            DeterministicRngStream rng,
            IEnumerable<BiomeTypeDefinition> biomes,
            IEnumerable<BiomePatchRuleDefinition> rules,
            IEnumerable<BiomeBoundaryProfileDefinition> profiles,
            IEnumerable<BiomeBoundaryPairRuleDefinition> pairs)
        {
            return new IntrusionPlacer().Place(
                source.Growth, source.Profile, biomes, rules, profiles, pairs, rng);
        }

        private static DeterministicRngStream CreateContinuedRng(Fixture source)
        {
            var rng = source.RngStreams.CreateBiomePatch(ViableWorldSeed, "PASS_BIOME", ViableAttempt);
            for (ulong draw = 0; draw < source.Growth.Diagnostics.RngDrawCountAfter; draw++)
                rng.NextUInt64();
            return rng;
        }

        private static ulong FindInitialStateForCounts(ulong drawCount, int first, int second)
        {
            for (ulong seed = 0; seed < 10000UL; seed++)
            {
                var probe = new DeterministicRngStream(seed);
                for (ulong draw = 0; draw < drawCount; draw++) probe.NextUInt64();
                if (probe.NextInt(0, 3) == first && probe.NextInt(0, 3) == second) return seed;
            }
            throw new InvalidOperationException("Unable to find deterministic count fixture.");
        }

        private static Fixture BuildFixture(ulong worldSeed, int attempt)
        {
            var definitions = BuildBiomeDefinitions();
            var routeDefinitions = BuildRouteDefinitions();
            var source = BuildSourceSnapshot(worldSeed);
            var initialization = new CorePatchSeedInitializer().Initialize(
                source, definitions.Biomes, definitions.CoreRules);
            if (!initialization.Succeeded) throw new InvalidOperationException("Core initialization failed.");
            var coreGrowth = new CorePatchGrower().Grow(
                initialization.Publication, definitions.Biomes, definitions.CoreRules);
            if (!coreGrowth.Succeeded) throw new InvalidOperationException("Core growth failed.");
            var rngStreams = new WorldGenerationRngStreams(routeDefinitions);
            var rng = rngStreams.CreateBiomePatch(worldSeed, "PASS_BIOME", attempt);
            var satellites = new SatelliteSeedPlacer().Place(
                coreGrowth.Publication, routeDefinitions.GenerationProfiles["GEN_MOONPALACE_V1"],
                definitions.Biomes, definitions.SatelliteRules, rng);
            if (!satellites.Succeeded) throw new InvalidOperationException("Satellite placement failed.");
            var growth = new MultiSeedBiomeGrower().Grow(
                satellites, routeDefinitions.GenerationProfiles["GEN_MOONPALACE_V1"],
                definitions.Biomes, definitions.CoreAndSatelliteRules, rng);
            if (!growth.Succeeded)
                throw new InvalidOperationException("Known viable multi-seed growth failed: " +
                    string.Join(",", growth.Errors.Select(value => value.Code.ToString())));
            return new Fixture(
                growth, routeDefinitions.GenerationProfiles["GEN_MOONPALACE_V1"],
                rngStreams, rng, definitions);
        }

        private static SiteReservationSnapshot BuildSourceSnapshot(ulong worldSeed)
        {
            var reservations = new List<SiteReservation>
            {
                CreateReservation(0, StartId, "WORLD_MOONPALACE_V1", SiteReservationKind.Start, string.Empty, new SectorCoord(0, 0), 1),
                CreateReservation(1, BossId, "SITE_MOON_BOSS_VAULT", SiteReservationKind.Boss, "BIO_ABANDONED_MILL", new SectorCoord(12, 12), 1),
                CreateReservation(2, ForgeId, "SITE_MOON_SEAL_FORGE", SiteReservationKind.Forge, "BIO_ABANDONED_MILL", new SectorCoord(2, 2), 1),
                CreateReservation(3, CassiaId, "SITE_CASSIA_SAP_HEART", SiteReservationKind.CoreResource, "BIO_CASSIA_ROOT", new SectorCoord(8, 2), 1),
                CreateReservation(4, DoughId, "SITE_DEEP_STAR_YEAST", SiteReservationKind.CoreResource, "BIO_MOON_DOUGH", new SectorCoord(2, 8), 1),
                CreateReservation(5, CraterId, "SITE_MOON_CORE_METEOR", SiteReservationKind.CoreResource, "BIO_MOON_CRATER", new SectorCoord(8, 8), 1),
                CreateReservation(6, VillageId, "SITE_PRIMARY_VILLAGE", SiteReservationKind.Village, string.Empty, new SectorCoord(0, 12), 2)
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
                worldSeed, reservations, CreateSectorReservations(reservations), seeds);
        }

        private static SiteReservation CreateReservation(
            int order, string reservationId, string sourceDefinitionId,
            SiteReservationKind kind, string biomeId, SectorCoord origin, int width)
        {
            var cells = Enumerable.Range(0, width).Select(localX => new SiteFootprintCell(
                localX, 0, kind == SiteReservationKind.Start ? "START" : "CORE",
                biomeId, string.Empty, Array.Empty<SiteEntrySide>()));
            return new SiteReservation(
                new SiteReservationId(reservationId), kind, sourceDefinitionId, origin,
                new SiteFootprint(width, 1, SiteFootprintTransform.R0, cells), biomeId,
                order, Array.Empty<SiteEntryAnchor>());
        }

        private static CoreBiomeSeed CoreSeed(
            SiteReservation reservation, string biomeId, string ruleId, int minimum)
        {
            return new CoreBiomeSeed(
                reservation.ReservationId, biomeId, ruleId,
                reservation.OccupiedSectors.OrderBy(WorldGridIndex.ToIndex).First(), minimum, 1);
        }

        private static List<SectorReservation> CreateSectorReservations(
            IEnumerable<SiteReservation> reservations)
        {
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var reservation in reservations)
                foreach (var coordinate in reservation.OccupiedSectors)
                {
                    reservation.TryGetFootprintCell(coordinate, out var cell);
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
                else result.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            return result;
        }

        private static BiomeDefinitions BuildBiomeDefinitions()
        {
            var specs = CreateBiomeFileSpecs();
            var rows = new Dictionary<string, string[][]>(StringComparer.Ordinal)
            {
                { "biome_types.csv", new[]
                    {
                        BiomeRow("BIO_MOON_CRATER", 0, 7, "1.0"),
                        BiomeRow("BIO_CASSIA_ROOT", 2, 12, "1.0"),
                        BiomeRow("BIO_ABANDONED_MILL", 1, 11, "0.9"),
                        BiomeRow("BIO_MOON_DOUGH", 0, 7, "1.0")
                    }
                },
                { "biome_patch_rules.csv", PatchRows() },
                { "biome_boundary_profiles.csv", ProfileRows() },
                { "biome_boundary_pair_rules.csv", PairRows() },
                { "boundary_chunk_catalog.csv", Array.Empty<string[]>() }
            };
            var sources = specs.Select(spec => BuildBiomeSource(spec, rows[spec.FileName])).ToArray();
            var result = new BiomeBoundaryDefinitionBuilder().Build(sources);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            var set = result.DefinitionSet;
            var allRules = set.BiomePatchRules.Values.ToArray();
            return new BiomeDefinitions(
                set.BiomeTypes.Values.ToArray(),
                allRules.Where(value => value.PatchRole == "CORE").ToArray(),
                allRules.Where(value => value.PatchRole == "SATELLITE").ToArray(),
                allRules.Where(value => value.PatchRole != "INTRUSION").ToArray(),
                allRules,
                set.BoundaryProfiles.Values.ToArray(),
                set.BoundaryPairRules.Values.ToArray());
        }

        private static string[] BiomeRow(string id, int minY, int maxY, string weight)
        {
            return new[]
            {
                id, "NAME", "STAGE_MOON_01", "1", "1", "4", "1",
                minY.ToString(CultureInfo.InvariantCulture), maxY.ToString(CultureInfo.InvariantCulture),
                weight, "THEME", "AUDIO", "MICRO", "RECIPE", "RESOURCE", "ELEMENT",
                "SITE_REQUIRED", "1", string.Empty
            };
        }

        private static string[][] PatchRows()
        {
            return new[]
            {
                PatchRow("PATCH_CRATER_CORE", "BIO_MOON_CRATER", "CORE", 5, 18, 4, 1, 1, "100", true, 1, false, "0.35", "1.0", "0.25", "0.45", "0.75", "0.45"),
                PatchRow("PATCH_CRATER_SAT", "BIO_MOON_CRATER", "SATELLITE", 2, 16, 3, 0, 3, "70", true, 0, false, "0.35", "1.0", "0.25", "0.6", "0.65", "0.55"),
                PatchRow("PATCH_ROOT_CORE", "BIO_CASSIA_ROOT", "CORE", 5, 18, 4, 1, 1, "100", false, 1, false, "0.35", "1.0", "0.35", "0.45", "0.7", "0.55"),
                PatchRow("PATCH_ROOT_SAT", "BIO_CASSIA_ROOT", "SATELLITE", 2, 14, 3, 0, 3, "70", false, 0, false, "0.35", "1.0", "0.35", "0.6", "0.6", "0.65"),
                PatchRow("PATCH_MILL_CORE", "BIO_ABANDONED_MILL", "CORE", 4, 14, 4, 1, 1, "100", false, 1, false, "0.35", "1.0", "0.2", "0.35", "0.85", "0.3"),
                PatchRow("PATCH_MILL_SAT", "BIO_ABANDONED_MILL", "SATELLITE", 2, 10, 3, 0, 2, "45", false, 0, false, "0.35", "1.0", "0.2", "0.5", "0.8", "0.35"),
                PatchRow("PATCH_DOUGH_CORE", "BIO_MOON_DOUGH", "CORE", 5, 18, 4, 1, 1, "100", true, 1, false, "0.35", "1.0", "0.4", "0.45", "0.7", "0.5"),
                PatchRow("PATCH_DOUGH_SAT", "BIO_MOON_DOUGH", "SATELLITE", 2, 14, 3, 0, 3, "70", true, 0, false, "0.35", "1.0", "0.4", "0.6", "0.65", "0.6"),
                PatchRow("PATCH_ROOT_INTRUSION", "BIO_CASSIA_ROOT", "INTRUSION", 1, 5, 2, 0, 2, "20", false, 0, true, "0.1", "1.0", "0.3", "0.8", "0.2", "0.9"),
                PatchRow("PATCH_MILL_INTRUSION", "BIO_ABANDONED_MILL", "INTRUSION", 1, 4, 2, 0, 2, "15", false, 0, true, "0.1", "1.0", "0.1", "0.8", "0.25", "0.85")
            };
        }

        private static string[] PatchRow(
            string id, string biome, string role, int min, int max, int distance,
            int countMin, int countMax, string seedWeight, bool edge, int buffer,
            bool single, string share, string distanceWeight, string altitudeWeight,
            string noiseWeight, string compactnessWeight, string branchiness)
        {
            return new[]
            {
                id, biome, role, min.ToString(CultureInfo.InvariantCulture),
                max.ToString(CultureInfo.InvariantCulture), distance.ToString(CultureInfo.InvariantCulture),
                countMin.ToString(CultureInfo.InvariantCulture), countMax.ToString(CultureInfo.InvariantCulture),
                seedWeight, edge ? "1" : "0", buffer.ToString(CultureInfo.InvariantCulture),
                single ? "1" : "0", share, distanceWeight, altitudeWeight,
                noiseWeight, compactnessWeight, branchiness, "1", string.Empty
            };
        }

        private static string[][] ProfileRows()
        {
            return new[]
            {
                new[] { "BOUND_SOFT_BLEND", "NAME", "SOFT_BLEND", "HORIZONTAL|VERTICAL", "1", "2", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_CLIFF", "NAME", "CLIFF", "HORIZONTAL|VERTICAL", "1", "2", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_TUNNEL", "NAME", "TUNNEL_INTRUSION", "HORIZONTAL|VERTICAL", "1", "3", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_LAYER", "NAME", "LAYER", "VERTICAL", "1", "2", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_RUIN", "NAME", "RUIN", "HORIZONTAL|VERTICAL", "1", "3", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_HARD_STARSTONE", "NAME", "HARD_STARSTONE", "HORIZONTAL|VERTICAL", "1", "1", "1", "0", "NONE", "1", "1", "" }
            };
        }

        private static string[][] PairRows()
        {
            return new[]
            {
                PairRow("PAIR_CRATER_ROOT", "BIO_MOON_CRATER", "BIO_CASSIA_ROOT", "BOUND_SOFT_BLEND|BOUND_CLIFF|BOUND_TUNNEL", "50|25|25", "BOUND_SOFT_BLEND"),
                PairRow("PAIR_CRATER_MILL", "BIO_MOON_CRATER", "BIO_ABANDONED_MILL", "BOUND_RUIN|BOUND_SOFT_BLEND", "70|30", "BOUND_RUIN"),
                PairRow("PAIR_CRATER_DOUGH", "BIO_MOON_CRATER", "BIO_MOON_DOUGH", "BOUND_CLIFF|BOUND_LAYER|BOUND_SOFT_BLEND", "45|35|20", "BOUND_CLIFF"),
                PairRow("PAIR_ROOT_MILL", "BIO_CASSIA_ROOT", "BIO_ABANDONED_MILL", "BOUND_RUIN|BOUND_TUNNEL|BOUND_SOFT_BLEND", "45|35|20", "BOUND_RUIN"),
                PairRow("PAIR_ROOT_DOUGH", "BIO_CASSIA_ROOT", "BIO_MOON_DOUGH", "BOUND_TUNNEL|BOUND_LAYER|BOUND_SOFT_BLEND", "45|30|25", "BOUND_TUNNEL"),
                PairRow("PAIR_MILL_DOUGH", "BIO_ABANDONED_MILL", "BIO_MOON_DOUGH", "BOUND_RUIN|BOUND_LAYER|BOUND_TUNNEL", "45|30|25", "BOUND_RUIN")
            };
        }

        private static string[] PairRow(
            string id, string biomeA, string biomeB, string profiles, string weights, string defaultProfile)
        {
            return new[]
            {
                id, biomeA, biomeB, profiles, weights, defaultProfile,
                "POOL_RESOURCE", "POOL_ELEMENT", "1", "1", string.Empty
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

        private static WorldRouteDefinitionSet BuildRouteDefinitions()
        {
            var specs = CreateWorldFileSpecs();
            var rows = new Dictionary<string, string[][]>(StringComparer.Ordinal)
            {
                { "generation_profiles.csv", new[] { new[]
                    {
                        "GEN_MOONPALACE_V1", "WORLD_MOONPALACE_V1", "75", "105", "40", "70",
                        "8", "16", "7", "30", "0", "1", "4", "10", "1", "4", "8", "18",
                        "200", "100", "200", "20", "1", string.Empty
                    } }
                },
                { "rng_streams.csv", new[]
                    {
                        new[] { "RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD", "test", "1" },
                        new[] { "RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS", "test", "1" },
                        new[] { "RNG_ROUTE", "C00FEE12AB341901", "PASS", "test", "1" },
                        new[] { "RNG_TYPE0", "D15EA5E007A4C883", "PASS", "test", "1" },
                        new[] { "RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR", "test", "1" },
                        new[] { "RNG_POPULATION", "F123456789ABCDEF", "SPAWN", "test", "1" }
                    }
                }
            };
            var sources = specs.Select(spec => BuildWorldRouteSource(
                spec, rows.TryGetValue(spec.FileName, out var value) ? value : Array.Empty<string[]>())).ToArray();
            var result = new WorldRouteDefinitionBuilder().Build(sources);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            return result.DefinitionSet;
        }

        private static FileSpec[] CreateWorldFileSpecs()
        {
            return new[]
            {
                File("world_profiles.csv", "world_profile_id:ID", "display_name_ko:STRING", "width_tiles:INT", "height_tiles:INT", "sector_width_tiles:INT", "sector_height_tiles:INT", "sector_cols:INT", "sector_rows:INT", "micro_width_tiles:INT", "micro_height_tiles:INT", "micro_cols_per_sector:INT", "micro_rows_per_sector:INT", "min_completion_distance_tiles:INT", "max_shortest_completion_distance_tiles:INT", "normal_completion_min_tiles:INT", "normal_completion_max_tiles:INT", "optional_completion_max_tiles:INT", "max_revisit_ratio:FLOAT", "required_village_count:INT", "active:BOOL", "notes:STRING"),
                File("generation_profiles.csv", "generation_profile_id:ID", "world_profile_id:ID", "mandatory_sector_min:INT", "mandatory_sector_max:INT", "type0_sector_min:INT", "type0_sector_max:INT", "reserved_sector_min:INT", "reserved_sector_max:INT", "inactive_sector_min:INT", "inactive_sector_max:INT", "start_edge_ring_min:INT", "start_edge_ring_max:INT", "mandatory_loop_min:INT", "mandatory_loop_max:INT", "optional_region_depth_min:INT", "optional_region_depth_max:INT", "optional_region_count_min:INT", "optional_region_count_max:INT", "site_reservation_retry_max:INT", "biome_retry_max:INT", "route_retry_max:INT", "sector_solve_retry_max:INT", "active:BOOL", "notes:STRING"),
                File("generation_passes.csv", "generation_profile_id:ID", "pass_order:INT", "pass_id:ID", "class_name:STRING", "rng_stream_id:ID", "input_artifacts:ID_LIST", "output_artifacts:ID_LIST", "failure_policy:ENUM", "max_retry_count:INT", "enabled:BOOL", "notes:STRING"),
                File("rng_streams.csv", "rng_stream_id:ID", "salt_hex:HEX", "reset_scope:ENUM", "description_ko:STRING", "active:BOOL"),
                File("sector_route_masks.csv", "route_mask_id:ID", "route_type:INT", "open_l:BOOL", "open_r:BOOL", "open_u:BOOL", "open_d:BOOL", "mandatory_allowed:BOOL", "description_ko:STRING", "active:BOOL"),
                File("socket_band_definitions.csv", "band_id:ID", "axis:ENUM", "min_local_coord:INT", "max_local_coord:INT", "recommended_center:FLOAT", "minimum_clearance_tiles:INT", "description_ko:STRING"),
                File("edge_signatures.csv", "edge_signature_id:ID", "axis:ENUM", "band_id:ID", "traversal_kind:ENUM", "ground_entry_height:INT", "clearance_width:INT", "clearance_height:INT", "tool_requirement:ENUM", "mandatory_allowed:BOOL", "tags:ID_LIST", "notes:STRING"),
                File("edge_signature_compatibility.csv", "signature_a:ID", "signature_b:ID", "compatible:BOOL", "adapter_microchunk_pool_id:ID", "notes:STRING"),
                File("sector_recipe_catalog.csv", "sector_recipe_id:ID", "display_name_ko:STRING", "route_type:INT", "route_mask_id:ID", "primary_biome_id:ID", "secondary_biome_id:ID", "boundary_profile_id:ID", "recipe_kind:ENUM", "microchunk_budget_profile_id:ID", "selection_weight:INT", "supports_special_entry:BOOL", "supports_village_entry:BOOL", "active:BOOL", "notes:STRING"),
                File("sector_recipe_cells.csv", "sector_recipe_id:ID", "chunk_x:INT", "chunk_y:INT", "cell_role:ENUM", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_usage_class:ENUM_LIST", "required_route_roles:ID_LIST", "required_biome_ids:ID_LIST", "required_signature_l:ID", "required_signature_r:ID", "required_signature_u:ID", "required_signature_d:ID", "transform_policy:ENUM_LIST", "notes:STRING"),
                File("sector_recipe_paths.csv", "sector_recipe_id:ID", "path_id:ID", "path_order:INT", "chunk_x:INT", "chunk_y:INT", "enter_side:ENUM", "exit_side:ENUM", "mandatory:BOOL", "traversal_kind:ENUM", "max_jump_tiles:INT", "notes:STRING"),
                File("sector_external_sockets.csv", "sector_recipe_id:ID", "socket_id:ID", "side:ENUM", "edge_chunk_index:INT", "band_id:ID", "traversal_kind:ENUM", "mandatory_allowed:BOOL", "edge_signature_id:ID", "notes:STRING"),
                File("sector_recipe_pool_entries.csv", "sector_recipe_pool_id:ID", "entry_order:INT", "sector_recipe_id:ID", "weight:INT", "min_repeat_distance_sectors:INT", "required_patch_role:ENUM", "active:BOOL")
            };
        }

        private static BiomeBoundaryDefinitionSource BuildBiomeSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var schema = BuildSchema(spec);
            var parsed = Parse(spec, rows, schema);
            return new BiomeBoundaryDefinitionSource(schema, parsed);
        }

        private static WorldRouteDefinitionSource BuildWorldRouteSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var schema = BuildSchema(spec);
            var parsed = Parse(spec, rows, schema);
            return new WorldRouteDefinitionSource(schema, parsed);
        }

        private static CsvScalarAndListParseResult Parse(
            FileSpec spec,
            IReadOnlyList<string[]> rows,
            CsvFileSchema schema)
        {
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in rows) csv += "\n" + string.Join(",", row.Select(CsvCell));
            var read = new Rfc4180CsvReader().Read(new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            if (!validation.Success) throw new InvalidOperationException(string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            if (!keys.Success) throw new InvalidOperationException("Primary-key fixture failed.");
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!parsed.Success) throw new InvalidOperationException(string.Join("\n", parsed.Errors));
            return parsed;
        }

        private static CsvFileSchema BuildSchema(FileSpec spec)
        {
            var schemaRows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName, (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name, column.DataType, index == 0 ? "1" : "0",
                index == 0 ? "1" : string.Empty, string.Empty, column.AllowedValues,
                string.Empty, string.Empty, index + 2));
            var catalog = new CsvSchemaCatalogBuilder().Build(schemaRows);
            if (!catalog.Success) throw new InvalidOperationException(string.Join("\n", catalog.Errors));
            return catalog.Catalog.GetFile(spec.FileName);
        }

        private static FileSpec File(string fileName, params string[] definitions)
        {
            return new FileSpec(fileName, definitions.Select(value =>
            {
                var parts = value.Split(':');
                var allowed = AllowedValues(parts[0], parts[1]);
                return new ColumnSpec(parts[0], parts[1], allowed);
            }).ToArray());
        }

        private static string AllowedValues(string name, string type)
        {
            if (name == "patch_role") return "CORE|SATELLITE|INTRUSION";
            if (name == "reset_scope") return "WORLD|PASS|SECTOR|PATCH|SITE|SPAWN";
            if (name == "boundary_type") return "SOFT_BLEND|CLIFF|TUNNEL_INTRUSION|LAYER|RUIN|HARD_STARSTONE";
            if (name == "allowed_orientations" || name == "orientation") return "HORIZONTAL|VERTICAL";
            if (name == "tool_requirement") return "NONE";
            return type == "ENUM" || type == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty;
        }

        private static string CsvCell(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static bool IsWorldEdge(SectorCoord coordinate)
        {
            return coordinate.X == 0 || coordinate.X == 12 || coordinate.Y == 0 || coordinate.Y == 12;
        }

        private static string SnapshotSignature(BiomePatchSnapshot snapshot)
        {
            return string.Join("|", snapshot.Patches.Select(patch =>
                patch.Id + ":" + patch.Role + ":" + string.Join(",", patch.SectorIndices))) + "#" +
                string.Join("|", snapshot.Sectors.Select(value =>
                    value.IsAssigned ? value.PrimaryBiomeId + ":" + value.PatchId.Value : "_"));
        }

        private static string ReservationSignature(SiteReservationSnapshot snapshot)
        {
            return snapshot.Seed + "|" + string.Join(";", snapshot.Sectors.Select(value =>
                value.Index + ":" + value.IsReserved + ":" +
                (value.ReservationId.HasValue ? value.ReservationId.Value.Value : string.Empty)));
        }

        private static string ResultSignature(IntrusionPlacementResult result)
        {
            if (!result.Succeeded)
                return result.Status + ":" + string.Join(",", result.Errors.Select(value => value.Code));
            return SnapshotSignature(result.Publication.Snapshot) + "#" + string.Join("|",
                result.Publication.Intrusions.Select(value =>
                    value.IntrusionRuleId + ":" + value.SectorIndex + ":" + value.CandidateRoll));
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
                MultiSeedBiomeGrowthResult growth, GenerationProfileDefinition profile,
                WorldGenerationRngStreams rngStreams, DeterministicRngStream continuedRng,
                BiomeDefinitions definitions)
            {
                Growth = growth; Profile = profile; RngStreams = rngStreams;
                ContinuedRng = continuedRng; Definitions = definitions;
            }
            public MultiSeedBiomeGrowthResult Growth { get; }
            public GenerationProfileDefinition Profile { get; }
            public WorldGenerationRngStreams RngStreams { get; }
            public DeterministicRngStream ContinuedRng { get; }
            public BiomeDefinitions Definitions { get; }
        }

        private sealed class BiomeDefinitions
        {
            public BiomeDefinitions(
                IReadOnlyList<BiomeTypeDefinition> biomes,
                IReadOnlyList<BiomePatchRuleDefinition> coreRules,
                IReadOnlyList<BiomePatchRuleDefinition> satelliteRules,
                IReadOnlyList<BiomePatchRuleDefinition> coreAndSatelliteRules,
                IReadOnlyList<BiomePatchRuleDefinition> allRules,
                IReadOnlyList<BiomeBoundaryProfileDefinition> profiles,
                IReadOnlyList<BiomeBoundaryPairRuleDefinition> pairs)
            {
                Biomes = biomes; CoreRules = coreRules; SatelliteRules = satelliteRules;
                CoreAndSatelliteRules = coreAndSatelliteRules; AllRules = allRules;
                Profiles = profiles; Pairs = pairs;
            }
            public IReadOnlyList<BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> CoreRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> SatelliteRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> CoreAndSatelliteRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> AllRules { get; }
            public IReadOnlyList<BiomeBoundaryProfileDefinition> Profiles { get; }
            public IReadOnlyList<BiomeBoundaryPairRuleDefinition> Pairs { get; }
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, IReadOnlyList<ColumnSpec> columns)
            {
                FileName = fileName; Columns = columns;
            }
            public string FileName { get; }
            public IReadOnlyList<ColumnSpec> Columns { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string name, string dataType, string allowedValues)
            {
                Name = name; DataType = dataType; AllowedValues = allowedValues;
            }
            public string Name { get; }
            public string DataType { get; }
            public string AllowedValues { get; }
        }
    }
}
