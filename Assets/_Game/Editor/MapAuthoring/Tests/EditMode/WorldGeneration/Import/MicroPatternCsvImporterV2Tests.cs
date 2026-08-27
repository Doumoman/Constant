using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.Import
{
    [TestFixture]
    [Category("MAP10_01")]
    public sealed class MicroPatternCsvImporterV2Tests
    {
        [Test]
        public void ProjectFilesHaveExactPathBomHeaderAndHeaderOnlyState()
        {
            var catalogPath = FullPath(MicroPatternCsvImporterV2.CatalogProjectRelativePath);
            var cellsPath = FullPath(MicroPatternCsvImporterV2.CellsProjectRelativePath);
            AssertHeaderOnly(catalogPath, MicroPatternCsvImporterV2.CatalogExpectedHeader);
            AssertHeaderOnly(cellsPath, MicroPatternCsvImporterV2.CellsExpectedHeader);

            var result = new MicroPatternCsvImporterV2().Import();
            Assert.That(result.Success, Is.True, Errors(result));
            Assert.That(result.IsHeaderOnly, Is.True);
            Assert.That(result.Published, Is.False);
            Assert.That(result.Catalog, Is.Null);
        }

        [Test]
        public void Rfc4180RowsImportExactSixteenCellsWithStableProvenance()
        {
            var catalog = MicroPatternCsvImporterV2.CatalogExpectedHeader + "\r\n" +
                          "MP_ALPHA,5,\"MoonCrater\",R0,FORCE_NO_CHANGE\r\n";
            var cells = MicroPatternCsvImporterV2.CellsExpectedHeader + "\r\n" +
                        CellRows("MP_ALPHA", "\r\n") +
                        "MP_ALPHA,0,0,SURFACE,SURFACE,STONE\r\n";
            var result = new MicroPatternCsvImporterV2().ParseBytes(Utf8Bom(catalog), Utf8Bom(cells));

            Assert.That(result.Success, Is.True, Errors(result));
            Assert.That(result.Published, Is.True);
            Assert.That(result.Catalog.Count, Is.EqualTo(1));
            Assert.That(result.Catalog.Definitions.Single().Cells, Has.Count.EqualTo(16));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void BomHeaderAndFieldCountFailuresRejectAtomicPublication()
        {
            var headerOnlyCells = Utf8Bom(MicroPatternCsvImporterV2.CellsExpectedHeader + "\n");
            var noBom = new UTF8Encoding(false).GetBytes(
                MicroPatternCsvImporterV2.CatalogExpectedHeader + "\n");
            AssertCodes(new MicroPatternCsvImporterV2().ParseBytes(noBom, headerOnlyCells),
                MicroPatternCellSchemaErrorCode.InvalidBom,
                MicroPatternCellSchemaErrorCode.AtomicPublishRejected);

            var wrongHeader = Utf8Bom("pattern_id,selection_weight\n");
            AssertCodes(new MicroPatternCsvImporterV2().ParseBytes(wrongHeader, headerOnlyCells),
                MicroPatternCellSchemaErrorCode.HeaderMismatch);

            var catalogWithShortRow = Utf8Bom(
                MicroPatternCsvImporterV2.CatalogExpectedHeader + "\nMP_ALPHA,1\n");
            AssertCodes(new MicroPatternCsvImporterV2().ParseBytes(catalogWithShortRow, headerOnlyCells),
                MicroPatternCellSchemaErrorCode.RowFieldCountMismatch);
        }

        [Test]
        public void SemanticErrorsPreserveFileRecordPatternCoordinateLayerAndStableOrder()
        {
            var catalog = Utf8Bom(MicroPatternCsvImporterV2.CatalogExpectedHeader + "\n" +
                                  "MP_ALPHA,1,MoonCrater,R0,FORCE_NO_CHANGE\n");
            var cells = Utf8Bom(MicroPatternCsvImporterV2.CellsExpectedHeader + "\n" +
                                "MP_ALPHA,0,0,HAZARD,SURFACE,SPIKES\n");
            var result = new MicroPatternCsvImporterV2().ParseBytes(catalog, cells);

            AssertCodes(result,
                MicroPatternCellSchemaErrorCode.LayerOperationMismatch,
                MicroPatternCellSchemaErrorCode.MissingCell,
                MicroPatternCellSchemaErrorCode.AtomicPublishRejected);
            var mismatch = result.Errors.Single(value =>
                value.Code == MicroPatternCellSchemaErrorCode.LayerOperationMismatch);
            Assert.That(mismatch.FilePath, Is.EqualTo(MicroPatternCsvImporterV2.CellsProjectRelativePath));
            Assert.That(mismatch.RecordNumber, Is.EqualTo(2));
            Assert.That(mismatch.PatternId, Is.EqualTo("MP_ALPHA"));
            Assert.That(mismatch.X, Is.EqualTo(0));
            Assert.That(mismatch.Y, Is.EqualTo(0));
            Assert.That(mismatch.Layer, Is.EqualTo("SURFACE"));
            Assert.That(result.Errors, Is.EqualTo(result.Errors.OrderBy(value => value).ToArray()));
        }

        [Test]
        public void ImportDigestIsIndependentOfCatalogAndCellRowOrder()
        {
            var catalogs = new[]
            {
                "MP_BETA,2,CassiaRoot,R0,REJECT_CANDIDATE",
                "MP_ALPHA,1,MoonCrater,R0,FORCE_NO_CHANGE",
            };
            var cells = CellRows("MP_BETA", "\n").Split(
                    new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Concat(CellRows("MP_ALPHA", "\n").Split(
                    new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();
            var first = Import(catalogs, cells);
            var second = Import(catalogs.Reverse(), cells.Reverse());

            Assert.That(first.Success, Is.True, Errors(first));
            Assert.That(second.Success, Is.True, Errors(second));
            Assert.That(second.StableDigest, Is.EqualTo(first.StableDigest));
            Assert.That(second.Catalog.Definitions.Select(value => value.Id.Value),
                Is.EqualTo(new[] { "MP_ALPHA", "MP_BETA" }));
        }

        [Test]
        public void RegistryAndLegacyAuthoringSubsetRemainExactWhileTotalBecomesFiftyTwo()
        {
            var registry = V2AuthoringSchemaRegistry.DescribeDefaultTables();
            Assert.That(V2AuthoringSchemaCanonicalDigest.Compute(registry),
                Is.EqualTo("272ec4f449a17179158720c94e92f6982cb5a32427ce6f6ea8ffc5eb92050621"));

            var root = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var allCsv = Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories);
            var allMeta = Directory.GetFiles(root, "*.csv.meta", SearchOption.AllDirectories);
            var legacyCsv = allCsv.Where(path => !Relative(root, path).StartsWith(
                "MicroPattern/", StringComparison.Ordinal)).ToArray();
            var legacyMeta = allMeta.Where(path => !Relative(root, path).StartsWith(
                "MicroPattern/", StringComparison.Ordinal)).ToArray();

            Assert.That(allCsv, Has.Length.EqualTo(52));
            Assert.That(allMeta, Has.Length.EqualTo(52));
            Assert.That(legacyCsv, Has.Length.EqualTo(50));
            Assert.That(legacyMeta, Has.Length.EqualTo(50));
            Assert.That(ComputeManifest(root, legacyCsv),
                Is.EqualTo("f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb"));

            var generated = FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated");
            Assert.That(Directory.GetFiles(generated, "*.csv", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void ImporterUsesOnlyTwoExactInputsAndHasNoWriterWatcherAssetOrRuntimeSideEffect()
        {
            Assert.That(MicroPatternCsvImporterV2.CatalogProjectRelativePath,
                Is.EqualTo("Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_catalog_v2.csv"));
            Assert.That(MicroPatternCsvImporterV2.CellsProjectRelativePath,
                Is.EqualTo("Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_cells_v2.csv"));
            var source = File.ReadAllText(FullPath(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/MicroPatternCsvImporterV2.cs"));
            var forbidden = new[]
            {
                "File.Write", "Directory.GetFiles", "AssetDatabase", "ScriptableObject",
                "EditorWindow", "FileSystemWatcher", "SceneManager", "System.Random",
                "UnityEngine.Random", "DateTime", "Generated/",
            };
            Assert.That(forbidden.Where(source.Contains), Is.Empty);
        }

        private static MicroPatternCsvImportResult Import(
            IEnumerable<string> catalogRows,
            IEnumerable<string> cellRows)
        {
            var catalog = MicroPatternCsvImporterV2.CatalogExpectedHeader + "\n" +
                          string.Join("\n", catalogRows) + "\n";
            var cells = MicroPatternCsvImporterV2.CellsExpectedHeader + "\n" +
                        string.Join("\n", cellRows) + "\n";
            return new MicroPatternCsvImporterV2().ParseBytes(Utf8Bom(catalog), Utf8Bom(cells));
        }

        private static string CellRows(string patternId, string newline)
        {
            var rows = new List<string>();
            for (var y = 0; y < 4; y++)
                for (var x = 0; x < 4; x++)
                    rows.Add(patternId + "," + x + "," + y + ",NO_CHANGE,GEOMETRY,");
            return string.Join(newline, rows) + newline;
        }

        private static void AssertHeaderOnly(string path, string expectedHeader)
        {
            var bytes = File.ReadAllBytes(path);
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(File.ReadAllText(path, Encoding.UTF8), Is.EqualTo(expectedHeader + "\n"));
        }

        private static void AssertCodes(
            MicroPatternCsvImportResult result,
            params MicroPatternCellSchemaErrorCode[] expected)
        {
            Assert.That(result.Success, Is.False);
            var codes = result.Errors.Select(value => value.Code).Distinct().ToArray();
            foreach (var code in expected)
                Assert.That(codes, Does.Contain(code), Errors(result));
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
        }

        private static string ComputeManifest(string root, IEnumerable<string> paths)
        {
            var noBom = new UTF8Encoding(false);
            var withBom = new UTF8Encoding(true);
            var records = paths.Select(path => new { Path = path, Relative = Relative(root, path) })
                .OrderBy(value => value.Relative, StringComparer.Ordinal)
                .Select(value =>
                {
                    var normalized = File.ReadAllText(value.Path, Encoding.UTF8)
                        .Replace("\r\n", "\n").Replace("\r", "\n");
                    return value.Relative + "\t" + Sha256(
                        withBom.GetPreamble().Concat(noBom.GetBytes(normalized)).ToArray());
                });
            return Sha256(noBom.GetBytes(string.Join("\n", records)));
        }

        private static string Relative(string root, string path)
        {
            return path.Substring(root.Length + 1).Replace('\\', '/');
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
        }

        private static byte[] Utf8Bom(string value)
        {
            var noBom = new UTF8Encoding(false);
            var withBom = new UTF8Encoding(true);
            return withBom.GetPreamble().Concat(noBom.GetBytes(value)).ToArray();
        }

        private static string Errors(MicroPatternCsvImportResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
