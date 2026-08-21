#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class StageLayoutPreviewApplier
    {
        public static void Apply(StageGeneratedLayout layout, bool registerUndo = true)
        {
            if (layout == null) return;
            Transform roomRoot = FindOrCreateRoot("RoomProxyRoot");
            Transform graphRoot = FindOrCreateRoot("GraphLineRoot");
            Transform corridorRoot = FindOrCreateRoot("CorridorProxyRoot");
            Transform slotRoot = FindOrCreateRoot("ElementSlotPreviewRoot");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate Stage Layout Preview");
            ClearChildren(roomRoot, registerUndo);
            ClearChildren(graphRoot, registerUndo);
            ClearChildren(corridorRoot, registerUndo);
            ClearChildren(slotRoot, registerUndo);

            var proxies = new Dictionary<string, StageRoomProxy>(StringComparer.Ordinal);
            for (int index = 0; index < layout.Rooms.Count; index++)
            {
                StageGeneratedRoom room = layout.Rooms[index];
                var roomObject = new GameObject($"Room_{index:D2}_{room.Role}_{room.Template.RoomId}");
                RegisterCreated(roomObject, registerUndo);
                roomObject.transform.SetParent(roomRoot, false);
                StageRoomProxy proxy = roomObject.AddComponent<StageRoomProxy>();
                proxy.ConfigureGenerated(room.NodeGuid, room.Template, room.PositionCells, room.Role, room.Locked, room.MainRoute);
                CreateRoomVisual(roomObject.transform, room);
                CreateRoomLabel(roomObject.transform, room);
                proxies.Add(room.NodeGuid, proxy);
            }

            for (int index = 0; index < layout.Connections.Count; index++)
            {
                StageGeneratedConnection edge = layout.Connections[index];
                if (!proxies.TryGetValue(edge.SourceNodeGuid, out StageRoomProxy source) ||
                    !proxies.TryGetValue(edge.TargetNodeGuid, out StageRoomProxy target)) continue;
                var edgeObject = new GameObject($"Connection_{index:D2}_{edge.RouteKind}");
                RegisterCreated(edgeObject, registerUndo);
                edgeObject.transform.SetParent(graphRoot, false);
                StageLayoutConnectionProxy proxy = edgeObject.AddComponent<StageLayoutConnectionProxy>();
                proxy.Configure(edge.ConnectionGuid, source, edge.SourceSocketGuid, target, edge.TargetSocketGuid, ConvertRouteKind(edge.RouteKind));
                CreateConnectionLine(edgeObject, source, edge.SourceSocketGuid, target, edge.TargetSocketGuid, edge.RouteKind);
            }

            for (int roomIndex = 0; roomIndex < layout.Rooms.Count; roomIndex++)
            {
                StageGeneratedRoom room = layout.Rooms[roomIndex];
                if (!proxies.TryGetValue(room.NodeGuid, out StageRoomProxy owner)) continue;
                for (int slotIndex = 0; slotIndex < room.ElementSlots.Count; slotIndex++)
                {
                    GeneratedElementSlot slot = room.ElementSlots[slotIndex];
                    var slotObject = new GameObject($"Slot_{roomIndex:D2}_{slotIndex:D2}_{slot.Kind}");
                    RegisterCreated(slotObject, registerUndo);
                    slotObject.transform.SetParent(slotRoot, false);
                    StageElementSlotPreview preview = slotObject.AddComponent<StageElementSlotPreview>();
                    preview.Configure(owner, slot.Kind, slot.LocalCell);
                    CreateSlotVisual(slotObject.transform, slot.Kind);
                }
            }

            StageLayoutSimulationPreviewBuilder.Rebuild(
                proxies.Values.ToArray(),
                UnityEngine.Object.FindObjectsByType<StageLayoutConnectionProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                registerUndo);

            PositionCanvasLabels(layout);
            SetCanvasText("ModeLabel", "MAP-E10 · GRAPH / ROOM / SIMULATION");
            SetCanvasText("SeedLabel", $"SEED {layout.Seed} · {layout.Family} · REROLL {layout.RerollNonce}");
            SetCanvasText("ValidationSummary", $"{(layout.ErrorCount == 0 ? "VALID" : "ERROR")} · ROOMS {layout.Rooms.Count} · MAIN {(layout.HasValidMainRoute ? "OK" : "FAIL")}");
            if (registerUndo) Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SceneView.RepaintAll();
        }

        public static Dictionary<string, StageLockedRoom> CaptureLockedRooms()
        {
            var locked = new Dictionary<string, StageLockedRoom>(StringComparer.Ordinal);
            StageRoomProxy[] rooms = UnityEngine.Object.FindObjectsByType<StageRoomProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < rooms.Length; index++)
            {
                StageRoomProxy room = rooms[index];
                if (!room.Locked) continue;
                locked[room.NodeGuid] = new StageLockedRoom
                {
                    NodeGuid = room.NodeGuid,
                    Template = room.Template,
                    PositionCells = room.PositionCells,
                    Role = room.Role,
                    MainRoute = room.MainRoute,
                };
            }
            return locked;
        }

        private static void CreateRoomLabel(Transform parent, StageGeneratedRoom room)
        {
            var labelObject = new GameObject("RoomLabel");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0.2f, 0.2f, -0.1f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = $"{room.Role} · {room.Template.RoomId}\n{room.Template.SizeCells.x} × {room.Template.SizeCells.y}{(room.Locked ? " · LOCK" : string.Empty)}";
            label.fontSize = 38;
            label.characterSize = 0.14f;
            label.color = new Color(0.82f, 0.9f, 1f);
            label.anchor = TextAnchor.UpperLeft;
        }

        private static void CreateRoomVisual(Transform parent, StageGeneratedRoom room)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "RoomRect";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = new Vector3(
                room.Template.SizeCells.x * StageRoomProxy.PreviewCellScale * 0.5f,
                room.Template.SizeCells.y * StageRoomProxy.PreviewCellScale * 0.5f,
                0.04f);
            visual.transform.localScale = new Vector3(
                room.Template.SizeCells.x * StageRoomProxy.PreviewCellScale,
                room.Template.SizeCells.y * StageRoomProxy.PreviewCellScale,
                1f);
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            visual.AddComponent<StagePreviewColor>().Configure(GetRoomColor(room.Role));
        }

        private static void CreateConnectionLine(
            GameObject owner,
            StageRoomProxy source,
            string sourceSocketGuid,
            StageRoomProxy target,
            string targetSocketGuid,
            GeneratedRouteKind kind)
        {
            if (!source.TryGetSocket(sourceSocketGuid, out RoomSocketDefinition sourceSocket) ||
                !target.TryGetSocket(targetSocketGuid, out RoomSocketDefinition targetSocket)) return;
            LineRenderer line = owner.AddComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, source.GetSocketWorldPosition(sourceSocket) + Vector3.back * 0.08f);
            line.SetPosition(1, target.GetSocketWorldPosition(targetSocket) + Vector3.back * 0.08f);
            line.startWidth = kind == GeneratedRouteKind.MainRoute ? 0.12f : 0.07f;
            line.endWidth = line.startWidth;
            line.numCapVertices = 2;
            owner.AddComponent<StagePreviewColor>().Configure(GetRouteColor(kind));
        }

        private static void CreateCorridorLine(
            GameObject owner,
            StageRoomProxy source,
            string sourceSocketGuid,
            StageRoomProxy target,
            string targetSocketGuid)
        {
            if (!source.TryGetSocket(sourceSocketGuid, out RoomSocketDefinition sourceSocket) ||
                !target.TryGetSocket(targetSocketGuid, out RoomSocketDefinition targetSocket)) return;
            Vector3 start = source.GetSocketWorldPosition(sourceSocket) + Vector3.back * 0.04f;
            Vector3 end = target.GetSocketWorldPosition(targetSocket) + Vector3.back * 0.04f;
            LineRenderer line = owner.AddComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            line.useWorldSpace = true;
            line.positionCount = 3;
            line.SetPosition(0, start);
            line.SetPosition(1, new Vector3(end.x, start.y, start.z));
            line.SetPosition(2, end);
            line.startWidth = 0.04f;
            line.endWidth = 0.04f;
            owner.AddComponent<StagePreviewColor>().Configure(new Color(0.45f, 0.5f, 0.58f));
        }

        private static void CreateSlotVisual(Transform parent, GeneratedElementSlotKind kind)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "SlotMarker";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = new Vector3(0f, 0f, -0.12f);
            visual.transform.localScale = Vector3.one * 0.16f;
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visual.AddComponent<StagePreviewColor>().Configure(GetSlotColor(kind));
        }

        private static Color GetRoomColor(RoomRole role)
        {
            switch (role)
            {
                case RoomRole.Start: return new Color(0.12f, 0.48f, 0.3f);
                case RoomRole.Exit: return new Color(0.62f, 0.36f, 0.08f);
                case RoomRole.Branch: return new Color(0.34f, 0.2f, 0.58f);
                case RoomRole.Secret: return new Color(0.08f, 0.45f, 0.52f);
                case RoomRole.Rest: return new Color(0.24f, 0.42f, 0.52f);
                default: return new Color(0.12f, 0.28f, 0.52f);
            }
        }

        private static Color GetRouteColor(GeneratedRouteKind kind)
        {
            switch (kind)
            {
                case GeneratedRouteKind.MainRoute: return Color.white;
                case GeneratedRouteKind.Secret: return Color.cyan;
                case GeneratedRouteKind.Loop: return Color.gray;
                default: return new Color(0.72f, 0.45f, 1f);
            }
        }

        private static Color GetSlotColor(GeneratedElementSlotKind kind)
        {
            switch (kind)
            {
                case GeneratedElementSlotKind.Threat: return new Color(1f, 0.2f, 0.2f);
                case GeneratedElementSlotKind.Utility: return new Color(0.2f, 0.75f, 1f);
                case GeneratedElementSlotKind.Shop: return new Color(1f, 0.72f, 0.12f);
                default: return new Color(0.85f, 0.25f, 1f);
            }
        }

        private static StageConnectionVisualKind ConvertRouteKind(GeneratedRouteKind kind)
        {
            switch (kind)
            {
                case GeneratedRouteKind.MainRoute: return StageConnectionVisualKind.MainRoute;
                case GeneratedRouteKind.Secret: return StageConnectionVisualKind.Secret;
                case GeneratedRouteKind.Loop: return StageConnectionVisualKind.Loop;
                default: return StageConnectionVisualKind.Branch;
            }
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
                else UnityEngine.Object.DestroyImmediate(child);
            }
        }

        private static void RegisterCreated(GameObject created, bool registerUndo)
        {
            if (registerUndo) Undo.RegisterCreatedObjectUndo(created, "Generate Stage Layout Preview");
        }

        private static void SetCanvasText(string name, string value)
        {
            GameObject labelObject = GameObject.Find($"LayoutCanvas/{name}");
            if (labelObject != null && labelObject.TryGetComponent(out TextMesh label))
            {
                label.text = value;
                EditorUtility.SetDirty(label);
            }
        }

        private static void PositionCanvasLabels(StageGeneratedLayout layout)
        {
            if (layout.Rooms.Count == 0) return;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int index = 0; index < layout.Rooms.Count; index++)
            {
                StageGeneratedRoom room = layout.Rooms[index];
                minX = Mathf.Min(minX, room.PositionCells.x * StageRoomProxy.PreviewCellScale);
                minY = Mathf.Min(minY, room.PositionCells.y * StageRoomProxy.PreviewCellScale);
                maxX = Mathf.Max(maxX, (room.PositionCells.x + room.Template.SizeCells.x) * StageRoomProxy.PreviewCellScale);
                maxY = Mathf.Max(maxY, (room.PositionCells.y + room.Template.SizeCells.y) * StageRoomProxy.PreviewCellScale);
            }
            SetCanvasPosition("ModeLabel", new Vector3(minX, maxY + 1.8f, -0.2f));
            SetCanvasPosition("SeedLabel", new Vector3(minX, maxY + 1.15f, -0.2f));
            SetCanvasPosition("ValidationSummary", new Vector3(maxX - 4.5f, maxY + 1.8f, -0.2f));
            SetCanvasPosition("SimulationControls", new Vector3(minX, minY - 0.8f, -0.2f));
        }

        private static void SetCanvasPosition(string name, Vector3 position)
        {
            GameObject labelObject = GameObject.Find($"LayoutCanvas/{name}");
            if (labelObject != null) labelObject.transform.position = position;
        }
    }
}

#endif
