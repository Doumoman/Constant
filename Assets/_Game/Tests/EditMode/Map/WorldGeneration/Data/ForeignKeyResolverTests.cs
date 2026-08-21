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
    public sealed class ForeignKeyResolverTests
    {
        [Test]
        public void ExactSourceContractContainsFortyNineUniqueOrdinalSortedNames()
        {
            var names = ForeignKeySourceSet.ExpectedFileNames;

            Assert.That(names.Count, Is.EqualTo(49));
            Assert.That(names.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(49));
            Assert.That(names, Is.EqualTo(names.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)names).Clear());
            Assert.That(names, Does.Not.Contain("CSV_DATA_DICTIONARY.csv"));
            Assert.That(names.Any(value => value.StartsWith("generated_", StringComparison.Ordinal)),
                Is.False);
        }

        [TestCase("battery_profiles.csv")]
        [TestCase("biome_types.csv")]
        [TestCase("edge_signatures.csv")]
        [TestCase("generation_profiles.csv")]
        [TestCase("microchunk_catalog.csv")]
        [TestCase("population_profiles.csv")]
        [TestCase("sector_route_masks.csv")]
        [TestCase("special_map_catalog.csv")]
        [TestCase("village_profiles.csv")]
        [TestCase("world_profiles.csv")]
        public void ExactSourceContractRecognizesRepresentativeStaticFile(string fileName)
        {
            Assert.That(ForeignKeySourceSet.ExpectedFileNames, Does.Contain(fileName));
        }

        [TestCase(ForeignKeyResolutionErrorCode.MissingSource)]
        [TestCase(ForeignKeyResolutionErrorCode.UnexpectedSource)]
        [TestCase(ForeignKeyResolutionErrorCode.DuplicateSource)]
        [TestCase(ForeignKeyResolutionErrorCode.UnsuccessfulParse)]
        [TestCase(ForeignKeyResolutionErrorCode.SchemaMismatch)]
        [TestCase(ForeignKeyResolutionErrorCode.InvalidForeignKeyDeclaration)]
        [TestCase(ForeignKeyResolutionErrorCode.MissingTargetRecord)]
        public void MinimumErrorCodeContractIsAvailable(ForeignKeyResolutionErrorCode code)
        {
            Assert.That(Enum.IsDefined(typeof(ForeignKeyResolutionErrorCode), code), Is.True);
        }

        [Test]
        public void ExactFortyNineSourceGateResolvesScalarAndListForeignKeys()
        {
            var fixture = BuildFixture();
            var result = Resolve(fixture);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.InputGatePassed, Is.True);
            Assert.That(result.RecordIndex, Is.Not.Null);
            Assert.That(result.References.Count, Is.EqualTo(4));
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ScalarForeignKeyResolvesExactTargetIdentity()
        {
            var fixture = BuildFixture();
            var result = Resolve(fixture);
            var reference = result.References.Single(item =>
                item.SourceColumnName == "biome_id");
            var targetRecord = fixture.Source("biome_types.csv").ParseResult.Records
                .Single(record => record.Fields[0].Value.IdValue == "B1");

            Assert.That(reference.TargetFileName, Is.EqualTo("biome_types.csv"));
            Assert.That(reference.TargetColumnName, Is.EqualTo("id"));
            Assert.That(reference.TargetValue, Is.EqualTo("B1"));
            Assert.That(reference.TargetIdentity.SourceRecord, Is.SameAs(targetRecord));
        }

        [Test]
        public void OptionalEmptyScalarCreatesNoReferenceOrError()
        {
            var result = Resolve(BuildFixture());

            Assert.That(result.References.Any(item =>
                item.SourceColumnName == "optional_biome_id"), Is.False);
            Assert.That(result.Errors.Any(item =>
                item.SourceColumnName == "optional_biome_id"), Is.False);
        }

        [Test]
        public void OptionalEmptyListCreatesNoReferenceOrError()
        {
            var rows = DefaultRows();
            rows["world_profiles.csv"] = "S1,B1,,,,POLY1,TEXT";
            var result = Resolve(BuildFixture(rows: rows));

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.References.Count, Is.EqualTo(1));
            Assert.That(result.References[0].SourceColumnName, Is.EqualTo("biome_id"));
        }

        [Test]
        public void IdListPreservesParsedOrderAndDuplicates()
        {
            var rows = DefaultRows();
            rows["world_profiles.csv"] = "S1,B1,B2|B1|B2,,BAT1,POLY1,TEXT";
            var result = Resolve(BuildFixture(rows: rows));
            var listReferences = result.References
                .Where(item => item.SourceColumnName == "biome_ids")
                .ToArray();

            Assert.That(listReferences.Select(item => item.ListIndex),
                Is.EqualTo(new int?[] { 0, 1, 2 }));
            Assert.That(listReferences.Select(item => item.TargetValue),
                Is.EqualTo(new[] { "B2", "B1", "B2" }));
        }

        [Test]
        public void RecordIndexLookupUsesExactOrdinalFileColumnAndValue()
        {
            var index = Resolve(BuildFixture()).RecordIndex;

            Assert.That(index.TryGet("biome_types.csv", "id", "B1", out var exact), Is.True);
            Assert.That(exact, Is.Not.Null);
            Assert.That(index.TryGet("BIOME_TYPES.csv", "id", "B1", out _), Is.False);
            Assert.That(index.TryGet("biome_types.csv", "ID", "B1", out _), Is.False);
            Assert.That(index.TryGet("biome_types.csv", "id", "b1", out _), Is.False);
        }

        [Test]
        public void MissingScalarTargetReportsExactProvenance()
        {
            var rows = DefaultRows();
            rows["world_profiles.csv"] = "S1,MISSING,B2|B1,,BAT1,POLY1,TEXT";
            var result = Resolve(BuildFixture(rows: rows));
            var error = result.Errors.Single();

            Assert.That(result.Success, Is.False);
            Assert.That(result.InputGatePassed, Is.True);
            Assert.That(error.ErrorCode, Is.EqualTo(ForeignKeyResolutionErrorCode.MissingTargetRecord));
            Assert.That(error.SourceFileName, Is.EqualTo("world_profiles.csv"));
            Assert.That(error.SourceRecordNumber, Is.EqualTo(2));
            Assert.That(error.SourceColumnName, Is.EqualTo("biome_id"));
            Assert.That(error.SourceColumnOrder, Is.EqualTo(2));
            Assert.That(error.ListIndex, Is.Null);
            Assert.That(error.TargetFileName, Is.EqualTo("biome_types.csv"));
            Assert.That(error.TargetColumnName, Is.EqualTo("id"));
            Assert.That(error.TargetValue, Is.EqualTo("MISSING"));
            Assert.That(error.SourceLocation, Is.Not.Null);
            Assert.That(error.SourceField, Is.Not.Null);
        }

        [Test]
        public void MissingListTargetsReportEveryTokenAndListIndex()
        {
            var rows = DefaultRows();
            rows["world_profiles.csv"] = "S1,B1,M2|B1|M1|M2,,BAT1,POLY1,TEXT";
            var result = Resolve(BuildFixture(rows: rows));
            var errors = result.Errors.ToArray();

            Assert.That(errors.Length, Is.EqualTo(3));
            Assert.That(errors.Select(item => item.ListIndex), Is.EqualTo(new int?[] { 0, 2, 3 }));
            Assert.That(errors.Select(item => item.RawValue), Is.EqualTo(new[] { "M2", "M1", "M2" }));
            Assert.That(errors.All(item => item.SourceColumnName == "biome_ids"), Is.True);
            Assert.That(errors.All(item => item.SourceLocation.HasValue), Is.True);
        }

        [Test]
        public void SameIdInDifferentTargetTablesRemainsIsolated()
        {
            var rows = DefaultRows();
            rows["world_profiles.csv"] = "S1,SAME,,,SAME,POLY1,TEXT";
            var result = Resolve(BuildFixture(rows: rows));
            var references = result.References.ToArray();

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(references.Length, Is.EqualTo(2));
            Assert.That(references.Select(item => item.TargetFileName),
                Is.EqualTo(new[] { "biome_types.csv", "battery_profiles.csv" }));
            Assert.That(references[0].TargetIdentity.SourceRecord,
                Is.Not.SameAs(references[1].TargetIdentity.SourceRecord));
        }

        [Test]
        public void ShuffledSourceOrderProducesIdenticalIndexReferenceAndErrorOrder()
        {
            var fixture = BuildFixture();
            var first = Resolve(fixture);
            var shuffled = new ForeignKeySourceSet(
                fixture.Catalog,
                fixture.Sources.AsEnumerable().Reverse());
            var second = new ForeignKeyResolver().Resolve(shuffled);

            Assert.That(IndexProjection(second), Is.EqualTo(IndexProjection(first)));
            Assert.That(ReferenceProjection(second), Is.EqualTo(ReferenceProjection(first)));
            Assert.That(ErrorProjection(second), Is.EqualTo(ErrorProjection(first)));
        }

        [Test]
        public void ShuffledParsedRecordCollectionProducesIdenticalStableOrder()
        {
            var fixture = BuildFixture();
            var target = fixture.Source("biome_types.csv");
            var shuffledParse = Construct<CsvScalarAndListParseResult>(
                target.ParseResult.Records.Reverse().ToArray(),
                Array.Empty<CsvValueParseError>());
            var shuffledSources = fixture.Sources.Select(source =>
                ReferenceEquals(source, target)
                    ? new ForeignKeySourceSet.Source(source.Schema, shuffledParse)
                    : source);
            var second = new ForeignKeyResolver().Resolve(
                new ForeignKeySourceSet(fixture.Catalog, shuffledSources));
            var first = Resolve(fixture);

            Assert.That(IndexProjection(second), Is.EqualTo(IndexProjection(first)));
            Assert.That(ReferenceProjection(second), Is.EqualTo(ReferenceProjection(first)));
        }

        [Test]
        public void SourceAndTargetParsedRecordIdentityIsPreserved()
        {
            var fixture = BuildFixture();
            var result = Resolve(fixture);
            var sourceRecord = fixture.Source("world_profiles.csv").ParseResult.Records.Single();
            var reference = result.References.Single(item => item.SourceColumnName == "biome_id");

            Assert.That(reference.SourceIdentity.SourceRecord, Is.SameAs(sourceRecord));
            Assert.That(reference.SourceField, Is.SameAs(sourceRecord.Fields[1]));
            Assert.That(reference.TargetIdentity.SourceRecord,
                Is.SameAs(fixture.Source("biome_types.csv").ParseResult.Records[0]));
        }

        [Test]
        public void ResultIndexAndNestedCollectionsAreReadOnly()
        {
            var result = Resolve(BuildFixture());

            Assert.Throws<NotSupportedException>(() =>
                ((IList<ResolvedForeignKeyReference>)result.References).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ForeignKeyResolutionError>)result.Errors).Add(null));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ForeignKeyRecordIdentity>)result.RecordIndex.Records).Clear());
        }

        [Test]
        public void MissingSourceFailsGateAndPublishesNoGraph()
        {
            var fixture = BuildFixture();
            fixture.Sources.RemoveAll(source => source.FileName == "battery_profiles.csv");
            var result = Resolve(fixture);

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.MissingSource);
            Assert.That(result.Errors.Count(item => item.SourceFileName == "battery_profiles.csv"),
                Is.EqualTo(1));
        }

        [Test]
        public void DuplicateSourceFailsGateAndPublishesNoGraph()
        {
            var fixture = BuildFixture();
            fixture.Sources.Add(fixture.Source("battery_profiles.csv"));
            var result = Resolve(fixture);

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.DuplicateSource);
        }

        [Test]
        public void UnexpectedSourceFailsGateAndPublishesNoGraph()
        {
            var fixture = BuildFixture();
            var unexpectedCatalog = BuildCatalog(new Dictionary<string, ColumnDefinition[]>
            {
                { "unexpected.csv", new[] { Column("id", CsvSchemaDataType.Id, true, 1) } }
            });
            var unexpectedSchema = unexpectedCatalog.GetFile("unexpected.csv");
            fixture.Sources.Add(new ForeignKeySourceSet.Source(
                unexpectedSchema,
                Parse(unexpectedSchema, "id\nU1")));
            var result = Resolve(fixture);

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.UnexpectedSource);
        }

        [Test]
        public void UnsuccessfulParseFailsGateAndPublishesNoGraph()
        {
            var fixture = BuildFixture();
            var source = fixture.Source("world_profiles.csv");
            var failedParse = Parse(
                source.Schema,
                Header(source.Schema) + "\nS1,lower,,,,POLY1,TEXT");
            Assert.That(failedParse.Success, Is.False);
            ReplaceSource(fixture, "world_profiles.csv",
                new ForeignKeySourceSet.Source(source.Schema, failedParse));
            var result = Resolve(fixture);

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.UnsuccessfulParse);
        }

        [Test]
        public void SchemaInstanceMismatchFailsGateAndPublishesNoGraph()
        {
            var fixture = BuildFixture();
            var other = BuildFixture();
            ReplaceSource(fixture, "battery_profiles.csv", other.Source("battery_profiles.csv"));
            var result = Resolve(fixture);

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.SchemaMismatch);
        }

        [Test]
        public void NullSourceEntryAccumulatesSafely()
        {
            var fixture = BuildFixture();
            fixture.Sources.Add(null);
            var result = Resolve(fixture);

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.MissingSource);
            Assert.That(result.Errors.Any(item => item.SourceFileName == string.Empty), Is.True);
        }

        [Test]
        public void NullSchemaEntryAccumulatesSafely()
        {
            var fixture = BuildFixture();
            fixture.Sources.Add(new ForeignKeySourceSet.Source(null, null));
            var result = Resolve(fixture);

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.SchemaMismatch);
        }

        [Test]
        public void NullCatalogAccumulatesSafelyWithoutPublication()
        {
            var fixture = BuildFixture();
            var result = new ForeignKeyResolver().Resolve(
                new ForeignKeySourceSet(null, fixture.Sources));

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.SchemaMismatch);
        }

        [Test]
        public void NullSourceSetReportsAllFortyNineMissingSources()
        {
            var result = new ForeignKeyResolver().Resolve(null);

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.MissingSource);
            Assert.That(result.Errors.Count, Is.EqualTo(49));
            Assert.That(result.Errors.Select(item => item.SourceFileName),
                Is.EqualTo(ForeignKeySourceSet.ExpectedFileNames));
        }

        [Test]
        public void ForeignKeyOnNonIdColumnIsInvalidDeclaration()
        {
            var columns = DefaultColumns();
            columns["world_profiles.csv"] = WorldColumns(
                CsvSchemaDataType.String,
                "biome_types.csv.id");
            var result = Resolve(BuildFixture(columns));

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.InvalidForeignKeyDeclaration);
        }

        [Test]
        public void MissingTargetFileIsInvalidDeclaration()
        {
            var columns = DefaultColumns();
            columns["world_profiles.csv"] = WorldColumns(
                CsvSchemaDataType.Id,
                "missing.csv.id");
            var result = Resolve(BuildFixture(columns));

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.InvalidForeignKeyDeclaration);
        }

        [Test]
        public void MissingTargetColumnIsInvalidDeclaration()
        {
            var columns = DefaultColumns();
            columns["world_profiles.csv"] = WorldColumns(
                CsvSchemaDataType.Id,
                "biome_types.csv.missing");
            var result = Resolve(BuildFixture(columns));

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.InvalidForeignKeyDeclaration);
        }

        [Test]
        public void TargetColumnWithoutPrimaryKeyDeclarationIsRejected()
        {
            var columns = DefaultColumns();
            columns["biome_types.csv"] = new[]
            {
                Column("id", CsvSchemaDataType.Id, true, 1),
                Column("code", CsvSchemaDataType.Id, true)
            };
            columns["world_profiles.csv"] = WorldColumns(
                CsvSchemaDataType.Id,
                "biome_types.csv.code");
            var rows = DefaultRows();
            rows["biome_types.csv"] = "T1,B1\nT2,B2\nT3,SAME";
            var result = Resolve(BuildFixture(columns, rows));

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.InvalidForeignKeyDeclaration);
        }

        [Test]
        public void AmbiguousReferencedComponentOfCompositePrimaryKeyIsRejected()
        {
            var columns = DefaultColumns();
            columns["biome_types.csv"] = new[]
            {
                Column("group_id", CsvSchemaDataType.Id, true, 1),
                Column("id", CsvSchemaDataType.Id, true, 2)
            };
            var rows = DefaultRows();
            rows["biome_types.csv"] = "G1,B1\nG2,B1\nG3,B2\nG4,SAME";
            var result = Resolve(BuildFixture(columns, rows));

            AssertGateFailure(result, ForeignKeyResolutionErrorCode.InvalidForeignKeyDeclaration);
        }

        [Test]
        public void BrokenReferenceKeepsIndependentResolvedSubset()
        {
            var rows = DefaultRows();
            rows["world_profiles.csv"] = "S1,MISSING,B2|B1,,BAT1,POLY1,TEXT";
            var result = Resolve(BuildFixture(rows: rows));

            Assert.That(result.Success, Is.False);
            Assert.That(result.InputGatePassed, Is.True);
            Assert.That(result.RecordIndex, Is.Not.Null);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.References.Select(item => item.TargetValue),
                Is.EqualTo(new[] { "B2", "B1", "BAT1" }));
        }

        [Test]
        public void SchemaWithoutForeignKeyAndPolymorphicIdRemainUntouched()
        {
            var fixture = BuildFixture();
            var sourceRecord = fixture.Source("world_profiles.csv").ParseResult.Records.Single();
            var result = Resolve(fixture);

            Assert.That(result.References.Any(item => item.SourceColumnName == "implicit_id"), Is.False);
            Assert.That(result.Errors.Any(item => item.SourceColumnName == "implicit_id"), Is.False);
            Assert.That(sourceRecord.Fields[5].Value.IdValue, Is.EqualTo("POLY1"));
        }

        [Test]
        public void ResolutionDoesNotModifySchemaParseRecordsOrFields()
        {
            var fixture = BuildFixture();
            var schemaFiles = fixture.Catalog.Files.ToArray();
            var source = fixture.Source("world_profiles.csv");
            var records = source.ParseResult.Records.ToArray();
            var fields = records[0].Fields.ToArray();

            var result = Resolve(fixture);

            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(fixture.Catalog.Files, Is.EqualTo(schemaFiles));
            Assert.That(source.ParseResult.Records, Is.EqualTo(records));
            Assert.That(source.ParseResult.Records[0].Fields, Is.EqualTo(fields));
        }

        [Test]
        public void RecordIndexEnumerationUsesFileThenRecordOrder()
        {
            var result = Resolve(BuildFixture());
            var projection = result.RecordIndex.Records
                .Select(item => item.FileName + ":" + item.RecordNumber)
                .ToArray();

            Assert.That(projection, Is.EqualTo(projection
                .OrderBy(value => value.Split(':')[0], StringComparer.Ordinal)
                .ThenBy(value => int.Parse(value.Split(':')[1], CultureInfo.InvariantCulture))));
        }

        [Test]
        public void RecordIndexRejectsNullLookupArgumentsExplicitly()
        {
            var index = Resolve(BuildFixture()).RecordIndex;

            Assert.Throws<ArgumentNullException>(() => index.TryGet(null, "id", "B1", out _));
            Assert.Throws<ArgumentNullException>(() => index.TryGet("biome_types.csv", null, "B1", out _));
            Assert.Throws<ArgumentNullException>(() => index.TryGet("biome_types.csv", "id", null, out _));
        }

        [Test]
        public void SourceSetCopiesItsEntrySequence()
        {
            var fixture = BuildFixture();
            var input = fixture.Sources.ToList();
            var sourceSet = new ForeignKeySourceSet(fixture.Catalog, input);
            input.Clear();

            Assert.That(sourceSet.Sources.Count, Is.EqualTo(49));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ForeignKeySourceSet.Source>)sourceSet.Sources).Clear());
        }

        [Test]
        public void MissingTargetErrorsUseStableRecordColumnListAndValueOrder()
        {
            var rows = DefaultRows();
            rows["world_profiles.csv"] =
                "S1,M2,M3|M1,,BAT1,POLY1,TEXT\n" +
                "S2,M1,M2|M1,,BAT1,POLY2,TEXT";
            var result = Resolve(BuildFixture(rows: rows));
            var projection = result.Errors.Select(item =>
                item.SourceRecordNumber + ":" + item.SourceColumnOrder + ":" +
                (item.ListIndex.HasValue ? item.ListIndex.Value.ToString() : "-") + ":" +
                item.TargetValue).ToArray();

            Assert.That(projection, Is.EqualTo(new[]
            {
                "2:2:-:M2",
                "2:3:0:M3",
                "2:3:1:M1",
                "3:2:-:M1",
                "3:3:0:M2",
                "3:3:1:M1"
            }));
        }

        [Test]
        public void GateFailureNeverPublishesIndexOrPartialReferences()
        {
            var fixture = BuildFixture();
            fixture.Sources.RemoveAt(0);
            fixture.Sources.Add(fixture.Source("world_profiles.csv"));
            var result = Resolve(fixture);

            Assert.That(result.Success, Is.False);
            Assert.That(result.InputGatePassed, Is.False);
            Assert.That(result.RecordIndex, Is.Null);
            Assert.That(result.References, Is.Empty);
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ResolvedReferencePreservesRawTokenAndExactFieldLocation()
        {
            var fixture = BuildFixture();
            var sourceRecord = fixture.Source("world_profiles.csv").ParseResult.Records.Single();
            var result = Resolve(fixture);
            var reference = result.References.Single(item =>
                item.SourceColumnName == "biome_ids" && item.ListIndex == 0);

            Assert.That(reference.RawValue, Is.EqualTo("B2"));
            Assert.That(reference.SourceLocation,
                Is.EqualTo(sourceRecord.Fields[2].ValidatedField.SourceField.StartLocation));
            Assert.That(reference.SourceRecordNumber, Is.EqualTo(sourceRecord.RecordNumber));
        }

        private static ForeignKeyResolutionResult Resolve(Fixture fixture)
        {
            return new ForeignKeyResolver().Resolve(
                new ForeignKeySourceSet(fixture.Catalog, fixture.Sources));
        }

        private static Fixture BuildFixture(
            Dictionary<string, ColumnDefinition[]> columns = null,
            Dictionary<string, string> rows = null)
        {
            columns = columns ?? DefaultColumns();
            rows = rows ?? DefaultRows();
            var catalog = BuildCatalog(columns);
            var sources = new List<ForeignKeySourceSet.Source>();
            var fileIndex = 0;
            foreach (var fileName in ForeignKeySourceSet.ExpectedFileNames)
            {
                var schema = catalog.GetFile(fileName);
                if (!rows.TryGetValue(fileName, out var dataRows))
                {
                    dataRows = "D" + fileIndex.ToString("D2", CultureInfo.InvariantCulture);
                }

                var text = Header(schema) +
                           (dataRows.Length == 0 ? string.Empty : "\n" + dataRows);
                var parse = Parse(schema, text);
                Assert.That(parse.Success, Is.True,
                    fileName + ": " + string.Join("\n", parse.Errors.Select(error => error.Message)));
                sources.Add(new ForeignKeySourceSet.Source(schema, parse));
                fileIndex++;
            }

            return new Fixture(catalog, sources);
        }

        private static Dictionary<string, ColumnDefinition[]> DefaultColumns()
        {
            var result = ForeignKeySourceSet.ExpectedFileNames.ToDictionary(
                fileName => fileName,
                fileName => new[] { Column("id", CsvSchemaDataType.Id, true, 1) },
                StringComparer.Ordinal);
            result["world_profiles.csv"] = WorldColumns(
                CsvSchemaDataType.Id,
                "biome_types.csv.id");
            return result;
        }

        private static ColumnDefinition[] WorldColumns(
            CsvSchemaDataType biomeIdType,
            string biomeIdForeignKey)
        {
            return new[]
            {
                Column("id", CsvSchemaDataType.Id, true, 1),
                Column("biome_id", biomeIdType, true, null, biomeIdForeignKey),
                Column("biome_ids", CsvSchemaDataType.IdList, false, null, "biome_types.csv.id"),
                Column("optional_biome_id", CsvSchemaDataType.Id, false, null, "biome_types.csv.id"),
                Column("battery_id", CsvSchemaDataType.Id, false, null, "battery_profiles.csv.id"),
                Column("implicit_id", CsvSchemaDataType.Id, false),
                Column("label", CsvSchemaDataType.String, false)
            };
        }

        private static Dictionary<string, string> DefaultRows()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "biome_types.csv", "B1\nB2\nSAME" },
                { "battery_profiles.csv", "BAT1\nSAME" },
                { "world_profiles.csv", "S1,B1,B2|B1,,BAT1,POLY1,TEXT" }
            };
        }

        private static CsvSchemaCatalog BuildCatalog(
            IReadOnlyDictionary<string, ColumnDefinition[]> definitions)
        {
            var rows = new List<CsvSchemaDictionaryRow>();
            var sourceRow = 2;
            foreach (var pair in definitions.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                for (var index = 0; index < pair.Value.Length; index++)
                {
                    var column = pair.Value[index];
                    rows.Add(new CsvSchemaDictionaryRow(
                        pair.Key,
                        (index + 1).ToString(CultureInfo.InvariantCulture),
                        column.Name,
                        CsvSchemaDataTypes.ToToken(column.DataType),
                        column.Required ? "1" : "0",
                        column.PrimaryKeyOrder.HasValue
                            ? column.PrimaryKeyOrder.Value.ToString(CultureInfo.InvariantCulture)
                            : string.Empty,
                        string.Empty,
                        string.Empty,
                        column.ForeignKey,
                        string.Empty,
                        sourceRow++));
                }
            }

            var built = new CsvSchemaCatalogBuilder().Build(rows);
            Assert.That(built.Success, Is.True,
                string.Join("\n", built.Errors.Select(error => error.ToString())));
            return built.Catalog;
        }

        private static CsvScalarAndListParseResult Parse(
            CsvFileSchema schema,
            string text)
        {
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(text),
                schema.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(
                read,
                schema,
                schema.FileName);
            Assert.That(validation.Success, Is.True,
                string.Join("\n", validation.Errors.Select(error => error.Message)));
            var primaryKey = new CsvPrimaryKeyIndexBuilder().Build(
                schema,
                validation,
                schema.FileName);
            Assert.That(primaryKey.Success, Is.True);
            return new CsvScalarAndListParser().Parse(
                schema,
                validation,
                primaryKey,
                schema.FileName);
        }

        private static string Header(CsvFileSchema schema)
        {
            return string.Join(",", schema.Columns.Select(column => column.ColumnName));
        }

        private static ColumnDefinition Column(
            string name,
            CsvSchemaDataType dataType,
            bool required,
            int? primaryKeyOrder = null,
            string foreignKey = "")
        {
            return new ColumnDefinition(
                name,
                dataType,
                required,
                primaryKeyOrder,
                foreignKey);
        }

        private static void ReplaceSource(
            Fixture fixture,
            string fileName,
            ForeignKeySourceSet.Source replacement)
        {
            var index = fixture.Sources.FindIndex(source => source?.FileName == fileName);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            fixture.Sources[index] = replacement;
        }

        private static T Construct<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                arguments,
                CultureInfo.InvariantCulture);
        }

        private static void AssertGateFailure(
            ForeignKeyResolutionResult result,
            ForeignKeyResolutionErrorCode expectedCode)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.InputGatePassed, Is.False);
            Assert.That(result.RecordIndex, Is.Null);
            Assert.That(result.References, Is.Empty);
            Assert.That(result.Errors.Any(item => item.ErrorCode == expectedCode), Is.True,
                FormatErrors(result));
        }

        private static string[] IndexProjection(ForeignKeyResolutionResult result)
        {
            return result.RecordIndex.Records.Select(item =>
                item.FileName + ":" + item.RecordNumber).ToArray();
        }

        private static string[] ReferenceProjection(ForeignKeyResolutionResult result)
        {
            return result.References.Select(item =>
                item.SourceFileName + ":" + item.SourceRecordNumber + ":" +
                item.SourceColumnOrder + ":" + item.ListIndex + ":" +
                item.TargetFileName + ":" + item.TargetColumnName + ":" +
                item.TargetValue + ":" + item.TargetIdentity.RecordNumber).ToArray();
        }

        private static string[] ErrorProjection(ForeignKeyResolutionResult result)
        {
            return result.Errors.Select(item =>
                item.SourceFileName + ":" + item.SourceRecordNumber + ":" +
                item.SourceColumnOrder + ":" + item.ListIndex + ":" +
                item.TargetFileName + ":" + item.TargetColumnName + ":" +
                item.TargetValue + ":" + item.ErrorCode).ToArray();
        }

        private static string FormatErrors(ForeignKeyResolutionResult result)
        {
            return string.Join("\n", result.Errors.Select(error =>
                error.ErrorCode + " " + error.SourceFileName + " " + error.Message));
        }

        private sealed class Fixture
        {
            public Fixture(
                CsvSchemaCatalog catalog,
                List<ForeignKeySourceSet.Source> sources)
            {
                Catalog = catalog;
                Sources = sources;
            }

            public CsvSchemaCatalog Catalog { get; }
            public List<ForeignKeySourceSet.Source> Sources { get; }

            public ForeignKeySourceSet.Source Source(string fileName)
            {
                return Sources.Single(item => item?.FileName == fileName);
            }
        }

        private sealed class ColumnDefinition
        {
            public ColumnDefinition(
                string name,
                CsvSchemaDataType dataType,
                bool required,
                int? primaryKeyOrder,
                string foreignKey)
            {
                Name = name;
                DataType = dataType;
                Required = required;
                PrimaryKeyOrder = primaryKeyOrder;
                ForeignKey = foreignKey ?? string.Empty;
            }

            public string Name { get; }
            public CsvSchemaDataType DataType { get; }
            public bool Required { get; }
            public int? PrimaryKeyOrder { get; }
            public string ForeignKey { get; }
        }
    }
}
