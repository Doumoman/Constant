using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.MapAuthoring.WorldGeneration.Activities;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.Activities
{
    [TestFixture]
    [Category("MAP12_06")]
    public sealed class ActivityEventPreviewTests
    {
        private static readonly Lazy<Fixture> Physical = new Lazy<Fixture>(Fixture.Load);

        private static readonly IReadOnlyDictionary<string, string> EventActivities =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "EVT_METEOR_FALL", "ACT_CRATER_RICOCHET_MINE" },
                { "EVT_WANDERING_MERCHANT", "ACT_MILL_ESCORT_CART" },
                { "EVT_RARE_CREATURE", "ACT_MILL_ESCORT_CART" },
                { "EVT_MARU_INTERVENTION", "ACT_MARU_REWIND_ANOMALY" },
                { "EVT_EMPTY", "ACT_DOUGH_TIME_TRIAL" },
            };

        [Test]
        public void PhysicalSelectorsCoverExactSevenActivitiesAndFiveEvents()
        {
            var fixture = Physical.Value;
            Assert.That(fixture.Model.ActivityIds, Is.EqualTo(new[]
            {
                "ACT_CRATER_BOULDER_CHAIN", "ACT_CRATER_RICOCHET_MINE", "ACT_DOUGH_TIME_TRIAL",
                "ACT_MARU_REWIND_ANOMALY", "ACT_MILL_ESCORT_CART", "ACT_MILL_GEAR_GRID",
                "ACT_MILL_PESTLE_WORKSHOP",
            }));
            Assert.That(fixture.Model.EventIds, Is.EqualTo(new[]
            {
                "EVT_METEOR_FALL", "EVT_WANDERING_MERCHANT", "EVT_RARE_CREATURE",
                "EVT_MARU_INTERVENTION", "EVT_EMPTY",
            }));
            Assert.That(fixture.Content.ActivityCatalog.Entries, Has.Count.EqualTo(7));
            Assert.That(fixture.Content.EventCatalog.Entries, Has.Count.EqualTo(5));
            Assert.That(fixture.Content.AggregateStableDigest,
                Is.EqualTo(ActivityEventPreviewModel.ApprovedAggregateDigest));
            Assert.That(fixture.Content.ActivityCatalog.StableDigest,
                Is.EqualTo(ActivityEventPreviewModel.ApprovedActivityCatalogDigest));
            Assert.That(fixture.Content.EventCatalog.StableDigest,
                Is.EqualTo(ActivityEventPreviewModel.ApprovedEventCatalogDigest));
        }

        [Test]
        public void AllSevenPublishStaticActiveRemovedIdentityAndRemovalProofs()
        {
            var fixture = Physical.Value;
            foreach (var id in fixture.Model.ActivityIds)
            {
                var result = fixture.Build(id);
                AssertSuccess(result, id);
                var authored = fixture.Content.ActivityCatalog.ById[new ActivityStructureId(id)];
                Assert.That(result.StaticSnapshot.ActivityMarkerCount, Is.Zero, id);
                Assert.That(result.StaticSnapshot.EventMarkerCount, Is.Zero, id);
                Assert.That(result.ActiveSnapshot.ActivityMarkerCount,
                    Is.EqualTo(authored.Contract.Slots.Count + authored.Contract.RemovalSafety.SafePocketTiles.Count), id);
                Assert.That(result.ActiveSnapshot.EventMarkerCount, Is.Zero, id);
                Assert.That(result.RemovedSnapshot.MarkerCount, Is.Zero, id);
                Assert.That(result.StaticSnapshot.UnderlyingDigest,
                    Is.EqualTo(result.ActiveSnapshot.UnderlyingDigest), id);
                Assert.That(result.StaticSnapshot.UnderlyingDigest,
                    Is.EqualTo(result.RemovedSnapshot.UnderlyingDigest), id);
                Assert.That(result.StaticSnapshot.CellDigest,
                    Is.EqualTo(result.ActiveSnapshot.CellDigest), id);
                Assert.That(result.StaticSnapshot.RouteDigest,
                    Is.EqualTo(result.RemovedSnapshot.RouteDigest), id);
                Assert.That(result.StaticSnapshot.AccessDigest,
                    Is.EqualTo(result.RemovedSnapshot.AccessDigest), id);
                Assert.That(result.StaticSnapshot.ProtectionDigest,
                    Is.EqualTo(result.RemovedSnapshot.ProtectionDigest), id);
                Assert.That(result.ActiveSnapshot.CueObservationOrdinal,
                    Is.LessThan(result.ActiveSnapshot.ActivationBoundaryOrdinal), id);
                Assert.That(result.ActiveSnapshot.SafePocketProofCount,
                    Is.EqualTo(authored.Contract.RemovalSafety.SafePocketTiles.Count), id);
                Assert.That(result.ActiveSnapshot.RecoveryProofCount,
                    Is.EqualTo(authored.Contract.RemovalSafety.RecoveryTiles.Count), id);
                Assert.That(result.ActiveSnapshot.ExitPreservationProofCount, Is.EqualTo(1), id);
                Assert.That(result.ActiveSnapshot.RewardPreservationProofCount, Is.EqualTo(1), id);
                Assert.That(new[] { result.StaticSnapshot, result.ActiveSnapshot, result.RemovedSnapshot }
                    .All(value => value.ResidualMarkerCount == 0 && value.TileDeltaCount == 0 &&
                                  value.ColliderDeltaCount == 0 && value.RngDrawCount == 0), Is.True, id);
                Assert.That(result.Comparison.MarkerOnly, Is.True, id);
                Assert.That(result.Comparison.StaticToActiveMarkerDelta,
                    Is.EqualTo(result.ActiveSnapshot.MarkerCount), id);
                Assert.That(result.Comparison.ActiveToRemovedMarkerDelta,
                    Is.EqualTo(-result.ActiveSnapshot.MarkerCount), id);
                Assert.That(result.StaticSnapshot.RouteWitnesses.Select(value => value.Token), Does.Contain("EN"), id);
                Assert.That(result.StaticSnapshot.RouteWitnesses.Select(value => value.Token), Does.Contain("EX"), id);
                Assert.That(result.StaticSnapshot.RouteWitnesses.Select(value => value.Token), Does.Contain("AP"), id);
                TestContext.WriteLine("PREVIEW activity=" + id + " digest=" + result.StableDigest +
                                      " cells=" + result.StaticSnapshot.Cells.Count +
                                      " active_markers=" + result.ActiveSnapshot.ActivityMarkerCount);
            }
        }

        [Test]
        public void FourNonEmptyEventsAndExplicitEmptyPublishExactMarkerSemantics()
        {
            var fixture = Physical.Value;
            foreach (var pair in EventActivities)
            {
                var result = fixture.Build(pair.Value, pair.Key);
                AssertSuccess(result, pair.Key);
                var authored = fixture.Content.EventCatalog.ById[new EventOverlayId(pair.Key)];
                Assert.That(result.EventSnapshot.MarkerCount, Is.EqualTo(authored.MarkerTargets.Count), pair.Key);
                Assert.That(result.ActiveSnapshot.EventMarkerCount, Is.EqualTo(authored.MarkerTargets.Count), pair.Key);
                Assert.That(result.StaticSnapshot.EventMarkerCount, Is.Zero, pair.Key);
                Assert.That(result.RemovedSnapshot.EventMarkerCount, Is.Zero, pair.Key);
                Assert.That(result.EventSnapshot.Weight, Is.EqualTo(authored.Profile.Weight), pair.Key);
                Assert.That(result.EventSnapshot.MinimumProgressionGap,
                    Is.EqualTo(authored.Profile.MinimumProgressionGap), pair.Key);
                Assert.That(result.EventSnapshot.ContractDigest,
                    Is.EqualTo(authored.Profile.ContractDigest), pair.Key);
                Assert.That(result.EventSnapshot.CandidateIndexDigest, Does.Match("^[0-9a-f]{64}$"), pair.Key);
                if (pair.Key == "EVT_EMPTY")
                {
                    Assert.That(result.EventSnapshot.ExplicitEmpty, Is.True);
                    Assert.That(result.EventSnapshot.Kind, Is.EqualTo("Explicit Empty"));
                    Assert.That(result.EventSnapshot.MarkerCount, Is.Zero);
                    Assert.That(result.EventSnapshot.Weight, Is.Zero);
                    Assert.That(result.EventSnapshot.MinimumProgressionGap, Is.Zero);
                }
                else
                {
                    Assert.That(result.EventSnapshot.ExplicitEmpty, Is.False, pair.Key);
                    Assert.That(result.EventSnapshot.MarkerCount, Is.EqualTo(1), pair.Key);
                    Assert.That(result.EventSnapshot.SourceOwnerSummary, Is.Not.Empty, pair.Key);
                    Assert.That(result.EventSnapshot.OperationSummary, Is.Not.Empty, pair.Key);
                    Assert.That(result.EventSnapshot.PayloadSummary, Is.Not.Empty, pair.Key);
                }
                Assert.That(result.Comparison.MarkerOnly, Is.True, pair.Key);
                TestContext.WriteLine("EVENT event=" + pair.Key + " activity=" + pair.Value +
                                      " digest=" + result.EventSnapshot.StableDigest +
                                      " markers=" + result.EventSnapshot.MarkerCount);
            }
        }

        [Test]
        public void ReverseInputRepeatAndTurkishCultureKeepStableImmutableSnapshots()
        {
            var fixture = Physical.Value;
            var canonical = fixture.Build("ACT_CRATER_RICOCHET_MINE", "EVT_METEOR_FALL");
            var repeat = fixture.Build("ACT_CRATER_RICOCHET_MINE", "EVT_METEOR_FALL");
            AssertSuccess(canonical, "canonical");
            AssertSuccess(repeat, "repeat");

            var bytes = ActivityEventCsvImporterV2.ProjectRelativePaths.Reverse()
                .ToDictionary(path => path, path => File.ReadAllBytes(FullPath(path)), StringComparer.Ordinal);
            var reversedContent = new ActivityEventCsvImporterV2().ParseBytes(bytes, fixture.Terrain);
            Assert.That(reversedContent.Success, Is.True, string.Join("\n", reversedContent.Errors));
            var reversed = fixture.Model.Build(new ActivityEventPreviewRequest(
                    "ACT_CRATER_RICOCHET_MINE", "EVT_METEOR_FALL",
                    ActivityEventPreviewModel.ApprovedAggregateDigest),
                fixture.Terrain, fixture.TerrainDigest, fixture.Patterns, fixture.PatternDigest, reversedContent);
            AssertSuccess(reversed, "reversed");

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            ActivityEventPreviewBuildResult turkish;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                turkish = fixture.Build("ACT_CRATER_RICOCHET_MINE", "EVT_METEOR_FALL");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
            AssertSuccess(turkish, "tr-TR");
            Assert.That(repeat.StableDigest, Is.EqualTo(canonical.StableDigest));
            Assert.That(reversed.StableDigest, Is.EqualTo(canonical.StableDigest));
            Assert.That(turkish.StableDigest, Is.EqualTo(canonical.StableDigest));

            Assert.Throws<NotSupportedException>(() => ((IList)canonical.ActiveSnapshot.Cells).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList)canonical.ActiveSnapshot.ActivityMarkers).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList)canonical.EventSnapshot.Markers).Clear());
            foreach (var type in new[]
                     {
                         typeof(ActivityEventPreviewRequest), typeof(ActivityEventPreviewCell),
                         typeof(ActivityEventPreviewMarker), typeof(ActivityEventPreviewRouteWitness),
                         typeof(ActivityStatePreviewSnapshot), typeof(EventOverlayPreviewSnapshot),
                         typeof(ActivityEventComparisonSnapshot), typeof(ActivityEventPreviewBuildError),
                         typeof(ActivityEventPreviewBuildResult),
                     })
            {
                Assert.That(type.IsSealed, Is.True, type.Name);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(value => value.SetMethod != null), Is.Empty, type.Name);
            }
        }

        [Test]
        public void InvalidIdDigestSourceOwnerAndSlotPublishNothing()
        {
            var fixture = Physical.Value;
            AssertAtomic(fixture.Model.Build(new ActivityEventPreviewRequest("ACT_UNKNOWN"),
                    fixture.Terrain, fixture.TerrainDigest, fixture.Patterns, fixture.PatternDigest, fixture.Content),
                ActivityEventPreviewBuildErrorCode.ActivityNotFound);
            AssertAtomic(fixture.Model.Build(new ActivityEventPreviewRequest(
                    "ACT_CRATER_RICOCHET_MINE", "EVT_UNKNOWN"),
                    fixture.Terrain, fixture.TerrainDigest, fixture.Patterns, fixture.PatternDigest, fixture.Content),
                ActivityEventPreviewBuildErrorCode.EventNotFound);
            AssertAtomic(fixture.Model.Build(new ActivityEventPreviewRequest(
                    "ACT_CRATER_RICOCHET_MINE", "EVT_METEOR_FALL", new string('a', 64)),
                    fixture.Terrain, fixture.TerrainDigest, fixture.Patterns, fixture.PatternDigest, fixture.Content),
                ActivityEventPreviewBuildErrorCode.DigestMismatch);
            AssertAtomic(fixture.Model.Build(new ActivityEventPreviewRequest(
                    "ACT_CRATER_RICOCHET_MINE", "EVT_METEOR_FALL",
                    ActivityEventPreviewModel.ApprovedAggregateDigest, "TC_WRONG", "CORE"),
                    fixture.Terrain, fixture.TerrainDigest, fixture.Patterns, fixture.PatternDigest, fixture.Content),
                ActivityEventPreviewBuildErrorCode.SourceMismatch);
            AssertAtomic(fixture.Model.Build(new ActivityEventPreviewRequest(
                    "ACT_CRATER_RICOCHET_MINE", "EVT_METEOR_FALL",
                    ActivityEventPreviewModel.ApprovedAggregateDigest, "TC_CRATER_BROKEN_SLOPE", "WRONG"),
                    fixture.Terrain, fixture.TerrainDigest, fixture.Patterns, fixture.PatternDigest, fixture.Content),
                ActivityEventPreviewBuildErrorCode.SourceMismatch);
        }

        [Test]
        public void WindowPublishesMenuTitleControlsLegendAndNoMutationActions()
        {
            Assert.That(ActivityEventPreviewWindow.MenuPath,
                Is.EqualTo("Tools/MapDesign/Activity & Event Preview"));
            Assert.That(ActivityEventPreviewWindow.WindowTitle, Is.EqualTo("Activity & Event Preview"));
            foreach (var token in new[]
                     {
                         "EN Entry", "EX Exit", "AP Protected", "C Cue", "T Trigger",
                         "D Device", "H Hazard", "P Projectile", "N Npc", "RW Reward",
                         "SP SafePocket", "RC Recovery", "RS Reset", "EV Event",
                     })
                Assert.That(ActivityEventPreviewWindow.LegendText, Does.Contain(token), token);

            var window = ActivityEventPreviewWindow.Open();
            try
            {
                Assert.That(window.Reload(), Is.True, window.LastError);
                Assert.That(window.ActivityIds, Is.EqualTo(Physical.Value.Model.ActivityIds));
                Assert.That(window.EventIds.Skip(1), Is.EqualTo(Physical.Value.Model.EventIds));
                Assert.That(window.EventIds[0], Is.Empty);
                Assert.That(window.CurrentResult, Is.Not.Null);
                Assert.That(window.TrySelectViewMode(ActivityEventPreviewViewMode.Compare), Is.True);
                Assert.That(window.StatePanelCount, Is.EqualTo(3));
            }
            finally
            {
                window.Close();
            }

            var source = File.ReadAllText(FullPath(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewWindow.cs"));
            foreach (var forbidden in new[]
                     {
                         "AssetDatabase", "FileSystemWatcher", "OnInspectorUpdate", "SceneManager",
                         "PrefabUtility", "Tilemap", "Random.", "PlayerController", "Rigidbody", "Collider2D",
                     })
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
            Assert.That(source, Does.Contain("Reload"));
            Assert.That(source, Does.Contain("Static / Active / Removed Compare"));
            Assert.That(source, Does.Contain("CH 12x8 chunk boundary"));
        }

        private static void AssertSuccess(ActivityEventPreviewBuildResult result, string context)
        {
            Assert.That(result.Success, Is.True,
                context + "\n" + string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"), context);
            Assert.That(result.StaticSnapshot, Is.Not.Null, context);
            Assert.That(result.ActiveSnapshot, Is.Not.Null, context);
            Assert.That(result.RemovedSnapshot, Is.Not.Null, context);
            Assert.That(result.EventSnapshot, Is.Not.Null, context);
            Assert.That(result.Comparison, Is.Not.Null, context);
        }

        private static void AssertAtomic(
            ActivityEventPreviewBuildResult result,
            ActivityEventPreviewBuildErrorCode expected)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StaticSnapshot, Is.Null);
            Assert.That(result.ActiveSnapshot, Is.Null);
            Assert.That(result.RemovedSnapshot, Is.Null);
            Assert.That(result.EventSnapshot, Is.Null);
            Assert.That(result.Comparison, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.AggregateDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected),
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            Assert.That(result.Errors, Is.Ordered);
        }

        private static string FullPath(string projectRelativePath)
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(root,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class Fixture
        {
            private Fixture(
                ActivityEventPreviewModel model,
                StarNight.Map.WorldGeneration.TerrainClusters.Authoring.TerrainClusterAuthoringCatalog terrain,
                string terrainDigest,
                StarNight.Map.WorldGeneration.MicroPatterns.MicroPatternAuthoringCatalog patterns,
                string patternDigest,
                ActivityEventCsvImportResult content)
            {
                Model = model;
                Terrain = terrain;
                TerrainDigest = terrainDigest;
                Patterns = patterns;
                PatternDigest = patternDigest;
                Content = content;
            }

            public ActivityEventPreviewModel Model { get; }
            public StarNight.Map.WorldGeneration.TerrainClusters.Authoring.TerrainClusterAuthoringCatalog Terrain { get; }
            public string TerrainDigest { get; }
            public StarNight.Map.WorldGeneration.MicroPatterns.MicroPatternAuthoringCatalog Patterns { get; }
            public string PatternDigest { get; }
            public ActivityEventCsvImportResult Content { get; }

            public static Fixture Load()
            {
                var terrain = new TerrainClusterCsvImporterV2().Import();
                Assert.That(terrain.Success, Is.True, string.Join("\n", terrain.Errors));
                var patterns = new MicroPatternCsvImporterV2().Import();
                Assert.That(patterns.Success && patterns.Published, Is.True, string.Join("\n", patterns.Errors));
                var content = new ActivityEventCsvImporterV2().Import(terrain.Catalog);
                Assert.That(content.Success && content.Published, Is.True, string.Join("\n", content.Errors));
                return new Fixture(new ActivityEventPreviewModel(), terrain.Catalog, terrain.StableDigest,
                    patterns.Catalog, patterns.StableDigest, content);
            }

            public ActivityEventPreviewBuildResult Build(string activityId, string eventId = "") =>
                Model.Build(new ActivityEventPreviewRequest(activityId, eventId,
                        ActivityEventPreviewModel.ApprovedAggregateDigest),
                    Terrain, TerrainDigest, Patterns, PatternDigest, Content);
        }
    }
}
