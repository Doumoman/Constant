#if LEGACY_DISABLED
using System;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DisallowMultipleComponent]
    public sealed class P9StarArchive2D : P5ContextInteractable2D
    {
        [SerializeField] private P9ArchiveUnlockMethods unlockMethods =
            P9ArchiveUnlockMethods.SealLever
            | P9ArchiveUnlockMethods.CrackedOuterWall
            | P9ArchiveUnlockMethods.HookLatch;
        [SerializeField] private Transform mainRouteCue;
        [SerializeField] private GameObject sealedVisual;
        [SerializeField] private GameObject openVisual;
        [SerializeField] private bool opened;

        public event Action<P9ArchiveUnlockMethods> Opened;

        public P9ArchiveUnlockMethods UnlockMethods => unlockMethods;
        public Transform MainRouteCue => mainRouteCue;
        public bool IsOpen => opened;
        public bool MainRouteCueVisible =>
            mainRouteCue != null && mainRouteCue.gameObject.activeSelf;
        public bool OpeningDoesNotGateExit => true;
        public bool BombIsNotTheOnlySolution =>
            CountUnlockMethods(unlockMethods) >= 2;
        public int UnlockMethodCount =>
            CountUnlockMethods(unlockMethods);

        public void Configure(
            P9ArchiveUnlockMethods methods,
            Transform visibleCue,
            GameObject closedState,
            GameObject openedState)
        {
            unlockMethods = methods;
            mainRouteCue = visibleCue;
            sealedVisual = closedState;
            openVisual = openedState;
            opened = false;
            ConfigureInteraction(transform, 1.8f, 60);
            RefreshVisuals();
        }

        public bool CanOpenWith(P9ArchiveUnlockMethods method)
        {
            int raw = (int)method;
            return method != P9ArchiveUnlockMethods.None
                && (raw & (raw - 1)) == 0
                && (unlockMethods & method) != 0;
        }

        public bool TryOpen(P9ArchiveUnlockMethods method)
        {
            if (opened || !CanOpenWith(method))
            {
                return false;
            }

            opened = true;
            RefreshVisuals();
            Opened?.Invoke(method);
            return true;
        }

        public bool IgnoreAndContinue()
        {
            return OpeningDoesNotGateExit;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !opened && CanOpenWith(P9ArchiveUnlockMethods.SealLever);
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryOpen(P9ArchiveUnlockMethods.SealLever);
        }

        private void RefreshVisuals()
        {
            if (sealedVisual != null)
            {
                sealedVisual.SetActive(!opened);
            }

            if (openVisual != null)
            {
                openVisual.SetActive(opened);
            }

            if (mainRouteCue != null)
            {
                mainRouteCue.gameObject.SetActive(true);
            }
        }

        private static int CountUnlockMethods(
            P9ArchiveUnlockMethods methods)
        {
            int count = 0;
            int raw = (int)methods;
            while (raw != 0)
            {
                count += raw & 1;
                raw >>= 1;
            }

            return count;
        }
    }
}

#endif
