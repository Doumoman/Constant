#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11MaruCommandPillar2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P11MaruFinalBoss2D boss;
        [SerializeField, Min(0)] private int pillarIndex;

        public int PillarIndex => pillarIndex;

        public void Configure(
            P11MaruFinalBoss2D finalBoss,
            int index)
        {
            boss = finalBoss;
            pillarIndex = Mathf.Max(0, index);
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

            return boss.TryBreakCommandPillar(pillarIndex);
        }
    }
}

#endif
