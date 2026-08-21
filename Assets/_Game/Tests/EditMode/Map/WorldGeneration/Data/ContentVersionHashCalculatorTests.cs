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
    public sealed class ContentVersionHashCalculatorTests
    {
        private Fixture baseline;
        private Fixture secondIdentity;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            baseline = CreateFixture();
            secondIdentity = CreateFixture();
        }

        [Test]
        public void ExactInput_Succeeds()
        {
            var result = Calculate(baseline);
            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ExactInput_MatchesKnownSha256Vector()
        {
            Assert.That(Calculate(baseline).Hash.Hex, Is.EqualTo(
                "5cb9e42a22ad4cf89190c3b106c34db5bea420d7c0c5ebbeeff4b3bb9a4a4cdb"));
        }

        [Test]
        public void Hash_ContainsExactlyThirtyTwoBytes()
        {
            Assert.That(Calculate(baseline).Hash.Bytes.Count, Is.EqualTo(32));
        }

        [Test]
        public void Hash_UsesLowercaseSixtyFourCharacterHex()
        {
            var hex = Calculate(baseline).Hash.Hex;
            Assert.That(hex.Length, Is.EqualTo(64));
            Assert.That(hex, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(Calculate(baseline).Hash.ToString(), Is.EqualTo(hex));
        }

        [Test]
        public void Hash_ByteViewIsReadOnly()
        {
            var bytes = (IList<byte>)Calculate(baseline).Hash.Bytes;
            Assert.Throws<NotSupportedException>(() => bytes[0] = 0);
        }

        [Test]
        public void Hash_ToByteArrayReturnsSafeCopy()
        {
            var hash = Calculate(baseline).Hash;
            var copy = hash.ToByteArray();
            copy[0] ^= 0xff;
            Assert.That(copy[0], Is.Not.EqualTo(hash.Bytes[0]));
        }

        [Test]
        public void Hash_EqualityAndHashCodeAreValueBased()
        {
            var first = Calculate(baseline).Hash;
            var second = Calculate(CreateFixture()).Hash;
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void Hash_InequalityIsValueBased()
        {
            var changed = ChangedValue("notes", "DifferentNote");
            Assert.That(Calculate(baseline).Hash != Calculate(changed).Hash, Is.True);
        }

        [Test]
        public void RepeatedCalculation_IsDeterministic()
        {
            Assert.That(Calculate(baseline).Hash, Is.EqualTo(Calculate(baseline).Hash));
        }

        [Test]
        public void SourceProvisionOrder_DoesNotChangeHash()
        {
            var reversed = CreateFixture(spec => spec.ReverseSources = true);
            AssertSameHash(baseline, reversed);
        }

        [Test]
        public void CsvRowOrder_DoesNotChangeHash()
        {
            var reversed = CreateFixture(spec => spec.Rows["world_profiles.csv"].Reverse());
            AssertSameHash(baseline, reversed);
        }

        [Test]
        public void PrimaryKeyOrdering_IsIndependentOfRecordNumber()
        {
            var reversed = CreateFixture(spec => spec.Rows["world_profiles.csv"].Reverse());
            var firstRecords = baseline.Registry.Records.Where(item =>
                item.FileName == "world_profiles.csv").ToArray();
            var secondRecords = reversed.Registry.Records.Where(item =>
                item.FileName == "world_profiles.csv").ToArray();
            Assert.That(firstRecords[0].SourceRecord.Fields[0].Value.IdValue,
                Is.Not.EqualTo(secondRecords[0].SourceRecord.Fields[0].Value.IdValue));
            AssertSameHash(baseline, reversed);
        }

        [Test]
        public void CrLfAndLf_DoNotChangeHash()
        {
            AssertSameHash(baseline, CreateFixture(spec => spec.LineEnding = "\r\n"));
        }

        [Test]
        public void Utf8Bom_DoesNotChangeHash()
        {
            AssertSameHash(baseline, CreateFixture(spec => spec.IncludeBom = true));
        }

        [Test]
        public void RawCsvQuoting_DoesNotChangeHash()
        {
            AssertSameHash(baseline, CreateFixture(spec => spec.QuoteAllValues = true));
        }

        [Test]
        public void FloatNegativeZero_NormalizesToZero()
        {
            var zero = ChangedValue("number", "0");
            var negativeZero = ChangedValue("number", "-0");
            AssertSameHash(zero, negativeZero);
        }

        [Test]
        public void NumericFormatting_IsInvariantAcrossCurrentCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                var french = Calculate(baseline).Hash;
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var turkish = Calculate(baseline).Hash;
                Assert.That(french, Is.EqualTo(turkish));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void ListOrder_ChangesHash()
        {
            AssertDifferentHash(baseline, ChangedValue("kinds", "ENUM_B|ENUM_A"));
        }

        [Test]
        public void DuplicateListItem_ChangesHash()
        {
            AssertDifferentHash(baseline, ChangedValue("ids", "ID1"));
        }

        [Test]
        public void EmptyListDiffersFromOneItemList()
        {
            var empty = ChangedValue("ids", string.Empty);
            var oneItem = ChangedValue("ids", "ID1");
            AssertDifferentHash(empty, oneItem);
        }

        [Test]
        public void StringCase_ChangesHash()
        {
            AssertDifferentHash(baseline, ChangedValue("text", "textA"));
        }

        [Test]
        public void StringWhitespace_ChangesHash()
        {
            AssertDifferentHash(baseline, ChangedValue("text", " TextA "));
        }

        [Test]
        public void UnicodeNormalizationForm_ChangesHash()
        {
            var composed = ChangedValue("text", "é");
            var decomposed = ChangedValue("text", "e\u0301");
            AssertDifferentHash(composed, decomposed);
        }

        [Test]
        public void GeneratedSchemaContent_IsExcluded()
        {
            var changed = CreateFixture(spec =>
                spec.Schemas["generated_world_sectors.csv"][0].Name = "changed_output_id");
            AssertSameHash(baseline, changed);
        }

        [Test]
        public void StaticFilenameAssociation_ChangesHash()
        {
            var moved = CreateFixture(spec =>
            {
                var columns = spec.Schemas["world_profiles.csv"];
                var rows = spec.Rows["world_profiles.csv"];
                spec.Schemas["world_profiles.csv"] = DefaultColumns();
                spec.Rows["world_profiles.csv"] = new List<string[]>();
                spec.Schemas["village_profiles.csv"] = columns;
                spec.Rows["village_profiles.csv"] = rows;
            });
            AssertDifferentHash(baseline, moved);
        }

        [Test]
        public void SchemaColumnName_ChangesHash()
        {
            var changed = CreateFixture(spec => Column(spec, "notes").Name = "memo");
            AssertDifferentHash(baseline, changed);
        }

        [Test]
        public void SchemaTypeToken_ChangesHash()
        {
            var changed = CreateFixture(spec =>
            {
                var column = Column(spec, "text");
                column.DataType = "ENUM";
                column.AllowedValues = "TextA|TextB";
            });
            AssertDifferentHash(baseline, changed);
        }

        [TestCase("text", "TextX")]
        [TestCase("integer", "-8")]
        [TestCase("unsigned", "43")]
        [TestCase("number", "1.5")]
        [TestCase("flag", "0")]
        [TestCase("kind", "ENUM_B")]
        [TestCase("ids", "ID1|ID2")]
        [TestCase("kinds", "ENUM_B|ENUM_A")]
        [TestCase("ints", "1|-3")]
        [TestCase("hex", "0x0B")]
        [TestCase("timestamp", "2026-01-02T03:04:06Z")]
        [TestCase("active", "0")]
        [TestCase("notes", "NoteX")]
        public void EverySemanticFieldChange_ChangesHash(string columnName, string value)
        {
            AssertDifferentHash(baseline, ChangedValue(columnName, value));
        }

        [Test]
        public void NullRegistry_ReportsMissingRegistry()
        {
            AssertError(new ContentVersionHashCalculator().Calculate(
                null, baseline.SourceSet, baseline.Catalog), ContentVersionHashErrorCode.MissingRegistry);
        }

        [Test]
        public void NullSourceSet_ReportsMissingSourceSet()
        {
            AssertError(new ContentVersionHashCalculator().Calculate(
                baseline.Registry, null, baseline.Catalog), ContentVersionHashErrorCode.MissingSourceSet);
        }

        [Test]
        public void ForeignCatalogInstance_ReportsCatalogMismatch()
        {
            AssertError(new ContentVersionHashCalculator().Calculate(
                baseline.Registry, baseline.SourceSet, secondIdentity.Catalog),
                ContentVersionHashErrorCode.CatalogMismatch);
        }

        [Test]
        public void MissingStaticSource_ReportsSourceInventoryMismatch()
        {
            var sources = baseline.SourceSet.Sources.Skip(1).ToArray();
            var sourceSet = new ForeignKeySourceSet(baseline.Catalog, sources);
            AssertError(new ContentVersionHashCalculator().Calculate(
                baseline.Registry, sourceSet, baseline.Catalog),
                ContentVersionHashErrorCode.SourceInventoryMismatch);
        }

        [Test]
        public void DuplicateStaticSource_ReportsSourceInventoryMismatch()
        {
            var sources = baseline.SourceSet.Sources.Concat(
                new[] { baseline.SourceSet.Sources[0] }).ToArray();
            var sourceSet = new ForeignKeySourceSet(baseline.Catalog, sources);
            AssertError(new ContentVersionHashCalculator().Calculate(
                baseline.Registry, sourceSet, baseline.Catalog),
                ContentVersionHashErrorCode.SourceInventoryMismatch);
        }

        [Test]
        public void ForeignParsedRecordInstances_ReportRecordIdentityMismatch()
        {
            AssertError(new ContentVersionHashCalculator().Calculate(
                secondIdentity.Registry, baseline.SourceSet, baseline.Catalog),
                ContentVersionHashErrorCode.RecordIdentityMismatch);
        }

        [Test]
        public void ForeignParsedSchemaFields_ReportSchemaMismatch()
        {
            var foreignWorld = secondIdentity.SourceSet.Sources.Single(item =>
                item.FileName == "world_profiles.csv");
            var sources = baseline.SourceSet.Sources.Select(item =>
                item.FileName == "world_profiles.csv"
                    ? new ForeignKeySourceSet.Source(item.Schema, foreignWorld.ParseResult)
                    : item).ToArray();
            var sourceSet = new ForeignKeySourceSet(baseline.Catalog, sources);
            AssertError(new ContentVersionHashCalculator().Calculate(
                baseline.Registry, sourceSet, baseline.Catalog),
                ContentVersionHashErrorCode.SchemaMismatch);
        }

        [Test]
        public void NaN_ReportsUnsupportedValueAndNoDigest()
        {
            var fixture = TamperedValue("number", CsvSchemaDataType.Float, float.NaN);
            AssertError(Calculate(fixture), ContentVersionHashErrorCode.UnsupportedValue);
        }

        [Test]
        public void Infinity_ReportsUnsupportedValueAndNoDigest()
        {
            var fixture = TamperedValue("number", CsvSchemaDataType.Float, float.PositiveInfinity);
            AssertError(Calculate(fixture), ContentVersionHashErrorCode.UnsupportedValue);
        }

        [Test]
        public void InvalidUtf16_ReportsUnsupportedValueAndNoDigest()
        {
            var fixture = TamperedValue("text", CsvSchemaDataType.String, "\ud800");
            AssertError(Calculate(fixture), ContentVersionHashErrorCode.UnsupportedValue);
        }

        [Test]
        public void DuplicateCanonicalPrimaryKey_ReportsErrorAndNoDigest()
        {
            var fixture = CreateFixture(spec =>
            {
                var id = Column(spec, "id");
                id.DataType = "DATETIME";
                spec.Rows["world_profiles.csv"][0][0] = "2026-01-02T03:04:05Z";
                spec.Rows["world_profiles.csv"][1][0] = "2026-01-02T03:04:05.0Z";
            });
            AssertError(Calculate(fixture),
                ContentVersionHashErrorCode.DuplicateCanonicalPrimaryKey);
        }

        [Test]
        public void Failure_PublishesNoHash()
        {
            var result = new ContentVersionHashCalculator().Calculate(null, null, null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Hash, Is.Null);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public void GateErrors_AreDeterministicallySorted()
        {
            var errors = new ContentVersionHashCalculator().Calculate(null, null, null).Errors;
            Assert.That(errors.Select(item => item.ErrorCode), Is.EqualTo(new[]
            {
                ContentVersionHashErrorCode.MissingRegistry,
                ContentVersionHashErrorCode.MissingSourceSet,
                ContentVersionHashErrorCode.CatalogMismatch
            }));
        }

        [Test]
        public void FailureErrorView_IsReadOnly()
        {
            var errors = (IList<ContentVersionHashError>)new ContentVersionHashCalculator()
                .Calculate(null, null, null).Errors;
            Assert.Throws<NotSupportedException>(() => errors.Clear());
        }

        private static ContentVersionHashResult Calculate(Fixture fixture) =>
            new ContentVersionHashCalculator().Calculate(
                fixture.Registry, fixture.SourceSet, fixture.Catalog);

        private Fixture ChangedValue(string columnName, string value) =>
            CreateFixture(spec =>
            {
                var index = spec.Schemas["world_profiles.csv"].ToList()
                    .FindIndex(item => item.Name == columnName);
                spec.Rows["world_profiles.csv"][0][index] = value;
            });

        private Fixture TamperedValue(
            string columnName,
            CsvSchemaDataType dataType,
            object payload)
        {
            var worldSource = baseline.SourceSet.Sources.Single(item =>
                item.FileName == "world_profiles.csv");
            var original = worldSource.ParseResult.Records[0];
            var fields = original.Fields.ToArray();
            var index = worldSource.Schema.Columns.ToList()
                .FindIndex(item => item.ColumnName == columnName);
            var value = (CsvParsedValue)Activator.CreateInstance(
                typeof(CsvParsedValue),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { (object)dataType, false, payload },
                CultureInfo.InvariantCulture);
            fields[index] = Construct<CsvParsedField>(fields[index].ValidatedField, value);
            var record = Construct<CsvParsedRecord>(original.ValidatedRecord, fields);
            var parseResult = Construct<CsvScalarAndListParseResult>(
                new[] { record, worldSource.ParseResult.Records[1] },
                Array.Empty<CsvValueParseError>());
            var sources = baseline.SourceSet.Sources.Select(item =>
                item.FileName == "world_profiles.csv"
                    ? new ForeignKeySourceSet.Source(item.Schema, parseResult)
                    : item).ToList();
            return BuildFixture(baseline.Catalog, sources);
        }

        private static Fixture CreateFixture(Action<DataSpec> mutate = null)
        {
            var spec = DataSpec.Create();
            mutate?.Invoke(spec);
            var catalog = BuildCatalog(spec.Schemas);
            Assert.That(catalog.FileCount, Is.EqualTo(60));
            var sources = new List<ForeignKeySourceSet.Source>();
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                var schema = catalog.GetFile(fileName);
                var rows = spec.Rows[fileName];
                var text = Header(schema);
                foreach (var row in rows)
                {
                    text += spec.LineEnding + string.Join(",", row.Select(value =>
                        CsvCell(value, spec.QuoteAllValues)));
                }

                var bytes = new UTF8Encoding(spec.IncludeBom, true).GetBytes(text);
                sources.Add(new ForeignKeySourceSet.Source(schema, Parse(schema, bytes)));
            }

            if (spec.ReverseSources) sources.Reverse();
            return BuildFixture(catalog, sources);
        }

        private static Fixture BuildFixture(
            CsvSchemaCatalog catalog,
            List<ForeignKeySourceSet.Source> sources)
        {
            var sourceSet = new ForeignKeySourceSet(catalog, sources);
            var resolution = new ForeignKeyResolver().Resolve(sourceSet);
            Assert.That(resolution.Success, Is.True,
                string.Join("\n", resolution.Errors.Select(item => item.Message)));
            var input = new StaticDataRegistryInput(
                EmptyWorld(), EmptyBiome(), EmptySpecial(), EmptyMicro(), resolution);
            var built = new StaticDataRegistryBuilder().Build(input);
            Assert.That(built.Success, Is.True,
                string.Join("\n", built.Errors.Select(item => item.Message)));
            return new Fixture(catalog, sourceSet, built.Registry);
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
                        string.Empty,
                        string.Empty,
                        sourceRow++));
                }
            }

            var result = new CsvSchemaCatalogBuilder().Build(rows);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return result.Catalog;
        }

        private static CsvScalarAndListParseResult Parse(CsvFileSchema schema, byte[] bytes)
        {
            var read = new Rfc4180CsvReader().Read(bytes, schema.FileName);
            Assert.That(read.Success, Is.True, string.Join("\n", read.Errors));
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

        private static string CsvCell(string value, bool forceQuote)
        {
            return !forceQuote && value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static ColumnSpec Column(DataSpec spec, string name) =>
            spec.Schemas["world_profiles.csv"].Single(item => item.Name == name);

        private static ColumnSpec[] DefaultColumns() =>
            new[] { new ColumnSpec("id", "ID", true, 1) };

        private static WorldRouteDefinitionSet EmptyWorld() =>
            Construct<WorldRouteDefinitionSet>(
                Array.Empty<WorldProfileDefinition>(), Array.Empty<GenerationProfileDefinition>(),
                Array.Empty<GenerationPassDefinition>(), Array.Empty<RngStreamDefinition>(),
                Array.Empty<SectorRouteMaskDefinition>(), Array.Empty<SocketBandDefinition>(),
                Array.Empty<EdgeSignatureDefinition>(), Array.Empty<EdgeSignatureCompatibilityDefinition>(),
                Array.Empty<SectorRecipeDefinition>(), Array.Empty<SectorRecipeCellDefinition>(),
                Array.Empty<SectorRecipePathDefinition>(), Array.Empty<SectorExternalSocketDefinition>(),
                Array.Empty<SectorRecipePoolEntryDefinition>());

        private static BiomeBoundaryDefinitionSet EmptyBiome() =>
            Construct<BiomeBoundaryDefinitionSet>(
                Array.Empty<BiomeTypeDefinition>(), Array.Empty<BiomePatchRuleDefinition>(),
                Array.Empty<BiomeBoundaryProfileDefinition>(), Array.Empty<BiomeBoundaryPairRuleDefinition>(),
                Array.Empty<BoundaryChunkDefinition>());

        private static SpecialVillageDefinitionSet EmptySpecial() =>
            Construct<SpecialVillageDefinitionSet>(
                Array.Empty<EventActivationRouteDefinition>(), Array.Empty<SpecialMapDefinition>(),
                Array.Empty<SpecialMapEntrySocketDefinition>(), Array.Empty<SpecialMapFootprintCellDefinition>(),
                Array.Empty<SpecialMapRewardDefinition>(), Array.Empty<ShopArchetypeDefinition>(),
                Array.Empty<ShopInventoryRuleDefinition>(), Array.Empty<ShopkeeperSpeciesDefinition>(),
                Array.Empty<VillageFacilityDefinition>(), Array.Empty<VillageLayoutDefinition>(),
                Array.Empty<VillageLayoutCellDefinition>(), Array.Empty<VillageProfileDefinition>());

        private static MicrochunkPopulationItemDefinitionSet EmptyMicro() =>
            Construct<MicrochunkPopulationItemDefinitionSet>(
                Array.Empty<MapElementDefinition>(), Array.Empty<MapElementInteractionDefinition>(),
                Array.Empty<MicrochunkDefinition>(), Array.Empty<MicrochunkObjectSlotDefinition>(),
                Array.Empty<MicrochunkPoolEntryDefinition>(), Array.Empty<MicrochunkSocketDefinition>(),
                Array.Empty<MicrochunkTileCellDefinition>(), Array.Empty<MicrochunkVariantRuleDefinition>(),
                Array.Empty<PopulationProfileDefinition>(), Array.Empty<PrefabRegistryDefinition>(),
                Array.Empty<ResourceDefinition>(), Array.Empty<ResourceSpawnRuleDefinition>(),
                Array.Empty<SpawnPoolEntryDefinition>(), Array.Empty<SpecialItemSlotDefinition>(),
                Array.Empty<TileCodeDefinition>(), Array.Empty<ToolUpgradeDefinition>());

        private static T Construct<T>(params object[] arguments) =>
            (T)Activator.CreateInstance(
                typeof(T), BindingFlags.Instance | BindingFlags.NonPublic,
                null, arguments, CultureInfo.InvariantCulture);

        private static void AssertSameHash(Fixture left, Fixture right)
        {
            Assert.That(Calculate(left).Hash, Is.EqualTo(Calculate(right).Hash));
        }

        private static void AssertDifferentHash(Fixture left, Fixture right)
        {
            Assert.That(Calculate(left).Hash, Is.Not.EqualTo(Calculate(right).Hash));
        }

        private static void AssertError(
            ContentVersionHashResult result,
            ContentVersionHashErrorCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Hash, Is.Null);
            Assert.That(result.Errors.Any(item => item.ErrorCode == code), Is.True,
                FormatErrors(result));
        }

        private static string FormatErrors(ContentVersionHashResult result) =>
            string.Join("\n", result.Errors.Select(item =>
                item.ErrorCode + " " + item.FileName + " " + item.RecordNumber + " " +
                item.FieldName + " " + item.Message));

        private sealed class Fixture
        {
            public Fixture(
                CsvSchemaCatalog catalog,
                ForeignKeySourceSet sourceSet,
                StaticDataRegistry registry)
            {
                Catalog = catalog;
                SourceSet = sourceSet;
                Registry = registry;
            }

            public CsvSchemaCatalog Catalog { get; }
            public ForeignKeySourceSet SourceSet { get; }
            public StaticDataRegistry Registry { get; }
        }

        private sealed class DataSpec
        {
            private static readonly string[] GeneratedFiles =
            {
                "seed_manifest.csv", "generated_world_sectors.csv", "generated_world_edges.csv",
                "generated_biome_patches.csv", "generated_route_completion_path.csv",
                "generated_special_sites.csv", "generated_sector_microchunks.csv",
                "generated_spawns.csv", "generated_validation_results.csv",
                "generated_village_facilities.csv", "generated_tile_modifications_debug.csv"
            };

            private DataSpec(
                Dictionary<string, ColumnSpec[]> schemas,
                Dictionary<string, List<string[]>> rows)
            {
                Schemas = schemas;
                Rows = rows;
                LineEnding = "\n";
            }

            public Dictionary<string, ColumnSpec[]> Schemas { get; }
            public Dictionary<string, List<string[]>> Rows { get; }
            public string LineEnding { get; set; }
            public bool IncludeBom { get; set; }
            public bool QuoteAllValues { get; set; }
            public bool ReverseSources { get; set; }

            public static DataSpec Create()
            {
                var schemas = ForeignKeySourceSet.ExpectedFileNames.ToDictionary(
                    fileName => fileName,
                    fileName => DefaultColumns(),
                    StringComparer.Ordinal);
                var rows = ForeignKeySourceSet.ExpectedFileNames.ToDictionary(
                    fileName => fileName,
                    fileName => new List<string[]>(),
                    StringComparer.Ordinal);
                schemas["world_profiles.csv"] = new[]
                {
                    new ColumnSpec("id", "ID", true, 1),
                    new ColumnSpec("text", "STRING", false),
                    new ColumnSpec("integer", "INT", false),
                    new ColumnSpec("unsigned", "ULONG", false),
                    new ColumnSpec("number", "FLOAT", false),
                    new ColumnSpec("flag", "BOOL", false),
                    new ColumnSpec("kind", "ENUM", false, allowedValues: "ENUM_A|ENUM_B"),
                    new ColumnSpec("ids", "ID_LIST", false),
                    new ColumnSpec("kinds", "ENUM_LIST", false, allowedValues: "ENUM_A|ENUM_B"),
                    new ColumnSpec("ints", "INT_LIST", false),
                    new ColumnSpec("hex", "HEX", false),
                    new ColumnSpec("timestamp", "DATETIME", false),
                    new ColumnSpec("active", "BOOL", false),
                    new ColumnSpec("notes", "STRING", false)
                };
                rows["world_profiles.csv"].Add(new[]
                {
                    "A", "TextA", "-7", "42", "1.25", "1", "ENUM_A", "ID1|ID1",
                    "ENUM_A|ENUM_B", "1|-2", "0x0A", "2026-01-02T03:04:05Z", "1", "NoteA"
                });
                rows["world_profiles.csv"].Add(new[]
                {
                    "B", "TextB", "0", "0", "-0", "0", "ENUM_B", "ID2",
                    "ENUM_B", "0", "0x00", "2026-01-02T03:04:06.5Z", "0", "NoteB"
                });
                foreach (var fileName in GeneratedFiles)
                {
                    schemas.Add(fileName, DefaultColumns());
                }

                return new DataSpec(schemas, rows);
            }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(
                string name,
                string dataType,
                bool required,
                int? primaryKeyOrder = null,
                string allowedValues = "")
            {
                Name = name;
                DataType = dataType;
                Required = required;
                PrimaryKeyOrder = primaryKeyOrder;
                AllowedValues = allowedValues;
            }

            public string Name { get; set; }
            public string DataType { get; set; }
            public bool Required { get; }
            public int? PrimaryKeyOrder { get; }
            public string AllowedValues { get; set; }
        }
    }
}
