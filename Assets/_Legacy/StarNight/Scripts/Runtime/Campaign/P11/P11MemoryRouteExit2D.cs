#if LEGACY_DISABLED
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11MemoryRouteExit2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P11MaruFinalBoss2D boss;

        public void Configure(P11MaruFinalBoss2D finalBoss)
        {
            boss = finalBoss;
            ConfigureInteraction(transform, 1.8f, 95);
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return boss != null;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return boss.TryCompleteMemoryRoute();
        }
    }
}

#endif
