using System;
using System.Collections.Generic;
using System.Globalization;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Tooling
{
    /// <summary>
    /// Serializable display state for a runtime debug HUD. This is data only: there is no
    /// bootstrap, registration, GameObject creation, component enablement, or scene wiring.
    /// </summary>
    public sealed class GeneratedRuntimeDebugHudState
    {
        public const string SchemaVersion = "map20_05.runtime_hud_state.v1";

        public GeneratedRuntimeDebugHudState(int seed, string worldId,
            GeneratedNavigationCoordinate sectorCoordinate,
            GeneratedNavigationCoordinate cellCoordinate, string activePass,
            string activeToolMode, string selectedValidationErrorId,
            string selectedFailureRecordId, string selectedSourcePath,
            string selectedSelectionPath, string statusLine)
        {
            Seed = seed;
            WorldId = GeneratedReplayAuthoringRequest.Require(worldId, nameof(worldId));
            SectorCoordinate = sectorCoordinate ?? throw new ArgumentNullException(
                nameof(sectorCoordinate));
            CellCoordinate = cellCoordinate ?? throw new ArgumentNullException(
                nameof(cellCoordinate));
            ActivePass = GeneratedReplayAuthoringRequest.Require(activePass, nameof(activePass));
            ActiveToolMode = GeneratedReplayAuthoringRequest.Require(activeToolMode,
                nameof(activeToolMode));
            SelectedValidationErrorId = GeneratedReplayAuthoringRequest.Clean(
                selectedValidationErrorId);
            SelectedFailureRecordId = GeneratedReplayAuthoringRequest.Clean(
                selectedFailureRecordId);
            SelectedSourcePath = GeneratedReplayAuthoringRequest.Clean(selectedSourcePath)
                .Replace('\\', '/');
            SelectedSelectionPath = GeneratedReplayAuthoringRequest.Clean(
                selectedSelectionPath).Replace('\\', '/');
            StatusLine = GeneratedReplayAuthoringRequest.Require(statusLine, nameof(statusLine));
            HudStateId = "HUD-" + BakingCanonicalDigest.HashCanonicalLines(
                StableLines(false)).Substring(0, 24);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(StableLines(true));
        }

        public string TaskId => GeneratedReplayAuthoringPreconditions.TaskId;
        public string HudStateId { get; }
        public int Seed { get; }
        public string WorldId { get; }
        public GeneratedNavigationCoordinate SectorCoordinate { get; }
        public GeneratedNavigationCoordinate CellCoordinate { get; }
        public string ActivePass { get; }
        public string ActiveToolMode { get; }
        public string SelectedValidationErrorId { get; }
        public string SelectedFailureRecordId { get; }
        public string SelectedSourcePath { get; }
        public string SelectedSelectionPath { get; }
        public string StatusLine { get; }
        public int RuntimeMutationCount => 0;
        public string CanonicalDigest { get; }

        public string Serialize() => GeneratedReplayAuthoringJson.ToJson(
            new GeneratedRuntimeDebugHudStateDocument
            {
                schema_version = SchemaVersion,
                task_id = TaskId,
                hud_state_id = HudStateId,
                seed = Seed,
                world_id = WorldId,
                sector_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(
                    SectorCoordinate),
                cell_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(CellCoordinate),
                active_pass = ActivePass,
                active_tool_mode = ActiveToolMode,
                selected_validation_error_id = SelectedValidationErrorId,
                selected_failure_record_id = SelectedFailureRecordId,
                selected_source_path = SelectedSourcePath,
                selected_selection_path = SelectedSelectionPath,
                status_line = StatusLine,
                runtime_mutation_count = RuntimeMutationCount,
                canonical_digest = CanonicalDigest,
            });

        private IEnumerable<string> StableLines(bool includeId)
        {
            yield return SchemaVersion;
            yield return GeneratedReplayAuthoringPreconditions.TaskId;
            if (includeId) yield return HudStateId;
            yield return Seed.ToString(CultureInfo.InvariantCulture);
            yield return WorldId;
            yield return GeneratedReplayAuthoringCanonical.Coordinate(SectorCoordinate);
            yield return GeneratedReplayAuthoringCanonical.Coordinate(CellCoordinate);
            yield return ActivePass;
            yield return ActiveToolMode;
            yield return SelectedValidationErrorId;
            yield return SelectedFailureRecordId;
            yield return SelectedSourcePath;
            yield return SelectedSelectionPath;
            yield return StatusLine;
            yield return "runtime_mutation_count=0";
        }
    }

    /// <summary>Pure formatter for a HUD consumer supplied by a future integration task.</summary>
    public sealed class GeneratedRuntimeDebugHud
    {
        public GeneratedRuntimeDebugHud(GeneratedRuntimeDebugHudState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public GeneratedRuntimeDebugHudState State { get; }
        public int AutoSpawnPathCount => 0;
        public int ScenePrefabWiringCount => 0;
        public int RuntimeMutationCount => State.RuntimeMutationCount;

        public IReadOnlyList<string> FormatLines() => new[]
        {
            "Seed " + State.Seed.ToString(CultureInfo.InvariantCulture) +
            " / World " + State.WorldId,
            "Sector " + GeneratedReplayAuthoringCanonical.Coordinate(State.SectorCoordinate) +
            " / Cell " + GeneratedReplayAuthoringCanonical.Coordinate(State.CellCoordinate),
            "Pass " + State.ActivePass + " / Mode " + State.ActiveToolMode,
            "Validation " + State.SelectedValidationErrorId +
            " / Failure " + State.SelectedFailureRecordId,
            State.StatusLine,
        };
    }

    [Serializable]
    internal sealed class GeneratedRuntimeDebugHudStateDocument
    {
        public string schema_version;
        public string task_id;
        public string hud_state_id;
        public int seed;
        public string world_id;
        public GeneratedReplayAuthoringCoordinateDocument sector_coordinate;
        public GeneratedReplayAuthoringCoordinateDocument cell_coordinate;
        public string active_pass;
        public string active_tool_mode;
        public string selected_validation_error_id;
        public string selected_failure_record_id;
        public string selected_source_path;
        public string selected_selection_path;
        public string status_line;
        public int runtime_mutation_count;
        public string canonical_digest;
    }
}
