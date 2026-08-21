#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class P5MaruBellPresenter2D : MonoBehaviour
    {
        [SerializeField] private P5MaruBellClock2D clock;
        [SerializeField] private GameObject firstShortBellVisual;
        [SerializeField] private GameObject secondShortBellVisual;
        [SerializeField] private GameObject longBellVisual;
        [SerializeField] private SpriteRenderer backgroundTint;
        [SerializeField] private SpriteRenderer[] backgroundTints =
            System.Array.Empty<SpriteRenderer>();
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shortBellClip;
        [SerializeField] private AudioClip longBellClip;
        [SerializeField] private Color calmColor = Color.white;
        [SerializeField] private Color firstBellColor =
            new Color(0.85f, 0.90f, 1f, 1f);
        [SerializeField] private Color secondBellColor =
            new Color(0.70f, 0.76f, 0.92f, 1f);
        [SerializeField] private Color maruDueColor =
            new Color(0.48f, 0.46f, 0.68f, 1f);

        private bool subscribed;
        private AudioClip generatedShortClip;
        private AudioClip generatedLongClip;

        public P5MaruBellClock2D Clock => clock;
        public GameObject FirstShortBellVisual => firstShortBellVisual;
        public GameObject SecondShortBellVisual => secondShortBellVisual;
        public GameObject LongBellVisual => longBellVisual;
        public SpriteRenderer[] BackgroundTints => backgroundTints;

        public void Configure(
            P5MaruBellClock2D targetClock,
            GameObject firstShortVisual,
            GameObject secondShortVisual,
            GameObject longVisual,
            SpriteRenderer targetBackgroundTint = null,
            AudioSource targetAudioSource = null,
            AudioClip shortSignal = null,
            AudioClip longSignal = null)
        {
            Unsubscribe();
            clock = targetClock;
            firstShortBellVisual = firstShortVisual;
            secondShortBellVisual = secondShortVisual;
            longBellVisual = longVisual;
            backgroundTint = targetBackgroundTint;
            backgroundTints = targetBackgroundTint != null
                ? new[] { targetBackgroundTint }
                : System.Array.Empty<SpriteRenderer>();
            audioSource = targetAudioSource != null
                ? targetAudioSource
                : GetComponent<AudioSource>();
            shortBellClip = shortSignal;
            longBellClip = longSignal;
            ConfigureAudioSource();
            Subscribe();
            ApplyPhase(
                clock != null ? clock.Phase : P5MaruBellPhase.Calm);
        }

        private void OnEnable()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            ConfigureAudioSource();
            Subscribe();
            ApplyPhase(
                clock != null ? clock.Phase : P5MaruBellPhase.Calm);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            DestroyGeneratedClip(generatedShortClip);
            DestroyGeneratedClip(generatedLongClip);
        }

        private void HandlePhaseChanged(P5MaruBellPhase next)
        {
            ApplyPhase(next);
        }

        public void SetBackgroundTints(SpriteRenderer[] targetBackgroundTints)
        {
            backgroundTints = targetBackgroundTints
                ?? System.Array.Empty<SpriteRenderer>();
            if (backgroundTints.Length > 0)
            {
                backgroundTint = backgroundTints[0];
            }

            ApplyPhase(
                clock != null ? clock.Phase : P5MaruBellPhase.Calm);
        }

        public void SetPhaseColors(
            Color calm,
            Color first,
            Color second,
            Color maruDue)
        {
            calmColor = calm;
            firstBellColor = first;
            secondBellColor = second;
            maruDueColor = maruDue;
            ApplyPhase(
                clock != null ? clock.Phase : P5MaruBellPhase.Calm);
        }

        private void ApplyPhase(P5MaruBellPhase current)
        {
            P5MaruBellPhase displayed = current == P5MaruBellPhase.Stopped
                && clock != null
                ? clock.LastActivePhase
                : current;
            bool first = displayed == P5MaruBellPhase.FirstBell
                || displayed == P5MaruBellPhase.SecondBell
                || displayed == P5MaruBellPhase.MaruDue;
            bool second = displayed == P5MaruBellPhase.SecondBell
                || displayed == P5MaruBellPhase.MaruDue;
            bool third = displayed == P5MaruBellPhase.MaruDue;

            SetActive(firstShortBellVisual, first);
            SetActive(secondShortBellVisual, second);
            SetActive(longBellVisual, third);

            Color targetColor;
            switch (displayed)
            {
                case P5MaruBellPhase.FirstBell:
                    targetColor = firstBellColor;
                    break;
                case P5MaruBellPhase.SecondBell:
                    targetColor = secondBellColor;
                    break;
                case P5MaruBellPhase.MaruDue:
                    targetColor = maruDueColor;
                    break;
                default:
                    targetColor = calmColor;
                    break;
            }

            ApplyBackgroundColor(targetColor);
        }

        private void Subscribe()
        {
            if (subscribed || clock == null)
            {
                return;
            }

            clock.PhaseChanged += HandlePhaseChanged;
            clock.BellRang += HandleBellRang;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || clock == null)
            {
                return;
            }

            clock.PhaseChanged -= HandlePhaseChanged;
            clock.BellRang -= HandleBellRang;
            subscribed = false;
        }

        private void HandleBellRang(
            P5BellSignal signal,
            P5MaruBellPhase phase)
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip;
            if (signal == P5BellSignal.Long)
            {
                if (generatedLongClip == null && longBellClip == null)
                {
                    generatedLongClip =
                        CreateTone("P5_MaruBell_Long", 520f, 0.45f);
                }

                clip = longBellClip != null
                    ? longBellClip
                    : generatedLongClip;
            }
            else
            {
                if (generatedShortClip == null && shortBellClip == null)
                {
                    generatedShortClip =
                        CreateTone("P5_MaruBell_Short", 880f, 0.12f);
                }

                clip = shortBellClip != null
                    ? shortBellClip
                    : generatedShortClip;
            }

            audioSource.PlayOneShot(clip);
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private static AudioClip CreateTone(
            string clipName,
            float frequency,
            float duration)
        {
            const int sampleRate = 22050;
            int sampleCount =
                Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float time = (float)index / sampleRate;
                float normalized = (float)index / sampleCount;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                samples[index] =
                    Mathf.Sin(2f * Mathf.PI * frequency * time)
                    * envelope
                    * 0.18f;
            }

            AudioClip clip = AudioClip.Create(
                clipName,
                sampleCount,
                1,
                sampleRate,
                false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void DestroyGeneratedClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(clip);
            }
            else
            {
                DestroyImmediate(clip);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private void ApplyBackgroundColor(Color targetColor)
        {
            if (backgroundTints != null && backgroundTints.Length > 0)
            {
                for (int index = 0; index < backgroundTints.Length; index++)
                {
                    if (backgroundTints[index] != null)
                    {
                        backgroundTints[index].color = targetColor;
                    }
                }

                return;
            }

            if (backgroundTint != null)
            {
                backgroundTint.color = targetColor;
            }
        }
    }
}

#endif
