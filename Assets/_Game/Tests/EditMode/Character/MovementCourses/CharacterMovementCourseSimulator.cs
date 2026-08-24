using System;
using System.Collections.Generic;
using StarNight.Character.Movement;
using StarNight.Character.State;
using UnityEngine;

namespace StarNight.Character.Tests.MovementCourses
{
    /// <summary>
    /// test-only 고정 코스 시뮬레이터. CHAR01 이동 코어(GroundProbe, GroundMotor,
    /// AirControlMotor, GravityMotor, JumpController/State, LandingDetector)를
    /// 그대로 조립해 고정 틱으로 진행한다 — 궤적을 하드코딩하지 않는다.
    /// Scene, Prefab, Rigidbody2D, Tilemap, Animator에 의존하지 않는 순수 C#이다.
    /// </summary>
    public sealed class CharacterMovementCourseSimulator
    {
        /// <summary>한 틱의 스크립트 입력.</summary>
        public readonly struct CourseTickInput
        {
            public CourseTickInput(float horizontal, bool runHeld, bool jumpPressed, bool jumpHeld)
            {
                Horizontal = horizontal;
                RunHeld = runHeld;
                JumpPressed = jumpPressed;
                JumpHeld = jumpHeld;
            }

            public float Horizontal { get; }
            public bool RunHeld { get; }
            public bool JumpPressed { get; }
            public bool JumpHeld { get; }
        }

        /// <summary>입력 스크립트가 참조할 수 있는 틱 컨텍스트(결정적).</summary>
        public readonly struct CourseTickContext
        {
            public CourseTickContext(int tick, Vector2 bottomCenter, Vector2 velocity, bool grounded)
            {
                Tick = tick;
                BottomCenter = bottomCenter;
                Velocity = velocity;
                Grounded = grounded;
            }

            public int Tick { get; }
            public Vector2 BottomCenter { get; }
            public Vector2 Velocity { get; }
            public bool Grounded { get; }
        }

        private readonly struct FloorSegment
        {
            public FloorSegment(float minX, float maxX, float topY)
            {
                MinX = minX;
                MaxX = maxX;
                TopY = topY;
            }

            public float MinX { get; }
            public float MaxX { get; }
            public float TopY { get; }
        }

        /// <summary>
        /// 코스 지형용 fake collision world. 하향 캡슐 캐스트만 지원하며
        /// 발밑 스팬과 겹치는 바닥 세그먼트의 상단까지 거리를 반환한다.
        /// </summary>
        private sealed class CourseCollisionWorld : ICharacterCollisionWorld
        {
            private readonly List<FloorSegment> floors = new List<FloorSegment>();

            public void AddFloor(float minX, float maxX, float topY)
            {
                floors.Add(new FloorSegment(minX, maxX, topY));
            }

            public CharacterCollisionHit CapsuleCast(
                Vector2 origin,
                CharacterCapsuleGeometry capsule,
                Vector2 direction,
                float distance)
            {
                if (direction != Vector2.down)
                {
                    return CharacterCollisionHit.None;
                }

                float bottom = origin.y - capsule.HalfHeight;
                float halfWidth = capsule.Width * 0.5f;

                if (!TryFindSupportTop(origin.x, halfWidth, bottom, out float topY, out int id))
                {
                    return CharacterCollisionHit.None;
                }

                float hitDistance = Mathf.Max(0f, bottom - topY);
                if (hitDistance > distance)
                {
                    return CharacterCollisionHit.None;
                }

                return new CharacterCollisionHit(
                    true, new Vector2(origin.x, topY), Vector2.up, hitDistance, id);
            }

            /// <summary>
            /// 캡슐 바닥 접점 아래의 가장 높은 바닥 상단.
            /// 캡슐의 곡면 바닥은 최저점이 중심 직하이므로 지지 판정은 center x 기준이다 —
            /// 모서리 발끝 걸침(스팬 겹침만 있는 상태)을 지지로 보지 않는다.
            /// 이 기하 전제가 3셀 틈 기본 통과 불가 규칙의 성립 조건이다.
            /// </summary>
            public bool TryFindSupportTop(
                float centerX, float halfWidth, float bottom, out float topY, out int id)
            {
                topY = float.NegativeInfinity;
                id = 0;
                const float penetrationAllowance = 0.25f;

                for (int index = 0; index < floors.Count; index++)
                {
                    FloorSegment floor = floors[index];
                    bool overlaps = centerX >= floor.MinX && centerX <= floor.MaxX;

                    if (!overlaps)
                    {
                        continue;
                    }

                    if (floor.TopY <= bottom + penetrationAllowance && floor.TopY > topY)
                    {
                        topY = floor.TopY;
                        id = index + 1;
                    }
                }

                return id != 0;
            }
        }

        private readonly CourseCollisionWorld world = new CourseCollisionWorld();
        private readonly CharacterCapsuleGeometry capsule = CharacterCapsuleGeometry.Default;
        private readonly CharacterGroundProbe probe;
        private readonly CharacterGroundMotor groundMotor;
        private readonly CharacterAirControlMotor airControlMotor;
        private readonly CharacterGravityMotor gravityMotor;
        private readonly CharacterJumpController jumpController;
        private readonly CharacterLandingDetector landingDetector = new CharacterLandingDetector();

        private float watchMinX = float.NaN;
        private float watchMaxX = float.NaN;
        private float stopWhenGroundedAtOrPastX = float.NaN;

