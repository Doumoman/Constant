#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Interaction.Carry
{
    public enum CarryWeightClass
    {
        Light,
        Medium,
        Heavy,
        Fixed,
    }

    public enum PrimaryUseMode
    {
        Throw,
        Activate,
        Consume,
        ContextualOnly,
    }

    public enum HookResponse
    {
        PullToPlayer,
        PullPlayerToTarget,
        Trigger,
        Reject,
    }

    [System.Serializable]
    public sealed class CarryThrowProfile
    {
        public Vector2 HorizontalVelocity = new Vector2(6.5f, 2f);
        public Vector2 UpVelocity = new Vector2(2.5f, 7f);
        [Min(0.01f)] public float MaximumSpeed = 10f;
    }

    [CreateAssetMenu(menuName = "Game/Interaction/Carry Object Definition")]
    public sealed class CarryObjectDefinition : ScriptableObject
    {
        [SerializeField] private string objectId;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private CarryWeightClass weightClass = CarryWeightClass.Light;
        [SerializeField] private PrimaryUseMode primaryUseMode = PrimaryUseMode.Throw;
        [SerializeField] private bool criticalCarry;
        [SerializeField] private bool forceHeavyImpact;
        [SerializeField] private Vector2Int pivotCell;
        [SerializeField] private Vector2 heldVisualOffset;
        [SerializeField] private CarryThrowProfile throwProfile = new CarryThrowProfile();
        [SerializeField] private HookResponse hookResponse = HookResponse.PullToPlayer;
        [SerializeField, Range(0, 2)] private int plateWeight = 1;
        [SerializeField] private GameObject impactVfx;
        [SerializeField] private AudioClip impactSfx;

        public string ObjectId => objectId ?? string.Empty;
        public Vector2Int Footprint => new Vector2Int(1, Mathf.Clamp(footprint.y, 1, 2));
        public CarryWeightClass WeightClass => weightClass;
        public PrimaryUseMode PrimaryUseMode => primaryUseMode;
        public bool CriticalCarry => criticalCarry;
        public bool ForceHeavyImpact => forceHeavyImpact;
        public Vector2Int PivotCell => pivotCell;
        public Vector2 HeldVisualOffset => heldVisualOffset;
        public CarryThrowProfile ThrowProfile => throwProfile;
        public HookResponse HookResponse => hookResponse;
        public GameObject ImpactVfx => impactVfx;
        public AudioClip ImpactSfx => impactSfx;
        public bool CanHandCarry => weightClass != CarryWeightClass.Fixed;
        public bool CanClimbRope => weightClass == CarryWeightClass.Light || weightClass == CarryWeightClass.Medium;
        public float Mass => GetMass(weightClass);
        public float MovementMultiplier => GetMovementMultiplier(weightClass);
        public float JumpHeightMultiplier => GetJumpHeightMultiplier(weightClass);
        public int PlateWeight => plateWeight;

        private void OnValidate()
        {
            footprint = new Vector2Int(1, Mathf.Clamp(footprint.y, 1, 2));
            pivotCell = new Vector2Int(0, Mathf.Clamp(pivotCell.y, 0, footprint.y - 1));
            plateWeight = Mathf.Clamp(plateWeight, 0, 2);
            throwProfile ??= new CarryThrowProfile();
        }

        public void ConfigureForTests(
            string id,
            CarryWeightClass weight,
            Vector2Int size,
            PrimaryUseMode useMode = PrimaryUseMode.Throw,
            bool isCritical = false,
            bool forceHeavy = false,
            HookResponse configuredHookResponse = HookResponse.PullToPlayer)
        {
            objectId = id;
            weightClass = weight;
            footprint = new Vector2Int(1, Mathf.Clamp(size.y, 1, 2));
            primaryUseMode = useMode;
            criticalCarry = isCritical;
            forceHeavyImpact = forceHeavy;
            hookResponse = configuredHookResponse;
            plateWeight = weight == CarryWeightClass.Heavy ? 2 : weight == CarryWeightClass.Fixed ? 0 : 1;
            throwProfile = CreateApprovedThrowProfile(weight);
        }

        private static CarryThrowProfile CreateApprovedThrowProfile(CarryWeightClass weight)
        {
            return weight switch
            {
                CarryWeightClass.Light => new CarryThrowProfile
                {
                    HorizontalVelocity = new Vector2(6.5f, 2f),
                    UpVelocity = new Vector2(2.5f, 7f),
                },
                CarryWeightClass.Medium => new CarryThrowProfile
                {
                    HorizontalVelocity = new Vector2(5f, 1.5f),
                    UpVelocity = new Vector2(1.8f, 5.6f),
                },
                CarryWeightClass.Heavy => new CarryThrowProfile
                {
                    HorizontalVelocity = new Vector2(2.7f, 0.5f),
                    UpVelocity = Vector2.zero,
                },
                _ => new CarryThrowProfile
                {
                    HorizontalVelocity = Vector2.zero,
                    UpVelocity = Vector2.zero,
                },
            };
        }

        public static float GetMass(CarryWeightClass weight)
        {
            return weight switch
            {
                CarryWeightClass.Light => 0.5f,
                CarryWeightClass.Medium => 1.5f,
                CarryWeightClass.Heavy => 4.0f,
                _ => 0f,
            };
        }

        public static float GetMovementMultiplier(CarryWeightClass weight)
        {
            return weight switch
            {
                CarryWeightClass.Light => 1.00f,
                CarryWeightClass.Medium => 0.90f,
                CarryWeightClass.Heavy => 0.65f,
                _ => 0f,
            };
        }

        public static float GetJumpHeightMultiplier(CarryWeightClass weight)
        {
            return weight switch
            {
                CarryWeightClass.Light => 1.00f,
                CarryWeightClass.Medium => 0.90f,
                CarryWeightClass.Heavy => 0.67f,
                _ => 0f,
            };
        }
    }
}

#endif
