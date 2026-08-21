#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarNight.Debugging;
using StarNight.Generation.P6;
using StarNight.Population.P7;
using StarNight.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.Editor
{
    public static class P7PopulationLabBuilder
    {
        public const string ProfileFolder =
            "Assets/StarNight/Data/P7/RegionProfiles";

        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/"
            + "Cristal Sprites/Star particle.png";
        private const string CrystalSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/"
            + "Cristal Sprites/Crystal elements.png";
        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Square.png";

        [MenuItem("StarNight/P7/Rebuild Population and Economy Overlay")]
        public static void Rebuild()
        {
            P6RoomGraphLabBuilder.Rebuild();
        }

        [MenuItem("StarNight/P7/Validate Population and Economy Overlay")]
        public static void Validate()
        {
            P6RoomGraphLabBuilder.Validate();
            P7PopulationLabContract[] contracts =
                UnityEngine.Object.FindObjectsByType<P7PopulationLabContract>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (contracts.Length != 1)
            {
                throw new InvalidOperationException(
                    "The integrated P6/P7 Lab requires exactly one "
                    + nameof(P7PopulationLabContract) + ".");
            }

            if (!contracts[0].RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P7 population Lab validation failed:"
                    + Environment.NewLine
                    + contracts[0].LastValidation);
            }

            EditorSceneManager.SaveScene(
                contracts[0].gameObject.scene);
            Debug.Log(
                "[StarNight P7] Population and economy overlay validation PASS: "
                + $"seed={contracts[0].PopulationSeed}, "
                + $"markers={contracts[0].Markers.Count}, "
                + $"mainGold={contracts[0].MainPathGoldAvailableForShop}, "
                + $"fingerprint={contracts[0].PopulationFingerprint}");
        }

        internal static P7PopulationLabContract Decorate(
            Transform root,
            P6RoomGraphPlan graphPlan,
            IReadOnlyList<P6RoomCatalogEntry> catalog)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (graphPlan == null)
            {
                throw new ArgumentNullException(nameof(graphPlan));
            }

            P6RoomGraphLabContract graphContract =
                root.GetComponent<P6RoomGraphLabContract>();
            if (graphContract == null)
            {
                throw new InvalidOperationException(
                    "P7 must decorate the existing P6 Lab root.");
            }

            RemoveExistingOverlay(root);
            P7RegionPlacementProfileAsset[] assets =
                EnsureRegionProfileAssets();
            P7RegionPlacementProfileAsset profileAsset =
                assets.Single(asset =>
                    asset.Region == graphPlan.Rooms[0].Region);
            P7RoomPopulationDescriptor[] roomDescriptors =
                catalog.Select(entry =>
                    P7RoomPopulationDescriptor.Capture(
                        entry.Prefab.GetComponent<RoomTemplate2D>()))
                .ToArray();
            int populationSeed = P6DeterministicRandom.DeriveSeed(
                graphPlan.Seed,
                7);
            P7PopulationResult result = PopulationDirector.Generate(
                new P7PopulationRequest(
                    populationSeed,
                    P7StageGraphSnapshot.Capture(graphPlan),
                    roomDescriptors,
                    profileAsset.ToRuntimeProfile()));
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    "P7 Lab generation failed:"
                    + Environment.NewLine
                    + result.FailureReason);
            }

            Sprite star = LoadSprite(StarSpritePath);
            Sprite crystal = LoadSprite(CrystalSpritePath);
            Sprite square = LoadSprite(SquareSpritePath);
            Transform overlay =
                CreateChild(root, "P7PopulationEconomyOverlay").transform;
            Transform populationParent =
                CreateChild(overlay, "Population").transform;
            Transform economyParent =
                CreateChild(overlay, "RuntimeEconomy").transform;
            P7EconomyWallet2D wallet =
                CreateChild(economyParent, "EconomyWallet")
                    .AddComponent<P7EconomyWallet2D>();
            wallet.Configure();
            P7ShopInventory2D inventory =
                CreateChild(economyParent, "ShopInventory")
                    .AddComponent<P7ShopInventory2D>();

            P7PopulationMarker2D[] markers =
                BuildPopulationMarkers(
                    populationParent,
                    result.Plan.Placements,
                    wallet,
                    star,
                    crystal,
                    square);
            P7MomoShop2D shop = BuildMomoShop(
                economyParent,
                result.Plan.ShopOffers,
                wallet,
                inventory,
                star,
                crystal,
                square);

            P7PopulationLabContract contract =
                root.gameObject.AddComponent<P7PopulationLabContract>();
            contract.Configure(
                graphContract,
                profileAsset,
                populationSeed,
                result.Plan,
                result.Validation,
                markers,
                wallet,
                inventory,
                shop,
                overlay);
            if (!contract.RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P7 Lab contract failed before save:"
                    + Environment.NewLine
                    + contract.LastValidation);
            }

            return contract;
        }

        public static P7RegionPlacementProfileAsset[]
            EnsureRegionProfileAssets()
        {
            EnsureFolder(ProfileFolder);
            IReadOnlyList<P7RegionPlacementProfile> profiles =
                P7RegionProfileCatalog.CreateAll();
            var assets =
                new P7RegionPlacementProfileAsset[profiles.Count];
            for (int index = 0; index < profiles.Count; index++)
            {
                P7RegionPlacementProfile profile = profiles[index];
                string path =
                    $"{ProfileFolder}/P7_{profile.Region}_Population.asset";
                P7RegionPlacementProfileAsset asset =
                    AssetDatabase.LoadAssetAtPath<
                        P7RegionPlacementProfileAsset>(path);
                if (asset == null)
                {
                    asset =
                        ScriptableObject.CreateInstance<
                            P7RegionPlacementProfileAsset>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.Configure(profile);
                EditorUtility.SetDirty(asset);
                assets[index] = asset;
            }

            AssetDatabase.SaveAssets();
            return assets;
        }

        private static P7PopulationMarker2D[] BuildPopulationMarkers(
            Transform parent,
            IReadOnlyList<P7PopulationPlacement> placements,
            P7EconomyWallet2D wallet,
            Sprite star,
            Sprite crystal,
            Sprite square)
        {
            var markers =
                new P7PopulationMarker2D[placements.Count];
            for (int index = 0; index < placements.Count; index++)
            {
                P7PopulationPlacement placement = placements[index];
                GameObject markerObject = CreateChild(
                    parent,
                    $"Node_{placement.NodeId:00}_{placement.SlotId}_"
                    + placement.Kind);
                markerObject.transform.position = new Vector3(
                    placement.WorldCell.x + 0.5f,
                    placement.WorldCell.y + 0.5f,
                    -0.35f);
                P7PopulationMarker2D marker =
                    markerObject.AddComponent<P7PopulationMarker2D>();
                marker.Configure(placement);
                markers[index] = marker;

                Sprite visualSprite = SpriteFor(
                    placement.Kind,
                    star,
                    crystal,
                    square);
                Color color = ColorFor(placement.Kind);
                Vector2 size = SizeFor(placement.Kind);
                CreateSpritePart(
                    markerObject.transform,
                    "SpriteOnlyCue",
                    visualSprite,
                    color,
                    size,
                    placement.Kind == P7PopulationKind.Trap ? 45f : 0f,
                    90);

                if (placement.Kind == P7PopulationKind.SmallGold
                    || placement.Kind == P7PopulationKind.BigGold)
                {
                    CircleCollider2D collider =
                        markerObject.AddComponent<CircleCollider2D>();
                    collider.isTrigger = true;
                    collider.radius = 0.38f;
                    P7GoldPickup2D pickup =
                        markerObject.AddComponent<P7GoldPickup2D>();
                    pickup.Configure(wallet, placement.Value);
                }
                else if (placement.Kind == P7PopulationKind.TreasureChest)
                {
                    BoxCollider2D collider =
                        markerObject.AddComponent<BoxCollider2D>();
                    collider.isTrigger = true;
                    collider.size = new Vector2(0.9f, 0.7f);
                    P7TreasureChest2D chest =
                        markerObject.AddComponent<P7TreasureChest2D>();
                    chest.Configure(wallet, placement.Value);
                }
            }

            return markers;
        }

        private static P7MomoShop2D BuildMomoShop(
            Transform parent,
            IReadOnlyList<P7ShopOfferPlan> offers,
            P7EconomyWallet2D wallet,
            P7ShopInventory2D inventory,
            Sprite star,
            Sprite crystal,
            Sprite square)
        {
            if (offers.Count != P7EconomyRules.RequiredWorldOfferCount)
            {
                throw new InvalidOperationException(
                    "The P7 X-2 Lab must contain one three-offer Momo shop.");
            }

            GameObject shopObject = CreateChild(parent, "MomoWorldShop");
            P7MomoShop2D shop =
                shopObject.AddComponent<P7MomoShop2D>();
            var runtimeOffers = new P7MomoOffer2D[offers.Count];
            for (int index = 0; index < offers.Count; index++)
            {
                P7ShopOfferPlan offerPlan = offers[index];
                GameObject offerObject = CreateChild(
                    shopObject.transform,
                    $"Pedestal_{index:00}_{offerPlan.Product}");
                offerObject.transform.position = new Vector3(
                    offerPlan.WorldCell.x + 0.5f,
                    offerPlan.WorldCell.y + 0.5f,
                    -0.45f);

                GameObject pedestal = CreateSpritePart(
                    offerObject.transform,
                    "PhysicalPedestal",
                    square,
                    new Color(0.16f, 0.22f, 0.42f, 0.94f),
                    new Vector2(1.2f, 0.3f),
                    0f,
                    92);
                pedestal.transform.localPosition =
                    new Vector3(0f, -0.42f, 0f);
                GameObject productVisual = CreateSpritePart(
                    offerObject.transform,
                    "Product",
                    offerPlan.Price <= P7EconomyRules.BigGoldValue
                        ? star
                        : crystal,
                    ProductColor(offerPlan.Product),
                    new Vector2(0.62f, 0.62f),
                    0f,
                    94);
                BuildPriceIcons(
                    offerObject.transform,
                    star,
                    offerPlan.Price);

                P7MomoOffer2D runtimeOffer =
                    offerObject.AddComponent<P7MomoOffer2D>();
                runtimeOffer.Configure(
                    shop,
                    offerPlan.Product,
                    offerPlan.Price,
                    productVisual,
                    offerPlan.PriceIconCount);
                runtimeOffers[index] = runtimeOffer;
            }

            shop.Configure(wallet, inventory, runtimeOffers);
            return shop;
        }

        private static void BuildPriceIcons(
            Transform parent,
            Sprite star,
            int count)
        {
            Transform iconRoot =
                CreateChild(parent, "WorldGoldPrice").transform;
            float start = -(count - 1) * 0.11f;
            for (int index = 0; index < count; index++)
            {
                GameObject icon = CreateSpritePart(
                    iconRoot,
                    $"Gold_{index:00}",
                    star,
                    new Color(1f, 0.84f, 0.22f, 1f),
                    new Vector2(0.18f, 0.18f),
                    0f,
                    96);
                icon.transform.localPosition =
                    new Vector3(start + index * 0.22f, 0.72f, 0f);
            }
        }

        private static Sprite SpriteFor(
            P7PopulationKind kind,
            Sprite star,
            Sprite crystal,
            Sprite square)
        {
            switch (kind)
            {
                case P7PopulationKind.SmallGold:
                case P7PopulationKind.BigGold:
                    return star;
                case P7PopulationKind.TreasureChest:
                case P7PopulationKind.Enemy:
                    return crystal;
                case P7PopulationKind.Trap:
                    return square;
                default:
                    return star;
            }
        }

        private static Color ColorFor(P7PopulationKind kind)
        {
            switch (kind)
            {
                case P7PopulationKind.SmallGold:
                    return new Color(1f, 0.84f, 0.18f, 1f);
                case P7PopulationKind.BigGold:
                    return new Color(1f, 0.62f, 0.1f, 1f);
                case P7PopulationKind.TreasureChest:
                    return new Color(0.34f, 1f, 0.78f, 1f);
                case P7PopulationKind.Enemy:
                    return new Color(1f, 0.28f, 0.45f, 0.94f);
                case P7PopulationKind.Trap:
                    return new Color(1f, 0.55f, 0.16f, 0.88f);
                default:
                    return Color.white;
            }
        }

        private static Vector2 SizeFor(P7PopulationKind kind)
        {
            switch (kind)
            {
                case P7PopulationKind.SmallGold:
                    return new Vector2(0.34f, 0.34f);
                case P7PopulationKind.BigGold:
                    return new Vector2(0.5f, 0.5f);
                case P7PopulationKind.TreasureChest:
                    return new Vector2(0.8f, 0.65f);
                case P7PopulationKind.Enemy:
                    return new Vector2(0.65f, 0.75f);
                case P7PopulationKind.Trap:
                    return new Vector2(0.72f, 0.24f);
                default:
                    return Vector2.one * 0.5f;
            }
        }

        private static Color ProductColor(P7ShopProductKind product)
        {
            switch (product)
            {
                case P7ShopProductKind.RopeBundle3:
                    return new Color(0.86f, 0.68f, 0.34f, 1f);
                case P7ShopProductKind.BombBundle2:
                    return new Color(0.45f, 0.54f, 0.72f, 1f);
                case P7ShopProductKind.MoonCake:
                    return new Color(1f, 0.82f, 0.46f, 1f);
                case P7ShopProductKind.LaniLanternRecharge:
                    return new Color(0.35f, 0.9f, 1f, 1f);
                default:
                    return new Color(0.72f, 0.42f, 1f, 1f);
            }
        }

        private static GameObject CreateSpritePart(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            Vector2 size,
            float rotation,
            int sortingOrder)
        {
            GameObject part = CreateChild(parent, name);
            SpriteRenderer renderer =
                part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            part.transform.localRotation =
                Quaternion.Euler(0f, 0f, rotation);
            Vector2 spriteSize = sprite != null
                ? sprite.bounds.size
                : Vector2.one;
            part.transform.localScale = new Vector3(
                size.x / Mathf.Max(0.001f, spriteSize.x),
                size.y / Mathf.Max(0.001f, spriteSize.y),
                1f);
            return part;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"P7 source sprite is missing: {path}");
            }

            return sprite;
        }

        private static void RemoveExistingOverlay(Transform root)
        {
            P7PopulationLabContract existing =
                root.GetComponent<P7PopulationLabContract>();
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            Transform overlay = root.Find("P7PopulationEconomyOverlay");
            if (overlay != null)
            {
                UnityEngine.Object.DestroyImmediate(overlay.gameObject);
            }
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void EnsureFolder(string folder)
        {
            string normalized = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string parent =
                Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            string leaf = Path.GetFileName(normalized);
            if (string.IsNullOrWhiteSpace(parent)
                || string.IsNullOrWhiteSpace(leaf))
            {
                throw new InvalidOperationException(
                    $"Invalid P7 folder path: {folder}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}

#endif
