#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StarNight.MapAuthoring.Editor
{
    public sealed class MapElementWorkbenchWindow : EditorWindow
    {
        private static readonly (string label, string propertyPath)[] PropertyTabs =
        {
            ("Footprint", "Footprint"),
            ("Visual", "VisualProfile"),
            ("Collision", "CollisionProfile"),
            ("Behavior", "BehaviorProfile"),
            ("Signals", string.Empty),
            ("Tool Reactions", "ToolReactions"),
            ("Moon", "MoonProfile"),
            ("Bridge", "BridgeProfile"),
            ("Palace", "PalaceProfile"),
            ("Post", "PostProfile"),
            ("Sun", "SunProfile"),
            ("Polaris", "PolarisProfile"),
            ("Placement", "PlacementProfile"),
            ("Budget", "BudgetProfile"),
            ("Audio/VFX", string.Empty),
            ("Validation", "BakeMetadata"),
        };

        private ObjectField definitionField;
        private TextField searchField;
        private ScrollView definitionResults;
        private VisualElement assetFields;
        private VisualElement propertyContent;
        private Label statusLabel;
        private Label sourcePathLabel;
        private Label runtimePathLabel;
        private Label bakeVersionLabel;
        private Label validationLabel;
        private SerializedObject serializedDefinition;
        private MapElementDefinition boundDefinition;
        private string activePropertyPath = "Footprint";
        private MapElementValidationReport lastValidationReport;

        public static MapElementWorkbenchWindow OpenWindow()
        {
            var window = GetWindow<MapElementWorkbenchWindow>();
            window.titleContent = new GUIContent("Map Element Lab");
            window.minSize = new Vector2(980f, 560f);
            window.Show();
            return window;
        }

        public void CreateGUI()
        {
            MapElementAuthoringSession.Changed -= OnSessionChanged;
            MapElementAuthoringSession.Changed += OnSessionChanged;

            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 6f;
            rootVisualElement.style.paddingRight = 6f;
            rootVisualElement.style.paddingTop = 6f;
            rootVisualElement.style.paddingBottom = 6f;

            var title = new Label("MAP-E04 · Map Element Workbench · Validate / Bake");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16f;
            title.style.marginBottom = 6f;
            rootVisualElement.Add(title);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            body.Add(BuildAssetPanel());
            body.Add(BuildScenePanel());
            body.Add(BuildPropertyPanel());
            rootVisualElement.Add(body);
            rootVisualElement.Add(BuildTestPanel());

            statusLabel = new Label();
            statusLabel.style.marginTop = 4f;
            statusLabel.style.color = new Color(0.65f, 0.85f, 1f);
            rootVisualElement.Add(statusLabel);

            MapElementDefinitionPresetFactory.EnsureLabSamples(out var spike, out _);
            if (MapElementAuthoringSession.SelectedDefinition == null)
            {
                MapElementAuthoringSession.SelectedDefinition = spike;
            }

            BindDefinition(MapElementAuthoringSession.SelectedDefinition);
        }

        private void OnDisable()
        {
            MapElementAuthoringSession.Changed -= OnSessionChanged;
        }

        private VisualElement BuildAssetPanel()
        {
            var panel = CreatePanel(248f);
            panel.Add(CreatePanelTitle("Asset"));

            var presetRow = new VisualElement();
            presetRow.style.flexDirection = FlexDirection.Row;
            presetRow.Add(CreateButton("새 요소", () => CreatePreset(MapElementLabPreset.Empty)));
            presetRow.Add(CreateButton("1×1 Spike", () => CreatePreset(MapElementLabPreset.Spike1x1)));
            panel.Add(presetRow);
            panel.Add(CreateButton("2×1 Moving Platform", () => CreatePreset(MapElementLabPreset.MovingPlatform2x1)));

            definitionField = new ObjectField("Definition")
            {
                objectType = typeof(MapElementDefinition),
                allowSceneObjects = false,
            };
            definitionField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is MapElementDefinition definition)
                {
                    MapElementAuthoringSession.SelectedDefinition = definition;
                }
            });
            panel.Add(definitionField);

            searchField = new TextField("기존 Definition 검색");
            searchField.RegisterValueChangedCallback(_ => RefreshDefinitionResults());
            panel.Add(searchField);
            definitionResults = new ScrollView(ScrollViewMode.Vertical);
            definitionResults.style.height = 92f;
            panel.Add(definitionResults);

            assetFields = new VisualElement();
            assetFields.style.flexGrow = 1f;
            panel.Add(assetFields);
            return panel;
        }

        private VisualElement BuildScenePanel()
        {
            var panel = CreatePanel();
            panel.style.flexGrow = 1f;
            panel.Add(CreatePanelTitle("SceneView · 1×1 Grid / Mask / Handle"));

            var modeRow = new VisualElement();
            modeRow.style.flexDirection = FlexDirection.Row;
            modeRow.style.flexWrap = Wrap.Wrap;
            AddModeButton(modeRow, "Footprint", MapElementEditMode.Footprint);
            AddModeButton(modeRow, "Visual", MapElementEditMode.Visual);
            AddModeButton(modeRow, "Collider", MapElementEditMode.Collider);
            AddModeButton(modeRow, "Path", MapElementEditMode.Path);
            AddModeButton(modeRow, "Signal", MapElementEditMode.Signal);
            panel.Add(modeRow);

            var help = new HelpBox(
                "공간 편집은 SceneView에서 처리합니다.\n" +
                "Footprint: Click/Shift/Ctrl/Alt · F Focus\n" +
                "Visual 0.05셀 · Collider 0.01셀 · Path 0.5셀 스냅",
                HelpBoxMessageType.Info);
            help.style.flexGrow = 1f;
            help.style.marginTop = 8f;
            panel.Add(help);

            panel.Add(CreateButton("선택 요소 Preview 갱신", RefreshLabPreview));
            panel.Add(CreateButton("Lab 씬 다시 프레이밍", () => SceneView.lastActiveSceneView?.FrameSelected()));
            return panel;
        }

        private VisualElement BuildPropertyPanel()
        {
            var panel = CreatePanel(330f);
            panel.Add(CreatePanelTitle("속성 탭"));
            var tabs = new ScrollView(ScrollViewMode.Horizontal);
            tabs.style.height = 32f;
            for (var index = 0; index < PropertyTabs.Length; index++)
            {
                var tab = PropertyTabs[index];
                tabs.Add(CreateButton(tab.label, () => SelectPropertyTab(tab.propertyPath, tab.label)));
            }

            panel.Add(tabs);
            propertyContent = new ScrollView(ScrollViewMode.Vertical);
            propertyContent.style.flexGrow = 1f;
            panel.Add(propertyContent);
            return panel;
        }

        private VisualElement BuildTestPanel()
        {
            var panel = CreatePanel();
            panel.style.height = 118f;
            panel.style.marginTop = 6f;
            panel.Add(CreatePanelTitle("테스트"));
            var buttons = new ScrollView(ScrollViewMode.Horizontal);
            buttons.style.flexGrow = 1f;
            buttons.Add(CreateButton("Idle로 초기화", rig => rig.ResetToIdle()));
            buttons.Add(CreateButton("Warning 시작", rig => rig.SetPreviewState(MapElementState.Warning)));
            buttons.Add(CreateButton("Active 강제", rig => rig.SetPreviewState(MapElementState.Active)));
            buttons.Add(CreateButton("Cooldown 진행", rig => rig.SetPreviewState(MapElementState.Cooldown)));
            buttons.Add(CreateButton("Break 적용", rig => rig.SetPreviewState(MapElementState.Broken)));
            buttons.Add(CreateButton("Bomb", rig => rig.SimulateToolReaction(ToolTag.Bomb)));
            buttons.Add(CreateButton("Pickaxe", rig => rig.SimulateToolReaction(ToolTag.Pickaxe)));
            buttons.Add(CreateButton("Water", rig => rig.SimulateToolReaction(ToolTag.Water)));
            buttons.Add(CreateButton("Hook", rig => rig.SimulateToolReaction(ToolTag.Hook)));
            buttons.Add(CreateButton("HeavyObject 충돌", rig => rig.SimulateHeavyObjectCollision()));
            buttons.Add(CreateButton("Maru 충돌", rig => rig.SimulateMaruCollision()));
            buttons.Add(CreateButton("Play Test 진입", EnterPlayTest));
            buttons.Add(CreateButton("100회 반복", rig => rig.RunRepeatedSimulation(100)));
            buttons.Add(CreateButton("Validate", () => ValidateCurrentDefinition(true)));
            buttons.Add(CreateButton("Bake Runtime", BakeCurrentDefinition));
            panel.Add(buttons);
            return panel;
        }

        private void BindDefinition(MapElementDefinition definition)
        {
            if (definition == null || assetFields == null || propertyContent == null)
            {
                return;
            }

            boundDefinition = definition;
            serializedDefinition = new SerializedObject(definition);
            definitionField?.SetValueWithoutNotify(definition);
            RebuildAssetFields();
            RebuildPropertyContent(activePropertyPath, GetTabLabel(activePropertyPath));
            RefreshDefinitionResults();
            UpdateStatus();
        }

        private void RebuildAssetFields()
        {
            assetFields.Clear();
            AddBoundProperty(assetFields, "ElementId");
            AddBoundProperty(assetFields, "DisplayName");
            AddBoundProperty(assetFields, "Category");
            AddBoundProperty(assetFields, "AllowedRegions");
            AddBoundProperty(assetFields, "RuntimePrefab");

            sourcePathLabel = new Label();
            runtimePathLabel = new Label();
            bakeVersionLabel = new Label();
            validationLabel = new Label();
            assetFields.Add(sourcePathLabel);
            assetFields.Add(runtimePathLabel);
            assetFields.Add(bakeVersionLabel);
            assetFields.Add(validationLabel);
            assetFields.Bind(serializedDefinition);
        }

        private void RebuildPropertyContent(string propertyPath, string tabLabel)
        {
            propertyContent.Clear();
            propertyContent.Add(CreatePanelTitle(tabLabel));
            if (serializedDefinition == null)
            {
                return;
            }

            if (tabLabel == "Signals")
            {
                propertyContent.Add(new HelpBox(
                    "MAP-E03 Lab에서는 Port 기능의 저작 위치만 제공합니다. 실제 Room 링크는 Stage Layout Lab에서 연결합니다.",
                    HelpBoxMessageType.Info));
                return;
            }

            if (tabLabel == "Audio/VFX")
            {
                AddBoundProperty(propertyContent, "VisualProfile.MaterialOverride", "Material Override");
                AddBoundProperty(propertyContent, "ToolReactions", "Tool Reaction VFX");
                propertyContent.Bind(serializedDefinition);
                return;
            }

            var property = serializedDefinition.FindProperty(propertyPath);
            if (property != null)
            {
                var propertyField = new PropertyField(property);
                propertyField.Bind(serializedDefinition);
                propertyContent.Add(propertyField);
            }

            if (tabLabel == "Tool Reactions")
            {
                AddBoundProperty(propertyContent, "MaruReaction", "Maru Reaction");
                propertyContent.Bind(serializedDefinition);
                AddReactionMatrixPreview(propertyContent);
            }
            else if (tabLabel == "Validation")
            {
                propertyContent.Add(new HelpBox(
                    "Error 0개일 때만 Source→Runtime Prefab을 Bake합니다. 기존 GUID는 유지되며 실패한 검증은 Runtime을 덮어쓰지 않습니다.",
                    HelpBoxMessageType.Info));
                propertyContent.Add(CreateButton("Validate", () => ValidateCurrentDefinition(true)));
                propertyContent.Add(CreateButton("허용 항목 자동 수정", ApplyAllowedAutoFixes));
                propertyContent.Add(CreateButton("Bake Runtime Prefab", BakeCurrentDefinition));
            }
        }

        private void AddBoundProperty(VisualElement parent, string propertyPath, string label = null)
        {
            var property = serializedDefinition?.FindProperty(propertyPath);
            if (property == null)
            {
                return;
            }

            parent.Add(string.IsNullOrEmpty(label)
                ? new PropertyField(property)
                : new PropertyField(property, label));
        }

        private void AddReactionMatrixPreview(VisualElement parent)
        {
            parent.Add(CreatePanelTitle("Normalized Reaction Matrix"));
            parent.Add(new HelpBox(
                "Undefined rows are rejected without resource consumption. Each atomic ToolTag may resolve to only one reaction row.",
                HelpBoxMessageType.Info));
            var table = boundDefinition != null ? boundDefinition.ToolReactions : null;
            for (var index = 0; index < ToolReactionMatrix.AtomicTools.Length; index++)
            {
                var tool = ToolReactionMatrix.AtomicTools[index];
                ToolReactionEntry entry = null;
                var defined = table != null && table.TryResolve(tool, out entry, out _);
                var text = defined
                    ? $"{tool,-12}  {entry.Reaction} x{Mathf.Max(1, entry.StrengthRequired)}" +
                      (string.IsNullOrWhiteSpace(entry.ResultState)
                          ? string.Empty
                          : $"  -> {entry.ResultState}") +
                      $"  [{ToolReactionReceiver.ResolveFeedback(entry)}]"
                    : $"{tool,-12}  Rejected (Accepted=false)";
                var row = new Label(text);
                row.style.color = defined
                    ? new Color(0.48f, 0.9f, 0.62f)
                    : new Color(0.58f, 0.62f, 0.7f);
                row.style.unityFont = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
                parent.Add(row);
            }
        }

        private void SelectPropertyTab(string propertyPath, string label)
        {
            activePropertyPath = propertyPath;
            RebuildPropertyContent(propertyPath, label);
        }

        private void CreatePreset(MapElementLabPreset preset)
        {
            var definition = MapElementDefinitionPresetFactory.CreatePresetAsset(preset);
            MapElementAuthoringSession.SelectedDefinition = definition;
            statusLabel.text = $"생성 완료: {AssetDatabase.GetAssetPath(definition)}";
        }

        private void RefreshDefinitionResults()
        {
            if (definitionResults == null)
            {
                return;
            }

            definitionResults.Clear();
            var search = searchField?.value ?? string.Empty;
            var guids = AssetDatabase.FindAssets($"t:{nameof(MapElementDefinition)}");
            var results = new List<MapElementDefinition>();
            for (var index = 0; index < guids.Length; index++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[index]));
                if (definition == null)
                {
                    continue;
                }

                var text = $"{definition.ElementId} {definition.DisplayName}";
                if (string.IsNullOrWhiteSpace(search) ||
                    text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    results.Add(definition);
                }
            }

            results.Sort((left, right) => string.Compare(
                left.ElementId,
                right.ElementId,
                StringComparison.OrdinalIgnoreCase));
            for (var index = 0; index < results.Count; index++)
            {
                var result = results[index];
                definitionResults.Add(CreateButton(
                    $"{result.ElementId} · {result.DisplayName}",
                    () => MapElementAuthoringSession.SelectedDefinition = result));
            }
        }

        private void RefreshLabPreview()
        {
            var rig = FindFirstObjectByType<MapElementLabTestRig>();
            if (rig == null || boundDefinition == null)
            {
                statusLabel.text = "00_MapElementLab 씬을 먼저 여세요.";
                return;
            }

            serializedDefinition?.ApplyModifiedProperties();
            rig.SetDefinition(boundDefinition);
            SceneView.RepaintAll();
            statusLabel.text = $"Preview 갱신: {boundDefinition.DisplayName}";
        }

        private MapElementValidationReport ValidateCurrentDefinition(bool showReport)
        {
            serializedDefinition?.ApplyModifiedProperties();
            var sourceRoot = FindSourceRoot();
            lastValidationReport = MapElementValidator.ValidateSourceForBake(
                boundDefinition,
                sourceRoot);
            if (showReport)
            {
                ValidationReportWindow.ShowReport(lastValidationReport);
            }

            statusLabel.text = lastValidationReport.CreateSummary();
            validationLabel.text = lastValidationReport.IsValid
                ? $"마지막 검증: 통과 · Warning {lastValidationReport.WarningCount}"
                : $"마지막 검증: Error {lastValidationReport.ErrorCount} · Warning {lastValidationReport.WarningCount}";
            validationLabel.style.color = lastValidationReport.IsValid
                ? new Color(0.45f, 0.9f, 0.55f)
                : new Color(1f, 0.45f, 0.4f);
            return lastValidationReport;
        }

        private void ApplyAllowedAutoFixes()
        {
            var count = MapElementValidator.ApplyAllowedAutoFixes(boundDefinition, FindSourceRoot());
            serializedDefinition?.UpdateIfRequiredOrScript();
            RefreshLabPreview();
            ValidateCurrentDefinition(false);
            statusLabel.text = $"허용된 자동 수정 {count}건 적용 · {lastValidationReport.CreateSummary()}";
        }

        private void BakeCurrentDefinition()
        {
            serializedDefinition?.ApplyModifiedProperties();
            var result = MapElementBakePipeline.Bake(boundDefinition, FindSourceRoot());
            lastValidationReport = result.Validation;
            serializedDefinition?.UpdateIfRequiredOrScript();
            UpdateStatus();
            statusLabel.text = result.Message;
            if (!result.Success)
            {
                ValidationReportWindow.ShowReport(result.Validation);
                return;
            }

            AssetDatabase.SaveAssets();
            RefreshDefinitionResults();
            EditorGUIUtility.PingObject(result.RuntimePrefab);
            Debug.Log($"[MAP-E04] {result.Message} · GUID {result.BakedDefinition.BakeMetadata.RuntimePrefabGuid}");
        }

        private void EnterPlayTest()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (!string.Equals(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                MapElementLabBuilder.OpenOrCreateLab();
            }

            serializedDefinition?.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            EditorApplication.EnterPlaymode();
        }

        private void RunRig(Action<MapElementLabTestRig> action)
        {
            var rig = FindFirstObjectByType<MapElementLabTestRig>();
            if (rig == null)
            {
                statusLabel.text = "00_MapElementLab 씬을 먼저 여세요.";
                return;
            }

            action(rig);
            statusLabel.text = rig.LastSimulationResult;
            SceneView.RepaintAll();
        }

        private void UpdateStatus()
        {
            if (boundDefinition == null ||
                sourcePathLabel == null ||
                runtimePathLabel == null ||
                bakeVersionLabel == null ||
                validationLabel == null ||
                statusLabel == null)
            {
                return;
            }

            var sourcePath = AssetPathUtility.IsSafeFileName(boundDefinition.ElementId)
                ? AssetPathUtility.GetMapElementBakePaths(boundDefinition).SourcePrefab
                : "(Element ID 수정 필요)";
            var runtimePath = boundDefinition.RuntimePrefab != null
                ? AssetDatabase.GetAssetPath(boundDefinition.RuntimePrefab)
                : "(MAP-E04에서 Bake)";
            var report = MapElementValidator.ValidateSourceForBake(boundDefinition, FindSourceRoot());
            lastValidationReport = report;
            sourcePathLabel.text = $"Source: {sourcePath}";
            runtimePathLabel.text = $"Runtime: {runtimePath}";
            bakeVersionLabel.text = $"Bake Version: {boundDefinition.BakeMetadata?.SchemaVersion ?? 0}";
            validationLabel.text = report.IsValid
                ? $"마지막 검증: 통과 · Warning {report.WarningCount}"
                : $"마지막 검증: Error {report.ErrorCount} · Warning {report.WarningCount}";
            validationLabel.style.color = report.IsValid
                ? new Color(0.45f, 0.9f, 0.55f)
                : new Color(1f, 0.45f, 0.4f);
            statusLabel.text = report.IsValid
                ? $"{boundDefinition.DisplayName} · Validate/Bake 준비"
                : report.CreateSummary();
        }

        private void OnSessionChanged()
        {
            if (MapElementAuthoringSession.SelectedDefinition != boundDefinition)
            {
                BindDefinition(MapElementAuthoringSession.SelectedDefinition);
                return;
            }

            serializedDefinition?.UpdateIfRequiredOrScript();
            UpdateStatus();
            Repaint();
        }

        private static VisualElement CreatePanel(float width = 0f)
        {
            var panel = new VisualElement();
            panel.style.paddingLeft = 6f;
            panel.style.paddingRight = 6f;
            panel.style.paddingTop = 6f;
            panel.style.paddingBottom = 6f;
            panel.style.marginRight = 4f;
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderTopColor = new Color(0.22f, 0.3f, 0.4f);
            panel.style.borderBottomColor = new Color(0.22f, 0.3f, 0.4f);
            panel.style.borderLeftColor = new Color(0.22f, 0.3f, 0.4f);
            panel.style.borderRightColor = new Color(0.22f, 0.3f, 0.4f);
            panel.style.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.38f);
            if (width > 0f)
            {
                panel.style.width = width;
                panel.style.flexShrink = 0f;
            }

            return panel;
        }

        private static Label CreatePanelTitle(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 4f;
            return label;
        }

        private static Button CreateButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.style.marginRight = 3f;
            button.style.marginBottom = 3f;
            return button;
        }

        private Button CreateButton(string text, Action<MapElementLabTestRig> action)
        {
            return CreateButton(text, () => RunRig(action));
        }

        private static void AddModeButton(VisualElement parent, string label, MapElementEditMode mode)
        {
            parent.Add(CreateButton(label, () => MapElementAuthoringSession.EditMode = mode));
        }

        private static GameObject FindSourceRoot()
        {
            return string.Equals(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                    EditorSceneBuildGuard.MapElementLabPath,
                    StringComparison.OrdinalIgnoreCase)
                ? GameObject.Find("ActiveAuthoringElement")
                : null;
        }

        private static string GetTabLabel(string propertyPath)
        {
            for (var index = 0; index < PropertyTabs.Length; index++)
            {
                if (PropertyTabs[index].propertyPath == propertyPath)
                {
                    return PropertyTabs[index].label;
                }
            }

            return "Footprint";
        }
    }
}

#endif
