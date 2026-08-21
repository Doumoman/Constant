#if LEGACY_DISABLED
using System;
using System.Collections;
using System.Collections.Generic;
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using StarNight.Stage.Lab;
using StarNight.Stage.Rooms;
using StarNight.Stage.Streaming;
using StarNight.Stage.Transitions;
using StarNight.Stage.Validation;
using StarNight.Stage.Visuals;
using UnityEngine;

namespace StarNight.Stage.Secrets
{
    [DisallowMultipleComponent]
    public sealed class SecretDimensionController : MonoBehaviour
    {
        public const float FadeOutSeconds = 0.12f;
        public const float FadeInSeconds = 0.18f;

        private sealed class SecretRecord
        {
            public SecretAnchor Anchor;
            public RoomRuntime Room;
            public RoomPortal2D MainPortal;
            public RoomPortal2D ReturnPortal;
        }

        private readonly Dictionary<string, SecretRecord> records = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SecretDimensionPlan> plans = new(StringComparer.Ordinal);
        private Core04TwoRoomLab lab;
        private RoomTransitionController transitionController;
        private RoomStreamingManager streamingManager;
        private PlayerActionLock actionLock;

        public event Action<SecretAnchor, RoomRuntime> SecretRoomCreated;

        public bool IsTransitioning { get; private set; }
        public int SecretRoomCount => records.Count;
        public int PlannedSecretCount => plans.Count;
        public bool TimeContinues => SecretDimensionRuntimeContract.BellAndMaruTimeContinues;

        public void Configure(
            Core04TwoRoomLab configuredLab,
            RoomTransitionController transitions,
            RoomStreamingManager streaming)
        {
            lab = configuredLab;
            transitionController = transitions;
            streamingManager = streaming;
            actionLock = transitionController?.PlayerMotor != null
                ? transitionController.PlayerMotor.GetComponent<PlayerActionLock>()
                : null;
        }

        public bool RegisterPlan(SecretAnchor anchor)
        {
            if (anchor == null || string.IsNullOrWhiteSpace(anchor.AnchorStableId) ||
                string.IsNullOrWhiteSpace(anchor.SecretRoomId))
            {
                return false;
            }

            var plan = new SecretDimensionPlan(anchor.SecretRoomId, anchor.StableSecretSeed);
            if (plans.TryGetValue(anchor.AnchorStableId, out SecretDimensionPlan existing))
            {
                return existing.Equals(plan);
            }
            plans.Add(anchor.AnchorStableId, plan);
            return true;
        }

        public bool TryGetPlan(string anchorStableId, out SecretDimensionPlan plan)
        {
            return plans.TryGetValue(anchorStableId ?? string.Empty, out plan);
        }

        public bool CanMaruEnterSecret(string anchorStableId)
        {
            return records.TryGetValue(anchorStableId ?? string.Empty, out SecretRecord record)
                && record.Anchor != null
                && record.Anchor.IsRevealed;
        }

        public bool Reveal(SecretAnchor anchor)
        {
            if (anchor == null || anchor.SourceRoom == null || string.IsNullOrWhiteSpace(anchor.AnchorStableId))
            {
                return false;
            }
            if (!RegisterPlan(anchor))
            {
                return false;
            }
            if (records.TryGetValue(anchor.AnchorStableId, out SecretRecord existing))
            {
                anchor.SetRevealed(existing.MainPortal);
                return true;
            }

            Transform parent = lab?.RuntimeRoot != null ? lab.RuntimeRoot : transform;
            float offset = 10000f + Mathf.Abs(anchor.StableSecretSeed % 1000);
            RoomRuntime secretRoom = Core04TwoRoomLab.BuildPrototypeRoom(
                parent,
                anchor.SecretRoomId,
                new Vector2(offset, -10000f),
                new Color(0.025f, 0.035f, 0.12f, 1f),
                true,
                false,
                out RoomPortal2D returnPortal);
            secretRoom.SetDimension(RoomDimension.Secret);
            RoomGeometryValidator.ValidateAndApply(secretRoom);
            RoomPortal2D mainPortal = BuildMainPortal(anchor);
            mainPortal.Link(returnPortal);
            returnPortal.Link(mainPortal);
            mainPortal.Bind(transitionController);
            returnPortal.Bind(transitionController);

            AttachInteraction(mainPortal, "별문 들어가기", anchor.StableSecretSeed);
            AttachInteraction(returnPortal, "원래 방으로 돌아가기", anchor.StableSecretSeed ^ 0x5f3759df);

            var plan = new RoomStreamPlan(
                secretRoom.RoomId,
                anchor.StableSecretSeed,
                new[] { anchor.SourceRoom.RoomId },
                () => secretRoom);
            if (streamingManager != null)
            {
                streamingManager.RegisterPlan(plan, secretRoom);
                streamingManager.RequestWarmLoad(secretRoom.RoomId);
            }
            else
            {
                secretRoom.SetSimulationState(RoomSimulationState.NeighborPreview);
            }

            var record = new SecretRecord
            {
                Anchor = anchor,
                Room = secretRoom,
                MainPortal = mainPortal,
                ReturnPortal = returnPortal,
            };
            records.Add(anchor.AnchorStableId, record);
            anchor.SetRevealed(mainPortal);
            SecretRoomCreated?.Invoke(anchor, secretRoom);
            return true;
        }

