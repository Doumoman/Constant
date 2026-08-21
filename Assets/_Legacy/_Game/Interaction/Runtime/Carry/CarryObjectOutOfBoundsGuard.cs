#if LEGACY_DISABLED
using System.Collections;
using UnityEngine;

namespace StarNight.Interaction.Carry
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CarryableObject))]
    public sealed class CarryObjectOutOfBoundsGuard : MonoBehaviour
    {
        public const float VoidRecoverySeconds = 0.8f;
        public const float ExplosionRecoverySeconds = 0.5f;

        [SerializeField] private CarryableObject carryable;
        [SerializeField] private Transform lastCriticalObjectAnchor;
        [SerializeField] private Transform nextRoomEntryAnchor;

        private Coroutine recoveryRoutine;

        private void Awake()
        {
            if (carryable == null)
            {
                carryable = GetComponent<CarryableObject>();
            }
        }

        public void SetLastCriticalObjectAnchor(Transform anchor)
        {
            lastCriticalObjectAnchor = anchor;
        }

        public void SetNextRoomEntryAnchor(Transform anchor)
        {
            nextRoomEntryAnchor = anchor;
        }

        public void NotifyEnteredVoid()
        {
            if (!IsCriticalCarry())
            {
                Destroy(gameObject);
                return;
            }

            BeginRecovery(VoidRecoverySeconds, lastCriticalObjectAnchor);
        }

        public void NotifyExplosionWouldDestroy()
        {
            if (!IsCriticalCarry())
            {
                Destroy(gameObject);
                return;
            }

            BeginRecovery(ExplosionRecoverySeconds, lastCriticalObjectAnchor);
        }

        public void NotifyLostDuringRoomTransition()
        {
            if (IsCriticalCarry())
            {
                BeginRecovery(0f, nextRoomEntryAnchor != null ? nextRoomEntryAnchor : lastCriticalObjectAnchor);
            }
        }

        public void RecoverImmediatelyForTests(Vector2 position)
        {
            carryable?.RecoverTo(position);
        }

        private bool IsCriticalCarry()
        {
            return carryable != null
                && carryable.Definition != null
                && carryable.Definition.CriticalCarry;
        }

        private void BeginRecovery(float delay, Transform anchor)
        {
            if (recoveryRoutine != null)
            {
                StopCoroutine(recoveryRoutine);
            }

            recoveryRoutine = StartCoroutine(RecoverAfter(delay, anchor));
        }

        private IEnumerator RecoverAfter(float delay, Transform anchor)
        {
            carryable?.BeginRecovery();
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Vector2 destination = anchor != null ? anchor.position : Vector2.zero;
            carryable?.RecoverTo(destination);
            recoveryRoutine = null;
        }
    }
}

#endif
