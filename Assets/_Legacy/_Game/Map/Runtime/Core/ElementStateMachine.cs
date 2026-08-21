#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    public sealed class ElementStateMachine : MonoBehaviour
    {
        [SerializeField] private float elapsedSeconds;
        [SerializeField] private bool ticking;

        private MapElementInstance owner;

        public float ElapsedSeconds => elapsedSeconds;
        public bool IsTicking => ticking;

        private void Awake()
        {
            owner = GetComponent<MapElementInstance>();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Bind(MapElementInstance elementOwner)
        {
            owner = elementOwner;
        }

        public void SetTicking(bool shouldTick)
        {
            ticking = shouldTick;
            enabled = shouldTick;
        }

        public void NotifyStateChanged()
        {
            elapsedSeconds = 0f;
        }

        public void RestoreElapsedSeconds(float value)
        {
            elapsedSeconds = Mathf.Max(0f, value);
        }

        public void Tick(float deltaSeconds)
        {
            if (!ticking || owner == null || deltaSeconds <= 0f)
            {
                return;
            }

            var behavior = owner.Definition != null ? owner.Definition.BehaviorProfile : null;
            if (behavior == null)
            {
                return;
            }

            elapsedSeconds += deltaSeconds;
            switch (owner.CurrentState)
            {
                case MapElementState.Warning:
                    if (elapsedSeconds >= Mathf.Max(0f, behavior.WarningSeconds))
                    {
                        owner.TrySetState(MapElementState.Active);
                    }
                    break;

                case MapElementState.Active:
                    if (behavior.ActiveSeconds > 0f && elapsedSeconds >= behavior.ActiveSeconds)
                    {
                        owner.TrySetState(MapElementState.Cooldown);
                    }
                    break;

                case MapElementState.Cooldown:
                    if (elapsedSeconds >= Mathf.Max(0f, behavior.CooldownSeconds))
                    {
                        owner.TrySetState(MapElementState.Idle);
                    }
                    break;
            }
        }
    }
}

#endif
