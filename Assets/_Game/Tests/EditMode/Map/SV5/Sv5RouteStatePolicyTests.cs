#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.WorldData;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_05")]
    public sealed class Sv5RouteStatePolicyTests
    {
        private static readonly Lazy<Sv5CoreReservationPlan> Representative =
            new Lazy<Sv5CoreReservationPlan>(() => Sv5CoreReservationPlanner.Plan(BuildRouteSource(1304)));

        internal static Sv5CoreReservationPlan RepresentativePlanForFix01 => Representative.Value;

        [Test]
        public void T01_BindsElevenActualRoutesToGraphNodesPortsAndPredicates()
        {
            Sv5RouteStateAnalysis analysis = Analyze(SafeCandidates());
            Assert.That(analysis.ConditionBindings.Count, Is.EqualTo(11));
            Assert.That(analysis.ConditionBindings.Select(value => value.Route.RouteId),
                Is.EquivalentTo(analysis.CorePlan.RouteSource.Graph.Edges.Select(value => value.EdgeId)));
            Assert.That(analysis.ConditionBindings.All(value => value.Route.Condition == value.Edge.TraversalCondition), Is.True);
            Assert.That(analysis.ConditionBindings.All(value => value.FromAccess.OpenCells.Count != 0 && value.ToAccess.OpenCells.Count != 0), Is.True);
            Assert.That(analysis.ConditionBindings.All(value => value.GeometryReady == false && value.PlayerVerified == false), Is.True);
        }

        [Test]
        public void T02_AllSixOrdersUseActualActionsReturnsAndForgeSealBossExit()
        {
            Sv5RouteStateAnalysis analysis = Analyze(SafeCandidates());
            Assert.That(analysis.OrderProofs.Count, Is.EqualTo(6));
            foreach (Sv5RouteOrderProof proof in analysis.OrderProofs)
            {
                Assert.That(proof.Success, Is.True, Describe(proof.Source.Failures));
                Assert.That(proof.Source.Actions.Count(value => value.StartsWith("ACQUIRE|", StringComparison.Ordinal)), Is.EqualTo(3));
                Assert.That(proof.Source.Actions, Does.Contain("FORGE|MAKE_SEAL"));
                Assert.That(proof.Source.Actions, Does.Contain("SEAL|OPEN"));
                Assert.That(proof.Source.Actions, Does.Contain("BOSS|PLANNED_COMPLETION_EVENT"));
                Assert.That(proof.Source.FinalState.ForgeMade && proof.Source.FinalState.SealOpen &&
                    proof.Source.FinalState.BossComplete, Is.True);
                Assert.That(proof.Trace.Count, Is.EqualTo(proof.Source.Actions.Count));
            }
        }

        [Test]
        public void T03_WeakForgeSealBossAndExitCandidatesAreRejectedWithoutStateMutation()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            string start = Node(plan, RmapWorldGraphRole.Start);
            string forge = Node(plan, RmapWorldGraphRole.Forge);
            string seal = Node(plan, RmapWorldGraphRole.Seal);
            string exit = Node(plan, RmapWorldGraphRole.Exit);
            Sv5RouteStateAnalysis analysis = Analyze(new[]
            {
                Candidate("WEAK_FORGE", Existing(start), Existing(forge), 0, false, false, false),
                Candidate("WEAK_SEAL", Existing(start), Existing(seal), 0, false, false, false),
                Candidate("WEAK_EXIT", Existing(start), Existing(exit), 0, false, false, false),
            });
            Assert.That(analysis.ShortcutDecisions.Where(value => value.Id.StartsWith("WEAK_", StringComparison.Ordinal))
                .Select(value => value.Code), Is.All.EqualTo(Sv5RouteShortcutDecisionCode.WeakRequiredCondition));
            Assert.That(analysis.ShortcutDecisions.Single(value => value.Id == "CANDIDATE_SET").IsAllowed, Is.False);
            Assert.That(plan.Digest, Is.EqualTo(Representative.Value.Digest));
        }

        [Test]
        public void T04_MissingReturnAndReverseOneWayRemainExplicitFailures()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            RmapWorldGraphProof missingReturn = RmapWorldGraphPlanner.Evaluate(plan.RouteSource.Graph.Nodes,
                plan.RouteSource.Graph.Edges.Where(edge => edge.TraversalCondition != "NORMAL_RESOURCE_RETURN"),
                new[] { RmapWorldGraphRole.MooncoreOre, RmapWorldGraphRole.CondensedCoefficientSap, RmapWorldGraphRole.DeepStarYeast });
            Sv5RouteStateAnalysis reverse = Analyze(new[]
            {
                Candidate("REVERSE_SEAL_FORGE", Existing(Node(plan, RmapWorldGraphRole.Seal)),
                    Existing(Node(plan, RmapWorldGraphRole.Forge)), 7, true, false, false),
            });
            Assert.That(missingReturn.Success, Is.False);
            Assert.That(missingReturn.Failures.Select(value => value.Code), Does.Contain("GRAPH_GOAL_UNREACHABLE"));
            Assert.That(reverse.ShortcutDecisions.Single(value => value.Id == "REVERSE_SEAL_FORGE").Code,
                Is.EqualTo(Sv5RouteShortcutDecisionCode.ReverseOfOneWay));
        }

        [Test]
        public void T05_IndividualShortcutsRemainPureAndSensitiveBypassIsDenied()
        {
            Sv5RouteStateAnalysis safe = Analyze(SafeCandidates());
            Sv5CoreReservationPlan plan = Representative.Value;
            Sv5RouteStateAnalysis unknown = Analyze(new[]
            {
                Candidate("UNKNOWN", new Sv5RouteStateAnchor("UNKNOWN_ANCHOR", Sv5RouteStateAnchorKind.ExistingGraphNode),
                    Existing(Node(plan, RmapWorldGraphRole.Start)), 0, false, false, false),
            });
            Assert.That(safe.ShortcutDecisions.Where(value => value.Id != "CANDIDATE_SET"), Is.All.Matches<Sv5RouteShortcutDecision>(value => value.IsAllowed));
            Assert.That(safe.ShortcutDecisions.Single(value => value.Id == "CANDIDATE_SET").IsAllowed, Is.True);
            Assert.That(unknown.ShortcutDecisions.Single(value => value.Id == "UNKNOWN").Code,
                Is.EqualTo(Sv5RouteShortcutDecisionCode.UnknownAnchor));
        }

        [Test]
        public void T06_CandidateSetsUseAnalysisNodesAndAreNotAcceptedByIndividualChecksAlone()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            var general = new Sv5RouteStateAnchor("SV5_05_GENERAL_LINK", Sv5RouteStateAnchorKind.GeneralConnection);
            Sv5RouteShortcutCandidate[] candidates =
            {
                Candidate("GENERAL_IN", Existing(Node(plan, RmapWorldGraphRole.Start)), general, 0, false, false, false),
                Candidate("GENERAL_RESOURCE", general, Existing(Node(plan, RmapWorldGraphRole.MooncoreOre)), 0, false, false, false),
            };
            Sv5RouteStateAnalysis analysis = Analyze(candidates);
            Assert.That(analysis.ShortcutDecisions.Single(value => value.Id == "GENERAL_IN").IsAllowed, Is.True);
            Assert.That(analysis.ShortcutDecisions.Single(value => value.Id == "GENERAL_RESOURCE").IsAllowed, Is.True);
            Assert.That(analysis.ShortcutDecisions.Single(value => value.Id == "CANDIDATE_SET").IsAllowed, Is.True);
            Assert.That(analysis.OrderProofs, Is.All.Matches<Sv5RouteOrderProof>(value => value.Success));
        }

        [Test]
        public void T07_OptionalAndRequiredPoliciesPreserveNormalReturnsAndOnlyPlanShortcuts()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            RmapWorldGraphPlan optional = Sv5RouteStatePolicy.EvaluateReturnPolicy(plan, RmapWorldReturnShortcutPolicy.Optional);
            RmapWorldGraphPlan required = Sv5RouteStatePolicy.EvaluateReturnPolicy(plan, RmapWorldReturnShortcutPolicy.Required);
            Assert.That(optional.Success && required.Success, Is.True, Describe(optional.Failures.Concat(required.Failures)));
            Assert.That(optional.Edges.Any(value => value.TraversalCondition.Contains("SHORTCUT")), Is.False);
            Assert.That(required.Edges.Count(value => value.TraversalCondition.Contains("SHORTCUT")), Is.EqualTo(3));
            Assert.That(required.Edges.Count(value => value.TraversalCondition == "NORMAL_RESOURCE_RETURN"), Is.EqualTo(3));
            Assert.That(required.Reservations.Count, Is.EqualTo(optional.Reservations.Count + 3));
        }

        [Test]
        public void T08_ActualReviewContactsAreClassifiedFromPredicatesNotLabels()
        {
            Sv5RouteStateAnalysis analysis = Analyze(SafeCandidates());
            Sv5RouteContactCheck[] reviews = analysis.ContactChecks.Where(value => value.Kind == "REVIEW_LABEL_CONTACT").ToArray();
            Assert.That(reviews.Length, Is.EqualTo(3));
            Assert.That(reviews.Select(value => value.World), Is.EquivalentTo(new[]
            {
                new RmapSpecialWorldPoint(415, 301), new RmapSpecialWorldPoint(491, 301), new RmapSpecialWorldPoint(523, 134),
            }));
            Assert.That(reviews, Is.All.Matches<Sv5RouteContactCheck>(value =>
                value.Classification == "LOGICAL_GUARD_PRESENT_GEOMETRY_PENDING"));
            Assert.That(reviews.All(value => !value.LogicalStateTransitionChecked), Is.True);
            Assert.That(analysis.ContactStateVerified, Is.False);
        }

        [Test]
        public void T09_AirWitnessRemainsDiagnosticAndPhysicalObligationsStayPending()
        {
            Sv5RouteStateAnalysis analysis = Analyze(SafeCandidates());
            Sv5RouteContactCheck witness = analysis.ContactChecks.Single(value => value.Id == "AIR_WITNESS_109_EDGE");
            Assert.That(witness.Classification, Is.EqualTo("STATIC_AIR_CONTACT_GEOMETRY_PENDING"));
            Assert.That(analysis.LogicalStateVerified, Is.True);
            Assert.That(analysis.GeometryStateReady, Is.False);
            Assert.That(analysis.PlayerVerified, Is.False);
            Assert.That(analysis.Obligations.Select(value => value.OwnerTask), Does.Contain("SV5_06_SPACE_GRAPH"));
            Assert.That(analysis.Obligations.Select(value => value.OwnerTask), Does.Contain("SV5_41_COMPOSE"));
        }

        [Test]
        public void T10_OutputIsDeterministicAcrossCandidateOrderAndUsesTheTestedAnalysis()
        {
            Sv5RouteShortcutCandidate[] candidates = SafeCandidates();
            Sv5RouteStateAnalysis first = Analyze(candidates);
            Sv5RouteStateAnalysis reverse = Analyze(candidates.Reverse());
            Assert.That(reverse.Digest, Is.EqualTo(first.Digest));
            Assert.That(Sv5RouteStateExport.ConditionBindingsCsv(reverse), Is.EqualTo(Sv5RouteStateExport.ConditionBindingsCsv(first)));
            Assert.That(Sv5RouteStateExport.ShortcutDecisionsCsv(reverse), Is.EqualTo(Sv5RouteStateExport.ShortcutDecisionsCsv(first)));
            WriteOutputs(first);
            foreach (string file in Directory.GetFiles(GeneratedDirectory(), "*", SearchOption.TopDirectoryOnly))
                Assert.That(new FileInfo(file).Length, Is.GreaterThan(0), file);
            TestContext.Out.WriteLine("SV5_05_EXPORT_BEGIN");
            TestContext.Out.WriteLine("ANALYSIS_DIGEST=" + first.Digest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + GeneratedDirectory());
            TestContext.Out.WriteLine("SV5_05_EXPORT_END");
        }

        private static Sv5RouteStateAnalysis Analyze(IEnumerable<Sv5RouteShortcutCandidate> candidates) =>
            Sv5RouteStatePolicy.Analyze(Representative.Value, candidates, ReviewInput());

        private static Sv5RouteShortcutCandidate[] SafeCandidates()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            var general = new Sv5RouteStateAnchor("SV5_05_GENERAL_RESOURCE_APPROACH", Sv5RouteStateAnchorKind.GeneralConnection);
            return new[]
            {
                Candidate("SAFE_GENERAL_IN", Existing(Node(plan, RmapWorldGraphRole.Start)), general, 0, false, false, false),
                Candidate("SAFE_GENERAL_RESOURCE", general, Existing(Node(plan, RmapWorldGraphRole.MooncoreOre)), 0, false, false, false),
            };
        }

        private static Sv5RouteShortcutCandidate Candidate(string id, Sv5RouteStateAnchor from, Sv5RouteStateAnchor to,
            ulong resources, bool forge, bool seal, bool boss) => new Sv5RouteShortcutCandidate(id, from, to,
            RmapWorldGraphDirection.Right, resources, forge, seal, boss, "SV5_05_FOCUSED_TEST");

        private static Sv5RouteStateAnchor Existing(string id) =>
            new Sv5RouteStateAnchor(id, Sv5RouteStateAnchorKind.ExistingGraphNode);

        private static string Node(Sv5CoreReservationPlan plan, RmapWorldGraphRole role) =>
            plan.RouteSource.Graph.Nodes.Single(value => value.Role == role).NodeId;

        private static Sv5RouteStateReviewInput ReviewInput() => new Sv5RouteStateReviewInput(new[]
        {
            new Sv5RouteStateReviewContact(new RmapSpecialWorldPoint(415, 301), new[]
            {
                "0fc428a59647d62495d286c4c25fd66afac209842182354729188041dc5796e3",
                "16f8c96de43cf0ec7882d78bdcae5b4cc79a1e08fd38fcfbbb7e90b19bfc8998",
                "21e6a90771dd0ec3a5924c7bed7cedd6df7b87118024ccd550005017a8e7ebd0",
                "74004c50147eb4af54c7c3beb9c82282fb0d23628aa3c2e2a588301c04e965f4",
            }),
            new Sv5RouteStateReviewContact(new RmapSpecialWorldPoint(491, 301), new[]
            {
                "74004c50147eb4af54c7c3beb9c82282fb0d23628aa3c2e2a588301c04e965f4",
                "a98881f4240397805efa4bd7a49fec0d564a271544864f5fdd66657aed45cdff",
            }),
            new Sv5RouteStateReviewContact(new RmapSpecialWorldPoint(523, 134), new[]
            {
                "0fc428a59647d62495d286c4c25fd66afac209842182354729188041dc5796e3",
                "29dc755094886d48300e9f8cf0fb0e54f09125a29c2fac78855bda7885f7dbaa",
            }),
        }, ReadAirWitness());

        private static IReadOnlyList<RmapSpecialWorldPoint> ReadAirWitness()
        {
            string text = File.ReadAllText(Path.Combine(ProjectRoot(), "MapDesign", "MCP", "INPUTS", "SV5_05", "REVIEW_FINDINGS.json"), Encoding.UTF8);
            int air = text.IndexOf("\"air_contact_witness\"", StringComparison.Ordinal);
            int cells = text.IndexOf("\"cells\": [", air, StringComparison.Ordinal);
            int end = text.IndexOf("\"excluded_sealed_cells\"", cells, StringComparison.Ordinal);
            Assert.That(air, Is.GreaterThanOrEqualTo(0));
            Assert.That(cells, Is.GreaterThanOrEqualTo(0));
            Assert.That(end, Is.GreaterThan(cells));
            MatchCollection matches = Regex.Matches(text.Substring(cells, end - cells), @"\[\s*(\d+)\s*,\s*(\d+)\s*\]");
            var points = matches.Cast<Match>().Select(value => new RmapSpecialWorldPoint(
                int.Parse(value.Groups[1].Value, CultureInfo.InvariantCulture),
                int.Parse(value.Groups[2].Value, CultureInfo.InvariantCulture))).ToArray();
            Assert.That(points.Length, Is.EqualTo(110));
            return points;
        }

        private static void WriteOutputs(Sv5RouteStateAnalysis analysis)
        {
            string directory = GeneratedDirectory();
            Directory.CreateDirectory(directory);
            Write(Path.Combine(directory, "condition_bindings.csv"), Sv5RouteStateExport.ConditionBindingsCsv(analysis));
            Write(Path.Combine(directory, "order_proofs.csv"), Sv5RouteStateExport.OrderProofsCsv(analysis));
            Write(Path.Combine(directory, "state_traces.csv"), Sv5RouteStateExport.StateTracesCsv(analysis));
            Write(Path.Combine(directory, "shortcut_decisions.csv"), Sv5RouteStateExport.ShortcutDecisionsCsv(analysis));
            Write(Path.Combine(directory, "contact_checks.csv"), Sv5RouteStateExport.ContactChecksCsv(analysis));
            Write(Path.Combine(directory, "obligations.csv"), Sv5RouteStateExport.ObligationsCsv(analysis));
            Write(Path.Combine(directory, "route_state_manifest.json"), Sv5RouteStateExport.ManifestJson(analysis));
        }

        private static string GeneratedDirectory() => Path.Combine(ProjectRoot(), "MapDesign", "MCP", "GENERATED",
            "SV5_06_FIX01", "legacy_exports", "sv5_05_policy_t10");
        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static void Write(string path, string text) => File.WriteAllText(path, text, new UTF8Encoding(false));

        private static Rmap16ClusterAssemblyPlan BuildRouteSource(ulong seed)
        {
            WorldGenerationRngStreams streams = RngStreams();
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(
                new RmapWorldDataRequest(seed, "CONTENT_V1", "GENERATOR_V1"), streams);
            return RmapClusterAssemblyPlanner.Plan(definition, streams);
        }

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
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new WorldGenerationRngStreams(set);
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "SV5_05 focused fixture");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            byte[] bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(value.Substring(index * 2, 2),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string name, object value)
        {
            var field = target.GetType().GetField("<" + name + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static string Describe(IEnumerable<RmapWorldGraphFailure> failures) =>
            string.Join(";", (failures ?? Array.Empty<RmapWorldGraphFailure>()).Select(value => value.Code + ":" + value.Detail));
    }
}
#endif
