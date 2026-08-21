#if LEGACY_DISABLED
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace StarNight.ToolAuthoring
{
    [ExecuteAlways]
    public sealed class ToolInteractionLabZone : MonoBehaviour
    {
        [SerializeField] private ToolLabZoneKind zoneKind;
        [SerializeField] private Vector2 size = new Vector2(6f, 4f);

        public ToolLabZoneKind ZoneKind => zoneKind;
        public Vector2 Size => size;

        public void Configure(ToolLabZoneKind kind, Vector2 zoneSize)
        {
            zoneKind = kind;
            size = zoneSize;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.25f, 0.75f, 1f, 0.55f);
            Gizmos.DrawWireCube(transform.position, size);
#if UNITY_EDITOR
            Handles.Label(transform.position + Vector3.up * (size.y * 0.5f + 0.35f), zoneKind.ToString());
#endif
        }
    }
}

#endif
