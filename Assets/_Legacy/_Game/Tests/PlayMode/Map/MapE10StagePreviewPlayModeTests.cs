#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using StarNight.Stage.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE10StagePreviewPlayModeTests
    {
        [UnityTest]
        public IEnumerator GhostMovesRoomByRoomWithSingleFullRoomFocusAndMaruPreview()
        {
            var root = new GameObject("E10SimulationRoot");
            Camera camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform);
            camera.transform.position = new Vector3(0f, 0f, -20f);
            camera.orthographic = true;
            Transform ghost = new GameObject("GhostPlayer").transform;
            ghost.SetParent(root.transform);

            RoomTemplate template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.RoomId = "E10Room";
            template.SizeCells = RoomSizeCatalog.Micro;
            template.Sockets = new List<RoomSocketDefinition>();
            StageRoomProxy start = CreateRoom("start", RoomRole.Start, new Vector2Int(0, 0), template);
            StageRoomProxy middle = CreateRoom("middle", RoomRole.Main, new Vector2Int(16, 0), template);
            StageRoomProxy exit = CreateRoom("exit", RoomRole.Exit, new Vector2Int(32, 0), template);
            var rooms = new[] { start, middle, exit };
            var edges = new[]
            {
                CreateEdge("a", start, middle),
                CreateEdge("b", middle, exit),
            };
            var previews = new[]
            {
                CreateFullPreview(start), CreateFullPreview(middle), CreateFullPreview(exit),
            };

            GameObject maruObject = new GameObject("MaruRoute");
            LineRenderer line = maruObject.AddComponent<LineRenderer>();
            Transform marker = new GameObject("MaruGhost").transform;
            marker.SetParent(maruObject.transform);
            StageMaruRoutePreview maru = maruObject.AddComponent<StageMaruRoutePreview>();
            maru.Configure(line, marker, rooms);

            StageLayoutSimulationController controller = root.AddComponent<StageLayoutSimulationController>();
            controller.Configure(camera, ghost, maru, rooms, edges, previews);
            controller.BeginSimulation(false);
            Assert.That(controller.CurrentRoom, Is.EqualTo(start));
            Assert.That(controller.VisibleFullRoomCount, Is.EqualTo(1));
            Assert.That(controller.PreviewCamera.orthographicSize, Is.LessThan(5f));

            Assert.That(controller.MoveNextRoom(false), Is.True);
            Assert.That(controller.IsTransitioning, Is.True);
            Assert.That(controller.TransitionSeconds, Is.EqualTo(0.28f).Within(0.0001f));
            controller.CompleteTransitionImmediate();
            Assert.That(controller.CurrentRoom, Is.EqualTo(middle));
            Assert.That(controller.VisibleFullRoomCount, Is.EqualTo(1));

            controller.SetVirtualPhase(StageLayoutSimulationPhase.MaruChase);
            Assert.That(maru.IsVisible, Is.True);
            Assert.That(controller.MoveNextRoom(true), Is.True);
            Assert.That(controller.CurrentRoom, Is.EqualTo(exit));
            Assert.That(controller.Phase, Is.EqualTo(StageLayoutSimulationPhase.ExitReached));
            Assert.That(controller.ExitArrivalSeconds, Is.GreaterThanOrEqualTo(0f));
            Assert.That(controller.VisibleFullRoomCount, Is.EqualTo(1));

            Object.Destroy(root);
            Object.Destroy(start.gameObject);
            Object.Destroy(middle.gameObject);
            Object.Destroy(exit.gameObject);
            Object.Destroy(maruObject);
            Object.Destroy(template);
            yield return null;
        }

        private static StageRoomProxy CreateRoom(string id, RoomRole role, Vector2Int position, RoomTemplate template)
        {
            var roomObject = new GameObject(id);
            StageRoomProxy room = roomObject.AddComponent<StageRoomProxy>();
            room.ConfigureGenerated(id, template, position, role, false, true);
            return room;
        }

        private static StageLayoutConnectionProxy CreateEdge(string id, StageRoomProxy source, StageRoomProxy target)
        {
            StageLayoutConnectionProxy edge = new GameObject(id).AddComponent<StageLayoutConnectionProxy>();
            edge.Configure(id, source, string.Empty, target, string.Empty, StageConnectionVisualKind.MainRoute);
            return edge;
        }

        private static StageFullRoomPreviewInstance CreateFullPreview(StageRoomProxy room)
        {
            GameObject wrapper = new GameObject($"full-{room.NodeGuid}");
            GameObject content = new GameObject("content");
            content.transform.SetParent(wrapper.transform);
            StageFullRoomPreviewInstance preview = wrapper.AddComponent<StageFullRoomPreviewInstance>();
            preview.Configure(room, content, true);
            preview.SetVisible(false);
            return preview;
        }
    }
}

#endif
