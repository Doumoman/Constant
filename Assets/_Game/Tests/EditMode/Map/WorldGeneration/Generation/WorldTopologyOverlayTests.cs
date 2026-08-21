using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Generation
{
    public sealed class WorldTopologyOverlayTests
    {
        [Test]
        public void Snapshot_CreateRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => WorldTopologyOverlaySnapshot.Create(null));
        }

        [Test]
        public void Snapshot_PreservesExactSeedCountAndAscendingOrder()
        {
            var snapshot = CreateSnapshot(4660);

            Assert.That(snapshot.Seed, Is.EqualTo(4660UL));
            Assert.That(snapshot.Count, Is.EqualTo(WorldGenConstants.SectorCount));
            Assert.That(snapshot.Cells.Select(cell => cell.Index),
                Is.EqualTo(Enumerable.Range(0, WorldGenConstants.SectorCount)));
        }

        [Test]
        public void Snapshot_ProvidesStableIndexAndCoordinateLookups()
        {
            var snapshot = CreateSnapshot(7);
            var coordinate = new SectorCoord(6, 6);

            Assert.That(snapshot.GetCell(84), Is.SameAs(snapshot.GetCell(coordinate)));
            Assert.That(snapshot.TryGetCell(84, out var cell), Is.True);
            Assert.That(cell, Is.SameAs(snapshot.GetCell(84)));
        }

        [Test]
        public void Snapshot_All169CellsMatchIndependentRangesAndTopology()
        {
            var snapshot = CreateSnapshot(4660);

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var expectedX = index % WorldGenConstants.SectorColumns;
                var expectedY = index / WorldGenConstants.SectorColumns;
                var cell = snapshot.GetCell(index);

                Assert.That(cell.Index, Is.EqualTo(expectedY * 13 + expectedX), $"index {index}");
                Assert.That(cell.Coordinate, Is.EqualTo(new SectorCoord(expectedX, expectedY)), $"coordinate {index}");
                Assert.That(cell.WorldTileMinX, Is.EqualTo(expectedX * 48), $"min x {index}");
                Assert.That(cell.WorldTileMaxX, Is.EqualTo(expectedX * 48 + 47), $"max x {index}");
                Assert.That(cell.WorldTileMinY, Is.EqualTo(expectedY * 32), $"min y {index}");
                Assert.That(cell.WorldTileMaxY, Is.EqualTo(expectedY * 32 + 31), $"max y {index}");
                Assert.That(cell.LeftIndex, Is.EqualTo(expectedX == 0 ? -1 : index - 1), $"left {index}");
                Assert.That(cell.RightIndex, Is.EqualTo(expectedX == 12 ? -1 : index + 1), $"right {index}");
                Assert.That(cell.UpIndex, Is.EqualTo(expectedY == 12 ? -1 : index + 13), $"up {index}");
                Assert.That(cell.DownIndex, Is.EqualTo(expectedY == 0 ? -1 : index - 13), $"down {index}");
            }
        }

        [Test]
        public void Snapshot_All624DirectedLinksAreReciprocal()
        {
            var snapshot = CreateSnapshot(0);
            var directedLinks = 0;

            for (var index = 0; index < snapshot.Count; index++)
            {
                var cell = snapshot.GetCell(index);
                if (cell.LeftIndex >= 0)
                {
                    directedLinks++;
                    Assert.That(snapshot.GetCell(cell.LeftIndex).RightIndex, Is.EqualTo(index));
                }

                if (cell.RightIndex >= 0)
                {
                    directedLinks++;
                    Assert.That(snapshot.GetCell(cell.RightIndex).LeftIndex, Is.EqualTo(index));
                }

                if (cell.UpIndex >= 0)
                {
                    directedLinks++;
                    Assert.That(snapshot.GetCell(cell.UpIndex).DownIndex, Is.EqualTo(index));
                }

                if (cell.DownIndex >= 0)
                {
                    directedLinks++;
                    Assert.That(snapshot.GetCell(cell.DownIndex).UpIndex, Is.EqualTo(index));
                }
            }

            Assert.That(directedLinks, Is.EqualTo(624));
        }

        [Test]
        public void Snapshot_ExposesReadOnlyIndependentCollection()
        {
            var snapshot = CreateSnapshot(8);
            var collection = (ICollection<WorldTopologyOverlayCell>)snapshot.Cells;

            Assert.That(collection.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => collection.Add(snapshot.Cells[0]));
        }

        [TestCase(-1)]
        [TestCase(WorldGenConstants.SectorCount)]
        public void Snapshot_InvalidIndexLookupsDoNotResolve(int index)
        {
            var snapshot = CreateSnapshot(0);

            Assert.That(snapshot.TryGetCell(index, out var cell), Is.False);
            Assert.That(cell, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetCell(index));
        }

        [TestCase(GeneratedSectorRole.Unassigned, "UNASSIGNED", "U")]
        [TestCase(GeneratedSectorRole.Mandatory, "MANDATORY", "M")]
        [TestCase(GeneratedSectorRole.Type0, "TYPE0", "0")]
        [TestCase(GeneratedSectorRole.ReservedSite, "RESERVED_SITE", "S")]
        [TestCase(GeneratedSectorRole.InactiveBuffer, "INACTIVE_BUFFER", "X")]
        public void Cell_MapsRoleToExactIdentity(
            GeneratedSectorRole role,
            string token,
            string glyph)
        {
            var cell = CreateOverlayCell(0, role);

            Assert.That(cell.Role, Is.EqualTo(role));
            Assert.That(cell.RoleToken, Is.EqualTo(token));
            Assert.That(cell.RoleGlyph, Is.EqualTo(glyph));
        }

        [TestCase(GeneratedSectorRole.Unassigned, 96, 96, 96, 230)]
        [TestCase(GeneratedSectorRole.Mandatory, 20, 150, 220, 230)]
        [TestCase(GeneratedSectorRole.Type0, 60, 180, 90, 230)]
        [TestCase(GeneratedSectorRole.ReservedSite, 235, 135, 35, 230)]
        [TestCase(GeneratedSectorRole.InactiveBuffer, 35, 35, 35, 230)]
        public void Gui_MapsRoleToExactColor(
            GeneratedSectorRole role,
            int red,
            int green,
            int blue,
            int alpha)
        {
            Assert.That(
                WorldTopologyOverlayGui.GetRoleColor(role),
                Is.EqualTo(new Color32((byte)red, (byte)green, (byte)blue, (byte)alpha)));
        }

        [Test]
        public void Cell_RejectsUndefinedRole()
        {
            var source = CreateSourceCell(0, GeneratedSectorRole.Unassigned);
            var roleField = typeof(SectorCell).GetField(
                "<Role>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            roleField.SetValue(source, (GeneratedSectorRole)999);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new WorldTopologyOverlayCell(source, CreateNeighbors(0)));
        }

        [Test]
        public void Gui_RejectsUndefinedRole()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldTopologyOverlayGui.GetRoleColor((GeneratedSectorRole)999));
        }

        [TestCase(0, 0, 0, 0, 47, 0, 31, -1, 1, 13, -1)]
        [TestCase(12, 12, 0, 576, 623, 0, 31, 11, -1, 25, -1)]
        [TestCase(84, 6, 6, 288, 335, 192, 223, 83, 85, 97, 71)]
        [TestCase(156, 0, 12, 0, 47, 384, 415, -1, 157, -1, 143)]
        [TestCase(168, 12, 12, 576, 623, 384, 415, 167, -1, -1, 155)]
        public void Cell_PreservesExactRangesAndTopology(
            int index,
            int x,
            int y,
            int minX,
            int maxX,
            int minY,
            int maxY,
            int left,
            int right,
            int up,
            int down)
        {
            var cell = CreateSnapshot(0).GetCell(index);

            Assert.That(cell.Coordinate, Is.EqualTo(new SectorCoord(x, y)));
            Assert.That(cell.WorldTileMinX, Is.EqualTo(minX));
            Assert.That(cell.WorldTileMaxX, Is.EqualTo(maxX));
            Assert.That(cell.WorldTileMinY, Is.EqualTo(minY));
            Assert.That(cell.WorldTileMaxY, Is.EqualTo(maxY));
            Assert.That(cell.LeftIndex, Is.EqualTo(left));
            Assert.That(cell.RightIndex, Is.EqualTo(right));
            Assert.That(cell.UpIndex, Is.EqualTo(up));
            Assert.That(cell.DownIndex, Is.EqualTo(down));
        }

        [TestCase(0, "0,0\nU")]
        [TestCase(84, "6,6\nU")]
        [TestCase(168, "12,12\nU")]
        public void Cell_UsesExactLabelWithoutTrailingNewline(int index, string expected)
        {
            var label = CreateSnapshot(0).GetCell(index).CellLabel;

            Assert.That(label, Is.EqualTo(expected));
            Assert.That(label.EndsWith("\n", StringComparison.Ordinal), Is.False);
        }

        [TestCase(
            0,
            "Sector: SectorCoord(0, 0) / Index 0\n" +
            "World Tiles: X 0..47 / Y 0..31\n" +
            "Role: UNASSIGNED\n" +
            "Neighbors: L=-1 R=1 U=13 D=-1")]
        [TestCase(
            84,
            "Sector: SectorCoord(6, 6) / Index 84\n" +
            "World Tiles: X 288..335 / Y 192..223\n" +
            "Role: UNASSIGNED\n" +
            "Neighbors: L=83 R=85 U=97 D=71")]
        [TestCase(
            168,
            "Sector: SectorCoord(12, 12) / Index 168\n" +
            "World Tiles: X 576..623 / Y 384..415\n" +
            "Role: UNASSIGNED\n" +
            "Neighbors: L=167 R=-1 U=-1 D=155")]
        public void Cell_UsesExactTooltipWithoutTrailingNewline(int index, string expected)
        {
            var tooltip = CreateSnapshot(0).GetCell(index).Tooltip;

            Assert.That(tooltip, Is.EqualTo(expected));
            Assert.That(tooltip.EndsWith("\n", StringComparison.Ordinal), Is.False);
        }

        [Test]
        public void Cell_FormattingIsInvariantUnderNonEnglishCulture()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

                var cell = CreateOverlayCell(84, GeneratedSectorRole.Unassigned);

                Assert.That(cell.CellLabel, Is.EqualTo("6,6\nU"));
                Assert.That(cell.Tooltip, Is.EqualTo(
                    "Sector: SectorCoord(6, 6) / Index 84\n" +
                    "World Tiles: X 288..335 / Y 192..223\n" +
                    "Role: UNASSIGNED\n" +
                    "Neighbors: L=83 R=85 U=97 D=71"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [TestCase(0, 24, 428)]
        [TestCase(12, 408, 428)]
        [TestCase(84, 216, 236)]
        [TestCase(156, 24, 44)]
        [TestCase(168, 408, 44)]
        [TestCase(1, 56, 428)]
        [TestCase(13, 24, 396)]
        [TestCase(155, 408, 76)]
        [TestCase(167, 376, 44)]
        public void Gui_CellRectsUseFrozenOrientation(int index, float x, float y)
        {
            Assert.That(
                WorldTopologyOverlayGui.GetCellRect(index),
                Is.EqualTo(new Rect(x, y, 32, 32)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(84)]
        [TestCase(155)]
        [TestCase(156)]
        [TestCase(167)]
        [TestCase(168)]
        public void Gui_HitTestResolvesCellCenters(int index)
        {
            var center = WorldTopologyOverlayGui.GetCellRect(index).center;

            Assert.That(WorldTopologyOverlayGui.TryHitTest(center, out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(index));
        }

        [Test]
        public void Gui_All169RectsAndCentersMatchIndependentOrientation()
        {
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var x = index % 13;
                var y = index / 13;
                var expected = new Rect(24 + x * 32, 44 + (12 - y) * 32, 32, 32);

                Assert.That(WorldTopologyOverlayGui.GetCellRect(index), Is.EqualTo(expected), $"rect {index}");
                Assert.That(WorldTopologyOverlayGui.TryHitTest(expected.center, out var actual), Is.True, $"hit {index}");
                Assert.That(actual, Is.EqualTo(index), $"hit index {index}");
            }
        }

        [TestCase(24, 44, 156)]
        [TestCase(439.999f, 44, 168)]
        [TestCase(24, 459.999f, 0)]
        [TestCase(439.999f, 459.999f, 12)]
        public void Gui_HitTestIncludesGridLeftAndTop(float x, float y, int expected)
        {
            Assert.That(
                WorldTopologyOverlayGui.TryHitTest(new Vector2(x, y), out var actual),
                Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(23.999f, 44)]
        [TestCase(24, 43.999f)]
        [TestCase(440, 44)]
        [TestCase(24, 460)]
        [TestCase(0, 0)]
        [TestCase(452, 12)]
        [TestCase(24, 500)]
        [TestCase(-1, -1)]
        public void Gui_HitTestRejectsOutsideWithoutClamping(float x, float y)
        {
            Assert.That(
                WorldTopologyOverlayGui.TryHitTest(new Vector2(x, y), out var actual),
                Is.False);
            Assert.That(actual, Is.EqualTo(-1));
        }

        [Test]
        public void Gui_UsesExactFrozenDimensionsAndText()
        {
            Assert.That(WorldTopologyOverlayGui.PanelRect, Is.EqualTo(new Rect(12, 12, 440, 564)));
            Assert.That(WorldTopologyOverlayGui.GridRect, Is.EqualTo(new Rect(24, 44, 416, 416)));
            Assert.That(WorldTopologyOverlayGui.GridColumns, Is.EqualTo(13));
            Assert.That(WorldTopologyOverlayGui.GridRows, Is.EqualTo(13));
            Assert.That(WorldTopologyOverlayGui.LegendText,
                Is.EqualTo("U Unassigned | M Mandatory | 0 Type0 | S Reserved | X Inactive"));
            Assert.That(WorldTopologyOverlayGui.EmptyHoverText,
                Is.EqualTo("Hover a sector for details."));
            Assert.That(WorldTopologyOverlayGui.SmallViewportText,
                Is.EqualTo("World topology overlay requires 440 x 564 pixels."));
        }

        [Test]
        public void Gui_IsStaticAndHasNoMutableStaticFields()
        {
            var type = typeof(WorldTopologyOverlayGui);

            Assert.That(type.IsAbstract && type.IsSealed, Is.True);
            Assert.That(
                type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(field => !field.IsLiteral)
                    .ToArray(),
                Is.Empty);
            Assert.That(
                type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Count(method => method.Name == nameof(WorldTopologyOverlayGui.Draw)),
                Is.EqualTo(1));
        }

        [Test]
        public void P00PreviewContainsOnlyUnassignedGlyphs()
        {
            var snapshot = CreateSnapshot(4660);

            Assert.That(snapshot.Cells.All(cell => cell.Role == GeneratedSectorRole.Unassigned), Is.True);
            Assert.That(snapshot.Cells.All(cell => cell.RoleGlyph == "U"), Is.True);
            Assert.That(snapshot.Cells.All(cell => cell.RoleToken == "UNASSIGNED"), Is.True);
        }

        [Test]
        public void Component_HasExactAttributesAndPublicSurface()
        {
            var type = typeof(WorldTopologyOverlay);

            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetCustomAttribute<ExecuteAlways>(), Is.Not.Null);
            Assert.That(type.GetCustomAttribute<DisallowMultipleComponent>(), Is.Not.Null);
            Assert.That(type.GetCustomAttribute<AddComponentMenu>().componentMenu,
                Is.EqualTo("WorldGen/World Topology Overlay"));
            Assert.That(type.GetProperty(nameof(WorldTopologyOverlay.HasSnapshot)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(WorldTopologyOverlay.Snapshot)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(WorldTopologyOverlay.SetSnapshot)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(WorldTopologyOverlay.ClearSnapshot)), Is.Not.Null);
        }

        [Test]
        public void Component_InitialSetFailureAndClearAreTransactional()
        {
            var gameObject = new GameObject("WorldTopologyOverlayTests");
            try
            {
                var overlay = gameObject.AddComponent<WorldTopologyOverlay>();
                Assert.That(overlay.HasSnapshot, Is.False);
                Assert.That(overlay.Snapshot, Is.Null);

                overlay.SetSnapshot(new GridInitializationPass().Execute(123));
                var successful = overlay.Snapshot;
                Assert.That(overlay.HasSnapshot, Is.True);
                Assert.That(successful.Seed, Is.EqualTo(123UL));

                Assert.Throws<ArgumentNullException>(() => overlay.SetSnapshot(null));
                Assert.That(overlay.Snapshot, Is.SameAs(successful));

                overlay.ClearSnapshot();
                Assert.That(overlay.HasSnapshot, Is.False);
                Assert.That(overlay.Snapshot, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static WorldTopologyOverlaySnapshot CreateSnapshot(ulong seed)
        {
            return WorldTopologyOverlaySnapshot.Create(new GridInitializationPass().Execute(seed));
        }

        private static WorldTopologyOverlayCell CreateOverlayCell(
            int index,
            GeneratedSectorRole role)
        {
            return new WorldTopologyOverlayCell(
                CreateSourceCell(index, role),
                CreateNeighbors(index));
        }

        private static SectorCell CreateSourceCell(int index, GeneratedSectorRole role)
        {
            var coordinate = WorldGridIndex.ToCoordinate(index);
            return new SectorCell(
                index,
                coordinate,
                role,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                -1,
                false);
        }

        private static SectorNeighborIndices CreateNeighbors(int index)
        {
            return new SectorNeighborIndices(
                index,
                WorldGridIndex.GetLeftIndex(index),
                WorldGridIndex.GetRightIndex(index),
                WorldGridIndex.GetUpIndex(index),
                WorldGridIndex.GetDownIndex(index));
        }
    }
}