        public bool TryUsePortal(RoomPortal2D portal)
        {
            if (IsTransitioning || portal == null || !portal.IsReady || transitionController == null)
            {
                return false;
            }
            StartCoroutine(DimensionTransitionRoutine(portal));
            return true;
        }

        public bool TryGetSecretRoom(string anchorStableId, out RoomRuntime room)
        {
            room = records.TryGetValue(anchorStableId ?? string.Empty, out SecretRecord record)
                ? record.Room
                : null;
            return room != null;
        }

        private IEnumerator DimensionTransitionRoutine(RoomPortal2D portal)
        {
            bool isReturnPortal = TryGetReturnRecord(portal, out SecretRecord returnRecord);
            IsTransitioning = true;
            transitionController.SetExternalBlock(true);
            actionLock?.SetState(PlayerActionState.RoomTransitionLocked);
            transitionController.PlayerMotor?.ClearBufferedInput();
            yield return new WaitForSecondsRealtime(FadeOutSeconds);

            transitionController.SetExternalBlock(false);
            actionLock?.ResetToFree();
            bool committed = transitionController.CommitImmediate(
                portal,
                isReturnPortal ? returnRecord.Anchor.SourceRecoveryRack : null);
            if (!committed)
            {
                IsTransitioning = false;
                yield break;
            }
            if (isReturnPortal && transitionController.PlayerMotor != null)
            {
                SecretReturnMaruBiteImmunity immunity =
                    transitionController.PlayerMotor.GetComponent<SecretReturnMaruBiteImmunity>();
                if (immunity == null)
                {
                    immunity = transitionController.PlayerMotor.gameObject
                        .AddComponent<SecretReturnMaruBiteImmunity>();
                }
                immunity.Grant();
            }

            transitionController.SetExternalBlock(true);
            actionLock?.SetState(PlayerActionState.RoomTransitionLocked);
            yield return new WaitForSecondsRealtime(FadeInSeconds);
            transitionController.SetExternalBlock(false);
            actionLock?.ResetToFree();
            IsTransitioning = false;
        }

        private bool TryGetReturnRecord(RoomPortal2D portal, out SecretRecord returnRecord)
        {
            foreach (SecretRecord record in records.Values)
            {
                if (record.ReturnPortal == portal)
                {
                    returnRecord = record;
                    return true;
                }
            }
            returnRecord = null;
            return false;
        }

        private RoomPortal2D BuildMainPortal(SecretAnchor anchor)
        {
            GameObject portalObject = new GameObject("SecretPortal_" + anchor.AnchorStableId);
            portalObject.transform.SetParent(anchor.SourceRoom.PortalRoot, true);
            portalObject.transform.position = anchor.transform.position;
            RoomPortal2D portal = portalObject.AddComponent<RoomPortal2D>();

            Transform preview = CreateNode(portalObject.transform, "NeighborLoadLine");
            Transform commit = CreateNode(portalObject.transform, "CommitLine");
            Transform boundary = CreateNode(portalObject.transform, "PortalBoundary");
            boundary.localScale = new Vector3(0.15f, 2.5f, 1f);
            Transform safeFloor = CreateNode(portalObject.transform, "EntrySafeFloor");
            safeFloor.position = anchor.ReturnSafeCell != null
                ? anchor.ReturnSafeCell.position + Vector3.down * 0.5f
                : anchor.SourceRoom.GetPrimarySafePosition() + Vector2.down * 0.5f;
            safeFloor.localScale = new Vector3(RoomPortalContract.EntrySafeFloorWidthCells, 0.1f, 1f);
            Transform clearZoneObject = CreateNode(portalObject.transform, "GameplayClearZone");
            clearZoneObject.position = anchor.ReturnSafeCell != null
                ? anchor.ReturnSafeCell.position
                : anchor.SourceRoom.GetPrimarySafePosition();
            clearZoneObject.gameObject.AddComponent<GameplayClearZone>()
                .Configure(new Vector2(RoomPortalContract.PortalPaddingCells, 3f));
            portal.Configure(
                "SECRET_" + anchor.AnchorStableId,
                CardinalDirection.Right,
                1,
                anchor.SourceRoom,
                anchor.ReturnSafeCell,
                preview,
                commit,
                boundary,
                safeFloor);
            return portal;
        }

        private void AttachInteraction(RoomPortal2D portal, string prompt, int stableId)
        {
            GameObject interactionObject = new GameObject("Interaction");
            interactionObject.layer = LayerMask.NameToLayer("Interaction");
            interactionObject.transform.SetParent(portal.transform, true);
            interactionObject.transform.position = portal.EntryAnchor != null
                ? portal.EntryAnchor.position
                : portal.transform.position;
            BoxCollider2D collider = interactionObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 2f);
            collider.isTrigger = true;
            InteractionCandidate candidate = interactionObject.AddComponent<InteractionCandidate>();
            candidate.Configure(InteractionTargetKind.Mechanism, stableId & int.MaxValue, "별문", prompt);
            SecretDimensionPortal interaction = interactionObject.AddComponent<SecretDimensionPortal>();
            interaction.Configure(portal, this, prompt);
        }

        private static Transform CreateNode(Transform parent, string name)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            return node.transform;
        }
    }
}

#endif
