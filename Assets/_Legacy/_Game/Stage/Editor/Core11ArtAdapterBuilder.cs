#if LEGACY_DISABLED
using System;
using System.Linq;
using StarNight.Stage.Data;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace StarNight.Stage.Editor
{
    public static class Core11ArtAdapterBuilder
    {
        public const string AdapterRoot = "Assets/_Game/ArtAdapters";
        public const string MoonProfilePath = AdapterRoot + "/Profiles/RegionArt_Moon.asset";
        public const string MoonAtlasPath = AdapterRoot + "/Atlases/Atlas_Moon.spriteatlas";
        public const string AuditPath = AdapterRoot + "/ArtAssetAudit.csv";

        private const string Stage0Path = "Assets/_Game/Stage/Data/Stages/Stage_0_1.asset";
        private const string Stage1Path = "Assets/_Game/Stage/Data/Stages/Stage_1_1.asset";
        private const string SourceRoot = "Assets/2D Fantasy sprite bundle";
        private const string SkyPath = SourceRoot + "/Mount pack/Sprites/Sky A.png";
        private const string MidBackgroundPath = SourceRoot + "/Island pack/Sprites/backgrounds back.png";
        private const string MoonPath = SourceRoot + "/Mount pack/Sprites/Moon A.png";
        private const string FillPath = SourceRoot + "/Dungeon pack/Sprites/stwall fill.png";
        private const string EdgePath = SourceRoot + "/Dungeon pack/Sprites/stwall sides 2.png";
        private const string ForegroundPath = SourceRoot + "/Dungeon pack/Sprites/root.png";

        [MenuItem("Star Night/Build CORE-11 Art Adapters")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game", "ArtAdapters");
            EnsureFolder(AdapterRoot, "Profiles");
            EnsureFolder(AdapterRoot, "Materials");
            EnsureFolder(AdapterRoot, "Atlases");

            Material backgroundMaterial = LoadOrCreateMaterial(
                AdapterRoot + "/Materials/Moon_Background.mat",
                new Color(0.74f, 0.83f, 1f, 1f));
            Material terrainMaterial = LoadOrCreateMaterial(
                AdapterRoot + "/Materials/Moon_Terrain.mat",
                new Color(0.72f, 0.78f, 0.95f, 1f));

            Sprite sky = LoadSprite(SkyPath, "Sky A");
            Sprite mountains = LoadSprite(MidBackgroundPath, "backgrounds back");
            Sprite moon = LoadSprite(MoonPath, "Moon A");
            Sprite fill = LoadSprite(FillPath, "stwall fill");
            Sprite edge = LoadSprite(EdgePath, "stwall sides 2_0");
            Sprite foreground = LoadSprite(ForegroundPath, "root_1");
            Sprite[] required = { sky, mountains, moon, fill, edge, foreground };
            if (required.Any(sprite => sprite == null))
            {
                throw new InvalidOperationException("CORE-11 Moon palette is missing one or more audited source sprites.");
            }

            RegionArtProfile profile = AssetDatabase.LoadAssetAtPath<RegionArtProfile>(MoonProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<RegionArtProfile>();
                AssetDatabase.CreateAsset(profile, MoonProfilePath);
            }

            profile.regionId = "moon";
            profile.backgroundMaterial = backgroundMaterial;
            profile.terrainMaterial = terrainMaterial;
            profile.farBackgrounds = new[] { sky };
            profile.midBackgrounds = new[] { mountains };
            profile.terrainEdges = new[] { edge };
            profile.terrainFillers = new[] { fill };
            profile.roomProps = new[] { moon };
            profile.foregroundProps = new[] { foreground };
            profile.ambientTint = new Color(0.63f, 0.74f, 0.94f, 1f);
            profile.fogTint = new Color(0.14f, 0.22f, 0.40f, 0.42f);
            profile.pixelsPerUnit = 100f;
            profile.visualTiles = new[]
            {
                new RegionVisualBinding
                {
                    visualTileKey = Visuals.RoomVisualKeys.SolidStone,
                    sprites = new[] { fill },
                    materialOverride = terrainMaterial,
                    tint = Color.white,
                },
                new RegionVisualBinding
                {
                    visualTileKey = Visuals.RoomVisualKeys.StoneEdge,
                    sprites = new[] { edge },
                    materialOverride = terrainMaterial,
                    tint = Color.white,
                },
            };
            EditorUtility.SetDirty(profile);

            // The 4,846 px sky gradient stays outside the atlas as a large backdrop.
            // Packing it would force Atlas_Moon to 8K for no batching benefit.
            BuildMoonAtlas(new[] { MidBackgroundPath, MoonPath, FillPath, EdgePath, ForegroundPath });
            AssignProfile(Stage0Path, profile);
            AssignProfile(Stage1Path, profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CORE-11 Moon art adapter, Atlas_Moon, and stage profile links are ready.");
        }

        private static void BuildMoonAtlas(string[] sourcePaths)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(MoonAtlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, MoonAtlasPath);
            }

            UnityEngine.Object[] existing = SpriteAtlasExtensions.GetPackables(atlas);
            if (existing.Length > 0)
            {
                SpriteAtlasExtensions.Remove(atlas, existing);
            }

            UnityEngine.Object[] sources = sourcePaths
                .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path))
                .Where(texture => texture != null)
                .Cast<UnityEngine.Object>()
                .ToArray();
            SpriteAtlasExtensions.Add(atlas, sources);

            SpriteAtlasPackingSettings packing = SpriteAtlasExtensions.GetPackingSettings(atlas);
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.padding = 4;
            SpriteAtlasExtensions.SetPackingSettings(atlas, packing);
            EditorUtility.SetDirty(atlas);
        }

        private static Material LoadOrCreateMaterial(string path, Color tint)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    throw new InvalidOperationException("Sprites/Default shader is unavailable.");
                }
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = tint;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => string.Equals(sprite.name, spriteName, StringComparison.Ordinal));
        }

        private static void AssignProfile(string stagePath, RegionArtProfile profile)
        {
            StageDefinition stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(stagePath);
            if (stage == null)
            {
                throw new InvalidOperationException("Missing stage asset: " + stagePath);
            }

            stage.artProfile = profile;
            EditorUtility.SetDirty(stage);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}

#endif
