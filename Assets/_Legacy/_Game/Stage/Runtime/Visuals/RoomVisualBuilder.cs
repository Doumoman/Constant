#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Player.Presentation;
using StarNight.Stage.Data;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Visuals
{
    public static class RoomVisualKeys
    {
        public const string SolidStone = "Moon.Stone.Solid";
        public const string StoneEdge = "Moon.Stone.Edge";
    }

    public static class RoomVisualSorting
    {
        public const int BackgroundFar = -50;
        public const int BackgroundMid = -40;
        public const int RoomBack = -20;
        public const int TerrainBack = -10;
        public const int Terrain = 0;
        public const int Fixture = 5;
        public const int Actor = 10;
        public const int HeldObject = 12;
        public const int FrontVfx = 20;
        public const int Foreground = 30;
        public const int WorldUi = 40;
        public const int ScreenUi = 100;
    }

    [DisallowMultipleComponent]
    public sealed class RoomVisualBuilder : MonoBehaviour, IRoomSimulationParticipant
    {
        private const string GeneratedRootName = "GeneratedVisuals";

        [SerializeField] private RoomRuntime room;
        [SerializeField] private RegionArtProfile profile;
        [SerializeField] private Transform gridVisual;
        [SerializeField] private Transform backgroundRoot;
        [SerializeField] private Transform propRoot;
        [SerializeField] private Transform actorRoot;
        [SerializeField] private Transform vfxRoot;
        [SerializeField] private Transform foregroundRoot;
        [SerializeField] private Color fallbackBackdrop = new Color(0.035f, 0.22f, 0.27f, 1f);

        private readonly List<SpriteRenderer> generatedRenderers = new List<SpriteRenderer>();
        private readonly List<Sprite> adapterSprites = new List<Sprite>();

        public RegionArtProfile Profile => profile;
        public IReadOnlyList<SpriteRenderer> GeneratedRenderers => generatedRenderers;
        public int BuildRevision { get; private set; }

        public void Configure(
            RoomRuntime owner,
            Transform visualGrid,
            Transform backgrounds,
            Transform props,
            Transform actors,
            Transform vfx,
            Transform foreground,
            Color fallbackColor)
        {
            room = owner;
            gridVisual = visualGrid;
            backgroundRoot = backgrounds;
            propRoot = props;
            actorRoot = actors;
            vfxRoot = vfx;
            foregroundRoot = foreground;
            fallbackBackdrop = fallbackColor;
        }

        public void ApplyProfile(RegionArtProfile nextProfile)
        {
            profile = nextProfile;
            Rebuild();
        }

        public void Rebuild()
        {
            if (room == null || gridVisual == null)
            {
                return;
            }

            ClearGeneratedVisuals();
            generatedRenderers.Clear();
            ClearAdapterSprites();

            Transform terrainBack = EnsureLayer(gridVisual, "TerrainBackTilemap");
            Transform terrainFace = EnsureLayer(gridVisual, "TerrainFaceTilemap");
            EnsureLayer(gridVisual, "TerrainFrontTilemap");
            EnsureLayer(gridVisual, "DecalTilemap");

            BuildBackdrop(backgroundRoot);
            BuildTerrain(terrainBack, terrainFace);
            BuildDecorations(propRoot, foregroundRoot);
            BuildRevision++;
        }

        public bool HasGeneratedVisualInsideClearZone(int minimumSortingOrder = RoomVisualSorting.Fixture)
        {
            GameplayClearZone[] zones = GetComponentsInChildren<GameplayClearZone>(true);
            for (int rendererIndex = 0; rendererIndex < generatedRenderers.Count; rendererIndex++)
            {
                SpriteRenderer renderer = generatedRenderers[rendererIndex];
                if (renderer == null || renderer.sortingOrder < minimumSortingOrder)
                {
                    continue;
                }

                for (int zoneIndex = 0; zoneIndex < zones.Length; zoneIndex++)
                {
                    if (zones[zoneIndex] != null && renderer.bounds.Intersects(zones[zoneIndex].WorldBounds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void SetRoomSimulationState(RoomSimulationState state)
        {
            bool visible = state == RoomSimulationState.Active;
            SetActive(gridVisual, visible);
            SetActive(backgroundRoot, visible);
            SetActive(propRoot, visible);
            SetActive(actorRoot, visible);
            SetActive(vfxRoot, visible);
            SetActive(foregroundRoot, visible);
        }

        private void BuildBackdrop(Transform parent)
        {
            Transform generated = CreateGeneratedRoot(parent);
            Sprite far = profile != null ? profile.SelectFarBackground() : null;
            Sprite mid = profile != null ? profile.SelectMidBackground() : null;
            Color ambient = profile != null ? profile.ambientTint : Color.white;

            SpriteRenderer farRenderer = CreateSizedSprite(
                generated,
                "FarBackground",
                far ?? PrototypeSpriteFactory.GetWhitePixel(),
                new Vector2(room.SizeCells.x * 0.5f, room.SizeCells.y * 0.5f),
                room.SizeCells,
                Multiply(fallbackBackdrop, ambient),
                RoomVisualSorting.BackgroundFar,
                profile != null ? profile.backgroundMaterial : null,
                SpriteDrawMode.Sliced);
            farRenderer.gameObject.AddComponent<RoomThemeOcclusionMask>()
                .Configure(room, farRenderer);

            if (mid != null)
            {
                CreateSimpleSprite(
                    generated,
                    "MidBackground",
                    mid,
                    new Vector2(room.SizeCells.x * 0.5f, 3.2f),
                    Multiply(new Color(0.78f, 0.86f, 1f, 0.36f), ambient),
                    RoomVisualSorting.BackgroundMid,
                    profile.backgroundMaterial);
            }
        }

        private void BuildTerrain(Transform terrainBack, Transform terrainFace)
        {
            ResolveVisual(RoomVisualKeys.SolidStone, profile?.terrainFillers, out Sprite filler, out Material fillMaterial, out Color fillTint);
            ResolveVisual(RoomVisualKeys.StoneEdge, profile?.terrainEdges, out Sprite edge, out Material edgeMaterial, out Color edgeTint);
            Color ambient = profile != null ? profile.ambientTint : Color.white;

            Transform backGenerated = CreateGeneratedRoot(terrainBack);
            CreateSizedSprite(
                backGenerated,
                "TerrainFill",
                filler ?? PrototypeSpriteFactory.GetWhitePixel(),
                new Vector2(room.SizeCells.x * 0.5f, 0.5f),
                new Vector2(room.SizeCells.x, 1f),
                Multiply(filler != null ? fillTint : new Color(0.10f, 0.48f, 0.48f, 1f), ambient),
                RoomVisualSorting.TerrainBack,
                fillMaterial ?? profile?.terrainMaterial,
                SpriteDrawMode.Tiled);

            Transform faceGenerated = CreateGeneratedRoot(terrainFace);
            if (edge == null)
            {
                CreateSizedSprite(
                    faceGenerated,
                    "TerrainEdgeFallback",
                    PrototypeSpriteFactory.GetWhitePixel(),
                    new Vector2(room.SizeCells.x * 0.5f, 0.94f),
                    new Vector2(room.SizeCells.x, 0.12f),
                    Multiply(new Color(0.36f, 0.70f, 0.69f, 1f), ambient),
                    RoomVisualSorting.Terrain,
                    null,
                    SpriteDrawMode.Tiled);
            }
            else
            {
                float width = Mathf.Max(0.5f, edge.bounds.size.x);
                int count = Mathf.CeilToInt(room.SizeCells.x / width);
                for (int index = 0; index < count; index++)
                {
                    float x = Mathf.Min(room.SizeCells.x - width * 0.5f, width * (index + 0.5f));
                    CreateSimpleSprite(
                        faceGenerated,
                        "TerrainEdge_" + index,
                        edge,
                        new Vector2(x, 0.85f),
                        Multiply(edgeTint, ambient),
                        RoomVisualSorting.Terrain,
                        edgeMaterial ?? profile.terrainMaterial);
                }
            }

            CreateSizedSprite(
                faceGenerated,
                "OneWayPlatformVisual",
                filler ?? PrototypeSpriteFactory.GetWhitePixel(),
                new Vector2(room.SizeCells.x * 0.5f, 3f),
                new Vector2(3f, 0.2f),
                Multiply(filler != null ? fillTint : new Color(0.36f, 0.70f, 0.69f, 1f), ambient),
                RoomVisualSorting.Terrain,
                fillMaterial ?? profile?.terrainMaterial,
                SpriteDrawMode.Tiled);
        }

        private void BuildDecorations(Transform props, Transform foreground)
        {
            if (profile == null)
            {
                return;
            }

            Sprite prop = profile.SelectRoomProp();
            if (prop != null)
            {
                TryCreateClearDecoration(
                    CreateGeneratedRoot(props),
                    "MoonProp",
                    prop,
                    new Vector2(room.SizeCells.x * 0.36f, room.SizeCells.y * 0.70f),
                    new Color(0.88f, 0.92f, 1f, 0.82f),
                    RoomVisualSorting.Fixture,
                    profile.backgroundMaterial);
            }

            Sprite foregroundProp = profile.SelectForegroundProp();
            if (foregroundProp != null)
            {
                TryCreateClearDecoration(
                    CreateGeneratedRoot(foreground),
                    "ForegroundProp",
                    foregroundProp,
                    new Vector2(room.SizeCells.x * 0.5f, room.SizeCells.y - 0.6f),
                    new Color(0.72f, 0.78f, 0.95f, 0.34f),
                    RoomVisualSorting.Foreground,
                    profile.terrainMaterial);
            }
        }

        private bool TryCreateClearDecoration(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 localPosition,
            Color color,
            int sortingOrder,
            Material material)
        {
            Bounds prospective = new Bounds(transform.TransformPoint(localPosition), sprite.bounds.size);
            GameplayClearZone[] zones = GetComponentsInChildren<GameplayClearZone>(true);
            for (int index = 0; index < zones.Length; index++)
            {
                if (zones[index] != null && prospective.Intersects(zones[index].WorldBounds))
                {
                    return false;
                }
            }

            CreateSimpleSprite(parent, name, sprite, localPosition, color, sortingOrder, material);
            return true;
        }

        private void ResolveVisual(
            string visualTileKey,
            Sprite[] categoryFallback,
            out Sprite sprite,
            out Material material,
            out Color tint)
        {
            if (profile != null && profile.TryResolve(visualTileKey, room.RoomId.GetHashCode(), out RegionVisual visual))
            {
                sprite = visual.Sprite;
                material = visual.Material;
                tint = visual.Tint;
                return;
            }

            sprite = categoryFallback != null && categoryFallback.Length > 0 ? categoryFallback[0] : null;
            material = null;
            tint = Color.white;
        }

        private SpriteRenderer CreateSizedSprite(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 localPosition,
            Vector2 size,
            Color color,
            int sortingOrder,
            Material material,
            SpriteDrawMode drawMode)
        {
            SpriteRenderer renderer = CreateRenderer(parent, name, sprite, localPosition, color, sortingOrder, material);
            renderer.sprite = CreateFullRectAdapter(sprite);
            renderer.drawMode = drawMode;
            renderer.size = size;
            return renderer;
        }

        private SpriteRenderer CreateSimpleSprite(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 localPosition,
            Color color,
            int sortingOrder,
            Material material)
        {
            return CreateRenderer(parent, name, sprite, localPosition, color, sortingOrder, material);
        }

        private Sprite CreateFullRectAdapter(Sprite source)
        {
            if (source == null || source.texture == null)
            {
                return source;
            }

            Rect sourceRect = source.rect;
            Rect textureRect = source.packed ? source.textureRect : sourceRect;
            Vector2 pivot = new Vector2(
                sourceRect.width <= 0f ? 0.5f : source.pivot.x / sourceRect.width,
                sourceRect.height <= 0f ? 0.5f : source.pivot.y / sourceRect.height);
            Sprite adapter = Sprite.Create(
                source.texture,
                textureRect,
                pivot,
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                source.border,
                false);
            adapter.name = source.name + "_ArtAdapter";
            adapter.hideFlags = HideFlags.HideAndDontSave;
            adapterSprites.Add(adapter);
            return adapter;
        }

        private SpriteRenderer CreateRenderer(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 localPosition,
            Color color,
            int sortingOrder,
            Material material)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            generatedRenderers.Add(renderer);
            return renderer;
        }

        private static Color Multiply(Color first, Color second)
        {
            return new Color(first.r * second.r, first.g * second.g, first.b * second.b, first.a * second.a);
        }

        private static Transform EnsureLayer(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            return layer.transform;
        }

        private static Transform CreateGeneratedRoot(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(GeneratedRootName);
            if (existing != null)
            {
                return existing;
            }

            GameObject generated = new GameObject(GeneratedRootName);
            generated.transform.SetParent(parent, false);
            return generated.transform;
        }

        private void ClearGeneratedVisuals()
        {
            Transform[] roots = { gridVisual, backgroundRoot, propRoot, actorRoot, vfxRoot, foregroundRoot };
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (roots[rootIndex] == null)
                {
                    continue;
                }

                Transform[] descendants = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int index = descendants.Length - 1; index >= 0; index--)
                {
                    Transform descendant = descendants[index];
                    if (descendant == null || descendant.name != GeneratedRootName)
                    {
                        continue;
                    }

                    // Destroy is deferred during Play Mode. Rename first so the replacement
                    // build never reuses a root that is already scheduled for destruction.
                    descendant.name = GeneratedRootName + "_Retired";
                    descendant.gameObject.SetActive(false);
                    if (Application.isPlaying)
                    {
                        Destroy(descendant.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(descendant.gameObject);
                    }
                }
            }
        }

        private void ClearAdapterSprites()
        {
            for (int index = 0; index < adapterSprites.Count; index++)
            {
                Sprite adapter = adapterSprites[index];
                if (adapter == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(adapter);
                }
                else
                {
                    DestroyImmediate(adapter);
                }
            }
            adapterSprites.Clear();
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }
    }
}

#endif
