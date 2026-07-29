using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class DamageHazard2D : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int damage = 1;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            health?.TryTakeDamage(damage);
        }
    }
}
