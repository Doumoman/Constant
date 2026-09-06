using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.MoonPalace
{
    public sealed class MoonPalaceMicroPatternProductionTests
    {
        private const string CategoryName = "MAP21_02";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace.MoonPalaceMicroPatternPublisher";
        private const string ZeroDigest =
            "0000000000000000000000000000000000000000000000000000000000000000";

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternsContainExact24StarterMappedPatternsAndSixPerBiome()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.Catalog, Has.Count.EqualTo(24));
            CollectionAssert.AreEquivalent(MoonPalaceMicroPatternProduction.RequiredPatternIds,
                production.Catalog.Select(value => value.PatternId));
            Assert.That(production.Catalog.All(value =>
                value.PatternId == value.SourceStarterPatternId &&
                BakingCanonicalDigest.IsLowerHexSha256(value.SourceStarterDigest)), Is.True);
            foreach (var biome in MoonPalaceMicroPatternProduction.RequiredBiomeIds)
                Assert.That(production.Catalog.Count(value => value.BiomeId == biome),
                    Is.EqualTo(6), biome);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternCellsCoverExactly4x4AndPreserveSilhouetteFamilies()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.Cells, Has.Count.EqualTo(384));
            foreach (var pattern in production.Catalog)
            {
                var cells = production.Cells.Where(value =>
                    value.PatternId == pattern.PatternId).ToArray();
                Assert.That(cells, Has.Length.EqualTo(16), pattern.PatternId);
                Assert.That(cells.Select(value => value.CoordinateToken).Distinct().Count(),
                    Is.EqualTo(16), pattern.PatternId);
                Assert.That(cells.All(value => value.X >= 0 && value.X <= 3 &&
                    value.Y >= 0 && value.Y <= 3), Is.True, pattern.PatternId);
                Assert.That(cells.All(value => value.SourceOperation == "NO_CHANGE" ||
                    value.SourceOperation == "ADD_SOLID" ||
                    value.SourceOperation == "CARVE_AIR"), Is.True, pattern.PatternId);
                Assert.That(string.IsNullOrWhiteSpace(pattern.SilhouetteFamily), Is.False,
                    pattern.PatternId);
            }
            CollectionAssert.AreEquivalent(MoonPalaceMicroPatternProduction.RequiredPatternIds,
                production.PreservedSilhouettePatternIds);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternsMapCellsToTileShellRolesCollisionAndFallbacks()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.TileShellRecordsRead, Is.EqualTo(10));
            Assert.That(production.TileCodeLookupsResolved, Is.EqualTo(384));
            Assert.That(production.MissingFallbackCellCount, Is.EqualTo(384));
            Assert.That(production.UnknownTileCodeReferences, Is.Zero);
            Assert.That(production.Cells.All(value =>
                !string.IsNullOrWhiteSpace(value.TileCode) &&
                !string.IsNullOrWhiteSpace(value.MaterialToken) &&
                !string.IsNullOrWhiteSpace(value.FallbackTileCode)), Is.True);
            Assert.That(production.Cells.Where(value =>
                    value.TileRole == MoonPalaceTileRole.OneWayPlatform)
                .All(value => value.CollisionKind == MoonPalaceCollisionKind.OneWay), Is.True);
            Assert.That(production.Cells.Where(value =>
                    value.TileRole == MoonPalaceTileRole.Background)
                .All(value => value.CollisionKind == MoonPalaceCollisionKind.Background), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternsRecordSurfaceMaterialAffordanceHazardAndMarkerTags()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.Tags, Has.Count.EqualTo(69));
            Assert.That(production.Tags.Count(value =>
                value.TagKind == MoonPalaceProductionTagKind.Surface), Is.EqualTo(16));
            Assert.That(production.Tags.Count(value =>
                value.TagKind == MoonPalaceProductionTagKind.Material), Is.EqualTo(26));
            Assert.That(production.Tags.Count(value =>
                value.TagKind == MoonPalaceProductionTagKind.Affordance), Is.EqualTo(10));
            Assert.That(production.Tags.Count(value =>
                value.TagKind == MoonPalaceProductionTagKind.Hazard), Is.EqualTo(8));
            Assert.That(production.Tags.Count(value =>
                value.TagKind == MoonPalaceProductionTagKind.Marker), Is.EqualTo(9));
            Assert.That(production.Catalog.Count(value => value.PatternRole ==
                MoonPalaceProductionPatternRole.GeometrySilhouette), Is.EqualTo(12));
            Assert.That(production.Catalog.Count(value => value.PatternRole ==
                MoonPalaceProductionPatternRole.SurfaceAffordance), Is.EqualTo(4));
            Assert.That(production.Catalog.Count(value => value.PatternRole ==
                MoonPalaceProductionPatternRole.MaterialHazardMarker), Is.EqualTo(8));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternsRepresentMissingAssetsWithoutImportOrRuntimeBinding()
        {
            var sample = CreateSample(FixedUtc, false);
            var production = Production(sample);

            Assert.That(production.Cells.All(value =>
                value.AssetReferenceKind == MoonPalaceAssetReferenceKind.MissingData &&
                !string.IsNullOrWhiteSpace(value.FallbackTileCode) &&
                !string.IsNullOrWhiteSpace(value.MissingReason)), Is.True);
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(MoonPalaceRuntimeBindingState)),
                production.Tags.Select(value => value.RuntimeBindingState).Distinct().ToArray());
            Assert.That(typeof(MoonPalaceMicroPatternProduction).IsSubclassOf(
                typeof(UnityEngine.Object)), Is.False);
            Assert.That(Counter(Property(sample, "Counters"), "AssetImports"), Is.Zero);
            Assert.That(Counter(Property(sample, "Counters"), "RuntimeBindingsCreated"), Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternsRejectDuplicateIdsInvalidCellsUnknownTilesAndBadRanges()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Cell("KNOWN", 4, "KNOWN",
                new[] { "KNOWN" }));
            Assert.Throws<ArgumentException>(() => Cell("UNKNOWN", 0, "UNKNOWN",
                new[] { "KNOWN" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Catalog("DUPLICATE", 0));

            var duplicate = Catalog("DUPLICATE", 1);
            Assert.Throws<ArgumentException>(() => new MoonPalaceMicroPatternProduction(
                new[] { duplicate, duplicate }, Array.Empty<MoonPalaceProductionPatternCell>(),
                Array.Empty<MoonPalaceProductionPatternTag>(), Array.Empty<string>(),
                10, 0, 0, 0, string.Empty));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternArtifactsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
                var forward = CreateSample(FixedUtc, false);
                CultureInfo.CurrentCulture = new CultureInfo("ko-KR");
                CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
                var reverse = CreateSample(FixedUtc, true);

                Assert.That(Production(forward).SerializeCatalogManifest(),
                    Is.EqualTo(Production(reverse).SerializeCatalogManifest()));
                Assert.That(Production(forward).SerializeCellManifest(),
                    Is.EqualTo(Production(reverse).SerializeCellManifest()));
                Assert.That(Production(forward).SerializeCatalogCsv(),
                    Is.EqualTo(Production(reverse).SerializeCatalogCsv()));
                Assert.That(Production(forward).SerializeCellCsv(),
                    Is.EqualTo(Production(reverse).SerializeCellCsv()));
                Assert.That(Production(forward).SerializeTagCsv(),
                    Is.EqualTo(Production(reverse).SerializeTagCsv()));
                Assert.That(DigestManifest(forward).Serialize(),
                    Is.EqualTo(DigestManifest(reverse).Serialize()));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternPublisherWritesOnlyMap21_02AuthoringAndGeneratedRoots()
        {
            var sourcePaths = new[]
            {
                Constant("SourceMap10CatalogRelativePath"),
                Constant("SourceMap10CellsRelativePath"),
                Constant("SourceMap2101TileShellManifestRelativePath"),
                Constant("SourceMap2101DigestManifestRelativePath"),
            };
            var before = sourcePaths.ToDictionary(value => value,
                value => FileSha256(ProjectPath(value)), StringComparer.Ordinal);
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var written = ((IEnumerable<string>)Property(sample, "WrittenRelativePaths")).ToArray();

            Assert.That(written, Has.Length.EqualTo(6));
            Assert.That(written.All(value =>
                value.StartsWith(Constant("AuthoringDirectoryRelativePath") + "/",
                    StringComparison.Ordinal) ||
                value.StartsWith(Constant("GeneratedDirectoryRelativePath") + "/",
                    StringComparison.Ordinal)), Is.True);
            Assert.That(written.All(value => File.Exists(ProjectPath(value))), Is.True);
            foreach (var sourcePath in sourcePaths)
                Assert.That(FileSha256(ProjectPath(sourcePath)), Is.EqualTo(before[sourcePath]),
                    sourcePath);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternsPublishMap21_03HandoffOnlyAfterFocusedPass()
        {
            var before = OutputSnapshot();
            var blocked = Assert.Throws<TargetInvocationException>(() =>
                InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());
            CollectionAssert.AreEquivalent(before, OutputSnapshot());

            var published = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                DigestManifest(published).Map2103HandoffDigest), Is.True);
            Assert.That(Directory.GetFiles(ProjectPath(
                Constant("AuthoringDirectoryRelativePath")), "*.csv"), Has.Length.EqualTo(3));
            Assert.That(Directory.GetFiles(ProjectPath(
                Constant("GeneratedDirectoryRelativePath")), "*.json"), Has.Length.EqualTo(3));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternsDoNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var counters = Property(CreateSample(FixedUtc, false), "Counters");

            Assert.That((bool)Property(counters, "AllZero"), Is.True);
            Assert.That(Counter(counters, "GenerationExecutions"), Is.Zero);
            Assert.That(Counter(counters, "RendererExecutions"), Is.Zero);
            Assert.That(Counter(counters, "ValidationRunnerExecutions"), Is.Zero);
            Assert.That(Counter(counters, "ReplayExecutions"), Is.Zero);
            Assert.That(Counter(counters, "RollbackExecutions"), Is.Zero);
            Assert.That(Counter(counters, "PlayModeSelections"), Is.Zero);
            Assert.That(Counter(counters, "LegacyRegressionSelections"), Is.Zero);
            Assert.That(Counter(counters, "PriorCategorySelections"), Is.Zero);
            Assert.That(Counter(counters, "UnfilteredOrFullRegressionRuns"), Is.Zero);
            Assert.That(Counter(counters, "RuntimeObjectSpawns"), Is.Zero);
        }

        private static MoonPalaceProductionPatternCell Cell(string patternId, int x,
            string tileCode, IEnumerable<string> knownCodes) =>
            new MoonPalaceProductionPatternCell(patternId, x, 0, "NO_CHANGE", tileCode,
                MoonPalaceTileRole.BoundaryBlend, MoonPalaceCollisionKind.PassThrough,
                "MAT", "FALLBACK", MoonPalaceAssetReferenceKind.MissingData,
                "Missing", "FORCE_NO_CHANGE", knownCodes);

        private static MoonPalaceProductionPatternCatalogRecord Catalog(string patternId,
            int selectionWeight) => new MoonPalaceProductionPatternCatalogRecord(patternId,
                patternId, ZeroDigest, "MoonCrater",
                MoonPalaceProductionPatternRole.GeometrySilhouette, "TEST",
                new[] { "R0" }, selectionWeight,
                MoonPalaceMicroPatternPreconditions.SourceTileShellDigest,
                ZeroDigest, ZeroDigest, "PreserveMissingDataAndFallback",
                MoonPalaceRuntimeBindingState.MissingData);

        private static object CreateSample(string createdUtc, bool reverseInputOrder) =>
            InvokePublisher("CreateReadOnlySample", ProjectRoot, createdUtc, reverseInputOrder);

        private static object InvokePublisher(string methodName, params object[] arguments) =>
            PublisherType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, arguments);

        private static MoonPalaceMicroPatternProduction Production(object sample) =>
            (MoonPalaceMicroPatternProduction)Property(sample, "Production");

        private static MoonPalaceMicroPatternDigestManifest DigestManifest(object sample) =>
            (MoonPalaceMicroPatternDigestManifest)Property(sample, "DigestManifest");

        private static object Property(object instance, string name) =>
            instance.GetType().GetProperty(name).GetValue(instance);

        private static int Counter(object instance, string name) =>
            (int)Property(instance, name);

        private static string Constant(string name) =>
            (string)PublisherType.GetField(name).GetRawConstantValue();

        private static Dictionary<string, string> OutputSnapshot()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var root in new[]
            {
                Constant("AuthoringDirectoryRelativePath"),
                Constant("GeneratedDirectoryRelativePath"),
            })
            {
                var path = ProjectPath(root);
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
                    result[Path.GetFileName(file)] = FileSha256(file);
            }
            return result;
        }

        private static string FileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static string ProjectPath(string relativePath) => Path.Combine(ProjectRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor")
            .GetType(PublisherTypeName, true);

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private const string FixedUtc = "2026-09-06T00:00:00.0000000Z";
    }
}
