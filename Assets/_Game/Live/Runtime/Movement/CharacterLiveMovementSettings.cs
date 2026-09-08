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
        }
    }
}
