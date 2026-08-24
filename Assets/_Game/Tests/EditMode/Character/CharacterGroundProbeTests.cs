using System.Linq;
using NUnit.Framework;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Tests
{
    public sealed class CharacterGroundProbeTests
    {
        private sealed class FakeCollisionWorld : ICharacterCollisionWorld
        {
            public CharacterCollisionHit NextHit = CharacterCollisionHit.None;
            public CharacterCapsuleGeometry LastCapsule;
            public Vector2 LastOrigin;
            public Vector2 LastDirection;
            public float LastDistance;

            public CharacterCollisionHit CapsuleCast(
                Vector2 origin,
                CharacterCapsuleGeometry capsule,
                Vector2 direction,
                float distance)
            {
                LastOrigin = origin;
                LastCapsule = capsule;
                LastDirection = direction;
                LastDistance = distance;
                return NextHit;
            }
        }

        [Test]
        public void GroundProbe_UsesLockedCapsuleSize()
        {
            Assert.That(CharacterCapsuleGeometry.Default.Width, Is.EqualTo(0.72f));
            Assert.That(CharacterCapsuleGeometry.Default.Height, Is.EqualTo(0.90f));

            var world = new FakeCollisionWorld();
            var probe = new CharacterGroundProbe(
                world,
                CharacterCapsuleGeometry.Default,
                CharacterGroundProbeSettings.Default);

            probe.Probe(Vector2.zero, 0f);

            Assert.That(world.LastCapsule.Width, Is.EqualTo(0.72f));
            Assert.That(world.LastCapsule.Height, Is.EqualTo(0.90f));
            Assert.That(world.LastDirection, Is.EqualTo(Vector2.down));
            Assert.That(world.LastDistance, Is.EqualTo(0.08f));
        }

        [Test]
        public void GroundProbe_ReturnsGroundedForValidDownwardHit()
        {
            var world = new FakeCollisionWorld
            {
                NextHit = new CharacterCollisionHit(
                    true, new Vector2(0f, -0.45f), Vector2.up, 0.04f, 42)
            };
            var probe = new CharacterGroundProbe(
                world,
                CharacterCapsuleGeometry.Default,
                CharacterGroundProbeSettings.Default);

            var result = probe.Probe(Vector2.zero, 0f);

            Assert.That(result.IsGrounded, Is.True);
            Assert.That(result.HasHit, Is.True);
            Assert.That(result.Normal, Is.EqualTo(Vector2.up));
            Assert.That(result.Distance, Is.EqualTo(0.04f));
            Assert.That(result.SupportId, Is.EqualTo(42));
        }

        [Test]
        public void GroundProbe_RejectsMissTooFarWallNormalAndRisingVelocity()
        {
            var world = new FakeCollisionWorld();
            var probe = new CharacterGroundProbe(
                world,
                CharacterCapsuleGeometry.Default,
                CharacterGroundProbeSettings.Default);

            // query miss
            world.NextHit = CharacterCollisionHit.None;
            Assert.That(probe.Probe(Vector2.zero, 0f).IsGrounded, Is.False);

            // 너무 먼 hit (probe distance 0.08 초과)
            world.NextHit = new CharacterCollisionHit(true, Vector2.zero, Vector2.up, 0.2f, 1);
            Assert.That(probe.Probe(Vector2.zero, 0f).IsGrounded, Is.False);

            // 벽/수평 normal
            world.NextHit = new CharacterCollisionHit(true, Vector2.zero, Vector2.right, 0.04f, 2);
            Assert.That(probe.Probe(Vector2.zero, 0f).IsGrounded, Is.False);

            // 상승 임계값(0.05) 초과의 빠른 상승 상태
            world.NextHit = new CharacterCollisionHit(true, Vector2.zero, Vector2.up, 0.04f, 3);
            Assert.That(probe.Probe(Vector2.zero, 1.0f).IsGrounded, Is.False);

            // 동일 hit이라도 상승이 아니면 grounded
            Assert.That(probe.Probe(Vector2.zero, 0.05f).IsGrounded, Is.True);
        }

        [Test]
        public void GroundProbe_RuntimeDependsOnlyOnApprovedAssemblies()
        {
            // CHAR03_01 수리: CHAR01 시대의 "Game.Map* 전면 금지" 가드는 폐기됐다.
            // CHAR03 승인 의존 방향 — MAP 공용 런타임(Game.Map.Runtime) 정확히 1개만
            // 허용하고, Tilemap/InputSystem/authoring/legacy는 계속 금지한다.
            var runtimeAssembly = typeof(CharacterGroundProbe).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            var mapReferences = referenced
                .Where(referencedName => referencedName.StartsWith("Game.Map"))
                .ToArray();

            Assert.That(mapReferences, Is.EquivalentTo(new[] { "Game.Map.Runtime" }),
                "MAP 참조는 공용 런타임 정확히 1개여야 한다");

            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));
            Assert.That(referenced, Does.Not.Contain("Unity.InputSystem"));
            Assert.That(referenced, Does.Not.Contain("Game.Stage.Runtime"));
            Assert.That(referenced, Does.Not.Contain("StarNight.Runtime"));

            foreach (var referencedName in referenced)
            {
                Assert.That(referencedName, Does.Not.StartWith("MapAuthoring"));
                Assert.That(referencedName, Does.Not.Contain(".Editor"));
                Assert.That(referencedName, Does.Not.Contain("Tests"));
            }
        }
    }
}
