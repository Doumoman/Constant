#if LEGACY_DISABLED
using System;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5MoonRabbitPestleEvent2D :
        P5ContextInteractable2D
    {
        public const int DefaultMoonCakeReward = 1;

        [SerializeField] private P5StoryPestle2D storyPestle;
        [SerializeField] private P5RunState2D runState;
        [SerializeField] private Transform returnedPestleAnchor;
        [SerializeField] private GameObject rewardVisual;
        [SerializeField, Min(1)] private int moonCakeReward =
            DefaultMoonCakeReward;
        [SerializeField] private P5MoonRabbitPestleState state;

        private bool subscribed;

        public event Action<P5MoonRabbitPestleState> StateChanged;
        public event Action<int> Completed;

        public P5MoonRabbitPestleState State => state;
        public bool IsCompleted =>
            state == P5MoonRabbitPestleState.Completed;
        public P5StoryPestle2D StoryPestle => storyPestle;

        public void Configure(
            P5StoryPestle2D targetStoryPestle,
            P5RunState2D targetRunState,
            Transform targetReturnedPestleAnchor,
            GameObject targetRewardVisual = null,
            Transform interactionPoint = null,
            int rewardAmount = DefaultMoonCakeReward,
            float interactionRadius = 1.75f)
        {
            if (rewardAmount != DefaultMoonCakeReward)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rewardAmount),
                    rewardAmount,
                    "The P5 moon-rabbit event grants exactly one moon cake.");
            }

            Unsubscribe();
            storyPestle = targetStoryPestle;
            runState = targetRunState;
            returnedPestleAnchor = targetReturnedPestleAnchor;
            rewardVisual = targetRewardVisual;
            moonCakeReward = DefaultMoonCakeReward;
            state = storyPestle != null && storyPestle.WasDiscovered
                ? P5MoonRabbitPestleState.PestleDiscovered
                : P5MoonRabbitPestleState.WaitingForPestle;
            if (rewardVisual != null)
            {
                rewardVisual.SetActive(false);
            }

            ConfigureInteraction(
                interactionPoint,
                interactionRadius,
                100);
            Subscribe();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Subscribe();
        }

        protected override void OnDisable()
        {
            Unsubscribe();
            base.OnDisable();
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !IsCompleted
                && storyPestle != null
                && runState != null
                && returnedPestleAnchor != null
                && storyPestle.IsHeldBy(context.ToolInventory)
                && runState.MoonCakes
                    <= P5RunState2D.MaximumSmallNumber - moonCakeReward;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            if (!CanInteract(context)
                || !storyPestle.TryReturn(
                    context.ToolInventory,
                    returnedPestleAnchor))
            {
                return false;
            }

            int granted = runState.AddMoonCakes(moonCakeReward);
            if (granted != moonCakeReward)
            {
                return false;
            }

            SetState(P5MoonRabbitPestleState.Completed);
            if (rewardVisual != null)
            {
                rewardVisual.SetActive(true);
            }

            Completed?.Invoke(granted);
            return true;
        }

        public bool TryResolveForTests(PlayerToolInventory2D inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            P5PlayerInteractionContext context =
                new P5PlayerInteractionContext(
                    inventory.transform,
                    inventory.GetComponent<StarNight.Objects.CarrySystem>(),
                    inventory,
                    inventory.GetComponent<PlayerConsumableTools2D>(),
                    runState);
            return TryInteract(context);
        }

        private void HandleStoryPestleDiscovered(
            P5StoryPestle2D discoveredPestle)
        {
            if (discoveredPestle == storyPestle && !IsCompleted)
            {
                SetState(P5MoonRabbitPestleState.PestleDiscovered);
            }
        }

        private void SetState(P5MoonRabbitPestleState next)
        {
            if (state == next)
            {
                return;
            }

            state = next;
            StateChanged?.Invoke(state);
        }

        private void Subscribe()
        {
            if (subscribed || storyPestle == null)
            {
                return;
            }

            storyPestle.Discovered += HandleStoryPestleDiscovered;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || storyPestle == null)
            {
                return;
            }

            storyPestle.Discovered -= HandleStoryPestleDiscovered;
            subscribed = false;
        }
    }
}

#endif
