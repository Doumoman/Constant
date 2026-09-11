#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_06_FIX02")]
    public sealed class Sv5SpaceGraphFix02Tests
    {
        private static readonly Lazy<Sv5CoreReservationPlan> Core = new Lazy<Sv5CoreReservationPlan>(() =>
            Sv5RouteStatePolicyTests.RepresentativePlanForFix01);
        private static readonly Lazy<Sv5SpaceGraphPlan> Plan = new Lazy<Sv5SpaceGraphPlan>(() =>
            Sv5SpaceGraphPlanner.Plan(Core.Value, 1304));

        [Test]
        public void G01_W01OldSealEntryBlockIsReproducedAndForgeStateOpensOwnedGate()
        {
            string findings = Compact(File.ReadAllText(Input("REVIEW_FINDINGS.json")));
            Assert.That(findings, Does.Contain("SV5_GATE_F3608BF3D1EE0923F69A")
                .And.Contain("[533,311]").And.Contain("[533,312]").And.Contain("[533,313]"));
            Sv5SpaceGateStateCheck witness = Plan.Value.GateStateChecks.Single(value =>
                value.Id == "W01_SEAL_ENTRY_REACHABLE");
            Sv5SpaceGate gate = Plan.Value.Gates.Single(value => value.Id == witness.GateId);
            Assert.That(witness.Success && witness.ExpectedOpen && witness.ActualOpen &&
                witness.SourceAnchorReachable && witness.TargetPortReachable && witness.OpenPathVerified, Is.True);
            Assert.That(gate.TypedPredicate.RequiresForge, Is.True);
            Assert.That(gate.TypedPredicate.RequiresSeal, Is.False);
            Assert.That(gate.SourcePortId, Is.EqualTo("RMAP15_SITE_FORGE_PORT_EXIT"));
            Assert.That(gate.TargetPortId, Is.EqualTo("RMAP15_SITE_SEALBOSS_PORT_ENTRY"));
            Assert.That(Plan.Value.Gates.Any(value => value.Id == "SV5_GATE_F3608BF3D1EE0923F69A"), Is.False);
        }

        [Test]
        public void G02_W02BossApproachOpensButExitRemainsSealedUntilBossComplete()
        {
            string findings = Compact(File.ReadAllText(Input("REVIEW_FINDINGS.json")));
            Assert.That(findings, Does.Contain("SV5_GATE_3699556D982E455C6E7E")
                .And.Contain("[560,311]").And.Contain("[560,312]").And.Contain("[560,313]"));
            Sv5SpaceGateStateCheck approach = Plan.Value.GateStateChecks.Single(value =>
                value.Id == "W02_BOSS_APPROACH_REACHABLE");
            Sv5SpaceGateStateCheck exit = Plan.Value.GateStateChecks.Single(value =>
                value.Id == "W02_EXIT_REMAINS_SEALED");
            Assert.That(approach.Success && approach.ExpectedOpen && approach.TargetPortReachable, Is.True);
            Assert.That(exit.Success && !exit.ExpectedOpen && !exit.ActualOpen &&
                !exit.TargetPortReachable && exit.SealedCutVerified && exit.OpenPathVerified, Is.True);
            Sv5SpaceGate exitGate = Plan.Value.Gates.Single(value => value.Id == exit.GateId);
            Assert.That(exitGate.TypedPredicate.RequiresBossComplete, Is.True);
            Assert.That(exitGate.SourcePortId, Is.EqualTo("RMAP15_SITE_SEALBOSS_PORT_EXIT"));
            Assert.That(exitGate.TargetPortId, Is.EqualTo("RMAP15_SITE_EXIT_PORT_ENTRY"));
        }

        [Test]
        public void G03_TruncatedCutAllowsSideBypassWhileAcceptedFullCutsPassAllStates()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Assert.That(Sv5SpaceGateGeometry.FindStateErrors(plan.Core, plan.Connections, plan.Gates,
                plan.GateStateChecks), Is.Empty);
            Assert.That(plan.GateStateChecks.All(value => value.Success), Is.True);
            Sv5SpaceGate source = plan.Gates.OrderByDescending(value => value.BlockingFaces.Count).First();
            // FIX04 reroutes to a single-face neck; replacing that face with a remote
            // one must still reproduce the side bypass (no distributed-count assumption).
            Assert.That(source.BlockingFaces.Count, Is.EqualTo(1));
            Sv5SpaceBoundaryFace foreign = plan.Gates.First(value => value.Id != source.Id).BlockingFaces.First();
            Sv5SpaceGate truncated = CloneGate(source, new[] { foreign }, source.TypedPredicate,
                source.SealedState, source.OpenState);
            Sv5SpaceGate[] gates = plan.Gates.Select(value => value.Id == source.Id ? truncated : value).ToArray();
            Assert.That(Sv5SpaceGateGeometry.FindStateErrors(plan.Core, plan.Connections, gates,
                plan.GateStateChecks), Has.Some.EqualTo("GATE_STATE_SIDE_BYPASS|" + source.Id));
        }

        [Test]
        public void G04_ConditionalGateProtectedAirConflictRejectsCellOwnershipButAllowsFaceBoundary()
        {
            RmapSpecialWorldPoint protectedAir = Core.Value.CoreCells.First(value =>
                value.Protection == RmapSpecialProtectionKind.ProtectedAir).World;
            Assert.That(Sv5SpaceGraphValidator.FindRequiredReservationConflicts(Core.Value, new[]
            {
                new Sv5SpaceReservationProbe(protectedAir, Sv5SpaceReservationKind.ConditionalGate,
                    "NEGATIVE_CELL_GATE", "CELL_BLOCK|SEALED"),
            }), Has.Some.StartsWith("RESERVATION_PROTECTED_AIR_STATE_CONFLICT|"));
            Assert.That(Sv5SpaceGraphValidator.FindRequiredReservationConflicts(Core.Value, new[]
            {
                new Sv5SpaceReservationProbe(protectedAir, Sv5SpaceReservationKind.ConditionalGate,
                    "FACE_GATE", "FACE_ONLY_STATE_BOUNDARY|SEALED"),
            }), Is.Empty);
            Assert.That(Plan.Value.Gates.All(value => value.BlockingCells.Count == 0), Is.True);
            Assert.That(Sv5SpaceGraphValidator.FindReservationConflicts(Core.Value, Plan.Value.Reservations),
                Is.Empty);
        }

        [Test]
        public void G05_ProductionConnectionValidatorRejectsPortsFlowDirectionAndDisconnectedAperture()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Assert.That(Sv5SpaceGraphValidator.FindConnectionErrors(plan.Ports, plan.Connections), Is.Empty);
            Sv5SpaceConnection connection = plan.Connections.First(value =>
                value.Kind == Sv5SpaceConnectionKind.OptionalBranch && value.Centerline.Count > 8);
            RmapSpecialWorldPoint isolated = connection.Centerline[connection.Centerline.Count / 2];
            RmapSpecialWorldPoint[] aperture =
            {
                connection.Centerline.First(), isolated, connection.Centerline.Last(),
            };
            Assert.That(Validate(connection, connection.FromPortId, connection.Flow, connection.Direction,
                connection.Envelope, aperture), Has.Some.StartsWith("CONNECTION_APERTURE_NOT_PORT_REACHABLE|"));
            Assert.That(Validate(connection, "UNKNOWN_PORT", connection.Flow, connection.Direction,
                connection.Envelope, connection.ApertureCells), Has.Some.StartsWith("CONNECTION_UNKNOWN_PORT|"));
            Assert.That(Validate(connection, connection.FromPortId, "SIDEWAYS", connection.Direction,
                connection.Envelope, connection.ApertureCells), Has.Some.StartsWith("CONNECTION_UNKNOWN_FLOW|"));
            Assert.That(Validate(connection, connection.FromPortId, connection.Flow,
                Opposite(connection.Direction), connection.Envelope, connection.ApertureCells),
                Has.Some.StartsWith("CONNECTION_DIRECTION_MISMATCH|"));
        }

        [Test]
        public void G06_DeletedConnectorAndOneWayOptionalFailWhileSixOrdersAllRecover()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Sv5SpaceConnection removed = plan.Connections.First(value =>
                value.Kind == Sv5SpaceConnectionKind.CoreProgression);
            Assert.That(Sv5SpaceGraphStateProjection.ValidateAcceptedProjection(plan.Core,
                plan.Connections.Where(value => value.Id != removed.Id)),
                Has.Some.StartsWith("MISSING_ACTUAL_CORE_CONNECTOR|"));
            Sv5SpaceConnection optional = plan.Connections.First(value =>
                value.Kind == Sv5SpaceConnectionKind.OptionalBranch);
            Sv5SpaceConnection oneWay = CloneConnection(optional, "ONE_WAY", optional.Envelope);
            Assert.That(Sv5SpaceGraphValidator.FindConnectionErrors(plan.Ports, new[] { oneWay }),
                Has.Some.StartsWith("CONNECTION_OPTIONAL_NOT_BIDIRECTIONAL|"));
            Assert.That(plan.ProjectionProofs.Count, Is.EqualTo(6));
            Assert.That(plan.ProjectionProofs.All(value => value.Success &&
                value.ReachableStates == value.ReverseReachableStates && value.DeadEnds.Count == 0), Is.True);
        }

        [Test]
        public void G07_DigestBindsPortFlowEnvelopeGatePredicateAndStatesButNotSetOrder()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            var digests = new HashSet<string>(StringComparer.Ordinal) { plan.Digest };
            Sv5SpacePort port = plan.Ports.First(value => value.BoundaryCells.Count > 1);
            RmapSpecialWorldPoint[] boundary = port.BoundaryCells.Where(value => !value.Equals(port.Anchor))
                .Skip(1).Concat(new[] { port.Anchor }).ToArray();
            digests.Add(ClonePlan(plan, ports: Replace(plan.Ports, port.Id, ClonePort(port, boundary))).Digest);

            Sv5SpaceConnection connection = plan.Connections.First(value =>
                value.Kind == Sv5SpaceConnectionKind.OptionalBranch);
            digests.Add(ClonePlan(plan, connections: Replace(plan.Connections, connection.Id,
                CloneConnection(connection, "ONE_WAY", connection.Envelope))).Digest);
            RmapSpecialWorldPoint removable = connection.Envelope.First(value =>
                !connection.Centerline.Contains(value) && !connection.ApertureCells.Contains(value));
            digests.Add(ClonePlan(plan, connections: Replace(plan.Connections, connection.Id,
                CloneConnection(connection, connection.Flow, connection.Envelope.Where(value =>
                    !value.Equals(removable))))).Digest);

            Sv5SpaceGate gate = plan.Gates.First();
            var predicate = new Sv5SpaceGatePredicate(gate.TypedPredicate.RequiredResourceMask ^ 1UL,
                gate.TypedPredicate.RequiresForge, gate.TypedPredicate.RequiresSeal,
                gate.TypedPredicate.RequiresBossComplete);
            digests.Add(ClonePlan(plan, gates: Replace(plan.Gates, gate.Id, CloneGate(gate,
                gate.BlockingFaces, predicate, gate.SealedState, gate.OpenState))).Digest);
            digests.Add(ClonePlan(plan, gates: Replace(plan.Gates, gate.Id, CloneGate(gate,
                gate.BlockingFaces, gate.TypedPredicate, gate.SealedState + "_MUTATED",
                gate.OpenState))).Digest);
            Assert.That(digests.Count, Is.EqualTo(6));
            Assert.That(new Sv5SpaceGraphAuthoringProfile(plan.Profile.Id, plan.Profile.Version,
                plan.Profile.Families.Reverse()).Digest, Is.EqualTo(plan.Profile.Digest));
        }

        [Test]
        public void G08_OneAcceptedPlanExportsDigestConsistentEvidenceAndPreservesPriorBytes()
        {
            string[] preserved =
            {
                Historical("GENERATED", "SV5_06_FIX01", "validation.json"),
                Historical("GENERATED", "SV5_06_FIX01", "gate_geometry.json"),
                Historical("REPORTS", "SV5_06_FIX01_RESULT.md"),
                Historical("TASKS", "SV5_06_FIX01.md"),
            };
            string[] before = preserved.Select(HashFile).ToArray();
            string output = GeneratedDirectory();
            Sv5SpaceGraphExport.WriteAll(Plan.Value, output);
            Assert.That(preserved.Select(HashFile), Is.EqualTo(before));
            string[] required =
            {
                "space_graph.json", "places.csv", "ports.csv", "connections.csv", "reservation_cells.csv",
                "contact_checks.csv", "gate_geometry.json", "gate_state_checks.json", "state_proofs.json",
                "validation.json", "obligations.csv",
            };
            Assert.That(required.All(value => File.Exists(Path.Combine(output, value)) &&
                new FileInfo(Path.Combine(output, value)).Length > 0), Is.True);
            Assert.That(new[] { "space_graph.json", "gate_geometry.json", "gate_state_checks.json",
                "state_proofs.json", "validation.json" }.All(value => File.ReadAllText(Path.Combine(output, value))
                    .Contains(Plan.Value.Digest)), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(output, "validation.json")),
                Does.Contain("\"status\": \"PASS\"").And.Contain("\"composed_geometry_ready\": false")
                    .And.Contain("\"player_verified\": false"));
            Assert.That(File.ReadAllLines(Path.Combine(output, "connections.csv")).Length - 1,
                Is.EqualTo(Plan.Value.Connections.Count));
            Assert.That(File.ReadAllLines(Path.Combine(output, "contact_checks.csv")).Length - 1,
                Is.EqualTo(Plan.Value.ContactDecisions.Count));
            string preview = Path.Combine(output, "preview");
            Assert.That(Directory.GetFiles(preview, "*.svg").Length, Is.EqualTo(20));
            Assert.That(Directory.GetFiles(preview, "*.svg").All(value =>
                File.ReadAllText(value).Contains(Plan.Value.Digest)), Is.True);
            TestContext.Out.WriteLine("SV5_06_FIX02_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + Plan.Value.Digest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + output);
            TestContext.Out.WriteLine("SV5_06_FIX02_EXPORT_END");
        }

        private static IReadOnlyList<string> Validate(Sv5SpaceConnection value, string fromPort, string flow,
            RmapWorldGraphDirection direction, IEnumerable<RmapSpecialWorldPoint> envelope,
            IEnumerable<RmapSpecialWorldPoint> aperture) => Sv5SpaceGraphValidator.ValidatePortTransitionFixture(
                Plan.Value.Ports, fromPort, value.ToPortId, value.FromPlaceId, value.ToPlaceId, flow, direction,
                value.Centerline, envelope, aperture);

        private static Sv5SpacePort ClonePort(Sv5SpacePort value, IEnumerable<RmapSpecialWorldPoint> boundary) =>
            Create<Sv5SpacePort>(value.Id, value.PlaceId, boundary, value.Anchor, value.Direction, value.Flow,
                value.Condition, value.SourceAccessId, value.SourceNodeId, value.Status);

        private static Sv5SpaceConnection CloneConnection(Sv5SpaceConnection value, string flow,
            IEnumerable<RmapSpecialWorldPoint> envelope) => Create<Sv5SpaceConnection>(value.Id, value.Kind,
                value.FromPortId, value.ToPortId, value.FromPlaceId, value.ToPlaceId, value.Direction, flow,
                value.Condition, value.SourceGraphEdgeId, value.SelectionState, value.Centerline, envelope,
                value.ApertureCells);

        private static Sv5SpaceGate CloneGate(Sv5SpaceGate value,
            IEnumerable<Sv5SpaceBoundaryFace> blockingFaces, Sv5SpaceGatePredicate predicate,
            string sealedState, string openState) => Create<Sv5SpaceGate>(value.Id, value.BoundaryId,
                value.ContactIds, value.BlockingCells, blockingFaces, value.SideAAnchor, value.SideBAnchor,
                value.Direction, value.Flow, value.Predicate, predicate, value.SourceConnectionId,
                value.SourceRouteId, value.SourcePortId, value.TargetPortId, value.Crossing, sealedState, openState);

        private static Sv5SpaceGraphPlan ClonePlan(Sv5SpaceGraphPlan value,
            IEnumerable<Sv5SpacePort> ports = null, IEnumerable<Sv5SpaceConnection> connections = null,
            IEnumerable<Sv5SpaceGate> gates = null) => Create<Sv5SpaceGraphPlan>(value.Core, value.Seed,
                value.Profile, value.Places, ports ?? value.Ports, connections ?? value.Connections,
                gates ?? value.Gates, value.Reservations, value.ContactDecisions, value.ProjectionProofs,
                value.GateStateChecks, value.Diagnostics);

        private static T[] Replace<T>(IEnumerable<T> source, string id, T replacement)
        {
            PropertyInfo property = typeof(T).GetProperty("Id");
            return source.Select(value => string.Equals((string)property.GetValue(value), id,
                StringComparison.Ordinal) ? replacement : value).ToArray();
        }

        private static T Create<T>(params object[] arguments) => (T)Activator.CreateInstance(typeof(T),
            BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, CultureInfo.InvariantCulture);

        private static RmapWorldGraphDirection Opposite(RmapWorldGraphDirection value) =>
            value == RmapWorldGraphDirection.Left ? RmapWorldGraphDirection.Right :
            value == RmapWorldGraphDirection.Right ? RmapWorldGraphDirection.Left :
            value == RmapWorldGraphDirection.Up ? RmapWorldGraphDirection.Down : RmapWorldGraphDirection.Up;
        private static string Input(string file) => Path.Combine(ProjectRoot(), "MapDesign", "MCP", "INPUTS",
            "SV5_06_FIX02", file);
        private static string Compact(string value) => value.Replace(" ", string.Empty).Replace("\r", string.Empty)
            .Replace("\n", string.Empty).Replace("\t", string.Empty);
        private static string Historical(params string[] parts) => parts.Aggregate(
            Path.Combine(ProjectRoot(), "MapDesign", "MCP"), Path.Combine);
        private static string GeneratedDirectory() => Historical("GENERATED", "SV5_07", "_work", "legacy_exports",
            "sv5_06_fix02_g08");
        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path))
                .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
#endif
