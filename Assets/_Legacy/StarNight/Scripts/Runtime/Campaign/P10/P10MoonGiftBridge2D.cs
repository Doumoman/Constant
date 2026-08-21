#if LEGACY_DISABLED
using StarNight.Folklore.P9;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10MoonGiftBridge2D : MonoBehaviour
    {
        [SerializeField] private P5MoonRabbitPestleEvent2D rabbitEvent;
        [SerializeField] private P9FolkloreChainState2D folkloreState;
        [SerializeField] private P10CampaignDirector2D director;
        private bool subscribed;

        public bool RabbitHelpCompleted =>
            rabbitEvent != null && rabbitEvent.IsCompleted;
        public bool MoonCakeGranted =>
            folkloreState != null && folkloreState.HasMoonCake;
        public bool MedicineGranted =>
            folkloreState != null
            && folkloreState.HasJadeRabbitMedicine;

        public void Configure(
            P5MoonRabbitPestleEvent2D moonRabbitEvent,
            P9FolkloreChainState2D chainState,
            P10CampaignDirector2D campaignDirector)
        {
            Unsubscribe();
            rabbitEvent = moonRabbitEvent;
            folkloreState = chainState;
            director = campaignDirector;
            Subscribe();
            SyncRabbitReward();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SyncRabbitReward()
        {
            if (RabbitHelpCompleted)
            {
                folkloreState?.GrantItem(
                    P9FolkloreItemKind.MoonCake);
            }
        }

        private void HandleRabbitCompleted(int reward)
        {
            folkloreState?.GrantItem(P9FolkloreItemKind.MoonCake);
        }

        private void HandleStageCompleted(P10StageId stageId)
        {
            if (stageId == P10StageId.MoonPalace12
                && RabbitHelpCompleted)
            {
                folkloreState?.GrantItem(
                    P9FolkloreItemKind.JadeRabbitMedicine);
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (rabbitEvent != null)
            {
                rabbitEvent.Completed += HandleRabbitCompleted;
            }

            if (director != null)
            {
                director.StageCompleted += HandleStageCompleted;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (rabbitEvent != null)
            {
                rabbitEvent.Completed -= HandleRabbitCompleted;
            }

            if (director != null)
            {
                director.StageCompleted -= HandleStageCompleted;
            }

            subscribed = false;
        }
    }
}

#endif
