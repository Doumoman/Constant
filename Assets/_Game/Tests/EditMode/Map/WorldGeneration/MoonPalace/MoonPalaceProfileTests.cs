using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace;
using StarNight.Map.WorldGeneration.Validation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.MoonPalace
{
    public sealed class MoonPalaceProfileTests
    {
        private const string CategoryName = "MAP21_01";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace.MoonPalaceProfilePublisher";

        [Test, Category(CategoryName)]
        public void MoonPalaceProfileLocksExplicitMovementNumbersWithoutPlayerCodeChanges()
        {
            var profile = Profile(CreateSample("2026-09-06T00:00:00.0000000Z", false));
            var movement = profile.Movement;

            Assert.That(movement.WalkSpeedTilesPerSecond, Is.EqualTo(4.0d));
            Assert.That(movement.RunSpeedTilesPerSecond, Is.EqualTo(6.5d));
            Assert.That(movement.JumpHeightTiles, Is.EqualTo(3.0d));
            Assert.That(movement.JumpDistanceTiles, Is.EqualTo(5.0d));
            Assert.That(movement.MaxSafeDropTiles, Is.EqualTo(6.0d));
            Assert.That(movement.PlayerWidthCells, Is.EqualTo(0.75d));
            Assert.That(movement.PlayerHeightCells, Is.EqualTo(1.80d));
            Assert.That(movement.HeadClearanceCells, Is.EqualTo(2.0d));
            Assert.That(movement.LandingClearanceCells, Is.EqualTo(1.0d));
            Assert.That(movement.RecoveryTimeSecondsMin, Is.EqualTo(0.08d));
            Assert.That(movement.RecoveryTimeSecondsMax, Is.EqualTo(0.18d));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                movement.TraversalProfileDigest), Is.True);
            Assert.That(typeof(MoonPalaceProductionProfile).BaseType,
                Is.EqualTo(typeof(object)));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBiomeProfilesDefineFourBiomesWithValidDensityPacingAndDifficulty()
        {
            var biomes = Profile(CreateSample("2026-09-06T00:00:00.0000000Z", false))
                .Biomes;

            Assert.That(biomes.Select(value => value.BiomeId), Is.EquivalentTo(new[]
                { "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough" }));
            Assert.That(biomes, Has.Count.EqualTo(4));
            Assert.That(biomes.All(value =>
                value.DensityMin >= 0d && value.DensityMin <= value.DensityMax &&
                value.DensityMax <= 1d &&
                value.QuietRatioMin >= 0d && value.QuietRatioMin <= value.QuietRatioMax &&
                value.QuietRatioMax <= 1d &&
                value.ClusterRatioMin >= 0d && value.ClusterRatioMin <= value.ClusterRatioMax &&
                value.ClusterRatioMax <= 1d &&
                value.ActivityRatioMin >= 0d && value.ActivityRatioMin <= value.ActivityRatioMax &&
                value.ActivityRatioMax <= 1d &&
                value.OverlayRatioMin >= 0d && value.OverlayRatioMin <= value.OverlayRatioMax &&
                value.OverlayRatioMax <= 1d &&
                !string.IsNullOrWhiteSpace(value.VerticalityBand) &&
                !string.IsNullOrWhiteSpace(value.DifficultyBand)), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTileShellDefinesUniqueTileCodesCollisionMaterialAudioAndBackgroundTokens()
        {
            var records = TileShell(CreateSample("2026-09-06T00:00:00.0000000Z", false))
                .Records;
            var roles = Enum.GetValues(typeof(MoonPalaceTileRole))
                .Cast<MoonPalaceTileRole>();
            var collisions = Enum.GetValues(typeof(MoonPalaceCollisionKind))
                .Cast<MoonPalaceCollisionKind>();

            Assert.That(records.Select(value => value.TileCode).Distinct().Count(),
                Is.EqualTo(records.Count));
            Assert.That(records.Select(value => value.TileRole), Is.SupersetOf(roles));
            Assert.That(records.Select(value => value.CollisionKind),
                Is.SupersetOf(collisions));
            Assert.That(records.All(value =>
                !string.IsNullOrWhiteSpace(value.LayerToken) &&
                !string.IsNullOrWhiteSpace(value.MaterialToken) &&
                !string.IsNullOrWhiteSpace(value.FootstepAudioToken) &&
                !string.IsNullOrWhiteSpace(value.ImpactAudioToken) &&
                !string.IsNullOrWhiteSpace(value.BackgroundToken)), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTileShellRepresentsMissingAssetsAsMissingDataOrFallbackWithoutImport()
        {
            var records = TileShell(CreateSample("2026-09-06T00:00:00.0000000Z", false))
                .Records;

            Assert.That(records.All(value =>
                value.AssetReferenceKind == MoonPalaceAssetReferenceKind.MissingData &&
                value.AssetReference == string.Empty &&
                !string.IsNullOrWhiteSpace(value.FallbackTileCode) &&
                !string.IsNullOrWhiteSpace(value.MissingReason)), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceProfileRejectsDuplicateBiomeIdsTileCodesAndInvalidRanges()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoonPalaceBiomeProfile(
                "MoonCrater", -0.01d, 0.5d, 0.1d, 0.2d, 0.1d, 0.2d,
                0.1d, 0.2d, 0.1d, 0.2d, "High", "Mid", "MAT", "AMB", "BG"));

            var biome = ValidBiome("MoonCrater");
            Assert.Throws<ArgumentException>(() => new MoonPalaceProductionProfile(
                ValidMovement(), new[] { biome, biome },
                GeneratedTraversalProfileCatalog.Create().Digest, string.Empty));

            var tile = ValidTile("DUPLICATE");
            Assert.Throws<ArgumentException>(() => new MoonPalaceTileShell(
                new[] { tile, tile }, string.Empty));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceProfileArtifactsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
                var forward = CreateSample("2026-09-06T00:00:00.0000000Z", false);
                CultureInfo.CurrentCulture = new CultureInfo("ko-KR");
                CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
                var reverse = CreateSample("2026-09-06T00:00:00.0000000Z", true);

                Assert.That(Profile(forward).Serialize(), Is.EqualTo(Profile(reverse).Serialize()));
                Assert.That(TileShell(forward).Serialize(),
                    Is.EqualTo(TileShell(reverse).Serialize()));
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
        public void MoonPalaceProfilePublishesMap21_02HandoffOnlyAfterFocusedPass()
        {
            var blocked = Assert.Throws<TargetInvocationException>(() =>
                InvokePublisher("PublishSamples", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());

            var published = InvokePublisher("PublishSamples", ProjectRoot, true);
            var outputRelative = (string)PublisherType.GetField(
                "OutputDirectoryRelativePath").GetRawConstantValue();
            var output = Path.Combine(ProjectRoot,
                outputRelative.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(Directory.GetFiles(output, "*.json"), Has.Length.EqualTo(3));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                DigestManifest(published).Map2102HandoffDigest), Is.True);
            Assert.That(File.Exists(Path.Combine(output, Constant("ProfileManifestFileName"))),
                Is.True);
            Assert.That(File.Exists(Path.Combine(output,
                Constant("TileShellManifestFileName"))), Is.True);
            Assert.That(File.Exists(Path.Combine(output,
                Constant("DigestManifestFileName"))), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceProfileDoesNotRunGenerationValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var counters = Property(CreateSample("2026-09-06T00:00:00.0000000Z", false),
                "Counters");
            Assert.That((bool)Property(counters, "AllZero"), Is.True);
            Assert.That(Counter(counters, "GenerationRunnerExecutions"), Is.Zero);
            Assert.That(Counter(counters, "ValidationRunnerExecutions"), Is.Zero);
            Assert.That(Counter(counters, "ReplayExecutions"), Is.Zero);
            Assert.That(Counter(counters, "RollbackExecutions"), Is.Zero);
            Assert.That(Counter(counters, "PlayModeSelections"), Is.Zero);
            Assert.That(Counter(counters, "LegacyRegressionExecutions"), Is.Zero);
            Assert.That(Counter(counters, "PriorCategoryExecutions"), Is.Zero);
            Assert.That(Counter(counters, "UnfilteredOrFullRegressionExecutions"), Is.Zero);
            Assert.That(Counter(counters, "RuntimeObjectSpawns"), Is.Zero);
        }

        private static MoonPalaceMovementProfile ValidMovement() =>
            new MoonPalaceMovementProfile("MOONPALACE_PRODUCTION_V1",
                MoonPalaceProductionProfile.SchemaVersion,
                MoonPalaceProfilePreconditions.SourceMap20PhaseExitDigest,
                4d, 6.5d, 3d, 5d, 6d, 0.75d, 1.8d, 2d, 1d, 0.08d, 0.18d);

        private static MoonPalaceBiomeProfile ValidBiome(string biomeId) =>
            new MoonPalaceBiomeProfile(biomeId, 0.2d, 0.4d, 0.1d, 0.2d,
                0.1d, 0.2d, 0.1d, 0.2d, 0.1d, 0.2d, "High", "Mid",
                "MAT", "AMB", "BG");

        private static MoonPalaceTileShellRecord ValidTile(string tileCode) =>
            new MoonPalaceTileShellRecord(tileCode, MoonPalaceTileRole.Ground,
                "LAYER", MoonPalaceCollisionKind.Solid, "MAT", "STEP", "IMPACT",
                "BG", new[] { "MoonCrater" }, MoonPalaceAssetReferenceKind.MissingData,
                string.Empty, "MP_DEBUG_MISSING", "Not imported", 1);

        private static object CreateSample(string createdUtc, bool reverseInputOrder) =>
            InvokePublisher("CreateReadOnlySample", ProjectRoot, createdUtc, reverseInputOrder);

        private static object InvokePublisher(string methodName, params object[] arguments) =>
            PublisherType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, arguments);

        private static MoonPalaceProductionProfile Profile(object sample) =>
            (MoonPalaceProductionProfile)Property(sample, "Profile");

        private static MoonPalaceTileShell TileShell(object sample) =>
            (MoonPalaceTileShell)Property(sample, "TileShell");

        private static MoonPalaceProfileDigestManifest DigestManifest(object sample) =>
            (MoonPalaceProfileDigestManifest)Property(sample, "DigestManifest");

        private static object Property(object instance, string name) =>
            instance.GetType().GetProperty(name).GetValue(instance);

        private static int Counter(object counters, string name) =>
            (int)Property(counters, name);

        private static string Constant(string name) =>
            (string)PublisherType.GetField(name).GetRawConstantValue();

        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor")
            .GetType(PublisherTypeName, true);

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath).FullName;
    }
}
