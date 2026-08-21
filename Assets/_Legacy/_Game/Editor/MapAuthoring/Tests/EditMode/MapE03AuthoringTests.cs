#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE03AuthoringTests
    {
        [Test]
        public void LabCreatesRequiredPresetsWithoutCodeChanges()
        {
            var spike = MapElementDefinitionPresetFactory.CreatePreset(MapElementLabPreset.Spike1x1);
            var platform = MapElementDefinitionPresetFactory.CreatePreset(MapElementLabPreset.MovingPlatform2x1);
            try
            {
                Assert.That(MapBuildTag.Milestone, Is.EqualTo("MAP-E11-BatchValidation"));
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneBuildGuard.MapElementLabPath),
                    Is.Not.Null,
                    "00_MapElementLab scene must exist before authoring tests run.");
                Assert.That(EditorSceneBuildGuard.FindForbiddenScenesInCurrentBuildSettings(), Is.Empty);

                Assert.That(spike.Footprint.BoundsSize, Is.EqualTo(Vector2Int.one));
                Assert.That(spike.Footprint.OccupiedCells, Is.EquivalentTo(new[] { Vector2Int.zero }));
                Assert.That(spike.Footprint.HazardCells, Does.Contain(Vector2Int.zero));
                Assert.That(spike.CollisionProfile.TriggerShapes, Has.Count.EqualTo(1));

                Assert.That(platform.Footprint.BoundsSize, Is.EqualTo(new Vector2Int(2, 1)));
                Assert.That(platform.Footprint.OccupiedCells, Is.EquivalentTo(new[]
                {
                    Vector2Int.zero,
                    Vector2Int.right,
                }));
                Assert.That(platform.VisualProfile.RenderMode, Is.EqualTo(ElementVisualRenderMode.TiledSprite));
                Assert.That(platform.VisualProfile.VisualSizeCells, Is.EqualTo(new Vector2(2f, 1f)));
                Assert.That(platform.CollisionProfile.SolidShapes[0].SizeCells.x, Is.EqualTo(1.98f));
                Assert.That(platform.BehaviorProfile.Path.Nodes, Has.Count.EqualTo(2));
                Assert.That(platform.BehaviorProfile.Path.PingPong, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(spike);
                Object.DestroyImmediate(platform);
            }
        }
    }
}

#endif
