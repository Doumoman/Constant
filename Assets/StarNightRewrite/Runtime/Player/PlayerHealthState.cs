using UnityEngine;

namespace StarNight.Rewrite.Player
{
    public sealed class PlayerHealthState
    {
        public PlayerHealthState(int maximumHealth)
        {
            Maximum = Mathf.Max(1, maximumHealth);
            Current = Maximum;
        }

        public int Maximum { get; }
        public int Current { get; private set; }
        public float InvulnerabilityRemaining { get; private set; }
        public bool IsInvulnerable => InvulnerabilityRemaining > 0f;
        public bool IsDepleted => Current <= 0;

        public void Tick(float deltaTime)
        {
            InvulnerabilityRemaining = Mathf.Max(
                0f,
                InvulnerabilityRemaining - Mathf.Max(0f, deltaTime));
        }

        public bool TryDamage(int amount, float invulnerabilitySeconds)
        {
            if (amount <= 0 || IsInvulnerable || IsDepleted)
            {
                return false;
            }

            Current = Mathf.Max(0, Current - amount);
            InvulnerabilityRemaining = Mathf.Max(0f, invulnerabilitySeconds);
            return true;
        }

        public int Heal(int amount)
        {
            if (amount <= 0 || IsDepleted)
            {
                return 0;
            }

            int previous = Current;
            Current = Mathf.Min(Maximum, Current + amount);
            return Current - previous;
        }

        public void RestoreAfterRescue(int health, float safeSeconds)
        {
            Current = Mathf.Clamp(health, 1, Maximum);
            InvulnerabilityRemaining = Mathf.Max(0f, safeSeconds);
        }
    }
}
