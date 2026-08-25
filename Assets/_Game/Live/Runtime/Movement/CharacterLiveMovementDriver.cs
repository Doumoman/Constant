using StarNight.Character.Input;
using StarNight.Character.Live.Player;
using StarNight.Character.Movement;
using StarNight.Character.State;
using UnityEngine;

namespace StarNight.Character.Live.Movement
{
    /// <summary>
    /// FixedUpdate 이동 드라이버. CHAR01 순수 이동 코어(프로브/지면·공중
    /// 모터/중력/점프/착지)를 코스 시뮬레이터와 동일한 순서로 조립하고,
    /// 충돌 질의는 승인된 UnityPhysics2DCharacterCollisionWorld로 수행한다.
    /// 이동 수학·상태 전이의 권위는 전부 순수 계약이며, 이 드라이버는
    /// 스윕 clamp 후 kinematic Rigidbody2D.MovePosition으로 적용만 한다.
    /// 대시/벽점프/이중점프/공격 경로 없음.
    /// </summary>
    public sealed class CharacterLiveMovementDriver : MonoBehaviour
    {
        private const float Skin = 0.01f;
        private const float MinSweep = 0.000001f;

        [SerializeField] private CharacterLivePlayerRig rig;
        [SerializeField] private CharacterLiveMovementSettings settings =
            new CharacterLiveMovementSettings();

        private readonly CharacterCapsuleGeometry capsule =
            CharacterCapsuleGeometry.Default;

        private ICharacterCollisionWorld collisionWorld;
        private CharacterGroundProbe probe;
        private CharacterGroundMotor groundMotor;
        private CharacterAirControlMotor airControlMotor;
        private CharacterGravityMotor gravityMotor;
        private CharacterJumpController jumpController;
        private CharacterLandingDetector landingDetector;
        private CharacterJumpState jumpState;

        private Vector2 velocity;
        private CharacterFacingDirection facing = CharacterFacingDirection.Right;
        private bool wasGrounded;
        private bool isDriving;
        private long physicsTick;
        private double physicsTime;

        public bool IsDriving
        {
            get { return isDriving; }
        }

        public bool IsGroundedNow { get; private set; }

        public Vector2 Velocity
        {
            get { return velocity; }
        }

        public CharacterFacingDirection Facing
        {
            get { return facing; }
        }

        public long PhysicsTick
        {
            get { return physicsTick; }
        }

        /// <summary>스폰 소비 직후 호출 — 운동 상태 초기화 + 구동 시작.</summary>
        public void ResetMotion()
        {
            velocity = Vector2.zero;
            facing = CharacterFacingDirection.Right;
            wasGrounded = false;
            IsGroundedNow = false;
            physicsTick = 0;
            physicsTime = 0d;
            jumpState = new CharacterJumpState();
            isDriving = true;
        }

        private void Awake()
        {
            if (rig == null)
            {
                rig = GetComponent<CharacterLivePlayerRig>();
            }

            collisionWorld = new UnityPhysics2DCharacterCollisionWorld(
                settings.SolidLayers);
            probe = new CharacterGroundProbe(
                collisionWorld, capsule, CharacterGroundProbeSettings.Default);
            groundMotor = new CharacterGroundMotor(
                CharacterGroundMotorSettings.Default);
            airControlMotor = new CharacterAirControlMotor(
                CharacterAirControlSettings.Default);
            gravityMotor = new CharacterGravityMotor(
                CharacterGravitySettings.Default);
            jumpController = new CharacterJumpController(
                CharacterJumpSettings.Default);
            landingDetector = new CharacterLandingDetector();
            jumpState = new CharacterJumpState();
        }

