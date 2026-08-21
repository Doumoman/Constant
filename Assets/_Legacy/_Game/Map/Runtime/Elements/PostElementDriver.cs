#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    public interface IPostParcelPayload
    {
        string ParcelId { get; }
        bool IsHeavyParcel { get; }
    }

    public interface IPostPlayerMarker
    {
    }

    public interface IPostMarkedParcel
    {
        void ApplyPostmark(string stampType);
    }

    public interface IPostHiddenFootprintReceiver
    {
        void RevealHiddenFootprints(bool revealed);
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class PostElementDriver : MonoBehaviour,
        IMapElementSignalReceiver,
        IMapElementInteractionReceiver,
        IMapElementPersistentParticipant
    {
        private static readonly Dictionary<string, List<PostElementDriver>> PairRegistry =
            new Dictionary<string, List<PostElementDriver>>(StringComparer.Ordinal);

        [SerializeField] private string variantState = string.Empty;
        [SerializeField] private bool conveyorStopped;
        [SerializeField] private bool stampScheduled;
        [SerializeField] private float stampWarningElapsed;
        [SerializeField] private float stampActiveRemaining;
        [SerializeField] private int sortingSequenceIndex;
        [SerializeField] private bool inkDiluted;
        [SerializeField] private bool parcelStackFlattened;
        [SerializeField] private bool expressActive;
        [SerializeField] private int parcelsProcessed;

        private readonly HashSet<int> heavyConveyorOccupants = new HashSet<int>();
        private readonly HashSet<int> stampedTargets = new HashSet<int>();
        private readonly Dictionary<int, float> transportCooldowns = new Dictionary<int, float>();
        private MapElementInstance element;
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Color[] rendererBaseColors = Array.Empty<Color>();
        private Transform visualRoot;
        private Transform physicsRoot;
        private Transform triggerRoot;
        private Vector3 visualOrigin;
        private Vector3 visualOriginScale;
        private Vector3 physicsOrigin;
        private Vector3 triggerOrigin;
        private string registeredPairGuid = string.Empty;
        private bool initialized;

        public PostElementKind Kind => Profile != null ? Profile.Kind : PostElementKind.None;
        public string VariantState => variantState;
        public bool ConveyorStopped => conveyorStopped;
        public bool StampScheduled => stampScheduled;
        public bool StampActive => stampActiveRemaining > 0f;
        public int SortingSequenceIndex => sortingSequenceIndex;
        public bool InkDiluted => inkDiluted;
        public bool ParcelStackFlattened => parcelStackFlattened;
        public bool ExpressActive => expressActive;
        public int ParcelsProcessed => parcelsProcessed;
        public int RegisteredPairCount => GetPairList(Profile != null ? Profile.PairGuid : string.Empty)?.Count ?? 0;
        public string PersistenceId => element != null ? $"{element.PersistenceId}:post" : string.Empty;
        public string InteractionPrompt => Kind switch
        {
            PostElementKind.ParcelLauncher => "[X] Insert parcel",
            PostElementKind.ReturnStamp => "[X] Trigger return stamp",
            PostElementKind.SortingArm => "[X] Rotate sorting arm",
            PostElementKind.MailTube => "[X] Insert compatible parcel",
            PostElementKind.ExpressTube when !expressActive => "[X] Express tube locked",
            PostElementKind.ExpressTube => "[X] Insert express parcel",
            _ => string.Empty,
        };

        private PostElementRuntimeProfile Profile =>
            element != null && element.Definition != null ? element.Definition.PostProfile : null;

        private void Awake() => Initialize();
        private void OnEnable()
        {
            if (initialized) RegisterPair();
        }
        private void OnDisable() => UnregisterPair();

        private void Update()
        {
            if (!initialized || Profile == null || element.CurrentState == MapElementState.Dormant)
            {
                return;
            }
            Tick(Time.deltaTime);
        }

        public void Rebind()
        {
            UnregisterPair();
            initialized = false;
            Initialize();
        }

        public void TickForTests(float deltaSeconds)
        {
            Initialize();
            Tick(Mathf.Max(0f, deltaSeconds));
        }

        public bool ApplyToolReaction(ToolReactionEntry entry, ToolReactionContext context)
        {
            Initialize();
            if (entry == null || Profile == null || element.CurrentState == MapElementState.Broken)
            {
                return false;
            }

            switch (Kind)
            {
                case PostElementKind.Conveyor when (context.Tags & ToolTag.HeavyImpact) != 0:
                    conveyorStopped = true;
                    variantState = "HeavyStopped";
                    RefreshPresentation();
                    return true;

                case PostElementKind.ParcelLauncher when (context.Tags & ToolTag.Context) != 0:
                    variantState = "AwaitingParcel";
                    RefreshPresentation();
                    return true;

                case PostElementKind.ReturnStamp
                    when (context.Tags & (ToolTag.Hook | ToolTag.Pound)) != 0:
                    return TriggerStamp();

                case PostElementKind.SortingArm
                    when (context.Tags & (ToolTag.Context | ToolTag.HeavyImpact)) != 0:
                    AdvanceSortingArm();
                    return true;

                case PostElementKind.MailTube when (context.Tags & ToolTag.Context) != 0:
                    variantState = "AwaitingParcel";
                    RefreshPresentation();
                    return true;

                case PostElementKind.InkPool when (context.Tags & ToolTag.Water) != 0:
                    inkDiluted = true;
                    variantState = "Diluted";
                    element.TrySetState(MapElementState.Disabled);
                    RefreshPresentation();
                    return true;

                case PostElementKind.ParcelStack when (context.Tags & ToolTag.Pound) != 0:
                    parcelStackFlattened = true;
                    variantState = "Flattened";
                    element.TrySetState(MapElementState.Active);
                    RefreshPresentation();
                    return true;

                case PostElementKind.ParcelStack when (context.Tags & ToolTag.Bomb) != 0:
                    variantState = "Collapsed";
                    element.TrySetState(MapElementState.Broken);
                    RefreshPresentation();
                    return true;

                case PostElementKind.ExpressTube when (context.Tags & ToolTag.Context) != 0:
                    variantState = expressActive ? "AwaitingExpressParcel" : "StoryOrParcelRequired";
                    RefreshPresentation();
                    return true;
            }

            return false;
        }

        public bool TryInteract(GameObject instigator)
        {
            Initialize();
            if (instigator != null && TryInsertParcel(instigator)) return true;
            if (Kind == PostElementKind.ReturnStamp) return TriggerStamp();
            if (Kind == PostElementKind.SortingArm)
            {
                AdvanceSortingArm();
                return true;
            }
            return false;
        }

        public void ReceiveSignal(string channel, bool active)
        {
            Initialize();
            if (Kind == PostElementKind.SortingArm && active)
            {
                AdvanceSortingArm();
            }
            else if (Kind == PostElementKind.ReturnStamp && active)
            {
                TriggerStamp();
            }
            else if (Kind == PostElementKind.ExpressTube)
            {
                expressActive = active;
                variantState = active ? "StoryUnlocked" : "Locked";
                element.TrySetState(active ? MapElementState.Active : MapElementState.Idle);
                RefreshPresentation();
            }
        }

        public bool TryInsertParcel(GameObject payload)
        {
            Initialize();
            if (payload == null || Profile == null || IsPlayer(payload)) return false;
            var parcel = FindInterface<IPostParcelPayload>(payload);
            if (parcel == null || !IsCompatibleParcel(parcel.ParcelId)) return false;

            switch (Kind)
            {
                case PostElementKind.ParcelLauncher:
                    LaunchParcel(payload);
                    parcelsProcessed++;
                    variantState = "ParcelLaunched";
                    RefreshPresentation();
                    return true;

                case PostElementKind.MailTube:
                    return TransportParcel(payload);

                case PostElementKind.ExpressTube:
                    if (!expressActive &&
                        !string.Equals(parcel.ParcelId, Profile.RequiredParcelId, StringComparison.Ordinal))
                    {
                        variantState = "RejectedParcel";
                        RefreshPresentation();
                        return false;
                    }
                    expressActive = true;
                    element.TrySetState(MapElementState.Active);
                    return TransportParcel(payload);
            }
            return false;
        }

        public bool TriggerStamp()
        {
            Initialize();
            if (Kind != PostElementKind.ReturnStamp || stampScheduled || StampActive) return false;
            stampScheduled = true;
            stampWarningElapsed = 0f;
            stampedTargets.Clear();
            variantState = "Warning";
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
            return true;
        }

        public void NotifyTriggerEnter(Collider2D other)
        {
            Initialize();
            if (other == null || Profile == null) return;

            if (Kind == PostElementKind.Conveyor && Profile.StopsOnHeavy && IsHeavy(other.gameObject))
            {
                heavyConveyorOccupants.Add(other.gameObject.GetInstanceID());
                conveyorStopped = true;
                variantState = "HeavyStopped";
                RefreshPresentation();
            }
            else if (Kind == PostElementKind.ParcelLauncher ||
                     Kind == PostElementKind.MailTube ||
                     Kind == PostElementKind.ExpressTube)
            {
                TryInsertParcel(other.gameObject);
            }
            else if (Kind == PostElementKind.InkPool)
            {
                ApplyInk(other);
            }
        }

        public void NotifyTriggerStay(Collider2D other)
        {
            if (other == null || Profile == null) return;
            if (Kind == PostElementKind.Conveyor) ApplyConveyor(other);
            else if (Kind == PostElementKind.SortingArm) ApplySortingPush(other);
            else if (Kind == PostElementKind.ReturnStamp && StampActive) ApplyStamp(other);
        }

        public void NotifyTriggerExit(Collider2D other)
        {
            if (other == null) return;
            if (Kind == PostElementKind.Conveyor)
            {
                heavyConveyorOccupants.Remove(other.gameObject.GetInstanceID());
                conveyorStopped = heavyConveyorOccupants.Count > 0;
                variantState = conveyorStopped ? "HeavyStopped" : string.Empty;
                RefreshPresentation();
            }
            transportCooldowns.Remove(other.gameObject.GetInstanceID());
        }

        public string CaptureMapElementState()
        {
            return JsonUtility.ToJson(new PersistentState
            {
                VariantState = variantState,
                ConveyorStopped = conveyorStopped,
                SortingSequenceIndex = sortingSequenceIndex,
                InkDiluted = inkDiluted,
                ParcelStackFlattened = parcelStackFlattened,
                ExpressActive = expressActive,
                ParcelsProcessed = parcelsProcessed,
            });
        }

        public void RestoreMapElementState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            Initialize();
            var state = JsonUtility.FromJson<PersistentState>(payload);
            if (state == null) return;
            variantState = state.VariantState ?? string.Empty;
            conveyorStopped = state.ConveyorStopped;
            sortingSequenceIndex = Mathf.Max(0, state.SortingSequenceIndex);
            inkDiluted = state.InkDiluted;
            parcelStackFlattened = state.ParcelStackFlattened;
            expressActive = state.ExpressActive;
            parcelsProcessed = Mathf.Max(0, state.ParcelsProcessed);
            RefreshPresentation();
        }

        private void Initialize()
        {
            if (initialized) return;
            element = GetComponent<MapElementInstance>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            rendererBaseColors = new Color[renderers.Length];
            for (var index = 0; index < renderers.Length; index++)
                rendererBaseColors[index] = renderers[index] != null ? renderers[index].color : Color.white;
            visualRoot = transform.Find("VisualRoot");
            physicsRoot = transform.Find("PhysicsRoot");
            triggerRoot = transform.Find("TriggerRoot");
            visualOrigin = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
            visualOriginScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            physicsOrigin = physicsRoot != null ? physicsRoot.localPosition : Vector3.zero;
            triggerOrigin = triggerRoot != null ? triggerRoot.localPosition : Vector3.zero;
            expressActive = Profile != null && Profile.Kind == PostElementKind.ExpressTube && Profile.StartsActive;
            initialized = true;
            RegisterPair();
            RefreshPresentation();
        }

        private void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Profile == null || Kind != PostElementKind.ReturnStamp) return;
            if (stampScheduled)
            {
                stampWarningElapsed += deltaSeconds;
                if (stampWarningElapsed >= Profile.WarningDelaySeconds)
                {
                    stampScheduled = false;
                    stampActiveRemaining = Mathf.Max(0.01f, Profile.StampActiveSeconds);
                    variantState = "StampDown";
                    RefreshPresentation();
                }
            }
            else if (stampActiveRemaining > 0f)
            {
                stampActiveRemaining = Mathf.Max(0f, stampActiveRemaining - deltaSeconds);
                if (stampActiveRemaining <= 0f)
                {
                    variantState = string.Empty;
                    element.TrySetState(MapElementState.Idle);
                    RefreshPresentation();
                }
            }
        }

        private void AdvanceSortingArm()
        {
            var count = Profile.RotationSequenceDegrees != null
                ? Profile.RotationSequenceDegrees.Count
                : 0;
            sortingSequenceIndex = count > 0 ? (sortingSequenceIndex + 1) % count : 0;
            variantState = "RouteChanged";
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
        }

        private void ApplyConveyor(Collider2D other)
        {
            if (conveyorStopped || other.attachedRigidbody == null) return;
            var direction = ResolveDirection();
            var perpendicular = new Vector2(-direction.y, direction.x);
            var perpendicularVelocity = Vector2.Dot(other.attachedRigidbody.linearVelocity, perpendicular);
            other.attachedRigidbody.linearVelocity =
                direction * Profile.SurfaceSpeedCellsPerSecond + perpendicular * perpendicularVelocity;
        }

        private void ApplySortingPush(Collider2D other)
        {
            if (other.attachedRigidbody == null) return;
            other.attachedRigidbody.linearVelocity +=
                ResolveDirection() * Profile.PushForceCellsPerSecond * Time.fixedDeltaTime;
        }

        private void ApplyInk(Collider2D other)
        {
            if (inkDiluted) return;
            if (other.attachedRigidbody != null)
                other.attachedRigidbody.linearVelocity *= Mathf.Clamp01(1f - Profile.SlowRate);
            var footprints = FindInterface<IPostHiddenFootprintReceiver>(other.gameObject);
            if (Profile.RevealsHiddenFootprints && footprints != null)
                footprints.RevealHiddenFootprints(true);
        }

        private void ApplyStamp(Collider2D other)
        {
            var id = other.gameObject.GetInstanceID();
            if (!stampedTargets.Add(id)) return;
            var markedParcel = FindInterface<IPostMarkedParcel>(other.gameObject);
            markedParcel?.ApplyPostmark(Profile.StampType);
            var damageReceiver = FindInterface<IMapElementDamageReceiver>(other.gameObject);
            damageReceiver?.ReceiveMapElementDamage(new MapElementDamageEvent(
                Profile.StampDamage,
                Vector2.down * 3f,
                gameObject,
                id));
        }

        private void LaunchParcel(GameObject payload)
        {
            var body = payload.GetComponentInParent<Rigidbody2D>();
            if (body == null) return;
            var direction = ResolveDirection();
            body.linearVelocity = direction * Profile.LaunchPower + Vector2.up *
                (Profile.LaunchPower * Mathf.Clamp01(Profile.LaunchArc));
            var impact = payload.GetComponent<PostParcelLaunchImpact>();
            if (impact == null) impact = payload.AddComponent<PostParcelLaunchImpact>();
            impact.Configure(Profile.CollisionDamage, gameObject);
        }

        private bool TransportParcel(GameObject payload)
        {
            var id = payload.GetInstanceID();
            if (transportCooldowns.TryGetValue(id, out var until) && until > Time.unscaledTime)
                return false;
            if (Kind == PostElementKind.ExpressTube && Profile.OneWay && !IsOneWaySender())
                return false;
            var destination = FindPairDestination();
            if (destination == null)
            {
                variantState = "PairMissing";
                RefreshPresentation();
                return false;
            }

            destination.transportCooldowns[id] = Time.unscaledTime + 0.25f;
            payload.transform.position = destination.transform.position +
                                         (Vector3)(destination.ResolveDirection() * 0.75f);
            var body = payload.GetComponentInParent<Rigidbody2D>();
            if (body != null)
                body.linearVelocity = destination.ResolveDirection() *
                                      (Kind == PostElementKind.ExpressTube ? Profile.LaunchPower : 0f);
            parcelsProcessed++;
            variantState = Kind == PostElementKind.ExpressTube ? "ExpressSent" : "ParcelSent";
            destination.variantState = "ParcelReceived";
            RefreshPresentation();
            destination.RefreshPresentation();
            return true;
        }

        private PostElementDriver FindPairDestination()
        {
            var list = GetPairList(Profile.PairGuid);
            if (list == null) return null;
            for (var index = 0; index < list.Count; index++)
            {
                var candidate = list[index];
                if (candidate != null && candidate != this && candidate.Kind == Kind && candidate.isActiveAndEnabled)
                    return candidate;
            }
            return null;
        }

        private bool IsOneWaySender()
        {
            var list = GetPairList(Profile.PairGuid);
            if (list == null) return false;
            for (var index = 0; index < list.Count; index++)
            {
                if (list[index] == null || list[index].Kind != Kind) continue;
                return list[index] == this;
            }
            return false;
        }

        private void RegisterPair()
        {
            if (Profile == null || !Profile.RequiresPair || string.IsNullOrWhiteSpace(Profile.PairGuid)) return;
            if (string.Equals(registeredPairGuid, Profile.PairGuid, StringComparison.Ordinal)) return;
            UnregisterPair();
            if (!PairRegistry.TryGetValue(Profile.PairGuid, out var list))
            {
                list = new List<PostElementDriver>();
                PairRegistry.Add(Profile.PairGuid, list);
            }
            if (!list.Contains(this)) list.Add(this);
            registeredPairGuid = Profile.PairGuid;
        }

        private void UnregisterPair()
        {
            if (string.IsNullOrWhiteSpace(registeredPairGuid)) return;
            if (PairRegistry.TryGetValue(registeredPairGuid, out var list))
            {
                list.Remove(this);
                if (list.Count == 0) PairRegistry.Remove(registeredPairGuid);
            }
            registeredPairGuid = string.Empty;
        }

        private static List<PostElementDriver> GetPairList(string pairGuid)
        {
            return !string.IsNullOrWhiteSpace(pairGuid) && PairRegistry.TryGetValue(pairGuid, out var list)
                ? list
                : null;
        }

        private bool IsCompatibleParcel(string parcelId)
        {
            return string.Equals(Profile.CompatibleParcelId, "*", StringComparison.Ordinal) ||
                   string.Equals(Profile.CompatibleParcelId, parcelId, StringComparison.Ordinal);
        }

        private static bool IsPlayer(GameObject target)
        {
            return FindInterface<IPostPlayerMarker>(target) != null || target.CompareTag("Player");
        }

        private static bool IsHeavy(GameObject target)
        {
            var parcel = FindInterface<IPostParcelPayload>(target);
            if (parcel != null && parcel.IsHeavyParcel) return true;
            var weight = FindInterface<IMapElementWeightSource>(target);
            if (weight != null) return weight.PressureWeight >= 2;
            var body = target.GetComponentInParent<Rigidbody2D>();
            return body != null && body.mass >= 2f;
        }

        private Vector2 ResolveDirection()
        {
            var direction = Profile != null ? Profile.Direction : Vector2Int.right;
            if (Kind == PostElementKind.SortingArm && Profile.RotationSequenceDegrees != null &&
                Profile.RotationSequenceDegrees.Count > 0)
            {
                var angle = Profile.RotationSequenceDegrees[
                    Mathf.Clamp(sortingSequenceIndex, 0, Profile.RotationSequenceDegrees.Count - 1)];
                return Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            }
            return direction == Vector2Int.zero ? Vector2.right : ((Vector2)direction).normalized;
        }

        private void RefreshPresentation()
        {
            if (!initialized || Profile == null) return;
            var tint = Color.white;
            if (variantState == "Warning") tint = new Color(1f, 0.72f, 0.28f);
            else if (variantState == "StampDown" || variantState == "Collapsed") tint = new Color(1f, 0.42f, 0.38f);
            else if (variantState == "HeavyStopped" || variantState == "Locked" || variantState == "PairMissing")
                tint = new Color(0.62f, 0.58f, 0.68f);
            else if (inkDiluted || expressActive || variantState.Contains("Sent") || variantState.Contains("Received"))
                tint = new Color(0.72f, 0.9f, 1f);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null) renderers[index].color = rendererBaseColors[index] * tint;
            }

            if (visualRoot != null)
            {
                visualRoot.localPosition = visualOrigin +
                    (Kind == PostElementKind.ReturnStamp && StampActive ? Vector3.down : Vector3.zero);
                visualRoot.localScale = Kind == PostElementKind.ParcelStack && parcelStackFlattened
                    ? new Vector3(visualOriginScale.x, visualOriginScale.y * Profile.FlattenedHeightMultiplier,
                        visualOriginScale.z)
                    : visualOriginScale;
                if (Kind == PostElementKind.SortingArm)
                    visualRoot.localRotation = Quaternion.Euler(0f, 0f, CurrentSortingAngle());
            }
            if (physicsRoot != null)
            {
                physicsRoot.localPosition = physicsOrigin +
                    (Kind == PostElementKind.ReturnStamp && StampActive ? Vector3.down : Vector3.zero);
                if (Kind == PostElementKind.SortingArm)
                    physicsRoot.localRotation = Quaternion.Euler(0f, 0f, CurrentSortingAngle());
            }
            if (triggerRoot != null)
            {
                triggerRoot.localPosition = triggerOrigin;
                if (Kind == PostElementKind.SortingArm)
                    triggerRoot.localRotation = Quaternion.Euler(0f, 0f, CurrentSortingAngle());
            }
        }

        private float CurrentSortingAngle()
        {
            return Profile.RotationSequenceDegrees != null && Profile.RotationSequenceDegrees.Count > 0
                ? Profile.RotationSequenceDegrees[
                    Mathf.Clamp(sortingSequenceIndex, 0, Profile.RotationSequenceDegrees.Count - 1)]
                : sortingSequenceIndex * Profile.RotationStepDegrees;
        }

        private static T FindInterface<T>(GameObject target) where T : class
        {
            if (target == null) return null;
            var behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
                if (behaviours[index] is T result) return result;
            return null;
        }

        [Serializable]
        private sealed class PersistentState
        {
            public string VariantState;
            public bool ConveyorStopped;
            public int SortingSequenceIndex;
            public bool InkDiluted;
            public bool ParcelStackFlattened;
            public bool ExpressActive;
            public int ParcelsProcessed;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PostParcelLaunchImpact : MonoBehaviour
    {
        private int damage;
        private GameObject source;
        private bool spent;

        public void Configure(int impactDamage, GameObject impactSource)
        {
            damage = Mathf.Clamp(impactDamage, 0, 1);
            source = impactSource;
            spent = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (spent || collision == null) return;
            spent = true;
            var behaviours = collision.gameObject.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (!(behaviours[index] is IMapElementDamageReceiver receiver)) continue;
                receiver.ReceiveMapElementDamage(new MapElementDamageEvent(
                    damage,
                    GetComponent<Rigidbody2D>() != null ? GetComponent<Rigidbody2D>().linearVelocity : Vector2.zero,
                    source,
                    gameObject.GetInstanceID()));
                break;
            }
            Destroy(this);
        }
    }
}

#endif
