using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEditor;
using UnityEngine;

namespace StarNight.Map.Editor.WorldGeneration.SpecialRegions
{
    public sealed class SpecialRegionPreviewWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MapDesign/Special Region Validator & Preview";
        public const string WindowTitle = "Special Region Validator & Preview";
        public const float MinimumWidth = 1000f;
        public const float MinimumHeight = 680f;

        private readonly SpecialRegionPreviewModel model = new SpecialRegionPreviewModel();
        private readonly List<string> artifactIds = new List<string>();
        private Vector2 scroll;
        private SpecialRegionAuditFamily family = SpecialRegionAuditFamily.Village;
        private string artifactId = string.Empty;
        private SpecialRegionPreviewViewMode viewMode = SpecialRegionPreviewViewMode.Overview;
        private SpecialRegionPreviewOverlay overlays = SpecialRegionPreviewOverlay.All;

        public SpecialRegionPreviewSnapshot CurrentSnapshot { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public SpecialRegionAuditFamily SelectedFamily => family;
        public string SelectedArtifactId => artifactId;
        public SpecialRegionPreviewViewMode SelectedViewMode => viewMode;
        public SpecialRegionPreviewOverlay SelectedOverlays => overlays;
        public IReadOnlyList<string> ArtifactIds => new ReadOnlyCollection<string>(artifactIds.ToArray());
        public int SelectorCount => 3;
        public int OverlayToggleCount => 13;
        public int PanelCount => 5;

        [MenuItem(MenuPath)]
        public static SpecialRegionPreviewWindow Open()
        {
            var window = GetWindow<SpecialRegionPreviewWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(MinimumWidth, MinimumHeight);
            window.Show();
            window.Focus();
            return window;
        }

        public static int CloseAllOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll<SpecialRegionPreviewWindow>();
            foreach (var window in windows) window.Close();
            return windows.Length;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(MinimumWidth, MinimumHeight);
            Reload();
        }

        public bool Reload()
        {
            var result = model.Reload();
            if (!result.Success) return Publish(result);
            family = result.Snapshot.Selection.Family;
            artifactId = result.Snapshot.Selection.ArtifactId;
            viewMode = result.Snapshot.ViewMode;
            overlays = result.Snapshot.Overlays;
            BindArtifacts();
            return Publish(result);
        }

        public bool TrySelectFamily(SpecialRegionAuditFamily selected)
        {
            if (!Enum.IsDefined(typeof(SpecialRegionAuditFamily), selected)) return false;
            family = selected;
            BindArtifacts();
            artifactId = artifactIds.FirstOrDefault() ?? string.Empty;
            return Rebuild();
        }

        public bool TrySelectArtifact(string selectedArtifactId)
        {
            if (!artifactIds.Contains(selectedArtifactId ?? string.Empty)) return false;
            artifactId = selectedArtifactId;
            return Rebuild();
        }

        public bool TrySelectViewMode(SpecialRegionPreviewViewMode selected)
        {
            if (!Enum.IsDefined(typeof(SpecialRegionPreviewViewMode), selected)) return false;
            viewMode = selected;
            return Rebuild();
        }

        public bool TrySetOverlay(SpecialRegionPreviewOverlay overlay, bool enabled)
        {
            if (overlay == SpecialRegionPreviewOverlay.None || overlay == SpecialRegionPreviewOverlay.All ||
                ((int)overlay & ((int)overlay - 1)) != 0) return false;
            overlays = enabled ? overlays | overlay : overlays & ~overlay;
            return Rebuild();
        }

        private void BindArtifacts()
        {
            artifactIds.Clear();
            artifactIds.AddRange(model.Artifacts.Where(value => value.Family == family)
                .OrderBy(value => value.CanonicalOrder).Select(value => value.ArtifactId));
            if (!artifactIds.Contains(artifactId)) artifactId = artifactIds.FirstOrDefault() ?? string.Empty;
        }

        private bool Rebuild()
        {
            SpecialRegionPreviewSelection selected;
            if (!model.TrySelectArtifact(artifactId, out selected))
            {
                LastError = "The selected artifact is unavailable.";
                CurrentSnapshot = null;
                return false;
            }
            return Publish(model.Build(selected, viewMode, overlays));
        }

