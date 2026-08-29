using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Activities.Authoring;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Activities
{
    public enum ActivityEventPreviewViewMode
    {
        Static = 1,
        Active = 2,
        Removed = 3,
        Compare = 4,
    }

    public sealed class ActivityEventPreviewWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MapDesign/Activity & Event Preview";
        public const string WindowTitle = "Activity & Event Preview";
        public const string LegendText =
            "EN Entry | EX Exit | AP Protected | C Cue | T Trigger | D Device | H Hazard | " +
            "P Projectile | N Npc | RW Reward | SP SafePocket | RC Recovery | RS Reset | EV Event";

        private readonly ActivityEventPreviewModel model = new ActivityEventPreviewModel();
        private readonly List<string> activityIds = new List<string>();
        private readonly List<string> eventIds = new List<string>();

        private TerrainClusterAuthoringCatalog terrainCatalog;
        private string terrainCatalogDigest = string.Empty;
        private MicroPatternAuthoringCatalog microPatternCatalog;
        private string microPatternCatalogDigest = string.Empty;
        private ActivityEventCsvImportResult content;
        private string selectedActivityId = string.Empty;
        private string selectedEventId = string.Empty;
        private ActivityEventPreviewViewMode viewMode = ActivityEventPreviewViewMode.Static;
        private Vector2 scroll;
        private bool showShell = true;
        private bool showEntryExit = true;
        private bool showProtected = true;
        private bool showCue = true;
        private bool showMechanism = true;
        private bool showReward = true;
        private bool showSafeRecovery = true;
        private bool showEvent = true;

        public IReadOnlyList<string> ActivityIds => new ReadOnlyCollection<string>(activityIds.ToArray());
        public IReadOnlyList<string> EventIds => new ReadOnlyCollection<string>(eventIds.ToArray());
        public string SelectedActivityId => selectedActivityId;
        public string SelectedEventId => selectedEventId;
        public ActivityEventPreviewViewMode ViewMode => viewMode;
        public ActivityEventPreviewBuildResult CurrentResult { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public int StatePanelCount => viewMode == ActivityEventPreviewViewMode.Compare ? 3 : 1;

        [MenuItem(MenuPath)]
        public static ActivityEventPreviewWindow Open()
        {
            var window = GetWindow<ActivityEventPreviewWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(900f, 620f);
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(900f, 620f);
            if (content == null) Reload();
        }

        public bool Reload()
        {
            LastError = string.Empty;
            CurrentResult = null;
            try
            {
                var terrain = new TerrainClusterCsvImporterV2().Import();
                var patterns = new MicroPatternCsvImporterV2().Import();
                if (!terrain.Success || !patterns.Success || !patterns.Published)
                {
                    LastError = string.Join("\n", terrain.Errors.Select(value => value.ToString())
                        .Concat(patterns.Errors.Select(value => value.ToString())));
                    return false;
                }
                var imported = new ActivityEventCsvImporterV2().Import(terrain.Catalog);
                if (!imported.Success || !imported.Published)
                {
                    LastError = string.Join("\n", imported.Errors.Select(value => value.ToString()));
                    return false;
                }
                terrainCatalog = terrain.Catalog;
                terrainCatalogDigest = terrain.StableDigest;
                microPatternCatalog = patterns.Catalog;
                microPatternCatalogDigest = patterns.StableDigest;
                content = imported;
                activityIds.Clear();
                activityIds.AddRange(model.ActivityIds);
                eventIds.Clear();
                eventIds.Add(string.Empty);
                eventIds.AddRange(model.EventIds);
                if (!activityIds.Contains(selectedActivityId)) selectedActivityId = activityIds[0];
                if (!eventIds.Contains(selectedEventId)) selectedEventId = string.Empty;
                return Rebuild();
            }
            catch (Exception exception)
            {
                LastError = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        public bool TrySelectActivity(string activityId)
        {
            if (!activityIds.Contains(activityId ?? string.Empty)) return false;
            selectedActivityId = activityId;
            return Rebuild();
        }

        public bool TrySelectEvent(string eventId)
        {
            if (!eventIds.Contains(eventId ?? string.Empty)) return false;
            selectedEventId = eventId ?? string.Empty;
            return Rebuild();
        }

        public bool TrySelectViewMode(ActivityEventPreviewViewMode mode)
        {
            if (!Enum.IsDefined(typeof(ActivityEventPreviewViewMode), mode)) return false;
            viewMode = mode;
            return CurrentResult != null || Rebuild();
        }

        private bool Rebuild()
        {
            LastError = string.Empty;
            CurrentResult = null;
            if (terrainCatalog == null || microPatternCatalog == null || content == null || selectedActivityId.Length == 0)
            {
                LastError = "Published catalogs and an Activity selection are required.";
                return false;
            }
            var result = model.Build(new ActivityEventPreviewRequest(
                    selectedActivityId, selectedEventId, ActivityEventPreviewModel.ApprovedAggregateDigest),
                terrainCatalog, terrainCatalogDigest, microPatternCatalog, microPatternCatalogDigest, content);
            if (!result.Success)
            {
                LastError = string.Join("\n", result.Errors.Select(value => value.ToString()));
                return false;
            }
            CurrentResult = result;
            Repaint();
            return true;
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (LastError.Length != 0) EditorGUILayout.HelpBox(LastError, MessageType.Error);
            if (CurrentResult == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(LegendText + " | CH 12x8 chunk boundary", MessageType.None);
            if (viewMode == ActivityEventPreviewViewMode.Compare)
                DrawCompare();
            else
                DrawSingle(SelectedSnapshot(), viewMode.ToString(), 7f);
            DrawProfileDetails();
            DrawEvidenceDetails();
            DrawDigestDetails();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(72f))) Reload();
                var activityIndex = Math.Max(0, activityIds.IndexOf(selectedActivityId));
                var nextActivity = EditorGUILayout.Popup(activityIndex, activityIds.ToArray(),
                    EditorStyles.toolbarPopup, GUILayout.MinWidth(260f));
                if (nextActivity >= 0 && nextActivity < activityIds.Count && nextActivity != activityIndex)
                    TrySelectActivity(activityIds[nextActivity]);
                var eventLabels = eventIds.Select(value => value.Length == 0 ? "None" : value).ToArray();
                var eventIndex = Math.Max(0, eventIds.IndexOf(selectedEventId));
                var nextEvent = EditorGUILayout.Popup(eventIndex, eventLabels,
                    EditorStyles.toolbarPopup, GUILayout.MinWidth(210f));
                if (nextEvent >= 0 && nextEvent < eventIds.Count && nextEvent != eventIndex)
                    TrySelectEvent(eventIds[nextEvent]);
                var nextMode = (ActivityEventPreviewViewMode)EditorGUILayout.EnumPopup(viewMode,
                    EditorStyles.toolbarPopup, GUILayout.Width(90f));
                if (nextMode != viewMode) TrySelectViewMode(nextMode);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                showShell = GUILayout.Toggle(showShell, "Shell");
                showEntryExit = GUILayout.Toggle(showEntryExit, "Entry/Exit");
                showProtected = GUILayout.Toggle(showProtected, "Protected");
                showCue = GUILayout.Toggle(showCue, "Cue");
                showMechanism = GUILayout.Toggle(showMechanism, "Mechanism");
                showReward = GUILayout.Toggle(showReward, "Reward");
                showSafeRecovery = GUILayout.Toggle(showSafeRecovery, "Safe/Recovery");
                showEvent = GUILayout.Toggle(showEvent, "Event");
            }
        }

        private ActivityStatePreviewSnapshot SelectedSnapshot()
        {
            switch (viewMode)
            {
                case ActivityEventPreviewViewMode.Active: return CurrentResult.ActiveSnapshot;
                case ActivityEventPreviewViewMode.Removed: return CurrentResult.RemovedSnapshot;
                default: return CurrentResult.StaticSnapshot;
            }
        }

        private void DrawCompare()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Static / Active / Removed Compare", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSingle(CurrentResult.StaticSnapshot, "Static", 4f);
                DrawSingle(CurrentResult.ActiveSnapshot, "Active", 4f);
                DrawSingle(CurrentResult.RemovedSnapshot, "Removed", 4f);
            }
            var comparison = CurrentResult.Comparison;
            EditorGUILayout.LabelField("Marker deltas",
                comparison.StaticToActiveMarkerDelta + " / " + comparison.ActiveToRemovedMarkerDelta);
            EditorGUILayout.LabelField("Geometry / Cell / Route / Access / Protected deltas",
                comparison.GeometryDelta + " / " + comparison.StaticToActiveCellDelta + " / " +
                comparison.RouteDelta + " / " + comparison.AccessDelta + " / " + comparison.ProtectionDelta);
        }

        private void DrawSingle(ActivityStatePreviewSnapshot snapshot, string heading, float scale)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(230f)))
            {
                EditorGUILayout.LabelField(heading, EditorStyles.boldLabel);
                DrawCanvas(snapshot, scale);
                EditorGUILayout.LabelField("Activity / Event markers",
                    snapshot.ActivityMarkerCount + " / " + snapshot.EventMarkerCount);
                EditorGUILayout.LabelField("Residual / Tile / Collider / RNG",
                    snapshot.ResidualMarkerCount + " / " + snapshot.TileDeltaCount + " / " +
                    snapshot.ColliderDeltaCount + " / " + snapshot.RngDrawCount);
            }
        }

        private void DrawCanvas(ActivityStatePreviewSnapshot snapshot, float scale)
        {
            var width = Math.Max(1, snapshot.Cells.Max(value => value.SourceCoordinate.X) + 1);
            var height = Math.Max(1, snapshot.Cells.Max(value => value.SourceCoordinate.Y) + 1);
            var area = GUILayoutUtility.GetRect(width * scale, height * scale,
                GUILayout.Width(width * scale), GUILayout.Height(height * scale));
            foreach (var cell in snapshot.Cells)
            {
                var rect = CellRect(area, cell.SourceCoordinate, height, scale);
                var color = !showShell ? new Color(0.16f, 0.16f, 0.16f) :
                    cell.Occupancy == "SOLID" ? new Color(0.28f, 0.30f, 0.34f) : new Color(0.64f, 0.66f, 0.70f);
                if (showProtected && cell.ProtectedOpen) color = new Color(0.20f, 0.65f, 0.88f);
                EditorGUI.DrawRect(rect, color);
            }
            if (showEntryExit)
                foreach (var route in snapshot.RouteWitnesses.Where(value => value.Token == "EN" || value.Token == "EX"))
                    DrawToken(area, route.SourceStart, height, scale, route.Token,
                        route.Token == "EN" ? new Color(0.20f, 0.85f, 0.35f) : new Color(0.95f, 0.45f, 0.20f));
            foreach (var marker in snapshot.ActivityMarkers.Where(ShowActivityMarker))
                DrawToken(area, marker.SourceCoordinate, height, scale, marker.Token, MarkerColor(marker.Token));
            if (showEvent)
                foreach (var marker in snapshot.EventMarkers)
                    DrawToken(area, marker.SourceCoordinate, height, scale, marker.Token, new Color(0.95f, 0.35f, 0.88f));
            for (var x = 0; x <= width; x += 12)
                EditorGUI.DrawRect(new Rect(area.x + x * scale, area.y, 1f, height * scale), Color.black);
            for (var y = 0; y <= height; y += 8)
                EditorGUI.DrawRect(new Rect(area.x, area.y + y * scale, width * scale, 1f), Color.black);
        }

        private bool ShowActivityMarker(ActivityEventPreviewMarker marker)
        {
            if (marker.Token == "C") return showCue;
            if (marker.Token == "RW") return showReward;
            if (marker.Token == "SP" || marker.Token == "RC" || marker.Token == "RS") return showSafeRecovery;
            return showMechanism;
        }

        private static Rect CellRect(Rect area, StarNight.Map.WorldGeneration.Domain.LocalTileCoord coordinate,
            int height, float scale) => new Rect(area.x + coordinate.X * scale,
            area.y + (height - coordinate.Y - 1) * scale, scale, scale);

        private static void DrawToken(Rect area, StarNight.Map.WorldGeneration.Domain.LocalTileCoord coordinate,
            int height, float scale, string token, Color color)
        {
            var rect = CellRect(area, coordinate, height, scale);
            EditorGUI.DrawRect(rect, color);
            if (scale >= 7f) GUI.Label(rect, token, EditorStyles.miniLabel);
        }

        private static Color MarkerColor(string token)
        {
            switch (token)
            {
                case "C": return new Color(0.95f, 0.90f, 0.25f);
                case "T": return new Color(0.95f, 0.65f, 0.20f);
                case "D": return new Color(0.75f, 0.40f, 0.95f);
                case "H": return new Color(0.95f, 0.20f, 0.20f);
                case "P": return new Color(0.95f, 0.35f, 0.15f);
                case "N": return new Color(0.25f, 0.80f, 0.70f);
                case "RW": return new Color(0.95f, 0.75f, 0.15f);
                case "SP": return new Color(0.25f, 0.85f, 0.45f);
                case "RC": return new Color(0.25f, 0.70f, 0.95f);
                case "RS": return new Color(0.55f, 0.70f, 0.95f);
                default: return Color.white;
            }
        }

        private void DrawProfileDetails()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profile / Compatibility", EditorStyles.boldLabel);
            var activity = content.ActivityCatalog.ById[new ActivityStructureId(selectedActivityId)];
            var profile = activity.PlacementProfile;
            EditorGUILayout.LabelField("Weight / Strength", profile.Weight + " / " + profile.Strength);
            EditorGUILayout.LabelField("Biome", string.Join(", ", profile.AllowedBiomes.Select(value => value.CanonicalId)));
            EditorGUILayout.LabelField("Pacing", string.Join(", ", profile.AllowedPacingRoles));
            EditorGUILayout.LabelField("Access", string.Join(", ", profile.AllowedAccessClasses));
            if (selectedEventId.Length == 0) return;
            var eventEntry = content.EventCatalog.ById[new EventOverlayId(selectedEventId)];
            EditorGUILayout.LabelField("Event kind / weight / gap",
                eventEntry.Contract.Kind + " / " + eventEntry.Profile.Weight + " / " +
                eventEntry.Profile.MinimumProgressionGap);
            EditorGUILayout.LabelField("Event source", CurrentResult.EventSnapshot.SourceOwnerSummary);
            EditorGUILayout.LabelField("Event operation / payload",
                CurrentResult.EventSnapshot.OperationSummary + " / " + CurrentResult.EventSnapshot.PayloadSummary);
        }

        private void DrawEvidenceDetails()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cue / Removal Evidence", EditorStyles.boldLabel);
            var active = CurrentResult.ActiveSnapshot;
            EditorGUILayout.LabelField("Cue before Activation",
                active.CueObservationOrdinal + " < " + active.ActivationBoundaryOrdinal);
            EditorGUILayout.LabelField("SafePocket / Recovery proofs",
                active.SafePocketProofCount + " / " + active.RecoveryProofCount);
            EditorGUILayout.LabelField("Exit / Reward preservation",
                active.ExitPreservationProofCount + " / " + active.RewardPreservationProofCount);
        }

        private void DrawDigestDetails()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Source / Compiler / Snapshot Digests", EditorStyles.boldLabel);
            DigestLine("Aggregate", CurrentResult.AggregateDigest);
            DigestLine("Activity catalog", CurrentResult.ActivityCatalogDigest);
            DigestLine("Event catalog", CurrentResult.EventCatalogDigest);
            DigestLine("Underlying", CurrentResult.StaticSnapshot.UnderlyingDigest);
            DigestLine("Static", CurrentResult.StaticSnapshot.StableDigest);
            DigestLine("Active", CurrentResult.ActiveSnapshot.StableDigest);
            DigestLine("Removed", CurrentResult.RemovedSnapshot.StableDigest);
            DigestLine("Event", CurrentResult.EventSnapshot.StableDigest);
            DigestLine("Comparison", CurrentResult.Comparison.StableDigest);
            DigestLine("Result", CurrentResult.StableDigest);
        }

        private static void DigestLine(string label, string value) =>
            EditorGUILayout.SelectableLabel(label + " " + value, EditorStyles.textField, GUILayout.Height(18f));
    }
}
