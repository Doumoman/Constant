#if LEGACY_DISABLED
using TMPro;
using UnityEngine;

namespace StarNight.Narrative.Presenters
{
    [DisallowMultipleComponent]
    public sealed class FieldBubblePresenter : NarrativeLinePresenterBase
    {
        protected override NarrativeMode SupportedMode => NarrativeMode.Bubble;
        protected override CanvasGroup Group => View?.BubbleGroup;
        protected override TMP_Text BodyText => View?.BubbleBody;
        protected override TMP_Text NameText => View?.BubbleName;
        protected override bool AutoAdvance => true;
        protected override float AutoAdvanceSeconds => 3f;

        protected override void ApplyCharacter(string characterId)
        {
            if (View?.BubblePanel == null)
            {
                return;
            }
            View.BubblePanel.color = Characters != null && Characters.TryGet(characterId, out CharacterPresentation character)
                ? new Color(character.bubbleColor.r, character.bubbleColor.g, character.bubbleColor.b, 0.94f)
                : new Color32(19, 39, 56, 235);
        }
    }
}

#endif
