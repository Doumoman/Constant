using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public sealed class BiomeBoundaryDefinitionBuilderTests
    {
        private static readonly FileSpec[] Specs = CreateSpecs();
        private static IEnumerable<string> FileNames => Specs.Select(spec => spec.FileName);

        [Test]
        public void ExactFiveSourcesBuildSuccessfully()
        {
            var result = Build(StandardSources());

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(TotalCount(result.DefinitionSet), Is.EqualTo(5));
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
                error.ErrorCode == BiomeBoundaryDefinitionBuildErrorCode.MissingSource), Is.True);
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
                Is.EqualTo(BiomeBoundaryDefinitionBuildErrorCode.DuplicateSource));
        }

        [Test]
        public void UnexpectedSourceFailsWithoutPartialSet()
        {
            var sources = StandardSources();
            sources.Add(BuildSource(new FileSpec("unexpected.csv", Column("id", "ID"))));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single(error => error.FileName == "unexpected.csv").ErrorCode,
                Is.EqualTo(BiomeBoundaryDefinitionBuildErrorCode.UnexpectedSource));
        }

        [Test]
        public void UnsuccessfulParseIsRejected()
        {
            var sources = StandardSources();
            var spec = Spec("biome_types.csv");
            var row = StandardRow(spec);
            row[4] = "bad-int";
            Replace(sources, BuildSource(spec, new[] { row }, false));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Single().ErrorCode,
                Is.EqualTo(BiomeBoundaryDefinitionBuildErrorCode.UnsuccessfulParse));
        }

        [Test]
        public void SchemaColumnTypeMismatchIsRejected()
        {
            var sources = StandardSources();
            var original = Spec("biome_types.csv");
            var columns = original.Columns.ToArray();
            columns[4] = Column("min_patch_count", "STRING");
            Replace(sources, BuildSource(new FileSpec(original.FileName, columns)));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Any(error =>
                error.ErrorCode == BiomeBoundaryDefinitionBuildErrorCode.SchemaMismatch &&
                error.ColumnOrder == 5), Is.True);
        }

        [TestCase("biome_types.csv", "microchunk_pool_prefix", "ID", "STRING")]
        [TestCase("biome_types.csv", "sector_recipe_pool_prefix", "ID", "STRING")]
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
            Replace(sources, BuildSource(new FileSpec(fileName, columns)));

            var result = Build(sources);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors.Any(error =>
                error.ErrorCode == BiomeBoundaryDefinitionBuildErrorCode.SchemaMismatch &&
                error.ColumnOrder == columnIndex + 1 &&
                error.ColumnName == columnName), Is.True);
        }

        [Test]
        public void ParsedFieldsFromEquivalentOtherSchemaAreRejected()
        {
            var sources = StandardSources();
            var original = sources.Single(source => source.FileName == "biome_types.csv");
            var other = BuildSource(Spec("biome_types.csv"));
            Replace(sources, new BiomeBoundaryDefinitionSource(original.Schema, other.ParseResult));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.All(error =>
                error.ErrorCode == BiomeBoundaryDefinitionBuildErrorCode.FieldMappingFailed), Is.True);
            Assert.That(result.Errors.First().RecordNumber, Is.EqualTo(2));
            Assert.That(result.Errors.First().Location, Is.Not.Null);
        }

        [Test]
        public void NullEnumerableThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BiomeBoundaryDefinitionBuilder().Build(null));
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
                Is.EqualTo(BiomeBoundaryDefinitionBuildErrorCode.MissingSource));
        }

        [Test]
        public void SourceRejectsNullSchemaAndParseResult()
        {
            var source = BuildSource(Spec("biome_types.csv"));

            Assert.Throws<ArgumentNullException>(() =>
                new BiomeBoundaryDefinitionSource(null, source.ParseResult));
            Assert.Throws<ArgumentNullException>(() =>
                new BiomeBoundaryDefinitionSource(source.Schema, null));
        }

        [Test]
        public void FiveDictionariesUseOrdinalLookupAndEnumeration()
        {
            var sources = StandardSources();
            foreach (var spec in Specs)
            {
                var z = StandardRow(spec);
                var a = StandardRow(spec);
                z[0] = "Z_KEY";
                a[0] = "A_KEY";
                Replace(sources, BuildSource(spec, new[] { z, a }));
            }

            var set = Build(sources).DefinitionSet;

            Assert.That(set.BiomeTypes.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.BiomePatchRules.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.BoundaryProfiles.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.BoundaryPairRules.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.BoundaryChunks.Keys, Is.EqualTo(new[] { "A_KEY", "Z_KEY" }));
            Assert.That(set.BiomeTypes.ContainsKey("a_key"), Is.False);
        }

        [Test]
        public void PatchRuleQueryByBiomeIsStableAndReadOnly()
        {
            var sources = StandardSources();
            var spec = Spec("biome_patch_rules.csv");
            var z = StandardRow(spec); z[0] = "Z_RULE"; z[1] = "BIOME_A";
            var a = StandardRow(spec); a[0] = "A_RULE"; a[1] = "BIOME_A";
            Replace(sources, BuildSource(spec, new[] { z, a }));
            var set = Build(sources).DefinitionSet;

            var rules = set.GetBiomePatchRules("BIOME_A");

            Assert.That(rules.Select(item => item.PatchRuleId), Is.EqualTo(new[] { "A_RULE", "Z_RULE" }));
            Assert.Throws<NotSupportedException>(() => ((IList<BiomePatchRuleDefinition>)rules).Clear());
            Assert.That(set.GetBiomePatchRules("UNKNOWN"), Is.Empty);
        }

        [Test]
        public void PairRuleQueryPreservesExactDirection()
        {
            var sources = StandardSources();
            var spec = Spec("biome_boundary_pair_rules.csv");
            var ab = StandardRow(spec); ab[0] = "RULE_AB"; ab[1] = "BIOME_A"; ab[2] = "BIOME_B";
            var ba = StandardRow(spec); ba[0] = "RULE_BA"; ba[1] = "BIOME_B"; ba[2] = "BIOME_A";
            Replace(sources, BuildSource(spec, new[] { ba, ab }));
            var set = Build(sources).DefinitionSet;

            Assert.That(set.GetBoundaryPairRules("BIOME_A", "BIOME_B").Single().BoundaryPairRuleId,
                Is.EqualTo("RULE_AB"));
            Assert.That(set.GetBoundaryPairRules("BIOME_B", "BIOME_A").Single().BoundaryPairRuleId,
                Is.EqualTo("RULE_BA"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<BiomeBoundaryPairRuleDefinition>)set.GetBoundaryPairRules("BIOME_A", "BIOME_B")).Clear());
        }

        [Test]
        public void PairQueryDoesNotCanonicalizeOrGenerateReverse()
        {
            var set = Build(StandardSources()).DefinitionSet;
            var pair = set.BoundaryPairRules.Values.Single();

            Assert.That(set.GetBoundaryPairRules(pair.BiomeAId, pair.BiomeBId), Has.Count.EqualTo(1));
            Assert.That(set.GetBoundaryPairRules(pair.BiomeBId, pair.BiomeAId), Is.Empty);
        }

        [Test]
        public void ChunkQueryByProfileIsStableAndReadOnly()
        {
            var set = Build(ChunkQuerySources()).DefinitionSet;
            var chunks = set.GetBoundaryChunksByProfile("PROFILE_A");

            Assert.That(chunks.Select(item => item.BoundaryChunkId), Is.EqualTo(new[] { "A_CHUNK", "Z_CHUNK" }));
            Assert.Throws<NotSupportedException>(() => ((IList<BoundaryChunkDefinition>)chunks).Clear());
            Assert.That(set.GetBoundaryChunksByProfile("UNKNOWN"), Is.Empty);
        }

        [Test]
        public void ChunkQueryByDirectedPairDoesNotMergeReverse()
        {
            var sources = StandardSources();
            var spec = Spec("boundary_chunk_catalog.csv");
            var ab = StandardRow(spec); ab[0] = "CHUNK_AB"; ab[2] = "BIOME_A"; ab[3] = "BIOME_B";
            var ba = StandardRow(spec); ba[0] = "CHUNK_BA"; ba[2] = "BIOME_B"; ba[3] = "BIOME_A";
            Replace(sources, BuildSource(spec, new[] { ba, ab }));
            var set = Build(sources).DefinitionSet;

            Assert.That(set.GetBoundaryChunks("BIOME_A", "BIOME_B").Single().BoundaryChunkId,
                Is.EqualTo("CHUNK_AB"));
            Assert.That(set.GetBoundaryChunks("BIOME_B", "BIOME_A").Single().BoundaryChunkId,
                Is.EqualTo("CHUNK_BA"));
        }

        [Test]
        public void NestedListsPreserveOrderDuplicatesAndAreReadOnly()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.That(set.BiomeTypes.Values.Single().RequiredSpecialMapIds,
                Is.EqualTo(new[] { "LIST_A", "LIST_B", "LIST_A" }));
            Assert.That(set.BoundaryProfiles.Values.Single().AllowedOrientations,
                Is.EqualTo(new[] { "ENUM_A", "ENUM_B", "ENUM_A" }));
            Assert.That(set.BoundaryPairRules.Values.Single().BoundaryProfileWeights,
                Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)set.BoundaryPairRules.Values.Single().BoundaryProfileWeights).Add(3));
        }

        [Test]
        public void OptionalEmptyValuesArePreservedExactly()
        {
            var sources = StandardSources();
            var spec = Spec("biome_boundary_pair_rules.csv");
            var row = StandardRow(spec);
            row[6] = string.Empty;
            row[7] = string.Empty;
            row[10] = string.Empty;
            Replace(sources, BuildSource(spec, new[] { row }));

            var definition = Build(sources).DefinitionSet.BoundaryPairRules.Values.Single();

            Assert.That(definition.TransitionResourcePoolId, Is.Empty);
            Assert.That(definition.TransitionElementPoolId, Is.Empty);
            Assert.That(definition.Notes, Is.Empty);
        }

        [Test]
        public void DefaultAppliedValueAndUsedDefaultArePreserved()
        {
            var sources = StandardSources();
            var spec = Spec("biome_types.csv");
            var columns = spec.Columns.ToArray();
            columns[4] = Column("min_patch_count", "INT", "+17");
            var defaultSpec = new FileSpec(spec.FileName, columns);
            var row = StandardRow(defaultSpec);
            row[4] = string.Empty;
            Replace(sources, BuildSource(defaultSpec, new[] { row }));

            var definition = Build(sources).DefinitionSet.BiomeTypes.Values.Single();

            Assert.That(definition.MinPatchCount, Is.EqualTo(17));
            Assert.That(definition.SourceRecord.Fields[4].UsedDefault, Is.True);
            Assert.That(definition.SourceRecord.Fields[4].RawValue, Is.Empty);
        }

        [Test]
        public void InactiveRowsAreRetained()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.That(set.BiomeTypes.Values.Single().Active, Is.False);
            Assert.That(set.BiomePatchRules.Values.Single().Active, Is.False);
            Assert.That(set.BoundaryProfiles.Values.Single().Active, Is.False);
            Assert.That(set.BoundaryPairRules.Values.Single().Active, Is.False);
            Assert.That(set.BoundaryChunks.Values.Single().Active, Is.False);
        }

        [Test]
        public void DictionariesAndResultErrorsAreReadOnly()
        {
            var success = Build(StandardSources());
            var failure = Build(StandardSources().Where(source => source.FileName != "biome_types.csv"));

            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, BiomeTypeDefinition>)success.DefinitionSet.BiomeTypes).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<BiomeBoundaryDefinitionBuildError>)failure.Errors).Clear());
        }

        [Test]
        public void ForeignKeyIdsRemainUnresolvedStrings()
        {
            var set = Build(StandardSources()).DefinitionSet;

            Assert.That(set.BiomeTypes.Values.Single().StageId, Is.EqualTo("ID_3"));
            Assert.That(set.BiomePatchRules.Values.Single().BiomeId, Is.EqualTo("ID_2"));
            Assert.That(set.BoundaryChunks.Values.Single().EntryEdgeSignatureId, Is.EqualTo("ID_8"));
        }

        [Test]
        public void DomainInvalidButTypedValuesAreNotRejected()
        {
            var sources = StandardSources();
            var spec = Spec("biome_patch_rules.csv");
            var row = StandardRow(spec);
            row[3] = "-100";
            row[4] = "-200";
            Replace(sources, BuildSource(spec, new[] { row }));

            var result = Build(sources);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.DefinitionSet.BiomePatchRules.Values.Single().MinSectorCount,
                Is.EqualTo(-100));
        }

        [Test]
        public void ErrorsAccumulateAndSortDeterministically()
        {
            var sources = StandardSources();
            sources.RemoveAll(source =>
                source.FileName == "biome_types.csv" ||
                source.FileName == "boundary_chunk_catalog.csv");
            sources.Add(BuildSource(new FileSpec("z_unexpected.csv", Column("id", "ID"))));
            sources.Add(sources.Single(source => source.FileName == "biome_patch_rules.csv"));

            var result = Build(sources);

            Assert.That(result.DefinitionSet, Is.Null);
            Assert.That(result.Errors.Select(error => error.FileName),
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Does.Contain(BiomeBoundaryDefinitionBuildErrorCode.MissingSource));
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Does.Contain(BiomeBoundaryDefinitionBuildErrorCode.DuplicateSource));
            Assert.That(result.Errors.Select(error => error.ErrorCode),
                Does.Contain(BiomeBoundaryDefinitionBuildErrorCode.UnexpectedSource));
        }

        [Test]
        public void BuildDoesNotModifyInputModelsOrPreviousDefinitionSet()
        {
            var sources = StandardSources();
            var sourceRefs = sources.ToArray();
            var columns = sources.Select(source => source.Schema.Columns.ToArray()).ToArray();
            var records = sources.Select(source => source.ParseResult.Records.ToArray()).ToArray();
            var previous = new WorldRouteDefinitionBuilder().Build(Array.Empty<WorldRouteDefinitionSource>());

            var result = Build(sources);

            Assert.That(result.Success, Is.True);
            Assert.That(previous.Success, Is.False);
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

        private static List<BiomeBoundaryDefinitionSource> ChunkQuerySources()
        {
            var sources = StandardSources();
            var spec = Spec("boundary_chunk_catalog.csv");
            var z = StandardRow(spec); z[0] = "Z_CHUNK"; z[4] = "PROFILE_A"; z[2] = "BIOME_A"; z[3] = "BIOME_B";
            var a = StandardRow(spec); a[0] = "A_CHUNK"; a[4] = "PROFILE_A"; a[2] = "BIOME_A"; a[3] = "BIOME_B";
            Replace(sources, BuildSource(spec, new[] { z, a }));
            return sources;
        }

        private static List<BiomeBoundaryDefinitionSource> StandardSources()
        {
            return Specs.Select(spec => BuildSource(spec)).ToList();
        }

        private static BiomeBoundaryDefinitionBuildResult Build(
            IEnumerable<BiomeBoundaryDefinitionSource> sources)
        {
            return new BiomeBoundaryDefinitionBuilder().Build(sources);
        }

        private static BiomeBoundaryDefinitionSource BuildSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows = null,
            bool expectSuccess = true)
        {
            var schemaRows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName,
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                column.DataType,
                index == 0 ? "1" : "0",
                index == 0 ? "1" : string.Empty,
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
            return new BiomeBoundaryDefinitionSource(schema, parsed);
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

        private static void AssertFullMapping(string fileName, BiomeBoundaryDefinitionSet set)
        {
            switch (fileName)
            {
                case "biome_types.csv": AssertBiome(set.BiomeTypes.Values.Single()); break;
                case "biome_patch_rules.csv": AssertPatch(set.BiomePatchRules.Values.Single()); break;
                case "biome_boundary_profiles.csv": AssertProfile(set.BoundaryProfiles.Values.Single()); break;
                case "biome_boundary_pair_rules.csv": AssertPair(set.BoundaryPairRules.Values.Single()); break;
                case "boundary_chunk_catalog.csv": AssertChunk(set.BoundaryChunks.Values.Single()); break;
                default: throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
            }
        }

        private static void AssertBiome(BiomeTypeDefinition d)
        {
            var r = d.SourceRecord;
            Assert.That(new[] { d.BiomeId, d.DisplayNameKo, d.StageId }, Is.EqualTo(Enumerable.Range(0, 3).Select(i => S(r, i))));
            Assert.That(d.Required, Is.EqualTo(B(r, 3)));
            Assert.That(new[] { d.MinPatchCount, d.MaxPatchCount, d.MinCorePatchCount,
                d.PreferredAltitudeMinSectorY, d.PreferredAltitudeMaxSectorY },
                Is.EqualTo(Enumerable.Range(4, 5).Select(i => I(r, i))));
            Assert.That(d.GrowthWeight, Is.EqualTo(F(r, 9)));
            Assert.That(new[] { d.TileThemeId, d.AudioProfileId, d.MicrochunkPoolPrefix,
                d.SectorRecipePoolPrefix, d.CommonResourcePoolId, d.MapElementPoolId },
                Is.EqualTo(Enumerable.Range(10, 6).Select(i => S(r, i))));
            Assert.That(d.RequiredSpecialMapIds, Is.EqualTo(SL(r, 16)));
            Assert.That(d.Active, Is.EqualTo(B(r, 17)));
            Assert.That(d.Notes, Is.EqualTo(S(r, 18)));
        }

        private static void AssertPatch(BiomePatchRuleDefinition d)
        {
            var r = d.SourceRecord;
            Assert.That(new[] { d.PatchRuleId, d.BiomeId, d.PatchRole }, Is.EqualTo(Enumerable.Range(0, 3).Select(i => S(r, i))));
            Assert.That(new[] { d.MinSectorCount, d.MaxSectorCount, d.MinSeedDistance, d.SeedCountMin, d.SeedCountMax },
                Is.EqualTo(Enumerable.Range(3, 5).Select(i => I(r, i))));
            Assert.That(d.SeedWeight, Is.EqualTo(F(r, 8)));
            Assert.That(d.CanTouchWorldEdge, Is.EqualTo(B(r, 9)));
            Assert.That(d.BufferRingSectors, Is.EqualTo(I(r, 10)));
            Assert.That(d.AllowSingleSector, Is.EqualTo(B(r, 11)));
            Assert.That(new[] { d.MaxWorldShare, d.DistanceWeight, d.AltitudeWeight, d.NoiseWeight,
                d.CompactnessWeight, d.BranchinessTarget }, Is.EqualTo(Enumerable.Range(12, 6).Select(i => F(r, i))));
            Assert.That(d.Active, Is.EqualTo(B(r, 18)));
            Assert.That(d.Notes, Is.EqualTo(S(r, 19)));
        }

        private static void AssertProfile(BiomeBoundaryProfileDefinition d)
        {
            var r = d.SourceRecord;
            Assert.That(new[] { d.BoundaryProfileId, d.DisplayNameKo, d.BoundaryType },
                Is.EqualTo(Enumerable.Range(0, 3).Select(i => S(r, i))));
            Assert.That(d.AllowedOrientations, Is.EqualTo(SL(r, 3)));
            Assert.That(new[] { d.WidthMicrochunksMin, d.WidthMicrochunksMax, d.WarningMicrochunksMin },
                Is.EqualTo(Enumerable.Range(4, 3).Select(i => I(r, i))));
            Assert.That(d.MandatoryRouteAllowed, Is.EqualTo(B(r, 7)));
            Assert.That(d.ToolRequirement, Is.EqualTo(S(r, 8)));
            Assert.That(new[] { d.HardBorder, d.Active }, Is.EqualTo(new[] { B(r, 9), B(r, 10) }));
            Assert.That(d.Notes, Is.EqualTo(S(r, 11)));
        }

        private static void AssertPair(BiomeBoundaryPairRuleDefinition d)
        {
            var r = d.SourceRecord;
            Assert.That(new[] { d.BoundaryPairRuleId, d.BiomeAId, d.BiomeBId },
                Is.EqualTo(Enumerable.Range(0, 3).Select(i => S(r, i))));
            Assert.That(d.AllowedBoundaryProfileIds, Is.EqualTo(SL(r, 3)));
            Assert.That(d.BoundaryProfileWeights, Is.EqualTo(IL(r, 4)));
            Assert.That(new[] { d.DefaultBoundaryProfileId, d.TransitionResourcePoolId, d.TransitionElementPoolId },
                Is.EqualTo(Enumerable.Range(5, 3).Select(i => S(r, i))));
            Assert.That(d.MinSharedEdgeCount, Is.EqualTo(I(r, 8)));
            Assert.That(d.Active, Is.EqualTo(B(r, 9)));
            Assert.That(d.Notes, Is.EqualTo(S(r, 10)));
        }

        private static void AssertChunk(BoundaryChunkDefinition d)
        {
            var r = d.SourceRecord;
            Assert.That(new[] { d.BoundaryChunkId, d.MicrochunkId, d.BiomeAId, d.BiomeBId,
                d.BoundaryProfileId, d.Orientation }, Is.EqualTo(Enumerable.Range(0, 6).Select(i => S(r, i))));
            Assert.That(d.RouteType, Is.EqualTo(I(r, 6)));
            Assert.That(new[] { d.EntryEdgeSignatureId, d.ExitEdgeSignatureId },
                Is.EqualTo(Enumerable.Range(7, 2).Select(i => S(r, i))));
            Assert.That(d.Weight, Is.EqualTo(I(r, 9)));
            Assert.That(new[] { d.Reversible, d.Active }, Is.EqualTo(new[] { B(r, 10), B(r, 11) }));
            Assert.That(d.Notes, Is.EqualTo(S(r, 12)));
        }

        private static string S(CsvParsedRecord record, int index) => record.Fields[index].Value.StringValue;
        private static int I(CsvParsedRecord record, int index) => record.Fields[index].Value.IntValue;
        private static float F(CsvParsedRecord record, int index) => record.Fields[index].Value.FloatValue;
        private static bool B(CsvParsedRecord record, int index) => record.Fields[index].Value.BoolValue;
        private static IReadOnlyList<string> SL(CsvParsedRecord record, int index) => record.Fields[index].Value.StringListValue;
        private static IReadOnlyList<int> IL(CsvParsedRecord record, int index) => record.Fields[index].Value.IntListValue;

        private static int TotalCount(BiomeBoundaryDefinitionSet set)
        {
            return set.BiomeTypes.Count + set.BiomePatchRules.Count + set.BoundaryProfiles.Count +
                   set.BoundaryPairRules.Count + set.BoundaryChunks.Count;
        }

        private static string Snapshot(BiomeBoundaryDefinitionSet set)
        {
            return string.Join("|", new[]
            {
                string.Join(",", set.BiomeTypes.Keys),
                string.Join(",", set.BiomePatchRules.Keys),
                string.Join(",", set.BoundaryProfiles.Keys),
                string.Join(",", set.BoundaryPairRules.Keys),
                string.Join(",", set.BoundaryChunks.Keys)
            });
        }

        private static FileSpec Spec(string fileName) => Specs.Single(spec => spec.FileName == fileName);

        private static void Replace(
            List<BiomeBoundaryDefinitionSource> sources,
            BiomeBoundaryDefinitionSource replacement)
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

        private static string FormatErrors(BiomeBoundaryDefinitionBuildResult result)
        {
            return string.Join("\n", result.Errors.Select(error =>
                error.FileName + " " + error.ErrorCode + " " + error.Message));
        }

        private static FileSpec[] CreateSpecs()
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

        private static FileSpec File(string fileName, params string[] definitions)
        {
            return new FileSpec(fileName, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                return Column(parts[0], parts[1]);
            }).ToArray());
        }

        private static ColumnSpec Column(string name, string dataType, string defaultValue = "")
        {
            var allowed = dataType == "ENUM" || dataType == "ENUM_LIST"
                ? "ENUM_A|ENUM_B"
                : string.Empty;
            return new ColumnSpec(name, dataType, defaultValue, allowed);
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, params ColumnSpec[] columns)
            {
                FileName = fileName;
                Columns = columns;
            }

            public string FileName { get; }
            public IReadOnlyList<ColumnSpec> Columns { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string name, string dataType, string defaultValue, string allowedValues)
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
