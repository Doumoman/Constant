#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11RegionalBossEnvironmentTarget2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P11RegionalBoss2D boss;
        [SerializeField, Min(0)] private int targetIndex;
        [SerializeField] private P11BossSolutionInput solutionInput;

        public int TargetIndex => targetIndex;
        public P11BossSolutionInput SolutionInput => solutionInput;

        public void Configure(
            P11RegionalBoss2D regionalBoss,
            int index,
            P11BossSolutionInput input)
        {
            boss = regionalBoss;
            targetIndex = Mathf.Max(0, index);
            solutionInput = input;
            ConfigureInteraction(transform, 1.7f, 85);
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return boss != null && !boss.IsDefeated;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            if (boss.Phase == P11BossPhase.Idle)
            {
                boss.BeginEncounter();
            }

            return boss.TryEnvironmentTarget(
                targetIndex,
                solutionInput);
        }
    }
}

#endif
