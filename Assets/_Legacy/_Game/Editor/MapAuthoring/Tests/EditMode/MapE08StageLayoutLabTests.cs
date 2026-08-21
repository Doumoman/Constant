#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.MapAuthoring.Editor;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE08StageLayoutLabTests
    {
        [Test]
        public void StageLayoutLabProvidesVariableRoomsConnectionsAndCleanValidation()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneBuildGuard.StageLayoutLabPath), Is.Not.Null);
            EditorSceneManager.OpenScene(EditorSceneBuildGuard.StageLayoutLabPath);

            StageRoomProxy[] rooms = Object.FindObjectsByType<StageRoomProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            StageLayoutConnectionProxy[] connections = Object.FindObjectsByType<StageLayoutConnectionProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(rooms.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(connections.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(Object.FindFirstObjectByType<StageLayoutGridGuide>(), Is.Not.Null);

            var sizes = new System.Collections.Generic.HashSet<Vector2Int>();
            for (int index = 0; index < rooms.Length; index++) sizes.Add(rooms[index].SizeCells);
            Assert.That(sizes, Does.Contain(RoomSizeCatalog.Micro));
            Assert.That(sizes, Does.Contain(RoomSizeCatalog.Wide));
            Assert.That(sizes, Does.Contain(RoomSizeCatalog.Tall));
            Assert.That(sizes, Does.Contain(RoomSizeCatalog.Large));

            MapElementValidationReport report = StageLayoutValidator.ValidateCurrentScene();
            Assert.That(report.ErrorCount, Is.Zero, report.CreateSummary());
            Assert.That(GameObject.Find("LayoutCanvas/ValidationSummary"), Is.Not.Null);
        }
    }
}

#endif
