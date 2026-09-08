using UnityEngine;

namespace StarNight.Character.Live.Movement
{
    /// <summary>
    /// Small RMAP03 fixture mover.  The safe solid itself is a kinematic
    /// Rigidbody2D, so a Grab anchor follows an actual Collider2D rather than
    /// a synthetic Player-position correction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CharacterLiveGrabMovingSolid : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Vector2 start;
        [SerializeField] private Vector2 end = new Vector2(3f, 0f);
        [SerializeField] private float speed = 1.5f;

        private float travelled;

        public Vector2 Start { get { return start; } }
        public Vector2 End { get { return end; } }
        public float Speed { get { return speed; } }

        public void Configure(Rigidbody2D value, Vector2 from, Vector2 to, float unitsPerSecond)
        {
            body = value;
            start = from;
            end = to;
            speed = Mathf.Max(0f, unitsPerSecond);
            travelled = 0f;
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.position = start;
            }
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        private void FixedUpdate()
        {
            if (body == null || speed <= 0f)
            {
                return;
            }

            float length = Vector2.Distance(start, end);
            if (length <= Mathf.Epsilon)
            {
                body.MovePosition(start);
                return;
            }

            travelled += Time.fixedDeltaTime * speed / length;
            body.MovePosition(Vector2.Lerp(start, end, Mathf.PingPong(travelled, 1f)));
        }
    }
}
