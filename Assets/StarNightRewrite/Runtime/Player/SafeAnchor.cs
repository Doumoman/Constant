using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class SafeAnchor : MonoBehaviour
    {
        [SerializeField]
        private Transform recoveryPoint;

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            SafeAnchorService service = other.GetComponentInParent<SafeAnchorService>();
            if (service == null)
            {
                return;
            }

            Vector2 point = recoveryPoint != null
                ? recoveryPoint.position
                : transform.position;
            service.Register(point);
        }
    }
}
