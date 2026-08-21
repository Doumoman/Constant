#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using UnityEngine;

namespace StarNight.Tools.Bomb
{
    public enum BombLaunchKind
    {
        Place,
        Horizontal,
        Upward,
        FacingLow,
    }

    public readonly struct BombLaunchSolution
    {
        public BombLaunchSolution(BombLaunchKind kind, int horizontalSign, Vector2 velocity)
        {
            Kind = kind;
            HorizontalSign = horizontalSign < 0 ? -1 : 1;
            Velocity = velocity;
        }

        public BombLaunchKind Kind { get; }
        public int HorizontalSign { get; }
        public Vector2 Velocity { get; }
    }

    [CreateAssetMenu(menuName = "Game/Tools/Bomb Definition")]
    public sealed class BombDefinition : ScriptableObject
    {
        public const int ApprovedStartingCount = 4;
        public const float ApprovedFuseSeconds = 2.40f;
        public const float ApprovedLastWarningSeconds = 0.60f;
        public const float ApprovedChainFuseSeconds = 0.15f;
        public const float ApprovedPickupMinimumFuseSeconds = 0.45f;
        public const int ApprovedEntityDamage = 1;
        public const float ApprovedKnockbackCellsPerSecond = 7f;
        public const float ApprovedResidualSeconds = 3f;

        [SerializeField, Min(0)] private int startingCount = ApprovedStartingCount;
        [SerializeField, Min(0.01f)] private float fuseSeconds = ApprovedFuseSeconds;
        [SerializeField, Min(0f)] private float lastWarningSeconds = ApprovedLastWarningSeconds;
        [SerializeField, Min(0.01f)] private float chainFuseSeconds = ApprovedChainFuseSeconds;
        [SerializeField, Min(0f)] private float pickupMinimumFuseSeconds = ApprovedPickupMinimumFuseSeconds;
        [SerializeField, Range(1, 1)] private int entityDamage = ApprovedEntityDamage;
        [SerializeField, Min(0f)] private float knockbackCellsPerSecond = ApprovedKnockbackCellsPerSecond;
        [SerializeField, Min(0.01f)] private float residualSimulationSeconds = ApprovedResidualSeconds;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;

        public int StartingCount => startingCount;
        public float FuseSeconds => fuseSeconds;
        public float LastWarningSeconds => lastWarningSeconds;
        public float ChainFuseSeconds => chainFuseSeconds;
        public float PickupMinimumFuseSeconds => pickupMinimumFuseSeconds;
        public int EntityDamage => entityDamage;
        public float KnockbackCellsPerSecond => knockbackCellsPerSecond;
        public float ResidualSimulationSeconds => residualSimulationSeconds;
        public float CellSize => cellSize;

        public BombLaunchSolution ResolveLaunch(PlayerActionContext context, int facingSign)
        {
            int facing = facingSign < 0 ? -1 : 1;
            if (context.DownHeld || context.LookVertical < -0.5f)
            {
                return new BombLaunchSolution(BombLaunchKind.Place, facing, Vector2.zero);
            }

            if (context.LookVertical > 0.5f)
            {
                return new BombLaunchSolution(
                    BombLaunchKind.Upward,
                    facing,
                    new Vector2(facing * 1.5f, 6.5f));
            }

            if (Mathf.Abs(context.MoveHorizontal) > 0.5f)
            {
                int sign = context.MoveHorizontal < 0f ? -1 : 1;
                return new BombLaunchSolution(
                    BombLaunchKind.Horizontal,
                    sign,
                    new Vector2(sign * 5.2f, 1.8f));
            }

            return new BombLaunchSolution(
                BombLaunchKind.FacingLow,
                facing,
                new Vector2(facing * 5.2f, 1.8f));
        }

        public void ConfigureForTests(float configuredFuseSeconds = ApprovedFuseSeconds)
        {
            fuseSeconds = Mathf.Max(0.01f, configuredFuseSeconds);
        }

        private void OnValidate()
        {
            startingCount = Mathf.Max(0, startingCount);
            fuseSeconds = Mathf.Max(0.01f, fuseSeconds);
            lastWarningSeconds = Mathf.Clamp(lastWarningSeconds, 0f, fuseSeconds);
            chainFuseSeconds = Mathf.Max(0.01f, chainFuseSeconds);
            pickupMinimumFuseSeconds = Mathf.Max(0f, pickupMinimumFuseSeconds);
            entityDamage = 1;
            knockbackCellsPerSecond = Mathf.Max(0f, knockbackCellsPerSecond);
            residualSimulationSeconds = Mathf.Max(0.01f, residualSimulationSeconds);
            cellSize = Mathf.Max(0.01f, cellSize);
        }
    }
}

#endif
