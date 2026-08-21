#if LEGACY_DISABLED
using System.Globalization;
using System.Text;
using StarNight.Stage.Data;
using UnityEngine;

namespace StarNight.UI.HUD
{
    public static class HUDFormatting
    {
        public static string Money(int won)
        {
            return Mathf.Max(0, won).ToString("N0", CultureInfo.InvariantCulture) + "원";
        }

        public static string MoneyDelta(int won)
        {
            if (won == 0)
            {
                return string.Empty;
            }

            string sign = won > 0 ? "+" : "-";
            return sign + Mathf.Abs(won).ToString("N0", CultureInfo.InvariantCulture) + "원";
        }

        public static string Health(int current, int maximum = 4)
        {
            int slots = Mathf.Max(1, maximum);
            int filled = Mathf.Clamp(current, 0, slots);
            var builder = new StringBuilder(slots * 2);
            for (int index = 0; index < slots; index++)
            {
                if (index > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(index < filled ? '♥' : '♡');
            }
            return builder.ToString();
        }

        public static string Bells(BellPhase phase)
        {
            int filled = Mathf.Clamp((int)phase, 0, 3);
            var builder = new StringBuilder(5);
            for (int index = 0; index < 3; index++)
            {
                if (index > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(index < filled ? '●' : '○');
            }
            return builder.ToString();
        }

        public static string Direction(Vector2Int direction, bool exitInCurrentRoom)
        {
            if (exitInCurrentRoom) return "[문]";
            if (direction.x > 0) return ">";
            if (direction.x < 0) return "<";
            if (direction.y > 0) return "^";
            return "v";
        }

        public static string Consumable(string name, int count)
        {
            return count > 0 ? $"{name} {count}" : $"{name} ╱ 0";
        }
    }
}

#endif
