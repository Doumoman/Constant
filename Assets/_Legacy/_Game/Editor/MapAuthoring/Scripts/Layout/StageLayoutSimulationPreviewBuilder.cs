#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using StarNight.Stage.CameraSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarNight.MapAuthoring.Editor
{
    public static class StageLayoutSimulationPreviewBuilder
    {
        public static StageLayoutSimulationController Rebuild(
            IReadOnlyCollection<StageRoomProxy> rooms,
            IReadOnlyCollection<StageLayoutConnectionProxy> connections,
            bool registerUndo)
        {
            Transform fullRoomRoot = FindOrCreateRoot("FullRoomPreviewRoot");
            Transform maruRoot = FindOrCreateRoot("MaruPathPreviewRoot");
            Transform simulationRoot = FindOrCreateRoot("CameraSimulationRoot");
            ClearChildren(fullRoomRoot, registerUndo);
            ClearChildren(maruRoot, registerUndo);

            var fullPreviews = new List<StageFullRoomPreviewInstance>();
            foreach (StageRoomProxy room in rooms.OrderBy(room => room.NodeGuid))
                fullPreviews.Add(CreateFullRoomPreview(fullRoomRoot, room, registerUndo));

            Camera camera = EnsureCamera(simulationRoot);
            Transform ghost = EnsureGhostPlayer(simulationRoot);
            StageLayoutSimulationController controller = simulationRoot.GetComponent<StageLayoutSimulationController>();
            if (controller == null) controller = simulationRoot.gameObject.AddComponent<StageLayoutSimulationController>();
            controller.Configure(camera, ghost, null, rooms, connections, fullPreviews);

            StageMaruRoutePreview maruPreview = CreateMaruPreview(maruRoot, controller.MainRoute, registerUndo);
            controller.Configure(camera, ghost, maruPreview, rooms, connections, fullPreviews);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static StageFullRoomPreviewInstance CreateFullRoomPreview(Transform root, StageRoomProxy room, bool registerUndo)
        {
            string shortGuid = string.IsNullOrEmpty(room.NodeGuid) ? "Missing" : room.NodeGuid.Substring(0, Mathf.Min(6, room.NodeGuid.Length));
            GameObject wrapper = new GameObject($"FullRoom_{room.Role}_{shortGuid}");
            RegisterCreated(wrapper, registerUndo);
            wrapper.transform.SetParent(root, false);
            wrapper.transform.position = room.transform.position;
            StageFullRoomPreviewInstance preview = wrapper.AddComponent<StageFullRoomPreviewInstance>();

            GameObject content = new GameObject("Content");
            RegisterCreated(content, registerUndo);
            content.transform.SetParent(wrapper.transform, false);
            bool fallback = room.Template == null || room.Template.RoomPrefab == null;
            if (!fallback)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(room.Template.RoomPrefab, content.transform) as GameObject;
                if (instance != null)
                {
                    instance.name = "FullRoomPrefab";
                    instance.transform.localPosition = Vector3.zero;
                }
            }
            else
            {
                CreateFallbackShell(content.transform, room);
            }

            preview.Configure(room, content, fallback);
            preview.SetVisible(false);
            return preview;
        }

        private static void CreateFallbackShell(Transform parent, StageRoomProxy room)
        {
            float width = room.SizeCells.x * StageRoomProxy.PreviewCellScale;
            float height = room.SizeCells.y * StageRoomProxy.PreviewCellScale;
            GameObject fill = CreatePrimitive(PrimitiveType.Quad, "RoomPrefabFallback", parent);
            fill.transform.localPosition = new Vector3(width * 0.5f, height * 0.5f, -0.08f);
            fill.transform.localScale = new Vector3(width, height, 1f);
            fill.AddComponent<StagePreviewColor>().Configure(new Color(0.06f, 0.11f, 0.19f, 1f));

            GameObject borderObject = new GameObject("CameraBoundsOverlay");
            borderObject.transform.SetParent(parent, false);
            LineRenderer border = borderObject.AddComponent<LineRenderer>();
            ConfigureLine(border, new Color(0.3f, 0.75f, 1f), 0.055f);
            border.positionCount = 5;
            border.useWorldSpace = false;
            border.SetPositions(new[]
            {
                new Vector3(0f, 0f, -0.1f), new Vector3(width, 0f, -0.1f),
                new Vector3(width, height, -0.1f), new Vector3(0f, height, -0.1f), new Vector3(0f, 0f, -0.1f),
            });

            GameObject safe = CreatePrimitive(PrimitiveType.Sphere, "SafeCellOverlay", parent);
            safe.transform.localPosition = new Vector3(width * 0.5f, Mathf.Min(0.3f, height * 0.2f), -0.14f);
            safe.transform.localScale = Vector3.one * 0.16f;
            safe.AddComponent<StagePreviewColor>().Configure(new Color(0.25f, 1f, 0.45f));

            GameObject recoveryObject = new GameObject("VoidRecoveryOverlay");
            recoveryObject.transform.SetParent(parent, false);
            LineRenderer recovery = recoveryObject.AddComponent<LineRenderer>();
            ConfigureLine(recovery, new Color(1f, 0.25f, 0.25f), 0.04f);
            recovery.useWorldSpace = false;
            recovery.positionCount = 2;
            recovery.SetPositions(new[] { new Vector3(0f, -0.22f, -0.12f), new Vector3(width, -0.22f, -0.12f) });

            if (room.Template?.Sockets == null) return;
            for (int index = 0; index < room.Template.Sockets.Count; index++)
            {
                RoomSocketDefinition socket = room.Template.Sockets[index];
                if (socket == null) continue;
                GameObject portal = CreatePrimitive(PrimitiveType.Cube, $"Portal_{socket.SocketGuid}", parent);
                portal.transform.localPosition = new Vector3(
                    socket.LocalCell.x * StageRoomProxy.PreviewCellScale,
                    socket.LocalCell.y * StageRoomProxy.PreviewCellScale,
                    -0.16f);
                portal.transform.localScale = new Vector3(0.13f, 0.22f, 0.05f);
                portal.AddComponent<StagePreviewColor>().Configure(new Color(0.2f, 1f, 0.72f));
            }
        }

        private static StageMaruRoutePreview CreateMaruPreview(
            Transform root,
            IReadOnlyList<StageRoomProxy> route,
            bool registerUndo)
        {
            StageMaruRoutePreview preview = root.gameObject.GetComponent<StageMaruRoutePreview>();
            if (preview == null) preview = root.gameObject.AddComponent<StageMaruRoutePreview>();

            GameObject lineObject = new GameObject("MaruLane");
            RegisterCreated(lineObject, registerUndo);
            lineObject.transform.SetParent(root, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            ConfigureLine(line, new Color(1f, 0.18f, 0.18f), 0.08f);
            line.numCapVertices = 3;

            GameObject marker = CreatePrimitive(PrimitiveType.Sphere, "MaruGhost", root);
            marker.transform.localScale = Vector3.one * 0.28f;
            marker.AddComponent<StagePreviewColor>().Configure(new Color(1f, 0.1f, 0.1f));
            preview.Configure(line, marker.transform, route);
            EditorUtility.SetDirty(preview);
            return preview;
        }

        private static Camera EnsureCamera(Transform simulationRoot)
        {
            Transform cameraTransform = simulationRoot.Find("PreviewCamera");
            if (cameraTransform == null)
            {
                var cameraObject = new GameObject("PreviewCamera");
                cameraObject.transform.SetParent(simulationRoot, false);
                cameraTransform = cameraObject.transform;
            }
            Camera camera = cameraTransform.GetComponent<Camera>();
            if (camera == null) camera = cameraTransform.gameObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.075f);
            cameraTransform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y, -20f);
            cameraTransform.gameObject.tag = "MainCamera";
            CameraCriticalFrame frame = cameraTransform.GetComponent<CameraCriticalFrame>();
            if (frame == null) frame = cameraTransform.gameObject.AddComponent<CameraCriticalFrame>();
            frame.Configure(new CameraTileProfile(), StageRoomProxy.PreviewCellScale);
            return camera;
        }

        private static Transform EnsureGhostPlayer(Transform simulationRoot)
        {
            Transform ghost = simulationRoot.Find("GhostPlayer");
            if (ghost == null)
            {
                var ghostObject = new GameObject("GhostPlayer");
                ghostObject.transform.SetParent(simulationRoot, false);
                ghost = ghostObject.transform;
            }
            if (ghost.Find("GhostVisual") == null)
            {
                GameObject visual = CreatePrimitive(PrimitiveType.Capsule, "GhostVisual", ghost);
                visual.transform.localScale = new Vector3(0.16f, 0.22f, 0.12f);
                visual.AddComponent<StagePreviewColor>().Configure(new Color(0.45f, 0.95f, 1f));
            }
            return ghost;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent)
        {
            GameObject created = GameObject.CreatePrimitive(type);
            created.name = name;
            created.transform.SetParent(parent, false);
            Collider collider = created.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            MeshRenderer renderer = created.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return created;
        }

        private static void ConfigureLine(LineRenderer line, Color color, float width)
        {
            line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        private static Transform FindOrCreateRoot(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null) return found.transform;
            GameObject layoutRoot = GameObject.Find("LayoutLabRoot");
            var created = new GameObject(name);
            created.transform.SetParent(layoutRoot != null ? layoutRoot.transform : null, false);
            return created.transform;
        }

        private static void ClearChildren(Transform root, bool registerUndo)
        {
            while (root.childCount > 0)
            {
                GameObject child = root.GetChild(0).gameObject;
                if (registerUndo) Undo.DestroyObjectImmediate(child);
                else Object.DestroyImmediate(child);
            }
        }

        private static void RegisterCreated(GameObject created, bool registerUndo)
        {
            if (registerUndo) Undo.RegisterCreatedObjectUndo(created, "Build Stage Simulation Preview");
        }
    }
}

#endif
