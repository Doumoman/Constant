#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE07MoonCatalogTests
    {
        [Test]
        public void MoonCatalogContainsEightBakedRegionElementsAndPlacementContracts()
        {
            var definitions = MoonElementCatalogFactory.EnsureCatalog();
            Assert.That(definitions.Count, Is.EqualTo(8));
            CollectionAssert.AreEquivalent(MoonElementCatalogFactory.CatalogIds,
                definitions.Select(item => item.ElementId));
            Assert.That(definitions.Select(item => item.MoonProfile.Kind).Distinct().Count(), Is.EqualTo(8));
            Assert.That(definitions.All(item => item.AllowedRegions == RegionMask.Moon), Is.True);

            var byId = definitions.ToDictionary(item => item.ElementId);
            var ironBall = byId["MOON_MoonIronBall"];
            Assert.That(ironBall.MoonProfile.ChainLengthCells, Is.InRange(2, 4));
            Assert.That(ironBall.MoonProfile.SwingArcDegrees, Is.EqualTo(50f).Within(0.0001f));
            Assert.That(ironBall.MoonProfile.SwingPeriodSeconds, Is.EqualTo(2.6f).Within(0.0001f));
            Assert.That(ironBall.PlacementProfile.ForbiddenNeighborTags, Does.Contain("EntrySafeZone"));

            var mortar = byId["MOON_FallingMortar"];
            Assert.That(mortar.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(mortar.MoonProfile.ShadowWarningSeconds, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(mortar.PlacementProfile.ForbiddenNeighborTags, Does.Contain("Crusher"));

            var dough = byId["MOON_DoughPlatform"];
            Assert.That(dough.MoonProfile.WidthCells, Is.InRange(1, 4));
            Assert.That(dough.MoonProfile.CompressionCells, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(dough.PlacementProfile.RequiredNeighborTags, Does.Contain("FallLandingOrPuzzleResult"));

            var slab = byId["MOON_CraterSlab"];
            Assert.That(slab.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(slab.MoonProfile.FallDelaySeconds, Is.EqualTo(0.5f).Within(0.0001f));

            var root = byId["MOON_CassiaRoot"];
            Assert.That(root.MoonProfile.MinimumSegmentCount, Is.EqualTo(2));
            Assert.That(root.MoonProfile.SegmentCount, Is.InRange(2, 8));
            Assert.That(root.PlacementProfile.ForbiddenNeighborTags, Does.Contain("Portal"));

            var shaft = byId["MOON_MillShaft"];
            Assert.That(shaft.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(shaft.MoonProfile.StepAngleDegrees, Is.EqualTo(90f).Within(0.0001f));

            var medicine = byId["MOON_MedicineMortar"];
            Assert.That(medicine.MoonProfile.InputSlots, Is.GreaterThan(0));
            Assert.That(medicine.MoonProfile.OutputId, Is.Not.Empty);

            var vent = byId["MOON_FlourVent"];
            Assert.That(vent.Footprint.BoundsSize, Is.EqualTo(Vector2Int.one));
            Assert.That(vent.MoonProfile.CycleOnSeconds, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(vent.MoonProfile.CycleOffSeconds, Is.EqualTo(1f).Within(0.0001f));

            foreach (var definition in definitions)
            {
                Assert.That(MapElementValidator.ValidateSourceForBake(definition).ErrorCount,
                    Is.Zero, definition.ElementId);
                Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<MoonElementDriver>(), Is.Not.Null, definition.ElementId);
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
