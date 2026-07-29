using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PolarisFinaleWorldPresenter : MonoBehaviour
    {
        [SerializeField] private Transform maru;
        [SerializeField] private SpriteRenderer centerStar;
        [SerializeField] private Vector3 maruStart = new(158f, 4f, 0f);
        [SerializeField] private Vector3 maruFinish = new(150f, 0.4f, 0f);
        private PolarisFinaleState finale;

        public void Configure(Transform maruTransform, SpriteRenderer centerStarRenderer,
            Vector3 start, Vector3 finish)
        {
            maru = maruTransform;
            centerStar = centerStarRenderer;
            maruStart = start;
            maruFinish = finish;
        }

        private void Start()
        {
            finale = StarNightRunState.Ensure().GetComponent<PolarisFinaleState>();
        }

        private void Update()
        {
            if (finale == null)
            {
                return;
            }

            if (maru != null && finale.CountdownActive)
            {
                float progress = 1f - finale.TimeRemaining / Mathf.Max(1f, finale.PursuitDuration);
                maru.position = Vector3.Lerp(maruStart, maruFinish, Mathf.Clamp01(progress));
            }
            if (centerStar != null)
            {
                float pulse = 0.86f + Mathf.Sin(Time.time * 2.5f) * 0.14f;
                centerStar.transform.localScale = Vector3.one * pulse;
                centerStar.color = finale.Phase == PolarisFinalePhase.FinalChoice
                    ? new Color(0.45f, 1f, 0.88f)
                    : finale.TimeRemaining < 30f
                        ? new Color(1f, 0.2f, 0.42f)
                        : new Color(1f, 0.78f, 0.25f);
            }
        }
    }
}
