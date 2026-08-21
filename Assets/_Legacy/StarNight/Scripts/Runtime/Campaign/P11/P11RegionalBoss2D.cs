#if LEGACY_DISABLED
using StarNight.Explosions;
using StarNight.Stages.P5;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11RegionalBoss2D :
        P5ContextInteractable2D,
        IExplosionReceiver2D
    {
        public const int RequiredStarKnots = 3;

        [SerializeField] private P11BossKind bossKind;
        [SerializeField] private P11StageNode2D stageNode;
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private SpriteRenderer[] starKnotVisuals =
            System.Array.Empty<SpriteRenderer>();
        [SerializeField] private GameObject[] environmentTargets =
            System.Array.Empty<GameObject>();
        [SerializeField] private P11BossPhase phase;
        [SerializeField] private int remainingStarKnots =
            RequiredStarKnots;
        [SerializeField] private bool[] completedTargets =
            new bool[RequiredStarKnots];
        [SerializeField] private int environmentProgress;
        [SerializeField] private bool priorEventSupportApplied;

        public P11BossKind BossKind => bossKind;
        public P11BossPhase Phase => phase;
        public int RemainingStarKnots => remainingStarKnots;
        public int EnvironmentProgress => environmentProgress;
        public bool IsDefeated => phase == P11BossPhase.Defeated;
        public bool PriorEventSupportApplied =>
            priorEventSupportApplied;
        public bool FirstAttackSafelyDemonstratesEnvironment => true;
        public bool SupportsDirectSolution => true;
        public bool SupportsToolFreeEnvironmentalSolution => true;
        public bool BossStagePausesMaruClock => true;
        public int MaximumSimultaneousPatterns => 2;

        public void Configure(
            P11BossKind kind,
            P11StageNode2D node,
            P11StoryState2D state,
            SpriteRenderer[] knots,
            GameObject[] targets)
        {
            bossKind = kind;
            stageNode = node;
            storyState = state;
            starKnotVisuals = knots
                ?? System.Array.Empty<SpriteRenderer>();
            environmentTargets = targets
                ?? System.Array.Empty<GameObject>();
            ConfigureInteraction(transform, 3.2f, 75);
            ResetEncounterForTests();
        }

        public bool BeginEncounter()
        {
            if (phase != P11BossPhase.Idle)
            {
                return false;
            }

            phase = P11BossPhase.SafeDemonstration;
            priorEventSupportApplied =
                bossKind == P11BossKind.Popo
                    ? storyState != null
                        && storyState.LostParcelSorted
                    : storyState != null
                        && storyState.CrowNestRestored;
            if (priorEventSupportApplied)
            {
                ApplyEnvironmentTarget(0);
            }

            RefreshVisuals();
            return true;
        }

        public bool AdvanceSafeDemonstration()
        {
            if (phase != P11BossPhase.SafeDemonstration)
            {
                return false;
            }

            phase = P11BossPhase.Active;
            return true;
        }

        public bool ExposeWeakPoint()
        {
            if (phase != P11BossPhase.Active)
            {
                return false;
            }

            phase = P11BossPhase.Vulnerable;
            return true;
        }

        public bool TryDirectWeakPointHit(
            P11BossSolutionInput input)
        {
            if (phase != P11BossPhase.Vulnerable
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
                phase = P11BossPhase.Active;
            }

            RefreshVisuals();
            return true;
        }

        public bool TryEnvironmentTarget(
            int targetIndex,
            P11BossSolutionInput input)
        {
            if (phase == P11BossPhase.SafeDemonstration)
            {
                AdvanceSafeDemonstration();
            }

            if ((phase != P11BossPhase.Active
                    && phase != P11BossPhase.Vulnerable)
                || !IsAllowedEnvironmentInput(input))
            {
                return false;
            }

            return ApplyEnvironmentTarget(targetIndex);
        }

        public void ResetEncounterForTests()
        {
            phase = P11BossPhase.Idle;
            remainingStarKnots = RequiredStarKnots;
            completedTargets = new bool[RequiredStarKnots];
            environmentProgress = 0;
            priorEventSupportApplied = false;
            for (int index = 0;
                 index < environmentTargets.Length;
                 index++)
            {
                environmentTargets[index]?.SetActive(true);
            }

            stageNode?.ResetForTests();
            RefreshVisuals();
        }

        public void ReceiveExplosion(ExplosionHit2D hit)
        {
            if (IsDefeated)
            {
                return;
            }

            if (phase == P11BossPhase.Idle)
            {
                BeginEncounter();
            }

            if (phase == P11BossPhase.SafeDemonstration)
            {
                AdvanceSafeDemonstration();
            }

            if (phase == P11BossPhase.Active)
            {
                ExposeWeakPoint();
            }

            if (phase == P11BossPhase.Vulnerable)
            {
                TryDirectWeakPointHit(P11BossSolutionInput.Bomb);
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
            if (phase == P11BossPhase.Idle)
            {
                return BeginEncounter();
            }

            if (phase == P11BossPhase.SafeDemonstration)
            {
                return AdvanceSafeDemonstration();
            }

            if (phase == P11BossPhase.Active)
            {
                return ExposeWeakPoint();
            }

            return TryDirectWeakPointHit(
                ResolveHeldTool(context.ToolInventory));
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
            phase = P11BossPhase.Defeated;
            remainingStarKnots = 0;
            stageNode?.MarkBossDefeated();
            if (bossKind == P11BossKind.Popo)
            {
                storyState?.MarkPopoDefeated();
            }
            else if (bossKind == P11BossKind.SunFlower)
            {
                storyState?.MarkSunFlowerDefeated();
            }

            RefreshVisuals();
        }

        private bool IsAllowedDirectInput(
            P11BossSolutionInput input)
        {
            if (input == P11BossSolutionInput.BasicWeakPoint
                || input == P11BossSolutionInput.Bomb
                || input == P11BossSolutionInput.Pickaxe
                || input == P11BossSolutionInput.Pestle)
            {
                return true;
            }

            return bossKind == P11BossKind.SunFlower
                && input == P11BossSolutionInput.Water;
        }

        private bool IsAllowedEnvironmentInput(
            P11BossSolutionInput input)
        {
            return bossKind == P11BossKind.Popo
                ? input == P11BossSolutionInput.ParcelReflection
                    || input == P11BossSolutionInput.ReturnStamp
                : input == P11BossSolutionInput.CrossedLightAndShadow
                    || input == P11BossSolutionInput.GrowingVine;
        }

        private static P11BossSolutionInput ResolveHeldTool(
            PlayerToolInventory2D toolInventory)
        {
            if (toolInventory == null
                || !toolInventory.HasHeldTool)
            {
                return P11BossSolutionInput.BasicWeakPoint;
            }

            switch (toolInventory.HeldTool.Kind)
            {
                case HandToolKind.Pickaxe:
                    return P11BossSolutionInput.Pickaxe;
                case HandToolKind.Pestle:
                    return P11BossSolutionInput.Pestle;
                case HandToolKind.WateringCan:
                    return P11BossSolutionInput.Water;
                case HandToolKind.Grapple:
                    return P11BossSolutionInput.Grapple;
                default:
                    return P11BossSolutionInput.BasicWeakPoint;
            }
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
