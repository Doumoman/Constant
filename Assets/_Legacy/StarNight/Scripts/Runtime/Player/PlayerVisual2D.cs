#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerVisual2D : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private string idleState = "Player_Idle";
        [SerializeField] private string moveState = "Player_Move";

        private int currentStateHash;

        public void Configure(
            PlayerMotor2D playerMotor,
            SpriteRenderer targetRenderer,
            Animator targetAnimator)
        {
            motor = playerMotor;
            spriteRenderer = targetRenderer;
            animator = targetAnimator;
        }

        private void Awake()
        {
            if (motor == null)
            {
                motor = GetComponent<PlayerMotor2D>();
            }
        }

        private void LateUpdate()
        {
            if (motor == null)
            {
                return;
            }

            float horizontal = motor.Velocity.x;
            if (spriteRenderer != null && Mathf.Abs(horizontal) > 0.05f)
            {
                spriteRenderer.flipX = horizontal < 0f;
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            string targetState = Mathf.Abs(horizontal) > 0.15f ? moveState : idleState;
            int stateHash = Animator.StringToHash(targetState);
            if (stateHash == currentStateHash)
            {
                return;
            }

            currentStateHash = stateHash;
            animator.Play(stateHash, 0, 0f);
        }
    }
}

#endif
