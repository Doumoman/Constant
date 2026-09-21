#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace StarNight.Map.SV5.Examples.Tests
{
    public sealed class Sv5WorldPlayerExamplePlayModeTests
    {
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/Examples/SV5_624x416_PlayerExample.unity";

        [UnityTest]
        public IEnumerator SceneLoadsCompletedSv5LayersAndVerifiedLivePlayer()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Sv5ExampleSceneDescriptor descriptor =
                Object.FindFirstObjectByType<Sv5ExampleSceneDescriptor>();
            Sv5ExamplePlayerController player =
                Object.FindFirstObjectByType<Sv5ExamplePlayerController>();
            CharacterLiveCameraFollowDriver camera =
                Object.FindFirstObjectByType<CharacterLiveCameraFollowDriver>();
            Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.WorldWidth, Is.EqualTo(624));
            Assert.That(descriptor.WorldHeight, Is.EqualTo(416));
            Assert.That(descriptor.JumpSupportCount, Is.EqualTo(10));
            Assert.That(descriptor.JumpFixtureCellCount, Is.EqualTo(46));
            Assert.That(descriptor.VerifiedPhysicalCaseCount, Is.EqualTo(38));
            Assert.That(descriptor.CurrentMilestone, Is.EqualTo("SV5_20_JUMP_PLAYER"));
            Assert.That(descriptor.PlayerVerified, Is.True);
            Assert.That(descriptor.FullWorldCompletionMap, Is.True);
            Assert.That(descriptor.UsesOnlySv5Content, Is.True);
            Assert.That(descriptor.CompletionStart, Is.EqualTo(new Vector2(420.5f, 337.46f)));
            Assert.That(descriptor.CompletionExit, Is.EqualTo(new Vector2(358f, 297f)));
            Assert.That(descriptor.SourceArtifacts.All(value => value.Contains("SV5")), Is.True);
            Assert.That(player, Is.Not.Null);
            Assert.That(player.name, Is.EqualTo("SV5_PlayerVerified_Player"));
            Assert.That(player.Rig, Is.TypeOf<CharacterLivePlayerRig>());
            Assert.That(player.Movement, Is.TypeOf<CharacterLiveMovementDriver>());
            Assert.That(player.Rig.IsBound, Is.True);
            Assert.That(player.Body, Is.Not.Null);
            Assert.That(player.BodyCollider, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.VisibleWorldSize, Is.EqualTo(new Vector2(12f, 8f)));
            Assert.That(camera.GetComponent<Camera>().orthographicSize, Is.EqualTo(4f).Within(0.001f));
            Assert.That(camera.GetComponent<Camera>().pixelRect.width /
                camera.GetComponent<Camera>().pixelRect.height, Is.EqualTo(1.5f).Within(0.02f));
            Assert.That(tilemaps.Select(value => value.name), Does.Contain("SV5_Completed_Solid"));
            Assert.That(tilemaps.Select(value => value.name), Does.Contain("SV5_Completed_OneWay"));
            Assert.That(Object.FindFirstObjectByType<CharacterLiveGrabSurface>().IsGrabAllowed, Is.True);
            CharacterLiveOneWayPlatform[] oneWays =
                Object.FindObjectsByType<CharacterLiveOneWayPlatform>(FindObjectsSortMode.None);
            Assert.That(oneWays.Length, Is.GreaterThan(0));
            Assert.That(oneWays.Sum(value => Mathf.RoundToInt(value.PlatformCollider.bounds.size.x)),
                Is.EqualTo(descriptor.OneWayCellCount));
            Assert.That(oneWays.All(value => value.PlatformCollider is BoxCollider2D), Is.True);
            Assert.That(Object.FindFirstObjectByType<Sv5CompletionGoal>(), Is.Not.Null);

            player.Respawn();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(player.Movement.IsDriving, Is.True);
            Assert.That(player.IsGroundedNow, Is.True);
            Assert.That(player.Body.position.x, Is.InRange(0f, 624f));
            Assert.That(player.Body.position.y, Is.InRange(0f, 416f));
        }

        [UnityTest]
        public IEnumerator VerifiedLivePlayerNaturallyLandsOnIndividualTopOnlyCell()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            Sv5ExampleSceneDescriptor descriptor =
                Object.FindFirstObjectByType<Sv5ExampleSceneDescriptor>();
            Sv5ExamplePlayerController player =
                Object.FindFirstObjectByType<Sv5ExamplePlayerController>();
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(player, Is.Not.Null);

            Vector2Int origin = descriptor.JumpFixtureOrigin;
            var start = new Vector2(origin.x + 7.5f, origin.y + 7.46f);
            float expectedLandingY = origin.y + 3.46f;
            player.Body.position = start;
            player.transform.position = start;
            player.Body.linearVelocity = Vector2.zero;
            player.Movement.ResetMotion();
            Physics2D.SyncTransforms();

            bool landed = false;
            for (int step = 0; step < 180; step++)
            {
                yield return new WaitForFixedUpdate();
                if (player.IsGroundedNow &&
                    Mathf.Abs(player.Body.position.y - expectedLandingY) <= 0.03f)
                {
                    landed = true;
                    break;
                }
            }

            Assert.That(landed, Is.True,
                "The verified live Player must stand on the R0 TOP_ONLY cell at local (7,2). " +
                "final=" + player.Body.position + " grounded=" + player.IsGroundedNow);
            Assert.That(player.Body.position.y, Is.EqualTo(expectedLandingY).Within(0.03f));

            CharacterLiveOneWayPlatform support =
                Object.FindObjectsByType<CharacterLiveOneWayPlatform>(FindObjectsSortMode.None)
                    .SingleOrDefault(value =>
                        value.PlatformCollider.bounds.min.x <= origin.x + 7.5f &&
                        value.PlatformCollider.bounds.max.x >= origin.x + 7.5f &&
                        Mathf.Abs(value.PlatformCollider.bounds.max.y - (origin.y + 3f)) < 0.001f);
            Assert.That(support, Is.Not.Null);
            Assert.That(support.PlatformCollider.bounds.max.y,
                Is.EqualTo(origin.y + 3f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ReachingTheSv5ExitIsTheOnlyCompletionCondition()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            Sv5ExampleSceneDescriptor descriptor =
                Object.FindFirstObjectByType<Sv5ExampleSceneDescriptor>();
            Sv5ExamplePlayerController player =
                Object.FindFirstObjectByType<Sv5ExamplePlayerController>();
            Sv5CompletionGoal goal = Object.FindFirstObjectByType<Sv5CompletionGoal>();
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(goal, Is.Not.Null);
            Assert.That(goal.Completed, Is.False);

            player.Body.position = descriptor.CompletionExit;
            player.transform.position = descriptor.CompletionExit;
            player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(goal.Completed, Is.True,
                "The full-world SV5 example completes only when the live Player reaches Exit.");
        }

        [UnityTest]
        public IEnumerator FullWorldShaftOneWaySupportsStanding()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            Sv5ExamplePlayerController player =
                Object.FindFirstObjectByType<Sv5ExamplePlayerController>();
            Assert.That(player, Is.Not.Null);

            var standingPosition = new Vector2(570.5f, 200.46f);
            player.Body.position = standingPosition;
            player.transform.position = standingPosition;
            player.Body.linearVelocity = Vector2.zero;
            player.Movement.ResetMotion();
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(player.IsGroundedNow, Is.True,
                "The authored 624x416 completion shaft ONE_WAY must support the live Player.");
            Assert.That(player.Body.position.y, Is.EqualTo(standingPosition.y).Within(0.03f));

            CharacterLiveOneWayPlatform support =
                Object.FindObjectsByType<CharacterLiveOneWayPlatform>(FindObjectsSortMode.None)
                    .SingleOrDefault(value =>
                        value.PlatformCollider.bounds.min.x <= standingPosition.x &&
                        value.PlatformCollider.bounds.max.x >= standingPosition.x &&
                        Mathf.Abs(value.PlatformCollider.bounds.max.y - 200f) < 0.001f);
            Assert.That(support, Is.Not.Null);
        }
    }
}
#endif
