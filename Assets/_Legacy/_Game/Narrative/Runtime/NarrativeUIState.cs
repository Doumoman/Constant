#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Narrative
{
    [DisallowMultipleComponent]
    public sealed class NarrativeUIState : MonoBehaviour
    {
        public NarrativeMode Mode { get; private set; } = NarrativeMode.Conversation;
        public bool IsTypewriting { get; private set; }
        public bool HasOptions { get; private set; }
        public bool BlocksGameplay { get; private set; }
        public string CharacterId { get; private set; } = string.Empty;
        public string ExpressionId { get; private set; } = string.Empty;

        public void Begin(NarrativeMode mode, bool blocksGameplay)
        {
            Mode = mode;
            BlocksGameplay = blocksGameplay;
            IsTypewriting = false;
            HasOptions = false;
        }

        public void SetMode(NarrativeMode mode) => Mode = mode;
        public void SetTypewriting(bool value) => IsTypewriting = value;
        public void SetHasOptions(bool value) => HasOptions = value;

        public void SetExpression(string characterId, string expressionId)
        {
            CharacterId = characterId ?? string.Empty;
            ExpressionId = expressionId ?? string.Empty;
        }

        public void Clear()
        {
            IsTypewriting = false;
            HasOptions = false;
            BlocksGameplay = false;
        }
    }
}

#endif
