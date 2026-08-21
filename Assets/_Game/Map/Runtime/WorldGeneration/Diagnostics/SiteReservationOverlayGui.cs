using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public static class SiteReservationOverlayGui
    {
        public const int PanelOriginX = 12;
        public const int PanelOriginY = 12;
        public const int PanelPixelWidth = 1000;
        public const int PanelPixelHeight = 760;
        public const int TitleOriginX = 24;
        public const int TitleOriginY = 22;
        public const int GridOriginX = 24;
        public const int GridOriginY = 56;
        public const int CellSize = 44;
        public const int GridColumns = WorldGenConstants.SectorColumns;
        public const int GridRows = WorldGenConstants.SectorRows;
        public const int GridPixelWidth = 572;
        public const int GridPixelHeight = 572;
        public const int SidebarOriginX = 608;
        public const int SidebarOriginY = 56;
        public const int SidebarPixelWidth = 392;
        public const int SidebarPixelHeight = 704;
        public const int TooltipOriginX = 24;
        public const int TooltipOriginY = 640;
        public const int TooltipPixelWidth = 572;
        public const int TooltipPixelHeight = 120;
        public const int RequiredViewportWidth = 1024;
        public const int RequiredViewportHeight = 784;
        public const string EmptyHoverText = "Hover a sector for reservation details.";
        public const string SmallViewportText =
            "Site reservation overlay requires 1024 x 784 pixels.";
        public const string CoreWitnessLegendText =
            "Core outline = minimum expected witness, not painted biome";

        public static Rect PanelRect =>
            new Rect(PanelOriginX, PanelOriginY, PanelPixelWidth, PanelPixelHeight);

        public static Rect GridRect =>
            new Rect(GridOriginX, GridOriginY, GridPixelWidth, GridPixelHeight);

        public static Rect SidebarRect =>
            new Rect(SidebarOriginX, SidebarOriginY, SidebarPixelWidth, SidebarPixelHeight);

        public static Rect TooltipRect =>
            new Rect(TooltipOriginX, TooltipOriginY, TooltipPixelWidth, TooltipPixelHeight);

        public static Rect GetCellRect(SectorCoord coordinate)
        {
            var index = WorldGridIndex.ToIndex(coordinate);
            var exactCoordinate = WorldGridIndex.ToCoordinate(index);
            var visualRow = GridRows - 1 - exactCoordinate.Y;
            return new Rect(
                GridOriginX + exactCoordinate.X * CellSize,
                GridOriginY + visualRow * CellSize,
                CellSize,
                CellSize);
        }

        public static bool TryHitTest(
            SiteReservationOverlaySnapshot snapshot,
            Vector2 mousePosition,
            out SiteReservationOverlayCell cell)
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

        public static Color32 GetSiteColor(string sourceDefinitionId)
        {
            switch (sourceDefinitionId)
            {
                case "": return new Color32(60, 60, 68, 220);
                case "WORLD_MOONPALACE_V1": return new Color32(40, 170, 240, 235);
                case "SITE_MOON_BOSS_VAULT": return new Color32(220, 70, 70, 235);
                case "SITE_MOON_SEAL_FORGE": return new Color32(240, 145, 45, 235);
                case "SITE_CASSIA_SAP_HEART": return new Color32(70, 185, 105, 235);
                case "SITE_DEEP_STAR_YEAST": return new Color32(235, 205, 70, 235);
                case "SITE_MOON_CORE_METEOR": return new Color32(155, 95, 220, 235);
                case "SITE_PRIMARY_VILLAGE": return new Color32(65, 125, 235, 235);
                case null: throw new ArgumentNullException(nameof(sourceDefinitionId));
                default: throw new ArgumentException("Unknown site source definition ID.", nameof(sourceDefinitionId));
            }
        }

        public static string GetEntryArrowToken(SiteEntrySide side)
        {
            switch (side)
            {
                case SiteEntrySide.L: return "L:<";
                case SiteEntrySide.R: return "R:>";
                case SiteEntrySide.U: return "U:^";
                case SiteEntrySide.D: return "D:v";
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        public static void Draw(
            SiteReservationOverlaySnapshot snapshot,
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
                    fontSize = 10
                };
                var arrowStyle = new GUIStyle(GUI.skin.label)
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
                    new Rect(TitleOriginX, TitleOriginY, 960f, 24f),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "MAP03 SITE RESERVATION / Seed {0}",
                        snapshot.Seed),
                    titleStyle);

                for (var index = 0; index < snapshot.Count; index++)
                {
                    var cell = snapshot.GetCell(index);
                    var color = cell.IsReserved
                        ? GetSiteColor(cell.SourceDefinitionId)
                        : cell.IsCoreWitness
                            ? GetWitnessFill(snapshot, cell.CoreWitnessOwnerId.Value)
                            : GetSiteColor(string.Empty);
                    GUI.backgroundColor = color;
                    var cellRect = GetCellRect(cell.Coordinate);
                    GUI.Box(cellRect, GUIContent.none, cellStyle);
                    DrawOutline(cellRect, new Color32(104, 104, 112, 210), 1f);
                }

                GUI.backgroundColor = Color.white;
                for (var index = 0; index < snapshot.Count; index++)
                {
                    var cell = snapshot.GetCell(index);
                    GUI.Label(GetCellRect(cell.Coordinate), cell.CellLabel, cellLabelStyle);
                }

                for (var index = 0; index < snapshot.Count; index++)
                {
                    var cell = snapshot.GetCell(index);
                    if (!cell.IsCoreWitness) continue;
                    var ownerSource = GetOwnerSource(snapshot, cell.CoreWitnessOwnerId.Value);
                    var ownerColor = GetSiteColor(ownerSource);
                    ownerColor.a = 255;
                    DrawOutline(Inset(GetCellRect(cell.Coordinate), 3f), ownerColor, 2f);
                }

                for (var index = 0; index < snapshot.Count; index++)
                {
                    var cell = snapshot.GetCell(index);
                    var rect = GetCellRect(cell.Coordinate);
                    foreach (var side in cell.EntrySides)
                        GUI.Label(GetArrowRect(rect, side), GetEntryArrowToken(side), arrowStyle);
                }

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
            }
        }

        private static void DrawSidebar(
            SiteReservationOverlaySnapshot snapshot,
            GUIStyle swatchStyle,
            GUIStyle labelStyle,
            GUIStyle headerStyle)
        {
            GUI.Box(SidebarRect, GUIContent.none);
            var x = SidebarOriginX + 10f;
            var y = SidebarOriginY + 6f;
            GUI.Label(new Rect(x, y, 370f, 18f), "SITE LEGEND", headerStyle);
            y += 20f;
            for (var index = 0; index < 7; index++)
            {
                var source = GetLegendSource(index);
                GUI.backgroundColor = GetSiteColor(source);
                GUI.Box(new Rect(x, y + 2f, 16f, 14f), GUIContent.none, swatchStyle);
                GUI.backgroundColor = Color.white;
                GUI.Label(
                    new Rect(x + 22f, y, 350f, 18f),
                    SiteReservationOverlayCell.GetSiteGlyph(source) + "  " + source,
                    labelStyle);
                y += 18f;
            }

            y += 2f;
            GUI.Label(new Rect(x, y, 370f, 18f), CoreWitnessLegendText, labelStyle);
            y += 18f;
            GUI.Label(new Rect(x, y, 370f, 18f), "Entry arrows: L:<  R:>  U:^  D:v", labelStyle);
            y += 18f;
            GUI.Label(
                new Rect(x, y, 370f, 18f),
                "Classes: Candidate rejection / Final gate / Soft cost",
                labelStyle);
            y += 22f;
            GUI.Label(new Rect(x, y, 370f, 18f), "SUMMARY", headerStyle);
            y += 18f;
            GUI.Label(
                new Rect(x, y, 370f, 18f),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} reservations / {1} reserved / {2} entries",
                    snapshot.ReservationCount,
                    snapshot.ReservedSectorCount,
                    snapshot.EntryArrowCount),
                labelStyle);
            y += 16f;
            GUI.Label(
                new Rect(x, y, 370f, 18f),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} witnesses / {1} sectors / {2} rules",
                    snapshot.CoreWitnessCount,
                    snapshot.CoreWitnessSectorCount,
                    snapshot.PassedValidationRuleCount),
                labelStyle);
            y += 22f;
            GUI.Label(new Rect(x, y, 370f, 18f), "DIAGNOSTICS", headerStyle);
            y += 18f;
            foreach (var row in snapshot.DiagnosticRows)
            {
                GUI.Label(
                    new Rect(x, y, 370f, 17f),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[{0}] {1}: {2}",
                        GetClassToken(row.Class),
                        row.Label,
                        row.Value),
                    labelStyle);
                y += 17f;
            }
        }

        private static Color32 GetWitnessFill(
            SiteReservationOverlaySnapshot snapshot,
            SiteReservationId ownerId)
        {
            var color = GetSiteColor(GetOwnerSource(snapshot, ownerId));
            color.a = 72;
            return color;
        }

        private static string GetOwnerSource(
            SiteReservationOverlaySnapshot snapshot,
            SiteReservationId ownerId)
        {
            for (var index = 0; index < snapshot.Count; index++)
            {
                var cell = snapshot.GetCell(index);
                if (cell.ReservationId.HasValue && cell.ReservationId.Value == ownerId)
                    return cell.SourceDefinitionId;
            }
            throw new ArgumentException("Core witness owner is not present in the snapshot.", nameof(ownerId));
        }

        private static string GetLegendSource(int index)
        {
            switch (index)
            {
                case 0: return "WORLD_MOONPALACE_V1";
                case 1: return "SITE_MOON_BOSS_VAULT";
                case 2: return "SITE_MOON_SEAL_FORGE";
                case 3: return "SITE_CASSIA_SAP_HEART";
                case 4: return "SITE_DEEP_STAR_YEAST";
                case 5: return "SITE_MOON_CORE_METEOR";
                case 6: return "SITE_PRIMARY_VILLAGE";
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static string GetClassToken(SiteReservationOverlayDiagnosticClass value)
        {
            switch (value)
            {
                case SiteReservationOverlayDiagnosticClass.CandidateRejection: return "Candidate rejection";
                case SiteReservationOverlayDiagnosticClass.FinalGate: return "Final gate";
                case SiteReservationOverlayDiagnosticClass.SoftCost: return "Soft cost";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static Rect GetArrowRect(Rect rect, SiteEntrySide side)
        {
            switch (side)
            {
                case SiteEntrySide.L: return new Rect(rect.x, rect.y + 14f, 22f, 14f);
                case SiteEntrySide.R: return new Rect(rect.xMax - 22f, rect.y + 14f, 22f, 14f);
                case SiteEntrySide.U: return new Rect(rect.x + 10f, rect.y, 24f, 14f);
                case SiteEntrySide.D: return new Rect(rect.x + 10f, rect.yMax - 14f, 24f, 14f);
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private static Rect Inset(Rect rect, float inset)
        {
            return new Rect(
                rect.x + inset,
                rect.y + inset,
                rect.width - inset * 2f,
                rect.height - inset * 2f);
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            var previousBackgroundColor = GUI.backgroundColor;
            try
            {
                GUI.backgroundColor = color;
                GUI.Box(new Rect(rect.x, rect.y, rect.width, thickness), GUIContent.none);
                GUI.Box(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), GUIContent.none);
                GUI.Box(new Rect(rect.x, rect.y, thickness, rect.height), GUIContent.none);
                GUI.Box(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), GUIContent.none);
            }
            finally
            {
                GUI.backgroundColor = previousBackgroundColor;
            }
        }
    }
}
