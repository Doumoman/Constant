#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Tools.Grapple
{
    public static class GrappleAim8
    {
        private static readonly Vector2[] Directions =
        {
            Vector2.right,
            new Vector2(1f, 1f).normalized,
            Vector2.up,
            new Vector2(-1f, 1f).normalized,
            Vector2.left,
            new Vector2(-1f, -1f).normalized,
            Vector2.down,
            new Vector2(1f, -1f).normalized
        };

        public static Vector2 Quantize(Vector2 rawAim)
        {
            if (rawAim.sqrMagnitude <= 0.0001f)
            {
                return Vector2.right;
            }

            float angle = Mathf.Atan2(rawAim.y, rawAim.x) * Mathf.Rad2Deg;
            int index = Mathf.RoundToInt(angle / 45f);
            index = (index % 8 + 8) % 8;
            return Directions[index];
        }
    }
}

#endif
