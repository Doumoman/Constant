#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Debugging;
using StarNight.Generation.P6;
using StarNight.Grid;
using StarNight.Maru.P8;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Population.P7;
using StarNight.Rooms;
using StarNight.Stages.P5;
using StarNight.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.Editor
{
    public static class P8MaruSystemBuilder
    {
        private const string OverlayName = "P8MaruSystem";
        private const string MoonSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Moon B.png";
        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Square.png";
        private const string PlatformSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/"
            + "Platforms and doors.png";
        private const string RockSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/rocks.png";
        private const string ItemSpritePath =
            "Assets/2D Fantasy sprite bundle/Dungeon pack/Sprites/"
            + "dungeon items 2.png";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/"
            + "Cristal Sprites/Star particle.png";
        private const string CrystalSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/"
            + "Cristal Sprites/Crystal elements.png";
        private const string MaruSpritePath =
            "Assets/StarNight/Art/Player/char_black_full.png";

        [MenuItem("StarNight/P8/Rebuild Maru System Integration")]
        public static void Rebuild()
        {
            P5MoonPalaceSliceBuilder.Rebuild();
            P6RoomGraphLabBuilder.Rebuild();
            Debug.Log(
                "[StarNight P8] Rebuilt the playable P5 production "
                + "integration and the shared P6/P7/P8 X-2 Lab.");
        }

        [MenuItem("StarNight/P8/Validate Maru System Integration")]
        public static void Validate()
        {
            P5MoonPalaceSliceBuilder.Validate();
            P8MaruProductionContract production =
                UnityEngine.Object.FindFirstObjectByType<
                    P8MaruProductionContract>(
                    FindObjectsInactive.Include);
            if (production == null)
            {
                throw new InvalidOperationException(
                    "P8 production contract is missing.");
            }

            production.ValidateOrThrow();
            P6RoomGraphLabBuilder.Validate();
            P8MaruLabContract lab =
                UnityEngine.Object.FindFirstObjectByType<
                    P8MaruLabContract>(
                    FindObjectsInactive.Include);
            if (lab == null)
            {
                throw new InvalidOperationException(
                    "P8 integrated Lab contract is missing.");
            }

            lab.ValidateOrThrow();
            EditorSceneManager.SaveScene(lab.gameObject.scene);
            Debug.Log(
                "[StarNight P8] Production and integrated Lab "
                + "validation PASS.");
        }

        internal static P8MaruLabContract DecorateLab(
            Transform stageRoot,
            P6RoomGraphPlan graphPlan)
        {
            if (stageRoot == null)
            {
                throw new ArgumentNullException(nameof(stageRoot));
            }

            if (graphPlan == null)
            {
                throw new ArgumentNullException(nameof(graphPlan));
            }

            P6RoomGraphLabContract graphContract =
                stageRoot.GetComponent<P6RoomGraphLabContract>();
            P7PopulationLabContract populationContract =
                stageRoot.GetComponent<P7PopulationLabContract>();
            if (graphContract == null || populationContract == null)
            {
                throw new InvalidOperationException(
                    "P8 Lab must decorate the existing P6/P7 root.");
            }

            RemoveExistingOverlay(stageRoot);
            SourceArt art = LoadSourceArt();
            if (!art.IsComplete)
            {
                throw new InvalidOperationException(
                    "P8 Moon Palace source art is incomplete.");
            }

            if (graphContract.StageCamera != null
                && graphContract.StageCamera
                    .GetComponent<AudioListener>() == null)
            {
                graphContract.StageCamera.gameObject
                    .AddComponent<AudioListener>();
            }

            P7StageGraphSnapshot graphSnapshot =
                P7StageGraphSnapshot.Capture(graphPlan);
            int maruSeed = P6DeterministicRandom.DeriveSeed(
                graphPlan.Seed,
                8);
            P8MaruStagePlan stagePlan =
                P8MaruStagePlanner.Generate(
                    maruSeed,
                    graphSnapshot,
                    authoredStatueNodeIds:
                        P8MaruStagePlanner.CollectAuthoredStatueNodeIds(
                            graphPlan));
            if (!stagePlan.HasHomecomingStatue
                || !stagePlan.StatueDistanceSatisfied)
            {
                throw new InvalidOperationException(
                    "The integrated X-2 Lab requires one visible "
                    + "Homecoming Statue three to five rooms from Exit.");
            }

            Transform overlay =
                CreateChild(stageRoot, OverlayName).transform;
            Transform runtime =
                CreateChild(overlay, "Runtime").transform;
            Transform presentation =
                CreateChild(overlay, "MoonPalacePresentation").transform;

            P8MaruTimeline2D timeline =
                CreateChild(runtime, "ThreeBellTimeline")
                    .AddComponent<P8MaruTimeline2D>();
            timeline.Configure(
                P8MaruTimelineProfile.Create(P6StageSlot.X2));

            P8MaruRoomGraph2D roomGraph =
                CreateChild(runtime, "TerrainIgnoringRoomGraph")
                    .AddComponent<P8MaruRoomGraph2D>();
            roomGraph.Configure(BuildGraphNodes(graphPlan));

            Vector2 startPosition = FindNodeCenter(
                graphPlan,
                graphPlan.StartNodeId);
            Vector2 exitPosition = FindNodeCenter(
                graphPlan,
                graphPlan.ExitNodeId);
            Vector2 statuePosition = FindSlotPosition(
                graphContract,
                stagePlan.StatueNodeId,
                RoomContentSlotKind.MaruStatue,
                FindNodeCenter(graphPlan, stagePlan.StatueNodeId));
            Vector2 maruEntryPosition = FindSlotPosition(
                graphContract,
                stagePlan.StatueNodeId,
                RoomContentSlotKind.MaruEntry,
                statuePosition + Vector2.left * 2f);
            Vector2 pilePosition = FindSlotPosition(
                graphContract,
                stagePlan.ReturnPileNodeId,
                RoomContentSlotKind.SafeCell,
                startPosition) + new Vector2(1.15f, 0.35f);

            P8ReturnPile2D returnPile = BuildReturnPile(
                runtime,
                presentation,
                pilePosition,
                art);
            Transform playerProbe =
                CreateChild(runtime, "PlayerRouteProbe").transform;
            playerProbe.position = exitPosition;
            P8MaruTarget2D playerTarget =
                playerProbe.gameObject.AddComponent<P8MaruTarget2D>();
            playerTarget.Configure(
                P8MaruTargetKind.Player,
                deterministicOrder: 1000);

            P8MaruBiteController2D biteController =
                CreateChild(runtime, "FirstBiteEscape")
                    .AddComponent<P8MaruBiteController2D>();
            biteController.Configure(
                playerProbe,
                returnPile.DepositAnchor,
                timeline);

            GameObject pursuerObject =
                CreateChild(runtime, "MaruRoomGraphPursuer");
            pursuerObject.transform.position = maruEntryPosition;
            BuildMaruSprite(
                pursuerObject.transform,
                art,
                true);
            P8MaruPursuer2D pursuer =
                pursuerObject.AddComponent<P8MaruPursuer2D>();
            pursuer.Configure(
                roomGraph,
                returnPile,
                biteController,
                null,
                P8MaruPursuer2D.DefaultMoveSpeed);

            P8StarTear2D starTear = BuildStarTear(
                runtime,
                statuePosition + new Vector2(0f, 1.2f),
                null,
                null,
                populationContract.Wallet,
                art);
            P8MaruTarget2D tearTarget =
                starTear.gameObject.AddComponent<P8MaruTarget2D>();
            tearTarget.Configure(
                P8MaruTargetKind.LuminousTreasure,
                starTear.Carryable,
                deterministicOrder: 20);
            P8HomecomingStatue2D statue = BuildStatue(
                runtime,
                presentation,
                statuePosition,
                timeline,
                starTear,
                art);

            P8MaruTelemetry2D telemetry =
                CreateChild(runtime, "HumanGateTelemetry")
                    .AddComponent<P8MaruTelemetry2D>();
            telemetry.Configure(timeline, statue, true);
            P8MaruStageController2D controller =
                CreateChild(runtime, "MaruStageController")
                    .AddComponent<P8MaruStageController2D>();
            controller.Configure(
                timeline,
                pursuer,
                biteController,
                telemetry);
            CreateChild(runtime, "BiteAndRunFeedback")
                .AddComponent<P8MaruRunFeedback2D>()
                .Configure(biteController);

            P8MaruBellPresenter2D presenter =
                BuildBellPresentation(
                    runtime,
                    presentation,
                    timeline,
                    graphPlan,
                    art);
            BuildTrackingTrail(
                presentation,
                graphPlan,
                stagePlan.StatueNodeId,
                graphPlan.ExitNodeId,
                art);

            P8MaruGateSummary gate =
                P8MaruGateEvaluator.EvaluateSyntheticCohort(
                    1000,
                    maruSeed);
            P8MaruLabContract contract =
                stageRoot.gameObject.AddComponent<P8MaruLabContract>();
            contract.Configure(
                graphContract,
                populationContract,
                roomGraph,
                timeline,
                pursuer,
                returnPile,
                statue,
                starTear,
                presenter,
                gate);
            contract.ValidateOrThrow();
            return contract;
        }

        internal static P8MaruProductionContract DecorateProduction(
            Transform stageRoot,
            P5MoonStageContract p5Contract)
        {
            if (stageRoot == null)
            {
                throw new ArgumentNullException(nameof(stageRoot));
            }

            if (p5Contract == null)
            {
                throw new ArgumentNullException(nameof(p5Contract));
            }

            SourceArt art = LoadSourceArt();
            if (!art.IsComplete)
            {
                throw new InvalidOperationException(
                    "P8 production source art is incomplete.");
            }

            Transform old = stageRoot.Find(OverlayName);
            if (old != null)
            {
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            P8MaruProductionContract oldContract =
                stageRoot.GetComponent<P8MaruProductionContract>();
            if (oldContract != null)
            {
                UnityEngine.Object.DestroyImmediate(oldContract);
            }

            Transform overlay =
                CreateChild(stageRoot, OverlayName).transform;
            Transform runtime =
                CreateChild(overlay, "Runtime").transform;
            Transform presentation =
                CreateChild(overlay, "MoonPalacePresentation").transform;

            P5StageCoreLoop2D coreLoop =
                p5Contract.CoreLoop as P5StageCoreLoop2D;
            P5StageExit2D stageExit =
                p5Contract.StageExit as P5StageExit2D;
            P5MaruBellClock2D compatibilityClock =
                p5Contract.BellClock as P5MaruBellClock2D;
            P5RunState2D runState =
                p5Contract.RunState as P5RunState2D;
            if (coreLoop == null
                || stageExit == null
                || compatibilityClock == null
                || runState == null
                || p5Contract.Player == null)
            {
                throw new InvalidOperationException(
                    "P8 production integration requires the configured "
                    + "P5 player, CoreLoop, RunState, Exit, and bell clock.");
            }

            P8MaruTimeline2D dormantTimeline =
                CreateChild(runtime, "DormantX1Timeline")
                    .AddComponent<P8MaruTimeline2D>();
            dormantTimeline.Configure(
                P8MaruTimelineProfile.Create(P6StageSlot.X1));

            P8MaruRoomGraph2D roomGraph =
                CreateChild(runtime, "TerrainIgnoringRoomGraph")
                    .AddComponent<P8MaruRoomGraph2D>();
            roomGraph.Configure(
                BuildProductionGraphNodes(p5Contract.RoomPlacements));

            Vector2 pilePosition =
                (Vector2)p5Contract.Entry.position
                + new Vector2(1.5f, 0.55f);
            P8ReturnPile2D returnPile = BuildReturnPile(
                runtime,
                presentation,
                pilePosition,
                art);

            P8MaruTarget2D playerTarget =
                p5Contract.Player.GetComponent<P8MaruTarget2D>();
            if (playerTarget == null)
            {
                playerTarget =
                    p5Contract.Player.gameObject
                        .AddComponent<P8MaruTarget2D>();
            }

            playerTarget.Configure(
                P8MaruTargetKind.Player,
                deterministicOrder: 1000);

            P8MaruBiteController2D biteController =
                CreateChild(runtime, "FirstBiteEscape")
                    .AddComponent<P8MaruBiteController2D>();
            biteController.Configure(
                p5Contract.Player,
                returnPile.DepositAnchor,
                dormantTimeline,
                p5Contract.Player.GetComponent<Rigidbody2D>(),
                p5Contract.Player.GetComponent<PlayerInputAdapter>(),
                p5Contract.Player.GetComponent<PlayerMotor2D>(),
                p5Contract.Player.GetComponent<PlayerRecovery>(),
                p5Contract.Player.GetComponent<SafeCellTracker>(),
                p5Contract.Player.GetComponent<CarrySystem>(),
                p5Contract.Player.GetComponent<PlayerToolInventory2D>());

            GameObject maruObject =
                CreateChild(runtime, "MaruRoomGraphPursuer");
            maruObject.transform.position =
                p5Contract.Exit.position + Vector3.left * 2f;
            GameObject huntVisual = BuildMaruSprite(
                maruObject.transform,
                art,
                false);
            P8MaruPursuer2D pursuer =
                maruObject.AddComponent<P8MaruPursuer2D>();
            pursuer.Configure(
                roomGraph,
                returnPile,
                biteController,
                huntVisual,
                P8MaruPursuer2D.DefaultMoveSpeed);

            RegisterProductionTargets(p5Contract);
            BuildProductionTrackingCues(
                presentation,
                p5Contract,
                art);

            P8MaruTelemetry2D telemetry =
                CreateChild(runtime, "HumanGateTelemetry")
                    .AddComponent<P8MaruTelemetry2D>();
            telemetry.Configure(dormantTimeline, null, true);
            P8MaruStageController2D controller =
                CreateChild(runtime, "MaruStageController")
                    .AddComponent<P8MaruStageController2D>();
            controller.Configure(
                dormantTimeline,
                pursuer,
                biteController,
                telemetry,
                coreLoop,
                compatibilityClock,
                stageExit);
            CreateChild(runtime, "BiteAndRunFeedback")
                .AddComponent<P8MaruRunFeedback2D>()
                .Configure(biteController);

            P8MaruProductionContract contract =
                stageRoot.gameObject
                    .AddComponent<P8MaruProductionContract>();
            contract.Configure(
                p5Contract.Player,
                coreLoop,
                stageExit,
                compatibilityClock,
                playerTarget,
                biteController,
                returnPile,
                pursuer,
                controller);
            contract.ValidateOrThrow();
            return contract;
        }

        private static void RegisterProductionTargets(
            P5MoonStageContract contract)
        {
            if (contract.StoryPestle == null)
            {
                return;
            }

            HandToolPickup2D pickup =
                contract.StoryPestle.GetComponent<HandToolPickup2D>();
            if (pickup == null)
            {
                return;
            }

            P8MaruTarget2D target =
                pickup.GetComponent<P8MaruTarget2D>();
            if (target == null)
            {
                target = pickup.gameObject.AddComponent<P8MaruTarget2D>();
            }

            target.Configure(
                P8MaruTargetKind.DroppedHandTool,
                null,
                pickup,
                30);
        }

        private static P8ReturnPile2D BuildReturnPile(
            Transform runtimeParent,
            Transform presentationParent,
            Vector2 worldPosition,
            SourceArt art)
        {
            GameObject pileObject =
                CreateChild(runtimeParent, "ReturnPile");
            pileObject.transform.position = worldPosition;
            Transform anchor =
                CreateChild(pileObject.transform, "DepositAnchor")
                    .transform;
            anchor.localPosition = new Vector3(0f, 0.35f, 0f);
            P8ReturnPile2D pile =
                pileObject.AddComponent<P8ReturnPile2D>();
            pile.Configure(anchor, new Vector2(0.48f, 0.22f), 3);

            GameObject visual =
                CreateChild(presentationParent, "ReturnPileVisual");
            visual.transform.position = worldPosition;
            CreateSprite(
                visual.transform,
                "Pile",
                art.Item,
                Vector2.zero,
                new Vector2(1.45f, 0.9f),
                new Color(0.88f, 0.82f, 1f, 1f),
                95);
            CreateSprite(
                visual.transform,
                "PileGlow",
                art.Star,
                new Vector2(0f, 0.45f),
                new Vector2(0.75f, 0.75f),
                new Color(0.62f, 0.92f, 1f, 0.8f),
                96);
            return pile;
        }

        private static P8StarTear2D BuildStarTear(
            Transform parent,
            Vector2 position,
            GridWorld gridWorld,
            P5StageExit2D stageExit,
            P7EconomyWallet2D wallet,
            SourceArt art,
            P5RunState2D runState = null)
        {
            GameObject tearObject =
                CreateChild(parent, "StarTear_12Gold");
            tearObject.transform.position = position;
            Rigidbody2D body =
                tearObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.freezeRotation = true;
            BoxCollider2D collider =
                tearObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.68f, 0.78f);
            CarryableObject2D carryable =
                tearObject.AddComponent<CarryableObject2D>();
            carryable.Configure(
                gridWorld,
                body,
                collider,
                WorldObjectTraits.Carryable
                | WorldObjectTraits.Pullable,
                1f,
                6.5f,
                false);
            CreateSprite(
                tearObject.transform,
                "Crystal",
                art.Crystal,
                Vector2.zero,
                new Vector2(0.72f, 0.82f),
                new Color(0.74f, 0.96f, 1f, 1f),
                112);
            CreateSprite(
                tearObject.transform,
                "Glow",
                art.Star,
                Vector2.zero,
                new Vector2(1.05f, 1.05f),
                new Color(0.52f, 0.88f, 1f, 0.72f),
                111);
            P8StarTear2D tear =
                tearObject.AddComponent<P8StarTear2D>();
            tear.Configure(
                carryable,
                stageExit,
                runState,
                wallet);
            return tear;
        }

        private static P8HomecomingStatue2D BuildStatue(
            Transform runtimeParent,
            Transform presentationParent,
            Vector2 position,
            P8MaruTimeline2D timeline,
            P8StarTear2D starTear,
            SourceArt art)
        {
            GameObject statueObject =
                CreateChild(runtimeParent, "HomecomingStatue_1x2");
            statueObject.transform.position =
                position + new Vector2(0f, 0.5f);
            Rigidbody2D body =
                statueObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.freezeRotation = true;
            BoxCollider2D collider =
                statueObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.82f, 1.72f);

            GameObject intact =
                CreateSprite(
                    statueObject.transform,
                    "Intact",
                    art.Platform,
                    Vector2.zero,
                    new Vector2(0.95f, 1.9f),
                    new Color(0.72f, 0.78f, 0.92f, 1f),
                    108).gameObject;
            GameObject cracked =
                CreateSprite(
                    statueObject.transform,
                    "Cracked",
                    art.Rock,
                    Vector2.zero,
                    new Vector2(1.05f, 1.8f),
                    new Color(0.66f, 0.54f, 0.82f, 1f),
                    109).gameObject;
            GameObject broken =
                CreateSprite(
                    statueObject.transform,
                    "Destroyed",
                    art.Rock,
                    new Vector2(0f, -0.7f),
                    new Vector2(1.2f, 0.55f),
                    new Color(0.48f, 0.42f, 0.62f, 1f),
                    109).gameObject;
            cracked.SetActive(false);
            broken.SetActive(false);
            SpriteRenderer glow =
                CreateSprite(
                    statueObject.transform,
                    "VisibleStarTearGlow",
                    art.Star,
                    new Vector2(0f, 0.25f),
                    new Vector2(0.72f, 0.72f),
                    new Color(0.58f, 0.94f, 1f, 0.75f),
                    110);

            GameObject baseVisual =
                CreateChild(presentationParent, "StatueBase_1Cell");
            baseVisual.transform.position =
                position + new Vector2(0f, -0.47f);
            CreateSprite(
                baseVisual.transform,
                "Base",
                art.Square,
                Vector2.zero,
                new Vector2(1f, 0.28f),
                new Color(0.40f, 0.46f, 0.65f, 0.95f),
                106);

            P8HomecomingStatue2D statue =
                statueObject.AddComponent<P8HomecomingStatue2D>();
            statue.Configure(
                timeline,
                starTear,
                body,
                collider,
                intact,
                cracked,
                broken,
                glow);
            return statue;
        }

        private static P8MaruBellPresenter2D BuildBellPresentation(
            Transform runtimeParent,
            Transform presentationParent,
            P8MaruTimeline2D timeline,
            P6RoomGraphPlan graphPlan,
            SourceArt art)
        {
            GameObject bellObject =
                CreateChild(runtimeParent, "ThreeBellPresenter");
            Vector2 legendOrigin = new Vector2(
                graphPlan.CanvasBounds.xMin
                    * RoomTemplate2D.MacroCellSize.x + 2.2f,
                graphPlan.CanvasBounds.yMax
                    * RoomTemplate2D.MacroCellSize.y - 1.7f);

            GameObject first = BuildBellIcon(
                presentationParent,
                "FirstShortBell",
                legendOrigin,
                0.55f,
                new Color(0.75f, 0.9f, 1f, 1f),
                art);
            GameObject second = BuildBellIcon(
                presentationParent,
                "SecondShortBell",
                legendOrigin + Vector2.right * 1.0f,
                0.55f,
                new Color(0.6f, 0.76f, 1f, 1f),
                art);
            GameObject hunt = BuildBellIcon(
                presentationParent,
                "ThirdLongBell",
                legendOrigin + Vector2.right * 2.2f,
                0.9f,
                new Color(0.82f, 0.56f, 1f, 1f),
                art);
            P8MaruBellPresenter2D presenter =
                bellObject.AddComponent<P8MaruBellPresenter2D>();
            presenter.Configure(
                timeline,
                first,
                second,
                hunt);

            Transform preview =
                CreateChild(
                    presentationParent,
                    "ThreeBellPatternPreview").transform;
            BuildBellIcon(
                preview,
                "Short_A",
                legendOrigin + Vector2.down * 1.15f,
                0.42f,
                new Color(0.75f, 0.9f, 1f, 0.82f),
                art);
            BuildBellIcon(
                preview,
                "Short_B",
                legendOrigin
                    + Vector2.right * 0.8f
                    + Vector2.down * 1.15f,
                0.42f,
                new Color(0.65f, 0.8f, 1f, 0.86f),
                art);
            BuildBellIcon(
                preview,
                "Long",
                legendOrigin
                    + Vector2.right * 1.8f
                    + Vector2.down * 1.15f,
                0.72f,
                new Color(0.82f, 0.56f, 1f, 0.92f),
                art);
            return presenter;
        }

        private static GameObject BuildBellIcon(
            Transform parent,
            string name,
            Vector2 position,
            float size,
            Color color,
            SourceArt art)
        {
            GameObject icon = CreateChild(parent, name);
            icon.transform.position = position;
            CreateSprite(
                icon.transform,
                "MoonBell",
                art.Moon,
                Vector2.zero,
                new Vector2(size, size),
                color,
                122);
            CreateSprite(
                icon.transform,
                "BellSpark",
                art.Star,
                Vector2.zero,
                new Vector2(size * 1.3f, size * 1.3f),
                new Color(color.r, color.g, color.b, 0.6f),
                121);
            return icon;
        }

        private static GameObject BuildMaruSprite(
            Transform parent,
            SourceArt art,
            bool alwaysVisible)
        {
            GameObject visual =
                CreateChild(parent, "MaruVisual");
            CreateSprite(
                visual.transform,
                "Silhouette",
                art.Maru,
                Vector2.zero,
                new Vector2(0.95f, 1.35f),
                new Color(0.16f, 0.1f, 0.28f, 1f),
                118);
            CreateSprite(
                visual.transform,
                "TrackingAura",
                art.Star,
                new Vector2(0f, 0.2f),
                new Vector2(1.4f, 1.4f),
                new Color(0.62f, 0.36f, 0.94f, 0.42f),
                117);
            visual.SetActive(alwaysVisible);
            return visual;
        }

        private static void BuildTrackingTrail(
            Transform parent,
            P6RoomGraphPlan plan,
            int origin,
            int destination,
            SourceArt art)
        {
            IReadOnlyList<int> path =
                FindPath(plan, origin, destination);
            Transform trail =
                CreateChild(parent, "RoomGraphTrackingTrail").transform;
            for (int index = 0; index < path.Count; index++)
            {
                Vector2 center = FindNodeCenter(plan, path[index]);
                CreateSprite(
                    trail,
                    $"Footprint_{index:00}",
                    art.Star,
                    center + new Vector2(0f, 0.22f),
                    new Vector2(0.34f, 0.34f),
                    new Color(0.62f, 0.42f, 0.94f, 0.64f),
                    116);
            }
        }

        private static void BuildProductionTrackingCues(
            Transform parent,
            P5MoonStageContract contract,
            SourceArt art)
        {
            Transform cues =
                CreateChild(parent, "MaruTrackingCues").transform;
            for (int index = 0;
                 index < contract.RoomPlacements.Count;
                 index++)
            {
                P5FixedRoomPlacement room =
                    contract.RoomPlacements[index];
                Vector2 center =
                    room.Origin + (Vector2)room.Size * 0.5f;
                CreateSprite(
                    cues,
                    $"RoomGraphFootprint_{index:00}",
                    art.Star,
                    center + new Vector2(0f, -2.25f),
                    new Vector2(0.24f, 0.24f),
                    new Color(0.55f, 0.38f, 0.86f, 0.28f),
                    86);
            }
        }

        private static P8MaruRoomNode[] BuildGraphNodes(
            P6RoomGraphPlan plan)
        {
            var adjacency = new Dictionary<int, List<int>>();
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                adjacency[plan.Rooms[index].Id] = new List<int>();
            }

            for (int index = 0; index < plan.Edges.Count; index++)
            {
                P6GraphEdge edge = plan.Edges[index];
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }

            P8MaruRoomNode[] nodes =
                new P8MaruRoomNode[plan.Rooms.Count];
            Vector2Int macro = RoomTemplate2D.MacroCellSize;
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                var bounds = new Rect(
                    room.MacroBounds.xMin * macro.x,
                    room.MacroBounds.yMin * macro.y,
                    room.MacroBounds.width * macro.x,
                    room.MacroBounds.height * macro.y);
                List<int> neighbours = adjacency[room.Id];
                neighbours.Sort();
                nodes[index] = new P8MaruRoomNode(
                    room.Id,
                    bounds,
                    bounds.center,
                    neighbours.ToArray());
            }

            return nodes;
        }

        private static P8MaruRoomNode[] BuildProductionGraphNodes(
            IReadOnlyList<P5FixedRoomPlacement> rooms)
        {
            if (rooms == null || rooms.Count == 0)
            {
                throw new InvalidOperationException(
                    "P8 production room graph requires P5 fixed rooms.");
            }

            P8MaruRoomNode[] nodes =
                new P8MaruRoomNode[rooms.Count];
            for (int index = 0; index < rooms.Count; index++)
            {
                P5FixedRoomPlacement room = rooms[index];
                var bounds = new Rect(
                    room.Origin.x,
                    room.Origin.y,
                    room.Size.x,
                    room.Size.y);
                var neighbours = new List<int>(2);
                if (index > 0)
                {
                    neighbours.Add(index - 1);
                }

                if (index + 1 < rooms.Count)
                {
                    neighbours.Add(index + 1);
                }

                nodes[index] = new P8MaruRoomNode(
                    index,
                    bounds,
                    bounds.center,
                    neighbours.ToArray());
            }

            return nodes;
        }

        private static Vector2 FindSlotPosition(
            P6RoomGraphLabContract contract,
            int nodeId,
            RoomContentSlotKind kind,
            Vector2 fallback)
        {
            for (int index = 0;
                 index < contract.Placements.Count;
                 index++)
            {
                P6RoomGraphLabPlacement placement =
                    contract.Placements[index];
                if (placement.NodeId != nodeId
                    || placement.Instance == null)
                {
                    continue;
                }

                IReadOnlyList<RoomContentSlot2D> slots =
                    placement.Instance.ContentSlots;
                for (int slotIndex = 0;
                     slotIndex < slots.Count;
                     slotIndex++)
                {
                    if (slots[slotIndex] != null
                        && slots[slotIndex].Kind == kind)
                    {
                        return slots[slotIndex].transform.position;
                    }
                }
            }

            return fallback;
        }

        private static Vector2 FindNodeCenter(
            P6RoomGraphPlan plan,
            int nodeId)
        {
            Vector2Int macro = RoomTemplate2D.MacroCellSize;
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                if (room.Id == nodeId)
                {
                    return new Vector2(
                        (room.MacroBounds.xMin
                            + room.MacroBounds.width * 0.5f) * macro.x,
                        (room.MacroBounds.yMin
                            + room.MacroBounds.height * 0.5f) * macro.y);
                }
            }

            throw new InvalidOperationException(
                $"P8 could not find generated node {nodeId}.");
        }

        private static IReadOnlyList<int> FindPath(
            P6RoomGraphPlan plan,
            int origin,
            int destination)
        {
            var adjacency = new Dictionary<int, List<int>>();
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                adjacency[plan.Rooms[index].Id] = new List<int>();
            }

            for (int index = 0; index < plan.Edges.Count; index++)
            {
                P6GraphEdge edge = plan.Edges[index];
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }

            var previous = new Dictionary<int, int>();
            var visited = new HashSet<int> { origin };
            var queue = new Queue<int>();
            queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == destination)
                {
                    break;
                }

                List<int> neighbours = adjacency[current];
                neighbours.Sort();
                for (int index = 0; index < neighbours.Count; index++)
                {
                    int next = neighbours[index];
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!visited.Contains(destination))
            {
                return Array.Empty<int>();
            }

            var path = new List<int> { destination };
            int step = destination;
            while (step != origin)
            {
                step = previous[step];
                path.Add(step);
            }

            path.Reverse();
            return path;
        }

        private static void RemoveExistingOverlay(Transform root)
        {
            Transform existing = root.Find(OverlayName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    existing.gameObject);
            }

            P8MaruLabContract existingContract =
                root.GetComponent<P8MaruLabContract>();
            if (existingContract != null)
            {
                UnityEngine.Object.DestroyImmediate(existingContract);
            }
        }

        private static SourceArt LoadSourceArt()
        {
            return new SourceArt(
                LoadSprite(MoonSpritePath),
                LoadSprite(SquareSpritePath),
                LoadSprite(
                    PlatformSpritePath,
                    "Platforms and doors_4"),
                LoadSprite(RockSpritePath, "rocks_3"),
                LoadSprite(ItemSpritePath, "dungeon items 2_20"),
                LoadSprite(StarSpritePath),
                LoadSprite(
                    CrystalSpritePath,
                    "Crystal elements_3"),
                LoadSprite(MaruSpritePath, "char_black_full_1"));
        }

        private static Sprite LoadSprite(
            string path,
            string spriteName = null)
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(path);
            Sprite fallback = null;
            for (int index = 0; index < assets.Length; index++)
            {
                if (!(assets[index] is Sprite sprite))
                {
                    continue;
                }

                fallback ??= sprite;
                if (string.IsNullOrEmpty(spriteName)
                    || sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return fallback;
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static SpriteRenderer CreateSprite(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            Vector2 size,
            Color color,
            int sortingOrder)
        {
            GameObject visual = CreateChild(parent, name);
            visual.transform.localPosition =
                new Vector3(position.x, position.y, 0f);
            visual.transform.localScale =
                CalculateFitScale(sprite, size);
            SpriteRenderer renderer =
                visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Vector3 CalculateFitScale(
            Sprite sprite,
            Vector2 size)
        {
            if (sprite == null)
            {
                return Vector3.one;
            }

            Vector2 bounds = sprite.bounds.size;
            return new Vector3(
                bounds.x > 0.001f ? size.x / bounds.x : 1f,
                bounds.y > 0.001f ? size.y / bounds.y : 1f,
                1f);
        }

        private readonly struct SourceArt
        {
            public SourceArt(
                Sprite moon,
                Sprite square,
                Sprite platform,
                Sprite rock,
                Sprite item,
                Sprite star,
                Sprite crystal,
                Sprite maru)
            {
                Moon = moon;
                Square = square;
                Platform = platform;
                Rock = rock;
                Item = item;
                Star = star;
                Crystal = crystal;
                Maru = maru;
            }

            public Sprite Moon { get; }
            public Sprite Square { get; }
            public Sprite Platform { get; }
            public Sprite Rock { get; }
            public Sprite Item { get; }
            public Sprite Star { get; }
            public Sprite Crystal { get; }
            public Sprite Maru { get; }
            public bool IsComplete =>
                Moon != null
                && Square != null
                && Platform != null
                && Rock != null
                && Item != null
                && Star != null
                && Crystal != null
                && Maru != null;
        }
    }
}

#endif
