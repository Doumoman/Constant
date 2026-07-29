using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class SideScrollCamera2D : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private Vector2 offset = new Vector2(0f, 0.6f);

        [SerializeField]
        private float smoothTime = 0.18f;

        [SerializeField]
        private float horizontalLookAhead = 1.2f;

        private Vector3 smoothVelocity;
        private PlayerMotor2D motor;

        private void Start()
        {
            if (target == null)
            {
                motor = FindFirstObjectByType<PlayerMotor2D>();
                target = motor != null ? motor.transform : null;
            }
            else
            {
                motor = target.GetComponent<PlayerMotor2D>();
            }

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float facing = motor != null ? motor.FacingSign : 0f;
            Vector3 desired = new Vector3(
                target.position.x + offset.x + horizontalLookAhead * facing,
                target.position.y + offset.y,
                transform.position.z);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref smoothVelocity,
                smoothTime);
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            float facing = motor != null ? motor.FacingSign : 0f;
            transform.position = new Vector3(
                target.position.x + offset.x + horizontalLookAhead * facing,
                target.position.y + offset.y,
                transform.position.z);
        }
    }
}
