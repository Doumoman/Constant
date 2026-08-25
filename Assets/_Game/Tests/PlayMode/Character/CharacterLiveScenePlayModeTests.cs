#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Hud;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Character.Live.Presentation;
using StarNight.Character.Live.Rooms;
using StarNight.Character.Live.Run;
using StarNight.Character.Presentation;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StarNight.Character.Tests.PlayMode
{
    /// <summary>
    /// 라이브 씬 부트 스모크: 실제 CharacterLiveTest 씬을 로드해 부트스트랩/
    /// 세션/리그/무브먼트/방·카메라 드라이버/HUD 바인더가 기동하고 HUD
    /// 텍스트가 실데이터로 채워지는지, 연출 피드백이 HUD에 1회 반영되는지
    /// 검증한다(깨진 직렬화 참조·콘솔 에러 없음).
    /// </summary>
    public sealed class CharacterLiveScenePlayModeTests
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/Live/CharacterLiveTest.unity";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 다음 테스트 격리를 위해 로드된 라이브 씬 루트를 정리한다.
            Scene active = SceneManager.GetActiveScene();
            foreach (GameObject root in active.GetRootGameObjects())
            {
                Object.Destroy(root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneBoot_LiveStackStarts_AndHudBindsRealData()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            // Start/Awake + 첫 Update/FixedUpdate 프레임 소화.
            for (int frame = 0; frame < 5; frame++)
            {
                yield return null;
            }

            var bootstrap =
                Object.FindFirstObjectByType<CharacterLiveRunBootstrap>();
            Assert.IsNotNull(bootstrap, "RunBootstrap 부재");
            Assert.IsTrue(bootstrap.IsRunStarted, "런 미시작");
            Assert.IsTrue(bootstrap.Session.IsSpawnConsumed);

            var rig = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            Assert.IsNotNull(rig, "PlayerRig 부재");
            Assert.IsTrue(rig.IsBound, "리그 미바인딩");

            var movement =
                Object.FindFirstObjectByType<CharacterLiveMovementDriver>();
            Assert.IsNotNull(movement, "MovementDriver 부재");
            Assert.IsTrue(movement.IsDriving, "무브먼트 미구동");

            Assert.IsNotNull(
                Object.FindFirstObjectByType<CharacterLiveRoomTransitionDriver>(),
                "RoomTransitionDriver 부재");
            var cameraDriver =
                Object.FindFirstObjectByType<CharacterLiveCameraRoomDriver>();
            Assert.IsNotNull(cameraDriver, "CameraRoomDriver 부재");
            Assert.IsTrue(cameraDriver.HasCameraRoom, "카메라 초기 정착 안 됨");

            // HUD 바인더: 단일 경로 + 직렬화 참조 유효 + 실데이터 표시.
            CharacterLiveHudBinder[] binders =
                Object.FindObjectsByType<CharacterLiveHudBinder>(
                    FindObjectsSortMode.None);
            Assert.AreEqual(1, binders.Length, "HUD 바인더는 정확히 1개여야 한다");
            CharacterLiveHudBinder binder = binders[0];
            Assert.IsTrue(binder.HasBootstrap, "바인더 bootstrap 참조 끊김");
            Assert.IsNotNull(binder.FeedbackLog);
            Assert.IsNotNull(binder.PresentationConsumer);

            Dictionary<string, string> hudTexts = ReadHudTexts(binder);
            Assert.AreEqual("HP 4/4", hudTexts["HealthText"]);
            Assert.AreEqual("BOMB 4", hudTexts["BombText"]);
            Assert.AreEqual("ROPE 4", hudTexts["RopeText"]);
            Assert.AreEqual("ROOM S0,0 C0,0", hudTexts["RoomText"]);
            Assert.AreEqual("RUN Active", hudTexts["StatusText"]);
            Assert.AreEqual(string.Empty, hudTexts["FeedbackText"]);

            // 연출 이벤트(중복 포함) → 피드백 1회 → HUD 텍스트 반영.
            WorldTileCoord cell;
            WorldCoordinateUtility.TryCreateWorldTile(5, 1, out cell);
            var raw = new List<CharacterPresentationEventRequest>
            {
                new CharacterPresentationEventRequest(
                    CharacterPresentationEventType.RopePlaced,
                    1, false, 0, true, cell, 0),
                new CharacterPresentationEventRequest(
                    CharacterPresentationEventType.RopePlaced,
                    1, false, 0, true, cell, 0)
            };
            int accepted = binder.PresentationConsumer.ConsumeBatch(raw);
            Assert.AreEqual(1, accepted, "중복 연출 이벤트는 1회만 수락");
            Assert.AreEqual(1, binder.PresentationConsumer.DuplicateEventCount);
            Assert.AreEqual(1, binder.FeedbackLog.TotalAppendedCount);

            yield return null;
            hudTexts = ReadHudTexts(binder);
            Assert.AreEqual("ROPE PLACED (5,1)", hudTexts["FeedbackText"]);

            // 플레이어는 연출/HUD 소비로 이동하지 않는다.
            Vector2 positionBefore = rig.Body.position;
            binder.PresentationConsumer.ConsumeBatch(raw);
            yield return null;
            Assert.AreEqual(positionBefore, rig.Body.position);
        }

        private static Dictionary<string, string> ReadHudTexts(
            CharacterLiveHudBinder binder)
        {
            var texts = new Dictionary<string, string>();
            foreach (Text text in binder.GetComponentsInChildren<Text>())
            {
                texts[text.gameObject.name] = text.text;
            }

            return texts;
        }
    }
}
#endif