        private bool Publish(SpecialRegionPreviewBuildResult result)
        {
            if (result == null || !result.Success)
            {
                CurrentSnapshot = null;
                LastError = result == null ? "Preview build returned no result." : string.Join("\n", result.Errors);
                return false;
            }
            CurrentSnapshot = result.Snapshot;
            LastError = string.Empty;
            Repaint();
            return true;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawOverlayToggles();
            if (LastError.Length != 0)
            {
                EditorGUILayout.HelpBox(LastError, MessageType.Error);
                return;
            }
            if (CurrentSnapshot == null)
            {
                EditorGUILayout.HelpBox("Reload to build the read-only reference audit.", MessageType.Info);
                return;
            }

            DrawBindingBanner();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawGrid();
            DrawLegend();
            DrawAuditPanel();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(72f))) Reload();
                GUILayout.Label("Family", GUILayout.Width(42f));
                var selectedFamily = (SpecialRegionAuditFamily)EditorGUILayout.EnumPopup(
                    family, EditorStyles.toolbarPopup, GUILayout.Width(120f));
                if (selectedFamily != family) TrySelectFamily(selectedFamily);

                GUILayout.Label("Artifact", GUILayout.Width(48f));
                var currentArtifact = Math.Max(0, artifactIds.IndexOf(artifactId));
                var nextArtifact = EditorGUILayout.Popup(
                    currentArtifact, artifactIds.ToArray(), EditorStyles.toolbarPopup, GUILayout.MinWidth(280f));
                if (nextArtifact >= 0 && nextArtifact < artifactIds.Count && nextArtifact != currentArtifact)
                    TrySelectArtifact(artifactIds[nextArtifact]);

                GUILayout.Label("View", GUILayout.Width(32f));
                var selectedView = (SpecialRegionPreviewViewMode)EditorGUILayout.EnumPopup(
                    viewMode, EditorStyles.toolbarPopup, GUILayout.Width(110f));
                if (selectedView != viewMode) TrySelectViewMode(selectedView);
                GUILayout.FlexibleSpace();
                GUILayout.Label("READ-ONLY", EditorStyles.miniBoldLabel);
            }
        }

        private void DrawOverlayToggles()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Toggle(SpecialRegionPreviewOverlay.DesignChunks, "DesignChunks");
                    Toggle(SpecialRegionPreviewOverlay.SectorSeams, "SectorSeams");
                    Toggle(SpecialRegionPreviewOverlay.EntryReturn, "EntryReturn");
                    Toggle(SpecialRegionPreviewOverlay.ApronsBuffers, "ApronsBuffers");
                    Toggle(SpecialRegionPreviewOverlay.FixedCollision, "FixedCollision");
                    Toggle(SpecialRegionPreviewOverlay.FixedAccess, "FixedAccess");
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    Toggle(SpecialRegionPreviewOverlay.ReplaceableSlots, "ReplaceableSlots");
                    Toggle(SpecialRegionPreviewOverlay.LowRoute, "LowRoute");
                    Toggle(SpecialRegionPreviewOverlay.HighRoute, "HighRoute");
                    Toggle(SpecialRegionPreviewOverlay.RecoveryRoute, "RecoveryRoute");
                    Toggle(SpecialRegionPreviewOverlay.RequiredReward, "RequiredReward");
                    Toggle(SpecialRegionPreviewOverlay.StateMarkers, "StateMarkers");
                    Toggle(SpecialRegionPreviewOverlay.ResetMarkers, "ResetMarkers");
                }
            }
        }

        private void Toggle(SpecialRegionPreviewOverlay flag, string label)
        {
            var enabled = (overlays & flag) == flag;
            var selected = GUILayout.Toggle(enabled, label, GUILayout.MinWidth(105f));
            if (selected == enabled) return;
            overlays = selected ? overlays | flag : overlays & ~flag;
            Rebuild();
        }

        private void DrawBindingBanner()
        {
            var type = CurrentSnapshot.BindingBanner == "REFERENCE FIXTURE" ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(
                CurrentSnapshot.BindingBanner + "  |  " + CurrentSnapshot.ProvenanceLabel +
                "  |  " + CurrentSnapshot.PhysicsWarning,
                type);
        }

        private void DrawGrid()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scale-to-fit Grid / " + viewMode, EditorStyles.boldLabel);
            var frame = GUILayoutUtility.GetRect(920f, 330f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(frame, new Color(0.09f, 0.10f, 0.12f, 1f));
            GUI.Box(frame, GUIContent.none);
            var width = Math.Max(1, CurrentSnapshot.GridMaximumX - CurrentSnapshot.GridMinimumX + 1);
            var height = Math.Max(1, CurrentSnapshot.GridMaximumY - CurrentSnapshot.GridMinimumY + 1);
            var scale = Math.Min((frame.width - 24f) / width, (frame.height - 24f) / height);
            scale = Mathf.Clamp(scale, 2f, 18f);
            var contentWidth = width * scale;
            var contentHeight = height * scale;
            var origin = new Vector2(frame.x + (frame.width - contentWidth) * 0.5f,
                frame.y + (frame.height - contentHeight) * 0.5f);

            foreach (var token in CurrentSnapshot.Tokens)
            {
                var position = new Rect(
                    origin.x + (token.X - CurrentSnapshot.GridMinimumX) * scale,
                    origin.y + (CurrentSnapshot.GridMaximumY - token.Y) * scale,
                    Math.Max(4f, scale), Math.Max(4f, scale));
                EditorGUI.DrawRect(position, TokenColor(token.Kind));
                if (scale >= 8f)
                    GUI.Label(new Rect(position.x + 2f, position.y - 1f, 72f, 16f), TokenText(token.Kind), EditorStyles.miniLabel);
            }
            GUI.Label(new Rect(frame.x + 8f, frame.y + 6f, frame.width - 16f, 18f),
                CurrentSnapshot.Artifact.ArtifactId + "  |  " + CurrentSnapshot.Artifact.Input.KindOrTheme +
                "  |  " + CurrentSnapshot.ScaleToFitTokenCount + " visible tokens",
                EditorStyles.miniBoldLabel);
        }

        private void DrawLegend()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legend (token + text; color is never the only meaning)", EditorStyles.boldLabel);
            for (var index = 0; index < CurrentSnapshot.Legend.Count; index += 3)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var column = 0; column < 3 && index + column < CurrentSnapshot.Legend.Count; column++)
                    {
                        var entry = CurrentSnapshot.Legend[index + column];
                        var previous = GUI.backgroundColor;
                        GUI.backgroundColor = TokenColor(entry.Kind);
                        GUILayout.Box(entry.Token + " — " + entry.Meaning, GUILayout.MinWidth(290f), GUILayout.Height(22f));
                        GUI.backgroundColor = previous;
                    }
                }
            }
        }

        private void DrawAuditPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audit Panel", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Sections",
                CurrentSnapshot.AuditSectionPassCount + " PASS / " + CurrentSnapshot.AuditSectionFailCount + " FAIL");
            foreach (var section in CurrentSnapshot.Artifact.Sections)
                EditorGUILayout.LabelField(
                    (section.Passed ? "PASS" : "FAIL") + "  " + section.Section,
                    section.ObservedCount + "  |  " + section.Detail);
            EditorGUILayout.Space();
            EditorGUILayout.SelectableLabel("Source     " + CurrentSnapshot.Artifact.Input.SourceDigest,
                EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel("Component  " + CurrentSnapshot.Artifact.Input.ComponentDigest,
                EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel("Artifact   " + CurrentSnapshot.Artifact.CanonicalDigest,
                EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel("Audit      " + CurrentSnapshot.AuditDigest,
                EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.HelpBox(
                "No Save / Apply / Fix / Generate / Bake / Export actions are provided. " +
                "This window does not edit CSV, assets, Scene, Prefab, Tilemap, or gameplay objects.",
                MessageType.None);
        }

        private static Color TokenColor(SpecialRegionAuditTokenKind kind)
        {
            switch (kind)
            {
                case SpecialRegionAuditTokenKind.Entry: return new Color(0.20f, 0.82f, 0.48f, 1f);
                case SpecialRegionAuditTokenKind.Return: return new Color(0.20f, 0.68f, 0.92f, 1f);
                case SpecialRegionAuditTokenKind.FixedCollision: return new Color(0.86f, 0.25f, 0.25f, 1f);
                case SpecialRegionAuditTokenKind.FixedAccess: return new Color(0.98f, 0.58f, 0.20f, 1f);
                case SpecialRegionAuditTokenKind.Reward: return new Color(1.00f, 0.84f, 0.20f, 1f);
                case SpecialRegionAuditTokenKind.HighRoute: return new Color(0.74f, 0.42f, 0.96f, 1f);
                case SpecialRegionAuditTokenKind.RecoveryRoute: return new Color(0.95f, 0.44f, 0.72f, 1f);
                case SpecialRegionAuditTokenKind.State: return new Color(0.48f, 0.78f, 0.98f, 1f);
                case SpecialRegionAuditTokenKind.Reset: return new Color(0.98f, 0.42f, 0.42f, 1f);
                default: return new Color(0.52f, 0.60f, 0.68f, 1f);
            }
        }

        private static string TokenText(SpecialRegionAuditTokenKind kind)
        {
            switch (kind)
            {
                case SpecialRegionAuditTokenKind.Entry: return "EN";
                case SpecialRegionAuditTokenKind.Return: return "RT";
                case SpecialRegionAuditTokenKind.FixedCollision: return "FC";
                case SpecialRegionAuditTokenKind.FixedAccess: return "FA";
                case SpecialRegionAuditTokenKind.Reward: return "RW";
                case SpecialRegionAuditTokenKind.LowRoute: return "L";
                case SpecialRegionAuditTokenKind.HighRoute: return "H";
                case SpecialRegionAuditTokenKind.RecoveryRoute: return "R";
                case SpecialRegionAuditTokenKind.State: return "S";
                case SpecialRegionAuditTokenKind.Reset: return "X";
                default: return kind.ToString().Substring(0, Math.Min(2, kind.ToString().Length)).ToUpperInvariant();
            }
        }
    }
}
