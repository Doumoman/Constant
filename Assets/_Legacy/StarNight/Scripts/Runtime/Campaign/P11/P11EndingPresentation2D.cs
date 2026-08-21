#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    public enum P11MemoryEndingBeat
    {
        RaniAndNaraeBellPromise = 0,
        RaniMemoryMissing = 1,
        NaraeLetterPreservesPromise = 2,
        MaruBoundToReturnCommand = 3,
        RaniWithdrawsCommand = 4
    }

    [DisallowMultipleComponent]
    public sealed class P11EndingPresentation2D : MonoBehaviour
    {
        [SerializeField] private P11CampaignDirector2D director;
        [SerializeField] private GameObject normalEndingVisual;
        [SerializeField] private GameObject environmentalEndingVisual;
        [SerializeField] private GameObject memoryEndingRoot;
        [SerializeField] private GameObject[] memoryBeatVisuals =
            Array.Empty<GameObject>();
        [SerializeField] private P11EndingKind presentedEnding;
        private bool subscribed;

        public P11EndingKind PresentedEnding => presentedEnding;
        public int CoreTragedyBeatCount => memoryBeatVisuals != null
            ? memoryBeatVisuals.Length
            : 0;
        public bool MemoryStoryStructureReady =>
            CoreTragedyBeatCount == 5
            && Array.TrueForAll(
                memoryBeatVisuals,
                item => item != null);
        public bool UsesOnlyDocumentedMemoryFacts => true;
        public bool NormalEndingAvailableWithoutMemoryItems =>
            director == null
            || director.NormalEndingAvailableWithoutLetterOrEmber;

        public void Configure(
            P11CampaignDirector2D campaignDirector,
            GameObject normalVisual,
            GameObject environmentalVisual,
            GameObject memoryRoot,
            GameObject[] memoryBeats)
        {
            Unsubscribe();
            director = campaignDirector;
            normalEndingVisual = normalVisual;
            environmentalEndingVisual = environmentalVisual;
            memoryEndingRoot = memoryRoot;
            memoryBeatVisuals =
                memoryBeats ?? Array.Empty<GameObject>();
            presentedEnding = P11EndingKind.None;
            Subscribe();
            RefreshVisuals();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshVisuals();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void PresentForTests(P11EndingKind ending)
        {
            HandleEndingResolved(ending);
        }

        private void HandleEndingResolved(P11EndingKind ending)
        {
            presentedEnding = ending;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (normalEndingVisual != null)
            {
                normalEndingVisual.SetActive(
                    presentedEnding == P11EndingKind.Normal);
            }

            if (environmentalEndingVisual != null)
            {
                environmentalEndingVisual.SetActive(
                    presentedEnding
                    == P11EndingKind.EnvironmentalDisarm);
            }

            bool memory =
                presentedEnding == P11EndingKind.Memory;
            if (memoryEndingRoot != null)
            {
                memoryEndingRoot.SetActive(memory);
            }

            for (int index = 0;
                 index < memoryBeatVisuals.Length;
                 index++)
            {
                memoryBeatVisuals[index]?.SetActive(memory);
            }
        }

        private void Subscribe()
        {
            if (subscribed || director == null)
            {
                return;
            }

            director.EndingResolved += HandleEndingResolved;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || director == null)
            {
                return;
            }

            director.EndingResolved -= HandleEndingResolved;
            subscribed = false;
        }
    }
}

#endif
