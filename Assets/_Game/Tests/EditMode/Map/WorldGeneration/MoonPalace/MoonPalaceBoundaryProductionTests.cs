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

namespace StarNight.Tests.EditMode.Map.WorldGeneration.MoonPalace
{
    public sealed class MoonPalaceBoundaryProductionTests
    {
        private const string CategoryName = "MAP21_06";
        private const string PublisherTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.MoonPalaceBoundaryPoolPublisher";

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryPoolContainsExactSixPairsAndFortyEightCandidates()
        {
            var production = Production(Sample());
            Assert.That(production.Candidates, Has.Count.EqualTo(48));
            Assert.That(production.Candidates.Select(x => x.PairId).Distinct().Count(), Is.EqualTo(6));
            Assert.That(production.Candidates.Select(x => x.PairId).Distinct().OrderBy(x => x),
                Is.EqualTo(MoonPalaceBoundaryProduction.RequiredPairIds.OrderBy(x => x)));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryPoolHasFourHorizontalAndFourVerticalCandidatesPerPair()
        {
            foreach (var group in Production(Sample()).Candidates.GroupBy(x => x.PairId))
            {
                Assert.That(group.Count(), Is.EqualTo(8));
                Assert.That(group.Count(x => x.Orientation == "Horizontal"), Is.EqualTo(4));
                Assert.That(group.Count(x => x.Orientation == "Vertical"), Is.EqualTo(4));
                Assert.That(group.Select(x => x.VariantIndex).Distinct().Count(), Is.EqualTo(4));
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryTilesHaveExactNinetySixCellsPerCandidateAndNoDuplicates()
        {
            var production = Production(Sample());
            Assert.That(production.Tiles, Has.Count.EqualTo(4608));
            foreach (var group in production.Tiles.GroupBy(x => x.CandidateId))
            {
                Assert.That(group.Count(), Is.EqualTo(96));
                Assert.That(group.Select(x => x.LocalX + "/" + x.LocalY).Distinct().Count(), Is.EqualTo(96));
                Assert.That(group.All(x => x.LocalX >= 0 && x.LocalX < 12 && x.LocalY >= 0 && x.LocalY < 8), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundarySocketsPublishTwoDirectionalProjectionsPerCandidate()
        {
            var production = Production(Sample());
            Assert.That(production.Sockets, Has.Count.EqualTo(96));
            Assert.That(production.Routes, Has.Count.EqualTo(96));
            Assert.That(production.Sockets.Select(x => x.SocketKey).Distinct().Count(), Is.EqualTo(96));
            foreach (var candidate in production.Candidates)
            {
                Assert.That(production.Sockets.Count(x => x.CandidateId == candidate.CandidateId), Is.EqualTo(2));
                Assert.That(production.Routes.Where(x => x.CandidateId == candidate.CandidateId).Select(x => x.Direction).OrderBy(x => x), Is.EqualTo(new[] { "A_TO_B", "B_TO_A" }));
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryRouteProfilesPreserveHorizontalAndVerticalCompatibilityFamilies()
        {
            var production = Production(Sample());
            foreach (var route in production.Routes)
            {
                var candidate = production.Candidates.Single(x => x.CandidateId == route.CandidateId);
                Assert.That(route.SocketSignature, Is.EqualTo(candidate.Orientation == "Horizontal" ? "EDGE_H_MID_WALK" : "EDGE_V_CENTER_CLIMB"));
                Assert.That(route.ToolRequirement, Is.EqualTo("NONE"));
                Assert.That(route.MandatoryRoute, Is.True);
                Assert.That(route.EntrySide, Is.Not.EqualTo(route.ExitSide));
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryWarningEvidencePublishesAtLeastTwoCategoriesPerProjection()
        {
            var production = Production(Sample());
            Assert.That(production.Warnings, Has.Count.EqualTo(384));
            Assert.That(production.Warnings.GroupBy(x => x.ProjectionId).Count(), Is.EqualTo(96));
            Assert.That(production.Warnings.GroupBy(x => x.ProjectionId).Min(group => group.Select(x => x.Category).Distinct().Count()), Is.GreaterThanOrEqualTo(2));
            Assert.That(production.Warnings.Select(x => x.Category).Distinct().OrderBy(x => x), Is.EqualTo(new[] { "Audio", "Background", "Resource", "Tile" }));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryWarningEvidencePreservesEnteringBiomeOnDirectionReversal()
        {
            var production = Production(Sample());
            foreach (var candidate in production.Candidates)
            foreach (var direction in new[] { "A_TO_B", "B_TO_A" })
            {
                var projection = candidate.CandidateId + "__" + direction;
                var expected = direction == "A_TO_B" ? candidate.BiomeB : candidate.BiomeA;
                var evidence = production.Warnings.Where(x => x.ProjectionId == projection).ToArray();
                Assert.That(evidence, Has.Length.EqualTo(4));
                Assert.That(evidence.All(x => x.EnteringBiome == expected && x.Direction == direction), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryPublisherReadsMap08AndMap21SourcesWithoutRewriting()
        {
            var sample = Sample();
            var sourcePaths = (IReadOnlyList<string>)Property(sample, "SourceReadRelativePaths");
            var before = sourcePaths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
            sample = Sample(true);
            var after = sourcePaths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
            Assert.That(sourcePaths, Has.Count.EqualTo(9));
            Assert.That(after, Is.EqualTo(before));
            Assert.That(sourcePaths.Count(path => path.Contains("MAP08")), Is.EqualTo(2));
            Assert.That(sourcePaths.Count(path => path.Contains("MAP21_04") || path.Contains("MAP21_05")), Is.EqualTo(2));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryPublisherWritesOnlyMap21_06AuthoringAndGeneratedRoots()
        {
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var paths = (IReadOnlyList<string>)Property(sample, "WrittenRelativePaths");
            var authoring = Constant("AuthoringDirectoryRelativePath") + "/";
            var generated = Constant("GeneratedDirectoryRelativePath") + "/";
            Assert.That(paths, Has.Count.EqualTo(9));
            Assert.That(paths.Count(path => path.StartsWith(authoring, StringComparison.Ordinal)), Is.EqualTo(5));
            Assert.That(paths.Count(path => path.StartsWith(generated, StringComparison.Ordinal)), Is.EqualTo(4));
            foreach (var path in paths)
            {
                var bytes = File.ReadAllBytes(Resolve(path));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = Encoding.UTF8.GetString(bytes);
                Assert.That(text.Contains("\r"), Is.False);
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryDigestsAreDeterministicReverseOrderRepeatAndCultureStable()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR"); CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
                var leftSample = Sample(false); var left = Production(leftSample);
                CultureInfo.CurrentCulture = new CultureInfo("ko-KR"); CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
                var rightSample = Sample(true); var right = Production(rightSample);
                Assert.That(left.SerializeCandidatesCsv(), Is.EqualTo(right.SerializeCandidatesCsv()));
                Assert.That(left.SerializeTilesCsv(), Is.EqualTo(right.SerializeTilesCsv()));
                Assert.That(left.SerializeSocketsCsv(), Is.EqualTo(right.SerializeSocketsCsv()));
                Assert.That(left.SerializeRoutesCsv(), Is.EqualTo(right.SerializeRoutesCsv()));
                Assert.That(left.SerializeWarningsCsv(), Is.EqualTo(right.SerializeWarningsCsv()));
                Assert.That(left.SerializePoolManifest(), Is.EqualTo(right.SerializePoolManifest()));
                Assert.That(left.SerializeProjectionManifest(), Is.EqualTo(right.SerializeProjectionManifest()));
                Assert.That(left.SerializeWarningManifest(), Is.EqualTo(right.SerializeWarningManifest()));
                Assert.That(DigestManifest(leftSample).Serialize(), Is.EqualTo(DigestManifest(rightSample).Serialize()));
            }
            finally { CultureInfo.CurrentCulture = previousCulture; CultureInfo.CurrentUICulture = previousUiCulture; }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryRejectsDuplicateIdsMissingPairsBadCellsWrongSocketsAndInsufficientWarnings()
        {
            var value = Production(Sample());
            Assert.Throws<ArgumentException>(() => Rebuild(value, candidates: value.Candidates.Concat(new[] { value.Candidates[0] })));
            Assert.Throws<ArgumentException>(() => Rebuild(value, candidates: value.Candidates.Where(x => x.PairId != "PAIR_CRATER_ROOT")));
            Assert.Throws<ArgumentException>(() => Rebuild(value, tiles: value.Tiles.Skip(1).Concat(new[] { value.Tiles[1] })));
            var original = value.Sockets[0];
            var wrong = new MoonPalaceBoundarySocketRecord(original.SocketKey, original.CandidateId, original.ProjectionId,
                original.Direction, original.EntrySide, original.ExitSide, "EDGE_WRONG", original.RouteProfileId);
            Assert.Throws<ArgumentException>(() => Rebuild(value, sockets: value.Sockets.Skip(1).Concat(new[] { wrong })));
            Assert.Throws<ArgumentException>(() => Rebuild(value, warnings: value.Warnings.Where(x => x.Category == "Tile")));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var counters = (MoonPalaceBoundaryForbiddenOperationCounters)Property(Sample(), "Counters");
            Assert.That(counters.AllZero, Is.True);
            Assert.That(counters.GenerationRunnerExecutions + counters.RendererExecutions + counters.ValidationRunnerExecutions +
                counters.ReplayExecutions + counters.RollbackExecutions + counters.TilemapWrites + counters.RuntimeObjectSpawns +
                counters.ScenePrefabChanges + counters.PriorCategorySelections + counters.PlayModeSelections +
                counters.LegacyRegressionSelections + counters.UnfilteredOrFullRegressionSelections + counters.Map08CategoryReruns + counters.UpstreamRegenerationRuns, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBoundaryPublishesMap21_07HandoffOnlyAfterFocusedPass()
        {
            var blocked = Assert.Throws<TargetInvocationException>(() => InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var manifest = DigestManifest(sample);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(manifest.Map2107HandoffDigest), Is.True);
            Assert.That(manifest.CsvDigests, Has.Count.EqualTo(5));
            Assert.That(manifest.JsonDigests, Has.Count.EqualTo(3));
            Assert.That(File.Exists(Resolve(Constant("GeneratedDirectoryRelativePath") + "/" + Constant("DigestManifestFileName"))), Is.True);
        }

        private static MoonPalaceBoundaryProduction Rebuild(MoonPalaceBoundaryProduction value,
            IEnumerable<MoonPalaceBoundaryCandidate> candidates = null, IEnumerable<MoonPalaceBoundaryTileRecord> tiles = null,
            IEnumerable<MoonPalaceBoundarySocketRecord> sockets = null, IEnumerable<MoonPalaceBoundaryWarningEvidence> warnings = null) =>
            new MoonPalaceBoundaryProduction(candidates ?? value.Candidates, tiles ?? value.Tiles, sockets ?? value.Sockets,
                value.Routes, warnings ?? value.Warnings, value.CreatedUtc);
        private static object Sample(bool reverse = false) => InvokePublisher("CreateReadOnlySample", ProjectRoot, "2026-09-06T00:00:00.0000000Z", reverse);
        private static MoonPalaceBoundaryProduction Production(object sample) => (MoonPalaceBoundaryProduction)Property(sample, "Production");
        private static MoonPalaceBoundaryDigestManifest DigestManifest(object sample) => (MoonPalaceBoundaryDigestManifest)Property(sample, "DigestManifest");
        private static object Property(object instance, string name) => instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokePublisher(string methodName, params object[] arguments) => PublisherType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        private static string Constant(string name) => (string)PublisherType.GetField(name).GetRawConstantValue();
        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor").GetType(PublisherTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string path) => Path.Combine(ProjectRoot, path.Replace('/', Path.DirectorySeparatorChar));
        private static string FileSha(string path) { using (var stream = File.OpenRead(Resolve(path))) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }
    }
}
