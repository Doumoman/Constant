#if LEGACY_DISABLED
using StarNight.Map;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class PixelScaleValidator
    {
        private const float ScaleTolerance = 0.0001f;
        private const float PixelPositionTolerance = 0.001f;

        public static void Validate(
            GameObject sourceRoot,
            MapElementDefinition definition,
            MapElementValidationReport report)
        {
            if (sourceRoot == null || report == null)
            {
                return;
            }

            var renderers = sourceRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (!ApproximatelyOne(renderer.transform.localScale))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "PIXEL_SCALE_NON_UNIT",
                        $"SpriteRenderer '{renderer.name}' Transform Scale은 (1,1,1)이어야 합니다.",
                        context: renderer,
                        autoFixable: true);
                }

                if (definition != null &&
                    definition.VisualProfile != null &&
                    definition.VisualProfile.RenderMode == ElementVisualRenderMode.TiledSprite &&
                    renderer.drawMode == SpriteDrawMode.Simple)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "PIXEL_TILED_SIMPLE",
                        $"Tiled Sprite '{renderer.name}'가 Simple Draw Mode입니다.",
                        context: renderer);
                }

                var sprite = renderer.sprite;
                if (sprite == null || sprite.texture == null ||
                    sprite.texture.filterMode != FilterMode.Point)
                {
                    continue;
                }

                var pixelPosition = (Vector2)renderer.transform.position * sprite.pixelsPerUnit;
                if (Mathf.Abs(pixelPosition.x - Mathf.Round(pixelPosition.x)) > PixelPositionTolerance ||
                    Mathf.Abs(pixelPosition.y - Mathf.Round(pixelPosition.y)) > PixelPositionTolerance)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "PIXEL_POINT_OFF_GRID",
                        $"Point Filter Sprite '{renderer.name}'가 비정수 픽셀 위치에 있습니다.",
                        context: renderer,
                        autoFixable: true);
                }
            }
        }

        private static bool ApproximatelyOne(Vector3 scale)
        {
            return Mathf.Abs(scale.x - 1f) <= ScaleTolerance &&
                   Mathf.Abs(scale.y - 1f) <= ScaleTolerance &&
                   Mathf.Abs(scale.z - 1f) <= ScaleTolerance;
        }
    }
}

#endif
