#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_06_FIX04")]
    public sealed class Sv5SpaceGraphFix04Tests
    {
        private static readonly Lazy<Sv5CoreReservationPlan> Core = new Lazy<Sv5CoreReservationPlan>(() =>
            Sv5RouteStatePolicyTests.RepresentativePlanForFix01);
        private static readonly Lazy<Sv5SpaceGraphPlan> Plan = new Lazy<Sv5SpaceGraphPlan>(() =>
            Sv5SpaceGraphPlanner.Plan(Core.Value, 1304));

        [Test]
        public void C01_Fix03CounterexampleIsPreserved()
        {
            string path = Path.Combine(Application.dataPath, "..", "MapDesign", "MCP", "INPUTS", "SV5_06_FIX04", "REVIEW_FINDINGS.json");
            Assert.That(File.Exists(path), Is.True, path);
            Assert.That(Plan.Value.GateStateChecks.Count, Is.EqualTo(9));
            foreach (Sv5SpaceGateStateCheck check in Plan.Value.GateStateChecks)
                TestContext.Out.WriteLine($"{check.Id}|expected={check.ExpectedOpen}|actual={check.ActualOpen}|reachable={check.TargetPortReachable}|success={check.Success}|faces={check.CheckedFaces}");
            TestContext.Out.WriteLine("diagnostics=" + string.Join(";", Plan.Value.Diagnostics));
            Assert.That(Plan.Value.GateStateChecks.All(v => v.Success), Is.True);
        }

        [Test]
        public void C02_GateCandidateHasAnOutsidePortBoundary()
        {
            // The old loop iterated aperture cells, then required !allPortCells.Contains(first).
            // Do not depend on a historical accepted-plan contact surviving a reroute.
            var line = Enumerable.Range(10, 8).Select(x => new RmapSpecialWorldPoint(x, 10)).ToArray();
            var ports = new[] { line[0] };
            var protectedAir = new[] { line[0], line[1] };
            var faces = Sv5SpaceGateGeometry.PortBoundaryCandidates(line, ports, ports, protectedAir);
            Assert.That(faces.Count, Is.EqualTo(5));
            Assert.That(faces[0].First, Is.EqualTo(line[2]));
            Assert.That(faces[0].Second, Is.EqualTo(line[3]));
            Assert.That(faces.Any(f => protectedAir.Contains(f.First) || protectedAir.Contains(f.Second)), Is.False);
            Assert.That(Sv5SpaceGateGeometry.PortBoundaryCandidates(line.Take(2), ports, ports,
                protectedAir), Is.Empty, "No legal neck must remain a rejected candidate.");
        }

        [Test]
        public void T01_ForgeOpenWithOtherGatesClosedUsesRealCorridor()
        {
            var plan = Plan.Value;
            foreach (var c in plan.Connections.Where(c => c.Condition.Contains("GATED")))
                TestContext.Out.WriteLine(c.Id + "|" + c.Condition + "|" + string.Join(";", c.Centerline));
            foreach (var g in plan.Gates)
                TestContext.Out.WriteLine(g.Id + "|" + g.SourceConnectionId + "|" + string.Join(";", g.BlockingFaces.Select(f => f.StableToken)));
            foreach (var c in plan.Connections.Where(c => c.Kind == Sv5SpaceConnectionKind.CoreProgression))
            {
                var edge = Core.Value.RouteSource.Graph.Edges.Single(e => e.EdgeId == c.SourceGraphEdgeId);
                bool expected = new Sv5SpaceGatePredicate(edge.RequiredResourceMask, edge.RequiresForge,
                    edge.RequiresSeal, edge.RequiresBossComplete).IsOpen(7, true, false, false);
                var reach = Sv5SpacePhysicalMovement.Evaluate(plan.Core, plan.Connections, plan.Gates,
                    c.Id, 7, true, false, false);
                Assert.That(reach.TargetPortReachable, Is.EqualTo(expected), c.Id + "|" + c.Condition);
            }
        }

        [Test] public void F01_ConditionalGatesHaveExactLocalBarriers()
        {
            var beforeGates = HistoricalGates().ToDictionary(g => g.BoundaryId);
            int count = 0, errors = 0;
            foreach (var row in Rows("contact_checks.csv").Where(r => r[18] == "ConditionalGate"))
            {
                count++;
                var contact = Create<Sv5RouteContactPair>(row[1], P(row[2],row[3]), P(row[4],row[5]),
                    row[7],row[11],P(row[8],row[9]),P(row[12],row[13]),row[10],row[14],row[15],row[16]);
                var gate = beforeGates[row[20]];
                if (Sv5SpaceGraphValidator.ValidateBarrierFixture(contact,gate.BlockingCells,gate.BlockingFaces).Count != 0) errors++;
            }
            Assert.That(count, Is.EqualTo(225), "exact historical fixture, not a new-plan contact count");
            Assert.That(errors, Is.EqualTo(218));
            Assert.That(Sv5SpaceGraphValidator.FindGateErrors(Plan.Value.ContactDecisions,Plan.Value.Gates), Is.Empty);
            Assert.That(Plan.Value.Gates.Count, Is.EqualTo(3));
            Assert.That(Plan.Value.PhysicalMovement.GateStateChecks.Count(v => !v.ExpectedReachable && v.Success), Is.EqualTo(6));
            TestContext.Out.WriteLine("FIX03 local errors=218/225; FIX04 local errors=0");
        }

        [Test] public void F02_SharedAndFaceUsePhysicalGeometry()
        {
            var a = new RmapSpecialWorldPoint(10,10); var b = new RmapSpecialWorldPoint(11,10);
            Sv5RouteContactPair Pair(string kind) => Create<Sv5RouteContactPair>(kind,a,kind == "SHARED" ? a : b,
                "A","B",a,kind == "SHARED" ? a : b,"Passage","Passage","Passage","Passage");
            var exact = Create<Sv5SpaceBoundaryFace>(a,b);
            var remote = Create<Sv5SpaceBoundaryFace>(new RmapSpecialWorldPoint(30,30),new RmapSpecialWorldPoint(31,30));
            Assert.That(Sv5SpaceGraphValidator.ValidateBarrierFixture(Pair("SHARED"),Array.Empty<RmapSpecialWorldPoint>(),new[] { exact }), Is.Not.Empty);
            Assert.That(Sv5SpaceGraphValidator.ValidateBarrierFixture(Pair("SHARED"),new[] { a },Array.Empty<Sv5SpaceBoundaryFace>()), Is.Empty);
            Assert.That(Sv5SpaceGraphValidator.ValidateBarrierFixture(Pair("FACE"),Array.Empty<RmapSpecialWorldPoint>(),new[] { remote }), Is.Not.Empty);
            Assert.That(Sv5SpaceGraphValidator.ValidateBarrierFixture(Pair("FACE"),Array.Empty<RmapSpecialWorldPoint>(),new[] { exact }), Is.Empty);
            var fake = Create<Sv5SpaceContactDecision>(Pair("FACE"),"FIXTURE",Sv5SpaceCrossingKind.ConditionalGate,
                "forge","FAKE_BOUNDARY",true,true,"fixture");
            Assert.That(Sv5SpaceGraphValidator.FindGateErrors(new[] { fake },Plan.Value.Gates), Is.Not.Empty);
        }

        [Test] public void F03_DeepStarYeastApproachAndReturnReachable()
        {
            var before = HistoricalConnections(); var gates = HistoricalGates();
            foreach (string id in new[] { "SV5_CORE_CONN_93f752cf7b8f813c","SV5_CORE_CONN_be0aa26aa7cc3b03" })
            {
                Assert.That(Sv5SpacePhysicalMovement.Evaluate(Core.Value,before,gates,id,0,false,false,false).TargetPortReachable, Is.False,id);
                Assert.That(Sv5SpacePhysicalMovement.Evaluate(Core.Value,before,Array.Empty<Sv5SpaceGate>(),id,0,false,false,false).TargetPortReachable, Is.True,id);
                Assert.That(Reach(id,0,false,false,false), Is.True,id);
            }
            var first = Plan.Value.PhysicalProduct.Proofs.Where(p => p.GoalProof.RequestedOrder.First() == RmapWorldGraphRole.DeepStarYeast).ToArray();
            Assert.That(first.Length, Is.EqualTo(2));
            foreach (var proof in first) Assert.That(proof.Success, Is.True,proof.GoalProof.ProofId);
        }

        [Test] public void F04_BossForgeClosedPreservesNormalTransitions()
        {
            var plan = Plan.Value;
            for (int closed = 0; closed < 8; closed++)
            {
                var subset = plan.Gates.Where((g,i) => (closed & (1 << i)) != 0).ToArray();
                foreach (var c in plan.Connections.Where(c => c.Condition.StartsWith("NORMAL_",StringComparison.Ordinal)))
                    Assert.That(Sv5SpacePhysicalMovement.Evaluate(plan.Core,plan.Connections,subset,c.Id,0,false,false,false)
                        .TargetPortReachable, Is.True,c.Id + "|closed=" + closed);
            }
            T01_ForgeOpenWithOtherGatesClosedUsesRealCorridor();
        }

        [Test] public void F05_ResourceOrderStateProductIsComplete()
        {
            var product = Plan.Value.PhysicalProduct;
            Assert.That(product.Proofs.Count, Is.EqualTo(6));
            Assert.That(product.Matrix.Select(r => r.ResourceOrder).Distinct().Count(), Is.EqualTo(6));
            Assert.That(product.Matrix.Any(r => !r.ExpectedOpen), Is.True);
            foreach (var proof in product.Proofs)
            {
                Assert.That(proof.Success, Is.True, string.Join(";",proof.DeadEnds.Take(3)));
                Assert.That(proof.ReachableStates, Is.GreaterThan(1));
                Assert.That(proof.Transitions, Is.GreaterThan(proof.ReachableStates));
                Assert.That(proof.GoalProof.Actions.Where(a => a.StartsWith("ACQUIRE|")).Select(a => a.Substring(8)),
                    Is.EqualTo(proof.GoalProof.RequestedOrder.Select(r => r.ToString())));
                TestContext.Out.WriteLine(string.Join(">",proof.GoalProof.RequestedOrder) + "|states=" + proof.ReachableStates +
                    "|transitions=" + proof.Transitions + "|reverse=" + proof.ReverseReachableStates);
            }
            Assert.That(product.Diagnostics, Is.Empty);
            Assert.That(product.Matrix.Where(r => !r.Success).Select(r => r.Connection.Id + "|" + r.State.StableToken), Is.Empty);
        }

        [Test] public void F06_OptionalCollateralLocksAreAbsent()
        {
            foreach (string id in new[] { "SV5_OPTIONAL_01","SV5_OPTIONAL_RETURN_TO_START","SV5_OPTIONAL_TO_VILLAGE" })
                foreach (bool reverse in new[] { false,true })
                    Assert.That(Reach(id,0,false,false,false,reverse), Is.True,id + "|" + reverse);
            var optional = Plan.Value.PhysicalProduct.Matrix.Where(r => r.Connection.Kind != Sv5SpaceConnectionKind.CoreProgression).ToArray();
            Assert.That(optional.Length, Is.GreaterThan(0));
            Assert.That(optional.Count(r => r.Reverse), Is.GreaterThan(0));
            Assert.That(optional.Where(r => !r.Success), Is.Empty);
        }

        [Test] public void F07_RenameOrderDigestRegression()
        {
            var plan = Plan.Value;
            var renamed = plan.Connections.Select(c => Create<Sv5SpaceConnection>("RENAMED|" + c.Id,c.Kind,
                c.FromPortId,c.ToPortId,c.FromPlaceId,c.ToPlaceId,c.Direction,c.Flow,c.Condition,c.SourceGraphEdgeId,
                c.SelectionState,c.Centerline,c.Envelope.Reverse(),c.ApertureCells.Reverse())).Reverse().ToArray();
            foreach (var check in plan.PhysicalMovement.GateStateChecks)
                Assert.That(Sv5SpacePhysicalMovement.Evaluate(plan.Core,renamed,plan.Gates.Reverse(),"RENAMED|" + check.ConnectionId,
                    check.ResourceMask,check.ForgeMade,check.SealOpen,check.BossComplete).TargetPortReachable, Is.EqualTo(check.Reachable));
            var reordered = Sv5SpacePhysicalMovement.Analyze(plan.Core,plan.Connections.Reverse(),plan.ContactDecisions.Reverse(),plan.Gates.Reverse());
            Assert.That(reordered.SemanticDigest, Is.EqualTo(plan.PhysicalMovement.SemanticDigest));
            var changed = Sv5SpacePhysicalMovement.Analyze(plan.Core,plan.Connections,plan.ContactDecisions,plan.Gates.Skip(1));
            Assert.That(changed.SemanticDigest, Is.Not.EqualTo(plan.PhysicalMovement.SemanticDigest));
            Assert.That(changed.Success, Is.False, "Removing a gate must expose a real bypass.");
        }

        [Test] public void F08_ExportEvidenceBindsProductDigest()
        {
            var plan = Plan.Value;
            var files = Directory.GetFiles(Historical(""),"*",SearchOption.AllDirectories).OrderBy(f => f).ToArray();
            var before = files.Select(Hash).ToArray();
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath,"../MapDesign/MCP/GENERATED/SV5_07/_work/legacy_exports/sv5_06_fix04"));
            Sv5SpaceGraphExport.WriteAll(plan,directory);
            string comparison = Sv5SpaceGraphExport.RepairDetailSvg(plan,HistoricalConnections(),HistoricalGates());
            new System.Xml.XmlDocument().LoadXml(comparison);
            File.WriteAllText(Path.Combine(directory,"repair_before_after.svg"),comparison,new System.Text.UTF8Encoding(false));
            var historical = HistoricalGates().ToDictionary(g => g.BoundaryId);
            int oldErrors = Rows("contact_checks.csv").Where(r => r[18] == "ConditionalGate").Count(r => {
                var c = Create<Sv5RouteContactPair>(r[1],P(r[2],r[3]),P(r[4],r[5]),r[7],r[11],
                    P(r[8],r[9]),P(r[12],r[13]),r[10],r[14],r[15],r[16]);
                var g = historical[r[20]];
                return Sv5SpaceGraphValidator.ValidateBarrierFixture(c,g.BlockingCells,g.BlockingFaces).Count != 0;
            });
            Assert.That(oldErrors,Is.EqualTo(218));
            string deepStar = Sv5SpaceGraphExport.RepairDetailSvg(plan,HistoricalConnections(),historical.Values,true,oldErrors);
            new System.Xml.XmlDocument().LoadXml(deepStar);
            File.WriteAllText(Path.Combine(directory,"deepstar_before_after.svg"),deepStar,new System.Text.UTF8Encoding(false));
            Assert.That(files.Select(Hash), Is.EqualTo(before));
            Assert.That(File.ReadAllText(Path.Combine(directory,"physical_transition_matrix.json")), Is.EqualTo(Sv5SpaceGraphExport.PhysicalTransitionMatrixJson(plan)));
            Assert.That(File.ReadAllText(Path.Combine(directory,"local_barrier_checks.json")), Is.EqualTo(Sv5SpaceGraphExport.LocalBarrierChecksJson(plan)));
            Assert.That(Sv5SpaceGraphExport.PhysicalTransitionMatrixJson(plan), Does.Contain(plan.Digest).And.Contain(plan.PhysicalProduct.SemanticDigest));
            Assert.That(plan.Success, Is.True,string.Join(";",plan.Diagnostics.Take(15)));
            Assert.That(Sv5SpaceGraphExport.ValidationJson(plan), Does.Contain("\"status\": \"PASS\""));
        }

        private static bool Reach(string id,ulong mask,bool forge,bool seal,bool boss,bool reverse=false) =>
            Sv5SpacePhysicalMovement.Evaluate(Plan.Value.Core,Plan.Value.Connections,Plan.Value.Gates,id,mask,forge,seal,boss,reverse).TargetPortReachable;
        private static T Create<T>(params object[] args) => (T)Activator.CreateInstance(typeof(T),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,null,args,System.Globalization.CultureInfo.InvariantCulture);
        private static RmapSpecialWorldPoint P(string x,string y) => new RmapSpecialWorldPoint(int.Parse(x),int.Parse(y));
        private static RmapSpecialWorldPoint P(int[] xy) => new RmapSpecialWorldPoint(xy[0],xy[1]);
        private static RmapSpecialWorldPoint[] Points(string value) => value.Split('|').Where(s => s.Length != 0).Select(s => s.Split(':')).Select(p => P(p[0],p[1])).ToArray();
        private static string Historical(string file) => Path.GetFullPath(Path.Combine(Application.dataPath,"../MapDesign/MCP/GENERATED/SV5_06_FIX03",file));
        private static string[][] Rows(string file) => File.ReadAllLines(Historical(file)).Skip(1).Where(s => s.Length != 0)
            .Select(s => System.Text.RegularExpressions.Regex.Split(s, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)")
                .Select(v => v.Trim('"').Replace("\"\"","\"")).ToArray()).ToArray();
        private static string Hash(string path) { using(var sha=System.Security.Cryptography.SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-",""); }
        private static Sv5SpaceConnection[] HistoricalConnections() => Rows("connections.csv").Select(r =>
            Create<Sv5SpaceConnection>(r[0],Enum.Parse(typeof(Sv5SpaceConnectionKind),r[1]),r[2],r[3],r[4],r[5],
                Enum.Parse(typeof(RmapWorldGraphDirection),r[6]),r[7],r[8],r[9],r[10],Points(r[12]),Points(r[14]),Points(r[16]))).ToArray();
        private static Sv5SpaceGate[] HistoricalGates()
        {
            string json = File.ReadAllText(Historical("gate_geometry.json"));
            Assert.That(System.Text.RegularExpressions.Regex.Matches(json,"\"blocking_cells\":\\[\\]").Count, Is.EqualTo(3));
            return JsonUtility.FromJson<GateFile>(json).gates.Select(g => Create<Sv5SpaceGate>(g.gate_id,g.boundary_id,
                g.contact_ids,Array.Empty<RmapSpecialWorldPoint>(),g.blocking_faces.Select(f => Create<Sv5SpaceBoundaryFace>(P(f.first),P(f.second))),
                P(g.side_a_anchor),P(g.side_b_anchor),Enum.Parse(typeof(RmapWorldGraphDirection),g.direction),g.flow,g.predicate,
                new Sv5SpaceGatePredicate(g.typed_predicate.required_resource_mask,g.typed_predicate.requires_forge,g.typed_predicate.requires_seal,g.typed_predicate.requires_boss_complete),
                g.source_connection_id,g.source_route_id,g.source_port_id,g.target_port_id,Sv5SpaceCrossingKind.ConditionalGate,g.sealed_state,g.open_state)).ToArray();
        }
        [Serializable] private sealed class GateFile { public GateData[] gates; }
        [Serializable] private sealed class GateData
        {
            public string gate_id,boundary_id,direction,flow,predicate,source_connection_id,source_route_id,source_port_id,target_port_id,sealed_state,open_state;
            public string[] contact_ids;
            public int[] side_a_anchor,side_b_anchor;
            public FaceData[] blocking_faces;
            public PredicateData typed_predicate;
        }
        [Serializable] private sealed class FaceData { public int[] first,second; }
        [Serializable] private sealed class PredicateData { public ulong required_resource_mask; public bool requires_forge,requires_seal,requires_boss_complete; }
    }
}
#endif
