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
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.MoonPalace
{
    public sealed class MoonPalaceClusterPoolProductionTests
    {
        private const string CategoryName = "MAP21_03";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace.MoonPalaceClusterPoolPublisher";
        private const string FixedUtc = "2026-09-06T00:00:00.0000000Z";

        [Test, Category(CategoryName)]
        public void CraterRootClusterPoolContainsExact24ClustersAndNoMillDoughRecords()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.Catalog, Has.Count.EqualTo(24));
            CollectionAssert.AreEquivalent(MoonPalaceClusterPoolProduction.RequiredClusterIds,
                production.Catalog.Select(value => value.ClusterId));
            Assert.That(production.Catalog.Count(value => value.BiomeId == "MoonCrater"),
                Is.EqualTo(12));
            Assert.That(production.Catalog.Count(value => value.BiomeId == "CassiaRoot"),
                Is.EqualTo(12));
            Assert.That(production.Catalog.Count(value => value.BiomeId == "AbandonedMill" ||
                value.BiomeId == "MoonDough"), Is.Zero);
            Assert.That(production.Catalog.Count(value =>
                value.SourceStarterClusterId != "MissingData"), Is.EqualTo(8));
        }

        [Test, Category(CategoryName)]
        public void CraterRootClusterPoolHasTerrainQuietBufferCountsAndValidActiveChunkRanges()
        {
            var production = Production(CreateSample(FixedUtc, false));

            foreach (var biome in new[] { "MoonCrater", "CassiaRoot" })
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
        public void CraterRootClusterFootprintsAreConnectedNormalizedUniqueAndBounded()
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
        public void CraterRootClusterSpineVariantsHaveSingleBaselineAltPortsAndStaticRouteIntent()
        {
            var production = Production(CreateSample(FixedUtc, false));

            Assert.That(production.SpineVariants, Has.Count.EqualTo(48));
            foreach (var cluster in production.Catalog)
            {
                var variants = production.SpineVariants.Where(value =>
                    value.ClusterId == cluster.ClusterId).ToArray();
                Assert.That(variants, Has.Length.EqualTo(2));
                Assert.That(variants.Count(value => value.IsBaseline), Is.EqualTo(1));
                Assert.That(variants.Count(value => value.SpineVariantId.EndsWith("_BASE",
                    StringComparison.Ordinal)), Is.EqualTo(1));
                Assert.That(variants.Count(value => value.SpineVariantId.EndsWith("_ALT",
                    StringComparison.Ordinal)), Is.EqualTo(1));
                Assert.That(variants.All(value => value.MovementTokens.Count > 0 &&
                    !string.IsNullOrWhiteSpace(value.RouteIntent) &&
                    value.ProtectedMaskPolicy == "PreserveSpineAndEntryExit"), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void CraterRootClusterPatternSlotsReferenceOnlySameBiomeMap21_02Patterns()
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
        public void CraterRootClusterPoolRejectsDuplicateIdsBadFootprintsUnknownPatternsAndInvalidPools()
        {
            var production = Production(CreateSample(FixedUtc, false));
            Assert.Throws<ArgumentException>(() => new MoonPalaceClusterPoolProduction(
                production.Catalog.Concat(new[] { production.Catalog[0] }),
                production.Footprints, production.SpineVariants, production.PatternSlots,
                Array.Empty<string>(), Array.Empty<string>(), FixedUtc));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MoonPalaceClusterFootprintRecord(production.Catalog[0].ClusterId, 4, 0,
                    ClusterRoleKind.Entry, ClusterPortSide.L, ClusterPortSide.R, true, false));

            var patternBiomes = ReadPatternBiomes();
            Assert.Throws<ArgumentException>(() => new MoonPalaceClusterPatternSlotRecord(
                production.Catalog[0].ClusterId, "BAD_SLOT", 0, 0,
                MoonPalaceClusterPatternSlotKind.BaselineRequired,
                new[] { "MP_UNKNOWN" }, "MoonCrater", "PreserveSpineAndEntryExit", 0,
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
        public void CraterRootClusterRepetitionSignaturesAreUniqueAndDeterministic()
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
                Assert.That(DigestManifest(forward).Serialize(),
                    Is.EqualTo(DigestManifest(reverse).Serialize()));
                Assert.That(a.Catalog.Select(value => value.StructuralSignature)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(24));
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
        public void CraterRootClusterPoolKeepsQuietBufferStaticWithoutActivityEventSpecialPopulationOrRewards()
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
        public void CraterRootClusterPublisherWritesOnlyMap21_03AuthoringAndGeneratedRoots()
        {
            var sources = new[]
            {
                PublisherConstant("SourceMap2102CatalogRelativePath"),
                PublisherConstant("SourceMap2102CellsRelativePath"),
                PublisherConstant("SourceMap2102TagsRelativePath"),
                PublisherConstant("SourceMap11CatalogRelativePath"),
            };
            var before = sources.ToDictionary(path => path,
                path => FileSha256(Path.Combine(ProjectRoot, path)), StringComparer.Ordinal);
            var sample = Publish(true);
            var written = (IReadOnlyList<string>)Property(sample, "WrittenRelativePaths");

            Assert.That(written, Has.Count.EqualTo(7));
            Assert.That(written.All(path => path.StartsWith(
                    PublisherConstant("AuthoringDirectoryRelativePath") + "/",
                    StringComparison.Ordinal) || path.StartsWith(
                    PublisherConstant("GeneratedDirectoryRelativePath") + "/",
                    StringComparison.Ordinal)), Is.True);
            Assert.That(written.All(path => File.Exists(Path.Combine(ProjectRoot,
                path.Replace('/', Path.DirectorySeparatorChar)))), Is.True);
            foreach (var path in sources)
                Assert.That(FileSha256(Path.Combine(ProjectRoot, path)), Is.EqualTo(before[path]));
        }

        [Test, Category(CategoryName)]
        public void CraterRootClusterPoolPublishesMap21_04HandoffOnlyAfterFocusedPass()
        {
            var exception = Assert.Throws<TargetInvocationException>(() => Publish(false));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            var digest = DigestManifest(CreateSample(FixedUtc, false));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(digest.Map2104HandoffDigest),
                Is.True);
            Assert.That(digest.Map2104HandoffDigest,
                Is.Not.EqualTo(MoonPalaceClusterPoolPreconditions.SourceMap2103HandoffDigest));
        }

        [Test, Category(CategoryName)]
        public void CraterRootClusterPoolDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var sample = CreateSample(FixedUtc, false);
            var counters = Property(sample, "Counters");
            foreach (var name in new[]
            {
                "GenerationExecutions", "RendererExecutions", "ValidationRunnerExecutions",
                "ReplayExecutions", "RollbackExecutions", "PlayModeSelections",
                "LegacyRegressionSelections", "PriorCategorySelections",
                "UnfilteredOrFullRegressionRuns", "RuntimeObjectSpawns",
                "MillDoughClustersAuthored", "BufferLandmarkSearchExecutions",
                "CsvWritesOutsideMap2103",
            })
                Assert.That(Counter(counters, name), Is.Zero, name);
            Assert.That((bool)counters.GetType().GetProperty("AllZero").GetValue(counters),
                Is.True);
            Assert.That(typeof(MoonPalaceClusterPoolProduction).IsSubclassOf(
                typeof(UnityEngine.Object)), Is.False);
            Assert.That(Directory.Exists(Path.Combine(ProjectRoot,
                "MapDesign/MCP/GENERATED/MAP21_04")), Is.False);
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
            var path = Path.Combine(ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(path), relative);
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

        private static MoonPalaceClusterPoolProduction Production(object sample) =>
            (MoonPalaceClusterPoolProduction)Property(sample, "Production");

        private static MoonPalaceClusterPoolDigestManifest DigestManifest(object sample) =>
            (MoonPalaceClusterPoolDigestManifest)Property(sample, "DigestManifest");

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
                "MAP21_03 publisher type is unavailable.");

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

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
