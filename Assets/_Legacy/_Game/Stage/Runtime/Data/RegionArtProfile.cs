#if LEGACY_DISABLED
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace StarNight.Stage.Data
{
    [CreateAssetMenu(menuName = "Star Night/Region Art Profile", fileName = "RegionArtProfile")]
    public sealed class RegionArtProfile : ScriptableObject
    {
        [FormerlySerializedAs("profileId")]
        public string regionId;
        public Material backgroundMaterial;
        public Material terrainMaterial;
        public Sprite[] farBackgrounds = Array.Empty<Sprite>();
        public Sprite[] midBackgrounds = Array.Empty<Sprite>();
        public Sprite[] terrainEdges = Array.Empty<Sprite>();
        public Sprite[] terrainFillers = Array.Empty<Sprite>();
        public Sprite[] roomProps = Array.Empty<Sprite>();
        public Sprite[] foregroundProps = Array.Empty<Sprite>();
        public Color ambientTint = Color.white;
        public Color fogTint = Color.clear;
        [Min(1f)] public float pixelsPerUnit = 100f;
        public RegionVisualBinding[] visualTiles = Array.Empty<RegionVisualBinding>();

        public bool TryResolve(string visualTileKey, int variantIndex, out RegionVisual resolved)
        {
            if (!string.IsNullOrWhiteSpace(visualTileKey) && visualTiles != null)
            {
                for (int index = 0; index < visualTiles.Length; index++)
                {
                    RegionVisualBinding binding = visualTiles[index];
                    if (binding == null || !string.Equals(binding.visualTileKey, visualTileKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Sprite sprite = SelectVariant(binding.sprites, variantIndex);
                    if (sprite != null)
                    {
                        resolved = new RegionVisual(sprite, binding.materialOverride, binding.tint);
                        return true;
                    }
                }
            }

            resolved = default;
            return false;
        }

        public Sprite SelectFarBackground(int variantIndex = 0) => SelectVariant(farBackgrounds, variantIndex);
        public Sprite SelectMidBackground(int variantIndex = 0) => SelectVariant(midBackgrounds, variantIndex);
        public Sprite SelectRoomProp(int variantIndex = 0) => SelectVariant(roomProps, variantIndex);
        public Sprite SelectForegroundProp(int variantIndex = 0) => SelectVariant(foregroundProps, variantIndex);

        private static Sprite SelectVariant(Sprite[] variants, int variantIndex)
        {
            if (variants == null || variants.Length == 0)
            {
                return null;
            }

            int wrapped = (int)((uint)variantIndex % (uint)variants.Length);
            return variants[wrapped];
        }
    }

    [Serializable]
    public sealed class RegionVisualBinding
    {
        public string visualTileKey;
        public Sprite[] sprites = Array.Empty<Sprite>();
        public Material materialOverride;
        public Color tint = Color.white;
    }

    public readonly struct RegionVisual
    {
        public RegionVisual(Sprite sprite, Material material, Color tint)
        {
            Sprite = sprite;
            Material = material;
            Tint = tint;
        }

        public Sprite Sprite { get; }
        public Material Material { get; }
        public Color Tint { get; }
    }
}

#endif
