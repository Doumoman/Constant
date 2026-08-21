#if LEGACY_DISABLED
using StarNight.Player;
using UnityEngine;

namespace StarNight.Tools.Umbrella
{
    [DefaultExecutionOrder(-70)]
    [DisallowMultipleComponent]
    public sealed class WindUmbrellaMotor2D : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private Rigidbody2D body;
        [SerializeField, Min(0.5f)] private float openMaxFallSpeed = 5.5f;

        public bool IsHeld { get; private set; }
        public bool OpenRequested { get; private set; }
        public bool IsOpen =>
            IsHeld
            && OpenRequested
            && playerMotor != null
            && !playerMotor.IsGrounded;

        public void Configure(
            PlayerMotor2D motor,
            Rigidbody2D targetBody,
            float configuredOpenMaxFallSpeed = 5.5f)
        {
            playerMotor = motor;
            body = targetBody;
            openMaxFallSpeed = Mathf.Max(0.5f, configuredOpenMaxFallSpeed);
        }

        private void Awake()
        {
            if (playerMotor == null)
            {
                playerMotor = GetComponentInParent<PlayerMotor2D>();
            }

            if (body == null)
            {
                body = playerMotor != null
                    ? playerMotor.Body
                    : GetComponentInParent<Rigidbody2D>();
            }
        }

        private void FixedUpdate()
        {
            SimulatePostMotorStep(Time.fixedDeltaTime);
        }

        public void SetHeld(bool held)
        {
            IsHeld = held;
            if (!held)
            {
                OpenRequested = false;
            }
        }

        public void SetOpen(bool open)
        {
            OpenRequested = open;
        }

        public void SetHeldAndOpen(bool held, bool open)
        {
            IsHeld = held;
            OpenRequested = held && open;
        }

        public void SimulatePostMotorStep(float deltaTime)
        {
            if (body == null || !IsHeld)
            {
                return;
            }

            Vector2 wind = WindField2D.SampleAcceleration(
                body.position,
                IsOpen ? WindResponse.Umbrella : WindResponse.Normal);
            ApplyPostMotorStepForTests(deltaTime, wind);
        }

        public void ApplyPostMotorStepForTests(
            float deltaTime,
            Vector2 windAcceleration)
        {
            if (body == null || !IsHeld)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            if (IsOpen && velocity.y < -openMaxFallSpeed)
            {
                velocity.y = -openMaxFallSpeed;
            }

            velocity += windAcceleration * Mathf.Max(0f, deltaTime);
            body.linearVelocity = velocity;
        }
    }
}

#endif
