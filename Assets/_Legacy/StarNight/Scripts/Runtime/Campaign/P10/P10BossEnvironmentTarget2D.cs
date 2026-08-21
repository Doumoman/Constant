#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10BossEnvironmentTarget2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P10KungtteokiBoss2D kungtteoki;
        [SerializeField] private P10BranchBoss2D branchBoss;
        [SerializeField, Min(0)] private int targetIndex;

        public int TargetIndex => targetIndex;
        public bool IsKungtteokiTarget => kungtteoki != null;
        public bool IsBranchTarget => branchBoss != null;

        public void Configure(
            P10KungtteokiBoss2D boss,
            int index)
        {
            kungtteoki = boss;
            branchBoss = null;
            targetIndex = Mathf.Max(0, index);
            ConfigureInteraction(transform, 1.65f, 85);
        }

        public void Configure(
            P10BranchBoss2D boss,
            int index)
        {
            branchBoss = boss;
            kungtteoki = null;
            targetIndex = Mathf.Max(0, index);
            ConfigureInteraction(transform, 1.65f, 85);
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return kungtteoki != null && !kungtteoki.IsDefeated
                || branchBoss != null && !branchBoss.IsDefeated;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            if (kungtteoki != null)
            {
                if (kungtteoki.Phase == P10BossPhase.Idle)
                {
                    kungtteoki.BeginEncounter();
                }

                if (kungtteoki.Phase
                    == P10BossPhase.SafeDemonstration)
                {
                    kungtteoki.TickEncounter(0f);
                }

                return kungtteoki.TryBreakCrackedFloor(targetIndex);
            }

            if (branchBoss == null)
            {
                return false;
            }

            if (branchBoss.Phase == P10BossPhase.Idle)
            {
                branchBoss.BeginEncounter();
            }

            return branchBoss.TryEnvironmentTarget(targetIndex);
        }
    }
}

#endif
