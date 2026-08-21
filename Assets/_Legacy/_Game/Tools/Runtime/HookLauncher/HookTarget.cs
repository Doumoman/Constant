#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Carry;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.HookLauncher
{
    [DisallowMultipleComponent]
    public sealed class HookTarget : MonoBehaviour, IHookTrigger
    {
        [SerializeField] private HookResponse response = HookResponse.PullPlayerToTarget;
        [SerializeField] private Rigidbody2D targetBody;
        private int triggerCount;

        public event Action<long, GameObject> Triggered;

        public HookResponse Response => response;
        public int TriggerCount => triggerCount;

        public void ConfigureForTests(HookResponse configuredResponse, Rigidbody2D configuredBody = null)
        {
            response = configuredResponse;
            targetBody = configuredBody;
        }

        public HookLatch CreateLatch()
        {
            var carryable = GetComponentInParent<CarryableObject>();
            if (carryable != null && carryable.Definition != null)
            {
                return new HookLatch(
                    carryable.gameObject,
                    carryable.Definition.HookResponse,
                    carryable.Body,
                    this);
            }

            return new HookLatch(
                gameObject,
                response,
                targetBody != null ? targetBody : GetComponentInParent<Rigidbody2D>(),
                this);
        }

        public bool TryTriggerHook(long actionId, GameObject instigator)
        {
            if (response != HookResponse.Trigger)
            {
                return false;
            }

            var accepted = false;
            var behaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IToolReactionReceiver receiver)
                {
                    var result = receiver.TryReact(new ToolReactionContext
                    {
                        ActionId = unchecked((int)actionId),
                        Tags = ToolTag.Hook,
                        Direction = Vector2Int.zero,
                        Magnitude = 1f,
                        Source = instigator,
                        Instigator = instigator,
                    });
                    accepted |= result.Accepted;
                }
            }

            triggerCount++;
            Triggered?.Invoke(actionId, instigator);
            return accepted;
        }
    }
}

#endif
