#if LEGACY_DISABLED
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class VisualOffsetHandle
    {
        private const float SnapSize = 0.05f;

        public static void Draw(MapElementDefinition definition, Vector3 anchor, bool acceptInput)
        {
            if (definition == null || definition.VisualProfile == null)
            {
                return;
            }

            var profile = definition.VisualProfile;
            var center = anchor + (Vector3)profile.VisualOffsetCells;
            Handles.color = new Color(0.35f, 0.9f, 1f, 0.95f);
            Handles.DrawWireCube(center, new Vector3(
                Mathf.Max(0.05f, profile.VisualSizeCells.x),
                Mathf.Max(0.05f, profile.VisualSizeCells.y),
                0f));
            Handles.Label(center + Vector3.up * (profile.VisualSizeCells.y * 0.5f + 0.15f),
                $"Visual {profile.VisualSizeCells.x:0.##}×{profile.VisualSizeCells.y:0.##}");

            if (!acceptInput)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(center, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            var offset = (Vector2)(moved - anchor);
            offset.x = Mathf.Round(offset.x / SnapSize) * SnapSize;
            offset.y = Mathf.Round(offset.y / SnapSize) * SnapSize;
            Undo.RecordObject(definition, "Move Map Element Visual Offset");
            profile.VisualOffsetCells = offset;
            EditorUtility.SetDirty(definition);
            MapElementAuthoringSession.NotifyDefinitionChanged();
        }
    }
}

#endif
