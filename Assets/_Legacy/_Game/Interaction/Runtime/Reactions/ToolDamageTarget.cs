#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Interaction.Reactions
{
    public enum ToolDamageTargetKind
    {
        Enemy,
        BreakableContainer,
    }

    [DisallowMultipleComponent]
    public sealed class ToolDamageTarget : MonoBehaviour, IToolDamageReceiver, IMapElementDamageReceiver
    {
        [SerializeField] private ToolDamageTargetKind targetKind;
        [SerializeField, Min(1)] private int maximumHealth = 1;
        [SerializeField, Min(0)] private int currentHealth = 1;
        [SerializeField] private bool defeated;

        private readonly HashSet<long> processedActionIds = new HashSet<long>();

        public ToolDamageTargetKind TargetKind => targetKind;
        public int CurrentHealth => currentHealth;
        public bool Defeated => defeated;

        private void Awake()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
            if (currentHealth <= 0 && !defeated)
            {
                currentHealth = maximumHealth;
            }
        }

        public bool TryReceiveToolDamage(ToolDamageEvent damageEvent)
        {
            if (defeated || damageEvent.Damage <= 0 || !processedActionIds.Add(damageEvent.ActionId))
            {
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - damageEvent.Damage);
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body != null && body.simulated)
            {
                body.linearVelocity += damageEvent.Knockback;
            }
            if (currentHealth == 0)
            {
                SetDefeated();
            }
            return true;
        }

        public bool ReceiveMapElementDamage(MapElementDamageEvent damageEvent)
        {
            return TryReceiveToolDamage(new ToolDamageEvent(
                damageEvent.ActivationId,
                damageEvent.Damage,
                damageEvent.Knockback,
                ToolTag.Projectile,
                damageEvent.Source,
                damageEvent.Source));
        }

        public void ConfigureForTests(ToolDamageTargetKind kind, int health)
        {
            targetKind = kind;
            maximumHealth = Mathf.Max(1, health);
            currentHealth = maximumHealth;
            defeated = false;
            processedActionIds.Clear();
        }

        private void SetDefeated()
        {
            defeated = true;
            foreach (Collider2D targetCollider in GetComponentsInChildren<Collider2D>(true))
            {
                targetCollider.enabled = false;
            }
            foreach (Renderer targetRenderer in GetComponentsInChildren<Renderer>(true))
            {
                targetRenderer.enabled = false;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = targetKind == ToolDamageTargetKind.Enemy
                ? new Color(1f, 0.25f, 0.25f, 0.8f)
                : new Color(0.65f, 0.4f, 0.18f, 0.8f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
        }
    }
}

#endif
