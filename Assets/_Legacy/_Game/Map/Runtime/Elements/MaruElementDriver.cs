#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class MaruElementDriver : MonoBehaviour,
        IMapElementInteractionReceiver,
        IMapElementWeightSource,
        IMapElementPersistentParticipant
    {
        [SerializeField] private string variantState = string.Empty;
        [SerializeField] private int durabilityRemaining;
        [SerializeField] private int interactionProgress;
        [SerializeField] private bool carried;
        [SerializeField] private bool outcomeApplied;
        [SerializeField] private bool rewardVisible;
        [SerializeField] private bool penaltyVisible;
        [SerializeField] private string lastRewardText = string.Empty;
        [SerializeField] private string lastPenaltyText = string.Empty;

        private MapElementInstance element;
        private ToolReactionReceiver toolReceiver;
        private Rigidbody2D body;
        private Collider2D[] colliders = Array.Empty<Collider2D>();
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Transform feedbackRoot;
        private TextMesh rewardTextMesh;
        private TextMesh penaltyTextMesh;
        private float feedbackRemaining;
        private bool initialized;

        public event Action<MaruElementEventResult> OutcomeChanged;

        public MaruElementKind Kind => Profile != null ? Profile.Kind : MaruElementKind.None;
        public string VariantState => variantState;
        public int DurabilityRemaining => durabilityRemaining;
        public bool IsCarried => carried;
        public bool RewardVisible => rewardVisible;
        public bool PenaltyVisible => penaltyVisible;
        public string LastRewardText => lastRewardText;
        public string LastPenaltyText => lastPenaltyText;
        public string PreviewRewardText => Profile != null ? Profile.PreviewRewardText : string.Empty;
        public string PreviewPenaltyText => Profile != null ? Profile.PreviewPenaltyText : string.Empty;
        public int PressureWeight => Profile != null ? Mathf.Clamp(Profile.PressureWeight, 1, 2) : 1;
        public string PersistenceId => element != null ? $"{element.PersistenceId}:maru" : string.Empty;
        public string InteractionPrompt => ResolveInteractionPrompt();

        private MaruElementRuntimeProfile Profile =>
            element != null && element.Definition != null ? element.Definition.MaruProfile : null;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (feedbackRemaining <= 0f)
            {
                return;
            }

            feedbackRemaining -= Time.deltaTime;
            if (feedbackRemaining <= 0f && feedbackRoot != null)
            {
                feedbackRoot.gameObject.SetActive(false);
                rewardVisible = false;
                penaltyVisible = false;
            }
        }

        private void OnDestroy()
        {
            if (carried && initialized)
            {
                Dispatch(MaruElementEventType.CollarCarryChanged, active: false);
            }
        }

        public void Rebind()
        {
            initialized = false;
            Initialize();
        }

        public bool ApplyPartialToolReaction(
            ToolReactionEntry entry,
            ToolReactionContext context,
            int hitCount,
            int requiredHits)
        {
            Initialize();
            if (Kind != MaruElementKind.ReturnStatue || entry == null ||
                entry.Reaction != ElementReactionType.Break || hitCount >= requiredHits ||
                element.CurrentState == MapElementState.Broken)
            {
                return false;
            }

            durabilityRemaining = Mathf.Max(1, requiredHits - hitCount);
            variantState = "Cracked";
            var warning = Dispatch(MaruElementEventType.StatueWarning);
            if (!warning.Accepted)
            {
                warning = new MaruElementEventResult
                {
                    Accepted = true,
                    PenaltyApplied = true,
                    PenaltyText = "방울 경고 1회",
                };
            }
            ShowOutcome(warning);
            RefreshPresentation();
            return true;
        }

        public bool ApplyToolReaction(ToolReactionEntry entry, ToolReactionContext context)
        {
            Initialize();
            if (entry == null || Profile == null || element.CurrentState == MapElementState.Broken)
            {
                return false;
            }

            if (Kind == MaruElementKind.ReturnStatue)
            {
                if (entry.Reaction == ElementReactionType.Break)
                {
                    return CompleteStatueBreak();
                }
                if (entry.Reaction == ElementReactionType.Pull || entry.Reaction == ElementReactionType.Move)
                {
                    var direction = SanitizeDirection(context.Direction);
                    transform.position += (Vector3)(Vector2)direction;
                    variantState = "Dragged";
                    RefreshPresentation();
                    return true;
                }
            }
            else if (Kind == MaruElementKind.ReturnBellJar && entry.Reaction == ElementReactionType.Break)
            {
                return CompleteBellJarBreak();
            }

            return false;
        }

        public bool TryInteract(GameObject instigator)
        {
            Initialize();
            if (Profile == null || element.CurrentState == MapElementState.Broken ||
                element.CurrentState == MapElementState.Disabled)
            {
                return false;
            }

            switch (Kind)
            {
                case MaruElementKind.CollarFragment:
                    return SetCarried(true, instigator);
                case MaruElementKind.ReturnMarker:
                {
                    var result = Dispatch(MaruElementEventType.ReturnMarkerUsed);
                    ShowOutcome(result);
                    return result.Accepted;
                }
                case MaruElementKind.RecordCasket:
                    return AdvanceCasketInteraction(false);
                default:
                    return false;
            }
        }

        public bool OpenFromPuzzle()
        {
            Initialize();
            return Kind == MaruElementKind.RecordCasket && AdvanceCasketInteraction(true);
        }

        public bool SetCarried(bool active, GameObject carrier = null)
        {
            Initialize();
            if (Kind != MaruElementKind.CollarFragment || carried == active)
            {
                return false;
            }

            var result = Dispatch(MaruElementEventType.CollarCarryChanged, active: active);
            if (!result.Accepted)
            {
                ShowOutcome(result);
                return false;
            }

            carried = active;
            variantState = active ? "StoryCarry" : "Idle";
            if (active && carrier != null)
            {
                transform.SetParent(carrier.transform, false);
                transform.localPosition = new Vector3(0.38f, -0.10f, 0f);
            }
            if (body != null)
            {
                body.simulated = !active;
            }
            RefreshPresentation();
            ShowOutcome(result);
            return true;
        }

        public bool CommitAtExit()
        {
            Initialize();
            if (Kind != MaruElementKind.CollarFragment || !carried)
            {
                return false;
            }

            var result = Dispatch(MaruElementEventType.CollarCommittedAtExit, active: true);
            ShowOutcome(result);
            return result.Accepted;
        }

        public void NotifyTriggerEnter(Collider2D other)
        {
            Initialize();
            if (Kind != MaruElementKind.PawprintPool || outcomeApplied ||
                other == null || MaruElementEventHub.IsExitDiscovered())
            {
                return;
            }

            var result = Dispatch(MaruElementEventType.PawprintPoolTriggered);
            if (!result.Accepted)
            {
                return;
            }

            outcomeApplied = true;
            variantState = "Activated";
            element.TrySetState(MapElementState.Active);
            ShowOutcome(result);
            RefreshPresentation();
        }

        public void NotifyCollisionEnter(Collision2D collision)
        {
            Initialize();
            if ((Kind != MaruElementKind.ReturnStatue && Kind != MaruElementKind.ReturnBellJar) ||
                collision == null || toolReceiver == null)
            {
                return;
            }

            var sourceBody = collision.rigidbody;
            var mass = sourceBody != null ? Mathf.Max(1f, sourceBody.mass) : 1f;
            var speed = collision.relativeVelocity.magnitude;
            var score = mass * speed;
            if (score < 2f)
            {
                return;
            }

            var direction = collision.relativeVelocity.sqrMagnitude > 0.001f
                ? Cardinal(collision.relativeVelocity)
                : Vector2Int.right;
            toolReceiver.TryReact(new ToolReactionContext
            {
                ActionId = unchecked((collision.gameObject.GetInstanceID() * 397) ^ Time.frameCount),
                Tags = score >= 6f || (mass >= 2f && speed >= 3f)
                    ? ToolTag.HeavyImpact
                    : ToolTag.LightImpact,
                Direction = direction,
                Magnitude = score,
                Source = collision.gameObject,
                Instigator = collision.gameObject,
            });
        }

        public string CaptureMapElementState()
        {
            return JsonUtility.ToJson(new PersistentState
            {
                VariantState = variantState,
                DurabilityRemaining = durabilityRemaining,
                InteractionProgress = interactionProgress,
                OutcomeApplied = outcomeApplied,
            });
        }

        public void RestoreMapElementState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            var state = JsonUtility.FromJson<PersistentState>(payload);
            if (state == null)
            {
                return;
            }

            variantState = state.VariantState ?? string.Empty;
            durabilityRemaining = Mathf.Max(0, state.DurabilityRemaining);
            interactionProgress = Mathf.Max(0, state.InteractionProgress);
            outcomeApplied = state.OutcomeApplied;
            RefreshPresentation();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            element = GetComponent<MapElementInstance>();
            toolReceiver = GetComponent<ToolReactionReceiver>();
            body = GetComponent<Rigidbody2D>();
            colliders = GetComponentsInChildren<Collider2D>(true);
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (durabilityRemaining <= 0 && Profile != null && !outcomeApplied)
            {
                durabilityRemaining = Mathf.Max(1, Profile.DurabilityStages);
            }
            initialized = true;
            RefreshPresentation();
        }

        private bool CompleteStatueBreak()
        {
            if (outcomeApplied)
            {
                return false;
            }

            durabilityRemaining = 0;
            outcomeApplied = true;
            variantState = "Broken";
            element.TrySetState(MapElementState.Broken);
            var result = Dispatch(MaruElementEventType.StatueBroken);
            ShowOutcome(result);
            RefreshPresentation();
            return true;
        }

        private bool CompleteBellJarBreak()
        {
            if (outcomeApplied)
            {
                return false;
            }

            durabilityRemaining = 0;
            outcomeApplied = true;
            variantState = "Broken";
            element.TrySetState(MapElementState.Broken);
            var result = Dispatch(MaruElementEventType.BellJarBroken);
            ShowOutcome(result);
            RefreshPresentation();
            return true;
        }

        private bool AdvanceCasketInteraction(bool puzzleSolved)
        {
            if (outcomeApplied)
            {
                return false;
            }

            if (!puzzleSolved)
            {
                interactionProgress++;
                durabilityRemaining = Mathf.Max(0, Profile.DurabilityStages - interactionProgress);
                if (interactionProgress < Mathf.Max(1, Profile.DurabilityStages))
                {
                    variantState = "Unsealed";
                    ShowOutcome(new MaruElementEventResult
                    {
                        Accepted = true,
                        RewardText = "봉인 1단계 해제",
                    });
                    RefreshPresentation();
                    return true;
                }
            }

            interactionProgress = Mathf.Max(interactionProgress, Profile.DurabilityStages);
            durabilityRemaining = 0;
            outcomeApplied = true;
            variantState = "Opened";
            element.TrySetState(MapElementState.Active);
            var result = Dispatch(MaruElementEventType.RecordTravelerFreed);
            ShowOutcome(result);
            RefreshPresentation();
            return result.Accepted;
        }

        private MaruElementEventResult Dispatch(MaruElementEventType eventType, bool active = false)
        {
            return MaruElementEventHub.Dispatch(new MaruElementEventRequest
            {
                EventType = eventType,
                ElementId = element?.Definition != null ? element.Definition.ElementId : string.Empty,
                SourceRuntimeId = element != null ? element.PersistenceId : string.Empty,
                RewardMoney = Profile != null ? Profile.RewardMoney : 0,
                RewardId = Profile != null ? Profile.RewardId : string.Empty,
                Seconds = ResolveEventSeconds(eventType),
                GuidanceSeconds = Profile != null ? Profile.GuidanceSeconds : 0f,
                RateMultiplier = Profile != null ? Profile.TimerRateMultiplier : 1f,
                Active = active,
                MarkerCostType = Profile != null ? Profile.MarkerCostType : MaruMarkerCostType.Money,
                MarkerCostValue = Profile != null ? Profile.MarkerCostValue : 0,
                RecordGuideEffect = Profile != null ? Profile.RecordGuideEffect : MaruRecordGuideEffect.ExitDirection,
                NoiseLevel = Profile != null ? Profile.NoiseLevel : 0f,
            });
        }

        private float ResolveEventSeconds(MaruElementEventType eventType)
        {
            if (Profile == null)
            {
                return 0f;
            }
            switch (eventType)
            {
                case MaruElementEventType.BellJarBroken:
                    return Profile.ScheduledEntryDelaySeconds;
                case MaruElementEventType.PawprintPoolTriggered:
                    return Profile.ShortenNextBellSeconds;
                default:
                    return Profile.GuidanceSeconds;
            }
        }

        private void ShowOutcome(MaruElementEventResult result)
        {
            lastRewardText = result.RewardText ?? string.Empty;
            lastPenaltyText = result.PenaltyText ?? string.Empty;
            rewardVisible = result.RewardGranted || !string.IsNullOrWhiteSpace(lastRewardText);
            penaltyVisible = result.PenaltyApplied || !string.IsNullOrWhiteSpace(lastPenaltyText);
            EnsureFeedbackVisuals();
            rewardTextMesh.text = rewardVisible ? lastRewardText : string.Empty;
            penaltyTextMesh.text = penaltyVisible ? lastPenaltyText : string.Empty;
            feedbackRoot.gameObject.SetActive(rewardVisible || penaltyVisible);
            feedbackRemaining = 2.5f;
            OutcomeChanged?.Invoke(result);
        }

        private void EnsureFeedbackVisuals()
        {
            if (feedbackRoot != null)
            {
                return;
            }

            feedbackRoot = new GameObject("OutcomeFeedbackRoot").transform;
            feedbackRoot.SetParent(transform, false);
            var height = element?.Definition?.Footprint != null
                ? element.Definition.Footprint.BoundsSize.y
                : 1;
            feedbackRoot.localPosition = new Vector3(0f, height * 0.5f + 0.65f, -0.2f);
            rewardTextMesh = CreateFeedbackText("Reward", feedbackRoot, new Vector3(0f, 0.18f, 0f), new Color(0.35f, 1f, 0.48f));
            penaltyTextMesh = CreateFeedbackText("Penalty", feedbackRoot, new Vector3(0f, -0.18f, 0f), new Color(1f, 0.34f, 0.30f));
        }

        private void RefreshPresentation()
        {
            if (!initialized && element == null)
            {
                return;
            }

            var unavailable = outcomeApplied &&
                              (Kind == MaruElementKind.ReturnStatue || Kind == MaruElementKind.ReturnBellJar);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null)
                {
                    colliders[index].enabled = !unavailable && !carried;
                }
            }

            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] == null)
                {
                    continue;
                }
                var color = renderers[index].color;
                if (variantState == "Cracked" || variantState == "Unsealed")
                {
                    color = new Color(1f, 0.62f, 0.18f, 1f);
                }
                color.a = unavailable ? 0.25f : 1f;
                renderers[index].color = color;
            }
        }

        private string ResolveInteractionPrompt()
        {
            switch (Kind)
            {
                case MaruElementKind.CollarFragment: return carried ? string.Empty : "[X] 별목줄 파편 챙기기";
                case MaruElementKind.ReturnMarker: return "[X] Entry SafeCell로 귀환";
                case MaruElementKind.RecordCasket: return "[X] 별기록관의 관 열기";
                default: return string.Empty;
            }
        }

        private static TextMesh CreateFeedbackText(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Color color)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            var text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.11f;
            text.fontSize = 32;
            text.color = color;
            return text;
        }

        private static Vector2Int Cardinal(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x < 0f ? Vector2Int.left : Vector2Int.right;
            }
            return direction.y < 0f ? Vector2Int.down : Vector2Int.up;
        }

        private static Vector2Int SanitizeDirection(Vector2Int direction)
        {
            return direction == Vector2Int.left || direction == Vector2Int.right ||
                   direction == Vector2Int.up || direction == Vector2Int.down
                ? direction
                : Vector2Int.right;
        }

        [Serializable]
        private sealed class PersistentState
        {
            public string VariantState;
            public int DurabilityRemaining;
            public int InteractionProgress;
            public bool OutcomeApplied;
        }
    }

    [DisallowMultipleComponent]
    public sealed class MaruElementPhysicsRelay : MonoBehaviour
    {
        [SerializeField] private MaruElementDriver driver;

        public void Configure(MaruElementDriver maruDriver)
        {
            driver = maruDriver;
        }

        private void OnTriggerEnter2D(Collider2D other) => driver?.NotifyTriggerEnter(other);
        private void OnCollisionEnter2D(Collision2D collision) => driver?.NotifyCollisionEnter(collision);
    }
}

#endif
