using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class BiomePatchModelsTests
    {
        [TestCase("A")]
        [TestCase("PATCH_01")]
        [TestCase("0")]
        [TestCase("CORE_PATCH")]
        [TestCase("ABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789")]
        [TestCase("BIOME_4_CORE_0")]
        public void PatchId_AcceptsExactAsciiGrammar(string text)
        {
            var id = new BiomePatchId(text);
            Assert.That(id.IsValid, Is.True);
            Assert.That(id.Value, Is.EqualTo(text));
            Assert.That(id.ToString(), Is.EqualTo(text));
            Assert.That(BiomePatchId.TryCreate(text, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(id));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("patch")]
        [TestCase("Patch")]
        [TestCase("PATCH-1")]
        [TestCase("PATCH 1")]
        [TestCase("PATCH.1")]
        [TestCase("PATCH/1")]
        [TestCase("PATCH\t1")]
        [TestCase("PÄTCH")]
        [TestCase("패치")]
        [TestCase("ＰＡＴＣＨ")]
        public void PatchId_RejectsEveryNonCanonicalForm(string text)
        {
            Assert.That(BiomePatchId.TryCreate(text, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            Assert.That(() => new BiomePatchId(text), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void PatchId_DefaultIsInvalid()
        {
            var id = default(BiomePatchId);
            Assert.That(id.IsValid, Is.False);
            Assert.That(id.Value, Is.Empty);
            Assert.That(id.ToString(), Is.Empty);
        }

        [Test]
        public void PatchId_UsesOrdinalValueSemanticsAndDeterministicHash()
        {
            var first = new BiomePatchId("PATCH_A");
            var same = new BiomePatchId("PATCH_A");
            var later = new BiomePatchId("PATCH_B");
            Assert.That(first, Is.EqualTo(same));
            Assert.That(first == same, Is.True);
            Assert.That(first != later, Is.True);
            Assert.That(first.CompareTo(later), Is.LessThan(0));
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first.GetHashCode(), Is.EqualTo(588503385));
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void PatchId_IsCultureInvariant(string cultureName)
        {
            WithCulture(cultureName, () =>
            {
                var a = new BiomePatchId("PATCH_I");
                var b = new BiomePatchId("PATCH_I");
                Assert.That(a.CompareTo(b), Is.Zero);
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            });
        }

        [Test]
        public void Role_HasExactOrderedValues()
        {
            Assert.That(
                Enum.GetValues(typeof(BiomePatchRole)).Cast<BiomePatchRole>().Select(value => (int)value),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(Enum.GetNames(typeof(BiomePatchRole)), Is.EqualTo(new[] { "Core", "Satellite", "Intrusion" }));
        }

        [TestCase("CORE", BiomePatchRole.Core)]
        [TestCase("SATELLITE", BiomePatchRole.Satellite)]
        [TestCase("INTRUSION", BiomePatchRole.Intrusion)]
        public void Role_TryParseUsesExactTokens(string token, BiomePatchRole expected)
        {
            Assert.That(BiomePatchRoleTokenCodec.TryParse(token, out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(BiomePatchRole.Core, "CORE")]
        [TestCase(BiomePatchRole.Satellite, "SATELLITE")]
        [TestCase(BiomePatchRole.Intrusion, "INTRUSION")]
        public void Role_ToTokenUsesExactTokens(BiomePatchRole role, string expected)
        {
            Assert.That(BiomePatchRoleTokenCodec.ToToken(role), Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("core")]
        [TestCase("Core")]
        [TestCase("CORE ")]
        [TestCase(" SATELLITE")]
        [TestCase("0")]
        [TestCase("3")]
        public void Role_TryParseRejectsNearMisses(string token)
        {
            Assert.That(BiomePatchRoleTokenCodec.TryParse(token, out _), Is.False);
        }

        [Test]
        public void Role_ToTokenRejectsUndefinedValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BiomePatchRoleTokenCodec.ToToken((BiomePatchRole)3));
        }

        [TestCase(BiomePatchRole.Core)]
        [TestCase(BiomePatchRole.Satellite)]
        [TestCase(BiomePatchRole.Intrusion)]
        public void Seed_AcceptsExactRoleSourceContract(BiomePatchRole role)
        {
            SiteReservationId? source = role == BiomePatchRole.Core ? Site("RSV_A") : (SiteReservationId?)null;
            var seed = new BiomePatchSeed(14, Coord(14), role, source);
            Assert.That(seed.SectorIndex, Is.EqualTo(14));
            Assert.That(seed.Sector, Is.EqualTo(Coord(14)));
            Assert.That(seed.Role, Is.EqualTo(role));
            Assert.That(seed.SourceSiteReservationId, Is.EqualTo(source));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void Seed_RejectsInvalidIdentityRoleAndSource(int caseId)
        {
            Assert.That(() => CreateInvalidSeed(caseId), Throws.Exception);
        }

        [TestCase(0)]
        [TestCase(168)]
        public void Ownership_UnassignedHasExactNeutralState(int index)
        {
            var row = BiomeSectorOwnership.CreateUnassigned(index, Coord(index));
            Assert.That(row.SectorIndex, Is.EqualTo(index));
            Assert.That(row.Sector, Is.EqualTo(Coord(index)));
            Assert.That(row.IsAssigned, Is.False);
            Assert.That(row.PrimaryBiomeId, Is.Empty);
            Assert.That(row.SecondaryBiomeId, Is.Empty);
            Assert.That(row.PatchId, Is.Null);
        }

        [TestCase(0, "")]
        [TestCase(14, "BIOME_B")]
        [TestCase(168, "BIOME_Z")]
        public void Ownership_AssignedPreservesCanonicalState(int index, string secondary)
        {
            var row = new BiomeSectorOwnership(index, Coord(index), "BIOME_A", secondary, PatchId("PATCH_A"));
            Assert.That(row.IsAssigned, Is.True);
            Assert.That(row.PrimaryBiomeId, Is.EqualTo("BIOME_A"));
            Assert.That(row.SecondaryBiomeId, Is.EqualTo(secondary));
            Assert.That(row.PatchId, Is.EqualTo(PatchId("PATCH_A")));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void Ownership_RejectsInvalidAndHalfStateInputs(int caseId)
        {
            Assert.That(() => CreateInvalidOwnership(caseId), Throws.Exception);
        }

        [Test]
        public void SiteBinding_SortsCopiesAndExposesReadOnlyIndices()
        {
            var input = new List<int> { 14, 0, 7 };
            var binding = new BiomePatchSiteBinding(Site("RSV_A"), PatchId("PATCH_A"), "BIOME_A", input);
            input.Clear();
            Assert.That(binding.OccupiedSectorIndices, Is.EqualTo(new[] { 0, 7, 14 }));
            AssertReadOnly(binding.OccupiedSectorIndices);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void SiteBinding_RejectsInvalidInputs(int caseId)
        {
            Assert.That(() => CreateInvalidBinding(caseId), Throws.Exception);
        }

        [TestCase(BiomePatchRole.Core)]
        [TestCase(BiomePatchRole.Satellite)]
        [TestCase(BiomePatchRole.Intrusion)]
        public void Patch_SortsCopiesAndContainsOwnedSectors(BiomePatchRole role)
        {
            var cells = new List<int> { 15, 14 };
            var seeds = new List<BiomePatchSeed> { Seed(14, role, role == BiomePatchRole.Core ? "RSV_A" : null) };
            var patch = new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", role, seeds, cells);
            cells.Clear();
            seeds.Clear();
            Assert.That(patch.SectorIndices, Is.EqualTo(new[] { 14, 15 }));
            Assert.That(patch.Seeds.Select(value => value.SectorIndex), Is.EqualTo(new[] { 14 }));
            Assert.That(patch.SectorCount, Is.EqualTo(2));
            Assert.That(patch.ContainsSector(14), Is.True);
            Assert.That(patch.ContainsSector(13), Is.False);
            AssertReadOnly(patch.SectorsForReadOnlyTest());
            AssertReadOnly(patch.Seeds);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void Patch_RejectsInvalidInputs(int caseId)
        {
            Assert.That(() => CreateInvalidPatch(caseId), Throws.Exception);
        }

        [Test]
        public void Snapshot_AllUnassignedIsValidPartialArtifact()
        {
            var snapshot = new BiomePatchSnapshot(9, Array.Empty<BiomePatch>(), UnassignedRows(), Array.Empty<BiomePatchSiteBinding>());
            Assert.That(snapshot.Seed, Is.EqualTo(9));
            Assert.That(snapshot.Patches, Is.Empty);
            Assert.That(snapshot.SiteBindings, Is.Empty);
            Assert.That(snapshot.AssignedSectorCount, Is.Zero);
            Assert.That(snapshot.UnassignedSectorCount, Is.EqualTo(169));
            Assert.That(snapshot.IsComplete, Is.False);
        }

        [Test]
        public void Snapshot_CoreBindingIsExactBidirectionalGraph()
        {
            var graph = CoreGraph.Create("PATCH_A", "RSV_A", "BIOME_A", 0, 1);
            var snapshot = new BiomePatchSnapshot(10, new[] { graph.Patch }, Rows(graph.Patch), new[] { graph.Binding });
            Assert.That(snapshot.AssignedSectorCount, Is.EqualTo(2));
            Assert.That(snapshot.UnassignedSectorCount, Is.EqualTo(167));
            Assert.That(snapshot.IsComplete, Is.False);
            Assert.That(snapshot.Patches[0], Is.SameAs(graph.Patch));
            Assert.That(snapshot.SiteBindings[0], Is.SameAs(graph.Binding));
        }

        [Test]
        public void Snapshot_FullyAssignedIsComplete()
        {
            var indices = Enumerable.Range(0, 169).ToArray();
            var patch = Patch("PATCH_ALL", "BIOME_A", BiomePatchRole.Satellite, new[] { Seed(0, BiomePatchRole.Satellite) }, indices);
            var snapshot = new BiomePatchSnapshot(11, new[] { patch }, Rows(patch), Array.Empty<BiomePatchSiteBinding>());
            Assert.That(snapshot.AssignedSectorCount, Is.EqualTo(169));
            Assert.That(snapshot.UnassignedSectorCount, Is.Zero);
            Assert.That(snapshot.IsComplete, Is.True);
        }

        [Test]
        public void Snapshot_SecondaryBiomeDoesNotChangeMembership()
        {
            var patch = Patch("PATCH_A", "BIOME_A", BiomePatchRole.Satellite, new[] { Seed(0, BiomePatchRole.Satellite) }, new[] { 0 });
            var rows = Rows(patch);
            rows[0] = new BiomeSectorOwnership(0, Coord(0), "BIOME_A", "BIOME_B", patch.Id);
            var snapshot = new BiomePatchSnapshot(0, new[] { patch }, rows, Array.Empty<BiomePatchSiteBinding>());
            Assert.That(snapshot.GetSector(0).SecondaryBiomeId, Is.EqualTo("BIOME_B"));
            Assert.That(snapshot.Patches[0].SectorIndices, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Snapshot_LookupsReturnExactObjectsAndRejectMisses()
        {
            var graph = CoreGraph.Create("PATCH_A", "RSV_A", "BIOME_A", 14);
            var snapshot = new BiomePatchSnapshot(0, new[] { graph.Patch }, Rows(graph.Patch), new[] { graph.Binding });
            Assert.That(snapshot.GetSector(14).Sector, Is.EqualTo(Coord(14)));
            Assert.That(snapshot.TryGetSector(Coord(14), out var row), Is.True);
            Assert.That(row, Is.SameAs(snapshot.GetSector(14)));
            Assert.That(snapshot.TryGetSector(new SectorCoord(-1, 0), out var missingRow), Is.False);
            Assert.That(missingRow, Is.Null);
            Assert.That(snapshot.TryGetPatch(graph.Patch.Id, out var foundPatch), Is.True);
            Assert.That(foundPatch, Is.SameAs(graph.Patch));
            Assert.That(snapshot.TryGetPatch(PatchId("MISSING"), out _), Is.False);
            Assert.That(snapshot.TryGetSiteBinding(graph.Binding.SiteReservationId, out var foundBinding), Is.True);
            Assert.That(foundBinding, Is.SameAs(graph.Binding));
            Assert.That(snapshot.TryGetSiteBinding(Site("MISSING"), out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetSector(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetSector(169));
        }

        [TestCase(168)]
        [TestCase(170)]
        public void Snapshot_RequiresExactly169Rows(int count)
        {
            var rows = UnassignedRows();
            if (count < rows.Count) rows.RemoveAt(rows.Count - 1);
            else rows.Add(rows[0]);
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, Array.Empty<BiomePatch>(), rows, Array.Empty<BiomePatchSiteBinding>()));
        }

        [Test]
        public void Snapshot_RejectsNullAndDuplicateRows()
        {
            var nullRows = UnassignedRows();
            nullRows[0] = null;
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, Array.Empty<BiomePatch>(), nullRows, Array.Empty<BiomePatchSiteBinding>()));
            var duplicate = UnassignedRows();
            duplicate[168] = duplicate[0];
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, Array.Empty<BiomePatch>(), duplicate, Array.Empty<BiomePatchSiteBinding>()));
        }

        [Test]
        public void Snapshot_RejectsNullCollectionsAndElements()
        {
            var rows = UnassignedRows();
            Assert.Throws<ArgumentNullException>(() => new BiomePatchSnapshot(0, null, rows, Array.Empty<BiomePatchSiteBinding>()));
            Assert.Throws<ArgumentNullException>(() => new BiomePatchSnapshot(0, Array.Empty<BiomePatch>(), null, Array.Empty<BiomePatchSiteBinding>()));
            Assert.Throws<ArgumentNullException>(() => new BiomePatchSnapshot(0, Array.Empty<BiomePatch>(), rows, null));
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new BiomePatch[] { null }, rows, Array.Empty<BiomePatchSiteBinding>()));
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, Array.Empty<BiomePatch>(), rows, new BiomePatchSiteBinding[] { null }));
        }

        [Test]
        public void Snapshot_RejectsDuplicatePatchAndBindingIds()
        {
            var a = Patch("PATCH_A", "BIOME_A", BiomePatchRole.Satellite, new[] { Seed(0, BiomePatchRole.Satellite) }, new[] { 0 });
            var duplicate = Patch("PATCH_A", "BIOME_A", BiomePatchRole.Satellite, new[] { Seed(1, BiomePatchRole.Satellite) }, new[] { 1 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { a, duplicate }, Rows(a), Array.Empty<BiomePatchSiteBinding>()));

            var graph = CoreGraph.Create("PATCH_C", "RSV_A", "BIOME_A", 2);
            var duplicateBinding = new BiomePatchSiteBinding(Site("RSV_A"), graph.Patch.Id, "BIOME_A", new[] { 2 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { graph.Patch }, Rows(graph.Patch), new[] { graph.Binding, duplicateBinding }));
        }

        [Test]
        public void Snapshot_RejectsOrphanAndWrongOwnership()
        {
            var orphanRows = UnassignedRows();
            orphanRows[0] = new BiomeSectorOwnership(0, Coord(0), "BIOME_A", "", PatchId("MISSING"));
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, Array.Empty<BiomePatch>(), orphanRows, Array.Empty<BiomePatchSiteBinding>()));

            var patch = Patch("PATCH_A", "BIOME_A", BiomePatchRole.Satellite, new[] { Seed(0, BiomePatchRole.Satellite) }, new[] { 0 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { patch }, UnassignedRows(), Array.Empty<BiomePatchSiteBinding>()));

            var wrongBiomeRows = Rows(patch);
            wrongBiomeRows[0] = new BiomeSectorOwnership(0, Coord(0), "BIOME_B", "", patch.Id);
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { patch }, wrongBiomeRows, Array.Empty<BiomePatchSiteBinding>()));
        }

        [Test]
        public void Snapshot_RejectsWrongPatchAndOverlap()
        {
            var a = Patch("PATCH_A", "BIOME_A", BiomePatchRole.Satellite, new[] { Seed(0, BiomePatchRole.Satellite) }, new[] { 0 });
            var b = Patch("PATCH_B", "BIOME_B", BiomePatchRole.Intrusion, new[] { Seed(1, BiomePatchRole.Intrusion) }, new[] { 1 });
            var swapped = UnassignedRows();
            swapped[0] = new BiomeSectorOwnership(0, Coord(0), "BIOME_B", "", b.Id);
            swapped[1] = new BiomeSectorOwnership(1, Coord(1), "BIOME_A", "", a.Id);
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { a, b }, swapped, Array.Empty<BiomePatchSiteBinding>()));

            var overlap = Patch("PATCH_C", "BIOME_C", BiomePatchRole.Satellite, new[] { Seed(0, BiomePatchRole.Satellite) }, new[] { 0 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { a, overlap }, Rows(a), Array.Empty<BiomePatchSiteBinding>()));
        }

        [Test]
        public void Snapshot_RejectsNonCoreWrongBiomeAndMissingBindingPatch()
        {
            var satellite = Patch("PATCH_S", "BIOME_A", BiomePatchRole.Satellite, new[] { Seed(0, BiomePatchRole.Satellite) }, new[] { 0 });
            var nonCoreBinding = new BiomePatchSiteBinding(Site("RSV_A"), satellite.Id, "BIOME_A", new[] { 0 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { satellite }, Rows(satellite), new[] { nonCoreBinding }));

            var graph = CoreGraph.Create("PATCH_C", "RSV_C", "BIOME_A", 1);
            var wrongBiome = new BiomePatchSiteBinding(graph.Binding.SiteReservationId, graph.Patch.Id, "BIOME_B", new[] { 1 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { graph.Patch }, Rows(graph.Patch), new[] { wrongBiome }));

            var missingPatch = new BiomePatchSiteBinding(Site("RSV_M"), PatchId("MISSING"), "BIOME_A", new[] { 1 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { graph.Patch }, Rows(graph.Patch), new[] { missingPatch }));
        }

        [Test]
        public void Snapshot_RejectsMissingSeedAndOrphanCoreSeed()
        {
            var graph = CoreGraph.Create("PATCH_C", "RSV_C", "BIOME_A", 0, 1);
            var incompleteBinding = new BiomePatchSiteBinding(graph.Binding.SiteReservationId, graph.Patch.Id, "BIOME_A", new[] { 1 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { graph.Patch }, Rows(graph.Patch), new[] { incompleteBinding }));
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { graph.Patch }, Rows(graph.Patch), Array.Empty<BiomePatchSiteBinding>()));

            var oneSeedPatch = Patch("PATCH_ONE", "BIOME_A", BiomePatchRole.Core, new[] { Seed(0, BiomePatchRole.Core, "RSV_A") }, new[] { 0, 1 });
            var asksForTwo = new BiomePatchSiteBinding(Site("RSV_A"), oneSeedPatch.Id, "BIOME_A", new[] { 0, 1 });
            Assert.Throws<ArgumentException>(() => new BiomePatchSnapshot(0, new[] { oneSeedPatch }, Rows(oneSeedPatch), new[] { asksForTwo }));
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void Snapshot_OrderAndIdentityIgnoreCallerOrderCultureAndMutation(string cultureName)
        {
            WithCulture(cultureName, () =>
            {
                var first = CoreGraph.Create("PATCH_A", "RSV_A", "BIOME_A", 0);
                var second = CoreGraph.Create("PATCH_B", "RSV_B", "BIOME_B", 168);
                var patches = new List<BiomePatch> { second.Patch, first.Patch };
                var bindings = new List<BiomePatchSiteBinding> { second.Binding, first.Binding };
                var rows = Rows(first.Patch, second.Patch);
                rows.Reverse();
                var snapshot = new BiomePatchSnapshot(77, patches, rows, bindings);
                var signature = Signature(snapshot);
                patches.Clear();
                bindings.Clear();
                rows.Clear();
                Assert.That(snapshot.Patches.Select(value => value.Id.Value), Is.EqualTo(new[] { "PATCH_A", "PATCH_B" }));
                Assert.That(snapshot.SiteBindings.Select(value => value.SiteReservationId.Value), Is.EqualTo(new[] { "RSV_A", "RSV_B" }));
                Assert.That(snapshot.Sectors.Select(value => value.SectorIndex), Is.EqualTo(Enumerable.Range(0, 169)));
                Assert.That(Signature(snapshot), Is.EqualTo(signature));
                AssertReadOnly(snapshot.Patches);
                AssertReadOnly(snapshot.Sectors);
                AssertReadOnly(snapshot.SiteBindings);
            });
        }

        [Test]
        public void RuntimeModelsExposeNoMutationOrForbiddenDependencySurface()
        {
            var types = new[]
            {
                typeof(BiomePatchId), typeof(BiomePatchRoleTokenCodec), typeof(BiomePatchSeed),
                typeof(BiomeSectorOwnership), typeof(BiomePatchSiteBinding), typeof(BiomePatch),
                typeof(BiomePatchSnapshot)
            };
            foreach (var type in types)
            {
                if (type.IsClass && !(type.IsAbstract && type.IsSealed))
                    Assert.That(type.IsSealed, Is.True, type.FullName);
                if (type != typeof(BiomePatchId) && !(type.IsAbstract && type.IsSealed))
                    Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty, type.FullName);
                Assert.That(type.GetProperties(BindingFlags.Instance | BindingFlags.Public).All(property => property.SetMethod == null), Is.True, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);

                var surface = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(member => member.ToString()).ToArray();
                Assert.That(surface.Any(value => value.Contains("UnityEditor") || value.Contains("UnityEngine.Object") ||
                    value.Contains("System.IO") || value.Contains("Random") || value.Contains("DateTime")), Is.False, type.FullName);
            }
        }

        private static BiomePatchId PatchId(string value) => new BiomePatchId(value);
        private static SiteReservationId Site(string value) => new SiteReservationId(value);
        private static SectorCoord Coord(int index) => WorldGridIndex.ToCoordinate(index);

        private static BiomePatchSeed Seed(int index, BiomePatchRole role, string siteId = null)
        {
            return new BiomePatchSeed(index, Coord(index), role, siteId == null ? (SiteReservationId?)null : Site(siteId));
        }

        private static BiomePatch Patch(
            string id,
            string biome,
            BiomePatchRole role,
            IEnumerable<BiomePatchSeed> seeds,
            IEnumerable<int> sectors)
        {
            return new BiomePatch(PatchId(id), biome, "RULE_A", role, seeds, sectors);
        }

        private static List<BiomeSectorOwnership> UnassignedRows()
        {
            return Enumerable.Range(0, 169)
                .Select(index => BiomeSectorOwnership.CreateUnassigned(index, Coord(index)))
                .ToList();
        }

        private static List<BiomeSectorOwnership> Rows(params BiomePatch[] patches)
        {
            var rows = UnassignedRows();
            foreach (var patch in patches)
                foreach (var index in patch.SectorIndices)
                    rows[index] = new BiomeSectorOwnership(index, Coord(index), patch.BiomeId, string.Empty, patch.Id);
            return rows;
        }

        private static BiomePatchSeed CreateInvalidSeed(int caseId)
        {
            switch (caseId)
            {
                case 0: return new BiomePatchSeed(-1, new SectorCoord(0, 0), BiomePatchRole.Satellite, null);
                case 1: return new BiomePatchSeed(169, new SectorCoord(0, 0), BiomePatchRole.Satellite, null);
                case 2: return new BiomePatchSeed(0, new SectorCoord(1, 0), BiomePatchRole.Satellite, null);
                case 3: return new BiomePatchSeed(0, Coord(0), BiomePatchRole.Core, null);
                case 4: return new BiomePatchSeed(0, Coord(0), BiomePatchRole.Core, default(SiteReservationId));
                case 5: return new BiomePatchSeed(0, Coord(0), BiomePatchRole.Satellite, Site("RSV_A"));
                case 6: return new BiomePatchSeed(0, Coord(0), BiomePatchRole.Intrusion, Site("RSV_A"));
                default: return new BiomePatchSeed(0, Coord(0), (BiomePatchRole)3, null);
            }
        }

        private static BiomeSectorOwnership CreateInvalidOwnership(int caseId)
        {
            switch (caseId)
            {
                case 0: return new BiomeSectorOwnership(-1, new SectorCoord(0, 0), "BIOME_A", "", PatchId("PATCH_A"));
                case 1: return new BiomeSectorOwnership(169, new SectorCoord(0, 0), "BIOME_A", "", PatchId("PATCH_A"));
                case 2: return new BiomeSectorOwnership(0, new SectorCoord(1, 0), "BIOME_A", "", PatchId("PATCH_A"));
                case 3: return new BiomeSectorOwnership(0, Coord(0), null, "", PatchId("PATCH_A"));
                case 4: return new BiomeSectorOwnership(0, Coord(0), "", "", PatchId("PATCH_A"));
                case 5: return new BiomeSectorOwnership(0, Coord(0), "biome", "", PatchId("PATCH_A"));
                case 6: return new BiomeSectorOwnership(0, Coord(0), "BIOME_A", null, PatchId("PATCH_A"));
                case 7: return new BiomeSectorOwnership(0, Coord(0), "BIOME_A", "biome_b", PatchId("PATCH_A"));
                case 8: return new BiomeSectorOwnership(0, Coord(0), "BIOME_A", "BIOME_A", PatchId("PATCH_A"));
                default: return new BiomeSectorOwnership(0, Coord(0), "BIOME_A", "", default(BiomePatchId));
            }
        }

        private static BiomePatchSiteBinding CreateInvalidBinding(int caseId)
        {
            switch (caseId)
            {
                case 0: return new BiomePatchSiteBinding(default(SiteReservationId), PatchId("PATCH_A"), "BIOME_A", new[] { 0 });
                case 1: return new BiomePatchSiteBinding(Site("RSV_A"), default(BiomePatchId), "BIOME_A", new[] { 0 });
                case 2: return new BiomePatchSiteBinding(Site("RSV_A"), PatchId("PATCH_A"), "", new[] { 0 });
                case 3: return new BiomePatchSiteBinding(Site("RSV_A"), PatchId("PATCH_A"), "BIOME_A", null);
                case 4: return new BiomePatchSiteBinding(Site("RSV_A"), PatchId("PATCH_A"), "BIOME_A", Array.Empty<int>());
                case 5: return new BiomePatchSiteBinding(Site("RSV_A"), PatchId("PATCH_A"), "BIOME_A", new[] { -1 });
                case 6: return new BiomePatchSiteBinding(Site("RSV_A"), PatchId("PATCH_A"), "BIOME_A", new[] { 169 });
                default: return new BiomePatchSiteBinding(Site("RSV_A"), PatchId("PATCH_A"), "BIOME_A", new[] { 0, 0 });
            }
        }

        private static BiomePatch CreateInvalidPatch(int caseId)
        {
            var satellite = Seed(0, BiomePatchRole.Satellite);
            switch (caseId)
            {
                case 0: return new BiomePatch(default(BiomePatchId), "BIOME_A", "RULE_A", BiomePatchRole.Satellite, new[] { satellite }, new[] { 0 });
                case 1: return new BiomePatch(PatchId("PATCH_A"), "", "RULE_A", BiomePatchRole.Satellite, new[] { satellite }, new[] { 0 });
                case 2: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "rule", BiomePatchRole.Satellite, new[] { satellite }, new[] { 0 });
                case 3: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", (BiomePatchRole)3, new[] { satellite }, new[] { 0 });
                case 4: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", BiomePatchRole.Satellite, null, new[] { 0 });
                case 5: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", BiomePatchRole.Satellite, new[] { satellite }, null);
                case 6: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", BiomePatchRole.Satellite, Array.Empty<BiomePatchSeed>(), new[] { 0 });
                case 7: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", BiomePatchRole.Satellite, new[] { satellite }, new[] { 0, 0 });
                case 8: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", BiomePatchRole.Satellite, new BiomePatchSeed[] { null }, new[] { 0 });
                case 9: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", BiomePatchRole.Satellite, new[] { satellite, satellite }, new[] { 0 });
                case 10: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", BiomePatchRole.Core, new[] { satellite }, new[] { 0 });
                default: return new BiomePatch(PatchId("PATCH_A"), "BIOME_A", "RULE_A", BiomePatchRole.Satellite, new[] { satellite }, new[] { 1 });
            }
        }

        private static string Signature(BiomePatchSnapshot snapshot)
        {
            return string.Join("|", snapshot.Patches.Select(value => value.Id.Value + ":" + string.Join(",", value.SectorIndices))) +
                   "#" + string.Join("|", snapshot.Sectors.Select(value => value.SectorIndex.ToString(CultureInfo.InvariantCulture) + ":" + value.PrimaryBiomeId + ":" + (value.PatchId.HasValue ? value.PatchId.Value.Value : ""))) +
                   "#" + string.Join("|", snapshot.SiteBindings.Select(value => value.SiteReservationId.Value + ":" + value.PatchId.Value));
        }

        private static void AssertReadOnly<T>(IReadOnlyList<T> values)
        {
            Assert.That(values, Is.InstanceOf<IList>());
            Assert.Throws<NotSupportedException>(() => ((IList)values).Add(default(T)));
        }

        private static void WithCulture(string name, Action action)
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
                action();
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        private sealed class CoreGraph
        {
            private CoreGraph(BiomePatch patch, BiomePatchSiteBinding binding)
            {
                Patch = patch;
                Binding = binding;
            }

            public BiomePatch Patch { get; }
            public BiomePatchSiteBinding Binding { get; }

            public static CoreGraph Create(string patchId, string siteId, string biomeId, params int[] indices)
            {
                var seeds = indices.Select(index => Seed(index, BiomePatchRole.Core, siteId)).ToArray();
                var patch = new BiomePatch(PatchId(patchId), biomeId, "RULE_A", BiomePatchRole.Core, seeds, indices.Reverse());
                var binding = new BiomePatchSiteBinding(Site(siteId), patch.Id, biomeId, indices.Reverse());
                return new CoreGraph(patch, binding);
            }
        }
    }

    internal static class BiomePatchTestExtensions
    {
        public static IReadOnlyList<int> SectorsForReadOnlyTest(this BiomePatch patch)
        {
            return patch.SectorIndices;
        }
    }
}
