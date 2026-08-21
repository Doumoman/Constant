#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DisallowMultipleComponent]
    public sealed class P9InferenceCue2D : MonoBehaviour
    {
        [SerializeField] private P9InferenceCueKind cueKind;
        [SerializeField] private Transform focusTarget;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float bobFrequency = 2f;

        private Vector3 baseLocalPosition;

        public P9InferenceCueKind CueKind => cueKind;
        public Transform FocusTarget => focusTarget;
        public bool UsesText => false;

        public void Configure(
            P9InferenceCueKind kind,
            Transform target = null,
            float amplitude = 0.08f,
            float frequency = 2f)
        {
            cueKind = kind;
            focusTarget = target;
            bobAmplitude = Mathf.Max(0f, amplitude);
            bobFrequency = Mathf.Max(0f, frequency);
            baseLocalPosition = transform.localPosition;
        }

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * bobFrequency)
                * bobAmplitude;
            transform.localPosition =
                baseLocalPosition + Vector3.up * bob;

            if (focusTarget == null)
            {
                return;
            }

            Vector2 direction =
                focusTarget.position - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.right = direction.normalized;
            }
        }
    }
}

#endif
