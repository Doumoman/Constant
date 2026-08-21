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
    public sealed class MultiSeedBiomeGrowerTests
    {
        private const ulong KnownWorldSeed = 0x0123456789ABCDEFUL;
        private Fixture fixture;

        public static IEnumerable<TestCaseData> CostCases
        {
            get
            {
                for (var index = 0; index < 160; index++)
                    yield return new TestCaseData(index).SetName(
                        "Cost_CheckedIntegerContract_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            fixture = BuildFixture();
        }

        [TestCaseSource(nameof(CostCases))]
        public void Cost_CheckedIntegerContract(int value)
        {
            var graph = value % 13;
            var altitude2 = value % 25;
            var noise = (value * 37) % 1001;
            var neighbors = value % 5;
            var penalty = (value & 1) != 0;
            var cost = new BiomeGrowthCost(
                graph, altitude2, noise, neighbors, penalty,
                1000, 350, 600, 700);

            Assert.That(cost.GraphTerm2, Is.EqualTo(2 * graph * 1000));
            Assert.That(cost.AltitudeTerm2, Is.EqualTo(altitude2 * 350));
            Assert.That(cost.NoiseTerm2,
                Is.EqualTo((int)(((2L * noise * 600) + 500L) / 1000L)));
            Assert.That(cost.ExposedPerimeterDelta, Is.EqualTo(4 - (2 * neighbors)));
            Assert.That(cost.PerimeterTerm2,
                Is.EqualTo(2 * (4 - (2 * neighbors)) * 700));
            Assert.That(cost.ReservationTerm2,
                Is.EqualTo(penalty ? BiomeGrowthCost.ReservationPenaltyValue2 : 0));
            Assert.That(cost.TotalCost2, Is.EqualTo(
                cost.GraphTerm2 + cost.AltitudeTerm2 + cost.NoiseTerm2 +
                cost.PerimeterTerm2 + cost.ReservationTerm2));
        }

        [Test]
        public void Grow_AttemptZeroReportsExactCapacityRetryBeforeNoise()
        {
            var rng = fixture.RngStreams.CreateBiomePatch(KnownWorldSeed, "PASS_BIOME", 0);
            var placement = Place(fixture, rng);
            Assert.That(placement.Status, Is.EqualTo(SatelliteSeedPlacementStatus.Completed));
            Assert.That(placement.Diagnostics.RngDrawCountAfter, Is.EqualTo(13UL));

            var sourceSignature = SnapshotSignature(placement.Publication.Snapshot);
            var result = Grow(fixture, placement, rng);

            Assert.That(result.Status, Is.EqualTo(MultiSeedBiomeGrowthStatus.RetryRequired));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics.InitialPatchCount, Is.EqualTo(11));
            Assert.That(result.Diagnostics.InitialAssignedSectorCount, Is.EqualTo(27));
            Assert.That(result.Diagnostics.TargetUnassignedSectorCount, Is.EqualTo(138));
            Assert.That(result.Diagnostics.HardBlockedReservedSectorCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.TargetOwnedSectorCount, Is.EqualTo(165));
            Assert.That(result.Diagnostics.AggregateLegalCapacity, Is.EqualTo(161));
            Assert.That(result.Diagnostics.NoiseValueCount, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCountBefore, Is.EqualTo(13UL));
            Assert.That(result.Diagnostics.RngDrawCountAfter, Is.EqualTo(13UL));
            Assert.That(result.Errors.Single().Code,
                Is.EqualTo(MultiSeedBiomeGrowthErrorCode.InsufficientAggregateCapacity));
            Assert.That(result.Errors.Single().Shortfall, Is.EqualTo(4));
            Assert.That(SnapshotSignature(placement.Publication.Snapshot), Is.EqualTo(sourceSignature));
        }

        [Test]
        public void Grow_FirstViableFactoryAttemptCompletesAndConservesWorld()
        {
            MultiSeedBiomeGrowthResult success = null;
            SatelliteSeedPlacementResult successfulPlacement = null;
            var attempt = -1;
            var successfulWorldSeed = 0UL;
            var attempts = new StringBuilder();
            for (var seedOffset = 0; seedOffset < 32 && success == null; seedOffset++)
            {
                var worldSeed = KnownWorldSeed + (ulong)seedOffset;
                var candidateFixture = seedOffset == 0
                    ? fixture
                    : BuildFixture(worldSeed, fixture.RngStreams.Definitions, fixture.Definitions);
                for (var ordinal = 0; ordinal <= candidateFixture.Profile.BiomeRetryMax; ordinal++)
                {
                    var rng = candidateFixture.RngStreams.CreateBiomePatch(worldSeed, "PASS_BIOME", ordinal);
                    var placement = Place(candidateFixture, rng);
                    if (!placement.Succeeded) continue;
                    var result = Grow(candidateFixture, placement, rng);
                    if (!result.Succeeded)
                    {
                        if (seedOffset == 0)
                            attempts.Append(ordinal).Append(':').Append(result.Errors[0].Code).Append(';');
                        continue;
                    }
                    successfulWorldSeed = worldSeed;
                    attempt = ordinal;
                    success = result;
                    successfulPlacement = placement;
                    break;
                }
            }

            Assert.That(success, Is.Not.Null,
                "No viable attempt completed in BiomeRetryMax. " + attempts);
            Assert.That(success.Status, Is.EqualTo(MultiSeedBiomeGrowthStatus.Completed));
            Assert.That(success.Publication.FinalAssignedSectorCount, Is.EqualTo(165));
            Assert.That(success.Publication.FinalUnassignedReservedSectorCount, Is.EqualTo(4));
            Assert.That(success.Publication.Snapshot.IsComplete, Is.False);
            Assert.That(success.Diagnostics.TotalClaimCount,
                Is.EqualTo(165 - successfulPlacement.Publication.AssignedSectorCount));
            Assert.That(success.Diagnostics.NoiseValueCount,
                Is.EqualTo(success.Diagnostics.InitialPatchCount *
                           success.Diagnostics.TargetUnassignedSectorCount));
            Assert.That(success.Diagnostics.PatchOverlapCount, Is.Zero);
            Assert.That(success.Diagnostics.DisconnectedPatchCount, Is.Zero);
            Assert.That(success.Publication.Snapshot.Sectors.Count(value => !value.IsAssigned), Is.EqualTo(4));
            Assert.That(successfulPlacement.Publication.Snapshot.AssignedSectorCount,
                Is.EqualTo(success.Publication.InitialAssignedSectorCount));
            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "MAP04_05_VIABLE seed={0} attempt={1} patches={2} initial={3} target={4} noise={5} draws={6}->{7} checksum={8} biomes={9} patchesFinal={10}",
                successfulWorldSeed,
                attempt,
                success.Diagnostics.InitialPatchCount,
                success.Diagnostics.InitialAssignedSectorCount,
                success.Diagnostics.TargetOwnedSectorCount,
                success.Diagnostics.NoiseValueCount,
                success.Diagnostics.RngDrawCountBefore,
                success.Diagnostics.RngDrawCountAfter,
                success.Diagnostics.NoiseChecksum,
                string.Join(",", success.Diagnostics.BiomeSectorCounts.Select(value => value.Key + ":" + value.Value)),
                string.Join(",", success.Diagnostics.PatchSectorCounts.Select(value => value.Key + ":" + value.Value))));
        }

        [Test]
        public void Grow_NullInputsAccumulateStableErrorsWithoutRngConsumption()
        {
            var rng = new DeterministicRngStream(123UL);
            var result = new MultiSeedBiomeGrower().Grow(null, null, null, null, rng);
            Assert.That(result.Status, Is.EqualTo(MultiSeedBiomeGrowthStatus.InvalidInput));
            Assert.That(result.Errors.Select(value => value.Code), Is.Ordered);
            Assert.That(result.Errors.Any(value =>
                value.Code == MultiSeedBiomeGrowthErrorCode.MissingPlacementResult), Is.True);
            Assert.That(result.Errors.Any(value =>
                value.Code == MultiSeedBiomeGrowthErrorCode.MissingGenerationProfile), Is.True);
            Assert.That(result.Errors.Any(value =>
                value.Code == MultiSeedBiomeGrowthErrorCode.MissingBiomeTypes), Is.True);
            Assert.That(result.Errors.Any(value =>
                value.Code == MultiSeedBiomeGrowthErrorCode.MissingPatchRules), Is.True);
            Assert.That(rng.DrawCount, Is.Zero);
        }

        [Test]
        public void FrozenEnumsAndPublicSurfaceHaveExactOrder()
        {
            CollectionAssert.AreEqual(new[]
            {
                "Completed", "InvalidInput", "RetryRequired"
            }, Enum.GetNames(typeof(MultiSeedBiomeGrowthStatus)));
            CollectionAssert.AreEqual(new[]
            {
                "MissingPlacementResult", "PlacementNotCompleted", "MissingPlacementPublication",
                "MissingPlacementDiagnostics", "InvalidPlacementPublication", "InvalidSourceSiteSnapshot",
                "MissingGenerationProfile", "InvalidGenerationProfile", "MissingBiomeTypes", "MissingPatchRules",
                "NullDefinition", "DuplicateDefinitionId", "MissingBiomeDefinition", "UnexpectedBiomeDefinition",
                "MissingPatchRule", "UnexpectedPatchRule", "InvalidBiomeDefinition", "InvalidPatchRule",
                "DefinitionIdentityMismatch", "InvalidPatchState", "InvalidReservationState",
                "MissingBiomePatchRng", "InvalidBiomePatchRngState", "InternalInvariantViolation",
                "InsufficientAggregateCapacity", "MinimumGrowthBlocked", "GrowthFrontierExhausted"
            }, Enum.GetNames(typeof(MultiSeedBiomeGrowthErrorCode)));
        }

        private static MultiSeedBiomeGrowthResult Grow(
            Fixture source,
            SatelliteSeedPlacementResult placement,
            DeterministicRngStream rng)
        {
            return new MultiSeedBiomeGrower().Grow(
                placement, source.Profile, source.Definitions.Biomes,
                source.Definitions.AllRules, rng);
        }

        private static SatelliteSeedPlacementResult Place(
            Fixture source,
            DeterministicRngStream rng)
        {
            return new SatelliteSeedPlacer().Place(
                source.Growth, source.Profile, source.Definitions.Biomes,
                source.Definitions.SatelliteRules, rng);
        }

        private static Fixture BuildFixture()
        {
            var definitions = BuildBiomeDefinitions();
            var routeDefinitions = BuildRouteDefinitions();
            return BuildFixture(KnownWorldSeed, routeDefinitions, definitions);
        }

        private static Fixture BuildFixture(
            ulong worldSeed,
            WorldRouteDefinitionSet routeDefinitions,
            BiomeDefinitions definitions)
        {
            var source = BuildSourceSnapshot(worldSeed);
            var initialization = new CorePatchSeedInitializer().Initialize(
                source, definitions.Biomes, definitions.CoreRules);
            if (!initialization.Succeeded)
                throw new InvalidOperationException(string.Join("\n", initialization.Errors.Select(value =>
                    value.Code + ":" + value.Message)));
            var growth = new CorePatchGrower().Grow(
                initialization.Publication, definitions.Biomes, definitions.CoreRules);
            if (!growth.Succeeded)
                throw new InvalidOperationException(string.Join("\n", growth.Errors.Select(value =>
                    value.Code + ":" + value.Message)));
            return new Fixture(
                growth.Publication,
                routeDefinitions.GenerationProfiles["GEN_MOONPALACE_V1"],
                new WorldGenerationRngStreams(routeDefinitions), definitions);
        }

        private static SiteReservationSnapshot BuildSourceSnapshot(ulong worldSeed)
        {
            var forge = new SectorCoord(2, 2);
            var cassia = new SectorCoord(8, 2);
            var dough = new SectorCoord(2, 8);
            var crater = new SectorCoord(8, 8);
            var reservations = new List<SiteReservation>
            {
                CreateReservation(0, StartId, "WORLD_MOONPALACE_V1",
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
                worldSeed, reservations, CreateSectorReservations(reservations), seeds);
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
                localX, 0, kind == SiteReservationKind.Start ? "START" : "CORE",
                biomeId, string.Empty, Array.Empty<SiteEntrySide>()));
            return new SiteReservation(
                new SiteReservationId(reservationId), kind, sourceDefinitionId, origin,
                new SiteFootprint(width, 1, SiteFootprintTransform.R0, cells), biomeId,
                order, Array.Empty<SiteEntryAnchor>());
        }

        private static CoreBiomeSeed CoreSeed(
            SiteReservation reservation,
            string biomeId,
            string ruleId,
            int minimum)
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
                else result.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            return result;
        }

        private static BiomeDefinitions BuildBiomeDefinitions()
        {
            var specs = CreateBiomeFileSpecs();
            var biomeRows = new[]
            {
                BiomeRow("BIO_ABANDONED_MILL", 1, 11),
                BiomeRow("BIO_CASSIA_ROOT", 2, 12),
                BiomeRow("BIO_MOON_DOUGH", 0, 7),
                BiomeRow("BIO_MOON_CRATER", 0, 7)
            };
            var patchRows = new[]
            {
                PatchRow("PATCH_MILL_CORE", "BIO_ABANDONED_MILL", "CORE", 4, 14, 1, 1, 1, 1, false, "0.20", "0.35", "0.85"),
                PatchRow("PATCH_ROOT_CORE", "BIO_CASSIA_ROOT", "CORE", 5, 18, 1, 1, 1, 1, false, "0.35", "0.45", "0.70"),
                PatchRow("PATCH_DOUGH_CORE", "BIO_MOON_DOUGH", "CORE", 5, 18, 1, 1, 1, 1, true, "0.40", "0.45", "0.70"),
                PatchRow("PATCH_CRATER_CORE", "BIO_MOON_CRATER", "CORE", 5, 18, 1, 1, 1, 1, true, "0.25", "0.45", "0.75"),
                PatchRow("PATCH_CRATER_SAT", "BIO_MOON_CRATER", "SATELLITE", 2, 16, 3, 0, 3, 70, true, "0.25", "0.60", "0.65"),
                PatchRow("PATCH_DOUGH_SAT", "BIO_MOON_DOUGH", "SATELLITE", 2, 14, 3, 0, 3, 70, true, "0.40", "0.60", "0.65"),
                PatchRow("PATCH_MILL_SAT", "BIO_ABANDONED_MILL", "SATELLITE", 2, 10, 3, 0, 2, 45, false, "0.20", "0.50", "0.80"),
                PatchRow("PATCH_ROOT_SAT", "BIO_CASSIA_ROOT", "SATELLITE", 2, 14, 3, 0, 3, 70, false, "0.35", "0.60", "0.60")
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
                rules.Where(value => value.PatchRole == "SATELLITE").ToArray(),
                rules);
        }

        private static string[] BiomeRow(string biomeId, int altitudeMinimum, int altitudeMaximum)
        {
            return new[]
            {
                biomeId, "NAME", "STAGE_MOON", "1", "1", "4", "1",
                altitudeMinimum.ToString(CultureInfo.InvariantCulture),
                altitudeMaximum.ToString(CultureInfo.InvariantCulture),
                "1", "THEME", "AUDIO", "MICRO", "RECIPE", "RESOURCE", "ELEMENT",
                "SITE_REQUIRED", "1", string.Empty
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
            bool edge,
            string altitudeWeight,
            string noiseWeight,
            string compactnessWeight)
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
                role == "CORE" ? "1" : "0", "0", "0.35", "1", altitudeWeight,
                noiseWeight, compactnessWeight, "0.5", "1", string.Empty
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
                error.FileName + ":" + error.ErrorCode + ":" + error.ColumnName + ":" + error.Message)));
            return result.DefinitionSet;
        }

        private static BiomeBoundaryDefinitionSource BuildBiomeSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var schema = BuildSchema(spec);
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
            var schema = BuildSchema(spec);
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
                var allowed = parts[0] == "patch_role" ? "CORE|SATELLITE|INTRUSION" :
                    parts[0] == "reset_scope" ? "WORLD|PASS|SECTOR|PATCH|SITE|SPAWN" :
                    parts[1] == "ENUM" || parts[1] == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty;
                return new ColumnSpec(parts[0], parts[1], allowed);
            }).ToArray());
        }

        private static string CsvCell(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string SnapshotSignature(BiomePatchSnapshot snapshot)
        {
            return string.Join("|", snapshot.Patches.Select(patch =>
                patch.Id + ":" + string.Join(",", patch.SectorIndices))) + "#" +
                string.Join("|", snapshot.Sectors.Select(value =>
                    value.IsAssigned ? value.PatchId.Value.ToString() : "_"));
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
                CorePatchGrowthPublication growth,
                GenerationProfileDefinition profile,
                WorldGenerationRngStreams rngStreams,
                BiomeDefinitions definitions)
            {
                Growth = growth;
                Profile = profile;
                RngStreams = rngStreams;
                Definitions = definitions;
            }
            public CorePatchGrowthPublication Growth { get; }
            public GenerationProfileDefinition Profile { get; }
            public WorldGenerationRngStreams RngStreams { get; }
            public BiomeDefinitions Definitions { get; }
        }

        private sealed class BiomeDefinitions
        {
            public BiomeDefinitions(
                IReadOnlyList<BiomeTypeDefinition> biomes,
                IReadOnlyList<BiomePatchRuleDefinition> coreRules,
                IReadOnlyList<BiomePatchRuleDefinition> satelliteRules,
                IReadOnlyList<BiomePatchRuleDefinition> allRules)
            {
                Biomes = biomes;
                CoreRules = coreRules;
                SatelliteRules = satelliteRules;
                AllRules = allRules;
            }
            public IReadOnlyList<BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> CoreRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> SatelliteRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> AllRules { get; }
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
