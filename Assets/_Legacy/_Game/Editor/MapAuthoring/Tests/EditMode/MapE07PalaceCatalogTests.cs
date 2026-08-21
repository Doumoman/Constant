#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE07PalaceCatalogTests
    {
        [Test]
        public void PalaceCatalogContainsEightBakedElementsAndSafetyContracts()
        {
            var definitions = PalaceElementCatalogFactory.EnsureCatalog();
            Assert.That(definitions.Count, Is.EqualTo(8));
            CollectionAssert.AreEquivalent(PalaceElementCatalogFactory.CatalogIds,
                definitions.Select(item => item.ElementId));
            Assert.That(definitions.Select(item => item.PalaceProfile.Kind).Distinct().Count(), Is.EqualTo(8));
            Assert.That(definitions.All(item => item.AllowedRegions == RegionMask.Palace), Is.True);

            var byId = definitions.ToDictionary(item => item.ElementId);
            var gate = byId["PALACE_SluiceGate"];
            Assert.That(gate.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(1, 3)));
            Assert.That(gate.PalaceProfile.PreventPermanentLock, Is.True);
            Assert.That(gate.PlacementProfile.RequiredNeighborTags, Does.Contain("NonLockingAlternateRoute"));
            Assert.That(gate.ToolReactions.Entries.Any(entry =>
                (entry.Tool & (ToolTag.Bomb | ToolTag.Pickaxe)) != 0 &&
                entry.Reaction != ElementReactionType.None), Is.False);

            var cannon = byId["PALACE_BubbleCannon"];
            Assert.That(cannon.PalaceProfile.IntervalSeconds, Is.EqualTo(1.8f).Within(0.0001f));
            Assert.That(cannon.PalaceProfile.UmbrellaPushMultiplier, Is.LessThan(1f));

            var current = byId["PALACE_CurrentVolume"];
            Assert.That(current.PalaceProfile.ExitSafePocketCells, Is.GreaterThanOrEqualTo(2));
            Assert.That(current.PlacementProfile.RequiredNeighborTags, Does.Contain("ExitSafePocket2Cells"));
            Assert.That(current.PalaceProfile.HeavyBlockMultiplier, Is.LessThan(1f));

            var turtle = byId["PALACE_TurtlePlatform"];
            Assert.That(turtle.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(turtle.PalaceProfile.SinkDepthCells, Is.EqualTo(1f).Within(0.0001f));

            var clam = byId["PALACE_ClamBounce"];
            Assert.That(clam.PalaceProfile.CycleSeconds, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(clam.PalaceProfile.ReflectProjectiles, Is.True);

            var mirror = byId["PALACE_WaterMirrorWall"];
            Assert.That(mirror.Footprint.BoundsSize.y, Is.InRange(2, 4));
            Assert.That(mirror.PalaceProfile.TransparentOnSignal, Is.True);
            Assert.That(mirror.PalaceProfile.TransparencyContextId, Is.EqualTo("yeouiju"));

            var drain = byId["PALACE_DrainGrate"];
            Assert.That(drain.PalaceProfile.StartsMudBlocked, Is.True);
            Assert.That(drain.PalaceProfile.KeepVoidRecoveryIndependent, Is.True);
            Assert.That(drain.PlacementProfile.RequiredNeighborTags,
                Does.Contain("VoidRecoveryWaterIndependent"));

            var waterfall = byId["PALACE_DragonGateWaterfall"];
            Assert.That(waterfall.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(3, 4)));
            Assert.That(waterfall.PalaceProfile.UmbrellaLiftMultiplier, Is.GreaterThan(1f));
            Assert.That(waterfall.PalaceProfile.CloudSupportMultiplier, Is.GreaterThan(1f));
            Assert.That(waterfall.PalaceProfile.CanRefillWateringCan, Is.True);

            foreach (var definition in definitions)
            {
                Assert.That(MapElementValidator.ValidateSourceForBake(definition).ErrorCount,
                    Is.Zero, definition.ElementId);
                Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<PalaceElementDriver>(), Is.Not.Null,
                    definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<ToolReactionReceiver>(), Is.Not.Null,
                    definition.ElementId);
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
