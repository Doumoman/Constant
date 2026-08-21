#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Player.Presentation
{
    public static class PrototypeSpriteFactory
    {
        private static Texture2D texture;
        private static Sprite sprite;

        public static Sprite GetWhitePixel()
        {
            if (sprite != null)
            {
                return sprite;
            }

            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Core03PrototypePixel",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "Core03PrototypeSprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            texture = null;
            sprite = null;
        }
    }
}

#endif
