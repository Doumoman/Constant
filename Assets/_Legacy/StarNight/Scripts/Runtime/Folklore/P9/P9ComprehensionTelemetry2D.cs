#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DisallowMultipleComponent]
    public sealed class P9ComprehensionTelemetry2D : MonoBehaviour
    {
        public const float GiftInferenceTarget = 0.80f;
        public const float GuestHelpTarget = 0.85f;

        [SerializeField, Min(0)] private int giftInferenceTrials;
        [SerializeField, Min(0)] private int giftInferenceSuccesses;
        [SerializeField, Min(0)] private int guestHelpTrials;
        [SerializeField, Min(0)] private int guestHelpSuccesses;

        public int GiftInferenceTrials => giftInferenceTrials;
        public int GuestHelpTrials => guestHelpTrials;
        public float GiftInferenceRate =>
            Rate(giftInferenceSuccesses, giftInferenceTrials);
        public float GuestHelpUnderstandingRate =>
            Rate(guestHelpSuccesses, guestHelpTrials);
        public bool GiftGatePassed =>
            giftInferenceTrials > 0
            && GiftInferenceRate >= GiftInferenceTarget;
        public bool GuestHelpGatePassed =>
            guestHelpTrials > 0
            && GuestHelpUnderstandingRate >= GuestHelpTarget;
        public bool InstrumentationReady => true;

        public void RecordGiftInference(bool inferredCorrectly)
        {
            giftInferenceTrials++;
            if (inferredCorrectly)
            {
                giftInferenceSuccesses++;
            }
        }

        public void RecordGuestHelpUnderstanding(bool understood)
        {
            guestHelpTrials++;
            if (understood)
            {
                guestHelpSuccesses++;
            }
        }

        public void ResetTelemetry()
        {
            giftInferenceTrials = 0;
            giftInferenceSuccesses = 0;
            guestHelpTrials = 0;
            guestHelpSuccesses = 0;
        }

        private static float Rate(int successes, int trials)
        {
            return trials <= 0 ? 0f : successes / (float)trials;
        }
    }
}

#endif
