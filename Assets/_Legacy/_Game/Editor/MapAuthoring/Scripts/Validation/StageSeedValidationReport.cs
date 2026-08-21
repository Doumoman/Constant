#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Stage.Layout;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    [Serializable]
    public sealed class StageSeedFamilyCount
    {
        public LayoutFamily Family;
        public int Count;
    }

    [Serializable]
    public sealed class StageSeedFailureReport
    {
        public int Seed;
        public string RoomNodeStableId;
        public string FailureCode;
        public Vector2Int FirstFailedCell;
        public string InventoryState;
        public string RoomStreamingState;
    }

    public sealed class StageSeedValidationReport : ScriptableObject
    {
        public string StageId;
        public int StartSeed;
        public int SeedCount;
        public int FixedRegressionSeedCount;
        public int RandomSeedCount;
        public int PassedSeedCount;
        public int FailedSeedCount;
        public int TotalRooms;
        public int TotalConnections;
        public int OuterEscapeFailureCount;
        public int FloorGapFailureCount;
        public int PortalGapFailureCount;
        public int MainRouteFailureCount;
        public int MaruRouteFailureCount;
        public int OtherFailureCount;
        public int UniqueValidationHashCount;
        public double DurationMilliseconds;
        public string GeneratedAtUtc;
        public string JsonReportPath;
        public string CsvReportPath;
        public List<StageSeedFamilyCount> FamilyCounts = new List<StageSeedFamilyCount>();
        public List<StageSeedFailureReport> Failures = new List<StageSeedFailureReport>();

        public bool IsValid => FailedSeedCount == 0;

        public string CreateSummary()
        {
            return $"{StageId} · Seeds {SeedCount} (Fixed {FixedRegressionSeedCount} + Random {RandomSeedCount}) · " +
                   $"Pass {PassedSeedCount} · Fail {FailedSeedCount} · " +
                   $"Outer {OuterEscapeFailureCount} / Floor {FloorGapFailureCount} / Portal {PortalGapFailureCount} · " +
                   $"{DurationMilliseconds:0.0} ms";
        }
    }
}

#endif
