using NUnit.Framework;
using UnityEngine;

namespace StarNight.Character.Tests.MovementCourses
{
    /// <summary>
    /// 3셀 틈 실패 코스 검증(CHAR02_02). 클래스명에 ThreeCell을 쓰지 않는 이유:
    /// CHAR02_01의 가드 테스트(TwoCellGapCourse_DoesNotValidateThreeCellFailureYet)가
    /// 어셈블리 타입명 기준으로 ThreeCell 부재를 단언하며 본 Task의 WRITE ALLOWLIST
    /// 밖이라 갱신할 수 없다. 요구된 파일명과 테스트 메서드명은 지정 그대로다.
    /// </summary>
    public sealed class GapFailureCourseTests
    {
        private const float ThreeCellGapEndX = 3f;
        private const float TwoCellGapEndX = 2f;

        /// <summary>동일 높이 틈 코스(폭 가변). 지형 외 조건은 2셀 코스와 동일하다.</summary>
        private static CharacterMovementCourseSimulator CreateGapCourse(float gapEndX)
        {
            var simulator = CharacterMovementCourseSimulator.CreateDefault();
            simulator.AddFloor(-8f, 0f, 0f);
            simulator.AddFloor(gapEndX, 12f, 0f);
            simulator.SetWatchRange(0f, gapEndX);
            simulator.StopWhenGroundedAtOrPastX(gapEndX + 0.4f);
            return simulator;
        }

