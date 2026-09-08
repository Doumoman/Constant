#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.Tests.EditMode.Rmap13
{
    [Category("RMAP13")]
    public sealed class RmapWorldGraphPlannerTests
    {
        [Test]
        public void W01_W02_AllSixOrdersReachForgeSealBossAndExitWithStateAwareProofs()
        {
            RmapWorldGraphPlan plan = RmapWorldGraphPlanner.Plan(Definition(1301));

            Assert.That(plan.Success, Is.True, Describe(plan.Failures));
            Assert.That(plan.Nodes.Count, Is.EqualTo(8));
            Assert.That(plan.Nodes.Select(value => value.AnchorStableId).Distinct().Count(), Is.EqualTo(8));
            Assert.That(plan.Definition.RngBindings[RmapWorldRngStream.RunGraph].SourceStreamId,
                Is.EqualTo(WorldGenerationRngStreams.RouteStreamId));
            Assert.That(plan.Proofs.Count, Is.EqualTo(6));
            foreach (RmapWorldGraphProof proof in plan.Proofs)
            {
                Assert.That(proof.Success, Is.True, Describe(proof.Failures));
                Assert.That(proof.Actions.Count(action => action.StartsWith("ACQUIRE|", StringComparison.Ordinal)),
                    Is.EqualTo(3));
                Assert.That(proof.Actions, Does.Contain("FORGE|MAKE_SEAL"));
                Assert.That(proof.Actions, Does.Contain("SEAL|OPEN"));
                Assert.That(proof.Actions, Does.Contain("BOSS|PLANNED_COMPLETION_EVENT"));
                int previous = -1;
                foreach (RmapWorldGraphRole role in proof.RequestedOrder)
                {
                    int action = proof.Actions.ToList().IndexOf("ACQUIRE|" + role);
                    Assert.That(action, Is.GreaterThan(previous), string.Join(";", proof.Actions));
                    previous = action;
                }
                Assert.That(proof.FinalState.BossComplete, Is.True);
                Assert.That(proof.FinalState.ForgeMade, Is.True);
                Assert.That(proof.FinalState.SealOpen, Is.True);
            }

            Assert.That(RmapWorldGraphPlanner.Plan(Definition(1301)).Digest, Is.EqualTo(plan.Digest));
        }

        [Test]
        public void W02_RejectsBrokenReturnAndReverseOneWayConnectionWithRecordedReason()
        {
            RmapWorldGraphPlan plan = RmapWorldGraphPlanner.Plan(Definition(1302));
            var firstOrder = new[]
            {
                RmapWorldGraphRole.MooncoreOre,
                RmapWorldGraphRole.CondensedCoefficientSap,
                RmapWorldGraphRole.DeepStarYeast,
            };
            RmapWorldGraphProof brokenReturn = RmapWorldGraphPlanner.Evaluate(plan.Nodes,
                plan.Edges.Where(edge => !string.Equals(edge.TraversalCondition,
                    "NORMAL_RESOURCE_RETURN", StringComparison.Ordinal)), firstOrder);
            Assert.That(brokenReturn.Success, Is.False);
            Assert.That(brokenReturn.Failures.Select(value => value.Code),
                Does.Contain("GRAPH_GOAL_UNREACHABLE"));
            Assert.That(brokenReturn.Failures[0].Detail, Does.StartWith("RESOURCE_ORDER_OR_RETURN_RESERVATION"));

            RmapWorldGraphEdge source = plan.Edges[0];
            var invalidReverse = new RmapWorldGraphEdge(source.TargetNodeId, source.SourceNodeId,
                Opposite(source.Direction), source.TraversalCondition, source.SourceConnectionId, true);
            IReadOnlyList<RmapWorldGraphFailure> failures = RmapWorldGraphPlanner.ValidateRequirements(
                plan.Nodes, plan.Edges.Concat(new[] { invalidReverse }), plan.Reservations);
            Assert.That(failures.Select(value => value.Code), Does.Contain("REVERSE_OF_ONE_WAY_EDGE"));
        }

        [Test]
        public void W07_OptionalAndRequiredPoliciesPreserveOrdersAndExposeOnlyPlannedShortcuts()
        {
            RmapWorldDefinition definition = Definition(1303);
            RmapWorldGraphPlan optional = RmapWorldGraphPlanner.Plan(definition,
                RmapWorldReturnShortcutPolicy.Optional);
            RmapWorldGraphPlan required = RmapWorldGraphPlanner.Plan(definition,
                RmapWorldReturnShortcutPolicy.Required);

            Assert.That(optional.Success, Is.True, Describe(optional.Failures));
            Assert.That(required.Success, Is.True, Describe(required.Failures));
            Assert.That(optional.Edges.Any(edge => edge.TraversalCondition.Contains("SHORTCUT")), Is.False);
            Assert.That(required.Edges.Count(edge => edge.TraversalCondition.Contains("SHORTCUT")), Is.EqualTo(3));
            Assert.That(required.Reservations.Count, Is.EqualTo(optional.Reservations.Count + 3));
            Assert.That(required.Reservations.Where(value => value.ReleaseCondition.StartsWith("AFTER_"))
                .All(value => value.Status == RmapWorldGraphReservationStatus.Planned), Is.True);
            Assert.That(required.Edges.All(value => value.VerificationLevel ==
                RmapWorldGraphVerificationLevel.PlannedSpace), Is.True);
        }

        [Test]
        public void W02_W07_StableExportsPreserveApiIdsAndEscapedProofRows()
        {
            RmapWorldGraphPlan plan = RmapWorldGraphPlanner.Plan(Definition(1304),
                RmapWorldReturnShortcutPolicy.Required);
            string nodes = RmapWorldGraphExport.NodesCsv(plan);
            string edges = RmapWorldGraphExport.EdgesCsv(plan);
            string reservations = RmapWorldGraphExport.ReservationsCsv(plan);
            string proofs = RmapWorldGraphExport.ProofsCsv(plan);

            Assert.That(nodes.Split('\n')[0], Is.EqualTo("node_id,role,anchor_stable_id,verification_level"));
            Assert.That(edges.Split('\n')[0], Does.StartWith("edge_id,source_node_id"));
            Assert.That(reservations.Split('\n')[0], Does.StartWith("reservation_id,node_id"));
            Assert.That(proofs.Split('\n')[0], Does.StartWith("proof_id,resource_order"));
            Assert.That(nodes, Does.Contain(plan.Nodes[0].NodeId));
            Assert.That(edges, Does.Contain(plan.Edges[0].EdgeId));
            Assert.That(reservations, Does.Contain(plan.Reservations[0].ReservationId));
            Assert.That(proofs, Does.Contain(plan.Proofs[0].ProofId));
            Assert.That(proofs, Does.Contain("\"MOVE|"));
            TestContext.Out.WriteLine("RMAP13_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + plan.Digest);
            TestContext.Out.WriteLine("NODES_CSV=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(nodes)));
            TestContext.Out.WriteLine("EDGES_CSV=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(edges)));
            TestContext.Out.WriteLine("RESERVATIONS_CSV=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(reservations)));
            TestContext.Out.WriteLine("PROOFS_CSV=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(proofs)));
            TestContext.Out.WriteLine("RMAP13_EXPORT_END");
        }

        private static RmapWorldDefinition Definition(ulong seed) => RmapWorldDataGenerator.Generate(
            new RmapWorldDataRequest(seed, "CONTENT_V1", "GENERATOR_V1"), RngStreams());

        private static WorldGenerationRngStreams RngStreams()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { "RNG_WORLD_SITE", Rng("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD") },
                { "RNG_BIOME_PATCH", Rng("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS") },
                { "RNG_ROUTE", Rng("RNG_ROUTE", "C00FEE12AB341901", "PASS") },
                { "RNG_TYPE0", Rng("RNG_TYPE0", "D15EA5E007A4C883", "PASS") },
                { "RNG_SECTOR_RECIPE", Rng("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR") },
                { "RNG_POPULATION", Rng("RNG_POPULATION", "F123456789ABCDEF", "SPAWN") },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(
                typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new WorldGenerationRngStreams(set);
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(
                typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "RMAP13 focused fixture");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(
                value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance |
                BindingFlags.NonPublic, null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string name, object value)
        {
            var field = target.GetType().GetField("<" + name + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static RmapWorldGraphDirection Opposite(RmapWorldGraphDirection direction) =>
            direction == RmapWorldGraphDirection.Left ? RmapWorldGraphDirection.Right :
            direction == RmapWorldGraphDirection.Right ? RmapWorldGraphDirection.Left :
            direction == RmapWorldGraphDirection.Up ? RmapWorldGraphDirection.Down : RmapWorldGraphDirection.Up;
        private static string Describe(IEnumerable<RmapWorldGraphFailure> failures) =>
            string.Join(";", failures.Select(value => value.Code + ":" + value.Detail));
    }
}
#endif
