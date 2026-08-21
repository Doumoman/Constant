#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    public interface IPalaceUmbrellaState
    {
        bool IsUmbrellaOpen { get; }
        float WindForceMultiplier { get; }
        float WaterCurrentMultiplier { get; }
    }

    public interface IPalaceCloudSupportState
    {
        bool HasCloudSupport { get; }
    }

    public interface IPalaceWateringCanReceiver
    {
        void RefillFromWaterfall(float amount);
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class PalaceElementDriver : MonoBehaviour,
        IMapElementSignalReceiver,
        IMapElementInteractionReceiver,
        IMapElementPersistentParticipant
    {
        [SerializeField] private string variantState = string.Empty;
        [SerializeField] private float gateOpenProgress;
        [SerializeField] private float sinkProgress;
        [SerializeField] private float cycleElapsed;
        [SerializeField] private float bubbleElapsed;
        [SerializeField] private float waterLevelDeltaTotal;
        [SerializeField] private bool targetGateOpen;
        [SerializeField] private bool mirrorTransparent;
        [SerializeField] private bool mudBlocked;
        [SerializeField] private bool drainOpen;
        [SerializeField] private bool waterfallActive;
        [SerializeField] private bool currentPartiallyBlocked;
        [SerializeField] private int bubbleShotsFired;

        private readonly Dictionary<int, int> weights = new Dictionary<int, int>();
        private MapElementInstance element;
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Collider2D[] colliders = Array.Empty<Collider2D>();
        private Transform visualRoot;
        private Transform physicsRoot;
        private Transform triggerRoot;
        private Vector3 visualOrigin;
        private Vector3 physicsOrigin;
        private Vector3 triggerOrigin;
        private bool initialized;

        public event Action<float> WaterLevelChangeRequested;

        public PalaceElementKind Kind => Profile != null ? Profile.Kind : PalaceElementKind.None;
        public string VariantState => variantState;
        public float GateOpenProgress => gateOpenProgress;
        public float SinkProgress => sinkProgress;
        public bool IsClamOpen => Kind == PalaceElementKind.ClamBounce &&
                                  cycleElapsed < Mathf.Max(0.01f, Profile.CycleSeconds) * 0.5f;
        public bool MirrorTransparent => mirrorTransparent;
        public bool MudBlocked => mudBlocked;
        public bool DrainOpen => drainOpen;
        public bool WaterfallActive => waterfallActive;
        public bool CurrentPartiallyBlocked => currentPartiallyBlocked;
        public int BubbleShotsFired => bubbleShotsFired;
        public float WaterLevelDeltaTotal => waterLevelDeltaTotal;
        public string PersistenceId => element != null ? $"{element.PersistenceId}:palace" : string.Empty;
        public string InteractionPrompt => Kind switch
        {
            PalaceElementKind.SluiceGate => "[X] 수문 레버",
            PalaceElementKind.DrainGrate when mudBlocked => "[X] 진흙 제거 필요",
            PalaceElementKind.DrainGrate => "[X] 배수 손잡이",
            _ => string.Empty,
        };

        private PalaceElementRuntimeProfile Profile =>
            element != null && element.Definition != null ? element.Definition.PalaceProfile : null;

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
                case PalaceElementKind.SluiceGate:
                    if ((context.Tags & ToolTag.Hook) != 0)
                    {
                        ToggleGate();
                        return true;
                    }
                    break;
                case PalaceElementKind.BubbleCannon:
                    if ((context.Tags & ToolTag.WindGuard) != 0)
                    {
                        variantState = "UmbrellaGuarded";
                        RefreshPresentation();
                        return true;
                    }
                    break;
                case PalaceElementKind.CurrentVolume:
                    if ((context.Tags & ToolTag.HeavyImpact) != 0)
                    {
                        currentPartiallyBlocked = true;
                        variantState = "PartiallyBlocked";
                        RefreshPresentation();
                        return true;
                    }
                    break;
                case PalaceElementKind.WaterMirrorWall:
                    if ((context.Tags & ToolTag.Context) != 0)
                    {
                        SetMirrorTransparent(true, "YeouijuTransparent");
                        return true;
                    }
                    break;
                case PalaceElementKind.DrainGrate:
                    if ((context.Tags & ToolTag.Shovel) != 0)
                    {
                        mudBlocked = false;
                        variantState = "MudCleared";
                        RefreshPresentation();
                        return true;
                    }
                    if ((context.Tags & ToolTag.Hook) != 0 && !mudBlocked)
                    {
                        SetDrainOpen(!drainOpen);
                        return true;
                    }
                    break;
                case PalaceElementKind.DragonGateWaterfall:
                    if ((context.Tags & ToolTag.WindGuard) != 0)
                    {
                        variantState = "UmbrellaBoost";
                        RefreshPresentation();
                        return true;
                    }
                    if ((context.Tags & ToolTag.Water) != 0 && Profile.CanRefillWateringCan)
                    {
                        variantState = "WateringCanCharged";
                        RefreshPresentation();
                        return true;
                    }
                    break;
            }
            return false;
        }

        public bool TryInteract(GameObject instigator)
        {
            Initialize();
            if (Kind == PalaceElementKind.SluiceGate)
            {
                ToggleGate();
                return true;
            }
            if (Kind == PalaceElementKind.DrainGrate && !mudBlocked)
            {
                SetDrainOpen(!drainOpen);
                return true;
            }
            return false;
        }

        public void ReceiveSignal(string channel, bool active)
        {
            Initialize();
            switch (Kind)
            {
                case PalaceElementKind.SluiceGate:
                    targetGateOpen = active;
                    variantState = active ? "Opening" : "Closing";
                    element.TrySetState(MapElementState.Active);
                    break;
                case PalaceElementKind.WaterMirrorWall when Profile.TransparentOnSignal:
                    SetMirrorTransparent(active, active ? "SignalTransparent" : "Reflective");
                    break;
                case PalaceElementKind.DrainGrate when !mudBlocked:
                    SetDrainOpen(active);
                    break;
                case PalaceElementKind.DragonGateWaterfall:
                    waterfallActive = active;
                    variantState = active ? "WaterfallOn" : "WaterfallOff";
                    element.TrySetState(active ? MapElementState.Active : MapElementState.Idle);
                    RefreshPresentation();
                    break;
            }
        }

        public GameObject FireBubble()
        {
            Initialize();
            if (Kind != PalaceElementKind.BubbleCannon || Profile == null)
            {
                return null;
            }
            var bubble = new GameObject($"{name}_Bubble_{++bubbleShotsFired}");
            bubble.transform.position = transform.position;
            var collider = bubble.AddComponent<CircleCollider2D>();
            collider.radius = 0.28f;
            collider.isTrigger = true;
            var projectile = bubble.AddComponent<PalaceBubbleProjectile>();
            projectile.Configure(Profile.Direction, Profile.ProjectileSpeedCellsPerSecond,
                Profile.Knockback, Profile.UmbrellaPushMultiplier, gameObject);
            return bubble;
        }

        public void NotifyTriggerEnter(Collider2D other)
        {
            Initialize();
            if (other == null || Profile == null)
            {
                return;
            }
            if (Kind == PalaceElementKind.TurtlePlatform)
            {
                SetWeight(other.gameObject.GetInstanceID(), ResolveWeight(other.gameObject), true);
            }
            else if (Kind == PalaceElementKind.ClamBounce && IsClamOpen)
            {
                Launch(other);
            }
            else if (Kind == PalaceElementKind.CurrentVolume && ResolveWeight(other.gameObject) >= 2)
            {
                currentPartiallyBlocked = true;
                variantState = "PartiallyBlocked";
                RefreshPresentation();
            }
        }

        public void NotifyTriggerStay(Collider2D other)
        {
            if (other == null || Profile == null)
            {
                return;
            }
            if (Kind == PalaceElementKind.CurrentVolume)
            {
                ApplyCurrent(other);
            }
            else if (Kind == PalaceElementKind.DragonGateWaterfall && waterfallActive)
            {
                ApplyWaterfall(other);
            }
        }

        public void NotifyTriggerExit(Collider2D other)
        {
            if (other == null)
            {
                return;
            }
            if (Kind == PalaceElementKind.TurtlePlatform)
            {
                SetWeight(other.gameObject.GetInstanceID(), 0, false);
            }
            else if (Kind == PalaceElementKind.CurrentVolume)
            {
                currentPartiallyBlocked = false;
                variantState = string.Empty;
                RefreshPresentation();
            }
        }

        public void NotifyCollisionEnter(Collision2D collision)
        {
            Initialize();
            if (collision == null || Kind != PalaceElementKind.WaterMirrorWall || mirrorTransparent)
            {
                return;
            }
            var targetBody = collision.rigidbody;
            if (targetBody != null)
            {
                var normal = Profile.NormalDirection == Vector2Int.zero
                    ? Vector2.left
                    : ((Vector2)Profile.NormalDirection).normalized;
                targetBody.linearVelocity = Vector2.Reflect(targetBody.linearVelocity, normal);
                variantState = "Reflected";
                RefreshPresentation();
            }
        }

        public string CaptureMapElementState()
        {
            return JsonUtility.ToJson(new PersistentState
            {
                VariantState = variantState,
                GateOpenProgress = gateOpenProgress,
                SinkProgress = sinkProgress,
                WaterLevelDeltaTotal = waterLevelDeltaTotal,
                TargetGateOpen = targetGateOpen,
                MirrorTransparent = mirrorTransparent,
                MudBlocked = mudBlocked,
                DrainOpen = drainOpen,
                WaterfallActive = waterfallActive,
            });
        }

        public void RestoreMapElementState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            Initialize();
            var state = JsonUtility.FromJson<PersistentState>(payload);
            if (state == null) return;
            variantState = state.VariantState ?? string.Empty;
            gateOpenProgress = Mathf.Clamp01(state.GateOpenProgress);
            sinkProgress = Mathf.Clamp01(state.SinkProgress);
            waterLevelDeltaTotal = state.WaterLevelDeltaTotal;
            targetGateOpen = state.TargetGateOpen;
            mirrorTransparent = state.MirrorTransparent;
            mudBlocked = state.MudBlocked;
            drainOpen = state.DrainOpen;
            waterfallActive = state.WaterfallActive;
            RefreshPresentation();
        }

        private void Initialize()
        {
            if (initialized) return;
            element = GetComponent<MapElementInstance>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            colliders = GetComponentsInChildren<Collider2D>(true);
            visualRoot = transform.Find("VisualRoot");
            physicsRoot = transform.Find("PhysicsRoot");
            triggerRoot = transform.Find("TriggerRoot");
            visualOrigin = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
            physicsOrigin = physicsRoot != null ? physicsRoot.localPosition : Vector3.zero;
            triggerOrigin = triggerRoot != null ? triggerRoot.localPosition : Vector3.zero;
            if (Profile != null)
            {
                mudBlocked = Profile.Kind == PalaceElementKind.DrainGrate && Profile.StartsMudBlocked;
                waterfallActive = Profile.Kind == PalaceElementKind.DragonGateWaterfall && Profile.StartsActive;
            }
            initialized = true;
            RefreshPresentation();
        }

        private void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Profile == null) return;
            if (Kind == PalaceElementKind.SluiceGate)
            {
                var target = targetGateOpen ? 1f : 0f;
                gateOpenProgress = Mathf.MoveTowards(gateOpenProgress, target,
                    Profile.MoveSpeedCellsPerSecond / Mathf.Max(1f, Profile.HeightCells) * deltaSeconds);
                if (Mathf.Abs(gateOpenProgress - target) <= 0.001f)
                {
                    variantState = targetGateOpen ? "Open" : "Closed";
                    element.TrySetState(MapElementState.Idle);
                }
                RefreshPresentation();
            }
            else if (Kind == PalaceElementKind.BubbleCannon)
            {
                bubbleElapsed += deltaSeconds;
                if (bubbleElapsed >= Profile.IntervalSeconds)
                {
                    bubbleElapsed = Mathf.Repeat(bubbleElapsed, Profile.IntervalSeconds);
                    FireBubble();
                }
            }
            else if (Kind == PalaceElementKind.TurtlePlatform)
            {
                var target = TotalWeight() >= Mathf.Max(1, Profile.WeightThreshold) ? 1f : 0f;
                sinkProgress = Mathf.MoveTowards(sinkProgress, target, deltaSeconds * 2f);
                variantState = target > 0f ? "Submerged" : string.Empty;
                RefreshPresentation();
            }
            else if (Kind == PalaceElementKind.ClamBounce)
            {
                cycleElapsed = Mathf.Repeat(cycleElapsed + deltaSeconds, Profile.CycleSeconds);
                variantState = IsClamOpen ? "Open" : "Closed";
                RefreshPresentation();
            }
            else if (Kind == PalaceElementKind.DrainGrate && drainOpen)
            {
                var delta = -Profile.DrainRatePerSecond * deltaSeconds;
                waterLevelDeltaTotal += delta;
                WaterLevelChangeRequested?.Invoke(delta);
            }
        }

        private void ToggleGate()
        {
            targetGateOpen = !targetGateOpen;
            variantState = targetGateOpen ? "Opening" : "Closing";
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
        }

        private void SetMirrorTransparent(bool value, string state)
        {
            mirrorTransparent = value;
            variantState = state;
            element.TrySetState(value ? MapElementState.Active : MapElementState.Idle);
            RefreshPresentation();
        }

        private void SetDrainOpen(bool value)
        {
            drainOpen = value;
            variantState = value ? "Draining" : "Closed";
            element.TrySetState(value ? MapElementState.Active : MapElementState.Idle);
            RefreshPresentation();
        }

        private void SetWeight(int sourceId, int weight, bool present)
        {
            if (present) weights[sourceId] = Mathf.Clamp(weight, 1, 2);
            else weights.Remove(sourceId);
        }

        private void ApplyCurrent(Collider2D other)
        {
            var direction = Profile.Direction == Vector2Int.zero ? Vector2.right : ((Vector2)Profile.Direction).normalized;
            var multiplier = currentPartiallyBlocked ? Profile.HeavyBlockMultiplier : 1f;
            var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPalaceUmbrellaState umbrella && umbrella.IsUmbrellaOpen)
                {
                    multiplier *= umbrella.WaterCurrentMultiplier;
                    break;
                }
            }
            var halfExtent = Mathf.Max(0.5f,
                Mathf.Max(Profile.VolumeSizeCells.x, Profile.VolumeSizeCells.y) * 0.5f);
            var normalizedDistance = Mathf.Clamp01(
                Vector2.Distance(other.bounds.center, transform.position) / halfExtent);
            var falloffMultiplier = Mathf.Lerp(1f, Mathf.Clamp01(1f - Profile.Falloff), normalizedDistance);
            var velocity = direction * Profile.ForceCellsPerSecond * multiplier * falloffMultiplier * Time.fixedDeltaTime;
            ApplyWaterVelocity(other, velocity);
        }

        private int TotalWeight()
        {
            var total = 0;
            foreach (var weight in weights.Values) total += weight;
            return total;
        }

        private void ApplyWaterfall(Collider2D other)
        {
            var multiplier = 1f;
            var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPalaceUmbrellaState umbrella && umbrella.IsUmbrellaOpen)
                    multiplier = Mathf.Max(multiplier, umbrella.WindForceMultiplier);
                if (behaviours[index] is IPalaceCloudSupportState cloud && cloud.HasCloudSupport)
                    multiplier = Mathf.Max(multiplier, Profile.CloudSupportMultiplier);
                if (Profile.CanRefillWateringCan && behaviours[index] is IPalaceWateringCanReceiver wateringCan)
                    wateringCan.RefillFromWaterfall(Time.fixedDeltaTime);
            }
            ApplyWaterVelocity(other, Vector2.up * Profile.ForceCellsPerSecond * multiplier * Time.fixedDeltaTime);
        }

        private static void ApplyWaterVelocity(Collider2D other, Vector2 velocity)
        {
            var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IMapElementEnvironmentalReceiver receiver)
                {
                    receiver.ReceiveWater(velocity);
                    return;
                }
            }
            if (other.attachedRigidbody != null) other.attachedRigidbody.linearVelocity += velocity;
        }

        private void Launch(Collider2D other)
        {
            if (other.attachedRigidbody == null) return;
            var speed = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * Profile.LaunchHeightCells);
            other.attachedRigidbody.linearVelocity = new Vector2(other.attachedRigidbody.linearVelocity.x, speed);
        }

        private void RefreshPresentation()
        {
            if (!initialized || Profile == null) return;
            var tint = Color.white;
            if (variantState == "MudCleared" || variantState == "YeouijuTransparent" || variantState == "SignalTransparent")
                tint = new Color(0.48f, 1f, 0.82f, mirrorTransparent ? 0.45f : 1f);
            else if (variantState == "PartiallyBlocked") tint = new Color(0.58f, 0.62f, 0.68f);
            else if (variantState == "UmbrellaBoost" || variantState == "WateringCanCharged") tint = new Color(0.52f, 0.86f, 1f);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null) renderers[index].color = tint;
            }
            if (visualRoot != null)
            {
                if (Kind == PalaceElementKind.SluiceGate)
                    visualRoot.localPosition = visualOrigin + Vector3.up * (gateOpenProgress * Profile.HeightCells);
                else if (Kind == PalaceElementKind.TurtlePlatform)
                    visualRoot.localPosition = visualOrigin + Vector3.down * (sinkProgress * Profile.SinkDepthCells);
            }
            if (physicsRoot != null)
            {
                if (Kind == PalaceElementKind.SluiceGate)
                    physicsRoot.localPosition = physicsOrigin + Vector3.up * (gateOpenProgress * Profile.HeightCells);
                else if (Kind == PalaceElementKind.TurtlePlatform)
                    physicsRoot.localPosition = physicsOrigin + Vector3.down * (sinkProgress * Profile.SinkDepthCells);
            }
            if (triggerRoot != null && Kind == PalaceElementKind.TurtlePlatform)
                triggerRoot.localPosition = triggerOrigin + Vector3.down * (sinkProgress * Profile.SinkDepthCells);
            if (Kind == PalaceElementKind.WaterMirrorWall)
            {
                for (var index = 0; index < colliders.Length; index++)
                {
                    if (colliders[index] != null) colliders[index].enabled = !mirrorTransparent;
                }
            }
        }

        private static int ResolveWeight(GameObject target)
        {
            var sources = target.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < sources.Length; index++)
            {
                if (sources[index] is IMapElementWeightSource source)
                    return Mathf.Clamp(source.PressureWeight, 1, 2);
            }
            var targetBody = target.GetComponentInParent<Rigidbody2D>();
            return targetBody != null && targetBody.mass >= 2f ? 2 : 1;
        }

        [Serializable]
        private sealed class PersistentState
        {
            public string VariantState;
            public float GateOpenProgress;
            public float SinkProgress;
            public float WaterLevelDeltaTotal;
            public bool TargetGateOpen;
            public bool MirrorTransparent;
            public bool MudBlocked;
            public bool DrainOpen;
            public bool WaterfallActive;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PalaceBubbleProjectile : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private Vector2 knockback;
        private float umbrellaMultiplier;
        private GameObject source;
        private float lifeSeconds = 4f;

        public void Configure(Vector2Int moveDirection, float moveSpeed, Vector2 push,
            float guardedMultiplier, GameObject owner)
        {
            direction = moveDirection == Vector2Int.zero ? Vector2.right : ((Vector2)moveDirection).normalized;
            speed = Mathf.Max(0f, moveSpeed);
            knockback = push;
            umbrellaMultiplier = Mathf.Clamp01(guardedMultiplier);
            source = owner;
        }

        private void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            lifeSeconds -= Time.deltaTime;
            if (lifeSeconds <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || other.gameObject == source) return;
            var multiplier = 1f;
            var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPalaceUmbrellaState umbrella && umbrella.IsUmbrellaOpen)
                    multiplier = umbrella.WaterCurrentMultiplier;
                if (behaviours[index] is IMapElementEnvironmentalReceiver receiver)
                {
                    receiver.ReceiveWater(direction * knockback.magnitude * multiplier);
                    Destroy(gameObject);
                    return;
                }
            }
            if (other.attachedRigidbody != null)
                other.attachedRigidbody.linearVelocity += direction * knockback.magnitude * multiplier;
            Destroy(gameObject);
        }
    }
}

#endif
