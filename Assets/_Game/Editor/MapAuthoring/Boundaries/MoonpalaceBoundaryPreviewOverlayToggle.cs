using System;

namespace StarNight.MapAuthoring.Boundaries
{
    [Flags]
    public enum MoonpalaceBoundaryPreviewOverlayToggle
    {
        None = 0,
        Foreground = 1 << 0,
        Background = 1 << 1,
        Route = 1 << 2,
        Sockets = 1 << 3,
        Warnings = 1 << 4,
        BoundaryLayer = 1 << 5,
        Issues = 1 << 6,
        All = Foreground | Background | Route | Sockets | Warnings | BoundaryLayer | Issues,
    }
}
