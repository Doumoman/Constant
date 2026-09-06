using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public enum GeneratedValidationJumpTargetKind
    {
        Tile = 0,
        Pattern = 1,
        Cluster = 2,
        Socket = 3,
        Slot = 4,
    }

    public sealed class GeneratedNavigationCoordinate
    {
        public GeneratedNavigationCoordinate(int x, int y)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y));
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            X.ToString(CultureInfo.InvariantCulture), Y.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Immutable selection-only destination for one validation error. It exposes no validation,
    /// generation, repair, replay, or write operation.
    /// </summary>
    public sealed class GeneratedValidationJumpTarget
    {
        public GeneratedValidationJumpTarget(string validationErrorId, string severity,
            string owner, string message, GeneratedCsvSourceLocation sourceLocation,
            GeneratedValidationJumpTargetKind jumpTargetKind,
            GeneratedNavigationCoordinate sectorCoordinate,
            GeneratedNavigationCoordinate localCellCoordinate,
            GeneratedNavigationCoordinate worldCellCoordinate,
            string patternId, string clusterId, string socketId, string slotId,
            string inspectorTabToken, string inspectorRecordId, string selectionPath,
            string replayReference, bool sourceAvailable, string missingReason)
        {
            ValidationErrorId = Require(validationErrorId, nameof(validationErrorId));
            Severity = Require(severity, nameof(severity));
            Owner = Require(owner, nameof(owner));
            Message = Require(message, nameof(message));
            SourceLocation = sourceLocation ?? throw new ArgumentNullException(nameof(sourceLocation));
            JumpTargetKind = jumpTargetKind;
            SectorCoordinate = sectorCoordinate;
            LocalCellCoordinate = localCellCoordinate;
            WorldCellCoordinate = worldCellCoordinate;
            PatternId = Clean(patternId);
            ClusterId = Clean(clusterId);
            SocketId = Clean(socketId);
            SlotId = Clean(slotId);
            InspectorTabToken = Require(inspectorTabToken, nameof(inspectorTabToken));
            InspectorRecordId = Require(inspectorRecordId, nameof(inspectorRecordId));
            SelectionPath = Require(selectionPath, nameof(selectionPath));
            ReplayReference = Clean(replayReference);
            SourceAvailable = sourceAvailable;
            MissingReason = Clean(missingReason);

            if (sourceAvailable && !sourceLocation.SourceAvailable)
                throw new ArgumentException("An available jump target requires an available source.",
                    nameof(sourceLocation));
            if (!sourceAvailable && MissingReason.Length == 0)
                throw new ArgumentException("An unavailable jump target requires a missing reason.",
                    nameof(missingReason));
            ValidateKindPayload();
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public string ValidationErrorId { get; }
        public string Severity { get; }
        public string Owner { get; }
        public string Message { get; }
        public GeneratedCsvSourceLocation SourceLocation { get; }
        public GeneratedValidationJumpTargetKind JumpTargetKind { get; }
        public GeneratedNavigationCoordinate SectorCoordinate { get; }
        public GeneratedNavigationCoordinate LocalCellCoordinate { get; }
        public GeneratedNavigationCoordinate WorldCellCoordinate { get; }
        public string PatternId { get; }
        public string ClusterId { get; }
        public string SocketId { get; }
        public string SlotId { get; }
        public string InspectorTabToken { get; }
        public string InspectorRecordId { get; }
        public string SelectionPath { get; }
        public string ReplayReference { get; }
        public bool SourceAvailable { get; }
        public string MissingReason { get; }
        public string CanonicalDigest { get; }

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            ValidationErrorId, Severity, Owner, Message, SourceLocation.CanonicalLine,
            JumpTargetKind.ToString(), CoordinateLine(SectorCoordinate),
            CoordinateLine(LocalCellCoordinate), CoordinateLine(WorldCellCoordinate), PatternId,
            ClusterId, SocketId, SlotId, InspectorTabToken, InspectorRecordId, SelectionPath,
            ReplayReference, SourceAvailable ? "1" : "0", MissingReason, CanonicalDigest);

        internal GeneratedValidationJumpTargetDocument ToDocument() =>
            new GeneratedValidationJumpTargetDocument
            {
                validation_error_id = ValidationErrorId,
                severity = Severity,
                owner = Owner,
                message = Message,
                source_location = GeneratedCsvSourceLocationDocument.From(SourceLocation),
                jump_target_kind = JumpTargetKind.ToString(),
                sector_coordinate = GeneratedNavigationCoordinateDocument.From(SectorCoordinate),
                local_cell_coordinate = GeneratedNavigationCoordinateDocument.From(
                    LocalCellCoordinate),
                world_cell_coordinate = GeneratedNavigationCoordinateDocument.From(
                    WorldCellCoordinate),
                pattern_id = PatternId,
                cluster_id = ClusterId,
                socket_id = SocketId,
                slot_id = SlotId,
                inspector_tab_token = InspectorTabToken,
                inspector_record_id = InspectorRecordId,
                selection_path = SelectionPath,
                replay_reference = ReplayReference,
                source_available = SourceAvailable,
                missing_reason = MissingReason,
                canonical_digest = CanonicalDigest,
            };

        private void ValidateKindPayload()
        {
            var hasExplicitMissing = !SourceAvailable && MissingReason.Length > 0;
            switch (JumpTargetKind)
            {
                case GeneratedValidationJumpTargetKind.Tile:
                    if (LocalCellCoordinate == null && !hasExplicitMissing)
                        throw new ArgumentException("A Tile target requires a local cell or explicit MissingData.");
                    break;
                case GeneratedValidationJumpTargetKind.Pattern:
                    RequireIdentity(PatternId, hasExplicitMissing, "Pattern");
                    break;
                case GeneratedValidationJumpTargetKind.Cluster:
                    RequireIdentity(ClusterId, hasExplicitMissing, "Cluster");
                    break;
                case GeneratedValidationJumpTargetKind.Socket:
                    RequireIdentity(SocketId, hasExplicitMissing, "Socket");
                    break;
                case GeneratedValidationJumpTargetKind.Slot:
                    RequireIdentity(SlotId, hasExplicitMissing, "Slot");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(JumpTargetKind));
            }
        }

        private string ComputeCanonicalDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            ValidationErrorId, Severity, Owner, Message, SourceLocation.CanonicalLine,
            JumpTargetKind.ToString(), CoordinateLine(SectorCoordinate),
            CoordinateLine(LocalCellCoordinate), CoordinateLine(WorldCellCoordinate), PatternId,
            ClusterId, SocketId, SlotId, InspectorTabToken, InspectorRecordId, SelectionPath,
            ReplayReference, SourceAvailable ? "1" : "0", MissingReason,
        });

        private static void RequireIdentity(string value, bool hasExplicitMissing, string kind)
        {
            if (value.Length == 0 && !hasExplicitMissing)
                throw new ArgumentException(kind + " target requires its id or explicit MissingData.");
        }

        private static string CoordinateLine(GeneratedNavigationCoordinate value) =>
            value == null ? "NONE" : value.CanonicalLine;

        private static string Require(string value, string parameterName)
        {
            var result = Clean(value);
            if (result.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return result;
        }

        private static string Clean(string value) => value == null ? string.Empty : value.Trim();
    }

    /// <summary>Deterministic MAP20_04 validation-jump sample artifact.</summary>
    public sealed class GeneratedValidationJumpSample
    {
        public const string SchemaVersion = "map20_04.validation_jump_sample.v1";

        private readonly ReadOnlyCollection<string> jumpTargetKindTokens;
        private readonly ReadOnlyCollection<string> sampleErrorIds;
        private readonly ReadOnlyCollection<GeneratedValidationJumpTarget> sampleJumpTargets;

        public GeneratedValidationJumpSample(IEnumerable<GeneratedValidationJumpTarget> targets,
            string createdUtc)
        {
            var ordered = (targets ?? throw new ArgumentNullException(nameof(targets)))
                .Where(value => value != null)
                .OrderBy(value => value.ValidationErrorId, StringComparer.Ordinal)
                .ThenBy(value => value.CanonicalDigest, StringComparer.Ordinal).ToArray();
            if (ordered.Length == 0)
                throw new ArgumentException("At least one jump target is required.", nameof(targets));
            if (ordered.GroupBy(value => value.ValidationErrorId, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("Validation error ids must be unique.", nameof(targets));

            jumpTargetKindTokens = new ReadOnlyCollection<string>(Enum
                .GetValues(typeof(GeneratedValidationJumpTargetKind))
                .Cast<GeneratedValidationJumpTargetKind>().OrderBy(value => (int)value)
                .Select(value => value.ToString()).ToArray());
            sampleJumpTargets = new ReadOnlyCollection<GeneratedValidationJumpTarget>(ordered);
            sampleErrorIds = new ReadOnlyCollection<string>(
                ordered.Select(value => value.ValidationErrorId).ToArray());
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public IReadOnlyList<string> JumpTargetKindTokens => jumpTargetKindTokens;
        public IReadOnlyList<string> SampleErrorIds => sampleErrorIds;
        public IReadOnlyList<GeneratedValidationJumpTarget> SampleJumpTargets => sampleJumpTargets;
        public int SourceLocationCount => sampleJumpTargets.Count;
        public int SelectionPathCount => sampleJumpTargets.Count(value =>
            !string.IsNullOrWhiteSpace(value.SelectionPath));
        public int MissingDataCount => sampleJumpTargets.Count(value => !value.SourceAvailable);
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }

        public string Serialize() => GeneratedInspectionCanonical.ToJson(
            new GeneratedValidationJumpSampleDocument
            {
                schema_version = SchemaVersion,
                task_id = GeneratedCsvNavigationPreconditions.TaskId,
                jump_target_kind_tokens = jumpTargetKindTokens.ToArray(),
                sample_error_ids = sampleErrorIds.ToArray(),
                sample_jump_targets = sampleJumpTargets.Select(value => value.ToDocument()).ToArray(),
                source_location_count = SourceLocationCount,
                selection_path_count = SelectionPathCount,
                missing_data_count = MissingDataCount,
                canonical_digest = CanonicalDigest,
                created_utc = CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
            });

        private string ComputeCanonicalDigest()
        {
            var lines = new List<string>
            {
                SchemaVersion, GeneratedCsvNavigationPreconditions.TaskId,
                string.Join("|", jumpTargetKindTokens), "created_utc_excluded=true",
            };
            lines.AddRange(sampleJumpTargets.Select(value => "target=" + value.CanonicalLine));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }
    }

    [Serializable]
    internal sealed class GeneratedNavigationCoordinateDocument
    {
        public int x;
        public int y;

        public static GeneratedNavigationCoordinateDocument From(
            GeneratedNavigationCoordinate value) => value == null ? null :
            new GeneratedNavigationCoordinateDocument { x = value.X, y = value.Y };
    }

    [Serializable]
    internal sealed class GeneratedValidationJumpTargetDocument
    {
        public string validation_error_id;
        public string severity;
        public string owner;
        public string message;
        public GeneratedCsvSourceLocationDocument source_location;
        public string jump_target_kind;
        public GeneratedNavigationCoordinateDocument sector_coordinate;
        public GeneratedNavigationCoordinateDocument local_cell_coordinate;
        public GeneratedNavigationCoordinateDocument world_cell_coordinate;
        public string pattern_id;
        public string cluster_id;
        public string socket_id;
        public string slot_id;
        public string inspector_tab_token;
        public string inspector_record_id;
        public string selection_path;
        public string replay_reference;
        public bool source_available;
        public string missing_reason;
        public string canonical_digest;
    }

    [Serializable]
    internal sealed class GeneratedValidationJumpSampleDocument
    {
        public string schema_version;
        public string task_id;
        public string[] jump_target_kind_tokens;
        public string[] sample_error_ids;
        public GeneratedValidationJumpTargetDocument[] sample_jump_targets;
        public int source_location_count;
        public int selection_path_count;
        public int missing_data_count;
        public string canonical_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
    }
}
