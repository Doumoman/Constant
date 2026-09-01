using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum WorldDependencyKind
    {
        SpecialReservation,
        MandatoryRoute,
        BoundaryPair,
        ExternalSocket,
        NeighborContinuity,
        PacingWindow,
        RetryGuard,
    }

    public enum WorldSolvePriority
    {
        FixedSpecial = 0,
        MandatoryRouteOrBoundary = 10,
        ExternalSocket = 20,
        PacingConstraint = 30,
        OrdinaryTerrain = 40,
    }

    public enum WorldSolveAbortReason
    {
        None,
        SectorLocalAttemptsExhausted,
        DependencyRollbackLimitReached,
        InvalidWorldPlan,
    }

    public enum WorldSolveFailureCode
    {
        MissingInput,
        SectorCountMismatch,
        DuplicateSectorId,
        DuplicateCoordinate,
        SectorIdOutOfRange,
        CoordinateOutOfBounds,
        SectorIdCoordinateMismatch,
        InvalidNodeFact,
        MissingMap14Handoff,
        SelfDependency,
        MissingDependencySector,
        DuplicateDependency,
        MissingRequiredDependency,
        CycleDetected,
        InvalidRetryEnvelope,
        WholeWorldRerandomRequired,
        MutationClaim,
    }

    public readonly struct WorldSectorId : IEquatable<WorldSectorId>, IComparable<WorldSectorId>
    {
        public WorldSectorId(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public int CompareTo(WorldSectorId other) => Value.CompareTo(other.Value);
        public bool Equals(WorldSectorId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is WorldSectorId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => "SECTOR_" + Value.ToString("D3", CultureInfo.InvariantCulture);
        public static bool operator ==(WorldSectorId left, WorldSectorId right) => left.Equals(right);
        public static bool operator !=(WorldSectorId left, WorldSectorId right) => !left.Equals(right);
    }

    public readonly struct WorldSectorCoordinate :
        IEquatable<WorldSectorCoordinate>,
        IComparable<WorldSectorCoordinate>
    {
        public WorldSectorCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < WorldGenConstants.SectorColumns &&
                                  Y >= 0 && Y < WorldGenConstants.SectorRows;
        public WorldSectorId RowMajorId => new WorldSectorId((Y * WorldGenConstants.SectorColumns) + X);

        public int CompareTo(WorldSectorCoordinate other)
        {
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(WorldSectorCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is WorldSectorCoordinate other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0},{1})", X, Y);
        public static bool operator ==(WorldSectorCoordinate left, WorldSectorCoordinate right) => left.Equals(right);
        public static bool operator !=(WorldSectorCoordinate left, WorldSectorCoordinate right) => !left.Equals(right);
    }

    public sealed class WorldSectorNode : IComparable<WorldSectorNode>
    {
        public WorldSectorNode(
            WorldSectorId id,
            WorldSectorCoordinate coordinate,
            string primaryBiome,
            int routeType,
            AccessClass accessClass,
            PacingRole pacingRole,
            bool hasSpecialReservation,
            bool isBoundaryPair,
            bool hasExternalSocketObligation,
            bool isWorldStart = false,
            string specialReservationId = "")
        {
            Id = id;
            Coordinate = coordinate;
            PrimaryBiome = primaryBiome ?? string.Empty;
            RouteType = routeType;
            AccessClass = accessClass;
            PacingRole = pacingRole;
            HasSpecialReservation = hasSpecialReservation;
            IsBoundaryPair = isBoundaryPair;
            HasExternalSocketObligation = hasExternalSocketObligation;
            IsWorldStart = isWorldStart;
            SpecialReservationId = specialReservationId ?? string.Empty;
        }

        public WorldSectorId Id { get; }
        public WorldSectorCoordinate Coordinate { get; }
        public string PrimaryBiome { get; }
        public int RouteType { get; }
        public AccessClass AccessClass { get; }
        public PacingRole PacingRole { get; }
        public bool HasSpecialReservation { get; }
        public bool IsBoundaryPair { get; }
        public bool HasExternalSocketObligation { get; }
        public bool IsWorldStart { get; }
        public string SpecialReservationId { get; }

        public bool IsMandatoryRoute => RouteType >= 1 && RouteType <= 4 &&
                                        AccessClass == AccessClass.MandatoryNoTool;

        public string StableConstraintKey => string.Join("|", new[]
        {
            RouteType.ToString(CultureInfo.InvariantCulture),
            AccessClass.ToString(),
            HasSpecialReservation ? "SPECIAL" : "NO_SPECIAL",
            SpecialReservationId,
            PrimaryBiome,
            PacingRole.ToString(),
        });

        public int CompareTo(WorldSectorNode other) => other == null ? -1 : Id.CompareTo(other.Id);
    }

    public sealed class WorldDependencyEdge : IComparable<WorldDependencyEdge>, IEquatable<WorldDependencyEdge>
    {
        public WorldDependencyEdge(
            WorldSectorId fromSector,
            WorldSectorId toSector,
            WorldDependencyKind kind,
            string reason,
            string sourceOwner)
        {
            FromSector = fromSector;
            ToSector = toSector;
            Kind = kind;
            Reason = reason ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public WorldSectorId FromSector { get; }
        public WorldSectorId ToSector { get; }
        public WorldDependencyKind Kind { get; }
        public string Reason { get; }
        public string SourceOwner { get; }

        public int CompareTo(WorldDependencyEdge other)
        {
            if (other == null) return -1;
            var comparison = FromSector.CompareTo(other.FromSector);
            if (comparison != 0) return comparison;
            comparison = ToSector.CompareTo(other.ToSector);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Reason, other.Reason, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(SourceOwner, other.SourceOwner, StringComparison.Ordinal);
        }

        public bool Equals(WorldDependencyEdge other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as WorldDependencyEdge);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => string.Join("|", new[]
        {
            FromSector.ToString(), ToSector.ToString(), Kind.ToString(), Reason, SourceOwner,
        });
    }

    public sealed class WorldRetryEnvelope
    {
        public WorldRetryEnvelope(
            int maxSectorLocalAttemptsPerNode,
            int dependencyRollbackRadius,
            WorldSolveAbortReason abortReason,
            bool requiresWholeWorldRerandom = false,
            int newRngDrawCount = 0,
            int fallbackCarveCount = 0)
        {
            MaxSectorLocalAttemptsPerNode = maxSectorLocalAttemptsPerNode;
            DependencyRollbackRadius = dependencyRollbackRadius;
            AbortReason = abortReason;
            RequiresWholeWorldRerandom = requiresWholeWorldRerandom;
            NewRngDrawCount = newRngDrawCount;
            FallbackCarveCount = fallbackCarveCount;
        }

        public int MaxSectorLocalAttemptsPerNode { get; }
        public int DependencyRollbackRadius { get; }
        public WorldSolveAbortReason AbortReason { get; }
        public bool RequiresWholeWorldRerandom { get; }
        public int NewRngDrawCount { get; }
        public int FallbackCarveCount { get; }
    }

    public sealed class WorldPlanInput
    {
        private readonly ReadOnlyCollection<WorldSectorNode> nodes;
        private readonly ReadOnlyCollection<WorldDependencyEdge> dependencies;

        public WorldPlanInput(
            IEnumerable<WorldSectorNode> sourceNodes,
            IEnumerable<WorldDependencyEdge> sourceDependencies,
            WorldRetryEnvelope retryEnvelope,
            string map14PhaseExitDigest,
            string publicationLabel,
            int generatedFileWriteCount = 0,
            int tilemapMutationCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int gameplaySpawnCount = 0,
            int sectorPlannerMutationCount = 0)
        {
            nodes = new ReadOnlyCollection<WorldSectorNode>((sourceNodes ?? Array.Empty<WorldSectorNode>())
                .Where(value => value != null)
                .OrderBy(value => value)
                .ToArray());
            dependencies = new ReadOnlyCollection<WorldDependencyEdge>((sourceDependencies ?? Array.Empty<WorldDependencyEdge>())
                .Where(value => value != null)
                .OrderBy(value => value)
                .ToArray());
            RetryEnvelope = retryEnvelope;
            Map14PhaseExitDigest = map14PhaseExitDigest ?? string.Empty;
            PublicationLabel = publicationLabel ?? string.Empty;
            GeneratedFileWriteCount = generatedFileWriteCount;
            TilemapMutationCount = tilemapMutationCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            GameplaySpawnCount = gameplaySpawnCount;
            SectorPlannerMutationCount = sectorPlannerMutationCount;
            CanonicalDigest = WorldSolveDigest.ComputeInput(this);
        }

        public const int WorldWidthTiles = WorldGenConstants.WorldWidthTiles;
        public const int WorldHeightTiles = WorldGenConstants.WorldHeightTiles;
        public const int SectorWidthTiles = WorldGenConstants.SectorWidthTiles;
        public const int SectorHeightTiles = WorldGenConstants.SectorHeightTiles;
        public const int SectorColumns = WorldGenConstants.SectorColumns;
        public const int SectorRows = WorldGenConstants.SectorRows;
        public const int SectorCount = WorldGenConstants.SectorCount;

        public IReadOnlyList<WorldSectorNode> Nodes => nodes;
        public IReadOnlyList<WorldDependencyEdge> Dependencies => dependencies;
        public WorldRetryEnvelope RetryEnvelope { get; }
        public string Map14PhaseExitDigest { get; }
        public string PublicationLabel { get; }
        public int GeneratedFileWriteCount { get; }
        public int TilemapMutationCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int GameplaySpawnCount { get; }
        public int SectorPlannerMutationCount { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class WorldSolveStep
    {
        private readonly ReadOnlyCollection<WorldSectorId> prerequisites;

        public WorldSolveStep(
            int stepIndex,
            WorldSectorId sectorId,
            WorldSolvePriority priority,
            IEnumerable<WorldSectorId> sourcePrerequisites,
            string reasonDigest)
        {
            StepIndex = stepIndex;
            SectorId = sectorId;
            Priority = priority;
            prerequisites = new ReadOnlyCollection<WorldSectorId>((sourcePrerequisites ?? Array.Empty<WorldSectorId>())
                .Distinct()
                .OrderBy(value => value)
                .ToArray());
            ReasonDigest = reasonDigest ?? string.Empty;
        }

        public int StepIndex { get; }
        public WorldSectorId SectorId { get; }
        public WorldSolvePriority Priority { get; }
        public IReadOnlyList<WorldSectorId> PrerequisiteSectorIds => prerequisites;
        public string ReasonDigest { get; }
    }

    public sealed class WorldSolveFailure : IComparable<WorldSolveFailure>, IEquatable<WorldSolveFailure>
    {
        public WorldSolveFailure(WorldSolveFailureCode code, string subject, string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public WorldSolveFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }

        public int CompareTo(WorldSolveFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(WorldSolveFailure other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as WorldSolveFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Reason;
    }

    public sealed class WorldSolveOrderResult
    {
        private readonly ReadOnlyCollection<WorldSolveStep> steps;
        private readonly ReadOnlyCollection<WorldSolveFailure> failures;

        private WorldSolveOrderResult(
            WorldPlanInput input,
            IEnumerable<WorldSolveStep> sourceSteps,
            IEnumerable<WorldSolveFailure> sourceFailures,
            string outputDigest)
        {
            Input = input;
            steps = new ReadOnlyCollection<WorldSolveStep>((sourceSteps ?? Array.Empty<WorldSolveStep>()).ToArray());
            failures = new ReadOnlyCollection<WorldSolveFailure>((sourceFailures ?? Array.Empty<WorldSolveFailure>())
                .Distinct()
                .OrderBy(value => value)
                .ToArray());
            OutputDigest = outputDigest ?? string.Empty;
        }

        public bool Success => Input != null && failures.Count == 0 && steps.Count == WorldPlanInput.SectorCount;
        public WorldPlanInput Input { get; }
        public IReadOnlyList<WorldSolveStep> Steps => steps;
        public IReadOnlyList<WorldSolveFailure> Failures => failures;
        public string InputDigest => Input == null ? string.Empty : Input.CanonicalDigest;
        public string OutputDigest { get; }
        public WorldRetryEnvelope RetryEnvelope => Input == null ? null : Input.RetryEnvelope;
        public int NewRngDrawCount => RetryEnvelope == null ? 0 : RetryEnvelope.NewRngDrawCount;
        public bool WholeWorldRerandom => RetryEnvelope != null && RetryEnvelope.RequiresWholeWorldRerandom;
        public int FallbackCarveCount => RetryEnvelope == null ? 0 : RetryEnvelope.FallbackCarveCount;

        internal static WorldSolveOrderResult Pass(
            WorldPlanInput input,
            IEnumerable<WorldSolveStep> sourceSteps,
            string outputDigest) =>
            new WorldSolveOrderResult(input, sourceSteps, Array.Empty<WorldSolveFailure>(), outputDigest);

        internal static WorldSolveOrderResult Fail(IEnumerable<WorldSolveFailure> sourceFailures) =>
            new WorldSolveOrderResult(null, Array.Empty<WorldSolveStep>(), sourceFailures, string.Empty);
    }

    public static class WorldSolveDigest
    {
        public static string ComputeInput(WorldPlanInput input)
        {
            if (input == null) return string.Empty;
            var lines = new List<string>
            {
                "WORLD|" + WorldPlanInput.WorldWidthTiles + "|" + WorldPlanInput.WorldHeightTiles,
                "SECTOR|" + WorldPlanInput.SectorWidthTiles + "|" + WorldPlanInput.SectorHeightTiles + "|" +
                WorldPlanInput.SectorColumns + "|" + WorldPlanInput.SectorRows + "|" + WorldPlanInput.SectorCount,
                "MAP14|" + Token(input.Map14PhaseExitDigest),
                "PUBLICATION|" + Token(input.PublicationLabel),
                Retry(input.RetryEnvelope),
                string.Join("|", new[]
                {
                    "MUTATION",
                    Number(input.GeneratedFileWriteCount), Number(input.TilemapMutationCount),
                    Number(input.SceneMutationCount), Number(input.PrefabMutationCount),
                    Number(input.GameObjectMutationCount), Number(input.GameplaySpawnCount),
                    Number(input.SectorPlannerMutationCount),
                }),
            };
            lines.AddRange(input.Nodes.OrderBy(value => value).Select(Node));
            lines.AddRange(input.Dependencies.OrderBy(value => value).Select(Edge));
            return Hash(string.Join("\n", lines));
        }

        public static string ComputeReason(WorldSectorNode node, IEnumerable<WorldDependencyEdge> incoming)
        {
            var lines = new List<string> { Node(node) };
            lines.AddRange((incoming ?? Array.Empty<WorldDependencyEdge>()).OrderBy(value => value).Select(Edge));
            return Hash(string.Join("\n", lines));
        }

        public static string ComputeOutput(
            WorldPlanInput input,
            IEnumerable<WorldSolveStep> sourceSteps)
        {
            var lines = new List<string> { "INPUT|" + (input == null ? string.Empty : input.CanonicalDigest) };
            if (input != null) lines.AddRange(input.Dependencies.OrderBy(value => value).Select(Edge));
            lines.AddRange((sourceSteps ?? Array.Empty<WorldSolveStep>())
                .OrderBy(value => value.StepIndex)
                .Select(Step));
            return Hash(string.Join("\n", lines));
        }

        public static bool IsLowerHexSha256(string value)
        {
            if (value == null || value.Length != 64) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var item = value[index];
                if (!((item >= '0' && item <= '9') || (item >= 'a' && item <= 'f'))) return false;
            }
            return true;
        }

        private static string Node(WorldSectorNode value)
        {
            if (value == null) return "NODE|null";
            return string.Join("|", new[]
            {
                "NODE", Number(value.Id.Value), Number(value.Coordinate.X), Number(value.Coordinate.Y),
                Token(value.PrimaryBiome), Number(value.RouteType), value.AccessClass.ToString(),
                value.PacingRole.ToString(), Bool(value.HasSpecialReservation), Bool(value.IsBoundaryPair),
                Bool(value.HasExternalSocketObligation), Bool(value.IsWorldStart), Token(value.SpecialReservationId),
            });
        }

        private static string Edge(WorldDependencyEdge value)
        {
            if (value == null) return "EDGE|null";
            return string.Join("|", new[]
            {
                "EDGE", Number(value.FromSector.Value), Number(value.ToSector.Value), value.Kind.ToString(),
                Token(value.Reason), Token(value.SourceOwner),
            });
        }

        private static string Step(WorldSolveStep value)
        {
            return string.Join("|", new[]
            {
                "STEP", Number(value.StepIndex), Number(value.SectorId.Value), value.Priority.ToString(),
                string.Join(",", value.PrerequisiteSectorIds.Select(item => Number(item.Value))), value.ReasonDigest,
            });
        }

        private static string Retry(WorldRetryEnvelope value)
        {
            return value == null
                ? "RETRY|null"
                : string.Join("|", new[]
                {
                    "RETRY", Number(value.MaxSectorLocalAttemptsPerNode), Number(value.DependencyRollbackRadius),
                    value.AbortReason.ToString(), Bool(value.RequiresWholeWorldRerandom),
                    Number(value.NewRngDrawCount), Number(value.FallbackCarveCount),
                });
        }

        private static string Token(string value)
        {
            var normalized = value ?? string.Empty;
            return normalized.Length.ToString(CultureInfo.InvariantCulture) + ":" + normalized;
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "1" : "0";

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
