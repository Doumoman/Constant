using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Generation
{
    public sealed class DeterministicRngStreamTests
    {
        private const ulong KnownWorldSeed = 0x0123456789ABCDEFUL;

        [Test]
        public void ResetScope_HasExactOrderedValues()
        {
            CollectionAssert.AreEqual(
                new[] { "World", "Pass", "Sector", "Patch", "Site", "Spawn" },
                Enum.GetNames(typeof(RngResetScope)));
        }

        [TestCase(RngResetScope.World, "WORLD")]
        [TestCase(RngResetScope.Pass, "PASS")]
        [TestCase(RngResetScope.Sector, "SECTOR")]
        [TestCase(RngResetScope.Patch, "PATCH")]
        [TestCase(RngResetScope.Site, "SITE")]
        [TestCase(RngResetScope.Spawn, "SPAWN")]
        public void ResetScope_FormatUsesExactToken(RngResetScope scope, string token)
        {
            Assert.That(RngResetScopeToken.Format(scope), Is.EqualTo(token));
        }

        [TestCase("WORLD", RngResetScope.World)]
        [TestCase("PASS", RngResetScope.Pass)]
        [TestCase("SECTOR", RngResetScope.Sector)]
        [TestCase("PATCH", RngResetScope.Patch)]
        [TestCase("SITE", RngResetScope.Site)]
        [TestCase("SPAWN", RngResetScope.Spawn)]
        public void ResetScope_ParseUsesExactToken(string token, RngResetScope scope)
        {
            Assert.That(RngResetScopeToken.Parse(token), Is.EqualTo(scope));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("world")]
        [TestCase("World")]
        [TestCase(" WORLD")]
        [TestCase("WORLD ")]
        [TestCase("1")]
        [TestCase("0")]
        [TestCase("UNKNOWN")]
        public void ResetScope_ParseRejectsTokenMismatch(string token)
        {
            Assert.Catch<ArgumentException>(() => RngResetScopeToken.Parse(token));
        }

        [Test]
        public void ResetScope_FormatRejectsUndefinedValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RngResetScopeToken.Format((RngResetScope)999));
        }

        [Test]
        public void Scope_WorldUsesExactEmptyIdentity()
        {
            var scope = RngStreamScope.World(7);

            Assert.That(scope.ResetScope, Is.EqualTo(RngResetScope.World));
            Assert.That(scope.Identity, Is.EqualTo(string.Empty));
            Assert.That(scope.AttemptOrdinal, Is.EqualTo(7));
        }

        [TestCase(RngResetScope.Pass, " PASS_é ")]
        [TestCase(RngResetScope.Patch, " Patch_Å ")]
        [TestCase(RngResetScope.Site, " Site_가 ")]
        [TestCase(RngResetScope.Spawn, " Spawn_Ｅ ")]
        [TestCase(RngResetScope.Sector, " 6,6 ")]
        public void Scope_PreservesExactIdentity(RngResetScope resetScope, string identity)
        {
            var scope = new RngStreamScope(resetScope, identity, 3);

            Assert.That(scope.Identity, Is.EqualTo(identity));
            Assert.That(scope.AttemptOrdinal, Is.EqualTo(3));
        }

        [Test]
        public void Scope_SectorUsesInvariantExactCoordinateIdentity()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                var scope = RngStreamScope.Sector(new SectorCoord(6, 12), 2);

                Assert.That(scope.ResetScope, Is.EqualTo(RngResetScope.Sector));
                Assert.That(scope.Identity, Is.EqualTo("6,12"));
                Assert.That(scope.AttemptOrdinal, Is.EqualTo(2));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCase(RngResetScope.Pass)]
        [TestCase(RngResetScope.Sector)]
        [TestCase(RngResetScope.Patch)]
        [TestCase(RngResetScope.Site)]
        [TestCase(RngResetScope.Spawn)]
        public void Scope_NonWorldRejectsEmptyIdentity(RngResetScope resetScope)
        {
            Assert.Throws<ArgumentException>(() => new RngStreamScope(resetScope, string.Empty));
        }

        [Test]
        public void Scope_WorldRejectsNonEmptyIdentity()
        {
            Assert.Throws<ArgumentException>(() => new RngStreamScope(RngResetScope.World, "WORLD"));
        }

        [Test]
        public void Scope_RejectsNullIdentity()
        {
            Assert.Throws<ArgumentNullException>(() => new RngStreamScope(RngResetScope.Pass, null));
        }

        [Test]
        public void Scope_RejectsNegativeAttempt()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RngStreamScope.Pass("PASS_ROUTE", -1));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(WorldGenConstants.SectorColumns, 0)]
        [TestCase(0, WorldGenConstants.SectorRows)]
        public void Scope_SectorRejectsOutOfRangeCoordinate(int x, int y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RngStreamScope.Sector(new SectorCoord(x, y)));
        }

        [Test]
        public void Scope_EqualityIsOrdinalAndIncludesAttempt()
        {
            var first = RngStreamScope.Site("Site_İ", 1);

            Assert.That(first, Is.EqualTo(RngStreamScope.Site("Site_İ", 1)));
            Assert.That(first, Is.Not.EqualTo(RngStreamScope.Site("site_İ", 1)));
            Assert.That(first, Is.Not.EqualTo(RngStreamScope.Site("Site_İ", 2)));
        }

        [TestCase("RNG_WORLD_SITE", "", "60D4B46EBF6EF00D", "F627BD56683B33FC", "4CA318D8E4EA97BA")]
        [TestCase("RNG_BIOME_PATCH", "PASS_BIOME", "98BC23250806566B", "D2E329C4A736E686", "F63F41F61CC1B52C")]
        [TestCase("RNG_ROUTE", "PASS_ROUTE", "8EDC9EB9BA0977DC", "CA6E229CF519975D", "2289076DA3C2FFE2")]
        [TestCase("RNG_TYPE0", "PASS_TYPE0", "570969677634D631", "3F79615689D9D77E", "8A8D7006920CD2E8")]
        [TestCase("RNG_SECTOR_RECIPE", "6,6", "08D7C54EF3F843DE", "612FB5C8F12DDB0A", "DD0D4A17DDF66EA1")]
        [TestCase("RNG_POPULATION", "6,6", "36D00A33DAED7549", "472FBC58241A8307", "93591B6C5B950D32")]
        public void KnownVectors_MatchInitialAndFirstTwoDraws(
            string streamId,
            string identity,
            string initialHex,
            string firstHex,
            string secondHex)
        {
            var streams = new WorldGenerationRngStreams(CreateDefinitionSet());
            var stream = streams.Create(streamId, KnownWorldSeed, KnownScope(streamId, identity));

            Assert.That(stream.InitialState, Is.EqualTo(Hex(initialHex)));
            Assert.That(stream.NextUInt64(), Is.EqualTo(Hex(firstHex)));
            Assert.That(stream.NextUInt64(), Is.EqualTo(Hex(secondHex)));
            Assert.That(stream.DrawCount, Is.EqualTo(2UL));
        }

        [Test]
        public void SplitMix64_InitialStateAndDrawCountAreReadOnlyBehavior()
        {
            var stream = new DeterministicRngStream(123UL);

            Assert.That(stream.InitialState, Is.EqualTo(123UL));
            Assert.That(stream.DrawCount, Is.EqualTo(0UL));
            stream.NextUInt64();
            Assert.That(stream.InitialState, Is.EqualTo(123UL));
            Assert.That(stream.DrawCount, Is.EqualTo(1UL));
        }

        [TestCase(0UL)]
        [TestCase(ulong.MaxValue)]
        public void SplitMix64_ZeroAndMaxStateMatchReferenceWithWraparound(ulong initialState)
        {
            var referenceState = initialState;
            var expected = ReferenceNext(ref referenceState);
            var stream = new DeterministicRngStream(initialState);

            Assert.That(stream.NextUInt64(), Is.EqualTo(expected));
        }

        [Test]
        public void SameInput_OneHundredFreshStreamsHaveExactSequence()
        {
            var factory = new DeterministicRngStreamFactory(CreateDefinitionSet());
            var expected = Draw(factory.Create("RNG_ROUTE", KnownWorldSeed, RngStreamScope.Pass("PASS_ROUTE")), 32);

            for (var iteration = 0; iteration < 100; iteration++)
            {
                CollectionAssert.AreEqual(
                    expected,
                    Draw(factory.Create("RNG_ROUTE", KnownWorldSeed, RngStreamScope.Pass("PASS_ROUTE")), 32));
            }
        }

        [TestCase("world_seed")]
        [TestCase("salt")]
        [TestCase("stream_id")]
        [TestCase("reset_scope")]
        [TestCase("scope_identity")]
        [TestCase("attempt")]
        public void SeedDerivation_OneFieldMutationChangesInitialState(string mutation)
        {
            var baselineDefinition = CreateDefinition("RNG_ROUTE", "C00FEE12AB341901", "PASS", true);
            var baseline = DeterministicRngSeedDeriver.DeriveInitialState(
                KnownWorldSeed,
                baselineDefinition,
                RngStreamScope.Pass("PASS_ROUTE"));

            ulong changed;
            switch (mutation)
            {
                case "world_seed":
                    changed = DeterministicRngSeedDeriver.DeriveInitialState(
                        KnownWorldSeed + 1,
                        baselineDefinition,
                        RngStreamScope.Pass("PASS_ROUTE"));
                    break;
                case "salt":
                    changed = DeterministicRngSeedDeriver.DeriveInitialState(
                        KnownWorldSeed,
                        CreateDefinition("RNG_ROUTE", "C10FEE12AB341901", "PASS", true),
                        RngStreamScope.Pass("PASS_ROUTE"));
                    break;
                case "stream_id":
                    changed = DeterministicRngSeedDeriver.DeriveInitialState(
                        KnownWorldSeed,
                        CreateDefinition("RNG_ROUTE_X", "C00FEE12AB341901", "PASS", true),
                        RngStreamScope.Pass("PASS_ROUTE"));
                    break;
                case "reset_scope":
                    changed = DeterministicRngSeedDeriver.DeriveInitialState(
                        KnownWorldSeed,
                        CreateDefinition("RNG_ROUTE", "C00FEE12AB341901", "SITE", true),
                        RngStreamScope.Site("PASS_ROUTE"));
                    break;
                case "scope_identity":
                    changed = DeterministicRngSeedDeriver.DeriveInitialState(
                        KnownWorldSeed,
                        baselineDefinition,
                        RngStreamScope.Pass("PASS_ROUTE_X"));
                    break;
                case "attempt":
                    changed = DeterministicRngSeedDeriver.DeriveInitialState(
                        KnownWorldSeed,
                        baselineDefinition,
                        RngStreamScope.Pass("PASS_ROUTE", 1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation));
            }

            Assert.That(changed, Is.Not.EqualTo(baseline));
        }

        [Test]
        public void SeedDerivation_MatchesIndependentExactByteReferenceWithUtf8ByteLength()
        {
            var definition = CreateDefinition("RNG_가", "0102030405060708", "SITE", true);
            var scope = RngStreamScope.Site("영역_é", 12);

            var actual = DeterministicRngSeedDeriver.DeriveInitialState(0xFFEEDDCCBBAA0099UL, definition, scope);
            var expected = ReferenceDerive(0xFFEEDDCCBBAA0099UL, definition, scope);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("stream")]
        [TestCase("identity")]
        public void SeedDerivation_StrictUtf8RejectsInvalidSurrogate(string field)
        {
            var invalid = "\uD800";
            var definition = CreateDefinition(
                field == "stream" ? invalid : "RNG_SITE_TEST",
                "0102030405060708",
                "SITE",
                true);
            var scope = RngStreamScope.Site(field == "identity" ? invalid : "SITE_A");

            Assert.Throws<EncoderFallbackException>(() =>
                DeterministicRngSeedDeriver.DeriveInitialState(0, definition, scope));
        }

        [Test]
        public void SeedDerivation_RejectsNullDefinition()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DeterministicRngSeedDeriver.DeriveInitialState(0, null, RngStreamScope.World()));
        }

        [Test]
        public void SeedDerivation_RejectsInactiveDefinition()
        {
            Assert.Throws<ArgumentException>(() => DeterministicRngSeedDeriver.DeriveInitialState(
                0,
                CreateDefinition("RNG_WORLD_SITE", "0102030405060708", "WORLD", false),
                RngStreamScope.World()));
        }

        [Test]
        public void SeedDerivation_RejectsEmptyStreamId()
        {
            Assert.Throws<ArgumentException>(() => DeterministicRngSeedDeriver.DeriveInitialState(
                0,
                CreateDefinition(string.Empty, "0102030405060708", "WORLD", true),
                RngStreamScope.World()));
        }

        [TestCase("01020304050607")]
        [TestCase("010203040506070809")]
        public void SeedDerivation_RejectsSaltThatIsNotExactlyEightBytes(string saltHex)
        {
            Assert.Throws<ArgumentException>(() => DeterministicRngSeedDeriver.DeriveInitialState(
                0,
                CreateDefinition("RNG_WORLD_SITE", saltHex, "WORLD", true),
                RngStreamScope.World()));
        }

        [Test]
        public void SeedDerivation_UsesSaltBytesInsteadOfOriginalText()
        {
            var first = CreateDefinition("RNG_ROUTE", "C00FEE12AB341901", "PASS", true, "text-one");
            var second = CreateDefinition("RNG_ROUTE", "C00FEE12AB341901", "PASS", true, "text-two");

            Assert.That(
                DeterministicRngSeedDeriver.DeriveInitialState(7, first, RngStreamScope.Pass("PASS_ROUTE")),
                Is.EqualTo(DeterministicRngSeedDeriver.DeriveInitialState(7, second, RngStreamScope.Pass("PASS_ROUTE"))));
        }

        [Test]
        public void SeedDerivation_RejectsInvalidResetToken()
        {
            Assert.Throws<ArgumentException>(() => DeterministicRngSeedDeriver.DeriveInitialState(
                0,
                CreateDefinition("RNG_ROUTE", "0102030405060708", "pass", true),
                RngStreamScope.Pass("PASS_ROUTE")));
        }

        [Test]
        public void SeedDerivation_RejectsResetScopeMismatch()
        {
            Assert.Throws<ArgumentException>(() => DeterministicRngSeedDeriver.DeriveInitialState(
                0,
                CreateDefinition("RNG_ROUTE", "0102030405060708", "PASS", true),
                RngStreamScope.Site("PASS_ROUTE")));
        }

        [Test]
        public void Factory_RejectsNullDefinitionSet()
        {
            Assert.Throws<ArgumentNullException>(() => new DeterministicRngStreamFactory((WorldRouteDefinitionSet)null));
        }

        [Test]
        public void Factory_RejectsNullRegistry()
        {
            Assert.Throws<ArgumentNullException>(() => new DeterministicRngStreamFactory((StaticDataRegistry)null));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("RNG_MISSING")]
        public void Factory_RejectsMissingOrInvalidStreamId(string streamId)
        {
            Assert.Catch<Exception>(() => new DeterministicRngStreamFactory(CreateDefinitionSet()).Create(
                streamId,
                0,
                RngStreamScope.World()));
        }

        [Test]
        public void RequiredCatalog_IsExactOrdinalAndReadOnly()
        {
            var expected = new Dictionary<string, RngResetScope>(StringComparer.Ordinal)
            {
                { "RNG_WORLD_SITE", RngResetScope.World },
                { "RNG_BIOME_PATCH", RngResetScope.Pass },
                { "RNG_ROUTE", RngResetScope.Pass },
                { "RNG_TYPE0", RngResetScope.Pass },
                { "RNG_SECTOR_RECIPE", RngResetScope.Sector },
                { "RNG_POPULATION", RngResetScope.Spawn }
            };

            Assert.That(WorldGenerationRngStreams.RequiredCatalog.Count, Is.EqualTo(6));
            foreach (var pair in expected)
            {
                Assert.That(WorldGenerationRngStreams.RequiredCatalog[pair.Key], Is.EqualTo(pair.Value));
                Assert.That(WorldGenerationRngStreams.RequiredCatalog.ContainsKey(pair.Key.ToLowerInvariant()), Is.False);
            }

            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, RngResetScope>)WorldGenerationRngStreams.RequiredCatalog)
                .Add("RNG_EXTRA", RngResetScope.World));
        }

        [Test]
        public void RequiredCatalog_RejectsMissingDefinition()
        {
            var set = CreateDefinitionSet(definitions => definitions.Remove("RNG_ROUTE"));

            Assert.Throws<KeyNotFoundException>(() => new WorldGenerationRngStreams(set));
        }

        [Test]
        public void RequiredCatalog_RejectsInactiveDefinition()
        {
            var set = CreateDefinitionSet(definitions => definitions["RNG_ROUTE"] =
                CreateDefinition("RNG_ROUTE", "C00FEE12AB341901", "PASS", false));

            Assert.Throws<ArgumentException>(() => new WorldGenerationRngStreams(set));
        }

        [Test]
        public void RequiredCatalog_RejectsWrongResetScope()
        {
            var set = CreateDefinitionSet(definitions => definitions["RNG_ROUTE"] =
                CreateDefinition("RNG_ROUTE", "C00FEE12AB341901", "SITE", true));

            Assert.Throws<ArgumentException>(() => new WorldGenerationRngStreams(set));
        }

        [Test]
        public void RequiredCatalog_RejectsInvalidSalt()
        {
            var set = CreateDefinitionSet(definitions => definitions["RNG_ROUTE"] =
                CreateDefinition("RNG_ROUTE", "C00FEE12AB3419", "PASS", true));

            Assert.Throws<ArgumentException>(() => new WorldGenerationRngStreams(set));
        }

        [Test]
        public void GenericFactory_SupportsActiveVillageDefinitionWithoutAddingRequiredCatalogEntry()
        {
            var streams = new WorldGenerationRngStreams(CreateDefinitionSet());

            var stream = streams.Create("RNG_VILLAGE", 9, RngStreamScope.Site("VILLAGE_01"));

            Assert.That(stream.DrawCount, Is.EqualTo(0UL));
            Assert.That(WorldGenerationRngStreams.RequiredCatalog.ContainsKey("RNG_VILLAGE"), Is.False);
        }

        [Test]
        public void RegistryConstructor_UsesExactWorldRouteDefinitionRoot()
        {
            var set = CreateDefinitionSet();
            var registry = CreateRegistry(set);
            var streams = new WorldGenerationRngStreams(registry);

            Assert.That(streams.Definitions, Is.SameAs(set));
        }

        [Test]
        public void Factory_ReturnsFreshIndependentInstancesForSameInput()
        {
            var factory = new DeterministicRngStreamFactory(CreateDefinitionSet());
            var first = factory.Create("RNG_ROUTE", 1, RngStreamScope.Pass("PASS_ROUTE"));
            var second = factory.Create("RNG_ROUTE", 1, RngStreamScope.Pass("PASS_ROUTE"));

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.InitialState, Is.EqualTo(second.InitialState));
            first.NextUInt64();
            Assert.That(first.DrawCount, Is.EqualTo(1UL));
            Assert.That(second.DrawCount, Is.EqualTo(0UL));
        }

        [Test]
        public void Streams_InterleavedConsumptionMatchesSeparateConsumption()
        {
            var streams = new WorldGenerationRngStreams(CreateDefinitionSet());
            var route = streams.CreateRoute(KnownWorldSeed, "PASS_ROUTE");
            var biome = streams.CreateBiomePatch(KnownWorldSeed, "PASS_BIOME");
            var routeExpected = streams.CreateRoute(KnownWorldSeed, "PASS_ROUTE");
            var biomeExpected = streams.CreateBiomePatch(KnownWorldSeed, "PASS_BIOME");

            for (var draw = 0; draw < 32; draw++)
            {
                Assert.That(route.NextUInt64(), Is.EqualTo(routeExpected.NextUInt64()));
                Assert.That(biome.NextUInt64(), Is.EqualTo(biomeExpected.NextUInt64()));
            }
        }

        [Test]
        public void Streams_ReversedCreationOrderDoesNotChangeIdSequences()
        {
            var streams = new WorldGenerationRngStreams(CreateDefinitionSet());
            var routeFirst = streams.CreateRoute(33, "PASS_ROUTE");
            var biomeSecond = streams.CreateBiomePatch(33, "PASS_BIOME");
            var biomeFirst = streams.CreateBiomePatch(33, "PASS_BIOME");
            var routeSecond = streams.CreateRoute(33, "PASS_ROUTE");

            CollectionAssert.AreEqual(Draw(routeFirst, 20), Draw(routeSecond, 20));
            CollectionAssert.AreEqual(Draw(biomeSecond, 20), Draw(biomeFirst, 20));
        }

        [Test]
        public void Streams_ExtraDrawsOnOneDoNotAlterOtherFive()
        {
            var streams = new WorldGenerationRngStreams(CreateDefinitionSet());
            var baseline = CreateRequired(streams, 77);
            var compared = CreateRequired(streams, 77);
            for (var draw = 0; draw < 100; draw++)
            {
                compared["RNG_ROUTE"].NextUInt64();
            }

            foreach (var id in baseline.Keys.Where(id => id != "RNG_ROUTE"))
            {
                Assert.That(compared[id].DrawCount, Is.EqualTo(0UL));
                CollectionAssert.AreEqual(Draw(baseline[id], 10), Draw(compared[id], 10));
            }
        }

        [Test]
        public void Streams_RejectionOnOneDoesNotAlterAnother()
        {
            var rejecting = new DeterministicRngStream(unchecked(0UL - 0x9E3779B97F4A7C15UL));
            var other = new DeterministicRngStream(1234);
            var expectedOther = new DeterministicRngStream(1234);

            rejecting.NextInt(10);

            Assert.That(rejecting.DrawCount, Is.EqualTo(2UL));
            Assert.That(other.DrawCount, Is.EqualTo(0UL));
            Assert.That(other.NextUInt64(), Is.EqualTo(expectedOther.NextUInt64()));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        [TestCase(int.MaxValue)]
        public void NextInt_StaysInsideExclusiveRange(int exclusiveMax)
        {
            var stream = new DeterministicRngStream(5);
            for (var draw = 0; draw < 1000; draw++)
            {
                var value = stream.NextInt(exclusiveMax);
                Assert.That(value, Is.GreaterThanOrEqualTo(0));
                Assert.That(value, Is.LessThan(exclusiveMax));
            }
        }

        [Test]
        public void NextInt_SupportsFullRepresentableIntHalfOpenRangeWithoutOverflow()
        {
            var stream = new DeterministicRngStream(ulong.MaxValue);
            for (var draw = 0; draw < 1000; draw++)
            {
                var value = stream.NextInt(int.MinValue, int.MaxValue);
                Assert.That(value, Is.GreaterThanOrEqualTo(int.MinValue));
                Assert.That(value, Is.LessThan(int.MaxValue));
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NextInt_RejectsNonPositiveExclusiveMax(int exclusiveMax)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeterministicRngStream(0).NextInt(exclusiveMax));
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        public void NextInt_RejectsEmptyOrReversedRange(int minInclusive, int maxExclusive)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DeterministicRngStream(0).NextInt(minInclusive, maxExclusive));
        }

        [Test]
        public void NextInt_RejectionSamplingCountsEveryActualDraw()
        {
            var stream = new DeterministicRngStream(unchecked(0UL - 0x9E3779B97F4A7C15UL));

            var value = stream.NextInt(10);

            Assert.That(value, Is.GreaterThanOrEqualTo(0));
            Assert.That(value, Is.LessThan(10));
            Assert.That(stream.DrawCount, Is.EqualTo(2UL));
        }

        [Test]
        public void NextDouble01_UsesOneDrawAndAlwaysStaysInHalfOpenRange()
        {
            var stream = new DeterministicRngStream(17);
            for (var draw = 0; draw < 1000; draw++)
            {
                var value = stream.NextDouble01();
                Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(value, Is.LessThan(1.0));
            }

            Assert.That(stream.DrawCount, Is.EqualTo(1000UL));
        }

        [TestCase("fr-FR")]
        [TestCase("tr-TR")]
        public void KnownSequence_IsInvariantUnderCulture(string cultureName)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);
                CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
                var streams = new WorldGenerationRngStreams(CreateDefinitionSet());
                var stream = streams.CreateRoute(KnownWorldSeed, "PASS_ROUTE");

                Assert.That(stream.InitialState, Is.EqualTo(0x8EDC9EB9BA0977DCUL));
                Assert.That(stream.NextUInt64(), Is.EqualTo(0xCA6E229CF519975DUL));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void RuntimeSurface_HasNoUnityRandomSystemRandomIoOrMutableGlobalStream()
        {
            var types = new[]
            {
                typeof(RngResetScope),
                typeof(RngStreamScope),
                typeof(DeterministicRngStream),
                typeof(DeterministicRngSeedDeriver),
                typeof(DeterministicRngStreamFactory),
                typeof(WorldGenerationRngStreams)
            };
            var surface = types
                .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Select(member => member.ToString())
                .ToArray();

            Assert.That(surface.Any(value => value.Contains("UnityEngine.Random")), Is.False);
            Assert.That(surface.Any(value => value.Contains("System.Random")), Is.False);
            Assert.That(surface.Any(value => value.Contains("System.IO")), Is.False);
            Assert.That(types.SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Any(field => typeof(DeterministicRngStream).IsAssignableFrom(field.FieldType)), Is.False);
            Assert.That(typeof(DeterministicRngStream).GetProperties()
                .Any(property => property.GetSetMethod(false) != null), Is.False);
        }

        [Test]
        public void Factory_PreservesDefinitionRootDictionaryAndInstances()
        {
            var definitions = CreateDefinitionSet();
            var originalDictionary = definitions.RngStreams;
            var originalRoute = originalDictionary["RNG_ROUTE"];
            var factory = new DeterministicRngStreamFactory(definitions);

            Assert.That(factory.Definitions, Is.SameAs(definitions));
            Assert.That(factory.Definitions.RngStreams, Is.SameAs(originalDictionary));
            Assert.That(factory.Definitions.RngStreams["RNG_ROUTE"], Is.SameAs(originalRoute));
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, RngStreamDefinition>)originalDictionary)
                .Add("RNG_NEW", originalRoute));
        }

        [Test]
        public void ExistingGeneratedWorldData_RemainsUnchangedWhenStreamsAreConsumed()
        {
            var cells = new List<SectorCell>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                cells.Add(SectorCell.CreateUnassigned(
                    index,
                    new SectorCoord(index % WorldGenConstants.SectorColumns, index / WorldGenConstants.SectorColumns)));
            }

            var world = new GeneratedWorldData(KnownWorldSeed, cells);
            var firstCell = world.Cells[0];
            var stream = new WorldGenerationRngStreams(CreateDefinitionSet())
                .CreateRoute(world.Seed, "PASS_ROUTE");
            Draw(stream, 100);

            Assert.That(world.Seed, Is.EqualTo(KnownWorldSeed));
            Assert.That(world.Cells.Count, Is.EqualTo(WorldGenConstants.SectorCount));
            Assert.That(world.Cells[0], Is.SameAs(firstCell));
            Assert.That(world.Cells[0].Role, Is.EqualTo(GeneratedSectorRole.Unassigned));
        }

        private static RngStreamScope KnownScope(string streamId, string identity)
        {
            switch (streamId)
            {
                case "RNG_WORLD_SITE": return RngStreamScope.World();
                case "RNG_BIOME_PATCH": return RngStreamScope.Pass(identity);
                case "RNG_ROUTE": return RngStreamScope.Pass(identity);
                case "RNG_TYPE0": return RngStreamScope.Pass(identity);
                case "RNG_SECTOR_RECIPE": return RngStreamScope.Sector(new SectorCoord(6, 6));
                case "RNG_POPULATION": return RngStreamScope.Spawn(identity);
                default: throw new ArgumentOutOfRangeException(nameof(streamId));
            }
        }

        private static Dictionary<string, DeterministicRngStream> CreateRequired(
            WorldGenerationRngStreams streams,
            ulong seed)
        {
            return new Dictionary<string, DeterministicRngStream>(StringComparer.Ordinal)
            {
                { "RNG_WORLD_SITE", streams.CreateWorldSite(seed) },
                { "RNG_BIOME_PATCH", streams.CreateBiomePatch(seed, "PASS_BIOME") },
                { "RNG_ROUTE", streams.CreateRoute(seed, "PASS_ROUTE") },
                { "RNG_TYPE0", streams.CreateType0(seed, "PASS_TYPE0") },
                { "RNG_SECTOR_RECIPE", streams.CreateSectorRecipe(seed, new SectorCoord(6, 6)) },
                { "RNG_POPULATION", streams.CreatePopulation(seed, "6,6") }
            };
        }

        private static ulong[] Draw(DeterministicRngStream stream, int count)
        {
            var values = new ulong[count];
            for (var index = 0; index < count; index++)
            {
                values[index] = stream.NextUInt64();
            }

            return values;
        }

        private static ulong ReferenceNext(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                var value = state;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        private static ulong ReferenceDerive(
            ulong worldSeed,
            RngStreamDefinition definition,
            RngStreamScope scope)
        {
            var material = new List<byte>();
            material.AddRange(Encoding.ASCII.GetBytes("STARNIGHT_MAP_RNG_V1"));
            AppendU64(material, worldSeed);
            material.AddRange(definition.SaltHex.Bytes);
            AppendUtf8(material, definition.RngStreamId);
            AppendUtf8(material, definition.ResetScope);
            AppendUtf8(material, scope.Identity);
            AppendU64(material, (ulong)scope.AttemptOrdinal);

            byte[] digest;
            using (var sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(material.ToArray());
            }

            return ((ulong)digest[0] << 56) |
                   ((ulong)digest[1] << 48) |
                   ((ulong)digest[2] << 40) |
                   ((ulong)digest[3] << 32) |
                   ((ulong)digest[4] << 24) |
                   ((ulong)digest[5] << 16) |
                   ((ulong)digest[6] << 8) |
                   digest[7];
        }

        private static void AppendUtf8(List<byte> target, string value)
        {
            var bytes = new UTF8Encoding(false, true).GetBytes(value);
            AppendU64(target, (ulong)bytes.Length);
            target.AddRange(bytes);
        }

        private static void AppendU64(List<byte> target, ulong value)
        {
            for (var shift = 56; shift >= 0; shift -= 8)
            {
                target.Add((byte)(value >> shift));
            }
        }

        private static ulong Hex(string value)
        {
            return ulong.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        private static WorldRouteDefinitionSet CreateDefinitionSet(
            Action<SortedDictionary<string, RngStreamDefinition>> mutate = null)
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { "RNG_WORLD_SITE", CreateDefinition("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD", true) },
                { "RNG_BIOME_PATCH", CreateDefinition("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS", true) },
                { "RNG_ROUTE", CreateDefinition("RNG_ROUTE", "C00FEE12AB341901", "PASS", true) },
                { "RNG_TYPE0", CreateDefinition("RNG_TYPE0", "D15EA5E007A4C883", "PASS", true) },
                { "RNG_SECTOR_RECIPE", CreateDefinition("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR", true) },
                { "RNG_POPULATION", CreateDefinition("RNG_POPULATION", "F123456789ABCDEF", "SPAWN", true) },
                { "RNG_VILLAGE", CreateDefinition("RNG_VILLAGE", "91AB43FECC018812", "SITE", true) }
            };
            mutate?.Invoke(definitions);

            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return set;
        }

        private static StaticDataRegistry CreateRegistry(WorldRouteDefinitionSet definitions)
        {
            var registry = (StaticDataRegistry)FormatterServices.GetUninitializedObject(typeof(StaticDataRegistry));
            SetAutoProperty(registry, "WorldRouteDefinitions", definitions);
            return registry;
        }

        private static RngStreamDefinition CreateDefinition(
            string id,
            string saltHex,
            string resetScope,
            bool active,
            string originalSaltText = null)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(originalSaltText ?? saltHex, saltHex));
            SetAutoProperty(definition, "ResetScope", resetScope);
            SetAutoProperty(definition, "DescriptionKo", "test");
            SetAutoProperty(definition, "Active", active);
            return definition;
        }

        private static CsvHexValue CreateHex(string originalValue, string byteHex)
        {
            var bytes = Enumerable.Range(0, byteHex.Length / 2)
                .Select(index => byte.Parse(
                    byteHex.Substring(index * 2, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture))
                .ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(IEnumerable<byte>) },
                null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { originalValue, bytes });
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var field = target.GetType().GetField(
                "<" + propertyName + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, propertyName);
            field.SetValue(target, value);
        }
    }
}
