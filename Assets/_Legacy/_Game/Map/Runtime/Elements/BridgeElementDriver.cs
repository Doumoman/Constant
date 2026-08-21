#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    public interface IBridgeUmbrellaState
    {
        bool IsUmbrellaOpen { get; }
        float WindForceMultiplier { get; }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class BridgeElementDriver : MonoBehaviour,
        IMapElementSignalReceiver,
        IMapElementInteractionReceiver,
        IMapElementPersistentParticipant
    {
        [SerializeField] private string variantState = string.Empty;
        [SerializeField] private float sagCells;
        [SerializeField] private float pathProgress;
        [SerializeField] private float pathWaitRemaining;
        [SerializeField] private float pulleyOffset;
        [SerializeField] private int pathIndex;
        [SerializeField] private int pathDirection = 1;
        [SerializeField] private int landingHitCount;
        [SerializeField] private int repairedPieces;
        [SerializeField] private bool moonCakeDelivered;
        [SerializeField] private bool occupied;
        [SerializeField] private bool heavyOccupied;
        [SerializeField] private float dwellSeconds;

        private readonly Dictionary<int, int> weights = new Dictionary<int, int>();
        private MapElementInstance element;
        private Rigidbody2D body;
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Transform visualRoot;
        private Vector3 originLocalPosition;
        private Vector2Int windDirection = Vector2Int.right;
        private bool initialized;

        public BridgeElementKind Kind => Profile != null ? Profile.Kind : BridgeElementKind.None;
        public string VariantState => variantState;
        public float SagCells => sagCells;
        public float PulleyOffset => pulleyOffset;
        public int LandingHitCount => landingHitCount;
        public int RepairedPieces => repairedPieces;
        public bool MoonCakeDelivered => moonCakeDelivered;
        public Vector2Int WindDirection => windDirection;
        public float CurrentUpdraftMultiplier => variantState == "UmbrellaBoost"
            ? Profile.UmbrellaLiftMultiplier
            : 1f;
        public string PersistenceId => element != null ? $"{element.PersistenceId}:bridge" : string.Empty;
        public string InteractionPrompt => Kind switch
        {
            BridgeElementKind.MagpiePlatform => "[X] 까치 발판 호출",
            BridgeElementKind.Nest when repairedPieces < Mathf.Max(1, Profile.RequiredPieces) =>
                $"[X] 실 조각 수리 {repairedPieces}/{Mathf.Max(1, Profile.RequiredPieces)}",
            BridgeElementKind.Nest when !moonCakeDelivered => "[X] 달떡 전달",
            _ => string.Empty,
        };

        private BridgeElementRuntimeProfile Profile =>
            element != null && element.Definition != null ? element.Definition.BridgeProfile : null;

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
                case BridgeElementKind.ThreadBridge:
                    if ((context.Tags & (ToolTag.Pickaxe | ToolTag.Bomb)) != 0)
                    {
                        return BreakElement("Cut");
                    }
                    break;

                case BridgeElementKind.KnotPulley:
                    if ((context.Tags & ToolTag.Hook) != 0)
                    {
                        TogglePulley();
                        return true;
                    }
                    if ((context.Tags & ToolTag.HeavyImpact) != 0)
                    {
                        pulleyOffset = Mathf.Clamp(
                            pulleyOffset - Mathf.Sign(context.Direction.x == 0 ? 1 : context.Direction.x) *
                            Profile.TravelCells / Mathf.Max(0.01f, Profile.WeightRatio),
                            -Profile.TravelCells,
                            Profile.TravelCells);
                        variantState = "HeavyBalanced";
                        RefreshPresentation();
                        return true;
                    }
                    break;

                case BridgeElementKind.WindBanner:
                    if ((context.Tags & ToolTag.Water) != 0)
                    {
                        variantState = "WetWeak";
                        RefreshPresentation();
                        return true;
                    }
                    if ((context.Tags & ToolTag.WindGuard) != 0)
                    {
                        variantState = "UmbrellaAssist";
                        RefreshPresentation();
                        return true;
                    }
                    break;

                case BridgeElementKind.MagpiePlatform:
                    if ((context.Tags & ToolTag.HeavyImpact) != 0)
                    {
                        heavyOccupied = true;
                        variantState = "HeavyDescending";
                        RefreshPresentation();
                        return true;
                    }
                    break;

                case BridgeElementKind.FeatherUpdraft:
                    if ((context.Tags & ToolTag.WindGuard) != 0)
                    {
                        variantState = "UmbrellaBoost";
                        RefreshPresentation();
                        return true;
                    }
                    break;

                case BridgeElementKind.BreakingStarPanel:
                    if ((context.Tags & ToolTag.HeavyImpact) != 0)
                    {
                        return BreakElement("Collapsed");
                    }
                    break;

                case BridgeElementKind.Nest:
                    if ((context.Tags & ToolTag.Bomb) != 0 && Profile.CriticalObject)
                    {
                        return false;
                    }
                    if ((context.Tags & ToolTag.Context) != 0 &&
                        repairedPieces >= Mathf.Max(1, Profile.RequiredPieces) &&
                        !moonCakeDelivered)
                    {
                        moonCakeDelivered = true;
                        variantState = "MagpieSupportReady";
                        element.TrySetState(MapElementState.Active);
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
            if (Profile == null || element.CurrentState == MapElementState.Broken)
            {
                return false;
            }

            if (Kind == BridgeElementKind.MagpiePlatform)
            {
                CallNextStop();
                return true;
            }

            if (Kind != BridgeElementKind.Nest || moonCakeDelivered)
            {
                return false;
            }

            if (repairedPieces < Mathf.Max(1, Profile.RequiredPieces))
            {
                repairedPieces++;
                variantState = repairedPieces >= Profile.RequiredPieces ? "NestRepaired" : $"ThreadPiece{repairedPieces}";
                RefreshPresentation();
                return true;
            }

            moonCakeDelivered = true;
            variantState = "MagpieSupportReady";
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
            return true;
        }

        public void ReceiveSignal(string channel, bool active)
        {
            Initialize();
            if (!active || Profile == null)
            {
                return;
            }

            if (Kind == BridgeElementKind.KnotPulley)
            {
                TogglePulley();
            }
            else if (Kind == BridgeElementKind.WindBanner && Profile.FlipOnSignal)
            {
                windDirection = -windDirection;
                variantState = "Flipped";
                RefreshPresentation();
            }
            else if (Kind == BridgeElementKind.MagpiePlatform)
            {
                CallNextStop();
            }
        }

        public void NotifyTriggerEnter(Collider2D other)
        {
            Initialize();
            if (other == null || Profile == null)
            {
                return;
            }

            var weight = ResolveWeight(other.gameObject);
            if (Kind == BridgeElementKind.ThreadBridge)
            {
                SetWeight(other.gameObject.GetInstanceID(), weight, true);
            }
            else if (Kind == BridgeElementKind.KnotPulley && weight >= 2)
            {
                heavyOccupied = true;
                pulleyOffset = Mathf.Clamp(-Profile.TravelCells / Mathf.Max(0.01f, Profile.WeightRatio),
                    -Profile.TravelCells, Profile.TravelCells);
                variantState = "HeavyBalanced";
                RefreshPresentation();
            }
            else if (Kind == BridgeElementKind.MagpiePlatform && weight >= 2)
            {
                heavyOccupied = true;
                variantState = "HeavyDescending";
                RefreshPresentation();
            }
            else if (Kind == BridgeElementKind.BreakingStarPanel)
            {
                occupied = true;
                landingHitCount++;
                variantState = landingHitCount >= Profile.HitCount ? "Collapsed" : "Cracked";
                if (landingHitCount >= Mathf.Max(1, Profile.HitCount))
                {
                    BreakElement("Collapsed");
                }
                else
                {
                    RefreshPresentation();
                }
            }
        }

        public void NotifyTriggerStay(Collider2D other)
        {
            if (other == null || Profile == null)
            {
                return;
            }

            if (Kind == BridgeElementKind.FeatherUpdraft)
            {
                ApplyUpdraft(other);
            }
        }

        public void NotifyTriggerExit(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (Kind == BridgeElementKind.ThreadBridge)
            {
                SetWeight(other.gameObject.GetInstanceID(), 0, false);
            }
            else if (Kind == BridgeElementKind.KnotPulley || Kind == BridgeElementKind.MagpiePlatform)
            {
                heavyOccupied = false;
            }
            else if (Kind == BridgeElementKind.BreakingStarPanel)
            {
                occupied = false;
                dwellSeconds = 0f;
            }
        }

        public void NotifyCollisionEnter(Collision2D collision)
        {
            Initialize();
            if (collision == null || Profile == null)
            {
                return;
            }

            if (Kind == BridgeElementKind.ThreadBlade)
            {
                ApplyDamage(collision.gameObject);
                ApplyCut(collision.gameObject, collision.relativeVelocity);
            }
            else if (Kind == BridgeElementKind.BreakingStarPanel &&
                     collision.relativeVelocity.magnitude >= 3f)
            {
                BreakElement("Collapsed");
            }
        }

        public string CaptureMapElementState()
        {
            return JsonUtility.ToJson(new PersistentState
            {
                VariantState = variantState,
                PathProgress = pathProgress,
                PulleyOffset = pulleyOffset,
                PathIndex = pathIndex,
                PathDirection = pathDirection,
                LandingHitCount = landingHitCount,
                RepairedPieces = repairedPieces,
                MoonCakeDelivered = moonCakeDelivered,
                WindDirection = windDirection,
            });
        }

        public void RestoreMapElementState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            Initialize();
            var state = JsonUtility.FromJson<PersistentState>(payload);
            if (state == null)
            {
                return;
            }

            variantState = state.VariantState ?? string.Empty;
            pathProgress = Mathf.Max(0f, state.PathProgress);
            pulleyOffset = state.PulleyOffset;
            pathIndex = Mathf.Max(0, state.PathIndex);
            pathDirection = state.PathDirection == 0 ? 1 : state.PathDirection;
            landingHitCount = Mathf.Max(0, state.LandingHitCount);
            repairedPieces = Mathf.Max(0, state.RepairedPieces);
            moonCakeDelivered = state.MoonCakeDelivered;
            windDirection = state.WindDirection == Vector2Int.zero ? Profile.Direction : state.WindDirection;
            RefreshPresentation();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            element = GetComponent<MapElementInstance>();
            body = GetComponent<Rigidbody2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            visualRoot = transform.Find("VisualRoot");
            originLocalPosition = transform.localPosition;
            windDirection = Profile != null && Profile.Direction != Vector2Int.zero
                ? Profile.Direction
                : Vector2Int.right;
            initialized = true;
            RefreshPresentation();
        }

        private void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Profile == null)
            {
                return;
            }

            if (Kind == BridgeElementKind.ThreadBlade || Kind == BridgeElementKind.MagpiePlatform)
            {
                TickPath(deltaSeconds);
            }

            if (Kind == BridgeElementKind.KnotPulley)
            {
                var target = originLocalPosition + Vector3.up * pulleyOffset;
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    target,
                    Mathf.Max(1f, Profile.TravelCells) * deltaSeconds);
            }

            if (Kind == BridgeElementKind.BreakingStarPanel && occupied &&
                element.CurrentState != MapElementState.Broken)
            {
                dwellSeconds += deltaSeconds;
                if (dwellSeconds >= Mathf.Max(0.01f, Profile.DwellBreakSeconds))
                {
                    BreakElement("Collapsed");
                }
            }
        }

        private void TickPath(float deltaSeconds)
        {
            var nodes = element.Definition.BehaviorProfile?.Path?.Nodes;
            if (nodes == null || nodes.Count < 2)
            {
                return;
            }

            if (pathWaitRemaining > 0f)
            {
                pathWaitRemaining -= deltaSeconds;
                return;
            }

            var speed = Kind == BridgeElementKind.ThreadBlade
                ? Profile.PathSpeedCellsPerSecond
                : element.Definition.BehaviorProfile.Path.SpeedCellsPerSecond;
            if (Kind == BridgeElementKind.MagpiePlatform && heavyOccupied)
            {
                speed *= Profile.HeavyDescentMultiplier;
            }

            var nextIndex = Mathf.Clamp(pathIndex + pathDirection, 0, nodes.Count - 1);
            var target = originLocalPosition + (Vector3)nodes[nextIndex];
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * deltaSeconds);
            if (Vector3.Distance(transform.localPosition, target) > 0.001f)
            {
                return;
            }

            pathIndex = nextIndex;
            pathWaitRemaining = Kind == BridgeElementKind.MagpiePlatform ? Profile.WaitTimeSeconds : 0f;
            if (pathIndex == 0 || pathIndex == nodes.Count - 1)
            {
                pathDirection *= -1;
            }
        }

        private void SetWeight(int sourceId, int weight, bool present)
        {
            if (present) weights[sourceId] = Mathf.Clamp(weight, 1, 2);
            else weights.Remove(sourceId);

            var total = 0;
            foreach (var pair in weights) total += pair.Value;
            sagCells = Mathf.Clamp01(total / (float)Mathf.Max(1, Profile.MaxWeight)) * Profile.SagCells;
            variantState = total > Profile.MaxWeight ? "Overloaded" : total > 0 ? "Sagging" : string.Empty;
            if (total > Profile.MaxWeight)
            {
                BreakElement("OverloadedBroken");
            }
            else
            {
                RefreshPresentation();
            }
        }

        private void TogglePulley()
        {
            pulleyOffset = pulleyOffset >= 0f ? -Profile.TravelCells : Profile.TravelCells;
            variantState = "HookTriggered";
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
        }

        private void CallNextStop()
        {
            pathWaitRemaining = 0f;
            element.TrySetState(MapElementState.Active);
            variantState = "Called";
            RefreshPresentation();
        }

        private bool BreakElement(string state)
        {
            variantState = state;
            var changed = element.TrySetState(MapElementState.Broken);
            RefreshPresentation();
            return changed;
        }

        private void ApplyUpdraft(Collider2D other)
        {
            var multiplier = 1f;
            var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IBridgeUmbrellaState umbrella && umbrella.IsUmbrellaOpen)
                {
                    multiplier = umbrella.WindForceMultiplier;
                    variantState = "UmbrellaBoost";
                    break;
                }
            }

            var velocity = Vector2.up * Profile.ForceCellsPerSecond * multiplier * Time.fixedDeltaTime;
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IMapElementEnvironmentalReceiver receiver)
                {
                    receiver.ReceiveWind(velocity);
                    return;
                }
            }

            if (other.attachedRigidbody != null)
            {
                other.attachedRigidbody.linearVelocity += velocity;
            }
        }

        private void ApplyDamage(GameObject target)
        {
            var behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IMapElementDamageReceiver receiver)
                {
                    receiver.ReceiveMapElementDamage(new MapElementDamageEvent(
                        Mathf.Clamp(Profile.Damage, 0, 1), Profile.Knockback, gameObject, Time.frameCount));
                    return;
                }
            }
        }

        private void ApplyCut(GameObject target, Vector2 velocity)
        {
            var behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IToolReactionReceiver receiver)
                {
                    receiver.TryReact(new ToolReactionContext
                    {
                        ActionId = unchecked((GetInstanceID() * 397) ^ Time.frameCount),
                        Tags = ToolTag.Cut,
                        Direction = Cardinal(velocity),
                        Magnitude = Profile.PathSpeedCellsPerSecond,
                        Source = gameObject,
                        Instigator = gameObject,
                    });
                    return;
                }
            }
        }

        private void RefreshPresentation()
        {
            if (!initialized || Profile == null)
            {
                return;
            }

            var tint = Color.white;
            if (variantState == "WetWeak") tint = new Color(0.52f, 0.74f, 1f);
            else if (variantState == "Overloaded" || variantState == "Cracked") tint = new Color(1f, 0.58f, 0.32f);
            else if (variantState == "UmbrellaBoost" || variantState == "MagpieSupportReady") tint = new Color(0.52f, 1f, 0.64f);
            else if (variantState == "HeavyDescending" || variantState == "HeavyBalanced") tint = new Color(0.62f, 0.62f, 0.70f);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null) renderers[index].color = tint;
            }

            if (Kind == BridgeElementKind.ThreadBridge && visualRoot != null)
            {
                visualRoot.localPosition = Vector3.down * sagCells;
            }
            if (Kind == BridgeElementKind.WindBanner && visualRoot != null)
            {
                visualRoot.localScale = new Vector3(windDirection.x < 0 ? -1f : 1f, 1f, 1f);
            }
        }

        private static int ResolveWeight(GameObject target)
        {
            var sources = target.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < sources.Length; index++)
            {
                if (sources[index] is IMapElementWeightSource source)
                {
                    return Mathf.Clamp(source.PressureWeight, 1, 2);
                }
            }
            var targetBody = target.GetComponentInParent<Rigidbody2D>();
            return targetBody != null && targetBody.mass >= 2f ? 2 : 1;
        }

        private static Vector2Int Cardinal(Vector2 value)
        {
            if (Mathf.Abs(value.x) >= Mathf.Abs(value.y))
            {
                return value.x < 0f ? Vector2Int.left : Vector2Int.right;
            }
            return value.y < 0f ? Vector2Int.down : Vector2Int.up;
        }

        [Serializable]
        private sealed class PersistentState
        {
            public string VariantState;
            public float PathProgress;
            public float PulleyOffset;
            public int PathIndex;
            public int PathDirection;
            public int LandingHitCount;
            public int RepairedPieces;
            public bool MoonCakeDelivered;
            public Vector2Int WindDirection;
        }
    }

}

#endif
