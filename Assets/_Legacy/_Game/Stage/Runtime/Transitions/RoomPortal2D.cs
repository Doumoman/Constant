#if LEGACY_DISABLED
using StarNight.Interaction.Carry;
using StarNight.Map;
using StarNight.Player.Motor;
using StarNight.Player.Presentation;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Transitions
{
    [DisallowMultipleComponent]
    public sealed class RoomPortal2D : MonoBehaviour, ICarryPortalClearance
    {
        [SerializeField] private string portalId;
        [SerializeField] private CardinalDirection side;
        [SerializeField] private int floorHeightCell = 1;
        [SerializeField] private RoomRuntime owner;
        [SerializeField] private RoomPortal2D destinationPortal;
        [SerializeField] private Transform entryAnchor;
        [SerializeField] private Transform previewLine;
        [SerializeField] private Transform commitLine;
        [SerializeField] private Transform portalBoundary;
        [SerializeField] private Transform entrySafeFloor;
        [SerializeField] private bool streamingReady = true;

        private RoomTransitionController transitionController;
        private Collider2D streamingBoundaryCollider;
        private Transform loadingIndicator;
        private PortalFacade portalFacade;

        public string PortalId => portalId;
        public CardinalDirection Side => side;
        public int FloorHeightCell => floorHeightCell;
        public RoomRuntime Owner => owner;
        public RoomPortal2D DestinationPortal => destinationPortal;
        public RoomRuntime Destination => destinationPortal != null ? destinationPortal.owner : null;
        public Transform EntryAnchor => entryAnchor;
        public Transform PreviewLine => previewLine;
        public Transform CommitLine => commitLine;
        public Transform PortalBoundary => portalBoundary;
        public Transform EntrySafeFloor => entrySafeFloor;
        public bool HasDestination => Destination != null;
        public bool IsReady => streamingReady && HasDestination && Destination.IsInitialized && Destination.GeometryApproved;
        public bool StreamingReady => streamingReady;
        public bool HasProtectedSafeFloor => entrySafeFloor != null &&
                                                entrySafeFloor.GetComponent<PortalSafeFloorMarker>() != null;

        private void Update()
        {
            if (loadingIndicator != null && loadingIndicator.gameObject.activeSelf)
            {
                loadingIndicator.Rotate(0f, 0f, 180f * Time.unscaledDeltaTime);
            }
        }

        public bool Allows(CarryObjectDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }
            bool needsClearance = definition.WeightClass == CarryWeightClass.Heavy
                || definition.Footprint.y > 1;
            return !needsClearance || entryAnchor != null && entrySafeFloor != null;
        }

        public void Configure(
            string id,
            CardinalDirection portalSide,
            int portalFloorHeightCell,
            RoomRuntime owningRoom,
            Transform roomEntryAnchor,
            Transform roomPreviewLine,
            Transform roomCommitLine,
            Transform boundary,
            Transform safeFloor)
        {
            portalId = id;
            side = portalSide;
            floorHeightCell = portalFloorHeightCell;
            owner = owningRoom;
            entryAnchor = roomEntryAnchor;
            previewLine = roomPreviewLine;
            commitLine = roomCommitLine;
            portalBoundary = boundary;
            entrySafeFloor = safeFloor;

            EnsureSafeFloorProtection();

            ConfigureRelay(previewLine, PortalTriggerKind.Preview);
            ConfigureRelay(commitLine, PortalTriggerKind.Commit);
            EnsureStreamingGateVisuals();
            EnsurePortalFacade();
            ApplyStreamingGate();
        }

        public bool Link(RoomPortal2D destination)
        {
            if (destination == null || !RoomPortalContract.AreOpposite(side, destination.side))
            {
                destinationPortal = null;
                return false;
            }
            destinationPortal = destination;
            return true;
        }

        public void Bind(RoomTransitionController controller)
        {
            transitionController = controller;
        }

        public void SetStreamingReady(bool ready)
        {
            streamingReady = ready;
            EnsureStreamingGateVisuals();
            ApplyStreamingGate();
        }

        internal void HandleTrigger(PortalTriggerKind triggerKind, Collider2D other)
        {
            if (transitionController == null || other.GetComponentInParent<PlayerMotor2D>() == null)
            {
                return;
            }

            if (triggerKind == PortalTriggerKind.Preview)
            {
                transitionController.TryPreview(this);
            }
            else
            {
                transitionController.TryCommit(this);
            }
        }

        private void ConfigureRelay(Transform line, PortalTriggerKind triggerKind)
        {
            if (line == null)
            {
                return;
            }

            PortalTriggerRelay relay = line.GetComponent<PortalTriggerRelay>();
            if (relay == null)
            {
                relay = line.gameObject.AddComponent<PortalTriggerRelay>();
            }

            relay.Configure(this, triggerKind);
        }

        private void EnsureStreamingGateVisuals()
        {
            if (portalBoundary != null)
            {
                streamingBoundaryCollider = portalBoundary.GetComponent<Collider2D>();
                if (streamingBoundaryCollider == null)
                {
                    portalBoundary.gameObject.layer = LayerMask.NameToLayer("Ground");
                    streamingBoundaryCollider = portalBoundary.gameObject.AddComponent<BoxCollider2D>();
                }
            }

            if (loadingIndicator == null)
            {
                loadingIndicator = transform.Find("LoadingStar");
            }
            if (loadingIndicator == null)
            {
                var indicator = new GameObject("LoadingStar");
                indicator.transform.SetParent(transform, false);
                indicator.transform.localPosition = portalBoundary != null
                    ? portalBoundary.localPosition
                    : Vector3.zero;
                indicator.transform.localScale = Vector3.one * 0.28f;
                SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
                renderer.sprite = PrototypeSpriteFactory.GetWhitePixel();
                renderer.color = new Color32(239, 205, 118, 255);
                renderer.sortingOrder = 30;
                loadingIndicator = indicator.transform;
            }
        }

        private void ApplyStreamingGate()
        {
            if (streamingBoundaryCollider != null)
            {
                streamingBoundaryCollider.enabled = !streamingReady;
            }
            if (loadingIndicator != null)
            {
                loadingIndicator.gameObject.SetActive(!streamingReady);
            }
        }

        private void EnsureSafeFloorProtection()
        {
            if (entrySafeFloor != null && entrySafeFloor.GetComponent<PortalSafeFloorMarker>() == null)
            {
                entrySafeFloor.gameObject.AddComponent<PortalSafeFloorMarker>();
            }
        }

        private void EnsurePortalFacade()
        {
            portalFacade = GetComponentInChildren<PortalFacade>(true);
            if (portalFacade != null)
            {
                return;
            }
            GameObject facadeObject = new GameObject("PortalFacade");
            facadeObject.transform.SetParent(transform, false);
            facadeObject.transform.localPosition = portalBoundary != null
                ? portalBoundary.localPosition
                : Vector3.zero;
            facadeObject.transform.localScale = new Vector3(0.18f, 2.6f, 1f);
            SpriteRenderer renderer = facadeObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSpriteFactory.GetWhitePixel();
            renderer.color = new Color32(50, 61, 86, 230);
            renderer.sortingOrder = 26;
            portalFacade = facadeObject.AddComponent<PortalFacade>();
        }
    }

    [DisallowMultipleComponent]
    public sealed class PortalSafeFloorMarker : MonoBehaviour, IMapExplosionProtected { }

    [DisallowMultipleComponent]
    public sealed class PortalFacade : MonoBehaviour { }

    internal enum PortalTriggerKind
    {
        Preview,
        Commit,
    }

    [DisallowMultipleComponent]
    internal sealed class PortalTriggerRelay : MonoBehaviour
    {
        private RoomPortal2D portal;
        private PortalTriggerKind triggerKind;

        public void Configure(RoomPortal2D owner, PortalTriggerKind kind)
        {
            portal = owner;
            triggerKind = kind;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            portal?.HandleTrigger(triggerKind, other);
        }
    }
}

#endif
