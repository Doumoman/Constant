using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerCarry))]
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField]
        private float interactionRadius = 1.35f;

        [SerializeField]
        private LayerMask interactionMask = ~0;

        private readonly Collider2D[] overlaps = new Collider2D[16];
        private PlayerInputReader input;
        private PlayerCarry carry;
        private ContactFilter2D filter;
        private IPlayerInteractable currentTarget;

        public string CurrentPrompt
        {
            get
            {
                if (carry.IsCarrying)
                {
                    return "E 내려놓기 · 이동하며 E 던지기";
                }

                return currentTarget?.Prompt ?? string.Empty;
            }
        }

        public PlayerCarry Carry => carry;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            carry = GetComponent<PlayerCarry>();
            filter = new ContactFilter2D
            {
                useTriggers = true
            };
            filter.SetLayerMask(interactionMask);
        }

        private void OnEnable()
        {
            input.InteractPressed += OnInteractPressed;
        }

        private void OnDisable()
        {
            input.InteractPressed -= OnInteractPressed;
        }

        private void Update()
        {
            currentTarget = FindNearestTarget();
        }

        private void OnInteractPressed()
        {
            if (carry.IsCarrying)
            {
                carry.Release(Mathf.Abs(input.MoveX) > 0.25f);
                return;
            }

            IPlayerInteractable target = FindNearestTarget();
            if (target != null && target.CanInteract(this))
            {
                target.Interact(this);
            }
        }

        private IPlayerInteractable FindNearestTarget()
        {
            int count = Physics2D.OverlapCircle(
                transform.position,
                interactionRadius,
                filter,
                overlaps);

            IPlayerInteractable nearest = null;
            float nearestDistance = float.PositiveInfinity;

            for (int colliderIndex = 0; colliderIndex < count; colliderIndex++)
            {
                Collider2D candidateCollider = overlaps[colliderIndex];
                if (candidateCollider == null ||
                    candidateCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                MonoBehaviour[] behaviours =
                    candidateCollider.GetComponentsInParent<MonoBehaviour>();
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is not IPlayerInteractable candidate ||
                        !candidate.CanInteract(this))
                    {
                        continue;
                    }

                    float distance = ((Vector2)candidateCollider.bounds.center -
                        (Vector2)transform.position).sqrMagnitude;
                    if (distance >= nearestDistance)
                    {
                        continue;
                    }

                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
