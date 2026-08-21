#if LEGACY_DISABLED
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace StarNight.ToolAuthoring
{
    [ExecuteAlways]
    public sealed class ToolInteractionLabStation : MonoBehaviour
    {
        [SerializeField] private ToolLabStationKind stationKind;
        [SerializeField] private Object definition;

        public ToolLabStationKind StationKind => stationKind;
        public Object Definition => definition;

        public void Configure(ToolLabStationKind kind, Object asset)
        {
            stationKind = kind;
            definition = asset;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.72f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
#if UNITY_EDITOR
            Handles.Label(transform.position + Vector3.up * 0.55f, stationKind.ToString());
#endif
        }
    }
}

#endif
