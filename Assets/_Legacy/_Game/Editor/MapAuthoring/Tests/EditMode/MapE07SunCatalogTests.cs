#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE07SunCatalogTests
    {
        [Test]
        public void SunCatalogContainsEightBakedElementsAndLightSafetyContracts()
        {
            var definitions = SunElementCatalogFactory.EnsureCatalog();
            Assert.That(definitions.Count, Is.EqualTo(8));
            CollectionAssert.AreEquivalent(SunElementCatalogFactory.CatalogIds,
                definitions.Select(item => item.ElementId));
            Assert.That(definitions.Select(item => item.SunProfile.Kind).Distinct().Count(), Is.EqualTo(8));
            Assert.That(definitions.All(item => item.AllowedRegions == RegionMask.Sun), Is.True);

            var byId = definitions.ToDictionary(item => item.ElementId);
            var sunbeam = byId["SUN_RotatingSunbeam"];
            Assert.That(sunbeam.SunProfile.ArcDegrees, Is.InRange(60f, 180f));
            Assert.That(sunbeam.SunProfile.Damage, Is.EqualTo(1));
            Assert.That(sunbeam.SunProfile.IgnoreSolidBlockers, Is.True);
            Assert.That(sunbeam.SunProfile.IgnoreUmbrellaBlock, Is.True);
            Assert.That(sunbeam.PlacementProfile.ForbiddenNeighborTags,
                Does.Contain("FullCycleOverlapWithOverheatPlatform"));

            var shadow = byId["SUN_ShadowSeed"];
            Assert.That(shadow.SunProfile.ShadowSizeCells, Is.EqualTo(new Vector2(2f, 2f)));
            Assert.That(shadow.SunProfile.WaterSuppressesShadow, Is.True);
            Assert.That(shadow.SunProfile.KeepExitMarkersVisible, Is.True);
            Assert.That(shadow.PlacementProfile.RequiredNeighborTags,
                Does.Contain("ExitMarkerVisibleInShadow"));

            var sunflower = byId["SUN_SunflowerPlatform"];
            Assert.That(sunflower.SunProfile.PlatformWidthCells, Is.InRange(1, 2));
            Assert.That(sunflower.SunProfile.PlatformRotationStepDegrees, Is.EqualTo(90));
            Assert.That(sunflower.SunProfile.ClosesOnOverheat, Is.True);

            var vine = byId["SUN_GrowthVine"];
            Assert.That(vine.SunProfile.MaxLengthCells, Is.GreaterThanOrEqualTo(1));
            Assert.That(vine.SunProfile.StopAtUnbreakableBoundary, Is.True);
            Assert.That(vine.PlacementProfile.ForbiddenNeighborTags,
                Does.Contain("UnbreakableBoundaryInGrowthPath"));

            var dew = byId["SUN_DewDrop"];
            Assert.That(dew.SunProfile.FallIntervalSeconds, Is.GreaterThan(0f));
            Assert.That(dew.SunProfile.CanFullyRefillWateringCan, Is.True);
            Assert.That(dew.SunProfile.CoolOnImpact, Is.True);

            var overheat = byId["SUN_OverheatPlatform"];
            Assert.That(overheat.SunProfile.SafeSeconds, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(overheat.SunProfile.OverheatSeconds, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(overheat.SunProfile.Damage, Is.EqualTo(1));
            Assert.That(overheat.PlacementProfile.ForbiddenNeighborTags,
                Does.Contain("FullCycleOverlapWithSunbeam"));

            var sunset = byId["SUN_SunsetFlower"];
            Assert.That(sunset.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(sunset.SunProfile.InitialPhase, Is.EqualTo(SunPhase.Day));

            var perch = byId["SUN_CrowPerch"];
            Assert.That(perch.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(perch.SunProfile.EventId, Is.Not.Empty);
            Assert.That(perch.SunProfile.AcceptedContextIds, Does.Contain("letter"));
            Assert.That(perch.SunProfile.AcceptedContextIds, Does.Contain("sun_ember"));

            foreach (var definition in definitions)
            {
                Assert.That(MapElementValidator.ValidateSourceForBake(definition).ErrorCount,
                    Is.Zero, definition.ElementId);
                Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<SunElementDriver>(), Is.Not.Null,
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
