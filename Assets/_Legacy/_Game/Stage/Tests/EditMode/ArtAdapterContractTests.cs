#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Stage.Data;
using StarNight.Stage.Editor;
using StarNight.Stage.Lab;
using StarNight.Stage.Rooms;
using StarNight.Stage.Transitions;
using StarNight.Stage.Visuals;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace StarNight.Stage.Tests
{
    public sealed class ArtAdapterContractTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ArtAdapterContractTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RegionProfileExposesRequiredArtAdapterContract()
        {
            string[] expected =
            {
                "regionId", "backgroundMaterial", "terrainMaterial", "farBackgrounds", "midBackgrounds",
                "terrainEdges", "terrainFillers", "roomProps", "foregroundProps", "ambientTint", "fogTint",
            };
            string[] fields = typeof(RegionArtProfile).GetFields().Select(field => field.Name).ToArray();

            CollectionAssert.IsSubsetOf(expected, fields);
        }

        [Test]
        public void SwappingProfileChangesVisualsWithoutChangingLogicalColliders()
        {
            RoomRuntime room = BuildRoom(out _);
            RoomVisualBuilder builder = room.GetComponent<RoomVisualBuilder>();
            BoxCollider2D[] originalColliders = room.GridLogic.GetComponentsInChildren<BoxCollider2D>(true);
            int[] originalColliderIds = originalColliders.Select(collider => collider.GetInstanceID()).ToArray();
            RegionArtProfile first = CreateRuntimeProfile("first", Color.red, out Texture2D firstTexture, out Sprite firstSprite);
            RegionArtProfile second = CreateRuntimeProfile("second", Color.blue, out Texture2D secondTexture, out Sprite secondSprite);

            builder.ApplyProfile(first);
            SpriteRenderer firstTerrain = builder.GeneratedRenderers.First(renderer => renderer.name == "TerrainFill");
            Assert.That(firstTerrain.sprite.texture, Is.SameAs(firstSprite.texture));

            builder.ApplyProfile(second);
            SpriteRenderer secondTerrain = builder.GeneratedRenderers.First(renderer => renderer.name == "TerrainFill");
            int[] currentColliderIds = room.GridLogic.GetComponentsInChildren<BoxCollider2D>(true)
                .Select(collider => collider.GetInstanceID())
                .ToArray();

            Assert.That(secondTerrain.sprite.texture, Is.SameAs(secondSprite.texture));
            Assert.That(secondTerrain.transform.localScale, Is.EqualTo(Vector3.one));
            CollectionAssert.AreEqual(originalColliderIds, currentColliderIds);
            Assert.That(room.GridLogic.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty);
            Assert.That(room.GridVisual.GetComponentsInChildren<Collider2D>(true), Is.Empty);

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(firstSprite);
            Object.DestroyImmediate(secondSprite);
            Object.DestroyImmediate(firstTexture);
            Object.DestroyImmediate(secondTexture);
        }

        [Test]
        public void DecorationsDoNotEnterGameplayClearZones()
        {
            RoomRuntime room = BuildRoom(out _);
            GameObject clearObject = new GameObject("InteractionClearZone");
            clearObject.transform.SetParent(room.transform, false);
            clearObject.transform.localPosition = new Vector3(Core04TwoRoomLab.RoomWidth * 0.36f, Core04TwoRoomLab.RoomHeight * 0.70f, 0f);
            GameplayClearZone clear = clearObject.AddComponent<GameplayClearZone>();
            clear.Configure(new Vector2(5f, 5f));
            RegionArtProfile profile = CreateRuntimeProfile("clear-zone", Color.white, out Texture2D texture, out Sprite sprite);
            profile.roomProps = new[] { sprite };

            RoomVisualBuilder builder = room.GetComponent<RoomVisualBuilder>();
            builder.ApplyProfile(profile);

            Assert.That(builder.GeneratedRenderers.Any(renderer => renderer.name == "MoonProp"), Is.False);
            Assert.That(builder.HasGeneratedVisualInsideClearZone(), Is.False);

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void MoonAdapterReferencesOriginalSourcesThroughProjectOwnedAssets()
        {
            RegionArtProfile profile = AssetDatabase.LoadAssetAtPath<RegionArtProfile>(Core11ArtAdapterBuilder.MoonProfilePath);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(Core11ArtAdapterBuilder.MoonAtlasPath);
            TextAsset audit = AssetDatabase.LoadAssetAtPath<TextAsset>(Core11ArtAdapterBuilder.AuditPath);

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.regionId, Is.EqualTo("moon"));
            Assert.That(profile.terrainFillers, Is.Not.Empty);
            Assert.That(profile.farBackgrounds, Is.Not.Empty);
            Assert.That(atlas, Is.Not.Null);
            Assert.That(audit, Is.Not.Null);
            StringAssert.StartsWith("SourcePath,Category,RegionUsage,AdapterPrefab,LicenseNote,Approved", audit.text);

            Sprite[] referenced = profile.farBackgrounds
                .Concat(profile.midBackgrounds)
                .Concat(profile.terrainEdges)
                .Concat(profile.terrainFillers)
                .Concat(profile.roomProps)
                .Concat(profile.foregroundProps)
                .ToArray();
            Assert.That(referenced, Is.Not.Empty);
            Assert.That(referenced.All(sprite => AssetDatabase.GetAssetPath(sprite).StartsWith("Assets/2D Fantasy sprite bundle/")), Is.True);
            Assert.That(referenced.All(sprite => Mathf.Approximately(sprite.pixelsPerUnit, profile.pixelsPerUnit)), Is.True);
            Assert.That(SpriteAtlasExtensions.GetPackables(atlas), Has.Length.GreaterThanOrEqualTo(5));
            Assert.That(SpriteAtlasExtensions.GetPackingSettings(atlas).padding, Is.GreaterThanOrEqualTo(4));

            StageDefinition stage0 = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/_Game/Stage/Data/Stages/Stage_0_1.asset");
            StageDefinition stage1 = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/_Game/Stage/Data/Stages/Stage_1_1.asset");
            Assert.That(stage0.artProfile, Is.SameAs(profile));
            Assert.That(stage1.artProfile, Is.SameAs(profile));
        }

        private RoomRuntime BuildRoom(out RoomPortal2D portal)
        {
            return Core04TwoRoomLab.BuildPrototypeRoom(
                root.transform,
                "Room_Art",
                Vector2.zero,
                new Color(0.1f, 0.2f, 0.3f, 1f),
                false,
                true,
                out portal);
        }

        private static RegionArtProfile CreateRuntimeProfile(
            string id,
            Color color,
            out Texture2D texture,
            out Sprite sprite)
        {
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);
            RegionArtProfile profile = ScriptableObject.CreateInstance<RegionArtProfile>();
            profile.regionId = id;
            profile.terrainFillers = new[] { sprite };
            profile.terrainEdges = new[] { sprite };
            profile.visualTiles = new[]
            {
                new RegionVisualBinding
                {
                    visualTileKey = RoomVisualKeys.SolidStone,
                    sprites = new[] { sprite },
                    tint = Color.white,
                },
                new RegionVisualBinding
                {
                    visualTileKey = RoomVisualKeys.StoneEdge,
                    sprites = new[] { sprite },
                    tint = Color.white,
                },
            };
            return profile;
        }
    }
}

#endif
