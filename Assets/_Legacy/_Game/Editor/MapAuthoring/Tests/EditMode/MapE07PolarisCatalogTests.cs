#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE07PolarisCatalogTests
    {
        [Test]
        public void PolarisCatalogContainsEightBakedElementsAndObservatoryContracts()
        {
            var definitions = PolarisElementCatalogFactory.EnsureCatalog();
            Assert.That(definitions.Count, Is.EqualTo(8));
            CollectionAssert.AreEquivalent(PolarisElementCatalogFactory.CatalogIds,
                definitions.Select(item => item.ElementId));
            Assert.That(definitions.Select(item => item.PolarisProfile.Kind).Distinct().Count(),
                Is.EqualTo(8));
            Assert.That(definitions.All(item => item.AllowedRegions == RegionMask.Polaris), Is.True);

            var byId = definitions.ToDictionary(item => item.ElementId);

            var orbit = byId["POLARIS_OrbitPlatform"];
            Assert.That(orbit.PolarisProfile.PlatformWidthCells, Is.InRange(1, 2));
            Assert.That(orbit.PolarisProfile.KeepOrbitInsideCamera, Is.True);
            Assert.That(orbit.PlacementProfile.RequiredNeighborTags,
                Does.Contain("OrbitPathInsideCameraBounds"));

            var beam = byId["POLARIS_ObservationBeam"];
            Assert.That(beam.PolarisProfile.Damage, Is.EqualTo(1));
            Assert.That(beam.PolarisProfile.AppliesReturnMark, Is.True);
            Assert.That(beam.PolarisProfile.MirrorCanReflect, Is.True);
            Assert.That(beam.PolarisProfile.UmbrellaCanReflect, Is.False);
            Assert.That(beam.PolarisProfile.SignalChangesDirection, Is.True);

            var returnField = byId["POLARIS_ReturnField"];
            Assert.That(returnField.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(4, 2)));
            Assert.That(returnField.PolarisProfile.RequiresEntryAnchor, Is.True);
            Assert.That(returnField.PolarisProfile.DestinationAnchorId, Is.EqualTo("EntryAnchor"));
            Assert.That(returnField.PlacementProfile.RequiredNeighborTags,
                Does.Contain("EntryAnchorRequired"));

            var weight = byId["POLARIS_StarWeight"];
            Assert.That(weight.PolarisProfile.MassTag, Is.EqualTo("Heavy"));
            Assert.That(weight.PolarisProfile.PressureWeight, Is.EqualTo(2));
            Assert.That(weight.PolarisProfile.HeavyCarryAllowed, Is.True);
            Assert.That(weight.PolarisProfile.HookPullAllowed, Is.True);

            var dial = byId["POLARIS_GravityDial"];
            Assert.That(dial.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(dial.PolarisProfile.MaxInstancesPerRoom, Is.EqualTo(1));
            Assert.That(dial.PolarisProfile.LowGravityScale,
                Is.LessThan(dial.PolarisProfile.NormalGravityScale));
            Assert.That(dial.PlacementProfile.RequiredNeighborTags,
                Does.Contain("UniqueGravityDialPerRoom"));

            var bridge = byId["POLARIS_ConstellationBridge"];
            Assert.That(bridge.Footprint.BoundsSize, Is.EqualTo(Vector2Int.one));
            Assert.That(bridge.PolarisProfile.NodeGuids.Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(bridge.PolarisProfile.BridgeCellCount, Is.GreaterThan(0));

            var bell = byId["POLARIS_MemoryBell"];
            Assert.That(bell.PolarisProfile.RhythmPattern, Is.Not.Empty);
            Assert.That(bell.PolarisProfile.InteractionClearanceCells, Is.EqualTo(3));
            Assert.That(bell.PlacementProfile.ForbiddenNeighborTags,
                Does.Contain("OtherXInteractionWithin3Cells"));

            var immutable = byId["POLARIS_ImmutableStarBlock"];
            Assert.That(immutable.PolarisProfile.IgnoreAllTools, Is.True);
            Assert.That(immutable.PolarisProfile.VisualVariant, Is.Not.Empty);
            Assert.That(immutable.ToolReactions.Entries, Is.Empty);

            foreach (var definition in definitions)
            {
                Assert.That(MapElementValidator.ValidateSourceForBake(definition).ErrorCount,
                    Is.Zero, definition.ElementId);
                Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<PolarisElementDriver>(), Is.Not.Null,
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
