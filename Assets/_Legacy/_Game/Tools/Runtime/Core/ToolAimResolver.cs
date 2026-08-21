#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Tools.Core
{
    public readonly struct ToolAimSolution
    {
        public ToolAimSolution(Vector2Int originCell, Vector2Int targetCell, Vector2Int direction)
        {
            OriginCell = originCell;
            TargetCell = targetCell;
            Direction = direction;
        }

        public Vector2Int OriginCell { get; }
        public Vector2Int TargetCell { get; }
        public Vector2Int Direction { get; }
    }

    public static class ToolAimResolver
    {
        public static ToolAimSolution Resolve(
            ToolActionProfile profile,
            Vector2Int originCell,
            int facingSign,
            float lookVertical)
        {
            int facing = facingSign < 0 ? -1 : 1;
            Vector2Int direction = profile != null && profile.AimMode == ToolAimMode.DownAutomatic
                ? Vector2Int.down
                : profile != null && profile.AimMode == ToolAimMode.UpOrFacing && lookVertical > 0.5f
                    ? Vector2Int.up
                    : new Vector2Int(facing, 0);
            return new ToolAimSolution(originCell, originCell + direction, direction);
        }

        public static Vector2Int WorldToCell(Vector2 worldPosition, Vector2 gridOrigin, float cellSize)
        {
            Vector2 local = (worldPosition - gridOrigin) / Mathf.Max(0.01f, cellSize);
            return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
        }
    }
}

#endif
