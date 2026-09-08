using System;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Live.Movement
{
    /// <summary>
    /// 라이브 이동 드라이버 구성. 순수 motor의 값 객체를 여기서 한 번만
    /// 조립해 Player와 fixture가 다른 이동 상수를 복제하지 않게 한다.
    /// </summary>
    [Serializable]
    public sealed class CharacterLiveMovementSettings
    {
        [Tooltip("지형 판정 대상 레이어(기본: Default). 플레이어는 이 마스크 밖 레이어에 둔다.")]
        [SerializeField] private LayerMask solidLayers = 1;

        [Tooltip("Shift 보행이 아닌 경우 달리기 속도를 사용한다.")]
        [SerializeField] private bool alwaysRun = true;

        [SerializeField] private float walkSpeed = 2.8f;
        [SerializeField] private float runSpeed = 5.5f;
        [SerializeField] private float groundAcceleration = 30f;
        [SerializeField] private float groundDeceleration = 40f;
        [SerializeField] private float airAcceleration = 40f;
        [SerializeField] private float maxAirSpeed = 5.5f;
        [SerializeField] private float riseGravity = 20f;
        [SerializeField] private float fallGravity = 26f;
        [SerializeField] private float maxFallSpeed = 20f;
        [SerializeField] private float jumpVelocity = 7.2f;
        [SerializeField] private float coyoteTime = 0.08f;
        [SerializeField] private float jumpBufferTime = 0.10f;
        [SerializeField] private float releaseCutMultiplier = 0.42f;
        [SerializeField] private float grabProbeDistance = 0.35f;
        [SerializeField] private float grabVerticalWindow = 0.45f;
        [SerializeField] private float grabSideOffset = 0.21f;
        [SerializeField] private float grabHangOffset = 0.62f;
        [SerializeField] private float grabReentryDelay = 0.12f;
        [SerializeField] private float grabMaxUpwardVelocity = 0.01f;
        [SerializeField] private float climbSpeed = 4f;
        [SerializeField] private float climbSlowSpeed = 2f;
        [SerializeField] private float climbDownSpeed = 5f;
        [SerializeField] private float climbReentryDelay = 0.12f;
        [SerializeField] private float oneWayDropThroughDuration = 0.18f;
        [SerializeField] private int fallMaxHealth = 5;
        [SerializeField] private float fallStunDuration = 0.5f;
        [SerializeField] private float fallStunThreshold = 6f;
        [SerializeField] private float fallFatalThreshold = 30f;

        public LayerMask SolidLayers
        {
            get { return solidLayers; }
        }

        public bool AlwaysRun
        {
            get { return alwaysRun; }
        }

        public float WalkSpeed { get { return walkSpeed; } }
        public float RunSpeed { get { return runSpeed; } }
        public float JumpVelocity { get { return jumpVelocity; } }
        public float CoyoteTime { get { return coyoteTime; } }
        public float JumpBufferTime { get { return jumpBufferTime; } }
        public float GrabProbeDistance { get { return grabProbeDistance; } }
        public float GrabVerticalWindow { get { return grabVerticalWindow; } }
        public float GrabSideOffset { get { return grabSideOffset; } }
        public float GrabHangOffset { get { return grabHangOffset; } }
        public float GrabReentryDelay { get { return grabReentryDelay; } }
        public float GrabMaxUpwardVelocity { get { return grabMaxUpwardVelocity; } }
        public float ClimbSpeed { get { return climbSpeed; } }
        public float ClimbSlowSpeed { get { return climbSlowSpeed; } }
        public float ClimbDownSpeed { get { return climbDownSpeed; } }
        public float ClimbReentryDelay { get { return climbReentryDelay; } }
        public float OneWayDropThroughDuration { get { return oneWayDropThroughDuration; } }
        public int FallMaxHealth { get { return fallMaxHealth; } }
        public float FallStunDuration { get { return fallStunDuration; } }
        public float FallStunThreshold { get { return fallStunThreshold; } }
        public float FallFatalThreshold { get { return fallFatalThreshold; } }

        public bool ResolveAlwaysRun(bool walkHeld)
        {
            return alwaysRun && !walkHeld;
        }

        public CharacterGroundMotorSettings CreateGroundMotorSettings()
        {
            return new CharacterGroundMotorSettings(
                walkSpeed, runSpeed, groundAcceleration, groundDeceleration);
        }

        public CharacterAirControlSettings CreateAirControlSettings()
        {
            return new CharacterAirControlSettings(airAcceleration, maxAirSpeed);
        }

        public CharacterGravitySettings CreateGravitySettings()
        {
            return new CharacterGravitySettings(
                riseGravity, fallGravity, maxFallSpeed);
        }

        public CharacterJumpSettings CreateJumpSettings()
        {
            return new CharacterJumpSettings(
                jumpVelocity, coyoteTime, jumpBufferTime, releaseCutMultiplier);
        }

        /// <summary>
        /// RMAP05 착지 결과의 단일 소유자. 거리는 반올림하지 않고 연속 구간으로
        /// 비교한다. 체력 적용은 live fall state가 기존 Survival 계약으로 위임한다.
        /// </summary>
        public CharacterLiveFallLandingResult EvaluateFallLanding(float fallDistance)
        {
            float distance = Mathf.Max(0f, fallDistance);
            if (distance >= fallFatalThreshold)
            {
                return new CharacterLiveFallLandingResult(0, false, true);
            }

            if (distance >= 25f)
            {
                return new CharacterLiveFallLandingResult(4, true, false);
            }

            if (distance >= 20f)
            {
                return new CharacterLiveFallLandingResult(3, true, false);
            }

            if (distance >= 15f)
            {
                return new CharacterLiveFallLandingResult(2, true, false);
            }

            if (distance >= 10f)
            {
                return new CharacterLiveFallLandingResult(1, true, false);
            }

            return distance >= fallStunThreshold
                ? new CharacterLiveFallLandingResult(0, true, false)
                : new CharacterLiveFallLandingResult(0, false, false);
        }

        /// <summary>RMAP05 fixture가 RMAP02 이동 수치와 분리해 설정하는 낙하 표.</summary>
        public void ConfigureRmap05Fall()
        {
            fallMaxHealth = 5;
            fallStunDuration = 0.5f;
            fallStunThreshold = 6f;
            fallFatalThreshold = 30f;
        }

        /// <summary>RMAP02 fixture의 Default 레이어와 P01~P03 기준값을 명시한다.</summary>
        public void ConfigureRmap02(int solidLayerMask)
        {
            solidLayers = solidLayerMask;
            alwaysRun = true;
            walkSpeed = 2.8f;
            runSpeed = 5.5f;
            groundAcceleration = 30f;
            groundDeceleration = 40f;
            airAcceleration = 40f;
            maxAirSpeed = 5.5f;
            riseGravity = 20f;
            fallGravity = 26f;
            maxFallSpeed = 20f;
            jumpVelocity = 7.2f;
            coyoteTime = 0.08f;
            jumpBufferTime = 0.10f;
            releaseCutMultiplier = 0.42f;
            grabProbeDistance = 0.35f;
            grabVerticalWindow = 0.45f;
            grabSideOffset = 0.21f;
            grabHangOffset = 0.62f;
            grabReentryDelay = 0.12f;
            grabMaxUpwardVelocity = 0.01f;
            climbSpeed = 4f;
            climbSlowSpeed = 2f;
            climbDownSpeed = 5f;
            climbReentryDelay = 0.12f;
            oneWayDropThroughDuration = 0.18f;
            ConfigureRmap05Fall();
        }
    }

    /// <summary>낙하 거리 표 평가 결과. Health/physics side effect는 포함하지 않는다.</summary>
    public readonly struct CharacterLiveFallLandingResult
    {
        public CharacterLiveFallLandingResult(int damage, bool appliesStun, bool isFatal)
        {
            Damage = damage;
            AppliesStun = appliesStun;
            IsFatal = isFatal;
        }

        public int Damage { get; }
        public bool AppliesStun { get; }
        public bool IsFatal { get; }
    }
}
