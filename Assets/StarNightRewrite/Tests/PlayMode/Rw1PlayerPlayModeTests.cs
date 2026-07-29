using System.Collections;
using NUnit.Framework;
using StarNight.Rewrite.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace StarNight.Rewrite.PlayModeTests
{
    public sealed class Rw1PlayerPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator KeyboardMovementAndJump_ProduceExpectedVelocity()
        {
            Time.timeScale = 1f;
            GameObject ground = CreateGround();
            GameObject player = CreatePlayer();

            try
            {
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
                Press(keyboard.dKey);
                yield return null;
                yield return new WaitForFixedUpdate();

                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                Assert.That(body.linearVelocity.x, Is.GreaterThan(0.1f));

                Press(keyboard.spaceKey);
                yield return null;
                yield return new WaitForFixedUpdate();

                Assert.That(body.linearVelocity.y, Is.GreaterThan(0.1f));

                Release(keyboard.spaceKey);
                Release(keyboard.dKey);
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator FallingBelowThreshold_ReturnsToAnchorAndCostsOneHeart()
        {
            Time.timeScale = 1f;
            GameObject player = CreatePlayer();

            try
            {
                yield return null;

                PlayerMotor2D motor = player.GetComponent<PlayerMotor2D>();
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                SafeAnchorService anchor =
                    player.GetComponent<SafeAnchorService>();
                anchor.Register(Vector2.zero);

                motor.Teleport(new Vector2(0f, -9f));
                for (int frame = 0;
                     frame < 10 &&
                     (player.transform.position.y < -2f ||
                      health.Current == health.Maximum);
                     frame++)
                {
                    yield return null;
                }

                Assert.That(player.transform.position.y, Is.GreaterThan(-2f));
                Assert.That(health.Current, Is.EqualTo(3));
            }
            finally
            {
                Object.Destroy(player);
            }
        }

        [UnityTest]
        public IEnumerator RaniLamp_RescuesFirstDepletionButNotSecond()
        {
            Time.timeScale = 1f;
            GameObject player = CreatePlayer();

            try
            {
                yield return null;

                PlayerHealth health = player.GetComponent<PlayerHealth>();
                RaniLampController lamp =
                    player.GetComponent<RaniLampController>();
                bool defeated = false;
                health.Defeated += () => defeated = true;

                Assert.That(health.TryTakeDamage(4), Is.True);
                Assert.That(health.Current, Is.EqualTo(2));
                Assert.That(lamp.IsAvailable, Is.False);
                Assert.That(defeated, Is.False);

                float timeout = Time.realtimeSinceStartup + 3f;
                while (health.IsInvulnerable &&
                       Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(health.IsInvulnerable, Is.False);
                Assert.That(health.TryTakeDamage(2), Is.True);
                Assert.That(health.Current, Is.Zero);
                Assert.That(defeated, Is.True);
            }
            finally
            {
                Object.Destroy(player);
            }
        }

        [UnityTest]
        public IEnumerator Carryable_CanBePickedUpAndThrown()
        {
            Time.timeScale = 1f;
            GameObject player = CreatePlayer();
            GameObject crate = new GameObject("Test Crate");
            crate.AddComponent<Rigidbody2D>();
            crate.AddComponent<BoxCollider2D>();
            Carryable2D carryable = crate.AddComponent<Carryable2D>();

            try
            {
                yield return null;

                PlayerCarry carry = player.GetComponent<PlayerCarry>();
                Assert.That(carry.TryPickUp(carryable), Is.True);
                Assert.That(carry.IsCarrying, Is.True);
                Assert.That(crate.GetComponent<Collider2D>().enabled, Is.False);

                carry.Release(true);

                Assert.That(carry.IsCarrying, Is.False);
                Assert.That(crate.GetComponent<Collider2D>().enabled, Is.True);
                Assert.That(
                    crate.GetComponent<Rigidbody2D>().linearVelocity.x,
                    Is.GreaterThan(0f));
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(crate);
            }
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("RW1 Test Player");
            player.layer = LayerMask.NameToLayer("Player");
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            player.AddComponent<CapsuleCollider2D>().size =
                new Vector2(0.72f, 1.1f);
            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerMotor2D>();
            player.AddComponent<SafeAnchorService>();
            player.AddComponent<RaniLampController>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerFallRecovery>();
            player.AddComponent<PlayerCarry>();
            return player;
        }

        private static GameObject CreateGround()
        {
            GameObject ground = new GameObject("RW1 Test Ground");
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.position = new Vector2(0f, -1.05f);
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(10f, 1f);
            return ground;
        }
    }
}
