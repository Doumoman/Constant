#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.Rmap08
{
    [Category("RMAP08")]
    public sealed class RmapPortCatalogTests
    {
        [Test]
        public void A05_SpaceState_IsIndependentFromType0AndReservation()
        {
            RmapPortCatalogSnapshot catalog = RmapPortCatalog.BuildFixture();
            Assert.IsTrue(RmapPortCatalog.Validate(catalog).IsValid);
            Assert.AreEqual(10, catalog.Chunks.Count);

            RmapPortChunk type0Single = GetChunk(catalog, "T0_SINGLE_ENTRANCE");
            RmapPortChunk type0Breakable = GetChunk(catalog, "T0_BREAKABLE_SECRET");
            RmapPortChunk inactive = GetChunk(catalog, "INACTIVE_SOLID_WALL");
            RmapPortChunk reserved = GetChunk(catalog, "SPECIAL_RESERVED_START");
            Assert.AreEqual(RmapPortSpaceState.Active, type0Single.SpaceState);
            Assert.AreEqual(RmapPortChunkType.Type0, type0Single.ChunkType);
            Assert.AreEqual(1, type0Single.Ports.Count);
            Assert.AreEqual(RmapPortSpaceState.Secret, type0Breakable.SpaceState);
            Assert.AreEqual(RmapPortChunkType.Type0, type0Breakable.ChunkType);
            Assert.AreEqual(0, type0Breakable.Ports.Count);
            Assert.AreEqual(1, type0Breakable.BreakableAccesses.Count);
            Assert.AreEqual(RmapPortSpaceState.InactiveSolid, inactive.SpaceState);
            Assert.IsFalse(inactive.ChunkType.HasValue);
            Assert.AreEqual(RmapPortCatalog.ChunkCellCount, inactive.SolidCells.Count);
            Assert.AreEqual(0, inactive.Ports.Count);
            Assert.AreEqual(RmapPortSpaceState.SpecialReserved, reserved.SpaceState);
            Assert.IsFalse(reserved.ChunkType.HasValue);
            Assert.AreEqual("RESERVED_START_SLOT", reserved.ReservationId);
        }

        [Test]
        public void A16_Type1To4_AcceptOnlyDeclaredSideSetsAndType4RequiresBothVerticalSides()
        {
            foreach (RmapPortChunkType type in new[]
                     {
                         RmapPortChunkType.Type1, RmapPortChunkType.Type2,
                         RmapPortChunkType.Type3, RmapPortChunkType.Type4
                     })
            {
                for (var mask = 0; mask < 16; mask++)
                {
                    var sides = new List<RmapPortSide>();
                    foreach (RmapPortSide side in Enum.GetValues(typeof(RmapPortSide)))
                        if ((mask & (1 << (int)side)) != 0) sides.Add(side);
                    bool expected = ExpectedAllowed(type, mask);
                    Assert.AreEqual(expected, RmapPortCatalog.IsAllowedSideSet(type, sides),
                        type + " mask=" + mask);
                }
            }
        }

        [Test]
        public void A17_Type0_KeepsZeroBreakableAndSingleEntranceCasesSeparateFromInactiveSolid()
        {
            RmapPortCatalogSnapshot catalog = RmapPortCatalog.BuildFixture();
            RmapPortChunk breakable = GetChunk(catalog, "T0_BREAKABLE_SECRET");
            RmapPortChunk single = GetChunk(catalog, "T0_SINGLE_ENTRANCE");
            RmapBreakableAccess access = breakable.BreakableAccesses.Single();

            Assert.AreEqual(0, breakable.Ports.Count, "BreakableAccess is not promoted into an EdgePort.");
            Assert.AreEqual(1, single.Ports.Count);
            Assert.IsTrue(access.OpenCells.SequenceEqual(new[] { 2, 3 }));
            Assert.AreEqual("BREAKABLE_TOOL_REQUIRED", access.AccessCondition);
            Assert.AreEqual(1, catalog.Chunks.Count(value => value.SpaceState == RmapPortSpaceState.InactiveSolid));
        }

        [Test]
        public void A18_EdgePorts_RetainAllCoordinatesDirectionsAndRmap07OffsetProvenance()
        {
            RmapPortCatalogSnapshot catalog = RmapPortCatalog.BuildFixture();
            RmapPatternCatalogSnapshot patterns = RmapPatternCatalog.BuildInitialPool();
            foreach (RmapPortChunk chunk in catalog.Chunks.Where(value => value.SpaceState != RmapPortSpaceState.InactiveSolid))
            {
                Assert.AreEqual(4, chunk.SourcePlacement.OriginX);
                Assert.AreEqual(2, chunk.SourcePlacement.OriginY);
                Assert.IsTrue(patterns.TryGetCandidate(chunk.SourcePlacement.CandidateId, out _));
                foreach (RmapEdgePort port in chunk.Ports)
                {
                    Assert.IsNotEmpty(port.PortId);
                    Assert.IsNotEmpty(port.EntranceGroupId);
                    Assert.IsNotEmpty(port.OpenCells);
                    Assert.AreEqual(port.OpenCells.OrderBy(value => value), port.OpenCells);
                    foreach (int coordinate in port.OpenCells)
                    {
                        int maximum = port.Side == RmapPortSide.Left || port.Side == RmapPortSide.Right
                            ? RmapPortCatalog.ChunkHeight : RmapPortCatalog.ChunkWidth;
                        Assert.That(coordinate, Is.InRange(0, maximum - 1));
                        RmapPortCell edge = RmapPortCatalog.ToChunkCell(port.Side, coordinate);
                        Assert.IsFalse(chunk.IsSolid(edge.X, edge.Y),
                            chunk.ChunkId + "/" + port.PortId + " cannot claim a solid edge cell as open.");
                    }
                }
            }
        }

        [Test]
        public void A19_AdjacencyAndInteriorLinks_AreExplicitDirectionalRecordsWithCandidateBlockedAndUnknownStates()
        {
            RmapPortCatalogSnapshot catalog = RmapPortCatalog.BuildFixture();
            RmapPortAdjacency open = catalog.AdjacencyConnections.Single(value => value.ConnectionId == "ADJ_T1_A_TO_B");
            RmapPortAdjacency blocked = catalog.AdjacencyConnections.Single(value => value.ConnectionId == "ADJ_BLOCKED_INACTIVE");
            RmapPortAdjacency unknown = catalog.AdjacencyConnections.Single(value => value.ConnectionId == "ADJ_T4_CONTEXT_UNKNOWN");
            RmapPortInteriorLink drop = catalog.InteriorLinks.Single(value => value.ConnectionId == "INT_T2_LEFT_TO_DOWN");
            RmapPortInteriorLink climb = catalog.InteriorLinks.Single(value => value.ConnectionId == "INT_T3_LEFT_TO_UP");

            Assert.IsTrue(open.SharedCells.SequenceEqual(new[] { 1, 2 }));
            Assert.AreEqual(RmapPortEvidenceState.FixturePassExpected, open.EvidenceState);
            Assert.AreEqual(RmapPortEvidenceState.FixtureBlockedExpected, blocked.EvidenceState);
            Assert.AreEqual(string.Empty, blocked.ToPortId);
            Assert.AreEqual(RmapPortEvidenceState.ProfileContextUnknown, unknown.EvidenceState);
            Assert.IsTrue(drop.Required && climb.Required);
            Assert.AreEqual(RmapPortTraversalKind.Drop, drop.TraversalKind);
            Assert.AreEqual(RmapPortTraversalKind.Climb, climb.TraversalKind);
            Assert.IsFalse(catalog.InteriorLinks.Any(value => value.FromPortId == "P_T4_L_HIGH"),
                "Independent same-side entrances are not auto-connected by their common side.");
        }

        [Test]
        public void A20_Type2AndType3_UseExplicitOneWayInteriorFlowAndSharedTraversalProfile()
        {
            RmapPortCatalogSnapshot catalog = RmapPortCatalog.BuildFixture();
            GeneratedTraversalProfile profile = GeneratedTraversalProfileCatalog.Create();
            Assert.AreEqual(profile.Digest, catalog.ProfileDigest);
            foreach (RmapPortInteriorLink link in catalog.InteriorLinks.Where(value => value.Required))
            {
                Assert.AreEqual(profile.Digest, link.ProfileDigest);
                Assert.IsTrue(catalog.TryGetPort(link.ChunkId, link.FromPortId, out RmapEdgePort from));
                Assert.IsTrue(catalog.TryGetPort(link.ChunkId, link.ToPortId, out RmapEdgePort to));
                Assert.IsTrue(from.AllowsEntry);
                Assert.IsTrue(to.AllowsExit);
            }
            Assert.IsFalse(catalog.InteriorLinks.Any(value => value.ConnectionId == "INT_T2_DOWN_TO_LEFT"));
            Assert.IsFalse(catalog.InteriorLinks.Any(value => value.ConnectionId == "INT_T3_UP_TO_LEFT"));
        }

        [Test]
        public void CsvExports_AreStableAndPreserveAllRequiredPortFields()
        {
            RmapPortCatalogSnapshot first = RmapPortCatalog.BuildFixture();
            RmapPortCatalogSnapshot second = RmapPortCatalog.BuildFixture();
            string chunks = RmapPortCatalog.ExportChunksCsv(first);
            string ports = RmapPortCatalog.ExportPortsCsv(first);
            string adjacency = RmapPortCatalog.ExportAdjacencyCsv(first);
            string interior = RmapPortCatalog.ExportInteriorLinksCsv(first);
            string breakable = RmapPortCatalog.ExportBreakableAccessCsv(first);

            Assert.AreEqual(chunks, RmapPortCatalog.ExportChunksCsv(second));
            RmapPortCatalog.ValidateCsv(chunks, 9, 10);
            RmapPortCatalog.ValidateCsv(ports, 8, 14);
            RmapPortCatalog.ValidateCsv(adjacency, 8, 3);
            RmapPortCatalog.ValidateCsv(interior, 10, 3);
            RmapPortCatalog.ValidateCsv(breakable, 6, 1);
            StringAssert.Contains("OpenCells", ports);
            StringAssert.Contains("FlowDirection", ports);
            StringAssert.Contains("ProfileDigest", interior);
        }

        private static RmapPortChunk GetChunk(RmapPortCatalogSnapshot catalog, string id)
        {
            Assert.IsTrue(catalog.TryGetChunk(id, out RmapPortChunk chunk), id + " is required in the fixed port lab.");
            return chunk;
        }

        private static bool ExpectedAllowed(RmapPortChunkType type, int mask)
        {
            const int l = 1 << (int)RmapPortSide.Left;
            const int r = 1 << (int)RmapPortSide.Right;
            const int u = 1 << (int)RmapPortSide.Up;
            const int d = 1 << (int)RmapPortSide.Down;
            switch (type)
            {
                case RmapPortChunkType.Type1: return mask == (l | r);
                case RmapPortChunkType.Type2: return mask == (l | d) || mask == (r | d) || mask == (l | r | d);
                case RmapPortChunkType.Type3: return mask == (l | u) || mask == (r | u) || mask == (l | r | u);
                case RmapPortChunkType.Type4: return (mask & u) != 0 && (mask & d) != 0;
                default: return false;
            }
        }
    }
}
#endif
