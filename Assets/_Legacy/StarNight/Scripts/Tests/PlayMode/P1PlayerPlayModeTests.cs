#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Player;
using StarNight.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P1PlayerPlayModeTests
    {
        private const string ScenePath = "Assets/StarNight/Scenes/Labs/P1_GridLab_30x18.unity";
        private const string PrefabPath = "Assets/StarNight/Prefabs/Gameplay/P1_Player.prefab";

        [UnityTest]
        public IEnumerator JumpHeight_IsWithinP1TargetRange()
        {
            PlayerMotor2D motor = CreateIsolatedPlayer(out GameObject ground, out GameObject player);
            PlayerInputAdapter input = motor.Input;
            Rigidbody2D body = motor.Body;

            yield return WaitFixedFrames(8);
            float startFootY = body.position.y - 0.45f;
            float maximumFootY = startFootY;
            int startJumpCount = motor.JumpCount;

            input.PressJumpForTests();
            bool becameAirborne = false;
            for (int frame = 0; frame < 180; frame++)
            {
                yield return new WaitForFixedUpdate();
                maximumFootY = Mathf.Max(maximumFootY, body.position.y - 0.45f);
                becameAirborne |= body.linearVelocity.y > 0.05f;
                if (becameAirborne && motor.IsGrounded && body.linearVelocity.y <= 0.05f)
                {
                    break;
                }
            }

            float height = maximumFootY - startFootY;
            Assert.That(motor.JumpCount, Is.EqualTo(startJumpCount + 1));
            Assert.That(height, Is.InRange(2.05f, 2.35f), $"Measured jump height was {height:0.000} cells.");

            Object.Destroy(player);
            Object.Destroy(ground);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HorizontalReach_IsApproximatelyThreePointTwoCells()
        {
            PlayerMotor2D motor = CreateIsolatedPlayer(out GameObject ground, out GameObject player);
            PlayerInputAdapter input = motor.Input;
            Rigidbody2D body = motor.Body;

            input.SetMoveForTests(Vector2.right);
            yield return WaitFixedFrames(80);
            Assert.That(body.linearVelocity.x, Is.EqualTo(3.75f).Within(0.08f));

            float startX = body.position.x;
            input.PressJumpForTests();
            bool becameAirborne = false;
            for (int frame = 0; frame < 180; frame++)
            {
                yield return new WaitForFixedUpdate();
                becameAirborne |= body.linearVelocity.y > 0.05f;
                if (becameAirborne && motor.IsGrounded && body.linearVelocity.y <= 0.05f)
                {
                    break;
                }
            }

            float reach = body.position.x - startX;
            Assert.That(reach, Is.InRange(3.0f, 3.4f), $"Measured horizontal reach was {reach:0.000} cells.");

            Object.Destroy(player);
            Object.Destroy(ground);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OneCellTunnel_OneHundredBidirectionalSweepsHaveZeroFailures()
        {
            yield return LoadLabScene();
            int groundMask = 1 << LayerMask.NameToLayer("Ground");
            Physics2D.SyncTransforms();

            int failures = 0;
            for (int index = 0; index < 100; index++)
            {
                bool leftToRight = index % 2 == 0;
                float offset = Mathf.Lerp(-0.04f, 0.04f, (index % 50) / 49f);
                Vector2 origin = new Vector2(leftToRight ? 2.5f : 10.5f, 1.5f + offset);
                Vector2 direction = leftToRight ? Vector2.right : Vector2.left;
                RaycastHit2D hit = Physics2D.CapsuleCast(
                    origin,
                    new Vector2(0.72f, 0.90f),
                    CapsuleDirection2D.Vertical,
                    0f,
                    direction,
                    8f,
                    groundMask);
                if (hit.collider != null)
                {
                    failures++;
                }
            }

            Assert.That(failures, Is.EqualTo(0), "A 0.72x0.90 player must sweep through the exact one-cell tunnel without collision.");
        }

        [UnityTest]
        public IEnumerator OneCellTunnel_PlayerMovesThroughBothDirectionsWithoutStall()
        {
            yield return LoadLabScene();
            PlayerMotor2D motor = Object.FindFirstObjectByType<PlayerMotor2D>();
            P1GridLabTelemetry telemetry = Object.FindFirstObjectByType<P1GridLabTelemetry>();
            Assert.That(motor, Is.Not.Null);
            Assert.That(telemetry, Is.Not.Null);

            PlayerInputAdapter input = motor.Input;
            input.SetMoveForTests(Vector2.right);
            bool exitedRight = false;
            for (int frame = 0; frame < 320; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (motor.Body.position.x > 10.6f)
                {
                    exitedRight = true;
                    break;
                }
            }

            Assert.That(
                exitedRight,
                Is.True,
                $"Player failed to traverse the tunnel left-to-right. " +
                $"x={motor.Body.position.x:0.000}, y={motor.Body.position.y:0.000}, " +
                $"velocity={motor.Body.linearVelocity}, grounded={motor.IsGrounded}, " +
                $"failures={telemetry.TunnelFailureCount}.");

            input.SetMoveForTests(Vector2.left);
            bool exitedLeft = false;
            for (int frame = 0; frame < 320; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (motor.Body.position.x < 2.4f)
                {
                    exitedLeft = true;
                    break;
                }
            }

            Assert.That(exitedLeft, Is.True, "Player failed to traverse the tunnel right-to-left.");
            Assert.That(telemetry.TunnelFailureCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FallRecovery_ReturnsToLatestSafeCellAndConsumesOneHealth()
        {
            yield return LoadLabScene();
            PlayerMotor2D motor = Object.FindFirstObjectByType<PlayerMotor2D>();
            PlayerRecovery recovery = Object.FindFirstObjectByType<PlayerRecovery>();
            SafeCellTracker safeCellTracker = Object.FindFirstObjectByType<SafeCellTracker>();
            GridBoundedCamera2D cameraFollow = Object.FindFirstObjectByType<GridBoundedCamera2D>();
            Assert.That(motor, Is.Not.Null);
            Assert.That(recovery, Is.Not.Null);
            Assert.That(safeCellTracker, Is.Not.Null);

            PlayerInputAdapter input = motor.Input;
            input.SetMoveForTests(Vector2.right);
            for (int frame = 0; frame < 320 && motor.Body.position.x < 10.25f; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            input.SetMoveForTests(Vector2.zero);
            yield return WaitFixedFrames(50);
            Assert.That(
                safeCellTracker.LastSafePosition.x,
                Is.GreaterThan(9.5f),
                $"The latest settled cell was not recorded after movement: {safeCellTracker.LastSafePosition}.");

            Vector2 expectedSafePosition = safeCellTracker.LastSafePosition;

            motor.Body.position = new Vector2(26.5f, 5f);
            motor.transform.position = new Vector3(26.5f, 5f, motor.transform.position.z);
            motor.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            for (int frame = 0; frame < 300 && recovery.RecoveryCount == 0; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(recovery.RecoveryCount, Is.EqualTo(1));
            Assert.That(recovery.CurrentHealth, Is.EqualTo(3));
            Assert.That(
                motor.Body.position.x,
                Is.EqualTo(expectedSafePosition.x).Within(0.08f),
                $"Recovered body={motor.Body.position}; safe={safeCellTracker.LastSafePosition}; " +
                $"cell={safeCellTracker.LastSafeCell}; reason={recovery.LastReason}.");
            Assert.That(motor.Body.position.y, Is.EqualTo(expectedSafePosition.y).Within(0.08f));
            Assert.That(motor.Body.linearVelocity.sqrMagnitude, Is.LessThan(0.05f));

            yield return null;
            Vector2 expectedCamera = cameraFollow.CalculateClampedPosition(
                expectedSafePosition + new Vector2(0f, 1.25f));
            Assert.That(
                Vector2.Distance(cameraFollow.transform.position, expectedCamera),
                Is.LessThan(0.05f));
        }

        private static PlayerMotor2D CreateIsolatedPlayer(out GameObject ground, out GameObject player)
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
#else
            GameObject prefab = null;
            Assert.Ignore("P1 physics tests require the Unity Editor asset database.");
#endif
            const float isolatedOriginX = 100f;
            ground = new GameObject("TestGround");
            int groundLayer = LayerMask.NameToLayer("Ground");
            ground.layer = groundLayer >= 0 ? groundLayer : 0;
            ground.transform.position = new Vector3(isolatedOriginX, -0.5f, 0f);
            BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(20f, 1f);

            player = Object.Instantiate(prefab);
            player.name = "TestPlayer";
            player.transform.position = new Vector3(isolatedOriginX, 0.45f, 0f);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.position = player.transform.position;
            body.linearVelocity = Vector2.zero;
            PlayerInputAdapter input = player.GetComponent<PlayerInputAdapter>();
            input.EnableTestInput(true);
            Physics2D.SyncTransforms();
            return player.GetComponent<PlayerMotor2D>();
        }

        private static IEnumerator LoadLabScene()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("P1 lab scene tests require the Unity Editor.");
#endif
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();
        }

        private static IEnumerator WaitFixedFrames(int count)
        {
            for (int index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}

#endif
