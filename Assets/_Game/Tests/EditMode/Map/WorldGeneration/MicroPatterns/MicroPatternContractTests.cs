using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.MicroPatterns
{
    [TestFixture]
    [Category("MAP09_03")]
    public sealed class MicroPatternContractTests
    {
        [Test]
        public void ValidDefinitionPublishesExactlySixteenExplicitCells()
        {
            var definition = CreateValid();
            var result = MicroPatternValidator.Validate(definition);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Definition, Is.SameAs(definition));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(definition.Width, Is.EqualTo(4));
            Assert.That(definition.Height, Is.EqualTo(4));
            Assert.That(definition.Cells, Has.Count.EqualTo(16));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(definition.ComputeStableDigest(), Is.EqualTo(result.StableDigest));
        }

        [Test]
        public void CellsAreStoredInCanonicalIndexOrderZeroThroughFifteen()
        {
            var definition = CreateValid(cells: CreateCells().Reverse());
            var indices = definition.Cells
                .Select(value => MicroPatternDefinition.CanonicalCellIndex(value.Coordinate))
                .ToArray();

            Assert.That(indices, Is.EqualTo(Enumerable.Range(0, 16)));
        }

        [Test]
        public void MissingCellIsRejected()
        {
            var result = MicroPatternValidator.Validate(CreateValid(cells: CreateCells().Take(15)));
            AssertCodes(result,
                MicroPatternValidationErrorCode.InvalidCellCount,
                MicroPatternValidationErrorCode.MissingCell);
        }

        [Test]
        public void DuplicateCellIsRejectedWithoutThrowing()
        {
            var cells = CreateCells().ToArray();
            cells[15] = new MicroPatternCell(new LocalTileCoord(0, 0));
            MicroPatternValidationResult result = null;

            Assert.DoesNotThrow(() => result = MicroPatternValidator.Validate(CreateValid(cells: cells)));
            AssertCodes(result,
                MicroPatternValidationErrorCode.DuplicateCell,
                MicroPatternValidationErrorCode.MissingCell);
        }

        [Test]
        public void OutOfRangeCellIsRejectedAndLeavesTheExpectedCellMissing()
        {
            var cells = CreateCells().ToArray();
            cells[15] = new MicroPatternCell(new LocalTileCoord(4, 3));
            var result = MicroPatternValidator.Validate(CreateValid(cells: cells));

            AssertCodes(result,
                MicroPatternValidationErrorCode.MissingCell,
                MicroPatternValidationErrorCode.CellOutOfRange);
        }

        [Test]
        public void ContractEnumsContainOnlyTheExactPublishedValues()
        {
            Assert.That(Enum.GetNames(typeof(MicroPatternLayer)), Is.EqualTo(new[]
            {
                "Geometry", "Surface", "Affordance", "Material", "Hazard", "Marker",
            }));
            Assert.That(Enum.GetNames(typeof(MicroPatternOperation)), Is.EqualTo(new[]
            {
                "NoChange", "AddSolid", "CarveAir", "SetSurface", "SetAffordance",
                "SetMaterial", "SetHazard", "SetMarker",
            }));
            Assert.That(Enum.GetNames(typeof(MicroPatternTransform)), Is.EqualTo(new[]
            {
                "R0", "MirrorX", "MirrorY", "R180",
            }));
            Assert.That(Enum.GetNames(typeof(MicroPatternProtectedPolicy)), Is.EqualTo(new[]
            {
                "ForceNoChange", "RejectCandidate",
            }));
        }

        [TestCase(MicroPatternLayer.Geometry, MicroPatternOperation.NoChange, null)]
        [TestCase(MicroPatternLayer.Geometry, MicroPatternOperation.AddSolid, null)]
        [TestCase(MicroPatternLayer.Geometry, MicroPatternOperation.CarveAir, null)]
        [TestCase(MicroPatternLayer.Surface, MicroPatternOperation.NoChange, null)]
        [TestCase(MicroPatternLayer.Surface, MicroPatternOperation.SetSurface, "STONE")]
        [TestCase(MicroPatternLayer.Affordance, MicroPatternOperation.NoChange, null)]
        [TestCase(MicroPatternLayer.Affordance, MicroPatternOperation.SetAffordance, "CLIMBABLE")]
        [TestCase(MicroPatternLayer.Material, MicroPatternOperation.NoChange, null)]
        [TestCase(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MOON_ROCK")]
        [TestCase(MicroPatternLayer.Hazard, MicroPatternOperation.NoChange, null)]
        [TestCase(MicroPatternLayer.Hazard, MicroPatternOperation.SetHazard, "SPIKES")]
        [TestCase(MicroPatternLayer.Marker, MicroPatternOperation.NoChange, null)]
        [TestCase(MicroPatternLayer.Marker, MicroPatternOperation.SetMarker, "LOOT_HINT")]
        public void ExactLayerOperationMatrixAcceptsAllowedPair(
            MicroPatternLayer layer,
            MicroPatternOperation operation,
            string payload)
        {
            var result = ValidateSingleInstruction(new MicroPatternInstruction(layer, operation, payload));
            Assert.That(result.IsValid, Is.True);
        }

        [TestCase(MicroPatternLayer.Geometry, MicroPatternOperation.SetSurface)]
        [TestCase(MicroPatternLayer.Surface, MicroPatternOperation.AddSolid)]
        [TestCase(MicroPatternLayer.Affordance, MicroPatternOperation.SetMaterial)]
        [TestCase(MicroPatternLayer.Material, MicroPatternOperation.SetHazard)]
        [TestCase(MicroPatternLayer.Hazard, MicroPatternOperation.SetMarker)]
        [TestCase(MicroPatternLayer.Marker, MicroPatternOperation.CarveAir)]
        public void LayerOperationMismatchIsRejected(
            MicroPatternLayer layer,
            MicroPatternOperation operation)
        {
            var payload = operation >= MicroPatternOperation.SetSurface ? "VALUE" : null;
            var result = ValidateSingleInstruction(new MicroPatternInstruction(layer, operation, payload));
            AssertCodes(result, MicroPatternValidationErrorCode.InvalidLayerOperation);
        }

        [Test]
        public void DuplicateLayerInstructionIsRejectedAndDeduplicated()
        {
            var instruction = new MicroPatternInstruction(
                MicroPatternLayer.Geometry,
                MicroPatternOperation.AddSolid);
            var result = ValidateSingleInstruction(instruction, instruction, instruction);

            Assert.That(result.Errors.Count(value =>
                value.Code == MicroPatternValidationErrorCode.DuplicateLayerInstruction), Is.EqualTo(1));
        }

        [TestCase(MicroPatternOperation.NoChange)]
        [TestCase(MicroPatternOperation.AddSolid)]
        [TestCase(MicroPatternOperation.CarveAir)]
        public void PayloadFreeOperationsRejectPayload(MicroPatternOperation operation)
        {
            var result = ValidateSingleInstruction(new MicroPatternInstruction(
                MicroPatternLayer.Geometry,
                operation,
                "UNEXPECTED"));
            AssertCodes(result, MicroPatternValidationErrorCode.UnexpectedPayload);
        }

        [Test]
        public void SetOperationRequiresPayload()
        {
            var result = ValidateSingleInstruction(new MicroPatternInstruction(
                MicroPatternLayer.Surface,
                MicroPatternOperation.SetSurface));
            AssertCodes(result, MicroPatternValidationErrorCode.MissingPayload);
        }

        [TestCase("lowercase")]
        [TestCase("9STARTS_WITH_DIGIT")]
        [TestCase("HAS-DASH")]
        [TestCase("HAS SPACE")]
        public void PayloadMustUseStableIdGrammar(string payload)
        {
            var result = ValidateSingleInstruction(new MicroPatternInstruction(
                MicroPatternLayer.Surface,
                MicroPatternOperation.SetSurface,
                payload));
            AssertCodes(result, MicroPatternValidationErrorCode.InvalidPayloadId);
        }

        [TestCase(0)]
        [TestCase(999)]
        public void UndefinedLayerOrOperationIsRejected(int value)
        {
            var instruction = value == 0
                ? new MicroPatternInstruction((MicroPatternLayer)0, MicroPatternOperation.NoChange)
                : new MicroPatternInstruction(MicroPatternLayer.Geometry, (MicroPatternOperation)value);
            AssertCodes(
                ValidateSingleInstruction(instruction),
                MicroPatternValidationErrorCode.InvalidLayerOperation);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("MP_")]
        [TestCase("mp_VALID")]
        [TestCase("MP-VALID")]
        [TestCase("OTHER_VALID")]
        public void PatternIdMustUseExactGrammar(string id)
        {
            var result = MicroPatternValidator.Validate(CreateValid(id: new MicroPatternId(id)));
            AssertCodes(result, MicroPatternValidationErrorCode.InvalidPatternId);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(10001)]
        public void WeightOutsideInclusiveIntegerRangeIsRejected(int weight)
        {
            AssertCodes(
                MicroPatternValidator.Validate(CreateValid(weight: weight)),
                MicroPatternValidationErrorCode.InvalidWeight);
        }

        [TestCase(1)]
        [TestCase(10000)]
        public void WeightInclusiveEndpointsAreAccepted(int weight)
        {
            Assert.That(MicroPatternValidator.Validate(CreateValid(weight: weight)).IsValid, Is.True);
        }

        [Test]
        public void BiomeAllowlistMustBeNonEmptyUniqueAndKnown()
        {
            AssertCodes(
                MicroPatternValidator.Validate(CreateValid(biomes: Array.Empty<MoonpalaceBiomeId>())),
                MicroPatternValidationErrorCode.MissingBiome);
            AssertCodes(
                MicroPatternValidator.Validate(CreateValid(biomes: new[] { default(MoonpalaceBiomeId) })),
                MicroPatternValidationErrorCode.UnknownBiome);
            AssertCodes(
                MicroPatternValidator.Validate(CreateValid(biomes: new[]
                {
                    MoonpalaceBiomeId.MoonCrater,
                    MoonpalaceBiomeId.MoonCrater,
                })),
                MicroPatternValidationErrorCode.DuplicateBiome);
        }

        [Test]
        public void ExistingTypedStarterBiomesAreReusedAndStoredInCanonicalOrdinalOrder()
        {
            var definition = CreateValid(biomes: new[]
            {
                MoonpalaceBiomeId.MoonDough,
                MoonpalaceBiomeId.MoonCrater,
                MoonpalaceBiomeId.AbandonedMill,
                MoonpalaceBiomeId.CassiaRoot,
            });

            Assert.That(definition.AllowedBiomes.Select(value => value.CanonicalId), Is.EqualTo(new[]
            {
                "AbandonedMill", "CassiaRoot", "MoonCrater", "MoonDough",
            }));
            Assert.That(MicroPatternValidator.Validate(definition).IsValid, Is.True);
        }

        [Test]
        public void TransformAllowlistIsNonEmptyUniqueAndRequiresR0()
        {
            AssertCodes(
                MicroPatternValidator.Validate(CreateValid(transforms: Array.Empty<MicroPatternTransform>())),
                MicroPatternValidationErrorCode.MissingTransform,
                MicroPatternValidationErrorCode.MissingR0);
            AssertCodes(
                MicroPatternValidator.Validate(CreateValid(transforms: new[] { MicroPatternTransform.MirrorX })),
                MicroPatternValidationErrorCode.MissingR0);
            AssertCodes(
                MicroPatternValidator.Validate(CreateValid(transforms: new[]
                {
                    MicroPatternTransform.R0,
                    MicroPatternTransform.R0,
                })),
                MicroPatternValidationErrorCode.DuplicateTransform);
        }

        [TestCase(90)]
        [TestCase(270)]
        public void QuarterTurnsAreUnsupported(int value)
        {
            var result = MicroPatternValidator.Validate(CreateValid(transforms: new[]
            {
                MicroPatternTransform.R0,
                (MicroPatternTransform)value,
            }));
            AssertCodes(result, MicroPatternValidationErrorCode.UnsupportedTransform);
        }

        [Test]
        public void ExactProtectedPoliciesAreAcceptedAndAllowWritePolicyDoesNotExist()
        {
            Assert.That(MicroPatternValidator.Validate(CreateValid(
                policy: MicroPatternProtectedPolicy.ForceNoChange)).IsValid, Is.True);
            Assert.That(MicroPatternValidator.Validate(CreateValid(
                policy: MicroPatternProtectedPolicy.RejectCandidate)).IsValid, Is.True);
            AssertCodes(
                MicroPatternValidator.Validate(CreateValid(policy: (MicroPatternProtectedPolicy)0)),
                MicroPatternValidationErrorCode.InvalidProtectedPolicy);
            Assert.That(Enum.GetNames(typeof(MicroPatternProtectedPolicy)), Does.Not.Contain("AllowWrite"));
        }

        [Test]
        public void CallerCollectionsCannotMutatePublishedDefinitionOrDigest()
        {
            var instructions = new List<MicroPatternInstruction>
            {
                new MicroPatternInstruction(MicroPatternLayer.Geometry, MicroPatternOperation.AddSolid),
            };
            var cells = CreateCells().ToList();
            cells[0] = new MicroPatternCell(new LocalTileCoord(0, 0), instructions);
            var biomes = new List<MoonpalaceBiomeId> { MoonpalaceBiomeId.MoonCrater };
            var transforms = new List<MicroPatternTransform> { MicroPatternTransform.R0 };
            var definition = CreateValid(cells: cells, biomes: biomes, transforms: transforms);
            var digest = MicroPatternValidator.Validate(definition).StableDigest;

            instructions.Clear();
            cells.Clear();
            biomes.Clear();
            transforms.Clear();

            Assert.That(definition.Cells, Has.Count.EqualTo(16));
            Assert.That(definition.Cells[0].Instructions, Has.Count.EqualTo(1));
            Assert.That(definition.AllowedBiomes, Has.Count.EqualTo(1));
            Assert.That(definition.AllowedTransforms, Has.Count.EqualTo(1));
            Assert.That(MicroPatternValidator.Validate(definition).StableDigest, Is.EqualTo(digest));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternCell>)definition.Cells).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternInstruction>)definition.Cells[0].Instructions).Clear());
        }

        [Test]
        public void DigestIgnoresInputOrderAndExplicitNoChange()
        {
            var omitted = CreateValid(
                cells: CreateCells().Reverse(),
                biomes: MoonpalaceBiomePairCatalog.Canonical.Biomes.Reverse(),
                transforms: new[]
                {
                    MicroPatternTransform.MirrorY,
                    MicroPatternTransform.R0,
                    MicroPatternTransform.MirrorX,
                });
            var explicitNoChange = CreateValid(
                cells: CreateCells(explicitNoChange: true),
                biomes: MoonpalaceBiomePairCatalog.Canonical.Biomes,
                transforms: new[]
                {
                    MicroPatternTransform.R0,
                    MicroPatternTransform.MirrorX,
                    MicroPatternTransform.MirrorY,
                });

            Assert.That(
                MicroPatternValidator.Validate(omitted).StableDigest,
                Is.EqualTo(MicroPatternValidator.Validate(explicitNoChange).StableDigest));
        }

        [Test]
        public void DisplayTextAndCultureDoNotAffectDigest()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                var first = MicroPatternValidator.Validate(CreateValid(displayId: "DISPLAY_A")).StableDigest;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var second = MicroPatternValidator.Validate(CreateValid(displayId: "localized text")).StableDigest;
                Assert.That(second, Is.EqualTo(first));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void EveryDigestSemanticChangesTheDigest()
        {
            var baseline = Digest(CreateValid());
            Assert.That(Digest(CreateValid(id: new MicroPatternId("MP_OTHER"))), Is.Not.EqualTo(baseline));
            Assert.That(Digest(CreateValid(weight: 2)), Is.Not.EqualTo(baseline));
            Assert.That(Digest(CreateValid(
                biomes: new[] { MoonpalaceBiomeId.CassiaRoot })), Is.Not.EqualTo(baseline));
            Assert.That(Digest(CreateValid(transforms: new[]
            {
                MicroPatternTransform.R0,
                MicroPatternTransform.MirrorX,
            })), Is.Not.EqualTo(baseline));
            Assert.That(Digest(CreateValid(
                policy: MicroPatternProtectedPolicy.RejectCandidate)), Is.Not.EqualTo(baseline));
            Assert.That(Digest(CreateValid(cells: CellsWithInstruction(
                new MicroPatternInstruction(
                    MicroPatternLayer.Material,
                    MicroPatternOperation.SetMaterial,
                    "STONE")))), Is.Not.EqualTo(baseline));
            Assert.That(Digest(CreateValid(cells: CellsWithInstruction(
                new MicroPatternInstruction(
                    MicroPatternLayer.Material,
                    MicroPatternOperation.SetMaterial,
                    "WOOD")))), Is.Not.EqualTo(baseline));
        }

        [Test]
        public void ValidationAccumulatesSortsAndDeduplicatesErrorsWithoutPartialPublish()
        {
            var cells = new[]
            {
                new MicroPatternCell(
                    new LocalTileCoord(9, -1),
                    new[]
                    {
                        new MicroPatternInstruction(
                            MicroPatternLayer.Geometry,
                            MicroPatternOperation.AddSolid,
                            "BAD"),
                        new MicroPatternInstruction(
                            MicroPatternLayer.Geometry,
                            MicroPatternOperation.AddSolid,
                            "BAD"),
                    }),
            };
            var definition = new MicroPatternDefinition(
                new MicroPatternId("bad"),
                3,
                5,
                cells,
                0,
                Array.Empty<MoonpalaceBiomeId>(),
                new[] { (MicroPatternTransform)90 },
                (MicroPatternProtectedPolicy)0);
            MicroPatternValidationResult result = null;

            Assert.DoesNotThrow(() => result = MicroPatternValidator.Validate(definition));
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Definition, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.Errors, Has.Count.GreaterThan(8));
            Assert.That(result.Errors, Is.EqualTo(result.Errors.OrderBy(value => value).ToArray()));
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternValidationError>)result.Errors).Clear());
        }

        [Test]
        public void NullDefinitionCellAndInstructionAreReportedWithoutExceptions()
        {
            AssertCodes(
                MicroPatternValidator.Validate(null),
                MicroPatternValidationErrorCode.MissingInput);

            var cells = CreateCells().Cast<MicroPatternCell>().ToArray();
            cells[15] = null;
            MicroPatternValidationResult cellResult = null;
            Assert.DoesNotThrow(() => cellResult = MicroPatternValidator.Validate(CreateValid(cells: cells)));
            AssertCodes(cellResult,
                MicroPatternValidationErrorCode.MissingInput,
                MicroPatternValidationErrorCode.MissingCell);

            var instructionResult = ValidateSingleInstruction((MicroPatternInstruction)null);
            AssertCodes(instructionResult, MicroPatternValidationErrorCode.MissingInput);
        }

        [Test]
        public void DimensionsAreExactlyFourByFourAndDoNotRenameMicroChunk()
        {
            Assert.That(MicroPatternDefinition.RequiredWidth, Is.EqualTo(4));
            Assert.That(MicroPatternDefinition.RequiredHeight, Is.EqualTo(4));
            Assert.That(MicroPatternDefinition.RequiredCellCount, Is.EqualTo(16));
            Assert.That(MicroPatternValidator.Validate(CreateValid(width: 12, height: 8)).Errors
                .Select(value => value.Code), Does.Contain(MicroPatternValidationErrorCode.InvalidDimensions));
            Assert.That(typeof(MicroPatternCell).GetProperty(nameof(MicroPatternCell.Coordinate)).PropertyType,
                Is.EqualTo(typeof(LocalTileCoord)));
        }

        [Test]
        public void ContractObjectsExposeNoWritablePublicProperties()
        {
            var types = new[]
            {
                typeof(MicroPatternInstruction),
                typeof(MicroPatternCell),
                typeof(MicroPatternDefinition),
                typeof(MicroPatternValidationError),
                typeof(MicroPatternValidationResult),
            };
            Assert.That(types.SelectMany(value => value.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .Where(value => value.CanWrite), Is.Empty);
        }

        [Test]
        public void RuntimeScopeHasNoForbiddenExecutionOrDuplicateDomainSymbols()
        {
            var root = FullPath("Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns");
            var source = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(File.ReadAllText));
            var forbidden = new[]
            {
                "UnityEditor", "StageMapGenerator", "GridWorld", "RoomTemplate",
                "RoomGridTransform", "TileMutationService", "SectorRecipeResolver",
                "MonoBehaviour", "System.Random", "UnityEngine.Random", "DateTime",
                "File.", "Directory.", "WorldGenerationRoot",
            };

            Assert.That(forbidden.Where(source.Contains), Is.Empty);
            Assert.That(Regex.IsMatch(source, @"\b(class|struct|enum)\s+MicroChunk\b"), Is.False);
            Assert.That(Regex.IsMatch(source, @"\bstruct\s+MoonpalaceBiomeId\b"), Is.False);
            Assert.That(Regex.IsMatch(source, @"\b(class|struct)\s+LocalTileCoord\b"), Is.False);
        }

        private static MicroPatternValidationResult ValidateSingleInstruction(
            params MicroPatternInstruction[] instructions)
        {
            return MicroPatternValidator.Validate(CreateValid(cells: CellsWithInstructions(instructions)));
        }

        private static IEnumerable<MicroPatternCell> CellsWithInstruction(
            MicroPatternInstruction instruction)
        {
            return CellsWithInstructions(instruction);
        }

        private static IEnumerable<MicroPatternCell> CellsWithInstructions(
            params MicroPatternInstruction[] instructions)
        {
            var cells = CreateCells().ToArray();
            cells[0] = new MicroPatternCell(new LocalTileCoord(0, 0), instructions);
            return cells;
        }

        private static IEnumerable<MicroPatternCell> CreateCells(bool explicitNoChange = false)
        {
            for (var y = 0; y < MicroPatternDefinition.RequiredHeight; y++)
            {
                for (var x = 0; x < MicroPatternDefinition.RequiredWidth; x++)
                {
                    var instructions = explicitNoChange
                        ? Enum.GetValues(typeof(MicroPatternLayer))
                            .Cast<MicroPatternLayer>()
                            .Reverse()
                            .Select(value => new MicroPatternInstruction(
                                value,
                                MicroPatternOperation.NoChange))
                            .ToArray()
                        : Array.Empty<MicroPatternInstruction>();
                    yield return new MicroPatternCell(new LocalTileCoord(x, y), instructions);
                }
            }
        }

        private static MicroPatternDefinition CreateValid(
            MicroPatternId? id = null,
            int width = MicroPatternDefinition.RequiredWidth,
            int height = MicroPatternDefinition.RequiredHeight,
            IEnumerable<MicroPatternCell> cells = null,
            int weight = 1,
            IEnumerable<MoonpalaceBiomeId> biomes = null,
            IEnumerable<MicroPatternTransform> transforms = null,
            MicroPatternProtectedPolicy policy = MicroPatternProtectedPolicy.ForceNoChange,
            string displayId = "DISPLAY")
        {
            return new MicroPatternDefinition(
                id ?? new MicroPatternId("MP_VALID"),
                width,
                height,
                cells ?? CreateCells(),
                weight,
                biomes ?? MoonpalaceBiomePairCatalog.Canonical.Biomes,
                transforms ?? new[] { MicroPatternTransform.R0 },
                policy,
                displayId);
        }

        private static string Digest(MicroPatternDefinition definition)
        {
            var result = MicroPatternValidator.Validate(definition);
            Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
            return result.StableDigest;
        }

        private static void AssertCodes(
            MicroPatternValidationResult result,
            params MicroPatternValidationErrorCode[] expected)
        {
            Assert.That(result.IsValid, Is.False);
            var actual = result.Errors.Select(value => value.Code).Distinct().ToArray();
            foreach (var code in expected)
            {
                Assert.That(actual, Does.Contain(code), string.Join("; ", result.Errors));
            }
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