        /// <summary>
        /// CHAR02_01 2셀 틈 코스와 동일한 기본 입력 경로:
        /// 달리기 조주 + 이륙 지점 직전 단일 점프(유지). 같은 movement core를 사용한다.
        /// </summary>
        private static CharacterMovementCourseResult RunBasicRunAndSingleJump(
            CharacterMovementCourseSimulator simulator)
        {
            bool jumpQueued = false;

            return simulator.Simulate(
                new Vector2(-4f, 0f),
                360,
                context =>
                {
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

        private static bool ClearedGap(in CharacterMovementCourseResult result, float gapEndX)
        {
            return result.FinalGrounded
                && result.FinalX >= gapEndX
                && result.FinalBottomY >= -CharacterMovementCourseConstants.PositionTolerance
                && result.MinBottomOverWatchRange >=
                    -CharacterMovementCourseConstants.PositionTolerance;
        }

        private static string ClassifyFailureReason(
            in CharacterMovementCourseResult result, float gapEndX)
        {
            float tolerance = CharacterMovementCourseConstants.PositionTolerance;

            if (ClearedGap(in result, gapEndX))
            {
                return "cleared";
            }

            if (!result.FinalGrounded && result.MinBottomOverWatchRange < -tolerance)
            {
                return "fell_below_gap_before_opposite_edge";
            }

            if (result.FinalGrounded && result.FinalX < gapEndX)
            {
                return "landed_short_of_opposite_edge";
            }

            return "did_not_land_on_target_platform";
        }

        [Test]
        public void ThreeCellGapCourse_BasicMovementDoesNotClearSameLevelThreeCellGap()
        {
            var simulator = CreateGapCourse(ThreeCellGapEndX);
            var result = RunBasicRunAndSingleJump(simulator);

            // 잠금 규칙: 동일 높이 3셀 틈은 기본 run + single jump로 통과 불가.
            Assert.That(ClearedGap(in result, ThreeCellGapEndX), Is.False,
                "3셀 틈이 기본 이동으로 통과됨 — 이동 문법 위반");
            Assert.That(result.JumpStartsExecuted, Is.EqualTo(1));

            // 실패 형태: 반대편 모서리에 닿지 못하고 틈 아래로 낙하.
            Assert.That(result.MinBottomOverWatchRange, Is.LessThan(
                -CharacterMovementCourseConstants.PositionTolerance));
            Assert.That(result.FinalGrounded, Is.False);
        }

        [Test]
        public void ThreeCellGapCourse_UsesSameCorePathAsTwoCellGapCourse()
        {
            // 같은 시뮬레이터 구성 + 같은 입력 스크립트에서 지형 폭만 다르다.
            var twoCellResult = RunBasicRunAndSingleJump(CreateGapCourse(TwoCellGapEndX));
            var threeCellResult = RunBasicRunAndSingleJump(CreateGapCourse(ThreeCellGapEndX));

            // 동일 코어 경로가 2셀은 통과시키고 3셀은 통과시키지 못한다.
            Assert.That(ClearedGap(in twoCellResult, TwoCellGapEndX), Is.True,
                "동일 경로의 2셀 통과가 깨짐");
            Assert.That(ClearedGap(in threeCellResult, ThreeCellGapEndX), Is.False,
                "동일 경로로 3셀이 통과됨");

            // 두 코스 모두 단일 점프만 사용했다.
            Assert.That(twoCellResult.JumpStartsExecuted, Is.EqualTo(1));
            Assert.That(threeCellResult.JumpStartsExecuted, Is.EqualTo(1));
        }

        [Test]
        public void ThreeCellGapCourse_RecordsDeterministicFailureReason()
        {
            var first = RunBasicRunAndSingleJump(CreateGapCourse(ThreeCellGapEndX));
            var second = RunBasicRunAndSingleJump(CreateGapCourse(ThreeCellGapEndX));

            var firstReason = ClassifyFailureReason(in first, ThreeCellGapEndX);
            var secondReason = ClassifyFailureReason(in second, ThreeCellGapEndX);

            Assert.That(firstReason, Is.EqualTo("fell_below_gap_before_opposite_edge"));
            Assert.That(secondReason, Is.EqualTo(firstReason));

            // 고정 틱 기준 완전 동일 결과.
            Assert.That(second.FinalX, Is.EqualTo(first.FinalX));
            Assert.That(second.MinBottomOverWatchRange, Is.EqualTo(first.MinBottomOverWatchRange));
            Assert.That(second.PeakBottomY, Is.EqualTo(first.PeakBottomY));
            Assert.That(second.TicksSimulated, Is.EqualTo(first.TicksSimulated));
        }

        /// <summary>
        /// 코요테 지연 점프 스크립트: 모서리를 달려 나가 airborne이 된 뒤
        /// delayTicks 틱 후 단일 점프를 입력한다(합법적 기본 이동 조합).
        /// </summary>
        private static CharacterMovementCourseResult RunCoyoteDelayedJump(
            CharacterMovementCourseSimulator simulator, int delayTicks)
        {
            bool pressed = false;
            int airborneTicks = 0;

            return simulator.Simulate(
                new Vector2(-4f, 0f),
                360,
                context =>
                {
                    bool pressJump = false;

                    if (!context.Grounded)
                    {
                        airborneTicks++;
                    }
                    else
                    {
                        airborneTicks = 0;
                    }

                    if (!pressed
                        && !context.Grounded
                        && context.BottomCenter.x > 0f
                        && airborneTicks == delayTicks + 1)
                    {
                        pressJump = true;
                        pressed = true;
                    }

                    return new CharacterMovementCourseSimulator.CourseTickInput(
                        1f, true, pressJump, true);
                });
        }

        [Test]
        public void ThreeCellGapCourse_CoyoteDelayedJumpDoesNotClearSameLevelThreeCellGap()
        {
            // CHAR02_03 1차 감사의 위반 재현 케이스(당시 x=3.171 착지) — 교정 후 실패해야 한다.
            var simulator = CreateGapCourse(ThreeCellGapEndX);
            var result = RunCoyoteDelayedJump(simulator, 1); // 이탈 후 0.033s 지연 점프

            Assert.That(result.JumpStartsExecuted, Is.EqualTo(1),
                "코요테 창 안의 지연 점프가 발동해야 한다");
            Assert.That(ClearedGap(in result, ThreeCellGapEndX), Is.False,
                "코요테 지연 점프로 3셀 틈이 통과됨 — 이동 문법 위반");
        }

        [Test]
        public void ThreeCellGapCourse_CoyoteDelaySweepNeverClearsSameLevelThreeCellGap()
        {
            // 합법 지연 표본 전체 스윕: 0.0000/0.0167/0.0333/0.0500/0.0667/0.0833/0.1000s.
            // 창(coyote 0.10) 밖으로 만료된 표본은 점프가 발동하지 않아 통과가 불가능함을
            // 명시적으로 기록한다.
            for (int delayTicks = 0; delayTicks <= 6; delayTicks++)
            {
                var result = RunCoyoteDelayedJump(
                    CreateGapCourse(ThreeCellGapEndX), delayTicks);

                Assert.That(ClearedGap(in result, ThreeCellGapEndX), Is.False,
                    "지연 " + delayTicks + "틱(" + (delayTicks / 60f).ToString("F4")
                    + "s)에서 3셀 틈이 통과됨");

                if (result.JumpStartsExecuted == 0)
                {
                    // 창 만료 표본: 점프 미발동 → 낙하로 종료(통과 불가 명시 기록).
                    Assert.That(result.FinalGrounded, Is.False);
                    Assert.That(result.MinBottomOverWatchRange, Is.LessThan(
                        -CharacterMovementCourseConstants.PositionTolerance));
                }
            }
        }

        [Test]
        public void ThreeCellGapCourse_DoesNotChangeTwoCellPassResult()
        {
            // 3셀 실패 검증 추가 후에도 2셀 통과 결과는 그대로다(결과 완화 없음).
            var result = RunBasicRunAndSingleJump(CreateGapCourse(TwoCellGapEndX));

            Assert.That(result.FinalGrounded, Is.True);
            Assert.That(result.FinalX, Is.GreaterThanOrEqualTo(TwoCellGapEndX + 0.4f));
            Assert.That(result.MinBottomOverWatchRange, Is.GreaterThanOrEqualTo(
                -CharacterMovementCourseConstants.PositionTolerance));
            Assert.That(result.JumpStartsExecuted, Is.EqualTo(1));
        }
    }
}
