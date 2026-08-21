#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [Serializable]
    public sealed class RopeSnapshot
    {
        public string RuntimeId;
        public RopeAnchorKind AnchorKind;
        public Vector2Int AnchorCell;
        public List<Vector2Int> RemainingSegmentCells = new List<Vector2Int>();
    }

    public static class RopeInstallationRegistry
    {
        private static readonly List<RopeInstallationRuntime> Installations = new List<RopeInstallationRuntime>();

        public static void Register(RopeInstallationRuntime installation)
        {
            if (installation != null && !Installations.Contains(installation))
            {
                Installations.Add(installation);
            }
        }

        public static void Unregister(RopeInstallationRuntime installation) => Installations.Remove(installation);

        public static RopeInstallationRuntime FindInColumn(int columnX, RectInt roomBounds)
        {
            for (int index = Installations.Count - 1; index >= 0; index--)
            {
                RopeInstallationRuntime installation = Installations[index];
                if (installation == null)
                {
                    Installations.RemoveAt(index);
                    continue;
                }
                Vector2Int anchor = installation.AnchorCell;
                if (anchor.x == columnX
                    && (roomBounds.width <= 0 || roomBounds.height <= 0 || roomBounds.Contains(anchor)))
                {
                    return installation;
                }
            }
            return null;
        }
    }
}

#endif
