#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    public enum P12EpilogueBeat
    {
        None = 0,
        NaraeFirstShort = 1,
        NaraeSecondShort = 2,
        NaraeFinalLong = 3,
        RaniLanternResponse = 4,
        Finished = 5
    }

    [DisallowMultipleComponent]
    public sealed class P12EpiloguePresentation2D : MonoBehaviour
    {
        public const float ShortBeatSeconds = 0.5f;
        public const float LongBeatSeconds = 1.5f;
        public const float BeatGapSeconds = 0.5f;
        public const float ResponseSeconds = 2f;

        [SerializeField] private P12ChallengeDirector2D director;
        [SerializeField] private SpriteRenderer signalRenderer;
        [SerializeField] private SpriteRenderer responseRenderer;
        [SerializeField] private Color signalColor =
            new Color(1f, 0.87f, 0.55f);
        [SerializeField] private Color responseColor =
            new Color(0.55f, 0.8f, 1f);
        [SerializeField] private P12EpilogueBeat beat =
            P12EpilogueBeat.None;
        [SerializeField] private bool presentationShown;

        private bool subscribed;
        private bool playing;
        private float elapsed;

        public event Action EpiloguePresented;
        public event Action<P12EpilogueBeat> BeatChanged;

        public P12ChallengeDirector2D Director => director;
        public P12EpilogueBeat CurrentBeat => beat;
        public bool IsPlaying => playing;
        public bool PresentationShown => presentationShown;
        public bool PermanentRewardIsRecordOnly =>
            director == null || director.PermanentRewardIsRecordOnly;

        public void Configure(
            P12ChallengeDirector2D challengeDirector,
            SpriteRenderer naraeSignalRenderer,
            SpriteRenderer raniResponseRenderer)
        {
            Unsubscribe();
            director = challengeDirector;
            signalRenderer = naraeSignalRenderer;
            responseRenderer = raniResponseRenderer;
            ResetPresentationForTests();
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

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public void BeginPresentation()
        {
            if (playing || presentationShown)
            {
                return;
            }

            playing = true;
            elapsed = 0f;
            SetBeat(P12EpilogueBeat.NaraeFirstShort);
            ApplyAlphas(1f, 0f);
        }

        public void Advance(float deltaSeconds)
        {
            if (!playing || deltaSeconds <= 0f)
            {
                return;
            }

            elapsed += deltaSeconds;
            float firstShortEnd = ShortBeatSeconds;
            float firstGapEnd = firstShortEnd + BeatGapSeconds;
            float secondShortEnd = firstGapEnd + ShortBeatSeconds;
            float secondGapEnd = secondShortEnd + BeatGapSeconds;
            float longEnd = secondGapEnd + LongBeatSeconds;
            float responseEnd = longEnd + ResponseSeconds;
            if (elapsed >= responseEnd)
            {
                playing = false;
                presentationShown = true;
                SetBeat(P12EpilogueBeat.Finished);
                ApplyAlphas(0f, 1f);
                EpiloguePresented?.Invoke();
                return;
            }

            P12EpilogueBeat next;
            bool signalVisible;
            if (elapsed < firstShortEnd)
            {
                next = P12EpilogueBeat.NaraeFirstShort;
                signalVisible = true;
            }
            else if (elapsed < firstGapEnd)
            {
                next = P12EpilogueBeat.NaraeFirstShort;
                signalVisible = false;
            }
            else if (elapsed < secondShortEnd)
            {
                next = P12EpilogueBeat.NaraeSecondShort;
                signalVisible = true;
            }
            else if (elapsed < secondGapEnd)
            {
                next = P12EpilogueBeat.NaraeSecondShort;
                signalVisible = false;
            }
            else if (elapsed < longEnd)
            {
                next = P12EpilogueBeat.NaraeFinalLong;
                signalVisible = true;
            }
            else
            {
                next = P12EpilogueBeat.RaniLanternResponse;
                signalVisible = false;
            }

            SetBeat(next);
            ApplyAlphas(
                signalVisible ? 1f : 0f,
                next == P12EpilogueBeat.RaniLanternResponse
                    ? 1f
                    : 0f);
        }

        public void BeginPresentationForTests()
        {
            BeginPresentation();
        }

        public void ResetPresentationForTests()
        {
            playing = false;
            presentationShown = false;
            elapsed = 0f;
            SetBeat(P12EpilogueBeat.None);
            ApplyAlphas(0f, 0f);
        }

        private void HandleChallengeCompleted()
        {
            BeginPresentation();
        }

        private void SetBeat(P12EpilogueBeat next)
        {
            if (beat == next)
            {
                return;
            }

            beat = next;
            BeatChanged?.Invoke(beat);
        }

        private void ApplyAlphas(
            float signalAlpha,
            float responseAlpha)
        {
            if (signalRenderer != null)
            {
                Color signal = signalColor;
                signal.a = signalAlpha;
                signalRenderer.color = signal;
            }

            if (responseRenderer != null)
            {
                Color response = responseColor;
                response.a = responseAlpha;
                responseRenderer.color = response;
            }
        }

        private void Subscribe()
        {
            if (subscribed || director == null)
            {
                return;
            }

            director.ChallengeCompleted += HandleChallengeCompleted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || director == null)
            {
                return;
            }

            director.ChallengeCompleted -= HandleChallengeCompleted;
            subscribed = false;
        }
    }
}

#endif
