#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DisallowMultipleComponent]
    public sealed class P9RecordGuestFollower2D : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform guestVisual;
        [SerializeField] private string guestId;
        [SerializeField] private P9RecordGuestImmediateSupport immediateSupport;
        [SerializeField] private P9RecordGuestNextStageSupport nextStageSupport;
        [SerializeField] private Vector3 archivePosition;
        [SerializeField, Min(0.1f)] private float followSpeed = 5f;
        [SerializeField] private Vector2 followOffset = new Vector2(-0.8f, 0.7f);
        [SerializeField] private bool rescued;
        [SerializeField] private bool supportUsed;
        [SerializeField] private bool completedAtExit;

        public event Action<P9RecordGuestImmediateSupport> SupportUsed;
        public event Action ReturnedToArchive;

        public string GuestId => guestId;
        public P9RecordGuestImmediateSupport ImmediateSupport =>
            immediateSupport;
        public P9RecordGuestNextStageSupport NextStageSupport =>
            nextStageSupport;
        public bool IsRescued => rescued;
        public bool IsFollowing => rescued && !completedAtExit;
        public bool SupportAvailable => rescued && !supportUsed;
        public bool WasSupportUsed => supportUsed;
        public bool CompletedAtExit => completedAtExit;
        public bool HasCombatAi => false;
        public bool CanTakeDamage => false;
        public bool ReceivesTerrainDamage => false;

        public void Configure(
            P9RecordGuestDefinition definition,
            Transform target,
            Transform visual,
            Vector3 returnPosition,
            float speed = 5f)
        {
            guestId = definition != null
                ? definition.GuestId
                : string.Empty;
            immediateSupport = definition != null
                ? definition.ImmediateSupport
                : default;
            nextStageSupport = definition != null
                ? definition.NextStageSupport
                : default;
            followTarget = target;
            guestVisual = visual;
            archivePosition = returnPosition;
            followSpeed = Mathf.Max(0.1f, speed);
            rescued = false;
            supportUsed = false;
            completedAtExit = false;
            RefreshVisual();
        }

        public bool Rescue()
        {
            if (rescued || string.IsNullOrWhiteSpace(guestId))
            {
                return false;
            }

            rescued = true;
            supportUsed = false;
            completedAtExit = false;
            RefreshVisual();
            return true;
        }

        public bool TryUseSupport()
        {
            if (!SupportAvailable)
            {
                return false;
            }

            supportUsed = true;
            SupportUsed?.Invoke(immediateSupport);
            return true;
        }

        public void RejoinAfterRoomTransition(Vector3 roomEntryPosition)
        {
            if (!IsFollowing)
            {
                return;
            }

            transform.position =
                roomEntryPosition + (Vector3)followOffset;
        }

        public void ReturnToArchive()
        {
            rescued = false;
            completedAtExit = false;
            transform.position = archivePosition;
            RefreshVisual();
            ReturnedToArchive?.Invoke();
        }

        public bool CompleteAtExit()
        {
            if (!IsFollowing)
            {
                return false;
            }

            completedAtExit = true;
            rescued = false;
            RefreshVisual();
            return true;
        }

        private void Update()
        {
            if (!IsFollowing || followTarget == null)
            {
                return;
            }

            Vector3 destination =
                followTarget.position + (Vector3)followOffset;
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                followSpeed * Time.deltaTime);
        }

        private void RefreshVisual()
        {
            if (guestVisual != null)
            {
                guestVisual.gameObject.SetActive(
                    rescued && !completedAtExit);
            }
        }
    }
}

#endif
