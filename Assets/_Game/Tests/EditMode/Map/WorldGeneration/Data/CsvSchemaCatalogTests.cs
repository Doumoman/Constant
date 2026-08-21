using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public sealed class CsvSchemaCatalogTests
    {
        [TestCase("STRING", CsvSchemaDataType.String)]
        [TestCase("ID", CsvSchemaDataType.Id)]
        [TestCase("INT", CsvSchemaDataType.Int)]
        [TestCase("ULONG", CsvSchemaDataType.ULong)]
        [TestCase("FLOAT", CsvSchemaDataType.Float)]
        [TestCase("BOOL", CsvSchemaDataType.Bool)]
        [TestCase("ENUM", CsvSchemaDataType.Enum)]
        [TestCase("ID_LIST", CsvSchemaDataType.IdList)]
        [TestCase("ENUM_LIST", CsvSchemaDataType.EnumList)]
        [TestCase("INT_LIST", CsvSchemaDataType.IntList)]
        [TestCase("HEX", CsvSchemaDataType.Hex)]
        [TestCase("DATETIME", CsvSchemaDataType.DateTime)]
        public void ExactDataTypeTokensRoundTrip(
            string token,
            CsvSchemaDataType expectedDataType)
        {
            Assert.That(CsvSchemaDataTypes.TryParse(token, out var actualDataType), Is.True);
            Assert.That(actualDataType, Is.EqualTo(expectedDataType));
            Assert.That(CsvSchemaDataTypes.ToToken(actualDataType), Is.EqualTo(token));
        }

        [Test]
        public void DataTypeMappingRejectsUnknownAndWrongCase()
        {
            Assert.That(CsvSchemaDataTypes.TryParse("string", out _), Is.False);
            Assert.That(CsvSchemaDataTypes.TryParse("UNKNOWN", out _), Is.False);
            Assert.That(CsvSchemaDataTypes.TryParse(string.Empty, out _), Is.False);
        }

        [Test]
        public void CatalogUsesOrdinalLookupsDeterministicOrderAndReadOnlyCollections()
        {
            var catalog = BuildCatalog(
                Row("z.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 4),
                Row("a.csv", "2", "value", sourceRowNumber: 3),
                Row("a.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 2));

            Assert.That(catalog.Files.Select(file => file.FileName),
                Is.EqualTo(new[] { "a.csv", "z.csv" }));
            Assert.That(catalog.FileCount, Is.EqualTo(2));
            Assert.That(catalog.ColumnCount, Is.EqualTo(3));
            Assert.That(catalog.TryGetFile("a.csv", out var file), Is.True);
            Assert.That(catalog.TryGetFile("A.csv", out _), Is.False);
            Assert.That(file.Columns.Select(column => column.ColumnName),
                Is.EqualTo(new[] { "id", "value" }));
            Assert.That(file.TryGetColumn("id", out _), Is.True);
            Assert.That(file.TryGetColumn("ID", out _), Is.False);
            Assert.Throws<KeyNotFoundException>(() => catalog.GetFile("missing.csv"));
            Assert.Throws<KeyNotFoundException>(() => file.GetColumn("missing"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvFileSchema>)catalog.Files).Add(file));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvColumnSchema>)file.Columns).Clear());
        }

        [Test]
        public void ColumnPreservesRequiredDefaultAllowedValuesAndRawDescription()
        {
            var catalog = BuildCatalog(
                Row("a.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 2),
                Row(
                    "a.csv",
                    "2",
                    "mode",
                    "ENUM",
                    "0",
                    string.Empty,
                    "A",
                    " A |B ",
                    description: "raw description",
                    sourceRowNumber: 3));

            var column = catalog.GetFile("a.csv").GetColumn("mode");
            Assert.That(column.IsRequired, Is.False);
            Assert.That(column.PrimaryKeyOrder, Is.Null);
            Assert.That(column.DefaultValue, Is.EqualTo("A"));
            Assert.That(column.AllowedValues, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(column.Description, Is.EqualTo("raw description"));
            Assert.That(column.SourceRowNumber, Is.EqualTo(3));
        }

        [Test]
        public void CompositePrimaryKeyColumnsUsePrimaryKeyOrder()
        {
            var catalog = BuildCatalog(
                Row("a.csv", "1", "second_key", "ID", "1", "2", sourceRowNumber: 2),
                Row("a.csv", "2", "first_key", "ID", "1", "1", sourceRowNumber: 3),
                Row("a.csv", "3", "value", sourceRowNumber: 4));

            Assert.That(
                catalog.GetFile("a.csv").PrimaryKeyColumns.Select(column => column.ColumnName),
                Is.EqualTo(new[] { "first_key", "second_key" }));
        }

        [Test]
        public void ForeignKeyUsesLastDotAndPreservesStructuralParts()
        {
            var catalog = BuildCatalog(
                Row("a.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 2),
                Row(
                    "a.csv",
                    "2",
                    "target_id",
                    "ID",
                    "1",
                    foreignKey: "target.table.csv.target_id",
                    sourceRowNumber: 3));

            var reference = catalog.GetFile("a.csv").GetColumn("target_id").ForeignKey;
            Assert.That(reference, Is.Not.Null);
            Assert.That(reference.TargetFileName, Is.EqualTo("target.table.csv"));
            Assert.That(reference.TargetColumnName, Is.EqualTo("target_id"));
            Assert.That(reference.ToString(), Is.EqualTo("target.table.csv.target_id"));
        }

        [Test]
        public void BuilderRejectsDuplicateColumnNameAndOrder()
        {
            var result = new CsvSchemaCatalogBuilder().Build(new[]
            {
                Row("a.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 2),
                Row("a.csv", "2", "id", sourceRowNumber: 3),
                Row("a.csv", "1", "other", sourceRowNumber: 4),
            });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.Errors.Select(error => error.Code),
                Does.Contain("DUPLICATE_COLUMN_NAME"));
            Assert.That(result.Errors.Select(error => error.Code),
                Does.Contain("DUPLICATE_COLUMN_ORDER"));
        }

        [Test]
        public void BuilderRejectsNonContiguousColumnAndPrimaryKeyOrder()
        {
            var columnOrderResult = new CsvSchemaCatalogBuilder().Build(new[]
            {
                Row("a.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 2),
                Row("a.csv", "3", "value", sourceRowNumber: 3),
            });
            var primaryKeyOrderResult = new CsvSchemaCatalogBuilder().Build(new[]
            {
                Row("a.csv", "1", "first", "ID", "1", "1", sourceRowNumber: 2),
                Row("a.csv", "2", "second", "ID", "1", "3", sourceRowNumber: 3),
            });

            Assert.That(columnOrderResult.Errors.Select(error => error.Code),
                Does.Contain("NON_CONTIGUOUS_COLUMN_ORDER"));
            Assert.That(primaryKeyOrderResult.Errors.Select(error => error.Code),
                Does.Contain("NON_CONTIGUOUS_PRIMARY_KEY_ORDER"));
        }

        [Test]
        public void InvalidFieldsReportDeterministicContext()
        {
            var result = new CsvSchemaCatalogBuilder().Build(new[]
            {
                Row("bad.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 2),
                Row("bad.csv", "2", "required", required: "2", sourceRowNumber: 3),
                Row("bad.csv", "3", "type", "string", sourceRowNumber: 4),
                Row("bad.csv", "4", "pk", "ID", "1", "0", sourceRowNumber: 5),
                Row("bad.csv", "5", "allowed", "ENUM", allowedValues: "A||A", sourceRowNumber: 6),
                Row("bad.csv", "6", "fk", "ID", foreignKey: "target.column", sourceRowNumber: 7),
                Row("bad.csv", "7", "optional_pk", "ID", "0", "2", sourceRowNumber: 8),
            });

            var codes = result.Errors.Select(error => error.Code).ToArray();
            Assert.That(codes, Does.Contain("INVALID_REQUIRED"));
            Assert.That(codes, Does.Contain("INVALID_DATA_TYPE"));
            Assert.That(codes, Does.Contain("INVALID_PRIMARY_KEY_ORDER"));
            Assert.That(codes, Does.Contain("EMPTY_ALLOWED_VALUE"));
            Assert.That(codes, Does.Contain("DUPLICATE_ALLOWED_VALUE"));
            Assert.That(codes, Does.Contain("INVALID_FOREIGN_KEY"));
            Assert.That(codes, Does.Contain("PRIMARY_KEY_NOT_REQUIRED"));
            Assert.That(result.Errors.All(error =>
                error.FileName == "bad.csv" && error.SourceRowNumber > 0), Is.True);
            Assert.That(result.Errors, Is.Ordered.Using<CsvSchemaCatalogError>(
                Comparer<CsvSchemaCatalogError>.Create(CsvSchemaCatalogErrorComparer)));
        }

        [Test]
        public void AnyErrorPreventsPartialCatalogPublication()
        {
            var result = new CsvSchemaCatalogBuilder().Build(new[]
            {
                Row("valid.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 2),
                Row("invalid.csv", "1", "id", "UNKNOWN", "1", "1", sourceRowNumber: 3),
            });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public void ShuffledInputRowsProduceIdenticalCatalogProjection()
        {
            var rows = new[]
            {
                Row("b.csv", "2", "value", sourceRowNumber: 5),
                Row("a.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 2),
                Row("b.csv", "1", "id", "ID", "1", "1", sourceRowNumber: 4),
                Row("a.csv", "2", "mode", "ENUM", allowedValues: "A|B", sourceRowNumber: 3),
            };

            var forward = BuildCatalog(rows);
            var reverse = BuildCatalog(rows.Reverse().ToArray());
            Assert.That(Project(forward), Is.EqualTo(Project(reverse)));
        }

        [Test]
        public void ShuffledInvalidRowsProduceIdenticalErrorOrder()
        {
            var rows = new[]
            {
                Row("b.csv", "1", "id", "bad", "1", "1", sourceRowNumber: 8),
                Row("a.csv", "1", "id", "ID", "2", "1", sourceRowNumber: 3),
            };
            var builder = new CsvSchemaCatalogBuilder();
            var forward = builder.Build(rows);
            var reverse = builder.Build(rows.Reverse());

            Assert.That(
                forward.Errors.Select(error => error.ToString()),
                Is.EqualTo(reverse.Errors.Select(error => error.ToString())));
        }

        private static CsvSchemaCatalog BuildCatalog(params CsvSchemaDictionaryRow[] rows)
        {
            var result = new CsvSchemaCatalogBuilder().Build(rows);
            Assert.That(result.Success, Is.True,
                string.Join("\n", result.Errors.Select(error => error.ToString())));
            return result.Catalog;
        }

        private static string[] Project(CsvSchemaCatalog catalog)
        {
            return catalog.Files
                .SelectMany(file => file.Columns.Select(column =>
                    file.FileName + ":" + column.ColumnOrder + ":" + column.ColumnName + ":" +
                    CsvSchemaDataTypes.ToToken(column.DataType)))
                .ToArray();
        }

        private static int CsvSchemaCatalogErrorComparer(
            CsvSchemaCatalogError left,
            CsvSchemaCatalogError right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.FileName, right.FileName);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.Ordinal.Compare(left.ColumnName, right.ColumnName);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.SourceRowNumber.CompareTo(right.SourceRowNumber);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.Ordinal.Compare(left.Code, right.Code);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.Message, right.Message);
        }

        private static CsvSchemaDictionaryRow Row(
            string fileName,
            string columnOrder,
            string columnName,
            string dataType = "STRING",
            string required = "1",
            string primaryKeyOrder = "",
            string defaultValue = "",
            string allowedValues = "",
            string foreignKey = "",
            string description = "",
            int sourceRowNumber = 2)
        {
            return new CsvSchemaDictionaryRow(
                fileName,
                columnOrder,
                columnName,
                dataType,
                required,
                primaryKeyOrder,
                defaultValue,
                allowedValues,
                foreignKey,
                description,
                sourceRowNumber);
        }
    }
}
