#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_05_FIX01")]
    public sealed class Sv5RouteStateFix01Tests
    {
        [Test]
        public void T01_ForgeToBossWithoutSealIsRejectedWithTheReachableMissingGuard()
        {
            var candidate = Candidate("REVIEW_FORGE_TO_BOSS_NO_SEAL", Existing(Node(RmapWorldGraphRole.Forge)),
                Existing(Node(RmapWorldGraphRole.Boss)), RmapWorldGraphDirection.Right, 7, true, false, false, "SV5_05_REVIEW_FIX01");
            Sv5RouteShortcutDecision decision = Analyze(new[] { candidate }, EmptyReview()).ShortcutDecisions.Single(value => value.Id == candidate.Id);
            Assert.That(decision.Code, Is.EqualTo(Sv5RouteShortcutDecisionCode.WeakRequiredCondition));
            Assert.That(decision.Detail, Does.Contain("SealOpen"));
            Assert.That(decision.CounterexampleBefore, Is.Not.Null);
            Assert.That(decision.CounterexampleAfter, Is.Not.Null);
            Assert.That(decision.CounterexampleAction, Does.StartWith("MOVE|"));
            Assert.That(decision.CounterexamplePrefix.Count, Is.GreaterThan(0));
            Assert.That(decision.CounterexamplePrefix.Last(), Is.EqualTo(decision.CounterexampleAction));
        }

        [Test]
        public void T02_SealedEntryAndSameStageCandidatesPreserveSixOrdersAndNormalReturns()
        {
            var general = new Sv5RouteStateAnchor("FIX01_SAFE_GENERAL", Sv5RouteStateAnchorKind.GeneralConnection);
            Sv5RouteStateAnalysis analysis = Analyze(new[]
            {
                Candidate("SAFE_GENERAL_IN", Existing(Node(RmapWorldGraphRole.Start)), general, RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_T02"),
                Candidate("SAFE_GENERAL_OUT", general, Existing(Node(RmapWorldGraphRole.MooncoreOre)), RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_T02"),
                Candidate("SAFE_SEALED_BOSS", Existing(Node(RmapWorldGraphRole.Forge)), Existing(Node(RmapWorldGraphRole.Boss)), RmapWorldGraphDirection.Right, 7, true, true, false, "FIX01_T02"),
            }, EmptyReview());
            Assert.That(analysis.ShortcutDecisions.Where(value => value.Id != "CANDIDATE_SET"), Is.All.Matches<Sv5RouteShortcutDecision>(value => value.IsAllowed));
            Assert.That(analysis.ShortcutDecisions.Single(value => value.Id == "CANDIDATE_SET").IsAllowed, Is.True);
            Assert.That(analysis.OrderProofs, Is.All.Matches<Sv5RouteOrderProof>(value => value.Success));
            Assert.That(analysis.CandidateSetProofs, Is.All.Matches<Sv5RouteOrderProof>(value => value.Success));
            Assert.That(analysis.OrderProofs.All(value => value.Source.Actions.Contains("FORGE|MAKE_SEAL") && value.Source.Actions.Contains("SEAL|OPEN") && value.Source.Actions.Contains("BOSS|PLANNED_COMPLETION_EVENT")), Is.True);
            RmapWorldGraphPlan required = Sv5RouteStatePolicy.EvaluateReturnPolicy(Plan, RmapWorldReturnShortcutPolicy.Required);
            Assert.That(required.Success, Is.True);
            Assert.That(required.Edges.Count(value => value.TraversalCondition == "NORMAL_RESOURCE_RETURN"), Is.EqualTo(3));
        }

        [Test]
        public void T03_GeneralBossEntryAndReachableDeadEndAreRejectedAsCandidateSetFailures()
        {
            var early = new Sv5RouteStateAnchor("FIX01_EARLY_BOSS", Sv5RouteStateAnchorKind.GeneralConnection);
            Sv5RouteStateAnalysis earlyBoss = Analyze(new[]
            {
                Candidate("EARLY_IN", Existing(Node(RmapWorldGraphRole.Start)), early, RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_T03"),
                Candidate("EARLY_BOSS", early, Existing(Node(RmapWorldGraphRole.Boss)), RmapWorldGraphDirection.Right, 7, true, false, false, "FIX01_T03"),
            }, EmptyReview());
            Assert.That(earlyBoss.ShortcutDecisions.Single(value => value.Id == "EARLY_BOSS").Code, Is.EqualTo(Sv5RouteShortcutDecisionCode.WeakRequiredCondition));
            Assert.That(earlyBoss.ShortcutDecisions.Single(value => value.Id == "CANDIDATE_SET").IsAllowed, Is.False);

            var dead = new Sv5RouteStateAnchor("FIX01_REACHABLE_DEAD_END", Sv5RouteStateAnchorKind.GeneralConnection);
            Sv5RouteStateAnalysis deadEnd = Analyze(new[]
            {
                Candidate("DEAD_END_IN", Existing(Node(RmapWorldGraphRole.Start)), dead, RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_T03"),
            }, EmptyReview());
            Sv5RouteShortcutDecision decision = deadEnd.ShortcutDecisions.Single(value => value.Id == "CANDIDATE_SET");
            Assert.That(decision.Code, Is.EqualTo(Sv5RouteShortcutDecisionCode.CandidateSetUnsafeState));
            Assert.That(decision.CounterexampleBefore, Is.Not.Null);
            Assert.That(decision.CounterexampleAfter, Is.Not.Null);
            Assert.That(decision.CounterexamplePrefix.Count, Is.GreaterThan(0));
            Assert.That(deadEnd.OrderProofs, Is.All.Matches<Sv5RouteOrderProof>(value => value.Success));
            Assert.That(deadEnd.CandidateSetProofs, Is.All.Matches<Sv5RouteOrderProof>(value => value.Success));
        }

        [Test]
        public void T04_CanonicalDigestBindsEveryCandidateSemanticAndIgnoresSetEnumerationOrder()
        {
            string start = Node(RmapWorldGraphRole.Start);
            string ore = Node(RmapWorldGraphRole.MooncoreOre);
            string sap = Node(RmapWorldGraphRole.CondensedCoefficientSap);
            Sv5RouteShortcutCandidate baseline = Candidate("SEMANTIC", Existing(start), Existing(ore), RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_T04_A");
            Sv5RouteShortcutCandidate companion = Candidate("COMPANION", Existing(start), Existing(sap), RmapWorldGraphDirection.Up, 0, false, false, false, "FIX01_T04_A");
            string digest = Analyze(new[] { baseline, companion }, EmptyReview()).Digest;
            Assert.That(Analyze(new[] { companion, baseline }, EmptyReview()).Digest, Is.EqualTo(digest));
            Assert.That(Analyze(new[] { Candidate("SEMANTIC", Existing(start), Existing(sap), RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_T04_A"), companion }, EmptyReview()).Digest, Is.Not.EqualTo(digest));
            Assert.That(Analyze(new[] { Candidate("SEMANTIC", Existing(start), Existing(ore), RmapWorldGraphDirection.Left, 0, false, false, false, "FIX01_T04_A"), companion }, EmptyReview()).Digest, Is.Not.EqualTo(digest));
            Assert.That(Analyze(new[] { Candidate("SEMANTIC", Existing(start), Existing(ore), RmapWorldGraphDirection.Right, 1, false, false, false, "FIX01_T04_A"), companion }, EmptyReview()).Digest, Is.Not.EqualTo(digest));
            Assert.That(Analyze(new[] { Candidate("SEMANTIC", Existing(start), Existing(ore), RmapWorldGraphDirection.Right, 0, true, false, false, "FIX01_T04_A"), companion }, EmptyReview()).Digest, Is.Not.EqualTo(digest));
            Assert.That(Analyze(new[] { Candidate("SEMANTIC", Existing(start), Existing(ore), RmapWorldGraphDirection.Right, 0, false, true, false, "FIX01_T04_A"), companion }, EmptyReview()).Digest, Is.Not.EqualTo(digest));
            Assert.That(Analyze(new[] { Candidate("SEMANTIC", Existing(start), Existing(ore), RmapWorldGraphDirection.Right, 0, false, false, true, "FIX01_T04_A"), companion }, EmptyReview()).Digest, Is.Not.EqualTo(digest));
            Assert.That(Analyze(new[] { Candidate("SEMANTIC", Existing(start), Existing(ore), RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_T04_B"), companion }, EmptyReview()).Digest, Is.Not.EqualTo(digest));
        }

        [Test]
        public void T05_AugmentedProofsAndExportsComeFromTheSameTestedAnalysis()
        {
            Sv5RouteStateAnalysis analysis = Analyze(SafeCandidates(), ActualReview());
            Assert.That(analysis.OrderProofs.Count, Is.EqualTo(6));
            Assert.That(analysis.CandidateSetProofs.Count, Is.EqualTo(6));
            Assert.That(analysis.CandidateSetProofs, Is.All.Matches<Sv5RouteOrderProof>(value => value.Success));
            string json = Sv5RouteStateExport.AnalysisJson(analysis);
            Assert.That(json, Does.Contain(analysis.Digest));
            Assert.That(json, Does.Contain("candidate_set_proofs"));
            Assert.That(json, Does.Contain(analysis.Candidates[0].CanonicalPayload));
            WriteOutputs(analysis);
            foreach (string file in Directory.GetFiles(GeneratedDirectory(), "*", SearchOption.TopDirectoryOnly))
                Assert.That(new FileInfo(file).Length, Is.GreaterThan(0), file);
        }

        [Test]
        public void T06_InvalidReviewInputsRemainUnverifiedWhileOriginalContactsAndOwnersRemainPending()
        {
            Sv5RouteStateAnalysis invalid = Analyze(SafeCandidates(), new Sv5RouteStateReviewInput(new[]
            {
                new Sv5RouteStateReviewContact(new RmapSpecialWorldPoint(1, 1), new[] { "UNKNOWN_ROUTE", "OTHER_UNKNOWN_ROUTE" }),
            }, new[] { new RmapSpecialWorldPoint(0, 0), new RmapSpecialWorldPoint(0, 1) }));
            Assert.That(invalid.LogicalStateVerified, Is.True);
            Assert.That(invalid.ContactStateVerified, Is.False);
            Assert.That(invalid.ContactChecks.Any(value => value.Classification == "UNKNOWN_ROUTE_REJECTED"), Is.True);
            Assert.That(invalid.ContactChecks.Single(value => value.Id == "AIR_WITNESS_109_EDGE").Classification, Is.EqualTo("INVALID_WITNESS"));

            Sv5RouteStateAnalysis actual = Analyze(SafeCandidates(), ActualReview());
            Sv5RouteContactCheck[] reviews = actual.ContactChecks.Where(value => value.Kind == "REVIEW_LABEL_CONTACT").ToArray();
            Assert.That(reviews.Select(value => value.World), Is.EquivalentTo(new[] { new RmapSpecialWorldPoint(415, 301), new RmapSpecialWorldPoint(491, 301), new RmapSpecialWorldPoint(523, 134) }));
            Assert.That(reviews.All(value => !value.LogicalStateTransitionChecked && value.RequiredPredicate.Length != 0), Is.True);
            Assert.That(actual.ContactStateVerified, Is.False);
            Assert.That(actual.ContactChecks.Single(value => value.Id == "AIR_WITNESS_109_EDGE").Classification, Is.EqualTo("STATIC_AIR_CONTACT_GEOMETRY_PENDING"));
            Assert.That(actual.GeometryStateReady, Is.False);
            Assert.That(actual.PlayerVerified, Is.False);
            Assert.That(actual.Obligations.Select(value => value.OwnerTask), Does.Contain("SV5_06_SPACE_GRAPH"));
            Assert.That(actual.Obligations.Select(value => value.OwnerTask), Does.Contain("SV5_09_LOOPS"));
            Assert.That(actual.Obligations.Select(value => value.OwnerTask), Does.Contain("SV5_41_COMPOSE"));
            Assert.That(actual.Obligations.Select(value => value.OwnerTask), Does.Contain("SV5_44_WORLD_PLAYER"));
        }

        [Test]
        public void T07_AnalysisIsPureDeterministicAndDoesNotWriteHistoricalSv505Evidence()
        {
            string policy = Path.Combine(ProjectRoot(), "Assets", "_Game", "Map", "Runtime", "WorldGeneration", "SectorPlanning", "Sv5RouteStatePolicy.cs");
            string core = Path.Combine(ProjectRoot(), "Assets", "_Game", "Map", "Runtime", "WorldGeneration", "SpecialRegions", "Sv5CoreReservationPlan.cs");
            string historical = Path.Combine(ProjectRoot(), "MapDesign", "MCP", "GENERATED", "SV5_05", "route_state_manifest.json");
            string policyHash = HashFile(policy);
            string coreHash = HashFile(core);
            string historicalHash = HashFile(historical);
            string coreDigest = Plan.Digest;
            Sv5RouteStateAnalysis rejected = Analyze(new[] { Candidate("REJECTED", Existing(Node(RmapWorldGraphRole.Forge)), Existing(Node(RmapWorldGraphRole.Boss)), RmapWorldGraphDirection.Right, 7, true, false, false, "FIX01_T07") }, EmptyReview());
            Sv5RouteStateAnalysis first = Analyze(SafeCandidates(), ActualReview());
            Sv5RouteStateAnalysis second = Analyze(SafeCandidates().Reverse().ToArray(), ActualReview());
            Assert.That(rejected.ShortcutDecisions.Single(value => value.Id == "REJECTED").IsAllowed, Is.False);
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            Assert.That(Plan.Digest, Is.EqualTo(coreDigest));
            Assert.That(HashFile(policy), Is.EqualTo(policyHash));
            Assert.That(HashFile(core), Is.EqualTo(coreHash));
            Assert.That(HashFile(historical), Is.EqualTo(historicalHash));
            Assert.That(GeneratedDirectory(), Is.Not.EqualTo(Path.Combine(ProjectRoot(), "MapDesign", "MCP", "GENERATED", "SV5_05")));
        }

        private static Sv5CoreReservationPlan Plan => Sv5RouteStatePolicyTests.RepresentativePlanForFix01;
        private static Sv5RouteStateAnalysis Analyze(Sv5RouteShortcutCandidate[] candidates, Sv5RouteStateReviewInput review) => Sv5RouteStatePolicy.Analyze(Plan, candidates, review);
        private static Sv5RouteStateReviewInput EmptyReview() => new Sv5RouteStateReviewInput(Array.Empty<Sv5RouteStateReviewContact>(), Array.Empty<RmapSpecialWorldPoint>());
        private static Sv5RouteShortcutCandidate[] SafeCandidates()
        {
            var general = new Sv5RouteStateAnchor("FIX01_EXPORT_GENERAL", Sv5RouteStateAnchorKind.GeneralConnection);
            return new[]
            {
                Candidate("FIX01_SAFE_IN", Existing(Node(RmapWorldGraphRole.Start)), general, RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_EXPORT"),
                Candidate("FIX01_SAFE_OUT", general, Existing(Node(RmapWorldGraphRole.MooncoreOre)), RmapWorldGraphDirection.Right, 0, false, false, false, "FIX01_EXPORT"),
            };
        }
        private static Sv5RouteShortcutCandidate Candidate(string id, Sv5RouteStateAnchor from, Sv5RouteStateAnchor to, RmapWorldGraphDirection direction, ulong resources, bool forge, bool seal, bool boss, string provenance) => new Sv5RouteShortcutCandidate(id, from, to, direction, resources, forge, seal, boss, provenance);
        private static Sv5RouteStateAnchor Existing(string id) => new Sv5RouteStateAnchor(id, Sv5RouteStateAnchorKind.ExistingGraphNode);
        private static string Node(RmapWorldGraphRole role) => Plan.RouteSource.Graph.Nodes.Single(value => value.Role == role).NodeId;

        private static Sv5RouteStateReviewInput ActualReview() => new Sv5RouteStateReviewInput(new[]
        {
            new Sv5RouteStateReviewContact(new RmapSpecialWorldPoint(415, 301), new[] { "0fc428a59647d62495d286c4c25fd66afac209842182354729188041dc5796e3", "16f8c96de43cf0ec7882d78bdcae5b4cc79a1e08fd38fcfbbb7e90b19bfc8998", "21e6a90771dd0ec3a5924c7bed7cedd6df7b87118024ccd550005017a8e7ebd0", "74004c50147eb4af54c7c3beb9c82282fb0d23628aa3c2e2a588301c04e965f4" }),
            new Sv5RouteStateReviewContact(new RmapSpecialWorldPoint(491, 301), new[] { "74004c50147eb4af54c7c3beb9c82282fb0d23628aa3c2e2a588301c04e965f4", "a98881f4240397805efa4bd7a49fec0d564a271544864f5fdd66657aed45cdff" }),
            new Sv5RouteStateReviewContact(new RmapSpecialWorldPoint(523, 134), new[] { "0fc428a59647d62495d286c4c25fd66afac209842182354729188041dc5796e3", "29dc755094886d48300e9f8cf0fb0e54f09125a29c2fac78855bda7885f7dbaa" }),
        }, ReadAirWitness());
        private static RmapSpecialWorldPoint[] ReadAirWitness()
        {
            string text = File.ReadAllText(Path.Combine(ProjectRoot(), "MapDesign", "MCP", "INPUTS", "SV5_05", "REVIEW_FINDINGS.json"), Encoding.UTF8);
            int air = text.IndexOf("\"air_contact_witness\"", StringComparison.Ordinal);
            int cells = text.IndexOf("\"cells\": [", air, StringComparison.Ordinal);
            int end = text.IndexOf("\"excluded_sealed_cells\"", cells, StringComparison.Ordinal);
            MatchCollection matches = Regex.Matches(text.Substring(cells, end - cells), @"\[\s*(\d+)\s*,\s*(\d+)\s*\]");
            RmapSpecialWorldPoint[] points = matches.Cast<Match>().Select(value => new RmapSpecialWorldPoint(int.Parse(value.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(value.Groups[2].Value, CultureInfo.InvariantCulture))).ToArray();
            Assert.That(points.Length, Is.EqualTo(110));
            return points;
        }
        private static void WriteOutputs(Sv5RouteStateAnalysis analysis)
        {
            string directory = GeneratedDirectory();
            Directory.CreateDirectory(directory);
            Write(Path.Combine(directory, "analysis.json"), Sv5RouteStateExport.AnalysisJson(analysis));
            Write(Path.Combine(directory, "condition_bindings.csv"), Sv5RouteStateExport.ConditionBindingsCsv(analysis));
            Write(Path.Combine(directory, "order_proofs.csv"), Sv5RouteStateExport.OrderProofsCsv(analysis));
            Write(Path.Combine(directory, "candidate_set_proofs.csv"), Sv5RouteStateExport.CandidateSetProofsCsv(analysis));
            Write(Path.Combine(directory, "state_traces.csv"), Sv5RouteStateExport.StateTracesCsv(analysis));
            Write(Path.Combine(directory, "shortcut_decisions.csv"), Sv5RouteStateExport.ShortcutDecisionsCsv(analysis));
            Write(Path.Combine(directory, "contact_checks.csv"), Sv5RouteStateExport.ContactChecksCsv(analysis));
            Write(Path.Combine(directory, "obligations.csv"), Sv5RouteStateExport.ObligationsCsv(analysis));
            Write(Path.Combine(directory, "route_state_manifest.json"), Sv5RouteStateExport.ManifestJson(analysis));
        }
        private static string GeneratedDirectory() => Path.Combine(ProjectRoot(), "MapDesign", "MCP", "GENERATED",
            "SV5_06_FIX02", "legacy_exports", "sv5_05_fix01_t05");
        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static void Write(string path, string text) => File.WriteAllText(path, text, new UTF8Encoding(false));
        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
#endif
