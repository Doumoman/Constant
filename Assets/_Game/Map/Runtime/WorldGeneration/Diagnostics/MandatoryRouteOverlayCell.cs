using System;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class MandatoryRouteOverlayCell : IEquatable<MandatoryRouteOverlayCell>
    {
        internal MandatoryRouteOverlayCell(MandatoryRouteGraphCell cell, MandatoryRouteGraphNode node)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if (node == null || node.SectorIndex != cell.SectorIndex) throw new ArgumentException("Matching graph node required.", nameof(node));
            Index = cell.SectorIndex; Coordinate = cell.Coordinate; RouteType = cell.Mask.RouteType;
            OpenLeft = cell.OpenLeft; OpenRight = cell.OpenRight; OpenUp = cell.OpenUp; OpenDown = cell.OpenDown;
            if (RouteType == 4 && (!OpenUp || !OpenDown)) throw new ArgumentException("Type4 requires U+D.", nameof(cell));
            DisplayTypeToken = GetDisplayTypeToken(RouteType, OpenLeft, OpenRight, OpenUp, OpenDown);
            SideGlyph = (OpenLeft ? "L" : "-") + (OpenRight ? "R" : "-") + (OpenUp ? "U" : "-") + (OpenDown ? "D" : "-");
            DistanceFromStart = node.ShortestDistanceFromStart;
            TerminalRoleToken = node.TerminalSourceIds.Count == 0 ? string.Empty : string.Join("+", node.TerminalSourceIds);
            IsLoop = node.LoopSourceIds.Count != 0;
            DirectedEdgeCount = (OpenLeft ? 1 : 0) + (OpenRight ? 1 : 0) + (OpenUp ? 1 : 0) + (OpenDown ? 1 : 0);
            ValidationPassed = true; WarningToken = string.Empty;
            Label = string.Format(CultureInfo.InvariantCulture, "{0}\n{1} d{2}{3}", DisplayTypeToken, SideGlyph, DistanceFromStart, IsLoop ? " *" : string.Empty);
        }

        public int Index { get; } public SectorCoord Coordinate { get; } public int RouteType { get; }
        public bool OpenLeft { get; } public bool OpenRight { get; } public bool OpenUp { get; } public bool OpenDown { get; }
        public string DisplayTypeToken { get; } public string SideGlyph { get; } public int DistanceFromStart { get; }
        public string TerminalRoleToken { get; } public bool IsLoop { get; } public int DirectedEdgeCount { get; }
        public bool ValidationPassed { get; } public string WarningToken { get; } public string Label { get; }

        public static string GetDisplayTypeToken(int type, bool left, bool right, bool up, bool down)
        {
            if (type == 1 && left && right && !up && !down) return "T1";
            if (type == 2 && left && right && !up && down) return "T2";
            if (type == 3 && left && right && up && !down) return "T3";
            if (type == 4 && up && down) return "T4-" + (left ? "L" : string.Empty) + (right ? "R" : string.Empty) + "UD";
            throw new ArgumentException("Unsupported mandatory route mask.");
        }

        public bool Equals(MandatoryRouteOverlayCell other) => other != null && Index == other.Index && DisplayTypeToken == other.DisplayTypeToken && SideGlyph == other.SideGlyph && DistanceFromStart == other.DistanceFromStart && TerminalRoleToken == other.TerminalRoleToken && IsLoop == other.IsLoop;
        public override bool Equals(object obj) => Equals(obj as MandatoryRouteOverlayCell);
        public override int GetHashCode() { unchecked { var hash = Index; hash = hash * 397 ^ DisplayTypeToken.GetHashCode(); hash = hash * 397 ^ SideGlyph.GetHashCode(); hash = hash * 397 ^ DistanceFromStart; return hash; } }
    }
}
