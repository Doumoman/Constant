using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteReservationKind
    {
        Start,
        CoreResource,
        Forge,
        Boss,
        Village
    }

    public enum SiteFootprintTransform
    {
        R0,
        MirrorX,
        MirrorY,
        R180
    }

    public enum SiteEntrySide
    {
        L,
        R,
        U,
        D
    }

    public static class SiteReservationTokenCodec
    {
        public static bool TryParseKind(string token, out SiteReservationKind value)
        {
            switch (token)
            {
                case "START": value = SiteReservationKind.Start; return true;
                case "CORE_RESOURCE": value = SiteReservationKind.CoreResource; return true;
                case "FORGE": value = SiteReservationKind.Forge; return true;
                case "BOSS": value = SiteReservationKind.Boss; return true;
                case "VILLAGE": value = SiteReservationKind.Village; return true;
                default: value = default(SiteReservationKind); return false;
            }
        }

        public static bool TryParseTransform(string token, out SiteFootprintTransform value)
        {
            switch (token)
            {
                case "R0": value = SiteFootprintTransform.R0; return true;
                case "MIRROR_X": value = SiteFootprintTransform.MirrorX; return true;
                case "MIRROR_Y": value = SiteFootprintTransform.MirrorY; return true;
                case "R180": value = SiteFootprintTransform.R180; return true;
                default: value = default(SiteFootprintTransform); return false;
            }
        }

        public static bool TryParseEntrySide(string token, out SiteEntrySide value)
        {
            switch (token)
            {
                case "L": value = SiteEntrySide.L; return true;
                case "R": value = SiteEntrySide.R; return true;
                case "U": value = SiteEntrySide.U; return true;
                case "D": value = SiteEntrySide.D; return true;
                default: value = default(SiteEntrySide); return false;
            }
        }

        public static string ToToken(SiteReservationKind value)
        {
            switch (value)
            {
                case SiteReservationKind.Start: return "START";
                case SiteReservationKind.CoreResource: return "CORE_RESOURCE";
                case SiteReservationKind.Forge: return "FORGE";
                case SiteReservationKind.Boss: return "BOSS";
                case SiteReservationKind.Village: return "VILLAGE";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static string ToToken(SiteFootprintTransform value)
        {
            switch (value)
            {
                case SiteFootprintTransform.R0: return "R0";
                case SiteFootprintTransform.MirrorX: return "MIRROR_X";
                case SiteFootprintTransform.MirrorY: return "MIRROR_Y";
                case SiteFootprintTransform.R180: return "R180";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static string ToToken(SiteEntrySide value)
        {
            switch (value)
            {
                case SiteEntrySide.L: return "L";
                case SiteEntrySide.R: return "R";
                case SiteEntrySide.U: return "U";
                case SiteEntrySide.D: return "D";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static SiteEntrySide GetOpposite(SiteEntrySide side)
        {
            switch (side)
            {
                case SiteEntrySide.L: return SiteEntrySide.R;
                case SiteEntrySide.R: return SiteEntrySide.L;
                case SiteEntrySide.U: return SiteEntrySide.D;
                case SiteEntrySide.D: return SiteEntrySide.U;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        public static SiteEntrySide Opposite(SiteEntrySide side)
        {
            return GetOpposite(side);
        }

        public static void GetDelta(SiteEntrySide side, out int deltaX, out int deltaY)
        {
            switch (side)
            {
                case SiteEntrySide.L: deltaX = -1; deltaY = 0; return;
                case SiteEntrySide.R: deltaX = 1; deltaY = 0; return;
                case SiteEntrySide.U: deltaX = 0; deltaY = 1; return;
                case SiteEntrySide.D: deltaX = 0; deltaY = -1; return;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        public static int GetDeltaX(SiteEntrySide side)
        {
            GetDelta(side, out var deltaX, out _);
            return deltaX;
        }

        public static int GetDeltaY(SiteEntrySide side)
        {
            GetDelta(side, out _, out var deltaY);
            return deltaY;
        }
    }
}
