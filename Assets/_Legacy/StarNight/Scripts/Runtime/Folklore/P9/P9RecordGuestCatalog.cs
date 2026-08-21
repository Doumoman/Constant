#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [Serializable]
    public sealed class P9RecordGuestDefinition
    {
        [SerializeField] private string guestId;
        [SerializeField] private string displayName;
        [SerializeField] private RoomRegion region;
        [SerializeField] private string symbolicMotif;
        [SerializeField] private P9RecordGuestImmediateSupport immediateSupport;
        [SerializeField] private P9RecordGuestNextStageSupport nextStageSupport;
        [SerializeField] private string helpSentence;
        [SerializeField] private bool requiresCulturalReview = true;

        public string GuestId => guestId;
        public string DisplayName => displayName;
        public RoomRegion Region => region;
        public string SymbolicMotif => symbolicMotif;
        public P9RecordGuestImmediateSupport ImmediateSupport =>
            immediateSupport;
        public P9RecordGuestNextStageSupport NextStageSupport =>
            nextStageSupport;
        public string HelpSentence => helpSentence;
        public bool RequiresCulturalReview => requiresCulturalReview;

        public void Configure(
            string id,
            string guestName,
            RoomRegion guestRegion,
            string motif,
            P9RecordGuestImmediateSupport immediate,
            P9RecordGuestNextStageSupport nextStage,
            string oneSentenceHelp,
            bool culturalReviewRequired = true)
        {
            guestId = id ?? string.Empty;
            displayName = guestName ?? string.Empty;
            region = guestRegion;
            symbolicMotif = motif ?? string.Empty;
            immediateSupport = immediate;
            nextStageSupport = nextStage;
            helpSentence = oneSentenceHelp ?? string.Empty;
            requiresCulturalReview = culturalReviewRequired;
        }
    }

    [CreateAssetMenu(
        menuName = "StarNight/P9/Record Guest Catalog",
        fileName = "P9_RecordGuestCatalog")]
    public sealed class P9RecordGuestCatalog : ScriptableObject
    {
        [SerializeField] private P9RecordGuestDefinition[] definitions =
            Array.Empty<P9RecordGuestDefinition>();

        public IReadOnlyList<P9RecordGuestDefinition> Definitions =>
            definitions;

        public void Configure(P9RecordGuestDefinition[] guestDefinitions)
        {
            definitions = guestDefinitions
                ?? Array.Empty<P9RecordGuestDefinition>();
        }

        public P9RecordGuestDefinition FindForRegion(RoomRegion region)
        {
            for (int index = 0; index < definitions.Length; index++)
            {
                P9RecordGuestDefinition definition = definitions[index];
                if (definition != null && definition.Region == region)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}

#endif
