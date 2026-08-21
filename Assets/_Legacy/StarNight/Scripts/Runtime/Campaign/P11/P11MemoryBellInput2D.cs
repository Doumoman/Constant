#if LEGACY_DISABLED
using StarNight.Maru.P8;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11MemoryBellInput2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P11MaruFinalBoss2D boss;
        [SerializeField] private P8BellSignal signal;
        [SerializeField] private bool lightsBellInsteadOfRinging;

        public P8BellSignal Signal => signal;
        public bool LightsBellInsteadOfRinging =>
            lightsBellInsteadOfRinging;

        public void Configure(
            P11MaruFinalBoss2D finalBoss,
            P8BellSignal bellSignal,
            bool lightBell)
        {
            boss = finalBoss;
            signal = bellSignal;
            lightsBellInsteadOfRinging = lightBell;
            ConfigureInteraction(transform, 1.6f, 88);
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return boss != null && !boss.IsDefeated;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return lightsBellInsteadOfRinging
                ? boss.TryLightFirstMemoryBell()
                : boss.TryRingMemoryBell(signal);
        }
    }
}

#endif
