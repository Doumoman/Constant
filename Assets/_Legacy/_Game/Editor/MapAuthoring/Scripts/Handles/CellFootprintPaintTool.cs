#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class CellFootprintPaintTool
    {
        private static readonly Color OccupiedColor = new Color(0.2f, 0.75f, 1f, 0.28f);
        private static readonly Color SupportColor = new Color(0.25f, 0.95f, 0.45f, 0.28f);
        private static readonly Color ClearanceColor = new Color(0.95f, 0.75f, 0.2f, 0.25f);
        private static readonly Color TriggerColor = new Color(0.95f, 0.3f, 0.75f, 0.26f);
        private static readonly Color HazardColor = new Color(1f, 0.2f, 0.22f, 0.34f);

        public static void Draw(
            MapElementDefinition definition,
            Vector3 anchor,
            SceneView sceneView,
            bool acceptInput)
        {
            if (definition == null || definition.Footprint == null)
            {
                return;
            }

            var footprint = definition.Footprint;
            DrawGrid(footprint, anchor);
            DrawCells(footprint.OccupiedCells, footprint, anchor, OccupiedColor);
            DrawCells(footprint.SupportRequiredCells, footprint, anchor, SupportColor);
            DrawCells(footprint.ClearanceRequiredCells, footprint, anchor, ClearanceColor);
            DrawCells(footprint.TriggerCells, footprint, anchor, TriggerColor);
            DrawCells(footprint.HazardCells, footprint, anchor, HazardColor);
            DrawPivot(anchor);

            if (!acceptInput)
            {
                return;
            }

            HandlePivot(definition, anchor);
            HandleFocusKey(footprint, anchor, sceneView);
            HandlePaintInput(definition, anchor);
        }

        private static void DrawGrid(CellFootprint footprint, Vector3 anchor)
        {
            var minimum = new Vector2(-footprint.PivotCell.x - 0.5f, -footprint.PivotCell.y - 0.5f);
            var maximum = minimum + footprint.BoundsSize;
            Handles.color = new Color(0.55f, 0.7f, 0.85f, 0.55f);
            for (var x = 0; x <= footprint.BoundsSize.x; x++)
            {
                var worldX = anchor.x + minimum.x + x;
                Handles.DrawLine(
                    new Vector3(worldX, anchor.y + minimum.y, 0f),
                    new Vector3(worldX, anchor.y + maximum.y, 0f));
            }

            for (var y = 0; y <= footprint.BoundsSize.y; y++)
            {
                var worldY = anchor.y + minimum.y + y;
                Handles.DrawLine(
                    new Vector3(anchor.x + minimum.x, worldY, 0f),
                    new Vector3(anchor.x + maximum.x, worldY, 0f));
            }
        }

        private static void DrawCells(
            IReadOnlyList<Vector2Int> cells,
            CellFootprint footprint,
            Vector3 anchor,
            Color fill)
        {
            if (cells == null)
            {
                return;
            }

            for (var index = 0; index < cells.Count; index++)
            {
                var relative = cells[index] - footprint.PivotCell;
                var center = anchor + new Vector3(relative.x, relative.y, 0f);
                var corners = new[]
                {
                    center + new Vector3(-0.48f, -0.48f),
                    center + new Vector3(-0.48f, 0.48f),
                    center + new Vector3(0.48f, 0.48f),
                    center + new Vector3(0.48f, -0.48f),
                };
                Handles.DrawSolidRectangleWithOutline(corners, fill, new Color(fill.r, fill.g, fill.b, 0.9f));
            }
        }

        private static void DrawPivot(Vector3 anchor)
        {
            Handles.color = Color.white;
            var size = HandleUtility.GetHandleSize(anchor) * 0.08f;
            Handles.DrawLine(anchor + Vector3.left * size, anchor + Vector3.right * size);
            Handles.DrawLine(anchor + Vector3.down * size, anchor + Vector3.up * size);
            Handles.Label(anchor + new Vector3(size, size, 0f), "Pivot");
        }

        private static void HandlePivot(MapElementDefinition definition, Vector3 anchor)
        {
            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(anchor, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            var footprint = definition.Footprint;
            var delta = new Vector2Int(
                Mathf.RoundToInt(moved.x - anchor.x),
                Mathf.RoundToInt(moved.y - anchor.y));
            if (delta == Vector2Int.zero)
            {
                return;
            }

            var next = footprint.PivotCell + delta;
            next.x = Mathf.Clamp(next.x, 0, footprint.BoundsSize.x - 1);
            next.y = Mathf.Clamp(next.y, 0, footprint.BoundsSize.y - 1);
            if (next == footprint.PivotCell)
            {
                return;
            }

            Undo.RecordObject(definition, "Move Map Element Pivot");
            footprint.PivotCell = next;
            MarkChanged(definition);
        }

        private static void HandleFocusKey(CellFootprint footprint, Vector3 anchor, SceneView sceneView)
        {
            var current = Event.current;
            if (current.type != EventType.KeyDown || current.keyCode != KeyCode.F)
            {
                return;
            }

            var center = anchor + new Vector3(
                (footprint.BoundsSize.x - 1) * 0.5f - footprint.PivotCell.x,
                (footprint.BoundsSize.y - 1) * 0.5f - footprint.PivotCell.y,
                0f);
            sceneView.Frame(new Bounds(center, new Vector3(
                Mathf.Max(2f, footprint.BoundsSize.x + 2f),
                Mathf.Max(2f, footprint.BoundsSize.y + 2f),
                1f)), false);
            current.Use();
        }

        private static void HandlePaintInput(MapElementDefinition definition, Vector3 anchor)
        {
            var current = Event.current;
            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                return;
            }

            if (current.type != EventType.MouseDown || current.button != 0)
            {
                return;
            }

            if (!TryGetCell(current.mousePosition, definition.Footprint, anchor, out var cell))
            {
                return;
            }

            var label = "Paint Occupied Cell";
            IList<Vector2Int> target = definition.Footprint.OccupiedCells;
            if (current.shift)
            {
                label = "Paint Support Cell";
                target = definition.Footprint.SupportRequiredCells;
            }
            else if (current.control || current.command)
            {
                label = "Paint Clearance Cell";
                target = definition.Footprint.ClearanceRequiredCells;
            }
            else if (current.alt)
            {
                label = "Paint Trigger Cell";
                target = definition.Footprint.TriggerCells;
            }

            if (ReferenceEquals(target, definition.Footprint.OccupiedCells) &&
                target.Contains(cell) && target.Count == 1)
            {
                EditorApplication.Beep();
                current.Use();
                return;
            }

            Undo.RecordObject(definition, label);
            if (target.Contains(cell))
            {
                target.Remove(cell);
            }
            else
            {
                target.Add(cell);
            }

            MarkChanged(definition);
            current.Use();
        }

        private static bool TryGetCell(
            Vector2 mousePosition,
            CellFootprint footprint,
            Vector3 anchor,
            out Vector2Int cell)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
            {
                cell = default;
                return false;
            }

            var distance = -ray.origin.z / ray.direction.z;
            var world = ray.GetPoint(distance);
            var local = (Vector2)(world - anchor) + footprint.PivotCell;
            cell = new Vector2Int(
                Mathf.FloorToInt(local.x + 0.5f),
                Mathf.FloorToInt(local.y + 0.5f));
            return footprint.ContainsLocalCell(cell);
        }

        private static void MarkChanged(MapElementDefinition definition)
        {
            EditorUtility.SetDirty(definition);
            MapElementAuthoringSession.NotifyDefinitionChanged();
        }
    }
}

#endif
