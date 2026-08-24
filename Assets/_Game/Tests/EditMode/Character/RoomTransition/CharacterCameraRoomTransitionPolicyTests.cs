using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.MapIntegration;
using StarNight.Character.RoomTransition;
using StarNight.Character.State;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.RoomTransition
{
    public sealed class CharacterCameraRoomTransitionPolicyTests
    {
        private sealed class FakeReadinessSource : ICharacterRoomReadinessSource
        {
            private readonly Dictionary<CharacterRoomId, bool> rooms =
                new Dictionary<CharacterRoomId, bool>();

            public void SetRoom(CharacterRoomId room, bool isReady)
            {
                rooms[room] = isReady;
            }

            public bool TryGetRoomReadiness(CharacterRoomId room, out bool isReady)
            {
                return rooms.TryGetValue(room, out isReady);
            }
        }

        // 방 A = 마이크로청크 (0,0): 타일 x 0..11, 방 B = (1,0): 타일 x 12..23. 공유 경계 x=12.
        private static readonly WorldTileCoord AnchorInRoomA = new WorldTileCoord(11, 4);
        private static readonly CharacterRoomId RoomA =
            CharacterRoomId.FromWorldTile(new WorldTileCoord(5, 4));
        private static readonly CharacterRoomId RoomB =
            CharacterRoomId.FromWorldTile(new WorldTileCoord(12, 4));

        private static CharacterCameraRoomTransitionPolicy CreatePolicy(
            FakeReadinessSource readiness)
        {
            var policy = new CharacterCameraRoomTransitionPolicy(
                new CharacterRoomBoundaryGate(readiness),
                CharacterRoomTransitionSettings.Default);
            policy.SetActiveRoom(AnchorInRoomA);
            return policy;
        }

        private static FakeReadinessSource ReadinessWithRoomB(bool isReady)
        {
            var readiness = new FakeReadinessSource();
            readiness.SetRoom(RoomB, isReady);
            return readiness;
        }

        [Test]
        public void CameraRoomTransition_PreparedBoundaryCrossingRequestsTargetRoom()
        {
            var policy = CreatePolicy(ReadinessWithRoomB(true));

            // margin(0.25) 이상 침투 + 연속 2샘플 후 전환 요청.
            var first = policy.Evaluate(new Vector2(12.3f, 4.5f));

            Assert.That(first.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.PendingStabilization));

            var second = policy.Evaluate(new Vector2(12.35f, 4.5f));

            Assert.That(second.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.TransitionRequested));
            Assert.That(second.HasRequest, Is.True);
            Assert.That(second.Request.SourceRoom, Is.EqualTo(RoomA));
            Assert.That(second.Request.TargetRoom, Is.EqualTo(RoomB));
            Assert.That(policy.ActiveRoom, Is.EqualTo(RoomB));

            // 전환 후 같은 방 내부는 전환 없음.
            Assert.That(policy.Evaluate(new Vector2(13f, 4.5f)).Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.NoTransition));
        }

        [Test]
        public void CameraRoomTransition_UnpreparedDestinationIsBlocked()
        {
            var policy = CreatePolicy(ReadinessWithRoomB(false));

            for (var attempt = 0; attempt < 4; attempt++)
            {
                var result = policy.Evaluate(new Vector2(13f, 4.5f));

                Assert.That(result.Decision,
                    Is.EqualTo(CharacterRoomTransitionDecision.BlockedUnpreparedRoom));
                Assert.That(result.HasRequest, Is.False);
            }

            Assert.That(policy.ActiveRoom, Is.EqualTo(RoomA));
        }

        [Test]
        public void CameraRoomTransition_MissingDestinationIsBlocked()
        {
            // 방 B 미등록.
            var policy = CreatePolicy(new FakeReadinessSource());

            var result = policy.Evaluate(new Vector2(13f, 4.5f));

            Assert.That(result.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.BlockedMissingRoom));
            Assert.That(result.HasRequest, Is.False);
            Assert.That(policy.ActiveRoom, Is.EqualTo(RoomA));
        }

        [Test]
        public void CameraRoomTransition_KeepsInputSnapshot()
        {
            var policy = CreatePolicy(ReadinessWithRoomB(true));

            // 입력 스냅샷·상태를 곁에 두고 평가 — 값이 그대로이고 잠금 사유도 없다.
            var snapshot = new CharacterInputSnapshot(
                0.8f,
                true,
                CharacterButtonSnapshot.Pressed(1L),
                CharacterButtonSnapshot.Idle(1L),
                CharacterButtonSnapshot.Idle(1L),
                CharacterButtonSnapshot.Pressed(1L));
            var playerState = new CharacterPlayerState();

            policy.Evaluate(new Vector2(12.3f, 4.5f));
            policy.Evaluate(new Vector2(12.35f, 4.5f));

            Assert.That(snapshot.Horizontal, Is.EqualTo(0.8f));
            Assert.That(snapshot.DownHeld, Is.True);
            Assert.That(snapshot.Jump.PressedThisFrame, Is.True);
            Assert.That(snapshot.Rope.PressedThisFrame, Is.True);
            Assert.That(playerState.Locks.Count, Is.EqualTo(0));
            Assert.That(playerState.CanAcceptInput, Is.True);

            // API 형태 증명: 정책 공개 메서드는 입력/잠금 타입을 받지 않는다.
            var parameterTypes = typeof(CharacterCameraRoomTransitionPolicy)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(method => method.GetParameters())
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.That(parameterTypes, Has.No.Member(typeof(CharacterInputSnapshot)));
            Assert.That(parameterTypes, Has.No.Member(typeof(CharacterInputBuffer)));
            Assert.That(parameterTypes, Has.No.Member(typeof(CharacterInputLockSet)));
        }

        [Test]
        public void CameraRoomTransition_KeepsVelocityForAllowedAndBlockedDecisions()
        {
            var velocity = new Vector2(3.1f, -7.7f);

            // 허용(요청) 경로.
            var allowedPolicy = CreatePolicy(ReadinessWithRoomB(true));
            allowedPolicy.Evaluate(new Vector2(12.3f, 4.5f));
            var requested = allowedPolicy.Evaluate(new Vector2(12.35f, 4.5f));

            Assert.That(requested.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.TransitionRequested));
            Assert.That(velocity, Is.EqualTo(new Vector2(3.1f, -7.7f)));

            // 차단 경로.
            var blockedPolicy = CreatePolicy(ReadinessWithRoomB(false));
            var blocked = blockedPolicy.Evaluate(new Vector2(13f, 4.5f));

            Assert.That(blocked.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.BlockedUnpreparedRoom));
            Assert.That(velocity, Is.EqualTo(new Vector2(3.1f, -7.7f)));

            // 결과 타입에도 속도 필드가 없다(변조 불가 형태).
            var resultProperties = typeof(CharacterRoomTransitionResult)
                .GetProperties()
                .Select(property => property.PropertyType)
                .ToArray();

            Assert.That(resultProperties, Has.No.Member(typeof(Vector2)));
        }

        [Test]
        public void CameraRoomTransition_HysteresisPreventsBoundaryPingPong()
        {
            var policy = CreatePolicy(ReadinessWithRoomB(true));

            // margin(0.25) 미만 침투로 경계를 빠르게 왕복 — 전환이 발행되지 않는다.
            for (var cycle = 0; cycle < 8; cycle++)
            {
                var inside = policy.Evaluate(new Vector2(12.1f, 4.5f));

                Assert.That(inside.Decision,
                    Is.EqualTo(CharacterRoomTransitionDecision.PendingStabilization));
                Assert.That(inside.HasRequest, Is.False);

                var back = policy.Evaluate(new Vector2(11.9f, 4.5f));

                Assert.That(back.Decision,
                    Is.EqualTo(CharacterRoomTransitionDecision.NoTransition));
            }

            Assert.That(policy.ActiveRoom, Is.EqualTo(RoomA));

            // 연속성 요구: 깊은 침투 1샘플 후 복귀하면 카운트가 리셋된다.
            policy.Evaluate(new Vector2(12.3f, 4.5f));
            policy.Evaluate(new Vector2(11.9f, 4.5f));
            var afterReset = policy.Evaluate(new Vector2(12.3f, 4.5f));

            Assert.That(afterReset.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.PendingStabilization));
            Assert.That(policy.ActiveRoom, Is.EqualTo(RoomA));
        }

        [Test]
        public void CameraRoomTransition_HysteresisAllowsReturnBeyondMargin()
        {
            var readiness = ReadinessWithRoomB(true);
            readiness.SetRoom(RoomA, true);
            var policy = CreatePolicy(readiness);

            // A → B 전환.
            policy.Evaluate(new Vector2(12.3f, 4.5f));
            var forward = policy.Evaluate(new Vector2(12.35f, 4.5f));

            Assert.That(forward.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.TransitionRequested));
            Assert.That(policy.ActiveRoom, Is.EqualTo(RoomB));

            // margin 너머로 되돌아가면 역방향 전환이 요청된다(B → A).
            policy.Evaluate(new Vector2(11.7f, 4.5f));
            var reverse = policy.Evaluate(new Vector2(11.65f, 4.5f));

            Assert.That(reverse.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.TransitionRequested));
            Assert.That(reverse.Request.SourceRoom, Is.EqualTo(RoomB));
            Assert.That(reverse.Request.TargetRoom, Is.EqualTo(RoomA));
            Assert.That(policy.ActiveRoom, Is.EqualTo(RoomA));
        }

        [Test]
        public void CameraRoomTransition_HighSpeedCrossingProducesSingleTransition()
        {
            var policy = CreatePolicy(ReadinessWithRoomB(true));
            var requestCount = 0;

            // 고속 진입: 한 평가 스텝에 경계를 깊이 지나쳐 들어온다(스윕 미지원 —
            // 이전/현재 샘플 평가 방식이므로 최종 방 하나로만 수렴해야 한다).
            var samples = new[]
            {
                new Vector2(11.0f, 4.5f),
                new Vector2(14.5f, 4.5f),
                new Vector2(14.6f, 4.5f),
                new Vector2(15.2f, 4.5f),
                new Vector2(16.0f, 4.5f)
            };

            foreach (var sample in samples)
            {
                var result = policy.Evaluate(sample);

                if (result.HasRequest)
                {
                    requestCount++;
                    Assert.That(result.Request.TargetRoom, Is.EqualTo(RoomB));
                }
            }

            // 경계 1회 통과 = 전환 요청 정확히 1회(핑퐁·연사 없음).
            Assert.That(requestCount, Is.EqualTo(1));
            Assert.That(policy.ActiveRoom, Is.EqualTo(RoomB));

            // 고속으로 미준비 방에 진입하면 차단된다.
            var blockedPolicy = CreatePolicy(ReadinessWithRoomB(false));
            var blocked = blockedPolicy.Evaluate(new Vector2(15f, 4.5f));

            Assert.That(blocked.Decision,
                Is.EqualTo(CharacterRoomTransitionDecision.BlockedUnpreparedRoom));
        }

        [Test]
        public void CameraRoomTransition_AirborneCrossingUsesSamePolicyAsGrounded()
        {
            // 정책 공개 메서드에는 grounded/locomotion 매개변수가 아예 없다 —
            // 지상/공중 경계 진입이 같은 코드 경로임이 API 형태로 보장된다.
            var parameters = typeof(CharacterCameraRoomTransitionPolicy)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(method => method.GetParameters())
                .ToArray();

            foreach (var parameter in parameters)
            {
                Assert.That(parameter.ParameterType, Is.Not.EqualTo(typeof(bool)),
                    "grounded 분기 매개변수가 존재한다: " + parameter.Name);
                Assert.That(parameter.ParameterType,
                    Is.Not.EqualTo(typeof(CharacterLocomotionState)));
            }

            // 동일 위치 시퀀스는 (지상이든 공중이든) 동일 판정을 낸다.
            var first = CreatePolicy(ReadinessWithRoomB(true));
            var second = CreatePolicy(ReadinessWithRoomB(true));
            var sequence = new[]
            {
                new Vector2(12.3f, 4.5f),
                new Vector2(12.4f, 6.9f),
                new Vector2(13.0f, 2.1f)
            };

            foreach (var position in sequence)
            {
                var groundedLike = first.Evaluate(position);
                var airborneLike = second.Evaluate(position);

                Assert.That(airborneLike.Decision, Is.EqualTo(groundedLike.Decision));
            }
        }

        [Test]
        public void CameraRoomTransition_DoesNotReferenceSceneCameraOrPresentationTypes()
        {
            var runtimeAssembly = typeof(CharacterCameraRoomTransitionPolicy).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            Assert.That(referenced, Does.Not.Contain("Unity.Cinemachine"));
            Assert.That(referenced, Does.Not.Contain("Cinemachine"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AudioModule"));

            // RoomTransition 표면에 카메라/연출 타입이 등장하지 않는다.
            var transitionTypes = runtimeAssembly.GetTypes()
                .Where(type => type.Namespace == "StarNight.Character.RoomTransition")
                .ToArray();

            Assert.That(transitionTypes, Is.Not.Empty);

            var forbidden = new[] { "Camera", "Animator", "Renderer", "Audio", "Cinemachine" };

            foreach (var type in transitionTypes)
            {
                var memberTypeNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .SelectMany(member =>
                    {
                        switch (member)
                        {
                            case MethodInfo method:
                                return method.GetParameters()
                                    .Select(parameter => parameter.ParameterType.Name)
                                    .Concat(new[] { method.ReturnType.Name });
                            case PropertyInfo property:
                                return new[] { property.PropertyType.Name };
                            case FieldInfo field:
                                return new[] { field.FieldType.Name };
                            default:
                                return Enumerable.Empty<string>();
                        }
                    });

                foreach (var typeName in memberTypeNames)
                {
                    foreach (var keyword in forbidden)
                    {
                        Assert.That(typeName, Does.Not.Contain(keyword),
                            type.Name + " 표면에 연출 타입 노출: " + typeName);
                    }
                }
            }
        }
    }
}
