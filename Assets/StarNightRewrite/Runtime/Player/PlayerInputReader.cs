using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.Rewrite.Player
{
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction interactAction;
        private InputAction useHandToolAction;
        private InputAction useRopeAction;
        private InputAction useBombAction;
        private InputAction pauseAction;
        private float moveX;

        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action InteractPressed;
        public event Action UseHandToolPressed;
        public event Action UseRopePressed;
        public event Action UseBombPressed;
        public event Action PausePressed;

        public float MoveX => moveX;

        private void Awake()
        {
            EnsureActions();
        }

        private void OnEnable()
        {
            EnsureActions();
            playerMap?.Enable();
        }

        private void OnDisable()
        {
            playerMap?.Disable();
            moveX = 0f;
        }

        public bool ApplyBindingOverride(string actionName, int bindingIndex, string controlPath)
        {
            EnsureActions();
            InputAction action = playerMap?.FindAction(actionName, false);
            if (action == null ||
                bindingIndex < 0 ||
                bindingIndex >= action.bindings.Count ||
                string.IsNullOrWhiteSpace(controlPath))
            {
                return false;
            }

            action.ApplyBindingOverride(bindingIndex, controlPath);
            return true;
        }

        public void RemoveAllBindingOverrides()
        {
            EnsureActions();
            foreach (InputAction action in playerMap)
            {
                action.RemoveAllBindingOverrides();
            }
        }

        private void EnsureActions()
        {
            if (playerMap != null)
            {
                return;
            }

            playerMap = new InputActionMap("Player");

            moveAction = playerMap.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick/x");
            moveAction.AddBinding("<Gamepad>/dpad/x");
            moveAction.performed += context =>
                moveX = context.ReadValue<float>();
            moveAction.canceled += _ => moveX = 0f;

            jumpAction = AddButton("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            interactAction = AddButton("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            useHandToolAction = AddButton("UseHandTool", "<Keyboard>/j", "<Gamepad>/buttonNorth");
            useRopeAction = AddButton("UseRope", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            useBombAction = AddButton("UseBomb", "<Keyboard>/r", "<Gamepad>/rightShoulder");
            pauseAction = AddButton("Pause", "<Keyboard>/escape", "<Gamepad>/start");

            jumpAction.started += _ => JumpPressed?.Invoke();
            jumpAction.canceled += _ => JumpReleased?.Invoke();
            interactAction.performed += _ => InteractPressed?.Invoke();
            useHandToolAction.performed += _ => UseHandToolPressed?.Invoke();
            useRopeAction.performed += _ => UseRopePressed?.Invoke();
            useBombAction.performed += _ => UseBombPressed?.Invoke();
            pauseAction.performed += _ => PausePressed?.Invoke();

            if (isActiveAndEnabled)
            {
                playerMap.Enable();
            }
        }

        private InputAction AddButton(string name, string keyboardPath, string gamepadPath)
        {
            InputAction action = playerMap.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboardPath);
            action.AddBinding(gamepadPath);
            return action;
        }
    }
}