        private void FixedUpdate()
        {
            if (!isDriving || rig == null || !rig.IsBound)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            physicsTick++;
            physicsTime += dt;

            CharacterInputSnapshot input = rig.ConsumeFixedSnapshot(physicsTick);
            Vector2 center = rig.Body.position;

            // (1) 지면 판정 — 순수 프로브(실물리 질의 주입).
            CharacterGroundProbeResult probeResult = probe.Probe(center, velocity.y);
            bool grounded = probeResult.IsGrounded;

            // (2) 착지 정리 + 접지 기록 (코스 시뮬레이터와 동일 순서).
            landingDetector.Step(
                jumpState, wasGrounded, grounded, physicsTime, ref velocity);
            if (grounded)
            {
                jumpState.NoteGrounded(physicsTime);

                // 접지 정착: 프로브 갭을 Skin 간격까지 좁혀 지지면 위에 스냅
                // (코스 시뮬레이터의 support-snap 대응). Skin 간격을 남기는
                // 이유: 바닥과 정확 접촉 상태에서는 수평 캡슐 캐스트가
                // 바닥면을 스치는 히트를 만들어 수평 이동을 오차단한다.
                if (probeResult.HasHit && probeResult.Distance > Skin)
                {
                    center.y -= probeResult.Distance - Skin;
                    if (velocity.y < 0f)
                    {
                        velocity.y = 0f;
                    }
                }
            }

            // (3) 점프: press 기록 → 시작 시도(버퍼/코요테는 순수 계약 소관)
            //     → 가변 release cut.
            if (input.Jump.PressedThisFrame)
            {
                jumpState.NoteJumpPressed(physicsTime);
            }

            if (jumpController.TryStartJump(
                jumpState, grounded, physicsTime, ref velocity))
            {
                grounded = false;
            }

            velocity = jumpController.ApplyJumpRelease(
                jumpState, input.Jump.Held, velocity);

            // (4) 지면 모터 → 공중 제어 → 중력.
            var motorState = new CharacterGroundMotorState(
                velocity,
                facing,
                grounded
                    ? CharacterLocomotionState.Grounded
                    : CharacterLocomotionState.Airborne);
            motorState = groundMotor.Step(
                in motorState, input.Horizontal, settings.AlwaysRun, dt);
            velocity = motorState.Velocity;
            facing = motorState.Facing;

            velocity = airControlMotor.Step(
                velocity, grounded, input.Horizontal, dt);
            velocity = gravityMotor.Step(velocity, grounded, dt);

            // (5) 스윕 clamp 이동 — 축별 캡슐 캐스트 후 MovePosition(결정적).
            Vector2 delta = velocity * dt;

            float moveX = SweepAxis(center, new Vector2(
                Mathf.Sign(delta.x), 0f), Mathf.Abs(delta.x),
                requireHorizontalNormal: true, out bool blockedX);
            center.x += moveX * Mathf.Sign(delta.x);
            if (blockedX)
            {
                velocity.x = 0f;
            }

            float moveY = SweepAxis(center, new Vector2(
                0f, Mathf.Sign(delta.y)), Mathf.Abs(delta.y),
                requireHorizontalNormal: false, out bool blockedY);
            center.y += moveY * Mathf.Sign(delta.y);
            if (blockedY)
            {
                velocity.y = 0f;
            }

            rig.Body.MovePosition(center);

            IsGroundedNow = grounded;
            wasGrounded = grounded;
        }

        /// <summary>
        /// 한 축 스윕: 이동 가능 거리를 반환하고 차단 여부를 보고한다.
        /// 수평 스윕은 벽 성질 법선(|normal.x| ≥ 0.5 — GroundProbe의 법선
        /// 관례와 동일 기준)만 차단으로 인정해 바닥면 스침 히트를 무시한다.
        /// </summary>
        private float SweepAxis(
            Vector2 center,
            Vector2 direction,
            float distance,
            bool requireHorizontalNormal,
            out bool blocked)
        {
            blocked = false;

            if (distance <= MinSweep)
            {
                return 0f;
            }

            CharacterCollisionHit hit = collisionWorld.CapsuleCast(
                center, capsule, direction, distance + Skin);

            if (!hit.HasHit || hit.Distance >= distance + Skin)
            {
                return distance;
            }

            if (requireHorizontalNormal
                && Mathf.Abs(hit.Normal.x) < CharacterGroundProbe.MinimumUpwardNormalY)
            {
                return distance;
            }

            blocked = true;
            return Mathf.Max(0f, hit.Distance - Skin);
        }
    }
}
