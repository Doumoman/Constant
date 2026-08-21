#if LEGACY_DISABLED
using StarNight.Explosions;
using StarNight.Maru.P8;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11MaruFinalBoss2D :
        P5ContextInteractable2D,
        IExplosionReceiver2D
    {
        public const int RequiredStarKnots = 5;
        public const int RequiredCommandPillars = 5;

        [SerializeField] private P11StageFlowController2D flow;
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private SpriteRenderer[] starKnotVisuals =
            System.Array.Empty<SpriteRenderer>();
        [SerializeField] private GameObject[] commandPillarVisuals =
            System.Array.Empty<GameObject>();
        [SerializeField] private GameObject firstBellUnlitVisual;
        [SerializeField] private GameObject firstBellLitVisual;
        [SerializeField] private GameObject memoryRouteVisual;
        [SerializeField] private P11BossPhase phase;
        [SerializeField] private int remainingStarKnots =
            RequiredStarKnots;
        [SerializeField] private bool[] brokenPillars =
            new bool[RequiredCommandPillars];
        [SerializeField] private int brokenPillarCount;
        [SerializeField] private int memoryPatternProgress;

        private readonly P11BellPattern memoryPattern =
            new P11BellPattern();

        public P11BossPhase Phase => phase;
        public int RemainingStarKnots => remainingStarKnots;
        public int BrokenPillarCount => brokenPillarCount;
        public int MemoryPatternProgress => memoryPattern.Progress;
        public bool IsDefeated => phase == P11BossPhase.Defeated;
        public bool SupportsDirectEnding => true;
        public bool SupportsEnvironmentalEnding => true;
        public bool SupportsMemoryEnding => true;
        public bool FirstChargeDemonstratesPillarDamage => true;
        public bool MemoryBellAlwaysCameraVisible => true;
        public int MaximumSimultaneousPatterns => 2;

        public void Configure(
            P11StageFlowController2D stageFlow,
            P11StoryState2D state,
            SpriteRenderer[] knots,
            GameObject[] pillars,
            GameObject unlitBell,
            GameObject litBell,
            GameObject memoryRoute)
        {
            flow = stageFlow;
            storyState = state;
            starKnotVisuals = knots
                ?? System.Array.Empty<SpriteRenderer>();
            commandPillarVisuals = pillars
                ?? System.Array.Empty<GameObject>();
            firstBellUnlitVisual = unlitBell;
            firstBellLitVisual = litBell;
            memoryRouteVisual = memoryRoute;
            ConfigureInteraction(transform, 3.5f, 76);
            ResetEncounterForTests();
        }

        public bool BeginEncounter()
        {
            if (phase != P11BossPhase.Idle)
            {
                return false;
            }

            phase = P11BossPhase.SafeDemonstration;
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

        public bool TryDirectStarKnotHit()
        {
            if (phase != P11BossPhase.Vulnerable)
            {
                return false;
            }

            remainingStarKnots =
                Mathf.Max(0, remainingStarKnots - 1);
            if (remainingStarKnots == 0)
            {
                return Finish(P11EndingKind.Normal);
            }

            phase = P11BossPhase.Active;
            RefreshVisuals();
            return true;
        }

        public bool TryBreakCommandPillar(int pillarIndex)
        {
            if (phase == P11BossPhase.SafeDemonstration)
            {
                AdvanceSafeDemonstration();
            }

            if ((phase != P11BossPhase.Active
                    && phase != P11BossPhase.Vulnerable)
                || pillarIndex < 0
                || pillarIndex >= RequiredCommandPillars
                || brokenPillars[pillarIndex])
            {
                return false;
            }

            brokenPillars[pillarIndex] = true;
            brokenPillarCount++;
            if (pillarIndex < commandPillarVisuals.Length)
            {
                commandPillarVisuals[pillarIndex]?.SetActive(false);
            }

            if (brokenPillarCount >= RequiredCommandPillars)
            {
                return Finish(P11EndingKind.EnvironmentalDisarm);
            }

            return true;
        }

        public bool TryLightFirstMemoryBell()
        {
            if (IsDefeated
                || storyState == null
                || !storyState.TryLightFirstMemoryBell())
            {
                return false;
            }

            RefreshVisuals();
            return true;
        }

        public bool TryRingMemoryBell(P8BellSignal signal)
        {
            if (IsDefeated
                || storyState == null
                || !storyState.CanAttemptMemoryPattern)
            {
                return false;
            }

            bool completed = memoryPattern.Push(signal);
            memoryPatternProgress = memoryPattern.Progress;
            if (completed)
            {
                storyState.MarkNaraeBellPatternCompleted();
            }

            RefreshVisuals();
            return completed;
        }

        public bool TryCompleteMemoryRoute()
        {
            if (storyState == null
                || !storyState.TryCompleteMemoryRoute())
            {
                return false;
            }

            return Finish(P11EndingKind.Memory);
        }

        public void ResetEncounterForTests()
        {
            phase = P11BossPhase.Idle;
            remainingStarKnots = RequiredStarKnots;
            brokenPillars = new bool[RequiredCommandPillars];
            brokenPillarCount = 0;
            memoryPattern.Reset();
            memoryPatternProgress = 0;
            for (int index = 0;
                 index < commandPillarVisuals.Length;
                 index++)
            {
                commandPillarVisuals[index]?.SetActive(true);
            }

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

            TryDirectStarKnotHit();
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

            return TryDirectStarKnotHit();
        }

        private bool Finish(P11EndingKind ending)
        {
            if (flow == null || !flow.ResolveFinalEnding(ending))
            {
                return false;
            }

            phase = P11BossPhase.Defeated;
            remainingStarKnots = 0;
            RefreshVisuals();
            return true;
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

            bool lit = storyState != null
                && storyState.FirstMemoryBellLit;
            if (firstBellUnlitVisual != null)
            {
                firstBellUnlitVisual.SetActive(!lit);
            }

            if (firstBellLitVisual != null)
            {
                firstBellLitVisual.SetActive(lit);
            }

            if (memoryRouteVisual != null)
            {
                memoryRouteVisual.SetActive(
                    storyState != null
                    && storyState.NaraeBellPatternCompleted);
            }
        }
    }
}

#endif
