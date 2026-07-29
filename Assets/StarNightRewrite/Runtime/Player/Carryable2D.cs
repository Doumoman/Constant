using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class Carryable2D : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField]
        private float maximumCarryMass = 4f;

        private Rigidbody2D body;
        private Collider2D itemCollider;
        private Transform originalParent;
        private RigidbodyType2D originalBodyType;
        private float originalGravityScale;

        public string Prompt => "E 들기";
        public bool CanBeCarried => body != null && body.mass <= maximumCarryMass;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            itemCollider = GetComponent<Collider2D>();
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return CanBeCarried && interactor != null && !interactor.Carry.IsCarrying;
        }

        public void Interact(PlayerInteractor interactor)
        {
            interactor.Carry.TryPickUp(this);
        }

        public void AttachTo(Transform point)
        {
            originalParent = transform.parent;
            originalBodyType = body.bodyType;
            originalGravityScale = body.gravityScale;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            itemCollider.enabled = false;
            transform.SetParent(point, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void Detach(Vector2 releaseVelocity)
        {
            transform.SetParent(originalParent, true);
            itemCollider.enabled = true;
            body.bodyType = originalBodyType;
            body.gravityScale = originalGravityScale;
            body.linearVelocity = releaseVelocity;
        }
    }
}
