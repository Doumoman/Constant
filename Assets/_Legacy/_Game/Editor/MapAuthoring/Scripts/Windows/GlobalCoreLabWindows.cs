#if LEGACY_DISABLED
using System.Linq;
using StarNight.Map;
using StarNight.Stage.CameraSystem;
using StarNight.Stage.Layout;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public sealed class RoomTileLabWindow : EditorWindow
    {
        private int seed = 10801;
        private Vector2Int chunkGridSize = new Vector2Int(4, 3);
        private RoomInteriorLayout layout;
        private Vector2 scroll;

        [MenuItem("Tools/Star Night/Global Core/Room Tile Lab", priority = 210)]
        public static void OpenWindow()
        {
            var window = GetWindow<RoomTileLabWindow>();
            window.titleContent = new GUIContent("Room Tile Lab");
            window.minSize = new Vector2(620f, 560f);
            window.Generate();
            window.Show();
        }

        private void OnEnable()
        {
            if (layout == null) Generate();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("GCORE-08 · RoomTileLab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("8×8 청크와 1×1 셀을 동일 Seed로 즉시 재생성합니다.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                seed = EditorGUILayout.IntField("Seed", seed);
                chunkGridSize = EditorGUILayout.Vector2IntField("Chunk Grid", chunkGridSize);
                if (GUILayout.Button("Generate", GUILayout.Width(100f))) Generate();
            }

            if (layout == null) return;
            EditorGUILayout.LabelField(
                $"{layout.RoomId} · {layout.SizeCells.x}×{layout.SizeCells.y} cells · Hash {layout.ValidationHash}");
            EditorGUILayout.LabelField(
                $"T0 {(layout.HasT0MainRoute ? "PASS" : "FAIL")} · Hidden {layout.HiddenContents.Count} · ToolEscape {layout.ToolEscapes.Count}");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawGrid(layout);
            EditorGUILayout.Space(6f);
            foreach (GeneratedHiddenContent hidden in layout.HiddenContents)
            {
                EditorGUILayout.LabelField(
                    $"Hidden · {hidden.StableId} · {hidden.Hint} · {hidden.RevealTools}",
                    EditorStyles.miniLabel);
            }
            foreach (GeneratedToolEscape escape in layout.ToolEscapes)
            {
                EditorGUILayout.LabelField(
                    $"ToolEscape · {escape.PatternId} · {escape.RequiredTool} · {escape.ChunkGridCell}",
                    EditorStyles.miniLabel);
            }
            foreach (string error in layout.ValidationErrors.Take(20))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            chunkGridSize.x = Mathf.Clamp(chunkGridSize.x, 2, 16);
            chunkGridSize.y = Mathf.Clamp(chunkGridSize.y, 1, 16);
            layout = GlobalCoreEditorLabModels.GenerateRoomTiles(seed, chunkGridSize);
            Repaint();
        }

        private static void DrawGrid(RoomInteriorLayout room)
        {
            const float availableHeight = 380f;
            float cellSize = Mathf.Clamp(
                Mathf.Min((EditorGUIUtility.currentViewWidth - 45f) / room.SizeCells.x, availableHeight / room.SizeCells.y),
                3f,
                18f);
            Rect grid = GUILayoutUtility.GetRect(room.SizeCells.x * cellSize, room.SizeCells.y * cellSize);
            EditorGUI.DrawRect(grid, new Color(0.06f, 0.07f, 0.10f));
            for (int y = 0; y < room.SizeCells.y; y++)
            {
                for (int x = 0; x < room.SizeCells.x; x++)
                {
                    var worldCell = new Vector2Int(x, y);
                    Rect cell = new Rect(
                        grid.x + x * cellSize,
                        grid.y + (room.SizeCells.y - y - 1) * cellSize,
                        Mathf.Max(1f, cellSize - 0.5f),
                        Mathf.Max(1f, cellSize - 0.5f));
                    EditorGUI.DrawRect(cell, CellColor(room.GetWorldCell(worldCell)));
                }
            }
        }

        private static Color CellColor(MicroCellKind kind)
        {
            return kind switch
            {
                MicroCellKind.Empty => new Color(0.16f, 0.18f, 0.24f),
                MicroCellKind.Hazard => new Color(0.85f, 0.22f, 0.18f),
                MicroCellKind.Interaction => new Color(0.25f, 0.72f, 0.90f),
                MicroCellKind.Puzzle => new Color(0.65f, 0.38f, 0.92f),
                MicroCellKind.Reward => new Color(1f, 0.78f, 0.18f),
                MicroCellKind.SoftSoil => new Color(0.55f, 0.36f, 0.20f),
                _ => new Color(0.42f, 0.44f, 0.50f),
            };
        }
    }

    public sealed class SecretDimensionLabWindow : EditorWindow
    {
        private int stageSeed = 10801;
        private string sourceRoomId = "COMMON_TEST_ROOM";
        private string anchorId = "SECRET_ANCHOR_01";
        private Vector2Int returnSafeCell = new Vector2Int(2, 1);
        private ToolTag revealTool = ToolTag.Pickaxe;
        private SecretDimensionLabResult result;

        [MenuItem("Tools/Star Night/Global Core/Secret Dimension Lab", priority = 212)]
        public static void OpenWindow()
        {
            var window = GetWindow<SecretDimensionLabWindow>();
            window.titleContent = new GUIContent("Secret Dimension Lab");
            window.minSize = new Vector2(520f, 360f);
            window.RefreshPreview();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshPreview();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("GCORE-08 · SecretDimensionLab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Anchor ID, 발견 도구, 원래 방 복귀 셀을 코드 수정 없이 시험합니다.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            stageSeed = EditorGUILayout.IntField("Stage Seed", stageSeed);
            sourceRoomId = EditorGUILayout.TextField("Source Room Stable ID", sourceRoomId);
            anchorId = EditorGUILayout.TextField("Anchor Stable ID", anchorId);
            returnSafeCell = EditorGUILayout.Vector2IntField("Return Safe Cell", returnSafeCell);
            revealTool = (ToolTag)EditorGUILayout.EnumFlagsField("Reveal Tool", revealTool);
            if (EditorGUI.EndChangeCheck()) RefreshPreview();
            if (GUILayout.Button("Refresh Anchor Preview")) RefreshPreview();
            if (result == null) return;
            EditorGUILayout.Space(8f);
            if (!result.IsValid)
            {
                EditorGUILayout.HelpBox(result.Failure, MessageType.Error);
                return;
            }
            EditorGUILayout.LabelField("Secret Seed", result.SecretSeed.ToString());
            EditorGUILayout.LabelField("Main Portal", result.MainPortalId);
            EditorGUILayout.LabelField("Return Portal", result.ReturnPortalId);
            EditorGUILayout.LabelField("Return Safe Cell", result.ReturnSafeCell.ToString());
            EditorGUILayout.LabelField("Reveal", result.RevealTool.ToString());
        }

        private void RefreshPreview()
        {
            result = GlobalCoreEditorLabModels.PreviewSecret(
                stageSeed,
                sourceRoomId,
                anchorId,
                returnSafeCell,
                revealTool);
            Repaint();
        }
    }

    public sealed class InventoryInteractionLabWindow : EditorWindow
    {
        private InventoryInteractionLabState state;

        [MenuItem("Tools/Star Night/Global Core/Inventory Interaction Lab", priority = 213)]
        public static void OpenWindow()
        {
            var window = GetWindow<InventoryInteractionLabWindow>();
            window.titleContent = new GUIContent("Inventory Interaction Lab");
            window.minSize = new Vector2(500f, 330f);
            window.ResetState();
            window.Show();
        }

        private void OnEnable()
        {
            state ??= GlobalCoreEditorLabModels.CreateInventoryState(1001, 3, 10);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("GCORE-08 · InventoryInteractionLab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "내구도 장비는 ItemId마다 하나만 보유하며 Quantity를 사용하지 않습니다.",
                MessageType.Info);
            state.ItemId = Mathf.Max(1, EditorGUILayout.IntField("Item ID", state.ItemId));
            state.MaxDurability = Mathf.Max(0, EditorGUILayout.IntField("Max Durability", state.MaxDurability));
            state.CurrentDurability = Mathf.Clamp(
                EditorGUILayout.IntField("Current Durability", state.CurrentDurability),
                0,
                state.MaxDurability);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Acquire Duplicate"))
                    state = GlobalCoreEditorLabModels.ApplyDuplicate(state);
                if (GUILayout.Button("Set Durability 0"))
                    state = GlobalCoreEditorLabModels.DepleteWithoutAutoSwap(state);
                if (GUILayout.Button("Reset")) ResetState();
            }
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Durability", $"{state.CurrentDurability} / {state.MaxDurability}");
            EditorGUILayout.LabelField("Duplicate Repaired", state.LastDuplicateRepaired.ToString());
            EditorGUILayout.LabelField("Feedback", state.LastFeedbackMessage ?? string.Empty);
            EditorGUILayout.LabelField("Runtime Copy Replaced", state.RuntimeCopyReplaced.ToString());
        }

        private void ResetState()
        {
            state = GlobalCoreEditorLabModels.CreateInventoryState(1001, 3, 10);
            Repaint();
        }
    }
}

#endif
