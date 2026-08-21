#if LEGACY_DISABLED
using System;
using StarNight.Core.Flow;
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace StarNight.Narrative.Presenters
{
    public abstract class NarrativeLinePresenterBase : DialoguePresenterBase
    {
        private const float DefaultCharactersPerSecond = 35f;
        protected NarrativeViewLayout View;
        protected NarrativeUIState State;
        protected CharacterDatabase Characters;

        protected abstract NarrativeMode SupportedMode { get; }
        protected abstract CanvasGroup Group { get; }
        protected abstract TMP_Text BodyText { get; }
        protected virtual TMP_Text NameText => null;
        protected virtual GameObject WaitGlyph => null;
        protected virtual bool AutoAdvance => false;
        protected virtual float AutoAdvanceSeconds => 3f;

        public void Configure(NarrativeViewLayout view, NarrativeUIState state, CharacterDatabase characters)
        {
            View = view;
            State = state;
            Characters = characters;
        }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync()
        {
            if (Group != null) NarrativeViewLayout.SetVisible(Group, false);
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (State == null || State.Mode != SupportedMode || BodyText == null || Group == null)
            {
                return;
            }

            string characterId = line.CharacterName ?? string.Empty;
            if (NameText != null)
            {
                NameText.text = Characters != null ? Characters.ResolveDisplayName(characterId) : characterId;
                NameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(NameText.text));
            }
            ApplyCharacter(characterId);

            BodyText.text = line.TextWithoutCharacterName.Text;
            BodyText.ForceMeshUpdate();
            int totalCharacters = BodyText.textInfo.characterCount;
            BodyText.maxVisibleCharacters = 0;
            if (WaitGlyph != null) WaitGlyph.SetActive(false);
            NarrativeViewLayout.SetVisible(Group, true);

            State.SetTypewriting(true);
            float visible = 0f;
            while (BodyText.maxVisibleCharacters < totalCharacters && !token.IsHurryUpRequested && !token.IsNextContentRequested)
            {
                if (Time.timeScale <= 0f)
                {
                    await YarnTask.Yield();
                    continue;
                }

                visible += ResolveCharactersPerSecond() * Time.unscaledDeltaTime;
                BodyText.maxVisibleCharacters = Mathf.Min(totalCharacters, Mathf.FloorToInt(visible));
                await YarnTask.Yield();
            }
            BodyText.maxVisibleCharacters = int.MaxValue;
            State.SetTypewriting(false);

            if (!token.IsNextContentRequested)
            {
                if (AutoAdvance || IsAutoAdvanceEnabled())
                {
                    float elapsed = 0f;
                    while (elapsed < AutoAdvanceSeconds && !token.IsNextContentRequested)
                    {
                        if (Time.timeScale > 0f)
                        {
                            elapsed += Time.unscaledDeltaTime;
                        }
                        await YarnTask.Yield();
                    }
                }
                else
                {
                    if (WaitGlyph != null) WaitGlyph.SetActive(true);
                    await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();
                }
            }

            if (WaitGlyph != null) WaitGlyph.SetActive(false);
            NarrativeViewLayout.SetVisible(Group, false);
        }

        protected virtual void ApplyCharacter(string characterId) { }

        private static float ResolveCharactersPerSecond()
        {
            return GameBootstrap.Instance?.Settings?.gameplay == null
                ? DefaultCharactersPerSecond
                : (float)GameBootstrap.Instance.Settings.gameplay.dialogueSpeed;
        }

        private static bool IsAutoAdvanceEnabled()
        {
            return GameBootstrap.Instance?.Settings?.gameplay?.autoAdvanceDialogue == true;
        }
    }
}

#endif
