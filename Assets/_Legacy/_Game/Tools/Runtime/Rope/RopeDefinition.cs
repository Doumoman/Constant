#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [CreateAssetMenu(menuName = "Game/Tools/Rope Definition")]
    public sealed class RopeDefinition : ScriptableObject
    {
        public const int ApprovedStartingCount = 4;
        public const int ApprovedMaximumScanCells = 6;
        public const int ApprovedMaximumLengthCells = 6;
        public const float ApprovedLaunchCellsPerSecond = 12f;
        public const float ApprovedMaximumInstallSeconds = 0.35f;
        public const float ApprovedClimbCellsPerSecond = 4f;
        public const float ApprovedSwingCells = 0.25f;
        public const float ApprovedFallenSegmentSeconds = 0.5f;

        [SerializeField, Min(0)] private int startingCount = ApprovedStartingCount;
        [SerializeField, Range(2, ApprovedMaximumScanCells)] private int maximumScanCells = ApprovedMaximumScanCells;
        [SerializeField, Range(2, ApprovedMaximumLengthCells)] private int maximumLengthCells = ApprovedMaximumLengthCells;
        [SerializeField, Min(0.01f)] private float launchCellsPerSecond = ApprovedLaunchCellsPerSecond;
        [SerializeField, Min(0.01f)] private float maximumInstallSeconds = ApprovedMaximumInstallSeconds;
        [SerializeField, Min(0.01f)] private float climbCellsPerSecond = ApprovedClimbCellsPerSecond;
        [SerializeField, Range(0f, 0.25f)] private float swingCells = ApprovedSwingCells;
        [SerializeField, Min(0.01f)] private float fallenSegmentSeconds = ApprovedFallenSegmentSeconds;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;

        public int StartingCount => startingCount;
        public int MaximumScanCells => maximumScanCells;
        public int MaximumLengthCells => maximumLengthCells;
        public float LaunchCellsPerSecond => launchCellsPerSecond;
        public float MaximumInstallSeconds => maximumInstallSeconds;
        public float ClimbCellsPerSecond => climbCellsPerSecond;
        public float SwingCells => swingCells;
        public float FallenSegmentSeconds => fallenSegmentSeconds;
        public float CellSize => cellSize;

        private void OnValidate()
        {
            startingCount = Mathf.Max(0, startingCount);
            maximumScanCells = Mathf.Clamp(maximumScanCells, 2, ApprovedMaximumScanCells);
            maximumLengthCells = Mathf.Clamp(maximumLengthCells, 2, ApprovedMaximumLengthCells);
            launchCellsPerSecond = Mathf.Max(0.01f, launchCellsPerSecond);
            maximumInstallSeconds = Mathf.Max(0.01f, maximumInstallSeconds);
            climbCellsPerSecond = Mathf.Max(0.01f, climbCellsPerSecond);
            swingCells = Mathf.Clamp(swingCells, 0f, ApprovedSwingCells);
            fallenSegmentSeconds = Mathf.Max(0.01f, fallenSegmentSeconds);
            cellSize = Mathf.Max(0.01f, cellSize);
        }
    }
}

#endif
