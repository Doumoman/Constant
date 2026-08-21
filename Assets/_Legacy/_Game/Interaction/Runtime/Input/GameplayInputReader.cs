#if LEGACY_DISABLED
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.Interaction.Input
{
    public enum PlayerInputContext
    {
        Disabled,
        Gameplay,
        Dialogue,
        Menu,
    }

    [DisallowMultipleComponent]
    public sealed class GameplayInputReader : MonoBehaviour
    {
        public const string DefaultResourcePath = "Input/StarNightControls";

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PlayerInputContext initialContext = PlayerInputContext.Gameplay;

        private InputActionAsset runtimeActions;
        private InputActionMap gameplayMap;
        private InputActionMap dialogueMap;
        private InputActionMap menuMap;
        private InputAction moveHorizontal;
        private InputAction lookVertical;
        private InputAction jump;
        private InputAction primaryAction;
        private InputAction placeBomb;
        private InputAction placeRope;
        private InputAction selectNextItem;
        private InputAction selectPreviousItem;
        private InputAction openMap;
        private InputAction pause;
        private InputAction dialogueAdvance;
        private InputAction dialogueNavigate;
        private InputAction dialoguePause;
        private InputAction menuNavigate;
        private InputAction menuSubmit;
        private InputAction menuCancel;
        private int jumpPressedTokens;
        private int jumpReleasedTokens;
        private int primaryPressedTokens;
        private int bombPressedTokens;
        private int ropePressedTokens;
        private int selectNextPressedTokens;
        private int selectPreviousPressedTokens;
        private int mapPressedTokens;
        private int pausePressedTokens;
        private int dialogueAdvancePressedTokens;
        private int dialoguePausePressedTokens;
        private int menuSubmitPressedTokens;
        private int menuCancelPressedTokens;
        private bool initialized;

        public event Action PauseRequested;
        public event Action PrimaryActionPressed;

        public PlayerInputContext Context { get; private set; } = PlayerInputContext.Disabled;
        public InputActionAsset SourceAsset => inputActions;
        public InputActionAsset RuntimeActions => runtimeActions;
        public float MoveHorizontal => ReadAxis(moveHorizontal);
        public float LookVertical => ReadAxis(lookVertical);
        public bool JumpHeld => jump != null && jump.IsPressed();
        public bool PrimaryHeld => primaryAction != null && primaryAction.IsPressed();
        public float DialogueNavigate => ReadAxis(dialogueNavigate);
        public Vector2 MenuNavigate => ReadVector2(menuNavigate);

        private void Awake()
        {
            if (inputActions == null)
            {
                inputActions = Resources.Load<InputActionAsset>(DefaultResourcePath);
            }

            Initialize();
        }

        private void OnEnable()
        {
            if (Initialize())
            {
                SetContext(initialContext);
            }
        }

        private void OnDisable()
        {
            runtimeActions?.Disable();
            Context = PlayerInputContext.Disabled;
            ClearBufferedButtons();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (runtimeActions != null)
            {
                Destroy(runtimeActions);
            }
        }

        public void Configure(InputActionAsset asset, PlayerInputContext context = PlayerInputContext.Gameplay)
        {
            runtimeActions?.Disable();
            Unsubscribe();
            if (runtimeActions != null)
            {
                Destroy(runtimeActions);
            }

            inputActions = asset;
            runtimeActions = null;
            initialized = false;
            initialContext = context;
            if (Initialize() && isActiveAndEnabled)
            {
                SetContext(context);
            }
        }

        public void SetContext(PlayerInputContext context)
        {
            if (!Initialize())
            {
                Context = PlayerInputContext.Disabled;
                return;
            }

            runtimeActions.Disable();
            ClearBufferedButtons();
            Context = context;

            switch (context)
            {
                case PlayerInputContext.Gameplay:
                    gameplayMap.Enable();
                    break;
                case PlayerInputContext.Dialogue:
                    dialogueMap.Enable();
                    break;
                case PlayerInputContext.Menu:
                    menuMap.Enable();
                    break;
            }
        }

        public bool ConsumeJumpPressed() => Consume(ref jumpPressedTokens);
        public bool ConsumeJumpReleased() => Consume(ref jumpReleasedTokens);
        public bool ConsumePrimaryPressed() => Consume(ref primaryPressedTokens);
        public bool ConsumeBombPressed() => Consume(ref bombPressedTokens);
        public bool ConsumeRopePressed() => Consume(ref ropePressedTokens);
        public bool ConsumeSelectNextPressed() => Consume(ref selectNextPressedTokens);
        public bool ConsumeSelectPreviousPressed() => Consume(ref selectPreviousPressedTokens);
        public bool ConsumeMapPressed() => Consume(ref mapPressedTokens);
        public bool ConsumePausePressed() => Consume(ref pausePressedTokens);
        public bool ConsumeDialogueAdvancePressed() => Consume(ref dialogueAdvancePressedTokens);
        public bool ConsumeDialoguePausePressed() => Consume(ref dialoguePausePressedTokens);
        public bool ConsumeMenuSubmitPressed() => Consume(ref menuSubmitPressedTokens);
        public bool ConsumeMenuCancelPressed() => Consume(ref menuCancelPressedTokens);

        public bool ApplyBindingOverrides(string overridesJson)
        {
            if (!Initialize())
            {
                return false;
            }

            runtimeActions.RemoveAllBindingOverrides();
            if (string.IsNullOrWhiteSpace(overridesJson))
            {
                return true;
            }

            try
            {
                runtimeActions.LoadBindingOverridesFromJson(overridesJson);
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                runtimeActions.RemoveAllBindingOverrides();
                return false;
            }
        }

        public string SaveBindingOverrides()
        {
            return Initialize() ? runtimeActions.SaveBindingOverridesAsJson() : string.Empty;
        }

        public void ResetBindingOverrides()
        {
            if (Initialize())
            {
                runtimeActions.RemoveAllBindingOverrides();
            }
        }

        public int FindBindingIndex(string mapName, string actionName, string group, string partName = null)
        {
            if (!Initialize())
            {
                return -1;
            }

            InputAction action = runtimeActions.FindActionMap(mapName, false)?.FindAction(actionName, false);
            if (action == null)
            {
                return -1;
            }

            for (int index = 0; index < action.bindings.Count; index++)
            {
                InputBinding binding = action.bindings[index];
                if (binding.isComposite || !BindingHasGroup(binding, group))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(partName) && !string.Equals(binding.name, partName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                return index;
            }
            return -1;
        }

        public string GetBindingDisplayString(string mapName, string actionName, int bindingIndex)
        {
            if (!Initialize())
            {
                return "미지정";
            }

            InputAction action = runtimeActions.FindActionMap(mapName, false)?.FindAction(actionName, false);
            return action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count
                ? "미지정"
                : action.GetBindingDisplayString(bindingIndex);
        }

        public bool TryApplyBindingOverride(
            string mapName,
            string actionName,
            int bindingIndex,
            string controlPath,
            out string error)
        {
            error = string.Empty;
            if (!Initialize())
            {
                error = "입력 자산을 불러오지 못했습니다.";
                return false;
            }

            InputActionMap map = runtimeActions.FindActionMap(mapName, false);
            InputAction action = map?.FindAction(actionName, false);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                error = "리바인딩 대상을 찾지 못했습니다.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(controlPath))
            {
                error = "필수 액션은 비워 둘 수 없습니다.";
                return false;
            }

            InputBinding target = action.bindings[bindingIndex];
            if (string.Equals(target.effectivePath, controlPath, StringComparison.OrdinalIgnoreCase))
            {
                error = "현재 키를 다시 눌러 변경을 취소했습니다.";
                return false;
            }

            string group = FirstBindingGroup(target.groups);
            foreach (InputAction candidateAction in map.actions)
            {
                for (int index = 0; index < candidateAction.bindings.Count; index++)
                {
                    if (candidateAction == action && index == bindingIndex)
                    {
                        continue;
                    }
                    InputBinding candidate = candidateAction.bindings[index];
                    if (candidate.isComposite || !BindingHasGroup(candidate, group))
                    {
                        continue;
                    }
                    if (string.Equals(candidate.effectivePath, controlPath, StringComparison.OrdinalIgnoreCase))
                    {
                        error = $"{candidateAction.name} 액션과 키가 겹칩니다.";
                        return false;
                    }
                }
            }

            action.ApplyBindingOverride(bindingIndex, controlPath);
            return true;
        }

        public void ClearBufferedButtons()
        {
            jumpPressedTokens = 0;
            jumpReleasedTokens = 0;
            primaryPressedTokens = 0;
            bombPressedTokens = 0;
            ropePressedTokens = 0;
            selectNextPressedTokens = 0;
            selectPreviousPressedTokens = 0;
            mapPressedTokens = 0;
            pausePressedTokens = 0;
            dialogueAdvancePressedTokens = 0;
            dialoguePausePressedTokens = 0;
            menuSubmitPressedTokens = 0;
            menuCancelPressedTokens = 0;
        }

        private bool Initialize()
        {
            if (initialized)
            {
                return true;
            }

            if (inputActions == null)
            {
                return false;
            }

            runtimeActions = Instantiate(inputActions);
            gameplayMap = runtimeActions.FindActionMap("Gameplay", true);
            dialogueMap = runtimeActions.FindActionMap("Dialogue", true);
            menuMap = runtimeActions.FindActionMap("Menu", true);
            moveHorizontal = gameplayMap.FindAction("MoveHorizontal", true);
            lookVertical = gameplayMap.FindAction("LookVertical", true);
            jump = gameplayMap.FindAction("Jump", true);
            primaryAction = gameplayMap.FindAction("PrimaryAction", true);
            placeBomb = gameplayMap.FindAction("PlaceBomb", true);
            placeRope = gameplayMap.FindAction("PlaceRope", true);
            selectNextItem = gameplayMap.FindAction("SelectNextItem", true);
            selectPreviousItem = gameplayMap.FindAction("SelectPreviousItem", true);
            openMap = gameplayMap.FindAction("OpenMap", true);
            pause = gameplayMap.FindAction("Pause", true);
            dialogueAdvance = dialogueMap.FindAction("AdvanceDialogue", true);
            dialogueNavigate = dialogueMap.FindAction("Navigate", true);
            dialoguePause = dialogueMap.FindAction("Pause", true);
            menuNavigate = menuMap.FindAction("Navigate", true);
            menuSubmit = menuMap.FindAction("MenuSubmit", true);
            menuCancel = menuMap.FindAction("MenuCancel", true);
            Subscribe();
            initialized = true;
            return true;
        }

        private void Subscribe()
        {
            jump.performed += HandleJumpPerformed;
            jump.canceled += HandleJumpCanceled;
            primaryAction.performed += HandlePrimaryPerformed;
            placeBomb.performed += HandleBombPerformed;
            placeRope.performed += HandleRopePerformed;
            selectNextItem.performed += HandleSelectNextPerformed;
            selectPreviousItem.performed += HandleSelectPreviousPerformed;
            openMap.performed += HandleMapPerformed;
            pause.performed += HandlePausePerformed;
            dialogueAdvance.performed += HandleDialogueAdvancePerformed;
            dialoguePause.performed += HandleDialoguePausePerformed;
            menuSubmit.performed += HandleMenuSubmitPerformed;
            menuCancel.performed += HandleMenuCancelPerformed;
        }

        private void Unsubscribe()
        {
            if (!initialized)
            {
                return;
            }

            jump.performed -= HandleJumpPerformed;
            jump.canceled -= HandleJumpCanceled;
            primaryAction.performed -= HandlePrimaryPerformed;
            placeBomb.performed -= HandleBombPerformed;
            placeRope.performed -= HandleRopePerformed;
            selectNextItem.performed -= HandleSelectNextPerformed;
            selectPreviousItem.performed -= HandleSelectPreviousPerformed;
            openMap.performed -= HandleMapPerformed;
            pause.performed -= HandlePausePerformed;
            dialogueAdvance.performed -= HandleDialogueAdvancePerformed;
            dialoguePause.performed -= HandleDialoguePausePerformed;
            menuSubmit.performed -= HandleMenuSubmitPerformed;
            menuCancel.performed -= HandleMenuCancelPerformed;
            initialized = false;
        }

        private void HandleJumpPerformed(InputAction.CallbackContext context) => jumpPressedTokens++;
        private void HandleJumpCanceled(InputAction.CallbackContext context) => jumpReleasedTokens++;
        private void HandlePrimaryPerformed(InputAction.CallbackContext context)
        {
            primaryPressedTokens++;
            PrimaryActionPressed?.Invoke();
        }
        private void HandleBombPerformed(InputAction.CallbackContext context) => bombPressedTokens++;
        private void HandleRopePerformed(InputAction.CallbackContext context) => ropePressedTokens++;
        private void HandleSelectNextPerformed(InputAction.CallbackContext context)
        {
            if (Keyboard.current == null || !Keyboard.current.shiftKey.isPressed)
            {
                selectNextPressedTokens++;
            }
        }
        private void HandleSelectPreviousPerformed(InputAction.CallbackContext context) => selectPreviousPressedTokens++;
        private void HandleMapPerformed(InputAction.CallbackContext context) => mapPressedTokens++;
        private void HandlePausePerformed(InputAction.CallbackContext context)
        {
            pausePressedTokens++;
            PauseRequested?.Invoke();
        }
        private void HandleDialogueAdvancePerformed(InputAction.CallbackContext context) => dialogueAdvancePressedTokens++;
        private void HandleDialoguePausePerformed(InputAction.CallbackContext context)
        {
            dialoguePausePressedTokens++;
            PauseRequested?.Invoke();
        }
        private void HandleMenuSubmitPerformed(InputAction.CallbackContext context) => menuSubmitPressedTokens++;
        private void HandleMenuCancelPerformed(InputAction.CallbackContext context) => menuCancelPressedTokens++;

        private static float ReadAxis(InputAction action)
        {
            return action == null || !action.enabled ? 0f : Mathf.Clamp(action.ReadValue<float>(), -1f, 1f);
        }

        private static Vector2 ReadVector2(InputAction action)
        {
            return action == null || !action.enabled ? Vector2.zero : Vector2.ClampMagnitude(action.ReadValue<Vector2>(), 1f);
        }

        private static bool BindingHasGroup(InputBinding binding, string group)
        {
            if (string.IsNullOrEmpty(group))
            {
                return string.IsNullOrEmpty(binding.groups);
            }

            string[] groups = binding.groups.Split(';');
            for (int index = 0; index < groups.Length; index++)
            {
                if (string.Equals(groups[index], group, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string FirstBindingGroup(string groups)
        {
            return string.IsNullOrWhiteSpace(groups) ? string.Empty : groups.Split(';')[0];
        }

        private static bool Consume(ref int tokenCount)
        {
            if (tokenCount <= 0)
            {
                return false;
            }

            tokenCount--;
            return true;
        }
    }
}

#endif
