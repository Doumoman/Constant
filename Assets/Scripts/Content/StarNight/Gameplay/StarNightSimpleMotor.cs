using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class StarNightSimpleMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float umbrellaForce = 8f;
        [SerializeField] private LayerMask groundMask;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private float horizontal;
        private float defaultGravity;
        private bool umbrellaOpen;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            defaultGravity = body.gravityScale;
            if (groundMask.value == 0)
            {
                groundMask = 1 << 7;
            }
        }

        private void Update()
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(horizontal);
                transform.localScale = scale;
            }
            if (Input.GetButtonDown("Jump") && IsGrounded())
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            }

            umbrellaOpen = Input.GetKey(KeyCode.B);
            body.gravityScale = umbrellaOpen && body.linearVelocity.y < 0f ? defaultGravity * 0.28f : defaultGravity;
            if (Input.GetKeyDown(KeyCode.B))
            {
                UmbrellaShove();
            }
        }

        private void FixedUpdate()
        {
            body.linearVelocity = new Vector2(horizontal * moveSpeed, body.linearVelocity.y);
        }

        private bool IsGrounded()
        {
            if (bodyCollider == null)
            {
                return false;
            }
            Bounds bounds = bodyCollider.bounds;
            return Physics2D.BoxCast(bounds.center, new Vector2(bounds.size.x * 0.8f, 0.12f), 0f, Vector2.down, bounds.extents.y + 0.12f, groundMask);
        }

        private void UmbrellaShove()
        {
            Vector2 direction = transform.localScale.x < 0f ? Vector2.left : Vector2.right;
            Collider2D[] hits = Physics2D.OverlapCircleAll((Vector2)transform.position + direction * 1.1f, 1.35f);
            foreach (Collider2D hit in hits)
            {
                if (hit.attachedRigidbody == null || hit.attachedRigidbody == body)
                {
                    continue;
                }
                hit.attachedRigidbody.AddForce(direction * umbrellaForce + Vector2.up * 1.5f, ForceMode2D.Impulse);
            }
        }
    }
}
