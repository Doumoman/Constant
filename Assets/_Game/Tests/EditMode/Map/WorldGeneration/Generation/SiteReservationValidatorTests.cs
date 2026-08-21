using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class SiteReservationValidatorTests
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";
        private const string VillageId = "SITE_PRIMARY_VILLAGE";
        private const string VillageProfileId = "VIL_MOON_PRIMARY";
        private const string Layout5Id = "VIL_LAYOUT_5";
        private const string Layout6Id = "VIL_LAYOUT_6";
        private const string CraterBiomeId = "BIO_MOON_CRATER";
        private const string CassiaBiomeId = "BIO_CASSIA_ROOT";
        private const string MillBiomeId = "BIO_ABANDONED_MILL";
        private const string DoughBiomeId = "BIO_MOON_DOUGH";

        private static readonly FileSpec[] BiomeSpecs = CreateBiomeSpecs();
        private static readonly FileSpec[] SpecialSpecs = CreateSpecialSpecs();
        private static readonly StarterData Starter = BuildStarterData();

        public static IEnumerable ValidationCases()
        {
            for (var index = 0; index < 260; index++)
                yield return new TestCaseData(index).SetName(
                    "ValidateAndPublish_CanonicalCase_" + index.ToString("D3", CultureInfo.InvariantCulture));
        }

        [TestCaseSource(nameof(ValidationCases))]
        public void ValidateAndPublish_CanonicalCasesPublishExactSnapshot(int caseIndex)
        {
            var seed = caseIndex == 0 ? 0UL :
                caseIndex == 1 ? 4660UL :
                caseIndex == 2 ? ulong.MaxValue : (ulong)caseIndex;
            var result = Validate(seed, Starter.Maps, Starter.Cells, Starter.Entries);

            Assert.That(result.Status, Is.EqualTo(SiteReservationValidationStatus.Completed));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Violations, Is.Empty);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Publication.SourceApproval, Is.SameAs(Starter.Approval));
            Assert.That(result.Publication.Snapshot.Seed, Is.EqualTo(seed));
            Assert.That(result.Publication.ReservationCount, Is.EqualTo(7));
            Assert.That(result.Publication.ReservedSectorCount, Is.EqualTo(8));
            Assert.That(result.Publication.Snapshot.Sectors, Has.Count.EqualTo(169));
            Assert.That(result.Publication.EntryAnchorCount, Is.EqualTo(6));
            Assert.That(result.Publication.CoreSeedCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.Rules.Count(item => item.Passed), Is.EqualTo(6));
            Assert.That(result.Diagnostics.NonVillageDistanceConstraintCount, Is.EqualTo(15));
            Assert.That(result.Diagnostics.VillageDistanceCheckCount, Is.EqualTo(6));
            Assert.That(result.Diagnostics.CoreClusterCheckCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.CoreWitnessSectorCount, Is.EqualTo(20));
        }

        [Test]
        public void ValidateAndPublish_UsesFrozenReservationIdsAndOrder()
        {
            var result = Validate(4660, Starter.Maps.Reverse(), Starter.Cells.Reverse(), Starter.Entries.Reverse());
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Publication.ReservationIds.Select(item => item.Value), Is.EqualTo(new[]
            {
                "RSV_00_WORLD_MOONPALACE_V1",
                "RSV_01_SITE_MOON_BOSS_VAULT",
                "RSV_02_SITE_MOON_SEAL_FORGE",
                "RSV_03_SITE_CASSIA_SAP_HEART",
                "RSV_04_SITE_DEEP_STAR_YEAST",
                "RSV_05_SITE_MOON_CORE_METEOR",
                "RSV_06_SITE_PRIMARY_VILLAGE"
            }));
            Assert.That(result.Publication.Snapshot.Reservations.Select(item => item.ReservationOrder),
                Is.EqualTo(Enumerable.Range(0, 7)));
        }

        [Test]
        public void ValidateAndPublish_PublishesExactSectorAndVillageContracts()
        {
            var result = Validate(0, Starter.Maps, Starter.Cells, Starter.Entries);
            var snapshot = result.Publication.Snapshot;
            Assert.That(snapshot.Sectors.Select(item => item.Index), Is.EqualTo(Enumerable.Range(0, 169)));
            Assert.That(snapshot.Sectors.Count(item => item.IsReserved), Is.EqualTo(8));
            Assert.That(snapshot.Sectors.Count(item => !item.IsReserved), Is.EqualTo(161));
            Assert.That(snapshot.TryGetReservation(
                new SiteReservationId("RSV_06_SITE_PRIMARY_VILLAGE"), out var village), Is.True);
            Assert.That(village.Footprint.Transform, Is.EqualTo(SiteFootprintTransform.R0));
            Assert.That(village.Footprint.Cells.All(item => item.LocalRole == "VILLAGE"), Is.True);
            Assert.That(village.EntryAnchors, Has.Count.EqualTo(1));
            Assert.That(village.EntryAnchors[0].Required, Is.True);
            Assert.That(village.EntryAnchors[0].ReturnPathRequired, Is.True);
        }

        [Test]
        public void ValidateAndPublish_PublishesExactCoreSeeds()
        {
            var result = Validate(0, Starter.Maps, Starter.Cells, Starter.Entries);
            var seeds = result.Publication.Snapshot.CoreBiomeSeeds;
            Assert.That(seeds.Select(item => item.BiomeId), Is.EqualTo(new[]
            {
                MillBiomeId, CassiaBiomeId, DoughBiomeId, CraterBiomeId
            }));
            Assert.That(seeds.Select(item => item.CorePatchRuleId), Is.EqualTo(new[]
            {
                "PATCH_MILL_CORE", "PATCH_ROOT_CORE", "PATCH_DOUGH_CORE", "PATCH_CRATER_CORE"
            }));
            Assert.That(seeds.Select(item => item.MinimumCoreSectorCount), Is.EqualTo(new[] { 4, 5, 5, 5 }));
            Assert.That(seeds.Select(item => item.BufferRingSectors), Is.EqualTo(new[] { 1, 1, 1, 1 }));
        }

        [Test]
        public void ValidateAndPublish_NullInputsAccumulateAndPublishNothing()
        {
            var result = new SiteReservationValidator().ValidateAndPublish(0, null, null, null, null);
            Assert.That(result.Status, Is.EqualTo(SiteReservationValidationStatus.InvalidInput));
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Violations, Is.Empty);
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                SiteReservationValidationErrorCode.MissingApproval));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                SiteReservationValidationErrorCode.MissingSpecialMaps));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                SiteReservationValidationErrorCode.MissingFootprintCells));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                SiteReservationValidationErrorCode.MissingEntrySockets));
        }

        [Test]
        public void ValidateAndPublish_MissingDefinitionIsAtomicInvalidInput()
        {
            var maps = Starter.Maps.Where(item => item.SpecialMapId != VillageId).ToArray();
            var result = Validate(0, maps, Starter.Cells, Starter.Entries);
            Assert.That(result.Status, Is.EqualTo(SiteReservationValidationStatus.InvalidInput));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors.Any(item =>
                item.Code == SiteReservationValidationErrorCode.MissingRequiredSpecialMap &&
                item.DefinitionId == VillageId), Is.True);
        }

        [Test]
        public void ValidateAndPublish_DuplicateDefinitionsAreSortedAndDeduplicated()
        {
            var maps = Starter.Maps.Concat(new[] { Starter.Maps[0], Starter.Maps[0] }).Reverse().ToArray();
            var result = Validate(0, maps, Starter.Cells, Starter.Entries);
            Assert.That(result.Status, Is.EqualTo(SiteReservationValidationStatus.InvalidInput));
            Assert.That(result.Errors.Count(item =>
                item.Code == SiteReservationValidationErrorCode.DuplicateSpecialMapId), Is.EqualTo(1));
            Assert.That(result.Errors, Is.Ordered.Using<SiteReservationValidationError>(
                Comparer<SiteReservationValidationError>.Create((left, right) =>
                    left.Code.CompareTo(right.Code))));
        }

        [Test]
        public void ValidateAndPublish_IsStableAcrossCultureAndReuse()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var validator = new SiteReservationValidator();
                var first = validator.ValidateAndPublish(
                    4660, Starter.Approval, Starter.Maps, Starter.Cells, Starter.Entries);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                var second = validator.ValidateAndPublish(
                    4660, Starter.Approval, Starter.Maps.Reverse(),
                    Starter.Cells.Reverse(), Starter.Entries.Reverse());
                Assert.That(Snapshot(first), Is.EqualTo(Snapshot(second)));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void PublicModelsExposeReadOnlySnapshotsAndExactEnumOrder()
        {
            Assert.That(Enum.GetNames(typeof(SiteReservationValidationRule)), Is.EqualTo(new[]
            {
                "RequiredSiteCounts", "WorldBounds", "FootprintOverlap",
                "DistanceConstraints", "EntryAnchors", "CoreCapacity"
            }));
            Assert.That(Enum.GetNames(typeof(SiteReservationValidationStatus)), Is.EqualTo(new[]
            {
                "Completed", "ValidationRejected", "InvalidInput"
            }));
            var result = Validate(0, Starter.Maps, Starter.Cells, Starter.Entries);
            Assert.That(result.Publication.ReservationIds, Is.InstanceOf<ReadOnlyCollection<SiteReservationId>>());
            Assert.That(result.Diagnostics.Rules, Is.InstanceOf<ReadOnlyCollection<SiteReservationRuleResult>>());
        }

        private static SiteReservationValidationResult Validate(
            ulong seed,
            IEnumerable<SpecialMapDefinition> maps,
            IEnumerable<SpecialMapFootprintCellDefinition> cells,
            IEnumerable<SpecialMapEntrySocketDefinition> entries) =>
            new SiteReservationValidator().ValidateAndPublish(
                seed, Starter.Approval, maps, cells, entries);

        private static string Snapshot(SiteReservationValidationResult result) =>
            string.Join("|", result.Publication.Snapshot.Reservations.Select(item =>
                item.ReservationId.Value + ":" + item.Origin + ":" + item.Footprint.Width + "x" +
                item.Footprint.Height)) + "|" + result.Diagnostics.ReservedSectorCount + "|" +
            result.Diagnostics.CoreWitnessSectorCount;

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
            if (!biomeResult.Success) throw new InvalidOperationException(string.Join("\n", biomeResult.Errors));

            var rowsByFile = SpecialRows();
            var specialResult = new SpecialVillageDefinitionBuilder().Build(
                SpecialSpecs.Select(spec => BuildSpecialSource(spec,
                    rowsByFile.TryGetValue(spec.FileName, out var rows) ? rows : null)));
            if (!specialResult.Success) throw new InvalidOperationException(string.Join("\n", specialResult.Errors));
            var definitions = specialResult.DefinitionSet;
            var maps = definitions.SpecialMaps.Values.ToArray();
            var cells = definitions.SpecialMapFootprintCells.ToArray();
            var entries = definitions.SpecialMapEntrySockets.ToArray();
            var policyResult = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(WorldId, maps);
            if (!policyResult.Succeeded) throw new InvalidOperationException(string.Join("\n", policyResult.Errors));

            var biomes = biomeResult.DefinitionSet.BiomeTypes;
            var coreRules = biomeResult.DefinitionSet.BiomePatchRules.Values.ToDictionary(
                item => item.BiomeId, StringComparer.Ordinal);
            var plan = BuildPlan(definitions, biomes, coreRules, policyResult.Policy);
            var requirements = new List<CoreCapacityRequirement>();
            for (var index = 2; index <= 5; index++)
            {
                var placement = plan.SelectedPlacements[index];
                var key = SitePlacementKey.FromPlacement(placement);
                var special = definitions.SpecialMaps[key.SourceDefinitionId];
                var biome = biomes[special.PrimaryBiomeId];
                requirements.Add(new CoreCapacityRequirement(
                    key, placement, special, biome, coreRules[biome.BiomeId]));
            }
            var capacity = new CoreCapacityFloodChecker().Check(plan, requirements);
            if (!capacity.Succeeded) throw new InvalidOperationException(string.Join("\n",
                capacity.Errors.Select(item => item.Code + ":" + item.Message)));

            var village = new VillageReservationSelector().Reserve(
                capacity.Approval,
                definitions.VillageProfiles[VillageProfileId],
                definitions.SpecialMaps[VillageId],
                definitions.GetSpecialMapEntrySockets(VillageId),
                new[] { definitions.VillageLayouts[Layout5Id], definitions.VillageLayouts[Layout6Id] },
                new DeterministicRngStream(20));
            if (!village.Succeeded) throw new InvalidOperationException(string.Join("\n",
                village.Errors.Select(item => item.Code + ":" + item.Message)));
            return new StarterData(village.Approval, maps, cells, entries);
        }

        private static SiteReservationSelectionPlan BuildPlan(
            SpecialVillageDefinitionSet definitions,
            IReadOnlyDictionary<string, BiomeTypeDefinition> biomes,
            IReadOnlyDictionary<string, BiomePatchRuleDefinition> coreRules,
            SiteDistancePolicy policy)
        {
            var solver = new FootprintPlacementSolver();
            var groups = new List<SiteReservationSearchGroup>
            {
                StartGroup(solver, 0, 0),
                SpecialGroup(solver, definitions, biomes, coreRules, BossId, SiteReservationKind.Boss, 4, 0),
                SpecialGroup(solver, definitions, biomes, coreRules, ForgeId, SiteReservationKind.Forge, 2, 4),
                SpecialGroup(solver, definitions, biomes, coreRules, CassiaId, SiteReservationKind.CoreResource, 5, 4),
                SpecialGroup(solver, definitions, biomes, coreRules, YeastId, SiteReservationKind.CoreResource, 8, 1),
                SpecialGroup(solver, definitions, biomes, coreRules, MeteorId, SiteReservationKind.CoreResource, 8, 8)
            };
            var result = new SiteReservationBacktracker().Search(
                groups, policy, SiteCandidateCostWeights.Default,
                SiteReservationSearchLimits.Default, new DeterministicRngStream(10));
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("\n",
                result.Errors.Select(item => item.Code + ":" + item.Message)));
            return result.SelectionPlan;
        }

        private static SiteReservationSearchGroup StartGroup(
            FootprintPlacementSolver solver,
            int x,
            int y)
        {
            var origin = new SectorCoord(x, y);
            var candidate = new SiteOriginCandidate(
                SiteReservationKind.Start, WorldId, 0, origin,
                WorldGridIndex.ToIndex(origin), EdgeRing(origin), 0);
            var placement = solver.SolveStart(candidate, FootprintPlacementBlockers.Empty);
            if (!placement.Succeeded) throw new InvalidOperationException("Start placement failed.");
            return new SiteReservationSearchGroup(
                new SitePlacementKey(SiteReservationKind.Start, WorldId, 0),
                null, null, null,
                new[] { new SiteReservationSearchOption(placement.Placement, -1) });
        }

        private static SiteReservationSearchGroup SpecialGroup(
            FootprintPlacementSolver solver,
            SpecialVillageDefinitionSet definitions,
            IReadOnlyDictionary<string, BiomeTypeDefinition> biomes,
            IReadOnlyDictionary<string, BiomePatchRuleDefinition> coreRules,
            string sourceId,
            SiteReservationKind kind,
            int x,
            int y)
        {
            var origin = new SectorCoord(x, y);
            var candidate = new SiteOriginCandidate(
                kind, sourceId, 0, origin, WorldGridIndex.ToIndex(origin), EdgeRing(origin), 0);
            var special = definitions.SpecialMaps[sourceId];
            var placement = solver.SolveSpecialSite(
                candidate, SiteFootprintTransform.R0, special,
                definitions.GetSpecialMapFootprintCells(sourceId),
                definitions.GetSpecialMapEntrySockets(sourceId),
                FootprintPlacementBlockers.Empty);
            if (!placement.Succeeded) throw new InvalidOperationException("Special placement failed: " + sourceId);
            var biome = biomes[special.PrimaryBiomeId];
            return new SiteReservationSearchGroup(
                new SitePlacementKey(kind, sourceId, 0), special, biome,
                coreRules[biome.BiomeId],
                new[] { new SiteReservationSearchOption(placement.Placement, -1) });
        }

        private static int EdgeRing(SectorCoord coordinate) => Math.Min(
            Math.Min(coordinate.X, WorldGenConstants.SectorColumns - 1 - coordinate.X),
            Math.Min(coordinate.Y, WorldGenConstants.SectorRows - 1 - coordinate.Y));

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

        private sealed class StarterData
        {
            public StarterData(
                VillageReservationApproval approval,
                IReadOnlyList<SpecialMapDefinition> maps,
                IReadOnlyList<SpecialMapFootprintCellDefinition> cells,
                IReadOnlyList<SpecialMapEntrySocketDefinition> entries)
            {
                Approval = approval;
                Maps = maps;
                Cells = cells;
                Entries = entries;
            }

            public VillageReservationApproval Approval { get; }
            public IReadOnlyList<SpecialMapDefinition> Maps { get; }
            public IReadOnlyList<SpecialMapFootprintCellDefinition> Cells { get; }
            public IReadOnlyList<SpecialMapEntrySocketDefinition> Entries { get; }
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
