#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Stage.CameraSystem;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public sealed class StageLayoutWorkbenchWindow : EditorWindow
    {
        private static StageLayoutWorkbenchWindow instance;
        private StageLayoutMode mode;
        private int seed = 10801;
        private Vector2 scroll;
        private MapElementValidationReport report;
        private IReadOnlyList<RoomTemplate> roomTemplates;
        private StageMapProfile stageProfile;
        private StageGeneratedLayout currentLayout;
        private readonly List<StageGeneratedLayout> seedResults = new List<StageGeneratedLayout>();
        private readonly HashSet<int> bookmarkedSeeds = new HashSet<int>();
        private int selectedSeedIndex = -1;
        private int rerollNonce;
        private bool lockSelectedSeed;
        private StageSeedValidationReport lastBatchReport;
        private Vector2Int cameraRoomSize = new Vector2Int(20, 11);
        private float cameraDisplayAspect = CameraTileProfile.ReferenceAspect;

        [MenuItem("Tools/Star Night/Map E11/Stage Layout Validation Workbench", priority = 112)]
        public static void OpenWindow()
        {
            instance = GetWindow<StageLayoutWorkbenchWindow>();
            instance.titleContent = new GUIContent("Stage Layout Lab");
            instance.minSize = new Vector2(680f, 560f);
            instance.RefreshValidation();
            instance.Show();
        }

        [MenuItem("Tools/Star Night/Global Core/Stage Layout Lab", priority = 211)]
        private static void OpenGlobalCoreWindow()
        {
            OpenWindow();
        }

        public static void RefreshValidationIfOpen()
        {
            if (instance == null) return;
            instance.RefreshValidation();
            instance.Repaint();
        }

        private void OnEnable()
        {
            instance = this;
            roomTemplates = RoomTemplateSampleFactory.EnsureSamples();
            stageProfile = StageMapProfileSampleFactory.EnsureSample();
            lastBatchReport = AssetDatabase.LoadAssetAtPath<StageSeedValidationReport>(
                StageSeedBatchValidator.GetReportAssetPath(stageProfile.StageId, seed));
            RefreshValidation();
        }

        private void OnDisable()
        {
            if (instance == this) instance = null;
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawRoomLibrary();
            EditorGUILayout.Space(6f);
            DrawSeedBrowser();
            EditorGUILayout.Space(6f);
            DrawCameraLab();
            EditorGUILayout.Space(6f);
            DrawBatchValidation();
            EditorGUILayout.Space(6f);
            DrawSelectedInspector();
            EditorGUILayout.Space(6f);
            DrawModePanel();
            EditorGUILayout.Space(6f);
            DrawValidationConsole();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCameraLab()
        {
            EditorGUILayout.LabelField("GCORE-08 · Camera Contract Preview", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                cameraRoomSize = EditorGUILayout.Vector2IntField("Room Cells", cameraRoomSize);
                cameraDisplayAspect = EditorGUILayout.FloatField("Display Aspect", cameraDisplayAspect);
            }
            StageCameraLabResult preview = GlobalCoreEditorLabModels.PreviewCamera(
                cameraRoomSize,
                cameraDisplayAspect);
            EditorGUILayout.LabelField(
                $"Mode {preview.Mode} · View {preview.VisibleWidthTiles:0.###}×{preview.VisibleHeightTiles:0.###} cells");
            EditorGUILayout.LabelField(
                $"Viewport {preview.ViewportRect.x:0.###}, {preview.ViewportRect.y:0.###}, " +
                $"{preview.ViewportRect.width:0.###}, {preview.ViewportRect.height:0.###}");
            if (GUILayout.Button("Apply 11-Tile Camera Contract To Scene Camera", GUILayout.Width(285f)))
            {
                Camera sceneCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
                if (sceneCamera != null)
                {
                    Undo.RecordObject(sceneCamera, "Apply Camera Tile Contract");
                    new CameraTileProfile().ApplyTo(sceneCamera, StageRoomProxy.PreviewCellScale);
                    EditorUtility.SetDirty(sceneCamera);
                    SceneView.RepaintAll();
                }
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.LabelField("MAP-E11 · 500 Seed Batch Validation", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Stage Profile", GUILayout.Width(82f));
                stageProfile = (StageMapProfile)EditorGUILayout.ObjectField(stageProfile, typeof(StageMapProfile), false, GUILayout.Width(185f));
                StageLayoutMode nextMode = (StageLayoutMode)GUILayout.Toolbar((int)mode, new[] { "Graph", "Room", "Element Slots", "Simulation" }, EditorStyles.toolbarButton);
                if (nextMode != mode)
                {
                    mode = nextMode;
                    ApplyMode();
                }
                GUILayout.Label("Seed", GUILayout.Width(34f));
                seed = EditorGUILayout.IntField(seed, GUILayout.Width(72f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate")) GenerateCurrentSeed();
                if (GUILayout.Button("Reroll Unlocked")) RerollUnlockedRooms();
                if (GUILayout.Button("Generate 16 Seeds")) GenerateSeedBrowser();
                if (GUILayout.Button("Validate")) RefreshValidation();
                if (GUILayout.Button("Bake Snapshot")) BakeSnapshot();
                if (GUILayout.Button("Build Preview Scene")) BuildPreviewScene();
                if (GUILayout.Button("Export Report")) ExportReport();
                if (GUILayout.Button("Clear Preview")) ClearPreview();
            }
        }

        private void DrawRoomLibrary()
        {
            EditorGUILayout.LabelField("Room Library · Variable Size", EditorStyles.boldLabel);
            IReadOnlyList<RoomTemplate> templates = roomTemplates ?? RoomTemplateSampleFactory.EnsureSamples();
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int index = 0; index < templates.Count; index++)
                {
                    RoomTemplate template = templates[index];
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(105f)))
                    {
                        EditorGUILayout.LabelField(template.Role.ToString(), EditorStyles.miniBoldLabel);
                        EditorGUILayout.LabelField($"{template.SizeCells.x} × {template.SizeCells.y}");
                        if (GUILayout.Button("Add")) AddRoom(template);
                    }
                }
            }
            EditorGUILayout.HelpBox("Rooms snap to 2 cells. Select a room to lock it before Reroll Unlocked.", MessageType.Info);
        }

        private void DrawSeedBrowser()
        {
            EditorGUILayout.LabelField("16 Seed Browser · 4 × 4", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                lockSelectedSeed = EditorGUILayout.ToggleLeft("Lock selected seed", lockSelectedSeed, GUILayout.Width(140f));
                GUILayout.Label(currentLayout != null
                        ? $"Selected {currentLayout.Seed} · {currentLayout.Family} · Hash {currentLayout.ValidationHash} · Bookmarks {bookmarkedSeeds.Count}"
                        : "Generate 16 Seeds to compare previews.",
                    EditorStyles.miniLabel);
            }
            if (seedResults.Count == 0)
            {
                EditorGUILayout.HelpBox("Cards show room rects, Main/Branch routes, errors, and estimated room moves.", MessageType.Info);
                return;
            }

            for (int row = 0; row < 4; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < 4; column++)
                    {
                        int index = row * 4 + column;
                        Rect card = GUILayoutUtility.GetRect(130f, 96f, GUILayout.ExpandWidth(true));
                        if (GUI.Button(card, GUIContent.none)) LoadSeedResult(index);
                        DrawSeedThumbnail(card, seedResults[index], index == selectedSeedIndex);
                        Rect bookmarkRect = new Rect(card.xMax - 25f, card.y + 3f, 21f, 18f);
                        bool bookmarked = bookmarkedSeeds.Contains(seedResults[index].Seed);
                        bool next = GUI.Toggle(bookmarkRect, bookmarked, "★", EditorStyles.miniButton);
                        if (next != bookmarked)
                        {
                            if (next) bookmarkedSeeds.Add(seedResults[index].Seed);
                            else bookmarkedSeeds.Remove(seedResults[index].Seed);
                        }
                    }
                }
            }
        }

        private void DrawBatchValidation()
        {
            EditorGUILayout.LabelField("500 Seed Validation · Geometry / Portal / Route", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Fixed 10 + Random 490", GUILayout.Width(180f));
                if (GUILayout.Button("Approve 500 Seeds", GUILayout.Width(160f)))
                {
                    lastBatchReport = StageSeedBatchValidator.RunApproval(
                        stageProfile,
                        roomTemplates,
                        seed,
                        true);
                    Selection.activeObject = lastBatchReport;
                }
                using (new EditorGUI.DisabledScope(lastBatchReport == null))
                {
                    if (GUILayout.Button("Select Report", GUILayout.Width(100f))) Selection.activeObject = lastBatchReport;
                }
            }

            if (lastBatchReport == null)
            {
                EditorGUILayout.HelpBox("Runs 10 fixed regression seeds and 490 reproducible random seeds. Success stores no detail; failures store at most 20 compact records.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(lastBatchReport.CreateSummary(), lastBatchReport.IsValid ? MessageType.Info : MessageType.Error);
            EditorGUILayout.LabelField("Families", string.Join(" · ", lastBatchReport.FamilyCounts.Select(item => $"{item.Family} {item.Count}")));
            EditorGUILayout.LabelField("Reports", $"{lastBatchReport.JsonReportPath} · {lastBatchReport.CsvReportPath}", EditorStyles.miniLabel);
            if (lastBatchReport.Failures.Count > 0)
            {
                StageSeedFailureReport first = lastBatchReport.Failures[0];
                EditorGUILayout.LabelField("First Failure", $"Seed {first.Seed} · {first.RoomNodeStableId} · {first.FailureCode} · {first.FirstFailedCell}", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Reproduce First Failure Seed"))
                {
                    seed = first.Seed;
                    GenerateCurrentSeed();
                }
            }
        }

        private void DrawSelectedInspector()
        {
            EditorGUILayout.LabelField("Selected Inspector", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Selection", Selection.activeObject, typeof(UnityEngine.Object), true);
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject == null) return;
            StageRoomProxy room = selectedObject.GetComponentInParent<StageRoomProxy>();
            StageLayoutConnectionProxy connection = selectedObject.GetComponent<StageLayoutConnectionProxy>();
            if (room != null)
            {
                EditorGUILayout.LabelField("Node Guid", room.NodeGuid);
                EditorGUILayout.LabelField("Cell Rect", $"{room.PositionCells} / {room.SizeCells}");
                EditorGUILayout.LabelField("Role", room.Role.ToString());
                EditorGUILayout.LabelField("Camera", room.Template != null ? room.Template.CameraMode.ToString() : "Missing Template");
                EditorGUILayout.LabelField("Locked", room.Locked.ToString());
                if (GUILayout.Button(room.Locked ? "Unlock Room" : "Lock Room"))
                {
                    Undo.RecordObject(room, room.Locked ? "Unlock Room" : "Lock Room");
                    room.SetLocked(!room.Locked);
                    EditorUtility.SetDirty(room);
                    Repaint();
                }
                if (GUILayout.Button("Full Room Preview"))
                {
                    mode = StageLayoutMode.Room;
                    GetSimulationController()?.ShowRoomPreview(room);
                    Repaint();
                }
                using (new EditorGUI.DisabledScope(room.Template == null || room.Template.RoomPrefab == null))
                {
                    if (GUILayout.Button("Open Room Prefab Mode")) AssetDatabase.OpenAsset(room.Template.RoomPrefab);
                }
            }
            else if (connection != null)
            {
                EditorGUILayout.LabelField("Connection", connection.ConnectionGuid);
                EditorGUILayout.LabelField("Compatibility", connection.GetCompatibility().ToString());
                EditorGUILayout.LabelField("Route", connection.VisualKind.ToString());
            }
        }

        private void DrawModePanel()
        {
            EditorGUILayout.LabelField($"{mode} Mode", EditorStyles.boldLabel);
            StageLayoutSimulationController controller = GetSimulationController();
            if (controller == null)
            {
                EditorGUILayout.HelpBox("Generate a layout to build GhostPlayer, Full Room Preview, and Maru Lane.", MessageType.Info);
                return;
            }

            if (mode == StageLayoutMode.Simulation)
            {
                EditorGUILayout.LabelField("Current Room", controller.CurrentRoom != null ? controller.CurrentRoom.NodeGuid : "Not started");
                EditorGUILayout.LabelField("Camera Transition", $"{controller.TransitionSeconds:0.00}s");
                EditorGUILayout.LabelField("Virtual Phase", $"{controller.Phase} · {controller.VirtualElapsedSeconds:0.0}s");
                EditorGUILayout.LabelField("Exit Arrival", controller.ExitArrivalSeconds >= 0f ? $"{controller.ExitArrivalSeconds:0.0}s" : "Pending");
                EditorGUILayout.LabelField("Render Rule", $"Full Room {controller.VisibleFullRoomCount} · adjacent proxies only");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Restart at Start")) controller.BeginSimulation(false);
                    if (GUILayout.Button("Next Room (0.28s)")) controller.MoveNextRoom(false);
                    if (GUILayout.Button("Complete Transition")) controller.CompleteTransitionImmediate();
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Bell1")) controller.SetVirtualPhase(StageLayoutSimulationPhase.Bell1);
                    if (GUILayout.Button("Bell2")) controller.SetVirtualPhase(StageLayoutSimulationPhase.Bell2);
                    if (GUILayout.Button("MaruChase")) controller.SetVirtualPhase(StageLayoutSimulationPhase.MaruChase);
                }
                EditorGUILayout.HelpBox("The generated Stage Preview Scene auto-runs the GhostPlayer in Play Mode. This panel provides deterministic manual stepping in Edit Mode.", MessageType.Info);
            }
            else if (mode == StageLayoutMode.Room)
            {
                StageRoomProxy selected = Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponentInParent<StageRoomProxy>()
                    : null;
                EditorGUILayout.HelpBox("Only the selected room's Full Room Prefab is rendered. CameraBounds, Portal, SafeCell, and VoidRecovery overlays remain visible in fallback rooms.", MessageType.Info);
                if (GUILayout.Button("Focus Selected Room")) controller.ShowRoomPreview(selected);
            }
            else
            {
                EditorGUILayout.HelpBox("Graph and Element Slots modes keep all room proxies visible for layout authoring.", MessageType.None);
            }
        }

        private void DrawValidationConsole()
        {
            EditorGUILayout.LabelField("Validation Console · Error / Warning / Info", EditorStyles.boldLabel);
            if (report == null) RefreshValidation();
            EditorGUILayout.HelpBox(report.CreateSummary(), report.IsValid ? MessageType.Info : MessageType.Error);
            for (int index = 0; index < report.Issues.Count; index++)
            {
                ValidationIssue issue = report.Issues[index];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"[{issue.Severity}] {issue.Code}\n{issue.Message}", EditorStyles.wordWrappedLabel);
                    if (issue.Context != null && GUILayout.Button("Focus", GUILayout.Width(54f)))
                    {
                        Selection.activeObject = issue.Context;
                        EditorGUIUtility.PingObject(issue.Context);
                        if (issue.Context is Component && SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
                    }
                    if (issue.AutoFixable && GUILayout.Button("Fix", GUILayout.Width(44f)))
                    {
                        StageLayoutValidator.SnapAllRooms();
                        RefreshValidation();
                    }
                }
            }
        }

        private void GenerateCurrentSeed()
        {
            if (stageProfile == null) return;
            rerollNonce = 0;
            currentLayout = StageMapGenerator.Generate(stageProfile, roomTemplates, seed);
            StageLayoutPreviewApplier.Apply(currentLayout);
            ApplyMode();
            selectedSeedIndex = -1;
            RefreshValidation();
        }

        private void GenerateSeedBrowser()
        {
            if (stageProfile == null) return;
            seedResults.Clear();
            int firstSeed = lockSelectedSeed && currentLayout != null ? currentLayout.Seed : seed;
            for (int index = 0; index < 16; index++)
                seedResults.Add(StageMapGenerator.Generate(stageProfile, roomTemplates, firstSeed + index));
            selectedSeedIndex = 0;
            LoadSeedResult(0);
        }

        private void LoadSeedResult(int index)
        {
            if (index < 0 || index >= seedResults.Count) return;
            selectedSeedIndex = index;
            currentLayout = seedResults[index];
            seed = currentLayout.Seed;
            rerollNonce = 0;
            StageLayoutPreviewApplier.Apply(currentLayout);
            ApplyMode();
            RefreshValidation();
        }

        private void RerollUnlockedRooms()
        {
            if (stageProfile == null) return;
            rerollNonce++;
            Dictionary<string, StageLockedRoom> lockedRooms = StageLayoutPreviewApplier.CaptureLockedRooms();
            currentLayout = StageMapGenerator.Generate(stageProfile, roomTemplates, seed, rerollNonce, lockedRooms);
            StageLayoutPreviewApplier.Apply(currentLayout);
            ApplyMode();
            RefreshValidation();
        }

        private void AddRoom(RoomTemplate template)
        {
            GameObject root = GameObject.Find("RoomProxyRoot");
            var roomObject = new GameObject($"Room_{template.RoomId}_{Guid.NewGuid().ToString("N").Substring(0, 4)}");
            Undo.RegisterCreatedObjectUndo(roomObject, "Add Room Proxy");
            roomObject.transform.SetParent(root != null ? root.transform : null, false);
            StageRoomProxy proxy = roomObject.AddComponent<StageRoomProxy>();
            int count = UnityEngine.Object.FindObjectsByType<StageRoomProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            proxy.Configure(Guid.NewGuid().ToString("N"), template, new Vector2Int(count * 4, -12), false, false);
            Selection.activeGameObject = roomObject;
            RefreshValidation();
        }

        private void BakeSnapshot()
        {
            RefreshValidation();
            StageLayoutSnapshot snapshot = StageLayoutSnapshotBaker.BakeCurrentScene(
                stageProfile,
                seed,
                currentLayout != null ? currentLayout.ValidationHash : null);
            Selection.activeObject = snapshot;
            Debug.Log($"[MAP-E10] Snapshot baked: {AssetDatabase.GetAssetPath(snapshot)} · {snapshot.Rooms.Count} rooms · {snapshot.Connections.Count} connections.");
        }

        private void BuildPreviewScene()
        {
            RefreshValidation();
            if (!report.IsValid)
            {
                Debug.LogError("[MAP-E10] Fix layout validation errors before building a Stage Preview Scene.");
                return;
            }
            StageLayoutSnapshot snapshot = StageLayoutSnapshotBaker.BakeCurrentScene(
                stageProfile,
                seed,
                currentLayout != null ? currentLayout.ValidationHash : null);
            string path = StagePreviewSceneBuilder.BuildCurrentScene(stageProfile, seed, snapshot);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            Debug.Log($"[MAP-E10] Playable Stage Preview Scene built: {path}");
        }

        private void ExportReport()
        {
            RefreshValidation();
            Debug.Log($"[MAP-E09] {report.CreateSummary()} | Seed {seed} | Family {currentLayout?.Family} | Hash {currentLayout?.ValidationHash}");
        }

        private void ClearPreview()
        {
            ClearRoot("RoomProxyRoot");
            ClearRoot("GraphLineRoot");
            ClearRoot("CorridorProxyRoot");
            ClearRoot("ElementSlotPreviewRoot");
            ClearRoot("FullRoomPreviewRoot");
            ClearRoot("MaruPathPreviewRoot");
            GetSimulationController()?.ShowGraphMode();
            StageLayoutAuthoringSession.ClearPendingSocket();
            currentLayout = null;
            RefreshValidation();
        }

        private static void DrawSeedThumbnail(Rect card, StageGeneratedLayout layout, bool selected)
        {
            EditorGUI.DrawRect(new Rect(card.x + 2f, card.y + 2f, card.width - 4f, card.height - 4f),
                selected ? new Color(0.16f, 0.31f, 0.48f) : new Color(0.08f, 0.1f, 0.14f));
            GUI.Label(new Rect(card.x + 6f, card.y + 3f, card.width - 30f, 18f), $"{layout.Seed} · {layout.Family}", EditorStyles.miniBoldLabel);
            if (layout.Rooms.Count == 0) return;
            float minX = layout.Rooms.Min(room => room.PositionCells.x);
            float minY = layout.Rooms.Min(room => room.PositionCells.y);
            float maxX = layout.Rooms.Max(room => room.PositionCells.x + room.Template.SizeCells.x);
            float maxY = layout.Rooms.Max(room => room.PositionCells.y + room.Template.SizeCells.y);
            Rect graphRect = new Rect(card.x + 6f, card.y + 22f, card.width - 12f, 50f);
            float scale = Mathf.Min(graphRect.width / Mathf.Max(1f, maxX - minX), graphRect.height / Mathf.Max(1f, maxY - minY));
            var centers = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            for (int index = 0; index < layout.Rooms.Count; index++)
            {
                StageGeneratedRoom room = layout.Rooms[index];
                Rect roomRect = new Rect(
                    graphRect.x + (room.PositionCells.x - minX) * scale,
                    graphRect.yMax - (room.PositionCells.y - minY + room.Template.SizeCells.y) * scale,
                    Mathf.Max(2f, room.Template.SizeCells.x * scale),
                    Mathf.Max(2f, room.Template.SizeCells.y * scale));
                Color color = room.Role == RoomRole.Start ? new Color(0.25f, 0.8f, 0.5f) :
                    room.Role == RoomRole.Exit ? new Color(0.95f, 0.65f, 0.2f) :
                    room.Role == RoomRole.Secret ? new Color(0.2f, 0.85f, 0.95f) :
                    room.MainRoute ? new Color(0.36f, 0.55f, 0.86f) : new Color(0.62f, 0.38f, 0.86f);
                EditorGUI.DrawRect(roomRect, color);
                centers[room.NodeGuid] = roomRect.center;
            }
            Handles.BeginGUI();
            for (int index = 0; index < layout.Connections.Count; index++)
            {
                StageGeneratedConnection edge = layout.Connections[index];
                if (!centers.TryGetValue(edge.SourceNodeGuid, out Vector2 start) || !centers.TryGetValue(edge.TargetNodeGuid, out Vector2 end)) continue;
                Handles.color = edge.RouteKind == GeneratedRouteKind.MainRoute ? Color.white :
                    edge.RouteKind == GeneratedRouteKind.Secret ? Color.cyan : new Color(0.72f, 0.45f, 1f);
                Handles.DrawLine(start, end);
            }
            Handles.EndGUI();
            GUI.Label(new Rect(card.x + 6f, card.yMax - 19f, card.width - 12f, 16f),
                $"E {layout.ErrorCount} · Move {layout.EstimatedRoomMoves} · {(layout.HasValidMainRoute ? "MAIN ✓" : "MAIN ✕")}",
                EditorStyles.miniLabel);
        }

        private static void ClearRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root == null) return;
            while (root.transform.childCount > 0) Undo.DestroyObjectImmediate(root.transform.GetChild(0).gameObject);
        }

        private void RefreshValidation()
        {
            report = StageLayoutValidator.ValidateCurrentScene();
        }

        private void ApplyMode()
        {
            StageLayoutSimulationController controller = GetSimulationController();
            if (controller == null) return;
            StageRoomProxy selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<StageRoomProxy>()
                : null;
            switch (mode)
            {
                case StageLayoutMode.Room:
                    controller.ShowRoomPreview(selected);
                    break;
                case StageLayoutMode.Simulation:
                    controller.BeginSimulation(false);
                    break;
                default:
                    controller.ShowGraphMode();
                    break;
            }
            SceneView.RepaintAll();
        }

        private static StageLayoutSimulationController GetSimulationController()
        {
            return UnityEngine.Object.FindFirstObjectByType<StageLayoutSimulationController>(FindObjectsInactive.Include);
        }
    }
}

#endif
