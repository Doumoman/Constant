using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP05_06")]
    public sealed class UpDownConflictResolverTests
    {
        private VerticalGatewayPlan verticalPlan;
        private MandatoryRouteMaskLookup lookup;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private UpDownConflictResolver reused;
        private string expectedSignature;

        public static IEnumerable DeterminismCases
        {
            get
            {
                for (var index = 0; index < 160; index++)
                    yield return new TestCaseData(index).SetName("Build_DeterministicType4ResolutionPlan_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable InvalidIds => new[]
        {
            null, string.Empty, "UDC_0_X", "udc_00_X", "UDC_000_X", "UDC_00_", "UDC_A0_X", "UDC_0A_X",
            "UDC_00_x", "UDC_00_A-B", "UDC00_X", "UDC_00_A B", "UDC_99_한글", "UDC_00_A/B", "UDC_00_A.B"
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var terminalFixture = new MandatoryTerminalBuilderTests();
            terminalFixture.OneTimeSetUp();
            site = GetField<SiteReservationSnapshot>(terminalFixture, typeof(MandatoryTerminalBuilderTests), "site");
            biome = GetField<BiomePatchValidationPublication>(terminalFixture, typeof(MandatoryTerminalBuilderTests), "biome");
            var terminalResult = new MandatoryTerminalBuilder().Build(site, biome);
            Assert.That(terminalResult.Succeeded, Is.True);
            lookup = BuildStarterLookup();
            var tree = new MandatoryConnectorTreeBuilder().Build(terminalResult.TerminalSet, lookup).Tree;
            var horizontal = new HorizontalBackboneRouter().Build(tree, lookup, site, biome).Plan;
            verticalPlan = new VerticalGatewayPlanner().Build(horizontal, lookup, site, biome).Plan;
            Assert.That(verticalPlan, Is.Not.Null);
            reused = new UpDownConflictResolver();
            expectedSignature = Signature(Complete(reused));
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void Build_DeterministicType4ResolutionPlan(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0 ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("tr-TR");
                var resolver = (caseId & 2) == 0 ? new UpDownConflictResolver() : reused;
                var result = Complete(resolver);
                Assert.That(Signature(result), Is.EqualTo(expectedSignature));
                Assert.That(result.Plan.Candidates.Select(value => value.ConflictId.Value), Is.Ordered.Using<string>(StringComparer.Ordinal));
                Assert.That(result.Diagnostics.RngDrawCount + result.Diagnostics.FileWriteCount + result.Diagnostics.RouteMaskWriteCount +
                    result.Diagnostics.GraphWriteCount + result.Diagnostics.SourceMutationCount, Is.Zero);
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestCaseSource(nameof(InvalidIds))]
        public void ConflictIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(UpDownConflictId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new UpDownConflictId(value));
            else Assert.Throws<ArgumentException>(() => new UpDownConflictId(value));
        }

        [TestCase("UDC_00_ALPHA")]
        [TestCase("UDC_09_GATEWAY_A")]
        [TestCase("UDC_99_Z9")]
        public void ConflictIdHasOrdinalValueEqualityOrderAndHash(string value)
        {
            var first = new UpDownConflictId(value);
            var second = new UpDownConflictId(new string(value.ToCharArray()));
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first.CompareTo(second), Is.Zero);
            Assert.That(first.Value, Is.EqualTo(value));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void AllHorizontalCombinationsPreserveLeftRightAndAcceptMandatoryUpDown(bool opensLeft, bool opensRight)
        {
            var candidate = Candidate("UDC_00_COMBO", new SectorCoord(6, 6), opensLeft, opensRight, true, false);
            Assert.That(candidate.RequiresUp && candidate.RequiresDown, Is.True);
            Assert.That(candidate.OpensLeft, Is.EqualTo(opensLeft));
            Assert.That(candidate.OpensRight, Is.EqualTo(opensRight));
            Assert.That(candidate.CanBeType4, Is.True);
            Assert.That(candidate.IsConflict, Is.False);
        }

        [Test]
        public void StarterFourGatewayPairsPublishElevenType4CandidatesAndNoConflictsOrResolutions()
        {
            var result = Complete(reused);
            Assert.That(verticalPlan.GatewayPairCount, Is.EqualTo(4));
            Assert.That(result.Plan.CandidateCount, Is.EqualTo(11));
            Assert.That(result.Plan.Type4ExpressibleCount, Is.EqualTo(11));
            Assert.That(result.Plan.ConflictCount, Is.Zero);
            Assert.That(result.Plan.ResolvedCount, Is.Zero);
            Assert.That(result.Plan.UnresolvedCount, Is.Zero);
            Assert.That(result.Plan.TotalCost, Is.Zero);
            Assert.That(result.Plan.Candidates.All(value => value.RequiresUp && value.RequiresDown && value.CanBeType4), Is.True);
        }

        [Test]
        public void PlanPreservesAllSourceArtifactIdentitiesAndProvidesExactLookup()
        {
            var plan = Complete(reused).Plan;
            Assert.That(plan.SourceVerticalGatewayPlan, Is.SameAs(verticalPlan));
            Assert.That(plan.SourceRouteMaskLookup, Is.SameAs(lookup));
            Assert.That(plan.SourceSiteSnapshot, Is.SameAs(site));
            Assert.That(plan.SourceBiomePublication, Is.SameAs(biome));
            foreach (var candidate in plan.Candidates)
            {
                Assert.That(plan.TryGetCandidate(candidate.ConflictId, out var found), Is.True);
                Assert.That(found, Is.SameAs(candidate));
                Assert.That(plan.TryGetResolution(candidate.ConflictId, out _), Is.False);
            }
        }

        [Test]
        public void SyntheticForbiddenCandidateUsesDeterministicLowerXAdjacentAdapterPair()
        {
            var coordinate = FindResolvableCoordinate();
            var candidate = Candidate("UDC_00_SYNTHETIC", coordinate, true, true, false, true);
            var result = Complete(new UpDownConflictResolver(), new[] { candidate });
            Assert.That(result.Plan.ConflictCount, Is.EqualTo(1));
            Assert.That(result.Plan.ResolvedCount, Is.EqualTo(1));
            Assert.That(result.Plan.UnresolvedCount, Is.Zero);
            var resolution = result.Plan.Resolutions.Single();
            Assert.That(resolution.ConflictId, Is.EqualTo(candidate.ConflictId));
            Assert.That(resolution.SourceGatewayId, Is.EqualTo(candidate.SourceGatewayId));
            Assert.That(resolution.InclusiveSpan.Count, Is.EqualTo(3));
            Assert.That(resolution.Upper.Coord.X, Is.LessThan(coordinate.X));
            Assert.That(resolution.Lower.Coord.X, Is.EqualTo(resolution.Upper.Coord.X));
            Assert.That(resolution.Reason, Is.EqualTo("TYPE4_FORBIDDEN_ADJACENT_GATEWAY"));
        }

        [Test]
        public void Type4SyntheticCandidateNeverCreatesResolutionOrChangesLeftRight()
        {
            var candidate = Candidate("UDC_00_TYPE4", new SectorCoord(6, 6), false, true, true, false);
            var result = Complete(new UpDownConflictResolver(), new[] { candidate });
            Assert.That(result.Plan.Type4ExpressibleCount, Is.EqualTo(1));
            Assert.That(result.Plan.ConflictCount, Is.Zero);
            Assert.That(result.Plan.Resolutions, Is.Empty);
            Assert.That(result.Plan.Candidates[0].OpensLeft, Is.False);
            Assert.That(result.Plan.Candidates[0].OpensRight, Is.True);
        }

        [TestCase(0)]
        [TestCase(12)]
        public void BoundaryConflictRemainsStableAndUnresolved(int y)
        {
            var candidate = Candidate("UDC_00_BOUNDARY", new SectorCoord(6, y), true, false, false, true);
            var result = Complete(new UpDownConflictResolver(), new[] { candidate });
            Assert.That(result.Plan.ConflictCount, Is.EqualTo(1));
            Assert.That(result.Plan.ResolvedCount, Is.Zero);
            Assert.That(result.Plan.UnresolvedCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateConflictIdentityReturnsInvalidInputWithoutPlan()
        {
            var coordinate = FindResolvableCoordinate();
            var first = Candidate("UDC_00_DUPLICATE", coordinate, true, false, false, true);
            var second = Candidate("UDC_00_DUPLICATE", coordinate, false, true, false, true);
            var result = reused.Build(verticalPlan, lookup, site, biome, new[] { first, second });
            Assert.That(result.Status, Is.EqualTo(UpDownConflictBuildStatus.InvalidInput));
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(UpDownConflictBuildErrorCode.DuplicateConflictId));
            Assert.That(result.RetryRequired, Is.False);
        }

        [Test]
        public void MissingInputsReturnAllDeterministicErrors()
        {
            var result = reused.Build(null, null, null, null);
            Assert.That(result.Status, Is.EqualTo(UpDownConflictBuildStatus.InvalidInput));
            Assert.That(result.Errors.Count, Is.EqualTo(4));
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
        }

        [Test]
        public void RouteLookupIdentityMismatchIsRejected()
        {
            var other = BuildStarterLookup();
            Assert.That(other, Is.Not.SameAs(lookup));
            var result = reused.Build(verticalPlan, other, site, biome);
            Assert.That(result.Status, Is.EqualTo(UpDownConflictBuildStatus.InvalidInput));
            Assert.That(result.Errors.Single().Code, Is.EqualTo(UpDownConflictBuildErrorCode.SourceIdentityMismatch));
        }

        [Test]
        public void SourceArtifactsRemainByteSemanticUnchangedAcrossBuilds()
        {
            var before = SourceSignature();
            for (var index = 0; index < 8; index++) Complete(reused);
            Assert.That(SourceSignature(), Is.EqualTo(before));
        }

        [Test]
        public void FreshReusedAndParallelBuildsAreIdentical()
        {
            var values = new string[12];
            Parallel.For(0, values.Length, index => values[index] = Signature(Complete((index & 1) == 0 ? new UpDownConflictResolver() : reused)));
            Assert.That(values.Distinct().Single(), Is.EqualTo(expectedSignature));
        }

        [Test]
        public void RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap06_03PlusSymbols()
        {
            var types = new[]
            {
                typeof(UpDownConflictId), typeof(UpDownConflictCandidate), typeof(UpDownConflictResolution),
                typeof(UpDownConflictResolutionPlan), typeof(UpDownConflictBuildError), typeof(UpDownConflictDiagnostics),
                typeof(UpDownConflictBuildResult), typeof(UpDownConflictResolver)
            };
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            var assembly = typeof(UpDownConflictResolver).Assembly;
            Assert.That(assembly.GetReferencedAssemblies().Any(value => value.Name == "UnityEditor"), Is.False);
            var names = string.Join("|", assembly.GetTypes().Select(value => value.Name));
            foreach (var forbidden in new[]
            {
                "MandatoryRoutePass", "SectorRouteMaskAssigner",
                "OptionalReturnConnection", "OptionalClueAssigner", "OptionalRegionValidationOverlayWindow"
            })
                Assert.That(names, Does.Not.Contain(forbidden));
        }

        private UpDownConflictBuildResult Complete(UpDownConflictResolver resolver)
        {
            var result = resolver.Build(verticalPlan, lookup, site, biome);
            Assert.That(result.Status, Is.EqualTo(UpDownConflictBuildStatus.Completed), FormatErrors(result));
            return result;
        }

        private UpDownConflictBuildResult Complete(UpDownConflictResolver resolver, IEnumerable<UpDownConflictCandidate> candidates)
        {
            var result = resolver.Build(verticalPlan, lookup, site, biome, candidates);
            Assert.That(result.Status, Is.EqualTo(UpDownConflictBuildStatus.Completed), FormatErrors(result));
            return result;
        }

        private UpDownConflictCandidate Candidate(string id, SectorCoord coordinate, bool left, bool right, bool eligible, bool reserved) =>
            new UpDownConflictCandidate(new UpDownConflictId(id), verticalPlan.GatewayPairs[0].GatewayId, coordinate,
                true, true, left, right, eligible, reserved, reserved ? "SYNTHETIC_RESERVATION" : string.Empty,
                "SYNTHETIC_BIOME", reserved ? 8 : 1);

        private SectorCoord FindResolvableCoordinate()
        {
            for (var y = 1; y < 12; y++)
            for (var x = 1; x < 12; x++)
            {
                var left = x - 1;
                if (!site.GetSector(new SectorCoord(left, y + 1)).IsReserved &&
                    !site.GetSector(new SectorCoord(left, y)).IsReserved &&
                    !site.GetSector(new SectorCoord(left, y - 1)).IsReserved &&
                    !site.GetSector(new SectorCoord(x + 1, y + 1)).IsReserved &&
                    !site.GetSector(new SectorCoord(x + 1, y)).IsReserved &&
                    !site.GetSector(new SectorCoord(x + 1, y - 1)).IsReserved)
                    return new SectorCoord(x, y);
            }
            throw new InvalidOperationException("Starter fixture has no resolvable synthetic coordinate.");
        }

        private string SourceSignature() =>
            verticalPlan.GatewayPairCount + "|" + verticalPlan.Type4JunctionCellCount + "|" + verticalPlan.TotalCost + "|" +
            lookup.Count + "|" + site.Sectors.Count + "|" + site.Reservations.Count + "|" +
            biome.Snapshot.Sectors.Count + "|" + biome.Snapshot.Patches.Count;

        private static string Signature(UpDownConflictBuildResult result) =>
            result.Status + "|" + string.Join(",", result.Errors.Select(value => value.Code + ":" + value.SourceId + ":" + value.Message)) + "|" +
            (result.Plan == null ? "null" : string.Join("/", result.Plan.Candidates.Select(value =>
                value.ConflictId.Value + ":" + value.SourceGatewayId.Value + ":" + value.Coordinate.X + ":" + value.Coordinate.Y + ":" +
                (value.OpensLeft ? "L" : "-") + (value.OpensRight ? "R" : "-") + "UD:" + value.CanBeType4)) + "|" +
                string.Join("/", result.Plan.Resolutions.Select(value => value.ConflictId.Value + ":" + value.Upper.Coord.X + ":" + value.CheckedCost)) + "|" +
                result.Plan.ConflictCount + ":" + result.Plan.ResolvedCount + ":" + result.Plan.Type4ExpressibleCount + ":" + result.Plan.UnresolvedCount + ":" + result.Plan.TotalCost) + "|" +
            (result.Diagnostics == null ? "null" : result.Diagnostics.GatewayPairCount + ":" + result.Diagnostics.CandidateCount + ":" +
                result.Diagnostics.Type4ExpressibleCount + ":" + result.Diagnostics.ConflictCount + ":" + result.Diagnostics.ResolvedCount + ":" +
                result.Diagnostics.UnresolvedCount + ":" + result.Diagnostics.AdjacentCandidateEvaluationCount + ":" + result.Diagnostics.RngDrawCount + ":" + result.Diagnostics.SourceMutationCount);

        private static MandatoryRouteMaskLookup BuildStarterLookup()
        {
            var method = typeof(MandatoryRouteMaskLookupBuilderTests).GetMethod("BuildStarter", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return ((MandatoryRouteMaskLookupBuildResult)method.Invoke(null, null)).Lookup;
        }

        private static string FormatErrors(UpDownConflictBuildResult result) => string.Join("\n", result.Errors.Select(value => value.Code + " " + value.Message));
        private static T GetField<T>(object target, Type type, string name) => (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
