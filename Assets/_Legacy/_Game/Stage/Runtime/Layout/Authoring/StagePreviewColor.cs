#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class StagePreviewColor : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;

        public void Configure(Color value)
        {
            color = value;
            Apply();
        }

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        private void Apply()
        {
            Renderer targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null) return;
            var block = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            targetRenderer.SetPropertyBlock(block);
        }
    }
}

#endif
