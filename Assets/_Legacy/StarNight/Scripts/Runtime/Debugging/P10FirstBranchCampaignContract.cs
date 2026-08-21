#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Campaign.P10;
using StarNight.Folklore.P9;
using StarNight.Player;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P10FirstBranchCampaignContract :
        MonoBehaviour
    {
        public const string ExpectedCampaignId =
            "P10_MoonPalace_FirstBranches_Production_v1";
        public const string CorridorReviewText =
            "P6 physical corridor module rhythm, readability, and room-to-"
            + "corridor transitions require a dedicated follow-up pass.";

        [SerializeField] private string campaignId =
            ExpectedCampaignId;
        [SerializeField] private P10CampaignCatalog catalog;
        [SerializeField] private P10CampaignDirector2D director;
        [SerializeField] private P10StageFlowController2D stageFlow;
        [SerializeField] private P10StageNode2D[] stageNodes =
            Array.Empty<P10StageNode2D>();
        [SerializeField] private P10KungtteokiBoss2D kungtteoki;
        [SerializeField] private P10BranchBoss2D[] branchBosses =
            Array.Empty<P10BranchBoss2D>();
        [SerializeField] private P9FolkloreChainState2D folkloreState;
        [SerializeField] private P9CorrespondenceEvent2D[]
            correspondenceEvents =
                Array.Empty<P9CorrespondenceEvent2D>();
        [SerializeField] private P10BranchStageEvent2D[] branchEvents =
            Array.Empty<P10BranchStageEvent2D>();
        [SerializeField] private P9StarArchive2D[] x2Archives =
            Array.Empty<P9StarArchive2D>();
        [SerializeField] private P9RecordGuestDirector2D[]
            recordGuestDirectors =
                Array.Empty<P9RecordGuestDirector2D>();
        [SerializeField] private P10RoutePortal2D[] routePortals =
            Array.Empty<P10RoutePortal2D>();
        [SerializeField] private P10RouteTelemetry2D telemetry;
        [SerializeField] private bool corridorReviewPending = true;
        [SerializeField, TextArea(2, 5)] private string
            corridorReviewNote = CorridorReviewText;
        [SerializeField] private bool humanTimingPlaytestPending = true;
        [SerializeField] private bool humanBranchFeelPlaytestPending =
            true;
        [SerializeField] private bool culturalReviewPending = true;
        [SerializeField] private string[] issues = Array.Empty<string>();
        [SerializeField, TextArea(3, 18)] private string lastValidation =
            "Not validated.";

        public string CampaignId => campaignId;
        public P10CampaignCatalog Catalog => catalog;
        public P10CampaignDirector2D Director => director;
        public P10StageFlowController2D StageFlow => stageFlow;
        public IReadOnlyList<P10StageNode2D> StageNodes =>
            stageNodes;
        public P10KungtteokiBoss2D Kungtteoki => kungtteoki;
        public IReadOnlyList<P10BranchBoss2D> BranchBosses =>
            branchBosses;
        public P9FolkloreChainState2D FolkloreState =>
            folkloreState;
        public IReadOnlyList<P9CorrespondenceEvent2D>
            CorrespondenceEvents => correspondenceEvents;
        public IReadOnlyList<P10BranchStageEvent2D> BranchEvents =>
            branchEvents;
        public IReadOnlyList<P9StarArchive2D> X2Archives =>
            x2Archives;
        public IReadOnlyList<P9RecordGuestDirector2D>
            RecordGuestDirectors => recordGuestDirectors;
        public IReadOnlyList<P10RoutePortal2D> RoutePortals =>
            routePortals;
        public P10RouteTelemetry2D Telemetry => telemetry;
        public bool CorridorReviewPending => corridorReviewPending;
        public string CorridorReviewNote => corridorReviewNote;
        public bool HumanTimingPlaytestPending =>
            humanTimingPlaytestPending;
        public bool HumanBranchFeelPlaytestPending =>
            humanBranchFeelPlaytestPending;
        public bool CulturalReviewPending => culturalReviewPending;
        public IReadOnlyList<string> Issues => issues;
        public string LastValidation => lastValidation;
        public bool ValidationPassed =>
            issues.Length == 0 && lastValidation == "PASS";

        public void Configure(
            P10CampaignCatalog campaignCatalog,
            P10CampaignDirector2D campaignDirector,
            P10StageFlowController2D flow,
            P10StageNode2D[] nodes,
            P10KungtteokiBoss2D moonBoss,
            P10BranchBoss2D[] firstBranchBosses,
            P9FolkloreChainState2D chainState,
            P9CorrespondenceEvent2D[] correspondence,
            P10BranchStageEvent2D[] stageEvents,
            P9StarArchive2D[] archives,
            P9RecordGuestDirector2D[] guestDirectors,
            P10RoutePortal2D[] portals,
            P10RouteTelemetry2D routeTelemetry)
        {
            campaignId = ExpectedCampaignId;
            catalog = campaignCatalog;
            director = campaignDirector;
            stageFlow = flow;
            stageNodes = nodes ?? Array.Empty<P10StageNode2D>();
            kungtteoki = moonBoss;
            branchBosses = firstBranchBosses
                ?? Array.Empty<P10BranchBoss2D>();
            folkloreState = chainState;
            correspondenceEvents = correspondence
                ?? Array.Empty<P9CorrespondenceEvent2D>();
            branchEvents = stageEvents
                ?? Array.Empty<P10BranchStageEvent2D>();
            x2Archives = archives ?? Array.Empty<P9StarArchive2D>();
            recordGuestDirectors = guestDirectors
                ?? Array.Empty<P9RecordGuestDirector2D>();
            routePortals = portals ?? Array.Empty<P10RoutePortal2D>();
            telemetry = routeTelemetry;
            corridorReviewPending = true;
            corridorReviewNote = CorridorReviewText;
            humanTimingPlaytestPending = true;
            humanBranchFeelPlaytestPending = true;
            culturalReviewPending = true;
        }

        [ContextMenu("Validate P10 First Branch Campaign")]
        public bool RefreshValidation()
        {
            List<string> found = new List<string>();
            ValidateCore(found);
            ValidateCatalogAndRoutes(found);
            ValidateStageContent(found);
            ValidateFolkloreAndRecords(found);
            ValidateGates(found);
            issues = found.ToArray();
            lastValidation = issues.Length == 0
                ? "PASS"
                : string.Join(Environment.NewLine, issues);
            return issues.Length == 0;
        }

        public void ValidateOrThrow()
        {
            if (!RefreshValidation())
            {
                throw new InvalidOperationException(lastValidation);
            }
        }

        private void ValidateCore(List<string> found)
        {
            if (campaignId != ExpectedCampaignId)
            {
                found.Add("P10 campaign identity is incorrect.");
            }

            if (director == null || stageFlow == null)
            {
                found.Add(
                    "Campaign director and stage flow are required.");
                return;
            }

            if (!stageFlow.UsesOnePersistentPlayer
                || !stageFlow.UsesOnePersistentCamera
                || !stageFlow.InventoryPersistsAcrossStages
                || !stageFlow.FolkloreStatePersistsAcrossStages
                || !stageFlow.MaruLifecyclePersistsAcrossStages)
            {
                found.Add(
                    "One persistent Player/Input/Inventory/Camera/folklore/"
                    + "Maru core must survive all stage transitions.");
            }

            int playerCount = UnityEngine.Object.FindObjectsByType<
                    PlayerMotor2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Length;
            int inputCount = UnityEngine.Object.FindObjectsByType<
                    PlayerInputAdapter>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Length;
            int coreLoopCount = UnityEngine.Object.FindObjectsByType<
                    P5StageCoreLoop2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Length;
            int cameraCount = UnityEngine.Object.FindObjectsByType<
                    Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(item => item.CompareTag("MainCamera"));
            if (playerCount != 1
                || inputCount != 1
                || coreLoopCount != 1
                || cameraCount != 1)
            {
                found.Add(
                    $"Production core counts invalid: player={playerCount}, "
                    + $"input={inputCount}, loop={coreLoopCount}, "
                    + $"camera={cameraCount}.");
            }

            if (stageFlow.ActiveEnvironmentCount != 1)
            {
                found.Add(
                    "Exactly one stage environment must be active.");
            }

            int shopCount = UnityEngine.Object.FindObjectsByType<
                    P5MomoShop2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Length;
            int goldCount = UnityEngine.Object.FindObjectsByType<
                    P5GoldPickup2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Length;
            if (shopCount < 2 || goldCount < 27)
            {
                found.Add(
                    $"Campaign economy is incomplete: shops={shopCount}, "
                    + $"gold pickups={goldCount}.");
            }
        }

        private void ValidateCatalogAndRoutes(List<string> found)
        {
            if (catalog == null)
            {
                found.Add("P10 campaign catalog is missing.");
                return;
            }

            found.AddRange(catalog.ValidateCatalog());
            P10CampaignRouteProof proof =
                P10CampaignRouteProof.Evaluate(catalog);
            if (!proof.Passed
                || !proof.SingleBranchCommonEntryValid
                || !proof.CrossRoutesSkipOppositeX1)
            {
                found.Add(
                    "Normal/cross/common-region route proof failed.");
            }

            if (director.MainProgressRequiresCrossRoute
                || director.SecondBranchShopFrequencyMultiplier >= 1f
                    && director.IsSecondBranchMode
                || director.SecondBranchBellIntervalMultiplier >= 1f
                    && director.IsSecondBranchMode)
            {
                found.Add(
                    "Cross route must remain optional and use harder "
                    + "second-branch tuning.");
            }
        }

        private void ValidateStageContent(List<string> found)
        {
            if (stageNodes == null || stageNodes.Length != 9)
            {
                found.Add("Exactly nine production stage nodes are required.");
            }
            else
            {
                int unique = stageNodes
                    .Where(item => item != null)
                    .Select(item => item.StageId)
                    .Distinct()
                    .Count();
                if (unique != 9)
                {
                    found.Add("P10 stage node ids are not unique.");
                }

                for (int index = 0; index < stageNodes.Length; index++)
                {
                    P10StageNode2D node = stageNodes[index];
                    if (node == null
                        || !node.MainPathToolFree
                        || !node.OptionalEventsNeverGateExit
                        || !node.ThemePresentationReady)
                    {
                        found.Add(
                            $"Stage node {index} lacks physical/theme/"
                            + "tool-free contracts.");
                    }

                    if (node != null
                        && node.Definition.Region
                            == StarNight.Rooms.RoomRegion.MagpieBridge
                        && !node.Environment.MagpieTraversalReady)
                    {
                        found.Add(
                            $"{node.StageId} lacks physical crosswind, "
                            + "swaying bridge, or temporary platforms.");
                    }

                    if (node != null
                        && node.Definition.Region
                            == StarNight.Rooms.RoomRegion.DragonPalace
                        && !node.Environment.DragonTraversalReady)
                    {
                        found.Add(
                            $"{node.StageId} lacks physical cell currents "
                            + "or floodgates.");
                    }
                }
            }

            if (kungtteoki == null
                || kungtteoki.RemainingStarKnots
                    != P10KungtteokiBoss2D.RequiredStarKnots
                || !kungtteoki.SupportsDirectSolution
                || !kungtteoki.SupportsToolFreeEnvironmentalSolution
                || !kungtteoki.FirstFiveSecondDemonstrationReady
                || !kungtteoki.BossStagePausesMaruClock)
            {
                found.Add("Kungtteoki boss contract is incomplete.");
            }

            if (branchBosses == null || branchBosses.Length != 2
                || branchBosses.Any(
                    item => item == null
                        || !item.SupportsDirectSolution
                        || !item.SupportsToolFreeEnvironmentalSolution
                        || !item.BossStagePausesMaruClock))
            {
                found.Add(
                    "Knot Spider and Dragon Gatekeeper contracts are "
                    + "incomplete.");
            }

            int bossEnvironmentTargetCount =
                UnityEngine.Object.FindObjectsByType<
                    P10BossEnvironmentTarget2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Length;
            if (bossEnvironmentTargetCount != 9)
            {
                found.Add(
                    "Kungtteoki and both branch bosses require exactly "
                    + "three playable environmental targets each.");
            }

            if (branchEvents == null
                || branchEvents.Length != 2
                || branchEvents.Any(
                    item => item == null
                        || item.MainProgressBlocked))
            {
                found.Add(
                    "Nest and waterway events must be optional.");
            }

            int branchChoicePortals = routePortals.Count(
                item => item != null
                    && item.PortalKind
                        == P10RoutePortalKind.FirstBranchChoice);
            int crossPortals = routePortals.Count(
                item => item != null
                    && item.PortalKind
                        == P10RoutePortalKind.CrossRoute);
            int commonPortals = routePortals.Count(
                item => item != null
                    && item.PortalKind
                        == P10RoutePortalKind.CommonRegion);
            if (branchChoicePortals != 2
                || crossPortals != 2
                || commonPortals != 2)
            {
                found.Add(
                    "Two branch choices, two cross routes, and two common "
                    + "region exits are required.");
            }
        }

        private void ValidateFolkloreAndRecords(List<string> found)
        {
            if (folkloreState == null
                || !folkloreState.MainProgressAlwaysAvailable
                || !folkloreState.OptionalEventsIgnoredWithoutPenalty)
            {
                found.Add(
                    "P9 folklore state is not preserved in production.");
            }

            if (correspondenceEvents == null
                || correspondenceEvents.Length != 2
                || correspondenceEvents.Any(
                    item => item == null
                        || item.MainProgressBlocked
                        || !item.AlternativeResolutionAvailable
                        || !item.GiftPurposeInferenceReady))
            {
                found.Add(
                    "Magpie and turtle gift events are incomplete.");
            }

            if (x2Archives == null
                || x2Archives.Length != 3
                || x2Archives.Any(
                    item => item == null
                        || item.UnlockMethodCount < 2
                        || !item.OpeningDoesNotGateExit))
            {
                found.Add(
                    "Every regional X-2 requires one optional Star Archive.");
            }

            if (recordGuestDirectors == null
                || recordGuestDirectors.Length != 3
                || recordGuestDirectors.Any(
                    item => item == null
                        || !item.ArchivePlaced
                        || item.ExitProgressBlocked
                        || item.IgnoringArchiveHasPenalty))
            {
                found.Add(
                    "Every regional X-2 requires one optional Record Guest.");
            }
        }

        private void ValidateGates(List<string> found)
        {
            if (telemetry == null
                || !telemetry.InstrumentationReady
                || !Mathf.Approximately(
                    P10RouteTelemetry2D.NormalRouteTargetMinSeconds,
                    1800f)
                || !Mathf.Approximately(
                    P10RouteTelemetry2D.NormalRouteTargetMaxSeconds,
                    2400f))
            {
                found.Add(
                    "30-40 minute route telemetry is not wired.");
            }

            if (!humanTimingPlaytestPending
                || !humanBranchFeelPlaytestPending
                || !culturalReviewPending)
            {
                found.Add(
                    "Human timing/feel and cultural review must remain "
                    + "explicitly pending.");
            }

            if (!corridorReviewPending
                || corridorReviewNote != CorridorReviewText)
            {
                found.Add(
                    "The requested P6 corridor follow-up was not preserved.");
            }
        }
    }
}

#endif
