#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Folklore.P9;
using StarNight.Generation.P6;
using StarNight.Population.P7;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P9FolkloreRecordLabContract : MonoBehaviour
    {
        public const string ExpectedLabId =
            "P9_FolkloreChain_RecordGuests_Integrated_X2_v1";
        public const string CorridorReviewText =
            "P6 physical corridor module rhythm, readability, and room-to-"
            + "corridor transitions require a dedicated follow-up pass.";

        [Header("Identity")]
        [SerializeField] private string labId = ExpectedLabId;

        [Header("Inherited integrated systems")]
        [SerializeField] private P6RoomGraphLabContract graphContract;
        [SerializeField] private P7PopulationLabContract populationContract;
        [SerializeField] private P8MaruLabContract maruContract;

        [Header("Folklore chain")]
        [SerializeField] private P9FolkloreChainState2D chainState;
        [SerializeField] private P9FolkloreGiftPickup2D[] moonPalaceGifts =
            Array.Empty<P9FolkloreGiftPickup2D>();
        [SerializeField] private P9CorrespondenceEvent2D[] events =
            Array.Empty<P9CorrespondenceEvent2D>();
        [SerializeField] private P9BranchRelicPickup2D[] branchRelics =
            Array.Empty<P9BranchRelicPickup2D>();

        [Header("Record guest")]
        [SerializeField] private P9RecordGuestCatalog guestCatalog;
        [SerializeField] private P9StarArchive2D archive;
        [SerializeField] private P9RecordGuestDirector2D guestDirector;
        [SerializeField] private P9RecordGuestFollower2D guestFollower;
        [SerializeField] private int archiveNodeId = -1;

        [Header("Gate instrumentation")]
        [SerializeField] private P9ComprehensionTelemetry2D telemetry;
        [SerializeField] private bool corridorReviewPending = true;
        [SerializeField, TextArea(2, 5)] private string corridorReviewNote =
            CorridorReviewText;
        [SerializeField] private bool culturalReviewPending = true;

        [Header("Validation")]
        [SerializeField] private string[] issues = Array.Empty<string>();
        [SerializeField, TextArea(3, 16)] private string lastValidation =
            "Not validated.";

        public string LabId => labId;
        public P6RoomGraphLabContract GraphContract => graphContract;
        public P7PopulationLabContract PopulationContract =>
            populationContract;
        public P8MaruLabContract MaruContract => maruContract;
        public P9FolkloreChainState2D ChainState => chainState;
        public IReadOnlyList<P9FolkloreGiftPickup2D> MoonPalaceGifts =>
            moonPalaceGifts;
        public IReadOnlyList<P9CorrespondenceEvent2D> Events => events;
        public IReadOnlyList<P9BranchRelicPickup2D> BranchRelics =>
            branchRelics;
        public P9RecordGuestCatalog GuestCatalog => guestCatalog;
        public P9StarArchive2D Archive => archive;
        public P9RecordGuestDirector2D GuestDirector => guestDirector;
        public P9RecordGuestFollower2D GuestFollower => guestFollower;
        public int ArchiveNodeId => archiveNodeId;
        public P9ComprehensionTelemetry2D Telemetry => telemetry;
        public bool CorridorReviewPending => corridorReviewPending;
        public string CorridorReviewNote => corridorReviewNote;
        public bool CulturalReviewPending => culturalReviewPending;
        public IReadOnlyList<string> Issues => issues;
        public string LastValidation => lastValidation;
        public bool ValidationPassed =>
            issues.Length == 0 && lastValidation == "PASS";
        public bool HumanComprehensionGatesRequirePlaytest => true;

        public void Configure(
            P6RoomGraphLabContract graph,
            P7PopulationLabContract population,
            P8MaruLabContract maru,
            P9FolkloreChainState2D folkloreChain,
            P9FolkloreGiftPickup2D[] gifts,
            P9CorrespondenceEvent2D[] correspondenceEvents,
            P9BranchRelicPickup2D[] relics,
            P9RecordGuestCatalog catalog,
            P9StarArchive2D starArchive,
            P9RecordGuestDirector2D director,
            P9RecordGuestFollower2D follower,
            int recordRoomNodeId,
            P9ComprehensionTelemetry2D gateTelemetry)
        {
            labId = ExpectedLabId;
            graphContract = graph;
            populationContract = population;
            maruContract = maru;
            chainState = folkloreChain;
            moonPalaceGifts = gifts
                ?? Array.Empty<P9FolkloreGiftPickup2D>();
            events = correspondenceEvents
                ?? Array.Empty<P9CorrespondenceEvent2D>();
            branchRelics = relics
                ?? Array.Empty<P9BranchRelicPickup2D>();
            guestCatalog = catalog;
            archive = starArchive;
            guestDirector = director;
            guestFollower = follower;
            archiveNodeId = recordRoomNodeId;
            telemetry = gateTelemetry;
            corridorReviewPending = true;
            corridorReviewNote = CorridorReviewText;
            culturalReviewPending = true;
        }

        [ContextMenu("Validate P9 Folklore and Record Guest Lab")]
        public bool RefreshValidation()
        {
            List<string> found = new List<string>();
            ValidateInheritedSystems(found);
            ValidateFolkloreChain(found);
            ValidateRecordGuest(found);
            ValidateGatesAndFollowups(found);
            issues = found.ToArray();
            lastValidation = issues.Length == 0
                ? "PASS"
                : string.Join(Environment.NewLine, issues);
            return issues.Length == 0;
        }

        public void ValidateOrThrow()
        {
            if (!RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P9 integrated Lab validation failed:"
                    + Environment.NewLine
                    + lastValidation);
            }
        }

        private void ValidateInheritedSystems(List<string> found)
        {
            if (labId != ExpectedLabId)
            {
                found.Add("P9 Lab id does not match the fixed contract.");
            }

            if (graphContract == null || !graphContract.RefreshValidation())
            {
                found.Add("The inherited P6 graph contract is not valid.");
            }

            if (populationContract == null
                || !populationContract.RefreshValidation())
            {
                found.Add(
                    "The inherited P7 population contract is not valid.");
            }

            if (maruContract == null)
            {
                found.Add("The inherited P8 Maru contract is missing.");
            }
            else
            {
                try
                {
                    maruContract.ValidateOrThrow();
                }
                catch (Exception exception)
                {
                    found.Add(
                        "The inherited P8 Maru contract is not valid: "
                        + exception.Message);
                }
            }
        }

        private void ValidateFolkloreChain(List<string> found)
        {
            if (chainState == null)
            {
                found.Add("FolkloreChainManager state is missing.");
                return;
            }

            if (!chainState.HasBothMoonPalaceGifts)
            {
                found.Add(
                    "The P9 Lab must begin with both Moon Palace gifts.");
            }

            if (!chainState.MainProgressAlwaysAvailable
                || !chainState.OptionalEventsIgnoredWithoutPenalty)
            {
                found.Add(
                    "Ignoring folklore events must not block main progress.");
            }

            if (moonPalaceGifts == null || moonPalaceGifts.Length != 2)
            {
                found.Add("Exactly two Moon Palace gift pickups are required.");
            }
            else
            {
                HashSet<P9FolkloreItemKind> kinds =
                    new HashSet<P9FolkloreItemKind>();
                for (int index = 0; index < moonPalaceGifts.Length; index++)
                {
                    P9FolkloreGiftPickup2D gift = moonPalaceGifts[index];
                    if (gift == null
                        || !gift.ImportantItemCannotBePermanentlyLost)
                    {
                        found.Add(
                            "Moon Palace gifts require loss-proof pickup data.");
                        continue;
                    }

                    kinds.Add(gift.ItemKind);
                }

                if (!kinds.SetEquals(
                        new[]
                        {
                            P9FolkloreItemKind.MoonCake,
                            P9FolkloreItemKind.JadeRabbitMedicine
                        }))
                {
                    found.Add(
                        "Moon Palace gift kinds must be MoonCake and "
                        + "JadeRabbitMedicine.");
                }
            }

            if (events == null || events.Length != 2)
            {
                found.Add(
                    "Exactly two correspondence events are required.");
            }
            else
            {
                HashSet<P9CorrespondenceEventKind> eventKinds =
                    new HashSet<P9CorrespondenceEventKind>();
                for (int index = 0; index < events.Length; index++)
                {
                    P9CorrespondenceEvent2D stageEvent = events[index];
                    if (stageEvent == null)
                    {
                        found.Add("A correspondence event reference is null.");
                        continue;
                    }

                    eventKinds.Add(stageEvent.EventKind);
                    if (!stageEvent.AlternativeResolutionAvailable
                        || stageEvent.MainProgressBlocked
                        || !stageEvent.MatchingGiftCreatesAssistance
                        || !stageEvent.GiftPurposeInferenceReady)
                    {
                        found.Add(
                            $"{stageEvent.EventKind} lacks the optional, "
                            + "non-text correspondence contract.");
                    }
                }

                if (eventKinds.Count != 2)
                {
                    found.Add(
                        "Magpie and Turtle correspondence events must both "
                        + "be present.");
                }
            }

            if (branchRelics == null || branchRelics.Length != 2)
            {
                found.Add("Exactly two branch relic pickups are required.");
            }
            else
            {
                HashSet<P9BranchKind> branches = new HashSet<P9BranchKind>();
                for (int index = 0; index < branchRelics.Length; index++)
                {
                    P9BranchRelicPickup2D relic = branchRelics[index];
                    if (relic == null
                        || !relic.ImportantItemCannotBePermanentlyLost)
                    {
                        found.Add(
                            "Branch relics require loss-proof pickup data.");
                        continue;
                    }

                    branches.Add(relic.Branch);
                }

                if (!branches.SetEquals(
                        new[]
                        {
                            P9BranchKind.MagpieBridge,
                            P9BranchKind.DragonPalace
                        }))
                {
                    found.Add(
                        "Red thread and Dragon Palace orb branches must "
                        + "both be represented.");
                }
            }
        }

        private void ValidateRecordGuest(List<string> found)
        {
            if (guestCatalog == null
                || guestCatalog.Definitions == null
                || guestCatalog.Definitions.Count != 6)
            {
                found.Add(
                    "The Record Guest catalog must contain exactly six "
                    + "regional definitions.");
            }
            else
            {
                int uniqueIds = guestCatalog.Definitions
                    .Where(item => item != null)
                    .Select(item => item.GuestId)
                    .Distinct(StringComparer.Ordinal)
                    .Count();
                int uniqueRegions = guestCatalog.Definitions
                    .Where(item => item != null)
                    .Select(item => item.Region)
                    .Distinct()
                    .Count();
                if (uniqueIds != 6 || uniqueRegions != 6)
                {
                    found.Add(
                        "Record Guest ids and regions must be unique.");
                }

                if (guestCatalog.Definitions.Any(
                        item => item == null
                            || string.IsNullOrWhiteSpace(item.HelpSentence)
                            || !item.RequiresCulturalReview))
                {
                    found.Add(
                        "Every Record Guest requires a one-sentence help "
                        + "contract and cultural review marker.");
                }
            }

            if (archive == null
                || archive.UnlockMethodCount < 2
                || !archive.BombIsNotTheOnlySolution
                || !archive.MainRouteCueVisible
                || !archive.OpeningDoesNotGateExit)
            {
                found.Add(
                    "Star Archive requires a visible cue, two opening "
                    + "methods, and optional progression.");
            }

            if (graphContract != null)
            {
                bool hasRecordPlacement =
                    graphContract.Placements.Any(
                        item => item.NodeId == archiveNodeId
                            && (item.Role & RoomRole.RecordRoom) != 0);
                if (!hasRecordPlacement)
                {
                    found.Add(
                        "The Star Archive is not assigned to a RecordRoom "
                        + "node.");
                }
            }

            if (guestDirector == null
                || !guestDirector.ArchivePlaced
                || !guestDirector.HasAtMostOneArchive
                || guestDirector.ExitProgressBlocked
                || guestDirector.IgnoringArchiveHasPenalty
                || guestDirector.StageSlot != P6StageSlot.X2)
            {
                found.Add(
                    "RecordGuestDirector does not satisfy the X-2 optional "
                    + "placement contract.");
            }

            if (guestFollower == null
                || guestFollower.HasCombatAi
                || guestFollower.CanTakeDamage
                || guestFollower.ReceivesTerrainDamage
                || string.IsNullOrWhiteSpace(guestFollower.GuestId))
            {
                found.Add(
                    "The Record Guest follower must be non-combat, "
                    + "invulnerable, and data-backed.");
            }
        }

        private void ValidateGatesAndFollowups(List<string> found)
        {
            if (telemetry == null
                || !telemetry.InstrumentationReady
                || !Mathf.Approximately(
                    P9ComprehensionTelemetry2D.GiftInferenceTarget,
                    0.80f)
                || !Mathf.Approximately(
                    P9ComprehensionTelemetry2D.GuestHelpTarget,
                    0.85f))
            {
                found.Add(
                    "P9 comprehension telemetry targets are not wired.");
            }

            if (!corridorReviewPending
                || corridorReviewNote != CorridorReviewText)
            {
                found.Add(
                    "The requested P6 corridor follow-up review was not "
                    + "preserved.");
            }

            if (!culturalReviewPending)
            {
                found.Add(
                    "Historical/cultural character review must remain "
                    + "explicitly pending.");
            }
        }
    }
}

#endif
