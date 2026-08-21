#if LEGACY_DISABLED
using StarNight.Explosions;
using StarNight.Folklore.P9;
using StarNight.Stages.P5;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10BranchBoss2D :
        P5ContextInteractable2D,
        IExplosionReceiver2D
    {
        public const int RequiredStarKnots = 3;

        [SerializeField] private P10BossKind bossKind;
        [SerializeField] private P9BranchKind branch;
        [SerializeField] private P10StageNode2D stageNode;
        [SerializeField] private P10BranchSupportState2D supportState;
        [SerializeField] private SpriteRenderer[] starKnotVisuals =
            System.Array.Empty<SpriteRenderer>();
        [SerializeField] private GameObject[] environmentTargets =
            System.Array.Empty<GameObject>();
        [SerializeField] private P10BossPhase phase;
        [SerializeField] private int remainingStarKnots =
            RequiredStarKnots;
        [SerializeField] private bool[] completedTargets =
            new bool[RequiredStarKnots];
        [SerializeField] private int environmentProgress;
        [SerializeField] private bool supportApplied;

        public P10BossKind BossKind => bossKind;
        public P9BranchKind Branch => branch;
        public P10BossPhase Phase => phase;
        public int RemainingStarKnots => remainingStarKnots;
        public int EnvironmentProgress => environmentProgress;
        public bool IsDefeated => phase == P10BossPhase.Defeated;
        public bool PriorStageSupportApplied => supportApplied;
        public bool SupportsDirectSolution => true;
        public bool SupportsToolFreeEnvironmentalSolution => true;
        public bool BossStagePausesMaruClock => true;
        public bool FallsAreNonLethal => true;
        public int MaximumSimultaneousPatterns => 2;

        public void Configure(
            P10BossKind kind,
            P9BranchKind bossBranch,
            P10StageNode2D node,
            P10BranchSupportState2D support,
            SpriteRenderer[] knots,
            GameObject[] targets)
        {
            bossKind = kind;
            branch = bossBranch;
            stageNode = node;
            supportState = support;
            starKnotVisuals = knots
                ?? System.Array.Empty<SpriteRenderer>();
            environmentTargets = targets
                ?? System.Array.Empty<GameObject>();
            ConfigureInteraction(transform, 3.2f, 75);
            ResetEncounterForTests();
        }

        public bool BeginEncounter()
        {
            if (phase != P10BossPhase.Idle)
            {
                return false;
            }

            phase = P10BossPhase.Active;
            supportApplied =
                branch == P9BranchKind.MagpieBridge
                    ? supportState != null
                        && supportState.KnotSpiderSupportReady
                    : supportState != null
                        && supportState.DragonGatekeeperSupportReady;
            if (supportApplied)
            {
                ApplyEnvironmentTarget(0);
            }

            RefreshVisuals();
            return true;
        }

        public bool ExposeWeakPoint()
        {
            if (phase != P10BossPhase.Active)
            {
                return false;
            }

            phase = P10BossPhase.Vulnerable;
            return true;
        }

        public bool TryDirectWeakPointHit(
            P10BossSolutionInput input)
        {
            if (phase != P10BossPhase.Vulnerable
                || !IsAllowedDirectInput(input))
            {
                return false;
            }

            remainingStarKnots =
                Mathf.Max(0, remainingStarKnots - 1);
            if (remainingStarKnots == 0)
            {
                Defeat();
            }
            else
            {
                phase = P10BossPhase.Active;
            }

            RefreshVisuals();
            return true;
        }

        public bool TryEnvironmentTarget(int targetIndex)
        {
            if (phase != P10BossPhase.Active
                && phase != P10BossPhase.Vulnerable)
            {
                return false;
            }

            return ApplyEnvironmentTarget(targetIndex);
        }

        public void ResetEncounterForTests()
        {
            phase = P10BossPhase.Idle;
            remainingStarKnots = RequiredStarKnots;
            completedTargets = new bool[RequiredStarKnots];
            for (int index = 0;
                 index < environmentTargets.Length;
                 index++)
            {
                environmentTargets[index]?.SetActive(true);
            }

            environmentProgress = 0;
            supportApplied = false;
            stageNode?.ResetForTests();
            RefreshVisuals();
        }

        public void ReceiveExplosion(ExplosionHit2D hit)
        {
            if (IsDefeated)
            {
                return;
            }

            if (phase == P10BossPhase.Idle)
            {
                BeginEncounter();
            }

            if (phase == P10BossPhase.Active)
            {
                ExposeWeakPoint();
            }

            if (phase == P10BossPhase.Vulnerable)
            {
                TryDirectWeakPointHit(P10BossSolutionInput.Bomb);
            }
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !IsDefeated;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            if (phase == P10BossPhase.Idle)
            {
                return BeginEncounter();
            }

            if (phase == P10BossPhase.Active)
            {
                return ExposeWeakPoint();
            }

            P10BossSolutionInput input =
                context.ToolInventory != null
                && context.ToolInventory.HasHeldTool
                && context.ToolInventory.HeldTool.Kind
                    == HandToolKind.Grapple
                    ? P10BossSolutionInput.Hook
                    : P10BossSolutionInput.BasicWeakPoint;
            return TryDirectWeakPointHit(input);
        }

        private bool ApplyEnvironmentTarget(int targetIndex)
        {
            if (targetIndex < 0
                || targetIndex >= RequiredStarKnots
                || completedTargets[targetIndex])
            {
                return false;
            }

            completedTargets[targetIndex] = true;
            environmentProgress++;
            if (targetIndex < environmentTargets.Length
                && environmentTargets[targetIndex] != null)
            {
                environmentTargets[targetIndex].SetActive(false);
            }

            if (environmentProgress >= RequiredStarKnots)
            {
                Defeat();
            }

            return true;
        }

        private void Defeat()
        {
            phase = P10BossPhase.Defeated;
            remainingStarKnots = 0;
            stageNode?.MarkBossDefeated();
            RefreshVisuals();
        }

        private bool IsAllowedDirectInput(
            P10BossSolutionInput input)
        {
            if (input == P10BossSolutionInput.BasicWeakPoint
                || input == P10BossSolutionInput.Bomb)
            {
                return true;
            }

            return bossKind == P10BossKind.KnotSpider
                && input == P10BossSolutionInput.Hook;
        }

        private void RefreshVisuals()
        {
            for (int index = 0;
                 index < starKnotVisuals.Length;
                 index++)
            {
                if (starKnotVisuals[index] != null)
                {
                    starKnotVisuals[index].enabled =
                        index < remainingStarKnots;
                }
            }
        }
    }
}

#endif
