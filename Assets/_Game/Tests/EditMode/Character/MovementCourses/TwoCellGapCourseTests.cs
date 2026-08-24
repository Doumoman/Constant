using System.Linq;
using NUnit.Framework;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Tests.MovementCourses
{
    public sealed class TwoCellGapCourseTests
    {
        private const float GapStartX = 0f;
        private const float GapEndX = 2f;
        private const float LandingTargetX = 2.6f;

        private static CharacterMovementCourseSimulator CreateGapCourse(
            CharacterJumpSettings jumpSettings)
        {
            var simulator = new CharacterMovementCourseSimulator(
                CharacterGroundMotorSettings.Default,
                CharacterAirControlSettings.Default,
                CharacterGravitySettings.Default,
                jumpSettings);

            // 동일 높이 시작/도착 플랫폼 사이 정확히 2.0 world unit 빈 틈.
            simulator.AddFloor(-8f, GapStartX, 0f);
            simulator.AddFloor(GapEndX, 10f, 0f);
            simulator.SetWatchRange(GapStartX, GapEndX);
            simulator.StopWhenGroundedAtOrPastX(LandingTargetX);
            return simulator;
        }

        private static CharacterMovementCourseResult RunGapCourse(
            CharacterMovementCourseSimulator simulator)
        {
            bool jumpQueued = false;

            return simulator.Simulate(
                new Vector2(-4f, 0f),
                600,
                context =>
                {
                    // 달리기로 조주하다가 이륙 지점 직전에서 단일 점프.
                    bool pressJump = false;
                    if (!jumpQueued && context.Grounded && context.BottomCenter.x >= -0.35f)
                    {
                        pressJump = true;
                        jumpQueued = true;
                    }

                    return new CharacterMovementCourseSimulator.CourseTickInput(
                        1f, true, pressJump, true);
                });
        }

        [Test]
        public void TwoCellGapCourse_RunSpeedClearsSameLevelTwoCellGap()
        {
            var simulator = CreateGapCourse(CharacterJumpSettings.Default);
            var result = RunGapCourse(simulator);

            // 도착 플랫폼 높이 이상을 유지하며 opposite edge를 통과하고 착지했다.
            Assert.That(result.FinalGrounded, Is.True, "도착 플랫폼 착지 실패");
            Assert.That(result.FinalX, Is.GreaterThanOrEqualTo(LandingTargetX));
            Assert.That(result.MinBottomOverWatchRange, Is.GreaterThanOrEqualTo(
                -CharacterMovementCourseConstants.PositionTolerance));
            Assert.That(result.JumpStartsExecuted, Is.EqualTo(1));
        }

        [Test]
        public void TwoCellGapCourse_RecordsDeterministicFrameTolerance()
        {
            // 같은 코스를 두 번 실행하면 고정 틱 기준으로 완전히 동일한 결과가 나온다.
            var first = RunGapCourse(CreateGapCourse(CharacterJumpSettings.Default));
            var second = RunGapCourse(CreateGapCourse(CharacterJumpSettings.Default));

            Assert.That(second.FinalX, Is.EqualTo(first.FinalX));
            Assert.That(second.PeakBottomY, Is.EqualTo(first.PeakBottomY));
            Assert.That(second.MinBottomOverWatchRange, Is.EqualTo(first.MinBottomOverWatchRange));
            Assert.That(second.TicksSimulated, Is.EqualTo(first.TicksSimulated));

            // fixture 공통 허용 오차 규약(±0.05 world unit)이 기록돼 있다.
            Assert.That(CharacterMovementCourseConstants.PositionTolerance, Is.EqualTo(0.05f));
        }

        [Test]
        public void TwoCellGapCourse_UsesCharacterMovementCoreNotHardcodedTrajectory()
        {
            // 궤적이 하드코딩이 아니라면 코어 튜닝을 약화했을 때 결과가 달라져야 한다.
            var weakJump = new CharacterJumpSettings(2.0f, 0.10d, 0.12d, 0.5f);
            var weakResult = RunGapCourse(CreateGapCourse(weakJump));

            bool weakCleared = weakResult.FinalGrounded
                && weakResult.FinalX >= LandingTargetX
                && weakResult.MinBottomOverWatchRange >=
                    -CharacterMovementCourseConstants.PositionTolerance;

            Assert.That(weakCleared, Is.False,
                "약화된 점프 코어로도 통과하면 궤적이 코어 물리에서 유도되지 않은 것이다");

            var defaultResult = RunGapCourse(CreateGapCourse(CharacterJumpSettings.Default));

            Assert.That(defaultResult.FinalGrounded, Is.True);
            Assert.That(defaultResult.PeakBottomY, Is.GreaterThan(weakResult.PeakBottomY));
        }

        [Test]
        public void TwoCellGapCourse_StillPassesAfterCoyoteRepair()
        {
            // CHAR02_03 교정(maxAirSpeed 3.75→3.1) 이후에도 2셀 틈 통과가 유지된다.
            Assert.That(
                CharacterAirControlSettings.Default.MaxAirSpeed, Is.EqualTo(3.1f),
                "교정 튜닝이 적용되지 않음");

            var simulator = CreateGapCourse(CharacterJumpSettings.Default);
            var result = RunGapCourse(simulator);

            Assert.That(result.FinalGrounded, Is.True);
            Assert.That(result.FinalX, Is.GreaterThanOrEqualTo(LandingTargetX));
            Assert.That(result.MinBottomOverWatchRange, Is.GreaterThanOrEqualTo(
                -CharacterMovementCourseConstants.PositionTolerance));
            Assert.That(result.JumpStartsExecuted, Is.EqualTo(1));
        }

        [Test]
        public void TwoCellGapCourse_DoesNotValidateThreeCellFailureYet()
        {
            // 3셀 실패 검증은 CHAR02_02 소관 — 이 어셈블리에 ThreeCell 코스 타입이 없다.
            var typeNames = typeof(TwoCellGapCourseTests).Assembly
                .GetTypes()
                .Select(type => type.Name)
                .ToArray();

            foreach (var typeName in typeNames)
            {
                Assert.That(typeName, Does.Not.Contain("ThreeCell"),
                    "CHAR02_02 소관 타입이 선행 구현됨: " + typeName);
            }

            // 이번 코스의 틈 폭은 정확히 2셀이다.
            Assert.That(GapEndX - GapStartX,
                Is.EqualTo(CharacterMovementCourseConstants.TwoCellGapWidth));
        }
    }
}
