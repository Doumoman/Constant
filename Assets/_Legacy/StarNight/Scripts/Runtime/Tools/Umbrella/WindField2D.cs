#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools.Umbrella
{
    public enum WindResponse
    {
        Normal = 0,
        Umbrella = 1
    }

    public static class WindField2D
    {
        private static readonly List<WindZone2D> SortedZones =
            new List<WindZone2D>();

        public static Vector2 SampleAcceleration(
            Vector2 worldPoint,
            WindResponse response)
        {
            SortedZones.Clear();
            foreach (WindZone2D zone in WindZone2D.ActiveZones)
            {
                if (zone != null && zone.isActiveAndEnabled && zone.Contains(worldPoint))
                {
                    SortedZones.Add(zone);
                }
            }

            SortedZones.Sort(CompareZones);
            Vector2 total = Vector2.zero;
            for (int index = 0; index < SortedZones.Count; index++)
            {
                total += SortedZones[index].GetAcceleration(response);
            }

            return total;
        }

        private static int CompareZones(WindZone2D left, WindZone2D right)
        {
            int order = left.StableOrder.CompareTo(right.StableOrder);
            return order != 0
                ? order
                : left.GetInstanceID().CompareTo(right.GetInstanceID());
        }
    }
}

#endif
