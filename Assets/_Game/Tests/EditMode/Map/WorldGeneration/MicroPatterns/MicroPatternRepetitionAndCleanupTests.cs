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
    [Category("MAP10_05")]
    public sealed class MicroPatternRepetitionAndCleanupTests
    {
        [Test]
        public void Signature_UsesExactMasksAndFourTransformCanonicalization()
        {
            var signature = Signature(Definition(
                "MP_MASK",
                1,
                Writes(
                    Geometry(0, 0, MicroPatternOperation.AddSolid),
                    Geometry(1, 2, MicroPatternOperation.CarveAir))));

            Assert.That(signature.AddSolidMask, Is.EqualTo((ushort)0x0001));
            Assert.That(signature.CarveAirMask, Is.EqualTo((ushort)0x0200));
            Assert.That(signature.CanonicalTransform, Is.EqualTo(MicroPatternTransform.R0));
            Assert.That(signature.StableDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(MicroPatternSilhouetteCanonicalDigest.Ruleset,
                Is.EqualTo("MAP10_05_SILHOUETTE_V1"));
        }

        [Test]
        public void Signature_MirrorEquivalentMatchesAndNonEquivalentGeometryDiffers()
        {
            var original = Signature(Definition(
                "MP_ORIGINAL", 1, Writes(
                    Geometry(0, 0, MicroPatternOperation.AddSolid),
                    Geometry(1, 2, MicroPatternOperation.CarveAir))));
            var mirror = Signature(Definition(
                "MP_MIRROR", 1, Writes(
                    Geometry(3, 0, MicroPatternOperation.AddSolid),
                    Geometry(2, 2, MicroPatternOperation.CarveAir))));
            var different = Signature(Definition(
                "MP_DIFFERENT", 1, Writes(
                    Geometry(1, 1, MicroPatternOperation.AddSolid),
                    Geometry(1, 2, MicroPatternOperation.CarveAir))));

            Assert.That(mirror.StableDigest, Is.EqualTo(original.StableDigest));
            Assert.That(mirror.AddSolidMask, Is.EqualTo(original.AddSolidMask));
            Assert.That(mirror.CarveAirMask, Is.EqualTo(original.CarveAirMask));
            Assert.That(different.StableDigest, Is.Not.EqualTo(original.StableDigest));
        }

        [Test]
        public void Signature_IgnoresPayloadWeightBiomeAndProtectedRemovedGeometry()
        {
            var baseline = Definition(
                "MP_BASE", 1, Writes(
                    Geometry(0, 0, MicroPatternOperation.AddSolid),
                    Layer(2, 2, MicroPatternLayer.Marker, MicroPatternOperation.SetMarker, "MARK_A")));
            var metadataVariant = Definition(
                "MP_METADATA", 99, Writes(
                    Geometry(0, 0, MicroPatternOperation.AddSolid),
                    Layer(2, 2, MicroPatternLayer.Marker, MicroPatternOperation.SetMarker, "MARK_B")),
                MicroPatternProtectedPolicy.ForceNoChange,
                MoonpalaceBiomeId.CassiaRoot);
            var protectedPlan = Plan(
                baseline,
                MicroPatternTransform.R0,
                default,
                new[]
                {
                    new MicroPatternProtectedCell(
                        new LocalTileCoord(0, 0),
                        MicroPatternProtectedSourceKind.RouteSpine,
                        "SPINE_A"),
                });

            Assert.That(Signature(metadataVariant).StableDigest,
                Is.EqualTo(Signature(baseline).StableDigest));
            var protectedSignature = BuildSignature(protectedPlan);
            Assert.That(protectedSignature.AddSolidMask, Is.Zero);
            Assert.That(protectedSignature.CarveAirMask, Is.Zero);
        }

        [Test]
        public void Guard_ExcludesExactThirdPatternAcrossTransformsAndSignatures()
        {
            var repeated = Definition(
                "MP_REPEAT", 1, Writes(Geometry(0, 0, MicroPatternOperation.AddSolid)));
            var other = Definition(
                "MP_OTHER", 1, Writes(Geometry(1, 1, MicroPatternOperation.AddSolid)));
            var historySignature = Signature(repeated);
            var sources = new[]
            {
                Source(repeated, MicroPatternTransform.R0),
                Source(repeated, MicroPatternTransform.MirrorX),
                Source(other),
            };

            var result = MicroPatternThirdRepeatGuard.Filter(
                Context(
                    History(10, "MPP_10", repeated.Id, historySignature),
                    History(11, "MPP_11", repeated.Id, historySignature)),
                sources);

            Assert.That(result.Success, Is.True, RepetitionErrors(result));
            CollectionAssert.AreEqual(
                new[] { "MP_OTHER" },
                result.AllowedSources.Select(value => value.Definition.Id.Value).ToArray());
            Assert.That(result.Exclusions.Count, Is.EqualTo(2));
            Assert.That(result.Exclusions.All(value => value.PatternId == repeated.Id), Is.True);
            Assert.That(result.Exclusions.Select(value => value.Transform).Distinct().Count(),
                Is.EqualTo(2));
        }

        [Test]
        public void Guard_AllowsDifferentIdWithSameSignatureAndMismatchExcludesNothing()
        {
            var first = Definition(
                "MP_FIRST", 1, Writes(Geometry(0, 0, MicroPatternOperation.AddSolid)));
            var second = Definition(
                "MP_SECOND", 1, Writes(Geometry(0, 0, MicroPatternOperation.AddSolid)));
            var sameHistory = Context(
                History(1, "MPP_1", first.Id, Signature(first)),
                History(2, "MPP_2", first.Id, Signature(first)));
            var allowedSameShape = MicroPatternThirdRepeatGuard.Filter(
                sameHistory,
                new[] { Source(second) });
            var mismatch = MicroPatternThirdRepeatGuard.Filter(
                Context(
                    History(1, "MPP_1", first.Id, Signature(first)),
                    History(2, "MPP_2", second.Id, Signature(second))),
                new[] { Source(first), Source(second) });

            Assert.That(allowedSameShape.Success, Is.True);
            Assert.That(allowedSameShape.AllowedSources.Single().Definition.Id,
                Is.EqualTo(second.Id));
            Assert.That(allowedSameShape.Exclusions, Is.Empty);
            Assert.That(mismatch.Success, Is.True);
            Assert.That(mismatch.AllowedSources.Count, Is.EqualTo(2));
            Assert.That(mismatch.Exclusions, Is.Empty);
        }

        [Test]
        public void Guard_FeedsMap1004IndexAndSelectorWithoutRerollOrDiscard()
        {
            var repeated = Definition("MP_REPEAT", 7, Writes());
            var allowed = Definition("MP_ALLOWED", 3, Writes());
            var guard = MicroPatternThirdRepeatGuard.Filter(
                Context(
                    History(1, "MPP_1", repeated.Id, Signature(repeated)),
                    History(2, "MPP_2", repeated.Id, Signature(repeated))),
                new[] { Source(repeated), Source(allowed) });
            var built = MicroPatternCandidateIndexBuilder.Build(
                MicroPatternBiomeProfileCatalog.CreateBuiltIn(),
                MoonpalaceBiomeId.MoonCrater,
                guard.AllowedSources);
            var selected = new MicroPatternDeterministicSelector(CreateDefinitionSet()).Select(
                0x12345678UL,
                new SectorCoord(2, 3),
                0,
                new[]
                {
                    new MicroPatternSelectionRequest(
                        new MicroPatternSelectionRequestId("MPS_ONLY"),
                        built.Index),
                });

            Assert.That(guard.Success, Is.True, RepetitionErrors(guard));
            Assert.That(built.Published, Is.True);
            Assert.That(built.Rejections, Is.Empty);
            Assert.That(built.Index.Candidates.Single().Key.PatternId, Is.EqualTo(allowed.Id));
            Assert.That(selected.Success, Is.True, SelectionErrors(selected));
            Assert.That(selected.Decisions.Single().ChosenKey.PatternId, Is.EqualTo(allowed.Id));
            Assert.That(selected.Decisions.Single().DrawCountAfter,
                Is.GreaterThan(selected.Decisions.Single().DrawCountBefore));
        }

        [Test]
        public void Guard_AllExcludedReturnsExplicitNoCandidateBeforeAnyRngDraw()
        {
            var repeated = Definition("MP_REPEAT", 1, Writes());
            var definitions = CreateDefinitionSet();
            var untouched = new DeterministicRngStreamFactory(definitions).Create(
                WorldGenerationRngStreams.SectorRecipeStreamId,
                1UL,
                RngStreamScope.Sector(new SectorCoord(0, 0), 0));

            var result = MicroPatternThirdRepeatGuard.Filter(
                Context(
                    History(1, "MPP_1", repeated.Id, Signature(repeated)),
                    History(2, "MPP_2", repeated.Id, Signature(repeated))),
                new[] { Source(repeated) });

            Assert.That(result.Success, Is.False);
            Assert.That(result.AllowedSources, Is.Empty);
            Assert.That(result.Exclusions.Count, Is.EqualTo(1));
            Assert.That(result.Errors.Single().Code,
                Is.EqualTo(MicroPatternRepetitionErrorCode.NoCandidateAfterThirdRepeatGuard));
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(untouched.DrawCount, Is.Zero);
        }

        [Test]
        public void Cleanup_ChangesExactSolidSpeckAndAirPinholeOnly()
        {
            var speck = MicroPatternLocalCleanup.Evaluate(Snapshot(
                Cell(0, 0, true, true),
                Cell(0, 1, false), Cell(0, -1, false),
                Cell(-1, 0, false), Cell(1, 0, false)));
            var pinhole = MicroPatternLocalCleanup.Evaluate(Snapshot(
                Cell(5, 5, false, true),
                Cell(5, 6, true), Cell(5, 4, true),
                Cell(4, 5, true), Cell(6, 5, true)));

            AssertDelta(speck, false, MicroPatternCleanupRule.SolidSpeck);
            AssertDelta(pinhole, true, MicroPatternCleanupRule.AirPinhole);
        }

        [Test]
        public void Cleanup_ChangesExactHeadSnagButNotNearMiss()
        {
            var exact = MicroPatternLocalCleanup.Evaluate(SixNeighborSnapshot(
                true,
                up: true, upLeft: true, upRight: true,
                left: false, right: false, down: false));
            var nearMiss = MicroPatternLocalCleanup.Evaluate(SixNeighborSnapshot(
                true,
                up: true, upLeft: true, upRight: true,
                left: true, right: false, down: false));

            AssertDelta(exact, false, MicroPatternCleanupRule.HeadSnag);
            Assert.That(nearMiss.Success, Is.True, CleanupErrors(nearMiss));
            Assert.That(nearMiss.Delta.Cells, Is.Empty);
        }

        [Test]
        public void Cleanup_ChangesExactBoxedBottomPitWithoutBroaderPitClaim()
        {
            var exact = MicroPatternLocalCleanup.Evaluate(SixNeighborSnapshot(
                false,
                up: false, upLeft: true, upRight: true,
                left: true, right: true, down: true));
            var broaderNearMiss = MicroPatternLocalCleanup.Evaluate(SixNeighborSnapshot(
                false,
                up: true, upLeft: true, upRight: true,
                left: true, right: true, down: false));

            AssertDelta(exact, true, MicroPatternCleanupRule.BoxedBottomPit);
            Assert.That(broaderNearMiss.Success, Is.True, CleanupErrors(broaderNearMiss));
            Assert.That(broaderNearMiss.Delta.Cells, Is.Empty);
        }

        [Test]
        public void Cleanup_ProtectedTargetPublishesEvidenceAndNoMutation()
        {
            var coordinate = new LocalTileCoord(0, 0);
            var source = new MicroPatternProtectedCell(
                coordinate,
                MicroPatternProtectedSourceKind.TraversalEnvelope,
                "ENVELOPE_A");
            var result = MicroPatternLocalCleanup.Evaluate(Snapshot(
                new MicroPatternCleanupCell(coordinate, true, true, true, new[] { source }),
                Cell(0, 1, false), Cell(0, -1, false),
                Cell(-1, 0, false), Cell(1, 0, false)));

            Assert.That(result.Success, Is.True, CleanupErrors(result));
            Assert.That(result.Delta.Cells, Is.Empty);
            var issue = result.Issues.Single(value =>
                value.Code == MicroPatternCleanupIssueCode.ProtectedWriteBlocked);
            Assert.That(issue.Rule, Is.EqualTo(MicroPatternCleanupRule.SolidSpeck));
            Assert.That(issue.ProtectionProvenance.Single(), Is.EqualTo(source));
        }

        [Test]
        public void Cleanup_MissingHaloSkipsWithEvidenceAndNeverCascadesSnapshot()
        {
            var sourceCells = new List<MicroPatternCleanupCell>
            {
                Cell(0, 0, true, true),
                Cell(0, 1, false),
                Cell(0, -1, false),
                Cell(-1, 0, false),
            };
            var snapshot = Snapshot(sourceCells.ToArray());
            var first = MicroPatternLocalCleanup.Evaluate(snapshot);
            var second = MicroPatternLocalCleanup.Evaluate(snapshot);

            Assert.That(first.Success, Is.True, CleanupErrors(first));
            Assert.That(first.Delta.Cells, Is.Empty);
            Assert.That(first.Issues.Any(value =>
                value.Code == MicroPatternCleanupIssueCode.InsufficientNeighborhood &&
                value.Detail.Contains("Right@1,0")), Is.True);
            Assert.That(second.StableDigest, Is.EqualTo(first.StableDigest));
            Assert.That(snapshot.Cells.Single(value => value.IsOwned).Solid, Is.True);
            sourceCells.Clear();
            Assert.That(snapshot.Cells.Count, Is.EqualTo(4));
        }

        [Test]
        public void Cleanup_CoalescesSameValueAndAtomicallyRejectsConflict()
        {
            var snapshot = Snapshot(Cell(0, 0, true, true));
            var same = MicroPatternLocalCleanup.ResolveProposals(
                snapshot,
                new[]
                {
                    new MicroPatternCleanupProposal(
                        new LocalTileCoord(0, 0), false, MicroPatternCleanupRule.SolidSpeck),
                    new MicroPatternCleanupProposal(
                        new LocalTileCoord(0, 0), false, MicroPatternCleanupRule.HeadSnag),
                });
            var conflict = MicroPatternLocalCleanup.ResolveProposals(
                snapshot,
                new[]
                {
                    new MicroPatternCleanupProposal(
                        new LocalTileCoord(0, 0), false, MicroPatternCleanupRule.SolidSpeck),
                    new MicroPatternCleanupProposal(
                        new LocalTileCoord(0, 0), true, MicroPatternCleanupRule.AirPinhole),
                });

            Assert.That(same.Success, Is.True, CleanupErrors(same));
            Assert.That(same.Proposals.Count, Is.EqualTo(1));
            CollectionAssert.AreEqual(
                new[] { MicroPatternCleanupRule.SolidSpeck, MicroPatternCleanupRule.HeadSnag },
                same.Delta.Cells.Single().Rules);
            Assert.That(conflict.Success, Is.False);
            Assert.That(conflict.Delta, Is.Null);
            Assert.That(conflict.StableDigest, Is.Empty);
            Assert.That(conflict.Errors.Select(value => value.Code),
                Does.Contain(MicroPatternLocalCleanupErrorCode.ConflictingCleanupProposal));
            Assert.That(conflict.Errors.Select(value => value.Code),
                Does.Contain(MicroPatternLocalCleanupErrorCode.AtomicCleanupRejected));
            Assert.That(snapshot.Cells.Single().Solid, Is.True);
        }

        [Test]
        public void ReversedEnumeration_IsStableAndPublishedCollectionsAreReadOnly()
        {
            var first = Definition("MP_A", 1, Writes());
            var second = Definition("MP_B", 1, Writes());
            var history = new[]
            {
                History(1, "MPP_1", first.Id, Signature(first)),
                History(2, "MPP_2", first.Id, Signature(first)),
            };
            var sources = new[] { Source(first), Source(second) };
            var forward = MicroPatternThirdRepeatGuard.Filter(Context(history), sources);
            var reverse = MicroPatternThirdRepeatGuard.Filter(
                Context(history.Reverse()), sources.Reverse());
            var cleanupCells = new[]
            {
                Cell(0, 0, true, true),
                Cell(0, 1, false), Cell(0, -1, false),
                Cell(-1, 0, false), Cell(1, 0, false),
            };
            var cleanupForward = MicroPatternLocalCleanup.Evaluate(Snapshot(cleanupCells));
            var cleanupReverse = MicroPatternLocalCleanup.Evaluate(
                Snapshot(cleanupCells.Reverse().ToArray()));

            Assert.That(reverse.StableDigest, Is.EqualTo(forward.StableDigest));
            CollectionAssert.AreEqual(
                forward.AllowedSources.Select(value => value.Definition.Id.Value).ToArray(),
                reverse.AllowedSources.Select(value => value.Definition.Id.Value).ToArray());
            Assert.That(cleanupReverse.StableDigest, Is.EqualTo(cleanupForward.StableDigest));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternCandidateSource>)forward.AllowedSources).Add(null));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternCleanupCellDelta>)cleanupForward.Delta.Cells).Add(null));
        }

        [Test]
        public void StructuralErrorsAreAtomicAndRuntimeHasNoForbiddenSideEffects()
        {
            var duplicate = MicroPatternLocalCleanup.Evaluate(Snapshot(
                Cell(0, 0, true, true),
                Cell(0, 0, false, true)));
            var invalidProtection = MicroPatternLocalCleanup.Evaluate(Snapshot(
                new MicroPatternCleanupCell(
                    new LocalTileCoord(1, 1), true, true, true,
                    Array.Empty<MicroPatternProtectedCell>())));
            var invalidHistory = MicroPatternThirdRepeatGuard.Filter(
                Context(
                    History(1, "MPP_DUP", new MicroPatternId("MP_A"), ZeroSignature()),
                    History(1, "MPP_DUP", new MicroPatternId("MP_A"), ZeroSignature())),
                Array.Empty<MicroPatternCandidateSource>());

            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Delta, Is.Null);
            Assert.That(duplicate.StableDigest, Is.Empty);
            Assert.That(duplicate.Errors.Select(value => value.Code),
                Does.Contain(MicroPatternLocalCleanupErrorCode.DuplicateCoordinate));
            Assert.That(invalidProtection.Errors.Select(value => value.Code),
                Does.Contain(MicroPatternLocalCleanupErrorCode.InvalidProtection));
            Assert.That(invalidHistory.Errors.Select(value => value.Code),
                Does.Contain(MicroPatternRepetitionErrorCode.DuplicateHistoryPlacement));

            var root = Path.Combine(
                Application.dataPath,
                "_Game", "Map", "Runtime", "WorldGeneration", "MicroPatterns");
            var source = string.Join("\n", new[]
            {
                "MicroPatternSilhouetteSignature.cs",
                "MicroPatternRepetitionGuard.cs",
                "MicroPatternLocalCleanup.cs",
            }.Select(name => File.ReadAllText(Path.Combine(root, name))));
            foreach (var forbidden in new[]
                     {
                         "System.Random", "UnityEngine.Random", "UnityEngine", "UnityEditor",
                         "MicroPatternOrderedRenderer", "SectorCanvas", "Tilemap", "MonoBehaviour",
                         "System.IO",
                     })
            {
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
            }
        }

        private static MicroPatternCleanupSnapshot SixNeighborSnapshot(
            bool center,
            bool up,
            bool upLeft,
            bool upRight,
            bool left,
            bool right,
            bool down)
        {
            return Snapshot(
                Cell(0, 0, center, true),
                Cell(0, 1, up), Cell(-1, 1, upLeft), Cell(1, 1, upRight),
                Cell(-1, 0, left), Cell(1, 0, right), Cell(0, -1, down));
        }

        private static MicroPatternCleanupCell Cell(
            int x,
            int y,
            bool solid,
            bool owned = false)
        {
            return new MicroPatternCleanupCell(
                new LocalTileCoord(x, y),
                solid,
                owned,
                false);
        }

        private static MicroPatternCleanupSnapshot Snapshot(
            params MicroPatternCleanupCell[] cells)
        {
            return new MicroPatternCleanupSnapshot(cells);
        }

        private static void AssertDelta(
            MicroPatternLocalCleanupResult result,
            bool expectedAfter,
            MicroPatternCleanupRule rule)
        {
            Assert.That(result.Success, Is.True, CleanupErrors(result));
            var delta = result.Delta.Cells.Single();
            Assert.That(delta.AfterSolid, Is.EqualTo(expectedAfter));
            Assert.That(delta.BeforeSolid, Is.Not.EqualTo(expectedAfter));
            Assert.That(delta.Rules, Does.Contain(rule));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static MicroPatternSilhouetteSignature ZeroSignature()
        {
            return Signature(Definition("MP_ZERO", 1, Writes()));
        }

        private static MicroPatternAcceptedHistoryItem History(
            long sequence,
            string placementId,
            MicroPatternId patternId,
            MicroPatternSilhouetteSignature signature)
        {
            return new MicroPatternAcceptedHistoryItem(
                sequence, placementId, patternId, signature);
        }

        private static MicroPatternRepetitionContext Context(
            params MicroPatternAcceptedHistoryItem[] history)
        {
            return new MicroPatternRepetitionContext(history);
        }

        private static MicroPatternRepetitionContext Context(
            IEnumerable<MicroPatternAcceptedHistoryItem> history)
        {
            return new MicroPatternRepetitionContext(history);
        }

        private static MicroPatternSilhouetteSignature Signature(
            MicroPatternDefinition definition)
        {
            return BuildSignature(Plan(definition));
        }

        private static MicroPatternSilhouetteSignature BuildSignature(
            MicroPatternApplicationPlan plan)
        {
            var result = MicroPatternSilhouetteSignatureBuilder.Build(plan);
            Assert.That(result.Success, Is.True,
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            return result.Signature;
        }

        private static MicroPatternCandidateSource Source(
            MicroPatternDefinition definition,
            MicroPatternTransform transform = MicroPatternTransform.R0)
        {
            return new MicroPatternCandidateSource(definition, transform, Plan(definition, transform));
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

        private static WorldRouteDefinitionSet CreateDefinitionSet()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                {
                    WorldGenerationRngStreams.SectorRecipeStreamId,
                    CreateDefinition(
                        WorldGenerationRngStreams.SectorRecipeStreamId,
                        "E9931A70C2D520F4",
                        "SECTOR")
                },
            };
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
            string resetScope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(
                typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(saltHex));
            SetAutoProperty(definition, "ResetScope", resetScope);
            SetAutoProperty(definition, "DescriptionKo", "test");
            SetAutoProperty(definition, "Active", true);
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

        private static string RepetitionErrors(MicroPatternRepetitionGuardResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string CleanupErrors(MicroPatternLocalCleanupResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string SelectionErrors(MicroPatternSelectionBatchResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }
    }
}
