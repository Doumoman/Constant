#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE07BridgeCatalogTests
    {
        [Test]
        public void BridgeCatalogContainsEightBakedElementsAndPlacementContracts()
        {
            var definitions = BridgeElementCatalogFactory.EnsureCatalog();
            Assert.That(definitions.Count, Is.EqualTo(8));
            CollectionAssert.AreEquivalent(BridgeElementCatalogFactory.CatalogIds,
                definitions.Select(item => item.ElementId));
            Assert.That(definitions.Select(item => item.BridgeProfile.Kind).Distinct().Count(), Is.EqualTo(8));
            Assert.That(definitions.All(item => item.AllowedRegions == RegionMask.Bridge), Is.True);

            var byId = definitions.ToDictionary(item => item.ElementId);
            var bridge = byId["BRIDGE_ThreadBridge"];
            Assert.That(bridge.BridgeProfile.LengthCells, Is.InRange(2, 8));
            Assert.That(bridge.BridgeProfile.SagCells, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(bridge.PlacementProfile.RequiredNeighborTags,
                Does.Contain("AlternativeRouteOrVoidRecovery"));

            var pulley = byId["BRIDGE_KnotPulley"];
            Assert.That(pulley.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(pulley.BridgeProfile.TravelCells, Is.GreaterThan(0f));

            var banner = byId["BRIDGE_WindBanner"];
            Assert.That(banner.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(banner.BridgeProfile.FlipOnSignal, Is.True);
            Assert.That(banner.BridgeProfile.WetForceMultiplier, Is.LessThan(1f));

            var blade = byId["BRIDGE_ThreadBlade"];
            Assert.That(blade.BridgeProfile.PathSpeedCellsPerSecond, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(blade.BridgeProfile.Damage, Is.EqualTo(1));
            Assert.That(blade.BridgeProfile.MinimumStrongCrosswindDistanceCells, Is.EqualTo(6));

            var magpie = byId["BRIDGE_MagpiePlatform"];
            Assert.That(magpie.BridgeProfile.PlatformWidthCells, Is.InRange(1, 2));
            Assert.That(magpie.PlacementProfile.RequiredNeighborTags, Does.Contain("BaseRouteFallback"));

            var updraft = byId["BRIDGE_FeatherUpdraft"];
            Assert.That(updraft.BridgeProfile.VolumeSizeCells, Is.EqualTo(new Vector2(2f, 4f)));
            Assert.That(updraft.BridgeProfile.UmbrellaLiftMultiplier, Is.GreaterThan(1f));

            var panel = byId["BRIDGE_BreakingStarPanel"];
            Assert.That(panel.BridgeProfile.HitCount, Is.EqualTo(2));
            Assert.That(panel.BridgeProfile.DwellBreakSeconds, Is.GreaterThan(0f));

            var nest = byId["BRIDGE_Nest"];
            Assert.That(nest.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(nest.BridgeProfile.RequiredPieces, Is.EqualTo(3));
            Assert.That(nest.BridgeProfile.CriticalObject, Is.True);
            Assert.That(nest.ToolReactions.Entries.Any(entry =>
                (entry.Tool & ToolTag.Bomb) != 0 && entry.Reaction != ElementReactionType.None), Is.False);

            foreach (var definition in definitions)
            {
                Assert.That(MapElementValidator.ValidateSourceForBake(definition).ErrorCount,
                    Is.Zero, definition.ElementId);
                Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<BridgeElementDriver>(), Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<ToolReactionReceiver>(), Is.Not.Null, definition.ElementId);
                var paths = AssetPathUtility.GetMapElementBakePaths(definition);
                var baked = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(paths.Definition);
                Assert.That(baked, Is.Not.Null, definition.ElementId);
                Assert.That(MapElementValidator.ValidateBakedDefinition(baked).ErrorCount,
                    Is.Zero, definition.ElementId);
            }

            Assert.That(MapBuildTag.Milestone, Is.EqualTo("MAP-E11-BatchValidation"));
        }
    }
}

#endif
