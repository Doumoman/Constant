using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Character.Input;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.Equipment
{
    public sealed class CharacterBombGuardTests
    {
        private sealed class FakeMapWorldQuery : ICharacterMapWorldQuery
        {
            private readonly Dictionary<long, CharacterMapCellState> cells =
                new Dictionary<long, CharacterMapCellState>();

            public void SetCell(int x, int y, CharacterMapCellState state)
            {
                cells[Key(x, y)] = state;
            }

            public bool TryGetCellState(WorldTileCoord tile, out CharacterMapCellState state)
            {
                return cells.TryGetValue(Key(tile.X, tile.Y), out state);
            }

            private static long Key(int x, int y)
            {
                return ((long)y << 32) | (uint)x;
            }
        }

        private static Type[] EquipmentRuntimeTypes()
        {
            return typeof(CharacterBombPlacementPolicy).Assembly
                .GetTypes()
                .Where(type => type.Namespace == "StarNight.Character.Equipment")
                .ToArray();
        }

        [Test]
        public void Explosion_DamageCandidatesAndTerrainRequestsDoNotApplySideEffects()
        {
            // (1) 행동 검증: 정책 실행 후에도 맵 셀 상태는 그대로다 —
            //     파괴 적용은 요청 소비자(후속 단계) 소관이다.
            var query = new FakeMapWorldQuery();
            var breakable = new CharacterMapCellState(true, false, false, false, true);
            query.SetCell(9, 5, breakable);

            var explosion = new CharacterExplosionRequest(
                1, 777, new WorldTileCoord(10, 5), 1.5f, 2);
            var terrainRequests = CharacterExplosionTerrainPolicy
                .CreateTerrainMutationRequests(in explosion, query);

            Assert.That(terrainRequests.Count, Is.EqualTo(1));

            CharacterMapCellState after;
            Assert.That(query.TryGetCellState(new WorldTileCoord(9, 5), out after), Is.True);
            Assert.That(after.IsBreakable, Is.True, "요청 생성이 맵 상태를 바꾸면 안 된다");

            // 피해 후보 생성도 순수하다 — 같은 입력이면 같은 출력, 대상 스냅샷 불변.
            var targets = new List<CharacterExplosionTargetSnapshot>
            {
                new CharacterExplosionTargetSnapshot(21, false, new Vector2(11.0f, 5.5f))
            };
            var enemiesFirst = new List<CharacterEnemyExplosionDamageCandidate>();
            var enemiesSecond = new List<CharacterEnemyExplosionDamageCandidate>();
            var players = new List<CharacterPlayerExplosionDamageCandidate>();

            CharacterExplosionDamagePolicy.CreateDamageCandidates(
                in explosion, targets, enemiesFirst, players);
            CharacterExplosionDamagePolicy.CreateDamageCandidates(
                in explosion, targets, enemiesSecond, players);

            Assert.That(enemiesFirst.Count, Is.EqualTo(1));
            Assert.That(enemiesSecond.Count, Is.EqualTo(1));
            Assert.That(enemiesSecond[0].Amount, Is.EqualTo(enemiesFirst[0].Amount));
            Assert.That(targets[0].Position, Is.EqualTo(new Vector2(11.0f, 5.5f)));

            // (2) 표면 검증: Equipment 공개 표면에 적용형 명명이 없다 —
            //     HP/체력/사망/점수/넉백/인벤토리 변조는 이 과제 밖이다.
            var forbiddenNames = new[]
            {
                "Health", "Hp", "Death", "Kill", "Score",
                "Knockback", "Inventory", "Apply"
            };

            foreach (var type in EquipmentRuntimeTypes())
            {
                var publicNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.Static)
                    .Select(member => member.Name)
                    .Concat(new[] { type.Name });

                foreach (var name in publicNames)
                {
                    foreach (var keyword in forbiddenNames)
                    {
                        Assert.That(name, Does.Not.Contain(keyword),
                            type.Name + " 표면에 적용형 명명이 있다: " + name);
                    }
                }

                // (3) 값 객체 불변: 공개 setter·공개 가변 인스턴스 필드 없음(enum 제외).
                foreach (var property in type.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    Assert.That(property.GetSetMethod(), Is.Null,
                        type.Name + "." + property.Name + "에 공개 setter가 있다");
                }

                if (!type.IsEnum)
                {
                    foreach (var field in type.GetFields(
                        BindingFlags.Public | BindingFlags.Instance))
                    {
                        Assert.That(field.IsInitOnly, Is.True,
                            type.Name + "." + field.Name + "은 가변 공개 필드다");
                    }
                }
            }
        }

        [Test]
        public void BombRuntime_DoesNotUseAnimatorPhysicsTilemapOrForbiddenActions()
        {
            var runtimeAssembly = typeof(CharacterBombPlacementPolicy).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            // Animator/Tilemap 직접 접근 금지(잠금 규칙).
            // Physics2DModule 참조는 CHAR01 승인 충돌 "질의" 어댑터
            // (UnityPhysics2DCharacterCollisionWorld) 소관이라 어셈블리 차원 부재를
            // 요구하지 않는다 — 폭탄 가드는 "물리 콜백 권위 없음"을 Equipment
            // 타입 범위에서 검증한다.
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));

            foreach (var type in EquipmentRuntimeTypes())
            {
                // Equipment 런타임은 MonoBehaviour/Component가 아니다 — 순수 값/정책.
                // Component가 아니면 Unity 물리 콜백은 애초에 호출되지 않는다.
                Assert.That(typeof(Component).IsAssignableFrom(type), Is.False,
                    type.Name + "은 Unity Component다");

                // 물리/충돌 콜백 시그니처가 폭탄 런타임에 존재하지 않는다.
                foreach (var method in type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance | BindingFlags.Static
                        | BindingFlags.DeclaredOnly))
                {
                    Assert.That(method.Name, Does.Not.StartWith("OnCollision"),
                        type.Name + "에 물리 콜백이 있다: " + method.Name);
                    Assert.That(method.Name, Does.Not.StartWith("OnTrigger"),
                        type.Name + "에 물리 콜백이 있다: " + method.Name);
                }

                // 표면 타입 이름에도 Animator/Tilemap/Rigidbody/Collider가 없다.
                var surfaceTypeNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.Static)
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

                foreach (var typeName in surfaceTypeNames)
                {
                    Assert.That(typeName, Does.Not.Contain("Animator"));
                    Assert.That(typeName, Does.Not.Contain("Tilemap"));
                    Assert.That(typeName, Does.Not.Contain("Rigidbody"));
                    Assert.That(typeName, Does.Not.Contain("Collider"));
                    Assert.That(typeName, Does.Not.Contain("RaycastHit"));
                }

                // 금지 이동/공격 개념이 폭탄 계약에 스며들지 않았다.
                foreach (var keyword in new[]
                    { "BasicAttack", "Melee", "Shoot", "Dash", "WallJump", "DoubleJump" })
                {
                    Assert.That(type.Name, Does.Not.Contain(keyword));
                }
            }

            // 논리 행동 ID는 잠금 5종 그대로다 — Bomb은 기존 슬롯을 쓴다.
            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));
        }
    }
}
