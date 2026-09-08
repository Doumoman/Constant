#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Map.Tests.EditMode.Rmap02
{
    [Category("RMAP02")]
    public sealed class GeneratedUnityTilemapApplierTests
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = owned.Count - 1; index >= 0; index--)
            {
                if (owned[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(owned[index]);
                }
            }

            owned.Clear();
        }

        [Test]
        public void RMAP02_FixtureApplier_WritesSevenLayersAndClearsStaleTiles()
        {
            var gridRoot = Own(new GameObject("RMAP02_EditMode_Grid", typeof(Grid)));
            var bindings = new List<GeneratedUnityTilemapLayerBinding>();
            Tilemap terrain = null;
            Tilemap affordance = null;

            foreach (GeneratedTilemapLayerId layer in Enum.GetValues(
                typeof(GeneratedTilemapLayerId)))
            {
                var layerObject = Own(new GameObject(layer.ToString(), typeof(Tilemap)));
                layerObject.transform.SetParent(gridRoot.transform, false);
                var tilemap = layerObject.GetComponent<Tilemap>();
                var tile = Own(ScriptableObject.CreateInstance<Tile>());
                tile.colliderType = layer == GeneratedTilemapLayerId.Terrain
                    ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                bindings.Add(new GeneratedUnityTilemapLayerBinding(layer, tilemap, tile));

                if (layer == GeneratedTilemapLayerId.Terrain)
                {
                    terrain = tilemap;
                    var collider = layerObject.AddComponent<TilemapCollider2D>();
                    layerObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                    layerObject.AddComponent<CompositeCollider2D>();
                    collider.compositeOperation = Collider2D.CompositeOperation.Merge;
                }
                else if (layer == GeneratedTilemapLayerId.Affordance)
                {
                    affordance = tilemap;
                }
            }

            var applier = gridRoot.AddComponent<GeneratedUnityTilemapApplier>();
            applier.Configure(bindings, false);
            affordance.SetTile(new Vector3Int(59, 39, 0), bindings[1].OccupiedTile);

            GeneratedUnityTilemapApplyReport first = applier.ApplyFixture();
            GeneratedUnityTilemapApplyReport second = applier.ApplyFixture();

            Assert.AreEqual(7, first.AppliedLayers.Count);
            Assert.AreEqual(235, first.AppliedTileCount);
            Assert.GreaterOrEqual(second.ClearedTileCount, 235,
                "The same owner must clear its previously applied cells.");
            Assert.IsNull(affordance.GetTile(new Vector3Int(59, 39, 0)),
                "Stale cells outside the fixture are cleared before reapply.");
            Assert.IsNotNull(terrain.GetTile(new Vector3Int(0, 0, 0)));
            Assert.IsNotNull(terrain.GetTile(new Vector3Int(12, 1, 0)));
            Assert.IsNotNull(terrain.GetTile(new Vector3Int(18, 2, 0)));
            Assert.IsNull(affordance.GetComponent<TilemapCollider2D>(),
                "Only Terrain owns a physical TilemapCollider2D.");
            Assert.IsNotNull(terrain.GetComponent<TilemapCollider2D>());
            Assert.IsNotNull(terrain.GetComponent<CompositeCollider2D>());
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            owned.Add(value);
            return value;
        }
    }
}
#endif
