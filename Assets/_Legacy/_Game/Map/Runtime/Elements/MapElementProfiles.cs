#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    public enum ElementCategory
    {
        Terrain,
        Platform,
        Hazard,
        Trigger,
        Control,
        Door,
        Vent,
        Utility,
        Anchor,
        Container,
        Event,
        Decoration,
        Maru,
    }

    [Flags]
    public enum ElementCategoryMask
    {
        None = 0,
        Terrain = 1 << 0,
        Platform = 1 << 1,
        Hazard = 1 << 2,
        Trigger = 1 << 3,
        Control = 1 << 4,
        Door = 1 << 5,
        Vent = 1 << 6,
        Utility = 1 << 7,
        Anchor = 1 << 8,
        Container = 1 << 9,
        Event = 1 << 10,
        Decoration = 1 << 11,
        Maru = 1 << 12,
        All = (1 << 13) - 1,
    }

    [Flags]
    public enum RegionMask
    {
        None = 0,
        Common = 1 << 0,
        Moon = 1 << 1,
        Bridge = 1 << 2,
        Palace = 1 << 3,
        Post = 1 << 4,
        Sun = 1 << 5,
        Polaris = 1 << 6,
        Maru = 1 << 7,
        All = (1 << 8) - 1,
    }

    public enum ElementVisualRenderMode
    {
        SingleSprite,
        TiledSprite,
        SegmentedSprite,
        TilemapStamp,
        AnimatorPrefab,
    }

    public enum ColliderAuthoringMode
    {
        BoxPerCell,
        MergedBoxes,
        Capsule,
        PolygonCustom,
        SpritePhysicsShape,
        CompositeTileStamp,
    }

    public enum SerializedColliderShapeType
    {
        Box,
        Capsule,
        Polygon,
    }

    public enum SurfaceRequirement
    {
        None,
        Floor,
        Wall,
        Ceiling,
        Solid,
        OneWay,
        FloorOrOneWay,
        Any,
    }

    public enum ElementPathEasing
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
    }

    [Flags]
    public enum ToolTag
    {
        None = 0,
        Bomb = 1 << 0,
        Pickaxe = 1 << 1,
        Shovel = 1 << 2,
        Water = 1 << 3,
        Pound = 1 << 4,
        Hook = 1 << 5,
        Rope = 1 << 6,
        WindGuard = 1 << 7,
        LightImpact = 1 << 8,
        HeavyImpact = 1 << 9,
        Fire = 1 << 10,
        Projectile = 1 << 11,
        Context = 1 << 12,
        Cut = 1 << 13,
    }

    public enum ElementReactionType
    {
        None,
        SetState,
        Break,
        Disable,
        Move,
        Push,
        Pull,
        Toggle,
    }

    public enum CommonElementKind
    {
        None,
        SolidBlock,
        UnbreakableBlock,
        CrackedBlock,
        SoftSoil,
        OneWayPlatform,
        FragileFloor,
        PressurePlate,
        Lever,
        WeightDoor,
        MovingPlatform,
        FallingStone,
        Spike,
        TotemShooter,
        LaserEmitter,
        WindVent,
        WaterVent,
        BouncePad,
        PendulumBall,
        Crusher,
        PulleyLift,
        RollingBoulder,
        RopeAnchor,
        HookAnchor,
        BreakableContainer,
        ExitGuideLantern,
    }

    public enum CommonSignalMode
    {
        Hold,
        Toggle,
        Pulse,
        OneShot,
    }

    [Serializable]
    public sealed class CommonElementRuntimeProfile
    {
        public CommonElementKind Kind;
        [Min(0)] public int Damage = 1;
        public Vector2 Knockback = new Vector2(4f, 2f);
        [Min(1)] public int WeakHitsRequired = 1;
        [Min(0f)] public float TriggerDwellSeconds;
        [Min(0f)] public float GravityScale;
        [Min(0f)] public float SightOrBeamRangeCells;
        [Min(0f)] public float ProjectileSpeedCellsPerSecond;
        [Min(0f)] public float OpenSpeedCellsPerSecond;
        [Min(1)] public int WeightThreshold = 1;
        public CommonSignalMode SignalMode = CommonSignalMode.Hold;
        public string SignalChannel;
        [Min(0f)] public float PulseSeconds;
        public Vector2 VolumeSizeCells = Vector2.one;
        [Min(0f)] public float ForceCellsPerSecond;
        [Min(0f)] public float CycleOnSeconds;
        [Min(0f)] public float CycleOffSeconds;
        [Min(0f)] public float LaunchHeightCells;
        [Min(0f)] public float DamageCooldownSeconds = 0.15f;
        [Range(2, 5)] public int ChainLengthCells = 3;
        [Range(0f, 55f)] public float SwingArcDegrees = 55f;
        [Min(0.01f)] public float SwingPeriodSeconds = 2.4f;
        [Min(0f)] public float TravelCells = 4f;
        [Min(0.01f)] public float MoveSpeedCellsPerSecond = 2f;
        [Min(0f)] public float HoldSeconds = 0.4f;
        [Min(0.01f)] public float ReturnSpeedCellsPerSecond = 3f;
        [Min(0.01f)] public float MaximumSpeedCellsPerSecond = 6f;
        [Min(0f)] public float GuideDurationSeconds = 3f;
        public string ContentsId = "Empty";
    }

    public enum MaruElementKind
    {
        None,
        ReturnStatue,
        ReturnBellJar,
        CollarFragment,
        ReturnMarker,
        PawprintPool,
        RecordCasket,
    }

    public enum MaruMarkerCostType
    {
        Money,
        Health,
    }

    public enum MaruRecordGuideEffect
    {
        ExitDirection,
        ValuableRoom,
        ElementWeakness,
        DisableOneTrap,
    }

    public enum MoonElementKind
    {
        None,
        MoonIronBall,
        FallingMortar,
        DoughPlatform,
        CraterSlab,
        CassiaRoot,
        MillShaft,
        MedicineMortar,
        FlourVent,
    }

    public enum MoonTiltSide
    {
        Left = -1,
        Right = 1,
    }

    [Serializable]
    public sealed class MoonElementRuntimeProfile
    {
        public MoonElementKind Kind;
        [Min(0)] public int Damage = 1;
        public Vector2 Knockback = new Vector2(4f, 2f);

        [Range(2, 4)] public int ChainLengthCells = 3;
        [Range(0f, 50f)] public float SwingArcDegrees = 50f;
        [Min(0.01f)] public float SwingPeriodSeconds = 2.6f;

        [Min(1f)] public float FallHeightCells = 5f;
        [Min(0f)] public float ShadowWarningSeconds = 0.75f;
        public bool ResetAfterFall;

        [Range(1, 4)] public int WidthCells = 2;
        [Range(0f, 1f)] public float CompressionCells = 0.4f;
        [Min(0f)] public float BounceHeightCells = 3f;
        [Range(0f, 1f)] public float Softness = 0.65f;

        public MoonTiltSide TiltSide = MoonTiltSide.Right;
        [Min(0f)] public float FallDelaySeconds = 0.5f;

        [Range(2, 8)] public int SegmentCount = 4;
        [Range(2, 8)] public int MinimumSegmentCount = 2;

        [Range(1f, 90f)] public float StepAngleDegrees = 90f;
        [Min(1f)] public float RotationSpeedDegreesPerSecond = 180f;

        [Min(1)] public int InputSlots = 2;
        public string OutputId = "moon_medicine";
        [Min(0)] public int HealAmount = 1;

        public Vector2Int Direction = Vector2Int.up;
        [Min(0f)] public float ForceCellsPerSecond = 7f;
        [Min(0f)] public float CycleOnSeconds = 1.2f;
        [Min(0f)] public float CycleOffSeconds = 1f;
        [Min(0f)] public float WaterDisableSeconds = 2f;
    }

    public enum BridgeElementKind
    {
        None,
        ThreadBridge,
        KnotPulley,
        WindBanner,
        ThreadBlade,
        MagpiePlatform,
        FeatherUpdraft,
        BreakingStarPanel,
        Nest,
    }

    [Serializable]
    public sealed class BridgeElementRuntimeProfile
    {
        public BridgeElementKind Kind;
        [Min(0)] public int Damage = 1;
        public Vector2 Knockback = new Vector2(4f, 2f);

        [Range(2, 8)] public int LengthCells = 4;
        [Min(1)] public int MaxWeight = 2;
        [Range(0f, 1f)] public float SagCells = 0.3f;

        [Min(0f)] public float TravelCells = 4f;
        [Min(0.01f)] public float WeightRatio = 1f;

        public Vector2Int Direction = Vector2Int.right;
        public bool FlipOnSignal = true;
        [Range(0f, 1f)] public float WetForceMultiplier = 0.5f;
        [Min(1f)] public float UmbrellaAssistMultiplier = 1.25f;

        [Min(0f)] public float PathSpeedCellsPerSecond = 3f;
        [Min(0f)] public float WarningSeconds = 0.35f;
        [Min(0)] public int MinimumStrongCrosswindDistanceCells = 6;

        [Range(1, 2)] public int PlatformWidthCells = 2;
        [Min(2)] public int StopCount = 2;
        [Min(0f)] public float WaitTimeSeconds = 0.75f;
        [Min(1f)] public float HeavyDescentMultiplier = 2f;

        public Vector2 VolumeSizeCells = new Vector2(2f, 4f);
        [Min(0f)] public float ForceCellsPerSecond = 8f;
        [Min(1f)] public float UmbrellaLiftMultiplier = 1.5f;

        [Min(1)] public int HitCount = 2;
        [Min(0f)] public float DwellBreakSeconds = 0.5f;

        [Min(1)] public int RequiredPieces = 3;
        public string SupportRewardId = "magpie_next_room_support";
        public bool CriticalObject;
    }

    public enum PalaceElementKind
    {
        None,
        SluiceGate,
        BubbleCannon,
        CurrentVolume,
        TurtlePlatform,
        ClamBounce,
        WaterMirrorWall,
        DrainGrate,
        DragonGateWaterfall,
    }

    [Serializable]
    public sealed class PalaceElementRuntimeProfile
    {
        public PalaceElementKind Kind;
        public Vector2 Knockback = new Vector2(5f, 2f);

        [Range(1, 2)] public int WidthCells = 1;
        [Min(1)] public int HeightCells = 3;
        [Min(0.01f)] public float MoveSpeedCellsPerSecond = 2f;
        public bool PreventPermanentLock = true;

        public Vector2Int Direction = Vector2Int.right;
        [Min(0.01f)] public float IntervalSeconds = 1.8f;
        [Min(0f)] public float ProjectileSpeedCellsPerSecond = 5f;
        [Range(0f, 1f)] public float UmbrellaPushMultiplier = 0.5f;

        public Vector2 VolumeSizeCells = new Vector2(4f, 2f);
        [Min(0f)] public float ForceCellsPerSecond = 8f;
        [Range(0f, 1f)] public float Falloff = 0.25f;
        [Range(0f, 1f)] public float HeavyBlockMultiplier = 0.35f;
        [Min(0)] public int ExitSafePocketCells = 2;

        [Min(0f)] public float SinkDepthCells = 1f;
        [Min(1)] public int WeightThreshold = 1;

        [Min(0.01f)] public float CycleSeconds = 0.8f;
        [Min(0f)] public float LaunchHeightCells = 4f;
        public bool ReflectProjectiles = true;

        public Vector2Int NormalDirection = Vector2Int.left;
        public bool TransparentOnSignal = true;
        public string TransparencyContextId = "yeouiju";

        [Min(0f)] public float DrainRatePerSecond = 0.5f;
        public bool StartsMudBlocked = true;
        public bool KeepVoidRecoveryIndependent = true;

        public bool StartsActive = true;
        [Min(1f)] public float UmbrellaLiftMultiplier = 1.4f;
        [Min(1f)] public float CloudSupportMultiplier = 1.5f;
        public bool CanRefillWateringCan = true;
    }

    public enum PostElementKind
    {
        None,
        Conveyor,
        ParcelLauncher,
        ReturnStamp,
        SortingArm,
        MailTube,
        InkPool,
        ParcelStack,
        ExpressTube,
    }

    [Serializable]
    public sealed class PostElementRuntimeProfile
    {
        public PostElementKind Kind;

        [Range(2, 8)] public int LengthCells = 4;
        public Vector2Int Direction = Vector2Int.right;
        [Min(0f)] public float SurfaceSpeedCellsPerSecond = 2.5f;
        public bool StopsOnHeavy = true;
        public bool KeepPortalExitSafe = true;

        [Min(0f)] public float LaunchArc = 0.65f;
        [Min(0f)] public float LaunchPower = 10f;
        [Min(0)] public int CollisionDamage = 1;
        public bool RequiresParcelInsertion = true;
        public bool RejectPlayerEntry = true;

        [Min(0f)] public float WarningDelaySeconds = 0.7f;
        [Min(0f)] public float StampActiveSeconds = 0.15f;
        [Min(0)] public int StampDamage = 1;
        public string StampType = "Return";
        [Min(0)] public int EscapeSpaceBelowCells = 1;

        [Min(1)] public int RotationStepDegrees = 90;
        public List<int> RotationSequenceDegrees = new List<int> { 0, 90, 180, 270 };
        [Min(0f)] public float PushForceCellsPerSecond = 6f;

        public bool RequiresPair = true;
        public string PairGuid = string.Empty;
        public bool OneWay;
        public string CompatibleParcelId = "*";

        [Range(2, 6)] public int WidthCells = 4;
        [Range(0f, 1f)] public float SlowRate = 0.4f;
        public bool RevealsHiddenFootprints = true;
        public bool WaterDilutes = true;
        public bool UmbrellaBlocksDrops;

        [Min(1)] public int BoxCount = 4;
        public string StackPattern = "2x2";
        [Range(0.1f, 1f)] public float FlattenedHeightMultiplier = 0.4f;

        public string RequiredStoryFlag = "post.express.enabled";
        public string RequiredParcelId = "OBJ_ParcelExpress";
        public bool StartsActive;
    }

    public enum SunElementKind
    {
        None,
        RotatingSunbeam,
        ShadowSeed,
        SunflowerPlatform,
        GrowthVine,
        DewDrop,
        OverheatPlatform,
        SunsetFlower,
        CrowPerch,
    }

    public enum SunPhase
    {
        Day,
        Shadow,
    }

    [Serializable]
    public sealed class SunElementRuntimeProfile
    {
        public SunElementKind Kind;

        [Range(60f, 180f)] public float ArcDegrees = 120f;
        [Min(0f)] public float RotationSpeedDegreesPerSecond = 60f;
        [Min(0.01f)] public float CycleOnSeconds = 2f;
        [Min(0.01f)] public float CycleOffSeconds = 1f;
        [Min(0)] public int Damage = 1;
        public bool CausesOverheat = true;
        public bool IgnoreSolidBlockers = true;
        public bool IgnoreUmbrellaBlock = true;
        public bool RotateOnSignal = true;
        public bool PreventFullOverheatOverlap = true;

        public Vector2 ShadowSizeCells = new Vector2(2f, 2f);
        [Min(0f)] public float ShadowRadiusCells = 1f;
        [Min(0f)] public float ShadowLifetimeSeconds = 6f;
        public bool WaterSuppressesShadow = true;
        public bool KeepExitMarkersVisible = true;

        [Range(1, 2)] public int PlatformWidthCells = 2;
        [Min(1)] public int PlatformRotationStepDegrees = 90;
        public string LightSourceRef = "RoomSun";
        public bool BloomsInLight = true;
        public bool ClosesOnOverheat = true;

        [Min(1)] public int StartLengthCells = 1;
        [Min(1)] public int MaxLengthCells = 6;
        public Vector2Int GrowthDirection = Vector2Int.up;
        public bool StopAtUnbreakableBoundary = true;

        [Min(0.01f)] public float FallIntervalSeconds = 2.5f;
        public bool CoolOnImpact = true;
        public bool CanFullyRefillWateringCan = true;
        [Min(0f)] public float ThrownWaterMagnitude = 1f;

        [Range(1, 2)] public int OverheatPlatformWidthCells = 2;
        [Min(0.01f)] public float SafeSeconds = 2f;
        [Min(0.01f)] public float OverheatSeconds = 1f;
        [Min(0f)] public float OverheatWarningSeconds = 0.25f;
        [Min(0f)] public float WaterCoolSeconds = 3f;
        public bool PreventFullSunbeamOverlap = true;

        public SunPhase InitialPhase = SunPhase.Day;

        public string EventId = "sun.crow.rescue";
        public List<string> AcceptedContextIds = new List<string> { "letter", "sun_ember" };
    }

    public enum PolarisElementKind
    {
        None,
        OrbitPlatform,
        ObservationBeam,
        ReturnField,
        StarWeight,
        GravityDial,
        ConstellationBridge,
        MemoryBell,
        ImmutableStarBlock,
    }

    [Serializable]
    public sealed class PolarisElementRuntimeProfile
    {
        public PolarisElementKind Kind;

        [Range(1, 2)] public int PlatformWidthCells = 2;
        public Vector2 OrbitRadiusCells = new Vector2(3f, 2f);
        [Min(0.01f)] public float OrbitPeriodSeconds = 4f;
        [Range(0.1f, 1f)] public float DialOrbitMultiplier = 0.65f;
        public bool KeepOrbitInsideCamera = true;

        [Min(0f)] public float BeamRangeCells = 8f;
        [Range(1f, 180f)] public float SweepDegrees = 90f;
        [Min(0.01f)] public float SweepPeriodSeconds = 3f;
        [Min(0)] public int Damage = 1;
        public bool AppliesReturnMark = true;
        public bool MirrorCanReflect = true;
        public bool UmbrellaCanReflect;
        public bool SignalChangesDirection = true;

        public Vector2 ReturnFieldSizeCells = new Vector2(4f, 2f);
        [Min(0f)] public float ReturnDelaySeconds = 0.5f;
        public string DestinationAnchorId = "EntryAnchor";
        public bool RequiresEntryAnchor = true;

        public string MassTag = "Heavy";
        [Min(1f)] public float Mass = 2f;
        public Vector2Int GravityDirection = Vector2Int.down;
        [Min(0)] public int CrushDamage = 1;
        [Range(1, 2)] public int PressureWeight = 2;
        public bool HeavyCarryAllowed = true;
        public bool HookPullAllowed = true;

        [Range(0.05f, 1f)] public float LowGravityScale = 0.45f;
        [Min(0.05f)] public float NormalGravityScale = 1f;
        public bool StartsLowGravity;
        [Min(1)] public int MaxInstancesPerRoom = 1;

        public List<string> NodeGuids = new List<string> { "POLARIS_NODE_A", "POLARIS_NODE_B" };
        [Min(1)] public int BridgeCellCount = 6;
        public bool StartsBridgeActive;

        public List<int> RhythmPattern = new List<int> { 0, 1, 0, 2 };
        public string MemoryId = "memory.narae.bell";
        [Min(0)] public int InteractionClearanceCells = 3;

        public bool IgnoreAllTools;
        public string VisualVariant = "PolarisImmutable";
    }

    [Serializable]
    public sealed class MaruElementRuntimeProfile
    {
        public MaruElementKind Kind;
        [Min(1)] public int DurabilityStages = 1;
        [Min(0)] public int RewardMoney;
        public string RewardId;
        public string PreviewRewardText;
        public string PreviewPenaltyText;
        [Min(0f)] public float ScheduledEntryDelaySeconds;
        [Min(1f)] public float TimerRateMultiplier = 1f;
        [Min(0f)] public float GuidanceSeconds;
        [Min(0f)] public float ShortenNextBellSeconds;
        public MaruMarkerCostType MarkerCostType = MaruMarkerCostType.Money;
        [Min(0)] public int MarkerCostValue;
        [Range(0f, 1f)] public float NoiseLevel;
        [Range(1, 2)] public int PressureWeight = 1;
        [Min(0)] public int MinimumExitRoomDistance;
        [Min(0)] public int MaximumExitRoomDistance;
        [Min(0)] public int MinimumAutomaticHazardDistanceCells;
        public bool ForbidExitRoom;
        public MaruRecordGuideEffect RecordGuideEffect = MaruRecordGuideEffect.ExitDirection;
    }

    [Serializable]
    public sealed class ElementVisualProfile
    {
        public ElementVisualRenderMode RenderMode;
        public Sprite SingleSprite;
        public Sprite SegmentStart;
        public Sprite SegmentMiddle;
        public Sprite SegmentEnd;
        public GameObject AnimatorPrefab;
        public Vector2 VisualSizeCells = Vector2.one;
        public Vector2 VisualOffsetCells;
        public string SortingLayerName = "Default";
        public int SortingOrder;
        public Material MaterialOverride;
        public Color Tint = Color.white;
        public bool FlipX;
        public bool FlipY;
    }

    [Serializable]
    public sealed class SerializedColliderShape
    {
        public SerializedColliderShapeType ShapeType = SerializedColliderShapeType.Box;
        public Vector2 OffsetCells;
        public Vector2 SizeCells = Vector2.one;
        public float RadiusCells = 0.5f;
        public List<Vector2> Points = new List<Vector2>();
    }

    [Serializable]
    public sealed class ElementCollisionProfile
    {
        public ColliderAuthoringMode Mode = ColliderAuthoringMode.MergedBoxes;
        public bool IsSolid;
        public bool IsOneWay;
        public bool IsTriggerOnly;
        public PhysicsMaterial2D PhysicsMaterial;
        public List<SerializedColliderShape> SolidShapes = new List<SerializedColliderShape>();
        public List<SerializedColliderShape> TriggerShapes = new List<SerializedColliderShape>();
        public LayerMask CollisionMask;
    }

    [Serializable]
    public sealed class ElementPathDefinition
    {
        public List<Vector2> Nodes = new List<Vector2>();
        public bool ClosedLoop;
        public float SpeedCellsPerSecond = 1f;
        public float WaitSeconds;
        public ElementPathEasing Easing = ElementPathEasing.Linear;
        public bool PingPong;
        public bool StartForward = true;
        public bool ResetOnRoomReenter;
    }

    [Serializable]
    public sealed class ProjectilePatternDefinition
    {
        public GameObject ProjectilePrefab;
        public int ProjectileCount = 1;
        public float IntervalSeconds;
        public float SpeedCellsPerSecond = 1f;
        public Vector2 Direction = Vector2.right;
        public bool AimAtPlayer;
    }

    [Serializable]
    public sealed class ElementBehaviorProfile
    {
        public MapElementState InitialState = MapElementState.Idle;
        public float WarningSeconds;
        public float ActiveSeconds;
        public float CooldownSeconds;
        public bool ResetOnRoomReenter;
        public bool PersistBrokenState = true;
        public bool PauseWhenRoomInactive = true;
        public ElementPathDefinition Path = new ElementPathDefinition();
        public ProjectilePatternDefinition ProjectilePattern = new ProjectilePatternDefinition();
    }

    [Serializable]
    public sealed class ElementPlacementProfile
    {
        public SurfaceRequirement Surface;
        public int MinimumPortalDistanceCells;
        public int MinimumSafeCellDistanceCells;
        public bool AllowMainRoute = true;
        public bool AllowBranchRoute = true;
        public bool AllowSecretRoute = true;
        public bool AllowMirrorX;
        public bool AllowMirrorY;
        public int MaxPerRoom;
        public List<string> ForbiddenNeighborTags = new List<string>();
        public List<string> RequiredNeighborTags = new List<string>();
    }

    [Serializable]
    public sealed class ElementBudgetProfile
    {
        [Range(0, 10)] public int ThreatCost;
        [Range(0, 10)] public int UtilityValue;
        [Range(0, 5)] public int CognitiveCost;
        [Range(0, 5)] public int MotionCost;
    }

    [Serializable]
    public sealed class ToolReactionTable
    {
        public List<ToolReactionEntry> Entries = new List<ToolReactionEntry>();

        public bool TryResolve(
            ToolTag contextTags,
            out ToolReactionEntry entry,
            out ToolTag matchedTool)
        {
            entry = null;
            matchedTool = ToolTag.None;
            if (contextTags == ToolTag.None || Entries == null)
            {
                return false;
            }

            for (var index = 0; index < Entries.Count; index++)
            {
                var candidate = Entries[index];
                if (candidate == null || candidate.Reaction == ElementReactionType.None ||
                    candidate.Tool != contextTags)
                {
                    continue;
                }

                entry = candidate;
                matchedTool = ToolReactionMatrix.FirstAtomicTag(candidate.Tool & contextTags);
                return matchedTool != ToolTag.None;
            }

            for (var index = 0; index < Entries.Count; index++)
            {
                var candidate = Entries[index];
                if (candidate == null || candidate.Reaction == ElementReactionType.None ||
                    (candidate.Tool & contextTags) == 0)
                {
                    continue;
                }

                entry = candidate;
                matchedTool = ToolReactionMatrix.FirstAtomicTag(candidate.Tool & contextTags);
                return matchedTool != ToolTag.None;
            }

            return false;
        }
    }

    [Serializable]
    public sealed class ToolReactionEntry
    {
        public ToolTag Tool;
        public ElementReactionType Reaction;
        public int StrengthRequired;
        public string ResultState;
        public GameObject ReactionVfx;
    }

    public static class ToolReactionMatrix
    {
        public static readonly ToolTag[] AtomicTools =
        {
            ToolTag.Bomb,
            ToolTag.Pickaxe,
            ToolTag.Shovel,
            ToolTag.Water,
            ToolTag.Pound,
            ToolTag.Hook,
            ToolTag.Rope,
            ToolTag.WindGuard,
            ToolTag.LightImpact,
            ToolTag.HeavyImpact,
            ToolTag.Fire,
            ToolTag.Projectile,
            ToolTag.Context,
            ToolTag.Cut,
        };

        public const ToolTag KnownToolMask =
            ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Shovel | ToolTag.Water |
            ToolTag.Pound | ToolTag.Hook | ToolTag.Rope | ToolTag.WindGuard |
            ToolTag.LightImpact | ToolTag.HeavyImpact | ToolTag.Fire |
            ToolTag.Projectile | ToolTag.Context | ToolTag.Cut;

        public static ToolTag FirstAtomicTag(ToolTag tags)
        {
            for (var index = 0; index < AtomicTools.Length; index++)
            {
                if ((tags & AtomicTools[index]) != 0)
                {
                    return AtomicTools[index];
                }
            }

            return ToolTag.None;
        }
    }

    [Serializable]
    public sealed class MaruReactionProfile
    {
        public bool IsTarget;
        public ElementReactionType Reaction;
        public string ResultState;
        public float DelaySeconds;
    }

    [Serializable]
    public sealed class ElementBakeMetadata
    {
        public int SchemaVersion = 1;
        public string SourceGuid;
        public string SourceHash;
        public string RuntimePrefabGuid;
        public string LastBakedUnityVersion;
        public string LastBakedAtUtc;
    }
}

#endif
