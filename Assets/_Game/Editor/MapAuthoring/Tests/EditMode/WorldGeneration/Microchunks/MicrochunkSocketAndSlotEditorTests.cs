using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;
using StarNight.MapAuthoring.Microchunks;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_09")]
    public sealed class MicrochunkSocketAndSlotEditorTests
    {
        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var caseId = 0; caseId < 380; caseId++)
                {
                    yield return new TestCaseData(caseId).SetName("SocketAndSlotEditorContract_" + caseId.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void SocketAndSlotEditorContract(int caseId)
        {
            var variant = caseId / 20;
            switch (caseId % 20)
            {
                case 0:
                    AssertSocketDefaults(variant);
                    break;
                case 1:
                    AssertSocketTokensAreCanonical(variant);
                    break;
                case 2:
                    AssertSocketSidesAndRuntimeTokens(variant);
                    break;
                case 3:
                    AssertBandRanges(variant);
                    break;
                case 4:
                    AssertBandRangesRejectInvalidValues(variant);
                    break;
                case 5:
                    AssertSocketCollectionRejectsDuplicatesAndSorts(variant);
                    break;
                case 6:
                    AssertSocketCollectionCommandsAreDeterministic(variant);
                    break;
                case 7:
                    AssertBandCollectionCommandsAreDeterministic(variant);
                    break;
                case 8:
                    AssertSlotDefaultsAndOrientations(variant);
                    break;
                case 9:
                    AssertSlotValuesRejectInvalidInputs(variant);
                    break;
                case 10:
                    AssertSlotCollectionRejectsDuplicatesAndSorts(variant);
                    break;
                case 11:
                    AssertSlotCollectionCommandsAreDeterministic(variant);
                    break;
                case 12:
                    AssertRuntimeProjectionCombinesAllRows(variant);
                    break;
                case 13:
                    AssertProjectionDoesNotMutateGridOrRows(variant);
                    break;
                case 14:
                    AssertExistingValidatorsAcceptValidAuthoringState(variant);
                    break;
                case 15:
                    AssertExistingSocketValidatorDetectsBadBand(variant);
                    break;
                case 16:
                    AssertExistingSocketValidatorDetectsBadSignature(variant);
                    break;
                case 17:
                    AssertExistingSlotValidatorDetectsBadPool(variant);
                    break;
                case 18:
                    AssertEditorBoundaryAndNoIoCommands(variant);
                    break;
                default:
                    AssertFutureBoundaryAndValidationDeterminism(variant);
                    break;
            }
        }

        private static void AssertSocketDefaults(int variant)
        {
            var row = Socket("SOCKET_" + variant, "L");
            Assert.That(row.ToolRequirementToken, Is.EqualTo("NONE"));
            Assert.That(row.MandatoryAllowed, Is.False);
            Assert.That(row.TraversalKindToken, Is.EqualTo("WALK"));
            Assert.That(row.ToRuntimeDefinition(variant % 3).ToolRequirement, Is.EqualTo(MicrochunkToolRequirement.None));
        }

        private static void AssertSocketTokensAreCanonical(int variant)
        {
            Assert.That(() => Socket(null, "L"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => Socket("SOCKET", " L"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkSocketAuthoringRow(
                "SOCKET", "L", "BAND", "walk", "EDGE", false, "NONE"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkSocketAuthoringRow(
                "SOCKET", "L", "BAND", "WALK", "EDGE", false, " NONE "), Throws.TypeOf<ArgumentException>());
            Assert.That(variant, Is.GreaterThanOrEqualTo(0));
        }

        private static void AssertSocketSidesAndRuntimeTokens(int variant)
        {
            var tokens = new[] { "L", "R", "D", "U" };
            var expected = new[] { MicrochunkSide.Left, MicrochunkSide.Right, MicrochunkSide.Down, MicrochunkSide.Up };
            var index = variant % tokens.Length;
            var runtime = Socket("SOCKET", tokens[index]).ToRuntimeDefinition(0);
            Assert.That(runtime.Side, Is.EqualTo(expected[index]));
            Assert.That(() => Socket("BAD", "LEFT"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => Socket("BAD", "X"), Throws.TypeOf<ArgumentException>());
        }

        private static void AssertBandRanges(int variant)
        {
            var side = new[] { "L", "R", "D", "U" }[variant % 4];
            var maximum = side == "L" || side == "R" ? 7 : 11;
            var start = variant % (maximum + 1);
            var row = new MicrochunkSocketBandAuthoringRow("BAND", side, start, maximum, variant % 3);
            Assert.That(row.InclusiveStart, Is.EqualTo(start));
            Assert.That(row.InclusiveEnd, Is.EqualTo(maximum));
            Assert.That(row.ToRuntimeDefinition().Axis, Is.EqualTo(
                side == "L" || side == "R" ? MicrochunkEdgeAxis.HorizontalEdge : MicrochunkEdgeAxis.VerticalEdge));
        }

        private static void AssertBandRangesRejectInvalidValues(int variant)
        {
            var side = variant % 2 == 0 ? "L" : "U";
            var outside = side == "L" ? 8 : 12;
            Assert.That(() => new MicrochunkSocketBandAuthoringRow("BAND", side, -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new MicrochunkSocketBandAuthoringRow("BAND", side, 2, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new MicrochunkSocketBandAuthoringRow("BAND", side, 0, outside), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new MicrochunkSocketBandAuthoringRow("BAND", side, 0, 0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static void AssertSocketCollectionRejectsDuplicatesAndSorts(int variant)
        {
            var collection = new MicrochunkSocketAuthoringCollection();
            collection.AddSocket(Socket("SOCKET_Z_" + variant, "R"));
            collection.AddSocket(Socket("SOCKET_A_" + variant, "L"));
            Assert.That(collection.Sockets.Select(row => row.SocketId),
                Is.EqualTo(new[] { "SOCKET_A_" + variant, "SOCKET_Z_" + variant }));
            Assert.That(() => collection.AddSocket(Socket("SOCKET_A_" + variant, "L")), Throws.TypeOf<ArgumentException>());
            var snapshot = collection.Sockets;
            Assert.That(() => ((IList<MicrochunkSocketAuthoringRow>)snapshot).Clear(), Throws.TypeOf<NotSupportedException>());
        }

        private static void AssertSocketCollectionCommandsAreDeterministic(int variant)
        {
            var collection = new MicrochunkSocketAuthoringCollection();
            collection.AddSocket(Socket("A_" + variant, "L"));
            collection.DuplicateSocket("A_" + variant, "B_" + variant);
            var original = collection.Sockets[0];
            collection.MoveSocket(0, 1);
            Assert.That(collection.Sockets.Select(row => row.SocketId), Is.EqualTo(new[] { "B_" + variant, "A_" + variant }));
            Assert.That(collection.RemoveSocket("B_" + variant), Is.True);
            Assert.That(collection.RemoveSocket("MISSING"), Is.False);
            Assert.That(collection.Sockets.Single(), Is.SameAs(original));
        }

        private static void AssertBandCollectionCommandsAreDeterministic(int variant)
        {
            var collection = new MicrochunkSocketAuthoringCollection();
            collection.AddBand(new MicrochunkSocketBandAuthoringRow("Z_" + variant, "R", 0, 7));
            collection.AddBand(new MicrochunkSocketBandAuthoringRow("A_" + variant, "L", 0, 7));
            Assert.That(collection.Bands.Select(row => row.BandId), Is.EqualTo(new[] { "A_" + variant, "Z_" + variant }));
            collection.DuplicateBand("A_" + variant, "B_" + variant);
            collection.MoveBand(0, 2);
            Assert.That(collection.Bands.Select(row => row.BandId), Is.EqualTo(new[] { "B_" + variant, "Z_" + variant, "A_" + variant }));
            Assert.That(collection.ProjectBandsById().Keys, Is.EqualTo(new[] { "A_" + variant, "B_" + variant, "Z_" + variant }));
            Assert.That(collection.RemoveBand("B_" + variant), Is.True);
        }

        private static void AssertSlotDefaultsAndOrientations(int variant)
        {
            var token = new[] { "NONE", "L", "R", "U", "D" }[variant % 5];
            var expected = new[]
            {
                MicrochunkObjectOrientation.None,
                MicrochunkObjectOrientation.Left,
                MicrochunkObjectOrientation.Right,
                MicrochunkObjectOrientation.Up,
                MicrochunkObjectOrientation.Down
            }[variant % 5];
            var row = Slot("SLOT_" + variant, variant % 12, variant % 8, token);
            Assert.That(row.ToRuntimeDefinition().Orientation, Is.EqualTo(expected));
            Assert.That(row.Required, Is.False);
            Assert.That(row.VisibleFromRoute, Is.True);
            Assert.That(row.RequiredMarkerCode, Is.EqualTo("NONE"));
        }

        private static void AssertSlotValuesRejectInvalidInputs(int variant)
        {
            Assert.That(() => Slot("SLOT", 12, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Slot("SLOT", 0, 8), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new MicrochunkObjectSlotAuthoringRow(
                "SLOT", 0, 0, "resource", "POOL"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkObjectSlotAuthoringRow(
                "SLOT", 0, 0, "RESOURCE", " POOL"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => Slot("SLOT", 0, 0, "LEFT"), Throws.TypeOf<ArgumentException>());
            Assert.That(variant, Is.LessThan(19));
        }

        private static void AssertSlotCollectionRejectsDuplicatesAndSorts(int variant)
        {
            var collection = new MicrochunkObjectSlotAuthoringCollection();
            collection.Add(Slot("Z_" + variant, 1, 1));
            collection.Add(Slot("A_" + variant, 2, 2));
            Assert.That(collection.Rows.Select(row => row.SlotId), Is.EqualTo(new[] { "A_" + variant, "Z_" + variant }));
            Assert.That(() => collection.Add(Slot("A_" + variant, 3, 3)), Throws.TypeOf<ArgumentException>());
            Assert.That(() => ((IList<MicrochunkObjectSlotAuthoringRow>)collection.Rows).RemoveAt(0), Throws.TypeOf<NotSupportedException>());
        }

        private static void AssertSlotCollectionCommandsAreDeterministic(int variant)
        {
            var collection = new MicrochunkObjectSlotAuthoringCollection();
            collection.Add(Slot("A_" + variant, 0, 0));
            collection.Duplicate("A_" + variant, "B_" + variant);
            collection.Move(0, 1);
            Assert.That(collection.Rows.Select(row => row.SlotId), Is.EqualTo(new[] { "B_" + variant, "A_" + variant }));
            Assert.That(collection.ProjectDefinitions().Select(row => row.SlotId), Is.EqualTo(new[] { "A_" + variant, "B_" + variant }));
            Assert.That(collection.Remove("B_" + variant), Is.True);
            Assert.That(collection.Remove("MISSING"), Is.False);
        }

        private static void AssertRuntimeProjectionCombinesAllRows(int variant)
        {
            var viewModel = PopulatedViewModel(variant);
            var definition = viewModel.ProjectDefinition();
            Assert.That(definition.TileCells, Has.Count.EqualTo(96));
            Assert.That(definition.Sockets, Has.Count.EqualTo(1));
            Assert.That(definition.ObjectSlots, Has.Count.EqualTo(1));
            Assert.That(viewModel.ProjectBandsById(), Has.Count.EqualTo(1));
            Assert.That(definition.Sockets[0].BandId, Is.EqualTo("BAND_" + variant));
            Assert.That(definition.ObjectSlots[0].AllowedPoolId, Is.EqualTo("POOL_" + variant));
        }

        private static void AssertProjectionDoesNotMutateGridOrRows(int variant)
        {
            var viewModel = PopulatedViewModel(variant);
            viewModel.Grid.State.PaintCell(variant % 12, variant % 8, MicrochunkTileLayer.Marker, "M_KEEP");
            var beforeGrid = GridSignature(viewModel.Grid.State);
            var beforeSockets = string.Join("|", viewModel.SocketAuthoring.Sockets.Select(row => row.SocketId));
            var first = viewModel.ProjectDefinition();
            var second = viewModel.ProjectDefinition();
            Assert.That(DefinitionSignature(first), Is.EqualTo(DefinitionSignature(second)));
            Assert.That(GridSignature(viewModel.Grid.State), Is.EqualTo(beforeGrid));
            Assert.That(string.Join("|", viewModel.SocketAuthoring.Sockets.Select(row => row.SocketId)), Is.EqualTo(beforeSockets));
        }

        private static void AssertExistingValidatorsAcceptValidAuthoringState(int variant)
        {
            var viewModel = PopulatedViewModel(variant);
            var summary = viewModel.Validate();
            Assert.That(summary.SocketResult.GetType(), Is.EqualTo(typeof(MicrochunkSocketEdgeValidationResult)));
            Assert.That(summary.ObjectSlotResult.GetType(), Is.EqualTo(typeof(MicrochunkObjectSlotValidationResult)));
            Assert.That(summary.SocketResult.Success, Is.True);
            Assert.That(summary.ObjectSlotResult.Success, Is.True);
            Assert.That(summary.Success, Is.True);
        }

        private static void AssertExistingSocketValidatorDetectsBadBand(int variant)
        {
            var viewModel = new MicrochunkSocketAndSlotEditorViewModel();
            viewModel.SocketAuthoring.AddBand(new MicrochunkSocketBandAuthoringRow("BAND", "U", 0, 0));
            viewModel.SocketAuthoring.AddSocket(Socket("SOCKET_" + variant, "L", "BAND"));
            var result = viewModel.Validate().SocketResult;
            Assert.That(result.Success, Is.False);
            Assert.That(result.Violations.Any(value => value.Reason == MicrochunkSocketEdgeValidator.BandAxisMismatchReason), Is.True);
        }

        private static void AssertExistingSocketValidatorDetectsBadSignature(int variant)
        {
            var viewModel = new MicrochunkSocketAndSlotEditorViewModel();
            viewModel.SocketAuthoring.AddBand(new MicrochunkSocketBandAuthoringRow("BAND", "L", 0, 0));
            viewModel.SocketAuthoring.AddSocket(Socket("SOCKET_" + variant, "L", "BAND"));
            var signatures = new Dictionary<string, MicrochunkEdgeSignatureDefinition>(StringComparer.Ordinal)
            {
                ["EDGE"] = new MicrochunkEdgeSignatureDefinition(
                    "EDGE", MicrochunkEdgeAxis.HorizontalEdge, "OTHER_BAND", MicrochunkTraversalKind.Climb,
                    0, 0, 0, MicrochunkToolRequirement.None, false, Array.Empty<string>(), string.Empty)
            };
            var result = viewModel.ValidateSocketEdges(signatures);
            Assert.That(result.Violations.Any(value => value.Reason == MicrochunkSocketEdgeValidator.SignatureBandMismatchReason), Is.True);
            Assert.That(result.Violations.Any(value => value.Reason == MicrochunkSocketEdgeValidator.TraversalMismatchReason), Is.True);
        }

        private static void AssertExistingSlotValidatorDetectsBadPool(int variant)
        {
            var viewModel = new MicrochunkSocketAndSlotEditorViewModel();
            viewModel.ObjectSlotAuthoring.Add(Slot("SLOT_" + variant, variant % 12, variant % 8));
            var policy = new MicrochunkObjectSlotValidationPolicy(
                Array.Empty<MicrochunkObjectSlotPoolDefinition>(),
                new[] { "NONE" });
            var result = viewModel.ValidateObjectSlots(policy);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Violations.Any(value => value.Reason == MicrochunkObjectSlotValidator.AllowedPoolIdNotFoundReason), Is.True);
        }

        private static void AssertEditorBoundaryAndNoIoCommands(int variant)
        {
            Assert.That(typeof(EditorWindow).IsAssignableFrom(typeof(MicrochunkSocketAndSlotEditorWindow)), Is.True);
            Assert.That(typeof(MicrochunkSocketAndSlotEditorWindow).Assembly.GetName().Name, Is.EqualTo("MapAuthoring.Editor"));
            Assert.That(typeof(MicrochunkSocketAndSlotEditorViewModel).Assembly, Is.Not.EqualTo(typeof(MicrochunkDefinition).Assembly));
            var productionTypes = new[]
            {
                typeof(MicrochunkSocketAndSlotEditorWindow),
                typeof(MicrochunkSocketAndSlotEditorViewModel),
                typeof(MicrochunkSocketAuthoringCollection),
                typeof(MicrochunkObjectSlotAuthoringCollection)
            };
            var methodNames = productionTypes
                .SelectMany(type => type.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Select(method => method.Name)
                .ToArray();
            Assert.That(methodNames, Has.None.Contains("Import").And.None.Contains("Export").And.None.Contains("Save"));
            Assert.That(productionTypes.Skip(1).Any(type =>
                typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type)), Is.False);

            var scene = EditorSceneManager.GetActiveScene();
            var dirtyBefore = scene.IsValid() && scene.isDirty;
            var viewModel = PopulatedViewModel(variant);
            viewModel.ProjectDefinition();
            viewModel.Validate();
            var dirtyAfter = scene.IsValid() && scene.isDirty;
            Assert.That(dirtyAfter, Is.EqualTo(dirtyBefore));
        }

        private static void AssertFutureBoundaryAndValidationDeterminism(int variant)
        {
            var names = typeof(MicrochunkSocketAndSlotEditorWindow).Assembly.GetTypes().Select(type => type.Name).ToArray();
            foreach (var forbidden in new[]
                     {
                         "MicrochunkCsvExporter", "MicrochunkPreviewReport",
                         "MicrochunkReachabilityHeatmap", "MicrochunkStarterCatalogRoundTrip",
                         "BoundaryChunkResolver", "SectorRecipeResolver", "GeneratedSectorMicrochunkWriter",
                         "PopulationSlotIndex", "StableSpawnId", "WorldTraversalValidator"
                     })
            {
                Assert.That(names, Does.Not.Contain(forbidden));
            }

            var viewModel = PopulatedViewModel(variant);
            var rowsBefore = RowSignature(viewModel);
            var first = ValidationSignature(viewModel.Validate());
            var second = ValidationSignature(viewModel.Validate());
            Assert.That(second, Is.EqualTo(first));
            Assert.That(RowSignature(viewModel), Is.EqualTo(rowsBefore));
        }

        private static MicrochunkSocketAndSlotEditorViewModel PopulatedViewModel(int variant)
        {
            var viewModel = new MicrochunkSocketAndSlotEditorViewModel();
            var bandId = "BAND_" + variant;
            viewModel.SocketAuthoring.AddBand(new MicrochunkSocketBandAuthoringRow(bandId, "L", 0, 0));
            viewModel.SocketAuthoring.AddSocket(Socket("SOCKET_" + variant, "L", bandId));
            viewModel.ObjectSlotAuthoring.Add(new MicrochunkObjectSlotAuthoringRow(
                "SLOT_" + variant,
                (variant + 2) % 12,
                (variant + 3) % 8,
                "RESOURCE",
                "POOL_" + variant));
            return viewModel;
        }

        private static MicrochunkSocketAuthoringRow Socket(
            string socketId,
            string side,
            string bandId = "BAND")
        {
            return new MicrochunkSocketAuthoringRow(socketId, side, bandId, "WALK", "EDGE");
        }

        private static MicrochunkObjectSlotAuthoringRow Slot(
            string slotId,
            int x,
            int y,
            string orientation = "NONE")
        {
            return new MicrochunkObjectSlotAuthoringRow(
                slotId, x, y, "RESOURCE", "POOL", orientation);
        }

        private static string GridSignature(MicrochunkAuthoringGridState state)
        {
            return string.Join("\n", state.Cells.Select(cell =>
                cell.Coordinate.RowMajorIndex + ":" + string.Join("|", cell.TileCodes)));
        }

        private static string DefinitionSignature(MicrochunkDefinition definition)
        {
            return string.Join("|", new[]
            {
                definition.Id.Value,
                definition.TileCells.Count.ToString(),
                string.Join(",", definition.Sockets.Select(socket => socket.SocketId)),
                string.Join(",", definition.ObjectSlots.Select(slot => slot.SlotId))
            });
        }

        private static string RowSignature(MicrochunkSocketAndSlotEditorViewModel viewModel)
        {
            return string.Join("|", new[]
            {
                string.Join(",", viewModel.SocketAuthoring.Bands.Select(row => row.BandId)),
                string.Join(",", viewModel.SocketAuthoring.Sockets.Select(row => row.SocketId)),
                string.Join(",", viewModel.ObjectSlotAuthoring.Rows.Select(row => row.SlotId)),
                GridSignature(viewModel.Grid.State)
            });
        }

        private static string ValidationSignature(MicrochunkSocketAndSlotValidationSummary summary)
        {
            return summary.SocketResult.IssueCount + ":" +
                   string.Join(",", summary.SocketResult.Violations.Select(value => value.SocketId + "/" + value.Reason)) + "|" +
                   summary.ObjectSlotResult.IssueCount + ":" +
                   string.Join(",", summary.ObjectSlotResult.Violations.Select(value => value.SlotId + "/" + value.Reason));
        }
    }
}
