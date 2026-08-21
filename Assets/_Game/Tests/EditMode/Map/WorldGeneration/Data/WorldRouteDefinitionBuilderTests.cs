using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public sealed class WorldRouteDefinitionBuilderTests
    {
        private static readonly FileSpec[] Specs = CreateSpecs();

        private static IEnumerable<string> FileNames => Specs.Select(spec => spec.FileName);

        private static IEnumerable<TestCaseData> CompositeFiles
        {
            get
            {
                yield return new TestCaseData("generation_passes.csv");
                yield return new TestCaseData("edge_signature_compatibility.csv");
                yield return new TestCaseData("sector_recipe_cells.csv");
                yield return new TestCaseData("sector_recipe_paths.csv");
                yield return new TestCaseData("sector_external_sockets.csv");
                yield return new TestCaseData("sector_recipe_pool_entries.csv");
            }
        }

        [Test]
        public void ExactThirteenSourcesBuildSuccessfulDefinitionSet()
        {
            var result = Build(StandardSources());

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.DefinitionSet, Is.Not.Null);
            Assert.That(TotalDefinitionCount(result.DefinitionSet), Is.EqualTo(13));
        }

        [Test]
        public void ShuffledSourceOrderProducesSameMembershipAndOrder()
        {
            var forward = Build(StandardSources()).DefinitionSet;
            var reverse = Build(StandardSources().AsEnumerable().Reverse()).DefinitionSet;

            Assert.That(Snapshot(reverse), Is.EqualTo(Snapshot(forward)));
        }

        [TestCaseSource(nameof(FileNames))]
        public void MissingExactSourceIsReported(string fileName)
        {
            var result = Build(StandardSources().Where(source => source.FileName != fileName));

            Assert.That(result.Success, Is.False);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error =>
                error.FileName == fileName &&
                error.ErrorCode == WorldRouteDefinitionBuildErrorCode.MissingSource), Is.True);
        }

        [TestCaseSource(nameof(FileNames))]
        public void EveryDefinitionMapsEveryColumnAndPreservesSourceRecord(string fileName)
        {
            var result = Build(StandardSources());

            Assert.That(result.Success, Is.True, FormatErrors(result));
            AssertFullMapping(fileName, result.DefinitionSet);
        }

        [Test]
        public void DuplicateSourceAccumulatesDeterministicErrorAndPublishesNoSet()
        {
            var sources = StandardSources();
            sources.Add(sources.Single(source => source.FileName == "world_profiles.csv"));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Count(error =>
                error.ErrorCode == WorldRouteDefinitionBuildErrorCode.DuplicateSource), Is.EqualTo(1));
        }

        [Test]
        public void UnexpectedSourceIsRejected()
        {
            var sources = StandardSources();
            sources.Add(BuildSource(new FileSpec(
                "unexpected.csv", 1, Column("id", "ID"))));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single(error => error.FileName == "unexpected.csv").ErrorCode,
                Is.EqualTo(WorldRouteDefinitionBuildErrorCode.UnexpectedSource));
        }

        [Test]
        public void UnsuccessfulParseIsRejected()
        {
            var sources = StandardSources();
            var spec = Spec("world_profiles.csv");
            var row = StandardRow(spec);
            row[2] = "not-an-int";
            Replace(sources, BuildSource(spec, new[] { row }, false));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single().ErrorCode,
                Is.EqualTo(WorldRouteDefinitionBuildErrorCode.UnsuccessfulParse));
        }

        [Test]
        public void SchemaDataTypeMismatchIsRejectedBeforeMaterialization()
        {
            var sources = StandardSources();
            var original = Spec("world_profiles.csv");
            var columns = original.Columns.ToArray();
            columns[2] = Column(columns[2].Name, "STRING");
            var mismatched = new FileSpec(original.FileName, original.PrimaryKeyCount, columns);
            Replace(sources, BuildSource(mismatched));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error =>
                error.ErrorCode == WorldRouteDefinitionBuildErrorCode.SchemaMismatch &&
                error.ColumnOrder == 3), Is.True);
        }

        [Test]
        public void ParsedFieldIdentityFromEquivalentOtherSchemaIsRejected()
        {
            var sources = StandardSources();
            var original = sources.Single(source => source.FileName == "world_profiles.csv");
            var other = BuildSource(Spec("world_profiles.csv"));
            Replace(sources, new WorldRouteDefinitionSource(original.Schema, other.ParseResult));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.All(error =>
                error.ErrorCode == WorldRouteDefinitionBuildErrorCode.FieldMappingFailed), Is.True);
            Assert.That(result.Errors.First().Location, Is.Not.Null);
            Assert.That(result.Errors.First().RecordNumber, Is.EqualTo(2));
        }

        [Test]
        public void NullInputEnumerableThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WorldRouteDefinitionBuilder().Build(null));
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
                Is.EqualTo(WorldRouteDefinitionBuildErrorCode.MissingSource));
        }

        [Test]
        public void SourceConstructorRejectsNullSchemaAndResult()
        {
            var source = BuildSource(Spec("world_profiles.csv"));

            Assert.Throws<ArgumentNullException>(() =>
                new WorldRouteDefinitionSource(null, source.ParseResult));
            Assert.Throws<ArgumentNullException>(() =>
                new WorldRouteDefinitionSource(source.Schema, null));
        }

        [Test]
        public void SevenSingleKeyDictionariesUseOrdinalLookupAndEnumeration()
        {
            var sources = StandardSources();
            foreach (var fileName in new[]
                     {
                         "world_profiles.csv", "generation_profiles.csv", "rng_streams.csv",
                         "sector_route_masks.csv", "socket_band_definitions.csv",
                         "edge_signatures.csv", "sector_recipe_catalog.csv"
                     })
            {
                var spec = Spec(fileName);
                var first = StandardRow(spec);
                var second = StandardRow(spec);
                first[0] = "Z_KEY";
                second[0] = "A_KEY";
                Replace(sources, BuildSource(spec, new[] { first, second }));
            }

            var set = Build(sources).DefinitionSet;

            Assert.That(set.WorldProfiles.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.GenerationProfiles.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.RngStreams.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.RouteMasks.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.SocketBands.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.EdgeSignatures.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.SectorRecipes.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.WorldProfiles.ContainsKey("a_key"), Is.False);
        }

        [TestCaseSource(nameof(CompositeFiles))]
        public void CompositeCollectionUsesExactDeterministicSort(string fileName)
        {
            var sources = StandardSources();
            var spec = Spec(fileName);
            var rows = CompositeRows(spec, fileName);
            Replace(sources, BuildSource(spec, rows));

            var set = Build(sources).DefinitionSet;

            AssertCompositeOrder(fileName, set);
        }

        [TestCaseSource(nameof(CompositeFiles))]
        public void ParentQueryReturnsStableReadOnlyView(string fileName)
        {
            var sources = StandardSources();
            var spec = Spec(fileName);
            Replace(sources, BuildSource(spec, CompositeRows(spec, fileName)));
            var set = Build(sources).DefinitionSet;

            var values = ParentQuery(fileName, set, "A_PARENT");

            Assert.That(values.Count, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.IList)values).Clear());
            Assert.That(ReferenceEquals(values, ParentQuery(fileName, set, "A_PARENT")), Is.True);
            Assert.That(ParentQuery(fileName, set, "UNKNOWN"), Is.Empty);
        }

        [Test]
        public void AllPublishedCollectionsAreReadOnly()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, WorldProfileDefinition>)set.WorldProfiles).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<GenerationPassDefinition>)set.GenerationPasses).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<EdgeSignatureCompatibilityDefinition>)set.EdgeSignatureCompatibilities).Clear());
        }

        [Test]
        public void NestedListPayloadsAreCopiedAndReadOnlyWithOrderAndDuplicates()
        {
            var set = Build(StandardSources()).DefinitionSet;
            var pass = set.GenerationPasses.Single();
            var edge = set.EdgeSignatures.Values.Single();

            Assert.That(pass.InputArtifacts, Is.EqualTo(new[] { "LIST_A", "LIST_B", "LIST_A" }));
            Assert.That(edge.Tags, Is.EqualTo(new[] { "LIST_A", "LIST_B", "LIST_A" }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)pass.InputArtifacts).Add("LIST_C"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)edge.Tags).Clear());
        }

        [Test]
        public void DefaultAppliedFieldValueAndProvenanceArePreserved()
        {
            var sources = StandardSources();
            var spec = Spec("world_profiles.csv");
            var columns = spec.Columns.ToArray();
            columns[2] = Column("width_tiles", "INT", "+17");
            var withDefault = new FileSpec(spec.FileName, spec.PrimaryKeyCount, columns);
            var row = StandardRow(withDefault);
            row[2] = string.Empty;
            Replace(sources, BuildSource(withDefault, new[] { row }));

            var definition = Build(sources).DefinitionSet.WorldProfiles.Values.Single();

            Assert.That(definition.WidthTiles, Is.EqualTo(17));
            Assert.That(definition.SourceRecord.Fields[2].UsedDefault, Is.True);
            Assert.That(definition.SourceRecord.Fields[2].RawValue, Is.Empty);
        }

        [Test]
        public void InactiveDefinitionsAreRetained()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.That(set.WorldProfiles.Count, Is.EqualTo(1));
            Assert.That(set.WorldProfiles.Values.Single().Active, Is.False);
            Assert.That(set.RngStreams.Values.Single().Active, Is.False);
            Assert.That(set.SectorRecipes.Values.Single().Active, Is.False);
        }

        [Test]
        public void OptionalEmptyStringIdAndEnumArePreservedExactly()
        {
            var sources = StandardSources();
            var spec = Spec("edge_signatures.csv");
            var row = StandardRow(spec);
            row[7] = string.Empty;
            row[10] = string.Empty;
            Replace(sources, BuildSource(spec, new[] { row }));

            var edge = Build(sources).DefinitionSet.EdgeSignatures.Values.Single();

            Assert.That(edge.ToolRequirement, Is.Empty);
            Assert.That(edge.Notes, Is.Empty);
            Assert.That(edge.SourceRecord.Fields[7].Value.IsEmpty, Is.True);
        }

        [Test]
        public void ForeignKeyIdsRemainStringsWithoutResolution()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.That(set.GenerationProfiles.Values.Single().WorldProfileId,
                Is.EqualTo("ID_2"));
            Assert.That(set.SectorRecipes.Values.Single().PrimaryBiomeId,
                Is.EqualTo("ID_5"));
            Assert.That(set.SectorExternalSockets.Single().EdgeSignatureId,
                Is.EqualTo("ID_8"));
        }

        [Test]
        public void DomainInvalidButTypedValuesAreNotValidatedByBuilder()
        {
            var sources = StandardSources();
            var spec = Spec("world_profiles.csv");
            var row = StandardRow(spec);
            row[2] = "-100";
            row[3] = "-200";
            Replace(sources, BuildSource(spec, new[] { row }));

            var result = Build(sources);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.DefinitionSet.WorldProfiles.Values.Single().WidthTiles,
                Is.EqualTo(-100));
        }

        [Test]
        public void MultipleErrorsAccumulateAndSortByFileRecordColumnAndCode()
        {
            var sources = StandardSources();
            sources.RemoveAll(source =>
                source.FileName == "world_profiles.csv" || source.FileName == "rng_streams.csv");
            sources.Add(BuildSource(new FileSpec("z_unexpected.csv", 1, Column("id", "ID"))));
            var duplicate = sources.Single(source => source.FileName == "generation_profiles.csv");
            sources.Add(duplicate);

            var result = Build(sources);

            Assert.That(result.Errors.Select(error => error.FileName),
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(
                WorldRouteDefinitionBuildErrorCode.MissingSource));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(
                WorldRouteDefinitionBuildErrorCode.DuplicateSource));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(
                WorldRouteDefinitionBuildErrorCode.UnexpectedSource));
            Assert.That(result.DefinitionSet, Is.Null);
        }

        [Test]
        public void BuildDoesNotModifyInputSchemaParseOrSourceModels()
        {
            var sources = StandardSources();
            var sourceRefs = sources.ToArray();
            var schemaColumns = sources.Select(source => source.Schema.Columns.ToArray()).ToArray();
            var parsedRecords = sources.Select(source => source.ParseResult.Records.ToArray()).ToArray();

            var result = Build(sources);

            Assert.That(result.Success, Is.True);
            Assert.That(sources, Is.EqualTo(sourceRefs));
            for (var index = 0; index < sources.Count; index++)
            {
                Assert.That(sources[index].Schema.Columns, Is.EqualTo(schemaColumns[index]));
                Assert.That(sources[index].ParseResult.Records, Is.EqualTo(parsedRecords[index]));
            }
        }

        [Test]
        public void HeaderOnlySourcesProduceSuccessfulEmptySet()
        {
            var sources = Specs.Select(spec => BuildSource(spec, Array.Empty<string[]>())).ToArray();

            var result = Build(sources);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(TotalDefinitionCount(result.DefinitionSet), Is.Zero);
        }

        [TestCase("edge_signatures.csv", "tags", "ID_LIST", "ENUM_LIST")]
        [TestCase("sector_recipe_cells.csv", "required_route_roles", "ID_LIST", "ENUM_LIST")]
        [TestCase("sector_recipe_cells.csv", "required_usage_class", "ENUM_LIST", "ENUM")]
        [TestCase("sector_recipe_cells.csv", "transform_policy", "ENUM_LIST", "ENUM")]
        [TestCase("socket_band_definitions.csv", "recommended_center", "FLOAT", "INT")]
        public void RepairV13AuthoritativeTypeIsAcceptedAndNearMissRejected(
            string fileName,
            string columnName,
            string authoritativeType,
            string nearMissType)
        {
            var accepted = Build(StandardSources());
            Assert.That(accepted.Success, Is.True, FormatErrors(accepted));

            var sources = StandardSources();
            var original = Spec(fileName);
            var columns = original.Columns.ToArray();
            var columnIndex = Array.FindIndex(columns, column => column.Name == columnName);
            Assert.That(columnIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(columns[columnIndex].DataType, Is.EqualTo(authoritativeType));
            columns[columnIndex] = Column(columnName, nearMissType);
            Replace(sources, BuildSource(new FileSpec(fileName, original.PrimaryKeyCount, columns)));

            var rejected = Build(sources);

            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.DefinitionSet, Is.Null);
            Assert.That(rejected.Errors.Any(error =>
                error.FileName == fileName &&
                error.ColumnOrder == columnIndex + 1 &&
                error.ErrorCode == WorldRouteDefinitionBuildErrorCode.SchemaMismatch), Is.True);
        }

        [TestCase("edge_signatures.csv", "tags")]
        [TestCase("sector_recipe_cells.csv", "required_usage_class")]
        [TestCase("sector_recipe_cells.csv", "required_route_roles")]
        [TestCase("sector_recipe_cells.csv", "transform_policy")]
        public void RepairV13ListMaterializationPreservesOrderDuplicatesAndImmutability(
            string fileName,
            string columnName)
        {
            var sources = StandardSources();
            var spec = Spec(fileName);
            var row = StandardRow(spec);
            var columnIndex = Array.FindIndex(spec.Columns.ToArray(), column => column.Name == columnName);
            row[columnIndex] = columnName == "transform_policy"
                ? "R0|MIRROR_X|R0"
                : "CUSTOM_A|CUSTOM_B|CUSTOM_A";
            Replace(sources, BuildSource(spec, new[] { row }));

            var list = RepairV13List(Build(sources).DefinitionSet, fileName, columnName);

            Assert.That(list, Is.EqualTo(row[columnIndex].Split('|')));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)list).Add("CUSTOM_C"));
        }

        [TestCase("edge_signatures.csv", "tags")]
        [TestCase("sector_recipe_cells.csv", "required_usage_class")]
        [TestCase("sector_recipe_cells.csv", "required_route_roles")]
        public void RepairV13OptionalListsMaterializeAsImmutableEmptyLists(
            string fileName,
            string columnName)
        {
            var sources = StandardSources();
            var spec = Spec(fileName);
            var row = StandardRow(spec);
            var columnIndex = Array.FindIndex(spec.Columns.ToArray(), column => column.Name == columnName);
            row[columnIndex] = string.Empty;
            Replace(sources, BuildSource(spec, new[] { row }));

            var list = RepairV13List(Build(sources).DefinitionSet, fileName, columnName);

            Assert.That(list, Is.Empty);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)list).Add("CUSTOM_A"));
        }

        [Test]
        public void RepairV13RecommendedCenterPreservesFractionalValue()
        {
            var sources = StandardSources();
            var spec = Spec("socket_band_definitions.csv");
            var row = StandardRow(spec);
            row[4] = "12.5";
            Replace(sources, BuildSource(spec, new[] { row }));

            var band = Build(sources).DefinitionSet.SocketBands.Values.Single();

            Assert.That(band.RecommendedCenter, Is.EqualTo(12.5f));
            Assert.That(band.SourceRecord.Fields[4].Value.FloatValue, Is.EqualTo(12.5f));
        }

        [Test]
        public void RepairV13TransformPolicyRejectsUnknownToken()
        {
            var sources = StandardSources();
            var spec = Spec("sector_recipe_cells.csv");
            var row = StandardRow(spec);
            row[13] = "R0|UNKNOWN";
            Replace(sources, BuildSource(spec, new[] { row }, false));

            var result = Build(sources);

            Assert.That(result.Success, Is.False);
            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error =>
                error.FileName == spec.FileName &&
                error.ErrorCode == WorldRouteDefinitionBuildErrorCode.UnsuccessfulParse), Is.True);
        }

        private static List<WorldRouteDefinitionSource> StandardSources()
        {
            return Specs.Select(spec => BuildSource(spec)).ToList();
        }

        private static IReadOnlyList<string> RepairV13List(
            WorldRouteDefinitionSet set,
            string fileName,
            string columnName)
        {
            if (fileName == "edge_signatures.csv" && columnName == "tags")
            {
                return set.EdgeSignatures.Values.Single().Tags;
            }

            var cell = set.SectorRecipeCells.Single();
            switch (columnName)
            {
                case "required_usage_class": return cell.RequiredUsageClass;
                case "required_route_roles": return cell.RequiredRouteRoles;
                case "transform_policy": return cell.TransformPolicy;
                default: throw new ArgumentOutOfRangeException(nameof(columnName), columnName, null);
            }
        }

        private static WorldRouteDefinitionBuildResult Build(
            IEnumerable<WorldRouteDefinitionSource> sources)
        {
            return new WorldRouteDefinitionBuilder().Build(sources);
        }

        private static WorldRouteDefinitionSource BuildSource(
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
                index < spec.PrimaryKeyCount ? (index + 1).ToString(CultureInfo.InvariantCulture) : string.Empty,
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
            return new WorldRouteDefinitionSource(schema, parsed);
        }

        private static string[] StandardRow(FileSpec spec)
        {
            return spec.Columns.Select((column, index) => Value(column, index)).ToArray();
        }

        private static string Value(ColumnSpec column, int index)
        {
            var allowed = column.AllowedValues.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (column.DataType == "ENUM" && allowed.Length > 0)
            {
                return allowed[0];
            }

            if (column.DataType == "ENUM_LIST" && allowed.Length > 0)
            {
                return allowed.Length > 1
                    ? allowed[0] + "|" + allowed[1] + "|" + allowed[0]
                    : allowed[0];
            }

            switch (column.DataType)
            {
                case "STRING": return "TEXT_" + (index + 1);
                case "ID": return "ID_" + (index + 1);
                case "INT": return (index + 1).ToString(CultureInfo.InvariantCulture);
                case "FLOAT": return "0.25";
                case "BOOL": return "0";
                case "ENUM": return "ENUM_A";
                case "ID_LIST": return "LIST_A|LIST_B|LIST_A";
                case "ENUM_LIST": return "ENUM_A|ENUM_B|ENUM_A";
                case "HEX": return "0x0A";
                default: throw new ArgumentOutOfRangeException(nameof(column.DataType), column.DataType, null);
            }
        }

        private static IReadOnlyList<string[]> CompositeRows(FileSpec spec, string fileName)
        {
            var first = StandardRow(spec);
            var second = StandardRow(spec);
            var third = StandardRow(spec);
            first[0] = "Z_PARENT";
            second[0] = "A_PARENT";
            third[0] = "A_PARENT";
            SetCompositeTieBreakers(fileName, first, 3, "Z_ITEM");
            SetCompositeTieBreakers(fileName, second, 2, "Z_ITEM");
            SetCompositeTieBreakers(fileName, third, 1, "A_ITEM");
            return new[] { first, second, third };
        }

        private static void SetCompositeTieBreakers(
            string fileName,
            string[] row,
            int order,
            string itemId)
        {
            switch (fileName)
            {
                case "generation_passes.csv":
                    row[1] = order.ToString(CultureInfo.InvariantCulture);
                    row[2] = itemId;
                    break;
                case "edge_signature_compatibility.csv":
                    row[1] = itemId;
                    break;
                case "sector_recipe_cells.csv":
                    row[1] = order.ToString(CultureInfo.InvariantCulture);
                    row[2] = order.ToString(CultureInfo.InvariantCulture);
                    break;
                case "sector_recipe_paths.csv":
                    row[1] = itemId;
                    row[2] = order.ToString(CultureInfo.InvariantCulture);
                    break;
                case "sector_external_sockets.csv":
                    row[1] = itemId;
                    break;
                case "sector_recipe_pool_entries.csv":
                    row[1] = order.ToString(CultureInfo.InvariantCulture);
                    row[2] = itemId;
                    break;
            }
        }

        private static void AssertCompositeOrder(string fileName, WorldRouteDefinitionSet set)
        {
            switch (fileName)
            {
                case "generation_passes.csv":
                    Assert.That(set.GenerationPasses.Select(item => item.GenerationProfileId),
                        Is.EqualTo(new[] { "A_PARENT", "A_PARENT", "Z_PARENT" }));
                    Assert.That(set.GenerationPasses.Select(item => item.PassOrder), Is.EqualTo(new[] { 1, 2, 3 }));
                    break;
                case "edge_signature_compatibility.csv":
                    Assert.That(set.EdgeSignatureCompatibilities.Select(item => item.SignatureB),
                        Is.EqualTo(new[] { "A_ITEM", "Z_ITEM", "Z_ITEM" }));
                    break;
                case "sector_recipe_cells.csv":
                    Assert.That(set.SectorRecipeCells.Select(item => item.ChunkX), Is.EqualTo(new[] { 1, 2, 3 }));
                    break;
                case "sector_recipe_paths.csv":
                    Assert.That(set.SectorRecipePaths.Select(item => item.PathId),
                        Is.EqualTo(new[] { "A_ITEM", "Z_ITEM", "Z_ITEM" }));
                    break;
                case "sector_external_sockets.csv":
                    Assert.That(set.SectorExternalSockets.Select(item => item.SocketId),
                        Is.EqualTo(new[] { "A_ITEM", "Z_ITEM", "Z_ITEM" }));
                    break;
                case "sector_recipe_pool_entries.csv":
                    Assert.That(set.SectorRecipePoolEntries.Select(item => item.EntryOrder),
                        Is.EqualTo(new[] { 1, 2, 3 }));
                    break;
            }
        }

        private static System.Collections.IList ParentQuery(
            string fileName,
            WorldRouteDefinitionSet set,
            string parentId)
        {
            switch (fileName)
            {
                case "generation_passes.csv": return (System.Collections.IList)set.GetGenerationPasses(parentId);
                case "edge_signature_compatibility.csv": return (System.Collections.IList)set.GetEdgeSignatureCompatibilities(parentId);
                case "sector_recipe_cells.csv": return (System.Collections.IList)set.GetSectorRecipeCells(parentId);
                case "sector_recipe_paths.csv": return (System.Collections.IList)set.GetSectorRecipePaths(parentId);
                case "sector_external_sockets.csv": return (System.Collections.IList)set.GetSectorExternalSockets(parentId);
                case "sector_recipe_pool_entries.csv": return (System.Collections.IList)set.GetSectorRecipePoolEntries(parentId);
                default: throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
            }
        }

        private static void AssertFullMapping(string fileName, WorldRouteDefinitionSet set)
        {
            switch (fileName)
            {
                case "world_profiles.csv": AssertWorld(set.WorldProfiles.Values.Single()); break;
                case "generation_profiles.csv": AssertGeneration(set.GenerationProfiles.Values.Single()); break;
                case "generation_passes.csv": AssertPass(set.GenerationPasses.Single()); break;
                case "rng_streams.csv": AssertRng(set.RngStreams.Values.Single()); break;
                case "sector_route_masks.csv": AssertMask(set.RouteMasks.Values.Single()); break;
                case "socket_band_definitions.csv": AssertBand(set.SocketBands.Values.Single()); break;
                case "edge_signatures.csv": AssertEdge(set.EdgeSignatures.Values.Single()); break;
                case "edge_signature_compatibility.csv": AssertCompatibility(set.EdgeSignatureCompatibilities.Single()); break;
                case "sector_recipe_catalog.csv": AssertRecipe(set.SectorRecipes.Values.Single()); break;
                case "sector_recipe_cells.csv": AssertCell(set.SectorRecipeCells.Single()); break;
                case "sector_recipe_paths.csv": AssertPath(set.SectorRecipePaths.Single()); break;
                case "sector_external_sockets.csv": AssertSocket(set.SectorExternalSockets.Single()); break;
                case "sector_recipe_pool_entries.csv": AssertPoolEntry(set.SectorRecipePoolEntries.Single()); break;
                default: throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
            }
        }

        private static void AssertWorld(WorldProfileDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.WorldProfileId, Is.EqualTo(S(r, 0))); Assert.That(d.DisplayNameKo, Is.EqualTo(S(r, 1)));
            Assert.That(new[] { d.WidthTiles, d.HeightTiles, d.SectorWidthTiles, d.SectorHeightTiles, d.SectorCols, d.SectorRows,
                d.MicroWidthTiles, d.MicroHeightTiles, d.MicroColsPerSector, d.MicroRowsPerSector, d.MinCompletionDistanceTiles,
                d.MaxShortestCompletionDistanceTiles, d.NormalCompletionMinTiles, d.NormalCompletionMaxTiles,
                d.OptionalCompletionMaxTiles }, Is.EqualTo(Enumerable.Range(2, 15).Select(i => I(r, i))));
            Assert.That(d.MaxRevisitRatio, Is.EqualTo(F(r, 17))); Assert.That(d.RequiredVillageCount, Is.EqualTo(I(r, 18)));
            Assert.That(d.Active, Is.EqualTo(B(r, 19))); Assert.That(d.Notes, Is.EqualTo(S(r, 20)));
        }

        private static void AssertGeneration(GenerationProfileDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.GenerationProfileId, Is.EqualTo(S(r, 0))); Assert.That(d.WorldProfileId, Is.EqualTo(S(r, 1)));
            Assert.That(new[] { d.MandatorySectorMin, d.MandatorySectorMax, d.Type0SectorMin, d.Type0SectorMax,
                d.ReservedSectorMin, d.ReservedSectorMax, d.InactiveSectorMin, d.InactiveSectorMax, d.StartEdgeRingMin,
                d.StartEdgeRingMax, d.MandatoryLoopMin, d.MandatoryLoopMax, d.OptionalRegionDepthMin,
                d.OptionalRegionDepthMax, d.OptionalRegionCountMin, d.OptionalRegionCountMax, d.SiteReservationRetryMax,
                d.BiomeRetryMax, d.RouteRetryMax, d.SectorSolveRetryMax }, Is.EqualTo(Enumerable.Range(2, 20).Select(i => I(r, i))));
            Assert.That(d.Active, Is.EqualTo(B(r, 22))); Assert.That(d.Notes, Is.EqualTo(S(r, 23)));
        }

        private static void AssertPass(GenerationPassDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.GenerationProfileId, Is.EqualTo(S(r, 0))); Assert.That(d.PassOrder, Is.EqualTo(I(r, 1)));
            Assert.That(d.PassId, Is.EqualTo(S(r, 2))); Assert.That(d.ClassName, Is.EqualTo(S(r, 3))); Assert.That(d.RngStreamId, Is.EqualTo(S(r, 4)));
            Assert.That(d.InputArtifacts, Is.EqualTo(L(r, 5))); Assert.That(d.OutputArtifacts, Is.EqualTo(L(r, 6)));
            Assert.That(d.FailurePolicy, Is.EqualTo(S(r, 7))); Assert.That(d.MaxRetryCount, Is.EqualTo(I(r, 8)));
            Assert.That(d.Enabled, Is.EqualTo(B(r, 9))); Assert.That(d.Notes, Is.EqualTo(S(r, 10)));
        }

        private static void AssertRng(RngStreamDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.RngStreamId, Is.EqualTo(S(r, 0))); Assert.That(d.SaltHex, Is.EqualTo(r.Fields[1].Value.HexValue));
            Assert.That(d.ResetScope, Is.EqualTo(S(r, 2))); Assert.That(d.DescriptionKo, Is.EqualTo(S(r, 3))); Assert.That(d.Active, Is.EqualTo(B(r, 4)));
        }

        private static void AssertMask(SectorRouteMaskDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.RouteMaskId, Is.EqualTo(S(r, 0))); Assert.That(d.RouteType, Is.EqualTo(I(r, 1)));
            Assert.That(new[] { d.OpenL, d.OpenR, d.OpenU, d.OpenD, d.MandatoryAllowed }, Is.EqualTo(Enumerable.Range(2, 5).Select(i => B(r, i))));
            Assert.That(d.DescriptionKo, Is.EqualTo(S(r, 7))); Assert.That(d.Active, Is.EqualTo(B(r, 8)));
        }

        private static void AssertBand(SocketBandDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.BandId, Is.EqualTo(S(r, 0))); Assert.That(d.Axis, Is.EqualTo(S(r, 1)));
            Assert.That(new[] { d.MinLocalCoord, d.MaxLocalCoord }, Is.EqualTo(Enumerable.Range(2, 2).Select(i => I(r, i))));
            Assert.That(d.RecommendedCenter, Is.EqualTo(F(r, 4)));
            Assert.That(d.MinimumClearanceTiles, Is.EqualTo(I(r, 5)));
            Assert.That(d.DescriptionKo, Is.EqualTo(S(r, 6)));
        }

        private static void AssertEdge(EdgeSignatureDefinition d)
        {
            var r = d.SourceRecord; Assert.That(new[] { d.EdgeSignatureId, d.Axis, d.BandId, d.TraversalKind }, Is.EqualTo(Enumerable.Range(0, 4).Select(i => S(r, i))));
            Assert.That(new[] { d.GroundEntryHeight, d.ClearanceWidth, d.ClearanceHeight }, Is.EqualTo(Enumerable.Range(4, 3).Select(i => I(r, i))));
            Assert.That(d.ToolRequirement, Is.EqualTo(S(r, 7))); Assert.That(d.MandatoryAllowed, Is.EqualTo(B(r, 8)));
            Assert.That(d.Tags, Is.EqualTo(L(r, 9))); Assert.That(d.Notes, Is.EqualTo(S(r, 10)));
        }

        private static void AssertCompatibility(EdgeSignatureCompatibilityDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.SignatureA, Is.EqualTo(S(r, 0))); Assert.That(d.SignatureB, Is.EqualTo(S(r, 1)));
            Assert.That(d.Compatible, Is.EqualTo(B(r, 2))); Assert.That(d.AdapterMicrochunkPoolId, Is.EqualTo(S(r, 3))); Assert.That(d.Notes, Is.EqualTo(S(r, 4)));
        }

        private static void AssertRecipe(SectorRecipeDefinition d)
        {
            var r = d.SourceRecord; Assert.That(new[] { d.SectorRecipeId, d.DisplayNameKo }, Is.EqualTo(new[] { S(r, 0), S(r, 1) }));
            Assert.That(d.RouteType, Is.EqualTo(I(r, 2))); Assert.That(new[] { d.RouteMaskId, d.PrimaryBiomeId, d.SecondaryBiomeId,
                d.BoundaryProfileId, d.RecipeKind, d.MicrochunkBudgetProfileId }, Is.EqualTo(Enumerable.Range(3, 6).Select(i => S(r, i))));
            Assert.That(d.SelectionWeight, Is.EqualTo(I(r, 9))); Assert.That(new[] { d.SupportsSpecialEntry, d.SupportsVillageEntry, d.Active },
                Is.EqualTo(Enumerable.Range(10, 3).Select(i => B(r, i)))); Assert.That(d.Notes, Is.EqualTo(S(r, 13)));
        }

        private static void AssertCell(SectorRecipeCellDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.SectorRecipeId, Is.EqualTo(S(r, 0))); Assert.That(d.ChunkX, Is.EqualTo(I(r, 1))); Assert.That(d.ChunkY, Is.EqualTo(I(r, 2)));
            Assert.That(new[] { d.CellRole, d.FixedMicrochunkId, d.MicrochunkPoolId }, Is.EqualTo(Enumerable.Range(3, 3).Select(i => S(r, i))));
            Assert.That(d.RequiredUsageClass, Is.EqualTo(L(r, 6)));
            Assert.That(d.RequiredRouteRoles, Is.EqualTo(L(r, 7))); Assert.That(d.RequiredBiomeIds, Is.EqualTo(L(r, 8)));
            Assert.That(new[] { d.RequiredSignatureL, d.RequiredSignatureR, d.RequiredSignatureU, d.RequiredSignatureD },
                Is.EqualTo(Enumerable.Range(9, 4).Select(i => S(r, i))));
            Assert.That(d.TransformPolicy, Is.EqualTo(L(r, 13)));
            Assert.That(d.Notes, Is.EqualTo(S(r, 14)));
        }

        private static void AssertPath(SectorRecipePathDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.SectorRecipeId, Is.EqualTo(S(r, 0))); Assert.That(d.PathId, Is.EqualTo(S(r, 1)));
            Assert.That(new[] { d.PathOrder, d.ChunkX, d.ChunkY }, Is.EqualTo(Enumerable.Range(2, 3).Select(i => I(r, i))));
            Assert.That(d.EnterSide, Is.EqualTo(S(r, 5))); Assert.That(d.ExitSide, Is.EqualTo(S(r, 6))); Assert.That(d.Mandatory, Is.EqualTo(B(r, 7)));
            Assert.That(d.TraversalKind, Is.EqualTo(S(r, 8))); Assert.That(d.MaxJumpTiles, Is.EqualTo(I(r, 9))); Assert.That(d.Notes, Is.EqualTo(S(r, 10)));
        }

        private static void AssertSocket(SectorExternalSocketDefinition d)
        {
            var r = d.SourceRecord; Assert.That(new[] { d.SectorRecipeId, d.SocketId, d.Side }, Is.EqualTo(Enumerable.Range(0, 3).Select(i => S(r, i))));
            Assert.That(d.EdgeChunkIndex, Is.EqualTo(I(r, 3))); Assert.That(d.BandId, Is.EqualTo(S(r, 4))); Assert.That(d.TraversalKind, Is.EqualTo(S(r, 5)));
            Assert.That(d.MandatoryAllowed, Is.EqualTo(B(r, 6))); Assert.That(d.EdgeSignatureId, Is.EqualTo(S(r, 7))); Assert.That(d.Notes, Is.EqualTo(S(r, 8)));
        }

        private static void AssertPoolEntry(SectorRecipePoolEntryDefinition d)
        {
            var r = d.SourceRecord; Assert.That(d.SectorRecipePoolId, Is.EqualTo(S(r, 0))); Assert.That(d.EntryOrder, Is.EqualTo(I(r, 1)));
            Assert.That(d.SectorRecipeId, Is.EqualTo(S(r, 2))); Assert.That(d.Weight, Is.EqualTo(I(r, 3)));
            Assert.That(d.MinRepeatDistanceSectors, Is.EqualTo(I(r, 4))); Assert.That(d.RequiredPatchRole, Is.EqualTo(S(r, 5))); Assert.That(d.Active, Is.EqualTo(B(r, 6)));
        }

        private static string S(CsvParsedRecord record, int index) => record.Fields[index].Value.StringValue;
        private static int I(CsvParsedRecord record, int index) => record.Fields[index].Value.IntValue;
        private static float F(CsvParsedRecord record, int index) => record.Fields[index].Value.FloatValue;
        private static bool B(CsvParsedRecord record, int index) => record.Fields[index].Value.BoolValue;
        private static IReadOnlyList<string> L(CsvParsedRecord record, int index) => record.Fields[index].Value.StringListValue;

        private static int TotalDefinitionCount(WorldRouteDefinitionSet set)
        {
            return set.WorldProfiles.Count + set.GenerationProfiles.Count + set.GenerationPasses.Count + set.RngStreams.Count +
                   set.RouteMasks.Count + set.SocketBands.Count + set.EdgeSignatures.Count + set.EdgeSignatureCompatibilities.Count +
                   set.SectorRecipes.Count + set.SectorRecipeCells.Count + set.SectorRecipePaths.Count +
                   set.SectorExternalSockets.Count + set.SectorRecipePoolEntries.Count;
        }

        private static string Snapshot(WorldRouteDefinitionSet set)
        {
            return string.Join("|", new[]
            {
                string.Join(",", set.WorldProfiles.Keys), string.Join(",", set.GenerationProfiles.Keys),
                string.Join(",", set.GenerationPasses.Select(item => item.PassId)), string.Join(",", set.RngStreams.Keys),
                string.Join(",", set.RouteMasks.Keys), string.Join(",", set.SocketBands.Keys),
                string.Join(",", set.EdgeSignatures.Keys), string.Join(",", set.EdgeSignatureCompatibilities.Select(item => item.SignatureA + ":" + item.SignatureB)),
                string.Join(",", set.SectorRecipes.Keys), string.Join(",", set.SectorRecipeCells.Select(item => item.SectorRecipeId)),
                string.Join(",", set.SectorRecipePaths.Select(item => item.PathId)), string.Join(",", set.SectorExternalSockets.Select(item => item.SocketId)),
                string.Join(",", set.SectorRecipePoolEntries.Select(item => item.SectorRecipePoolId))
            });
        }

        private static FileSpec Spec(string fileName) => Specs.Single(spec => spec.FileName == fileName);

        private static void Replace(List<WorldRouteDefinitionSource> sources, WorldRouteDefinitionSource replacement)
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

        private static string FormatErrors(WorldRouteDefinitionBuildResult result)
        {
            return string.Join("\n", result.Errors.Select(error =>
                error.FileName + " " + error.ErrorCode + " " + error.Message));
        }

        private static FileSpec[] CreateSpecs()
        {
            return new[]
            {
                File("world_profiles.csv", 1, "world_profile_id:ID", "display_name_ko:STRING", "width_tiles:INT", "height_tiles:INT", "sector_width_tiles:INT", "sector_height_tiles:INT", "sector_cols:INT", "sector_rows:INT", "micro_width_tiles:INT", "micro_height_tiles:INT", "micro_cols_per_sector:INT", "micro_rows_per_sector:INT", "min_completion_distance_tiles:INT", "max_shortest_completion_distance_tiles:INT", "normal_completion_min_tiles:INT", "normal_completion_max_tiles:INT", "optional_completion_max_tiles:INT", "max_revisit_ratio:FLOAT", "required_village_count:INT", "active:BOOL", "notes:STRING"),
                File("generation_profiles.csv", 1, "generation_profile_id:ID", "world_profile_id:ID", "mandatory_sector_min:INT", "mandatory_sector_max:INT", "type0_sector_min:INT", "type0_sector_max:INT", "reserved_sector_min:INT", "reserved_sector_max:INT", "inactive_sector_min:INT", "inactive_sector_max:INT", "start_edge_ring_min:INT", "start_edge_ring_max:INT", "mandatory_loop_min:INT", "mandatory_loop_max:INT", "optional_region_depth_min:INT", "optional_region_depth_max:INT", "optional_region_count_min:INT", "optional_region_count_max:INT", "site_reservation_retry_max:INT", "biome_retry_max:INT", "route_retry_max:INT", "sector_solve_retry_max:INT", "active:BOOL", "notes:STRING"),
                File("generation_passes.csv", 3, "generation_profile_id:ID", "pass_order:INT", "pass_id:ID", "class_name:STRING", "rng_stream_id:ID", "input_artifacts:ID_LIST", "output_artifacts:ID_LIST", "failure_policy:ENUM", "max_retry_count:INT", "enabled:BOOL", "notes:STRING"),
                File("rng_streams.csv", 1, "rng_stream_id:ID", "salt_hex:HEX", "reset_scope:ENUM", "description_ko:STRING", "active:BOOL"),
                File("sector_route_masks.csv", 1, "route_mask_id:ID", "route_type:INT", "open_l:BOOL", "open_r:BOOL", "open_u:BOOL", "open_d:BOOL", "mandatory_allowed:BOOL", "description_ko:STRING", "active:BOOL"),
                File("socket_band_definitions.csv", 1, "band_id:ID", "axis:ENUM", "min_local_coord:INT", "max_local_coord:INT", "recommended_center:FLOAT", "minimum_clearance_tiles:INT", "description_ko:STRING"),
                File("edge_signatures.csv", 1, "edge_signature_id:ID", "axis:ENUM", "band_id:ID", "traversal_kind:ENUM", "ground_entry_height:INT", "clearance_width:INT", "clearance_height:INT", "tool_requirement:ENUM", "mandatory_allowed:BOOL", "tags:ID_LIST", "notes:STRING"),
                File("edge_signature_compatibility.csv", 2, "signature_a:ID", "signature_b:ID", "compatible:BOOL", "adapter_microchunk_pool_id:ID", "notes:STRING"),
                File("sector_recipe_catalog.csv", 1, "sector_recipe_id:ID", "display_name_ko:STRING", "route_type:INT", "route_mask_id:ID", "primary_biome_id:ID", "secondary_biome_id:ID", "boundary_profile_id:ID", "recipe_kind:ENUM", "microchunk_budget_profile_id:ID", "selection_weight:INT", "supports_special_entry:BOOL", "supports_village_entry:BOOL", "active:BOOL", "notes:STRING"),
                File("sector_recipe_cells.csv", 3, "sector_recipe_id:ID", "chunk_x:INT", "chunk_y:INT", "cell_role:ENUM", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_usage_class:ENUM_LIST:", "required_route_roles:ID_LIST", "required_biome_ids:ID_LIST", "required_signature_l:ID", "required_signature_r:ID", "required_signature_u:ID", "required_signature_d:ID", "transform_policy:ENUM_LIST:R0|MIRROR_X|MIRROR_Y|R180", "notes:STRING"),
                File("sector_recipe_paths.csv", 3, "sector_recipe_id:ID", "path_id:ID", "path_order:INT", "chunk_x:INT", "chunk_y:INT", "enter_side:ENUM", "exit_side:ENUM", "mandatory:BOOL", "traversal_kind:ENUM", "max_jump_tiles:INT", "notes:STRING"),
                File("sector_external_sockets.csv", 2, "sector_recipe_id:ID", "socket_id:ID", "side:ENUM", "edge_chunk_index:INT", "band_id:ID", "traversal_kind:ENUM", "mandatory_allowed:BOOL", "edge_signature_id:ID", "notes:STRING"),
                File("sector_recipe_pool_entries.csv", 3, "sector_recipe_pool_id:ID", "entry_order:INT", "sector_recipe_id:ID", "weight:INT", "min_repeat_distance_sectors:INT", "required_patch_role:ENUM", "active:BOOL")
            };
        }

        private static FileSpec File(string fileName, int primaryKeyCount, params string[] definitions)
        {
            return new FileSpec(fileName, primaryKeyCount, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                return Column(parts[0], parts[1], string.Empty, parts.Length > 2 ? parts[2] : null);
            }).ToArray());
        }

        private static ColumnSpec Column(
            string name,
            string dataType,
            string defaultValue = "",
            string allowedValues = null)
        {
            var allowed = allowedValues ??
                          (dataType == "ENUM" || dataType == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty);
            return new ColumnSpec(name, dataType, defaultValue, allowed);
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, int primaryKeyCount, params ColumnSpec[] columns)
            {
                FileName = fileName; PrimaryKeyCount = primaryKeyCount; Columns = columns;
            }

            public string FileName { get; }
            public int PrimaryKeyCount { get; }
            public IReadOnlyList<ColumnSpec> Columns { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string name, string dataType, string defaultValue, string allowedValues)
            {
                Name = name; DataType = dataType; DefaultValue = defaultValue; AllowedValues = allowedValues;
            }

            public string Name { get; }
            public string DataType { get; }
            public string DefaultValue { get; }
            public string AllowedValues { get; }
        }
    }
}
