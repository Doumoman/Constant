using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MicroPatterns;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.MicroPatterns
{
    [TestFixture]
    [Category("MAP10_01")]
    public sealed class MicroPatternCellSchemaTests
    {
        [Test]
        public void TokenCodecIsExactOrdinalAndUsesApprovedRuntimeAuthority()
        {
            var operations = new[]
            {
                Tuple.Create("NO_CHANGE", MicroPatternOperation.NoChange),
                Tuple.Create("ADD_SOLID", MicroPatternOperation.AddSolid),
                Tuple.Create("CARVE_AIR", MicroPatternOperation.CarveAir),
                Tuple.Create("SURFACE", MicroPatternOperation.SetSurface),
                Tuple.Create("AFFORDANCE", MicroPatternOperation.SetAffordance),
                Tuple.Create("MATERIAL", MicroPatternOperation.SetMaterial),
                Tuple.Create("HAZARD", MicroPatternOperation.SetHazard),
                Tuple.Create("MARKER", MicroPatternOperation.SetMarker),
            };
            foreach (var pair in operations)
            {
                Assert.That(MicroPatternCellTokenCodec.TryParseOperation(pair.Item1, out var value), Is.True);
                Assert.That(value, Is.EqualTo(pair.Item2));
                Assert.That(MicroPatternCellTokenCodec.ToOperationToken(value), Is.EqualTo(pair.Item1));
            }

            Assert.That(MicroPatternCellTokenCodec.TryParseOperation("SET_SURFACE", out _), Is.False);
            Assert.That(MicroPatternCellTokenCodec.TryParseOperation(" surface", out _), Is.False);
            Assert.That(MicroPatternCellTokenCodec.TryParseLayer("geometry", out _), Is.False);
            Assert.That(MicroPatternCellTokenCodec.TryParseTransform("MirrorX", out _), Is.False);
            Assert.That(MicroPatternCellTokenCodec.TryParseProtectedPolicy("force_no_change", out _), Is.False);
        }

        [Test]
        public void ExactSixteenCellsPublishNormalizedImmutableCatalog()
        {
            var result = Build(ValidCatalog(), ValidCells());

            Assert.That(result.Success, Is.True, Errors(result));
            Assert.That(result.Published, Is.True);
            Assert.That(result.Catalog.Count, Is.EqualTo(1));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"));
            var definition = result.Catalog.Definitions.Single();
            Assert.That(definition.Cells.Count, Is.EqualTo(16));
            Assert.That(definition.Cells.Select(cell => MicroPatternDefinition.CanonicalCellIndex(cell.Coordinate)),
                Is.EqualTo(Enumerable.Range(0, 16)));
            Assert.That(definition.Cells.All(cell => cell.Instructions.Count == 6), Is.True);
            Assert.That(definition.Cells.SelectMany(cell => cell.Instructions)
                .All(instruction => instruction.Operation == MicroPatternOperation.NoChange), Is.True);
            Assert.That(MicroPatternValidator.Validate(definition).IsValid, Is.True);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternDefinition>)result.Catalog.Definitions).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<MicroPatternId, MicroPatternDefinition>)result.Catalog.DefinitionsById).Clear());
        }

        [Test]
        public void RowOrderDoesNotChangeDefinitionOrCatalogDigest()
        {
            var cells = ValidCells();
            cells.Add(new MicroPatternCellRowV2(
                PatternId, "0", "0", "SURFACE", "SURFACE", "STONE", CellsFile, 50));
            var first = Build(ValidCatalog("R0|MIRROR_X"), cells);
            var second = Build(ValidCatalog("MIRROR_X|R0"), cells.AsEnumerable().Reverse());

            Assert.That(first.Success, Is.True, Errors(first));
            Assert.That(second.Success, Is.True, Errors(second));
            Assert.That(second.StableDigest, Is.EqualTo(first.StableDigest));
            Assert.That(second.Catalog.Definitions.Single().ComputeStableDigest(),
                Is.EqualTo(first.Catalog.Definitions.Single().ComputeStableDigest()));
        }

        [Test]
        public void MissingDuplicateAndOutOfRangeCoordinatesAccumulateWithoutPublish()
        {
            var cells = ValidCells();
            cells.RemoveAt(15);
            cells.Add(new MicroPatternCellRowV2(
                PatternId, "0", "0", "NO_CHANGE", "GEOMETRY", string.Empty, CellsFile, 40));
            cells.Add(new MicroPatternCellRowV2(
                PatternId, "4", "-1", "NO_CHANGE", "GEOMETRY", string.Empty, CellsFile, 41));

            var result = Build(ValidCatalog(), cells);
            Assert.That(result.Success, Is.False);
            AssertCodes(result,
                MicroPatternCellSchemaErrorCode.MissingCell,
                MicroPatternCellSchemaErrorCode.DuplicateCellLayer,
                MicroPatternCellSchemaErrorCode.InvalidCoordinate,
                MicroPatternCellSchemaErrorCode.AtomicPublishRejected);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
        }

        [Test]
        public void SameCoordinateAllowsDistinctLayersButRejectsDuplicateLayer()
        {
            var cells = ValidCells();
            cells.Add(new MicroPatternCellRowV2(
                PatternId, "0", "0", "SURFACE", "SURFACE", "STONE", CellsFile, 40));
            var accepted = Build(ValidCatalog(), cells);
            Assert.That(accepted.Success, Is.True, Errors(accepted));
            var zero = accepted.Catalog.Definitions.Single().Cells.Single(cell =>
                cell.Coordinate.X == 0 && cell.Coordinate.Y == 0);
            Assert.That(zero.Instructions.Single(value => value.Layer == MicroPatternLayer.Surface).Operation,
                Is.EqualTo(MicroPatternOperation.SetSurface));

            cells.Add(new MicroPatternCellRowV2(
                PatternId, "0", "0", "NO_CHANGE", "SURFACE", string.Empty, CellsFile, 41));
            AssertCodes(Build(ValidCatalog(), cells), MicroPatternCellSchemaErrorCode.DuplicateCellLayer);
        }

        [Test]
        public void LayerOperationAndPayloadMatrixIsEnforcedExactly()
        {
            var validSetOperations = new[]
            {
                Tuple.Create("SURFACE", "SURFACE"),
                Tuple.Create("AFFORDANCE", "AFFORDANCE"),
                Tuple.Create("MATERIAL", "MATERIAL"),
                Tuple.Create("HAZARD", "HAZARD"),
                Tuple.Create("MARKER", "MARKER"),
            };
            foreach (var pair in validSetOperations)
            {
                var cells = ValidCells();
                cells.Add(new MicroPatternCellRowV2(
                    PatternId, "0", "0", pair.Item1, pair.Item2, "PAYLOAD_1", CellsFile, 40));
                Assert.That(Build(ValidCatalog(), cells).Success, Is.True, pair.Item1);
            }

            var mismatch = ValidCells();
            mismatch.Add(new MicroPatternCellRowV2(
                PatternId, "0", "0", "HAZARD", "SURFACE", "SPIKES", CellsFile, 40));
            AssertCodes(Build(ValidCatalog(), mismatch),
                MicroPatternCellSchemaErrorCode.LayerOperationMismatch);

            var missing = ValidCells();
            missing.Add(new MicroPatternCellRowV2(
                PatternId, "0", "0", "SURFACE", "SURFACE", string.Empty, CellsFile, 40));
            AssertCodes(Build(ValidCatalog(), missing), MicroPatternCellSchemaErrorCode.MissingPayload);

            var unexpected = ValidCells();
            unexpected[0] = new MicroPatternCellRowV2(
                PatternId, "0", "0", "ADD_SOLID", "GEOMETRY", "STONE", CellsFile, 2);
            AssertCodes(Build(ValidCatalog(), unexpected),
                MicroPatternCellSchemaErrorCode.UnexpectedPayload);

            var invalid = ValidCells();
            invalid.Add(new MicroPatternCellRowV2(
                PatternId, "0", "0", "SURFACE", "SURFACE", "bad payload", CellsFile, 40));
            AssertCodes(Build(ValidCatalog(), invalid), MicroPatternCellSchemaErrorCode.InvalidPayload);
        }

        [Test]
        public void CatalogCellForeignKeyAndPresenceAreAtomic()
        {
            AssertCodes(Build(ValidCatalog(), Array.Empty<MicroPatternCellRowV2>()),
                MicroPatternCellSchemaErrorCode.MissingCellRows);
            AssertCodes(Build(Array.Empty<MicroPatternCatalogRowV2>(), ValidCells()),
                MicroPatternCellSchemaErrorCode.OrphanCellRow);

            var duplicateCatalog = new[] { ValidCatalog().Single(), ValidCatalog().Single() };
            AssertCodes(Build(duplicateCatalog, ValidCells()),
                MicroPatternCellSchemaErrorCode.DuplicatePatternId);
        }

        [Test]
        public void UnknownAndAliasTokensAreRejectedWithoutFallback()
        {
            var unknownLayer = ValidCells();
            unknownLayer[0] = new MicroPatternCellRowV2(
                PatternId, "0", "0", "NO_CHANGE", "geometry", string.Empty, CellsFile, 2);
            AssertCodes(Build(ValidCatalog(), unknownLayer), MicroPatternCellSchemaErrorCode.UnknownLayer);

            var unknownOperation = ValidCells();
            unknownOperation[0] = new MicroPatternCellRowV2(
                PatternId, "0", "0", "SET_SURFACE", "SURFACE", "STONE", CellsFile, 2);
            AssertCodes(Build(ValidCatalog(), unknownOperation),
                MicroPatternCellSchemaErrorCode.UnknownOperation);

            var invalidCatalog = new[]
            {
                new MicroPatternCatalogRowV2("mp_bad", " 1", "MoonCrater|MoonCrater",
                    "R0|r180", "force_no_change", CatalogFile, 2),
            };
            AssertCodes(Build(invalidCatalog, ValidCells()),
                MicroPatternCellSchemaErrorCode.InvalidCatalogField);
        }

        [Test]
        public void ExistingDomainValidatorRejectsMissingR0AndPreventsPartialCatalog()
        {
            var result = Build(ValidCatalog("MIRROR_X"), ValidCells());
            AssertCodes(result,
                MicroPatternCellSchemaErrorCode.DomainValidationFailed,
                MicroPatternCellSchemaErrorCode.AtomicPublishRejected);
            Assert.That(result.Errors.Any(value => value.Detail.Contains("MissingR0")), Is.True);
            Assert.That(result.Catalog, Is.Null);
        }

        [Test]
        public void HeaderOnlySchemaStateSucceedsWithoutPublishingContent()
        {
            var result = Build(
                Array.Empty<MicroPatternCatalogRowV2>(),
                Array.Empty<MicroPatternCellRowV2>());
            Assert.That(result.Success, Is.True);
            Assert.That(result.IsHeaderOnly, Is.True);
            Assert.That(result.Published, Is.False);
            Assert.That(result.Catalog, Is.Null);
        }

        [Test]
        public void NewRuntimeSurfaceHasNoExecutionFileRngOrDuplicateAuthority()
        {
            var root = FullPath("Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns");
            var source = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(File.ReadAllText));
            var forbidden = new[]
            {
                "UnityEditor", "MonoBehaviour", "System.Random", "UnityEngine.Random",
                "File.", "Directory.", "DateTime", "StageMapGenerator", "GridWorld",
            };
            Assert.That(forbidden.Where(source.Contains), Is.Empty);
            Assert.That(typeof(MicroPatternCellSchemaResult).GetProperties(
                    BindingFlags.Instance | BindingFlags.Public).Where(value => value.CanWrite),
                Is.Empty);
        }

        private static MicroPatternCellSchemaResult Build(
            IEnumerable<MicroPatternCatalogRowV2> catalog,
            IEnumerable<MicroPatternCellRowV2> cells)
        {
            return new MicroPatternCellSchemaBuilder().Build(catalog, cells);
        }

        private static IEnumerable<MicroPatternCatalogRowV2> ValidCatalog(string transforms = "R0")
        {
            yield return new MicroPatternCatalogRowV2(
                PatternId, "1", "MoonCrater", transforms,
                "FORCE_NO_CHANGE", CatalogFile, 2);
        }

        private static List<MicroPatternCellRowV2> ValidCells()
        {
            var rows = new List<MicroPatternCellRowV2>();
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    rows.Add(new MicroPatternCellRowV2(
                        PatternId, x.ToString(), y.ToString(), "NO_CHANGE", "GEOMETRY",
                        string.Empty, CellsFile, (y * 4) + x + 2));
                }
            }
            return rows;
        }

        private static void AssertCodes(
            MicroPatternCellSchemaResult result,
            params MicroPatternCellSchemaErrorCode[] expected)
        {
            Assert.That(result.Success, Is.False);
            var codes = result.Errors.Select(value => value.Code).Distinct().ToArray();
            foreach (var code in expected)
                Assert.That(codes, Does.Contain(code), Errors(result));
            Assert.That(result.Errors, Is.EqualTo(result.Errors.OrderBy(value => value).ToArray()));
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
            Assert.That(result.Catalog, Is.Null);
        }

        private static string Errors(MicroPatternCellSchemaResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private const string PatternId = "MP_VALID";
        private const string CatalogFile = "micro_pattern_catalog_v2.csv";
        private const string CellsFile = "micro_pattern_cells_v2.csv";
    }
}
