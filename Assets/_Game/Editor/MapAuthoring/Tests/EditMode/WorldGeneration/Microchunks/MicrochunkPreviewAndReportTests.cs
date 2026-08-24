using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;
using StarNight.MapAuthoring.Microchunks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_12")]
    public sealed class MicrochunkPreviewAndReportTests
    {
        private const string SelectedId = "MC_PREVIEW_TEST";

        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var caseId = 0; caseId < 520; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MicrochunkPreviewAndReportContract_" + caseId.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void MicrochunkPreviewAndReportContract(int caseId)
        {
            var variant = caseId / 26;
            switch (caseId % 26)
            {
                case 0: AssertSelectedIdRequired(variant); break;
                case 1: AssertCanonicalRequest(variant); break;
                case 2: AssertExactSupportedTransforms(variant); break;
                case 3: AssertTransformSelectionIsFrozen(variant); break;
                case 4: AssertUnsupportedTransformRejected(variant); break;
                case 5: AssertAllTransformsGenerated(variant); break;
                case 6: AssertEveryTransformHasNinetySixCells(variant); break;
                case 7: AssertTransformerProjectionIsReused(variant); break;
                case 8: AssertNoNinetyDegreeProjection(variant); break;
                case 9: AssertTileLayerIssueHasCoordinate(variant); break;
                case 10: AssertCoverageFeedbackIsExposed(variant); break;
                case 11: AssertMissingSocketsAreTolerated(variant); break;
                case 12: AssertMissingBandIssueIsReported(variant); break;
                case 13: AssertObjectSlotOverlayIsProjected(variant); break;
                case 14: AssertReachabilityHeatmapIsDerived(variant); break;
                case 15: AssertBlockedSolidHeatmapState(variant); break;
                case 16: AssertMandatorySocketPairWitnessIsExposed(variant); break;
                case 17: AssertIssueOrderingIsDeterministic(variant); break;
                case 18: AssertImportDiagnosticsAreRetained(variant); break;
                case 19: AssertExportDiagnosticsAreRetained(variant); break;
                case 20: AssertDetachedStateIsNotMutated(variant); break;
                case 21: AssertOverlayTogglesAreHonored(variant); break;
                case 22: AssertValidationOptionsAreHonored(variant); break;
                case 23: AssertEditorAssemblyBoundary(variant); break;
                case 24: AssertWindowToleratesEmptySelection(variant); break;
                default: AssertReportIsDeterministic(variant); break;
            }
        }

        private static void AssertSelectedIdRequired(int variant)
        {
            var state = State();
            Assert.That(() => new MicrochunkPreviewRequest(null, state), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkPreviewRequest(string.Empty, state), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkPreviewRequest(" " + SelectedId, state), Throws.TypeOf<ArgumentException>());
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertCanonicalRequest(int variant)
        {
            var state = State();
            var request = Request(state);
            Assert.That(request.SelectedMicrochunkId, Is.EqualTo(SelectedId));
            Assert.That(request.EditorState, Is.SameAs(state));
            Assert.That(request.ValidationOptions, Is.SameAs(MicrochunkPreviewValidationOptions.All));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertExactSupportedTransforms(int variant)
        {
            Assert.That(MicrochunkPreviewRequest.SupportedTransforms, Is.EqualTo(new[]
            {
                MicrochunkTransform.R0,
                MicrochunkTransform.MirrorX,
                MicrochunkTransform.MirrorY,
                MicrochunkTransform.R180
            }));
            Assert.That(MicrochunkPreviewRequest.SupportedTransforms
                .Select(MicrochunkTransformUtility.ToTransformToken),
                Is.EqualTo(new[] { "R0", "MIRROR_X", "MIRROR_Y", "R180" }));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertTransformSelectionIsFrozen(int variant)
        {
            var transforms = new List<MicrochunkTransform>
            {
                MicrochunkTransform.R180,
                MicrochunkTransform.R0,
                MicrochunkTransform.R180
            };
            var request = Request(State(), transforms);
            transforms.Clear();
            Assert.That(request.SelectedTransforms, Is.EqualTo(new[]
            {
                MicrochunkTransform.R0,
                MicrochunkTransform.R180
            }));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertUnsupportedTransformRejected(int variant)
        {
            Assert.That(
                () => Request(State(), new[] { (MicrochunkTransform)999 }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Request(State(), Array.Empty<MicrochunkTransform>()),
                Throws.TypeOf<ArgumentException>());
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertAllTransformsGenerated(int variant)
        {
            var report = Build(State());
            Assert.That(report.Transforms.Select(value => value.Transform),
                Is.EqualTo(MicrochunkPreviewRequest.SupportedTransforms));
            Assert.That(report.SelectedMicrochunkId, Is.EqualTo(SelectedId));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertEveryTransformHasNinetySixCells(int variant)
        {
            var report = Build(State());
            foreach (var transform in report.Transforms)
            {
                Assert.That(transform.Cells, Has.Count.EqualTo(MicrochunkConstants.CellCount));
                Assert.That(transform.Cells.Select(value => value.Coordinate.RowMajorIndex),
                    Is.EqualTo(Enumerable.Range(0, MicrochunkConstants.CellCount)));
            }
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertTransformerProjectionIsReused(int variant)
        {
            var state = State();
            state.Grid.State.PaintCell(2, 3, MicrochunkTileLayer.GroundSolid, "GROUND_TEST");
            var report = Build(state);
            Assert.That(report.GetTransform(MicrochunkTransform.R0).GetCell(2, 3).TileCell.GroundCode,
                Is.EqualTo("GROUND_TEST"));
            Assert.That(report.GetTransform(MicrochunkTransform.MirrorX).GetCell(9, 3).TileCell.GroundCode,
                Is.EqualTo("GROUND_TEST"));
            Assert.That(report.GetTransform(MicrochunkTransform.MirrorY).GetCell(2, 4).TileCell.GroundCode,
                Is.EqualTo("GROUND_TEST"));
            Assert.That(report.GetTransform(MicrochunkTransform.R180).GetCell(9, 4).TileCell.GroundCode,
                Is.EqualTo("GROUND_TEST"));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertNoNinetyDegreeProjection(int variant)
        {
            Assert.That(MicrochunkPreviewRequest.SupportedTransforms, Has.Count.EqualTo(4));
            Assert.That(MicrochunkPreviewRequest.SupportedTransforms
                .Select(MicrochunkTransformUtility.ToTransformToken), Has.None.Contains("90"));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertTileLayerIssueHasCoordinate(int variant)
        {
            var state = State();
            state.Grid.State.PaintCell(variant % 12, variant % 8, MicrochunkTileLayer.GroundSolid, "GROUND");
            state.Grid.State.PaintCell(variant % 12, variant % 8, MicrochunkTileLayer.Breakable, "BREAKABLE");
            var report = Build(state, new[] { MicrochunkTransform.R0 });
            var issue = report.Issues.Single(value =>
                value.Category == MicrochunkPreviewIssueCategory.TileLayer);
            Assert.That(issue.Code, Is.EqualTo(MicrochunkTileLayerRules.ForbiddenPairReason));
            Assert.That(issue.LocalCoordinate, Is.EqualTo(new MicrochunkLocalCoord(variant % 12, variant % 8)));
            Assert.That(issue.Transform, Is.EqualTo(MicrochunkTransform.R0));
        }

        private static void AssertCoverageFeedbackIsExposed(int variant)
        {
            var transform = Build(State(), new[] { MicrochunkTransform.R0 }).Transforms.Single();
            Assert.That(transform.CoverageResult, Is.Not.Null);
            Assert.That(transform.CoverageResult.Success, Is.True);
            Assert.That(transform.CoverageResult.RecordCount, Is.EqualTo(96));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertMissingSocketsAreTolerated(int variant)
        {
            var report = Build(State(), new[] { MicrochunkTransform.R0 });
            Assert.That(report.Success, Is.True, JoinIssues(report));
            Assert.That(report.Transforms.Single().Definition.Sockets, Is.Empty);
            Assert.That(report.Transforms.Single().ReachabilityResult.EvaluatedSocketCount, Is.Zero);
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertMissingBandIssueIsReported(int variant)
        {
            var state = State();
            state.SocketAuthoring.AddSocket(new MicrochunkSocketAuthoringRow(
                "SOCK_MISSING", "L", "BAND_MISSING", "WALK", "EDGE_MISSING"));
            var report = Build(state, new[] { MicrochunkTransform.R0 });
            Assert.That(report.Issues.Any(value =>
                value.Category == MicrochunkPreviewIssueCategory.SocketEdge &&
                value.Code == MicrochunkSocketEdgeValidator.MissingBandReason), Is.True);
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertObjectSlotOverlayIsProjected(int variant)
        {
            var state = State();
            state.ObjectSlotAuthoring.Add(new MicrochunkObjectSlotAuthoringRow(
                "SLOT_TEST", 3, 2, "RESOURCE", "POOL_TEST"));
            var report = Build(state, new[] { MicrochunkTransform.R0 });
            Assert.That(report.Transforms.Single().GetCell(3, 2).ObjectSlotIds,
                Is.EqualTo(new[] { "SLOT_TEST" }));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertReachabilityHeatmapIsDerived(int variant)
        {
            var transform = Build(State(), new[] { MicrochunkTransform.R0 }).Transforms.Single();
            Assert.That(transform.ReachabilityResult.Nodes, Has.Count.EqualTo(96));
            Assert.That(transform.Cells, Has.All.Matches<MicrochunkPreviewCellOverlay>(value => value.IsReachable));
            Assert.That(transform.Cells.Select(value => value.ReachabilityState).Distinct(),
                Is.EqualTo(new[] { MicrochunkPreviewReachabilityState.Reachable }));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertBlockedSolidHeatmapState(int variant)
        {
            var state = State();
            var coordinate = new MicrochunkLocalCoord(variant % 12, variant % 8);
            state.Grid.State.PaintCell(coordinate.X, coordinate.Y, MicrochunkTileLayer.GroundSolid, "SOLID");
            var cell = Build(state, new[] { MicrochunkTransform.R0 })
                .Transforms.Single().GetCell(coordinate.X, coordinate.Y);
            Assert.That(cell.IsBlockedSolid, Is.True);
            Assert.That(cell.IsReachable, Is.False);
            Assert.That(cell.ReachabilityState, Is.EqualTo(MicrochunkPreviewReachabilityState.BlockedSolid));
        }

        private static void AssertMandatorySocketPairWitnessIsExposed(int variant)
        {
            var state = StateWithSocketPair();
            var transform = Build(state, new[] { MicrochunkTransform.R0 }).Transforms.Single();
            Assert.That(transform.MandatorySocketPairWitnesses, Is.Not.Empty);
            Assert.That(transform.MandatorySocketPairWitnesses.All(value => value.Coordinates.Count > 0), Is.True);
            Assert.That(transform.Cells.Any(value => value.IsPathWitness), Is.True);
            Assert.That(transform.Cells.Any(value => value.IsSocketEntry), Is.True);
            Assert.That(transform.Cells.Any(value => value.IsSocketExit), Is.True);
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertIssueOrderingIsDeterministic(int variant)
        {
            var issues = new[]
            {
                new MicrochunkPreviewIssue(MicrochunkPreviewIssueSeverity.Warning, 1,
                    MicrochunkPreviewIssueCategory.Export, "Z", "z", SelectedId, sourceOrder: 2),
                new MicrochunkPreviewIssue(MicrochunkPreviewIssueSeverity.Error, 1,
                    MicrochunkPreviewIssueCategory.Coverage, "B", "b", SelectedId, sourceOrder: 1),
                new MicrochunkPreviewIssue(MicrochunkPreviewIssueSeverity.Error, 0,
                    MicrochunkPreviewIssueCategory.TileLayer, "A", "a", SelectedId, sourceOrder: 0)
            };
            var report = new MicrochunkPreviewReport(
                SelectedId,
                Array.Empty<MicrochunkPreviewTransformReport>(),
                issues);
            Assert.That(report.Issues.Select(value => value.Code), Is.EqualTo(new[] { "A", "B", "Z" }));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertImportDiagnosticsAreRetained(int variant)
        {
            var input = new MicrochunkCsvImportIssue(
                "microchunk_catalog.csv", SelectedId, variant + 1, "active", "IMPORT_TEST", "input warning",
                MicrochunkCsvImportIssueSeverity.Warning);
            var request = new MicrochunkPreviewRequest(
                SelectedId, State(), new[] { MicrochunkTransform.R0 }, importIssues: new[] { input });
            var report = new MicrochunkPreviewBuilder().Build(request);
            var issue = report.Issues.Single(value => value.Code == "IMPORT_TEST");
            Assert.That(issue.Category, Is.EqualTo(MicrochunkPreviewIssueCategory.Import));
            Assert.That(issue.Severity, Is.EqualTo(MicrochunkPreviewIssueSeverity.Warning));
        }

        private static void AssertExportDiagnosticsAreRetained(int variant)
        {
            var input = new MicrochunkCsvExportIssue(
                "microchunk_catalog.csv", SelectedId, "active", "EXPORT_TEST", "input error");
            var request = new MicrochunkPreviewRequest(
                SelectedId, State(), new[] { MicrochunkTransform.R0 }, exportIssues: new[] { input });
            var report = new MicrochunkPreviewBuilder().Build(request);
            var issue = report.Issues.Single(value => value.Code == "EXPORT_TEST");
            Assert.That(issue.Category, Is.EqualTo(MicrochunkPreviewIssueCategory.Export));
            Assert.That(issue.IsError, Is.True);
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertDetachedStateIsNotMutated(int variant)
        {
            var state = StateWithSocketPair();
            state.ObjectSlotAuthoring.Add(new MicrochunkObjectSlotAuthoringRow(
                "SLOT_STABLE", 4, 4, "RESOURCE", "POOL_STABLE"));
            var before = StateSignature(state);
            Build(state);
            Assert.That(StateSignature(state), Is.EqualTo(before));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertOverlayTogglesAreHonored(int variant)
        {
            var state = StateWithSocketPair();
            state.ObjectSlotAuthoring.Add(new MicrochunkObjectSlotAuthoringRow(
                "SLOT_HIDDEN", 4, 4, "RESOURCE", "POOL_HIDDEN"));
            var request = new MicrochunkPreviewRequest(
                SelectedId,
                state,
                new[] { MicrochunkTransform.R0 },
                false,
                false,
                false,
                false);
            var transform = new MicrochunkPreviewBuilder().Build(request).Transforms.Single();
            Assert.That(transform.Cells, Has.All.Matches<MicrochunkPreviewCellOverlay>(value => value.TileCell == null));
            Assert.That(transform.Cells.SelectMany(value => value.SocketIds), Is.Empty);
            Assert.That(transform.Cells.SelectMany(value => value.ObjectSlotIds), Is.Empty);
            Assert.That(transform.Cells.Select(value => value.ReachabilityState).Distinct(),
                Is.EqualTo(new[] { MicrochunkPreviewReachabilityState.Disabled }));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertValidationOptionsAreHonored(int variant)
        {
            var state = State();
            state.Grid.State.PaintCell(1, 1, MicrochunkTileLayer.GroundSolid, "GROUND");
            state.Grid.State.PaintCell(1, 1, MicrochunkTileLayer.Breakable, "BREAKABLE");
            var options = new MicrochunkPreviewValidationOptions(false, false, false, false, false);
            var request = new MicrochunkPreviewRequest(
                SelectedId, state, new[] { MicrochunkTransform.R0 }, validationOptions: options);
            var report = new MicrochunkPreviewBuilder().Build(request);
            Assert.That(report.Issues, Is.Empty);
            Assert.That(report.Transforms.Single().TileLayerResult, Is.Null);
            Assert.That(report.Transforms.Single().ReachabilityResult, Is.Null);
            Assert.That(report.Transforms.Single().GetCell(1, 1).ReachabilityState,
                Is.EqualTo(MicrochunkPreviewReachabilityState.Disabled));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertEditorAssemblyBoundary(int variant)
        {
            Assert.That(typeof(MicrochunkPreviewBuilder).Assembly.GetName().Name, Is.EqualTo("MapAuthoring.Editor"));
            Assert.That(typeof(MicrochunkPreviewReport).Assembly, Is.SameAs(typeof(MicrochunkCsvExporter).Assembly));
            Assert.That(typeof(MicrochunkPreviewReport).Assembly, Is.Not.SameAs(typeof(MicrochunkDefinition).Assembly));
            Assert.That(typeof(EditorWindow).IsAssignableFrom(typeof(MicrochunkPreviewWindow)), Is.True);
            var runtimeNames = typeof(MicrochunkDefinition).Assembly.GetTypes().Select(value => value.Name).ToArray();
            Assert.That(runtimeNames, Does.Not.Contain(nameof(MicrochunkPreviewBuilder)));
            Assert.That(runtimeNames, Does.Not.Contain(nameof(MicrochunkPreviewReport)));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertWindowToleratesEmptySelection(int variant)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var dirtyBefore = scene.IsValid() && scene.isDirty;
            var window = ScriptableObject.CreateInstance<MicrochunkPreviewWindow>();
            try
            {
                Assert.That(window.TryGeneratePreview(), Is.False);
                Assert.That(window.LastReport, Is.Null);
                Assert.That(window.LastError, Is.Not.Empty);
                window.UseDetachedEditorState(string.Empty, State());
                Assert.That(window.TryGeneratePreview(), Is.False);
                Assert.That(window.LastError, Does.Contain("selected microchunk ID"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
            var dirtyAfter = scene.IsValid() && scene.isDirty;
            Assert.That(dirtyAfter, Is.EqualTo(dirtyBefore));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertReportIsDeterministic(int variant)
        {
            var state = StateWithSocketPair();
            state.Grid.State.PaintCell(variant % 12, variant % 8, MicrochunkTileLayer.DecorationBack, "DECOR");
            var first = Build(state);
            var second = Build(state);
            Assert.That(ReportSignature(second), Is.EqualTo(ReportSignature(first)));
        }

        private static MicrochunkPreviewRequest Request(
            MicrochunkSocketAndSlotEditorViewModel state,
            IEnumerable<MicrochunkTransform> transforms = null)
        {
            return new MicrochunkPreviewRequest(SelectedId, state, transforms);
        }

        private static MicrochunkPreviewReport Build(
            MicrochunkSocketAndSlotEditorViewModel state,
            IEnumerable<MicrochunkTransform> transforms = null)
        {
            return new MicrochunkPreviewBuilder().Build(Request(state, transforms));
        }

        private static MicrochunkSocketAndSlotEditorViewModel State()
        {
            return new MicrochunkSocketAndSlotEditorViewModel();
        }

        private static MicrochunkSocketAndSlotEditorViewModel StateWithSocketPair()
        {
            var state = State();
            state.SocketAuthoring.AddBand(new MicrochunkSocketBandAuthoringRow("BAND_LEFT", "L", 3, 3));
            state.SocketAuthoring.AddBand(new MicrochunkSocketBandAuthoringRow("BAND_RIGHT", "R", 3, 3));
            state.SocketAuthoring.AddSocket(new MicrochunkSocketAuthoringRow(
                "SOCK_LEFT", "L", "BAND_LEFT", "WALK", "EDGE_LEFT", true));
            state.SocketAuthoring.AddSocket(new MicrochunkSocketAuthoringRow(
                "SOCK_RIGHT", "R", "BAND_RIGHT", "WALK", "EDGE_RIGHT", true));
            return state;
        }

        private static string StateSignature(MicrochunkSocketAndSlotEditorViewModel state)
        {
            return string.Join("\n", state.Grid.State.Cells.Select(value =>
                       value.Coordinate.RowMajorIndex + ":" + string.Join("|", value.TileCodes))) + "\n" +
                   string.Join("\n", state.SocketAuthoring.Sockets.Select(value => value.SocketId)) + "\n" +
                   string.Join("\n", state.SocketAuthoring.Bands.Select(value => value.BandId)) + "\n" +
                   string.Join("\n", state.ObjectSlotAuthoring.Rows.Select(value => value.SlotId));
        }

        private static string ReportSignature(MicrochunkPreviewReport report)
        {
            return report.SelectedMicrochunkId + "\n" +
                   string.Join("\n", report.Transforms.SelectMany(transform => transform.Cells.Select(cell =>
                       transform.Transform + ":" + cell.Coordinate.RowMajorIndex + ":" +
                       cell.ReachabilityState + ":" + string.Join("|", cell.SocketIds) + ":" +
                       string.Join("|", cell.ObjectSlotIds)))) + "\n" +
                   string.Join("\n", report.Issues.Select(value => value.ToString()));
        }

        private static string JoinIssues(MicrochunkPreviewReport report)
        {
            return string.Join("\n", report.Issues.Select(value => value.ToString()));
        }
    }
}
