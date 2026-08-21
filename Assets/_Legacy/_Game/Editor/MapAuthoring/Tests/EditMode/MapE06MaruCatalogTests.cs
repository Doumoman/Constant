#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE06MaruCatalogTests
    {
        [Test]
        public void MaruCatalogContainsSixBakedElementsWithVisibleRewardPenaltyContracts()
        {
            var definitions = MaruElementCatalogFactory.EnsureCatalog();
            Assert.That(definitions.Count, Is.EqualTo(6));
            Assert.That(definitions.Select(item => item.ElementId).Distinct().Count(), Is.EqualTo(6));
            Assert.That(definitions.Select(item => item.MaruProfile.Kind).Distinct().Count(), Is.EqualTo(6));
            CollectionAssert.AreEquivalent(
                MaruElementCatalogFactory.CatalogIds,
                definitions.Select(item => item.ElementId));

            var byId = definitions.ToDictionary(item => item.ElementId);
            var statue = byId["MARU_ReturnStatue"];
            Assert.That(statue.Footprint.BoundsSize.x, Is.EqualTo(1));
            Assert.That(statue.Footprint.BoundsSize.y, Is.EqualTo(2));
            Assert.That(statue.MaruProfile.DurabilityStages, Is.EqualTo(2));
            Assert.That(statue.MaruProfile.RewardMoney, Is.EqualTo(500));
            Assert.That(statue.MaruProfile.MinimumExitRoomDistance, Is.EqualTo(3));
            Assert.That(statue.MaruProfile.MaximumExitRoomDistance, Is.EqualTo(5));
            Assert.That(statue.MaruProfile.ForbidExitRoom, Is.True);

            var bellJar = byId["MARU_ReturnBellJar"];
            Assert.That(bellJar.MaruProfile.RewardMoney, Is.EqualTo(300));
            Assert.That(bellJar.MaruProfile.ScheduledEntryDelaySeconds, Is.EqualTo(12f).Within(0.0001f));
            Assert.That(bellJar.MaruProfile.MinimumAutomaticHazardDistanceCells, Is.EqualTo(3));

            var collar = byId["MARU_CollarFragment"];
            Assert.That(collar.MaruProfile.TimerRateMultiplier, Is.EqualTo(1.15f).Within(0.0001f));
            Assert.That(collar.MaruProfile.PressureWeight, Is.EqualTo(2));

            var marker = byId["MARU_ReturnMarker"];
            Assert.That(marker.MaruProfile.MarkerCostType, Is.EqualTo(MaruMarkerCostType.Money));
            Assert.That(marker.MaruProfile.MarkerCostValue, Is.EqualTo(50));

            var pawprint = byId["MARU_PawprintPool"];
            Assert.That(pawprint.Footprint.BoundsSize.x, Is.EqualTo(2));
            Assert.That(pawprint.MaruProfile.GuidanceSeconds, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(pawprint.MaruProfile.ShortenNextBellSeconds, Is.EqualTo(8f).Within(0.0001f));

            var casket = byId["MARU_RecordCasket"];
            Assert.That(casket.Footprint.BoundsSize.x, Is.EqualTo(2));
            Assert.That(casket.Footprint.BoundsSize.y, Is.EqualTo(2));
            Assert.That(casket.MaruProfile.DurabilityStages, Is.EqualTo(2));
            Assert.That(casket.ToolReactions.Entries.Any(entry =>
                (entry.Tool & (ToolTag.Bomb | ToolTag.Pickaxe)) != 0 &&
                entry.Reaction != ElementReactionType.None), Is.False);

            foreach (var definition in definitions)
            {
                Assert.That(definition.MaruProfile.PreviewRewardText, Is.Not.Empty, definition.ElementId);
                Assert.That(definition.MaruProfile.PreviewPenaltyText, Is.Not.Empty, definition.ElementId);
                Assert.That(MapElementValidator.ValidateSourceForBake(definition).ErrorCount, Is.Zero, definition.ElementId);
                Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<MaruElementDriver>(), Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<ToolReactionReceiver>(), Is.Not.Null, definition.ElementId);
                var paths = AssetPathUtility.GetMapElementBakePaths(definition);
                var baked = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(paths.Definition);
                Assert.That(baked, Is.Not.Null, definition.ElementId);
                Assert.That(MapElementValidator.ValidateBakedDefinition(baked).ErrorCount, Is.Zero, definition.ElementId);
            }

            Assert.That(MapBuildTag.Milestone, Is.EqualTo("MAP-E11-BatchValidation"));
        }
    }
}

#endif
