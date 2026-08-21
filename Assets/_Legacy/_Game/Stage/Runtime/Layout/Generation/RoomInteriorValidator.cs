#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    public static class RoomInteriorValidator
    {
        public static IReadOnlyList<string> Validate(RoomInteriorLayout layout)
        {
            var errors = new List<string>();
            if (layout == null)
            {
                errors.Add("[ROOM] Layout is null.");
                return errors;
            }

            int expectedCount = Mathf.Max(0, layout.ChunkGridSize.x * layout.ChunkGridSize.y);
            if (layout.Chunks.Count != expectedCount)
            {
                errors.Add($"[CHUNK] Expected {expectedCount} chunks but found {layout.Chunks.Count}.");
            }

            var chunkCells = new HashSet<Vector2Int>();
            for (int index = 0; index < layout.Chunks.Count; index++)
            {
                GeneratedMicroChunk chunk = layout.Chunks[index];
                if (chunk == null || !chunkCells.Add(chunk.GridCell))
                {
                    errors.Add($"[CHUNK] Missing or duplicate chunk at index {index}.");
                    continue;
                }
                Vector2Int expectedOrigin = new Vector2Int(
                    chunk.GridCell.x * GeneratedMicroChunk.Width,
                    chunk.GridCell.y * GeneratedMicroChunk.Height);
                if (chunk.OriginCell != expectedOrigin)
                {
                    errors.Add($"[CHUNK] {chunk.GridCell} origin {chunk.OriginCell} must be {expectedOrigin}.");
                }
                ValidateSockets(layout, chunk, errors);
            }

            ValidateRoleSequence(layout, errors);
            ValidatePatternFrequency(layout, errors);
            ValidateMainRoute(layout, errors);
            ValidateSoftSoilRemovalSafety(layout, errors);
            ValidateHiddenContentAndToolEscape(layout, errors);
            return errors;
        }

        private static void ValidateSockets(
            RoomInteriorLayout layout,
            GeneratedMicroChunk chunk,
            ICollection<string> errors)
        {
            for (int index = 0; index < chunk.Sockets.Count; index++)
            {
                GeneratedMicroSocket socket = chunk.Sockets[index];
                if (socket == null || !IsValidSocketCell(socket.Side, socket.LocalCell))
                {
                    errors.Add($"[SOCKET] {chunk.GridCell} has an invalid one-cell socket.");
                    continue;
                }
                if (chunk.GetCell(socket.LocalCell) != MicroCellKind.Empty ||
                    chunk.GetCell(GetInnerCell(socket.Side, socket.LocalCell)) != MicroCellKind.Empty)
                {
                    errors.Add($"[SOCKET] {chunk.GridCell}/{socket.Side} aperture and inner cell must be Empty.");
                }
                for (int y = 0; y < GeneratedMicroChunk.Height; y++)
                {
                    for (int x = 0; x < GeneratedMicroChunk.Width; x++)
                    {
                        Vector2Int localCell = new Vector2Int(x, y);
                        Vector2Int delta = localCell - socket.LocalCell;
                        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) <=
                            StarNight.Stage.Transitions.RoomPortalContract.PortalPaddingCells &&
                            chunk.GetCell(localCell) == MicroCellKind.Hazard)
                        {
                            errors.Add($"[SOCKET] {chunk.GridCell}/{socket.Side} has a damage cell inside two-cell PortalPadding.");
                        }
                    }
                }
                if (socket.External) continue;

                GeneratedMicroChunk neighbor = layout.FindChunk(socket.NeighborChunk);
                GeneratedMicroSocket counterpart = neighbor?.Sockets.FirstOrDefault(candidate =>
                    candidate != null && !candidate.External && candidate.NeighborChunk == chunk.GridCell &&
                    candidate.Side == Opposite(socket.Side));
                if (counterpart == null || GetAxisOffset(socket) != GetAxisOffset(counterpart))
                {
                    errors.Add($"[SOCKET] {chunk.GridCell}/{socket.Side} has no aligned counterpart.");
                }
            }
        }

        private static void ValidateRoleSequence(RoomInteriorLayout layout, ICollection<string> errors)
        {
            List<GeneratedMicroChunk> ordered = layout.Chunks
                .Where(chunk => chunk != null)
                .OrderBy(chunk => chunk.GenerationOrder)
                .ToList();
            int nonPlainRun = 0;
            for (int index = 0; index < ordered.Count; index++)
            {
                GeneratedMicroChunk current = ordered[index];
                GeneratedMicroChunk previous = index > 0 ? ordered[index - 1] : null;
                if (previous != null && current.Role == previous.Role &&
                    (current.Role == ChunkPatternRole.Damage || current.Role == ChunkPatternRole.Puzzle))
                {
                    errors.Add($"[ROLE] Consecutive {current.Role} chunks are forbidden.");
                }
                if (current.Role == ChunkPatternRole.Condition && current.MainRoute)
                {
                    errors.Add("[ROLE] Condition cannot block the main route.");
                }
                nonPlainRun = current.Role == ChunkPatternRole.Plain ? 0 : nonPlainRun + 1;
                if (nonPlainRun > 2)
                {
                    errors.Add("[ROLE] Plain is required after two non-Plain chunks.");
                }
            }

            int mainCount = ordered.Count(chunk => chunk.MainRoute);
            int mainDamage = ordered.Count(chunk => chunk.MainRoute && chunk.Role == ChunkPatternRole.Damage);
            if (mainCount > 0 && mainDamage / (float)mainCount > 0.2f + 0.0001f)
            {
                errors.Add("[ROLE] Main-route Damage ratio exceeds 20%.");
            }
        }

        private static void ValidatePatternFrequency(RoomInteriorLayout layout, ICollection<string> errors)
        {
            foreach (IGrouping<string, GeneratedMicroChunk> group in layout.Chunks
                         .Where(chunk => chunk != null)
                         .GroupBy(chunk => chunk.PatternId ?? string.Empty, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(group.Key) || group.Count() > 2)
                {
                    errors.Add($"[PATTERN] Pattern '{group.Key}' appears {group.Count()} times; maximum is 2.");
                }
            }
        }

        private static void ValidateMainRoute(RoomInteriorLayout layout, ICollection<string> errors)
        {
            if (!IsWalkable(layout.GetWorldCell(layout.EntryWorldCell)) ||
                !IsWalkable(layout.GetWorldCell(layout.ExitWorldCell)))
            {
                errors.Add("[T0] Entry or Exit is blocked.");
                return;
            }

            var visited = new HashSet<Vector2Int> { layout.EntryWorldCell };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(layout.EntryWorldCell);
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                for (int index = 0; index < directions.Length; index++)
                {
                    Vector2Int next = current + directions[index];
                    if (next.x < 0 || next.y < 0 || next.x >= layout.SizeCells.x || next.y >= layout.SizeCells.y ||
                        !IsWalkable(layout.GetWorldCell(next)) || !visited.Add(next))
                    {
                        continue;
                    }
                    queue.Enqueue(next);
                }
            }

            if (!visited.Contains(layout.ExitWorldCell))
            {
                errors.Add("[T0] Entry cannot reach Exit through the hazard-free basic route.");
            }
            foreach (GeneratedMicroChunk chunk in layout.Chunks.Where(chunk => chunk != null && chunk.MainRoute))
            {
                Vector2Int center = chunk.OriginCell + new Vector2Int(3, 3);
                if (!visited.Contains(center)) errors.Add($"[T0] Main chunk {chunk.GridCell} is disconnected.");
            }
        }

        private static bool IsWalkable(MicroCellKind kind)
        {
            return kind != MicroCellKind.Solid && kind != MicroCellKind.SoftSoil && kind != MicroCellKind.Hazard;
        }

        private static void ValidateSoftSoilRemovalSafety(
            RoomInteriorLayout layout,
            ICollection<string> errors)
        {
            int maxX = layout.SizeCells.x - 1;
            int maxY = layout.SizeCells.y - 1;
            for (int y = 0; y <= maxY; y++)
            {
                for (int x = 0; x <= maxX; x++)
                {
                    if (layout.GetWorldCell(new Vector2Int(x, y)) != MicroCellKind.SoftSoil)
                    {
                        continue;
                    }
                    if (y == 0)
                    {
                        errors.Add($"[SOIL] Removing soil at ({x},{y}) would expose VoidRecoveryZone.");
                    }
                    else if (x == 0 || x == maxX || y == maxY)
                    {
                        errors.Add($"[SOIL] Removing soil at ({x},{y}) would expose UnbreakableBoundary.");
                    }
                }
            }

            Vector2Int[] portalFloorSupport =
            {
                layout.EntryWorldCell + Vector2Int.down,
                layout.EntryWorldCell + Vector2Int.right + Vector2Int.down,
                layout.ExitWorldCell + Vector2Int.down,
                layout.ExitWorldCell + Vector2Int.left + Vector2Int.down,
            };
            for (int index = 0; index < portalFloorSupport.Length; index++)
            {
                Vector2Int support = portalFloorSupport[index];
                if (layout.GetWorldCell(support) == MicroCellKind.SoftSoil)
                {
                    errors.Add($"[SOIL] Portal safe floor cannot depend on removable soil at {support}.");
                }
            }

            if (!CanReachExitAfterRemovingAllSoil(layout))
            {
                errors.Add("[SOIL] Main exit route is lost after removing all soil.");
            }
        }

        private static bool CanReachExitAfterRemovingAllSoil(RoomInteriorLayout layout)
        {
            var visited = new HashSet<Vector2Int> { layout.EntryWorldCell };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(layout.EntryWorldCell);
            Vector2Int[] directions =
                { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == layout.ExitWorldCell)
                {
                    return true;
                }
                for (int index = 0; index < directions.Length; index++)
                {
                    Vector2Int next = current + directions[index];
                    if (next.x < 0 || next.y < 0 || next.x >= layout.SizeCells.x ||
                        next.y >= layout.SizeCells.y || visited.Contains(next))
                    {
                        continue;
                    }
                    MicroCellKind kind = layout.GetWorldCell(next);
                    if (kind == MicroCellKind.Solid || kind == MicroCellKind.Hazard)
                    {
                        continue;
                    }
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        private static void ValidateHiddenContentAndToolEscape(
            RoomInteriorLayout layout,
            ICollection<string> errors)
        {
            for (int index = 0; index < layout.HiddenContents.Count; index++)
            {
                GeneratedHiddenContent hidden = layout.HiddenContents[index];
                GeneratedMicroChunk chunk = hidden != null ? layout.FindChunk(hidden.ChunkGridCell) : null;
                if (hidden == null || hidden.Type != HiddenContentType.EmbeddedPocket || chunk == null || chunk.MainRoute)
                {
                    errors.Add("[HIDDEN] EmbeddedPocket must exist in an optional chunk.");
                    continue;
                }
                ToolTag approved = ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Shovel;
                if ((hidden.RevealTools & approved) != approved)
                {
                    errors.Add("[HIDDEN] EmbeddedPocket must support Bomb, Pickaxe, and Shovel discovery.");
                }
            }

            string[] approvedPatterns = { "ESC_SHOVEL_DIRT_01", "ESC_BOMB_CRACK_01", "ESC_ROPE_SHAFT_01" };
            for (int index = 0; index < layout.ToolEscapes.Count; index++)
            {
                GeneratedToolEscape escape = layout.ToolEscapes[index];
                GeneratedMicroChunk chunk = escape != null ? layout.FindChunk(escape.ChunkGridCell) : null;
                if (escape == null || chunk == null || chunk.MainRoute)
                {
                    errors.Add("[ESCAPE] ToolEscape cannot be placed on the main route.");
                    continue;
                }
                if (!approvedPatterns.Contains(escape.PatternId, StringComparer.Ordinal))
                {
                    errors.Add($"[ESCAPE] Unknown ToolEscape pattern '{escape.PatternId}'.");
                }
                if (escape.RequiredTool == ToolTag.None ||
                    !Mathf.Approximately(escape.RecoveryDelaySeconds, 1.2f) ||
                    !Mathf.Approximately(escape.AbandonHoldSeconds, 2f) ||
                    !escape.EmergencyDoorAfterThirdBell)
                {
                    errors.Add("[ESCAPE] Tool guarantee, 1.2s recovery, 2s abandon, or Bell3 emergency contract is missing.");
                }
            }
        }

        private static bool IsValidSocketCell(MicroSocketSide side, Vector2Int cell)
        {
            return side switch
            {
                MicroSocketSide.West => cell.x == 0 && cell.y >= 2 && cell.y <= 5,
                MicroSocketSide.East => cell.x == 7 && cell.y >= 2 && cell.y <= 5,
                MicroSocketSide.South => cell.y == 0 && cell.x >= 2 && cell.x <= 5,
                MicroSocketSide.North => cell.y == 7 && cell.x >= 2 && cell.x <= 5,
                _ => false,
            };
        }

        private static Vector2Int GetInnerCell(MicroSocketSide side, Vector2Int cell)
        {
            return side switch
            {
                MicroSocketSide.West => cell + Vector2Int.right,
                MicroSocketSide.East => cell + Vector2Int.left,
                MicroSocketSide.South => cell + Vector2Int.up,
                MicroSocketSide.North => cell + Vector2Int.down,
                _ => cell,
            };
        }

        private static MicroSocketSide Opposite(MicroSocketSide side)
        {
            return side switch
            {
                MicroSocketSide.West => MicroSocketSide.East,
                MicroSocketSide.East => MicroSocketSide.West,
                MicroSocketSide.South => MicroSocketSide.North,
                _ => MicroSocketSide.South,
            };
        }

        private static int GetAxisOffset(GeneratedMicroSocket socket)
        {
            return socket.Side == MicroSocketSide.West || socket.Side == MicroSocketSide.East
                ? socket.LocalCell.y
                : socket.LocalCell.x;
        }
    }
}

#endif