        public CharacterMovementCourseSimulator(
            CharacterGroundMotorSettings groundSettings,
            CharacterAirControlSettings airSettings,
            CharacterGravitySettings gravitySettings,
            CharacterJumpSettings jumpSettings)
        {
            probe = new CharacterGroundProbe(world, capsule, CharacterGroundProbeSettings.Default);
            groundMotor = new CharacterGroundMotor(groundSettings);
            airControlMotor = new CharacterAirControlMotor(airSettings);
            gravityMotor = new CharacterGravityMotor(gravitySettings);
            jumpController = new CharacterJumpController(jumpSettings);
        }

        public static CharacterMovementCourseSimulator CreateDefault()
        {
            return new CharacterMovementCourseSimulator(
                CharacterGroundMotorSettings.Default,
                CharacterAirControlSettings.Default,
                CharacterGravitySettings.Default,
                CharacterJumpSettings.Default);
        }

        public CharacterCapsuleGeometry Capsule
        {
            get { return capsule; }
        }

        public CharacterJumpController JumpController
        {
            get { return jumpController; }
        }

        public void AddFloor(float minX, float maxX, float topY)
        {
            world.AddFloor(minX, maxX, topY);
        }

        /// <summary>이 x 구간 위를 지나는 동안 collider bottom 최저값을 기록한다.</summary>
        public void SetWatchRange(float minX, float maxX)
        {
            watchMinX = minX;
            watchMaxX = maxX;
        }

        /// <summary>grounded 상태로 이 x 이상에 도달하면 조기 종료한다.</summary>
        public void StopWhenGroundedAtOrPastX(float x)
        {
            stopWhenGroundedAtOrPastX = x;
        }

        public CharacterMovementCourseResult Simulate(
            Vector2 startBottomCenter,
            int maxTicks,
            Func<CourseTickContext, CourseTickInput> inputScript)
        {
            const float dt = CharacterMovementCourseConstants.FixedDeltaTime;

            Vector2 center = startBottomCenter + new Vector2(0f, capsule.HalfHeight);
            Vector2 velocity = Vector2.zero;
            CharacterFacingDirection facing = CharacterFacingDirection.Right;
            var jumpState = new CharacterJumpState();

            bool wasGrounded = false;
            bool grounded = false;
            float peakBottom = startBottomCenter.y;
            double peakTime = 0d;
            float minBottomOverWatch = float.PositiveInfinity;
            int jumpInputs = 0;
            int jumpStarts = 0;
            int tick = 0;

            for (tick = 0; tick < maxTicks; tick++)
            {
                double time = tick * (double)dt;

                CharacterGroundProbeResult probeResult = probe.Probe(center, velocity.y);
                grounded = probeResult.IsGrounded;

                landingDetector.Step(jumpState, wasGrounded, grounded, time, ref velocity);

                if (grounded)
                {
                    jumpState.NoteGrounded(time);
                }

                CourseTickInput input = inputScript(
                    new CourseTickContext(tick, BottomCenter(center), velocity, grounded));

                if (input.JumpPressed)
                {
                    jumpState.NoteJumpPressed(time);
                    jumpInputs++;
                }

                if (jumpController.TryStartJump(jumpState, grounded, time, ref velocity))
                {
                    jumpStarts++;
                    grounded = false;
                }

                velocity = jumpController.ApplyJumpRelease(jumpState, input.JumpHeld, velocity);

                var motorState = new CharacterGroundMotorState(
                    velocity,
                    facing,
                    grounded ? CharacterLocomotionState.Grounded : CharacterLocomotionState.Airborne);
                motorState = groundMotor.Step(in motorState, input.Horizontal, input.RunHeld, dt);
                velocity = motorState.Velocity;
                facing = motorState.Facing;

                velocity = airControlMotor.Step(velocity, grounded, input.Horizontal, dt);
                velocity = gravityMotor.Step(velocity, grounded, dt);

                float previousBottom = center.y - capsule.HalfHeight;
                center += velocity * dt;

                // 하강 관통 해소: 이전 틱에 바닥 위였던 지지면 아래로 내려가면 상단에 스냅.
                float bottom = center.y - capsule.HalfHeight;
                if (velocity.y <= 0f
                    && world.TryFindSupportTop(
                        center.x, capsule.Width * 0.5f, previousBottom, out float supportTop, out _)
                    && previousBottom >= supportTop - 0.001f
                    && bottom < supportTop)
                {
                    center.y = supportTop + capsule.HalfHeight;
                    velocity.y = 0f;
                    bottom = supportTop;
                }

                if (bottom > peakBottom)
                {
                    peakBottom = bottom;
                    peakTime = time;
                }

                if (!float.IsNaN(watchMinX)
                    && center.x >= watchMinX
                    && center.x <= watchMaxX
                    && bottom < minBottomOverWatch)
                {
                    minBottomOverWatch = bottom;
                }

                wasGrounded = grounded;

                if (!float.IsNaN(stopWhenGroundedAtOrPastX)
                    && grounded
                    && center.x >= stopWhenGroundedAtOrPastX)
                {
                    tick++;
                    break;
                }
            }

            Vector2 finalBottom = BottomCenter(center);
            return new CharacterMovementCourseResult(
                peakBottom,
                peakTime,
                finalBottom.x,
                finalBottom.y,
                grounded,
                float.IsPositiveInfinity(minBottomOverWatch) ? float.NaN : minBottomOverWatch,
                jumpInputs,
                jumpStarts,
                tick,
                tick * (double)dt);
        }

        private Vector2 BottomCenter(Vector2 center)
        {
            return new Vector2(center.x, center.y - capsule.HalfHeight);
        }
    }
}
