using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public static class BiomePatchOverlayGui
    {
        public const int PanelOriginX = 12;
        public const int PanelOriginY = 12;
        public const int PanelPixelWidth = 1200;
        public const int PanelPixelHeight = 820;
        public const int GridOriginX = 24;
        public const int GridOriginY = 56;
        public const int CellSize = 44;
        public const int GridColumns = WorldGenConstants.SectorColumns;
        public const int GridRows = WorldGenConstants.SectorRows;
        public const int GridPixelWidth = 572;
        public const int GridPixelHeight = 572;
        public const int SidebarOriginX = 612;
        public const int SidebarOriginY = 56;
        public const int SidebarPixelWidth = 564;
        public const int SidebarPixelHeight = 740;
        public const int TooltipOriginX = 24;
        public const int TooltipOriginY = 646;
        public const int TooltipPixelWidth = 572;
        public const int TooltipPixelHeight = 150;
        public const int RequiredViewportWidth = 1224;
        public const int RequiredViewportHeight = 844;
        public const string EmptyHoverText = "Hover a sector for biome patch details.";
        public const string SmallViewportText =
            "Biome patch overlay requires 1224 x 844 pixels.";

        public static Rect PanelRect =>
            new Rect(PanelOriginX, PanelOriginY, PanelPixelWidth, PanelPixelHeight);

        public static Rect GridRect =>
            new Rect(GridOriginX, GridOriginY, GridPixelWidth, GridPixelHeight);

        public static Rect SidebarRect =>
            new Rect(SidebarOriginX, SidebarOriginY, SidebarPixelWidth, SidebarPixelHeight);

        public static Rect TooltipRect =>
            new Rect(TooltipOriginX, TooltipOriginY, TooltipPixelWidth, TooltipPixelHeight);

        public static Color32 UnassignedColor => new Color32(60, 60, 68, 220);
        public static Color32 PatchBoundaryColor => new Color32(20, 20, 24, 255);
        public static Color32 CoreSiteMarkerColor => new Color32(255, 230, 80, 255);
        public static Color32 SeedMarkerColor => new Color32(245, 245, 245, 255);

        public static Rect GetCellRect(int index)
        {
            return GetCellRect(WorldGridIndex.ToCoordinate(index));
        }

        public static Rect GetCellRect(SectorCoord coordinate)
        {
            var index = WorldGridIndex.ToIndex(coordinate);
            var exact = WorldGridIndex.ToCoordinate(index);
            var visualRow = GridRows - 1 - exact.Y;
            return new Rect(
                GridOriginX + exact.X * CellSize,
                GridOriginY + visualRow * CellSize,
                CellSize,
                CellSize);
        }

        public static bool TryHitTest(
            BiomePatchOverlaySnapshot snapshot,
            Vector2 mousePosition,
            out BiomePatchOverlayCell cell)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var grid = GridRect;
            if (mousePosition.x < grid.xMin || mousePosition.y < grid.yMin ||
                mousePosition.x >= grid.xMax || mousePosition.y >= grid.yMax)
            {
                cell = null;
                return false;
            }

            var x = Mathf.FloorToInt((mousePosition.x - grid.xMin) / CellSize);
            var visualRow = Mathf.FloorToInt((mousePosition.y - grid.yMin) / CellSize);
            var y = GridRows - 1 - visualRow;
            cell = snapshot.GetCell(new SectorCoord(x, y));
            return true;
        }

        public static Color32 GetBiomeColor(string biomeId)
        {
            switch (biomeId)
            {
                case "BIO_MOON_CRATER": return new Color32(90, 145, 220, 235);
                case "BIO_CASSIA_ROOT": return new Color32(90, 180, 105, 235);
                case "BIO_ABANDONED_MILL": return new Color32(205, 135, 75, 235);
                case "BIO_MOON_DOUGH": return new Color32(190, 115, 205, 235);
                case null: throw new ArgumentNullException(nameof(biomeId));
                default: throw new ArgumentException("Unknown biome ID.", nameof(biomeId));
            }
        }

        public static string GetRoleGlyph(BiomePatchRole role)
        {
            switch (role)
            {
                case BiomePatchRole.Core: return "C";
                case BiomePatchRole.Satellite: return "S";
                case BiomePatchRole.Intrusion: return "I";
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        public static string FormatCompactness(int compactnessPermille)
        {
            if (compactnessPermille < 1 || compactnessPermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(compactnessPermille));
            return compactnessPermille.ToString(CultureInfo.InvariantCulture) + "/1000";
        }

        public static void Draw(
            BiomePatchOverlaySnapshot snapshot,
            Vector2 mousePosition,
            float viewportWidth,
            float viewportHeight)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var previousColor = GUI.color;
            var previousBackgroundColor = GUI.backgroundColor;
            var previousContentColor = GUI.contentColor;
            var previousEnabled = GUI.enabled;
            var previousMatrix = GUI.matrix;
            var previousDepth = GUI.depth;
            var previousChanged = GUI.changed;
            try
            {
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;
                GUI.enabled = true;
                GUI.matrix = Matrix4x4.identity;

                if (viewportWidth < RequiredViewportWidth || viewportHeight < RequiredViewportHeight)
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
                    fontStyle = FontStyle.Bold,
                    fontSize = 14
                };
                var cellStyle = new GUIStyle(GUI.skin.box);
                var cellLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 9
                };
                var sidebarStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 9,
                    clipping = TextClipping.Clip
                };
                var sidebarHeaderStyle = new GUIStyle(sidebarStyle)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 10
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
                    new Rect(GridOriginX, PanelOriginY + 10f, 1152f, 26f),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "MAP04 BIOME PATCHES / Seed {0}",
                        snapshot.WorldSeed),
                    titleStyle);

                for (var index = 0; index < snapshot.Cells.Count; index++)
                {
                    var cell = snapshot.GetCell(index);
                    var color = cell.IsAssigned
                        ? GetBiomeColor(cell.PrimaryBiomeId)
                        : UnassignedColor;
                    DrawFill(GetCellRect(cell.Coordinate), color);
                }

                GUI.backgroundColor = Color.white;
                for (var index = 0; index < snapshot.Cells.Count; index++)
                {
                    var cell = snapshot.GetCell(index);
                    var previous = GUI.contentColor;
                    GUI.contentColor = cell.IsCoreSiteCell
                        ? CoreSiteMarkerColor
                        : cell.IsSeed ? SeedMarkerColor : Color.white;
                    GUI.Label(GetCellRect(cell.Coordinate), cell.CellLabel, cellLabelStyle);
                    GUI.contentColor = previous;
                }

                for (var index = 0; index < snapshot.Cells.Count; index++)
                    DrawPatchBoundaries(snapshot.GetCell(index));

                var hoverText = EmptyHoverText;
                if (TryHitTest(snapshot, mousePosition, out var hoverCell))
                {
                    DrawOutline(GetCellRect(hoverCell.Coordinate), Color.white, 2f);
                    hoverText = hoverCell.Tooltip;
                }

                DrawSidebar(snapshot, cellStyle, sidebarStyle, sidebarHeaderStyle);
                GUI.Box(TooltipRect, hoverText, tooltipStyle);
            }
            finally
            {
                GUI.color = previousColor;
                GUI.backgroundColor = previousBackgroundColor;
                GUI.contentColor = previousContentColor;
                GUI.enabled = previousEnabled;
                GUI.matrix = previousMatrix;
                GUI.depth = previousDepth;
                GUI.changed = previousChanged;
            }
        }

        private static void DrawSidebar(
            BiomePatchOverlaySnapshot snapshot,
            GUIStyle swatchStyle,
            GUIStyle labelStyle,
            GUIStyle headerStyle)
        {
            GUI.Box(SidebarRect, GUIContent.none);
            var x = SidebarOriginX + 10f;
            var y = SidebarOriginY + 6f;
            GUI.Label(new Rect(x, y, 540f, 18f), "BIOME LEGEND", headerStyle);
            y += 20f;
            for (var index = 0; index < 4; index++)
            {
                var biomeId = GetLegendBiomeId(index);
                DrawFill(new Rect(x, y + 2f, 16f, 14f), GetBiomeColor(biomeId));
                GUI.Label(new Rect(x + 22f, y, 510f, 18f), biomeId, labelStyle);
                y += 18f;
            }

            GUI.Label(new Rect(x, y, 540f, 18f), "C Core | S Satellite | I Intrusion", labelStyle);
            y += 16f;
            GUI.Label(new Rect(x, y, 540f, 18f), "* Core site | + seed | dark line = PatchId boundary", labelStyle);
            y += 20f;
            GUI.Label(new Rect(x, y, 540f, 18f), "SUMMARY", headerStyle);
            y += 18f;
            GUI.Label(
                new Rect(x, y, 540f, 18f),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} patches / {1} assigned / {2} unassigned / {3} rules",
                    snapshot.Patches.Count,
                    snapshot.AssignedCount,
                    snapshot.UnassignedCount,
                    snapshot.PassedValidationRuleCount),
                labelStyle);
            y += 16f;
            GUI.Label(
                new Rect(x, y, 540f, 18f),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Roles C/S/I: {0}/{1}/{2}",
                    snapshot.CoreCount,
                    snapshot.SatelliteCount,
                    snapshot.IntrusionCount),
                labelStyle);
            y += 22f;
            GUI.Label(new Rect(x, y, 540f, 18f), "PATCHES", headerStyle);
            y += 18f;
            foreach (var row in snapshot.Patches)
            {
                GUI.Label(
                    new Rect(x, y, 540f, 14f),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} | {1} | {2}",
                        row.PatchId.Value,
                        row.BiomeId,
                        BiomePatchRoleTokenCodec.ToToken(row.Role)),
                    labelStyle);
                GUI.Label(
                    new Rect(x + 10f, y + 13f, 530f, 14f),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "size {0} | perimeter {1} | compact {2} | seeds {3} | Core sites {4}",
                        row.Size,
                        row.Perimeter,
                        FormatCompactness(row.CompactnessPermille),
                        row.SeedCount,
                        row.CoreSiteCellCount),
                    labelStyle);
                y += 28f;
            }
        }

        private static void DrawPatchBoundaries(BiomePatchOverlayCell cell)
        {
            var rect = GetCellRect(cell.Coordinate);
            var color = PatchBoundaryColor;
            const float thickness = 2f;
            if (cell.BorderLeft)
                DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            if (cell.BorderRight)
                DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
            if (cell.BorderUp)
                DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            if (cell.BorderDown)
                DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            var previous = GUI.backgroundColor;
            try
            {
                GUI.backgroundColor = color;
                GUI.Box(rect, GUIContent.none);
            }
            finally
            {
                GUI.backgroundColor = previous;
            }
        }

        private static void DrawFill(Rect rect, Color color)
        {
            var previous = GUI.color;
            try
            {
                GUI.color = color;
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            }
            finally
            {
                GUI.color = previous;
            }
        }

        private static string GetLegendBiomeId(int index)
        {
            switch (index)
            {
                case 0: return "BIO_MOON_CRATER";
                case 1: return "BIO_CASSIA_ROOT";
                case 2: return "BIO_ABANDONED_MILL";
                case 3: return "BIO_MOON_DOUGH";
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
