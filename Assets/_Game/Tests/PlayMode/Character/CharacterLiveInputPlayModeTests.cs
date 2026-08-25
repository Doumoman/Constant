#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Live.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace StarNight.Character.Tests.PlayMode
{
    /// <summary>
    /// 키보드 입력 스모크(InputTestFixture 합성 장치). 잠금 바인딩
    /// (Move=A/D+화살표, Down=S/아래, Jump=Space, Action=X, Bomb=Z, Rope=C)이
    /// CharacterLiveInputSource/Adapter를 거쳐 잠금 CharacterActionId로
    /// 흐르는지, Move/Down이 축으로 유지되는지 검증한다.
    /// </summary>
    public sealed class CharacterLiveInputPlayModeTests : InputTestFixture
    {
        private const string ActionsAssetPath =
            "Assets/_Game/Live/Input/CharacterLiveControls.inputactions";

        private GameObject sourceGo;
        private CharacterLiveInputSource source;
        private Keyboard keyboard;
        private long tick;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            tick = 0;

            var asset = UnityEditor.AssetDatabase
                .LoadAssetAtPath<InputActionAsset>(ActionsAssetPath);
            Assert.IsNotNull(asset, "CharacterLiveControls.inputactions 로드 실패");

            // Awake 전에 직렬화 필드를 채우기 위해 비활성 생성 → 배선 → 활성.
            sourceGo = new GameObject("InputSmokeSource");
            sourceGo.SetActive(false);
            source = sourceGo.AddComponent<CharacterLiveInputSource>();
            var so = new UnityEditor.SerializedObject(source);
            so.FindProperty("actionsAsset").objectReferenceValue = asset;
            so.ApplyModifiedPropertiesWithoutUndo();
            sourceGo.SetActive(true);
        }

        public override void TearDown()
        {
            if (sourceGo != null)
            {
                Object.DestroyImmediate(sourceGo);
            }

            base.TearDown();
        }

        private CharacterInputSnapshot Consume()
        {
            tick++;
            return source.ConsumeFixedSnapshot(tick);
        }

        [Test]
        public void MoveAndDown_AreAxisStyleActions()
        {
            var asset = UnityEditor.AssetDatabase
                .LoadAssetAtPath<InputActionAsset>(ActionsAssetPath);
            InputActionMap map = asset.FindActionMap("Player", true);

            Assert.AreEqual("Axis", map.FindAction("Move", true).expectedControlType);
            Assert.AreEqual(
                InputActionType.Value, map.FindAction("Move", true).type);
            Assert.AreEqual(
                InputActionType.Button, map.FindAction("Jump", true).type);
            Assert.AreEqual(
                InputActionType.Button, map.FindAction("Down", true).type);

            // 잠금 액션 6종 외 액션 없음.
            Assert.AreEqual(6, map.actions.Count);
        }

        [UnityTest]
        public IEnumerator Keyboard_MoveAxis_FlowsThroughAdapter()
        {
            Assert.IsTrue(source.IsReady);

            Press(keyboard.dKey);
            yield return null;
            Assert.AreEqual(1f, Consume().Horizontal, "D=+1");

            Release(keyboard.dKey);
            yield return null;
            Consume();
            Press(keyboard.aKey);
            yield return null;
            Assert.AreEqual(-1f, Consume().Horizontal, "A=-1");

            Release(keyboard.aKey);
            yield return null;
            Consume();
            Press(keyboard.rightArrowKey);
            yield return null;
            Assert.AreEqual(1f, Consume().Horizontal, "Right=+1");

            Release(keyboard.rightArrowKey);
            yield return null;
            Consume();
            Press(keyboard.leftArrowKey);
            yield return null;
            Assert.AreEqual(-1f, Consume().Horizontal, "Left=-1");

            Release(keyboard.leftArrowKey);
            yield return null;
            Consume();
            Press(keyboard.sKey);
            yield return null;
            CharacterInputSnapshot snapshot = Consume();
            Assert.IsTrue(snapshot.DownHeld, "S=down");
            Assert.AreEqual(0f, snapshot.Horizontal, "S horizontal 0");

            Release(keyboard.sKey);
            yield return null;
            Consume();
            Press(keyboard.downArrowKey);
            yield return null;
            Assert.IsTrue(Consume().DownHeld, "DownArrow=down");
        }

        [UnityTest]
        public IEnumerator Keyboard_Buttons_MapToLockedActionIds()
        {
            // Space → Jump: 눌림 에지 1회 소비 후 소거, held 유지.
            Press(keyboard.spaceKey);
            yield return null;
            CharacterInputSnapshot snapshot = Consume();
            Assert.IsTrue(snapshot.Jump.PressedThisFrame, "jump pressed edge");
            Assert.IsTrue(
                snapshot.IsPressedThisFrame(CharacterActionId.Jump),
                "ActionId.Jump");
            snapshot = Consume();
            Assert.IsFalse(snapshot.Jump.PressedThisFrame, "jump edge cleared");
            Assert.IsTrue(snapshot.Jump.Held, "jump held persists");
            Release(keyboard.spaceKey);
            yield return null;
            Assert.IsTrue(Consume().Jump.ReleasedThisFrame, "jump released edge");

            // X → Action(단독).
            Press(keyboard.xKey);
            yield return null;
            snapshot = Consume();
            Assert.IsTrue(
                snapshot.IsPressedThisFrame(CharacterActionId.Action),
                "ActionId.Action plain");
            Assert.IsFalse(
                snapshot.IsPressedThisFrame(CharacterActionId.SafeDrop),
                "no SafeDrop without down");
            Release(keyboard.xKey);
            yield return null;
            Consume();

            // S+X → SafeDrop 우선(잠금 조합 규칙 — 단독 Action 아님).
            Press(keyboard.sKey);
            yield return null;
            Consume();
            Press(keyboard.xKey);
            yield return null;
            snapshot = Consume();
            Assert.IsTrue(
                snapshot.IsPressedThisFrame(CharacterActionId.SafeDrop),
                "ActionId.SafeDrop combo");
            Assert.IsFalse(
                snapshot.IsPressedThisFrame(CharacterActionId.Action),
                "SafeDrop suppresses plain Action");
            Release(keyboard.sKey);
            yield return null;
            Consume();
            Release(keyboard.xKey);
            yield return null;
            Consume();

            // Z → Bomb, C → Rope.
            Press(keyboard.zKey);
            yield return null;
            Assert.IsTrue(
                Consume().IsPressedThisFrame(CharacterActionId.Bomb),
                "ActionId.Bomb");
            Release(keyboard.zKey);
            yield return null;
            Consume();
            Press(keyboard.cKey);
            yield return null;
            snapshot = Consume();
            Assert.IsTrue(
                snapshot.IsPressedThisFrame(CharacterActionId.Rope),
                "ActionId.Rope");
            Assert.IsFalse(
                snapshot.IsPressedThisFrame(CharacterActionId.Bomb),
                "no Bomb on C");
            Release(keyboard.cKey);
            yield return null;
            Consume();

            // 레거시 금지 키(E/F/Q)는 어떤 행동도 만들지 않는다.
            // (기준선: 이전 키 상태가 전부 해제된 것을 먼저 확인한다.)
            yield return null;
            snapshot = Consume();
            Assert.IsFalse(snapshot.DownHeld, "baseline no down before legacy");
            Press(keyboard.eKey);
            yield return null;
            Press(keyboard.fKey);
            yield return null;
            Press(keyboard.qKey);
            yield return null;
            snapshot = Consume();
            Assert.AreEqual(0f, snapshot.Horizontal, "legacy no move");
            Assert.IsFalse(snapshot.DownHeld, "legacy no down");
            Assert.IsFalse(
                snapshot.Jump.PressedThisFrame || snapshot.Jump.Held,
                "legacy no jump");
            Assert.IsFalse(
                snapshot.Action.PressedThisFrame || snapshot.Action.Held,
                "legacy no action");
            Assert.IsFalse(
                snapshot.Bomb.PressedThisFrame || snapshot.Bomb.Held,
                "legacy no bomb");
            Assert.IsFalse(
                snapshot.Rope.PressedThisFrame || snapshot.Rope.Held,
                "legacy no rope");
        }
    }
}
#endif
