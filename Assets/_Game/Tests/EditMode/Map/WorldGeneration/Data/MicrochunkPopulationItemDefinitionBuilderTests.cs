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
    public sealed class MicrochunkPopulationItemDefinitionBuilderTests
    {
        private static readonly FileSpec[] Specs = CreateSpecs();
        private static IEnumerable<string> FileNames => Specs.Select(spec => spec.FileName);
        private static IEnumerable<int> CoreCases => Enumerable.Range(0, 32);
        private static IEnumerable<int> BatteryCases => Enumerable.Range(0, 18);

        [TestCaseSource(nameof(FileNames))]
        public void MissingSourceIsReported(string fileName)
        {
            var result = Build(StandardSources().Where(source => source.FileName != fileName));

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error =>
                error.FileName == fileName &&
                error.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.MissingSource), Is.True);
        }

        [TestCaseSource(nameof(FileNames))]
        public void EveryDefinitionMapsAllColumnsAndSourceRecord(string fileName)
        {
            var result = Build(StandardSources());

            Assert.That(result.Success, Is.True, FormatErrors(result));
            AssertFullMapping(fileName, result.DefinitionSet);
        }

        [TestCaseSource(nameof(CoreCases))]
        public void CoreContractCase(int caseIndex)
        {
            switch (caseIndex)
            {
                case 0: ExactSixteenSourcesBuildSuccessfully(); break;
                case 1: ShuffledSourceOrderProducesSameSnapshot(); break;
                case 2: DuplicateSourceFailsWithoutPartialSet(); break;
                case 3: UnexpectedSourceFailsWithoutPartialSet(); break;
                case 4: UnsuccessfulParseIsRejected(); break;
                case 5: SchemaColumnTypeMismatchIsRejected(); break;
                case 6: ParsedFieldsFromEquivalentOtherSchemaAreRejected(); break;
                case 7: NullEnumerableThrows(); break;
                case 8: NullSourceElementIsDeterministicFailure(); break;
                case 9: SourceRejectsNullArguments(); break;
                case 10: NineDictionariesUseOrdinalOrder(); break;
                case 11: SevenCompositeCollectionsAreStable(); break;
                case 12: ParentQueriesAreStableAndReadOnly(); break;
                case 13: NestedListsPreserveOrderDuplicatesAndAreReadOnly(); break;
                case 14: OptionalEmptyValuesArePreserved(); break;
                case 15: DefaultValueAndUsedDefaultArePreserved(); break;
                case 16: InactiveRowsAreRetained(); break;
                case 17: PublishedCollectionsAndErrorsAreReadOnly(); break;
                case 18: ForeignKeyIdsRemainUnresolvedStrings(); break;
                case 19: VariantReplacementPairsRemainOpaque(); break;
                case 20: DomainInvalidTypedValuesAreNotRejected(); break;
                case 21: ErrorsAccumulateAndSortDeterministically(); break;
                case 22: BuildDoesNotModifyInputs(); break;
                case 23: HeaderOnlySourcesProduceEmptySet(); break;
                case 24: ErrorCodeInventoryIsExact(); break;
                case 25: SchemaColumnNameMismatchIsRejected(); break;
                case 26: SchemaColumnOrderMismatchIsRejected(); break;
                case 27: DictionaryLookupIsCaseSensitive(); break;
                case 28: UnknownAndNullParentQueriesFollowContract(); break;
                case 29: CompositeInputShuffleDoesNotChangeSnapshot(); break;
                case 30: ActiveFilteringIsNotPerformed(); break;
                case 31: EveryDefinitionRetainsExactParsedRecord(); break;
                default: throw new ArgumentOutOfRangeException(nameof(caseIndex));
            }
        }

        [TestCase("map_element_definitions.csv", "interaction_tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("map_element_definitions.csv", "forbidden_near_tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("map_element_interactions.csv", "target_tag", "ID", "ENUM")]
        [TestCase("microchunk_catalog.csv", "route_roles", "ID_LIST", "ENUM_LIST")]
        [TestCase("microchunk_pool_entries.csv", "required_tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("microchunk_pool_entries.csv", "forbidden_tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("microchunk_sockets.csv", "tool_requirement", "ENUM", "ID")]
        [TestCase("microchunk_variant_rules.csv", "required_world_tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("microchunk_variant_rules.csv", "forbidden_world_tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("spawn_pool_entries.csv", "required_tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("spawn_pool_entries.csv", "forbidden_tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("tile_code_dictionary.csv", "semantic", "STRING", "ENUM")]
        [TestCase("tile_code_dictionary.csv", "runtime_tag", "ID", "STRING")]
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
                error.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.SchemaMismatch &&
                error.ColumnOrder == columnIndex + 1 &&
                error.ColumnName == columnName), Is.True);
        }

        [TestCaseSource(nameof(BatteryCases))]
        public void BatteryTypedRegistryRepairContract(int caseIndex)
        {
            var spec = Spec("battery_profiles.csv");
            switch (caseIndex)
            {
                case 0:
                    Assert.That(MicrochunkPopulationItemDefinitionSource.ExpectedFileNames,
                        Is.EqualTo(Specs.Select(item => item.FileName)));
                    break;
                case 1:
                {
                    var sources = StandardSources();
                    var result = Build(sources);
                    Assert.That(result.Success, Is.True, FormatErrors(result));
                    Assert.That(result.DefinitionSet.BatteryProfiles.Values.Single().SourceRecord,
                        Is.SameAs(sources.Single(item => item.FileName == spec.FileName).ParseResult.Records.Single()));
                    break;
                }
                case 2:
                {
                    var battery = Build(StandardSources()).DefinitionSet.BatteryProfiles.Values.Single();
                    Assert.That(battery.BatteryId, Is.EqualTo("ID_1"));
                    Assert.That(battery.DeliveryMode, Is.EqualTo("PLACE"));
                    Assert.That(battery.FuseSeconds, Is.EqualTo(0.25f));
                    Assert.That(battery.PrefabId, Is.EqualTo("ID_15"));
                    break;
                }
                case 3:
                {
                    var sources = StandardSources();
                    var first = StandardRow(spec);
                    var second = StandardRow(spec);
                    first[0] = "Z_BATTERY";
                    second[0] = "A_BATTERY";
                    Replace(sources, BuildSource(spec, new[] { first, second }));
                    var dictionary = Build(sources).DefinitionSet.BatteryProfiles;
                    Assert.That(dictionary.Keys, Is.EqualTo(new[] { "A_BATTERY", "Z_BATTERY" }));
                    Assert.That(dictionary.ContainsKey("a_battery"), Is.False);
                    Assert.Throws<NotSupportedException>(() =>
                        ((IDictionary<string, BatteryProfileDefinition>)dictionary).Clear());
                    break;
                }
                case 4: AssertBatterySchemaMismatch(WithBatteryColumn("display_name_ko", item => item.With(isRequired: false))); break;
                case 5: AssertBatterySchemaMismatch(WithBatteryPrimaryKeyOnDisplayName()); break;
                case 6: AssertBatterySchemaMismatch(WithBatteryColumn("active", item => item.With(defaultValue: "0"))); break;
                case 7: AssertBatterySchemaMismatch(WithBatteryColumn("delivery_mode", item => item.With(allowedValues: "PLACE|THROW"))); break;
                case 8: AssertBatterySchemaMismatch(WithBatteryColumn("prefab_id", item => item.With(foreignKey: "prefab_registry.csv.asset_address"))); break;
                case 9:
                {
                    var result = Build(StandardSources().Where(item => item.FileName != spec.FileName));
                    Assert.That(result.Errors.Any(item => item.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.MissingSource), Is.True);
                    break;
                }
                case 10:
                {
                    var sources = StandardSources();
                    sources.Add(sources.Single(item => item.FileName == spec.FileName));
                    Assert.That(Build(sources).Errors.Any(item => item.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.DuplicateSource), Is.True);
                    break;
                }
                case 11:
                {
                    var sources = StandardSources();
                    sources.Add(BuildSource(new FileSpec("battery_profile.csv", 1, Column("battery_id", "ID"))));
                    Assert.That(Build(sources).Errors.Any(item => item.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.UnexpectedSource), Is.True);
                    break;
                }
                case 12:
                {
                    var sources = StandardSources();
                    var row = StandardRow(spec);
                    row[2] = "not-an-int";
                    Replace(sources, BuildSource(spec, new[] { row }, false));
                    Assert.That(Build(sources).Errors.Any(item => item.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.UnsuccessfulParse), Is.True);
                    break;
                }
                case 13:
                {
                    var forward = Build(StandardSources()).DefinitionSet;
                    var reverse = Build(StandardSources().AsEnumerable().Reverse()).DefinitionSet;
                    Assert.That(reverse.BatteryProfiles.Keys, Is.EqualTo(forward.BatteryProfiles.Keys));
                    break;
                }
                case 14:
                {
                    var sources = StandardSources();
                    var row = StandardRow(spec);
                    row[15] = string.Empty;
                    Replace(sources, BuildSource(spec, new[] { row }));
                    var battery = Build(sources).DefinitionSet.BatteryProfiles.Values.Single();
                    Assert.That(battery.Active, Is.True);
                    Assert.That(battery.SourceRecord.Fields[15].UsedDefault, Is.True);
                    break;
                }
                case 15:
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<string>)MicrochunkPopulationItemDefinitionSource.ExpectedFileNames).Clear());
                    break;
                case 16:
                    Assert.That(spec.Columns.Single(item => item.Name == "delivery_mode").AllowedValues,
                        Is.EqualTo("PLACE|THROW|BLAST_CONE"));
                    break;
                case 17:
                    AssertBatterySchemaMismatch(WithBatteryColumn("fuel_cost", item => item.With(dataType: "FLOAT")));
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(caseIndex));
            }
        }

        private static void AssertBatterySchemaMismatch(FileSpec batterySpec)
        {
            var sources = StandardSources();
            Replace(sources, BuildSource(batterySpec));
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(item =>
                item.FileName == "battery_profiles.csv" &&
                item.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.SchemaMismatch), Is.True);
        }

        private static FileSpec WithBatteryColumn(string columnName, Func<ColumnSpec, ColumnSpec> change)
        {
            var spec = Spec("battery_profiles.csv");
            var columns = spec.Columns.ToArray();
            var index = Array.FindIndex(columns, item => item.Name == columnName);
            columns[index] = change(columns[index]);
            return new FileSpec(spec.FileName, spec.PrimaryKeyCount, columns);
        }

        private static FileSpec WithBatteryPrimaryKeyOnDisplayName()
        {
            var spec = Spec("battery_profiles.csv");
            var columns = spec.Columns.ToArray();
            var batteryId = Array.FindIndex(columns, item => item.Name == "battery_id");
            var displayName = Array.FindIndex(columns, item => item.Name == "display_name_ko");
            columns[batteryId] = columns[batteryId].With(primaryKeyOrder: null);
            columns[displayName] = columns[displayName].With(primaryKeyOrder: 1);
            return new FileSpec(spec.FileName, spec.PrimaryKeyCount, columns);
        }

        private static void ExactSixteenSourcesBuildSuccessfully()
        {
            var result = Build(StandardSources());
            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(TotalCount(result.DefinitionSet), Is.EqualTo(17));
        }

        private static void ShuffledSourceOrderProducesSameSnapshot()
        {
            var forward = Build(StandardSources()).DefinitionSet;
            var reverse = Build(StandardSources().AsEnumerable().Reverse()).DefinitionSet;
            Assert.That(Snapshot(reverse), Is.EqualTo(Snapshot(forward)));
        }

        private static void DuplicateSourceFailsWithoutPartialSet()
        {
            var sources = StandardSources();
            sources.Add(sources[0]);
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single().ErrorCode,
                Is.EqualTo(MicrochunkPopulationItemDefinitionBuildErrorCode.DuplicateSource));
        }

        private static void UnexpectedSourceFailsWithoutPartialSet()
        {
            var sources = StandardSources();
            sources.Add(BuildSource(new FileSpec("unexpected.csv", 1, Column("id", "ID"))));
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single(error => error.FileName == "unexpected.csv").ErrorCode,
                Is.EqualTo(MicrochunkPopulationItemDefinitionBuildErrorCode.UnexpectedSource));
        }

        private static void UnsuccessfulParseIsRejected()
        {
            var sources = StandardSources();
            var spec = Spec("map_element_definitions.csv");
            var row = StandardRow(spec);
            row[4] = "bad-int";
            Replace(sources, BuildSource(spec, new[] { row }, false));
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single().ErrorCode,
                Is.EqualTo(MicrochunkPopulationItemDefinitionBuildErrorCode.UnsuccessfulParse));
        }

        private static void SchemaColumnTypeMismatchIsRejected()
        {
            var sources = StandardSources();
            var spec = Spec("microchunk_catalog.csv");
            var columns = spec.Columns.ToArray();
            columns[2] = Column("width_tiles", "STRING");
            Replace(sources, BuildSource(new FileSpec(spec.FileName, spec.PrimaryKeyCount, columns)));
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error =>
                error.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.SchemaMismatch &&
                error.ColumnOrder == 3), Is.True);
        }

        private static void ParsedFieldsFromEquivalentOtherSchemaAreRejected()
        {
            var sources = StandardSources();
            var original = sources.Single(source => source.FileName == "map_element_definitions.csv");
            var other = BuildSource(Spec("map_element_definitions.csv"));
            Replace(sources, new MicrochunkPopulationItemDefinitionSource(original.Schema, other.ParseResult));
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.All(error =>
                error.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.FieldMappingFailed), Is.True);
            Assert.That(result.Errors.First().Location, Is.Not.Null);
        }

        private static void NullEnumerableThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MicrochunkPopulationItemDefinitionBuilder().Build(null));
        }

        private static void NullSourceElementIsDeterministicFailure()
        {
            var sources = StandardSources();
            sources.Add(null);
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.First().FileName, Is.Empty);
            Assert.That(result.Errors.First().ErrorCode,
                Is.EqualTo(MicrochunkPopulationItemDefinitionBuildErrorCode.MissingSource));
        }

        private static void SourceRejectsNullArguments()
        {
            var source = BuildSource(Spec("map_element_definitions.csv"));
            Assert.Throws<ArgumentNullException>(() =>
                new MicrochunkPopulationItemDefinitionSource(null, source.ParseResult));
            Assert.Throws<ArgumentNullException>(() =>
                new MicrochunkPopulationItemDefinitionSource(source.Schema, null));
        }

        private static void NineDictionariesUseOrdinalOrder()
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
            Assert.That(set.BatteryProfiles.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.MapElements.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.Microchunks.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.MicrochunkVariantRules.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.PopulationProfiles.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.Prefabs.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.Resources.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.ResourceSpawnRules.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.SpecialItemSlots.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.TileCodes.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
        }

        private static void SevenCompositeCollectionsAreStable()
        {
            var set = Build(CompositeSources()).DefinitionSet;
            Assert.That(set.MapElementInteractions.Select(item => item.TargetTag), Is.EqualTo(new[] { "ENUM_A", "ENUM_B" }));
            Assert.That(set.MicrochunkObjectSlots.Select(item => item.SlotId), Is.EqualTo(new[] { "A", "Z" }));
            Assert.That(set.MicrochunkPoolEntries.Select(item => item.EntryOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.MicrochunkSockets.Select(item => item.SocketId), Is.EqualTo(new[] { "A", "Z" }));
            Assert.That(set.MicrochunkTileCells.Select(item => item.LocalX), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.SpawnPoolEntries.Select(item => item.EntryOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.ToolUpgrades.Select(item => item.UpgradeLevel), Is.EqualTo(new[] { 1, 2 }));
        }

        private static void ParentQueriesAreStableAndReadOnly()
        {
            var set = Build(CompositeSources()).DefinitionSet;
            Assert.That(set.GetMapElementInteractions("PARENT").Select(item => item.TargetTag), Is.EqualTo(new[] { "ENUM_A", "ENUM_B" }));
            Assert.That(set.GetMicrochunkObjectSlots("PARENT").Select(item => item.SlotId), Is.EqualTo(new[] { "A", "Z" }));
            Assert.That(set.GetMicrochunkPoolEntries("PARENT").Select(item => item.EntryOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.GetMicrochunkSockets("PARENT").Select(item => item.SocketId), Is.EqualTo(new[] { "A", "Z" }));
            Assert.That(set.GetMicrochunkTileCells("PARENT").Select(item => item.LocalX), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.GetSpawnPoolEntries("PARENT").Select(item => item.EntryOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(set.GetToolUpgrades("PARENT").Select(item => item.UpgradeLevel), Is.EqualTo(new[] { 1, 2 }));
            Assert.Throws<NotSupportedException>(() => ((IList<MicrochunkObjectSlotDefinition>)set.GetMicrochunkObjectSlots("PARENT")).Clear());
        }

        private static void NestedListsPreserveOrderDuplicatesAndAreReadOnly()
        {
            var set = Build(StandardSources()).DefinitionSet;
            Assert.That(set.MapElements.Values.Single().InteractionTags, Is.EqualTo(new[] { "LIST_A", "LIST_B", "LIST_A" }));
            Assert.That(set.Microchunks.Values.Single().BiomeIds, Is.EqualTo(new[] { "LIST_A", "LIST_B", "LIST_A" }));
            Assert.That(set.ResourceSpawnRules.Values.Single().SectorRouteTypes, Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)set.MapElements.Values.Single().InteractionTags).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<int>)set.ResourceSpawnRules.Values.Single().SectorRouteTypes).Add(3));
        }

        private static void OptionalEmptyValuesArePreserved()
        {
            var sources = StandardSources();
            var prefabSpec = Spec("prefab_registry.csv");
            var prefabRow = StandardRow(prefabSpec);
            prefabRow[3] = string.Empty;
            prefabRow[6] = string.Empty;
            Replace(sources, BuildSource(prefabSpec, new[] { prefabRow }));
            var ruleSpec = Spec("resource_spawn_rules.csv");
            var ruleRow = StandardRow(ruleSpec);
            ruleRow[12] = string.Empty;
            ruleRow[14] = string.Empty;
            Replace(sources, BuildSource(ruleSpec, new[] { ruleRow }));
            var set = Build(sources).DefinitionSet;
            Assert.That(set.Prefabs.Values.Single().ExpectedComponent, Is.Empty);
            Assert.That(set.Prefabs.Values.Single().Notes, Is.Empty);
            Assert.That(set.ResourceSpawnRules.Values.Single().MandatorySiteId, Is.Empty);
            Assert.That(set.ResourceSpawnRules.Values.Single().Notes, Is.Empty);
        }

        private static void DefaultValueAndUsedDefaultArePreserved()
        {
            var sources = StandardSources();
            var spec = Spec("map_element_definitions.csv");
            var columns = spec.Columns.ToArray();
            columns[10] = Column("telegraph_seconds", "FLOAT", "+1.25");
            var defaultSpec = new FileSpec(spec.FileName, spec.PrimaryKeyCount, columns);
            var row = StandardRow(defaultSpec);
            row[10] = string.Empty;
            Replace(sources, BuildSource(defaultSpec, new[] { row }));
            var definition = Build(sources).DefinitionSet.MapElements.Values.Single();
            Assert.That(definition.TelegraphSeconds, Is.EqualTo(1.25f));
            Assert.That(definition.SourceRecord.Fields[10].UsedDefault, Is.True);
        }

        private static void InactiveRowsAreRetained()
        {
            var set = Build(StandardSources()).DefinitionSet;
            Assert.That(set.MapElements.Values.Single().Active, Is.False);
            Assert.That(set.Microchunks.Values.Single().Active, Is.False);
            Assert.That(set.MicrochunkPoolEntries.Single().Active, Is.False);
            Assert.That(set.PopulationProfiles.Values.Single().Active, Is.False);
            Assert.That(set.Prefabs.Values.Single().Active, Is.False);
            Assert.That(set.Resources.Values.Single().Active, Is.False);
            Assert.That(set.ResourceSpawnRules.Values.Single().Active, Is.False);
            Assert.That(set.SpawnPoolEntries.Single().Active, Is.False);
            Assert.That(set.SpecialItemSlots.Values.Single().Active, Is.False);
            Assert.That(set.TileCodes.Values.Single().Active, Is.False);
            Assert.That(set.BatteryProfiles.Values.Single().Active, Is.False);
        }

        private static void PublishedCollectionsAndErrorsAreReadOnly()
        {
            var success = Build(StandardSources());
            var failure = Build(StandardSources().Where(source => source.FileName != "map_element_definitions.csv"));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, MapElementDefinition>)success.DefinitionSet.MapElements).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<MicrochunkSocketDefinition>)success.DefinitionSet.MicrochunkSockets).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<MicrochunkPopulationItemDefinitionBuildError>)failure.Errors).Clear());
        }

        private static void ForeignKeyIdsRemainUnresolvedStrings()
        {
            var set = Build(StandardSources()).DefinitionSet;
            Assert.That(set.MapElements.Values.Single().PrefabId, Is.EqualTo("ID_4"));
            Assert.That(set.Microchunks.Values.Single().PrefabId, Is.EqualTo("ID_14"));
            Assert.That(set.Resources.Values.Single().PickupPrefabId, Is.EqualTo("ID_7"));
            Assert.That(set.SpawnPoolEntries.Single().EntryId, Is.EqualTo("ID_4"));
            Assert.That(set.BatteryProfiles.Values.Single().PrefabId, Is.EqualTo("ID_15"));
        }

        private static void VariantReplacementPairsRemainOpaque()
        {
            var sources = StandardSources();
            var spec = Spec("microchunk_variant_rules.csv");
            var row = StandardRow(spec);
            row[6] = "slot_a:pool_x|slot_b:pool_y";
            Replace(sources, BuildSource(spec, new[] { row }));
            Assert.That(Build(sources).DefinitionSet.MicrochunkVariantRules.Values.Single().ReplaceSlotPoolPairs,
                Is.EqualTo("slot_a:pool_x|slot_b:pool_y"));
        }

        private static void DomainInvalidTypedValuesAreNotRejected()
        {
            var sources = StandardSources();
            var chunkSpec = Spec("microchunk_catalog.csv");
            var chunkRow = StandardRow(chunkSpec);
            chunkRow[2] = "-100";
            chunkRow[3] = "999";
            Replace(sources, BuildSource(chunkSpec, new[] { chunkRow }));
            var resourceSpec = Spec("resource_spawn_rules.csv");
            var resourceRow = StandardRow(resourceSpec);
            resourceRow[6] = "999";
            resourceRow[7] = "-5";
            Replace(sources, BuildSource(resourceSpec, new[] { resourceRow }));
            var result = Build(sources);
            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.DefinitionSet.Microchunks.Values.Single().WidthTiles, Is.EqualTo(-100));
            Assert.That(result.DefinitionSet.ResourceSpawnRules.Values.Single().WorldMax, Is.EqualTo(-5));
        }

        private static void ErrorsAccumulateAndSortDeterministically()
        {
            var sources = StandardSources();
            sources.RemoveAll(source => source.FileName == "map_element_definitions.csv" || source.FileName == "tool_upgrade_definitions.csv");
            sources.Add(BuildSource(new FileSpec("z_unexpected.csv", 1, Column("id", "ID"))));
            sources.Add(sources.Single(source => source.FileName == "microchunk_catalog.csv"));
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Select(error => error.FileName), Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(MicrochunkPopulationItemDefinitionBuildErrorCode.MissingSource));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(MicrochunkPopulationItemDefinitionBuildErrorCode.DuplicateSource));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(MicrochunkPopulationItemDefinitionBuildErrorCode.UnexpectedSource));
        }

        private static void BuildDoesNotModifyInputs()
        {
            var sources = StandardSources();
            var sourceRefs = sources.ToArray();
            var columns = sources.Select(source => source.Schema.Columns.ToArray()).ToArray();
            var records = sources.Select(source => source.ParseResult.Records.ToArray()).ToArray();
            var result = Build(sources);
            Assert.That(result.Success, Is.True);
            Assert.That(sources, Is.EqualTo(sourceRefs));
            for (var index = 0; index < sources.Count; index++)
            {
                Assert.That(sources[index].Schema.Columns, Is.EqualTo(columns[index]));
                Assert.That(sources[index].ParseResult.Records, Is.EqualTo(records[index]));
            }
        }

        private static void HeaderOnlySourcesProduceEmptySet()
        {
            var result = Build(Specs.Select(spec => BuildSource(spec, Array.Empty<string[]>())));
            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(TotalCount(result.DefinitionSet), Is.Zero);
        }

        private static void ErrorCodeInventoryIsExact()
        {
            Assert.That(Enum.GetNames(typeof(MicrochunkPopulationItemDefinitionBuildErrorCode)), Is.EqualTo(new[]
            {
                "MissingSource", "UnexpectedSource", "DuplicateSource", "UnsuccessfulParse", "SchemaMismatch", "FieldMappingFailed"
            }));
        }

        private static void SchemaColumnNameMismatchIsRejected()
        {
            var sources = StandardSources();
            var spec = Spec("prefab_registry.csv");
            var columns = spec.Columns.ToArray();
            columns[1] = Column("wrong_asset_address", columns[1].DataType);
            Replace(sources, BuildSource(new FileSpec(spec.FileName, spec.PrimaryKeyCount, columns)));
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error => error.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.SchemaMismatch), Is.True);
        }

        private static void SchemaColumnOrderMismatchIsRejected()
        {
            var sources = StandardSources();
            var spec = Spec("resource_definitions.csv");
            var columns = spec.Columns.ToArray();
            var temp = columns[1];
            columns[1] = columns[2];
            columns[2] = temp;
            Replace(sources, BuildSource(new FileSpec(spec.FileName, spec.PrimaryKeyCount, columns)));
            var result = Build(sources);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Count(error => error.ErrorCode == MicrochunkPopulationItemDefinitionBuildErrorCode.SchemaMismatch), Is.GreaterThanOrEqualTo(2));
        }

        private static void DictionaryLookupIsCaseSensitive()
        {
            var set = Build(StandardSources()).DefinitionSet;
            var key = set.MapElements.Keys.Single();
            Assert.That(set.MapElements.ContainsKey(key.ToLowerInvariant()), Is.False);
        }

        private static void UnknownAndNullParentQueriesFollowContract()
        {
            var set = Build(StandardSources()).DefinitionSet;
            Assert.That(set.GetMapElementInteractions("UNKNOWN"), Is.Empty);
            Assert.That(set.GetMicrochunkObjectSlots("UNKNOWN"), Is.Empty);
            Assert.That(set.GetMicrochunkPoolEntries("UNKNOWN"), Is.Empty);
            Assert.That(set.GetMicrochunkSockets("UNKNOWN"), Is.Empty);
            Assert.That(set.GetMicrochunkTileCells("UNKNOWN"), Is.Empty);
            Assert.That(set.GetSpawnPoolEntries("UNKNOWN"), Is.Empty);
            Assert.That(set.GetToolUpgrades("UNKNOWN"), Is.Empty);
            Assert.Throws<ArgumentNullException>(() => set.GetMicrochunkSockets(null));
        }

        private static void CompositeInputShuffleDoesNotChangeSnapshot()
        {
            var sources = CompositeSources();
            var forward = Build(sources).DefinitionSet;
            var reverse = Build(sources.AsEnumerable().Reverse()).DefinitionSet;
            Assert.That(Snapshot(reverse), Is.EqualTo(Snapshot(forward)));
        }

        private static void ActiveFilteringIsNotPerformed()
        {
            var set = Build(StandardSources()).DefinitionSet;
            Assert.That(TotalCount(set), Is.EqualTo(17));
            Assert.That(set.MicrochunkPoolEntries.Single().Active, Is.False);
            Assert.That(set.ToolUpgrades.Single().Active, Is.False);
        }

        private static void EveryDefinitionRetainsExactParsedRecord()
        {
            var sources = StandardSources();
            var set = Build(sources).DefinitionSet;
            foreach (var spec in Specs)
            {
                var definition = DefinitionFor(spec.FileName, set);
                var record = (CsvParsedRecord)definition.GetType().GetProperty("SourceRecord").GetValue(definition);
                Assert.That(record, Is.SameAs(sources.Single(source => source.FileName == spec.FileName).ParseResult.Records.Single()));
            }
        }

        private static List<MicrochunkPopulationItemDefinitionSource> CompositeSources()
        {
            var sources = StandardSources();
            ReplaceComposite(sources, "map_element_interactions.csv", rows => { rows[0][0] = "PARENT"; rows[0][1] = "ENUM_B"; rows[1][0] = "PARENT"; rows[1][1] = "ENUM_A"; });
            ReplaceComposite(sources, "microchunk_object_slots.csv", rows => { rows[0][0] = "PARENT"; rows[0][1] = "Z"; rows[1][0] = "PARENT"; rows[1][1] = "A"; });
            ReplaceComposite(sources, "microchunk_pool_entries.csv", rows => { rows[0][0] = "PARENT"; rows[0][1] = "2"; rows[1][0] = "PARENT"; rows[1][1] = "1"; });
            ReplaceComposite(sources, "microchunk_sockets.csv", rows => { rows[0][0] = "PARENT"; rows[0][1] = "Z"; rows[1][0] = "PARENT"; rows[1][1] = "A"; });
            ReplaceComposite(sources, "microchunk_tile_cells.csv", rows => { rows[0][0] = "PARENT"; rows[0][1] = "2"; rows[0][2] = "0"; rows[1][0] = "PARENT"; rows[1][1] = "1"; rows[1][2] = "0"; });
            ReplaceComposite(sources, "spawn_pool_entries.csv", rows => { rows[0][0] = "PARENT"; rows[0][1] = "2"; rows[1][0] = "PARENT"; rows[1][1] = "1"; });
            ReplaceComposite(sources, "tool_upgrade_definitions.csv", rows => { rows[0][0] = "PARENT"; rows[0][1] = "2"; rows[1][0] = "PARENT"; rows[1][1] = "1"; });
            return sources;
        }

        private static void ReplaceComposite(
            List<MicrochunkPopulationItemDefinitionSource> sources,
            string fileName,
            Action<string[][]> configure)
        {
            var spec = Spec(fileName);
            var rows = new[] { StandardRow(spec), StandardRow(spec) };
            configure(rows);
            Replace(sources, BuildSource(spec, rows));
        }

        private static IEnumerable<string> DictionaryFileNames() => new[]
        {
            "battery_profiles.csv", "map_element_definitions.csv", "microchunk_catalog.csv", "microchunk_variant_rules.csv",
            "population_profiles.csv", "prefab_registry.csv", "resource_definitions.csv",
            "resource_spawn_rules.csv", "special_item_slots.csv", "tile_code_dictionary.csv"
        };

        private static List<MicrochunkPopulationItemDefinitionSource> StandardSources() =>
            Specs.Select(spec => BuildSource(spec)).ToList();

        private static MicrochunkPopulationItemDefinitionBuildResult Build(
            IEnumerable<MicrochunkPopulationItemDefinitionSource> sources) =>
            new MicrochunkPopulationItemDefinitionBuilder().Build(sources);

        private static MicrochunkPopulationItemDefinitionSource BuildSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows = null,
            bool expectSuccess = true)
        {
            var schemaRows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName,
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                column.DataType,
                column.ExactMetadata ? (column.IsRequired ? "1" : "0") : (index < spec.PrimaryKeyCount ? "1" : "0"),
                column.ExactMetadata
                    ? (column.PrimaryKeyOrder.HasValue ? column.PrimaryKeyOrder.Value.ToString(CultureInfo.InvariantCulture) : string.Empty)
                    : (index < spec.PrimaryKeyCount ? (index + 1).ToString(CultureInfo.InvariantCulture) : string.Empty),
                column.DefaultValue,
                column.AllowedValues,
                column.ForeignKey,
                string.Empty,
                index + 2));
            var catalog = new CsvSchemaCatalogBuilder().Build(schemaRows);
            Assert.That(catalog.Success, Is.True, string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var sourceRows = rows ?? new[] { StandardRow(spec) };
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in sourceRows) csv += "\n" + string.Join(",", row.Select(CsvCell));
            var read = new Rfc4180CsvReader().Read(new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            Assert.That(validation.Success, Is.True, string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            Assert.That(keys.Success, Is.True);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            Assert.That(parsed.Success, Is.EqualTo(expectSuccess), string.Join("\n", parsed.Errors));
            return new MicrochunkPopulationItemDefinitionSource(schema, parsed);
        }

        private static string[] StandardRow(FileSpec spec) =>
            spec.Columns.Select((column, index) => Value(column, index)).ToArray();

        private static string Value(ColumnSpec column, int index)
        {
            if ((column.DataType == "ENUM" || column.DataType == "ENUM_LIST") &&
                !string.IsNullOrEmpty(column.AllowedValues))
            {
                return column.AllowedValues.Split('|')[0];
            }

            return Value(column.DataType, index);
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

        private static void AssertFullMapping(string fileName, MicrochunkPopulationItemDefinitionSet set)
        {
            var spec = Spec(fileName);
            var definition = DefinitionFor(fileName, set);
            var sourceProperty = definition.GetType().GetProperty("SourceRecord");
            Assert.That(sourceProperty, Is.Not.Null);
            var record = (CsvParsedRecord)sourceProperty.GetValue(definition);
            for (var index = 0; index < spec.Columns.Count; index++)
            {
                var propertyName = ToPascalCase(spec.Columns[index].Name);
                var property = definition.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                Assert.That(property, Is.Not.Null, fileName + " missing property " + propertyName);
                Assert.That(property.GetValue(definition), Is.EqualTo(ParsedValue(record, index, spec.Columns[index].DataType)),
                    fileName + "." + spec.Columns[index].Name);
            }
            Assert.That(sourceProperty.GetValue(definition), Is.SameAs(record));
        }

        private static object DefinitionFor(string fileName, MicrochunkPopulationItemDefinitionSet set)
        {
            switch (fileName)
            {
                case "battery_profiles.csv": return set.BatteryProfiles.Values.Single();
                case "map_element_definitions.csv": return set.MapElements.Values.Single();
                case "map_element_interactions.csv": return set.MapElementInteractions.Single();
                case "microchunk_catalog.csv": return set.Microchunks.Values.Single();
                case "microchunk_object_slots.csv": return set.MicrochunkObjectSlots.Single();
                case "microchunk_pool_entries.csv": return set.MicrochunkPoolEntries.Single();
                case "microchunk_sockets.csv": return set.MicrochunkSockets.Single();
                case "microchunk_tile_cells.csv": return set.MicrochunkTileCells.Single();
                case "microchunk_variant_rules.csv": return set.MicrochunkVariantRules.Values.Single();
                case "population_profiles.csv": return set.PopulationProfiles.Values.Single();
                case "prefab_registry.csv": return set.Prefabs.Values.Single();
                case "resource_definitions.csv": return set.Resources.Values.Single();
                case "resource_spawn_rules.csv": return set.ResourceSpawnRules.Values.Single();
                case "spawn_pool_entries.csv": return set.SpawnPoolEntries.Single();
                case "special_item_slots.csv": return set.SpecialItemSlots.Values.Single();
                case "tile_code_dictionary.csv": return set.TileCodes.Values.Single();
                case "tool_upgrade_definitions.csv": return set.ToolUpgrades.Single();
                default: throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
            }
        }

        private static object ParsedValue(CsvParsedRecord record, int index, string dataType)
        {
            var value = record.Fields[index].Value;
            switch (dataType)
            {
                case "STRING": case "ID": case "ENUM": return value.StringValue;
                case "INT": return value.IntValue;
                case "FLOAT": return value.FloatValue;
                case "BOOL": return value.BoolValue;
                case "ID_LIST": case "ENUM_LIST": return value.StringListValue;
                case "INT_LIST": return value.IntListValue;
                default: throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        private static string ToPascalCase(string value) => string.Concat(value.Split('_').Select(part =>
            char.ToUpperInvariant(part[0]) + part.Substring(1)));

        private static int TotalCount(MicrochunkPopulationItemDefinitionSet set) =>
            set.BatteryProfiles.Count + set.MapElements.Count + set.MapElementInteractions.Count + set.Microchunks.Count +
            set.MicrochunkObjectSlots.Count + set.MicrochunkPoolEntries.Count + set.MicrochunkSockets.Count +
            set.MicrochunkTileCells.Count + set.MicrochunkVariantRules.Count + set.PopulationProfiles.Count +
            set.Prefabs.Count + set.Resources.Count + set.ResourceSpawnRules.Count + set.SpawnPoolEntries.Count +
            set.SpecialItemSlots.Count + set.TileCodes.Count + set.ToolUpgrades.Count;

        private static string Snapshot(MicrochunkPopulationItemDefinitionSet set) => string.Join("|", new[]
        {
            string.Join(",", set.BatteryProfiles.Keys),
            string.Join(",", set.MapElements.Keys),
            string.Join(",", set.MapElementInteractions.Select(item => item.SourceElementOrToolId + ":" + item.TargetTag)),
            string.Join(",", set.Microchunks.Keys),
            string.Join(",", set.MicrochunkObjectSlots.Select(item => item.MicrochunkId + ":" + item.SlotId)),
            string.Join(",", set.MicrochunkPoolEntries.Select(item => item.MicrochunkPoolId + ":" + item.EntryOrder)),
            string.Join(",", set.MicrochunkSockets.Select(item => item.MicrochunkId + ":" + item.SocketId)),
            string.Join(",", set.MicrochunkTileCells.Select(item => item.MicrochunkId + ":" + item.LocalX + ":" + item.LocalY)),
            string.Join(",", set.MicrochunkVariantRules.Keys), string.Join(",", set.PopulationProfiles.Keys),
            string.Join(",", set.Prefabs.Keys), string.Join(",", set.Resources.Keys),
            string.Join(",", set.ResourceSpawnRules.Keys),
            string.Join(",", set.SpawnPoolEntries.Select(item => item.SpawnPoolId + ":" + item.EntryOrder)),
            string.Join(",", set.SpecialItemSlots.Keys), string.Join(",", set.TileCodes.Keys),
            string.Join(",", set.ToolUpgrades.Select(item => item.ToolId + ":" + item.UpgradeLevel))
        });

        private static FileSpec Spec(string fileName) => Specs.Single(spec => spec.FileName == fileName);

        private static void Replace(
            List<MicrochunkPopulationItemDefinitionSource> sources,
            MicrochunkPopulationItemDefinitionSource replacement)
        {
            sources.RemoveAll(source => source.FileName == replacement.FileName);
            sources.Add(replacement);
        }

        private static string CsvCell(string value) => value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
            ? value
            : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string FormatErrors(MicrochunkPopulationItemDefinitionBuildResult result) =>
            string.Join("\n", result.Errors.Select(error => error.FileName + " " + error.ErrorCode + " " + error.Message));

        private static FileSpec[] CreateSpecs() => new[]
        {
            new FileSpec("battery_profiles.csv", 1,
                ExactColumn("battery_id", "ID", true, 1),
                ExactColumn("display_name_ko", "STRING", true),
                ExactColumn("fuel_cost", "INT", true),
                ExactColumn("battery_item_cost", "INT", true),
                ExactColumn("delivery_mode", "ENUM", true, allowedValues: "PLACE|THROW|BLAST_CONE"),
                ExactColumn("blast_radius_tiles", "FLOAT", true),
                ExactColumn("damage", "INT", true),
                ExactColumn("knockback", "FLOAT", true),
                ExactColumn("destroys_soft_soil", "BOOL", true),
                ExactColumn("destroys_cracked_terrain", "BOOL", true),
                ExactColumn("destroys_hard_terrain", "BOOL", true),
                ExactColumn("destroys_starstone", "BOOL", true),
                ExactColumn("terrain_damage_enabled", "BOOL", true),
                ExactColumn("fuse_seconds", "FLOAT", true),
                ExactColumn("prefab_id", "ID", true, foreignKey: "prefab_registry.csv.prefab_id"),
                ExactColumn("active", "BOOL", true, defaultValue: "1"),
                ExactColumn("notes", "STRING", false)),
            File("map_element_definitions.csv", 1, "map_element_id:ID", "display_name_ko:STRING", "category:ENUM", "prefab_id:ID", "footprint_width_tiles:INT", "footprint_height_tiles:INT", "threat:INT", "utility:INT", "cognitive:INT", "chain:INT", "telegraph_seconds:FLOAT", "interaction_tags:ID_LIST", "forbidden_near_tags:ID_LIST", "active:BOOL", "notes:STRING"),
            File("map_element_interactions.csv", 2, "source_element_or_tool_id:ID", "target_tag:ID", "interaction_result:ENUM", "magnitude:FLOAT", "consumes_source:BOOL", "notes:STRING"),
            File("microchunk_catalog.csv", 1, "microchunk_id:ID", "display_name_ko:STRING", "width_tiles:INT", "height_tiles:INT", "usage_class:ENUM", "biome_ids:ID_LIST", "route_roles:ID_LIST", "allowed_transforms:ENUM_LIST", "selection_weight:INT", "threat:INT", "cognitive:INT", "chain:INT", "tile_data_complete:BOOL", "prefab_id:ID", "active:BOOL", "notes:STRING"),
            File("microchunk_object_slots.csv", 2, "microchunk_id:ID", "slot_id:ID", "local_x:INT", "local_y:INT", "slot_category:ENUM", "allowed_pool_id:ID", "required:BOOL", "orientation:ENUM", "visible_from_route:BOOL", "forbidden_radius_tiles:INT", "required_marker_code:ID", "notes:STRING"),
            File("microchunk_pool_entries.csv", 2, "microchunk_pool_id:ID", "entry_order:INT", "microchunk_id:ID", "weight:INT", "required_tags:ID_LIST", "forbidden_tags:ID_LIST", "min_repeat_distance_chunks:INT", "active:BOOL"),
            File("microchunk_sockets.csv", 2, "microchunk_id:ID", "socket_id:ID", "side:ENUM", "band_id:ID", "traversal_kind:ENUM", "direction:ENUM", "mandatory_allowed:BOOL", "tool_requirement:ENUM", "edge_signature_id:ID", "route_layer:ENUM", "minimum_safe_tiles:INT", "notes:STRING"),
            File("microchunk_tile_cells.csv", 3, "microchunk_id:ID", "local_x:INT", "local_y:INT", "ground_code:ID", "one_way_code:ID", "breakable_code:ID", "hazard_code:ID", "liquid_code:ID", "decor_back_code:ID", "decor_front_code:ID", "marker_code:ID"),
            File("microchunk_variant_rules.csv", 1, "variant_rule_id:ID", "microchunk_id:ID", "variant_id:ID", "weight:INT", "required_world_tags:ID_LIST", "forbidden_world_tags:ID_LIST", "replace_slot_pool_pairs:STRING", "active:BOOL", "notes:STRING"),
            File("population_profiles.csv", 1, "population_profile_id:ID", "biome_id:ID", "sector_role:ENUM", "resource_pool_ids:ID_LIST", "element_pool_ids:ID_LIST", "enemy_pool_ids:ID_LIST", "reward_pool_ids:ID_LIST", "budget_profile_id:ID", "active:BOOL", "notes:STRING"),
            File("prefab_registry.csv", 1, "prefab_id:ID", "asset_address:STRING", "content_type:ENUM", "expected_component:STRING", "placeholder_allowed:BOOL", "active:BOOL", "notes:STRING"),
            File("resource_definitions.csv", 1, "resource_id:ID", "display_name_ko:STRING", "resource_category:ENUM", "hud_destination:ENUM", "unique_per_world:BOOL", "max_quantity:INT", "pickup_prefab_id:ID", "active:BOOL", "notes:STRING"),
            File("resource_spawn_rules.csv", 1, "spawn_rule_id:ID", "resource_id:ID", "biome_ids:ID_LIST", "patch_roles:ENUM_LIST", "sector_route_types:INT_LIST", "allowed_slot_pool_ids:ID_LIST", "world_min:INT", "world_max:INT", "patch_min:INT", "patch_max:INT", "spawn_weight:INT", "min_distance_from_same_resource_tiles:INT", "mandatory_site_id:ID", "active:BOOL", "notes:STRING"),
            File("spawn_pool_entries.csv", 2, "spawn_pool_id:ID", "entry_order:INT", "entry_kind:ENUM", "entry_id:ID", "weight:INT", "quantity_min:INT", "quantity_max:INT", "required_tags:ID_LIST", "forbidden_tags:ID_LIST", "active:BOOL", "notes:STRING"),
            File("special_item_slots.csv", 1, "special_item_slot_id:ID", "display_name_ko:STRING", "unknown_sprite_prefab_id:ID", "revealed_sprite_prefab_id:ID", "starts_revealed:BOOL", "maximum_per_world:INT", "effect_id:ID", "active:BOOL", "notes:STRING"),
            File("tile_code_dictionary.csv", 1, "tile_code:ID", "layer:ENUM", "semantic:STRING", "collision_kind:ENUM", "destructible:BOOL", "tile_asset_prefab_id:ID", "runtime_tag:ID", "debug_glyph:STRING", "active:BOOL"),
            File("tool_upgrade_definitions.csv", 2, "tool_id:ID", "upgrade_level:INT", "required_blueprint_fragments:INT", "gold_cost:INT", "max_durability_multiplier:FLOAT", "work_speed_multiplier:FLOAT", "special_effect_id:ID", "active:BOOL", "notes:STRING")
        };

        private static FileSpec File(string fileName, int primaryKeyCount, params string[] definitions) =>
            new FileSpec(fileName, primaryKeyCount, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                return Column(parts[0], parts[1]);
            }).ToArray());

        private static ColumnSpec Column(string name, string dataType, string defaultValue = "") =>
            new ColumnSpec(name, dataType, defaultValue,
                dataType == "ENUM" || dataType == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty,
                false, false, null, string.Empty);

        private static ColumnSpec ExactColumn(
            string name,
            string dataType,
            bool isRequired,
            int? primaryKeyOrder = null,
            string defaultValue = "",
            string allowedValues = "",
            string foreignKey = "") =>
            new ColumnSpec(
                name, dataType, defaultValue, allowedValues,
                true, isRequired, primaryKeyOrder, foreignKey);

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
                string allowedValues,
                bool exactMetadata,
                bool isRequired,
                int? primaryKeyOrder,
                string foreignKey)
            {
                Name = name;
                DataType = dataType;
                DefaultValue = defaultValue;
                AllowedValues = allowedValues;
                ExactMetadata = exactMetadata;
                IsRequired = isRequired;
                PrimaryKeyOrder = primaryKeyOrder;
                ForeignKey = foreignKey;
            }
            public string Name { get; }
            public string DataType { get; }
            public string DefaultValue { get; }
            public string AllowedValues { get; }
            public bool ExactMetadata { get; }
            public bool IsRequired { get; }
            public int? PrimaryKeyOrder { get; }
            public string ForeignKey { get; }

            public ColumnSpec With(
                string dataType = null,
                bool? isRequired = null,
                int? primaryKeyOrder = null,
                string defaultValue = null,
                string allowedValues = null,
                string foreignKey = null) =>
                new ColumnSpec(
                    Name,
                    dataType ?? DataType,
                    defaultValue ?? DefaultValue,
                    allowedValues ?? AllowedValues,
                    true,
                    isRequired ?? IsRequired,
                    primaryKeyOrder,
                    foreignKey ?? ForeignKey);
        }
    }
}
