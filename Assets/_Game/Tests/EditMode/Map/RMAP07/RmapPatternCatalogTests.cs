#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.Tests.EditMode.Rmap07
{
    [Category("RMAP07")]
    public sealed class RmapPatternCatalogTests
    {
        [Test]
        public void A01_A06_A08_InitialPool_Has48DistinctTyped16CellCandidatesAndAllRoles()
        {
            RmapPatternCatalogSnapshot pool = RmapPatternCatalog.BuildInitialPool();

            Assert.AreEqual(48, pool.Candidates.Count);
            Assert.GreaterOrEqual(pool.GeneratedDistinctBaseGeometryCount, pool.Candidates.Count);
            Assert.Greater(pool.RelationsCollapsedByBaseGeometry, 0);
            Assert.AreEqual(48, pool.Candidates.Select(value => value.CandidateId).Distinct().Count());
            Assert.IsTrue(pool.Candidates.All(value => value.BaseCells.Count == 16));
            Assert.IsTrue(pool.Candidates.All(value => value.BaseCells.All(cell => cell >= RmapPatternBaseCell.Air &&
                cell <= RmapPatternBaseCell.OneWayPlatform)));
            Assert.AreEqual(10, pool.Candidates.Select(value => value.PrimaryRole).Distinct().Count());
            Assert.AreEqual(1, pool.Candidates.Count(value => value.PrimaryRole == RmapPatternPrimaryRole.VoidClear));
        }

        [Test]
        public void A07_A09_FirstPool_PreservesAllIntentTagsWithoutUsingThemAsClassificationInput()
        {
            RmapPatternCatalogSnapshot pool = RmapPatternCatalog.BuildInitialPool();
            RmapPatternIntentTag all = pool.Candidates.Aggregate(RmapPatternIntentTag.None,
                (current, value) => current | value.IntentTags);
            RmapPatternIntentTag expected = Enum.GetValues(typeof(RmapPatternIntentTag)).Cast<RmapPatternIntentTag>()
                .Where(value => value != RmapPatternIntentTag.None)
                .Aggregate(RmapPatternIntentTag.None, (current, value) => current | value);

            Assert.AreEqual(expected, all);
            Assert.IsTrue(pool.Candidates.Any(value => value.TagConflicts.Contains(
                "REQUIRED_OK_VS_SECRET_SHELL_REVIEW_REQUIRED")));
            Assert.IsTrue(pool.Candidates.SelectMany(value => value.Origins).Any(value =>
                value.UnresolvedDirectionalTags.Count > 0),
                "Transforms keep an explicit review record rather than inventing missing reverse-direction tags.");
        }

        [Test]
        public void A10_Characteristics_KeepOneWaySeparateFromSolidAndAirAndUseFourNeighborAir()
        {
            RmapPatternCatalogSnapshot pool = RmapPatternCatalog.BuildInitialPool();
            RmapPatternCandidate oneWay = pool.Candidates.First(value =>
                value.Characteristics.OneWayCount > 0);
            RmapPatternAutomaticCharacteristics c = oneWay.Characteristics;
            RmapPatternAutomaticCharacteristics edgeTyped = RmapPatternCatalog.CalculateCharacteristics(
                RmapPatternCatalog.ParseBaseCells16("O" + new string('A', 15)));

            Assert.AreEqual(16, c.SolidCount + c.AirCount + c.OneWayCount);
            Assert.AreEqual(1d, c.SolidRatio + c.AirRatio + c.OneWayRatio, 0.000001d);
            Assert.IsTrue(edgeTyped.EdgeOpenCellSets.Values.SelectMany(value => value).Any(value =>
                value.Cell == RmapPatternBaseCell.OneWayPlatform));
            Assert.IsTrue(c.GrabCorners.All(value => oneWay.GetCell(value.X, value.Y) == RmapPatternBaseCell.Solid));
            Assert.IsTrue(c.ContextRequired.Contains("JUMP_ARC_AND_PLAYER_PROFILE_CONTEXT_REQUIRED"));
        }

        [Test]
        public void A11_A12_Transforms_UseLowerLeftCoordinatesReclassifyDirectionsAndDeduplicateByCells()
        {
            RmapPatternBaseCell[] riseRight = RmapPatternCatalog.ParseBaseCells16(
                "SSSS" + "ASSS" + "AASS" + "AAAS").ToArray();
            // Explicit y*4+x source: y0=SSSS, y1=ASSS, y2=AASS, y3=AAAS.
            Assert.AreEqual(RmapPatternPrimaryRole.SlopeRiseRight, RmapPatternCatalog.Classify(riseRight));
            var mirrored = RmapPatternCatalog.TransformCells(riseRight, RmapPatternTransform.MirrorX);
            Assert.AreEqual(RmapPatternPrimaryRole.SlopeRiseLeft, RmapPatternCatalog.Classify(mirrored));
            Assert.AreEqual(riseRight[0], mirrored[3]);
            Assert.AreEqual(riseRight[15], mirrored[12]);

            var ceiling = RmapPatternCatalog.ParseBaseCells16("AAAA" + "AAAA" + "AAAA" + "SSSS");
            Assert.AreEqual(RmapPatternPrimaryRole.CeilingFlat, RmapPatternCatalog.Classify(ceiling));
            Assert.AreEqual(RmapPatternPrimaryRole.StandableLedge,
                RmapPatternCatalog.Classify(RmapPatternCatalog.TransformCells(ceiling, RmapPatternTransform.R180)));
        }

        [Test]
        public void A15_Csv_RoundTripsStableIdsAndValidatesEvery16CellRow()
        {
            RmapPatternCatalogSnapshot first = RmapPatternCatalog.BuildInitialPool();
            RmapPatternCatalogSnapshot second = RmapPatternCatalog.BuildInitialPool();
            string csv = RmapPatternCatalog.ExportCatalogCsv(first);

            RmapPatternCatalog.ValidateCatalogCsv(csv, 48);
            CollectionAssert.AreEqual(first.Candidates.Select(value => value.CandidateId),
                second.Candidates.Select(value => value.CandidateId));
            Assert.IsTrue(first.Candidates.SelectMany(value => value.Origins).Any(value =>
                value.SourcePatternId.StartsWith("VIS01_MP_", StringComparison.Ordinal)));
            Assert.IsTrue(first.Candidates.Select(value => value.Characteristics.OneWayCount).Any(value => value > 0));
        }
    }
}
#endif
