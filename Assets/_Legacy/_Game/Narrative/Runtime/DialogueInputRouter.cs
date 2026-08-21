#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Input;
using StarNight.Narrative.Presenters;
using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

namespace StarNight.Narrative
{
    [DisallowMultipleComponent]
    public sealed class DialogueInputRouter : MonoBehaviour
    {
        private DialogueRunner runner;
        private NarrativeUIState state;
        private GameOptionsPresenter options;
        private GameplayInputReader inputReader;
        private int previousNavigateSign;

        public event Action PauseRequested;

        public void Configure(DialogueRunner dialogueRunner, NarrativeUIState uiState, GameOptionsPresenter optionPresenter)
        {
            runner = dialogueRunner ?? throw new ArgumentNullException(nameof(dialogueRunner));
            state = uiState ?? throw new ArgumentNullException(nameof(uiState));
            options = optionPresenter ?? throw new ArgumentNullException(nameof(optionPresenter));
        }

        private void Update()
        {
            if (runner == null || !runner.IsDialogueRunning)
            {
                previousNavigateSign = 0;
                return;
            }

            if (inputReader == null)
            {
                inputReader = FindFirstObjectByType<GameplayInputReader>();
            }
            if (inputReader == null || inputReader.Context != PlayerInputContext.Dialogue)
            {
                return;
            }

            int navigateSign = Mathf.Abs(inputReader.DialogueNavigate) > 0.5f ? Math.Sign(inputReader.DialogueNavigate) : 0;
            if (state.HasOptions && navigateSign != 0 && previousNavigateSign == 0)
            {
                options.Move(navigateSign);
            }
            previousNavigateSign = navigateSign;

            bool directAdvancePressed = Keyboard.current?.xKey.wasPressedThisFrame == true ||
                                        Gamepad.current?.buttonWest.wasPressedThisFrame == true;
            ProcessAdvanceInput(inputReader.ConsumeDialogueAdvancePressed(), directAdvancePressed);
            if (inputReader.ConsumeDialoguePausePressed())
            {
                PauseRequested?.Invoke();
            }
        }

        public void ProcessAdvance()
        {
            if (runner == null || !runner.IsDialogueRunning)
            {
                return;
            }
            if (state.HasOptions)
            {
                options.Confirm();
            }
            else if (state.IsTypewriting)
            {
                runner.RequestHurryUpLine();
            }
            else
            {
                runner.RequestNextLine();
            }
        }

        public bool ProcessAdvanceInput(bool actionMapPressed, bool directDevicePressed)
        {
            if (!actionMapPressed && !directDevicePressed)
            {
                return false;
            }

            ProcessAdvance();
            return true;
        }
    }
}

#endif
