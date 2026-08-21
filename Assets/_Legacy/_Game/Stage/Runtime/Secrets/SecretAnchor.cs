#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Carry;
using StarNight.Map;
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using StarNight.Player.Presentation;
using StarNight.Stage.Rooms;
using StarNight.Stage.Transitions;
using UnityEngine;

namespace StarNight.Stage.Secrets
{
    [DisallowMultipleComponent]
    public sealed class SecretAnchor : MonoBehaviour,
        IToolReactionReceiver,
        IRoomPersistentParticipant,
        IContextReceiver,
        IWorldInteractionReceiver,
        IInteractionPromptSource
    {
        [Serializable]
        private struct Snapshot
        {
            public bool revealed;
            public bool discovered;
        }

        [SerializeField] private string anchorStableId;
        [SerializeField] private int stageSeed;
        [SerializeField] private string secretRoomId;
        [SerializeField] private RoomRuntime sourceRoom;
        [SerializeField] private Transform returnSafeCell;
        [SerializeField] private Transform sourceRecoveryRack;
        [SerializeField] private SecretGateType gateType = SecretGateType.CrackedWall;
        [SerializeField] private bool discovered = true;
        [SerializeField] private bool revealed;

        private SecretDimensionController controller;
        private RoomPortal2D revealedPortal;
        private SpriteRenderer detectorHint;
        private SpriteRenderer detectorToolIcon;
        private SpriteRenderer gateSurfaceVisual;
        private SpriteRenderer gateAccentVisual;
        private InteractionCandidate interactionCandidate;
        private bool detectorHintActive;
        private bool compassFocused;

        public string AnchorStableId => anchorStableId ?? string.Empty;
        public string SecretRoomId => secretRoomId ?? string.Empty;
        public int StableSecretSeed => SecretSeedUtility.Create(stageSeed, sourceRoom?.RoomId, AnchorStableId);
        public RoomRuntime SourceRoom => sourceRoom;
        public Transform ReturnSafeCell => returnSafeCell;
        public Transform SourceRecoveryRack => sourceRecoveryRack != null ? sourceRecoveryRack : returnSafeCell;
        public bool IsRevealed => revealed;
        public SecretGateType GateType => gateType;
        public bool IsDiscovered => discovered;
        public SecretGateToolFamily RequiredToolFamily => SecretGateContract.ResolveToolFamily(gateType);
        public bool IsCompassFocused => compassFocused;
        public RoomPortal2D RevealedPortal => revealedPortal;
        public string PersistenceId => "secret-anchor:" + AnchorStableId;
        public int ContextPriority => (int)InteractionTargetKind.Mechanism;
        public string PromptLabel => gateType == SecretGateType.BlindPanel
            ? "비밀 패널 열기"
            : "장치 작동";

        private void Awake()
        {
            EnsureDetectorHint();
            EnsureGatePresentation();
            RefreshGatePresentation();
        }

        private void Update()
        {
            if (detectorHint != null && detectorHintActive)
            {
                Color color = detectorHint.color;
                color.a = 0.25f + (Mathf.Sin(Time.unscaledTime * 12f) + 1f) * 0.25f;
                detectorHint.color = color;
            }
        }

        public void Configure(
            string stableId,
            int configuredStageSeed,
            string configuredSecretRoomId,
            RoomRuntime room,
            Transform safeCell,
            SecretDimensionController dimensionController,
            SecretGateType configuredGateType = SecretGateType.CrackedWall)
        {
            anchorStableId = stableId;
            stageSeed = configuredStageSeed;
            secretRoomId = configuredSecretRoomId;
            sourceRoom = room;
            returnSafeCell = safeCell;
            controller = dimensionController;
            gateType = configuredGateType;
            discovered = gateType != SecretGateType.BlindPanel;
            EnsureSourceRecoveryRack();
            controller?.RegisterPlan(this);
            EnsureDetectorHint();
            EnsureGatePresentation();
            RefreshGatePresentation();
        }

        public ToolReactionResult TryReact(ToolReactionContext context)
        {
            if (revealed)
            {
                return new ToolReactionResult
                {
                    Accepted = true,
                    ChangedState = false,
                    ConsumeToolResource = false,
                    Feedback = FeedbackId.Accepted,
                };
            }

            if (gateType == SecretGateType.BlindPanel)
            {
                if (!discovered && SecretGateContract.DiscoversBlindPanel(context.Tags))
                {
                    Discover();
                    return new ToolReactionResult
                    {
                        Accepted = true,
                        ChangedState = true,
                        ConsumeToolResource = SecretGateContract.ShouldConsumeToolResource(context.Tags),
                        Feedback = FeedbackId.Accepted,
                    };
                }
                return ToolReactionResult.Rejected(FeedbackId.None);
            }

            if (!SecretGateContract.OpensFromTool(gateType, context.Tags))
            {
                return ToolReactionResult.Rejected(FeedbackId.None);
            }

            bool changed = controller != null && controller.Reveal(this);
            return new ToolReactionResult
            {
                Accepted = changed,
                ChangedState = changed,
                ConsumeToolResource = changed && SecretGateContract.ShouldConsumeToolResource(context.Tags),
                Feedback = changed ? FeedbackId.Break : FeedbackId.None,
            };
        }

        public bool CanReceive(ContextReceiverQuery query)
        {
            return !revealed && SecretGateContract.OpensFromContext(gateType, discovered);
        }

        public ContextReceiverResult TryReceive(ContextReceiverRequest request)
        {
            return TryOpenFromContext()
                ? new ContextReceiverResult(true, false, "SECRET_GATE_OPENED")
                : ContextReceiverResult.Rejected();
        }

        public bool CanInteract(GameObject actor)
        {
            return actor != null && !revealed
                && SecretGateContract.OpensFromContext(gateType, discovered);
        }

        public bool TryInteract(PlayerActionContext action, GameObject actor)
        {
            return CanInteract(actor) && TryOpenFromContext();
        }

        public bool TryActivateMechanismSignal()
        {
            return gateType == SecretGateType.MechanismSeal && TryOpenFromContext();
        }

        public void DiscoverFromNarrativeHint()
        {
            if (gateType == SecretGateType.BlindPanel && !discovered)
            {
                Discover();
            }
        }

        public void SetRevealed(RoomPortal2D portal)
        {
            revealed = true;
            revealedPortal = portal;
            interactionCandidate?.SetAvailable(false);
            SetDetectorHint(false);
            RefreshGatePresentation();
        }

        public void SetDetectorHint(bool active)
        {
            EnsureDetectorHint();
            if (active && gateType == SecretGateType.BlindPanel && !discovered)
            {
                Discover();
            }
            detectorHintActive = active && !revealed;
            RefreshDetectorPresentation();
        }

        public void SetCompassFocused(bool active)
        {
            EnsureDetectorHint();
            if (active && gateType == SecretGateType.BlindPanel && !discovered)
            {
                Discover();
            }
            compassFocused = active && !revealed;
            RefreshDetectorPresentation();
        }

        public string CaptureRoomState()
        {
            return JsonUtility.ToJson(new Snapshot
            {
                revealed = revealed,
                discovered = discovered,
            });
        }

        public void RestoreRoomState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }
            Snapshot snapshot = JsonUtility.FromJson<Snapshot>(payload);
            if (snapshot.discovered && !discovered)
            {
                Discover();
            }
            if (snapshot.revealed && !revealed)
            {
                controller?.Reveal(this);
            }
        }

        private bool TryOpenFromContext()
        {
            return SecretGateContract.OpensFromContext(gateType, discovered)
                && controller != null
                && controller.Reveal(this);
        }

        private void Discover()
        {
            discovered = true;
            RefreshGatePresentation();
        }

        private void EnsureSourceRecoveryRack()
        {
            if (sourceRecoveryRack != null || sourceRoom == null)
            {
                return;
            }

            string rackName = "SecretRecoveryRack_" + AnchorStableId;
            Transform existing = sourceRoom.transform.Find(rackName);
            GameObject rack = existing != null ? existing.gameObject : new GameObject(rackName);
            if (existing == null)
            {
                rack.transform.SetParent(sourceRoom.transform, true);
            }
            rack.transform.position = returnSafeCell != null
                ? returnSafeCell.position
                : sourceRoom.GetPrimarySafePosition();
            if (rack.GetComponent<CriticalObjectAnchor>() == null)
            {
                rack.AddComponent<CriticalObjectAnchor>();
            }
            sourceRecoveryRack = rack.transform;
        }

        private void EnsureDetectorHint()
        {
            if (detectorHint != null)
            {
                return;
            }
            Transform existing = transform.Find("CompassHint");
            GameObject hint = existing != null ? existing.gameObject : new GameObject("CompassHint");
            if (existing == null)
            {
                hint.transform.SetParent(transform, false);
            }
            detectorHint = hint.GetComponent<SpriteRenderer>();
            if (detectorHint == null)
            {
                detectorHint = hint.AddComponent<SpriteRenderer>();
            }
            detectorHint.sprite = PrototypeSpriteFactory.GetWhitePixel();
            detectorHint.color = new Color32(149, 218, 221, 128);
            detectorHint.sortingOrder = 28;
            hint.transform.localScale = Vector3.one * 1.05f;
            detectorHint.enabled = false;

            Transform iconTransform = hint.transform.Find("RequiredToolIcon");
            GameObject icon = iconTransform != null
                ? iconTransform.gameObject
                : new GameObject("RequiredToolIcon");
            if (iconTransform == null)
            {
                icon.transform.SetParent(hint.transform, false);
            }
            detectorToolIcon = icon.GetComponent<SpriteRenderer>();
            if (detectorToolIcon == null)
            {
                detectorToolIcon = icon.AddComponent<SpriteRenderer>();
            }
            detectorToolIcon.sprite = PrototypeSpriteFactory.GetWhitePixel();
            detectorToolIcon.color = ResolveToolFamilyColor(RequiredToolFamily);
            detectorToolIcon.sortingOrder = 29;
            icon.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            icon.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
            detectorToolIcon.enabled = false;
        }

        private void RefreshDetectorPresentation()
        {
            bool visible = !revealed && (detectorHintActive || compassFocused);
            detectorHint.enabled = visible;
            detectorHint.color = compassFocused
                ? new Color32(255, 211, 92, 210)
                : new Color32(149, 218, 221, 128);
            detectorToolIcon.color = ResolveToolFamilyColor(RequiredToolFamily);
            detectorToolIcon.enabled = visible && compassFocused;
        }

        private void EnsureGatePresentation()
        {
            if (gateSurfaceVisual == null)
            {
                Transform surfaceTransform = transform.Find("GateSurfaceVisual");
                GameObject surface = surfaceTransform != null
                    ? surfaceTransform.gameObject
                    : new GameObject("GateSurfaceVisual");
                if (surfaceTransform == null)
                {
                    surface.transform.SetParent(transform, false);
                }
                gateSurfaceVisual = surface.GetComponent<SpriteRenderer>();
                if (gateSurfaceVisual == null)
                {
                    gateSurfaceVisual = surface.AddComponent<SpriteRenderer>();
                }
                gateSurfaceVisual.sprite = PrototypeSpriteFactory.GetWhitePixel();
                gateSurfaceVisual.sortingOrder = 18;
            }

            if (gateAccentVisual == null)
            {
                Transform accentTransform = transform.Find("GateAccentVisual");
                GameObject accent = accentTransform != null
                    ? accentTransform.gameObject
                    : new GameObject("GateAccentVisual");
                if (accentTransform == null)
                {
                    accent.transform.SetParent(transform, false);
                }
                gateAccentVisual = accent.GetComponent<SpriteRenderer>();
                if (gateAccentVisual == null)
                {
                    gateAccentVisual = accent.AddComponent<SpriteRenderer>();
                }
                gateAccentVisual.sprite = PrototypeSpriteFactory.GetWhitePixel();
                gateAccentVisual.sortingOrder = 19;
            }

            interactionCandidate = GetComponent<InteractionCandidate>();
            if (interactionCandidate == null)
            {
                interactionCandidate = gameObject.AddComponent<InteractionCandidate>();
            }
            Collider2D interactionCollider = GetComponent<Collider2D>();
            if (interactionCollider == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = Vector2.one * 0.9f;
                box.isTrigger = true;
            }
            int interactionLayer = LayerMask.NameToLayer("Interaction");
            if (interactionLayer >= 0)
            {
                gameObject.layer = interactionLayer;
            }
        }

        private void RefreshGatePresentation()
        {
            EnsureGatePresentation();
            bool contextAvailable = !revealed
                && SecretGateContract.OpensFromContext(gateType, discovered);
            interactionCandidate.Configure(
                InteractionTargetKind.Mechanism,
                gameObject.GetInstanceID() & int.MaxValue,
                gateType == SecretGateType.BlindPanel ? "비밀 패널" : "비밀 장치",
                PromptLabel,
                contextAvailable);

            gateSurfaceVisual.enabled = !revealed;
            gateAccentVisual.enabled = !revealed;
            switch (gateType)
            {
                case SecretGateType.DirtSeal:
                    gateSurfaceVisual.color = new Color32(104, 72, 45, 180);
                    gateSurfaceVisual.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
                    gateAccentVisual.color = new Color32(176, 128, 75, 190);
                    gateAccentVisual.transform.localPosition = Vector3.zero;
                    gateAccentVisual.transform.localScale = new Vector3(0.08f, 0.88f, 1f);
                    break;
                case SecretGateType.ThinFloor:
                    gateSurfaceVisual.color = new Color32(92, 100, 115, 175);
                    gateSurfaceVisual.transform.localScale = new Vector3(1f, 0.18f, 1f);
                    gateAccentVisual.color = new Color32(169, 190, 211, 190);
                    gateAccentVisual.transform.localPosition = new Vector3(0f, 0.09f, 0f);
                    gateAccentVisual.transform.localScale = new Vector3(0.82f, 0.04f, 1f);
                    break;
                case SecretGateType.MechanismSeal:
                    gateSurfaceVisual.color = new Color32(35, 65, 72, 190);
                    gateSurfaceVisual.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
                    gateAccentVisual.color = new Color32(128, 221, 156, 220);
                    gateAccentVisual.transform.localPosition = Vector3.zero;
                    gateAccentVisual.transform.localScale = new Vector3(0.22f, 0.42f, 1f);
                    break;
                case SecretGateType.BlindPanel:
                    byte blindAlpha = discovered ? (byte)120 : (byte)18;
                    gateSurfaceVisual.color = new Color32(72, 68, 88, blindAlpha);
                    gateSurfaceVisual.transform.localScale = new Vector3(0.94f, 0.94f, 1f);
                    gateAccentVisual.color = new Color32(187, 133, 235, blindAlpha);
                    gateAccentVisual.transform.localPosition = Vector3.zero;
                    gateAccentVisual.transform.localScale = new Vector3(0.05f, 0.82f, 1f);
                    break;
                default:
                    gateSurfaceVisual.color = new Color32(61, 66, 79, 180);
                    gateSurfaceVisual.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
                    gateAccentVisual.color = new Color32(149, 218, 221, 200);
                    gateAccentVisual.transform.localPosition = new Vector3(0.14f, 0.16f, 0f);
                    gateAccentVisual.transform.localScale = new Vector3(0.08f, 0.42f, 1f);
                    break;
            }
        }

        private static Color ResolveToolFamilyColor(SecretGateToolFamily family)
        {
            return family switch
            {
                SecretGateToolFamily.Shovel => new Color32(188, 139, 82, 255),
                SecretGateToolFamily.PestleOrHeavyImpact => new Color32(229, 126, 90, 255),
                SecretGateToolFamily.ContextInteraction => new Color32(128, 221, 156, 255),
                SecretGateToolFamily.PanelInteraction => new Color32(187, 133, 235, 255),
                _ => new Color32(242, 196, 92, 255),
            };
        }
    }
}

#endif
