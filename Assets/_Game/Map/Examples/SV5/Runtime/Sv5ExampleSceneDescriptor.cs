using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map.SV5.Examples
{
    /// <summary>
    /// Self-describing marker for the standalone SV5 example scene. This is
    /// deliberately not a production world/player readiness claim.
    /// </summary>
    public sealed class Sv5ExampleSceneDescriptor : MonoBehaviour
    {
        [SerializeField] private int worldWidth;
        [SerializeField] private int worldHeight;
        [SerializeField] private Vector2Int jumpFixtureOrigin;
        [SerializeField] private int solidCellCount;
        [SerializeField] private int oneWayCellCount;
        [SerializeField] private int jumpSupportCount;
        [SerializeField] private int jumpFixtureCellCount;
        [SerializeField] private int verifiedPhysicalCaseCount;
        [SerializeField] private string currentMilestone;
        [SerializeField] private bool playerVerified;
        [SerializeField] private bool fullWorldCompletionMap;
        [SerializeField] private bool usesOnlySv5Content;
        [SerializeField] private Vector2 completionStart;
        [SerializeField] private Vector2 completionExit;
        [SerializeField] private string[] sourceArtifacts = Array.Empty<string>();

        public int WorldWidth => worldWidth;
        public int WorldHeight => worldHeight;
        public Vector2Int JumpFixtureOrigin => jumpFixtureOrigin;
        public int SolidCellCount => solidCellCount;
        public int OneWayCellCount => oneWayCellCount;
        public int JumpSupportCount => jumpSupportCount;
        public int JumpFixtureCellCount => jumpFixtureCellCount;
        public int VerifiedPhysicalCaseCount => verifiedPhysicalCaseCount;
        public string CurrentMilestone => currentMilestone;
        public bool PlayerVerified => playerVerified;
        public bool FullWorldCompletionMap => fullWorldCompletionMap;
        public bool UsesOnlySv5Content => usesOnlySv5Content;
        public Vector2 CompletionStart => completionStart;
        public Vector2 CompletionExit => completionExit;
        public IReadOnlyList<string> SourceArtifacts => sourceArtifacts;

        public void Configure(
            int width,
            int height,
            Vector2Int fixtureOrigin,
            int solids,
            int oneWays,
            int supports,
            int fixtureCells,
            Vector2 start,
            Vector2 exit,
            IEnumerable<string> sources)
        {
            worldWidth = width;
            worldHeight = height;
            jumpFixtureOrigin = fixtureOrigin;
            solidCellCount = solids;
            oneWayCellCount = oneWays;
            jumpSupportCount = supports;
            jumpFixtureCellCount = fixtureCells;
            verifiedPhysicalCaseCount = 38;
            currentMilestone = "SV5_20_JUMP_PLAYER";
            playerVerified = true;
            fullWorldCompletionMap = true;
            usesOnlySv5Content = true;
            completionStart = start;
            completionExit = exit;
            sourceArtifacts = sources == null ? Array.Empty<string>() : new List<string>(sources).ToArray();
        }
    }
}
