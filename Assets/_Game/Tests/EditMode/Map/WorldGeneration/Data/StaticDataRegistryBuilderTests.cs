using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Data
{
    public sealed class StaticDataRegistryBuilderTests
    {
        private Fixture fixture;
        private Fixture secondFixture;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            fixture = CreateFixture();
            secondFixture = CreateFixture();
        }

        [Test]
        public void Build_WithExactInputs_Succeeds()
        {
            Assert.That(Build().Success, Is.True, FormatErrors(Build()));
        }

        [Test]
        public void Build_WithExactInputs_HasNoErrors()
        {
            Assert.That(Build().Errors, Is.Empty);
        }

        [Test]
        public void Build_WithExactInputs_PassesInputGate()
        {
            Assert.That(Build().InputGatePassed, Is.True);
        }

        [Test]
        public void Registry_PreservesAllFourRootInstances()
        {
            var registry = Build().Registry;
            Assert.That(registry.WorldRouteDefinitions, Is.SameAs(fixture.World));
            Assert.That(registry.BiomeBoundaryDefinitions, Is.SameAs(fixture.Biome));
            Assert.That(registry.SpecialVillageDefinitions, Is.SameAs(fixture.Special));
            Assert.That(registry.MicrochunkPopulationItemDefinitions, Is.SameAs(fixture.Micro));
        }

        [Test]
        public void Registry_PreservesForeignKeyResolutionInstance()
        {
            Assert.That(Build().Registry.ForeignKeyResolution, Is.SameAs(fixture.Resolution));
        }

        [Test]
        public void Registry_PreservesForeignKeyRecordIndexInstance()
        {
            Assert.That(Build().Registry.RecordIndex, Is.SameAs(fixture.Resolution.RecordIndex));
        }

        [Test]
        public void Registry_PreservesResolvedReferenceInstances()
        {
            var registry = Build().Registry;
            Assert.That(registry.References.Count, Is.EqualTo(fixture.Resolution.References.Count));
            Assert.That(registry.References[0], Is.SameAs(fixture.Resolution.References[0]));
            Assert.That(registry.References[1], Is.SameAs(fixture.Resolution.References[1]));
        }

        [Test]
        public void Registry_MapsEveryMaterializedTypedDefinition()
        {
            Assert.That(Build().Registry.TypedDefinitions.Count, Is.EqualTo(3));
        }

        [Test]
        public void Registry_MapsRngDefinitionBySourceIdentity()
        {
            var registry = Build().Registry;
            var identity = Identity("rng_streams.csv");
            Assert.That(registry.TryGetTypedDefinition(identity, out var definition), Is.True);
            Assert.That(definition, Is.SameAs(fixture.Rng));
        }

        [Test]
        public void Registry_MapsGenerationPassBySourceIdentity()
        {
            var registry = Build().Registry;
            var identity = Identity("generation_passes.csv");
            Assert.That(registry.TryGetTypedDefinition(identity, out var definition), Is.True);
            Assert.That(definition, Is.SameAs(fixture.Pass));
        }

        [Test]
        public void Registry_GenericLookupFindsUntypedRecord()
        {
            var registry = Build().Registry;
            var expected = Identity("world_profiles.csv");
            Assert.That(registry.TryGetRecord("world_profiles.csv", expected.RecordNumber, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Registry_GenericLookupFindsTypedRecord()
        {
            var registry = Build().Registry;
            var expected = Identity("rng_streams.csv");
            Assert.That(registry.TryGetRecord("rng_streams.csv", expected.RecordNumber, out var actual), Is.True);
            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void Registry_GenericLookupReturnsFalseForMissingRecord()
        {
            Assert.That(Build().Registry.TryGetRecord("rng_streams.csv", 999, out var identity), Is.False);
            Assert.That(identity, Is.Null);
        }

        [Test]
        public void Registry_ReferencedPrimaryKeyLookupDelegatesToIndex()
        {
            var registry = Build().Registry;
            Assert.That(registry.TryGetReferencedPrimaryKey(
                "rng_streams.csv", "rng_stream_id", "RNG_A", out var identity), Is.True);
            Assert.That(identity, Is.SameAs(Identity("rng_streams.csv")));
        }

        [Test]
        public void Registry_ReferencedPrimaryKeyLookupReturnsFalseForMissingValue()
        {
            Assert.That(Build().Registry.TryGetReferencedPrimaryKey(
                "rng_streams.csv", "rng_stream_id", "MISSING", out var identity), Is.False);
            Assert.That(identity, Is.Null);
        }

        [Test]
        public void Registry_RecordsRemainOrdinalStable()
        {
            var projection = Build().Registry.Records
                .Select(item => item.FileName + ":" + item.RecordNumber).ToArray();
            Assert.That(projection, Is.EqualTo(projection.OrderBy(item => item, StringComparer.Ordinal)));
        }

        [Test]
        public void Registry_PreservesUntypedForeignKeyRecords()
        {
            var registry = Build().Registry;
            Assert.That(registry.Records.Count, Is.EqualTo(5));
            Assert.That(registry.TypedDefinitions.Count, Is.LessThan(registry.Records.Count));
        }

        [Test]
        public void ReverseIndex_TargetIdentityReturnsBothIncomingTokens()
        {
            Assert.That(Build().Registry.ReverseIndex.GetIncoming(Identity("rng_streams.csv")).Count,
                Is.EqualTo(2));
        }

        [Test]
        public void ReverseIndex_SourceIdentityReturnsBothOutgoingTokens()
        {
            Assert.That(Build().Registry.ReverseIndex.GetOutgoing(Identity("world_profiles.csv")).Count,
                Is.EqualTo(2));
        }

        [Test]
        public void ReverseIndex_TargetValueReturnsBothIncomingTokens()
        {
            Assert.That(Build().Registry.ReverseIndex.GetIncoming(
                "rng_streams.csv", "rng_stream_id", "RNG_A").Count, Is.EqualTo(2));
        }

        [Test]
        public void ReverseIndex_DuplicateListTokensRemainDistinctReferences()
        {
            var incoming = Build().Registry.ReverseIndex.GetIncoming(Identity("rng_streams.csv"));
            Assert.That(incoming[0], Is.Not.SameAs(incoming[1]));
        }

        [Test]
        public void ReverseIndex_DuplicateListTokensPreserveListOrder()
        {
            var incoming = Build().Registry.ReverseIndex.GetIncoming(Identity("rng_streams.csv"));
            Assert.That(incoming.Select(item => item.ListIndex), Is.EqualTo(new int?[] { 0, 1 }));
        }

        [Test]
        public void ReverseIndex_MissingTargetIdentityReturnsNonNullEmptyView()
        {
            var missing = new ForeignKeyRecordIdentity("rng_streams.csv", fixture.Rng.SourceRecord);
            var result = Build().Registry.ReverseIndex.GetIncoming(missing);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ReverseIndex_MissingSourceIdentityReturnsNonNullEmptyView()
        {
            var missing = new ForeignKeyRecordIdentity("rng_streams.csv", fixture.Rng.SourceRecord);
            var result = Build().Registry.ReverseIndex.GetOutgoing(missing);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ReverseIndex_NullIdentityReturnsNonNullEmptyView()
        {
            Assert.That(Build().Registry.ReverseIndex.GetIncoming((ForeignKeyRecordIdentity)null), Is.Empty);
            Assert.That(Build().Registry.ReverseIndex.GetOutgoing(null), Is.Empty);
        }

        [Test]
        public void ReverseIndex_MissingTargetValueReturnsNonNullEmptyView()
        {
            var result = Build().Registry.ReverseIndex.GetIncoming(
                "rng_streams.csv", "rng_stream_id", "MISSING");
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ReverseIndex_NullTargetValueComponentReturnsNonNullEmptyView()
        {
            Assert.That(Build().Registry.ReverseIndex.GetIncoming(null, "id", "value"), Is.Empty);
            Assert.That(Build().Registry.ReverseIndex.GetIncoming("file", null, "value"), Is.Empty);
            Assert.That(Build().Registry.ReverseIndex.GetIncoming("file", "id", null), Is.Empty);
        }

        [Test]
        public void Registry_TypedLookupReturnsFalseForUntypedIdentity()
        {
            Assert.That(Build().Registry.TryGetTypedDefinition(
                Identity("world_profiles.csv"), out var definition), Is.False);
            Assert.That(definition, Is.Null);
        }

        [Test]
        public void Registry_TypedDefinitionMapIsReadOnly()
        {
            var registry = Build().Registry;
            var dictionary = (IDictionary<ForeignKeyRecordIdentity, object>)registry.TypedDefinitions;
            Assert.Throws<NotSupportedException>(() => dictionary.Add(
                Identity("world_profiles.csv"), new object()));
        }

        [Test]
        public void Registry_RecordViewIsReadOnly()
        {
            var records = (IList<ForeignKeyRecordIdentity>)Build().Registry.Records;
            Assert.Throws<NotSupportedException>(() => records.Add(Identity("rng_streams.csv")));
        }

        [Test]
        public void ReverseIndex_ResultViewsAreReadOnly()
        {
            var incoming = (IList<ResolvedForeignKeyReference>)Build().Registry.ReverseIndex
                .GetIncoming(Identity("rng_streams.csv"));
            Assert.Throws<NotSupportedException>(() => incoming.Add(fixture.Resolution.References[0]));
        }

        [Test]
        public void RepeatedBuildsHaveStableRecordAndReferenceSnapshots()
        {
            Assert.That(Snapshot(Build().Registry), Is.EqualTo(Snapshot(Build().Registry)));
        }

        [Test]
        public void NullInput_AccumulatesFourMissingSetsAndForeignKeyFailure()
        {
            var result = new StaticDataRegistryBuilder().Build(null);
            Assert.That(result.Errors.Count, Is.EqualTo(5));
            Assert.That(result.Errors.Count(item =>
                item.ErrorCode == StaticDataRegistryBuildErrorCode.MissingDefinitionSet), Is.EqualTo(4));
            Assert.That(result.Errors.Any(item =>
                item.ErrorCode == StaticDataRegistryBuildErrorCode.UnsuccessfulForeignKeyResolution), Is.True);
        }

        [TestCase("world")]
        [TestCase("biome")]
        [TestCase("special")]
        [TestCase("micro")]
        public void MissingDefinitionSet_ReportsExactMinimumCode(string missing)
        {
            var input = new StaticDataRegistryInput(
                missing == "world" ? null : fixture.World,
                missing == "biome" ? null : fixture.Biome,
                missing == "special" ? null : fixture.Special,
                missing == "micro" ? null : fixture.Micro,
                fixture.Resolution);
            var result = new StaticDataRegistryBuilder().Build(input);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors.Count(item =>
                item.ErrorCode == StaticDataRegistryBuildErrorCode.MissingDefinitionSet), Is.EqualTo(1));
        }

        [Test]
        public void NullForeignKeyResolution_ReportsUnsuccessfulForeignKeyResolution()
        {
            var input = Input(fixture.World, null);
            AssertError(Build(input), StaticDataRegistryBuildErrorCode.UnsuccessfulForeignKeyResolution);
        }

        [Test]
        public void FailedForeignKeyResolution_ReportsUnsuccessfulForeignKeyResolution()
        {
            var input = Input(fixture.World, new ForeignKeyResolver().Resolve(null));
            AssertError(Build(input), StaticDataRegistryBuildErrorCode.UnsuccessfulForeignKeyResolution);
        }

        [Test]
        public void TypedDefinitionFromAnotherIndex_ReportsDefinitionRecordMissingFromIndex()
        {
            var input = new StaticDataRegistryInput(
                fixture.World,
                secondFixture.Biome,
                secondFixture.Special,
                secondFixture.Micro,
                secondFixture.Resolution);
            AssertError(Build(input), StaticDataRegistryBuildErrorCode.DefinitionRecordMissingFromIndex);
        }

        [Test]
        public void DuplicateTypedSourceIdentity_ReportsDuplicateTypedDefinitionIdentity()
        {
            var duplicateWorld = World(fixture.Rng, new[] { fixture.Pass, fixture.Pass });
            AssertError(Build(Input(duplicateWorld, fixture.Resolution)),
                StaticDataRegistryBuildErrorCode.DuplicateTypedDefinitionIdentity);
        }

        [Test]
        public void ReversedForeignKeyReferences_ReportForeignKeyGraphMismatch()
        {
            var resolution = Construct<ForeignKeyResolutionResult>(
                fixture.Resolution.RecordIndex,
                fixture.Resolution.References.Reverse().ToArray(),
                Array.Empty<ForeignKeyResolutionError>());
            AssertError(Build(Input(fixture.World, resolution)),
                StaticDataRegistryBuildErrorCode.ForeignKeyGraphMismatch);
        }

        [Test]
        public void ForeignReferenceIdentities_ReportForeignKeyGraphMismatch()
        {
            var resolution = Construct<ForeignKeyResolutionResult>(
                fixture.Resolution.RecordIndex,
                secondFixture.Resolution.References,
                Array.Empty<ForeignKeyResolutionError>());
            AssertError(Build(Input(fixture.World, resolution)),
                StaticDataRegistryBuildErrorCode.ForeignKeyGraphMismatch);
        }

        [Test]
        public void GateErrorsAreDeterministicallySorted()
        {
            var errors = new StaticDataRegistryBuilder().Build(null).Errors;
            var projection = errors.Select(item =>
                item.FileName + ":" + item.RecordNumber + ":" + item.DefinitionType + ":" + item.ErrorCode).ToArray();
            Assert.That(projection, Is.EqualTo(projection.OrderBy(item => item, StringComparer.Ordinal)));
        }

        [Test]
        public void GateFailurePublishesNoRegistry()
        {
            Assert.That(new StaticDataRegistryBuilder().Build(null).Registry, Is.Null);
        }

        [Test]
        public void GateFailureErrorViewIsReadOnly()
        {
            var errors = (IList<StaticDataRegistryBuildError>)new StaticDataRegistryBuilder().Build(null).Errors;
            Assert.Throws<NotSupportedException>(() => errors.Clear());
        }

        [Test]
        public void RegistryExposesTypedCollectionsThroughOriginalRoots()
        {
            var registry = Build().Registry;
            Assert.That(registry.WorldRouteDefinitions.RngStreams["RNG_A"], Is.SameAs(fixture.Rng));
            Assert.That(registry.WorldRouteDefinitions.GenerationPasses.Single(), Is.SameAs(fixture.Pass));
        }

        [Test]
        public void RegistryExposesBatteryTypedCollectionThroughOriginalRoot()
        {
            var registry = Build().Registry;
            Assert.That(registry.MicrochunkPopulationItemDefinitions.BatteryProfiles["BAT_A"],
                Is.SameAs(fixture.Battery));
        }

        [Test]
        public void RegistryMapsBatteryByExactSourceIdentity()
        {
            var registry = Build().Registry;
            Assert.That(registry.TryGetTypedDefinition(Identity("battery_profiles.csv"), out var definition), Is.True);
            Assert.That(definition, Is.SameAs(fixture.Battery));
        }

        [Test]
        public void RegistryBatteryRetainsExactParsedSourceRecord()
        {
            Assert.That(fixture.Battery.SourceRecord,
                Is.SameAs(Identity("battery_profiles.csv").SourceRecord));
        }

        [Test]
        public void RegistryBatteryPrefabForeignKeyTargetsExactPrefabRecord()
        {
            var outgoing = Build().Registry.ReverseIndex.GetOutgoing(Identity("battery_profiles.csv"));
            Assert.That(outgoing.Count, Is.EqualTo(1));
            Assert.That(outgoing[0].TargetIdentity, Is.SameAs(Identity("prefab_registry.csv")));
            Assert.That(outgoing[0].TargetValue, Is.EqualTo("PREFAB_A"));
        }

        [Test]
        public void RegistryBatteryDictionaryIsOrdinalCaseSensitiveAndReadOnly()
        {
            var dictionary = Build().Registry.MicrochunkPopulationItemDefinitions.BatteryProfiles;
            Assert.That(dictionary.ContainsKey("bat_a"), Is.False);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, BatteryProfileDefinition>)dictionary).Clear());
        }

        [Test]
        public void RegistryBatteryAddsExactlyOneTypedDefinitionWithoutDuplicatingRecords()
        {
            var registry = Build().Registry;
            Assert.That(registry.TypedDefinitions.Count, Is.EqualTo(3));
            Assert.That(registry.Records.Count, Is.EqualTo(5));
            Assert.That(registry.Records.Count(item => item.FileName == "battery_profiles.csv"), Is.EqualTo(1));
        }

        private StaticDataRegistryBuildResult Build() => Build(Input(fixture.World, fixture.Resolution));

        private StaticDataRegistryBuildResult Build(StaticDataRegistryInput input) =>
            new StaticDataRegistryBuilder().Build(input);

        private StaticDataRegistryInput Input(
            WorldRouteDefinitionSet world,
            ForeignKeyResolutionResult resolution) =>
            new StaticDataRegistryInput(
                world,
                fixture.Biome,
                fixture.Special,
                fixture.Micro,
                resolution);

        private ForeignKeyRecordIdentity Identity(string fileName) =>
            fixture.Resolution.RecordIndex.Records.Single(item => item.FileName == fileName);

        private static Fixture CreateFixture()
        {
            var specs = ForeignKeySourceSet.ExpectedFileNames.ToDictionary(
                fileName => fileName,
                fileName => new[] { Column("id", "ID", true, 1) },
                StringComparer.Ordinal);
            specs["rng_streams.csv"] = new[]
            {
                Column("rng_stream_id", "ID", true, 1),
                Column("salt_hex", "HEX", true),
                Column("reset_scope", "ENUM", true, allowedValues: "ENUM_A|ENUM_B"),
                Column("description_ko", "STRING", true),
                Column("active", "BOOL", true)
            };
            specs["generation_passes.csv"] = new[]
            {
                Column("generation_profile_id", "ID", true, 1),
                Column("pass_order", "INT", true, 2),
                Column("pass_id", "ID", true, 3),
                Column("class_name", "STRING", true),
                Column("rng_stream_id", "ID", true),
                Column("input_artifacts", "ID_LIST", true),
                Column("output_artifacts", "ID_LIST", true),
                Column("failure_policy", "ENUM", true, allowedValues: "ENUM_A|ENUM_B"),
                Column("max_retry_count", "INT", true),
                Column("enabled", "BOOL", true),
                Column("notes", "STRING", true)
            };
            specs["world_profiles.csv"] = new[]
            {
                Column("world_profile_id", "ID", true, 1),
                Column("rng_stream_ids", "ID_LIST", true,
                    foreignKey: "rng_streams.csv.rng_stream_id")
            };
            specs["battery_profiles.csv"] = new[]
            {
                Column("battery_id", "ID", true, 1),
                Column("display_name_ko", "STRING", true),
                Column("fuel_cost", "INT", true),
                Column("battery_item_cost", "INT", true),
                Column("delivery_mode", "ENUM", true, allowedValues: "PLACE|THROW|BLAST_CONE"),
                Column("blast_radius_tiles", "FLOAT", true),
                Column("damage", "INT", true),
                Column("knockback", "FLOAT", true),
                Column("destroys_soft_soil", "BOOL", true),
                Column("destroys_cracked_terrain", "BOOL", true),
                Column("destroys_hard_terrain", "BOOL", true),
                Column("destroys_starstone", "BOOL", true),
                Column("terrain_damage_enabled", "BOOL", true),
                Column("fuse_seconds", "FLOAT", true),
                Column("prefab_id", "ID", true, foreignKey: "prefab_registry.csv.prefab_id"),
                Column("active", "BOOL", true),
                Column("notes", "STRING", false)
            };
            specs["prefab_registry.csv"] = new[]
            {
                Column("prefab_id", "ID", true, 1)
            };

            var catalog = BuildCatalog(specs);
            var sources = new List<ForeignKeySourceSet.Source>();
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                var schema = catalog.GetFile(fileName);
                var body = string.Empty;
                if (fileName == "rng_streams.csv") body = "\nRNG_A,0x01,ENUM_A,RNG,1";
                if (fileName == "generation_passes.csv")
                    body = "\nGP_A,1,PASS_A,PassClass,RNG_A,A|A,B,ENUM_A,1,1,NOTE";
                if (fileName == "world_profiles.csv") body = "\nWORLD_A,RNG_A|RNG_A";
                if (fileName == "battery_profiles.csv")
                    body = "\nBAT_A,Battery,1,2,PLACE,1.5,10,0.5,1,1,0,0,1,2.5,PREFAB_A,1,NOTE";
                if (fileName == "prefab_registry.csv") body = "\nPREFAB_A";
                sources.Add(new ForeignKeySourceSet.Source(schema, Parse(schema, Header(schema) + body)));
            }

            var resolution = new ForeignKeyResolver().Resolve(new ForeignKeySourceSet(catalog, sources));
            Assert.That(resolution.Success, Is.True,
                string.Join("\n", resolution.Errors.Select(item => item.Message)));

            var rngRecord = sources.Single(item => item.FileName == "rng_streams.csv")
                .ParseResult.Records.Single();
            var passRecord = sources.Single(item => item.FileName == "generation_passes.csv")
                .ParseResult.Records.Single();
            var batteryRecord = sources.Single(item => item.FileName == "battery_profiles.csv")
                .ParseResult.Records.Single();
            var rng = Construct<RngStreamDefinition>(rngRecord);
            var pass = Construct<GenerationPassDefinition>(passRecord);
            var battery = Construct<BatteryProfileDefinition>(batteryRecord);
            return new Fixture(
                resolution,
                World(rng, new[] { pass }),
                EmptyBiome(),
                EmptySpecial(),
                EmptyMicro(battery),
                rng,
                pass,
                battery);
        }

        private static CsvSchemaCatalog BuildCatalog(
            IReadOnlyDictionary<string, ColumnSpec[]> specs)
        {
            var rows = new List<CsvSchemaDictionaryRow>();
            var sourceRow = 2;
            foreach (var pair in specs.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                for (var index = 0; index < pair.Value.Length; index++)
                {
                    var column = pair.Value[index];
                    rows.Add(new CsvSchemaDictionaryRow(
                        pair.Key,
                        (index + 1).ToString(CultureInfo.InvariantCulture),
                        column.Name,
                        column.DataType,
                        column.Required ? "1" : "0",
                        column.PrimaryKeyOrder.HasValue
                            ? column.PrimaryKeyOrder.Value.ToString(CultureInfo.InvariantCulture)
                            : string.Empty,
                        string.Empty,
                        column.AllowedValues,
                        column.ForeignKey,
                        string.Empty,
                        sourceRow++));
                }
            }

            var result = new CsvSchemaCatalogBuilder().Build(rows);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return result.Catalog;
        }

        private static CsvScalarAndListParseResult Parse(CsvFileSchema schema, string csv)
        {
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), schema.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, schema.FileName);
            Assert.That(validation.Success, Is.True,
                string.Join("\n", validation.Errors.Select(item => item.Message)));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, schema.FileName);
            Assert.That(keys.Success, Is.True);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys, schema.FileName);
            Assert.That(parsed.Success, Is.True,
                string.Join("\n", parsed.Errors.Select(item => item.Message)));
            return parsed;
        }

        private static string Header(CsvFileSchema schema) =>
            string.Join(",", schema.Columns.Select(item => item.ColumnName));

        private static ColumnSpec Column(
            string name,
            string dataType,
            bool required,
            int? primaryKeyOrder = null,
            string allowedValues = "",
            string foreignKey = "") =>
            new ColumnSpec(name, dataType, required, primaryKeyOrder, allowedValues, foreignKey);

        private static WorldRouteDefinitionSet World(
            RngStreamDefinition rng,
            IEnumerable<GenerationPassDefinition> passes) =>
            Construct<WorldRouteDefinitionSet>(
                Array.Empty<WorldProfileDefinition>(),
                Array.Empty<GenerationProfileDefinition>(),
                passes ?? Array.Empty<GenerationPassDefinition>(),
                rng == null ? Array.Empty<RngStreamDefinition>() : new[] { rng },
                Array.Empty<SectorRouteMaskDefinition>(),
                Array.Empty<SocketBandDefinition>(),
                Array.Empty<EdgeSignatureDefinition>(),
                Array.Empty<EdgeSignatureCompatibilityDefinition>(),
                Array.Empty<SectorRecipeDefinition>(),
                Array.Empty<SectorRecipeCellDefinition>(),
                Array.Empty<SectorRecipePathDefinition>(),
                Array.Empty<SectorExternalSocketDefinition>(),
                Array.Empty<SectorRecipePoolEntryDefinition>());

        private static BiomeBoundaryDefinitionSet EmptyBiome() =>
            Construct<BiomeBoundaryDefinitionSet>(
                Array.Empty<BiomeTypeDefinition>(),
                Array.Empty<BiomePatchRuleDefinition>(),
                Array.Empty<BiomeBoundaryProfileDefinition>(),
                Array.Empty<BiomeBoundaryPairRuleDefinition>(),
                Array.Empty<BoundaryChunkDefinition>());

        private static SpecialVillageDefinitionSet EmptySpecial() =>
            Construct<SpecialVillageDefinitionSet>(
                Array.Empty<EventActivationRouteDefinition>(),
                Array.Empty<SpecialMapDefinition>(),
                Array.Empty<SpecialMapEntrySocketDefinition>(),
                Array.Empty<SpecialMapFootprintCellDefinition>(),
                Array.Empty<SpecialMapRewardDefinition>(),
                Array.Empty<ShopArchetypeDefinition>(),
                Array.Empty<ShopInventoryRuleDefinition>(),
                Array.Empty<ShopkeeperSpeciesDefinition>(),
                Array.Empty<VillageFacilityDefinition>(),
                Array.Empty<VillageLayoutDefinition>(),
                Array.Empty<VillageLayoutCellDefinition>(),
                Array.Empty<VillageProfileDefinition>());

        private static MicrochunkPopulationItemDefinitionSet EmptyMicro(BatteryProfileDefinition battery = null) =>
            Construct<MicrochunkPopulationItemDefinitionSet>(
                Array.Empty<MapElementDefinition>(),
                Array.Empty<MapElementInteractionDefinition>(),
                Array.Empty<MicrochunkDefinition>(),
                Array.Empty<MicrochunkObjectSlotDefinition>(),
                Array.Empty<MicrochunkPoolEntryDefinition>(),
                Array.Empty<MicrochunkSocketDefinition>(),
                Array.Empty<MicrochunkTileCellDefinition>(),
                Array.Empty<MicrochunkVariantRuleDefinition>(),
                Array.Empty<PopulationProfileDefinition>(),
                Array.Empty<PrefabRegistryDefinition>(),
                Array.Empty<ResourceDefinition>(),
                Array.Empty<ResourceSpawnRuleDefinition>(),
                Array.Empty<SpawnPoolEntryDefinition>(),
                Array.Empty<SpecialItemSlotDefinition>(),
                Array.Empty<TileCodeDefinition>(),
                Array.Empty<ToolUpgradeDefinition>(),
                battery == null ? Array.Empty<BatteryProfileDefinition>() : new[] { battery });

        private static T Construct<T>(params object[] arguments) =>
            (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                arguments,
                CultureInfo.InvariantCulture);

        private static void AssertError(
            StaticDataRegistryBuildResult result,
            StaticDataRegistryBuildErrorCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Registry, Is.Null);
            Assert.That(result.Errors.Any(item => item.ErrorCode == code), Is.True, FormatErrors(result));
        }

        private static string Snapshot(StaticDataRegistry registry) =>
            string.Join("|", registry.Records.Select(item => item.FileName + ":" + item.RecordNumber)) + "#" +
            string.Join("|", registry.References.Select(item =>
                item.SourceFileName + ":" + item.SourceRecordNumber + ":" + item.SourceColumnOrder + ":" +
                item.ListIndex + ":" + item.TargetFileName + ":" + item.TargetColumnName + ":" + item.TargetValue));

        private static string FormatErrors(StaticDataRegistryBuildResult result) =>
            string.Join("\n", result.Errors.Select(item =>
                item.ErrorCode + " " + item.FileName + " " + item.RecordNumber + " " + item.Message));

        private sealed class Fixture
        {
            public Fixture(
                ForeignKeyResolutionResult resolution,
                WorldRouteDefinitionSet world,
                BiomeBoundaryDefinitionSet biome,
                SpecialVillageDefinitionSet special,
                MicrochunkPopulationItemDefinitionSet micro,
                RngStreamDefinition rng,
                GenerationPassDefinition pass,
                BatteryProfileDefinition battery)
            {
                Resolution = resolution;
                World = world;
                Biome = biome;
                Special = special;
                Micro = micro;
                Rng = rng;
                Pass = pass;
                Battery = battery;
            }

            public ForeignKeyResolutionResult Resolution { get; }
            public WorldRouteDefinitionSet World { get; }
            public BiomeBoundaryDefinitionSet Biome { get; }
            public SpecialVillageDefinitionSet Special { get; }
            public MicrochunkPopulationItemDefinitionSet Micro { get; }
            public RngStreamDefinition Rng { get; }
            public GenerationPassDefinition Pass { get; }
            public BatteryProfileDefinition Battery { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(
                string name,
                string dataType,
                bool required,
                int? primaryKeyOrder,
                string allowedValues,
                string foreignKey)
            {
                Name = name;
                DataType = dataType;
                Required = required;
                PrimaryKeyOrder = primaryKeyOrder;
                AllowedValues = allowedValues;
                ForeignKey = foreignKey;
            }

            public string Name { get; }
            public string DataType { get; }
            public bool Required { get; }
            public int? PrimaryKeyOrder { get; }
            public string AllowedValues { get; }
            public string ForeignKey { get; }
        }
    }
}
