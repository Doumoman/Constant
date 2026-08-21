#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    public interface ISunLightSensitive
    {
        void ReceiveSunlight(bool active, float intensity);
    }

    public interface ISunShadowReceiver
    {
        void ReceiveShadow(bool active);
    }

    public interface ISunCoolingReceiver
    {
        void ReceiveCooling(float amount);
    }

    public interface ISunWateringCanReceiver
    {
        void RefillWateringCanFully();
    }

    public interface ISunPerchOffering
    {
        string ContextId { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class SunElementDriver : MonoBehaviour,
        IMapElementSignalReceiver,
        IMapElementInteractionReceiver,
        IMapElementPersistentParticipant
    {
        [SerializeField] private string variantState = string.Empty;
        [SerializeField] private float sunbeamAngle;
        [SerializeField] private float cycleElapsed;
        [SerializeField] private float shadowLifetimeRemaining;
        [SerializeField] private bool shadowActive;
        [SerializeField] private bool sunflowerBloomed;
        [SerializeField] private int sunflowerRotationSteps;
        [SerializeField] private int vineLengthCells = 1;
        [SerializeField] private float dewElapsed;
        [SerializeField] private int dewDropsSpawned;
        [SerializeField] private float cooledRemaining;
        [SerializeField] private bool overheatActive;
        [SerializeField] private SunPhase sunsetPhase;
        [SerializeField] private string acceptedPerchOffering = string.Empty;

        private readonly HashSet<int> damagedTargets = new HashSet<int>();
        private MapElementInstance element;
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Color[] rendererBaseColors = Array.Empty<Color>();
        private Collider2D[] colliders = Array.Empty<Collider2D>();
        private Transform visualRoot;
        private Transform physicsRoot;
        private Transform triggerRoot;
        private Vector3 visualOriginScale;
        private Vector3 physicsOriginScale;
        private Vector3 triggerOriginScale;
        private bool previousSunbeamActive;
        private bool initialized;

        public event Action<SunPhase> SunsetPhaseChanged;

        public SunElementKind Kind => Profile != null ? Profile.Kind : SunElementKind.None;
        public string VariantState => variantState;
        public float SunbeamAngle => sunbeamAngle;
        public bool SunbeamActive => Kind == SunElementKind.RotatingSunbeam &&
                                     cycleElapsed < Profile.CycleOnSeconds;
        public bool ShadowActive => shadowActive;
        public bool SunflowerBloomed => sunflowerBloomed;
        public int SunflowerRotationSteps => sunflowerRotationSteps;
        public int VineLengthCells => vineLengthCells;
        public int DewDropsSpawned => dewDropsSpawned;
        public bool OverheatActive => overheatActive;
        public SunPhase SunsetPhase => sunsetPhase;
        public string AcceptedPerchOffering => acceptedPerchOffering;
        public string PersistenceId => element != null ? $"{element.PersistenceId}:sun" : string.Empty;
        public string InteractionPrompt => Kind switch
        {
            SunElementKind.CrowPerch => "[X] Offer letter or sun ember",
            SunElementKind.GrowthVine => "[X] Pull vine",
            _ => string.Empty,
        };

        private SunElementRuntimeProfile Profile =>
            element != null && element.Definition != null ? element.Definition.SunProfile : null;

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

        public bool ApplyToolReaction(ToolReactionEntry entry, ToolReactionContext context)
        {
            Initialize();
            if (entry == null || Profile == null || element.CurrentState == MapElementState.Broken)
            {
                return false;
            }

            switch (Kind)
            {
                case SunElementKind.ShadowSeed when (context.Tags & ToolTag.Water) != 0:
                    shadowActive = false;
                    shadowLifetimeRemaining = 0f;
                    variantState = "WaterSuppressed";
                    element.TrySetState(MapElementState.Disabled);
                    RefreshPresentation();
                    return true;

                case SunElementKind.GrowthVine when (context.Tags & ToolTag.Water) != 0:
                    return GrowVine("WaterGrowth");

                case SunElementKind.GrowthVine when (context.Tags & (ToolTag.Pickaxe | ToolTag.Shovel)) != 0:
                    vineLengthCells = 0;
                    variantState = "Removed";
                    element.TrySetState(MapElementState.Broken);
                    RefreshPresentation();
                    return true;

                case SunElementKind.GrowthVine when (context.Tags & ToolTag.Hook) != 0:
                    if (vineLengthCells < Profile.MaxLengthCells) vineLengthCells++;
                    variantState = "HookPulled";
                    element.TrySetState(MapElementState.Active);
                    RefreshPresentation();
                    return true;

                case SunElementKind.OverheatPlatform when (context.Tags & ToolTag.Water) != 0:
                    cooledRemaining = Profile.WaterCoolSeconds;
                    overheatActive = false;
                    cycleElapsed = 0f;
                    damagedTargets.Clear();
                    variantState = "WaterCooled";
                    element.TrySetState(MapElementState.Disabled);
                    RefreshPresentation();
                    return true;

                case SunElementKind.DewDrop when (context.Tags & ToolTag.Context) != 0:
                    variantState = "WateringCanCharged";
                    element.TrySetState(MapElementState.Active);
                    RefreshPresentation();
                    return true;

                case SunElementKind.CrowPerch when (context.Tags & ToolTag.Context) != 0:
                    var offering = context.Source != null ? context.Source : context.Instigator;
                    if (offering != null && TryOfferToPerch(offering)) return true;
                    variantState = "AwaitingLetterOrEmber";
                    RefreshPresentation();
                    return true;
            }

            return false;
        }

        public bool TryInteract(GameObject instigator)
        {
            Initialize();
            if (Kind == SunElementKind.CrowPerch && instigator != null)
                return TryOfferToPerch(instigator);
            if (Kind == SunElementKind.GrowthVine)
            {
                if (vineLengthCells < Profile.MaxLengthCells) vineLengthCells++;
                variantState = "HookPulled";
                RefreshPresentation();
                return true;
            }
            return false;
        }

        public void ReceiveSignal(string channel, bool active)
        {
            Initialize();
            switch (Kind)
            {
                case SunElementKind.RotatingSunbeam when active && Profile.RotateOnSignal:
                    sunbeamAngle = Mathf.Clamp(
                        sunbeamAngle + 90f,
                        -Profile.ArcDegrees * 0.5f,
                        Profile.ArcDegrees * 0.5f);
                    variantState = "SignalRotated";
                    RefreshPresentation();
                    break;

                case SunElementKind.SunflowerPlatform:
                    if (!string.IsNullOrWhiteSpace(channel) &&
                        channel.IndexOf("overheat", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        sunflowerBloomed = !active;
                        variantState = active ? "OverheatClosed" : "Bloomed";
                    }
                    else
                    {
                        sunflowerBloomed = active && Profile.BloomsInLight;
                        if (sunflowerBloomed) sunflowerRotationSteps = (sunflowerRotationSteps + 1) % 4;
                        variantState = sunflowerBloomed ? "Bloomed" : "Closed";
                    }
                    RefreshPresentation();
                    break;

                case SunElementKind.GrowthVine when active:
                    GrowVine("SignalGrowth");
                    break;

                case SunElementKind.SunsetFlower:
                    var shadowSignal = !string.IsNullOrWhiteSpace(channel) &&
                                       channel.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0;
                    SetSunsetPhase(shadowSignal && active ? SunPhase.Shadow : SunPhase.Day);
                    break;
            }
        }

        public GameObject SpawnDewDrop()
        {
            Initialize();
            if (Kind != SunElementKind.DewDrop || Profile == null) return null;
            var drop = new GameObject($"{name}_DewDrop_{++dewDropsSpawned}");
            drop.transform.position = transform.position + Vector3.down * 0.5f;
            var body = drop.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 1f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = drop.AddComponent<CircleCollider2D>();
            collider.radius = 0.22f;
            var projectile = drop.AddComponent<SunDewDropProjectile>();
            projectile.Configure(Profile.CoolOnImpact, Profile.CanFullyRefillWateringCan,
                Profile.ThrownWaterMagnitude, gameObject);
            return drop;
        }

        public bool TryOfferToPerch(GameObject offeringObject)
        {
            Initialize();
            if (Kind != SunElementKind.CrowPerch || offeringObject == null || Profile == null) return false;
            var offering = FindInterface<ISunPerchOffering>(offeringObject);
            if (offering == null || Profile.AcceptedContextIds == null ||
                !Profile.AcceptedContextIds.Contains(offering.ContextId)) return false;
            acceptedPerchOffering = offering.ContextId;
            variantState = string.Equals(offering.ContextId, "letter", StringComparison.Ordinal)
                ? "LetterAccepted"
                : "SunEmberAccepted";
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
            return true;
        }

        public void NotifyTriggerStay(Collider2D other)
        {
            Initialize();
            if (other == null || Profile == null) return;
            if (Kind == SunElementKind.RotatingSunbeam && SunbeamActive)
            {
                var lightSensitive = FindInterface<ISunLightSensitive>(other.gameObject);
                lightSensitive?.ReceiveSunlight(true, 1f);
                DamageTargetOnce(other, Profile.Damage, ResolveSunbeamDirection() * 3f);
            }
            else if (Kind == SunElementKind.ShadowSeed && shadowActive)
            {
                var shadowReceiver = FindInterface<ISunShadowReceiver>(other.gameObject);
                shadowReceiver?.ReceiveShadow(true);
            }
            else if (Kind == SunElementKind.OverheatPlatform && overheatActive)
            {
                DamageTargetOnce(other, Profile.Damage, Vector2.up * 2f);
            }
        }

        public string CaptureMapElementState()
        {
            return JsonUtility.ToJson(new PersistentState
            {
                VariantState = variantState,
                SunbeamAngle = sunbeamAngle,
                CycleElapsed = cycleElapsed,
                ShadowLifetimeRemaining = shadowLifetimeRemaining,
                ShadowActive = shadowActive,
                SunflowerBloomed = sunflowerBloomed,
                SunflowerRotationSteps = sunflowerRotationSteps,
                VineLengthCells = vineLengthCells,
                CooledRemaining = cooledRemaining,
                SunsetPhase = sunsetPhase,
                AcceptedPerchOffering = acceptedPerchOffering,
            });
        }

        public void RestoreMapElementState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            Initialize();
            var state = JsonUtility.FromJson<PersistentState>(payload);
            if (state == null) return;
            variantState = state.VariantState ?? string.Empty;
            sunbeamAngle = state.SunbeamAngle;
            cycleElapsed = Mathf.Max(0f, state.CycleElapsed);
            shadowLifetimeRemaining = Mathf.Max(0f, state.ShadowLifetimeRemaining);
            shadowActive = state.ShadowActive;
            sunflowerBloomed = state.SunflowerBloomed;
            sunflowerRotationSteps = Mathf.Max(0, state.SunflowerRotationSteps);
            vineLengthCells = Mathf.Clamp(state.VineLengthCells, 0,
                Profile != null ? Profile.MaxLengthCells : int.MaxValue);
            cooledRemaining = Mathf.Max(0f, state.CooledRemaining);
            sunsetPhase = state.SunsetPhase;
            acceptedPerchOffering = state.AcceptedPerchOffering ?? string.Empty;
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
            colliders = GetComponentsInChildren<Collider2D>(true);
            visualRoot = transform.Find("VisualRoot");
            physicsRoot = transform.Find("PhysicsRoot");
            triggerRoot = transform.Find("TriggerRoot");
            visualOriginScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            physicsOriginScale = physicsRoot != null ? physicsRoot.localScale : Vector3.one;
            triggerOriginScale = triggerRoot != null ? triggerRoot.localScale : Vector3.one;
            if (Profile != null)
            {
                vineLengthCells = Mathf.Clamp(Profile.StartLengthCells, 1, Profile.MaxLengthCells);
                shadowActive = Profile.Kind == SunElementKind.ShadowSeed;
                shadowLifetimeRemaining = shadowActive ? Profile.ShadowLifetimeSeconds : 0f;
                sunsetPhase = Profile.InitialPhase;
            }
            initialized = true;
            RefreshPresentation();
        }

        private void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Profile == null) return;
            switch (Kind)
            {
                case SunElementKind.RotatingSunbeam:
                    var totalSunbeamCycle = Profile.CycleOnSeconds + Profile.CycleOffSeconds;
                    cycleElapsed = Mathf.Repeat(cycleElapsed + deltaSeconds, totalSunbeamCycle);
                    sunbeamAngle = Mathf.PingPong(
                        Time.time * Profile.RotationSpeedDegreesPerSecond,
                        Profile.ArcDegrees) - Profile.ArcDegrees * 0.5f;
                    if (previousSunbeamActive != SunbeamActive)
                    {
                        previousSunbeamActive = SunbeamActive;
                        damagedTargets.Clear();
                    }
                    variantState = SunbeamActive ? "BeamOn" : "BeamOff";
                    RefreshPresentation();
                    break;

                case SunElementKind.ShadowSeed when shadowActive && Profile.ShadowLifetimeSeconds > 0f:
                    shadowLifetimeRemaining = Mathf.Max(0f, shadowLifetimeRemaining - deltaSeconds);
                    if (shadowLifetimeRemaining <= 0f)
                    {
                        shadowActive = false;
                        variantState = "Expired";
                        element.TrySetState(MapElementState.Disabled);
                        RefreshPresentation();
                    }
                    break;

                case SunElementKind.DewDrop:
                    dewElapsed += deltaSeconds;
                    if (dewElapsed >= Profile.FallIntervalSeconds)
                    {
                        dewElapsed = Mathf.Repeat(dewElapsed, Profile.FallIntervalSeconds);
                        SpawnDewDrop();
                    }
                    break;

                case SunElementKind.OverheatPlatform:
                    if (cooledRemaining > 0f)
                    {
                        cooledRemaining = Mathf.Max(0f, cooledRemaining - deltaSeconds);
                        overheatActive = false;
                        variantState = "WaterCooled";
                    }
                    else
                    {
                        var totalOverheatCycle = Profile.SafeSeconds + Profile.OverheatSeconds;
                        cycleElapsed = Mathf.Repeat(cycleElapsed + deltaSeconds, totalOverheatCycle);
                        var nextOverheat = cycleElapsed >= Profile.SafeSeconds;
                        if (nextOverheat != overheatActive) damagedTargets.Clear();
                        overheatActive = nextOverheat;
                        variantState = overheatActive ? "Overheated" : "Safe";
                    }
                    RefreshPresentation();
                    break;
            }
        }

        private bool GrowVine(string state)
        {
            if (vineLengthCells >= Profile.MaxLengthCells) return false;
            vineLengthCells++;
            variantState = state;
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
            return true;
        }

        private void SetSunsetPhase(SunPhase phase)
        {
            if (sunsetPhase == phase) return;
            sunsetPhase = phase;
            variantState = phase == SunPhase.Day ? "DaySignal" : "ShadowSignal";
            element.TrySetState(MapElementState.Active);
            SunsetPhaseChanged?.Invoke(phase);
            RefreshPresentation();
        }

        private void DamageTargetOnce(Collider2D other, int damage, Vector2 knockback)
        {
            var id = other.gameObject.GetInstanceID();
            if (!damagedTargets.Add(id)) return;
            var receiver = FindInterface<IMapElementDamageReceiver>(other.gameObject);
            receiver?.ReceiveMapElementDamage(new MapElementDamageEvent(
                damage,
                knockback,
                gameObject,
                id));
        }

        private Vector2 ResolveSunbeamDirection()
        {
            return Quaternion.Euler(0f, 0f, sunbeamAngle) * Vector2.right;
        }

        private void RefreshPresentation()
        {
            if (!initialized || Profile == null) return;
            var tint = Color.white;
            if (variantState == "BeamOn" || variantState == "Bloomed" || sunsetPhase == SunPhase.Day)
                tint = new Color(1f, 0.86f, 0.42f);
            if (variantState == "Overheated") tint = new Color(1f, 0.34f, 0.18f);
            else if (variantState == "WaterCooled" || variantState == "WaterSuppressed")
                tint = new Color(0.48f, 0.82f, 1f);
            else if (shadowActive || sunsetPhase == SunPhase.Shadow)
                tint = new Color(0.42f, 0.32f, 0.62f);
            else if (variantState == "Removed" || variantState == "Expired")
                tint = new Color(0.4f, 0.4f, 0.4f, 0.25f);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null) renderers[index].color = rendererBaseColors[index] * tint;
            }

            if (visualRoot != null)
            {
                visualRoot.localRotation = Kind == SunElementKind.RotatingSunbeam
                    ? Quaternion.Euler(0f, 0f, sunbeamAngle)
                    : Kind == SunElementKind.SunflowerPlatform
                        ? Quaternion.Euler(0f, 0f, sunflowerRotationSteps * Profile.PlatformRotationStepDegrees)
                        : Quaternion.identity;
            }
            ApplyVineScale(visualRoot, visualOriginScale);
            ApplyVineScale(physicsRoot, physicsOriginScale);
            ApplyVineScale(triggerRoot, triggerOriginScale);
            if (Kind == SunElementKind.GrowthVine && vineLengthCells <= 0)
            {
                for (var index = 0; index < colliders.Length; index++)
                    if (colliders[index] != null) colliders[index].enabled = false;
            }
        }

        private void ApplyVineScale(Transform target, Vector3 originalScale)
        {
            if (target == null || Kind != SunElementKind.GrowthVine) return;
            var ratio = Profile.MaxLengthCells > 0
                ? Mathf.Clamp01(vineLengthCells / (float)Profile.MaxLengthCells)
                : 1f;
            target.localScale = Mathf.Abs(Profile.GrowthDirection.x) > 0
                ? new Vector3(originalScale.x * ratio, originalScale.y, originalScale.z)
                : new Vector3(originalScale.x, originalScale.y * ratio, originalScale.z);
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
            public float SunbeamAngle;
            public float CycleElapsed;
            public float ShadowLifetimeRemaining;
            public bool ShadowActive;
            public bool SunflowerBloomed;
            public int SunflowerRotationSteps;
            public int VineLengthCells;
            public float CooledRemaining;
            public SunPhase SunsetPhase;
            public string AcceptedPerchOffering;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SunDewDropProjectile : MonoBehaviour
    {
        private bool coolOnImpact;
        private bool refillFully;
        private float waterMagnitude;
        private GameObject source;
        private bool spent;
        private float lifeSeconds = 6f;

        public void Configure(bool cool, bool fullRefill, float magnitude, GameObject owner)
        {
            coolOnImpact = cool;
            refillFully = fullRefill;
            waterMagnitude = Mathf.Max(0f, magnitude);
            source = owner;
            spent = false;
        }

        private void Update()
        {
            lifeSeconds -= Time.deltaTime;
            if (lifeSeconds <= 0f) Destroy(gameObject);
        }

        public bool ApplyTo(GameObject target)
        {
            if (spent || target == null || target == source) return false;
            spent = true;
            var behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (refillFully && behaviours[index] is ISunWateringCanReceiver wateringCan)
                    wateringCan.RefillWateringCanFully();
                if (coolOnImpact && behaviours[index] is ISunCoolingReceiver cooling)
                    cooling.ReceiveCooling(waterMagnitude);
                if (behaviours[index] is IMapElementEnvironmentalReceiver environment)
                    environment.ReceiveWater(Vector2.down * waterMagnitude);
            }
            Destroy(gameObject);
            return true;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision != null) ApplyTo(collision.gameObject);
        }
    }
}

#endif
