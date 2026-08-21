#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Tools;
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using StarNight.Player.Safety;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Player.Tests
{
    public sealed class PlayerMotorPlayModeTests
    {
        private const int GroundLayer = 7;
        private readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

        [UnityTest]
        public IEnumerator RunShellAndPrologueContainOneConfiguredPlayerAndTwoRoomRuntime()
        {
            AudioListener[] existingListeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener listener in existingListeners)
            {
                listener.enabled = false;
            }

            yield return SceneManager.LoadSceneAsync("02_RunShell", LoadSceneMode.Additive);
            yield return SceneManager.LoadSceneAsync("10_Prologue_0_1", LoadSceneMode.Additive);
            yield return waitForFixedUpdate;
            yield return waitForFixedUpdate;

            PlayerMotor2D[] motors = Object.FindObjectsByType<PlayerMotor2D>(FindObjectsSortMode.None);
            Assert.That(motors, Has.Length.EqualTo(1));
            Assert.That(motors[0].GetComponent<GameplayInputReader>(), Is.Not.Null);
            Assert.That(motors[0].GetComponent<PlayerActionRouter>(), Is.Not.Null);
            Assert.That(motors[0].GetComponent<PlayerActionLock>(), Is.Not.Null);
            Assert.That(motors[0].GetComponent<PlayerOutOfBoundsGuard>(), Is.Not.Null);
            GameObject roomRuntime = GameObject.Find("Core04TwoRoomRuntime");
            Assert.That(roomRuntime, Is.Not.Null);
            Assert.That(roomRuntime.transform.Find("Room_A"), Is.Not.Null);
            Assert.That(roomRuntime.transform.Find("Room_B"), Is.Not.Null);

            yield return SceneManager.UnloadSceneAsync("10_Prologue_0_1");
            yield return SceneManager.UnloadSceneAsync("02_RunShell");

            foreach (AudioListener listener in existingListeners)
            {
                if (listener != null)
                {
                    listener.enabled = true;
                }
            }
        }

        [UnityTest]
        public IEnumerator PlayerUsesRequiredColliderAndContinuousDynamicBody()
        {
            GameObject ground = CreateSolid("Ground", new Vector2(0f, -0.5f), new Vector2(8f, 1f));
            PlayerMotor2D motor = CreatePlayer(new Vector2(0f, PlayerMotor2D.ColliderHeight * 0.5f));
            yield return waitForFixedUpdate;

            Assert.That(motor.Capsule.size.x, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(motor.Capsule.size.y, Is.EqualTo(0.92f).Within(0.001f));
            Assert.That(motor.Body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(motor.Body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
            Assert.That(motor.Body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That(motor.Body.freezeRotation, Is.True);

            Object.Destroy(motor.gameObject);
            Object.Destroy(ground);
        }

        [UnityTest]
        public IEnumerator OneCellHighPassageDoesNotTrapPlayer()
        {
            GameObject ground = CreateSolid("Ground", new Vector2(0f, -0.5f), new Vector2(32f, 1f));
            GameObject ceiling = CreateSolid("Ceiling", new Vector2(0f, 1.5f), new Vector2(6f, 1f));
            PlayerMotor2D motor = CreatePlayer(new Vector2(-5f, PlayerMotor2D.ColliderHeight * 0.5f));
            yield return waitForFixedUpdate;
            yield return waitForFixedUpdate;

            motor.SetMoveInput(1f);
            for (int index = 0; index < 150; index++)
            {
                yield return waitForFixedUpdate;
            }

            Assert.That(motor.Body.position.x, Is.GreaterThan(4f));
            Assert.That(
                motor.Body.position.y,
                Is.GreaterThanOrEqualTo(0.44f),
                $"bounds={motor.Capsule.bounds}, size={motor.Capsule.size}, direction={motor.Capsule.direction}, grounded={motor.IsGrounded}");

            Object.Destroy(motor.gameObject);
            Object.Destroy(ground);
            Object.Destroy(ceiling);
        }

        [UnityTest]
        public IEnumerator BaseJumpClearsOneCellButNotTwoAndIsFrameRateIndependent()
        {
            int originalTargetFrameRate = Application.targetFrameRate;
            int originalVSync = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;

            float heightAt30 = 0f;
            yield return MeasureJump(30, value => heightAt30 = value);
            float heightAt60 = 0f;
            yield return MeasureJump(60, value => heightAt60 = value);
            float heightAt120 = 0f;
            yield return MeasureJump(120, value => heightAt120 = value);

            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.vSyncCount = originalVSync;

            PlayerMotionSettings settings = new PlayerMotionSettings();
            float expectedDiscreteApex = settings.PredictDiscreteApexHeight(
                PlayerMotionSettings.RequiredFixedDeltaTime);
            Assert.That(heightAt30, Is.EqualTo(expectedDiscreteApex).Within(0.05f));
            Assert.That(heightAt60, Is.EqualTo(expectedDiscreteApex).Within(0.05f));
            Assert.That(heightAt120, Is.EqualTo(expectedDiscreteApex).Within(0.05f));
            Assert.That(heightAt60, Is.GreaterThan(1f).And.LessThan(2f));
            Assert.That(Mathf.Abs(heightAt30 - heightAt60), Is.LessThanOrEqualTo(0.05f));
            Assert.That(Mathf.Abs(heightAt60 - heightAt120), Is.LessThanOrEqualTo(0.05f));
        }

        [UnityTest]
        public IEnumerator MaximumFallSpeedLandsWithoutTerrainTunneling()
        {
            GameObject ground = CreateSolid("Ground", new Vector2(0f, -0.5f), new Vector2(8f, 1f));
            PlayerMotor2D motor = CreatePlayer(new Vector2(0f, 8f));
            motor.Body.linearVelocity = new Vector2(0f, -12f);

            for (int index = 0; index < 90; index++)
            {
                yield return waitForFixedUpdate;
            }

            Assert.That(motor.Body.position.y, Is.GreaterThanOrEqualTo(0.44f));
            Assert.That(motor.Body.linearVelocity.y, Is.GreaterThan(-12.01f));

            Object.Destroy(motor.gameObject);
            Object.Destroy(ground);
        }

        [UnityTest]
        public IEnumerator SpecialJumpClearsTwoCellsButRejectsBlockedHeadroom()
        {
            GameObject ground = CreateSolid("SpringGround", new Vector2(0f, -0.5f), new Vector2(8f, 1f));
            PlayerMotor2D motor = CreatePlayer(new Vector2(0f, PlayerMotor2D.ColliderHeight * 0.5f));
            yield return waitForFixedUpdate;
            yield return waitForFixedUpdate;

            float startY = motor.Body.position.y;
            float maximumY = startY;
            Assert.That(motor.TryLaunchSpecialJump(
                SpringEquipmentContract.JumpVelocity,
                SpringEquipmentContract.RequiredHeadClearanceCells), Is.True);
            for (int index = 0; index < 75; index++)
            {
                yield return waitForFixedUpdate;
                maximumY = Mathf.Max(maximumY, motor.Body.position.y);
            }
            Assert.That(maximumY - startY, Is.GreaterThan(2f).And.LessThan(3f));

            Object.Destroy(motor.gameObject);
            yield return null;
            PlayerMotor2D blockedMotor = CreatePlayer(new Vector2(0f, PlayerMotor2D.ColliderHeight * 0.5f));
            GameObject ceiling = CreateSolid("SpringCeiling", new Vector2(0f, 1.8f), new Vector2(3f, 0.5f));
            yield return waitForFixedUpdate;
            yield return waitForFixedUpdate;
            Assert.That(blockedMotor.TryLaunchSpecialJump(
                SpringEquipmentContract.JumpVelocity,
                SpringEquipmentContract.RequiredHeadClearanceCells), Is.False);

            Object.Destroy(blockedMotor.gameObject);
            Object.Destroy(ceiling);
            Object.Destroy(ground);
        }

        [UnityTest]
        public IEnumerator OutOfBoundsRestoresSafePositionAndClearsVelocity()
        {
            Vector2 safePosition = new Vector2(0f, PlayerMotor2D.ColliderHeight * 0.5f);
            GameObject ground = CreateSolid("RecoveryGround", new Vector2(0f, -0.5f), new Vector2(8f, 1f));
            PlayerMotor2D motor = CreatePlayer(safePosition);
            PlayerOutOfBoundsGuard guard = motor.gameObject.AddComponent<PlayerOutOfBoundsGuard>();
            guard.Configure(new Rect(-2f, -1f, 4f, 4f), safePosition, false);
            motor.SnapTo(new Vector2(3f, 0f));
            motor.Body.linearVelocity = new Vector2(6f, -12f);

            yield return waitForFixedUpdate;
            yield return waitForFixedUpdate;

            Assert.That(Vector2.Distance(motor.Body.position, safePosition), Is.LessThan(0.02f));
            Assert.That(motor.Body.linearVelocity.sqrMagnitude, Is.LessThan(0.01f));
            Assert.That(guard.IsRecoveryInvulnerable, Is.True);
            Assert.That(guard.RecoveryInvulnerabilityRemaining, Is.GreaterThan(0.75f));
            Assert.That(guard.LastRecoveryCause, Is.EqualTo(PlayerRecoveryCause.RoomBounds));
            Assert.That(guard.LastSafeCell, Is.EqualTo(new Vector2Int(0, 0)));

            Object.Destroy(motor.gameObject);
            Object.Destroy(ground);
        }

        private IEnumerator MeasureJump(int targetFrameRate, System.Action<float> result)
        {
            Application.targetFrameRate = targetFrameRate;
            GameObject ground = CreateSolid($"Ground_{targetFrameRate}", new Vector2(0f, -0.5f), new Vector2(8f, 1f));
            PlayerMotor2D motor = CreatePlayer(new Vector2(0f, PlayerMotor2D.ColliderHeight * 0.5f));
            yield return waitForFixedUpdate;
            yield return waitForFixedUpdate;

            float startY = motor.Body.position.y;
            float maximumY = startY;
            motor.SetJumpHeld(true);
            motor.QueueJump();
            for (int index = 0; index < 60; index++)
            {
                yield return waitForFixedUpdate;
                maximumY = Mathf.Max(maximumY, motor.Body.position.y);
            }

            result(maximumY - startY);
            Object.Destroy(motor.gameObject);
            Object.Destroy(ground);
            yield return null;
        }

        private static PlayerMotor2D CreatePlayer(Vector2 position)
        {
            GameObject player = new GameObject("PlayerTest");
            player.layer = 31;
            player.transform.position = position;
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>();

            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -0.49f, 0f);

            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            motor.ConfigureForTests(1 << GroundLayer);
            return motor;
        }

        private static GameObject CreateSolid(string objectName, Vector2 position, Vector2 size)
        {
            GameObject solid = new GameObject(objectName);
            solid.layer = GroundLayer;
            solid.transform.position = position;
            BoxCollider2D collider = solid.AddComponent<BoxCollider2D>();
            collider.size = size;
            return solid;
        }
    }
}

#endif
