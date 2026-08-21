#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.MapAuthoring.Editor;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE10SimulationBakeTests
    {
        [Test]
        public void SimulationPreviewSnapshotAndPlayablePreviewSceneHonorE10Contract()
        {
            EditorSceneManager.OpenScene(EditorSceneBuildGuard.StageLayoutLabPath);
            StageMapProfile profile = StageMapProfileSampleFactory.EnsureSample();
            IReadOnlyList<RoomTemplate> templates = RoomTemplateSampleFactory.EnsureSamples();
            StageGeneratedLayout layout = StageMapGenerator.Generate(profile, templates, 10801);
            StageLayoutPreviewApplier.Apply(layout, false);

            StageLayoutSimulationController controller = Object.FindFirstObjectByType<StageLayoutSimulationController>(FindObjectsInactive.Include);
            StageMaruRoutePreview maru = Object.FindFirstObjectByType<StageMaruRoutePreview>(FindObjectsInactive.Include);
            StageFullRoomPreviewInstance[] fullRooms = Object.FindObjectsByType<StageFullRoomPreviewInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.TransitionSeconds, Is.EqualTo(0.28f).Within(0.0001f));
            Assert.That(controller.MainRoute.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(fullRooms.Length, Is.EqualTo(layout.Rooms.Count));
            Assert.That(maru.RouteRoomCount, Is.EqualTo(controller.MainRoute.Count));

            controller.BeginSimulation(false);
            StageRoomProxy start = controller.CurrentRoom;
            Assert.That(start.Role, Is.EqualTo(RoomRole.Start));
            Assert.That(controller.VisibleFullRoomCount, Is.EqualTo(1));
            Assert.That(controller.MoveNextRoom(false), Is.True);
            Assert.That(controller.IsTransitioning, Is.True);
            controller.CompleteTransitionImmediate();
            Assert.That(controller.CurrentRoom, Is.Not.EqualTo(start));
            controller.SetVirtualPhase(StageLayoutSimulationPhase.MaruChase);
            Assert.That(maru.IsVisible, Is.True);

            StageLayoutSnapshot snapshot = StageLayoutSnapshotBaker.BakeCurrentScene(profile, layout.Seed, layout.ValidationHash);
            Assert.That(snapshot.Rooms.Count, Is.EqualTo(layout.Rooms.Count));
            Assert.That(snapshot.Connections.Count, Is.EqualTo(layout.Connections.Count));
            Assert.That(snapshot.ValidationHash, Is.EqualTo(layout.ValidationHash));

            string previewPath = StagePreviewSceneBuilder.BuildCurrentScene(profile, layout.Seed, snapshot);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(previewPath), Is.Not.Null);
            CollectionAssert.Contains(EditorSceneBuildGuard.FindForbiddenScenePaths(new[] { previewPath }), previewPath);
            Assert.That(StageLayoutValidator.ValidateCurrentScene().ErrorCount, Is.Zero);
        }
    }
}

#endif
