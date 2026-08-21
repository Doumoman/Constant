#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    public interface IPolarisBeamMirror
    {
        Vector2 ReflectObservationBeam(Vector2 incomingDirection);
    }

    public interface IPolarisObservationReceiver
    {
        void ReceiveObservationBeam(bool active);
    }

    public interface IPolarisReturnMarkReceiver
    {
        void ApplyReturnMark();
    }

    public interface IPolarisGravityReceiver
    {
        void ApplyGravityScale(float gravityScale);
    }

    public interface IPolarisArtifactPayload
    {
        string ArtifactId { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class PolarisElementDriver : MonoBehaviour,
        IMapElementSignalReceiver,
        IMapElementInteractionReceiver,
        IMapElementPersistentParticipant,
        IMapElementWeightSource
    {
        [SerializeField] private string variantState = string.Empty;
        [SerializeField] private float orbitElapsed;
        [SerializeField] private bool alternateOrbit;
        [SerializeField] private float beamElapsed;
        [SerializeField] private float beamAngle;
        [SerializeField] private int beamSweepDirection = 1;
        [SerializeField] private bool lowGravity;
        [SerializeField] private int generatedBridgeCells;
        [SerializeField] private int rhythmInputIndex;
        [SerializeField] private bool memoryReplayComplete;
        [SerializeField] private bool starWeightCarryReady;

        private readonly Dictionary<int, PendingReturn> pendingReturns =
            new Dictionary<int, PendingReturn>();
        private readonly HashSet<int> beamDamagedTargets = new HashSet<int>();
        private readonly HashSet<int> crushDamagedTargets = new HashSet<int>();
        private MapElementInstance element;
        private Rigidbody2D body;
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Color[] rendererBaseColors = Array.Empty<Color>();
        private Collider2D[] physicsColliders = Array.Empty<Collider2D>();
        private Transform visualRoot;
        private Transform physicsRoot;
        private Transform triggerRoot;
        private Vector3 originPosition;
        private Vector3 visualOriginScale;
        private Vector3 physicsOriginScale;
        private Vector3 triggerOriginScale;
        private Transform entryAnchor;
        private bool initialized;

        public event Action<float> GravityScaleRequested;

        public PolarisElementKind Kind => Profile != null ? Profile.Kind : PolarisElementKind.None;
        public string VariantState => variantState;
        public bool AlternateOrbit => alternateOrbit;
        public float BeamAngle => beamAngle;
        public Vector2 BeamDirection => Quaternion.Euler(0f, 0f, beamAngle) * Vector2.right;
        public bool LowGravity => lowGravity;
        public float CurrentGravityScale => lowGravity ? Profile.LowGravityScale : Profile.NormalGravityScale;
        public int GeneratedBridgeCells => generatedBridgeCells;
        public int RhythmInputIndex => rhythmInputIndex;
        public bool MemoryReplayComplete => memoryReplayComplete;
        public bool StarWeightCarryReady => starWeightCarryReady;
        public int PendingReturnCount => pendingReturns.Count;
        public int PressureWeight => Kind == PolarisElementKind.StarWeight
            ? Mathf.Clamp(Profile.PressureWeight, 1, 2)
            : 1;
        public string PersistenceId => element != null ? $"{element.PersistenceId}:polaris" : string.Empty;
        public string InteractionPrompt => Kind switch
        {
            PolarisElementKind.StarWeight => "[X] Carry star weight",
            PolarisElementKind.GravityDial => "[X] Toggle gravity",
            PolarisElementKind.ConstellationBridge => "[X] Use artifact",
            PolarisElementKind.MemoryBell => "[X] Enter rhythm",
            _ => string.Empty,
        };

        private PolarisElementRuntimeProfile Profile =>
            element != null && element.Definition != null ? element.Definition.PolarisProfile : null;

        private void Awake() => Initialize();

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
            initialized = false;
            Initialize();
        }

        public void TickForTests(float deltaSeconds)
        {
            Initialize();
            Tick(Mathf.Max(0f, deltaSeconds));
        }

        public void ConfigureEntryAnchor(Transform anchor)
        {
            entryAnchor = anchor;
        }

        public bool ApplyToolReaction(ToolReactionEntry entry, ToolReactionContext context)
        {
            Initialize();
            if (entry == null || Profile == null || Profile.IgnoreAllTools ||
                element.CurrentState == MapElementState.Broken)
            {
                return false;
            }

            switch (Kind)
            {
                case PolarisElementKind.StarWeight when (context.Tags & ToolTag.Context) != 0:
                    starWeightCarryReady = true;
                    variantState = "HeavyCarryReady";
                    RefreshPresentation();
                    return true;

                case PolarisElementKind.StarWeight when (context.Tags & ToolTag.Hook) != 0:
                    var pullDirection = context.Direction == Vector2Int.zero
                        ? Vector2.left
                        : ((Vector2)context.Direction).normalized;
                    transform.position += (Vector3)(pullDirection * Mathf.Max(1f, context.Magnitude));
                    variantState = "HookPulled";
                    RefreshPresentation();
                    return true;

                case PolarisElementKind.GravityDial
                    when (context.Tags & (ToolTag.Context | ToolTag.Hook)) != 0:
                    ToggleGravity();
                    return true;

                case PolarisElementKind.ConstellationBridge when (context.Tags & ToolTag.Context) != 0:
                    GenerateBridgeCell(context.Source != null ? context.Source : context.Instigator);
                    return true;

                case PolarisElementKind.MemoryBell when (context.Tags & ToolTag.Context) != 0:
                    var input = Mathf.Clamp(Mathf.RoundToInt(context.Magnitude), 0, 9);
                    SubmitRhythmInput(input);
                    return true;
            }

            return false;
        }

        public bool TryInteract(GameObject instigator)
        {
            Initialize();
            switch (Kind)
            {
                case PolarisElementKind.StarWeight:
                    starWeightCarryReady = true;
                    variantState = "HeavyCarryReady";
                    RefreshPresentation();
                    return true;
                case PolarisElementKind.GravityDial:
                    ToggleGravity();
                    return true;
                case PolarisElementKind.ConstellationBridge:
                    return GenerateBridgeCell(instigator);
                case PolarisElementKind.MemoryBell:
                    var expected = Profile.RhythmPattern != null &&
                                   rhythmInputIndex < Profile.RhythmPattern.Count
                        ? Profile.RhythmPattern[rhythmInputIndex]
                        : 0;
                    return SubmitRhythmInput(expected);
                default:
                    return false;
            }
        }

        public void ReceiveSignal(string channel, bool active)
        {
            Initialize();
            switch (Kind)
            {
                case PolarisElementKind.OrbitPlatform:
                    alternateOrbit = active;
                    variantState = active ? "DialOrbit" : "NormalOrbit";
                    RefreshPresentation();
                    break;
                case PolarisElementKind.ObservationBeam when active && Profile.SignalChangesDirection:
                    beamSweepDirection *= -1;
                    variantState = "SignalReversed";
                    RefreshPresentation();
                    break;
                case PolarisElementKind.GravityDial:
                    SetGravity(active);
                    break;
                case PolarisElementKind.ConstellationBridge:
                    generatedBridgeCells = active ? Profile.BridgeCellCount : 0;
                    variantState = active ? "BridgeComplete" : "BridgeOff";
                    element.TrySetState(active ? MapElementState.Active : MapElementState.Idle);
                    RefreshPresentation();
                    break;
            }
        }

        public bool TryReflectBeam(GameObject mirrorObject)
        {
            Initialize();
            if (Kind != PolarisElementKind.ObservationBeam || !Profile.MirrorCanReflect ||
                mirrorObject == null) return false;
            var mirror = FindInterface<IPolarisBeamMirror>(mirrorObject);
            if (mirror == null) return false;
            var reflected = mirror.ReflectObservationBeam(BeamDirection);
            if (reflected.sqrMagnitude <= 0.0001f) return false;
            beamAngle = Mathf.Atan2(reflected.y, reflected.x) * Mathf.Rad2Deg;
            variantState = "MirrorReflected";
            RefreshPresentation();
            return true;
        }

        public bool GenerateBridgeCell(GameObject artifactObject)
        {
            Initialize();
            if (Kind != PolarisElementKind.ConstellationBridge || Profile == null) return false;
            if (artifactObject == null ||
                FindInterface<IPolarisArtifactPayload>(artifactObject) == null) return false;
            if (generatedBridgeCells >= Profile.BridgeCellCount) return false;
            generatedBridgeCells++;
            variantState = generatedBridgeCells >= Profile.BridgeCellCount
                ? "BridgeComplete"
                : "BridgeGrowing";
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
            return true;
        }

        public bool SubmitRhythmInput(int input)
        {
            Initialize();
            if (Kind != PolarisElementKind.MemoryBell || Profile.RhythmPattern == null ||
                Profile.RhythmPattern.Count == 0) return false;
            if (input != Profile.RhythmPattern[rhythmInputIndex])
            {
                rhythmInputIndex = 0;
                memoryReplayComplete = false;
                variantState = "RhythmReset";
                RefreshPresentation();
                return false;
            }
            rhythmInputIndex++;
            if (rhythmInputIndex >= Profile.RhythmPattern.Count)
            {
                rhythmInputIndex = 0;
                memoryReplayComplete = true;
                variantState = "MemoryReplayed";
                element.TrySetState(MapElementState.Active);
            }
            else
            {
                variantState = "RhythmAccepted";
            }
            RefreshPresentation();
            return true;
        }

        public void NotifyTriggerEnter(Collider2D other)
        {
            Initialize();
            if (other == null || Profile == null || Kind != PolarisElementKind.ReturnField) return;
            var id = other.gameObject.GetInstanceID();
            pendingReturns[id] = new PendingReturn
            {
                Target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject,
                Remaining = Profile.ReturnDelaySeconds,
            };
            variantState = "ReturnScheduled";
            RefreshPresentation();
        }

        public void NotifyTriggerExit(Collider2D other)
        {
            if (other == null) return;
            pendingReturns.Remove(other.gameObject.GetInstanceID());
        }

        public void NotifyTriggerStay(Collider2D other)
        {
            Initialize();
            if (other == null || Profile == null || Kind != PolarisElementKind.ObservationBeam) return;
            var observation = FindInterface<IPolarisObservationReceiver>(other.gameObject);
            observation?.ReceiveObservationBeam(true);
            if (Profile.AppliesReturnMark)
            {
                var mark = FindInterface<IPolarisReturnMarkReceiver>(other.gameObject);
                mark?.ApplyReturnMark();
            }
            DamageBeamTarget(other);
        }

        public void NotifyCollisionEnter(Collision2D collision)
        {
            Initialize();
            if (collision == null || Profile == null || Kind != PolarisElementKind.StarWeight) return;
            var target = collision.gameObject;
            var id = target.GetInstanceID();
            if (!crushDamagedTargets.Add(id)) return;
            var receiver = FindInterface<IMapElementDamageReceiver>(target);
            receiver?.ReceiveMapElementDamage(new MapElementDamageEvent(
                Profile.CrushDamage,
                Profile.GravityDirection == Vector2Int.zero
                    ? Vector2.down * 3f
                    : ((Vector2)Profile.GravityDirection).normalized * 3f,
                gameObject,
                id));
        }

        public string CaptureMapElementState()
        {
            return JsonUtility.ToJson(new PersistentState
            {
                VariantState = variantState,
                OrbitElapsed = orbitElapsed,
                AlternateOrbit = alternateOrbit,
                BeamElapsed = beamElapsed,
                BeamAngle = beamAngle,
                BeamSweepDirection = beamSweepDirection,
                LowGravity = lowGravity,
                GeneratedBridgeCells = generatedBridgeCells,
                RhythmInputIndex = rhythmInputIndex,
                MemoryReplayComplete = memoryReplayComplete,
                StarWeightCarryReady = starWeightCarryReady,
            });
        }

        public void RestoreMapElementState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            Initialize();
            var state = JsonUtility.FromJson<PersistentState>(payload);
            if (state == null) return;
            variantState = state.VariantState ?? string.Empty;
            orbitElapsed = Mathf.Max(0f, state.OrbitElapsed);
            alternateOrbit = state.AlternateOrbit;
            beamElapsed = Mathf.Max(0f, state.BeamElapsed);
            beamAngle = state.BeamAngle;
            beamSweepDirection = state.BeamSweepDirection == 0 ? 1 : state.BeamSweepDirection;
            lowGravity = state.LowGravity;
            generatedBridgeCells = Mathf.Clamp(state.GeneratedBridgeCells, 0,
                Profile != null ? Profile.BridgeCellCount : int.MaxValue);
            rhythmInputIndex = Mathf.Max(0, state.RhythmInputIndex);
            memoryReplayComplete = state.MemoryReplayComplete;
            starWeightCarryReady = state.StarWeightCarryReady;
            RefreshPresentation();
        }

        private void Initialize()
        {
            if (initialized) return;
            element = GetComponent<MapElementInstance>();
            body = GetComponent<Rigidbody2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            rendererBaseColors = new Color[renderers.Length];
            for (var index = 0; index < renderers.Length; index++)
                rendererBaseColors[index] = renderers[index] != null ? renderers[index].color : Color.white;
            visualRoot = transform.Find("VisualRoot");
            physicsRoot = transform.Find("PhysicsRoot");
            triggerRoot = transform.Find("TriggerRoot");
            physicsColliders = physicsRoot != null
                ? physicsRoot.GetComponentsInChildren<Collider2D>(true)
                : Array.Empty<Collider2D>();
            originPosition = transform.position;
            visualOriginScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            physicsOriginScale = physicsRoot != null ? physicsRoot.localScale : Vector3.one;
            triggerOriginScale = triggerRoot != null ? triggerRoot.localScale : Vector3.one;
            if (Profile != null)
            {
                lowGravity = Profile.Kind == PolarisElementKind.GravityDial && Profile.StartsLowGravity;
                generatedBridgeCells = Profile.Kind == PolarisElementKind.ConstellationBridge &&
                                       Profile.StartsBridgeActive
                    ? Profile.BridgeCellCount
                    : 0;
                if (Profile.Kind == PolarisElementKind.StarWeight && body != null)
                {
                    body.mass = Mathf.Max(1f, Profile.Mass);
                    body.gravityScale = 0f;
                }
            }
            initialized = true;
            ResolveEntryAnchor();
            RefreshPresentation();
        }

        private void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Profile == null) return;
            switch (Kind)
            {
                case PolarisElementKind.OrbitPlatform:
                    orbitElapsed = Mathf.Repeat(orbitElapsed + deltaSeconds, Profile.OrbitPeriodSeconds);
                    var radians = orbitElapsed / Profile.OrbitPeriodSeconds * Mathf.PI * 2f;
                    var radius = Profile.OrbitRadiusCells * (alternateOrbit ? Profile.DialOrbitMultiplier : 1f);
                    var nextPosition = originPosition + new Vector3(
                        Mathf.Cos(radians) * radius.x,
                        Mathf.Sin(radians) * radius.y,
                        0f);
                    if (body != null) body.MovePosition(nextPosition);
                    else transform.position = nextPosition;
                    break;

                case PolarisElementKind.ObservationBeam:
                    beamElapsed = Mathf.Repeat(beamElapsed + deltaSeconds, Profile.SweepPeriodSeconds);
                    var normalized = beamElapsed / Profile.SweepPeriodSeconds;
                    beamAngle = (Mathf.PingPong(normalized * 2f, 1f) - 0.5f) *
                                Profile.SweepDegrees * beamSweepDirection;
                    if (beamElapsed < deltaSeconds) beamDamagedTargets.Clear();
                    RefreshPresentation();
                    break;

                case PolarisElementKind.ReturnField:
                    TickPendingReturns(deltaSeconds);
                    break;

                case PolarisElementKind.StarWeight:
                    if (body != null)
                    {
                        var direction = Profile.GravityDirection == Vector2Int.zero
                            ? Vector2.down
                            : ((Vector2)Profile.GravityDirection).normalized;
                        body.linearVelocity += direction * Physics2D.gravity.magnitude * deltaSeconds;
                    }
                    break;
            }
        }

        private void TickPendingReturns(float deltaSeconds)
        {
            if (pendingReturns.Count == 0) return;
            var completed = new List<int>();
            var updates = new List<KeyValuePair<int, PendingReturn>>(pendingReturns);
            for (var index = 0; index < updates.Count; index++)
            {
                var id = updates[index].Key;
                var pending = updates[index].Value;
                if (pending.Target == null)
                {
                    completed.Add(id);
                    continue;
                }
                pending.Remaining -= deltaSeconds;
                if (pending.Remaining > 0f)
                {
                    pendingReturns[id] = pending;
                    continue;
                }
                ResolveEntryAnchor();
                if (entryAnchor != null)
                {
                    pending.Target.transform.position = entryAnchor.position;
                    var targetBody = pending.Target.GetComponent<Rigidbody2D>();
                    if (targetBody != null) targetBody.linearVelocity = Vector2.zero;
                    variantState = "ReturnedToEntry";
                }
                else
                {
                    variantState = "EntryAnchorMissing";
                }
                completed.Add(id);
            }
            for (var index = 0; index < completed.Count; index++) pendingReturns.Remove(completed[index]);
            RefreshPresentation();
        }

        private void ResolveEntryAnchor()
        {
            if (entryAnchor != null || Profile == null ||
                string.IsNullOrWhiteSpace(Profile.DestinationAnchorId)) return;
            var anchorObject = GameObject.Find(Profile.DestinationAnchorId);
            if (anchorObject != null) entryAnchor = anchorObject.transform;
        }

        private void ToggleGravity() => SetGravity(!lowGravity);

        private void SetGravity(bool low)
        {
            lowGravity = low;
            variantState = low ? "LowGravity" : "NormalGravity";
            element.TrySetState(MapElementState.Active);
            GravityScaleRequested?.Invoke(CurrentGravityScale);
            RefreshPresentation();
        }

        private void DamageBeamTarget(Collider2D other)
        {
            var id = other.gameObject.GetInstanceID();
            if (!beamDamagedTargets.Add(id)) return;
            var receiver = FindInterface<IMapElementDamageReceiver>(other.gameObject);
            receiver?.ReceiveMapElementDamage(new MapElementDamageEvent(
                Profile.Damage,
                BeamDirection * 2f,
                gameObject,
                id));
        }

        private void RefreshPresentation()
        {
            if (!initialized || Profile == null) return;
            var tint = Color.white;
            if (variantState == "LowGravity" || variantState == "DialOrbit")
                tint = new Color(0.58f, 0.78f, 1f);
            else if (variantState == "MirrorReflected" || variantState == "SignalReversed")
                tint = new Color(0.72f, 0.92f, 1f);
            else if (variantState == "EntryAnchorMissing" || variantState == "RhythmReset")
                tint = new Color(1f, 0.42f, 0.42f);
            else if (variantState == "BridgeComplete" || variantState == "MemoryReplayed")
                tint = new Color(1f, 0.86f, 0.48f);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null) renderers[index].color = rendererBaseColors[index] * tint;
            }
            if (visualRoot != null && Kind == PolarisElementKind.ObservationBeam)
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, beamAngle);
            ApplyBridgeScale(visualRoot, visualOriginScale);
            ApplyBridgeScale(physicsRoot, physicsOriginScale);
            ApplyBridgeScale(triggerRoot, triggerOriginScale);
            if (Kind == PolarisElementKind.ConstellationBridge)
            {
                var enabled = generatedBridgeCells > 0;
                for (var index = 0; index < physicsColliders.Length; index++)
                    if (physicsColliders[index] != null) physicsColliders[index].enabled = enabled;
            }
        }

        private void ApplyBridgeScale(Transform target, Vector3 originalScale)
        {
            if (target == null || Kind != PolarisElementKind.ConstellationBridge) return;
            var ratio = Profile.BridgeCellCount > 0
                ? Mathf.Clamp01(generatedBridgeCells / (float)Profile.BridgeCellCount)
                : 1f;
            target.localScale = new Vector3(originalScale.x * Mathf.Max(0.01f, ratio),
                originalScale.y, originalScale.z);
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
            public float OrbitElapsed;
            public bool AlternateOrbit;
            public float BeamElapsed;
            public float BeamAngle;
            public int BeamSweepDirection;
            public bool LowGravity;
            public int GeneratedBridgeCells;
            public int RhythmInputIndex;
            public bool MemoryReplayComplete;
            public bool StarWeightCarryReady;
        }

        private struct PendingReturn
        {
            public GameObject Target;
            public float Remaining;
        }
    }

}

#endif
