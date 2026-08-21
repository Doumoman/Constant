#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class CommonElementDriver : MonoBehaviour,
        IMapElementSignalReceiver,
        IMapElementInteractionReceiver,
        IMapElementWeightSource
    {
        private readonly Dictionary<int, int> pressureWeights = new Dictionary<int, int>();
        private readonly Dictionary<int, float> damageTimes = new Dictionary<int, float>();
        private readonly Dictionary<int, int> damageActivationIds = new Dictionary<int, int>();
        private readonly Dictionary<int, float> bounceTimes = new Dictionary<int, float>();
        private readonly HashSet<int> coverSources = new HashSet<int>();

        [SerializeField] private string variantState = string.Empty;
        [SerializeField] private Vector2Int facingDirection = Vector2Int.right;
        [SerializeField] private bool covered;
        [SerializeField] private bool signalActive;
        [SerializeField] private bool fragileOccupied;
        [SerializeField] private float fragileDwell;
        [SerializeField, Range(0f, 1f)] private float doorOpenProgress;
        [SerializeField] private ToolTag lastResolvedImpactTags;

        private MapElementInstance element;
        private ToolReactionReceiver toolReceiver;
        private Rigidbody2D body;
        private Collider2D[] colliders = Array.Empty<Collider2D>();
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Vector3 originLocalPosition;
        private int pathIndex;
        private int pathDirection = 1;
        private float pathWait;
        private float pulseRemaining;
        private float ventCycleElapsed;
        private float pendulumElapsed;
        private float guideRemaining;
        private float crusherHoldRemaining;
        private bool crusherReturning;
        private int activationId;
        private bool initialized;

        public event Action<string, bool> SignalChanged;
        public event Action<string, Vector3> ContainerContentsDropped;
        public event Action<Vector2Int, float> ExitGuideActivated;

        public CommonElementKind Kind => Profile != null ? Profile.Kind : CommonElementKind.None;
        public string VariantState => variantState;
        public Vector2Int FacingDirection => facingDirection;
        public bool IsCovered => covered;
        public bool SignalActive => signalActive;
        public float DoorOpenProgress => doorOpenProgress;
        public float GuideRemainingSeconds => guideRemaining;
        public ToolTag LastResolvedImpactTags => lastResolvedImpactTags;
        public int PressureWeight =>
            Kind == CommonElementKind.PendulumBall ||
            Kind == CommonElementKind.FallingStone ||
            Kind == CommonElementKind.RollingBoulder ? 2 : 1;
        public string InteractionPrompt => Kind switch
        {
            CommonElementKind.Lever => "[X] 레버 당기기",
            CommonElementKind.ExitGuideLantern => "[X] 출구 방향 보기",
            _ => string.Empty,
        };

        private CommonElementRuntimeProfile Profile =>
            element != null && element.Definition != null ? element.Definition.CommonProfile : null;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            if (element != null)
            {
                element.StateChanged -= HandleStateChanged;
                element.StateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (element != null)
            {
                element.StateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            if (!initialized || Profile == null || element.CurrentState == MapElementState.Dormant)
            {
                return;
            }

            if (pulseRemaining > 0f)
            {
                pulseRemaining -= Time.deltaTime;
                if (pulseRemaining <= 0f)
                {
                    PublishSignal(false);
                }
            }

            TickGuide(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!initialized || Profile == null || element.CurrentState == MapElementState.Dormant)
            {
                return;
            }

            TickMovingPlatform(Time.fixedDeltaTime);
            TickWeightDoor(Time.fixedDeltaTime);
            TickPendulum(Time.fixedDeltaTime);
            TickCrusher(Time.fixedDeltaTime);
            ClampRollingBoulderSpeed();
            TickLaserBeam();
        }

        public void Rebind()
        {
            initialized = false;
            Initialize();
        }

        public void TickForTests(float deltaSeconds)
        {
            Initialize();
            if (deltaSeconds <= 0f || Profile == null)
            {
                return;
            }

            element.StateMachine.Tick(deltaSeconds);

            if (fragileOccupied)
            {
                TickDwellTrigger(deltaSeconds);
            }

            if (pulseRemaining > 0f)
            {
                pulseRemaining -= deltaSeconds;
                if (pulseRemaining <= 0f)
                {
                    PublishSignal(false);
                }
            }

            TickMovingPlatform(deltaSeconds);
            TickWeightDoor(deltaSeconds);
            TickPendulum(deltaSeconds);
            TickCrusher(deltaSeconds);
            TickGuide(deltaSeconds);
        }

        public bool ApplyToolReaction(ToolReactionEntry entry, ToolReactionContext context)
        {
            Initialize();
            if (entry == null || element == null)
            {
                return false;
            }

            if (Kind == CommonElementKind.Lever && (context.Tags & ToolTag.Hook) != 0)
            {
                return TryInteract(context.Instigator);
            }

            if (string.Equals(entry.ResultState, "WetMud", StringComparison.OrdinalIgnoreCase))
            {
                variantState = "WetMud";
                RefreshPresentation();
                return true;
            }

            if (string.Equals(entry.ResultState, "CompressedPlatform", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(variantState, "WetMud", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                variantState = "CompressedPlatform";
                element.TrySetState(MapElementState.Active);
                RefreshPresentation();
                return true;
            }

            if (string.Equals(entry.ResultState, "Rotate", StringComparison.OrdinalIgnoreCase))
            {
                facingDirection = SanitizeDirection(context.Direction, -facingDirection);
                variantState = "Rotated";
                return true;
            }

            if (string.Equals(entry.ResultState, "TrajectoryChanged", StringComparison.OrdinalIgnoreCase))
            {
                facingDirection = SanitizeDirection(context.Direction, -facingDirection);
                variantState = "TrajectoryChanged";
                if (Kind == CommonElementKind.RollingBoulder)
                {
                    EnsureBody(RigidbodyType2D.Dynamic, Mathf.Max(0.01f, Profile.GravityScale));
                }

                return element.TrySetState(MapElementState.Active) ||
                       element.CurrentState == MapElementState.Active;
            }

            if (string.Equals(entry.ResultState, "WindAssist", StringComparison.OrdinalIgnoreCase))
            {
                variantState = "WindAssist";
                RefreshPresentation();
                return true;
            }

            switch (entry.Reaction)
            {
                case ElementReactionType.Break:
                    return element.TrySetState(MapElementState.Broken);
                case ElementReactionType.Disable:
                    return element.TrySetState(MapElementState.Disabled);
                case ElementReactionType.SetState:
                    if (Enum.TryParse(entry.ResultState, true, out MapElementState parsed))
                    {
                        return element.TrySetState(parsed);
                    }
                    return false;
                case ElementReactionType.Toggle:
                    return element.TrySetState(element.CurrentState == MapElementState.Active
                        ? MapElementState.Idle
                        : MapElementState.Active);
                case ElementReactionType.Move:
                case ElementReactionType.Push:
                case ElementReactionType.Pull:
                    facingDirection = SanitizeDirection(context.Direction, facingDirection);
                    return element.TrySetState(MapElementState.Active);
                default:
                    return false;
            }
        }

        public ToolReactionResult NotifyImpact(
            int actionId,
            float mass,
            float relativeSpeed,
            Vector2Int direction,
            GameObject source = null,
            bool forceHeavyImpact = false)
        {
            var score = Mathf.Max(0f, mass) * Mathf.Max(0f, relativeSpeed);
            var tag = forceHeavyImpact || (mass >= 2f && relativeSpeed >= 3f) || score >= 6f
                ? ToolTag.HeavyImpact
                : score >= 2f ? ToolTag.LightImpact : ToolTag.None;
            lastResolvedImpactTags = Kind == CommonElementKind.SoftSoil
                ? SoftSoilContract.ReduceImpactGrade(tag)
                : tag;
            if (Kind == CommonElementKind.SoftSoil && tag != ToolTag.None)
            {
                return new ToolReactionResult
                {
                    Accepted = true,
                    ChangedState = false,
                    ConsumeToolResource = false,
                    Feedback = FeedbackId.Hit,
                };
            }
            if (tag == ToolTag.None || toolReceiver == null)
            {
                return ToolReactionResult.Rejected(FeedbackId.None);
            }

            return toolReceiver.TryReact(new ToolReactionContext
            {
                ActionId = actionId,
                Tags = tag,
                Direction = SanitizeDirection(direction, facingDirection),
                Magnitude = score,
                Source = source,
                Instigator = source,
            });
        }

        public void BeginHazardCycle()
        {
            Initialize();
            if (element == null || element.CurrentState != MapElementState.Idle)
            {
                return;
            }

            if (Kind == CommonElementKind.TotemShooter || Kind == CommonElementKind.LaserEmitter)
            {
                element.TrySetState(MapElementState.Warning);
            }
        }

        public void TriggerFallingStone()
        {
            Initialize();
            if (Kind == CommonElementKind.FallingStone && element.CurrentState == MapElementState.Idle)
            {
                element.TrySetState(MapElementState.Warning);
            }
        }

        public void SetFragileOccupancy(bool occupied)
        {
            fragileOccupied = occupied;
            if (!occupied && element != null && element.CurrentState == MapElementState.Idle)
            {
                fragileDwell = 0f;
            }
        }

        public void SetCovered(bool value)
        {
            covered = value;
            RefreshPresentation();
        }

        public void SetFacingDirection(Vector2Int direction)
        {
            facingDirection = SanitizeDirection(direction, Vector2Int.right);
        }

        public void SetPressureWeight(int sourceId, int weight, bool present)
        {
            if (Kind != CommonElementKind.PressurePlate)
            {
                return;
            }

            if (present)
            {
                pressureWeights[sourceId] = Mathf.Clamp(weight, 1, 2);
            }
            else
            {
                pressureWeights.Remove(sourceId);
            }

            var total = 0;
            foreach (var pair in pressureWeights)
            {
                total += pair.Value;
            }

            var next = total >= Mathf.Max(1, Profile.WeightThreshold);
            if (Profile.SignalMode == CommonSignalMode.Toggle && next && !signalActive)
            {
                PublishSignal(true);
            }
            else if (Profile.SignalMode == CommonSignalMode.Pulse && next)
            {
                PublishSignal(true);
                pulseRemaining = Mathf.Max(0.01f, Profile.PulseSeconds);
            }
            else if (Profile.SignalMode == CommonSignalMode.Hold)
            {
                PublishSignal(next);
            }
        }

        public bool TryInteract(GameObject instigator)
        {
            Initialize();
            if ((Kind != CommonElementKind.Lever && Kind != CommonElementKind.ExitGuideLantern) ||
                element.CurrentState == MapElementState.Broken ||
                element.CurrentState == MapElementState.Disabled)
            {
                return false;
            }

            if (Kind == CommonElementKind.ExitGuideLantern)
            {
                guideRemaining = Mathf.Max(0.01f, Profile.GuideDurationSeconds);
                variantState = "Guiding";
                element.TrySetState(MapElementState.Active);
                ExitGuideActivated?.Invoke(facingDirection, guideRemaining);
                RefreshPresentation();
                return true;
            }

            switch (Profile.SignalMode)
            {
                case CommonSignalMode.Toggle:
                    PublishSignal(!signalActive);
                    break;
                case CommonSignalMode.OneShot:
                    if (signalActive)
                    {
                        return false;
                    }
                    PublishSignal(true);
                    break;
                case CommonSignalMode.Pulse:
                    PublishSignal(true);
                    pulseRemaining = Mathf.Max(0.01f, Profile.PulseSeconds);
                    break;
                default:
                    PublishSignal(true);
                    break;
            }

            return true;
        }

        public void ReceiveSignal(string channel, bool active)
        {
            Initialize();
            if ((Kind != CommonElementKind.WeightDoor &&
                 Kind != CommonElementKind.Crusher &&
                 Kind != CommonElementKind.PulleyLift) || Profile == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Profile.SignalChannel) &&
                !string.Equals(Profile.SignalChannel, channel, StringComparison.Ordinal))
            {
                return;
            }

            signalActive = active;
            if (Kind == CommonElementKind.Crusher && active &&
                element.CurrentState == MapElementState.Idle)
            {
                crusherReturning = false;
                element.TrySetState(MapElementState.Warning);
            }
        }

        public GameObject FireProjectile(Vector2Int direction)
        {
            Initialize();
            if (Kind != CommonElementKind.TotemShooter || Profile == null)
            {
                return null;
            }

            var projectile = new GameObject($"{name}_Projectile");
            projectile.transform.position = transform.position;
            var collider = projectile.AddComponent<CircleCollider2D>();
            collider.radius = 0.16f;
            collider.isTrigger = true;
            var runtimeProjectile = projectile.AddComponent<CommonElementProjectile>();
            runtimeProjectile.Configure(
                SanitizeDirection(direction, facingDirection),
                Profile.ProjectileSpeedCellsPerSecond,
                Mathf.Clamp(Profile.Damage, 0, 1),
                gameObject,
                ++activationId);
            return projectile;
        }

        public void NotifyTriggerEnter(Collider2D other)
        {
            Initialize();
            if (other == null || Profile == null)
            {
                return;
            }

            switch (Kind)
            {
                case CommonElementKind.Spike:
                    if (FindInterface<IMapElementWeightSource>(other.gameObject) != null)
                    {
                        coverSources.Add(other.gameObject.GetInstanceID());
                        SetCovered(coverSources.Count > 0);
                    }
                    else if (!covered)
                    {
                        TryDamage(other.gameObject);
                    }
                    break;
                case CommonElementKind.LaserEmitter:
                    if (element.CurrentState == MapElementState.Active) TryDamage(other.gameObject);
                    break;
                case CommonElementKind.PressurePlate:
                    SetPressureWeight(other.gameObject.GetInstanceID(), ResolveWeight(other.gameObject), true);
                    break;
                case CommonElementKind.FragileFloor:
                    SetFragileOccupancy(true);
                    break;
                case CommonElementKind.FallingStone:
                    SetFragileOccupancy(true);
                    break;
                case CommonElementKind.PendulumBall:
                case CommonElementKind.Crusher:
                case CommonElementKind.RollingBoulder:
                    TryDamage(other.gameObject);
                    break;
                case CommonElementKind.BouncePad:
                    ApplyBounce(other);
                    break;
            }
        }

        public void NotifyTriggerStay(Collider2D other)
        {
            Initialize();
            if (other == null || Profile == null)
            {
                return;
            }

            if (Kind == CommonElementKind.FragileFloor || Kind == CommonElementKind.FallingStone)
            {
                TickDwellTrigger(Time.fixedDeltaTime);
            }
            else if (Kind == CommonElementKind.WindVent)
            {
                ApplyEnvironment(other, true);
            }
            else if (Kind == CommonElementKind.WaterVent)
            {
                ApplyEnvironment(other, false);
            }
        }

        public void NotifyTriggerExit(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (Kind == CommonElementKind.PressurePlate)
            {
                SetPressureWeight(other.gameObject.GetInstanceID(), 0, false);
            }
            else if (Kind == CommonElementKind.FragileFloor || Kind == CommonElementKind.FallingStone)
            {
                SetFragileOccupancy(false);
            }
            else if (Kind == CommonElementKind.Spike)
            {
                coverSources.Remove(other.gameObject.GetInstanceID());
                SetCovered(coverSources.Count > 0);
            }
        }

        public void NotifyCollisionEnter(Collision2D collision)
        {
            if (collision == null ||
                (Kind != CommonElementKind.FallingStone &&
                 Kind != CommonElementKind.PendulumBall &&
                 Kind != CommonElementKind.Crusher &&
                 Kind != CommonElementKind.RollingBoulder) ||
                element.CurrentState != MapElementState.Active)
            {
                return;
            }

            TryDamage(collision.gameObject);
            if (Kind == CommonElementKind.FallingStone &&
                body != null && Mathf.Abs(body.linearVelocity.y) < 0.35f)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.linearVelocity = Vector2.zero;
                variantState = "HeavyBlock";
            }
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
            originLocalPosition = transform.localPosition;
            pathDirection = element != null && element.Definition?.BehaviorProfile?.Path?.StartForward == false ? -1 : 1;
            initialized = true;
            RefreshPresentation();
        }

        private void HandleStateChanged(MapElementState previous, MapElementState next)
        {
            if (toolReceiver != null)
            {
                toolReceiver.SetTransitionBusy(false);
            }

            if (next == MapElementState.Active)
            {
                activationId++;
                if (Kind == CommonElementKind.FragileFloor)
                {
                    element.TrySetState(MapElementState.Broken);
                    return;
                }
                if (Kind == CommonElementKind.FallingStone)
                {
                    EnsureBody(RigidbodyType2D.Dynamic, Mathf.Max(0.01f, Profile.GravityScale));
                }
                else if (Kind == CommonElementKind.RollingBoulder)
                {
                    EnsureBody(RigidbodyType2D.Dynamic, Mathf.Max(0.01f, Profile.GravityScale));
                }
                else if (Kind == CommonElementKind.TotemShooter)
                {
                    FireProjectile(facingDirection);
                }
            }

            if (next == MapElementState.Broken && Kind == CommonElementKind.BreakableContainer)
            {
                var contentsId = string.IsNullOrWhiteSpace(Profile.ContentsId)
                    ? "Empty"
                    : Profile.ContentsId;
                variantState = $"Dropped:{contentsId}";
                ContainerContentsDropped?.Invoke(contentsId, transform.position);
            }

            RefreshPresentation();
        }

        private void TickDwellTrigger(float deltaSeconds)
        {
            if ((Kind != CommonElementKind.FragileFloor && Kind != CommonElementKind.FallingStone) ||
                !fragileOccupied ||
                element.CurrentState != MapElementState.Idle)
            {
                return;
            }

            fragileDwell += Mathf.Max(0f, deltaSeconds);
            if (fragileDwell >= Mathf.Max(0f, Profile.TriggerDwellSeconds))
            {
                fragileDwell = 0f;
                element.TrySetState(MapElementState.Warning);
            }
        }

        private void TickMovingPlatform(float deltaSeconds)
        {
            if ((Kind != CommonElementKind.MovingPlatform && Kind != CommonElementKind.PulleyLift) ||
                element.CurrentState == MapElementState.Broken ||
                element.CurrentState == MapElementState.Disabled)
            {
                return;
            }

            var path = element.Definition?.BehaviorProfile?.Path;
            if (path?.Nodes == null || path.Nodes.Count < 2 || deltaSeconds <= 0f)
            {
                return;
            }

            if (pathWait > 0f)
            {
                pathWait -= deltaSeconds;
                return;
            }

            var targetIndex = Mathf.Clamp(pathIndex + pathDirection, 0, path.Nodes.Count - 1);
            var target = originLocalPosition + (Vector3)path.Nodes[targetIndex];
            var next = Vector3.MoveTowards(
                transform.localPosition,
                target,
                Mathf.Max(0.01f, path.SpeedCellsPerSecond) * deltaSeconds);
            if (body != null && body.bodyType == RigidbodyType2D.Kinematic && transform.parent == null)
            {
                body.MovePosition(next);
            }
            else
            {
                transform.localPosition = next;
            }

            if (Vector3.SqrMagnitude(next - target) > 0.000001f)
            {
                return;
            }

            pathIndex = targetIndex;
            pathWait = Mathf.Max(0f, path.WaitSeconds);
            if (pathIndex == path.Nodes.Count - 1 || pathIndex == 0)
            {
                if (path.PingPong)
                {
                    pathDirection *= -1;
                }
                else if (path.ClosedLoop)
                {
                    pathIndex = 0;
                    transform.localPosition = originLocalPosition + (Vector3)path.Nodes[0];
                }
            }
        }

        private void TickPendulum(float deltaSeconds)
        {
            if (Kind != CommonElementKind.PendulumBall || Profile == null || deltaSeconds <= 0f ||
                element.CurrentState == MapElementState.Broken ||
                element.CurrentState == MapElementState.Disabled)
            {
                return;
            }

            pendulumElapsed += deltaSeconds;
            var period = Mathf.Max(0.01f, Profile.SwingPeriodSeconds);
            var phase = pendulumElapsed * Mathf.PI * 2f / period;
            var angle = Mathf.Sin(phase) * Profile.SwingArcDegrees * Mathf.Deg2Rad;
            var radius = Mathf.Max(2f, Profile.ChainLengthCells);
            transform.localPosition = originLocalPosition + new Vector3(
                Mathf.Sin(angle) * radius,
                -Mathf.Cos(angle) * radius,
                0f);
        }

        private void TickCrusher(float deltaSeconds)
        {
            if (Kind != CommonElementKind.Crusher || Profile == null || deltaSeconds <= 0f)
            {
                return;
            }

            var end = originLocalPosition +
                      (Vector3)((Vector2)facingDirection * Profile.TravelCells);
            if (element.CurrentState == MapElementState.Active && !crusherReturning)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    end,
                    Profile.MoveSpeedCellsPerSecond * deltaSeconds);
                if (Vector3.SqrMagnitude(transform.localPosition - end) <= 0.000001f)
                {
                    crusherHoldRemaining = crusherHoldRemaining > 0f
                        ? crusherHoldRemaining - deltaSeconds
                        : Profile.HoldSeconds;
                    if (crusherHoldRemaining <= 0f)
                    {
                        crusherReturning = true;
                        element.TrySetState(MapElementState.Cooldown);
                    }
                }
            }
            else if (element.CurrentState == MapElementState.Cooldown || crusherReturning)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    originLocalPosition,
                    Profile.ReturnSpeedCellsPerSecond * deltaSeconds);
                if (Vector3.SqrMagnitude(transform.localPosition - originLocalPosition) <= 0.000001f)
                {
                    crusherReturning = false;
                    crusherHoldRemaining = 0f;
                    element.TrySetState(MapElementState.Idle);
                }
            }
        }

        private void TickGuide(float deltaSeconds)
        {
            if (guideRemaining <= 0f || deltaSeconds <= 0f)
            {
                return;
            }

            guideRemaining = Mathf.Max(0f, guideRemaining - deltaSeconds);
            if (guideRemaining <= 0f && Kind == CommonElementKind.ExitGuideLantern)
            {
                variantState = string.Empty;
                element.TrySetState(MapElementState.Idle);
                RefreshPresentation();
            }
        }

        private void ClampRollingBoulderSpeed()
        {
            if (Kind != CommonElementKind.RollingBoulder || body == null ||
                body.bodyType != RigidbodyType2D.Dynamic || Profile == null)
            {
                return;
            }

            body.linearVelocity = Vector2.ClampMagnitude(
                body.linearVelocity,
                Mathf.Max(0.01f, Profile.MaximumSpeedCellsPerSecond));
        }

        private void TickWeightDoor(float deltaSeconds)
        {
            if (Kind != CommonElementKind.WeightDoor || Profile == null || deltaSeconds <= 0f)
            {
                return;
            }

            var target = signalActive ? 1f : 0f;
            var footprintHeight = element.Definition?.Footprint != null
                ? Mathf.Max(1, element.Definition.Footprint.BoundsSize.y)
                : 1f;
            doorOpenProgress = Mathf.MoveTowards(
                doorOpenProgress,
                target,
                Mathf.Max(0.01f, Profile.OpenSpeedCellsPerSecond) * deltaSeconds / footprintHeight);
            transform.localPosition = originLocalPosition + Vector3.up * (doorOpenProgress * footprintHeight);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null && !colliders[index].isTrigger)
                {
                    colliders[index].enabled = doorOpenProgress < 0.9f;
                }
            }
        }

        private void PublishSignal(bool active)
        {
            if (signalActive == active)
            {
                return;
            }

            signalActive = active;
            element.TrySetState(active ? MapElementState.Active : MapElementState.Idle);
            SignalChanged?.Invoke(Profile != null ? Profile.SignalChannel : string.Empty, active);
            RefreshPresentation();
        }

        private bool TryDamage(GameObject target)
        {
            var receiver = FindInterface<IMapElementDamageReceiver>(target);
            if (receiver == null || Profile == null)
            {
                return false;
            }

            var targetId = target.GetInstanceID();
            var now = Time.time;
            if (Kind == CommonElementKind.LaserEmitter &&
                damageActivationIds.TryGetValue(targetId, out var priorActivation) &&
                priorActivation == activationId)
            {
                return false;
            }
            if (damageTimes.TryGetValue(targetId, out var lastTime) &&
                now - lastTime < Mathf.Max(0f, Profile.DamageCooldownSeconds))
            {
                return false;
            }

            damageTimes[targetId] = now;
            damageActivationIds[targetId] = activationId;
            return receiver.ReceiveMapElementDamage(new MapElementDamageEvent(
                Mathf.Clamp(Profile.Damage, 0, 1),
                Vector2.Scale(Profile.Knockback, facingDirection),
                gameObject,
                activationId));
        }

        private void ApplyBounce(Collider2D other)
        {
            var targetBody = other.attachedRigidbody;
            if (targetBody == null || Profile == null)
            {
                return;
            }

            var targetId = targetBody.gameObject.GetInstanceID();
            if (bounceTimes.TryGetValue(targetId, out var lastBounce) &&
                Time.time - lastBounce < Mathf.Max(0f, element.Definition.BehaviorProfile.CooldownSeconds))
            {
                return;
            }

            var gravity = Mathf.Max(0.01f, Physics2D.gravity.magnitude * Mathf.Max(0.01f, targetBody.gravityScale));
            var velocity = Mathf.Sqrt(2f * gravity * Mathf.Max(0f, Profile.LaunchHeightCells));
            targetBody.linearVelocity = new Vector2(targetBody.linearVelocity.x, velocity);
            bounceTimes[targetId] = Time.time;
        }

        private void ApplyEnvironment(Collider2D other, bool wind)
        {
            if (wind && Profile.CycleOnSeconds > 0f && Profile.CycleOffSeconds > 0f)
            {
                var cycle = Profile.CycleOnSeconds + Profile.CycleOffSeconds;
                ventCycleElapsed = Mathf.Repeat(Time.time, cycle);
                if (ventCycleElapsed > Profile.CycleOnSeconds)
                {
                    return;
                }
            }

            var direction = (Vector2)facingDirection;
            var multiplier = 1f;
            if (wind)
            {
                var umbrella = FindInterface<IBridgeUmbrellaState>(other.gameObject);
                if (umbrella != null && umbrella.IsUmbrellaOpen)
                {
                    multiplier = Mathf.Max(1f, umbrella.WindForceMultiplier);
                }
            }
            var delta = direction * (Profile.ForceCellsPerSecond * multiplier * Time.fixedDeltaTime);
            var receiver = FindInterface<IMapElementEnvironmentalReceiver>(other.gameObject);
            if (receiver != null)
            {
                if (wind) receiver.ReceiveWind(delta);
                else receiver.ReceiveWater(delta);
                return;
            }

            var targetBody = other.attachedRigidbody;
            if (targetBody != null && targetBody.bodyType == RigidbodyType2D.Dynamic)
            {
                targetBody.linearVelocity += delta;
            }
        }

        private int ResolveWeight(GameObject target)
        {
            var source = FindInterface<IMapElementWeightSource>(target);
            return source != null ? Mathf.Clamp(source.PressureWeight, 1, 2) : 1;
        }

        private void EnsureBody(RigidbodyType2D bodyType, float gravityScale)
        {
            body = body != null ? body : gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = bodyType;
            body.gravityScale = gravityScale;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void TickLaserBeam()
        {
            if (Kind != CommonElementKind.LaserEmitter || Profile == null ||
                element.CurrentState != MapElementState.Active)
            {
                return;
            }

            var hits = Physics2D.RaycastAll(
                transform.position,
                facingDirection,
                Mathf.Clamp(Profile.SightOrBeamRangeCells, 2f, 12f));
            for (var index = 0; index < hits.Length; index++)
            {
                var hit = hits[index];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                TryDamage(hit.collider.gameObject);
                if (!hit.collider.isTrigger)
                {
                    break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            var profile = Profile;
            if (profile == null)
            {
                return;
            }

            var direction = (Vector3)(Vector2)SanitizeDirection(facingDirection, Vector2Int.right);
            if (profile.Kind == CommonElementKind.LaserEmitter ||
                profile.Kind == CommonElementKind.TotemShooter)
            {
                Gizmos.color = element != null && element.CurrentState == MapElementState.Active
                    ? Color.red
                    : new Color(1f, 0.65f, 0.2f, 0.9f);
                Gizmos.DrawLine(transform.position, transform.position + direction * profile.SightOrBeamRangeCells);
            }
            else if (profile.Kind == CommonElementKind.WindVent ||
                     profile.Kind == CommonElementKind.WaterVent)
            {
                Gizmos.color = profile.Kind == CommonElementKind.WindVent ? Color.cyan : Color.blue;
                var center = transform.position + direction * (profile.VolumeSizeCells.x * 0.5f);
                Gizmos.DrawWireCube(center, profile.VolumeSizeCells);
                Gizmos.DrawLine(transform.position, transform.position + direction * 1.5f);
            }
            else if (profile.Kind == CommonElementKind.BouncePad)
            {
                Gizmos.color = Color.green;
                var gravity = Mathf.Max(0.01f, Physics2D.gravity.magnitude);
                var velocity = Mathf.Sqrt(2f * gravity * profile.LaunchHeightCells);
                var previous = transform.position;
                for (var step = 1; step <= 12; step++)
                {
                    var time = step * 0.08f;
                    var next = transform.position + Vector3.up * (velocity * time - 0.5f * gravity * time * time);
                    Gizmos.DrawLine(previous, next);
                    previous = next;
                }
            }
        }

        private void RefreshPresentation()
        {
            if (element == null)
            {
                return;
            }

            var unavailable = element.CurrentState == MapElementState.Broken ||
                              element.CurrentState == MapElementState.Disabled;
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] == null)
                {
                    continue;
                }

                var enabled = !unavailable;
                if (Kind == CommonElementKind.LaserEmitter && colliders[index].isTrigger)
                {
                    enabled = !unavailable && element.CurrentState == MapElementState.Active;
                }
                else if (Kind == CommonElementKind.Spike && colliders[index].isTrigger)
                {
                    enabled = !unavailable && !covered;
                }
                colliders[index].enabled = enabled;
            }

            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] == null)
                {
                    continue;
                }

                var color = renderers[index].color;
                color.a = unavailable ? 0.25f : element.CurrentState == MapElementState.Warning ? 0.55f : 1f;
                if (variantState == "WetMud") color = new Color(0.25f, 0.38f, 0.48f, color.a);
                else if (variantState == "CompressedPlatform") color = new Color(0.33f, 0.29f, 0.24f, color.a);
                else if (variantState == "Guiding") color = new Color(1f, 0.88f, 0.34f, color.a);
                renderers[index].color = color;
            }
        }

        private static Vector2Int SanitizeDirection(Vector2Int value, Vector2Int fallback)
        {
            if (value == Vector2Int.left || value == Vector2Int.right ||
                value == Vector2Int.up || value == Vector2Int.down)
            {
                return value;
            }
            if (fallback == Vector2Int.left || fallback == Vector2Int.right ||
                fallback == Vector2Int.up || fallback == Vector2Int.down)
            {
                return fallback;
            }
            return Vector2Int.right;
        }

        private static T FindInterface<T>(GameObject target) where T : class
        {
            if (target == null)
            {
                return null;
            }

            var behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is T match)
                {
                    return match;
                }
            }
            return null;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CommonElementProjectile : MonoBehaviour, IUmbrellaDeflectableProjectile
    {
        private Vector2 direction;
        private float speed;
        private int damage;
        private GameObject source;
        private int activationId;
        private float lifeSeconds = 4f;
        private int lastDeflectorId;
        private float lastDeflectedAt = float.NegativeInfinity;

        public bool CanUmbrellaDeflect => true;
        public Vector2 Velocity => direction * speed;

        public void Configure(Vector2Int moveDirection, float moveSpeed, int damageValue, GameObject damageSource, int id)
        {
            direction = moveDirection;
            speed = Mathf.Max(0f, moveSpeed);
            damage = Mathf.Clamp(damageValue, 0, 1);
            source = damageSource;
            activationId = id;
        }

        public bool TryDeflect(Vector2 reflectedDirection, float maximumSpeed, GameObject deflector)
        {
            if (reflectedDirection.sqrMagnitude <= 0.0001f || deflector == null)
            {
                return false;
            }
            if (lastDeflectorId == deflector.GetInstanceID()
                && Time.unscaledTime - lastDeflectedAt < 0.10f)
            {
                return false;
            }

            direction = reflectedDirection.normalized;
            speed = Mathf.Min(Mathf.Max(0f, maximumSpeed), Mathf.Max(1f, speed));
            source = deflector;
            lastDeflectorId = deflector.GetInstanceID();
            lastDeflectedAt = Time.unscaledTime;
            return true;
        }

        private void Update()
        {
            transform.position += (Vector3)(direction * (speed * Time.deltaTime));
            lifeSeconds -= Time.deltaTime;
            if (lifeSeconds <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || other.gameObject == source)
            {
                return;
            }

            var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IToolReactionReceiver toolReceiver)
                {
                    var cardinal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                        ? (direction.x < 0f ? Vector2Int.left : Vector2Int.right)
                        : (direction.y < 0f ? Vector2Int.down : Vector2Int.up);
                    var result = toolReceiver.TryReact(new ToolReactionContext
                    {
                        ActionId = unchecked((activationId * 397) ^ GetInstanceID()),
                        Tags = ToolTag.Projectile,
                        Direction = cardinal,
                        Magnitude = speed,
                        Source = source,
                        Instigator = source,
                    });
                    if (result.Accepted)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IMapElementDamageReceiver receiver)
                {
                    receiver.ReceiveMapElementDamage(new MapElementDamageEvent(
                        damage,
                        direction * 3f,
                        source,
                        activationId));
                    Destroy(gameObject);
                    return;
                }
            }

            if (!other.isTrigger)
            {
                Destroy(gameObject);
            }
        }
    }
}

#endif
