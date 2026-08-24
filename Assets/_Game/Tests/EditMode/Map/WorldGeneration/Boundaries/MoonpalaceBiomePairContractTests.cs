using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBiomePairContractTests
    {
        private static readonly string[] InvalidBiomeIds =
        {
            null,
            string.Empty,
            " ",
            "Mooncrater",
            "moonCrater",
            "MOONCRATER",
            "Moon_Crater",
            "Moon Crater",
            "Crater",
            "Cassia",
            "Abandonedmill",
            "MoonDough ",
            "\tMoonDough",
            "Unknown",
            "0",
        };

        private static readonly string[] InvalidPairIds =
        {
            null,
            string.Empty,
            " ",
            "MoonCrater",
            "MoonCrater-CassiaRoot",
            "MoonCrater<->MoonCrater",
            "Unknown<->CassiaRoot",
            "MoonCrater<->Unknown",
            " MoonCrater<->CassiaRoot",
            "MoonCrater<->CassiaRoot ",
            "MoonCrater<->CassiaRoot<->MoonDough",
            "MoonCrater<>CassiaRoot",
            "MoonCrater|CassiaRoot",
            "mooncrater<->cassiaroot",
            "<->",
        };

        public static IEnumerable ContractCases
        {
            get
            {
                for (var index = 0; index < 180; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("PairOrientationWarningContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void PairOrientationWarningContract(int caseIndex)
        {
            var catalog = MoonpalaceBiomePairCatalog.Canonical;
            var cycle = caseIndex / 12;
            var pair = catalog.Pairs[cycle % catalog.Pairs.Count];
            var biome = catalog.Biomes[cycle % catalog.Biomes.Count];
            var definition = catalog.GetDefinition(pair);

            switch (caseIndex % 12)
            {
                case 0:
                    Assert.That(MoonpalaceBiomePair.TryParse(pair.PairId, out var parsed), Is.True);
                    Assert.That(parsed, Is.EqualTo(pair));
                    Assert.That(MoonpalaceBiomePair.Parse(pair.ToString()), Is.EqualTo(pair));
                    break;
                case 1:
                    var reversedId = pair.Second.CanonicalId + "<->" + pair.First.CanonicalId;
                    Assert.That(MoonpalaceBiomePair.TryParse(reversedId, out var reversed), Is.True);
                    Assert.That(reversed, Is.EqualTo(pair));
                    Assert.That(reversed.PairId, Is.EqualTo(pair.PairId));
                    break;
                case 2:
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBiomePair(biome, biome));
                    break;
                case 3:
                    var invalidBiomeId = InvalidBiomeIds[cycle % InvalidBiomeIds.Length];
                    Assert.That(MoonpalaceBiomeId.TryParse(invalidBiomeId, out var invalidBiome), Is.False);
                    Assert.That(invalidBiome.IsDefined, Is.False);
                    Assert.Throws<FormatException>(() => MoonpalaceBiomeId.Parse(invalidBiomeId));
                    break;
                case 4:
                    var invalidPairId = InvalidPairIds[cycle % InvalidPairIds.Length];
                    Assert.That(MoonpalaceBiomePair.TryParse(invalidPairId, out var invalidPair), Is.False);
                    Assert.That(invalidPair.IsDefined, Is.False);
                    Assert.Throws<FormatException>(() => MoonpalaceBiomePair.Parse(invalidPairId));
                    break;
                case 5:
                    Assert.That(default(MoonpalaceBiomeId).IsDefined, Is.False);
                    Assert.That(default(MoonpalaceBiomePair).IsDefined, Is.False);
                    Assert.That(catalog.TryGetDefinition(default, out var missing), Is.False);
                    Assert.That(missing, Is.Null);
                    break;
                case 6:
                    Assert.That(definition.SupportedOrientations,
                        Is.EqualTo(new[]
                        {
                            MoonpalaceBoundaryOrientation.Horizontal,
                            MoonpalaceBoundaryOrientation.Vertical,
                        }));
                    Assert.That(definition.Supports((MoonpalaceBoundaryOrientation)99), Is.False);
                    break;
                case 7:
                    var orientations = new[]
                    {
                        MoonpalaceBoundaryOrientation.Horizontal,
                        MoonpalaceBoundaryOrientation.Vertical,
                    };
                    var detached = new MoonpalaceBiomePairDefinition(
                        pair,
                        orientations,
                        MoonpalaceBiomePairDefinition.NoToolRequirement,
                        true,
                        MoonpalaceBoundaryWarningMarker.Tile |
                        MoonpalaceBoundaryWarningMarker.Background,
                        2);
                    orientations[0] = MoonpalaceBoundaryOrientation.Vertical;
                    Assert.That(detached.SupportedOrientations[0],
                        Is.EqualTo(MoonpalaceBoundaryOrientation.Horizontal));
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBoundaryOrientation>)detached.SupportedOrientations).Clear());
                    break;
                case 8:
                    AssertInvalidDefinition(cycle, pair);
                    break;
                case 9:
                    var duplicateDefinitions = catalog.Definitions.ToArray();
                    duplicateDefinitions[duplicateDefinitions.Length - 1] = duplicateDefinitions[0];
                    Assert.Throws<ArgumentException>(() =>
                        new MoonpalaceBiomePairCatalog(duplicateDefinitions));
                    break;
                case 10:
                    Assert.Throws<ArgumentException>(() =>
                        new MoonpalaceBiomePairCatalog(catalog.Definitions.Take(5)));
                    break;
                case 11:
                    var originalCulture = CultureInfo.CurrentCulture;
                    var originalUiCulture = CultureInfo.CurrentUICulture;
                    try
                    {
                        var culture = CultureInfo.GetCultureInfo(
                            cycle % 3 == 0 ? "tr-TR" : cycle % 3 == 1 ? "ar-SA" : "fr-FR");
                        CultureInfo.CurrentCulture = culture;
                        CultureInfo.CurrentUICulture = culture;
                        Assert.That(definition.Signature,
                            Is.EqualTo(MoonpalaceBiomePairDefinition.CreateCanonical(pair).Signature));
                        Assert.That(pair.ToString(), Is.EqualTo(pair.PairId));
                    }
                    finally
                    {
                        CultureInfo.CurrentCulture = originalCulture;
                        CultureInfo.CurrentUICulture = originalUiCulture;
                    }
                    break;
                default:
                    Assert.Fail("Unexpected pair contract case.");
                    break;
            }
        }

        private static void AssertInvalidDefinition(int cycle, MoonpalaceBiomePair pair)
        {
            var orientations = new[]
            {
                MoonpalaceBoundaryOrientation.Horizontal,
                MoonpalaceBoundaryOrientation.Vertical,
            };
            var markers = MoonpalaceBoundaryWarningMarker.Tile |
                          MoonpalaceBoundaryWarningMarker.Background;

            switch (cycle % 5)
            {
                case 0:
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBiomePairDefinition(
                        pair, orientations.Reverse(), "NONE", true, markers, 2));
                    break;
                case 1:
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBiomePairDefinition(
                        pair, orientations, "PICKAXE", true, markers, 2));
                    break;
                case 2:
                    Assert.Throws<ArgumentException>(() => new MoonpalaceBiomePairDefinition(
                        pair, orientations, "NONE", false, markers, 2));
                    break;
                case 3:
                    Assert.Throws<ArgumentOutOfRangeException>(() => new MoonpalaceBiomePairDefinition(
                        pair, orientations, "NONE", true, MoonpalaceBoundaryWarningMarker.Tile, 2));
                    break;
                case 4:
                    Assert.Throws<ArgumentOutOfRangeException>(() => new MoonpalaceBiomePairDefinition(
                        pair, orientations, "NONE", true,
                        (MoonpalaceBoundaryWarningMarker)(1 << 7), 2));
                    break;
                default:
                    Assert.Fail("Unexpected invalid definition case.");
                    break;
            }
        }
    }
}
