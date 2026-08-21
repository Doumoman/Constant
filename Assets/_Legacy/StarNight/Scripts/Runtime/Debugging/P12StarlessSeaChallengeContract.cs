#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Campaign.P11;
using StarNight.Campaign.P12;
using StarNight.Maru.P8;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P12StarlessSeaChallengeContract :
        MonoBehaviour
    {
        public const string HumanGatesPendingMessage =
            "HUMAN GATES PENDING: skilled-player completion (5-15%) "
            + "and unfair-death (<10%) rates require real playtests; "
            + "automated structure cannot close P12 human gates.";
        public const float ProjectileVisibleHalfHeight = 7f;
        public const float ProjectileVisibleHalfWidth =
            ProjectileVisibleHalfHeight * 16f / 9f;

        [SerializeField] private P12ChallengeCatalog catalog;
        [SerializeField] private P12ChallengeDirector2D director;
        [SerializeField] private P12StageFlowController2D stageFlow;
        [SerializeField] private P12StageNode2D[] stageNodes =
            Array.Empty<P12StageNode2D>();
        [SerializeField] private P12ChallengeTelemetry2D telemetry;
        [SerializeField] private P8HomecomingStatue2D[] homecomingStatues =
            Array.Empty<P8HomecomingStatue2D>();
        [SerializeField] private P11ParcelLauncher2D[] projectileLaunchers =
            Array.Empty<P11ParcelLauncher2D>();
        [SerializeField] private string[] issues = Array.Empty<string>();
        [SerializeField] private bool automatedValidationPassed;
        [SerializeField, TextArea(3, 12)] private string lastValidation =
            "Not validated.";

        public P12ChallengeCatalog Catalog => catalog;
        public P12ChallengeDirector2D Director => director;
        public P12StageFlowController2D StageFlow => stageFlow;
        public IReadOnlyList<P12StageNode2D> StageNodes =>
            stageNodes ?? Array.Empty<P12StageNode2D>();
        public P12ChallengeTelemetry2D Telemetry => telemetry;
        public IReadOnlyList<P8HomecomingStatue2D> HomecomingStatues =>
            homecomingStatues ?? Array.Empty<P8HomecomingStatue2D>();
        public IReadOnlyList<P11ParcelLauncher2D> ProjectileLaunchers =>
            projectileLaunchers ?? Array.Empty<P11ParcelLauncher2D>();
        public IReadOnlyList<string> Issues => issues;
        public string LastValidation => lastValidation;
        public bool AutomatedValidationPassed =>
            automatedValidationPassed;
        public bool HumanGatesPending =>
            telemetry == null || telemetry.HumanGatePending;

        public int StageNodeCount =>
            stageNodes != null ? stageNodes.Length : 0;
        public int EnvironmentCount => stageNodes == null
            ? 0
            : stageNodes.Count(node =>
                node != null && node.Environment != null);
        public int ActiveEnvironmentCount => stageNodes == null
            ? 0
            : stageNodes.Count(node =>
                node != null
                && node.Environment != null
                && node.Environment.EnvironmentRoot != null
                && node.Environment.EnvironmentRoot.activeSelf);

        public void Configure(
            P12ChallengeCatalog challengeCatalog,
            P12ChallengeDirector2D challengeDirector,
            P12StageFlowController2D challengeFlow,
            P12StageNode2D[] nodes,
            P12ChallengeTelemetry2D challengeTelemetry,
            P8HomecomingStatue2D[] statues = null,
            P11ParcelLauncher2D[] launchers = null)
        {
            catalog = challengeCatalog;
            director = challengeDirector;
            stageFlow = challengeFlow;
            stageNodes = nodes ?? Array.Empty<P12StageNode2D>();
            telemetry = challengeTelemetry;
            homecomingStatues = statues
                ?? Array.Empty<P8HomecomingStatue2D>();
            projectileLaunchers = launchers
                ?? Array.Empty<P11ParcelLauncher2D>();
            issues = Array.Empty<string>();
            automatedValidationPassed = false;
            lastValidation = "Not validated.";
        }

        [ContextMenu("Validate P12 Starless Sea Challenge")]
        public bool RefreshValidation()
        {
            var found = new List<string>();
            if (catalog == null)
            {
                found.Add(
                    "[P12_CATALOG_MISSING] P12 challenge catalog is "
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
                        "[P12_CATALOG_INVALID] " + catalogIssues[index]);
                }
            }

            if (StageNodeCount != 12 || EnvironmentCount != 12)
            {
                found.Add(
                    "[P12_STAGE_COUNTS_INVALID] Exactly twelve stage "
                    + "nodes and environments are required; "
                    + $"nodes={StageNodeCount}, env={EnvironmentCount}.");
            }

            if (director == null || director.AddsNewRules)
            {
                found.Add(
                    "[P12_GATE_A_NEW_RULES] The challenge must add no "
                    + "new controls, items, or rules.");
            }

            if (director == null
                || !director.ChallengeFailurePreservesMainEndings
                || !director.PermanentRewardIsRecordOnly)
            {
                found.Add(
                    "[P12_GATE_C_ENDING_PRESERVATION] Challenge failure "
                    + "must never undo the memory ending and the "
                    + "permanent reward must stay record-only.");
            }

            if (telemetry == null
                || telemetry.AutomatedStructureCanSatisfyHumanGate
                || telemetry.CanAutomatedStructureSatisfyHumanGate(true))
            {
                found.Add(
                    "[P12_HUMAN_GATE_PROXY_FORBIDDEN] Skilled completion "
                    + "and unfair-death gates need real-player telemetry; "
                    + "automated proxies are forbidden.");
            }

            if (ActiveEnvironmentCount > 1)
            {
                found.Add(
                    "[P12_ACTIVE_ENVIRONMENT_COUNT] At most one P12 "
                    + "stage environment may be active; "
                    + $"active={ActiveEnvironmentCount}.");
            }

            AppendComboIssues(found);
            AppendStatueChainIssues(found);
            AppendProjectileIssues(found);
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

        private void AppendComboIssues(List<string> found)
        {
            if (catalog == null)
            {
                return;
            }

            for (int index = 0; index < catalog.StageCount; index++)
            {
                P12StageDefinition stage = catalog.Stages[index];
                if (stage == null)
                {
                    continue;
                }

                if (P12ComboRules.TryFindUnapprovedPairing(
                        stage.Mechanics,
                        out P12StageMechanics first,
                        out P12StageMechanics second))
                {
                    found.Add(
                        "[P12_UNAPPROVED_PAIRING] "
                        + $"{stage.StageId} combines {first} with "
                        + $"{second} outside the approved pairing "
                        + "table.");
                }
            }

            if (P12ComboRules.RequiresSafeDemonstrationForFirstPairing
                && !P12ChallengeRouteProof.Evaluate(catalog)
                    .SafeDemonstrationValid)
            {
                found.Add(
                    "[P12_FIRST_PAIRING_DEMO] Every stage that shows a "
                    + "pairing for the first time needs a safe "
                    + "demonstration.");
            }
        }

        private void AppendStatueChainIssues(List<string> found)
        {
            if (!P12ComboRules.ForbidsAutoChainStatueDestruction)
            {
                return;
            }

            if (homecomingStatues == null
                || homecomingStatues.Length == 0)
            {
                found.Add(
                    "[P12_STATUE_CHAIN_UNWIRED] Homecoming statue "
                    + "references are required to prove auto-chain "
                    + "destruction is blocked.");
                return;
            }

            for (int index = 0;
                 index < homecomingStatues.Length;
                 index++)
            {
                P8HomecomingStatue2D statue = homecomingStatues[index];
                if (statue == null)
                {
                    found.Add(
                        "[P12_STATUE_CHAIN_UNWIRED] Homecoming statue "
                        + $"slot {index} is empty.");
                }
                else if (!statue.RequireTwoStagesForChainImpact)
                {
                    found.Add(
                        "[P12_STATUE_CHAIN_DESTRUCTION] "
                        + $"{statue.name} still allows one automatic "
                        + "chain impact to destroy an intact statue.");
                }
            }
        }

        private void AppendProjectileIssues(List<string> found)
        {
            if (!P12ComboRules.ForbidsOffscreenProjectiles)
            {
                return;
            }

            if (projectileLaunchers == null
                || projectileLaunchers.Length == 0)
            {
                found.Add(
                    "[P12_PROJECTILE_UNWIRED] Parcel launcher "
                    + "references are required to prove projectiles "
                    + "stay inside the stage camera view.");
                return;
            }

            for (int index = 0;
                 index < projectileLaunchers.Length;
                 index++)
            {
                P11ParcelLauncher2D launcher =
                    projectileLaunchers[index];
                if (launcher == null)
                {
                    found.Add(
                        "[P12_PROJECTILE_UNWIRED] Parcel launcher slot "
                        + $"{index} is empty.");
                    continue;
                }

                Transform anchor =
                    ResolveCameraAnchor(launcher.transform);
                if (anchor == null)
                {
                    found.Add(
                        "[P12_PROJECTILE_UNWIRED] "
                        + $"{launcher.name} is not inside a stage "
                        + "environment that owns a camera anchor.");
                    continue;
                }

                Vector2 offset =
                    launcher.transform.position - anchor.position;
                if (Mathf.Abs(offset.x) > ProjectileVisibleHalfWidth
                    || Mathf.Abs(offset.y)
                        > ProjectileVisibleHalfHeight)
                {
                    found.Add(
                        "[P12_PROJECTILE_OFFSCREEN] "
                        + $"{launcher.name} launches from outside the "
                        + $"stage camera view; offset={offset}.");
                }
            }
        }

        private Transform ResolveCameraAnchor(Transform target)
        {
            for (int index = 0; index < StageNodeCount; index++)
            {
                P12StageNode2D node = stageNodes[index];
                P12StageEnvironment2D environment =
                    node != null ? node.Environment : null;
                if (environment == null
                    || environment.EnvironmentRoot == null
                    || environment.CameraAnchor == null)
                {
                    continue;
                }

                if (target.IsChildOf(
                        environment.EnvironmentRoot.transform))
                {
                    return environment.CameraAnchor;
                }
            }

            return null;
        }

        public void ValidateOrThrow()
        {
            if (!RefreshValidation())
            {
                throw new InvalidOperationException(lastValidation);
            }
        }
    }
}

#endif
