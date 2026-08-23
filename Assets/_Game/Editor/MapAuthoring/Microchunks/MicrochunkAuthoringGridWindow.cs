using System;
using StarNight.Map.WorldGeneration.Microchunks;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkAuthoringGridWindow : EditorWindow
    {
        private const float CellSize = 42f;

        [NonSerialized] private MicrochunkAuthoringGridViewModel viewModel;
        [NonSerialized] private Vector2 scrollPosition;

        public MicrochunkAuthoringGridViewModel ViewModel =>
            viewModel ?? (viewModel = new MicrochunkAuthoringGridViewModel());

        [MenuItem("Tools/Map/Microchunk Authoring Grid")]
        public static MicrochunkAuthoringGridWindow Open()
        {
            var window = GetWindow<MicrochunkAuthoringGridWindow>();
            window.titleContent = new GUIContent("Microchunk Grid");
            window.minSize = new Vector2(620f, 520f);
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            if (viewModel == null)
            {
                viewModel = new MicrochunkAuthoringGridViewModel();
            }
        }

        private void OnGUI()
        {
            DrawPalette();
            EditorGUILayout.Space(4f);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawGrid();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4f);
            DrawCommandsAndValidation();
        }

        private void DrawPalette()
        {
            EditorGUILayout.LabelField("12 x 8 Microchunk Authoring Grid", EditorStyles.boldLabel);
            var layerNames = new string[MicrochunkAuthoringGridLayer.Count];
            for (var index = 0; index < layerNames.Length; index++)
            {
                layerNames[index] = MicrochunkAuthoringGridLayer.At(index).ToString();
            }

            var selectedIndex = MicrochunkAuthoringGridLayer.IndexOf(ViewModel.Palette.SelectedLayer);
            var nextIndex = EditorGUILayout.Popup("Layer", selectedIndex, layerNames);
            if (nextIndex != selectedIndex)
            {
                ViewModel.Palette.SelectLayer(MicrochunkAuthoringGridLayer.At(nextIndex));
            }

            var tileCode = EditorGUILayout.TextField("Tile Code", ViewModel.Palette.SelectedTileCode);
            if (!string.Equals(tileCode, ViewModel.Palette.SelectedTileCode, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(tileCode) &&
                string.Equals(tileCode, tileCode.Trim(), StringComparison.Ordinal))
            {
                ViewModel.Palette.SelectTileCode(tileCode);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("NONE / Erase", GUILayout.Width(120f)))
            {
                ViewModel.Palette.SelectErase();
            }

            foreach (var swatch in ViewModel.Palette.Swatches)
            {
                if (GUILayout.Button(swatch, GUILayout.MinWidth(70f)))
                {
                    ViewModel.Palette.SelectTileCode(swatch);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            for (var y = MicrochunkConstants.HeightTiles - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("y=" + y, GUILayout.Width(32f));
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    var code = ViewModel.State.GetTileCode(x, y, ViewModel.Palette.SelectedLayer);
                    var label = string.Equals(code, MicrochunkAuthoringGridCell.EmptyTileCode, StringComparison.Ordinal)
                        ? x + "," + y
                        : code;
                    if (GUILayout.Button(label, GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                    {
                        ViewModel.PaintCell(x, y);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawCommandsAndValidation()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Selected Layer"))
            {
                ViewModel.ClearSelectedLayer();
            }
            if (GUILayout.Button("Clear All Layers"))
            {
                ViewModel.ClearAllLayers();
            }
            EditorGUILayout.EndHorizontal();

            var summary = ViewModel.Validate();
            EditorGUILayout.HelpBox(
                string.Format(
                    "Coverage: {0}/96, layer violations: {1}, total issues: {2}",
                    summary.CoverageResult.InRangeUniqueCoordinateCount,
                    summary.TileLayerResult.ViolationCount,
                    summary.IssueCount),
                summary.Success ? MessageType.Info : MessageType.Warning);
        }
    }
}
