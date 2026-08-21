#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE07PostCatalogTests
    {
        [Test]
        public void PostCatalogContainsEightBakedElementsAndPairSafetyContracts()
        {
            var definitions = PostElementCatalogFactory.EnsureCatalog();
            Assert.That(definitions.Count, Is.EqualTo(8));
            CollectionAssert.AreEquivalent(PostElementCatalogFactory.CatalogIds,
                definitions.Select(item => item.ElementId));
            Assert.That(definitions.Select(item => item.PostProfile.Kind).Distinct().Count(), Is.EqualTo(8));
            Assert.That(definitions.All(item => item.AllowedRegions == RegionMask.Post), Is.True);

            var byId = definitions.ToDictionary(item => item.ElementId);
            var conveyor = byId["POST_Conveyor"];
            Assert.That(conveyor.PostProfile.LengthCells, Is.InRange(2, 8));
            Assert.That(conveyor.PostProfile.SurfaceSpeedCellsPerSecond, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(conveyor.PostProfile.StopsOnHeavy, Is.True);
            Assert.That(conveyor.PlacementProfile.RequiredNeighborTags,
                Does.Contain("PortalExitSafeDestination"));

            var launcher = byId["POST_ParcelLauncher"];
            Assert.That(launcher.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(launcher.PostProfile.RequiresParcelInsertion, Is.True);
            Assert.That(launcher.PostProfile.RejectPlayerEntry, Is.True);
            Assert.That(launcher.PostProfile.CollisionDamage, Is.EqualTo(1));

            var stamp = byId["POST_ReturnStamp"];
            Assert.That(stamp.PostProfile.WarningDelaySeconds, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(stamp.PostProfile.EscapeSpaceBelowCells, Is.GreaterThanOrEqualTo(1));
            Assert.That(stamp.PlacementProfile.RequiredNeighborTags,
                Does.Contain("EscapeSpaceBelow1Cell"));

            var sortingArm = byId["POST_SortingArm"];
            Assert.That(sortingArm.PostProfile.RotationStepDegrees, Is.EqualTo(90));
            Assert.That(sortingArm.PostProfile.RotationSequenceDegrees.Count, Is.GreaterThanOrEqualTo(2));

            var mailTube = byId["POST_MailTube"];
            Assert.That(mailTube.PostProfile.RequiresPair, Is.True);
            Assert.That(mailTube.PostProfile.PairGuid, Is.Not.Empty);
            Assert.That(mailTube.PostProfile.OneWay, Is.False);
            Assert.That(mailTube.PlacementProfile.RequiredNeighborTags,
                Does.Contain("PairedTubeGuidRequired"));

            var ink = byId["POST_InkPool"];
            Assert.That(ink.PostProfile.WidthCells, Is.InRange(2, 6));
            Assert.That(ink.PostProfile.SlowRate, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(ink.PostProfile.WaterDilutes, Is.True);
            Assert.That(ink.PostProfile.UmbrellaBlocksDrops, Is.False);

            var stack = byId["POST_ParcelStack"];
            Assert.That(stack.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(stack.PostProfile.BoxCount, Is.EqualTo(4));
            Assert.That(stack.PostProfile.StackPattern, Is.EqualTo("2x2"));

            var express = byId["POST_ExpressTube"];
            Assert.That(express.PostProfile.RequiresPair, Is.True);
            Assert.That(express.PostProfile.PairGuid, Is.Not.Empty);
            Assert.That(express.PostProfile.OneWay, Is.True);
            Assert.That(express.PostProfile.RequiredStoryFlag, Is.Not.Empty);
            Assert.That(express.PostProfile.RequiredParcelId, Is.Not.Empty);

            foreach (var definition in definitions)
            {
                Assert.That(MapElementValidator.ValidateSourceForBake(definition).ErrorCount,
                    Is.Zero, definition.ElementId);
                Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<PostElementDriver>(), Is.Not.Null,
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
