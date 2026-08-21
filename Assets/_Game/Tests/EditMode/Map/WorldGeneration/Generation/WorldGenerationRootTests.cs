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

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Generation
{
    public sealed class WorldGenerationRootTests
    {
        [Test]
        public void FailurePolicy_HasExactOrderedValues()
        {
            CollectionAssert.AreEqual(
                new[] { "FailWorld", "RetryPass", "RetryScope", "ReportOnly" },
                Enum.GetNames(typeof(WorldGenerationFailurePolicy)));
        }

        [TestCase("FAIL_WORLD", WorldGenerationFailurePolicy.FailWorld)]
        [TestCase("RETRY_PASS", WorldGenerationFailurePolicy.RetryPass)]
        [TestCase("RETRY_SCOPE", WorldGenerationFailurePolicy.RetryScope)]
        [TestCase("REPORT_ONLY", WorldGenerationFailurePolicy.ReportOnly)]
        public void FailurePolicy_ParseUsesExactToken(string token, WorldGenerationFailurePolicy expected)
        {
            Assert.That(WorldGenerationFailurePolicyToken.Parse(token), Is.EqualTo(expected));
        }

        [TestCase(WorldGenerationFailurePolicy.FailWorld, "FAIL_WORLD")]
        [TestCase(WorldGenerationFailurePolicy.RetryPass, "RETRY_PASS")]
        [TestCase(WorldGenerationFailurePolicy.RetryScope, "RETRY_SCOPE")]
        [TestCase(WorldGenerationFailurePolicy.ReportOnly, "REPORT_ONLY")]
        public void FailurePolicy_FormatUsesExactToken(WorldGenerationFailurePolicy policy, string expected)
        {
            Assert.That(WorldGenerationFailurePolicyToken.Format(policy), Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("fail_world")]
        [TestCase("FailWorld")]
        [TestCase(" FAIL_WORLD")]
        [TestCase("FAIL_WORLD ")]
        [TestCase("0")]
        [TestCase("UNKNOWN")]
        public void FailurePolicy_ParseRejectsMismatch(string token)
        {
            Assert.Throws<ArgumentException>(() => WorldGenerationFailurePolicyToken.Parse(token));
        }

        [Test]
        public void FailurePolicy_FormatRejectsUndefinedValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldGenerationFailurePolicyToken.Format((WorldGenerationFailurePolicy)999));
        }

        [Test]
        public void ArtifactStore_EmptySnapshotIsSupported()
        {
            var store = new WorldGenerationArtifactStore();
            Assert.That(store.Count, Is.Zero);
            Assert.That(store.ArtifactIds, Is.Empty);
        }

        [Test]
        public void ArtifactStore_EnumeratesIdsInOrdinalOrder()
        {
            var store = Store(Pair("Z", new object()), Pair("A", new object()));
            CollectionAssert.AreEqual(new[] { "A", "Z" }, store.ArtifactIds);
        }

        [Test]
        public void ArtifactStore_SnapshotsCallerCollection()
        {
            var source = new List<KeyValuePair<string, object>> { Pair("A", new object()) };
            var store = new WorldGenerationArtifactStore(source);
            source.Clear();
            Assert.That(store.Count, Is.EqualTo(1));
        }

        [Test]
        public void ArtifactStore_RejectsNullValue()
        {
            Assert.Throws<ArgumentException>(() => Store(Pair("A", null)));
        }

        [Test]
        public void ArtifactStore_RejectsDuplicateId()
        {
            Assert.Throws<ArgumentException>(() => Store(Pair("A", 1), Pair("A", 2)));
        }

        [TestCase(null)]
        [TestCase("")]
        public void ArtifactStore_RejectsEmptyId(string id)
        {
            Assert.Throws<ArgumentException>(() => Store(Pair(id, 1)));
        }

        [Test]
        public void ArtifactStore_UsesExactCaseSensitiveIds()
        {
            var store = Store(Pair("GRID", 1));
            Assert.That(store.Contains("GRID"), Is.True);
            Assert.That(store.Contains("grid"), Is.False);
        }

        [Test]
        public void ArtifactStore_GetReturnsExactInstance()
        {
            var value = new object();
            Assert.That(Store(Pair("A", value)).Get("A"), Is.SameAs(value));
        }

        [Test]
        public void ArtifactStore_GetRejectsMissingId()
        {
            Assert.Throws<KeyNotFoundException>(() => new WorldGenerationArtifactStore().Get("A"));
        }

        [Test]
        public void ArtifactStore_TypedGetReturnsExactType()
        {
            Assert.That(Store(Pair("A", "value")).Get<string>("A"), Is.EqualTo("value"));
        }

        [Test]
        public void ArtifactStore_TypedGetRejectsWrongType()
        {
            Assert.Throws<InvalidCastException>(() => Store(Pair("A", "value")).Get<int>("A"));
        }

        [Test]
        public void ArtifactStore_TypedTryGetRejectsWrongTypeWithoutThrowing()
        {
            Assert.That(Store(Pair("A", "value")).TryGet<int>("A", out var value), Is.False);
            Assert.That(value, Is.Zero);
        }

        [Test]
        public void ArtifactStore_IdViewIsReadOnly()
        {
            var ids = (IList<string>)Store(Pair("A", 1)).ArtifactIds;
            Assert.Throws<NotSupportedException>(() => ids.Add("B"));
        }

        [Test]
        public void PassResult_SuccessCopiesAndSortsExactOutputs()
        {
            var source = new List<KeyValuePair<string, object>> { Pair("Z", 1), Pair("A", 2) };
            var result = WorldGenerationPassResult.Success(source);
            source.Clear();
            Assert.That(result.Succeeded, Is.True);
            CollectionAssert.AreEqual(new[] { "A", "Z" }, result.Outputs.Keys);
            Assert.That(result.FailureCode, Is.Empty);
            Assert.That(result.FailureMessage, Is.Empty);
            Assert.That(result.RetryScopeId, Is.Empty);
        }

        [Test]
        public void PassResult_FailureHasEmptyOutputsAndExactFields()
        {
            var result = WorldGenerationPassResult.Failure("CODE_1", "message", "6,6");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outputs, Is.Empty);
            Assert.That(result.FailureCode, Is.EqualTo("CODE_1"));
            Assert.That(result.FailureMessage, Is.EqualTo("message"));
            Assert.That(result.RetryScopeId, Is.EqualTo("6,6"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void PassResult_FailureRejectsEmptyCode(string code)
        {
            Assert.Throws<ArgumentException>(() => WorldGenerationPassResult.Failure(code, "message"));
        }

        [Test]
        public void PassContext_PreservesExactInputsAndAttemptFields()
        {
            var fixture = CreateFixture(new[] { Pass("P", 0, "Fake", "", new[] { "I" }, new[] { "A" }) });
            var inputs = Store(Pair("I", new object()));
            var context = new WorldGenerationPassContext(
                7, fixture.StaticData, fixture.Profile, fixture.Passes[0], inputs,
                new WorldGenerationRngStreams(fixture.StaticData), 3, "scope");
            Assert.That(context.WorldSeed, Is.EqualTo(7UL));
            Assert.That(context.StaticData, Is.SameAs(fixture.StaticData));
            Assert.That(context.GenerationProfile, Is.SameAs(fixture.Profile));
            Assert.That(context.PassDefinition, Is.SameAs(fixture.Passes[0]));
            Assert.That(context.Inputs, Is.SameAs(inputs));
            Assert.That(context.AttemptOrdinal, Is.EqualTo(3));
            Assert.That(context.RetryScopeId, Is.EqualTo("scope"));
        }

        [Test]
        public void PassContext_RejectsNegativeAttemptOrdinal()
        {
            var fixture = CreateFixture(new[] { Pass("P", 0, "Fake", "", Empty, new[] { "A" }) });
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldGenerationPassContext(
                0, fixture.StaticData, fixture.Profile, fixture.Passes[0],
                new WorldGenerationArtifactStore(), new WorldGenerationRngStreams(fixture.StaticData), -1, ""));
        }

        [Test]
        public void PassRegistry_SnapshotsAndSortsOrdinalIds()
        {
            var registry = new WorldGenerationPassRegistry(new IWorldGenerationPass[]
            {
                Fake("Z", "ZClass"), Fake("A", "AClass")
            });
            CollectionAssert.AreEqual(new[] { "A", "Z" }, registry.PassIds);
            Assert.That(registry.Get("A").ClassName, Is.EqualTo("AClass"));
        }

        [Test]
        public void PassRegistry_RejectsDuplicatePassId()
        {
            Assert.Throws<ArgumentException>(() => new WorldGenerationPassRegistry(new IWorldGenerationPass[]
            {
                Fake("A", "First"), Fake("A", "Second")
            }));
        }

        [Test]
        public void PassRegistry_RejectsNullImplementation()
        {
            Assert.Throws<ArgumentException>(() =>
                new WorldGenerationPassRegistry(new IWorldGenerationPass[] { null }));
        }

        [Test]
        public void PassRegistry_ExtraImplementationIsNotAutoExecuted()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" });
            var expected = Fake("P", "Fake", _ => WorldGenerationPassResult.Success("A", new object()));
            var extra = Fake("EXTRA", "Extra");
            var fixture = CreateFixture(new[] { definition });
            var result = Root(fixture, expected, extra).Execute(ProfileId, 1);
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
            Assert.That(expected.InvocationCount, Is.EqualTo(1));
            Assert.That(extra.InvocationCount, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        public void PassRegistry_RejectsEmptyPassId(string passId)
        {
            Assert.Throws<ArgumentException>(() =>
                new WorldGenerationPassRegistry(new[] { Fake(passId, "Class") }));
        }

        [Test]
        public void ProductionRegistry_ContainsOnlyGridAdapter()
        {
            var registry = WorldGenerationPassRegistry.CreateProduction();
            Assert.That(registry.Count, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { "PASS_GRID" }, registry.PassIds);
            Assert.That(registry.Get("PASS_GRID"), Is.TypeOf<GridInitializationPassAdapter>());
        }

        [Test]
        public void GridAdapter_HasExactDefinitionIdentity()
        {
            var adapter = new GridInitializationPassAdapter();
            Assert.That(adapter.PassId, Is.EqualTo("PASS_GRID"));
            Assert.That(adapter.ClassName, Is.EqualTo("GridInitializationPass"));
        }

        [Test]
        public void GridAdapter_ExecutesExistingPassExactlyIntoGridArtifact()
        {
            var definition = Pass("PASS_GRID", 0, "GridInitializationPass", "", Empty, new[] { "GRID" });
            var fixture = CreateFixture(new[] { definition });
            var context = Context(fixture, definition, 123);
            var result = new GridInitializationPassAdapter().Execute(context);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Outputs.Keys, Is.EqualTo(new[] { "GRID" }));
            Assert.That(((GridInitializationResult)result.Outputs["GRID"]).WorldData.Seed, Is.EqualTo(123UL));
        }

        [Test]
        public void StarterPlan_HasExactFrozenTenRows()
        {
            var snapshot = StarterPasses().Select(item => string.Join("/", new[]
            {
                item.PassOrder.ToString(CultureInfo.InvariantCulture),
                item.PassId,
                item.ClassName,
                item.RngStreamId,
                string.Join("|", item.InputArtifacts),
                string.Join("|", item.OutputArtifacts),
                item.FailurePolicy,
                item.MaxRetryCount.ToString(CultureInfo.InvariantCulture)
            }));
            CollectionAssert.AreEqual(new[]
            {
                "0/PASS_GRID/GridInitializationPass///GRID/FAIL_WORLD/1",
                "10/PASS_SITE/SpecialSiteReservationPass/RNG_WORLD_SITE/GRID/SITE_RESERVATIONS/RETRY_PASS/200",
                "20/PASS_BIOME/BiomePatchPass/RNG_BIOME_PATCH/GRID|SITE_RESERVATIONS/BIOME_PATCHES/RETRY_PASS/100",
                "30/PASS_ROUTE/MandatoryRoutePass/RNG_ROUTE/SITE_RESERVATIONS|BIOME_PATCHES/ROUTE123/RETRY_PASS/200",
                "40/PASS_TYPE0/OptionalRegionPass/RNG_TYPE0/ROUTE123/TYPE0_REGIONS/RETRY_PASS/100",
                "50/PASS_SECTOR_RECIPE/SectorRecipePass/RNG_SECTOR_RECIPE/BIOME_PATCHES|ROUTE123|TYPE0_REGIONS/SECTOR_RECIPES/RETRY_SCOPE/20",
                "60/PASS_MICRO_SOLVE/SectorConstraintPass/RNG_SECTOR_RECIPE/SECTOR_RECIPES/MICROCHUNKS/RETRY_SCOPE/20",
                "70/PASS_BAKE/TilemapBakePass//MICROCHUNKS/BAKED_TILES/FAIL_WORLD/1",
                "80/PASS_POPULATION/PopulationPass/RNG_POPULATION/BAKED_TILES/SPAWNS/RETRY_SCOPE/10",
                "90/PASS_VALIDATION/WorldValidationPass//SPAWNS/VALIDATION/FAIL_WORLD/1"
            }, snapshot);
        }

        [Test]
        public void Root_MissingProfileFailsWithoutInvokingPass()
        {
            var fake = Fake("P", "Fake");
            var fixture = CreateFixture(new[] { Pass("P", 0, "Fake", "", Empty, new[] { "A" }) });
            var result = Root(fixture, fake).Execute("MISSING", 1);
            AssertPlanFailure(result, "MISSING_PROFILE");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_InactiveProfileFailsPreInvocation()
        {
            var fake = Fake("P", "Fake");
            var fixture = CreateFixture(
                new[] { Pass("P", 0, "Fake", "", Empty, new[] { "A" }) }, profileActive: false);
            AssertPlanFailure(Root(fixture, fake).Execute(ProfileId, 1), "INACTIVE_PROFILE");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_MissingWorldProfileFailsPreInvocation()
        {
            var fake = Fake("P", "Fake");
            var fixture = CreateFixture(
                new[] { Pass("P", 0, "Fake", "", Empty, new[] { "A" }) }, includeWorld: false);
            AssertPlanFailure(Root(fixture, fake).Execute(ProfileId, 1), "MISSING_WORLD_PROFILE");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_InactiveWorldProfileFailsPreInvocation()
        {
            var fake = Fake("P", "Fake");
            var fixture = CreateFixture(
                new[] { Pass("P", 0, "Fake", "", Empty, new[] { "A" }) }, worldActive: false);
            AssertPlanFailure(Root(fixture, fake).Execute(ProfileId, 1), "INACTIVE_WORLD_PROFILE");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_UnknownThroughPassFailsPreInvocation()
        {
            var fake = Fake("P", "Fake");
            var fixture = CreateFixture(new[] { Pass("P", 0, "Fake", "", Empty, new[] { "A" }) });
            AssertPlanFailure(Root(fixture, fake).ExecuteThrough(ProfileId, 1, "UNKNOWN"), "UNKNOWN_THROUGH_PASS");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_DisabledPassIsNotExecutedOrRequired()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" });
            SetAutoProperty(definition, "Enabled", false);
            var fake = Fake("P", "Fake");
            var fixture = CreateFixture(new[] { definition });
            var result = Root(fixture, fake).Execute(ProfileId, 1);
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
            Assert.That(result.Artifacts.Count, Is.Zero);
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_ProductionGridPrefixSucceeds()
        {
            var fixture = CreateFixture(StarterPasses());
            var result = new WorldGenerationRoot(fixture.StaticData, WorldGenerationPassRegistry.CreateProduction())
                .ExecuteThrough(ProfileId, 55, "PASS_GRID");
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.LastCompletedPassId, Is.EqualTo("PASS_GRID"));
            Assert.That(result.Artifacts.Get<GridInitializationResult>("GRID").WorldData.Seed, Is.EqualTo(55UL));
        }

        [Test]
        public void Root_ProductionFullPlanFailsBeforeGridInvocation()
        {
            var fixture = CreateFixture(StarterPasses());
            var result = new WorldGenerationRoot(fixture.StaticData, WorldGenerationPassRegistry.CreateProduction())
                .Execute(ProfileId, 55);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Artifacts.Count, Is.Zero);
            Assert.That(result.LastCompletedPassId, Is.Empty);
            Assert.That(result.Issues.Any(item => item.Code == "MISSING_PASS_IMPLEMENTATION"), Is.True);
        }

        [Test]
        public void Root_FullStarterPlanWithNineFakesSucceeds()
        {
            var fixture = CreateFixture(StarterPasses());
            var registry = FullStarterRegistry(null);
            var result = new WorldGenerationRoot(fixture.StaticData, registry).Execute(ProfileId, 77);
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
            Assert.That(result.Artifacts.Count, Is.EqualTo(10));
            Assert.That(result.LastCompletedPassId, Is.EqualTo("PASS_VALIDATION"));
            Assert.That(result.Issues, Is.Empty);
        }

        [Test]
        public void Root_UsesExactFrozenStarterOrder()
        {
            var invoked = new List<string>();
            var fixture = CreateFixture(StarterPasses().Reverse());
            var result = new WorldGenerationRoot(fixture.StaticData, FullStarterRegistry(invoked)).Execute(ProfileId, 1);
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
            CollectionAssert.AreEqual(StarterPasses().Select(item => item.PassId), invoked);
        }

        [Test]
        public void Root_RetryPassUsesAdditionalRetryCountAndEmptyScope()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "RETRY_PASS", 2);
            var fake = Fake("P", "Fake", context => context.AttemptOrdinal < 2
                ? WorldGenerationPassResult.Failure("NO", "retry", "ignored")
                : WorldGenerationPassResult.Success("A", new object()));
            var result = ExecuteSingle(definition, fake);
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, fake.Contexts.Select(item => item.AttemptOrdinal));
            CollectionAssert.AreEqual(new[] { "", "", "" }, fake.Contexts.Select(item => item.RetryScopeId));
        }

        [Test]
        public void Root_RetryPassReusesExactImmutableInputSnapshot()
        {
            var first = Pass("A", 0, "First", "", Empty, new[] { "ONE" });
            var retry = Pass("B", 1, "Retry", "", new[] { "ONE" }, new[] { "TWO" }, "RETRY_PASS", 1);
            var second = Fake("B", "Retry", context => context.AttemptOrdinal == 0
                ? WorldGenerationPassResult.Failure("NO", "again")
                : WorldGenerationPassResult.Success("TWO", new object()));
            var fixture = CreateFixture(new[] { first, retry });
            var result = Root(
                fixture,
                Fake("A", "First", _ => WorldGenerationPassResult.Success("ONE", new object())),
                second).Execute(ProfileId, 1);
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
            Assert.That(second.Contexts[0].Inputs, Is.SameAs(second.Contexts[1].Inputs));
        }

        [Test]
        public void Root_RetryScopeCarriesExactFailureScope()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "RETRY_SCOPE", 1);
            var fake = Fake("P", "Fake", context => context.AttemptOrdinal == 0
                ? WorldGenerationPassResult.Failure("NO", "retry", " 6,6 ")
                : WorldGenerationPassResult.Success("A", new object()));
            var result = ExecuteSingle(definition, fake);
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
            CollectionAssert.AreEqual(new[] { "", " 6,6 " }, fake.Contexts.Select(item => item.RetryScopeId));
        }

        [Test]
        public void Root_RetryScopeWithoutScopeFailsTerminally()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "RETRY_SCOPE", 1);
            var fake = Fake("P", "Fake", _ => WorldGenerationPassResult.Failure("NO", "retry"));
            var result = ExecuteSingle(definition, fake);
            AssertTerminal(result, "MISSING_RETRY_SCOPE", 0);
            Assert.That(fake.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void Root_RetryExhaustionUsesFinalAttemptOrdinal()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "RETRY_PASS", 2);
            var fake = Fake("P", "Fake", _ => WorldGenerationPassResult.Failure("NO", "last"));
            var result = ExecuteSingle(definition, fake);
            AssertTerminal(result, "RETRY_EXHAUSTED", 2);
            Assert.That(fake.InvocationCount, Is.EqualTo(3));
        }

        [Test]
        public void Root_FailWorldDoesNotRetry()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "FAIL_WORLD", 99);
            var fake = Fake("P", "Fake", _ => WorldGenerationPassResult.Failure("NO", "stop"));
            AssertTerminal(ExecuteSingle(definition, fake), "PASS_FAILED", 0);
            Assert.That(fake.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void Root_ReportOnlyPreservesIssueThenMissingInputTerminates()
        {
            var first = Pass("A", 0, "First", "", Empty, new[] { "ONE" }, "REPORT_ONLY");
            var second = Pass("B", 1, "Second", "", new[] { "ONE" }, new[] { "TWO" });
            var report = Fake("A", "First", _ => WorldGenerationPassResult.Failure("NO", "reported"));
            var downstream = Fake("B", "Second");
            var fixture = CreateFixture(new[] { first, second });
            var result = Root(fixture, report, downstream).Execute(ProfileId, 1);
            Assert.That(result.Issues.Count, Is.EqualTo(2));
            Assert.That(result.Issues[0].Code, Is.EqualTo("PASS_FAILED"));
            Assert.That(result.Issues[0].Terminal, Is.False);
            Assert.That(result.Issues[1].Code, Is.EqualTo("MISSING_INPUT_ARTIFACT"));
            Assert.That(result.Issues[1].Terminal, Is.True);
            Assert.That(downstream.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_NullPassResultIsTerminalAndNotRetried()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "RETRY_PASS", 3);
            var fake = Fake("P", "Fake", _ => null);
            AssertTerminal(ExecuteSingle(definition, fake), "NULL_PASS_RESULT", 0);
            Assert.That(fake.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void Root_UnhandledExceptionReportsOnlyExceptionType()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" });
            var fake = Fake("P", "Fake", _ => throw new InvalidOperationException("variable message"));
            var result = ExecuteSingle(definition, fake);
            AssertTerminal(result, "UNHANDLED_PASS_EXCEPTION", 0);
            Assert.That(result.Issues.Single().Message, Is.EqualTo("Pass threw System.InvalidOperationException."));
        }

        [TestCase("missing")]
        [TestCase("extra")]
        public void Root_OutputSetMismatchIsTerminalWithoutCommit(string mutation)
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" });
            var fake = Fake("P", "Fake", _ => mutation == "missing"
                ? WorldGenerationPassResult.Success(Array.Empty<KeyValuePair<string, object>>())
                : WorldGenerationPassResult.Success(new[] { Pair("A", new object()), Pair("B", new object()) }));
            var result = ExecuteSingle(definition, fake);
            AssertTerminal(result, "OUTPUT_SET_MISMATCH", 0);
            Assert.That(result.Artifacts.Count, Is.Zero);
        }

        [Test]
        public void Root_RuntimeFailurePreservesPriorArtifactsAndLastCompletedPass()
        {
            var first = Pass("A", 0, "First", "", Empty, new[] { "ONE" });
            var second = Pass("B", 1, "Second", "", new[] { "ONE" }, new[] { "TWO" });
            var fixture = CreateFixture(new[] { first, second });
            var result = Root(
                fixture,
                Fake("A", "First", _ => WorldGenerationPassResult.Success("ONE", "kept")),
                Fake("B", "Second", _ => WorldGenerationPassResult.Failure("NO", "stop")))
                .Execute(ProfileId, 1);
            Assert.That(result.Artifacts.Get<string>("ONE"), Is.EqualTo("kept"));
            Assert.That(result.Artifacts.Contains("TWO"), Is.False);
            Assert.That(result.LastCompletedPassId, Is.EqualTo("A"));
        }

        [Test]
        public void Root_ReusedAndFreshInstancesAreDeterministicForGridPrefix()
        {
            var fixture = CreateFixture(StarterPasses());
            var reused = new WorldGenerationRoot(fixture.StaticData, WorldGenerationPassRegistry.CreateProduction());
            var expected = reused.ExecuteThrough(ProfileId, 0x1234, "PASS_GRID")
                .Artifacts.Get<GridInitializationResult>("GRID");
            for (var iteration = 0; iteration < 100; iteration++)
            {
                var root = iteration % 2 == 0
                    ? reused
                    : new WorldGenerationRoot(fixture.StaticData, WorldGenerationPassRegistry.CreateProduction());
                var actual = root.ExecuteThrough(ProfileId, 0x1234, "PASS_GRID")
                    .Artifacts.Get<GridInitializationResult>("GRID");
                Assert.That(actual.WorldData.Seed, Is.EqualTo(expected.WorldData.Seed));
                Assert.That(actual.WorldData.Cells.Count, Is.EqualTo(expected.WorldData.Cells.Count));
                Assert.That(actual.Neighbors.Select(item => item.ValidNeighborCount),
                    Is.EqualTo(expected.Neighbors.Select(item => item.ValidNeighborCount)));
            }
        }

        [Test]
        public void Root_SourceArtifactOrderDoesNotAffectExecutionInputs()
        {
            var first = Pass("A", 0, "First", "", Empty, new[] { "Z", "A" });
            var second = Pass("B", 1, "Second", "", new[] { "Z", "A" }, new[] { "DONE" });
            var observer = Fake("B", "Second", context =>
            {
                CollectionAssert.AreEqual(new[] { "A", "Z" }, context.Inputs.ArtifactIds);
                return WorldGenerationPassResult.Success("DONE", new object());
            });
            var fixture = CreateFixture(new[] { second, first });
            var result = Root(
                fixture,
                Fake("A", "First", _ => WorldGenerationPassResult.Success(
                    new[] { Pair("Z", new object()), Pair("A", new object()) })),
                observer).Execute(ProfileId, 1);
            Assert.That(result.Succeeded, Is.True, FormatIssues(result));
        }

        [Test]
        public void Root_ClassMismatchFailsPreInvocation()
        {
            var definition = Pass("P", 0, "Expected", "", Empty, new[] { "A" });
            var fake = Fake("P", "Actual");
            var result = ExecuteSingle(definition, fake);
            AssertPlanFailure(result, "PASS_CLASS_MISMATCH");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_MissingImplementationFailsPreInvocation()
        {
            var fixture = CreateFixture(new[] { Pass("P", 0, "Fake", "", Empty, new[] { "A" }) });
            AssertPlanFailure(
                new WorldGenerationRoot(fixture.StaticData, new WorldGenerationPassRegistry(Array.Empty<IWorldGenerationPass>()))
                    .Execute(ProfileId, 1),
                "MISSING_PASS_IMPLEMENTATION");
        }

        [Test]
        public void Root_InvalidRngDefinitionFailsPreInvocation()
        {
            var definition = Pass("P", 0, "Fake", "RNG_MISSING", Empty, new[] { "A" });
            var fake = Fake("P", "Fake");
            var result = ExecuteSingle(definition, fake);
            AssertPlanFailure(result, "INVALID_RNG_DEFINITION");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_DuplicatePassOrderFailsPreInvocation()
        {
            var first = Pass("A", 0, "First", "", Empty, new[] { "ONE" });
            var second = Pass("B", 0, "Second", "", new[] { "ONE" }, new[] { "TWO" });
            var one = Fake("A", "First");
            var two = Fake("B", "Second");
            var fixture = CreateFixture(new[] { first, second });
            AssertPlanFailure(Root(fixture, one, two).Execute(ProfileId, 1), "INVALID_PASS_DEFINITION");
            Assert.That(one.InvocationCount + two.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_DuplicatePassIdFailsPreInvocation()
        {
            var first = Pass("P", 0, "Fake", "", Empty, new[] { "ONE" });
            var second = Pass("P", 1, "Fake", "", new[] { "ONE" }, new[] { "TWO" });
            var fake = Fake("P", "Fake");
            var fixture = CreateFixture(new[] { first, second });
            AssertPlanFailure(Root(fixture, fake).Execute(ProfileId, 1), "INVALID_PASS_DEFINITION");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_InvalidFailurePolicyFailsPreInvocation()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "retry_pass");
            var fake = Fake("P", "Fake");
            AssertPlanFailure(ExecuteSingle(definition, fake), "INVALID_PASS_DEFINITION");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_NegativeRetryCountFailsPreInvocation()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "RETRY_PASS", -1);
            var fake = Fake("P", "Fake");
            AssertPlanFailure(ExecuteSingle(definition, fake), "INVALID_PASS_DEFINITION");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_ForwardArtifactDependencyFailsPreInvocation()
        {
            var consumer = Pass("A", 0, "Consumer", "", new[] { "LATER" }, new[] { "FIRST" });
            var producer = Pass("B", 1, "Producer", "", Empty, new[] { "LATER" });
            var first = Fake("A", "Consumer");
            var second = Fake("B", "Producer");
            var fixture = CreateFixture(new[] { consumer, producer });
            AssertPlanFailure(Root(fixture, first, second).Execute(ProfileId, 1), "INVALID_ARTIFACT_PLAN");
            Assert.That(first.InvocationCount + second.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_DuplicateOutputOwnerFailsPreInvocation()
        {
            var first = Pass("A", 0, "First", "", Empty, new[] { "ONE" });
            var second = Pass("B", 1, "Second", "", Empty, new[] { "ONE" });
            var one = Fake("A", "First");
            var two = Fake("B", "Second");
            var fixture = CreateFixture(new[] { first, second });
            AssertPlanFailure(Root(fixture, one, two).Execute(ProfileId, 1), "ARTIFACT_OWNERSHIP_CONFLICT");
            Assert.That(one.InvocationCount + two.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_MaxRetryCountThatCannotRepresentTotalAttemptsIsInvalid()
        {
            var definition = Pass("P", 0, "Fake", "", Empty, new[] { "A" }, "RETRY_PASS", int.MaxValue);
            var fake = Fake("P", "Fake");
            AssertPlanFailure(ExecuteSingle(definition, fake), "INVALID_PASS_DEFINITION");
            Assert.That(fake.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_ResultHasExactlyOneTerminalIssueOnFailure()
        {
            var definition = Pass("P", 0, "WrongClass", "RNG_MISSING", new[] { "MISSING" }, new[] { "A" });
            var result = ExecuteSingle(definition, Fake("P", "OtherClass"));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Issues.Count, Is.GreaterThan(1));
            Assert.That(result.Issues.Count(item => item.Terminal), Is.EqualTo(1));
            Assert.That(result.Issues.Last().Terminal, Is.True);
        }

        private const string ProfileId = "GEN_TEST";
        private const string WorldId = "WORLD_TEST";
        private static readonly string[] Empty = Array.Empty<string>();

        private static WorldGenerationRootResult ExecuteSingle(
            GenerationPassDefinition definition,
            ScriptedPass implementation)
        {
            var fixture = CreateFixture(new[] { definition });
            return Root(fixture, implementation).Execute(ProfileId, 1);
        }

        private static WorldGenerationRoot Root(Fixture fixture, params IWorldGenerationPass[] passes)
        {
            return new WorldGenerationRoot(fixture.StaticData, new WorldGenerationPassRegistry(passes));
        }

        private static WorldGenerationPassContext Context(
            Fixture fixture,
            GenerationPassDefinition definition,
            ulong seed)
        {
            return new WorldGenerationPassContext(
                seed,
                fixture.StaticData,
                fixture.Profile,
                definition,
                new WorldGenerationArtifactStore(),
                new WorldGenerationRngStreams(fixture.StaticData),
                0,
                string.Empty);
        }

        private static Fixture CreateFixture(
            IEnumerable<GenerationPassDefinition> passDefinitions,
            bool profileActive = true,
            bool includeWorld = true,
            bool worldActive = true)
        {
            var passes = passDefinitions.ToArray();
            var profile = Definition<GenerationProfileDefinition>(
                Pair("GenerationProfileId", (object)ProfileId),
                Pair("WorldProfileId", WorldId),
                Pair("Active", profileActive));
            var worlds = includeWorld
                ? new[] { Definition<WorldProfileDefinition>(Pair("WorldProfileId", (object)WorldId), Pair("Active", worldActive)) }
                : Array.Empty<WorldProfileDefinition>();
            var definitions = Construct<WorldRouteDefinitionSet>(
                worlds,
                new[] { profile },
                passes,
                RequiredRngDefinitions(),
                Array.Empty<SectorRouteMaskDefinition>(),
                Array.Empty<SocketBandDefinition>(),
                Array.Empty<EdgeSignatureDefinition>(),
                Array.Empty<EdgeSignatureCompatibilityDefinition>(),
                Array.Empty<SectorRecipeDefinition>(),
                Array.Empty<SectorRecipeCellDefinition>(),
                Array.Empty<SectorRecipePathDefinition>(),
                Array.Empty<SectorExternalSocketDefinition>(),
                Array.Empty<SectorRecipePoolEntryDefinition>());
            var registry = (StaticDataRegistry)FormatterServices.GetUninitializedObject(typeof(StaticDataRegistry));
            SetAutoProperty(registry, "WorldRouteDefinitions", definitions);
            return new Fixture(registry, profile, passes);
        }

        private static GenerationPassDefinition Pass(
            string id,
            int order,
            string className,
            string rngId,
            IReadOnlyList<string> inputs,
            IReadOnlyList<string> outputs,
            string policy = "FAIL_WORLD",
            int maxRetryCount = 0)
        {
            return Definition<GenerationPassDefinition>(
                Pair("GenerationProfileId", (object)ProfileId),
                Pair("PassOrder", order),
                Pair("PassId", id),
                Pair("ClassName", className),
                Pair("RngStreamId", rngId),
                Pair("InputArtifacts", new ReadOnlyCollection<string>(inputs.ToList())),
                Pair("OutputArtifacts", new ReadOnlyCollection<string>(outputs.ToList())),
                Pair("FailurePolicy", policy),
                Pair("MaxRetryCount", maxRetryCount),
                Pair("Enabled", true),
                Pair("Notes", string.Empty));
        }

        private static IReadOnlyList<GenerationPassDefinition> StarterPasses()
        {
            return new[]
            {
                Pass("PASS_GRID", 0, "GridInitializationPass", "", Empty, new[] { "GRID" }, "FAIL_WORLD", 1),
                Pass("PASS_SITE", 10, "SpecialSiteReservationPass", "RNG_WORLD_SITE", new[] { "GRID" }, new[] { "SITE_RESERVATIONS" }, "RETRY_PASS", 200),
                Pass("PASS_BIOME", 20, "BiomePatchPass", "RNG_BIOME_PATCH", new[] { "GRID", "SITE_RESERVATIONS" }, new[] { "BIOME_PATCHES" }, "RETRY_PASS", 100),
                Pass("PASS_ROUTE", 30, "MandatoryRoutePass", "RNG_ROUTE", new[] { "SITE_RESERVATIONS", "BIOME_PATCHES" }, new[] { "ROUTE123" }, "RETRY_PASS", 200),
                Pass("PASS_TYPE0", 40, "OptionalRegionPass", "RNG_TYPE0", new[] { "ROUTE123" }, new[] { "TYPE0_REGIONS" }, "RETRY_PASS", 100),
                Pass("PASS_SECTOR_RECIPE", 50, "SectorRecipePass", "RNG_SECTOR_RECIPE", new[] { "BIOME_PATCHES", "ROUTE123", "TYPE0_REGIONS" }, new[] { "SECTOR_RECIPES" }, "RETRY_SCOPE", 20),
                Pass("PASS_MICRO_SOLVE", 60, "SectorConstraintPass", "RNG_SECTOR_RECIPE", new[] { "SECTOR_RECIPES" }, new[] { "MICROCHUNKS" }, "RETRY_SCOPE", 20),
                Pass("PASS_BAKE", 70, "TilemapBakePass", "", new[] { "MICROCHUNKS" }, new[] { "BAKED_TILES" }, "FAIL_WORLD", 1),
                Pass("PASS_POPULATION", 80, "PopulationPass", "RNG_POPULATION", new[] { "BAKED_TILES" }, new[] { "SPAWNS" }, "RETRY_SCOPE", 10),
                Pass("PASS_VALIDATION", 90, "WorldValidationPass", "", new[] { "SPAWNS" }, new[] { "VALIDATION" }, "FAIL_WORLD", 1)
            };
        }

        private static WorldGenerationPassRegistry FullStarterRegistry(ICollection<string> invoked)
        {
            var implementations = new List<IWorldGenerationPass> { new RecordingGridAdapter(invoked) };
            foreach (var definition in StarterPasses().Skip(1))
            {
                var captured = definition;
                implementations.Add(Fake(captured.PassId, captured.ClassName, _ =>
                {
                    invoked?.Add(captured.PassId);
                    return WorldGenerationPassResult.Success(captured.OutputArtifacts[0], new object());
                }));
            }
            return new WorldGenerationPassRegistry(implementations);
        }

        private static RngStreamDefinition[] RequiredRngDefinitions()
        {
            return new[]
            {
                Rng("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD"),
                Rng("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS"),
                Rng("RNG_ROUTE", "C00FEE12AB341901", "PASS"),
                Rng("RNG_TYPE0", "D15EA5E007A4C883", "PASS"),
                Rng("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR"),
                Rng("RNG_POPULATION", "F123456789ABCDEF", "SPAWN")
            };
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            return Definition<RngStreamDefinition>(
                Pair("RngStreamId", (object)id),
                Pair("SaltHex", CreateHex(salt)),
                Pair("ResetScope", scope),
                Pair("DescriptionKo", "test"),
                Pair("Active", true));
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(IEnumerable<byte>) },
                null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static T Definition<T>(params KeyValuePair<string, object>[] values)
        {
            var value = (T)FormatterServices.GetUninitializedObject(typeof(T));
            foreach (var pair in values) SetAutoProperty(value, pair.Key, pair.Value);
            return value;
        }

        private static T Construct<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T), BindingFlags.Instance | BindingFlags.NonPublic,
                null, arguments, CultureInfo.InvariantCulture);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var field = target.GetType().GetField(
                "<" + propertyName + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, propertyName);
            field.SetValue(target, value);
        }

        private static ScriptedPass Fake(
            string passId,
            string className,
            Func<WorldGenerationPassContext, WorldGenerationPassResult> execute = null)
        {
            return new ScriptedPass(passId, className, execute ?? (_ =>
                WorldGenerationPassResult.Success("A", new object())));
        }

        private static WorldGenerationArtifactStore Store(params KeyValuePair<string, object>[] values)
        {
            return new WorldGenerationArtifactStore(values);
        }

        private static KeyValuePair<string, object> Pair(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        private static void AssertPlanFailure(WorldGenerationRootResult result, string code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Artifacts.Count, Is.Zero);
            Assert.That(result.LastCompletedPassId, Is.Empty);
            Assert.That(result.Issues.Any(item => item.Code == code), Is.True, FormatIssues(result));
            Assert.That(result.Issues.Count(item => item.Terminal), Is.EqualTo(1));
        }

        private static void AssertTerminal(WorldGenerationRootResult result, string code, int attemptOrdinal)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Issues.Last().Code, Is.EqualTo(code), FormatIssues(result));
            Assert.That(result.Issues.Last().AttemptOrdinal, Is.EqualTo(attemptOrdinal));
            Assert.That(result.Issues.Count(item => item.Terminal), Is.EqualTo(1));
        }

        private static string FormatIssues(WorldGenerationRootResult result)
        {
            return string.Join("\n", result.Issues.Select(item =>
                item.PassId + ":" + item.Code + ":" + item.Message));
        }

        private sealed class Fixture
        {
            public Fixture(
                StaticDataRegistry staticData,
                GenerationProfileDefinition profile,
                IReadOnlyList<GenerationPassDefinition> passes)
            {
                StaticData = staticData;
                Profile = profile;
                Passes = passes;
            }

            public StaticDataRegistry StaticData { get; }
            public GenerationProfileDefinition Profile { get; }
            public IReadOnlyList<GenerationPassDefinition> Passes { get; }
        }

        private sealed class ScriptedPass : IWorldGenerationPass
        {
            private readonly Func<WorldGenerationPassContext, WorldGenerationPassResult> execute;

            public ScriptedPass(
                string passId,
                string className,
                Func<WorldGenerationPassContext, WorldGenerationPassResult> execute)
            {
                PassId = passId;
                ClassName = className;
                this.execute = execute;
            }

            public string PassId { get; }
            public string ClassName { get; }
            public int InvocationCount { get; private set; }
            public List<WorldGenerationPassContext> Contexts { get; } = new List<WorldGenerationPassContext>();

            public WorldGenerationPassResult Execute(WorldGenerationPassContext context)
            {
                InvocationCount++;
                Contexts.Add(context);
                return execute(context);
            }
        }

        private sealed class RecordingGridAdapter : IWorldGenerationPass
        {
            private readonly ICollection<string> invoked;
            private readonly GridInitializationPassAdapter adapter = new GridInitializationPassAdapter();

            public RecordingGridAdapter(ICollection<string> invoked)
            {
                this.invoked = invoked;
            }

            public string PassId => adapter.PassId;
            public string ClassName => adapter.ClassName;

            public WorldGenerationPassResult Execute(WorldGenerationPassContext context)
            {
                invoked?.Add(PassId);
                return adapter.Execute(context);
            }
        }
    }
}
