#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Grid;
using StarNight.Tools;

namespace StarNight.Debugging
{
    public static class P3ToolGardenContract
    {
        public const int Width = 72;
        public const int Height = 30;
        public const int MainRouteY = 1;

        public static readonly GridPos Start = new GridPos(2, MainRouteY);
        public static readonly GridPos Exit = new GridPos(69, MainRouteY);

        private static readonly P3ToolKind[] RequiredOrder =
        {
            P3ToolKind.Rope,
            P3ToolKind.Bomb,
            P3ToolKind.Pickaxe,
            P3ToolKind.Shovel,
            P3ToolKind.WateringCan,
            P3ToolKind.Pestle,
            P3ToolKind.Grapple,
            P3ToolKind.WindUmbrella
        };

        public static IReadOnlyList<P3ToolKind> ToolOrder => RequiredOrder;

        public static bool ValidateToolFreeMainRoute(
            GridWorld world,
            out GridPos firstFailure)
        {
            firstFailure = default;
            if (world == null
                || world.Size.x != Width
                || world.Size.y != Height)
            {
                return false;
            }

            for (int x = Start.X; x <= Exit.X; x++)
            {
                GridPos floor = new GridPos(x, MainRouteY - 1);
                GridPos feet = new GridPos(x, MainRouteY);
                if (!world.IsSolid(floor)
                    || world.IsSolid(feet)
                    || world.IsHazard(feet))
                {
                    firstFailure = feet;
                    return false;
                }
            }

            return true;
        }
    }
}

#endif
