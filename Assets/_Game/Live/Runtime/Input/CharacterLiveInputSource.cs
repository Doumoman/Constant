using StarNight.Character.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.Character.Live.Input
{
    /// <summary>
    /// Unity Input System(Player 맵) → 잠금 캐릭터 입력 계약 공급자.
    /// Update에서 장치 에지를 누적하고, 고정 스텝 소비자(L01_02+ 배선)가
    /// ConsumeFixedSnapshot으로 CharacterInputSnapshot을 읽어 간다.
    /// 이 컴포넌트는 값 공급만 한다 — 이동/스폰/피해/인벤토리/요청 소비 없음,
    /// 레거시 Input.GetKey 폴링 없음, 신규 ActionId 없음.
    /// </summary>
    public sealed class CharacterLiveInputSource : MonoBehaviour
    {
        private const string PlayerMapName = "Player";

        [SerializeField] private InputActionAsset actionsAsset;

        private readonly CharacterLiveInputAdapter adapter =
            new CharacterLiveInputAdapter();

        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction downAction;
        private InputAction jumpAction;
        private InputAction actionAction;
        private InputAction bombAction;
        private InputAction ropeAction;

        /// <summary>L01_02 프리팹 배선용 공급자 표면.</summary>
        public CharacterLiveInputAdapter Adapter
        {
            get { return adapter; }
        }

        public bool IsReady
        {
            get { return playerMap != null; }
        }

        /// <summary>고정 스텝 소비: 누적 에지 포함 스냅샷 반환(에지 소거).</summary>
        public CharacterInputSnapshot ConsumeFixedSnapshot(long physicsTick)
        {
            return adapter.ConsumeFixedSnapshot(physicsTick);
        }

        private void Awake()
        {
            ResolveActions();
        }

        private void OnEnable()
        {
            if (playerMap == null)
            {
                ResolveActions();
            }

            if (playerMap != null)
            {
                playerMap.Enable();
            }
        }

        private void OnDisable()
        {
            if (playerMap != null)
            {
                playerMap.Disable();
            }

            adapter.Reset();
        }

        private void Update()
        {
            if (playerMap == null)
            {
                return;
            }

            adapter.AccumulateFrame(
                moveAction.ReadValue<float>(),
                downAction.IsPressed(),
                ReadButton(jumpAction),
                ReadButton(actionAction),
                ReadButton(bombAction),
                ReadButton(ropeAction));
        }

        private static CharacterLiveButtonFrame ReadButton(InputAction inputAction)
        {
            return new CharacterLiveButtonFrame(
                inputAction.WasPressedThisFrame(),
                inputAction.WasReleasedThisFrame(),
                inputAction.IsPressed());
        }

        private void ResolveActions()
        {
            if (actionsAsset == null)
            {
                Debug.LogWarning(
                    "CharacterLiveInputSource: actionsAsset이 비어 있다 — " +
                    "CharacterLiveControls.inputactions를 지정해야 한다.", this);
                return;
            }

            playerMap = actionsAsset.FindActionMap(PlayerMapName, false);
            if (playerMap == null)
            {
                Debug.LogWarning(
                    "CharacterLiveInputSource: '" + PlayerMapName +
                    "' 액션 맵을 찾지 못했다.", this);
                return;
            }

            moveAction = playerMap.FindAction("Move", true);
            downAction = playerMap.FindAction("Down", true);
            jumpAction = playerMap.FindAction("Jump", true);
            actionAction = playerMap.FindAction("Action", true);
            bombAction = playerMap.FindAction("Bomb", true);
            ropeAction = playerMap.FindAction("Rope", true);
        }
    }
}
