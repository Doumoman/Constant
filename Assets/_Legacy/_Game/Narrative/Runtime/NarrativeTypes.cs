#if LEGACY_DISABLED
using System;

namespace StarNight.Narrative
{
    public enum NarrativeMode
    {
        Conversation,
        Bubble,
        Narration,
    }

    public readonly struct NarrativeRequest
    {
        public NarrativeRequest(string nodeName, NarrativeMode mode, bool blocksGameplay, bool essential)
        {
            NodeName = nodeName;
            Mode = mode;
            BlocksGameplay = blocksGameplay;
            Essential = essential;
        }

        public string NodeName { get; }
        public NarrativeMode Mode { get; }
        public bool BlocksGameplay { get; }
        public bool Essential { get; }
    }

    [Serializable]
    public sealed class NarrativeRequestEvent
    {
        public string Id;
        public string SecondaryId;
        public float Value;
    }
}

#endif
