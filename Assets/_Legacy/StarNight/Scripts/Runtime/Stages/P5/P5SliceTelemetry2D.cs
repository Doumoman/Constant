#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5SliceTelemetry2D : MonoBehaviour
    {
        [SerializeField] private P5MoonRabbitPestleEvent2D rabbitEvent;
        [SerializeField] private P5MomoShop2D shop;
        [SerializeField] private P5StageExit2D stageExit;
        [SerializeField] private P5MaruBellClock2D bellClock;
        [SerializeField] private P5GoldPickup2D[] goldPickups =
            Array.Empty<P5GoldPickup2D>();

        private readonly List<P5BellSignal> observedBellPattern =
            new List<P5BellSignal>(3);
        private bool subscribed;
        private float stageStartRealtime;

        public int StageStartCount { get; private set; }
        public int GoldPickupCount { get; private set; }
        public int GoldCollected { get; private set; }
        public int PurchaseCount { get; private set; }
        public int RabbitCompletionCount { get; private set; }
        public int ExitDepartureCount { get; private set; }
        public int BacktrackAfterExitCount { get; private set; }
        public float ExitGuidanceShownAtSeconds { get; private set; } = -1f;
        public float ExitFirstReachedAtSeconds { get; private set; } = -1f;
        public bool RabbitPestleWasDiscovered { get; private set; }
        public bool HasExitIntentHumanResponse { get; private set; }
        public bool ExitIntentWasUnderstood { get; private set; }
        public float ExitIntentResponseSeconds { get; private set; } = -1f;
        public bool HasRabbitIntentHumanResponse { get; private set; }
        public bool RabbitIntentWasUnderstood { get; private set; }
        public IReadOnlyList<P5BellSignal> ObservedBellPattern =>
            observedBellPattern;

        public bool ExitGuidanceOneSecondProxy =>
            ExitGuidanceShownAtSeconds >= 0f
            && ExitGuidanceShownAtSeconds <= 1f;

        public bool ExitIntentHumanGateMet =>
            HasExitIntentHumanResponse
            && ExitIntentWasUnderstood
            && ExitIntentResponseSeconds >= 0f
            && ExitIntentResponseSeconds <= 10f;

        public bool ObservedShortShortLongPattern =>
            observedBellPattern.Count == 3
            && observedBellPattern[0] == P5BellSignal.Short
            && observedBellPattern[1] == P5BellSignal.Short
            && observedBellPattern[2] == P5BellSignal.Long;

        public void Configure(
            P5MoonRabbitPestleEvent2D targetRabbitEvent,
            P5MomoShop2D targetShop,
            P5StageExit2D targetExit,
            P5MaruBellClock2D targetBellClock,
            P5GoldPickup2D[] stageGoldPickups)
        {
            Unsubscribe();
            rabbitEvent = targetRabbitEvent;
            shop = targetShop;
            stageExit = targetExit;
            bellClock = targetBellClock;
            goldPickups = stageGoldPickups
                ?? Array.Empty<P5GoldPickup2D>();
            ResetTelemetryForTests();
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void MarkStageStarted()
        {
            if (StageStartCount > 0)
            {
                return;
            }

            StageStartCount = 1;
            stageStartRealtime = Time.realtimeSinceStartup;
        }

        public void MarkExitGuidanceShown()
        {
            if (ExitGuidanceShownAtSeconds >= 0f)
            {
                return;
            }

            ExitGuidanceShownAtSeconds = SecondsSinceStageStart();
        }

        public void MarkBacktrackAfterExit()
        {
            if (ExitFirstReachedAtSeconds < 0f
                || BacktrackAfterExitCount > 0
                || ExitDepartureCount > 0)
            {
                return;
            }

            BacktrackAfterExitCount = 1;
        }

        public void RecordExitIntentHumanResponse(
            bool understood,
            float responseSeconds)
        {
            HasExitIntentHumanResponse = true;
            ExitIntentWasUnderstood = understood;
            ExitIntentResponseSeconds = Mathf.Max(0f, responseSeconds);
        }

        public void RecordRabbitIntentHumanResponse(bool understood)
        {
            HasRabbitIntentHumanResponse = true;
            RabbitIntentWasUnderstood = understood;
        }

        public void ResetTelemetryForTests()
        {
            StageStartCount = 0;
            GoldPickupCount = 0;
            GoldCollected = 0;
            PurchaseCount = 0;
            RabbitCompletionCount = 0;
            ExitDepartureCount = 0;
            BacktrackAfterExitCount = 0;
            ExitGuidanceShownAtSeconds = -1f;
            ExitFirstReachedAtSeconds = -1f;
            RabbitPestleWasDiscovered = false;
            HasExitIntentHumanResponse = false;
            ExitIntentWasUnderstood = false;
            ExitIntentResponseSeconds = -1f;
            HasRabbitIntentHumanResponse = false;
            RabbitIntentWasUnderstood = false;
            observedBellPattern.Clear();
            stageStartRealtime = Time.realtimeSinceStartup;
        }

        private void HandleGoldCollected(P5GoldPickup2D pickup, int value)
        {
            GoldPickupCount++;
            GoldCollected += value;
        }

        private void HandlePurchaseCompleted(
            P5ShopProductKind product,
            int price)
        {
            PurchaseCount++;
        }

        private void HandleRabbitStateChanged(
            P5MoonRabbitPestleState next)
        {
            if (next == P5MoonRabbitPestleState.PestleDiscovered)
            {
                RabbitPestleWasDiscovered = true;
            }
        }

        private void HandleRabbitCompleted(int reward)
        {
            RabbitCompletionCount++;
        }

        private void HandleExitFirstReached()
        {
            if (ExitFirstReachedAtSeconds < 0f)
            {
                ExitFirstReachedAtSeconds = SecondsSinceStageStart();
            }
        }

        private void HandleExitDeparted()
        {
            ExitDepartureCount++;
        }

        private void HandleBellRang(
            P5BellSignal signal,
            P5MaruBellPhase phase)
        {
            if (observedBellPattern.Count < 3)
            {
                observedBellPattern.Add(signal);
            }
        }

        private float SecondsSinceStageStart()
        {
            return StageStartCount > 0
                ? Mathf.Max(
                    0f,
                    Time.realtimeSinceStartup - stageStartRealtime)
                : 0f;
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (rabbitEvent != null)
            {
                rabbitEvent.StateChanged += HandleRabbitStateChanged;
                rabbitEvent.Completed += HandleRabbitCompleted;
            }

            if (shop != null)
            {
                shop.PurchaseCompleted += HandlePurchaseCompleted;
            }

            if (stageExit != null)
            {
                stageExit.FirstReached += HandleExitFirstReached;
                stageExit.Departed += HandleExitDeparted;
            }

            if (bellClock != null)
            {
                bellClock.BellRang += HandleBellRang;
            }

            for (int index = 0; index < goldPickups.Length; index++)
            {
                if (goldPickups[index] != null)
                {
                    goldPickups[index].Collected += HandleGoldCollected;
                }
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
                rabbitEvent.StateChanged -= HandleRabbitStateChanged;
                rabbitEvent.Completed -= HandleRabbitCompleted;
            }

            if (shop != null)
            {
                shop.PurchaseCompleted -= HandlePurchaseCompleted;
            }

            if (stageExit != null)
            {
                stageExit.FirstReached -= HandleExitFirstReached;
                stageExit.Departed -= HandleExitDeparted;
            }

            if (bellClock != null)
            {
                bellClock.BellRang -= HandleBellRang;
            }

            for (int index = 0; index < goldPickups.Length; index++)
            {
                if (goldPickups[index] != null)
                {
                    goldPickups[index].Collected -= HandleGoldCollected;
                }
            }

            subscribed = false;
        }
    }
}

#endif
