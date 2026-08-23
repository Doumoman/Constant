using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkSocketAndSlotEditorWindow : EditorWindow
    {
        [NonSerialized] private MicrochunkSocketAndSlotEditorViewModel viewModel;
        [NonSerialized] private Vector2 scrollPosition;
        [NonSerialized] private string lastError = string.Empty;

        public MicrochunkSocketAndSlotEditorViewModel ViewModel =>
            viewModel ?? (viewModel = new MicrochunkSocketAndSlotEditorViewModel());

        [MenuItem("Tools/Map/Microchunk Socket and Slot Editor")]
        public static MicrochunkSocketAndSlotEditorWindow Open()
        {
            var window = GetWindow<MicrochunkSocketAndSlotEditorWindow>();
            window.titleContent = new GUIContent("Microchunk Sockets & Slots");
            window.minSize = new Vector2(720f, 560f);
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            if (viewModel == null) viewModel = new MicrochunkSocketAndSlotEditorViewModel();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("In-memory Microchunk Socket / Band / Slot Authoring", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This editor projects onto the 12 x 8 authoring grid and uses the existing socket-edge and object-slot validators.",
                MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawBands();
            EditorGUILayout.Space(8f);
            DrawSockets();
            EditorGUILayout.Space(8f);
            DrawObjectSlots();
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(lastError))
            {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            }
            DrawValidationSummary();
        }

        private void DrawBands()
        {
            EditorGUILayout.LabelField("Socket Bands", EditorStyles.boldLabel);
            var rows = ViewModel.SocketAuthoring.Bands;
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var bandId = EditorGUILayout.TextField("Band ID", row.BandId);
                var side = EditorGUILayout.TextField("Side (L/R/D/U)", row.SideToken);
                var start = EditorGUILayout.IntField("Inclusive Start", row.InclusiveStart);
                var end = EditorGUILayout.IntField("Inclusive End", row.InclusiveEnd);
                var clearance = EditorGUILayout.IntField("Minimum Clearance", row.MinimumClearanceTiles);
                if (Changed(row.BandId, bandId) || Changed(row.SideToken, side) ||
                    start != row.InclusiveStart || end != row.InclusiveEnd || clearance != row.MinimumClearanceTiles)
                {
                    TryCommand(() => ViewModel.SocketAuthoring.ReplaceBand(
                        index,
                        new MicrochunkSocketBandAuthoringRow(bandId, side, start, end, clearance)));
                }
                if (DrawRowCommands(
                    index,
                    rows.Count,
                    () => ViewModel.SocketAuthoring.MoveBand(index, index - 1),
                    () => ViewModel.SocketAuthoring.MoveBand(index, index + 1),
                    () => ViewModel.SocketAuthoring.DuplicateBand(row.BandId, UniqueBandId(row.BandId)),
                    () => ViewModel.SocketAuthoring.RemoveBand(row.BandId)))
                {
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Band"))
            {
                TryCommand(() => ViewModel.SocketAuthoring.AddBand(
                    new MicrochunkSocketBandAuthoringRow(UniqueBandId("BAND"), "L", 0, 0)));
            }
        }

        private void DrawSockets()
        {
            EditorGUILayout.LabelField("Sockets", EditorStyles.boldLabel);
            var rows = ViewModel.SocketAuthoring.Sockets;
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var socketId = EditorGUILayout.TextField("Socket ID", row.SocketId);
                var side = EditorGUILayout.TextField("Side (L/R/D/U)", row.SideToken);
                var bandId = EditorGUILayout.TextField("Band ID", row.BandId);
                var traversal = EditorGUILayout.TextField("Traversal Kind", row.TraversalKindToken);
                var signature = EditorGUILayout.TextField("Edge Signature ID", row.EdgeSignatureId);
                var mandatory = EditorGUILayout.Toggle("Mandatory Allowed", row.MandatoryAllowed);
                var tool = EditorGUILayout.TextField("Tool Requirement", row.ToolRequirementToken);
                if (Changed(row.SocketId, socketId) || Changed(row.SideToken, side) || Changed(row.BandId, bandId) ||
                    Changed(row.TraversalKindToken, traversal) || Changed(row.EdgeSignatureId, signature) ||
                    mandatory != row.MandatoryAllowed || Changed(row.ToolRequirementToken, tool))
                {
                    TryCommand(() => ViewModel.SocketAuthoring.ReplaceSocket(
                        index,
                        new MicrochunkSocketAuthoringRow(
                            socketId, side, bandId, traversal, signature, mandatory, tool)));
                }
                if (DrawRowCommands(
                    index,
                    rows.Count,
                    () => ViewModel.SocketAuthoring.MoveSocket(index, index - 1),
                    () => ViewModel.SocketAuthoring.MoveSocket(index, index + 1),
                    () => ViewModel.SocketAuthoring.DuplicateSocket(row.SocketId, UniqueSocketId(row.SocketId)),
                    () => ViewModel.SocketAuthoring.RemoveSocket(row.SocketId)))
                {
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Socket"))
            {
                TryCommand(() => ViewModel.SocketAuthoring.AddSocket(
                    new MicrochunkSocketAuthoringRow(
                        UniqueSocketId("SOCKET"), "L", FirstBandId(), "WALK", "EDGE_EDITOR_WALK")));
            }
        }

        private void DrawObjectSlots()
        {
            EditorGUILayout.LabelField("Object Slots", EditorStyles.boldLabel);
            var rows = ViewModel.ObjectSlotAuthoring.Rows;
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var slotId = EditorGUILayout.TextField("Slot ID", row.SlotId);
                var x = EditorGUILayout.IntField("Anchor X", row.Anchor.X);
                var y = EditorGUILayout.IntField("Anchor Y", row.Anchor.Y);
                var category = EditorGUILayout.TextField("Category", row.CategoryToken);
                var poolId = EditorGUILayout.TextField("Pool ID", row.PoolId);
                var orientation = EditorGUILayout.TextField("Orientation", row.OrientationToken);
                var radius = EditorGUILayout.IntField("Safety Radius", row.SafetyRadiusTiles);
                var required = EditorGUILayout.Toggle("Required", row.Required);
                var visible = EditorGUILayout.Toggle("Visible From Route", row.VisibleFromRoute);
                if (Changed(row.SlotId, slotId) || x != row.Anchor.X || y != row.Anchor.Y ||
                    Changed(row.CategoryToken, category) || Changed(row.PoolId, poolId) ||
                    Changed(row.OrientationToken, orientation) || radius != row.SafetyRadiusTiles ||
                    required != row.Required || visible != row.VisibleFromRoute)
                {
                    TryCommand(() => ViewModel.ObjectSlotAuthoring.Replace(
                        index,
                        new MicrochunkObjectSlotAuthoringRow(
                            slotId, x, y, category, poolId, orientation, radius, required, visible,
                            row.RequiredMarkerCode)));
                }
                if (DrawRowCommands(
                    index,
                    rows.Count,
                    () => ViewModel.ObjectSlotAuthoring.Move(index, index - 1),
                    () => ViewModel.ObjectSlotAuthoring.Move(index, index + 1),
                    () => ViewModel.ObjectSlotAuthoring.Duplicate(row.SlotId, UniqueSlotId(row.SlotId)),
                    () => ViewModel.ObjectSlotAuthoring.Remove(row.SlotId)))
                {
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Object Slot"))
            {
                TryCommand(() => ViewModel.ObjectSlotAuthoring.Add(
                    new MicrochunkObjectSlotAuthoringRow(
                        UniqueSlotId("SLOT"), 0, 0, "RESOURCE", "POOL_EDITOR_RESOURCE")));
            }
        }

        private void DrawValidationSummary()
        {
            try
            {
                var summary = ViewModel.Validate();
                EditorGUILayout.HelpBox(
                    string.Format(
                        "Socket issues: {0}; object-slot issues: {1}; total issues: {2}",
                        summary.SocketResult.IssueCount,
                        summary.ObjectSlotResult.IssueCount,
                        summary.IssueCount),
                    summary.Success ? MessageType.Info : MessageType.Warning);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox("Validation could not project the current row values: " + exception.Message, MessageType.Error);
            }
        }

        private static bool DrawRowCommands(
            int index,
            int count,
            Action moveUp,
            Action moveDown,
            Action duplicate,
            Action remove)
        {
            var changed = false;
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button("Up")) { moveUp(); changed = true; }
            }
            using (new EditorGUI.DisabledScope(index >= count - 1))
            {
                if (GUILayout.Button("Down")) { moveDown(); changed = true; }
            }
            if (GUILayout.Button("Duplicate")) { duplicate(); changed = true; }
            if (GUILayout.Button("Remove")) { remove(); changed = true; }
            EditorGUILayout.EndHorizontal();
            return changed;
        }

        private void TryCommand(Action command)
        {
            try
            {
                command();
                lastError = string.Empty;
            }
            catch (Exception exception)
            {
                lastError = exception.Message;
            }
        }

        private string FirstBandId()
        {
            return ViewModel.SocketAuthoring.Bands.Count > 0
                ? ViewModel.SocketAuthoring.Bands[0].BandId
                : "BAND_EDITOR";
        }

        private string UniqueSocketId(string prefix)
        {
            return UniqueId(prefix, candidate => ViewModel.SocketAuthoring.Sockets.Any(row => row.SocketId == candidate));
        }

        private string UniqueBandId(string prefix)
        {
            return UniqueId(prefix, candidate => ViewModel.SocketAuthoring.Bands.Any(row => row.BandId == candidate));
        }

        private string UniqueSlotId(string prefix)
        {
            return UniqueId(prefix, candidate => ViewModel.ObjectSlotAuthoring.Rows.Any(row => row.SlotId == candidate));
        }

        private static string UniqueId(string prefix, Func<string, bool> exists)
        {
            for (var ordinal = 1; ; ordinal++)
            {
                var candidate = prefix + "_" + ordinal.ToString("D2");
                if (!exists(candidate)) return candidate;
            }
        }

        private static bool Changed(string before, string after)
        {
            return !string.Equals(before, after, StringComparison.Ordinal);
        }
    }
}
