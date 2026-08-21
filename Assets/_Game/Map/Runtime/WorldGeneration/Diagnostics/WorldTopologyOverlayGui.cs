using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public static class WorldTopologyOverlayGui
    {
        public const int PanelOriginX = 12;
        public const int PanelOriginY = 12;
        public const int CellSize = 32;
        public const int GridColumns = WorldGenConstants.SectorColumns;
        public const int GridRows = WorldGenConstants.SectorRows;
        public const int GridPixelWidth = GridColumns * CellSize;
        public const int GridPixelHeight = GridRows * CellSize;
        public const int PanelPixelWidth = 440;
        public const int PanelPixelHeight = 564;
        public const string LegendText =
            "U Unassigned | M Mandatory | 0 Type0 | S Reserved | X Inactive";
        public const string EmptyHoverText = "Hover a sector for details.";
        public const string SmallViewportText =
            "World topology overlay requires 440 x 564 pixels.";

        private const int InnerX = PanelOriginX + 12;
        private const int TitleY = PanelOriginY + 6;
        private const int GridY = PanelOriginY + 32;
        private const int LegendY = GridY + GridPixelHeight + 4;
        private const int TooltipY = LegendY + 24;

        public static Rect PanelRect =>
            new Rect(PanelOriginX, PanelOriginY, PanelPixelWidth, PanelPixelHeight);

        public static Rect GridRect =>
            new Rect(InnerX, GridY, GridPixelWidth, GridPixelHeight);

        public static Rect GetCellRect(int index)
        {
            return GetCellRect(WorldGridIndex.ToCoordinate(index));
        }

        public static Rect GetCellRect(SectorCoord coordinate)
        {
            var index = WorldGridIndex.ToIndex(coordinate);
            var exactCoordinate = WorldGridIndex.ToCoordinate(index);
            var visualRow = GridRows - 1 - exactCoordinate.Y;
            return new Rect(
                InnerX + exactCoordinate.X * CellSize,
                GridY + visualRow * CellSize,
                CellSize,
                CellSize);
        }

        public static bool TryHitTest(Vector2 mousePosition, out int index)
        {
            var gridRect = GridRect;
            if (mousePosition.x < gridRect.xMin ||
                mousePosition.y < gridRect.yMin ||
                mousePosition.x >= gridRect.xMax ||
                mousePosition.y >= gridRect.yMax)
            {
                index = -1;
                return false;
            }

            var x = Mathf.FloorToInt((mousePosition.x - gridRect.xMin) / CellSize);
            var visualRow = Mathf.FloorToInt((mousePosition.y - gridRect.yMin) / CellSize);
            var y = GridRows - 1 - visualRow;
            index = WorldGridIndex.ToIndex(new SectorCoord(x, y));
            return true;
        }

        public static Color32 GetRoleColor(GeneratedSectorRole role)
        {
            switch (role)
            {
                case GeneratedSectorRole.Unassigned:
                    return new Color32(96, 96, 96, 230);
                case GeneratedSectorRole.Mandatory:
                    return new Color32(20, 150, 220, 230);
                case GeneratedSectorRole.Type0:
                    return new Color32(60, 180, 90, 230);
                case GeneratedSectorRole.ReservedSite:
                    return new Color32(235, 135, 35, 230);
                case GeneratedSectorRole.InactiveBuffer:
                    return new Color32(35, 35, 35, 230);
                default:
                    throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        public static void Draw(
            WorldTopologyOverlaySnapshot snapshot,
            Vector2 mousePosition,
            float viewportWidth,
            float viewportHeight)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var previousColor = GUI.color;
            var previousBackgroundColor = GUI.backgroundColor;
            var previousContentColor = GUI.contentColor;
            var previousEnabled = GUI.enabled;
            var previousMatrix = GUI.matrix;
            try
            {
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;
                GUI.enabled = true;
                GUI.matrix = Matrix4x4.identity;

                if (viewportWidth < PanelPixelWidth || viewportHeight < PanelPixelHeight)
                {
                    GUI.Label(
                        new Rect(
                            PanelOriginX,
                            PanelOriginY,
                            Mathf.Max(1f, viewportWidth - PanelOriginX * 2f),
                            40f),
                        SmallViewportText);
                    return;
                }

                var panelStyle = new GUIStyle(GUI.skin.box);
                var titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold
                };
                var cellStyle = new GUIStyle(GUI.skin.box);
                var labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                };
                var legendStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11
                };
                var tooltipStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = false,
                    fontSize = 11,
                    padding = new RectOffset(8, 8, 5, 5)
                };

                GUI.Box(PanelRect, GUIContent.none, panelStyle);
                GUI.Label(
                    new Rect(InnerX, TitleY, GridPixelWidth, 22f),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "MAP02 TOPOLOGY / Seed {0}",
                        snapshot.Seed),
                    titleStyle);

                for (var index = 0; index < snapshot.Count; index++)
                {
                    var cell = snapshot.GetCell(index);
                    var cellRect = GetCellRect(cell.Coordinate);
                    GUI.backgroundColor = GetRoleColor(cell.Role);
                    GUI.Box(cellRect, GUIContent.none, cellStyle);
                    GUI.backgroundColor = Color.white;
                    GUI.Label(cellRect, cell.CellLabel, labelStyle);
                }

                var hoverText = EmptyHoverText;
                if (TryHitTest(mousePosition, out var hoverIndex))
                {
                    var hoverCell = snapshot.GetCell(hoverIndex);
                    DrawOutline(GetCellRect(hoverCell.Coordinate), Color.white, 2f);
                    hoverText = hoverCell.Tooltip;
                }

                GUI.Label(
                    new Rect(InnerX, LegendY, GridPixelWidth, 20f),
                    LegendText,
                    legendStyle);
                GUI.Box(
                    new Rect(InnerX, TooltipY, GridPixelWidth, 76f),
                    hoverText,
                    tooltipStyle);
            }
            finally
            {
                GUI.color = previousColor;
                GUI.backgroundColor = previousBackgroundColor;
                GUI.contentColor = previousContentColor;
                GUI.enabled = previousEnabled;
                GUI.matrix = previousMatrix;
            }
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            var previousBackgroundColor = GUI.backgroundColor;
            try
            {
                GUI.backgroundColor = color;
                GUI.Box(new Rect(rect.x, rect.y, rect.width, thickness), GUIContent.none);
                GUI.Box(
                    new Rect(rect.x, rect.yMax - thickness, rect.width, thickness),
                    GUIContent.none);
                GUI.Box(new Rect(rect.x, rect.y, thickness, rect.height), GUIContent.none);
                GUI.Box(
                    new Rect(rect.xMax - thickness, rect.y, thickness, rect.height),
                    GUIContent.none);
            }
            finally
            {
                GUI.backgroundColor = previousBackgroundColor;
            }
        }
    }
}
