using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Character.MapIntegration;
using StarNight.Character.Traversal;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.Traversal
{
    public sealed class CharacterRopeClimbTests
    {
        private const int ActorId = 777;
        private const float Tolerance = 1e-4f;

        /// <summary>(10,5)~(10,10) 6셀 로프 — 세계 Y 범위 [5.5, 10.5].</summary>
        private static CharacterRopeExtent SixCellExtent()
        {
            var segments = new List<CharacterRopeSegmentRequest>();
            for (int offset = 0; offset < 6; offset++)
            {
                WorldTileCoord cell;
                Assert.That(WorldCoordinateUtility.TryCreateWorldTile(
                    10, 5 + offset, out cell), Is.True);
                segments.Add(new CharacterRopeSegmentRequest(1, cell, offset));
            }

            CharacterRopeExtent extent;
            Assert.That(
                CharacterRopeExtent.TryCreateFromSegments(segments, out extent),
                Is.True);
            Assert.That(extent.BottomWorldY, Is.EqualTo(5.5f).Within(Tolerance));
            Assert.That(extent.TopWorldY, Is.EqualTo(10.5f).Within(Tolerance));
            return extent;
        }

        private static CharacterRopeClimbInput Input(
            bool overlap,
            bool intent,
            float axis,
            float currentY,
            CharacterRopeExtent extent)
        {
            return new CharacterRopeClimbInput(
                ActorId, overlap, intent, axis, currentY, extent);
        }

        [Test]
        public void RopeClimb_OverlapAndClimbIntentCreatesMotorRequest()
        {
            var settings = CharacterRopeSettings.Default; // 등반 속도 4u/s
            var extent = SixCellExtent();
            CharacterRopeClimbMotorRequest request;

            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: 1f, currentY: 7f, extent),
                in settings, 0.1f, out request), Is.True);

            Assert.That(request.ActorId, Is.EqualTo(ActorId));
            Assert.That(request.VerticalVelocity, Is.EqualTo(4f).Within(Tolerance));
            Assert.That(request.TargetWorldY, Is.EqualTo(7.4f).Within(Tolerance));
        }

        [Test]
        public void RopeClimb_NoOverlapOrNoIntentCreatesNoMotorRequest()
        {
            var settings = CharacterRopeSettings.Default;
            var extent = SixCellExtent();
            CharacterRopeClimbMotorRequest request;

            // 로프 겹침 없음 — 의도가 있어도 요청 없음.
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: false, intent: true, axis: 1f, currentY: 7f, extent),
                in settings, 0.1f, out request), Is.False);

            // 등반 의도 없음 — 겹쳐 있어도 요청 없음.
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: false, axis: 1f, currentY: 7f, extent),
                in settings, 0.1f, out request), Is.False);
        }

        [Test]
        public void RopeClimb_UpDownInputProducesVerticalVelocity()
        {
            var settings = CharacterRopeSettings.Default;
            var extent = SixCellExtent();
            CharacterRopeClimbMotorRequest request;

            // 위 입력 → +등반 속도.
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: 1f, currentY: 7f, extent),
                in settings, 0.1f, out request), Is.True);
            Assert.That(request.VerticalVelocity, Is.EqualTo(4f).Within(Tolerance));

            // 아래 입력 → -등반 속도.
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: -1f, currentY: 7f, extent),
                in settings, 0.1f, out request), Is.True);
            Assert.That(request.VerticalVelocity, Is.EqualTo(-4f).Within(Tolerance));

            // 수직 입력 없음 → 속도 0, 제자리 유지(고정 규칙의 hold).
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: 0f, currentY: 7f, extent),
                in settings, 0.1f, out request), Is.True);
            Assert.That(request.VerticalVelocity, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(request.TargetWorldY, Is.EqualTo(7f).Within(Tolerance));

            // 과대 축 입력은 [-1,1]로 clamp — 속도는 등반 속도를 넘지 않는다.
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: 3f, currentY: 7f, extent),
                in settings, 0.1f, out request), Is.True);
            Assert.That(request.VerticalVelocity, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void RopeClimb_TopAndBottomBoundsClampTraversal()
        {
            var settings = CharacterRopeSettings.Default;
            var extent = SixCellExtent();
            CharacterRopeClimbMotorRequest request;

            // 상단 근처에서 위로 — 로프 위 끝(10.5)에서 clamp.
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: 1f, currentY: 10.4f, extent),
                in settings, 0.1f, out request), Is.True);
            Assert.That(request.TargetWorldY, Is.EqualTo(10.5f).Within(Tolerance));

            // 이미 위 끝 — 더 올라가지 않는다.
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: 1f, currentY: 10.5f, extent),
                in settings, 1.0f, out request), Is.True);
            Assert.That(request.TargetWorldY, Is.EqualTo(10.5f).Within(Tolerance));

            // 하단 근처에서 아래로 — 로프 아래 끝(5.5)에서 clamp.
            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: -1f, currentY: 5.6f, extent),
                in settings, 0.1f, out request), Is.True);
            Assert.That(request.TargetWorldY, Is.EqualTo(5.5f).Within(Tolerance));

            // 월드 상단 경계 로프: y=413 시작 3셀(413~415) → 위 끝 415.5는
            // 월드 높이 416 안이며, 그 위로는 clamp가 막는다.
            var nearTopSegments = new List<CharacterRopeSegmentRequest>();
            for (int offset = 0; offset < 3; offset++)
            {
                WorldTileCoord cell;
                Assert.That(WorldCoordinateUtility.TryCreateWorldTile(
                    10, 413 + offset, out cell), Is.True);
                nearTopSegments.Add(new CharacterRopeSegmentRequest(2, cell, offset));
            }

            CharacterRopeExtent nearTopExtent;
            Assert.That(CharacterRopeExtent.TryCreateFromSegments(
                nearTopSegments, out nearTopExtent), Is.True);

            Assert.That(CharacterRopeClimbPolicy.TryCreateClimbRequest(
                Input(overlap: true, intent: true, axis: 1f, currentY: 415.4f,
                    nearTopExtent),
                in settings, 5.0f, out request), Is.True);
            Assert.That(request.TargetWorldY, Is.EqualTo(415.5f).Within(Tolerance));
        }
    }
}
