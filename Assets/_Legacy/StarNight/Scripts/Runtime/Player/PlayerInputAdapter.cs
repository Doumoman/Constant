#if LEGACY_DISABLED
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string useHeldToolActionName = "UseHeldTool";
        [SerializeField] private string useRopeActionName = "UseRope";
        [SerializeField] private string useBombActionName = "UseBomb";
        [SerializeField] private string aimActionName = "Aim";

        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction interactAction;
        private InputAction useHeldToolAction;
        private InputAction useRopeAction;
        private InputAction useBombAction;
        private InputAction aimAction;
        private bool jumpPressed;
        private bool interactPressed;
        private bool useHeldToolPressed;
        private bool useRopePressed;
        private bool useBombPressed;
        private bool testMode;
        private Vector2 testMove;
        private Vector2 testAim;
        private bool testJumpHeld;
        private bool testInteractHeld;
        private bool testUseHeldToolHeld;
        private bool gameplaySuppressed;
        private bool interactAllowedWhileSuppressed;

        public Vector2 Move => gameplaySuppressed
            ? Vector2.zero
            : testMode
            ? testMove
            : moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        public bool JumpHeld => !gameplaySuppressed && (testMode
            ? testJumpHeld
            : jumpAction != null && jumpAction.IsPressed());

        public bool InteractHeld =>
            (!gameplaySuppressed || interactAllowedWhileSuppressed)
            && (testMode
            ? testInteractHeld
            : interactAction != null && interactAction.IsPressed());

        public bool UseHeldToolHeld => !gameplaySuppressed && (testMode
            ? testUseHeldToolHeld
            : useHeldToolAction != null && useHeldToolAction.IsPressed());

        public Vector2 Aim => gameplaySuppressed
            ? Vector2.zero
            : testMode
            ? testAim
            : aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;

        public bool GameplaySuppressed => gameplaySuppressed;

        public void Configure(InputActionAsset actions, string mapName = "Gameplay")
        {
            inputActions = actions;
            actionMapName = mapName;
            ResolveActions();
        }

        private void OnEnable()
        {
            ResolveActions();
            gameplayMap?.Enable();
        }

        private void OnDisable()
        {
            gameplayMap?.Disable();
            jumpPressed = false;
            interactPressed = false;
            useHeldToolPressed = false;
            useRopePressed = false;
            useBombPressed = false;
            testInteractHeld = false;
            gameplaySuppressed = false;
            interactAllowedWhileSuppressed = false;
        }

        private void Update()
        {
            if (testMode)
            {
                return;
            }

            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                jumpPressed = true;
            }

            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                interactPressed = true;
            }

            if (useHeldToolAction != null && useHeldToolAction.WasPressedThisFrame())
            {
                useHeldToolPressed = true;
            }

            if (useRopeAction != null && useRopeAction.WasPressedThisFrame())
            {
                useRopePressed = true;
            }

            if (useBombAction != null && useBombAction.WasPressedThisFrame())
            {
                useBombPressed = true;
            }
        }

        public bool ConsumeJumpPressed()
        {
            if (gameplaySuppressed)
            {
                jumpPressed = false;
                return false;
            }

            bool value = jumpPressed;
            jumpPressed = false;
            return value;
        }

        public bool ConsumeInteractPressed()
        {
            if (gameplaySuppressed && !interactAllowedWhileSuppressed)
            {
                interactPressed = false;
                return false;
            }

            bool value = interactPressed;
            interactPressed = false;
            return value;
        }

        public bool ConsumeUseHeldToolPressed()
        {
            if (gameplaySuppressed)
            {
                useHeldToolPressed = false;
                return false;
            }

            bool value = useHeldToolPressed;
            useHeldToolPressed = false;
            return value;
        }

        public bool ConsumeUseRopePressed()
        {
            if (gameplaySuppressed)
            {
                useRopePressed = false;
                return false;
            }

            bool value = useRopePressed;
            useRopePressed = false;
            return value;
        }

        public bool ConsumeUseBombPressed()
        {
            if (gameplaySuppressed)
            {
                useBombPressed = false;
                return false;
            }

            bool value = useBombPressed;
            useBombPressed = false;
            return value;
        }

        public void EnableTestInput(bool enabled)
        {
            testMode = enabled;
            jumpPressed = false;
            interactPressed = false;
            useHeldToolPressed = false;
            useRopePressed = false;
            useBombPressed = false;
            if (!enabled)
            {
                testMove = Vector2.zero;
                testAim = Vector2.zero;
                testJumpHeld = false;
                testInteractHeld = false;
                testUseHeldToolHeld = false;
            }
        }

        public void SetGameplaySuppressed(
            bool suppressed,
            bool allowInteract = false)
        {
            gameplaySuppressed = suppressed;
            interactAllowedWhileSuppressed = suppressed && allowInteract;
            if (!suppressed)
            {
                return;
            }

            jumpPressed = false;
            interactPressed = false;
            useHeldToolPressed = false;
            useRopePressed = false;
            useBombPressed = false;
        }

        public void SetMoveForTests(Vector2 move)
        {
            testMode = true;
            testMove = Vector2.ClampMagnitude(move, 1f);
        }

        public void PressJumpForTests()
        {
            testMode = true;
            testJumpHeld = true;
            jumpPressed = true;
        }

        public void ReleaseJumpForTests()
        {
            testMode = true;
            testJumpHeld = false;
        }

        public void PressInteractForTests()
        {
            testMode = true;
            testInteractHeld = true;
            interactPressed = true;
        }

        public void ReleaseInteractForTests()
        {
            testMode = true;
            testInteractHeld = false;
        }

        public void PressUseHeldToolForTests()
        {
            testMode = true;
            testUseHeldToolHeld = true;
            useHeldToolPressed = true;
        }

        public void ReleaseUseHeldToolForTests()
        {
            testMode = true;
            testUseHeldToolHeld = false;
        }

        public void PressUseRopeForTests()
        {
            testMode = true;
            useRopePressed = true;
        }

        public void PressUseBombForTests()
        {
            testMode = true;
            useBombPressed = true;
        }

        public void SetAimForTests(Vector2 aim)
        {
            testMode = true;
            testAim = aim;
        }

        private void ResolveActions()
        {
            if (inputActions == null)
            {
                gameplayMap = null;
                moveAction = null;
                jumpAction = null;
                interactAction = null;
                useHeldToolAction = null;
                useRopeAction = null;
                useBombAction = null;
                aimAction = null;
                return;
            }

            gameplayMap = inputActions.FindActionMap(actionMapName, true);
            moveAction = gameplayMap.FindAction(moveActionName, true);
            jumpAction = gameplayMap.FindAction(jumpActionName, true);
            interactAction = gameplayMap.FindAction(interactActionName, false);
            useHeldToolAction = gameplayMap.FindAction(useHeldToolActionName, false);
            useRopeAction = gameplayMap.FindAction(useRopeActionName, false);
            useBombAction = gameplayMap.FindAction(useBombActionName, false);
            aimAction = gameplayMap.FindAction(aimActionName, false);
        }
    }
}

#endif
