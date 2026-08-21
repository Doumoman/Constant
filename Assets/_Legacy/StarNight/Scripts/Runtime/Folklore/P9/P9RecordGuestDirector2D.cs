#if LEGACY_DISABLED
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DisallowMultipleComponent]
    public sealed class P9RecordGuestDirector2D : MonoBehaviour
    {
        public const float OptionalStageArchiveChance = 0.15f;

        [SerializeField] private P9RecordGuestCatalog catalog;
        [SerializeField] private RoomRegion region;
        [SerializeField] private P6StageSlot stageSlot;
        [SerializeField, Range(0f, 1f)] private float placementRoll;
        [SerializeField] private P9StarArchive2D archive;
        [SerializeField] private P9RecordGuestFollower2D follower;
        [SerializeField] private string selectedGuestId;
        [SerializeField] private bool archivePlaced;
        [SerializeField] private bool nextStageSupportQueued;
        [SerializeField] private P9RecordGuestNextStageSupport queuedSupport;

        public P9RecordGuestCatalog Catalog => catalog;
        public RoomRegion Region => region;
        public P6StageSlot StageSlot => stageSlot;
        public P9StarArchive2D Archive => archive;
        public P9RecordGuestFollower2D Follower => follower;
        public string SelectedGuestId => selectedGuestId;
        public bool ArchivePlaced => archivePlaced;
        public bool HasAtMostOneArchive => archive == null || archivePlaced;
        public bool ExitProgressBlocked => false;
        public bool IgnoringArchiveHasPenalty => false;
        public bool NextStageSupportQueued => nextStageSupportQueued;
        public P9RecordGuestNextStageSupport QueuedSupport =>
            queuedSupport;

        public void Configure(
            P9RecordGuestCatalog guestCatalog,
            RoomRegion stageRegion,
            P6StageSlot slot,
            float deterministicPlacementRoll,
            P9StarArchive2D stageArchive,
            P9RecordGuestFollower2D stageFollower)
        {
            catalog = guestCatalog;
            region = stageRegion;
            stageSlot = slot;
            placementRoll = Mathf.Clamp01(deterministicPlacementRoll);
            archive = stageArchive;
            follower = stageFollower;
            archivePlaced = archive != null
                && ShouldPlaceArchive(stageSlot, placementRoll);
            nextStageSupportQueued = false;

            P9RecordGuestDefinition selected =
                catalog != null ? catalog.FindForRegion(region) : null;
            selectedGuestId = selected != null
                ? selected.GuestId
                : string.Empty;
        }

        public P9RecordGuestDefinition SelectedDefinition =>
            catalog != null ? catalog.FindForRegion(region) : null;

        public bool TryOpenArchiveAndRescue(
            P9ArchiveUnlockMethods method)
        {
            if (!archivePlaced
                || archive == null
                || follower == null
                || SelectedDefinition == null)
            {
                return false;
            }

            if (!archive.IsOpen && !archive.TryOpen(method))
            {
                return false;
            }

            return follower.Rescue();
        }

        public bool TryUseImmediateSupport()
        {
            return follower != null && follower.TryUseSupport();
        }

        public void NotifyRoomTransition(Vector3 roomEntryPosition)
        {
            follower?.RejoinAfterRoomTransition(roomEntryPosition);
        }

        public void NotifyMaruBite()
        {
            follower?.ReturnToArchive();
        }

        public bool NotifyExitReached()
        {
            if (follower != null && follower.CompleteAtExit())
            {
                nextStageSupportQueued = true;
                queuedSupport = follower.NextStageSupport;
            }

            return true;
        }

        public static bool ShouldPlaceArchive(
            P6StageSlot slot,
            float deterministicRoll)
        {
            return slot == P6StageSlot.X2
                || Mathf.Clamp01(deterministicRoll)
                    < OptionalStageArchiveChance;
        }
    }
}

#endif
