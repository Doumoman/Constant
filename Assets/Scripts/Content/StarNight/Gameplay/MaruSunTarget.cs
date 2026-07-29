using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MaruSunTarget : MonoBehaviour
    {
        [SerializeField] private MaruDirector director;
        [SerializeField, Min(0.5f)] private float blindDuration = 5f;

        public void Configure(MaruDirector targetDirector, float duration = 5f)
        {
            director = targetDirector;
            blindDuration = Mathf.Max(0.5f, duration);
        }

        public void Blind()
        {
            if (director == null)
            {
                director = FindFirstObjectByType<MaruDirector>();
            }
            director?.Blind(blindDuration);
        }
    }
}
