using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightAtmosphere : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private Vector3 cameraOffset = new(3f, 1f, -10f);
        [SerializeField] private Color quietColor = new(0.025f, 0.035f, 0.095f);
        [SerializeField] private Color alarmColor = new(0.17f, 0.025f, 0.09f);
        [SerializeField] private int starCount = 70;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private Vector2 worldXBounds = new(-12f, 190f);
        [SerializeField] private Vector2 worldYBounds = new(-8f, 18f);

        private readonly List<Transform> stars = new();
        private StarNightChapterState chapter;

        public void Configure(Camera camera, Transform target, Sprite sprite)
        {
            targetCamera = camera;
            followTarget = target;
            starSprite = sprite;
        }

        public void SetWorldBounds(Vector2 xBounds, Vector2 yBounds, int count = 140)
        {
            worldXBounds = xBounds;
            worldYBounds = yBounds;
            starCount = Mathf.Max(20, count);
        }

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (followTarget == null && FindFirstObjectByType<StarNightPlayerAgent>() is { } player) followTarget = player.transform;
            chapter = StarNightRunState.Ensure().Chapter;
            BuildStars();
        }

        private void LateUpdate()
        {
            if (targetCamera != null && followTarget != null)
            {
                Vector3 bellShake = chapter != null && chapter.BellPhase == StarBellPhase.Third
                    ? new Vector3(Mathf.Sin(Time.time * 17f), Mathf.Cos(Time.time * 13f), 0f) * 0.07f
                    : Vector3.zero;
                Vector3 desired = followTarget.position + cameraOffset + bellShake;
                targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
                float t = chapter != null ? chapter.Scent / StarScentRules.MaxScent : 0f;
                if (chapter != null && chapter.GateLoopEnabled && chapter.GateActivated)
                {
                    float bellPressure = ((float)chapter.BellPhase - 0.5f) /
                                         (float)StarBellPhase.Third;
                    float alertPressure = chapter.PostGateAlert /
                                          StarGateAlertRules.ThirdBellThreshold;
                    t = Mathf.Max(t * 0.35f, Mathf.Clamp01(bellPressure + alertPressure * 0.3f));
                }
                targetCamera.backgroundColor = Color.Lerp(quietColor, alarmColor, t);
            }

            for (int i = 0; i < stars.Count; i++)
            {
                Transform star = stars[i];
                float pulse = 0.75f + Mathf.Sin(Time.time * (0.6f + (i % 5) * 0.13f) + i) * 0.25f;
                star.localScale = Vector3.one * pulse * (0.08f + (i % 4) * 0.025f);
            }
        }

        private void BuildStars()
        {
            if (starSprite == null)
            {
                return;
            }

            Random.State previous = Random.state;
            Random.InitState(20260727);
            GameObject root = new("DistantStars");
            root.transform.SetParent(transform);
            for (int i = 0; i < starCount; i++)
            {
                GameObject star = new($"Star_{i:00}");
                star.transform.SetParent(root.transform);
                star.transform.position = new Vector3(
                    Random.Range(worldXBounds.x, worldXBounds.y),
                    Random.Range(worldYBounds.x, worldYBounds.y),
                    8f);
                SpriteRenderer renderer = star.AddComponent<SpriteRenderer>();
                renderer.sprite = starSprite;
                renderer.color = i % 9 == 0 ? new Color(1f, 0.55f, 0.7f, 0.7f) : new Color(1f, 0.88f, 0.45f, 0.7f);
                renderer.sortingOrder = -100;
                stars.Add(star.transform);
            }
            Random.state = previous;
        }
    }
}
