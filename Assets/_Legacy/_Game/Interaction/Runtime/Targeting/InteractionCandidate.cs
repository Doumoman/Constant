#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Interaction.Targeting
{
    public enum InteractionTargetKind
    {
        ExitBeacon = 40,
        InspectObject = 50,
        Pickup = 60,
        Mechanism = 70,
        ShopCounter = 75,
        DialogueNpc = 80,
        OpenDepartureDoor = 90,
        SafetyRecovery = 95,
        RequiredHandSlotReceiver = 100,
    }

    [DisallowMultipleComponent]
    public sealed class InteractionCandidate : MonoBehaviour
    {
        [SerializeField] private InteractionTargetKind targetKind = InteractionTargetKind.InspectObject;
        [SerializeField] private int stableRuntimeId;
        [SerializeField] private bool available = true;
        [SerializeField] private Transform interactionAnchor;
        [SerializeField] private string displayName;
        [SerializeField] private string promptVerb;

        public InteractionTargetKind TargetKind => targetKind;
        public int Priority => (int)targetKind;
        public int StableRuntimeId => stableRuntimeId > 0
            ? stableRuntimeId
            : gameObject.GetInstanceID() & int.MaxValue;
        public bool Available => available;
        public Vector2 AnchorPosition => interactionAnchor != null
            ? interactionAnchor.position
            : transform.position;
        public string DisplayName => displayName ?? string.Empty;
        public string PromptVerb => promptVerb ?? string.Empty;

        public bool IsSelectable(ContextReceiverQuery query)
        {
            if (!available)
            {
                return false;
            }

            return targetKind != InteractionTargetKind.RequiredHandSlotReceiver
                || ContextReceiverResolver.Resolve(gameObject, query) != null;
        }

        public void SetAvailable(bool value)
        {
            available = value;
        }

        public void ConfigureForTests(
            InteractionTargetKind kind,
            int runtimeId,
            bool isAvailable = true)
        {
            targetKind = kind;
            stableRuntimeId = runtimeId;
            available = isAvailable;
        }

        public void Configure(
            InteractionTargetKind kind,
            int runtimeId,
            string candidateDisplayName,
            string candidatePromptVerb,
            bool isAvailable = true)
        {
            targetKind = kind;
            stableRuntimeId = runtimeId;
            displayName = candidateDisplayName ?? string.Empty;
            promptVerb = candidatePromptVerb ?? string.Empty;
            available = isAvailable;
        }
    }
}

#endif
