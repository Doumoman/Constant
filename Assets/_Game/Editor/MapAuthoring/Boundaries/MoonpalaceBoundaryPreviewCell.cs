using System;

namespace StarNight.MapAuthoring.Boundaries
{
    public sealed class MoonpalaceBoundaryPreviewCell : IComparable<MoonpalaceBoundaryPreviewCell>
    {
        public MoonpalaceBoundaryPreviewCell(
            int x,
            int y,
            int sourceX,
            int sourceY,
            string foregroundCode,
            string backgroundCode,
            string markerCode,
            bool showForeground,
            bool showBackground,
            bool showRoute,
            bool showSocket,
            bool showWarning,
            bool showBoundaryLayer,
            bool showIssue)
        {
            if (x < 0 || x >= 12) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= 8) throw new ArgumentOutOfRangeException(nameof(y));
            if (sourceX < 0 || sourceX >= 12) throw new ArgumentOutOfRangeException(nameof(sourceX));
            if (sourceY < 0 || sourceY >= 8) throw new ArgumentOutOfRangeException(nameof(sourceY));
            X = x;
            Y = y;
            SourceX = sourceX;
            SourceY = sourceY;
            ForegroundCode = foregroundCode ?? string.Empty;
            BackgroundCode = backgroundCode ?? string.Empty;
            MarkerCode = markerCode ?? string.Empty;
            ShowForeground = showForeground;
            ShowBackground = showBackground;
            ShowRoute = showRoute;
            ShowSocket = showSocket;
            ShowWarning = showWarning;
            ShowBoundaryLayer = showBoundaryLayer;
            ShowIssue = showIssue;
        }

        public int X { get; }
        public int Y { get; }
        public int SourceX { get; }
        public int SourceY { get; }
        public int RowMajorIndex => Y * 12 + X;
        public string ForegroundCode { get; }
        public string BackgroundCode { get; }
        public string MarkerCode { get; }
        public bool ShowForeground { get; }
        public bool ShowBackground { get; }
        public bool ShowRoute { get; }
        public bool ShowSocket { get; }
        public bool ShowWarning { get; }
        public bool ShowBoundaryLayer { get; }
        public bool ShowIssue { get; }

        public string OverlaySummary => string.Join(" | ", new[]
        {
            ShowForeground ? "FG:" + ForegroundCode : "FG:OFF",
            ShowBackground ? "BG:" + BackgroundCode : "BG:OFF",
            ShowRoute ? "ROUTE" : string.Empty,
            ShowSocket ? "SOCKET" : string.Empty,
            ShowWarning ? "WARNING" : string.Empty,
            ShowBoundaryLayer ? "BOUNDARY_LAYER" : string.Empty,
            ShowIssue ? "ISSUE" : string.Empty,
        });

        public int CompareTo(MoonpalaceBoundaryPreviewCell other)
        {
            return other == null ? 1 : RowMajorIndex.CompareTo(other.RowMajorIndex);
        }
    }
}
