#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class MoonElementDriver : MonoBehaviour,
        IMapElementSignalReceiver,
        IMapElementInteractionReceiver,
        IMapElementPersistentParticipant
    {
        [SerializeField] private string variantState = string.Empty;
        [SerializeField] private float cycleElapsed;
        [SerializeField] private float warningRemaining;
        [SerializeField] private float waterDisableRemaining;
        [SerializeField] private float targetRotation;
        [SerializeField] private int currentSegmentCount;
        [SerializeField] private int insertedIngredients;
        [SerializeField] private bool outputReady;
        [SerializeField] private bool occupied;

        private MapElementInstance element;
        private Rigidbody2D body;
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Vector3 originLocalScale;
        private bool initialized;
        private bool rotating;

        public MoonElementKind Kind => Profile != null ? Profile.Kind : MoonElementKind.None;
        public string VariantState => variantState;
        public float WarningRemaining => warningRemaining;
        public int CurrentSegmentCount => currentSegmentCount;
        public int InsertedIngredients => insertedIngredients;
        public bool OutputReady => outputReady;
        public bool IsVentActive => Kind == MoonElementKind.FlourVent &&
                                    waterDisableRemaining <= 0f &&
                                    cycleElapsed < Mathf.Max(0.01f, Profile.CycleOnSeconds);
        public string PersistenceId => element != null ? $"{element.PersistenceId}:moon" : string.Empty;
        public string InteractionPrompt => Kind == MoonElementKind.MedicineMortar
            ? outputReady ? "[X] 약 꺼내기" : "[X] 재료 넣기"
            : string.Empty;

        private MoonElementRuntimeProfile Profile =>
            element != null && element.Definition != null ? element.Definition.MoonProfile : null;

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
                case MoonElementKind.MoonIronBall:
                    if ((context.Tags & ToolTag.Hook) != 0)
                    {
                        cycleElapsed += Profile.SwingPeriodSeconds * 0.25f;
                        variantState = "OrbitPulled";
                        RefreshPresentation();
                        return true;
                    }
                    break;

                case MoonElementKind.FallingMortar:
                    if ((context.Tags & (ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Pound)) != 0)
                    {
                        return BeginFall(Profile.ShadowWarningSeconds, "SupportRemoved");
                    }
                    break;

                case MoonElementKind.DoughPlatform:
                    if ((context.Tags & ToolTag.Water) != 0)
                    {
                        variantState = "Sticky";
                        RefreshPresentation();
                        return true;
                    }
                    if ((context.Tags & ToolTag.Pound) != 0)
                    {
                        variantState = "BouncePad";
                        RefreshPresentation();
                        return true;
                    }
                    if ((context.Tags & ToolTag.Bomb) != 0)
                    {
                        variantState = "Scattered";
                        RefreshPresentation();
                        return element.TrySetState(MapElementState.Broken);
                    }
                    break;

                case MoonElementKind.CraterSlab:
                    if ((context.Tags & (ToolTag.HeavyImpact | ToolTag.Bomb)) != 0)
                    {
                        return BeginFall(Profile.FallDelaySeconds, "Tilting");
                    }
                    break;

                case MoonElementKind.CassiaRoot:
                    if ((context.Tags & ToolTag.Water) != 0)
                    {
                        SetRootSegments(Profile.SegmentCount, "Grown");
                        return true;
                    }
                    if ((context.Tags & ToolTag.Pickaxe) != 0)
                    {
                        variantState = "Cut";
                        RefreshPresentation();
                        return element.TrySetState(MapElementState.Broken);
                    }
                    if ((context.Tags & ToolTag.Hook) != 0)
                    {
                        SetRootSegments(Mathf.Max(Profile.MinimumSegmentCount, currentSegmentCount - 1), "Pulled");
                        return true;
                    }
                    break;

                case MoonElementKind.MillShaft:
                    if ((context.Tags & ToolTag.Hook) != 0)
                    {
                        QueueShaftStep();
                        return true;
                    }
                    if ((context.Tags & ToolTag.HeavyImpact) != 0)
                    {
                        rotating = false;
                        variantState = "StoppedByHeavy";
                        element.TrySetState(MapElementState.Disabled);
                        RefreshPresentation();
                        return true;
                    }
                    break;

                case MoonElementKind.MedicineMortar:
                    if ((context.Tags & ToolTag.Context) != 0)
                    {
                        return TryInteract(context.Source != null ? context.Source : context.Instigator);
                    }
                    if ((context.Tags & ToolTag.Pound) != 0 &&
                        insertedIngredients >= Mathf.Max(1, Profile.InputSlots))
                    {
                        outputReady = true;
                        variantState = "MedicineReady";
                        element.TrySetState(MapElementState.Active);
                        RefreshPresentation();
                        return true;
                    }
                    break;

                case MoonElementKind.FlourVent:
                    if ((context.Tags & ToolTag.WindGuard) != 0)
                    {
                        variantState = "UmbrellaLift";
                        element.TrySetState(MapElementState.Active);
                        RefreshPresentation();
                        return true;
                    }
                    if ((context.Tags & ToolTag.Water) != 0)
                    {
                        waterDisableRemaining = Mathf.Max(0.01f, Profile.WaterDisableSeconds);
                        variantState = "WetStopped";
                        element.TrySetState(MapElementState.Disabled);
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
            if (Kind != MoonElementKind.MedicineMortar || element.CurrentState == MapElementState.Broken)
            {
                return false;
            }

            if (outputReady)
            {
                outputReady = false;
                insertedIngredients = 0;
                variantState = "Collected";
                element.TrySetState(MapElementState.Idle);
                RefreshPresentation();
                return true;
            }

            if (insertedIngredients >= Mathf.Max(1, Profile.InputSlots))
            {
                return false;
            }

            insertedIngredients++;
            variantState = $"Ingredient{insertedIngredients}";
            RefreshPresentation();
            return true;
        }

        public void ReceiveSignal(string channel, bool active)
        {
            Initialize();
            if (Kind == MoonElementKind.CassiaRoot)
            {
                SetRootSegments(active ? Profile.SegmentCount : Profile.MinimumSegmentCount,
                    active ? "Extended" : "Contracted");
            }
            else if (Kind == MoonElementKind.MillShaft && active)
            {
                QueueShaftStep();
            }
        }

        public void NotifyTriggerEnter(Collider2D other)
        {
            Initialize();
            if (other == null)
            {
                return;
            }

            if (Kind == MoonElementKind.CraterSlab)
            {
                occupied = true;
                BeginFall(Profile.FallDelaySeconds, "Tilting");
            }
            else if (Kind == MoonElementKind.DoughPlatform)
            {
                occupied = true;
                variantState = "Compressed";
                ApplyDough(other);
                RefreshPresentation();
            }
        }

        public void NotifyTriggerStay(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (Kind == MoonElementKind.DoughPlatform)
            {
                ApplyDough(other);
            }
            else if (Kind == MoonElementKind.FlourVent && IsVentActive)
            {
                ApplyVent(other);
            }
        }

        public void NotifyTriggerExit(Collider2D other)
        {
            if (Kind == MoonElementKind.DoughPlatform)
            {
                occupied = false;
                if (variantState == "Compressed")
                {
                    variantState = string.Empty;
                    RefreshPresentation();
                }
            }
        }

        public void NotifyCollisionEnter(Collision2D collision)
        {
            Initialize();
            if (collision == null)
            {
                return;
            }

            if (Kind == MoonElementKind.MoonIronBall ||
                (Kind == MoonElementKind.FallingMortar && element.CurrentState == MapElementState.Active) ||
                (Kind == MoonElementKind.MillShaft && rotating))
            {
                ApplyDamage(collision.gameObject);
                ApplyHeavyImpact(collision.gameObject, collision.relativeVelocity);
            }
        }

        public string CaptureMapElementState()
        {
            return JsonUtility.ToJson(new PersistentState
            {
                VariantState = variantState,
                CycleElapsed = cycleElapsed,
                CurrentSegmentCount = currentSegmentCount,
                InsertedIngredients = insertedIngredients,
                OutputReady = outputReady,
                LocalRotation = transform.localEulerAngles.z,
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
            cycleElapsed = Mathf.Max(0f, state.CycleElapsed);
            currentSegmentCount = Mathf.Max(0, state.CurrentSegmentCount);
            insertedIngredients = Mathf.Max(0, state.InsertedIngredients);
            outputReady = state.OutputReady;
            targetRotation = state.LocalRotation;
            transform.localRotation = Quaternion.Euler(0f, 0f, targetRotation);
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
            originLocalScale = transform.localScale;
            targetRotation = transform.localEulerAngles.z;
            if (Profile != null && currentSegmentCount <= 0)
            {
                currentSegmentCount = Profile.SegmentCount;
            }
            initialized = true;
            RefreshPresentation();
        }

        private void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Profile == null)
            {
                return;
            }

            if (Kind == MoonElementKind.MoonIronBall)
            {
                cycleElapsed = Mathf.Repeat(cycleElapsed + deltaSeconds, Mathf.Max(0.01f, Profile.SwingPeriodSeconds));
                var phase = cycleElapsed / Mathf.Max(0.01f, Profile.SwingPeriodSeconds) * Mathf.PI * 2f;
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * Profile.SwingArcDegrees);
            }

            if (warningRemaining > 0f)
            {
                warningRemaining -= deltaSeconds;
                if (warningRemaining <= 0f)
                {
                    StartPhysicalFall();
                }
            }

            if (Kind == MoonElementKind.MillShaft && rotating)
            {
                var next = Mathf.MoveTowardsAngle(transform.localEulerAngles.z, targetRotation,
                    Profile.RotationSpeedDegreesPerSecond * deltaSeconds);
                transform.localRotation = Quaternion.Euler(0f, 0f, next);
                if (Mathf.Abs(Mathf.DeltaAngle(next, targetRotation)) <= 0.1f)
                {
                    rotating = false;
                    variantState = "Stepped90";
                    element.TrySetState(MapElementState.Idle);
                    RefreshPresentation();
                }
            }

            if (Kind == MoonElementKind.FlourVent)
            {
                if (waterDisableRemaining > 0f)
                {
                    waterDisableRemaining -= deltaSeconds;
                    if (waterDisableRemaining <= 0f)
                    {
                        cycleElapsed = 0f;
                        variantState = "VentOn";
                        element.TrySetState(MapElementState.Idle);
                        RefreshPresentation();
                    }
                }
                else
                {
                    var total = Mathf.Max(0.01f, Profile.CycleOnSeconds + Profile.CycleOffSeconds);
                    cycleElapsed = Mathf.Repeat(cycleElapsed + deltaSeconds, total);
                    var nextVariant = IsVentActive ? "VentOn" : "VentOff";
                    if (variantState != nextVariant)
                    {
                        variantState = nextVariant;
                        RefreshPresentation();
                    }
                }
            }
        }

        private bool BeginFall(float warningSeconds, string warningState)
        {
            if (warningRemaining > 0f || element.CurrentState == MapElementState.Active)
            {
                return false;
            }

            warningRemaining = Mathf.Max(0.01f, warningSeconds);
            variantState = warningState;
            element.TrySetState(MapElementState.Warning);
            if (Kind == MoonElementKind.CraterSlab)
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, (int)Profile.TiltSide * 8f);
            }
            RefreshPresentation();
            return true;
        }

        private void StartPhysicalFall()
        {
            if (Kind != MoonElementKind.FallingMortar && Kind != MoonElementKind.CraterSlab)
            {
                return;
            }

            variantState = "Falling";
            element.TrySetState(MapElementState.Active);
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 2.5f;
                body.freezeRotation = Kind == MoonElementKind.FallingMortar;
                body.simulated = true;
            }
            RefreshPresentation();
        }

        private void QueueShaftStep()
        {
            targetRotation += Profile.StepAngleDegrees;
            rotating = true;
            variantState = "Rotating";
            element.TrySetState(MapElementState.Active);
            RefreshPresentation();
        }

        private void SetRootSegments(int count, string state)
        {
            currentSegmentCount = Mathf.Clamp(count, Profile.MinimumSegmentCount, Profile.SegmentCount);
            variantState = state;
            RefreshPresentation();
        }

        private void ApplyDough(Collider2D other)
        {
            var targetBody = other.attachedRigidbody;
            if (targetBody == null)
            {
                return;
            }

            if (variantState == "BouncePad")
            {
                targetBody.linearVelocity = new Vector2(targetBody.linearVelocity.x,
                    Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0f, Profile.BounceHeightCells)));
            }
            else
            {
                var slow = variantState == "Sticky" ? 0.35f : 1f - Profile.Softness * 0.5f;
                targetBody.linearVelocity = new Vector2(targetBody.linearVelocity.x * slow, targetBody.linearVelocity.y);
            }
        }

        private void ApplyVent(Collider2D other)
        {
            var direction = Profile.Direction == Vector2Int.zero ? Vector2Int.up : Profile.Direction;
            var velocity = ((Vector2)direction).normalized * Profile.ForceCellsPerSecond * Time.fixedDeltaTime;
            var behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
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
            if (target == null || Profile.Damage <= 0)
            {
                return;
            }

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

        private void ApplyHeavyImpact(GameObject target, Vector2 velocity)
        {
            if (target == null)
            {
                return;
            }

            var behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IToolReactionReceiver receiver)
                {
                    receiver.TryReact(new ToolReactionContext
                    {
                        ActionId = unchecked((GetInstanceID() * 397) ^ Time.frameCount),
                        Tags = ToolTag.HeavyImpact,
                        Direction = Cardinal(velocity),
                        Magnitude = Mathf.Max(1f, velocity.magnitude),
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
            if (variantState == "Sticky" || variantState == "WetStopped") tint = new Color(0.55f, 0.78f, 1f);
            else if (variantState == "BouncePad" || variantState == "MedicineReady") tint = new Color(0.5f, 1f, 0.55f);
            else if (variantState == "SupportRemoved" || variantState == "Tilting" || variantState == "Falling") tint = new Color(1f, 0.48f, 0.25f);
            else if (variantState == "StoppedByHeavy" || variantState == "VentOff") tint = new Color(0.55f, 0.55f, 0.62f);

            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].color = tint;
                }
            }

            if (Kind == MoonElementKind.DoughPlatform)
            {
                var compression = occupied ? Mathf.Clamp01(Profile.CompressionCells) : 0f;
                transform.localScale = new Vector3(originLocalScale.x, originLocalScale.y * (1f - compression), originLocalScale.z);
            }
            else if (Kind == MoonElementKind.CassiaRoot)
            {
                var ratio = currentSegmentCount / (float)Mathf.Max(1, Profile.SegmentCount);
                var visualRoot = transform.Find("VisualRoot");
                if (visualRoot != null)
                {
                    visualRoot.localScale = new Vector3(ratio, 1f, 1f);
                }
            }
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
            public float CycleElapsed;
            public int CurrentSegmentCount;
            public int InsertedIngredients;
            public bool OutputReady;
            public float LocalRotation;
        }
    }

}

#endif
