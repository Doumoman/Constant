#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarNight.Interaction.Carry;
using StarNight.Map;
using StarNight.Tools.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.ToolAuthoring
{
    public enum ToolWorkbenchTab
    {
        ToolDefinition,
        CarryObject,
        ReactionMatrix,
        ActionTimeline,
        ThrowPreview,
        BatchValidation,
    }

    public sealed class ToolInteractionWorkbenchWindow : EditorWindow
    {
        private static readonly string[] TabLabels =
        {
            "Tool Definition", "Carry Object", "Reaction Matrix",
            "Action Timeline", "Throw Preview", "Batch Validation",
        };

        private ToolWorkbenchTab currentTab;
        private readonly List<HandToolDefinition> tools = new List<HandToolDefinition>();
        private readonly List<CarryObjectDefinition> carryObjects = new List<CarryObjectDefinition>();
        private readonly List<MapElementDefinition> elements = new List<MapElementDefinition>();
        private readonly List<ToolValidationIssue> issues = new List<ToolValidationIssue>();
        private int selectedToolIndex;
        private int selectedCarryIndex;
        private Vector2 assetListScroll;
        private Vector2 inspectorScroll;
        private Vector2 matrixScroll;
        private Vector2 validationScroll;
        private bool undefinedOnly;

        public static IReadOnlyList<string> RequiredTabLabels => TabLabels;

        [MenuItem("Tools/별을 물어오는 밤/Tool Interaction Lab 열기")]
        public static void Open()
        {
            var window = GetWindow<ToolInteractionWorkbenchWindow>();
            window.titleContent = new GUIContent("Tool Interaction Lab");
            window.minSize = new Vector2(820f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAssets();
            SceneView.duringSceneGui += DrawSceneHandles;
        }

        private void OnDisable() => SceneView.duringSceneGui -= DrawSceneHandles;

        private void OnGUI()
        {
            DrawHeader();
            currentTab = (ToolWorkbenchTab)GUILayout.Toolbar((int)currentTab, TabLabels, GUILayout.Height(25f));
            EditorGUILayout.Space(5f);
            switch (currentTab)
            {
                case ToolWorkbenchTab.ToolDefinition: DrawToolDefinition(); break;
                case ToolWorkbenchTab.CarryObject: DrawCarryObject(); break;
                case ToolWorkbenchTab.ReactionMatrix: DrawReactionMatrix(); break;
                case ToolWorkbenchTab.ActionTimeline: DrawActionTimeline(); break;
                case ToolWorkbenchTab.ThrowPreview: DrawThrowPreview(); break;
                case ToolWorkbenchTab.BatchValidation: DrawBatchValidation(); break;
            }
            DrawPlayTestControls();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("TOOL-05 · Tool Interaction Workbench", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) RefreshAssets();
            if (GUILayout.Button("Rebuild Lab", EditorStyles.toolbarButton))
            {
                ToolInteractionLabBuilder.Rebuild();
                RefreshAssets();
            }
            if (GUILayout.Button("Open Scene", EditorStyles.toolbarButton)) ToolInteractionLabBuilder.Open();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolDefinition()
        {
            DrawSplitAssetEditor(
                tools.Cast<UnityEngine.Object>().ToList(),
                ref selectedToolIndex,
                ref assetListScroll,
                "Hand Tool Definitions");
        }

        private void DrawCarryObject()
        {
            DrawSplitAssetEditor(
                carryObjects.Cast<UnityEngine.Object>().ToList(),
                ref selectedCarryIndex,
                ref assetListScroll,
                "Carry Object Definitions");
        }

        private void DrawSplitAssetEditor(
            List<UnityEngine.Object> assets,
            ref int selectedIndex,
            ref Vector2 listScroll,
            string title)
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, assets.Count - 1));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(220f));
            GUILayout.Label(title, EditorStyles.boldLabel);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            for (int index = 0; index < assets.Count; index++)
            {
                if (GUILayout.Toggle(index == selectedIndex, assets[index].name, "Button"))
                {
                    selectedIndex = index;
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (assets.Count > 0)
            {
                UnityEngine.Object selected = assets[selectedIndex];
                inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
                var serialized = new SerializedObject(selected);
                serialized.Update();
                SerializedProperty iterator = serialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    using (new EditorGUI.DisabledScope(iterator.propertyPath == "m_Script"))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
                if (serialized.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(selected);
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No assets found. Rebuild Lab to create approved defaults.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawReactionMatrix()
        {
            if (tools.Count == 0)
            {
                EditorGUILayout.HelpBox("No Hand Tool Definitions.", MessageType.Warning);
                return;
            }
            selectedToolIndex = Mathf.Clamp(selectedToolIndex, 0, tools.Count - 1);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(200f));
            GUILayout.Label("Tool Definitions", EditorStyles.boldLabel);
            for (int index = 0; index < tools.Count; index++)
            {
                if (GUILayout.Toggle(index == selectedToolIndex, tools[index].ToolId, "Button"))
                {
                    selectedToolIndex = index;
                }
            }
            undefinedOnly = EditorGUILayout.ToggleLeft("미정의 반응만 보기", undefinedOnly);
            EditorGUILayout.EndVertical();

            HandToolDefinition tool = tools[selectedToolIndex];
            ToolTag atomicTag = ToolReactionMatrix.FirstAtomicTag(tool.ToolTags);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Map Element Definitions · {atomicTag}", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Element", GUILayout.Width(220f));
            GUILayout.Label("Reaction", GUILayout.Width(120f));
            GUILayout.Label("Expected Result");
            GUILayout.Label("Validation", GUILayout.Width(190f));
            EditorGUILayout.EndHorizontal();
            matrixScroll = EditorGUILayout.BeginScrollView(matrixScroll);
            for (int index = 0; index < elements.Count; index++)
            {
                DrawMatrixRow(elements[index], atomicTag);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMatrixRow(MapElementDefinition element, ToolTag toolTag)
        {
            ToolReactionEntry entry = element.ToolReactions?.Entries?
                .FirstOrDefault(candidate => candidate != null && candidate.Tool == toolTag);
            bool defined = entry != null && entry.Reaction != ElementReactionType.None;
            if (undefinedOnly && defined) return;

            bool unbreakable = element.CommonProfile?.Kind == CommonElementKind.UnbreakableBlock;
            string validation = unbreakable && defined
                ? "ERROR · Unbreakable reaction"
                : defined && ToolReactionReceiver.ResolveFeedback(entry) == FeedbackId.None
                    ? "ERROR · Feedback 없음"
                    : defined ? "OK" : "Undefined";
            Color previous = GUI.color;
            if (validation.StartsWith("ERROR", StringComparison.Ordinal)) GUI.color = new Color(1f, 0.65f, 0.65f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(element, typeof(MapElementDefinition), false, GUILayout.Width(220f));
            ElementReactionType current = defined ? entry.Reaction : ElementReactionType.None;
            EditorGUI.BeginChangeCheck();
            ElementReactionType next = (ElementReactionType)EditorGUILayout.EnumPopup(current, GUILayout.Width(120f));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(element, "Edit Tool Reaction Matrix");
                element.ToolReactions ??= new ToolReactionTable();
                element.ToolReactions.Entries ??= new List<ToolReactionEntry>();
                if (entry == null && next != ElementReactionType.None)
                {
                    entry = new ToolReactionEntry { Tool = toolTag, Reaction = next, StrengthRequired = 1 };
                    element.ToolReactions.Entries.Add(entry);
                }
                else if (entry != null)
                {
                    if (next == ElementReactionType.None) element.ToolReactions.Entries.Remove(entry);
                    else entry.Reaction = next;
                }
                EditorUtility.SetDirty(element);
            }

            string resultState = entry != null ? entry.ResultState ?? string.Empty : string.Empty;
            EditorGUI.BeginChangeCheck();
            string nextResult = EditorGUILayout.TextField(resultState);
            if (EditorGUI.EndChangeCheck() && entry != null)
            {
                Undo.RecordObject(element, "Edit Reaction Expected Result");
                entry.ResultState = nextResult;
                EditorUtility.SetDirty(element);
            }
            GUILayout.Label(validation, GUILayout.Width(190f));
            EditorGUILayout.EndHorizontal();
            GUI.color = previous;
        }

        private void DrawActionTimeline()
        {
            if (tools.Count == 0) return;
            selectedToolIndex = Mathf.Clamp(selectedToolIndex, 0, tools.Count - 1);
            selectedToolIndex = EditorGUILayout.Popup("Tool", selectedToolIndex, tools.Select(tool => tool.ToolId).ToArray());
            DrawTimeline("Ground Action", tools[selectedToolIndex].GroundAction);
            DrawTimeline("Air Action", tools[selectedToolIndex].AirAction);
        }

        private static void DrawTimeline(string label, ToolActionProfile profile)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(100f, 34f, GUILayout.ExpandWidth(true));
            float total = Mathf.Max(0.01f, profile.TotalSeconds);
            float windupWidth = rect.width * profile.WindupSeconds / total;
            float activeWidth = rect.width * profile.ActiveSeconds / total;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, windupWidth, rect.height), new Color(0.95f, 0.65f, 0.18f));
            EditorGUI.DrawRect(new Rect(rect.x + windupWidth, rect.y, activeWidth, rect.height), new Color(1f, 0.3f, 0.2f));
            EditorGUI.DrawRect(new Rect(rect.x + windupWidth + activeWidth, rect.y,
                Mathf.Max(0f, rect.width - windupWidth - activeWidth), rect.height), new Color(0.25f, 0.65f, 0.95f));
            GUI.Label(rect, $"Windup {profile.WindupSeconds:0.00} · Impact {profile.ImpactSeconds:0.00} · Recovery {profile.RecoverySeconds:0.00} · Total {total:0.00}", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawThrowPreview()
        {
            if (carryObjects.Count == 0) return;
            selectedCarryIndex = Mathf.Clamp(selectedCarryIndex, 0, carryObjects.Count - 1);
            selectedCarryIndex = EditorGUILayout.Popup("Carry Object", selectedCarryIndex,
                carryObjects.Select(carry => carry.ObjectId).ToArray());
            CarryObjectDefinition carry = carryObjects[selectedCarryIndex];
            EditorGUILayout.Vector2Field("Horizontal Velocity", carry.ThrowProfile.HorizontalVelocity);
            EditorGUILayout.Vector2Field("Up Velocity", carry.ThrowProfile.UpVelocity);
            EditorGUILayout.FloatField("Maximum Speed", carry.ThrowProfile.MaximumSpeed);
            EditorGUILayout.HelpBox("Scene View displays the carry parabola, bomb arc, rope height, spray/hook range and umbrella canopy.", MessageType.Info);
            SceneView.RepaintAll();
        }

        private void DrawBatchValidation()
        {
            if (GUILayout.Button("Run Batch Validation", GUILayout.Height(28f)))
            {
                RunValidation();
            }
            if (GUILayout.Button("Save Markdown Report"))
            {
                if (issues.Count == 0) RunValidation();
                string folder = "Assets/_Game/Editor/ToolAuthoring/Reports";
                Directory.CreateDirectory(folder);
                string path = $"{folder}/ToolInteractionReport_{DateTime.Now:yyyyMMdd_HHmm}.md";
                File.WriteAllText(path, ToolInteractionValidation.BuildMarkdown(issues));
                AssetDatabase.ImportAsset(path);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            }
            validationScroll = EditorGUILayout.BeginScrollView(validationScroll);
            for (int index = 0; index < issues.Count; index++)
            {
                ToolValidationIssue issue = issues[index];
                MessageType type = issue.Severity == ToolValidationSeverity.Error
                    ? MessageType.Error
                    : issue.Severity == ToolValidationSeverity.Warning ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox($"{issue.AssetName}: {issue.Message}", type);
            }
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No validation issues in the current cached data.", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPlayTestControls()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Enter Test Play", EditorStyles.toolbarButton))
            {
                ToolInteractionLabBuilder.Open();
                EditorApplication.isPlaying = true;
            }
            if (GUILayout.Button("Reset Current Zone", EditorStyles.toolbarButton)) ToolInteractionLabBuilder.Rebuild();
            if (GUILayout.Button("Give All Tools", EditorStyles.toolbarButton)) FindLabController()?.GiveAllTools();
            if (GUILayout.Button("Set Bomb/Rope 99", EditorStyles.toolbarButton)) FindLabController()?.SetBombRope99();
            if (GUILayout.Button("Show Cell Overlay", EditorStyles.toolbarButton)) FindLabController()?.ToggleCellOverlay();
            if (GUILayout.Button("Show Impact Score", EditorStyles.toolbarButton)) FindLabController()?.ToggleImpactScore();
            if (GUILayout.Button("Show Interaction Priority", EditorStyles.toolbarButton)) FindLabController()?.ToggleInteractionPriority();
            if (GUILayout.Button("Show Placement Candidates", EditorStyles.toolbarButton)) FindLabController()?.TogglePlacementCandidates();
            EditorGUILayout.EndHorizontal();
        }

        private void RunValidation()
        {
            issues.Clear();
            issues.AddRange(ToolInteractionValidation.Validate(tools, carryObjects, elements));
        }

        private void RefreshAssets()
        {
            LoadAssets(tools);
            LoadAssets(carryObjects);
            LoadAssets(elements);
            selectedToolIndex = Mathf.Clamp(selectedToolIndex, 0, Mathf.Max(0, tools.Count - 1));
            selectedCarryIndex = Mathf.Clamp(selectedCarryIndex, 0, Mathf.Max(0, carryObjects.Count - 1));
            Repaint();
        }

        private static void LoadAssets<T>(List<T> destination) where T : UnityEngine.Object
        {
            destination.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int index = 0; index < guids.Length; index++)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[index]));
                if (asset != null) destination.Add(asset);
            }
            destination.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        }

        private static ToolInteractionLabController FindLabController()
        {
            return UnityEngine.Object.FindFirstObjectByType<ToolInteractionLabController>();
        }

        private void DrawSceneHandles(SceneView sceneView)
        {
            Handles.color = new Color(0.2f, 0.85f, 1f, 0.9f);
            Handles.Label(new Vector3(-2f, -0.3f, 0f), "Bomb Preview");
            DrawArc(new Vector3(-2f, 0f, 0f), new Vector2(5.2f, 1.8f), 0.65f);
            Handles.DrawAAPolyLine(3f, new Vector3(2f, 0f), new Vector3(2f, 6f));
            Handles.Label(new Vector3(2f, 6.2f), "Rope 6 cells");

            if (currentTab == ToolWorkbenchTab.ToolDefinition && tools.Count > 0)
            {
                HandToolDefinition tool = tools[Mathf.Clamp(selectedToolIndex, 0, tools.Count - 1)];
                var serialized = new SerializedObject(tool);
                SerializedProperty offsets = serialized.FindProperty("targetCellOffsets");
                for (int index = 0; index < offsets.arraySize; index++)
                {
                    SerializedProperty cell = offsets.GetArrayElementAtIndex(index);
                    Vector2Int value = cell.vector2IntValue;
                    EditorGUI.BeginChangeCheck();
                    Vector3 moved = Handles.PositionHandle(new Vector3(value.x, value.y, 0f), Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(tool, "Move Tool Target Cell");
                        cell.vector2IntValue = new Vector2Int(Mathf.RoundToInt(moved.x), Mathf.RoundToInt(moved.y));
                        serialized.ApplyModifiedProperties();
                        EditorUtility.SetDirty(tool);
                    }
                }
                Handles.DrawAAPolyLine(3f, Vector3.zero, Vector3.right * tool.PreviewRangeCells);
                if (tool.PreviewAngleDegrees > 0f)
                {
                    Handles.DrawWireArc(Vector3.zero, Vector3.forward, Vector3.right,
                        tool.PreviewAngleDegrees, tool.PreviewRangeCells);
                }
            }

            if (currentTab == ToolWorkbenchTab.ThrowPreview && carryObjects.Count > 0)
            {
                CarryObjectDefinition carry = carryObjects[Mathf.Clamp(selectedCarryIndex, 0, carryObjects.Count - 1)];
                DrawArc(new Vector3(-6f, 0f), carry.ThrowProfile.HorizontalVelocity, 1f);
            }
        }

        private static void DrawArc(Vector3 origin, Vector2 velocity, float seconds)
        {
            const int points = 24;
            var positions = new Vector3[points];
            for (int index = 0; index < points; index++)
            {
                float time = seconds * index / (points - 1f);
                positions[index] = origin + new Vector3(
                    velocity.x * time,
                    velocity.y * time - 4.9f * time * time,
                    0f);
            }
            Handles.DrawAAPolyLine(2f, positions);
        }
    }
}

#endif
