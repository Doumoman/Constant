#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.MapHarness.P11
{
    public enum P11MapCueKind
    {
        MainExit = 0,
        ExitEdge = 1,
        Landmark = 2,
        OptionalReward = 3,
        Folklore = 4,
        Homecoming = 5,
        Archive = 6
    }

    public enum P11MapRouteKind
    {
        Main = 0,
        Optional = 1
    }

    public enum P11MapRegion
    {
        StarPostOffice = 3,
        SunriseGarden = 4,
        PolarisObservatory = 5
    }

    public enum P11MapStageSlot
    {
        X1 = 1,
        X2 = 2,
        X3 = 3
    }

    public enum P11MapStageArchetype
    {
        Traverse = 0,
        Ascent = 1,
        Descent = 2,
        Circuit = 3,
        Fork = 4,
        FixedBoss = 5
    }

    public enum P11MapMainDirection
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3,
        Center = 4
    }

    [Flags]
    public enum P11MapRoomRole
    {
        None = 0,
        Start = 1 << 0,
        Teach = 1 << 1,
        Main = 1 << 2,
        Choice = 1 << 3,
        LoopConnector = 1 << 4,
        Landmark = 1 << 5,
        Utility = 1 << 6,
        LocalEvent = 1 << 7,
        RecordRoom = 1 << 8,
        MaruRoom = 1 << 9,
        Treasure = 1 << 10,
        ShopCorridor = 1 << 11,
        SupplyCorridor = 1 << 12,
        RelicRoom = 1 << 13,
        MercyRoom = 1 << 14,
        Exit = 1 << 15,
        Boss = 1 << 16
    }

    public enum P11MapRoomZone
    {
        EntrySafe = 0,
        Telegraph = 1,
        Action = 2,
        Recovery = 3,
        ExitSafe = 4
    }

    public enum P11MapSocketTraversal
    {
        Walk = 0,
        JumpSafe = 1,
        ReturnableDrop = 2,
        AutoLift = 3,
        ShallowWater = 4,
        RopeOptional = 5,
        HookOptional = 6,
        BreakableOptional = 7,
        DeepWater = 8,
        WindOptional = 9,
        BossEntry = 10,
        ExitOnly = 11
    }

    public enum P11MapValidationSeverity
    {
        Warning = 0,
        Error = 1
    }

    [Serializable]
    public sealed class P11MapRoomZoneDefinition
    {
        [SerializeField] private P11MapRoomZone kind;
        [SerializeField] private Transform anchor;
        [SerializeField, Min(0)] private int safeCellCount;
        [SerializeField] private bool visibleFromEntry;

        public P11MapRoomZone Kind => kind;
        public Transform Anchor => anchor;
        public int SafeCellCount => safeCellCount;
        public bool VisibleFromEntry => visibleFromEntry;

        public void Configure(
            P11MapRoomZone zoneKind,
            Transform zoneAnchor,
            int cells = 0,
            bool visible = true)
        {
            kind = zoneKind;
            anchor = zoneAnchor;
            safeCellCount = Mathf.Max(0, cells);
            visibleFromEntry = visible;
        }
    }

    [Serializable]
    public sealed class P11MapSocketDefinition
    {
        [SerializeField] private string socketId;
        [SerializeField] private Transform anchor;
        [SerializeField] private P11MapRouteKind route;
        [SerializeField] private P11MapSocketTraversal traversal;
        [SerializeField] private bool returnable = true;
        [SerializeField] private bool requiresTool;

        public string SocketId => socketId;
        public Transform Anchor => anchor;
        public P11MapRouteKind Route => route;
        public P11MapSocketTraversal Traversal => traversal;
        public bool Returnable => returnable;
        public bool RequiresTool => requiresTool;
        public bool IsMainRoute => route == P11MapRouteKind.Main;
        public bool IsOptionalRoute =>
            route == P11MapRouteKind.Optional;
        public bool IsToolFreeMainTraversal =>
            !IsMainRoute
            || (!requiresTool
                && P11MapSocketRules.IsMainRouteTraversal(traversal));

        public void Configure(
            string id,
            Transform socketAnchor,
            P11MapRouteKind routeKind,
            P11MapSocketTraversal traversalType,
            bool canReturn = true,
            bool toolRequired = false)
        {
            socketId = id ?? string.Empty;
            anchor = socketAnchor;
            route = routeKind;
            traversal = traversalType;
            returnable = canReturn;
            requiresTool = toolRequired;
        }
    }

    [Serializable]
    public sealed class P11MapValidationIssue
    {
        [SerializeField] private P11MapValidationSeverity severity;
        [SerializeField] private string code;
        [SerializeField] private string context;
        [SerializeField] private string message;

        public P11MapValidationSeverity Severity => severity;
        public string Code => code;
        public string Context => context;
        public string Message => message;

        public P11MapValidationIssue(
            P11MapValidationSeverity issueSeverity,
            string issueCode,
            string issueContext,
            string issueMessage)
        {
            severity = issueSeverity;
            code = issueCode ?? string.Empty;
            context = issueContext ?? string.Empty;
            message = issueMessage ?? string.Empty;
        }

        public override string ToString()
        {
            string location = string.IsNullOrWhiteSpace(context)
                ? string.Empty
                : $" [{context}]";
            return $"{severity}: {code}{location} - {message}";
        }
    }

    [Serializable]
    public sealed class P11MapValidationReport
    {
        [SerializeField]
        private List<P11MapValidationIssue> issues =
            new List<P11MapValidationIssue>();

        public IReadOnlyList<P11MapValidationIssue> Issues => issues;
        public bool Passed => ErrorCount == 0;

        public int ErrorCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < issues.Count; index++)
                {
                    if (issues[index].Severity
                        == P11MapValidationSeverity.Error)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int WarningCount => issues.Count - ErrorCount;

        public void AddError(
            string code,
            string message,
            string context = "")
        {
            Add(
                P11MapValidationSeverity.Error,
                code,
                message,
                context);
        }

        public void AddWarning(
            string code,
            string message,
            string context = "")
        {
            Add(
                P11MapValidationSeverity.Warning,
                code,
                message,
                context);
        }

        public void Add(
            P11MapValidationSeverity severity,
            string code,
            string message,
            string context = "")
        {
            if (string.IsNullOrWhiteSpace(code)
                || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            issues.Add(
                new P11MapValidationIssue(
                    severity,
                    code,
                    context,
                    message));
        }

        public void AddRange(
            P11MapValidationReport other,
            string contextPrefix = "")
        {
            if (other == null)
            {
                return;
            }

            for (int index = 0; index < other.issues.Count; index++)
            {
                P11MapValidationIssue issue = other.issues[index];
                string mergedContext = string.IsNullOrWhiteSpace(
                    contextPrefix)
                    ? issue.Context
                    : string.IsNullOrWhiteSpace(issue.Context)
                        ? contextPrefix
                        : $"{contextPrefix}/{issue.Context}";
                Add(
                    issue.Severity,
                    issue.Code,
                    issue.Message,
                    mergedContext);
            }
        }

        public bool ContainsCode(string issueCode)
        {
            for (int index = 0; index < issues.Count; index++)
            {
                if (string.Equals(
                    issues[index].Code,
                    issueCode,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            if (Passed && issues.Count == 0)
            {
                return "PASS";
            }

            var lines = new List<string>(issues.Count + 1)
            {
                Passed
                    ? $"PASS WITH {WarningCount} WARNING(S)"
                    : $"FAIL ({ErrorCount} ERROR(S))"
            };
            for (int index = 0; index < issues.Count; index++)
            {
                lines.Add(issues[index].ToString());
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    public static class P11MapCueRules
    {
        public static P11MapRouteKind ExpectedRoute(
            P11MapCueKind kind)
        {
            switch (kind)
            {
                case P11MapCueKind.MainExit:
                case P11MapCueKind.ExitEdge:
                case P11MapCueKind.Landmark:
                    return P11MapRouteKind.Main;
                default:
                    return P11MapRouteKind.Optional;
            }
        }

        public static bool MatchesRouteContract(
            P11MapCueKind kind,
            P11MapRouteKind route)
        {
            return ExpectedRoute(kind) == route;
        }
    }

    public static class P11MapMacroSizeRules
    {
        public static bool IsAllowed(Vector2Int size)
        {
            return size == new Vector2Int(1, 1)
                || size == new Vector2Int(2, 1)
                || size == new Vector2Int(1, 2)
                || size == new Vector2Int(2, 2)
                || size == new Vector2Int(3, 1)
                || size == new Vector2Int(1, 3);
        }

        public static bool IsLargeOrSpecial(Vector2Int size)
        {
            return size == new Vector2Int(2, 2)
                || size == new Vector2Int(3, 1)
                || size == new Vector2Int(1, 3);
        }

        public static bool IsX1Allowed(Vector2Int size)
        {
            return IsAllowed(size)
                && size != new Vector2Int(3, 1)
                && size != new Vector2Int(1, 3);
        }
    }

    public static class P11MapSocketRules
    {
        public static bool IsMainRouteTraversal(
            P11MapSocketTraversal traversal)
        {
            return traversal == P11MapSocketTraversal.Walk
                || traversal == P11MapSocketTraversal.JumpSafe
                || traversal == P11MapSocketTraversal.ReturnableDrop
                || traversal == P11MapSocketTraversal.AutoLift
                || traversal == P11MapSocketTraversal.ShallowWater
                || traversal == P11MapSocketTraversal.BossEntry
                || traversal == P11MapSocketTraversal.ExitOnly;
        }
    }
}

#endif
