using System;
using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(PlayerMotor2D))]
    [RequireComponent(typeof(SafeAnchorService))]
    [RequireComponent(typeof(RaniLampController))]
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        public const int DefaultMaximumHealth = 4;

        [SerializeField]
        private int maximumHealth = DefaultMaximumHealth;

        [SerializeField]
        private float hitInvulnerability = 1.3f;

        [SerializeField]
        private float rescueSafety = 1.5f;

        private PlayerHealthState state;
        private PlayerMotor2D motor;
        private SafeAnchorService safeAnchor;
        private RaniLampController raniLamp;

        public event Action<int, int> HealthChanged;
        public event Action Damaged;
        public event Action Rescued;
        public event Action Defeated;

        public int Current => state?.Current ?? maximumHealth;
        public int Maximum => state?.Maximum ?? maximumHealth;
        public bool IsInvulnerable => state?.IsInvulnerable ?? false;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            HealthChanged?.Invoke(Current, Maximum);
        }

        private void Update()
        {
            EnsureInitialized();
            state.Tick(Time.deltaTime);
        }

        public bool TryTakeDamage(int amount)
        {
            EnsureInitialized();
            if (!state.TryDamage(amount, hitInvulnerability))
            {
                return false;
            }

            Damaged?.Invoke();
            HealthChanged?.Invoke(Current, Maximum);
            if (state.IsDepleted)
            {
                ResolveDepletion();
            }

            return true;
        }

        public bool TakeFallDamage()
        {
            return TryTakeDamage(1);
        }

        public int Heal(int amount)
        {
            EnsureInitialized();
            int healed = state.Heal(amount);
            if (healed > 0)
            {
                HealthChanged?.Invoke(Current, Maximum);
            }

            return healed;
        }

        private void ResolveDepletion()
        {
            if (raniLamp.TryConsumeRescue())
            {
                safeAnchor.Recover(motor);
                state.RestoreAfterRescue(2, rescueSafety);
                HealthChanged?.Invoke(Current, Maximum);
                Rescued?.Invoke();
                return;
            }

            Defeated?.Invoke();
        }

        private void EnsureInitialized()
        {
            if (state != null &&
                motor != null &&
                safeAnchor != null &&
                raniLamp != null)
            {
                return;
            }

            maximumHealth = DefaultMaximumHealth;
            state ??= new PlayerHealthState(maximumHealth);
            motor ??= GetComponent<PlayerMotor2D>();
            safeAnchor ??= GetComponent<SafeAnchorService>();
            raniLamp ??= GetComponent<RaniLampController>();
        }
    }
}
