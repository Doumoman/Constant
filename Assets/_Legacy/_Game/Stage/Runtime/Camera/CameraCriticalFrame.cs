#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.CameraSystem
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraCriticalFrame : MonoBehaviour
    {
        [SerializeField] private CameraTileProfile profile = new CameraTileProfile();
        [SerializeField, Min(0.0001f)] private float worldUnitsPerCell = 1f;

        public CameraTileProfile Profile => profile;
        public float WorldUnitsPerCell => worldUnitsPerCell;
        public Rect RenderView => CreateCenteredRect(profile.VisibleWidthTiles, profile.visibleHeightTiles);
        public Rect CriticalFrame => CreateCenteredRect(profile.criticalWidthTiles, profile.criticalHeightTiles);
        public Rect DeadZone => CreateCenteredRect(profile.deadZoneWidthTiles, profile.deadZoneHeightTiles);

        public void Configure(CameraTileProfile configuredProfile, float configuredWorldUnitsPerCell = 1f)
        {
            profile = configuredProfile ?? new CameraTileProfile();
            worldUnitsPerCell = Mathf.Max(0.0001f, configuredWorldUnitsPerCell);
            profile.ApplyTo(GetComponent<Camera>(), worldUnitsPerCell);
        }

        private Rect CreateCenteredRect(float widthTiles, float heightTiles)
        {
            float width = widthTiles * worldUnitsPerCell;
            float height = heightTiles * worldUnitsPerCell;
            Vector2 center = transform.position;
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawRect(RenderView, new Color(0.22f, 0.78f, 1f, 0.9f));
            DrawRect(CriticalFrame, new Color(1f, 0.82f, 0.18f, 0.95f));
            DrawRect(DeadZone, new Color(0.28f, 1f, 0.46f, 0.9f));

            Rect render = RenderView;
            float preview = profile.portalPreviewDepthTiles * worldUnitsPerCell;
            DrawRect(new Rect(render.xMin, render.yMin, preview, render.height), new Color(0.65f, 0.35f, 1f, 0.65f));
            DrawRect(new Rect(render.xMax - preview, render.yMin, preview, render.height), new Color(0.65f, 0.35f, 1f, 0.65f));
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Gizmos.color = color;
            Vector3 bottomLeft = new Vector3(rect.xMin, rect.yMin, 0f);
            Vector3 bottomRight = new Vector3(rect.xMax, rect.yMin, 0f);
            Vector3 topRight = new Vector3(rect.xMax, rect.yMax, 0f);
            Vector3 topLeft = new Vector3(rect.xMin, rect.yMax, 0f);
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
#endif
    }
}

#endif
