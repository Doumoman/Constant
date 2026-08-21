#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Player;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P8MaruProductionContract : MonoBehaviour
    {
        public const string ContractId =
            "P8_Maru_P5ProductionCompatibility_v1";

        [Header("P5 production authority")]
        [SerializeField] private string contractId = ContractId;
        [SerializeField] private Transform player;
        [SerializeField] private P5StageCoreLoop2D coreLoop;
        [SerializeField] private P5StageExit2D stageExit;
        [SerializeField] private P5MaruBellClock2D compatibilityClock;

        [Header("P8 production runtime")]
        [SerializeField] private P8MaruTarget2D playerTarget;
        [SerializeField] private P8MaruBiteController2D biteController;
        [SerializeField] private P8ReturnPile2D returnPile;
        [SerializeField] private P8MaruPursuer2D pursuer;
        [SerializeField] private P8MaruStageController2D stageController;

        [Header("Validation")]
        [SerializeField, TextArea(3, 16)] private string lastValidation =
            "Not validated.";

        public Transform Player => player;
        public P5StageCoreLoop2D CoreLoop => coreLoop;
        public P5StageExit2D StageExit => stageExit;
        public P5MaruBellClock2D CompatibilityClock =>
            compatibilityClock;
        public P8MaruTarget2D PlayerTarget => playerTarget;
        public P8MaruBiteController2D BiteController => biteController;
        public P8ReturnPile2D ReturnPile => returnPile;
        public P8MaruPursuer2D Pursuer => pursuer;
        public P8MaruStageController2D StageController =>
            stageController;
        public P8MaruTimeline2D DormantP8Timeline =>
            stageController != null ? stageController.Timeline : null;
        public string LastValidation => lastValidation;
        public bool ValidationPassed => lastValidation == "PASS";

        public void Configure(
            Transform actualPlayer,
            P5StageCoreLoop2D actualCoreLoop,
            P5StageExit2D actualExit,
            P5MaruBellClock2D p5CompatibilityClock,
            P8MaruTarget2D maruPlayerTarget,
            P8MaruBiteController2D bite,
            P8ReturnPile2D pile,
            P8MaruPursuer2D maruPursuer,
            P8MaruStageController2D controller)
        {
            contractId = ContractId;
            player = actualPlayer;
            coreLoop = actualCoreLoop;
            stageExit = actualExit;
            compatibilityClock = p5CompatibilityClock;
            playerTarget = maruPlayerTarget;
            biteController = bite;
            returnPile = pile;
            pursuer = maruPursuer;
            stageController = controller;
            lastValidation = "Configured; not validated.";
        }

        [ContextMenu("Validate P8 Maru Production Integration")]
        public void ValidateOrThrow()
        {
            var issues = new List<string>();
            ValidateIdentity(issues);
            ValidateReferences(issues);
            ValidateProductionScene(issues);
            ValidateP5Authority(issues);
            ValidateP8Links(issues);
            ValidateCompatibilityTimeline(issues);

            if (issues.Count == 0)
            {
                lastValidation = "PASS";
                return;
            }

            lastValidation = string.Join(Environment.NewLine, issues);
            throw new InvalidOperationException(
                "P8 Maru production contract failed:"
                + Environment.NewLine
                + lastValidation);
        }

        private void ValidateIdentity(List<string> issues)
        {
            if (contractId != ContractId)
            {
                issues.Add("P8 production contract identity is invalid.");
            }
        }

        private void ValidateReferences(List<string> issues)
        {
            Require(player, nameof(player), issues);
            Require(coreLoop, nameof(coreLoop), issues);
            Require(stageExit, nameof(stageExit), issues);
            Require(compatibilityClock, nameof(compatibilityClock), issues);
            Require(playerTarget, nameof(playerTarget), issues);
            Require(biteController, nameof(biteController), issues);
            Require(returnPile, nameof(returnPile), issues);
            Require(pursuer, nameof(pursuer), issues);
            Require(stageController, nameof(stageController), issues);
        }

        private void ValidateProductionScene(List<string> issues)
        {
            ValidateSameScene(player, nameof(player), issues);
            ValidateSameScene(coreLoop, nameof(coreLoop), issues);
            ValidateSameScene(stageExit, nameof(stageExit), issues);
            ValidateSameScene(
                compatibilityClock,
                nameof(compatibilityClock),
                issues);
            ValidateSameScene(playerTarget, nameof(playerTarget), issues);
            ValidateSameScene(
                biteController,
                nameof(biteController),
                issues);
            ValidateSameScene(returnPile, nameof(returnPile), issues);
            ValidateSameScene(pursuer, nameof(pursuer), issues);
            ValidateSameScene(
                stageController,
                nameof(stageController),
                issues);
        }

        private void ValidateP5Authority(List<string> issues)
        {
            if (player != null
                && (player.GetComponent<PlayerMotor2D>() == null
                    || player.GetComponent<PlayerInputAdapter>() == null
                    || player.GetComponent<PlayerRecovery>() == null))
            {
                issues.Add(
                    "P8 production player must be the actual P5 player.");
            }

            if (coreLoop != null
                && (coreLoop.StageExit != stageExit
                    || coreLoop.BellClock != compatibilityClock))
            {
                issues.Add(
                    "P5 CoreLoop must own the configured exit and "
                    + "compatibility bell clock.");
            }

            if (compatibilityClock != null
                && (!Approximately(
                        compatibilityClock.FirstBellSeconds,
                        140f)
                    || !Approximately(
                        compatibilityClock.SecondBellSeconds,
                        185f)
                    || !Approximately(
                        compatibilityClock.MaruDueSeconds,
                        215f)))
            {
                issues.Add(
                    "P5 Moon Palace 1-1 must retain 140, 185, and "
                    + "215 second compatibility bells.");
            }
        }

        private void ValidateP8Links(List<string> issues)
        {
            if (playerTarget != null
                && (player == null
                    || playerTarget.gameObject != player.gameObject
                    || playerTarget.TargetKind
                        != P8MaruTargetKind.Player))
            {
                issues.Add(
                    "The P8 Player target must be attached to the "
                    + "actual P5 player.");
            }

            if (pursuer != null
                && (pursuer.ReturnPile != returnPile
                    || pursuer.BiteController != biteController
                    || pursuer.RoomGraph == null))
            {
                issues.Add(
                    "The production Maru pursuer is not fully linked.");
            }

            if (stageController != null
                && (stageController.Pursuer != pursuer
                    || stageController.BiteController != biteController
                    || stageController.Timeline == null
                    || !stageController.UsesP5ClockCompatibility))
            {
                issues.Add(
                    "P8 StageController must use the P5 compatibility "
                    + "clock and the production pursuer/bite runtime.");
            }

            if (returnPile != null && returnPile.DepositAnchor == null)
            {
                issues.Add("Production Return Pile requires an anchor.");
            }

            if (!Approximately(
                    P8MaruBiteController2D.EscapeWindowSeconds,
                    2f))
            {
                issues.Add(
                    "The first Maru bite escape window must be 2 seconds.");
            }
        }

        private void ValidateCompatibilityTimeline(List<string> issues)
        {
            P8MaruTimeline2D p8Timeline = DormantP8Timeline;
            if (p8Timeline == null)
            {
                return;
            }

            if (p8Timeline.StageSlot != P6StageSlot.X1
                || !Approximately(p8Timeline.FirstBellSeconds, 140f)
                || !Approximately(p8Timeline.SecondBellSeconds, 185f)
                || !Approximately(
                    p8Timeline.NaturalMaruDueSeconds,
                    215f))
            {
                issues.Add(
                    "The dormant P8 X-1 profile must mirror "
                    + "140, 185, and 215 seconds.");
            }

            if (p8Timeline.IsRunning)
            {
                issues.Add(
                    "The separate P8 timeline must remain stopped while "
                    + "the P5 compatibility clock is authoritative.");
            }
        }

        private void ValidateSameScene(
            Component component,
            string label,
            List<string> issues)
        {
            if (component != null
                && component.gameObject.scene != gameObject.scene)
            {
                issues.Add($"{label} is outside the production scene.");
            }
        }

        private static bool Approximately(float first, float second)
        {
            return Mathf.Abs(first - second) <= 0.001f;
        }

        private static void Require(
            UnityEngine.Object value,
            string label,
            List<string> issues)
        {
            if (value == null)
            {
                issues.Add(
                    $"P8 production reference is missing: {label}.");
            }
        }
    }
}

#endif
