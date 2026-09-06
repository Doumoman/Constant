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
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.MoonPalace;
using StarNight.Map.WorldGeneration.TerrainClusters;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.MoonPalace
{
    public sealed class MoonPalaceMillDoughClusterPoolProductionTests
    {
        private const string CategoryName = "MAP21_04";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace.MoonPalaceMillDoughClusterPoolPublisher";
        private const string FixedUtc = "2026-09-06T00:00:00.0000000Z";

        [Test, Category(CategoryName)]
        public void MillDoughClusterPoolContainsExact24ClustersAndNoCraterRootAuthoredRecords()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.Catalog, Has.Count.EqualTo(24));
            CollectionAssert.AreEquivalent(
                MoonPalaceMillDoughClusterPoolProduction.RequiredClusterIds,
                production.Catalog.Select(value => value.ClusterId));
            Assert.That(production.Catalog.Count(value => value.BiomeId == "AbandonedMill"),
                Is.EqualTo(12));
            Assert.That(production.Catalog.Count(value => value.BiomeId == "MoonDough"),
                Is.EqualTo(12));
            Assert.That(production.Catalog.Count(value => value.BiomeId == "MoonCrater" ||
                value.BiomeId == "CassiaRoot"), Is.Zero);
            Assert.That(production.Catalog.Count(value =>
                value.SourceStarterClusterId != "MissingData"), Is.EqualTo(8));
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterPoolHasTerrainQuietBufferCountsAndValidActiveChunkRanges()
        {
            var production = Production(CreateSample(FixedUtc, false));

            foreach (var biome in new[] { "AbandonedMill", "MoonDough" })
            {
                var records = production.Catalog.Where(value => value.BiomeId == biome).ToArray();
                Assert.That(records.Count(value => value.PoolKind ==
                    MoonPalaceClusterPoolKind.Terrain), Is.EqualTo(6), biome);
                Assert.That(records.Count(value => value.PoolKind ==
                    MoonPalaceClusterPoolKind.Quiet), Is.EqualTo(3), biome);
                Assert.That(records.Count(value => value.PoolKind ==
                    MoonPalaceClusterPoolKind.Buffer), Is.EqualTo(3), biome);
            }
            Assert.That(production.Catalog.Where(value => value.PoolKind ==
                    MoonPalaceClusterPoolKind.Terrain)
                .All(value => value.ActiveChunkCount >= 3 && value.ActiveChunkCount <= 5),
                Is.True);
            Assert.That(production.Catalog.Where(value => value.PoolKind ==
                    MoonPalaceClusterPoolKind.Quiet)
                .All(value => value.ActiveChunkCount == 2), Is.True);
            Assert.That(production.Catalog.Where(value => value.PoolKind ==
                    MoonPalaceClusterPoolKind.Buffer)
                .All(value => value.ActiveChunkCount >= 2 && value.ActiveChunkCount <= 3),
                Is.True);
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterFootprintsAreConnectedNormalizedUniqueAndBounded()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.Footprints, Has.Count.EqualTo(78));
            foreach (var cluster in production.Catalog)
            {
                var footprint = production.Footprints.Where(value =>
                    value.ClusterId == cluster.ClusterId).ToArray();
                Assert.That(footprint, Has.Length.EqualTo(cluster.ActiveChunkCount));
                Assert.That(footprint.Select(value => value.CoordinateToken).Distinct().Count(),
                    Is.EqualTo(footprint.Length));
                Assert.That(footprint.Min(value => value.ChunkX), Is.Zero);
                Assert.That(footprint.Min(value => value.ChunkY), Is.Zero);
                Assert.That(footprint.All(value => value.ChunkX <= 3 && value.ChunkY <= 3),
                    Is.True);
                Assert.That(Connected(footprint), Is.True, cluster.ClusterId);
                Assert.That(footprint.Count(value => value.IsEntryChunk), Is.EqualTo(1));
                Assert.That(footprint.Count(value => value.IsExitChunk), Is.EqualTo(1));
            }
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterSpineVariantsHaveSingleBaselineAltPortsAndStaticRouteIntent()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.SpineVariants, Has.Count.EqualTo(48));
            foreach (var cluster in production.Catalog)
            {
                var variants = production.SpineVariants.Where(value =>
                    value.ClusterId == cluster.ClusterId).ToArray();
                var footprint = production.Footprints.Where(value =>
                    value.ClusterId == cluster.ClusterId).ToArray();
                Assert.That(variants, Has.Length.EqualTo(2));
                Assert.That(variants.Count(value => value.IsBaseline), Is.EqualTo(1));
                Assert.That(variants.Count(value => value.SpineVariantId.EndsWith("_BASE",
                    StringComparison.Ordinal)), Is.EqualTo(1));
                Assert.That(variants.Count(value => value.SpineVariantId.EndsWith("_ALT",
                    StringComparison.Ordinal)), Is.EqualTo(1));
                Assert.That(variants.All(value => value.EntrySide ==
                    footprint.Single(item => item.IsEntryChunk).EntrySide && value.ExitSide ==
                    footprint.Single(item => item.IsExitChunk).ExitSide &&
                    value.MovementTokens.Count > 0 && !string.IsNullOrWhiteSpace(
                        value.RouteIntent) && value.ProtectedMaskPolicy ==
                    "PreserveSpineAndEntryExit"), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterPatternSlotsReferenceOnlySameBiomeMap21_02Patterns()
        {
            var production = Production(CreateSample(FixedUtc, false));
            var patternBiomes = ReadPatternBiomes();

            Assert.That(production.PatternSlots, Has.Count.EqualTo(78));
            Assert.That(production.PatternSlots.All(slot => slot.AllowedPatternIds.Count > 0 &&
                slot.AllowedPatternIds.All(pattern => patternBiomes.TryGetValue(pattern,
                    out var biome) && biome == slot.BiomeId)), Is.True);
            foreach (var footprint in production.Footprints)
                Assert.That(production.PatternSlots.Any(slot =>
                    slot.ClusterId == footprint.ClusterId &&
                    slot.CoordinateToken == footprint.CoordinateToken), Is.True,
                    footprint.ClusterId + "/" + footprint.CoordinateToken);
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterPoolRejectsDuplicateIdsBadFootprintsUnknownPatternsAndInvalidPools()
        {
            var production = Production(CreateSample(FixedUtc, false));
            Assert.Throws<ArgumentException>(() => new MoonPalaceMillDoughClusterPoolProduction(
                production.Catalog.Concat(new[] { production.Catalog[0] }),
                production.Footprints, production.SpineVariants, production.PatternSlots,
                Array.Empty<string>(), Array.Empty<string>(), production.CraterRootCatalog,
                FixedUtc));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MoonPalaceClusterFootprintRecord(production.Catalog[0].ClusterId, 4, 0,
                    ClusterRoleKind.Entry, ClusterPortSide.L, ClusterPortSide.R, true, false));

            var patternBiomes = ReadPatternBiomes();
            Assert.Throws<ArgumentException>(() => new MoonPalaceClusterPatternSlotRecord(
                production.Catalog[0].ClusterId, "BAD_SLOT", 0, 0,
                MoonPalaceClusterPatternSlotKind.BaselineRequired,
                new[] { "MP_UNKNOWN" }, "AbandonedMill", "PreserveSpineAndEntryExit", 0,
                patternBiomes));
            var record = production.Catalog[0];
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoonPalaceClusterCatalogRecord(
                record.ClusterId, record.BiomeId, (MoonPalaceClusterPoolKind)999,
                record.PacingRole, record.AccessClass, record.ActiveChunkCount,
                record.SourceStarterClusterId, record.SourcePatternPoolDigest,
                record.FootprintDigest, record.SpineVariantDigest, record.PatternSlotDigest,
                record.StructuralSignature, record.SilhouetteSignature, record.RepeatGroup));
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterRepetitionSignaturesAreUniqueLocallyAndAcrossCombined48()
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
                var a = Production(forward);
                var b = Production(reverse);

                Assert.That(a.SerializePoolManifest(), Is.EqualTo(b.SerializePoolManifest()));
                Assert.That(a.SerializeSignatureManifest(),
                    Is.EqualTo(b.SerializeSignatureManifest()));
                Assert.That(a.SerializeAllBiomeManifest(),
                    Is.EqualTo(b.SerializeAllBiomeManifest()));
                Assert.That(DigestManifest(forward).Serialize(),
                    Is.EqualTo(DigestManifest(reverse).Serialize()));
                Assert.That(a.Catalog.Select(value => value.StructuralSignature)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(24));
                Assert.That(a.AllBiomeCatalog, Has.Count.EqualTo(48));
                Assert.That(a.AllBiomeCatalog.Select(value => value.StructuralSignature)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(48));
                Assert.That(a.Catalog.GroupBy(value => value.BiomeId + "/" + value.PoolKind)
                    .All(group => group.Select(value => value.SilhouetteSignature)
                        .Distinct(StringComparer.Ordinal).Count() == group.Count()), Is.True);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterPoolKeepsQuietBufferStaticWithoutActivityEventSpecialPopulationOrRewards()
        {
            var sample = CreateSample(FixedUtc, false);
            var production = Production(sample);

            Assert.That(production.ActivitySlotCount, Is.Zero);
            Assert.That(production.EventOverlayCount, Is.Zero);
            Assert.That(production.SpecialRegionBindingCount, Is.Zero);
            Assert.That(production.PopulationSlotCount, Is.Zero);
            Assert.That(production.RewardAnchorCount, Is.Zero);
            Assert.That(production.RuntimeBindingCount, Is.Zero);
            Assert.That(production.TilemapWriteCount, Is.Zero);
            Assert.That(production.QuietHazardBaselineSlotCount, Is.Zero);
            Assert.That(production.BufferForbiddenMarkerReferenceCount, Is.Zero);
            Assert.That(production.BufferLandmarkSearchExecutions, Is.Zero);
            Assert.That(Counter(Property(sample, "Counters"),
                "BufferLandmarkSearchExecutions"), Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterPublisherReadsCraterRootArtifactsWithoutRegeneratingOrRewriting()
        {
            var sources = new[]
            {
                PublisherConstant("SourceMap2103CatalogRelativePath"),
                PublisherConstant("SourceMap2103PoolManifestRelativePath"),
                PublisherConstant("SourceMap2103SignatureManifestRelativePath"),
                PublisherConstant("SourceMap2103DigestManifestRelativePath"),
            };
            var before = sources.ToDictionary(path => path,
                path => FileSha256(FullPath(path)), StringComparer.Ordinal);

            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.CraterRootCatalog, Has.Count.EqualTo(24));
            Assert.That(production.CraterRootCatalog.All(value =>
                value.BiomeId == "MoonCrater" || value.BiomeId == "CassiaRoot"), Is.True);
            foreach (var path in sources)
                Assert.That(FileSha256(FullPath(path)), Is.EqualTo(before[path]), path);
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterPublisherWritesOnlyMap21_04AuthoringAndGeneratedRoots()
        {
            var sources = new[]
            {
                PublisherConstant("SourceMap2102CatalogRelativePath"),
                PublisherConstant("SourceMap2102CellsRelativePath"),
                PublisherConstant("SourceMap2102TagsRelativePath"),
                PublisherConstant("SourceMap11CatalogRelativePath"),
                PublisherConstant("SourceMap2103CatalogRelativePath"),
                PublisherConstant("SourceMap2103PoolManifestRelativePath"),
                PublisherConstant("SourceMap2103SignatureManifestRelativePath"),
                PublisherConstant("SourceMap2103DigestManifestRelativePath"),
            };
            var before = sources.ToDictionary(path => path,
                path => FileSha256(FullPath(path)), StringComparer.Ordinal);
            var sample = Publish(true);
            var written = (IReadOnlyList<string>)Property(sample, "WrittenRelativePaths");

            Assert.That(written, Has.Count.EqualTo(8));
            Assert.That(written.All(path => path.StartsWith(
                    PublisherConstant("AuthoringDirectoryRelativePath") + "/",
                    StringComparison.Ordinal) || path.StartsWith(
                    PublisherConstant("GeneratedDirectoryRelativePath") + "/",
                    StringComparison.Ordinal)), Is.True);
            Assert.That(written.All(path => File.Exists(FullPath(path))), Is.True);
            foreach (var path in sources)
                Assert.That(FileSha256(FullPath(path)), Is.EqualTo(before[path]), path);
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterPoolPublishesMap21_05HandoffOnlyAfterFocusedPass()
        {
            var exception = Assert.Throws<TargetInvocationException>(() => Publish(false));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            var production = Production(CreateSample(FixedUtc, false));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                production.Map2105HandoffDigest), Is.True);
            Assert.That(production.Map2105HandoffDigest, Is.Not.EqualTo(
                MoonPalaceMillDoughClusterPoolPreconditions.SourceMap2104HandoffDigest));
            Assert.That(DigestManifest(CreateSample(FixedUtc, false)).CanonicalDigest,
                Does.Match("^[0-9a-f]{64}$"));
        }

        [Test, Category(CategoryName)]
        public void MillDoughClusterPoolDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var sample = CreateSample(FixedUtc, false);
            var counters = Property(sample, "Counters");
            foreach (var name in new[]
            {
                "GenerationExecutions", "RendererExecutions", "ValidationRunnerExecutions",
                "ReplayExecutions", "RollbackExecutions", "PlayModeSelections",
                "LegacyRegressionSelections", "PriorCategorySelections",
                "UnfilteredOrFullRegressionRuns", "RuntimeObjectSpawns",
                "CraterRootClustersAuthored", "BufferLandmarkSearchExecutions",
                "CsvWritesOutsideMap2104",
            })
                Assert.That(Counter(counters, name), Is.Zero, name);
            Assert.That((bool)counters.GetType().GetProperty("AllZero").GetValue(counters),
                Is.True);
            Assert.That(typeof(MoonPalaceMillDoughClusterPoolProduction).IsSubclassOf(
                typeof(UnityEngine.Object)), Is.False);
            Assert.That(Directory.Exists(Path.Combine(ProjectRoot,
                "MapDesign/MCP/GENERATED/MAP21_05")), Is.False);
        }

        private static bool Connected(IReadOnlyCollection<MoonPalaceClusterFootprintRecord> values)
        {
            var remaining = new HashSet<string>(values.Select(value => value.CoordinateToken),
                StringComparer.Ordinal);
            var queue = new Queue<MoonPalaceClusterFootprintRecord>();
            queue.Enqueue(values.First());
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!remaining.Remove(current.CoordinateToken)) continue;
                foreach (var candidate in values)
                    if (remaining.Contains(candidate.CoordinateToken) &&
                        Math.Abs(candidate.ChunkX - current.ChunkX) +
                        Math.Abs(candidate.ChunkY - current.ChunkY) == 1)
                        queue.Enqueue(candidate);
            }
            return remaining.Count == 0;
        }

        private static IReadOnlyDictionary<string, string> ReadPatternBiomes()
        {
            var relative = PublisherConstant("SourceMap2102CatalogRelativePath");
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(FullPath(relative)),
                relative);
            Assert.That(read.Success, Is.True);
            var headers = read.Records[0].Fields.Select(field => field.Value.Trim()).ToArray();
            var idIndex = Array.IndexOf(headers, "pattern_id");
            var biomeIndex = Array.IndexOf(headers, "biome_id");
            return read.Records.Skip(1).ToDictionary(record =>
                record.Fields[idIndex].Value.Trim(), record =>
                record.Fields[biomeIndex].Value.Trim(), StringComparer.Ordinal);
        }

        private static object CreateSample(string createdUtc, bool reverseInputOrder) =>
            PublisherType.GetMethod("CreateReadOnlySample", BindingFlags.Public |
                BindingFlags.Static).Invoke(null, new object[]
                { ProjectRoot, createdUtc, reverseInputOrder });

        private static object Publish(bool focusedPass) => PublisherType.GetMethod(
            "PublishAuthoringAndSamples", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { ProjectRoot, focusedPass });

        private static MoonPalaceMillDoughClusterPoolProduction Production(object sample) =>
            (MoonPalaceMillDoughClusterPoolProduction)Property(sample, "Production");

        private static MoonPalaceMillDoughClusterDigestManifest DigestManifest(object sample) =>
            (MoonPalaceMillDoughClusterDigestManifest)Property(sample, "DigestManifest");

        private static object Property(object value, string name) =>
            value.GetType().GetProperty(name).GetValue(value);

        private static int Counter(object counters, string name) =>
            (int)counters.GetType().GetProperty(name).GetValue(counters);

        private static string PublisherConstant(string name) =>
            (string)PublisherType.GetField(name, BindingFlags.Public | BindingFlags.Static)
                .GetRawConstantValue();

        private static Type PublisherType => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(PublisherTypeName, false))
            .FirstOrDefault(value => value != null) ?? throw new InvalidOperationException(
                "MAP21_04 publisher type is unavailable.");

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string FullPath(string relative) => Path.Combine(ProjectRoot,
            relative.Replace('/', Path.DirectorySeparatorChar));

        private static string FileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var output = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    output.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }
    }
}
