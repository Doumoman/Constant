using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Tests.MovementCourses
{
    public sealed class TwoCellHeightCourseTests
    {
        private static CharacterMovementCourseSimulator CreateFlatFloorCourse()
        {
            var simulator = CharacterMovementCourseSimulator.CreateDefault();
            simulator.AddFloor(-10f, 10f, 0f);
            return simulator;
        }

        private static CharacterMovementCourseResult RunSingleJump(
            CharacterMovementCourseSimulator simulator)
        {
            return simulator.Simulate(
                new Vector2(0f, 0f),
                240,
                context => new CharacterMovementCourseSimulator.CourseTickInput(
                    0f,
                    false,
                    context.Tick == 5,
                    true));
        }

        [Test]
        public void TwoCellHeightCourse_UsesOneWorldUnitCellsAndLockedCapsule()
        {
            // 코스 검증 규약: 1 logical cell = 1 world unit (fixture 상수, MAP 소스 아님).
            Assert.That(CharacterMovementCourseConstants.WorldUnitsPerCell, Is.EqualTo(1f));
            Assert.That(CharacterMovementCourseConstants.TwoCellHeight, Is.EqualTo(2f));

            // collider baseline은 runtime CharacterCapsuleGeometry.Default에서 읽는다.
            var simulator = CreateFlatFloorCourse();

            Assert.That(simulator.Capsule.Width, Is.EqualTo(0.72f));
            Assert.That(simulator.Capsule.Height, Is.EqualTo(0.90f));
        }

        [Test]
        public void TwoCellHeightCourse_BasicJumpReachesTwoCellPlatformHeight()
        {
            var simulator = CreateFlatFloorCourse();
            var result = RunSingleJump(simulator);

            // 기본 Jump 1회로 collider bottom이 시작 바닥 기준 +2.0 world unit 이상 도달.
            Assert.That(result.JumpStartsExecuted, Is.EqualTo(1));
            Assert.That(result.PeakBottomY,
                Is.GreaterThanOrEqualTo(CharacterMovementCourseConstants.TwoCellHeight));

            // 코스 종료 시 착지 상태로 복귀한다.
            Assert.That(result.FinalGrounded, Is.True);
            Assert.That(result.FinalBottomY, Is.EqualTo(0f).Within(
                CharacterMovementCourseConstants.PositionTolerance));
        }

        [Test]
        public void TwoCellHeightCourse_UsesSingleJumpInputOnly()
        {
            var simulator = CreateFlatFloorCourse();
            var result = RunSingleJump(simulator);

            // Jump 입력 1회, 점프 시작 1회 — 2셀 높이는 단일 점프의 결과다.
            Assert.That(result.JumpInputsUsed, Is.EqualTo(1));
            Assert.That(result.JumpStartsExecuted, Is.EqualTo(1));
        }

        [Test]
        public void TwoCellHeightCourse_StillReachesTwoCellsAfterCoyoteRepair()
        {
            // CHAR02_03 교정은 공중 수평 상한만 변경한다 — 수직 점프 도달은 무영향이어야 한다.
            var simulator = CreateFlatFloorCourse();
            var result = RunSingleJump(simulator);

            Assert.That(result.PeakBottomY,
                Is.GreaterThanOrEqualTo(CharacterMovementCourseConstants.TwoCellHeight));
            Assert.That(result.JumpStartsExecuted, Is.EqualTo(1));
            Assert.That(result.FinalGrounded, Is.True);
        }

        [Test]
        public void TwoCellHeightCourse_DoesNotRequireSceneOrTilemap()
        {
            // 시뮬레이터는 UnityEngine.Object 파생 필드를 갖지 않는 순수 C#이다.
            var fields = typeof(CharacterMovementCourseSimulator)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                Assert.That(
                    typeof(Object).IsAssignableFrom(field.FieldType), Is.False,
                    "시뮬레이터가 scene object에 의존한다: " + field.Name);
            }

            // 런타임 어셈블리는 Tilemap 모듈을 참조하지 않는다(CHAR01 경계 유지).
            var referenced = typeof(CharacterGroundProbe).Assembly
                .GetReferencedAssemblies()
                .Select(name => name.Name)
                .ToArray();

            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));
        }
    }
}
