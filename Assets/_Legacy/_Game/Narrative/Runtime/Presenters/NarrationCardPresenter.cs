#if LEGACY_DISABLED
using TMPro;
using UnityEngine;

namespace StarNight.Narrative.Presenters
{
    [DisallowMultipleComponent]
    public sealed class NarrationCardPresenter : NarrativeLinePresenterBase
    {
        protected override NarrativeMode SupportedMode => NarrativeMode.Narration;
        protected override CanvasGroup Group => View?.NarrationGroup;
        protected override TMP_Text BodyText => View?.NarrationBody;
    }
}

#endif
