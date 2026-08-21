#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class P8MaruBellPresenter2D : MonoBehaviour
    {
        [SerializeField] private P8MaruTimeline2D timeline;
        [SerializeField] private GameObject firstBellVisual;
        [SerializeField] private GameObject secondBellVisual;
        [SerializeField] private GameObject huntVisual;
        [SerializeField] private SpriteRenderer[] backgroundTints =
            Array.Empty<SpriteRenderer>();
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shortBellClip;
        [SerializeField] private AudioClip longBellClip;
        [SerializeField] private Color calmColor = Color.white;
        [SerializeField] private Color firstColor =
            new Color(0.80f, 0.88f, 1f, 1f);
        [SerializeField] private Color secondColor =
            new Color(0.60f, 0.67f, 0.88f, 1f);
        [SerializeField] private Color huntColor =
            new Color(0.38f, 0.34f, 0.56f, 1f);

        private bool subscribed;
        private AudioClip generatedShortClip;
        private AudioClip generatedLongClip;

        public P8MaruTimeline2D Timeline => timeline;
        public GameObject FirstBellVisual => firstBellVisual;
        public GameObject SecondBellVisual => secondBellVisual;
        public GameObject HuntVisual => huntVisual;

        public void Configure(
            P8MaruTimeline2D maruTimeline,
            GameObject firstVisual,
            GameObject secondVisual,
            GameObject maruHuntVisual,
            SpriteRenderer[] tints = null,
            AudioSource targetAudioSource = null,
            AudioClip shortSignal = null,
            AudioClip longSignal = null)
        {
            Unsubscribe();
            timeline = maruTimeline;
            firstBellVisual = firstVisual;
            secondBellVisual = secondVisual;
            huntVisual = maruHuntVisual;
            backgroundTints = tints ?? Array.Empty<SpriteRenderer>();
            audioSource = targetAudioSource != null
                ? targetAudioSource
                : GetComponent<AudioSource>();
            shortBellClip = shortSignal;
            longBellClip = longSignal;
            ConfigureAudioSource();
            Subscribe();
            ApplyPhase(
                timeline != null
                    ? timeline.Phase
                    : P8MaruPhase.Calm);
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
                timeline != null
                    ? timeline.Phase
                    : P8MaruPhase.Calm);
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

        private void HandlePhaseChanged(P8MaruPhase next)
        {
            ApplyPhase(next);
        }

        private void ApplyPhase(P8MaruPhase phase)
        {
            P8MaruPhase displayed =
                phase == P8MaruPhase.Stopped && timeline != null
                    ? timeline.LastActivePhase
                    : phase;
            int displayedOrder = (int)displayed;
            bool first = displayedOrder >= (int)P8MaruPhase.FirstBell
                && displayedOrder <= (int)P8MaruPhase.Hunting;
            bool second = displayedOrder >= (int)P8MaruPhase.SecondBell
                && displayedOrder <= (int)P8MaruPhase.Hunting;
            SetActive(firstBellVisual, first);
            SetActive(secondBellVisual, second);
            SetActive(huntVisual, displayed == P8MaruPhase.Hunting);

            Color color;
            switch (displayed)
            {
                case P8MaruPhase.FirstBell:
                    color = firstColor;
                    break;
                case P8MaruPhase.SecondBell:
                    color = secondColor;
                    break;
                case P8MaruPhase.Hunting:
                    color = huntColor;
                    break;
                default:
                    color = calmColor;
                    break;
            }

            for (int index = 0; index < backgroundTints.Length; index++)
            {
                if (backgroundTints[index] != null)
                {
                    backgroundTints[index].color = color;
                }
            }
        }

        private void Subscribe()
        {
            if (subscribed || timeline == null)
            {
                return;
            }

            timeline.PhaseChanged += HandlePhaseChanged;
            timeline.BellRang += HandleBellRang;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || timeline == null)
            {
                return;
            }

            timeline.PhaseChanged -= HandlePhaseChanged;
            timeline.BellRang -= HandleBellRang;
            subscribed = false;
        }

        private void HandleBellRang(P8BellEvent signal)
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip;
            if (signal.Signal == P8BellSignal.Long)
            {
                if (generatedLongClip == null && longBellClip == null)
                {
                    generatedLongClip =
                        CreateTone("P8_MaruBell_Long", 520f, 0.45f);
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
                        CreateTone("P8_MaruBell_Short", 880f, 0.12f);
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
    }
}

#endif
