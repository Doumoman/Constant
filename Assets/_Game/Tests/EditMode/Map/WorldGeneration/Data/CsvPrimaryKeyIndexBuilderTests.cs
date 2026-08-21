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
    public sealed class CsvPrimaryKeyIndexBuilderTests
    {
        [Test]
        public void SinglePrimaryKeyBuildsOneEntryIndex()
        {
            var schema = Schema(Column("id", 1), Column("value"));
            var result = Build("id,value\nitem,text", schema);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Duplicates, Is.Empty);
            Assert.That(result.Index, Is.Not.Null);
            Assert.That(result.Index.Count, Is.EqualTo(1));
            Assert.That(result.Index.SchemaFileName, Is.EqualTo("schema.csv"));
        }

        [Test]
        public void LookupHitReturnsExactOccurrence()
        {
            var result = Build("id,value\nitem,text", Schema(Column("id", 1), Column("value")));

            Assert.That(result.Index.TryGet(
                new CsvPrimaryKey(new[] { "item" }),
                out var occurrence), Is.True);
            Assert.That(occurrence.Key.Components, Is.EqualTo(new[] { "item" }));
            Assert.That(occurrence.RecordNumber, Is.EqualTo(2));
            Assert.That(occurrence.SourceRecord, Is.SameAs(occurrence.SourceValidatedRecord.SourceRecord));
        }

        [Test]
        public void LookupMissReturnsFalseAndNullOccurrence()
        {
            var result = Build("id\nitem", Schema(Column("id", 1)));

            Assert.That(result.Index.TryGet(
                new CsvPrimaryKey(new[] { "missing" }),
                out var occurrence), Is.False);
            Assert.That(occurrence, Is.Null);
        }

        [Test]
        public void CompositePrimaryKeyUsesPrimaryKeyOrderInsteadOfColumnOrder()
        {
            var schema = Schema(
                Column("second", 2),
                Column("value"),
                Column("first", 1));
            var result = Build("second,value,first\nB,text,A", schema);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Index.Entries.Single().Key.Components,
                Is.EqualTo(new[] { "A", "B" }));
            Assert.That(result.Index.Entries.Single().PrimaryKeyFields
                .Select(field => field.Schema.ColumnName),
                Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void KeyComparisonIsExactOrdinalAndCaseSensitive()
        {
            var result = Build("id\nA\na", Schema(Column("id", 1)));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Index.Count, Is.EqualTo(2));
            Assert.That(result.Index.TryGet(new CsvPrimaryKey(new[] { "A" }), out _), Is.True);
            Assert.That(result.Index.TryGet(new CsvPrimaryKey(new[] { "a" }), out _), Is.True);
        }

        [Test]
        public void NumericLookingComponentsRemainDistinctRawStrings()
        {
            var result = Build("id\n01\n1", Schema(Column("id", 1)));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Index.Count, Is.EqualTo(2));
            Assert.That(result.Index.Entries.Select(entry => entry.Key.Components[0]),
                Is.EqualTo(new[] { "01", "1" }));
        }

        [Test]
        public void WhitespaceRemainsSignificant()
        {
            var result = Build("id\nvalue\n value ", Schema(Column("id", 1)));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Index.Count, Is.EqualTo(2));
            Assert.That(result.Index.TryGet(new CsvPrimaryKey(new[] { " value " }), out _), Is.True);
        }

        [Test]
        public void EffectiveDefaultValueIsIndexedUnchanged()
        {
            var schema = Schema(Column("id", 1, " fallback "));
            var validation = Validate("id\n\"\"", schema, "source.csv");
            var result = new CsvPrimaryKeyIndexBuilder().Build(schema, validation);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Index.Entries.Single().Key.Components,
                Is.EqualTo(new[] { " fallback " }));
            Assert.That(result.Index.Entries.Single().PrimaryKeyFields[0].UsedDefault, Is.True);
        }

        [Test]
        public void NonEmptyRawValueWinsOverSchemaDefault()
        {
            var result = Build("id\nraw", Schema(Column("id", 1, "fallback")));

            Assert.That(result.Success, Is.True);
            var field = result.Index.Entries.Single().PrimaryKeyFields[0];
            Assert.That(field.RawValue, Is.EqualTo("raw"));
            Assert.That(field.EffectiveValue, Is.EqualTo("raw"));
            Assert.That(field.UsedDefault, Is.False);
        }

        [Test]
        public void DelimiterLikeComponentsCannotCollide()
        {
            var schema = Schema(Column("left", 1), Column("right", 2));
            var result = Build("left,right\na|b,c\na,b|c", schema);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Index.Count, Is.EqualTo(2));
            Assert.That(result.Index.TryGet(
                new CsvPrimaryKey(new[] { "a|b", "c" }), out _), Is.True);
            Assert.That(result.Index.TryGet(
                new CsvPrimaryKey(new[] { "a", "b|c" }), out _), Is.True);
        }

        [Test]
        public void HeaderOnlySuccessfulFileBuildsEmptyIndex()
        {
            var result = Build("id", Schema(Column("id", 1)));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Index.Count, Is.Zero);
            Assert.That(result.Index.Entries, Is.Empty);
        }

        [Test]
        public void TwoRowDuplicateReportsBothOccurrences()
        {
            var result = Build("id\nsame\nsame", Schema(Column("id", 1)));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Duplicates.Count, Is.EqualTo(1));
            Assert.That(result.Duplicates[0].Occurrences.Select(value => value.RecordNumber),
                Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void ThreeRowDuplicateReportsAllOccurrencesInOneGroup()
        {
            var result = Build("id\nsame\nsame\nsame", Schema(Column("id", 1)));

            Assert.That(result.Duplicates.Count, Is.EqualTo(1));
            Assert.That(result.Duplicates[0].Occurrences.Count, Is.EqualTo(3));
            Assert.That(result.Duplicates[0].Occurrences.Select(value => value.RecordNumber),
                Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [Test]
        public void MultipleDuplicateKeysCreateSeparateLexicographicGroups()
        {
            var result = Build("id\nb\na\nb\na", Schema(Column("id", 1)));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Duplicates.Select(group => group.Key.Components[0]),
                Is.EqualTo(new[] { "a", "b" }));
            Assert.That(result.Duplicates.Select(group => group.Occurrences.Count),
                Is.EqualTo(new[] { 2, 2 }));
        }

        [Test]
        public void DuplicateOccurrencesPreserveFirstAndLaterExactLocations()
        {
            var result = Build(
                "id,value\nsame,first\nsame,second",
                Schema(Column("id", 1), Column("value")));
            var occurrences = result.Duplicates.Single().Occurrences;

            AssertLocation(occurrences[0], 9, 2, 1, 2);
            AssertLocation(occurrences[1], 20, 3, 1, 3);
        }

        [Test]
        public void CompositeDuplicateExposesEveryPrimaryKeySourceField()
        {
            var result = Build(
                "region,id\nr,x\nr,x",
                Schema(Column("region", 1), Column("id", 2)));
            var occurrence = result.Duplicates.Single().Occurrences[0];

            Assert.That(occurrence.PrimaryKeyFields.Select(field => field.Schema.ColumnName),
                Is.EqualTo(new[] { "region", "id" }));
            Assert.That(occurrence.PrimaryKeyFields.Select(
                    field => field.SourceField.StartLocation.PhysicalColumn),
                Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void DuplicateResultPublishesNoUsableOrPartialIndex()
        {
            var result = Build("id\nunique\nduplicate\nduplicate", Schema(Column("id", 1)));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Index, Is.Null);
            Assert.That(result.Duplicates.Count, Is.EqualTo(1));
        }

        [Test]
        public void AllDuplicateGroupsAreCollected()
        {
            var result = Build("id\nc\nb\nc\na\nb\na", Schema(Column("id", 1)));

            Assert.That(result.Index, Is.Null);
            Assert.That(result.Duplicates.Select(group => group.Key.Components[0]),
                Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(result.Duplicates.All(group => group.Occurrences.Count == 2), Is.True);
        }

        [Test]
        public void ShuffledRowsPreserveKeyMembershipAndDuplicateGroupOrder()
        {
            var schema = Schema(Column("id", 1));
            var first = Build("id\nb\na\nb\nc\na", schema);
            var second = Build("id\na\nc\nb\na\nb", schema);

            Assert.That(DuplicateProjection(first), Is.EqualTo(DuplicateProjection(second)));
        }

        [Test]
        public void IndexEnumerationUsesComponentOrdinalLexicographicOrder()
        {
            var result = Build("id\nb\na\nA\n1\n01", Schema(Column("id", 1)));

            Assert.That(result.Index.Entries.Select(entry => entry.Key.Components[0]),
                Is.EqualTo(new[] { "01", "1", "A", "a", "b" }));
        }

        [Test]
        public void NullArgumentsAndNullLookupKeyAreRejectedExplicitly()
        {
            var builder = new CsvPrimaryKeyIndexBuilder();
            var schema = Schema(Column("id", 1));
            var validation = Validate("id", schema, "source.csv");
            var index = builder.Build(schema, validation).Index;

            Assert.Throws<ArgumentNullException>(() => builder.Build(null, validation));
            Assert.Throws<ArgumentNullException>(() => builder.Build(schema, null));
            Assert.Throws<ArgumentNullException>(() => builder.Build(schema, validation, null));
            Assert.Throws<ArgumentNullException>(() => index.TryGet(null, out _));
        }

        [Test]
        public void UnsuccessfulValidationIsRejectedWithoutPartialIndex()
        {
            var schema = Schema(Column("id", 1));
            var validation = Validate("id\n\"\"", schema, "source.csv");

            Assert.That(validation.Success, Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                new CsvPrimaryKeyIndexBuilder().Build(schema, validation));
        }

        [Test]
        public void SchemaAndValidatedFieldMismatchIsRejected()
        {
            var validationSchema = Schema(Column("id", 1));
            var suppliedSchema = Schema(Column("id", 1));
            var validation = Validate("id\nvalue", validationSchema, "source.csv");

            Assert.Throws<InvalidOperationException>(() =>
                new CsvPrimaryKeyIndexBuilder().Build(suppliedSchema, validation));
        }

        [Test]
        public void SchemaWithoutPrimaryKeyIsRejected()
        {
            var schema = Construct<CsvFileSchema>(
                "no-pk.csv",
                Array.Empty<CsvColumnSchema>());
            var validation = Construct<CsvHeaderFieldValidationResult>(
                Array.Empty<CsvValidatedRecord>(),
                Array.Empty<CsvHeaderFieldError>());

            Assert.Throws<InvalidOperationException>(() =>
                new CsvPrimaryKeyIndexBuilder().Build(schema, validation));
        }

        [Test]
        public void EmptyEffectivePrimaryKeyComponentIsRejectedDefensively()
        {
            var schema = Schema(Column("id", 1));
            var read = Read("id\n\"\"", "source.csv");
            var sourceRecord = read.Records[1];
            var sourceField = sourceRecord.Fields[0];
            var validatedField = Construct<CsvValidatedField>(
                schema.Columns[0],
                sourceField,
                string.Empty,
                string.Empty,
                false);
            var validatedRecord = Construct<CsvValidatedRecord>(
                sourceRecord.RecordNumber,
                new[] { validatedField },
                sourceRecord);
            var validation = Construct<CsvHeaderFieldValidationResult>(
                new[] { validatedRecord },
                Array.Empty<CsvHeaderFieldError>());

            Assert.Throws<InvalidOperationException>(() =>
                new CsvPrimaryKeyIndexBuilder().Build(schema, validation));
        }

        [Test]
        public void PrimaryKeyComponentsAreImmutableAndCopied()
        {
            var source = new[] { "a", "b" };
            var key = new CsvPrimaryKey(source);
            source[0] = "changed";

            Assert.That(key.Components, Is.EqualTo(new[] { "a", "b" }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)key.Components).Add("c"));
        }

        [Test]
        public void IndexOccurrenceDuplicateAndResultCollectionsAreReadOnly()
        {
            var schema = Schema(Column("id", 1));
            var success = Build("id\nunique", schema);
            var failure = Build("id\nduplicate\nduplicate", schema);

            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvPrimaryKeyOccurrence>)success.Index.Entries).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvValidatedField>)success.Index.Entries[0].PrimaryKeyFields).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvDuplicatePrimaryKey>)failure.Duplicates).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvPrimaryKeyOccurrence>)failure.Duplicates[0].Occurrences).Clear());
        }

        [Test]
        public void BuildDoesNotModifySchemaValidationRecordsOrFields()
        {
            var schema = Schema(Column("id", 1), Column("value"));
            var validation = Validate("id,value\nkey,text", schema, "source.csv");
            var recordsBefore = validation.Records.ToArray();
            var fieldsBefore = validation.Records[0].Fields.ToArray();
            var schemaColumnsBefore = schema.Columns.ToArray();

            var result = new CsvPrimaryKeyIndexBuilder().Build(schema, validation);

            Assert.That(result.Success, Is.True);
            Assert.That(validation.Records, Is.EqualTo(recordsBefore));
            Assert.That(validation.Records[0].Fields, Is.EqualTo(fieldsBefore));
            Assert.That(schema.Columns, Is.EqualTo(schemaColumnsBefore));
            Assert.That(result.Index.Entries[0].SourceValidatedRecord,
                Is.SameAs(recordsBefore[0]));
            Assert.That(result.Index.Entries[0].PrimaryKeyFields[0],
                Is.SameAs(fieldsBefore[0]));
        }

        [Test]
        public void ExplicitSourceNameIsPreservedWithSchemaFileName()
        {
            var schema = Schema(Column("id", 1));
            var validation = Validate("id\nvalue", schema, "folder/source.csv");
            var result = new CsvPrimaryKeyIndexBuilder().Build(
                schema,
                validation,
                "folder/source.csv");
            var occurrence = result.Index.Entries.Single();

            Assert.That(occurrence.SourceName, Is.EqualTo("folder/source.csv"));
            Assert.That(occurrence.SchemaFileName, Is.EqualTo("schema.csv"));
        }

        [Test]
        public void SameInputProducesSameDeterministicProjection()
        {
            var schema = Schema(Column("left", 1), Column("right", 2));
            const string text = "left,right\nb,2\na,2\na,1";
            var first = Build(text, schema);
            var second = Build(text, schema);

            Assert.That(IndexProjection(first), Is.EqualTo(IndexProjection(second)));
        }

        [Test]
        public void EqualStructuralKeysHaveEqualHashCodes()
        {
            var first = new CsvPrimaryKey(new[] { "A", "01" });
            var second = new CsvPrimaryKey(new[] { "A", "01" });

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void ComponentBoundariesAndCountParticipateInIdentity()
        {
            var composite = new CsvPrimaryKey(new[] { "ab", "c" });
            var differentlySplit = new CsvPrimaryKey(new[] { "a", "bc" });
            var single = new CsvPrimaryKey(new[] { "abc" });

            Assert.That(composite, Is.Not.EqualTo(differentlySplit));
            Assert.That(composite, Is.Not.EqualTo(single));
            Assert.That(differentlySplit, Is.Not.EqualTo(single));
        }

        private static CsvPrimaryKeyIndexBuildResult Build(
            string text,
            CsvFileSchema schema)
        {
            var validation = Validate(text, schema, "source.csv");
            Assert.That(validation.Success, Is.True, FormatErrors(validation));
            return new CsvPrimaryKeyIndexBuilder().Build(schema, validation, "source.csv");
        }

        private static CsvHeaderFieldValidationResult Validate(
            string text,
            CsvFileSchema schema,
            string sourceName)
        {
            return new CsvHeaderAndFieldValidator().Validate(
                Read(text, sourceName),
                schema,
                sourceName);
        }

        private static CsvReadResult Read(string text, string sourceName)
        {
            return new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(text),
                sourceName);
        }

        private static CsvFileSchema Schema(params ColumnDefinition[] columns)
        {
            var rows = columns.Select((column, index) => new CsvSchemaDictionaryRow(
                "schema.csv",
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                "STRING",
                column.IsRequired ? "1" : "0",
                column.PrimaryKeyOrder.HasValue
                    ? column.PrimaryKeyOrder.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
                column.DefaultValue,
                string.Empty,
                string.Empty,
                string.Empty,
                index + 2));
            var built = new CsvSchemaCatalogBuilder().Build(rows);
            Assert.That(built.Success, Is.True,
                string.Join("\n", built.Errors.Select(error => error.ToString())));
            return built.Catalog.GetFile("schema.csv");
        }

        private static ColumnDefinition Column(
            string name,
            int? primaryKeyOrder = null,
            string defaultValue = "",
            bool isRequired = true)
        {
            return new ColumnDefinition(name, primaryKeyOrder, defaultValue, isRequired);
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

        private static string[] DuplicateProjection(CsvPrimaryKeyIndexBuildResult result)
        {
            return result.Duplicates.Select(group =>
                string.Join("/", group.Key.Components) + ":" + group.Occurrences.Count)
                .ToArray();
        }

        private static string[] IndexProjection(CsvPrimaryKeyIndexBuildResult result)
        {
            return result.Index.Entries.Select(entry =>
                string.Join("/", entry.Key.Components) + ":" + entry.RecordNumber)
                .ToArray();
        }

        private static void AssertLocation(
            CsvPrimaryKeyOccurrence occurrence,
            int charOffset,
            int physicalLine,
            int physicalColumn,
            int recordNumber)
        {
            Assert.That(occurrence.CharOffset, Is.EqualTo(charOffset));
            Assert.That(occurrence.PhysicalLine, Is.EqualTo(physicalLine));
            Assert.That(occurrence.PhysicalColumn, Is.EqualTo(physicalColumn));
            Assert.That(occurrence.RecordNumber, Is.EqualTo(recordNumber));
        }

        private static string FormatErrors(CsvHeaderFieldValidationResult result)
        {
            return string.Join("\n", result.Errors.Select(error => error.ToString()));
        }

        private sealed class ColumnDefinition
        {
            public ColumnDefinition(
                string name,
                int? primaryKeyOrder,
                string defaultValue,
                bool isRequired)
            {
                Name = name;
                PrimaryKeyOrder = primaryKeyOrder;
                DefaultValue = defaultValue;
                IsRequired = isRequired;
            }

            public string Name { get; }

            public int? PrimaryKeyOrder { get; }

            public string DefaultValue { get; }

            public bool IsRequired { get; }
        }
    }
}
