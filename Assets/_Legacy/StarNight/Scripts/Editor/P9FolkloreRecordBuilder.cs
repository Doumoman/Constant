#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarNight.Debugging;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.Editor
{
    public static class P9FolkloreRecordBuilder
    {
        public const string CatalogPath =
            "Assets/StarNight/Data/P9/P9_RecordGuestCatalog.asset";

        private const string OverlayName = "P9FolkloreAndRecord";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/"
            + "Cristal Sprites/Star particle.png";
        private const string CrystalSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/"
            + "Cristal Sprites/Crystal elements.png";
        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Square.png";
        private const string ItemSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/"
            + "dungeon items 2.png";
        private const string RockSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/rocks.png";

        [MenuItem("StarNight/P9/Rebuild Folklore and Record Guest Integration")]
        public static void Rebuild()
        {
            P8MaruSystemBuilder.Rebuild();

            P6RoomGraphLabContract graph =
                UnityEngine.Object.FindFirstObjectByType<
                    P6RoomGraphLabContract>(
                    FindObjectsInactive.Include);
            P7PopulationLabContract population =
                UnityEngine.Object.FindFirstObjectByType<
                    P7PopulationLabContract>(
                    FindObjectsInactive.Include);
            P8MaruLabContract maru =
                UnityEngine.Object.FindFirstObjectByType<
                    P8MaruLabContract>(
                    FindObjectsInactive.Include);
            if (graph == null || population == null || maru == null)
            {
                throw new InvalidOperationException(
                    "P9 requires the integrated P6/P7/P8 Lab.");
            }

            P9RecordGuestCatalog catalog = RebuildCatalog();
            GameObject previous = GameObject.Find(OverlayName);
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous);
            }

            SourceArt art = LoadArt();
            GameObject root = new GameObject(OverlayName);
            root.transform.SetParent(graph.transform);

            P9FolkloreRecordLabContract contract =
                BuildOverlay(
                    root.transform,
                    graph,
                    population,
                    maru,
                    catalog,
                    art);
            if (!contract.RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P9 rebuild produced an invalid integrated Lab:"
                    + Environment.NewLine
                    + contract.LastValidation);
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[StarNight P9] Folklore chain and Record Guest "
                + "integration rebuilt and validated.");
        }

        [MenuItem("StarNight/P9/Validate Folklore and Record Guest Integration")]
        public static void Validate()
        {
            P8MaruSystemBuilder.Validate();
            P9FolkloreRecordLabContract[] contracts =
                UnityEngine.Object.FindObjectsByType<
                    P9FolkloreRecordLabContract>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (contracts.Length != 1)
            {
                throw new InvalidOperationException(
                    "The integrated Lab requires exactly one "
                    + nameof(P9FolkloreRecordLabContract) + ".");
            }

            contracts[0].ValidateOrThrow();
            EditorSceneManager.MarkSceneDirty(
                contracts[0].gameObject.scene);
            EditorSceneManager.SaveScene(
                contracts[0].gameObject.scene);
            Debug.Log(
                "[StarNight P9] Integrated validation PASS. "
                + "Human comprehension gates remain instrumented for "
                + "playtesting.");
        }

        private static P9FolkloreRecordLabContract BuildOverlay(
            Transform root,
            P6RoomGraphLabContract graph,
            P7PopulationLabContract population,
            P8MaruLabContract maru,
            P9RecordGuestCatalog catalog,
            SourceArt art)
        {
            GameObject systems = CreateChild(root, "Systems");
            P9FolkloreChainState2D chain =
                systems.AddComponent<P9FolkloreChainState2D>();
            chain.Configure(true, true);
            P9ComprehensionTelemetry2D telemetry =
                systems.AddComponent<P9ComprehensionTelemetry2D>();
            P9RecordGuestDirector2D director =
                systems.AddComponent<P9RecordGuestDirector2D>();

            List<P6RoomGraphLabPlacement> mainRooms =
                graph.Placements
                    .Where(item => item.OnMainPath
                        && (item.Role & RoomRole.Start) == 0
                        && (item.Role & RoomRole.Exit) == 0)
                    .OrderBy(item => item.NodeId)
                    .ToList();
            P6RoomGraphLabPlacement magpiePlacement =
                mainRooms.Count > 0
                    ? mainRooms[0]
                    : graph.Placements[0];
            P6RoomGraphLabPlacement turtlePlacement =
                mainRooms.Count > 1
                    ? mainRooms[mainRooms.Count - 1]
                    : graph.Placements[
                        Mathf.Min(1, graph.Placements.Count - 1)];
            P6RoomGraphLabPlacement archivePlacement =
                graph.Placements
                    .Where(item =>
                        (item.Role & RoomRole.RecordRoom) != 0)
                    .OrderBy(item => item.NodeId)
                    .First();
            P6RoomGraphLabPlacement relicPlacement =
                graph.Placements
                    .Where(item =>
                        (item.Role & RoomRole.RelicRoom) != 0)
                    .OrderBy(item => item.NodeId)
                    .FirstOrDefault();
            if ((relicPlacement.Role & RoomRole.RelicRoom) == 0)
            {
                relicPlacement = archivePlacement;
            }

            GameObject giftsRoot =
                CreateChild(root, "MoonPalaceGifts_2");
            P9FolkloreGiftPickup2D moonCake =
                CreateGiftPickup(
                    giftsRoot.transform,
                    "Gift_MoonCake",
                    RoomCenter(magpiePlacement)
                        + new Vector2(-1.2f, 2.2f),
                    P9FolkloreItemKind.MoonCake,
                    chain,
                    art.Item,
                    new Color(1f, 0.75f, 0.30f, 1f));
            P9FolkloreGiftPickup2D medicine =
                CreateGiftPickup(
                    giftsRoot.transform,
                    "Gift_JadeRabbitMedicine",
                    RoomCenter(turtlePlacement)
                        + new Vector2(-1.2f, 2.2f),
                    P9FolkloreItemKind.JadeRabbitMedicine,
                    chain,
                    art.Crystal,
                    new Color(0.52f, 1f, 0.78f, 1f));

            GameObject eventsRoot =
                CreateChild(root, "CorrespondenceEvents_2");
            P9CorrespondenceEvent2D magpieEvent =
                CreateCorrespondenceEvent(
                    eventsRoot.transform,
                    "Event_HungryMagpie_MoonCake",
                    RoomCenter(magpiePlacement)
                        + new Vector2(1.2f, 2.1f),
                    P9CorrespondenceEventKind.HungryMagpie,
                    chain,
                    art,
                    new Color(0.93f, 0.94f, 1f, 1f),
                    new Color(1f, 0.60f, 0.84f, 1f));
            P9CorrespondenceEvent2D turtleEvent =
                CreateCorrespondenceEvent(
                    eventsRoot.transform,
                    "Event_InjuredTurtle_Medicine",
                    RoomCenter(turtlePlacement)
                        + new Vector2(1.2f, 2.1f),
                    P9CorrespondenceEventKind.InjuredTurtle,
                    chain,
                    art,
                    new Color(0.38f, 0.84f, 1f, 1f),
                    new Color(0.45f, 1f, 0.72f, 1f));

            GameObject relicRoot =
                CreateChild(root, "BranchRelics_2");
            Vector2 relicCenter = RoomCenter(relicPlacement)
                + new Vector2(0f, 2.5f);
            P9BranchRelicPickup2D redThread =
                CreateRelic(
                    relicRoot.transform,
                    "Relic_RedWeaverThread",
                    relicCenter + Vector2.left * 1.1f,
                    P9BranchKind.MagpieBridge,
                    chain,
                    art.Star,
                    new Color(1f, 0.22f, 0.36f, 1f));
            P9BranchRelicPickup2D dragonOrb =
                CreateRelic(
                    relicRoot.transform,
                    "Relic_DragonPalaceOrb",
                    relicCenter + Vector2.right * 1.1f,
                    P9BranchKind.DragonPalace,
                    chain,
                    art.Crystal,
                    new Color(0.28f, 0.82f, 1f, 1f));

            GameObject archiveRoot =
                CreateChild(root, "StarArchive_RecordGuest");
            archiveRoot.transform.position =
                RoomCenter(archivePlacement)
                + new Vector2(0f, 1.6f);
            P9StarArchive2D archive =
                BuildArchive(archiveRoot.transform, art);

            GameObject followerObject =
                CreateChild(archiveRoot.transform, "RecordGuestFollower");
            followerObject.transform.localPosition = Vector3.zero;
            Transform guestVisual = CreatePart(
                followerObject.transform,
                art.Star,
                "PaperLightAvatar_SeoBok",
                Vector2.zero,
                new Vector2(0.75f, 0.75f),
                new Color(0.54f, 1f, 0.92f, 1f),
                79).transform;
            P9RecordGuestFollower2D follower =
                followerObject.AddComponent<P9RecordGuestFollower2D>();
            follower.Configure(
                catalog.FindForRegion(RoomRegion.MoonPalace),
                graph.StartMarker,
                guestVisual,
                archiveRoot.transform.position,
                5f);
            director.Configure(
                catalog,
                RoomRegion.MoonPalace,
                P6StageSlot.X2,
                0.99f,
                archive,
                follower);

            CreateReviewMarker(
                root,
                "Followup_P6_CorridorDesignReview_REQUIRED",
                graph.CorridorOverlay != null
                    ? (Vector2)graph.CorridorOverlay.position
                    : Vector2.zero,
                art.Square,
                new Color(1f, 0.45f, 0.18f, 0.2f));
            CreateReviewMarker(
                root,
                "Followup_RecordGuest_CulturalReview_REQUIRED",
                archiveRoot.transform.position + Vector3.up * 2f,
                art.Square,
                new Color(0.55f, 0.9f, 1f, 0.18f));

            P9FolkloreRecordLabContract contract =
                root.gameObject.AddComponent<
                    P9FolkloreRecordLabContract>();
            contract.Configure(
                graph,
                population,
                maru,
                chain,
                new[] { moonCake, medicine },
                new[] { magpieEvent, turtleEvent },
                new[] { redThread, dragonOrb },
                catalog,
                archive,
                director,
                follower,
                archivePlacement.NodeId,
                telemetry);
            return contract;
        }

        private static P9RecordGuestCatalog RebuildCatalog()
        {
            EnsureFolder(Path.GetDirectoryName(CatalogPath)?.Replace('\\', '/'));
            AssetDatabase.DeleteAsset(CatalogPath);
            P9RecordGuestCatalog catalog =
                ScriptableObject.CreateInstance<P9RecordGuestCatalog>();
            catalog.name = "P9_RecordGuestCatalog";
            catalog.Configure(
                new[]
                {
                    Definition(
                        "record_seo_bok",
                        "서복의 기록",
                        RoomRegion.MoonPalace,
                        "불로약을 찾는 여행",
                        P9RecordGuestImmediateSupport
                            .NearestRecoveryAndMedicine,
                        P9RecordGuestNextStageSupport.MoonCakeNearExit,
                        "가장 가까운 회복 수단과 약병의 방향을 가리킨다."),
                    Definition(
                        "record_hyecho",
                        "혜초의 기록",
                        RoomRegion.MagpieBridge,
                        "먼 길을 건넌 여행",
                        P9RecordGuestImmediateSupport
                            .SafeMainAndOptionalRoute,
                        P9RecordGuestNextStageSupport.RopeAtStart,
                        "안전한 메인 경로와 위험한 우회로를 구분해 가리킨다."),
                    Definition(
                        "record_jang_bogo",
                        "장보고의 기록",
                        RoomRegion.DragonPalace,
                        "바다 항로",
                        P9RecordGuestImmediateSupport
                            .CurrentAndHighestValueTreasure,
                        P9RecordGuestNextStageSupport.FirstFloodgateOpened,
                        "물살의 방향과 가장 가치 높은 보물의 위치를 표시한다."),
                    Definition(
                        "record_kim_jeong_ho",
                        "김정호의 기록",
                        RoomRegion.StarPostOffice,
                        "지도",
                        P9RecordGuestImmediateSupport
                            .StageGraphAndOneSecretRoom,
                        P9RecordGuestNextStageSupport
                            .StrongerExitDirectionMark,
                        "현재 방 그래프와 비밀방 하나의 존재를 보여준다."),
                    Definition(
                        "record_jang_yeong_sil",
                        "장영실의 기록",
                        RoomRegion.SunriseGarden,
                        "천문과 시간 장치",
                        P9RecordGuestImmediateSupport.NextBellCountdown,
                        P9RecordGuestNextStageSupport
                            .DelayNextBellTwelveSeconds,
                        "다음 방울까지 남은 시간을 시각적으로 보여준다."),
                    Definition(
                        "record_hong_dae_yong",
                        "홍대용의 기록",
                        RoomRegion.PolarisObservatory,
                        "별과 세계의 관찰",
                        P9RecordGuestImmediateSupport
                            .RelicAndMemoryDoorResonance,
                        P9RecordGuestNextStageSupport
                            .IlluminateOneBossChoiceDevice,
                        "유물 장치와 기억 문의 방향을 공명으로 표시한다.")
                });
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static P9RecordGuestDefinition Definition(
            string id,
            string displayName,
            RoomRegion region,
            string motif,
            P9RecordGuestImmediateSupport immediate,
            P9RecordGuestNextStageSupport next,
            string sentence)
        {
            P9RecordGuestDefinition definition =
                new P9RecordGuestDefinition();
            definition.Configure(
                id,
                displayName,
                region,
                motif,
                immediate,
                next,
                sentence,
                true);
            return definition;
        }

        private static P9FolkloreGiftPickup2D CreateGiftPickup(
            Transform parent,
            string name,
            Vector2 position,
            P9FolkloreItemKind kind,
            P9FolkloreChainState2D chain,
            Sprite sprite,
            Color color)
        {
            GameObject root = CreateChild(parent, name);
            root.transform.position = position;
            CreatePart(
                root.transform,
                sprite,
                "VisibleGift",
                Vector2.zero,
                new Vector2(0.75f, 0.75f),
                color,
                72);
            CreatePart(
                root.transform,
                sprite,
                "GiftGlow",
                Vector2.zero,
                new Vector2(1.15f, 1.15f),
                new Color(color.r, color.g, color.b, 0.18f),
                71);
            P9FolkloreGiftPickup2D pickup =
                root.AddComponent<P9FolkloreGiftPickup2D>();
            pickup.Configure(kind, chain);
            return pickup;
        }

        private static P9CorrespondenceEvent2D CreateCorrespondenceEvent(
            Transform parent,
            string name,
            Vector2 position,
            P9CorrespondenceEventKind kind,
            P9FolkloreChainState2D chain,
            SourceArt art,
            Color actorColor,
            Color responseColor)
        {
            GameObject root = CreateChild(parent, name);
            root.transform.position = position;
            Transform actor = CreatePart(
                root.transform,
                kind == P9CorrespondenceEventKind.HungryMagpie
                    ? art.Star
                    : art.Rock,
                "CorrespondenceNpc",
                Vector2.zero,
                new Vector2(0.9f, 0.9f),
                actorColor,
                75).transform;
            Transform gift = CreatePart(
                root.transform,
                kind == P9CorrespondenceEventKind.HungryMagpie
                    ? art.Item
                    : art.Crystal,
                "VisibleMatchingGift",
                new Vector2(-1.2f, 0.25f),
                new Vector2(0.55f, 0.55f),
                responseColor,
                76).transform;
            P9InferenceCue2D giftCue =
                gift.gameObject.AddComponent<P9InferenceCue2D>();
            giftCue.Configure(P9InferenceCueKind.VisibleGift);

            Transform silhouette = CreatePart(
                root.transform,
                art.Square,
                "MatchingGiftSilhouette",
                new Vector2(0f, 1.15f),
                new Vector2(0.8f, 0.8f),
                new Color(
                    responseColor.r,
                    responseColor.g,
                    responseColor.b,
                    0.24f),
                73).transform;
            P9InferenceCue2D silhouetteCue =
                silhouette.gameObject.AddComponent<P9InferenceCue2D>();
            silhouetteCue.Configure(
                P9InferenceCueKind.MatchingSilhouette,
                gift);

            GameObject attention = CreateLine(
                root.transform,
                art.Square,
                "NpcAttentionGesture",
                actor.position + Vector3.up * 0.2f,
                gift.position,
                0.08f,
                responseColor,
                77);
            P9InferenceCue2D attentionCue =
                attention.AddComponent<P9InferenceCue2D>();
            attentionCue.Configure(
                P9InferenceCueKind.NpcAttention,
                gift,
                0.02f,
                3f);

            GameObject assistance =
                CreateChild(root.transform, "GiftCreatedAssistance");
            for (int index = 0; index < 3; index++)
            {
                CreatePart(
                    assistance.transform,
                    art.Square,
                    $"AssistanceStep_{index:00}",
                    new Vector2(index * 0.8f - 0.8f, -1.1f + index * 0.25f),
                    new Vector2(0.7f, 0.12f),
                    responseColor,
                    74);
            }

            GameObject mainPath =
                CreateChild(root.transform, "MainPath_AlwaysOpen");
            CreatePart(
                mainPath.transform,
                art.Square,
                "MainRoute",
                new Vector2(0f, -1.45f),
                new Vector2(3.5f, 0.10f),
                new Color(0.45f, 1f, 0.72f, 0.72f),
                70);

            P9CorrespondenceEvent2D stageEvent =
                root.AddComponent<P9CorrespondenceEvent2D>();
            stageEvent.Configure(
                kind,
                chain,
                gift,
                silhouette,
                attention.transform,
                assistance,
                mainPath,
                true);
            return stageEvent;
        }

        private static P9BranchRelicPickup2D CreateRelic(
            Transform parent,
            string name,
            Vector2 position,
            P9BranchKind branch,
            P9FolkloreChainState2D chain,
            Sprite sprite,
            Color color)
        {
            GameObject root = CreateChild(parent, name);
            root.transform.position = position;
            CreatePart(
                root.transform,
                sprite,
                "RelicVisual",
                Vector2.zero,
                new Vector2(0.9f, 0.9f),
                color,
                76);
            CreatePart(
                root.transform,
                sprite,
                "RelicResonance",
                Vector2.zero,
                new Vector2(1.35f, 1.35f),
                new Color(color.r, color.g, color.b, 0.16f),
                75);
            P9BranchRelicPickup2D relic =
                root.AddComponent<P9BranchRelicPickup2D>();
            relic.Configure(branch, chain);
            return relic;
        }

        private static P9StarArchive2D BuildArchive(
            Transform root,
            SourceArt art)
        {
            GameObject sealedState =
                CreateChild(root, "SealedArchive");
            CreatePart(
                sealedState.transform,
                art.Square,
                "PaperDoor",
                Vector2.zero,
                new Vector2(2.7f, 2.1f),
                new Color(0.08f, 0.24f, 0.34f, 0.92f),
                72);
            CreatePart(
                sealedState.transform,
                art.Star,
                "ThreeWaySeal",
                new Vector2(0f, 0.15f),
                new Vector2(0.85f, 0.85f),
                new Color(0.45f, 0.94f, 1f, 1f),
                75);

            GameObject openState =
                CreateChild(root, "OpenedArchive");
            CreatePart(
                openState.transform,
                art.Square,
                "OpenPaperDoor",
                Vector2.zero,
                new Vector2(2.7f, 2.1f),
                new Color(0.22f, 0.68f, 0.78f, 0.42f),
                72);
            CreatePart(
                openState.transform,
                art.Star,
                "RecordLight",
                new Vector2(0f, 0.2f),
                new Vector2(1.2f, 1.2f),
                new Color(0.62f, 1f, 0.91f, 1f),
                76);

            Transform mainCue = CreatePart(
                root,
                art.Star,
                "MainRouteVisibleNameplateLight",
                new Vector2(0f, 1.45f),
                new Vector2(0.5f, 0.5f),
                new Color(0.50f, 0.98f, 1f, 1f),
                78).transform;
            P9InferenceCue2D cue =
                mainCue.gameObject.AddComponent<P9InferenceCue2D>();
            cue.Configure(P9InferenceCueKind.RouteResponse);

            P9StarArchive2D archive =
                root.gameObject.AddComponent<P9StarArchive2D>();
            archive.Configure(
                P9ArchiveUnlockMethods.SealLever
                | P9ArchiveUnlockMethods.CrackedOuterWall
                | P9ArchiveUnlockMethods.HookLatch,
                mainCue,
                sealedState,
                openState);
            return archive;
        }

        private static void CreateReviewMarker(
            Transform parent,
            string name,
            Vector2 position,
            Sprite square,
            Color color)
        {
            GameObject marker = CreateChild(parent, name);
            marker.transform.position = position;
            CreatePart(
                marker.transform,
                square,
                "ReviewMarker",
                Vector2.zero,
                new Vector2(0.35f, 0.35f),
                color,
                68);
        }

        private static Vector2 RoomCenter(
            P6RoomGraphLabPlacement placement)
        {
            if (placement.Instance == null)
            {
                return Vector2.zero;
            }

            return (Vector2)placement.Instance.transform.position
                + new Vector2(
                    placement.Instance.LogicalSize.x * 0.5f,
                    placement.Instance.LogicalSize.y * 0.5f);
        }

        private static GameObject CreateLine(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color,
            int sortingOrder)
        {
            Vector2 delta = end - start;
            GameObject line = CreatePart(
                parent,
                sprite,
                name,
                (start + end) * 0.5f,
                new Vector2(delta.magnitude, thickness),
                color,
                sortingOrder);
            line.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            return line;
        }

        private static GameObject CreatePart(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 localPosition,
            Vector2 size,
            Color color,
            int sortingOrder)
        {
            GameObject part = CreateChild(parent, name);
            part.transform.localPosition =
                new Vector3(localPosition.x, localPosition.y, 0f);
            SpriteRenderer renderer =
                part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            Vector2 spriteSize = sprite.bounds.size;
            part.transform.localScale = new Vector3(
                size.x / Mathf.Max(0.001f, spriteSize.x),
                size.y / Mathf.Max(0.001f, spriteSize.y),
                1f);
            return part;
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static SourceArt LoadArt()
        {
            return new SourceArt(
                LoadSprite(StarSpritePath),
                LoadSprite(CrystalSpritePath),
                LoadSprite(SquareSpritePath),
                LoadSprite(ItemSpritePath),
                LoadSprite(RockSpritePath));
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"P9 source art is missing: {path}");
            }

            return sprite;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)
                || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct SourceArt
        {
            public SourceArt(
                Sprite star,
                Sprite crystal,
                Sprite square,
                Sprite item,
                Sprite rock)
            {
                Star = star;
                Crystal = crystal;
                Square = square;
                Item = item;
                Rock = rock;
            }

            public Sprite Star { get; }
            public Sprite Crystal { get; }
            public Sprite Square { get; }
            public Sprite Item { get; }
            public Sprite Rock { get; }
        }
    }
}

#endif
