#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Campaign.P10;
using StarNight.Campaign.P11;
using StarNight.Generation.P6;
using StarNight.MapHarness.P11;
using StarNight.Player;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P11CommonRegionsCampaignContract :
        MonoBehaviour
    {
        public const string HumanGatesPendingMessage =
            "HUMAN GATES PENDING: real-player comprehension samples "
            + "are required; automated structure and synthetic cohorts "
            + "cannot close P11 human gates.";

        [SerializeField] private GameObject campaignRoot;
        [SerializeField] private P11CampaignCatalog catalog;
        [SerializeField] private P10CampaignDirector2D p10Director;
        [SerializeField] private P10StageFlowController2D p10StageFlow;
        [SerializeField] private P11CampaignDirector2D director;
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private P11StageFlowController2D stageFlow;
        [SerializeField] private P11StageNode2D[] stageNodes =
            Array.Empty<P11StageNode2D>();
        [SerializeField] private P11RegionalBoss2D[] regionalBosses =
            Array.Empty<P11RegionalBoss2D>();
        [SerializeField] private P11MaruFinalBoss2D maruBoss;
        [SerializeField] private P11NaraeMailbox2D mailbox;
        [SerializeField] private P11YoungCrowEvent2D youngCrow;
        [SerializeField] private P11CrowNestEvent2D crowNest;
        [SerializeField] private P11SunEmberPickup2D sunEmber;
        [SerializeField] private P11MaruStoryTableau2D maruTableau;
        [SerializeField] private P11EndingPresentation2D
            endingPresentation;
        [SerializeField] private P11ComprehensionTelemetry2D telemetry;
        [SerializeField] private string[] issues = Array.Empty<string>();
        [SerializeField] private bool automatedValidationPassed;
        [SerializeField, TextArea(3, 24)] private string lastValidation =
            "Not validated.";

        public GameObject CampaignRoot => campaignRoot;
        public P11CampaignCatalog Catalog => catalog;
        public P10CampaignDirector2D P10Director => p10Director;
        public P10StageFlowController2D P10StageFlow =>
            p10StageFlow;
        public P11CampaignDirector2D Director => director;
        public P11StoryState2D StoryState => storyState;
        public P11StageFlowController2D StageFlow => stageFlow;
        public IReadOnlyList<P11StageNode2D> StageNodes =>
            stageNodes ?? Array.Empty<P11StageNode2D>();
        public IReadOnlyList<P11RegionalBoss2D> RegionalBosses =>
            regionalBosses ?? Array.Empty<P11RegionalBoss2D>();
        public P11MaruFinalBoss2D MaruBoss => maruBoss;
        public P11NaraeMailbox2D Mailbox => mailbox;
        public P11YoungCrowEvent2D YoungCrow => youngCrow;
        public P11CrowNestEvent2D CrowNest => crowNest;
        public P11SunEmberPickup2D SunEmber => sunEmber;
        public P11MaruStoryTableau2D MaruTableau => maruTableau;
        public P11EndingPresentation2D EndingPresentation =>
            endingPresentation;
        public P11ComprehensionTelemetry2D Telemetry => telemetry;
        public IReadOnlyList<string> Issues => issues;
        public string LastValidation => lastValidation;
        public bool AutomatedValidationPassed =>
            automatedValidationPassed;
        public bool HumanGatesPending =>
            telemetry == null || telemetry.HumanGatePending;
        public bool HumanGatesPassed =>
            telemetry != null && telemetry.AllHumanGatesPassed;

        public int StageNodeCount =>
            stageNodes != null ? stageNodes.Length : 0;
        public int RegionalBossCount =>
            regionalBosses != null ? regionalBosses.Length : 0;
        public int EnvironmentCount => stageNodes == null
            ? 0
            : stageNodes.Count(node =>
                node != null && node.Environment != null);
        public int MapHarnessCount => stageNodes == null
            ? 0
            : stageNodes.Count(node =>
                node != null
                && node.Environment != null
                && node.Environment.MapHarness != null);
        public int ActiveEnvironmentCount =>
            (p10StageFlow != null
                ? p10StageFlow.ActiveEnvironmentCount
                : 0)
            + (stageFlow != null
                ? stageFlow.ActiveEnvironmentCount
                : 0);
        public int PersistentPlayerCount => campaignRoot != null
            ? campaignRoot.GetComponentsInChildren<PlayerMotor2D>(true)
                .Length
            : 0;
        public int MainCameraCount => campaignRoot != null
            ? campaignRoot.GetComponentsInChildren<Camera>(true)
                .Count(item => item.CompareTag("MainCamera"))
            : 0;
        public int StarPostOfficeStageCount =>
            CountRegion(RoomRegion.StarPostOffice);
        public int SunriseGardenStageCount =>
            CountRegion(RoomRegion.SunriseGarden);
        public int PolarisObservatoryStageCount =>
            CountRegion(RoomRegion.PolarisObservatory);
        public int X2StageCount => stageNodes == null
            ? 0
            : stageNodes.Count(node =>
                node != null
                && node.Definition != null
                && node.Definition.StageSlot == P6StageSlot.X2);
        public int HumanSampleCount =>
            telemetry != null ? telemetry.SampleCount : 0;

        public void Configure(
            GameObject root,
            P11CampaignCatalog campaignCatalog,
            P10CampaignDirector2D firstBranchDirector,
            P10StageFlowController2D firstBranchFlow,
            P11CampaignDirector2D commonRegionDirector,
            P11StoryState2D commonStoryState,
            P11StageFlowController2D commonRegionFlow,
            P11StageNode2D[] nodes,
            P11RegionalBoss2D[] bosses,
            P11MaruFinalBoss2D finalMaruBoss,
            P11NaraeMailbox2D naraeMailbox,
            P11YoungCrowEvent2D crowEvent,
            P11CrowNestEvent2D nestEvent,
            P11SunEmberPickup2D emberPickup,
            P11MaruStoryTableau2D tableau,
            P11EndingPresentation2D presentation,
            P11ComprehensionTelemetry2D comprehensionTelemetry)
        {
            campaignRoot = root;
            catalog = campaignCatalog;
            p10Director = firstBranchDirector;
            p10StageFlow = firstBranchFlow;
            director = commonRegionDirector;
            storyState = commonStoryState;
            stageFlow = commonRegionFlow;
            stageNodes = nodes ?? Array.Empty<P11StageNode2D>();
            regionalBosses =
                bosses ?? Array.Empty<P11RegionalBoss2D>();
            maruBoss = finalMaruBoss;
            mailbox = naraeMailbox;
            youngCrow = crowEvent;
            crowNest = nestEvent;
            sunEmber = emberPickup;
            maruTableau = tableau;
            endingPresentation = presentation;
            telemetry = comprehensionTelemetry;
            issues = Array.Empty<string>();
            automatedValidationPassed = false;
            lastValidation = "Not validated.";
        }

        [ContextMenu("Validate P11 Common Regions Campaign")]
        public bool RefreshValidation()
        {
            var found = new List<string>();
            ValidatePersistentCoreAndHandoff(found);
            ValidateCatalogAndStages(found);
            ValidateRegionalX2Contracts(found);
            ValidateBosses(found);
            ValidateStoryInferenceAndEndings(found);
            ValidateHumanInstrumentation(found);

            issues = found.ToArray();
            automatedValidationPassed = issues.Length == 0;
            if (!automatedValidationPassed)
            {
                lastValidation = string.Join(
                    Environment.NewLine,
                    issues);
            }
            else
            {
                lastValidation = HumanGatesPending
                    ? "AUTOMATED PASS"
                        + Environment.NewLine
                        + HumanGatesPendingMessage
                    : "PASS";
            }

            return automatedValidationPassed;
        }

        public void ValidateOrThrow()
        {
            if (!RefreshValidation())
            {
                throw new InvalidOperationException(lastValidation);
            }
        }

        private void ValidatePersistentCoreAndHandoff(
            List<string> found)
        {
            if (campaignRoot == null)
            {
                found.Add(
                    "[P11_ROOT_MISSING] Campaign root is required.");
            }

            if (PersistentPlayerCount != 1 || MainCameraCount != 1)
            {
                found.Add(
                    "[P11_PERSISTENT_CORE_COUNT] Exactly one persistent "
                    + "PlayerMotor2D and one MainCamera are required; "
                    + $"players={PersistentPlayerCount}, "
                    + $"cameras={MainCameraCount}.");
            }

            if (p10Director == null
                || p10StageFlow == null
                || director == null
                || stageFlow == null)
            {
                found.Add(
                    "[P11_HANDOFF_COMPONENT_MISSING] P10 and P11 "
                    + "directors and stage flows are required.");
                return;
            }

            if (p10StageFlow.Director != p10Director
                || director.P10Director != p10Director
                || stageFlow.Director != director)
            {
                found.Add(
                    "[P11_HANDOFF_LINK_INVALID] The P10 director, P10 "
                    + "flow, P11 director handoff reference, and P11 "
                    + "flow are not linked consistently.");
            }

            if (stageFlow.PersistentPlayer
                    != p10StageFlow.PersistentPlayer
                || stageFlow.PersistentCamera
                    != p10StageFlow.PersistentCamera
                || stageFlow.MaruController
                    != p10StageFlow.MaruController)
            {
                found.Add(
                    "[P11_HANDOFF_CORE_CHANGED] P10 and P11 must share "
                    + "the exact persistent player, camera, and Maru "
                    + "runtime.");
            }

            if (!stageFlow.UsesOnePersistentPlayer
                || !stageFlow.UsesOnePersistentCamera
                || !stageFlow.InventoryPersistsAcrossStages
                || !stageFlow.StoryStatePersistsAcrossStages
                || !stageFlow.MaruLifecyclePersistsAcrossStages)
            {
                found.Add(
                    "[P11_PERSISTENT_CORE_INVALID] The P11 flow must "
                    + "preserve player/input/inventory/camera/story/Maru.");
            }

            if (ActiveEnvironmentCount > 1)
            {
                found.Add(
                    "[P11_ACTIVE_ENVIRONMENT_COUNT] At most one P10/P11 "
                    + "stage environment may be active; "
                    + $"active={ActiveEnvironmentCount}.");
            }

            if (stageFlow.ActiveNode != null
                && stageFlow.ActiveEnvironmentCount != 1)
            {
                found.Add(
                    "[P11_ACTIVE_NODE_ENVIRONMENT_MISMATCH] An active "
                    + "P11 stage requires exactly one active environment.");
            }
        }

        private void ValidateCatalogAndStages(List<string> found)
        {
            if (catalog == null)
            {
                found.Add(
                    "[P11_CATALOG_MISSING] P11 campaign catalog is "
                    + "required.");
            }
            else
            {
                string[] catalogIssues = catalog.ValidateCatalog();
                for (int index = 0;
                     index < catalogIssues.Length;
                     index++)
                {
                    found.Add(
                        "[P11_CATALOG_INVALID] "
                        + catalogIssues[index]);
                }

                P11CampaignRouteProof proof =
                    P11CampaignRouteProof.Evaluate(catalog);
                if (!proof.Passed)
                {
                    found.Add(
                        "[P11_ROUTE_PROOF_FAILED] Nine-stage regional "
                        + "rhythm, tool-free main progress, optional "
                        + "story, and ending reachability must all pass.");
                }
            }

            if (director == null
                || director.Catalog != catalog
                || director.StoryState != storyState
                || director.Telemetry != telemetry)
            {
                found.Add(
                    "[P11_DIRECTOR_REFERENCES_INVALID] P11 director "
                    + "catalog, story, and telemetry references are "
                    + "inconsistent.");
            }

            if (StageNodeCount != 9
                || EnvironmentCount != 9
                || MapHarnessCount != 9
                || StarPostOfficeStageCount != 3
                || SunriseGardenStageCount != 3
                || PolarisObservatoryStageCount != 3)
            {
                found.Add(
                    "[P11_STAGE_COUNTS_INVALID] Exactly nine stage nodes "
                    + "and map environments are required, with three per "
                    + "region; "
                    + $"nodes={StageNodeCount}, env={EnvironmentCount}, "
                    + $"maps={MapHarnessCount}, "
                    + $"post={StarPostOfficeStageCount}, "
                    + $"garden={SunriseGardenStageCount}, "
                    + $"observatory={PolarisObservatoryStageCount}.");
            }

            if (stageFlow != null
                && stageFlow.StageNodeCount != StageNodeCount)
            {
                found.Add(
                    "[P11_FLOW_STAGE_COUNT_INVALID] P11 flow and "
                    + "contract stage registries differ.");
            }

            var ids = new HashSet<P11StageId>();
            for (int index = 0; index < StageNodeCount; index++)
            {
                P11StageNode2D node = stageNodes[index];
                if (node == null || node.Definition == null)
                {
                    found.Add(
                        $"[P11_STAGE_NULL] Stage index {index} lacks a "
                        + "node or definition.");
                    continue;
                }

                if (!ids.Add(node.StageId))
                {
                    found.Add(
                        $"[P11_STAGE_ID_DUPLICATE] {node.StageId} appears "
                        + "more than once.");
                }

                P11StageDefinition catalogDefinition =
                    catalog != null
                        ? catalog.Find(node.StageId)
                        : null;
                if (node.Director != director
                    || !DefinitionsMatch(
                        node.Definition,
                        catalogDefinition))
                {
                    found.Add(
                        $"[P11_STAGE_BINDING_INVALID] {node.StageId} is "
                        + "not bound to the production director/catalog.");
                }

                if (stageFlow == null
                    || stageFlow.FindNode(node.StageId) != node)
                {
                    found.Add(
                        $"[P11_FLOW_NODE_BINDING_INVALID] {node.StageId} "
                        + "is not the node registered in the P11 flow.");
                }

                ValidateStageNode(node, found);
            }

            if (storyState == null
                || !storyState.MainProgressAlwaysAvailable
                || !storyState.OptionalStoryNeverGatesExit
                || director == null
                || director.MainProgressRequiresOptionalStory)
            {
                found.Add(
                    "[P11_OPTIONAL_STORY_GATES_PROGRESS] Main progress "
                    + "must remain tool-free and optional story must "
                    + "never gate an exit.");
            }
        }

        private static void ValidateStageNode(
            P11StageNode2D node,
            List<string> found)
        {
            if (!node.MainPathToolFree
                || !node.OptionalStoryNeverGatesExit)
            {
                found.Add(
                    $"[P11_STAGE_PROGRESS_INVALID] {node.StageId} "
                    + "requires a tool or optional story on its main path.");
            }

            P11StageEnvironment2D environment = node.Environment;
            if (environment == null
                || environment.StageId != node.StageId
                || !environment.ThemePresentationReady
                || !environment.MaruRoutingReady
                || environment.MapHarness == null)
            {
                found.Add(
                    $"[P11_STAGE_ENVIRONMENT_INVALID] {node.StageId} "
                    + "lacks its theme, Maru route, or map harness.");
                return;
            }

            P11MapStageHarness2D harness = environment.MapHarness;
            P11MapValidationReport report = harness.ValidateNow();
            if (report == null || !report.Passed)
            {
                found.Add(
                    $"[P11_MAP_HARNESS_FAILED] {node.StageId}: "
                    + (report != null
                        ? report.ToString()
                        : "validation report is null."));
            }

            if (!harness.MainPathToolFree
                || !harness.ExitDirectionContractSatisfied
                || !harness.CueRoutesSeparated)
            {
                found.Add(
                    $"[P11_MAP_ROUTE_INVALID] {node.StageId} lacks a "
                    + "tool-free route or separated text-free cues.");
            }

            if ((int)harness.StageSlot
                    != (int)node.Definition.StageSlot
                || !MapRegionMatches(
                    node.Definition.Region,
                    harness.Region))
            {
                found.Add(
                    $"[P11_MAP_IDENTITY_MISMATCH] {node.StageId} map "
                    + "region or stage slot differs from its catalog.");
            }
        }

        private void ValidateRegionalX2Contracts(
            List<string> found)
        {
            P11StageNode2D[] x2Nodes = stageNodes == null
                ? Array.Empty<P11StageNode2D>()
                : stageNodes.Where(node =>
                        node != null
                        && node.Definition != null
                        && node.Definition.StageSlot
                            == P6StageSlot.X2)
                    .ToArray();
            int distinctRegions = x2Nodes
                .Select(node => node.Definition.Region)
                .Distinct()
                .Count();
            if (x2Nodes.Length != 3 || distinctRegions != 3)
            {
                found.Add(
                    "[P11_X2_REGIONS_INVALID] One X-2 stage is required "
                    + "for each of the three common regions.");
                return;
            }

            for (int index = 0; index < x2Nodes.Length; index++)
            {
                P11StageNode2D node = x2Nodes[index];
                P11MapStageHarness2D harness =
                    node.Environment != null
                        ? node.Environment.MapHarness
                        : null;
                if (harness == null)
                {
                    found.Add(
                        $"[P11_X2_MAP_MISSING] {node.StageId} has no map "
                        + "harness.");
                    continue;
                }

                bool requiredRoles =
                    harness.HasRequiredRole(P11MapRoomRole.Start)
                    && harness.HasRequiredRole(
                        P11MapRoomRole.Landmark)
                    && harness.HasRequiredRole(
                        P11MapRoomRole.MaruRoom)
                    && harness.HasRequiredRole(
                        P11MapRoomRole.RecordRoom)
                    && harness.HasRequiredRole(
                        P11MapRoomRole.ShopCorridor)
                    && harness.HasRequiredRole(P11MapRoomRole.Exit)
                    && (harness.HasRequiredRole(
                            P11MapRoomRole.LocalEvent)
                        || harness.HasRequiredRole(
                            P11MapRoomRole.RelicRoom));
                bool hasMainCue = harness.Cues.Any(cue =>
                    cue != null
                    && cue.Route == P11MapRouteKind.Main);
                bool hasOptionalCue = harness.Cues.Any(cue =>
                    cue != null
                    && cue.Route == P11MapRouteKind.Optional);
                if (!requiredRoles
                    || !harness.CueRoutesSeparated
                    || !hasMainCue
                    || !hasOptionalCue)
                {
                    found.Add(
                        $"[P11_X2_ROLE_CUE_INVALID] {node.StageId} needs "
                        + "landmark/Maru/archive/shop/event roles and "
                        + "separate main/optional direction cues.");
                }
            }
        }

        private void ValidateBosses(List<string> found)
        {
            var kinds = new HashSet<P11BossKind>();
            bool regionalBossesReady =
                RegionalBossCount == 2
                && regionalBosses.All(boss =>
                {
                    if (boss == null)
                    {
                        return false;
                    }

                    kinds.Add(boss.BossKind);
                    return boss.SupportsDirectSolution
                        && boss.SupportsToolFreeEnvironmentalSolution
                        && boss.FirstAttackSafelyDemonstratesEnvironment
                        && boss.BossStagePausesMaruClock
                        && boss.MaximumSimultaneousPatterns <= 2;
                })
                && kinds.SetEquals(
                    new[]
                    {
                        P11BossKind.Popo,
                        P11BossKind.SunFlower
                    });
            if (!regionalBossesReady)
            {
                found.Add(
                    "[P11_REGIONAL_BOSSES_INVALID] Popo and Sun Flower "
                    + "must each support direct and tool-free "
                    + "environmental solutions with a safe demonstration.");
            }

            if (maruBoss == null
                || !maruBoss.SupportsDirectEnding
                || !maruBoss.SupportsEnvironmentalEnding
                || !maruBoss.SupportsMemoryEnding
                || !maruBoss.FirstChargeDemonstratesPillarDamage
                || !maruBoss.MemoryBellAlwaysCameraVisible
                || maruBoss.MaximumSimultaneousPatterns > 2)
            {
                found.Add(
                    "[P11_MARU_BOSS_INVALID] Maru must support normal, "
                    + "environmental, and memory solutions with readable "
                    + "pillar and bell demonstrations.");
            }
        }

        private void ValidateStoryInferenceAndEndings(
            List<string> found)
        {
            if (mailbox == null
                || mailbox.StoryState != storyState
                || !mailbox.VisualInferenceReady
                || youngCrow == null
                || !youngCrow.LetterUseInferenceReady
                || !youngCrow.MainExitRemainsAvailable)
            {
                found.Add(
                    "[P11_LETTER_INFERENCE_INVALID] The mailbox and young "
                    + "crow need linked, text-free letter-use cues while "
                    + "the main exit remains available.");
            }

            if (crowNest == null
                || !crowNest.RequiresWaterAndHook
                || !crowNest.MainExitRemainsAvailable
                || sunEmber == null
                || !sunEmber.SunEmberUseInferenceReady)
            {
                found.Add(
                    "[P11_SUN_EMBER_INFERENCE_INVALID] The optional nest "
                    + "chain and matching Sun Ember/bell visuals are "
                    + "incomplete.");
            }

            if (maruTableau == null
                || maruTableau.BeatCount != 4
                || !maruTableau.MaruBasicsStructureReady
                || !maruTableau.ExplainsMaruIsNotAnOrdinaryEnemy
                || !maruTableau.ExplainsObjectsReturnToStart
                || !maruTableau
                    .ExplainsCommandCollarWithoutInventingCause)
            {
                found.Add(
                    "[P11_MARU_TABLEAU_INVALID] Maru's main-route tableau "
                    + "must contain four documented explanatory beats.");
            }

            if (endingPresentation == null
                || endingPresentation.CoreTragedyBeatCount != 5
                || !endingPresentation.MemoryStoryStructureReady
                || !endingPresentation.UsesOnlyDocumentedMemoryFacts)
            {
                found.Add(
                    "[P11_MEMORY_PRESENTATION_INVALID] The memory ending "
                    + "must present exactly five documented tragedy beats.");
            }

            if (director == null
                || !director.NormalEndingAvailableWithoutLetterOrEmber
                || director.MainProgressRequiresOptionalStory
                || endingPresentation == null
                || !endingPresentation
                    .NormalEndingAvailableWithoutMemoryItems)
            {
                found.Add(
                    "[P11_NORMAL_ENDING_GATED] The normal ending must not "
                    + "require the letter, Sun Ember, or memory route.");
            }
        }

        private void ValidateHumanInstrumentation(
            List<string> found)
        {
            if (telemetry == null || !telemetry.InstrumentationReady)
            {
                found.Add(
                    "[P11_HUMAN_TELEMETRY_MISSING] Real-player "
                    + "comprehension instrumentation is required.");
                return;
            }

            if (telemetry.AutomatedStructureCanSatisfyHumanGate
                || telemetry.CanAutomatedStructureSatisfyHumanGate(true))
            {
                found.Add(
                    "[P11_HUMAN_GATE_PROXY_FORBIDDEN] Automated structure "
                    + "or synthetic cohorts must never satisfy a human "
                    + "comprehension gate.");
            }
        }

        private int CountRegion(RoomRegion region)
        {
            return stageNodes == null
                ? 0
                : stageNodes.Count(node =>
                    node != null
                    && node.Definition != null
                    && node.Definition.Region == region);
        }

        private static bool MapRegionMatches(
            RoomRegion region,
            P11MapRegion mapRegion)
        {
            switch (region)
            {
                case RoomRegion.StarPostOffice:
                    return mapRegion == P11MapRegion.StarPostOffice;
                case RoomRegion.SunriseGarden:
                    return mapRegion == P11MapRegion.SunriseGarden;
                case RoomRegion.PolarisObservatory:
                    return mapRegion
                        == P11MapRegion.PolarisObservatory;
                default:
                    return false;
            }
        }

        private static bool DefinitionsMatch(
            P11StageDefinition first,
            P11StageDefinition second)
        {
            return first != null
                && second != null
                && first.StageId == second.StageId
                && string.Equals(
                    first.DisplayName,
                    second.DisplayName,
                    StringComparison.Ordinal)
                && first.Region == second.Region
                && first.StageSlot == second.StageSlot
                && first.Archetype == second.Archetype
                && first.TraversalAxis == second.TraversalAxis
                && first.Mechanics == second.Mechanics
                && first.Guarantees == second.Guarantees
                && first.Boss == second.Boss
                && string.Equals(
                    first.CoreActionSentence,
                    second.CoreActionSentence,
                    StringComparison.Ordinal)
                && first.RecommendedMinutesMin
                    == second.RecommendedMinutesMin
                && first.RecommendedMinutesMax
                    == second.RecommendedMinutesMax
                && first.MainPathToolFree
                    == second.MainPathToolFree
                && first.OptionalStoryNeverGatesExit
                    == second.OptionalStoryNeverGatesExit;
        }
    }
}

#endif
