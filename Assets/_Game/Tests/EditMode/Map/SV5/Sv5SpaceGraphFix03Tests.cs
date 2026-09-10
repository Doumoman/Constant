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
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_06_FIX03")]
    public sealed class Sv5SpaceGraphFix03Tests
    {
        private static readonly Lazy<Sv5CoreReservationPlan> Core = new Lazy<Sv5CoreReservationPlan>(() =>
            Sv5RouteStatePolicyTests.RepresentativePlanForFix01);
        private static readonly Lazy<Sv5SpaceGraphPlan> Plan = new Lazy<Sv5SpaceGraphPlan>(() =>
            Sv5SpaceGraphPlanner.Plan(Core.Value, 1304));

        [Test]
        public void G01_ExactFix02CounterexampleIsRecordedAndAllNineProductionStatesPass()
        {
            string findings = Compact(File.ReadAllText(Input("REVIEW_FINDINGS.json")));
            Assert.That(findings, Does.Contain("\"contacts\":7188")
                .And.Contain("\"separated_contacts\":225").And.Contain("\"FACE\":164")
                .And.Contain("\"SHARED\":61").And.Contain("\"separated_with_passage_on_both_routes\":93"));
            Assert.That(Plan.Value.PhysicalMovement.GateStateChecks.Count, Is.EqualTo(9));
            Assert.That(Plan.Value.PhysicalMovement.GateStateChecks.Where(value => !value.ExpectedReachable)
                .Count(), Is.EqualTo(6));
            Assert.That(Plan.Value.PhysicalMovement.GateStateChecks.Where(value => !value.ExpectedReachable)
                .All(value => !value.Reachable && value.Success), Is.True);
            Assert.That(Plan.Value.PhysicalMovement.GateStateChecks.Where(value => value.ExpectedReachable)
                .All(value => value.Reachable && value.Success), Is.True);
        }

        [Test]
        public void G02_DifferentPredicateSharedContactCannotBypassButSamePredicateJoinRemainsTraversable()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Sv5SpacePhysicalContactCheck guarded = plan.PhysicalMovement.ContactChecks.First(value =>
                value.Source.Source.Kind == "SHARED" &&
                value.Source.Crossing == Sv5SpaceCrossingKind.ConditionalGate);
            Sv5SpacePhysicalContactCheck joined = plan.PhysicalMovement.ContactChecks.First(value =>
                value.Source.Source.Kind == "SHARED" && value.Source.Crossing == Sv5SpaceCrossingKind.Join &&
                value.FirstTraversable && value.SecondTraversable);
            Assert.That(guarded.Success && guarded.GeometryOwnerId.Length > 0, Is.True);
            Assert.That(joined.Success && joined.GeometryOwnerId.Length == 0, Is.True);
            Sv5SpaceGate owner = plan.Gates.Single(value => value.Id == guarded.GeometryOwnerId);
            Sv5SpaceGate weakened = CloneGate(owner, new[] { ForeignFace(plan, owner) });
            Assert.That(Sv5SpacePhysicalMovement.FindStateErrors(plan.Core, plan.Connections,
                Replace(plan.Gates, owner.Id, weakened)), Has.Some.StartsWith("GLOBAL_GATE_STATE_BYPASS|"));
            Assert.That(Sv5SpacePhysicalMovement.FindStateErrors(plan.Core, plan.Connections, plan.Gates), Is.Empty);
        }

        [Test]
        public void G03_DifferentPredicateFaceRequiresRealGlobalBoundaryGeometry()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Sv5SpacePhysicalContactCheck guarded = plan.PhysicalMovement.ContactChecks.First(value =>
                value.Source.Source.Kind == "FACE" &&
                value.Source.Crossing == Sv5SpaceCrossingKind.ConditionalGate);
            Sv5SpaceGate owner = plan.Gates.Single(value => value.Id == guarded.GeometryOwnerId);
            Assert.That(owner.BlockingFaces.Count, Is.GreaterThan(1));
            Assert.That(Sv5SpaceGraphValidator.ValidateBarrierFixture(guarded.Source.Source,
                Array.Empty<RmapSpecialWorldPoint>(), new[]
                {
                    Create<Sv5SpaceBoundaryFace>(guarded.Source.Source.FirstWorld,
                        guarded.Source.Source.SecondWorld),
                }), Is.Empty);
            Sv5SpaceGate weakened = CloneGate(owner, new[] { ForeignFace(plan, owner) });
            Assert.That(Sv5SpacePhysicalMovement.FindStateErrors(plan.Core, plan.Connections,
                Replace(plan.Gates, owner.Id, weakened)), Is.Not.Empty);
        }

        [Test]
        public void G04_RouteLabelsAndSetOrderDoNotChangeCoordinateReachability()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Sv5SpaceGate[] renamed = plan.Gates.Select(value => CloneGate(value, value.BlockingFaces,
                predicateLabel: "RENAMED_DIAGNOSTIC_LABEL", routeLabel: "RENAMED_" + value.Id)).ToArray();
            foreach (Sv5SpacePhysicalGateStateCheck check in plan.PhysicalMovement.GateStateChecks)
            {
                Sv5SpacePhysicalReachability reordered = Sv5SpacePhysicalMovement.Evaluate(plan.Core,
                    plan.Connections.Reverse(), plan.Gates.Reverse(), check.ConnectionId, check.ResourceMask,
                    check.ForgeMade, check.SealOpen, check.BossComplete);
                Sv5SpacePhysicalReachability relabeled = Sv5SpacePhysicalMovement.Evaluate(plan.Core,
                    plan.Connections, renamed, check.ConnectionId, check.ResourceMask, check.ForgeMade,
                    check.SealOpen, check.BossComplete);
                Assert.That(reordered.TargetPortReachable, Is.EqualTo(check.Reachable), check.Id);
                Assert.That(relabeled.TargetPortReachable, Is.EqualTo(check.Reachable), check.Id);
            }
        }

        [Test]
        public void G05_EmptyFakeAndSharedFaceOnlyBoundariesAreRejected()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Sv5SpaceContactDecision shared = plan.ContactDecisions.First(value =>
                value.Source.Kind == "SHARED" && value.Crossing == Sv5SpaceCrossingKind.ConditionalGate);
            Sv5SpaceGate owner = plan.Gates.Single(value => value.BoundaryId == shared.BoundaryId);
            Assert.That(Sv5SpaceGraphValidator.ValidateBarrierFixture(shared.Source,
                Array.Empty<RmapSpecialWorldPoint>(), owner.BlockingFaces),
                Has.Some.EqualTo("GATE_SHARED_CELL_BYPASS|" + shared.Source.Id));
            Sv5SpaceContactDecision fake = Create<Sv5SpaceContactDecision>(shared.Source, shared.SplitNodeId,
                shared.Crossing, shared.Predicate, "FAKE_BOUNDARY", true, true, shared.Detail);
            Assert.That(Sv5SpaceGraphValidator.FindGateErrors(new[] { fake }, plan.Gates),
                Has.Some.EqualTo("GATE_CONTACT_UNCOVERED|" + shared.Source.Id));
        }

        [Test]
        public void G06_BlockingIsGlobalAndEveryStateAppliesAllGatesSimultaneously()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            Assert.That(plan.PhysicalMovement.GateStateChecks.All(value =>
                value.ClosedGateIds.Count + value.OpenGateIds.Count == plan.Gates.Count), Is.True);
            Assert.That(plan.PhysicalMovement.GateStateChecks.Where(value => !value.ExpectedReachable)
                .All(value => value.ClosedGateIds.Count >= 1 && !value.Reachable), Is.True);
            Assert.That(plan.PhysicalMovement.GateStateChecks.Any(value => value.ClosedGateIds.Count > 1), Is.True);
            Sv5SpaceGate source = plan.Gates.OrderByDescending(value => value.BlockingFaces.Count).First();
            Sv5SpaceGate routeRelabeled = CloneGate(source, source.BlockingFaces,
                routeLabel: "UNRELATED_ROUTE_OWNER");
            Sv5SpacePhysicalReachability original = Sv5SpacePhysicalMovement.Evaluate(plan.Core,
                plan.Connections, plan.Gates, source.SourceConnectionId, 0, false, false, false);
            Sv5SpacePhysicalReachability relabeled = Sv5SpacePhysicalMovement.Evaluate(plan.Core,
                plan.Connections, Replace(plan.Gates, source.Id, routeRelabeled), source.SourceConnectionId,
                0, false, false, false);
            Assert.That(relabeled.TargetPortReachable, Is.EqualTo(original.TargetPortReachable));
        }

        [Test]
        public void G07_PhysicalDigestBindsContactBoundaryGateAndStateButIgnoresSetOrder()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            string original = plan.PhysicalMovement.SemanticDigest;
            string reversed = Sv5SpacePhysicalMovement.Analyze(plan.Core, plan.Connections.Reverse(),
                plan.ContactDecisions.Reverse(), plan.Gates.Reverse()).SemanticDigest;
            Assert.That(reversed, Is.EqualTo(original));

            Sv5SpaceContactDecision contact = plan.ContactDecisions.First(value =>
                value.Crossing == Sv5SpaceCrossingKind.ConditionalGate);
            Sv5SpaceContactDecision changedContact = Create<Sv5SpaceContactDecision>(contact.Source,
                contact.SplitNodeId, Sv5SpaceCrossingKind.Join, contact.Predicate, string.Empty, true, true,
                contact.Detail);
            string contactDigest = Analyze(plan, contacts: Replace(plan.ContactDecisions,
                contact.Source.Id, changedContact)).SemanticDigest;

            Sv5SpaceGate gate = plan.Gates.OrderByDescending(value => value.BlockingFaces.Count).First();
            string boundaryDigest = Analyze(plan, gates: Replace(plan.Gates, gate.Id, CloneGate(gate,
                gate.BlockingFaces, boundaryId: gate.BoundaryId + "_MUTATED"))).SemanticDigest;
            string geometryDigest = Analyze(plan, gates: Replace(plan.Gates, gate.Id, CloneGate(gate,
                gate.BlockingFaces.Take(gate.BlockingFaces.Count - 1)))).SemanticDigest;
            string stateDigest = Analyze(plan, gates: Replace(plan.Gates, gate.Id, CloneGate(gate,
                gate.BlockingFaces, sealedState: gate.SealedState + "_MUTATED"))).SemanticDigest;
            Assert.That(new[] { original, contactDigest, boundaryDigest, geometryDigest, stateDigest }
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(5));
        }

        [Test]
        public void G08_AcceptedPlanExportsOneDigestAndPreservesFix02Bytes()
        {
            Sv5SpaceGraphPlan plan = Plan.Value;
            string[] preserved =
            {
                Historical("GENERATED", "SV5_06_FIX02", "space_graph.json"),
                Historical("GENERATED", "SV5_06_FIX02", "contact_checks.csv"),
                Historical("GENERATED", "SV5_06_FIX02", "gate_geometry.json"),
                Historical("REPORTS", "SV5_06_FIX02_RESULT.md"),
                Historical("TASKS", "SV5_06_FIX02.md"),
            };
            string[] before = preserved.Select(HashFile).ToArray();
            string output = GeneratedDirectory();
            Sv5SpaceGraphExport.WriteAll(plan, output);
            Assert.That(preserved.Select(HashFile), Is.EqualTo(before));
            string[] required =
            {
                "space_graph.json", "places.csv", "ports.csv", "connections.csv", "reservation_cells.csv",
                "contact_checks.csv", "physical_contact_checks.json", "gate_geometry.json",
                "gate_state_checks.json", "physical_gate_state_checks.json", "state_proofs.json",
                "validation.json", "obligations.csv",
            };
            Assert.That(required.All(value => File.Exists(Path.Combine(output, value)) &&
                new FileInfo(Path.Combine(output, value)).Length > 0), Is.True);
            Assert.That(new[] { "space_graph.json", "physical_contact_checks.json",
                "physical_gate_state_checks.json", "validation.json" }.All(value =>
                    File.ReadAllText(Path.Combine(output, value)).Contains(plan.PhysicalMovement.SemanticDigest)),
                Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(output, "preview"), "*.svg").Length, Is.EqualTo(20));
            Assert.That(Directory.GetFiles(Path.Combine(output, "preview"), "*.svg").All(value =>
                File.ReadAllText(value).Contains(plan.Digest)), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(output, "validation.json")),
                Does.Contain("\"status\": \"PASS\"").And.Contain("\"physical_movement_pass\": true")
                    .And.Contain("\"composed_geometry_ready\": false")
                    .And.Contain("\"player_verified\": false"));
            TestContext.Out.WriteLine("SV5_06_FIX03_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + plan.Digest);
            TestContext.Out.WriteLine("PHYSICAL_DIGEST=" + plan.PhysicalMovement.SemanticDigest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + output);
            TestContext.Out.WriteLine("SV5_06_FIX03_EXPORT_END");
        }

        private static Sv5SpacePhysicalMovementPlan Analyze(Sv5SpaceGraphPlan plan,
            IEnumerable<Sv5SpaceContactDecision> contacts = null, IEnumerable<Sv5SpaceGate> gates = null) =>
            Sv5SpacePhysicalMovement.Analyze(plan.Core, plan.Connections, contacts ?? plan.ContactDecisions,
                gates ?? plan.Gates);

        private static Sv5SpaceBoundaryFace ForeignFace(Sv5SpaceGraphPlan plan, Sv5SpaceGate owner) =>
            plan.Gates.First(value => value.Id != owner.Id).BlockingFaces.First();

        private static Sv5SpaceGate CloneGate(Sv5SpaceGate value,
            IEnumerable<Sv5SpaceBoundaryFace> blockingFaces, string predicateLabel = null,
            string routeLabel = null, string boundaryId = null, string sealedState = null) =>
            Create<Sv5SpaceGate>(value.Id, boundaryId ?? value.BoundaryId, value.ContactIds,
                value.BlockingCells, blockingFaces, value.SideAAnchor, value.SideBAnchor, value.Direction,
                value.Flow, predicateLabel ?? value.Predicate, value.TypedPredicate, value.SourceConnectionId,
                routeLabel ?? value.SourceRouteId, value.SourcePortId, value.TargetPortId, value.Crossing,
                sealedState ?? value.SealedState, value.OpenState);

        private static T[] Replace<T>(IEnumerable<T> source, string id, T replacement)
        {
            PropertyInfo property = typeof(T) == typeof(Sv5SpaceContactDecision) ?
                typeof(Sv5SpaceContactDecision).GetProperty("Source") : typeof(T).GetProperty("Id");
            return source.Select(value =>
            {
                string valueId = typeof(T) == typeof(Sv5SpaceContactDecision) ?
                    ((Sv5SpaceContactDecision)(object)value).Source.Id : (string)property.GetValue(value);
                return string.Equals(valueId, id, StringComparison.Ordinal) ? replacement : value;
            }).ToArray();
        }

        private static T Create<T>(params object[] arguments) => (T)Activator.CreateInstance(typeof(T),
            BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, CultureInfo.InvariantCulture);
        private static string Input(string file) => Path.Combine(ProjectRoot(), "MapDesign", "MCP", "INPUTS",
            "SV5_06_FIX03", file);
        private static string Historical(params string[] parts) => parts.Aggregate(
            Path.Combine(ProjectRoot(), "MapDesign", "MCP"), Path.Combine);
        private static string GeneratedDirectory() => Historical("GENERATED", "SV5_06_FIX03");
        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string Compact(string value) => value.Replace(" ", string.Empty).Replace("\r", string.Empty)
            .Replace("\n", string.Empty).Replace("\t", string.Empty);
        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path))
                .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
#endif
