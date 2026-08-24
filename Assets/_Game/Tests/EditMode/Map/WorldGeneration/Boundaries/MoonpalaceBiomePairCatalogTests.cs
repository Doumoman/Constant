using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBiomePairCatalogTests
    {
        private static readonly string[] ExpectedBiomeIds =
        {
            "MoonCrater",
            "CassiaRoot",
            "AbandonedMill",
            "MoonDough",
        };

        private static readonly string[] ExpectedDisplayNames =
        {
            "Moon Crater",
            "Cassia Root",
            "Abandoned Mill",
            "Moon Dough",
        };

        private static readonly string[] ExpectedPairIds =
        {
            "MoonCrater<->CassiaRoot",
            "MoonCrater<->AbandonedMill",
            "MoonCrater<->MoonDough",
            "CassiaRoot<->AbandonedMill",
            "CassiaRoot<->MoonDough",
            "AbandonedMill<->MoonDough",
        };

        public static IEnumerable CatalogCases
        {
            get
            {
                for (var index = 0; index < 220; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("CanonicalCatalogContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(CatalogCases))]
        public void CanonicalCatalogContract(int caseIndex)
        {
            var catalog = MoonpalaceBiomePairCatalog.Canonical;
            var cycle = caseIndex / 11;
            var biomeIndex = cycle % catalog.Biomes.Count;
            var pairIndex = cycle % catalog.Pairs.Count;
            var biome = catalog.Biomes[biomeIndex];
            var pair = catalog.Pairs[pairIndex];
            var definition = catalog.Definitions[pairIndex];

            switch (caseIndex % 11)
            {
                case 0:
                    Assert.That(catalog.Biomes, Has.Count.EqualTo(4));
                    Assert.That(catalog.Pairs, Has.Count.EqualTo(6));
                    Assert.That(catalog.Definitions, Has.Count.EqualTo(6));
                    Assert.That(catalog.Biomes.Select(value => value.CanonicalId),
                        Is.EqualTo(ExpectedBiomeIds));
                    Assert.That(catalog.Pairs.Select(value => value.PairId),
                        Is.EqualTo(ExpectedPairIds));
                    break;
                case 1:
                    Assert.That(biome.IsDefined, Is.True);
                    Assert.That(biome.Order, Is.EqualTo(biomeIndex));
                    Assert.That(biome.CanonicalId, Is.EqualTo(ExpectedBiomeIds[biomeIndex]));
                    Assert.That(biome.DisplayName, Is.EqualTo(ExpectedDisplayNames[biomeIndex]));
                    break;
                case 2:
                    Assert.That(MoonpalaceBiomeId.TryParse(biome.CanonicalId, out var parsed), Is.True);
                    Assert.That(parsed, Is.EqualTo(biome));
                    Assert.That(MoonpalaceBiomeId.Parse(biome.ToString()), Is.EqualTo(biome));
                    break;
                case 3:
                    var nonCanonical = cycle % 2 == 0
                        ? biome.CanonicalId.ToLowerInvariant()
                        : " " + biome.CanonicalId;
                    Assert.That(MoonpalaceBiomeId.TryParse(nonCanonical, out _), Is.False);
                    Assert.Throws<FormatException>(() => MoonpalaceBiomeId.Parse(nonCanonical));
                    break;
                case 4:
                    Assert.That(pair.IsDefined, Is.True);
                    Assert.That(pair.First.Order, Is.LessThan(pair.Second.Order));
                    Assert.That(pair.PairId, Is.EqualTo(ExpectedPairIds[pairIndex]));
                    Assert.That(catalog.GetDefinition(pair), Is.SameAs(definition));
                    break;
                case 5:
                    var reversed = new MoonpalaceBiomePair(pair.Second, pair.First);
                    Assert.That(reversed, Is.EqualTo(pair));
                    Assert.That(reversed.GetHashCode(), Is.EqualTo(pair.GetHashCode()));
                    Assert.That(reversed.PairId, Is.EqualTo(pair.PairId));
                    break;
                case 6:
                    Assert.That(definition.Supports(MoonpalaceBoundaryOrientation.Horizontal), Is.True);
                    Assert.That(definition.SupportedOrientations[0],
                        Is.EqualTo(MoonpalaceBoundaryOrientation.Horizontal));
                    break;
                case 7:
                    Assert.That(definition.Supports(MoonpalaceBoundaryOrientation.Vertical), Is.True);
                    Assert.That(definition.SupportedOrientations[1],
                        Is.EqualTo(MoonpalaceBoundaryOrientation.Vertical));
                    break;
                case 8:
                    Assert.That(definition.MandatoryToolRequirement,
                        Is.EqualTo(MoonpalaceBiomePairDefinition.NoToolRequirement));
                    Assert.That(definition.MandatoryRouteAllowed, Is.True);
                    break;
                case 9:
                    Assert.That(definition.MinimumDistinctWarningMarkerCount,
                        Is.EqualTo(MoonpalaceBiomePairDefinition.RequiredMinimumWarningMarkerCount));
                    Assert.That(MoonpalaceBiomePairDefinition.CountWarningMarkers(
                            definition.AvailableWarningMarkers),
                        Is.GreaterThanOrEqualTo(definition.MinimumDistinctWarningMarkerCount));
                    break;
                case 10:
                    var rebuilt = new MoonpalaceBiomePairCatalog(catalog.Definitions.ToArray());
                    Assert.That(rebuilt.Signature, Is.EqualTo(catalog.Signature));
                    Assert.That(rebuilt.Pairs, Is.EqualTo(catalog.Pairs));
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBiomeId>)catalog.Biomes)[0] = MoonpalaceBiomeId.MoonDough);
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<MoonpalaceBiomePairDefinition>)catalog.Definitions).Clear());
                    break;
                default:
                    Assert.Fail("Unexpected catalog contract case.");
                    break;
            }
        }
    }
}
