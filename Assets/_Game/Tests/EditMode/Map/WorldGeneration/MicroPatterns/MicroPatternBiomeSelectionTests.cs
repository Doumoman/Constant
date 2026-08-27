using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.MicroPatterns
{
    [TestFixture]
    [Category("MAP10_04")]
    public sealed class MicroPatternBiomeSelectionTests
    {
        [Test]
        public void BuiltInProfiles_HaveExactFourMembershipMotifsAndUncalibratedPolicy()
        {
            var catalog = MicroPatternBiomeProfileCatalog.CreateBuiltIn();

            CollectionAssert.AreEqual(
                new[] { "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough" },
                catalog.Profiles.Select(value => value.Biome.CanonicalId).ToArray());
            AssertMotifs(catalog, MoonpalaceBiomeId.MoonCrater,
                "Bowl", "BrokenSlope", "RockShelf");
            AssertMotifs(catalog, MoonpalaceBiomeId.CassiaRoot,
                "HollowPocket", "RootArch", "VerticalTunnel");
            AssertMotifs(catalog, MoonpalaceBiomeId.AbandonedMill,
                "BeamOverhang", "BrokenPillar", "OrthogonalCarve");
            AssertMotifs(catalog, MoonpalaceBiomeId.MoonDough,
                "BounceCup", "SoftPocket", "StickyShelf");
            Assert.That(catalog.Profiles.All(value =>
                value.DensityPolicy == MicroPatternDensityPolicy.Uncalibrated), Is.True);
            Assert.That(catalog.Profiles.All(value => value.SilhouetteClasses.Count == 4), Is.True);
            Assert.That(catalog.StableDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void ProfileValidation_AccumulatesStableErrorsWithoutPartialCatalog()
        {
            var invalid = new MicroPatternBiomeProfile(
                MoonpalaceBiomeId.MoonCrater,
                new[] { "bad token", "Bowl", "Bowl" },
                string.Empty,
                (MicroPatternDensityPolicy)99,
                new[]
                {
                    MicroPatternSilhouetteClass.NoGeometry,
                    MicroPatternSilhouetteClass.NoGeometry,
                    (MicroPatternSilhouetteClass)99,
                });
            var duplicate = new MicroPatternBiomeProfile(
                MoonpalaceBiomeId.MoonCrater,
                new[] { "RockShelf" },
                "safe",
                MicroPatternDensityPolicy.Uncalibrated,
                AllSilhouettes());

            var result = MicroPatternBiomeProfileCatalog.Validate(new[] { invalid, duplicate });

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Catalog, Is.Null);
            AssertCodes(result.Errors,
                MicroPatternProfileValidationErrorCode.DuplicateBiome,
                MicroPatternProfileValidationErrorCode.MissingBiome,
                MicroPatternProfileValidationErrorCode.InvalidMotifToken,
                MicroPatternProfileValidationErrorCode.DuplicateMotif,
                MicroPatternProfileValidationErrorCode.MissingSafetyMeaning,
                MicroPatternProfileValidationErrorCode.InvalidDensityPolicy,
                MicroPatternProfileValidationErrorCode.DuplicateSilhouetteClass,
                MicroPatternProfileValidationErrorCode.InvalidSilhouetteClass,
                MicroPatternProfileValidationErrorCode.MissingSilhouetteClass);
            CollectionAssert.AreEqual(result.Errors.OrderBy(value => value).ToArray(), result.Errors);
        }

        [Test]
        public void FeatureSummary_ReportsFourSilhouettesAndRawIntegerEvidence()
        {
            var none = Feature(Definition("MP_NONE", 1, Writes()));
            var add = Feature(Definition("MP_ADD", 1, Writes(
                Geometry(0, 0, MicroPatternOperation.AddSolid))));
            var carve = Feature(Definition("MP_CARVE", 1, Writes(
                Geometry(1, 0, MicroPatternOperation.CarveAir))));
            var mixed = Feature(Definition("MP_MIXED", 1, Writes(
                Geometry(0, 0, MicroPatternOperation.AddSolid),
                Geometry(1, 0, MicroPatternOperation.CarveAir),
                Layer(2, 0, MicroPatternLayer.Marker, MicroPatternOperation.SetMarker, "MARK_A"))));

            Assert.That(none.SilhouetteClass, Is.EqualTo(MicroPatternSilhouetteClass.NoGeometry));
            Assert.That(add.SilhouetteClass, Is.EqualTo(MicroPatternSilhouetteClass.AddOnly));
            Assert.That(carve.SilhouetteClass, Is.EqualTo(MicroPatternSilhouetteClass.CarveOnly));
            Assert.That(mixed.SilhouetteClass, Is.EqualTo(MicroPatternSilhouetteClass.Mixed));
            Assert.That(mixed.AddSolidCellCount, Is.EqualTo(1));
            Assert.That(mixed.CarveAirCellCount, Is.EqualTo(1));
            Assert.That(mixed.GeometryWriteCellCount, Is.EqualTo(2));
            Assert.That(mixed.GeometryDensityNumerator, Is.EqualTo(2));
            Assert.That(mixed.GeometryDensityDenominator, Is.EqualTo(16));
            Assert.That(mixed.TotalWriteCount, Is.EqualTo(3));
        }

        [Test]
        public void FeatureSummary_PreservesTransformedWriteAndProtectedMaskEvidence()
        {
            var definition = Definition(
                "MP_PROTECTED",
                3,
                Writes(Geometry(0, 0, MicroPatternOperation.AddSolid)),
                MicroPatternProtectedPolicy.ForceNoChange);
            var origin = new LocalTileCoord(10, 20);
            var plan = Plan(
                definition,
                MicroPatternTransform.MirrorX,
                origin,
                new[]
                {
                    new MicroPatternProtectedCell(
                        new LocalTileCoord(13, 20),
                        MicroPatternProtectedSourceKind.RouteSpine,
                        "SPINE_A"),
                });

            var result = MicroPatternFeatureSummary.Create(
                definition,
                MicroPatternTransform.MirrorX,
                plan);

            Assert.That(result.Success, Is.True, FeatureErrors(result));
            Assert.That(result.Summary.AddSolidCellCount, Is.EqualTo(1));
            Assert.That(result.Summary.ProtectedOverlapCount, Is.EqualTo(1));
            Assert.That(result.Summary.ForcedNoChangeCount, Is.EqualTo(1));
            Assert.That(result.Summary.StableDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void CandidateBuilder_SeparatesEligibilityFromAccumulatedRejections()
        {
            var catalog = MicroPatternBiomeProfileCatalog.CreateBuiltIn();
            var valid = Definition("MP_VALID", 7, Writes());
            var wrongBiome = Definition(
                "MP_WRONG_BIOME",
                4,
                Writes(),
                MicroPatternProtectedPolicy.ForceNoChange,
                MoonpalaceBiomeId.CassiaRoot);
            var mismatch = Definition("MP_TRANSFORM_MISMATCH", 2, Writes());
            var badDigest = Definition("MP_BAD_DIGEST", 5, Writes());
            var badPlan = Plan(badDigest);
            SetAutoProperty(badPlan, "StableDigest", "INVALID");

            var result = MicroPatternCandidateIndexBuilder.Build(
                catalog,
                MoonpalaceBiomeId.MoonCrater,
                new[]
                {
                    Source(valid),
                    Source(wrongBiome),
                    new MicroPatternCandidateSource(
                        mismatch,
                        MicroPatternTransform.MirrorX,
                        Plan(mismatch, MicroPatternTransform.R0)),
                    new MicroPatternCandidateSource(
                        badDigest,
                        MicroPatternTransform.R0,
                        badPlan),
                });

            Assert.That(result.Published, Is.True);
            Assert.That(result.Index.Candidates.Count, Is.EqualTo(1));
            Assert.That(result.Index.Candidates[0].Key.PatternId.Value, Is.EqualTo("MP_VALID"));
            Assert.That(result.Index.Candidates[0].Weight, Is.EqualTo(7));
            Assert.That(result.Rejections.Any(value =>
                value.Code == MicroPatternCandidateRejectionCode.BiomeNotAllowed), Is.True);
            Assert.That(result.Rejections.Any(value =>
                value.Code == MicroPatternCandidateRejectionCode.TransformMismatch), Is.True);
            Assert.That(result.Rejections.Any(value =>
                value.Code == MicroPatternCandidateRejectionCode.InvalidApplicationPlanDigest), Is.True);
            CollectionAssert.AreEqual(
                result.Rejections.OrderBy(value => value).ToArray(),
                result.Rejections);
        }

        [Test]
        public void CandidateIndex_UsesCanonicalKeysAndRejectsAllDuplicateMembers()
        {
            var catalog = MicroPatternBiomeProfileCatalog.CreateBuiltIn();
            var first = Definition("MP_A", 2, Writes());
            var second = Definition("MP_B", 3, Writes());
            var duplicate = Source(first);
            var values = new[] { duplicate, Source(second), duplicate };

            var forward = MicroPatternCandidateIndexBuilder.Build(
                catalog,
                MoonpalaceBiomeId.MoonCrater,
                values);
            var reverse = MicroPatternCandidateIndexBuilder.Build(
                catalog,
                MoonpalaceBiomeId.MoonCrater,
                values.Reverse());

            Assert.That(forward.Index.Candidates.Count, Is.EqualTo(1));
            Assert.That(forward.Index.Candidates[0].Key.PatternId.Value, Is.EqualTo("MP_B"));
            Assert.That(forward.Rejections.Any(value =>
                value.Code == MicroPatternCandidateRejectionCode.DuplicateCandidateKey), Is.True);
            Assert.That(reverse.Index.StableDigest, Is.EqualTo(forward.Index.StableDigest));
            CollectionAssert.AreEqual(
                forward.Rejections.Select(value => value.ToString()).ToArray(),
                reverse.Rejections.Select(value => value.ToString()).ToArray());
        }

        [Test]
        public void CandidateWeights_AreExactAndTicketBoundariesAreHalfOpen()
        {
            var index = Index(
                Definition("MP_A", 2, Writes()),
                Definition("MP_B", 3, Writes()));

            Assert.That(index.TotalWeight, Is.EqualTo(5));
            CollectionAssert.AreEqual(new[] { 2, 3 },
                index.Candidates.Select(value => value.Weight).ToArray());
            Assert.That(MicroPatternWeightedTicket.Resolve(index.Candidates, 0), Is.EqualTo(0));
            Assert.That(MicroPatternWeightedTicket.Resolve(index.Candidates, 1), Is.EqualTo(0));
            Assert.That(MicroPatternWeightedTicket.Resolve(index.Candidates, 2), Is.EqualTo(1));
            Assert.That(MicroPatternWeightedTicket.Resolve(index.Candidates, 4), Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MicroPatternWeightedTicket.Resolve(index.Candidates, 5));
        }

        [Test]
        public void Selector_ReusesSectorRecipeStreamAndIsRequestOrderIndependent()
        {
            var definitions = CreateDefinitionSet();
            var selector = new MicroPatternDeterministicSelector(definitions);
            var index = Index(
                Definition("MP_A", 1, Writes()),
                Definition("MP_B", 4, Writes()));
            var sector = new SectorCoord(4, 6);
            var requests = new[]
            {
                Request("MPS_B", index),
                Request("MPS_A", index),
            };

            var forward = selector.Select(0x0123456789ABCDEFUL, sector, 2, requests);
            var reverse = selector.Select(0x0123456789ABCDEFUL, sector, 2, requests.Reverse());
            var expectedState = new DeterministicRngStreamFactory(definitions)
                .Create(
                    WorldGenerationRngStreams.SectorRecipeStreamId,
                    0x0123456789ABCDEFUL,
                    RngStreamScope.Sector(sector, 2))
                .InitialState;

            Assert.That(forward.Success, Is.True, SelectionErrors(forward));
            Assert.That(forward.RegisteredStreamId,
                Is.EqualTo(WorldGenerationRngStreams.SectorRecipeStreamId));
            Assert.That(forward.ResetScope, Is.EqualTo(RngResetScope.Sector));
            Assert.That(forward.ScopeIdentity, Is.EqualTo("4,6"));
            Assert.That(forward.AttemptOrdinal, Is.EqualTo(2));
            Assert.That(forward.InitialState, Is.EqualTo(expectedState));
            Assert.That(forward.FinalDrawCount, Is.GreaterThanOrEqualTo(2UL));
            Assert.That(forward.StableDigest, Is.EqualTo(reverse.StableDigest));
            CollectionAssert.AreEqual(
                new[] { "MPS_A", "MPS_B" },
                forward.Decisions.Select(value => value.RequestId.Value).ToArray());
            CollectionAssert.AreEqual(
                forward.Decisions.Select(DecisionEvidence).ToArray(),
                reverse.Decisions.Select(DecisionEvidence).ToArray());
        }

        [Test]
        public void Selector_DigestHasSeedSectorAttemptAndIndexSensitivity()
        {
            var selector = new MicroPatternDeterministicSelector(CreateDefinitionSet());
            var indexA = Index(Definition("MP_A", 1, Writes()));
            var indexB = Index(
                Definition("MP_A", 1, Writes()),
                Definition("MP_B", 1, Writes()));

            var baseline = Select(selector, 10UL, new SectorCoord(1, 1), 0, indexA);
            var seed = Select(selector, 11UL, new SectorCoord(1, 1), 0, indexA);
            var sector = Select(selector, 10UL, new SectorCoord(1, 2), 0, indexA);
            var attempt = Select(selector, 10UL, new SectorCoord(1, 1), 1, indexA);
            var index = Select(selector, 10UL, new SectorCoord(1, 1), 0, indexB);

            Assert.That(seed.StableDigest, Is.Not.EqualTo(baseline.StableDigest));
            Assert.That(sector.StableDigest, Is.Not.EqualTo(baseline.StableDigest));
            Assert.That(attempt.StableDigest, Is.Not.EqualTo(baseline.StableDigest));
            Assert.That(index.StableDigest, Is.Not.EqualTo(baseline.StableDigest));
        }

        [Test]
        public void Selector_AtomicallyRejectsInvalidOrEmptyBatchWithoutStreamOrDraw()
        {
            var catalog = MicroPatternBiomeProfileCatalog.CreateBuiltIn();
            var empty = MicroPatternCandidateIndexBuilder.Build(
                catalog,
                MoonpalaceBiomeId.MoonCrater,
                Array.Empty<MicroPatternCandidateSource>()).Index;
            var selector = new MicroPatternDeterministicSelector(CreateDefinitionSet());

            var invalid = selector.Select(
                1UL,
                new SectorCoord(0, 0),
                0,
                new[]
                {
                    Request("bad", Index(Definition("MP_A", 1, Writes()))),
                    Request("MPS_EMPTY", empty),
                });

            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.StreamCreated, Is.False);
            Assert.That(invalid.FinalDrawCount, Is.Zero);
            Assert.That(invalid.Decisions, Is.Empty);
            Assert.That(invalid.StableDigest, Is.Empty);
            Assert.That(invalid.Errors.Any(value =>
                value.Code == MicroPatternSelectionBatchErrorCode.InvalidRequestId), Is.True);
            Assert.That(invalid.Errors.Any(value =>
                value.Code == MicroPatternSelectionBatchErrorCode.EmptyCandidateIndex), Is.True);

            var wrongScope = new MicroPatternDeterministicSelector(CreateDefinitionSet(values =>
                values[WorldGenerationRngStreams.SectorRecipeStreamId] = CreateDefinition(
                    WorldGenerationRngStreams.SectorRecipeStreamId,
                    "E9931A70C2D520F4",
                    "PASS",
                    true)));
            var rngRejected = Select(
                wrongScope,
                1UL,
                new SectorCoord(0, 0),
                0,
                Index(Definition("MP_A", 1, Writes())));
            Assert.That(rngRejected.StreamCreated, Is.False);
            Assert.That(rngRejected.Errors.Single().Code,
                Is.EqualTo(MicroPatternSelectionBatchErrorCode.InvalidRngDefinition));
        }

        [Test]
        public void Selector_DoesNotTouchAnyOtherRngStreamInstance()
        {
            var definitions = CreateDefinitionSet();
            var factory = new DeterministicRngStreamFactory(definitions);
            var route = factory.Create(
                WorldGenerationRngStreams.RouteStreamId,
                77UL,
                RngStreamScope.Pass("PASS_ROUTE"));
            var expected = factory.Create(
                WorldGenerationRngStreams.RouteStreamId,
                77UL,
                RngStreamScope.Pass("PASS_ROUTE"));
            var selector = new MicroPatternDeterministicSelector(definitions);

            var result = Select(
                selector,
                77UL,
                new SectorCoord(3, 3),
                0,
                Index(Definition("MP_A", 1, Writes())));

            Assert.That(result.Success, Is.True, SelectionErrors(result));
            Assert.That(route.DrawCount, Is.Zero);
            Assert.That(route.NextUInt64(), Is.EqualTo(expected.NextUInt64()));
        }

        [Test]
        public void PublishedCollectionsAndDecisionsResistCallerMutation()
        {
            var sources = new List<MicroPatternCandidateSource>
            {
                Source(Definition("MP_A", 1, Writes())),
            };
            var built = MicroPatternCandidateIndexBuilder.Build(
                MicroPatternBiomeProfileCatalog.CreateBuiltIn(),
                MoonpalaceBiomeId.MoonCrater,
                sources);
            sources.Clear();

            Assert.That(built.Index.Candidates.Count, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternCandidate>)built.Index.Candidates).Add(null));

            var selected = Select(
                new MicroPatternDeterministicSelector(CreateDefinitionSet()),
                1UL,
                new SectorCoord(0, 0),
                0,
                built.Index);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternSelectionDecision>)selected.Decisions).Add(null));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternSelectionBatchError>)selected.Errors).Add(null));
        }

        [Test]
        public void RuntimeSources_ExcludeForbiddenRngRendererFileAndUnityLifecycleDependencies()
        {
            var root = Path.Combine(
                Application.dataPath,
                "_Game", "Map", "Runtime", "WorldGeneration", "MicroPatterns");
            var source = string.Join("\n", new[]
            {
                "MicroPatternBiomeProfiles.cs",
                "MicroPatternCandidates.cs",
                "MicroPatternSelection.cs",
            }.Select(name => File.ReadAllText(Path.Combine(root, name))));

            foreach (var forbidden in new[]
            {
                "System.Random",
                "UnityEngine.Random",
                "MicroPatternOrderedRenderer",
                "System.IO",
                "UnityEditor",
                "MonoBehaviour",
                "Tilemap",
            })
            {
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
            }
        }

        private static MicroPatternFeatureSummary Feature(MicroPatternDefinition definition)
        {
            var result = MicroPatternFeatureSummary.Create(
                definition,
                MicroPatternTransform.R0,
                Plan(definition));
            Assert.That(result.Success, Is.True, FeatureErrors(result));
            return result.Summary;
        }

        private static MicroPatternCandidateSource Source(MicroPatternDefinition definition)
        {
            return new MicroPatternCandidateSource(
                definition,
                MicroPatternTransform.R0,
                Plan(definition));
        }

        private static MicroPatternCandidateIndex Index(params MicroPatternDefinition[] definitions)
        {
            var result = MicroPatternCandidateIndexBuilder.Build(
                MicroPatternBiomeProfileCatalog.CreateBuiltIn(),
                MoonpalaceBiomeId.MoonCrater,
                definitions.Select(Source));
            Assert.That(result.Published, Is.True);
            Assert.That(result.Rejections, Is.Empty, string.Join("\n", result.Rejections));
            return result.Index;
        }

        private static MicroPatternSelectionRequest Request(
            string id,
            MicroPatternCandidateIndex index)
        {
            return new MicroPatternSelectionRequest(
                new MicroPatternSelectionRequestId(id),
                index);
        }

        private static MicroPatternSelectionBatchResult Select(
            MicroPatternDeterministicSelector selector,
            ulong worldSeed,
            SectorCoord sector,
            int attempt,
            MicroPatternCandidateIndex index)
        {
            return selector.Select(
                worldSeed,
                sector,
                attempt,
                new[] { Request("MPS_ONLY", index) });
        }

        private static MicroPatternApplicationPlan Plan(
            MicroPatternDefinition definition,
            MicroPatternTransform transform = MicroPatternTransform.R0,
            LocalTileCoord origin = default,
            IEnumerable<MicroPatternProtectedCell> protectedCells = null)
        {
            var transformed = MicroPatternTransformer.Transform(definition, transform);
            Assert.That(transformed.Success, Is.True,
                string.Join("\n", transformed.Errors.Select(value => value.ToString())));
            var planned = MicroPatternApplicationPlanner.Plan(
                transformed.Pattern,
                new MicroPatternPlacement(origin),
                protectedCells ?? Array.Empty<MicroPatternProtectedCell>());
            Assert.That(planned.Success, Is.True,
                string.Join("\n", planned.Errors.Select(value => value.ToString())));
            return planned.Plan;
        }

        private static MicroPatternDefinition Definition(
            string id,
            int weight,
            IDictionary<LocalTileCoord, IEnumerable<MicroPatternInstruction>> writes,
            MicroPatternProtectedPolicy policy = MicroPatternProtectedPolicy.ForceNoChange,
            MoonpalaceBiomeId? biome = null)
        {
            var cells = new List<MicroPatternCell>();
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var coordinate = new LocalTileCoord(x, y);
                    if (!writes.TryGetValue(coordinate, out var instructions))
                    {
                        instructions = Array.Empty<MicroPatternInstruction>();
                    }
                    cells.Add(new MicroPatternCell(coordinate, instructions));
                }
            }

            return new MicroPatternDefinition(
                new MicroPatternId(id),
                4,
                4,
                cells,
                weight,
                new[] { biome ?? MoonpalaceBiomeId.MoonCrater },
                new[]
                {
                    MicroPatternTransform.R0,
                    MicroPatternTransform.MirrorX,
                    MicroPatternTransform.MirrorY,
                    MicroPatternTransform.R180,
                },
                policy);
        }

        private static IDictionary<LocalTileCoord, IEnumerable<MicroPatternInstruction>> Writes(
            params KeyValuePair<LocalTileCoord, MicroPatternInstruction>[] writes)
        {
            return writes.ToDictionary(
                value => value.Key,
                value => (IEnumerable<MicroPatternInstruction>)new[] { value.Value });
        }

        private static KeyValuePair<LocalTileCoord, MicroPatternInstruction> Geometry(
            int x,
            int y,
            MicroPatternOperation operation)
        {
            return Layer(x, y, MicroPatternLayer.Geometry, operation);
        }

        private static KeyValuePair<LocalTileCoord, MicroPatternInstruction> Layer(
            int x,
            int y,
            MicroPatternLayer layer,
            MicroPatternOperation operation,
            string payload = null)
        {
            return new KeyValuePair<LocalTileCoord, MicroPatternInstruction>(
                new LocalTileCoord(x, y),
                new MicroPatternInstruction(layer, operation, payload));
        }

        private static MicroPatternSilhouetteClass[] AllSilhouettes()
        {
            return new[]
            {
                MicroPatternSilhouetteClass.NoGeometry,
                MicroPatternSilhouetteClass.AddOnly,
                MicroPatternSilhouetteClass.CarveOnly,
                MicroPatternSilhouetteClass.Mixed,
            };
        }

        private static WorldRouteDefinitionSet CreateDefinitionSet(
            Action<SortedDictionary<string, RngStreamDefinition>> mutate = null)
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                {
                    WorldGenerationRngStreams.SectorRecipeStreamId,
                    CreateDefinition(
                        WorldGenerationRngStreams.SectorRecipeStreamId,
                        "E9931A70C2D520F4",
                        "SECTOR",
                        true)
                },
                {
                    WorldGenerationRngStreams.RouteStreamId,
                    CreateDefinition(
                        WorldGenerationRngStreams.RouteStreamId,
                        "C00FEE12AB341901",
                        "PASS",
                        true)
                },
            };
            mutate?.Invoke(definitions);

            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(
                typeof(WorldRouteDefinitionSet));
            SetAutoProperty(
                set,
                "RngStreams",
                new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return set;
        }

        private static RngStreamDefinition CreateDefinition(
            string id,
            string saltHex,
            string resetScope,
            bool active)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(
                typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(saltHex));
            SetAutoProperty(definition, "ResetScope", resetScope);
            SetAutoProperty(definition, "DescriptionKo", "test");
            SetAutoProperty(definition, "Active", active);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(
                    value.Substring(index * 2, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture))
                .ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(IEnumerable<byte>) },
                null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var field = target.GetType().GetField(
                "<" + propertyName + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, propertyName);
            field.SetValue(target, value);
        }

        private static void AssertMotifs(
            MicroPatternBiomeProfileCatalog catalog,
            MoonpalaceBiomeId biome,
            params string[] expected)
        {
            Assert.That(catalog.TryGetProfile(biome, out var profile), Is.True);
            CollectionAssert.AreEqual(expected, profile.MotifMetadata);
        }

        private static void AssertCodes(
            IEnumerable<MicroPatternProfileValidationError> errors,
            params MicroPatternProfileValidationErrorCode[] required)
        {
            var actual = new HashSet<MicroPatternProfileValidationErrorCode>(
                errors.Select(value => value.Code));
            foreach (var code in required)
            {
                Assert.That(actual.Contains(code), Is.True, code.ToString());
            }
        }

        private static string DecisionEvidence(MicroPatternSelectionDecision value)
        {
            return value.RequestId.Value + "|" + value.Ticket + "|" +
                   value.ChosenKey.CanonicalValue + "|" +
                   value.DrawCountBefore + "|" + value.DrawCountAfter;
        }

        private static string FeatureErrors(MicroPatternFeatureSummaryResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string SelectionErrors(MicroPatternSelectionBatchResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }
    }
}
