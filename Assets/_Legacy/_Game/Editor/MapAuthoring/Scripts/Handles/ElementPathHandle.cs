#if LEGACY_DISABLED
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class ElementPathHandle
    {
        private const float SnapSize = 0.5f;
        private static readonly Rect PreviewBounds = new Rect(-16f, -9f, 32f, 18f);

        public static void Draw(MapElementDefinition definition, Vector3 anchor, bool acceptInput)
        {
            var path = definition != null && definition.BehaviorProfile != null
                ? definition.BehaviorProfile.Path
                : null;
            if (path == null || path.Nodes == null || path.Nodes.Count == 0)
            {
                if (acceptInput)
                {
                    Handles.Label(anchor + Vector3.up, "Behavior/Path 탭에서 Node를 추가하세요.");
                }
                return;
            }

            for (var index = 0; index < path.Nodes.Count; index++)
            {
                var world = anchor + (Vector3)path.Nodes[index];
                var nextIndex = index + 1;
                if (nextIndex < path.Nodes.Count)
                {
                    DrawSegment(anchor, path.Nodes[index], path.Nodes[nextIndex]);
                }
                else if (path.ClosedLoop && path.Nodes.Count > 1)
                {
                    DrawSegment(anchor, path.Nodes[index], path.Nodes[0]);
                }

                Handles.color = PreviewBounds.Contains(path.Nodes[index]) ? Color.cyan : Color.red;
                Handles.SphereHandleCap(
                    0,
                    world,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(world) * 0.09f,
                    EventType.Repaint);
                Handles.Label(world + Vector3.up * 0.15f, $"Path {index}");

                if (!acceptInput)
                {
                    continue;
                }

                EditorGUI.BeginChangeCheck();
                var moved = Handles.PositionHandle(world, Quaternion.identity);
                if (!EditorGUI.EndChangeCheck())
                {
                    continue;
                }

                var local = (Vector2)(moved - anchor);
                local.x = Mathf.Round(local.x / SnapSize) * SnapSize;
                local.y = Mathf.Round(local.y / SnapSize) * SnapSize;
                Undo.RecordObject(definition, "Move Map Element Path Node");
                path.Nodes[index] = local;
                EditorUtility.SetDirty(definition);
                MapElementAuthoringSession.NotifyDefinitionChanged();
            }
        }

        private static void DrawSegment(Vector3 anchor, Vector2 from, Vector2 to)
        {
            var valid = PreviewBounds.Contains(from) && PreviewBounds.Contains(to);
            Handles.color = valid ? new Color(0.2f, 0.9f, 1f, 0.9f) : Color.red;
            Handles.DrawAAPolyLine(3f, anchor + (Vector3)from, anchor + (Vector3)to);
        }
    }
}

#endif
