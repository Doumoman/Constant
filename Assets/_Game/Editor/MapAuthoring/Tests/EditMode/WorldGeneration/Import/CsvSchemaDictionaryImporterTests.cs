using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Import
{
    public sealed class CsvSchemaDictionaryImporterTests
    {
        [Test]
        public void CanonicalDictionaryBuildsSixtyFilesAndSixHundredSeventyNineColumns()
        {
            var imported = ImportCanonical();
            var built = new CsvSchemaCatalogBuilder().Build(imported.Rows);

            Assert.That(built.Success, Is.True,
                string.Join("\n", built.Errors.Select(error => error.ToString())));
            Assert.That(built.Catalog.FileCount, Is.EqualTo(60));
            Assert.That(built.Catalog.ColumnCount, Is.EqualTo(679));
        }

        [Test]
        public void CanonicalDictionaryHasExactDataTypeCounts()
        {
            var counts = ImportCanonical().Rows
                .GroupBy(row => row.DataType, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "STRING", 75 },
                { "ID", 174 },
                { "INT", 210 },
                { "ULONG", 10 },
                { "FLOAT", 18 },
                { "BOOL", 83 },
                { "ENUM", 61 },
                { "ID_LIST", 30 },
                { "ENUM_LIST", 7 },
                { "INT_LIST", 5 },
                { "HEX", 4 },
                { "DATETIME", 2 },
            };

            Assert.That(counts.Count, Is.EqualTo(expected.Count));
            foreach (var pair in expected)
            {
                Assert.That(counts[pair.Key], Is.EqualTo(pair.Value), pair.Key);
            }
        }

        [Test]
        public void CanonicalDictionaryHasExactRequiredPkFkAndDefaultCounts()
        {
            var rows = ImportCanonical().Rows;

            Assert.That(rows.Count(row => row.Required == "1"), Is.EqualTo(557));
            Assert.That(rows.Count(row => row.Required == "0"), Is.EqualTo(122));
            Assert.That(rows.Count(row => row.PrimaryKeyOrder.Length > 0), Is.EqualTo(103));
            Assert.That(rows.Count(row => row.ForeignKey.Length > 0), Is.EqualTo(84));
            Assert.That(rows.Count(row => row.DefaultValue.Length > 0), Is.EqualTo(33));
        }

        [Test]
        public void CanonicalDictionaryHasUtf8BomAndExactHeader()
        {
            var bytes = File.ReadAllBytes(DictionaryFullPath);
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));

            var text = new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3);
            var firstLine = text.Split('\n')[0].TrimEnd('\r');
            Assert.That(firstLine, Is.EqualTo(CsvSchemaDictionaryImporter.ExpectedHeader));

            var missingBom = new CsvSchemaDictionaryImporter().ParseBytes(
                Encoding.UTF8.GetBytes(CsvSchemaDictionaryImporter.ExpectedHeader + "\n"));
            var wrongHeader = new CsvSchemaDictionaryImporter().ParseBytes(
                WithBom("wrong_header\n"));
            Assert.That(missingBom.Success, Is.False);
            Assert.That(missingBom.Errors.Single(), Does.Contain("UTF-8 BOM"));
            Assert.That(wrongHeader.Success, Is.False);
            Assert.That(wrongHeader.Errors.Single(), Does.Contain("exact 10-column"));
        }

        [TestCase("\r\n")]
        [TestCase("\n")]
        public void ReaderBackedImporterAcceptsQuotedDescriptionWithCommaQuoteAndMultiline(
            string lineEnding)
        {
            var imported = new CsvSchemaDictionaryImporter().ParseBytes(WithBom(
                CsvSchemaDictionaryImporter.ExpectedHeader + lineEnding +
                "a.csv,1,id,ID,1,1,,,,\"hello, \"\"moon\"\"" +
                lineEnding + "line\"" + lineEnding));

            Assert.That(imported.Success, Is.True, string.Join("\n", imported.Errors));
            Assert.That(imported.Rows.Count, Is.EqualTo(1));
            Assert.That(imported.Rows[0].Description,
                Is.EqualTo("hello, \"moon\"" + lineEnding + "line"));
            Assert.That(imported.Rows[0].SourceRowNumber, Is.EqualTo(2));
        }

        [Test]
        public void ReaderBackedImporterRejectsSyntaxAndNonTenFieldRowsDeterministically()
        {
            var syntaxError = new CsvSchemaDictionaryImporter().ParseBytes(WithBom(
                CsvSchemaDictionaryImporter.ExpectedHeader + "\r\n" +
                "a.csv,1,\"unterminated"));
            var wrongFieldCount = new CsvSchemaDictionaryImporter().ParseBytes(WithBom(
                CsvSchemaDictionaryImporter.ExpectedHeader + "\r\n" +
                "a.csv,1,id\r\n"));

            Assert.That(syntaxError.Success, Is.False);
            Assert.That(syntaxError.Rows, Is.Empty);
            Assert.That(syntaxError.Errors.Single(), Does.Contain("UnterminatedQuotedField"));
            Assert.That(wrongFieldCount.Success, Is.False);
            Assert.That(wrongFieldCount.Rows, Is.Empty);
            Assert.That(wrongFieldCount.Errors.Any(error => error.Contains("exactly 10 fields")), Is.True);
        }

        [Test]
        public void ImportDoesNotModifyCanonicalSourceBytes()
        {
            var before = ComputeSha256(File.ReadAllBytes(DictionaryFullPath));
            var imported = ImportCanonical();
            var after = ComputeSha256(File.ReadAllBytes(DictionaryFullPath));

            Assert.That(imported.Success, Is.True);
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void ReaderBackedImporterAcceptsCrLfAndLfLineBoundaries()
        {
            var row = "a.csv,1,id,ID,1,1,,,,";
            var crlf = new CsvSchemaDictionaryImporter().ParseBytes(WithBom(
                CsvSchemaDictionaryImporter.ExpectedHeader + "\r\n" + row + "\r\n"));
            var lf = new CsvSchemaDictionaryImporter().ParseBytes(WithBom(
                CsvSchemaDictionaryImporter.ExpectedHeader + "\n" + row + "\n"));

            Assert.That(crlf.Success, Is.True, string.Join("\n", crlf.Errors));
            Assert.That(lf.Success, Is.True, string.Join("\n", lf.Errors));
            Assert.That(crlf.Rows.Count, Is.EqualTo(1));
            Assert.That(lf.Rows.Count, Is.EqualTo(1));
            Assert.That(crlf.Rows[0].SourceRowNumber, Is.EqualTo(2));
            Assert.That(lf.Rows[0].SourceRowNumber, Is.EqualTo(2));
        }

        private static CsvSchemaDictionaryImportResult ImportCanonical()
        {
            var result = new CsvSchemaDictionaryImporter().Import();
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Rows.Count, Is.EqualTo(679));
            return result;
        }

        private static byte[] WithBom(string text)
        {
            var content = new UTF8Encoding(false, true).GetBytes(text);
            var bytes = new byte[content.Length + 3];
            bytes[0] = 0xEF;
            bytes[1] = 0xBB;
            bytes[2] = 0xBF;
            Buffer.BlockCopy(content, 0, bytes, 3, content.Length);
            return bytes;
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty);
            }
        }

        private static string DictionaryFullPath =>
            Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                CsvSchemaDictionaryImporter.DictionaryProjectRelativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));
    }
}
