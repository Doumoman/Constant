using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public sealed class SpecialVillageDefinitionBuilderTests
    {
        private static readonly FileSpec[] Specs = CreateSpecs();
        private static IEnumerable<string> FileNames => Specs.Select(spec => spec.FileName);

        [Test]
        public void ExactTwelveSourcesBuildSuccessfully()
        {
            var result = Build(StandardSources());

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(TotalCount(result.DefinitionSet), Is.EqualTo(12));
        }

        [Test]
        public void ShuffledSourceOrderProducesSameMembershipAndOrder()
        {
            var forward = Build(StandardSources()).DefinitionSet;
            var reverse = Build(StandardSources().AsEnumerable().Reverse()).DefinitionSet;

            Assert.That(Snapshot(reverse), Is.EqualTo(Snapshot(forward)));
        }

        [TestCaseSource(nameof(FileNames))]
        public void MissingSourceIsReported(string fileName)
        {
            var result = Build(StandardSources().Where(source => source.FileName != fileName));

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error =>
                error.FileName == fileName &&
                error.ErrorCode == SpecialVillageDefinitionBuildErrorCode.MissingSource), Is.True);
        }

        [TestCaseSource(nameof(FileNames))]
        public void EveryDefinitionMapsAllColumnsAndSourceRecord(string fileName)
        {
            var result = Build(StandardSources());

            Assert.That(result.Success, Is.True, FormatErrors(result));
            AssertFullMapping(fileName, result.DefinitionSet);
        }

        [Test]
        public void DuplicateSourceFailsWithoutPartialSet()
        {
            var sources = StandardSources();
            sources.Add(sources[0]);

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single().ErrorCode,
                Is.EqualTo(SpecialVillageDefinitionBuildErrorCode.DuplicateSource));
        }

        [Test]
        public void UnexpectedSourceFailsWithoutPartialSet()
        {
            var sources = StandardSources();
            sources.Add(BuildSource(new FileSpec(
                "unexpected.csv", 1, Column("id", "ID"))));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single(error => error.FileName == "unexpected.csv").ErrorCode,
                Is.EqualTo(SpecialVillageDefinitionBuildErrorCode.UnexpectedSource));
        }

        [Test]
        public void UnsuccessfulParseIsRejected()
        {
            var sources = StandardSources();
            var spec = Spec("event_activation_routes.csv");
            var row = StandardRow(spec);
            row[7] = "bad-int";
            Replace(sources, BuildSource(spec, new[] { row }, false));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single().ErrorCode,
                Is.EqualTo(SpecialVillageDefinitionBuildErrorCode.UnsuccessfulParse));
        }

        [Test]
        public void SchemaColumnTypeMismatchIsRejected()
        {
            var sources = StandardSources();
            var original = Spec("special_map_catalog.csv");
            var columns = original.Columns.ToArray();
            columns[4] = Column("footprint_width_sectors", "STRING");
            Replace(sources, BuildSource(new FileSpec(
                original.FileName, original.PrimaryKeyCount, columns)));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error =>
                error.ErrorCode == SpecialVillageDefinitionBuildErrorCode.SchemaMismatch &&
                error.ColumnOrder == 5), Is.True);
        }

        [TestCase("event_activation_routes.csv", "requires_tool", "BOOL", "ID")]
        [TestCase("event_activation_routes.csv", "requires_consumable", "BOOL", "ID")]
        [TestCase("special_map_catalog.csv", "requires_tool", "BOOL", "ID")]
        public void AuthoritativeRepairTypeIsAcceptedAndNearMissIsRejected(
            string fileName,
            string columnName,
            string authoritativeType,
            string nearMissType)
        {
            var exact = Spec(fileName);
            var columnIndex = exact.Columns.ToList().FindIndex(column => column.Name == columnName);
            Assert.That(columnIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(exact.Columns[columnIndex].DataType, Is.EqualTo(authoritativeType));
            var exactResult = Build(StandardSources());
            Assert.That(exactResult.Success, Is.True, FormatErrors(exactResult));

            var columns = exact.Columns.ToArray();
            columns[columnIndex] = Column(columnName, nearMissType);
            var sources = StandardSources();
            Replace(sources, BuildSource(new FileSpec(
                fileName, exact.PrimaryKeyCount, columns)));

            var result = Build(sources);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors.Any(error =>
                error.ErrorCode == SpecialVillageDefinitionBuildErrorCode.SchemaMismatch &&
                error.ColumnOrder == columnIndex + 1 &&
                error.ColumnName == columnName), Is.True);
        }

        [TestCase("event_activation_routes.csv", "requires_tool", "0", false)]
        [TestCase("event_activation_routes.csv", "requires_tool", "1", true)]
        [TestCase("event_activation_routes.csv", "requires_consumable", "0", false)]
        [TestCase("event_activation_routes.csv", "requires_consumable", "1", true)]
        [TestCase("special_map_catalog.csv", "requires_tool", "0", false)]
        [TestCase("special_map_catalog.csv", "requires_tool", "1", true)]
        public void AuthoritativeBoolFieldsMaterializeAsPublicBool(
            string fileName,
            string columnName,
            string token,
            bool expected)
        {
            var spec = Spec(fileName);
            var columnIndex = spec.Columns.ToList().FindIndex(column => column.Name == columnName);
            var row = StandardRow(spec);
            row[columnIndex] = token;
            var sources = StandardSources();
            Replace(sources, BuildSource(spec, new[] { row }));

            var result = Build(sources);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            var definition = DefinitionFor(fileName, result.DefinitionSet);
            var property = definition.GetType().GetProperty(
                ToPascalCase(columnName),
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
            Assert.That(property.GetValue(definition), Is.EqualTo(expected));
        }

        [Test]
        public void ParsedFieldsFromEquivalentOtherSchemaAreRejected()
        {
            var sources = StandardSources();
            var original = sources.Single(source => source.FileName == "event_activation_routes.csv");
            var other = BuildSource(Spec("event_activation_routes.csv"));
            Replace(sources, new SpecialVillageDefinitionSource(original.Schema, other.ParseResult));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.All(error =>
                error.ErrorCode == SpecialVillageDefinitionBuildErrorCode.FieldMappingFailed), Is.True);
            Assert.That(result.Errors.First().RecordNumber, Is.EqualTo(2));
            Assert.That(result.Errors.First().Location, Is.Not.Null);
        }

        [Test]
        public void NullEnumerableThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SpecialVillageDefinitionBuilder().Build(null));
        }

        [Test]
        public void NullSourceElementIsDeterministicFailure()
        {
            var sources = StandardSources();
            sources.Add(null);

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.First().FileName, Is.Empty);
            Assert.That(result.Errors.First().ErrorCode,
                Is.EqualTo(SpecialVillageDefinitionBuildErrorCode.MissingSource));
        }

        [Test]
        public void SourceRejectsNullSchemaAndParseResult()
        {
            var source = BuildSource(Spec("event_activation_routes.csv"));

            Assert.Throws<ArgumentNullException>(() =>
                new SpecialVillageDefinitionSource(null, source.ParseResult));
            Assert.Throws<ArgumentNullException>(() =>
                new SpecialVillageDefinitionSource(source.Schema, null));
        }

        [Test]
        public void SevenDictionariesUseOrdinalLookupAndEnumeration()
        {
            var sources = StandardSources();
            foreach (var fileName in DictionaryFileNames())
            {
                var spec = Spec(fileName);
                var z = StandardRow(spec);
                var a = StandardRow(spec);
                z[0] = "Z_KEY";
                a[0] = "A_KEY";
                Replace(sources, BuildSource(spec, new[] { z, a }));
            }

            var set = Build(sources).DefinitionSet;

            Assert.That(set.EventActivationRoutes.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.SpecialMaps.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.ShopArchetypes.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.ShopkeeperSpecies.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.VillageFacilities.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.VillageLayouts.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.VillageProfiles.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.SpecialMaps.ContainsKey("a_key"), Is.False);
        }

        [Test]
        public void FiveCompositeCollectionsAreStable()
        {
            var set = Build(CompositeSources()).DefinitionSet;

            Assert.That(set.SpecialMapEntrySockets.Select(item => item.EntrySocketId),
                Is.EqualTo(new[] { "A_SOCKET", "Z_SOCKET" }));
            Assert.That(set.SpecialMapFootprintCells.Select(item => item.LocalSectorX),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.SpecialMapRewards.Select(item => item.RewardOrder),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.ShopInventoryRules.Select(item => item.SlotIndex),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.VillageLayoutCells.Select(item => item.LocalChunkX),
                Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void ParentQueriesAreStableAndReadOnly()
        {
            var set = Build(CompositeSources()).DefinitionSet;

            var sockets = set.GetSpecialMapEntrySockets("PARENT");
            var cells = set.GetSpecialMapFootprintCells("PARENT");
            var rewards = set.GetSpecialMapRewards("PARENT");
            var inventory = set.GetShopInventoryRules("PARENT");
            var layoutCells = set.GetVillageLayoutCells("PARENT");

            Assert.That(sockets.Select(item => item.EntrySocketId), Is.EqualTo(new[] { "A_SOCKET", "Z_SOCKET" }));
            Assert.That(cells.Select(item => item.LocalSectorX), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(rewards.Select(item => item.RewardOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(inventory.Select(item => item.SlotIndex), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(layoutCells.Select(item => item.LocalChunkX), Is.EqualTo(new[] { 1, 2 }));
            Assert.Throws<NotSupportedException>(() => ((IList<SpecialMapEntrySocketDefinition>)sockets).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<SpecialMapFootprintCellDefinition>)cells).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<SpecialMapRewardDefinition>)rewards).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<ShopInventoryRuleDefinition>)inventory).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<VillageLayoutCellDefinition>)layoutCells).Clear());
            Assert.That(set.GetSpecialMapEntrySockets("UNKNOWN"), Is.Empty);
            Assert.That(set.GetSpecialMapFootprintCells("UNKNOWN"), Is.Empty);
            Assert.That(set.GetSpecialMapRewards("UNKNOWN"), Is.Empty);
            Assert.That(set.GetShopInventoryRules("UNKNOWN"), Is.Empty);
            Assert.That(set.GetVillageLayoutCells("UNKNOWN"), Is.Empty);
        }

        [Test]
        public void NestedListsPreserveOrderDuplicatesAndAreReadOnly()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.That(set.EventActivationRoutes.Values.Single().AllowedSectorTypes,
                Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.That(set.SpecialMaps.Values.Single().AllowedEntryRouteTypes,
                Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.That(set.SpecialMapFootprintCells.Single().RequiredOpenSides,
                Is.EqualTo(new[] { "ENUM_A", "ENUM_B", "ENUM_A" }));
            Assert.That(set.ShopkeeperSpecies.Values.Single().AllowedBiomeIds,
                Is.EqualTo(new[] { "LIST_A", "LIST_B", "LIST_A" }));
            Assert.That(set.VillageProfiles.Values.Single().FixedFacilityIds,
                Is.EqualTo(new[] { "LIST_A", "LIST_B", "LIST_A" }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)set.EventActivationRoutes.Values.Single().AllowedSectorTypes).Add(3));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)set.VillageProfiles.Values.Single().FixedFacilityIds).Clear());
        }

        [Test]
        public void OptionalEmptyValuesArePreservedExactly()
        {
            var sources = StandardSources();
            var eventSpec = Spec("event_activation_routes.csv");
            var eventRow = StandardRow(eventSpec);
            eventRow[5] = string.Empty;
            eventRow[6] = string.Empty;
            eventRow[10] = string.Empty;
            Replace(sources, BuildSource(eventSpec, new[] { eventRow }));
            var facilitySpec = Spec("village_facilities.csv");
            var facilityRow = StandardRow(facilitySpec);
            facilityRow[6] = string.Empty;
            facilityRow[7] = string.Empty;
            facilityRow[9] = string.Empty;
            Replace(sources, BuildSource(facilitySpec, new[] { facilityRow }));

            var set = Build(sources).DefinitionSet;

            Assert.That(set.EventActivationRoutes.Values.Single().RequiresTool, Is.False);
            Assert.That(set.EventActivationRoutes.Values.Single().RequiresConsumable, Is.False);
            Assert.That(set.EventActivationRoutes.Values.Single().Notes, Is.Empty);
            Assert.That(set.VillageFacilities.Values.Single().ShopArchetypeId, Is.Empty);
            Assert.That(set.VillageFacilities.Values.Single().EvacuatedPrefabId, Is.Empty);
            Assert.That(set.VillageFacilities.Values.Single().Notes, Is.Empty);
        }

        [Test]
        public void DefaultAppliedValueAndUsedDefaultArePreserved()
        {
            var sources = StandardSources();
            var spec = Spec("shop_archetypes.csv");
            var columns = spec.Columns.ToArray();
            columns[5] = Column("base_price_multiplier", "FLOAT", "+1.25");
            var defaultSpec = new FileSpec(spec.FileName, spec.PrimaryKeyCount, columns);
            var row = StandardRow(defaultSpec);
            row[5] = string.Empty;
            Replace(sources, BuildSource(defaultSpec, new[] { row }));

            var definition = Build(sources).DefinitionSet.ShopArchetypes.Values.Single();

            Assert.That(definition.BasePriceMultiplier, Is.EqualTo(1.25f));
            Assert.That(definition.SourceRecord.Fields[5].UsedDefault, Is.True);
            Assert.That(definition.SourceRecord.Fields[5].RawValue, Is.Empty);
        }

        [Test]
        public void InactiveRowsAreRetained()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.That(set.SpecialMaps.Values.Single().Active, Is.False);
            Assert.That(set.ShopArchetypes.Values.Single().Active, Is.False);
            Assert.That(set.ShopInventoryRules.Single().Active, Is.False);
            Assert.That(set.ShopkeeperSpecies.Values.Single().Active, Is.False);
            Assert.That(set.VillageFacilities.Values.Single().Active, Is.False);
            Assert.That(set.VillageLayouts.Values.Single().Active, Is.False);
            Assert.That(set.VillageProfiles.Values.Single().Active, Is.False);
        }

        [Test]
        public void DictionariesCollectionsAndResultErrorsAreReadOnly()
        {
            var success = Build(StandardSources());
            var failure = Build(StandardSources().Where(source =>
                source.FileName != "event_activation_routes.csv"));

            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, SpecialMapDefinition>)success.DefinitionSet.SpecialMaps).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SpecialMapRewardDefinition>)success.DefinitionSet.SpecialMapRewards).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SpecialVillageDefinitionBuildError>)failure.Errors).Clear());
        }

        [Test]
        public void ForeignKeyIdsRemainUnresolvedStrings()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.That(set.EventActivationRoutes.Values.Single().SpecialMapId, Is.EqualTo("ID_2"));
            Assert.That(set.SpecialMaps.Values.Single().PrimaryBiomeId, Is.EqualTo("ID_4"));
            Assert.That(set.ShopInventoryRules.Single().SpawnPoolId, Is.EqualTo("ID_3"));
            Assert.That(set.VillageProfiles.Values.Single().WorldProfileId, Is.EqualTo("ID_3"));
        }

        [Test]
        public void StartDistanceBucketsRemainOneExactString()
        {
            var sources = StandardSources();
            var spec = Spec("village_profiles.csv");
            var row = StandardRow(spec);
            row[8] = "2-3:20|4-6:50|7-10:30";
            Replace(sources, BuildSource(spec, new[] { row }));

            var definition = Build(sources).DefinitionSet.VillageProfiles.Values.Single();

            Assert.That(definition.StartDistanceBuckets,
                Is.EqualTo("2-3:20|4-6:50|7-10:30"));
        }

        [Test]
        public void DomainInvalidButTypedValuesAreNotRejected()
        {
            var sources = StandardSources();
            var mapSpec = Spec("special_map_catalog.csv");
            var mapRow = StandardRow(mapSpec);
            mapRow[4] = "-100";
            mapRow[5] = "-200";
            Replace(sources, BuildSource(mapSpec, new[] { mapRow }));
            var inventorySpec = Spec("shop_inventory_rules.csv");
            var inventoryRow = StandardRow(inventorySpec);
            inventoryRow[4] = "999";
            inventoryRow[5] = "-5";
            Replace(sources, BuildSource(inventorySpec, new[] { inventoryRow }));

            var result = Build(sources);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.DefinitionSet.SpecialMaps.Values.Single().FootprintWidthSectors,
                Is.EqualTo(-100));
            Assert.That(result.DefinitionSet.ShopInventoryRules.Single().QuantityMax,
                Is.EqualTo(-5));
        }

        [Test]
        public void ErrorsAccumulateAndSortDeterministically()
        {
            var sources = StandardSources();
            sources.RemoveAll(source =>
                source.FileName == "event_activation_routes.csv" ||
                source.FileName == "village_profiles.csv");
            sources.Add(BuildSource(new FileSpec(
                "z_unexpected.csv", 1, Column("id", "ID"))));
            sources.Add(sources.Single(source => source.FileName == "special_map_catalog.csv"));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Select(error => error.FileName),
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Does.Contain(SpecialVillageDefinitionBuildErrorCode.MissingSource));
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Does.Contain(SpecialVillageDefinitionBuildErrorCode.DuplicateSource));
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Does.Contain(SpecialVillageDefinitionBuildErrorCode.UnexpectedSource));
        }

        [Test]
        public void BuildDoesNotModifyInputsOrPreviousDefinitionSets()
        {
            var sources = StandardSources();
            var sourceRefs = sources.ToArray();
            var columns = sources.Select(source => source.Schema.Columns.ToArray()).ToArray();
            var records = sources.Select(source => source.ParseResult.Records.ToArray()).ToArray();
            var world = new WorldRouteDefinitionBuilder().Build(Array.Empty<WorldRouteDefinitionSource>());
            var biome = new BiomeBoundaryDefinitionBuilder().Build(Array.Empty<BiomeBoundaryDefinitionSource>());

            var result = Build(sources);

            Assert.That(result.Success, Is.True);
            Assert.That(world.Success, Is.False);
            Assert.That(biome.Success, Is.False);
            Assert.That(sources, Is.EqualTo(sourceRefs));
            for (var index = 0; index < sources.Count; index++)
            {
                Assert.That(sources[index].Schema.Columns, Is.EqualTo(columns[index]));
                Assert.That(sources[index].ParseResult.Records, Is.EqualTo(records[index]));
            }
        }

        [Test]
        public void HeaderOnlySourcesProduceSuccessfulEmptySet()
        {
            var sources = Specs.Select(spec =>
                BuildSource(spec, Array.Empty<string[]>())).ToArray();

            var result = Build(sources);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(TotalCount(result.DefinitionSet), Is.Zero);
        }

        private static List<SpecialVillageDefinitionSource> CompositeSources()
        {
            var sources = StandardSources();

            ReplaceComposite(sources, "special_map_entry_sockets.csv", rows =>
            {
                rows[0][0] = "PARENT"; rows[0][1] = "Z_SOCKET";
                rows[1][0] = "PARENT"; rows[1][1] = "A_SOCKET";
            });
            ReplaceComposite(sources, "special_map_footprint_cells.csv", rows =>
            {
                rows[0][0] = "PARENT"; rows[0][1] = "2"; rows[0][2] = "0";
                rows[1][0] = "PARENT"; rows[1][1] = "1"; rows[1][2] = "0";
            });
            ReplaceComposite(sources, "special_map_rewards.csv", rows =>
            {
                rows[0][0] = "PARENT"; rows[0][1] = "2";
                rows[1][0] = "PARENT"; rows[1][1] = "1";
            });
            ReplaceComposite(sources, "shop_inventory_rules.csv", rows =>
            {
                rows[0][0] = "PARENT"; rows[0][1] = "2";
                rows[1][0] = "PARENT"; rows[1][1] = "1";
            });
            ReplaceComposite(sources, "village_layout_cells.csv", rows =>
            {
                rows[0][0] = "PARENT"; rows[0][1] = "2"; rows[0][2] = "0";
                rows[1][0] = "PARENT"; rows[1][1] = "1"; rows[1][2] = "0";
            });

            return sources;
        }

        private static void ReplaceComposite(
            List<SpecialVillageDefinitionSource> sources,
            string fileName,
            Action<string[][]> configure)
        {
            var spec = Spec(fileName);
            var rows = new[] { StandardRow(spec), StandardRow(spec) };
            configure(rows);
            Replace(sources, BuildSource(spec, rows));
        }

        private static IEnumerable<string> DictionaryFileNames()
        {
            return new[]
            {
                "event_activation_routes.csv",
                "special_map_catalog.csv",
                "shop_archetypes.csv",
                "shopkeeper_species.csv",
                "village_facilities.csv",
                "village_layout_catalog.csv",
                "village_profiles.csv"
            };
        }

        private static List<SpecialVillageDefinitionSource> StandardSources()
        {
            return Specs.Select(spec => BuildSource(spec)).ToList();
        }

        private static SpecialVillageDefinitionBuildResult Build(
            IEnumerable<SpecialVillageDefinitionSource> sources)
        {
            return new SpecialVillageDefinitionBuilder().Build(sources);
        }

        private static SpecialVillageDefinitionSource BuildSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows = null,
            bool expectSuccess = true)
        {
            var schemaRows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName,
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                column.DataType,
                index < spec.PrimaryKeyCount ? "1" : "0",
                index < spec.PrimaryKeyCount
                    ? (index + 1).ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
                column.DefaultValue,
                column.AllowedValues,
                string.Empty,
                string.Empty,
                index + 2));
            var catalog = new CsvSchemaCatalogBuilder().Build(schemaRows);
            Assert.That(catalog.Success, Is.True, string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var sourceRows = rows ?? new[] { StandardRow(spec) };
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in sourceRows)
            {
                csv += "\n" + string.Join(",", row.Select(CsvCell));
            }

            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            Assert.That(validation.Success, Is.True, string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            Assert.That(keys.Success, Is.True);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            Assert.That(parsed.Success, Is.EqualTo(expectSuccess), string.Join("\n", parsed.Errors));
            return new SpecialVillageDefinitionSource(schema, parsed);
        }

        private static string[] StandardRow(FileSpec spec)
        {
            return spec.Columns.Select((column, index) => Value(column.DataType, index)).ToArray();
        }

        private static string Value(string dataType, int index)
        {
            switch (dataType)
            {
                case "STRING": return "TEXT_" + (index + 1);
                case "ID": return "ID_" + (index + 1);
                case "INT": return (index + 1).ToString(CultureInfo.InvariantCulture);
                case "FLOAT": return "0.25";
                case "BOOL": return "0";
                case "ENUM": return "ENUM_A";
                case "ID_LIST": return "LIST_A|LIST_B|LIST_A";
                case "ENUM_LIST": return "ENUM_A|ENUM_B|ENUM_A";
                case "INT_LIST": return "1|2|1";
                default: throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        private static void AssertFullMapping(string fileName, SpecialVillageDefinitionSet set)
        {
            var spec = Spec(fileName);
            var definition = DefinitionFor(fileName, set);
            var sourceProperty = definition.GetType().GetProperty("SourceRecord");
            Assert.That(sourceProperty, Is.Not.Null);
            var record = (CsvParsedRecord)sourceProperty.GetValue(definition);
            Assert.That(record, Is.Not.Null);

            for (var index = 0; index < spec.Columns.Count; index++)
            {
                var propertyName = ToPascalCase(spec.Columns[index].Name);
                var property = definition.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.That(property, Is.Not.Null,
                    fileName + " missing property " + propertyName);
                Assert.That(property.GetValue(definition),
                    Is.EqualTo(ParsedValue(record, index, spec.Columns[index].DataType)),
                    fileName + "." + spec.Columns[index].Name);
            }

            Assert.That(sourceProperty.GetValue(definition), Is.SameAs(record));
        }

        private static object DefinitionFor(string fileName, SpecialVillageDefinitionSet set)
        {
            switch (fileName)
            {
                case "event_activation_routes.csv": return set.EventActivationRoutes.Values.Single();
                case "special_map_catalog.csv": return set.SpecialMaps.Values.Single();
                case "special_map_entry_sockets.csv": return set.SpecialMapEntrySockets.Single();
                case "special_map_footprint_cells.csv": return set.SpecialMapFootprintCells.Single();
                case "special_map_rewards.csv": return set.SpecialMapRewards.Single();
                case "shop_archetypes.csv": return set.ShopArchetypes.Values.Single();
                case "shop_inventory_rules.csv": return set.ShopInventoryRules.Single();
                case "shopkeeper_species.csv": return set.ShopkeeperSpecies.Values.Single();
                case "village_facilities.csv": return set.VillageFacilities.Values.Single();
                case "village_layout_catalog.csv": return set.VillageLayouts.Values.Single();
                case "village_layout_cells.csv": return set.VillageLayoutCells.Single();
                case "village_profiles.csv": return set.VillageProfiles.Values.Single();
                default: throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
            }
        }

        private static object ParsedValue(CsvParsedRecord record, int index, string dataType)
        {
            var value = record.Fields[index].Value;
            switch (dataType)
            {
                case "STRING":
                case "ID":
                case "ENUM": return value.StringValue;
                case "INT": return value.IntValue;
                case "FLOAT": return value.FloatValue;
                case "BOOL": return value.BoolValue;
                case "ID_LIST":
                case "ENUM_LIST": return value.StringListValue;
                case "INT_LIST": return value.IntListValue;
                default: throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        private static string ToPascalCase(string value)
        {
            return string.Concat(value.Split('_').Select(part =>
                char.ToUpperInvariant(part[0]) + part.Substring(1)));
        }

        private static int TotalCount(SpecialVillageDefinitionSet set)
        {
            return set.EventActivationRoutes.Count + set.SpecialMaps.Count +
                   set.SpecialMapEntrySockets.Count + set.SpecialMapFootprintCells.Count +
                   set.SpecialMapRewards.Count + set.ShopArchetypes.Count +
                   set.ShopInventoryRules.Count + set.ShopkeeperSpecies.Count +
                   set.VillageFacilities.Count + set.VillageLayouts.Count +
                   set.VillageLayoutCells.Count + set.VillageProfiles.Count;
        }

        private static string Snapshot(SpecialVillageDefinitionSet set)
        {
            return string.Join("|", new[]
            {
                string.Join(",", set.EventActivationRoutes.Keys),
                string.Join(",", set.SpecialMaps.Keys),
                string.Join(",", set.SpecialMapEntrySockets.Select(item => item.SpecialMapId + ":" + item.EntrySocketId)),
                string.Join(",", set.SpecialMapFootprintCells.Select(item => item.SpecialMapId + ":" + item.LocalSectorX + ":" + item.LocalSectorY)),
                string.Join(",", set.SpecialMapRewards.Select(item => item.SpecialMapId + ":" + item.RewardOrder)),
                string.Join(",", set.ShopArchetypes.Keys),
                string.Join(",", set.ShopInventoryRules.Select(item => item.ShopArchetypeId + ":" + item.SlotIndex)),
                string.Join(",", set.ShopkeeperSpecies.Keys),
                string.Join(",", set.VillageFacilities.Keys),
                string.Join(",", set.VillageLayouts.Keys),
                string.Join(",", set.VillageLayoutCells.Select(item => item.VillageLayoutId + ":" + item.LocalChunkX + ":" + item.LocalChunkY)),
                string.Join(",", set.VillageProfiles.Keys)
            });
        }

        private static FileSpec Spec(string fileName) =>
            Specs.Single(spec => spec.FileName == fileName);

        private static void Replace(
            List<SpecialVillageDefinitionSource> sources,
            SpecialVillageDefinitionSource replacement)
        {
            sources.RemoveAll(source => source.FileName == replacement.FileName);
            sources.Add(replacement);
        }

        private static string CsvCell(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string FormatErrors(SpecialVillageDefinitionBuildResult result)
        {
            return string.Join("\n", result.Errors.Select(error =>
                error.FileName + " " + error.ErrorCode + " " + error.Message));
        }

        private static FileSpec[] CreateSpecs()
        {
            return new[]
            {
                File("event_activation_routes.csv", 1, "event_route_id:ID", "special_map_id:ID", "event_id:ID", "mandatory:BOOL", "allowed_sector_types:INT_LIST", "requires_tool:BOOL", "requires_consumable:BOOL", "min_safe_tiles_before_trigger:INT", "return_path_required:BOOL", "trigger_slot_id:ID", "notes:STRING"),
                File("special_map_catalog.csv", 1, "special_map_id:ID", "display_name_ko:STRING", "site_role:ENUM", "primary_biome_id:ID", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "required_count:INT", "min_graph_distance_from_start:INT", "min_graph_distance_to_other_core_sites:INT", "allowed_entry_route_types:INT_LIST", "requires_tool:BOOL", "mandatory_reward_id:ID", "generation_mode:ENUM", "active:BOOL", "notes:STRING"),
                File("special_map_entry_sockets.csv", 2, "special_map_id:ID", "entry_socket_id:ID", "local_sector_x:INT", "local_sector_y:INT", "side:ENUM", "allowed_route_types:INT_LIST", "required:BOOL", "return_path_required:BOOL", "notes:STRING"),
                File("special_map_footprint_cells.csv", 3, "special_map_id:ID", "local_sector_x:INT", "local_sector_y:INT", "local_role:ENUM", "required_primary_biome_id:ID", "fixed_sector_recipe_id:ID", "required_open_sides:ENUM_LIST", "notes:STRING"),
                File("special_map_rewards.csv", 2, "special_map_id:ID", "reward_order:INT", "reward_id:ID", "reward_kind:ENUM", "mandatory:BOOL", "slot_id:ID", "quantity_min:INT", "quantity_max:INT", "notes:STRING"),
                File("shop_archetypes.csv", 1, "shop_archetype_id:ID", "display_name_ko:STRING", "shop_type:ENUM", "item_slot_count_min:INT", "item_slot_count_max:INT", "base_price_multiplier:FLOAT", "allows_reputation_reward:BOOL", "active:BOOL", "notes:STRING"),
                File("shop_inventory_rules.csv", 2, "shop_archetype_id:ID", "slot_index:INT", "spawn_pool_id:ID", "guaranteed:BOOL", "quantity_min:INT", "quantity_max:INT", "price_min_gold:INT", "price_max_gold:INT", "required_favor_tier:INT", "active:BOOL", "notes:STRING"),
                File("shopkeeper_species.csv", 1, "species_id:ID", "display_name_ko:STRING", "prefab_id:ID", "dialogue_style_id:ID", "animation_set_id:ID", "selection_weight:INT", "allowed_biome_ids:ID_LIST", "active:BOOL", "notes:STRING"),
                File("village_facilities.csv", 1, "facility_id:ID", "display_name_ko:STRING", "facility_group:ENUM", "fixed:BOOL", "selection_weight:INT", "prefab_id:ID", "shop_archetype_id:ID", "evacuated_prefab_id:ID", "active:BOOL", "notes:STRING"),
                File("village_layout_catalog.csv", 1, "village_layout_id:ID", "display_name_ko:STRING", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "target_facility_count:INT", "entry_sides:ENUM_LIST", "selection_weight:INT", "active:BOOL", "notes:STRING"),
                File("village_layout_cells.csv", 3, "village_layout_id:ID", "local_chunk_x:INT", "local_chunk_y:INT", "cell_role:ENUM", "facility_slot_id:ID", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_entry_side:ENUM", "notes:STRING"),
                File("village_profiles.csv", 1, "village_profile_id:ID", "display_name_ko:STRING", "world_profile_id:ID", "facility_count_min:INT", "facility_count_max:INT", "fixed_facility_ids:ID_LIST", "optional_facility_ids:ID_LIST", "allowed_layout_ids:ID_LIST", "start_distance_buckets:STRING", "maximum_sector_count:INT", "active:BOOL", "notes:STRING")
            };
        }

        private static FileSpec File(
            string fileName,
            int primaryKeyCount,
            params string[] definitions)
        {
            return new FileSpec(fileName, primaryKeyCount, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                return Column(parts[0], parts[1]);
            }).ToArray());
        }

        private static ColumnSpec Column(
            string name,
            string dataType,
            string defaultValue = "")
        {
            var allowed = dataType == "ENUM" || dataType == "ENUM_LIST"
                ? "ENUM_A|ENUM_B"
                : string.Empty;
            return new ColumnSpec(name, dataType, defaultValue, allowed);
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, int primaryKeyCount, params ColumnSpec[] columns)
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
            public ColumnSpec(
                string name,
                string dataType,
                string defaultValue,
                string allowedValues)
            {
                Name = name;
                DataType = dataType;
                DefaultValue = defaultValue;
                AllowedValues = allowedValues;
            }

            public string Name { get; }
            public string DataType { get; }
            public string DefaultValue { get; }
            public string AllowedValues { get; }
        }
    }
}
