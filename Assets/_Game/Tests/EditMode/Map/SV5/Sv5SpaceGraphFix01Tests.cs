#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_06_FIX01")]
    public sealed class Sv5SpaceGraphFix01Tests
    {
        private static readonly Lazy<Sv5CoreReservationPlan> Core = new Lazy<Sv5CoreReservationPlan>(() =>
            Sv5RouteStatePolicyTests.RepresentativePlanForFix01);
        private static readonly Lazy<Sv5SpaceGraphPlan> Plan = new Lazy<Sv5SpaceGraphPlan>(() =>
            Sv5SpaceGraphPlanner.Plan(Core.Value, 1304));

        [Test]
        public void N01_Old5347AndOmitted4746AreReproducedThenAcceptedCoverageIsComplete()
        {
            IReadOnlyList<Sv5RouteContactPair> oldPairs = Sv5RouteStatePolicy.EnumerateContactPairs(
                ReadOldContactCells(false));
            IReadOnlyList<Sv5RouteContactPair> expandedPairs = Sv5RouteStatePolicy.EnumerateContactPairs(
                ReadOldContactCells(true));
            Assert.That(oldPairs.Count, Is.EqualTo(5347));
            Assert.That(expandedPairs.Count, Is.EqualTo(10093));
            Assert.That(expandedPairs.Count - oldPairs.Count, Is.EqualTo(4746));
            IReadOnlyList<Sv5RouteContactCell> accepted = Sv5SpaceGraphValidator.AcceptedContactCells(Core.Value,
                Plan.Value.Connections);
            Assert.That(Sv5SpaceGraphValidator.FindContactCoverageErrors(accepted,
                Plan.Value.ContactDecisions.Select(value => value.Source)), Is.Empty);
            Assert.That(Plan.Value.ContactDecisions.All(value => value.CoverageChecked), Is.True);
        }

        [Test]
        public void N02_OldFixedSolidAndSupportConflictsAreRejectedWhileAcceptedPlanHasZero()
        {
            IReadOnlyList<string> old = Sv5SpaceGraphValidator.FindRequiredReservationConflicts(Core.Value,
                ReadOldReservationProbes());
            Assert.That(old.Count(value => value.StartsWith("RESERVATION_FIXED_SOLID_CONFLICT|",
                StringComparison.Ordinal)), Is.EqualTo(72));
            Assert.That(old.Count(value => value.StartsWith("RESERVATION_ROUTE_SUPPORT_CONFLICT|",
                StringComparison.Ordinal)), Is.EqualTo(113));
            Assert.That(old.Any(value => value.Contains("492,280|SV5_OPTIONAL_TO_VILLAGE")), Is.True);
            Assert.That(Sv5SpaceGraphValidator.FindReservationConflicts(Core.Value, Plan.Value.Reservations), Is.Empty);
        }

        [Test]
        public void N03_PortFlowDirectionAndFullWidthFixturesRejectMalformedInputs()
        {
            Sv5SpaceConnection connection = Plan.Value.Connections.First(value =>
                value.Kind == Sv5SpaceConnectionKind.OptionalBranch);
            Assert.That(Validate(connection, connection.FromPortId, connection.Flow, connection.Direction,
                connection.Envelope), Is.Empty);
            Assert.That(Validate(connection, "UNKNOWN_PORT", connection.Flow, connection.Direction,
                connection.Envelope), Does.Contain("CONNECTION_UNKNOWN_PORT|FIXTURE"));
            Assert.That(Validate(connection, connection.FromPortId, "SIDEWAYS", connection.Direction,
                connection.Envelope), Has.Some.StartsWith("CONNECTION_UNKNOWN_FLOW|"));
            Assert.That(Validate(connection, connection.FromPortId, connection.Flow, Opposite(connection.Direction),
                connection.Envelope), Has.Some.StartsWith("CONNECTION_DIRECTION_MISMATCH|"));
            IReadOnlyList<string> narrow = Validate(connection, connection.FromPortId, connection.Flow,
                connection.Direction, connection.Centerline.Concat(connection.ApertureCells));
            Assert.That(narrow.Any(value => value.StartsWith("CONNECTION_ENVELOPE_INCOMPLETE|",
                StringComparison.Ordinal) || value.StartsWith("CONNECTION_REQUIRED_WIDTH_NARROW|",
                StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void N04_DeletingAnActualCoreConnectorFailsProductionProjection()
        {
            Sv5SpaceConnection removed = Plan.Value.Connections.First(value =>
                value.Kind == Sv5SpaceConnectionKind.CoreProgression);
            IReadOnlyList<string> errors = Sv5SpaceGraphStateProjection.ValidateAcceptedProjection(Core.Value,
                Plan.Value.Connections.Where(value => value != removed));
            Assert.That(errors, Has.Some.StartsWith("MISSING_ACTUAL_CORE_CONNECTOR|"));
            Assert.That(Sv5SpaceGraphStateProjection.ValidateAcceptedProjection(Core.Value,
                Plan.Value.Connections), Is.Empty);
        }

        [Test]
        public void N05_SideBypassFixtureFailsAndAcceptedFullBoundaryPassesOpenSealedChecks()
        {
            Assert.That(Plan.Value.ContactDecisions.All(value => value.Crossing == Sv5SpaceCrossingKind.Join ||
                value.Crossing == Sv5SpaceCrossingKind.ConditionalGate), Is.True);
            // FIX04 may reroute all cross-route gated contacts away. Preserve the gate
            // requirement through the actual three necks and all closed/open witnesses.
            Assert.That(Plan.Value.Gates.Count, Is.EqualTo(3));
            Assert.That(Plan.Value.PhysicalMovement.GateStateChecks.Count, Is.EqualTo(9));
            Assert.That(Plan.Value.Gates.All(value => value.BlockingFaces.Count > 0 &&
                value.SealedState.Contains("GLOBAL_WORLD_FACE_CUT") &&
                value.OpenState.Contains("GLOBAL_WORLD_FACES")), Is.True);
            Assert.That(Sv5SpaceGateGeometry.FindStateErrors(Plan.Value.Core, Plan.Value.Connections,
                Plan.Value.Gates, Plan.Value.GateStateChecks), Is.Empty);
            Assert.That(Plan.Value.PhysicalMovement.Success, Is.True);
        }

        [Test]
        public void N06_SixOrdersAndEveryReachableStateRetainNormalReturn()
        {
            Assert.That(Plan.Value.ProjectionProofs.Count, Is.EqualTo(6));
            Assert.That(Plan.Value.ProjectionProofs.All(value => value.Success &&
                value.ReachableStates == value.ReverseReachableStates && value.DeadEnds.Count == 0), Is.True);
            Assert.That(Plan.Value.ProjectionProofs.All(value => value.GoalProof.Actions.Count(action =>
                action.StartsWith("ACQUIRE|", StringComparison.Ordinal)) == 3), Is.True);
            Assert.That(Plan.Value.Connections.Where(value => value.Kind != Sv5SpaceConnectionKind.CoreProgression)
                .All(value => value.Flow == "BIDIRECTIONAL"), Is.True);
        }

        [Test]
        public void N07_DigestBindsPortEnvelopeFlowGateAndSetOrderSemantics()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Assert.That(plan.Digest, Is.Not.EqualTo("fbbc847599b34b8b4695628d6e4970d0669938e04e35eb4b08220ef78d31cf84"));
            string graph = Sv5SpaceGraphExport.SpaceGraphJson(plan);
            string gates = Sv5SpaceGraphExport.GateGeometryJson(plan);
            Assert.That(graph, Does.Contain("\"aperture_cells\"").And.Contain("\"flow\":\"BIDIRECTIONAL\""));
            Assert.That(gates, Does.Contain("\"blocking_faces\"").And.Contain("\"sealed_state\""));
            Sv5SpaceGraphAuthoringProfile reordered = new Sv5SpaceGraphAuthoringProfile(plan.Profile.Id,
                plan.Profile.Version, plan.Profile.Families.Reverse());
            Assert.That(reordered.Digest, Is.EqualTo(plan.Profile.Digest));
            Sv5SpaceFamilySpec first = plan.Profile.Families.First();
            var changedFamilies = plan.Profile.Families.Skip(1).Concat(new[]
            {
                new Sv5SpaceFamilySpec(first.Ordinal, first.Family, first.Kind, first.Width + 4,
                    first.Height, first.FutureOwner),
            });
            Assert.That(new Sv5SpaceGraphAuthoringProfile(plan.Profile.Id, plan.Profile.Version, changedFamilies).Digest,
                Is.Not.EqualTo(plan.Profile.Digest));
        }

        [Test]
        public void N08_OneAcceptedPlanWritesConsistentEvidenceWithoutChangingHistoricalBytes()
        {
            string[] preserved =
            {
                Historical("GENERATED", "SV5_04", "validation.json"),
                Historical("GENERATED", "SV5_05_FIX01", "validation.json"),
                Historical("GENERATED", "SV5_06", "validation.json"),
                Historical("REPORTS", "SV5_06_SPACE_GRAPH_RESULT.md"),
            };
            string[] before = preserved.Select(HashFile).ToArray();
            string directory = GeneratedDirectory();
            Sv5SpaceGraphExport.WriteAll(Plan.Value, directory);
            Assert.That(preserved.Select(HashFile), Is.EqualTo(before));
            string[] required =
            {
                "space_graph.json", "reservation_cells.csv", "contact_checks.csv", "gate_geometry.json",
                "state_proofs.json", "validation.json", "obligations.csv",
            };
            Assert.That(required.All(value => File.Exists(Path.Combine(directory, value)) &&
                new FileInfo(Path.Combine(directory, value)).Length > 0), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(directory, "validation.json")),
                Does.Contain("\"status\": \"PASS\"").And.Contain(Plan.Value.Digest));
            Assert.That(Directory.GetFiles(Path.Combine(directory, "preview"), "*.svg").Length, Is.EqualTo(20));
            TestContext.Out.WriteLine("SV5_06_FIX01_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + Plan.Value.Digest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + directory);
            TestContext.Out.WriteLine("SV5_06_FIX01_EXPORT_END");
        }

        private static IReadOnlyList<string> Validate(Sv5SpaceConnection value, string fromPort, string flow,
            RmapWorldGraphDirection direction, IEnumerable<RmapSpecialWorldPoint> envelope) =>
            Sv5SpaceGraphValidator.ValidatePortTransitionFixture(Plan.Value.Ports, fromPort, value.ToPortId,
                value.FromPlaceId, value.ToPlaceId, flow, direction, value.Centerline, envelope,
                value.ApertureCells);

        private static Sv5RouteContactCell[] ReadOldContactCells(bool includeCorridorClearance)
        {
            var cells = new List<Sv5RouteContactCell>();
            foreach (string line in File.ReadAllLines(Historical("GENERATED", "SV5_06", "reservation_cells.csv")).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] fields = line.Split(',');
                var world = new RmapSpecialWorldPoint(I(fields[0]), I(fields[1]));
                if (fields[2] == "CoreRoute" && fields[4].StartsWith("Passage:", StringComparison.Ordinal))
                    cells.Add(new Sv5RouteContactCell(fields[3], world, Sv5RouteContactCellKind.Passage));
                else if (fields[2] == "CoreRoute" && fields[4].StartsWith("Clearance:", StringComparison.Ordinal))
                    cells.Add(new Sv5RouteContactCell(fields[3], world, Sv5RouteContactCellKind.Clearance));
                else if (includeCorridorClearance && fields[2] == "CorridorClearance")
                    cells.Add(new Sv5RouteContactCell(fields[3], world, Sv5RouteContactCellKind.Clearance));
            }
            foreach (string line in File.ReadAllLines(Historical("GENERATED", "SV5_06", "connections.csv")).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] fields = line.Split(',');
                string route = fields[1] == "CoreProgression" ? fields[9] : fields[0];
                foreach (string cell in fields[12].Split('|'))
                {
                    string[] xy = cell.Split(':');
                    cells.Add(new Sv5RouteContactCell(route, new RmapSpecialWorldPoint(I(xy[0]), I(xy[1])),
                        Sv5RouteContactCellKind.Passage));
                }
            }
            return cells.ToArray();
        }

        private static Sv5SpaceReservationProbe[] ReadOldReservationProbes() => File.ReadAllLines(
                Historical("GENERATED", "SV5_06", "reservation_cells.csv")).Skip(1)
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Split(','))
            .Where(value => value[2] == "CorridorCenterline" || value[2] == "CorridorClearance")
            .Select(value => new Sv5SpaceReservationProbe(new RmapSpecialWorldPoint(I(value[0]), I(value[1])),
                value[2] == "CorridorCenterline" ? Sv5SpaceReservationKind.CorridorCenterline :
                Sv5SpaceReservationKind.CorridorClearance, value[3])).ToArray();

        private static RmapWorldGraphDirection Opposite(RmapWorldGraphDirection value) =>
            value == RmapWorldGraphDirection.Left ? RmapWorldGraphDirection.Right :
            value == RmapWorldGraphDirection.Right ? RmapWorldGraphDirection.Left :
            value == RmapWorldGraphDirection.Up ? RmapWorldGraphDirection.Down : RmapWorldGraphDirection.Up;
        private static int I(string value) => int.Parse(value, CultureInfo.InvariantCulture);
        private static string Historical(params string[] parts) => parts.Aggregate(
            Path.Combine(ProjectRoot(), "MapDesign", "MCP"), Path.Combine);
        private static string GeneratedDirectory() => Historical("GENERATED", "SV5_08_FIX01", "_work", "legacy_exports",
            "sv5_06_fix01_n08");
        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path))
                .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
#endif
