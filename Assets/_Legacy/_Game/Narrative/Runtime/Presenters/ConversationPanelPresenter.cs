#if LEGACY_DISABLED
using TMPro;
using UnityEngine;

namespace StarNight.Narrative.Presenters
{
    [DisallowMultipleComponent]
    public sealed class ConversationPanelPresenter : NarrativeLinePresenterBase
    {
        protected override NarrativeMode SupportedMode => NarrativeMode.Conversation;
        protected override CanvasGroup Group => View?.ConversationGroup;
        protected override TMP_Text BodyText => View?.ConversationBody;
        protected override TMP_Text NameText => View?.ConversationName;
        protected override GameObject WaitGlyph => View?.ConversationWait;

        protected override void ApplyCharacter(string characterId)
        {
            if (View?.ConversationPortrait == null)
            {
                return;
            }
            if (Characters != null && Characters.TryGet(characterId, out CharacterPresentation character) && character.portrait != null)
            {
                View.ConversationPortrait.sprite = character.portrait;
                View.ConversationPortrait.color = Color.white;
            }
            else
            {
                View.ConversationPortrait.sprite = null;
                View.ConversationPortrait.color = new Color32(24, 42, 62, 255);
            }
        }
    }
}

#endif
