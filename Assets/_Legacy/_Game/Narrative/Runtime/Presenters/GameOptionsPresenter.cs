#if LEGACY_DISABLED
using TMPro;
using UnityEngine;
using Yarn.Unity;

#nullable enable

namespace StarNight.Narrative.Presenters
{
    [DisallowMultipleComponent]
    public sealed class GameOptionsPresenter : DialoguePresenterBase
    {
        private static readonly Color32 Normal = new(239, 235, 216, 255);
        private static readonly Color32 Selected = new(239, 205, 118, 255);
        private NarrativeViewLayout? view;
        private NarrativeUIState? state;
        private DialogueOption[]? visibleOptions;
        private int selectedIndex;
        private bool selectionMade;
        private DialogueOption? selectedOption;

        public int SelectedIndex => selectedIndex;
        public int VisibleOptionCount => visibleOptions?.Length ?? 0;

        public void Configure(NarrativeViewLayout layout, NarrativeUIState uiState)
        {
            view = layout;
            state = uiState;
        }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync()
        {
            Hide();
            return YarnTask.CompletedTask;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token) => YarnTask.CompletedTask;

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            if (view?.OptionLabels == null)
            {
                return null;
            }

            int availableCount = 0;
            for (int index = 0; index < dialogueOptions.Length; index++)
            {
                if (dialogueOptions[index].IsAvailable) availableCount++;
            }
            if (availableCount == 0)
            {
                return null;
            }

            visibleOptions = new DialogueOption[Mathf.Min(availableCount, view.OptionLabels.Length)];
            int target = 0;
            for (int index = 0; index < dialogueOptions.Length && target < visibleOptions.Length; index++)
            {
                if (!dialogueOptions[index].IsAvailable) continue;
                visibleOptions[target++] = dialogueOptions[index];
            }

            selectedIndex = 0;
            selectionMade = false;
            selectedOption = null;
            for (int index = 0; index < view.OptionLabels.Length; index++)
            {
                TMP_Text label = view.OptionLabels[index];
                bool visible = index < visibleOptions.Length;
                label.gameObject.SetActive(visible);
                label.text = visible ? visibleOptions[index].Line.TextWithoutCharacterName.Text : string.Empty;
            }
            RefreshSelection();
            state.SetHasOptions(true);
            NarrativeViewLayout.SetVisible(view.OptionsGroup, true);

            while (!selectionMade && !cancellationToken.IsNextContentRequested)
            {
                await YarnTask.Yield();
            }

            DialogueOption? result = selectionMade ? selectedOption : null;
            Hide();
            return result;
        }

        public void Move(int direction)
        {
            DialogueOption[]? currentOptions = visibleOptions;
            if (currentOptions == null || currentOptions.Length == 0 || direction == 0)
            {
                return;
            }
            selectedIndex = (selectedIndex + (direction > 0 ? -1 : 1) + currentOptions.Length) % currentOptions.Length;
            RefreshSelection();
        }

        public void Confirm()
        {
            if (visibleOptions == null || visibleOptions.Length == 0)
            {
                return;
            }
            selectedOption = visibleOptions[selectedIndex];
            selectionMade = true;
        }

        private void RefreshSelection()
        {
            if (view?.OptionLabels == null) return;
            DialogueOption[]? currentOptions = visibleOptions;
            for (int index = 0; index < view.OptionLabels.Length; index++)
            {
                view.OptionLabels[index].color = index == selectedIndex ? Selected : Normal;
                if (currentOptions != null && index < currentOptions.Length)
                {
                    view.OptionLabels[index].text = (index == selectedIndex ? "▶  " : "    ") + currentOptions[index].Line.TextWithoutCharacterName.Text;
                }
            }
        }

        private void Hide()
        {
            if (state != null) state.SetHasOptions(false);
            if (view?.OptionsGroup != null) NarrativeViewLayout.SetVisible(view.OptionsGroup, false);
            visibleOptions = null;
            selectedOption = null;
            selectionMade = false;
        }
    }
}

#endif
