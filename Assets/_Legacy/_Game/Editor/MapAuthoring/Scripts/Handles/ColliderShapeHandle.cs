#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class ColliderShapeHandle
    {
        private const float SnapSize = 0.01f;

        public static void Draw(MapElementDefinition definition, Vector3 anchor, bool acceptInput)
        {
            if (definition == null || definition.CollisionProfile == null)
            {
                return;
            }

            DrawOutlines(definition.CollisionProfile.SolidShapes, anchor, new Color(0.25f, 0.95f, 1f, 0.95f));
            DrawOutlines(definition.CollisionProfile.TriggerShapes, anchor, new Color(1f, 0.3f, 0.75f, 0.95f));

            if (!acceptInput)
            {
                return;
            }

            var shapes = definition.CollisionProfile.SolidShapes.Count > 0
                ? definition.CollisionProfile.SolidShapes
                : definition.CollisionProfile.TriggerShapes;
            if (shapes.Count == 0 || shapes[0] == null)
            {
                Handles.Label(anchor + Vector3.up * 0.75f,
                    "Collision 탭에서 Shape를 추가하세요.");
                return;
            }

            DrawEditableShape(definition, shapes[0], anchor);
        }

        private static void DrawEditableShape(
            MapElementDefinition definition,
            SerializedColliderShape shape,
            Vector3 anchor)
        {
            var boundsHandle = new BoxBoundsHandle
            {
                center = anchor + (Vector3)shape.OffsetCells,
                size = new Vector3(
                    Mathf.Max(SnapSize, shape.SizeCells.x),
                    Mathf.Max(SnapSize, shape.SizeCells.y),
                    0f),
                handleColor = new Color(0.25f, 0.95f, 1f, 1f),
                wireframeColor = new Color(0.25f, 0.95f, 1f, 0.85f),
            };

            EditorGUI.BeginChangeCheck();
            boundsHandle.DrawHandle();
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            var offset = (Vector2)(boundsHandle.center - anchor);
            var size = (Vector2)boundsHandle.size;
            offset.x = Snap(offset.x);
            offset.y = Snap(offset.y);
            size.x = Mathf.Max(SnapSize, Snap(size.x));
            size.y = Mathf.Max(SnapSize, Snap(size.y));
            ClampToFootprint(definition.Footprint, ref offset, ref size);

            Undo.RecordObject(definition, "Resize Map Element Collider");
            shape.OffsetCells = offset;
            shape.SizeCells = size;
            EditorUtility.SetDirty(definition);
            MapElementAuthoringSession.NotifyDefinitionChanged();
        }

        private static void ClampToFootprint(
            CellFootprint footprint,
            ref Vector2 center,
            ref Vector2 size)
        {
            if (footprint == null)
            {
                return;
            }

            var minimum = new Vector2(
                -footprint.PivotCell.x - 0.5f,
                -footprint.PivotCell.y - 0.5f);
            var maximum = minimum + footprint.BoundsSize;
            size.x = Mathf.Min(size.x, maximum.x - minimum.x + 0.02f);
            size.y = Mathf.Min(size.y, maximum.y - minimum.y + 0.02f);
            var half = size * 0.5f;
            center.x = Mathf.Clamp(center.x, minimum.x + half.x - 0.01f, maximum.x - half.x + 0.01f);
            center.y = Mathf.Clamp(center.y, minimum.y + half.y - 0.01f, maximum.y - half.y + 0.01f);
        }

        private static void DrawOutlines(
            IReadOnlyList<SerializedColliderShape> shapes,
            Vector3 anchor,
            Color color)
        {
            if (shapes == null)
            {
                return;
            }

            Handles.color = color;
            for (var index = 0; index < shapes.Count; index++)
            {
                var shape = shapes[index];
                if (shape == null)
                {
                    continue;
                }

                var center = anchor + (Vector3)shape.OffsetCells;
                if (shape.ShapeType == SerializedColliderShapeType.Polygon &&
                    shape.Points != null && shape.Points.Count >= 3)
                {
                    var points = new Vector3[shape.Points.Count + 1];
                    for (var pointIndex = 0; pointIndex < shape.Points.Count; pointIndex++)
                    {
                        points[pointIndex] = center + (Vector3)shape.Points[pointIndex];
                    }

                    points[points.Length - 1] = points[0];
                    Handles.DrawAAPolyLine(3f, points);
                    continue;
                }

                Handles.DrawWireCube(center, new Vector3(shape.SizeCells.x, shape.SizeCells.y, 0f));
            }
        }

        private static float Snap(float value)
        {
            return Mathf.Round(value / SnapSize) * SnapSize;
        }
    }
}

#endif
