using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightJourneyNavigation : MonoBehaviour
    {
        [SerializeField] private float fallThreshold = -12f;
        [SerializeField] private Vector3 lastSafePosition = new(-2f, -1.4f, 0f);
        private StarNightPlayerAgent player;

        private void Awake()
        {
            player = GetComponent<StarNightPlayerAgent>();
        }

        private void Update()
        {
            if (transform.position.y >= fallThreshold)
            {
                return;
            }

            transform.position = lastSafePosition;
            if (TryGetComponent(out Rigidbody2D body))
            {
                body.linearVelocity = Vector2.zero;
            }
            player?.TakeDamage(1, "별가루 아래로 떨어졌다. 마지막 달등불로 돌아왔다.");
        }

        public void SetCheckpoint(Vector3 position)
        {
            lastSafePosition = position;
        }
    }

}
