#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class MapElementLabGridGuide : MonoBehaviour
    {
        [SerializeField] private Vector2Int previewSize = new Vector2Int(32, 18);
        [SerializeField] private Color gridColor = new Color(0.2f, 0.35f, 0.5f, 0.28f);
        [SerializeField] private Color boundsColor = new Color(0.35f, 0.75f, 1f, 0.75f);

        private void OnDrawGizmos()
        {
            var half = (Vector2)previewSize * 0.5f;
            Gizmos.color = gridColor;
            for (var x = -previewSize.x / 2; x <= previewSize.x / 2; x++)
            {
                Gizmos.DrawLine(
                    transform.position + new Vector3(x, -half.y, 0f),
                    transform.position + new Vector3(x, half.y, 0f));
            }

            for (var y = -previewSize.y / 2; y <= previewSize.y / 2; y++)
            {
                Gizmos.DrawLine(
                    transform.position + new Vector3(-half.x, y, 0f),
                    transform.position + new Vector3(half.x, y, 0f));
            }

            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(transform.position, new Vector3(previewSize.x, previewSize.y, 0f));
        }
    }

    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class MapElementLabTint : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;

        public void SetColor(Color value)
        {
            color = value;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Apply()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor("_Color", color);
            properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
        }
    }
}

#endif
