using StarNight.Character.Input;
using StarNight.Character.Live.Cameras;
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

        private CharacterCapsuleGeometry capsule = CharacterCapsuleGeometry.Default;
        private Vector2 capsuleOffset;

        private ICharacterCollisionWorld collisionWorld;
        private CharacterGroundProbe probe;
        private CharacterGroundMotor groundMotor;
        private CharacterAirControlMotor airControlMotor;
        private CharacterGravityMotor gravityMotor;
        private CharacterJumpController jumpController;
        private CharacterLandingDetector landingDetector;
        private CharacterJumpState jumpState;
        private CharacterLiveFallDamageState fallDamageState;
        private CharacterLiveLookModeState lookModeState;

        private Vector2 velocity;
        private CharacterFacingDirection facing = CharacterFacingDirection.Right;
        private bool isGrabbing;
        private CharacterLiveGrabSurface grabbedSurface;
        private Collider2D grabbedCollider;
        private Vector2 grabAnchorLocal;
        private int grabSide;
        private double grabReentryAllowedAt;
        private bool isClimbing;
        private CharacterLiveClimbSurface climbedSurface;
        private double climbReentryAllowedAt;
        private Collider2D ignoredOneWayCollider;
        private double oneWayIgnoreEndsAt;
        private bool wasGrounded;
        private bool isDriving;
        private long physicsTick;
        private double physicsTime;
        private float lastFixedDeltaTime;
        private bool isFallTracking;
        private float fallPeakFeetY;

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

        /// <summary>마지막 실제 motor fixed step의 dt (RMAP04 측정 증거용).</summary>
        public float LastFixedDeltaTime { get { return lastFixedDeltaTime; } }

        public CharacterLiveMovementSettings Settings
        {
            get { return settings; }
        }

        /// <summary>RMAP05가 실제 Player에 붙이는 낙하 결과 상태.</summary>
        public CharacterLiveFallDamageState FallDamageState
        {
            get { return fallDamageState; }
        }

        /// <summary>RMAP06 Player-local Tab observation state.</summary>
        public CharacterLiveLookModeState LookModeState
        {
            get { return lookModeState; }
        }

        /// <summary>
        /// The most recent actual Player-state decision supplied to the look
        /// state. It is not a test marker and does not bypass movement state.
        /// </summary>
        public bool IsLookEligible { get; private set; }

        public float CurrentTrackedFallDistance
        {
            get { return isFallTracking ? Mathf.Max(0f, fallPeakFeetY - GetCurrentFeetY()) : 0f; }
        }

        /// <summary>RMAP03의 현재 모서리 Grab 상태(등반/벽차기 상태는 포함하지 않음).</summary>
        public bool IsGrabbing
        {
            get { return isGrabbing; }
        }

        /// <summary>RMAP05가 소비할 수 있는 마지막 안전 Grab 성립 정보.</summary>
        public bool HasSafeGrabContact { get; private set; }

        public Vector2 LastSafeGrabAnchor { get; private set; }

        /// <summary>RMAP04의 현재 ladder/pole climb 상태.</summary>
        public bool IsClimbing { get { return isClimbing; } }

        /// <summary>RMAP05가 소비할 수 있는 마지막 안전 climb 성립 정보.</summary>
        public bool HasSafeClimbContact { get; private set; }

        public Vector2 LastSafeClimbAnchor { get; private set; }

        /// <summary>선택된 one-way 발판 하나만 잠시 무시하고 있는지.</summary>
        public bool IsDroppingThroughOneWay
        {
            get { return ignoredOneWayCollider != null && physicsTime < oneWayIgnoreEndsAt; }
        }

        /// <summary>RMAP02 scene builder가 기존 Player prefab을 국소 fixture로 조립할 때 사용한다.</summary>
        public void ConfigureRmap02(int solidLayerMask)
        {
            settings.ConfigureRmap02(solidLayerMask);
        }

        public void ConfigureRmap05Fall()
        {
            settings.ConfigureRmap05Fall();
        }

        /// <summary>스폰 소비 직후 호출 — 운동 상태 초기화 + 구동 시작.</summary>
        public void ResetMotion()
        {
            ClearOneWayIgnore();
            velocity = Vector2.zero;
            facing = CharacterFacingDirection.Right;
            isGrabbing = false;
            grabbedSurface = null;
            grabbedCollider = null;
            grabAnchorLocal = Vector2.zero;
            grabSide = 0;
            grabReentryAllowedAt = 0d;
            isClimbing = false;
            climbedSurface = null;
            climbReentryAllowedAt = 0d;
            HasSafeGrabContact = false;
            LastSafeGrabAnchor = Vector2.zero;
            HasSafeClimbContact = false;
            LastSafeClimbAnchor = Vector2.zero;
            wasGrounded = false;
            IsGroundedNow = false;
            physicsTick = 0;
            physicsTime = 0d;
            lastFixedDeltaTime = 0f;
            jumpState = new CharacterJumpState();
            EnsureFallDamageState();
            EnsureLookModeState();
            if (fallDamageState != null)
            {
                fallDamageState.ResetForSpawn(gameObject.GetInstanceID(), settings);
            }

            if (lookModeState != null)
            {
                lookModeState.Cancel();
            }

            BeginFallTracking(GetCurrentFeetY());
            isDriving = true;
        }

        private void Awake()
        {
            if (rig == null)
            {
                rig = GetComponent<CharacterLivePlayerRig>();
            }

            EnsureFallDamageState();
            EnsureLookModeState();

            SynchronizeColliderGeometry();

            collisionWorld = new UnityPhysics2DCharacterCollisionWorld(
                settings.SolidLayers);
            probe = new CharacterGroundProbe(
                collisionWorld, capsule, CharacterGroundProbeSettings.Default);
            groundMotor = new CharacterGroundMotor(
                settings.CreateGroundMotorSettings());
            airControlMotor = new CharacterAirControlMotor(
                settings.CreateAirControlSettings());
            gravityMotor = new CharacterGravityMotor(
                settings.CreateGravitySettings());
            jumpController = new CharacterJumpController(
                settings.CreateJumpSettings());
            landingDetector = new CharacterLandingDetector();
            jumpState = new CharacterJumpState();
        }

        private void FixedUpdate()
        {
            if (!isDriving || rig == null || !rig.IsBound)
            {
                return;
            }

            EnsureFallDamageState();
            EnsureLookModeState();

            float dt = Time.fixedDeltaTime;
            lastFixedDeltaTime = dt;
            physicsTick++;
            physicsTime += dt;
            ClearExpiredOneWayIgnore();
            if (fallDamageState != null)
            {
                fallDamageState.Tick((float)physicsTime);
            }

            CharacterInputSnapshot input = rig.ConsumeFixedSnapshot(physicsTick);
            bool canAcceptInput = fallDamageState == null || fallDamageState.CanAcceptInput;
            IsLookEligible = EvaluateLookEligibility(canAcceptInput);
            if (lookModeState != null)
            {
                lookModeState.Step(in input, IsLookEligible, dt);
            }

            if (!canAcceptInput || (lookModeState != null && lookModeState.IsInputLocked))
            {
                input = new CharacterInputSnapshot(0f, false, false, false,
                    default, default, default, default);
                velocity.x = 0f;
            }
            // Rigidbody2D owns the Player's feet pivot in RMAP02.  The
            // collision queries instead use the capsule centre, matching the
            // real CapsuleCollider2D's local offset and dimensions.
            Vector2 center = rig.Body.position + capsuleOffset;

            if (isClimbing)
            {
                if (UpdateClimb(input, center, dt))
                {
                    return;
                }

                center = rig.Body.position + capsuleOffset;
            }
            else if (HasClimbIntent(input) && TryBeginClimb(center))
            {
                DriveClimb(input, center, dt);
                return;
            }

            if (isGrabbing)
            {
                if (UpdateGrab(input))
                {
                    return;
                }

                center = rig.Body.position + capsuleOffset;
            }
            else if (TryBeginGrab(input, center))
            {
                HoldGrabAtAnchor();
                return;
            }

            // (1) 지면 판정 — 순수 프로브(실물리 질의 주입).
            CharacterGroundProbeResult probeResult = probe.Probe(center, velocity.y);
            if (probeResult.HasHit && CharacterLiveOneWayPlatform.TryFind(
                    probeResult.SupportId, out CharacterLiveOneWayPlatform oneWaySupport) &&
                !ShouldBlockOneWay(oneWaySupport, center))
            {
                probeResult = CharacterGroundProbeResult.NotGrounded;
            }
            bool grounded = probeResult.IsGrounded;

            // (2) 착지 정리 + 접지 기록 (코스 시뮬레이터와 동일 순서).
            if (grounded && !wasGrounded)
            {
                HandleFallLanding(center, in probeResult);
            }
            else if (!grounded && wasGrounded)
            {
                BeginFallTracking(GetFeetY(center));
            }

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
            bool beganDropThrough = input.DownHeld && input.Jump.PressedThisFrame &&
                TryBeginOneWayDropThrough(center);
            if (input.Jump.PressedThisFrame && !beganDropThrough)
            {
                jumpState.NoteJumpPressed(physicsTime);
            }

            if (!beganDropThrough && jumpController.TryStartJump(
                jumpState, grounded, physicsTime, ref velocity))
            {
                grounded = false;
                BeginFallTracking(GetFeetY(center));
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
                in motorState, input.Horizontal,
                settings.ResolveAlwaysRun(input.WalkHeld), dt);
            velocity = motorState.Velocity;
            facing = motorState.Facing;

            velocity = airControlMotor.Step(
                velocity, grounded, input.Horizontal, dt);
            if (input.WalkHeld)
            {
                // Shift keeps the existing walk profile while airborne as
                // well, so a slow climb exit cannot accelerate into the
                // default-run distance on the immediately following ticks.
                velocity.x = Mathf.Clamp(velocity.x, -settings.WalkSpeed, settings.WalkSpeed);
            }
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

            rig.Body.MovePosition(center - capsuleOffset);

            if (grounded)
            {
                SetFallBaseline(GetFeetY(center));
            }
            else
            {
                ObserveFallPeak(GetFeetY(center));
            }

            IsGroundedNow = grounded;
            wasGrounded = grounded;
        }

        private bool UpdateGrab(in CharacterInputSnapshot input)
        {
            if (grabbedSurface == null || grabbedCollider == null ||
                !grabbedCollider.enabled || !grabbedSurface.IsGrabAllowed)
            {
                EndGrab(drop: true);
                return false;
            }

            if (input.DownHeld)
            {
                EndGrab(drop: true);
                return false;
            }

            if (input.Jump.PressedThisFrame)
            {
                EndGrab(drop: false);
                velocity.y = settings.JumpVelocity;
                return false;
            }

            // Horizontal input deliberately has no wall-kick branch.  It
            // exits Grab and the existing air-control motor consumes the same
            // snapshot in this fixed step.
            if (Mathf.Abs(input.Horizontal) > 0.01f)
            {
                EndGrab(drop: false);
                return false;
            }

            HoldGrabAtAnchor();
            return true;
        }

        private static bool HasClimbIntent(in CharacterInputSnapshot input)
        {
            return input.UpHeld || input.DownHeld;
        }

        private bool TryBeginClimb(Vector2 center)
        {
            if (physicsTime < climbReentryAllowedAt || !TryFindClimbSurface(out CharacterLiveClimbSurface surface))
            {
                return false;
            }

            if (isGrabbing)
            {
                // A climb axis deliberately wins only when the player supplied
                // Up/Down.  Without one, the RMAP03 Grab path remains unchanged.
                EndGrab(drop: false);
            }

            climbedSurface = surface;
            isClimbing = true;
            velocity = Vector2.zero;
            ResetFallTrackingForTraversal(CharacterLiveFallResetKind.Climb,
                GetFeetY(center));
            HasSafeClimbContact = true;
            LastSafeClimbAnchor = new Vector2(surface.AxisWorldX, center.y - capsuleOffset.y);
            return true;
        }

        private bool UpdateClimb(in CharacterInputSnapshot input, Vector2 center, float dt)
        {
            if (climbedSurface == null || !climbedSurface.IsUsable ||
                !TryFindClimbSurface(out CharacterLiveClimbSurface overlapping) || overlapping != climbedSurface)
            {
                EndClimb();
                return false;
            }

            if (input.Jump.PressedThisFrame && Mathf.Abs(input.Horizontal) > 0.01f)
            {
                // The exit reuses the RMAP02 jump value and run/walk profiles.
                // Later fixed steps stay on the ordinary air-control motor.
                float exitSpeed = input.WalkHeld ? settings.WalkSpeed : settings.RunSpeed;
                EndClimb();
                velocity = new Vector2(Mathf.Sign(input.Horizontal) * exitSpeed, settings.JumpVelocity);
                facing = input.Horizontal > 0f
                    ? CharacterFacingDirection.Right
                    : CharacterFacingDirection.Left;
                return false;
            }

            DriveClimb(input, center, dt);
            return true;
        }

        private void DriveClimb(in CharacterInputSnapshot input, Vector2 center, float dt)
        {
            float verticalSpeed = input.UpHeld
                ? (input.WalkHeld ? settings.ClimbSlowSpeed : settings.ClimbSpeed)
                : input.DownHeld ? -settings.ClimbDownSpeed : 0f;
            float distance = Mathf.Abs(verticalSpeed * dt);
            float moveY = SweepAxis(center, new Vector2(0f, Mathf.Sign(verticalSpeed)), distance,
                requireHorizontalNormal: false, out bool blockedY);
            center.y += moveY * Mathf.Sign(verticalSpeed);
            if (blockedY)
            {
                verticalSpeed = 0f;
            }

            // The axis owns a vertical route only.  Centering is intentionally
            // narrow and no solid cap is added, so a pole top cannot become a floor.
            center.x = climbedSurface.AxisWorldX;
            rig.Body.MovePosition(center - capsuleOffset);
            velocity = Vector2.zero;
            SetFallBaseline(GetFeetY(center));
            IsGroundedNow = false;
            wasGrounded = false;
            LastSafeClimbAnchor = new Vector2(climbedSurface.AxisWorldX,
                center.y - capsuleOffset.y);
        }

        private bool TryFindClimbSurface(out CharacterLiveClimbSurface surface)
        {
            surface = null;
            if (rig == null || rig.BodyCollider == null)
            {
                return false;
            }

            Collider2D[] overlaps = Physics2D.OverlapCapsuleAll(
                rig.Body.position + capsuleOffset,
                capsule.Size,
                CapsuleDirection2D.Vertical,
                0f);
            foreach (Collider2D overlap in overlaps)
            {
                if (overlap == null || overlap == rig.BodyCollider || !overlap.isTrigger)
                {
                    continue;
                }

                CharacterLiveClimbSurface candidate =
                    overlap.GetComponentInParent<CharacterLiveClimbSurface>();
                if (candidate != null && candidate.IsUsable)
                {
                    surface = candidate;
                    return true;
                }
            }

            return false;
        }

        private void EndClimb()
        {
            isClimbing = false;
            climbedSurface = null;
            climbReentryAllowedAt = physicsTime + settings.ClimbReentryDelay;
            BeginFallTracking(GetCurrentFeetY());
        }

        private bool TryBeginOneWayDropThrough(Vector2 center)
        {
            RaycastHit2D[] hits = Physics2D.CapsuleCastAll(
                center, capsule.Size, CapsuleDirection2D.Vertical, 0f, Vector2.down,
                settings.GrabProbeDistance, settings.SolidLayers);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider == rig.BodyCollider ||
                    !CharacterLiveOneWayPlatform.TryFind(hit.collider.GetInstanceID(),
                        out CharacterLiveOneWayPlatform oneWay) || !ShouldBlockOneWay(oneWay, center))
                {
                    continue;
                }

                ClearOneWayIgnore();
                ignoredOneWayCollider = hit.collider;
                oneWayIgnoreEndsAt = physicsTime + settings.OneWayDropThroughDuration;
                Physics2D.IgnoreCollision(rig.BodyCollider, ignoredOneWayCollider, true);
                velocity.y = Mathf.Min(velocity.y, -0.1f);
                IsGroundedNow = false;
                wasGrounded = false;
                BeginFallTracking(GetFeetY(center));
                return true;
            }

            return false;
        }

        private bool ShouldBlockOneWay(CharacterLiveOneWayPlatform oneWay, Vector2 center)
        {
            if (oneWay == null || oneWay.PlatformCollider == null || !oneWay.PlatformCollider.enabled ||
                (ignoredOneWayCollider == oneWay.PlatformCollider && physicsTime < oneWayIgnoreEndsAt))
            {
                return false;
            }

            float playerFeet = center.y - capsule.HalfHeight;
            return playerFeet >= oneWay.TopWorldY - Skin * 2f;
        }

        private void ClearExpiredOneWayIgnore()
        {
            if (ignoredOneWayCollider != null && physicsTime >= oneWayIgnoreEndsAt)
            {
                ClearOneWayIgnore();
            }
        }

        private void ClearOneWayIgnore()
        {
            if (ignoredOneWayCollider != null && rig != null && rig.BodyCollider != null)
            {
                Physics2D.IgnoreCollision(rig.BodyCollider, ignoredOneWayCollider, false);
            }

            ignoredOneWayCollider = null;
            oneWayIgnoreEndsAt = 0d;
        }

        private bool TryBeginGrab(in CharacterInputSnapshot input, Vector2 center)
        {
            if (physicsTime < grabReentryAllowedAt || input.DownHeld ||
                input.Jump.PressedThisFrame || velocity.y > settings.GrabMaxUpwardVelocity)
            {
                return false;
            }

            int preferredSide = input.Horizontal > 0.01f ? 1 :
                input.Horizontal < -0.01f ? -1 :
                facing == CharacterFacingDirection.Right ? 1 : -1;
            if (TryBeginGrabOnSide(preferredSide, center))
            {
                return true;
            }

            return Mathf.Abs(input.Horizontal) <= 0.01f &&
                TryBeginGrabOnSide(-preferredSide, center);
        }

        private bool TryBeginGrabOnSide(int side, Vector2 center)
        {
            Vector2 origin = center + new Vector2(side * (capsule.Width * 0.5f - Skin), 0f);
            RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(side, 0f),
                settings.GrabProbeDistance, settings.SolidLayers);
            if (hit.collider == null || hit.collider == rig.BodyCollider || hit.collider.isTrigger)
            {
                return false;
            }

            CharacterLiveGrabSurface surface =
                hit.collider.GetComponentInParent<CharacterLiveGrabSurface>();
            if (surface == null || !surface.IsGrabAllowed)
            {
                return false;
            }

            Bounds bounds = hit.collider.bounds;
            Vector2 corner = new Vector2(side > 0 ? bounds.min.x : bounds.max.x, bounds.max.y);
            Vector2 desiredFeet = corner + new Vector2(
                -side * settings.GrabSideOffset, -settings.GrabHangOffset);
            if (Mathf.Abs(rig.Body.position.x - desiredFeet.x) > settings.GrabProbeDistance ||
                Mathf.Abs(rig.Body.position.y - desiredFeet.y) > settings.GrabVerticalWindow)
            {
                return false;
            }

            // A solid directly above the outward shoulder means this is not
            // an exposed world-up corner.  This keeps ceiling/inner-corner
            // contacts out of the Grab candidate set.
            Vector2 exposedOrigin = corner + new Vector2(-side * 0.08f, 0.02f);
            if (Physics2D.Raycast(exposedOrigin, Vector2.up, 0.12f,
                    settings.SolidLayers).collider != null)
            {
                return false;
            }

            grabbedSurface = surface;
            grabbedCollider = hit.collider;
            grabAnchorLocal = surface.transform.InverseTransformPoint(corner);
            grabSide = side;
            isGrabbing = true;
            ResetFallTrackingForTraversal(CharacterLiveFallResetKind.Grab, desiredFeet.y);
            HasSafeGrabContact = true;
            LastSafeGrabAnchor = corner;
            return true;
        }

        private void HoldGrabAtAnchor()
        {
            Vector2 anchor = grabbedSurface.transform.TransformPoint(grabAnchorLocal);
            Vector2 feet = anchor + new Vector2(
                -grabSide * settings.GrabSideOffset, -settings.GrabHangOffset);
            rig.Body.MovePosition(feet);
            velocity = Vector2.zero;
            SetFallBaseline(feet.y);
            IsGroundedNow = false;
            wasGrounded = false;
            LastSafeGrabAnchor = anchor;
        }

        private void EndGrab(bool drop)
        {
            isGrabbing = false;
            grabbedSurface = null;
            grabbedCollider = null;
            grabSide = 0;
            grabReentryAllowedAt = physicsTime + settings.GrabReentryDelay;
            if (drop)
            {
                velocity.y = Mathf.Min(velocity.y, -0.1f);
            }

            BeginFallTracking(GetCurrentFeetY());
        }

        private void EnsureFallDamageState()
        {
            if (fallDamageState == null)
            {
                fallDamageState = GetComponent<CharacterLiveFallDamageState>();
            }
        }

        private void EnsureLookModeState()
        {
            if (lookModeState == null)
            {
                lookModeState = GetComponent<CharacterLiveLookModeState>();
            }

            if (lookModeState == null)
            {
                lookModeState = gameObject.AddComponent<CharacterLiveLookModeState>();
            }
        }

        private bool EvaluateLookEligibility(bool canAcceptInput)
        {
            if (!canAcceptInput || IsDroppingThroughOneWay)
            {
                return false;
            }

            bool stationary = velocity.sqrMagnitude <= 0.0001f;
            bool stationaryGround = IsGroundedNow && stationary;
            bool stationaryGrab = isGrabbing && stationary && grabbedSurface != null &&
                !grabbedSurface.IsMovingSafeSolid;
            bool stationaryClimb = isClimbing && stationary;
            return stationaryGround || stationaryGrab || stationaryClimb;
        }

        private float GetCurrentFeetY()
        {
            return rig != null && rig.Body != null ? rig.Body.position.y : 0f;
        }

        private float GetFeetY(Vector2 center)
        {
            return center.y - capsule.HalfHeight;
        }

        private void BeginFallTracking(float feetY)
        {
            isFallTracking = true;
            fallPeakFeetY = feetY;
        }

        private void SetFallBaseline(float feetY)
        {
            isFallTracking = false;
            fallPeakFeetY = feetY;
        }

        private void ObserveFallPeak(float feetY)
        {
            if (!isFallTracking)
            {
                BeginFallTracking(feetY);
                return;
            }

            fallPeakFeetY = Mathf.Max(fallPeakFeetY, feetY);
        }

        private void ResetFallTrackingForTraversal(
            CharacterLiveFallResetKind kind,
            float feetY)
        {
            float observedDistance = isFallTracking
                ? Mathf.Max(0f, fallPeakFeetY - feetY)
                : 0f;
            if (fallDamageState != null)
            {
                fallDamageState.RecordTraversalReset(kind, observedDistance);
            }

            SetFallBaseline(feetY);
        }

        private void HandleFallLanding(Vector2 center, in CharacterGroundProbeResult probeResult)
        {
            float landingFeetY = GetFeetY(center);
            if (probeResult.HasHit && probeResult.Distance > Skin)
            {
                landingFeetY -= probeResult.Distance - Skin;
            }

            float fallDistance = isFallTracking
                ? Mathf.Max(0f, fallPeakFeetY - landingFeetY)
                : 0f;
            bool landedOnOneWay = probeResult.HasHit &&
                CharacterLiveOneWayPlatform.TryFind(probeResult.SupportId, out _);
            if (fallDamageState != null)
            {
                // Required order: distance -> damage/stun/death -> reset.
                fallDamageState.ApplyLanding(fallDistance, landedOnOneWay,
                    (float)physicsTime, settings);
            }

            SetFallBaseline(landingFeetY);
            if (fallDamageState != null)
            {
                fallDamageState.RecordTraversalReset(
                    CharacterLiveFallResetKind.Landing, fallDistance);
                fallDamageState.CompleteLandingReset();
            }
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

            RaycastHit2D hit = FindBlockingSweepHit(center, direction, distance + Skin);
            if (hit.collider == null || hit.distance >= distance + Skin)
            {
                return distance;
            }

            if (requireHorizontalNormal
                && Mathf.Abs(hit.normal.x) < CharacterGroundProbe.MinimumUpwardNormalY)
            {
                return distance;
            }

            blocked = true;
            return Mathf.Max(0f, hit.distance - Skin);
        }

        private RaycastHit2D FindBlockingSweepHit(Vector2 center, Vector2 direction, float distance)
        {
            RaycastHit2D[] hits = Physics2D.CapsuleCastAll(
                center, capsule.Size, CapsuleDirection2D.Vertical, 0f, direction, distance,
                settings.SolidLayers);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider == rig.BodyCollider || hit.collider.isTrigger)
                {
                    continue;
                }

                // A capsule already resting on a floor can report that floor
                // at distance zero when casting upward.  It is support, not a
                // ceiling, so it must not stall a climb or an ordinary jump.
                if (direction.y > 0.01f && hit.normal.y > CharacterGroundProbe.MinimumUpwardNormalY)
                {
                    continue;
                }

                if (CharacterLiveOneWayPlatform.TryFind(hit.collider.GetInstanceID(),
                    out CharacterLiveOneWayPlatform oneWay))
                {
                    // Top-only collision: no side/underside block, and the
                    // temporarily selected drop-through platform alone is ignored.
                    if (direction.y >= -0.01f || !ShouldBlockOneWay(oneWay, center))
                    {
                        continue;
                    }
                }

                return hit;
            }

            return default;
        }

        private void SynchronizeColliderGeometry()
        {
            if (rig == null || rig.BodyCollider == null)
            {
                capsule = CharacterCapsuleGeometry.Default;
                capsuleOffset = Vector2.zero;
                return;
            }

            Vector2 size = rig.BodyCollider.size;
            capsule = new CharacterCapsuleGeometry(size.x, size.y);
            capsuleOffset = rig.BodyCollider.offset;
        }

        private void OnDisable()
        {
            ClearOneWayIgnore();
        }
    }
}
