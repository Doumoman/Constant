#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class P11InvariantStarBlock2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Transform invariantAnchor;
        [SerializeField] private Vector2 origin;
        [SerializeField] private float originRotation;
        [SerializeField] private bool configured;
        [SerializeField] private int correctionCount;

        public Rigidbody2D Body => body;
        public Vector2 Origin => origin;
        public float OriginRotation => originRotation;
        public bool IsConfigured => configured && body != null;
        public int CorrectionCount => correctionCount;
        public bool ReturnFieldInvariant => true;
        public bool IsAtOrigin => body != null
            && Vector2.Distance(body.position, origin) <= 0.001f
            && Mathf.Abs(Mathf.DeltaAngle(
                body.rotation,
                originRotation)) <= 0.05f;

        public void Configure(
            Rigidbody2D invariantBody,
            Transform anchor = null)
        {
            body = invariantBody != null
                ? invariantBody
                : GetComponent<Rigidbody2D>();
            invariantAnchor = anchor;
            correctionCount = 0;
            CaptureOrigin();
        }

        public void CaptureOrigin()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (body == null)
            {
                configured = false;
                return;
            }

            origin = invariantAnchor != null
                ? invariantAnchor.position
                : body.position;
            originRotation = invariantAnchor != null
                ? invariantAnchor.eulerAngles.z
                : body.rotation;
            configured = true;
        }

        public bool RestoreNow()
        {
            if (!IsConfigured)
            {
                return false;
            }

            bool corrected = !IsAtOrigin
                || body.linearVelocity.sqrMagnitude > 0.0001f
                || Mathf.Abs(body.angularVelocity) > 0.001f;
            body.position = origin;
            body.rotation = originRotation;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            if (corrected)
            {
                correctionCount++;
            }

            return corrected;
        }

        private void Awake()
        {
            if (!configured)
            {
                Configure(GetComponent<Rigidbody2D>());
            }
        }

        private void FixedUpdate()
        {
            RestoreNow();
        }
    }
}

#endif
