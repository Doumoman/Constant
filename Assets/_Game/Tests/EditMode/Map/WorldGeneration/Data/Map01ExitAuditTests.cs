using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    [TestFixture]
    [Category("MAP01_17")]
    [Parallelizable(ParallelScope.None)]
    public sealed class Map01ExitAuditTests
    {
        private const string EditorNamespace = "StarNight.Map.Editor.WorldGeneration.Data.";
        private const string DictionaryFileName = "CSV_DATA_DICTIONARY.csv";
        private const string ReportProjectPath = "MapDesign/MCP/REPORTS/CsvImportReport.json";

        private static readonly FamilyCase[] Families =
        {
            new FamilyCase(
                "World", "world_profiles.csv", "world_profile_id",
                "WorldRouteDefinitions", "WorldProfiles", "WorldProfileId",
                new[] { "WORLD_MOONPALACE_V1" }),
            new FamilyCase(
                "Biome", "biome_types.csv", "biome_id",
                "BiomeBoundaryDefinitions", "BiomeTypes", "BiomeId",
                new[]
                {
                    "BIO_MOON_CRATER", "BIO_CASSIA_ROOT", "BIO_ABANDONED_MILL",
                    "BIO_MOON_DOUGH"
                }),
            new FamilyCase(
                "RouteMask", "sector_route_masks.csv", "route_mask_id",
                "WorldRouteDefinitions", "RouteMasks", "RouteMaskId",
                new[]
                {
                    "ROUTE_T0_NONE", "ROUTE_T0_L", "ROUTE_T0_R", "ROUTE_T0_U",
                    "ROUTE_T0_D", "ROUTE_T0_LU", "ROUTE_T0_LD", "ROUTE_T0_RU",
                    "ROUTE_T0_RD", "ROUTE_T0_UD", "ROUTE_T0_LUD", "ROUTE_T0_RUD",
                    "ROUTE_T1_LR", "ROUTE_T2_LRD", "ROUTE_T3_LRU"
                }),
            new FamilyCase(
                "Battery", "battery_profiles.csv", "battery_id",
                "MicrochunkPopulationItemDefinitions", "BatteryProfiles", "BatteryId",
                new[] { "BAT_MINI", "BAT_AIR_CANNON", "BAT_STANDARD", "BAT_MEGA", "BAT_GRENADE" })
        };

        private static object firstSession;
        private static object secondSession;
        private static PublishedStaticDataSnapshot firstSnapshot;
        private static PublishedStaticDataSnapshot secondSnapshot;
        private static byte[] persistedSecondReportBytes;
        private static byte[] originalReportBytes;
        private static bool originalReportExisted;
        private static PreservationSnapshot preservationBefore;
        private static PreservationSnapshot preservationAfter;

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string AuthoringRoot => Path.Combine(
            Application.dataPath, "_Game", "Map", "Data", "WorldGeneration", "Authoring");

        private static string ReportPath => Path.Combine(ProjectRoot, ReportProjectPath);

        public static IEnumerable<FamilyCase> FamilyCases => Families;

        public static IEnumerable<TestCaseData> RequiredIdCases
        {
            get
            {
                foreach (var family in Families)
                {
                    foreach (var id in family.RequiredIds)
                    {
                        yield return new TestCaseData(family, id)
                            .SetName("RequiredId_" + family.Name + "_" + id);
                    }
                }
            }
        }

        [OneTimeSetUp]
        public void ImportExactAuthoringTwice()
        {
            originalReportExisted = File.Exists(ReportPath);
            originalReportBytes = originalReportExisted ? File.ReadAllBytes(ReportPath) : null;
            preservationBefore = CapturePreservation();

            try
            {
                var store = new StaticDataRegistryStore();
                firstSession = ExecuteProduction(store);
                firstSnapshot = store.Current;
                secondSession = ExecuteProduction(store);
                secondSnapshot = store.Current;
                persistedSecondReportBytes = File.ReadAllBytes(ReportPath);
            }
            finally
            {
                preservationAfter = CapturePreservation();
                RestoreOriginalReport();
            }
        }

        [OneTimeTearDown]
        public void RestorePersistedReport()
        {
            RestoreOriginalReport();
        }

        [Test]
        public void SourceInventory_IsExactDictionaryPlusFortyNineAndAllHaveBom()
        {
            var expected = ExpectedFileNames();
            var paths = Directory.GetFiles(AuthoringRoot, "*.csv", SearchOption.AllDirectories);
            var actual = paths.Select(Path.GetFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray();

            Assert.That(expected.Count, Is.EqualTo(50));
            Assert.That(expected[0], Is.EqualTo(DictionaryFileName));
            Assert.That(expected.Skip(1), Is.EquivalentTo(ForeignKeySourceSet.ExpectedFileNames));
            Assert.That(actual, Is.EquivalentTo(expected));
            Assert.That(actual.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(50));
            Assert.That(paths.Count(HasUtf8Bom), Is.EqualTo(50));
        }

        [Test]
        public void ParsedSources_AreExactFortyNineAndEveryFileSucceeded()
        {
            var index = (ForeignKeyRecordIndex)Property(secondSession, "RecordIndex");
            var sourceFiles = index.Records.Select(identity => identity.FileName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var statuses = Items(Property(secondSession, "Files"));

            Assert.That(sourceFiles, Is.EquivalentTo(ForeignKeySourceSet.ExpectedFileNames));
            Assert.That(sourceFiles.Length, Is.EqualTo(49));
            Assert.That(statuses.Count, Is.EqualTo(50));
            Assert.That(statuses.Select(status => Property(status, "State")), Is.All.EqualTo("SUCCESS"));
        }

        [Test]
        public void Pipeline_HasNoErrorsWarningsForeignKeyFailuresOrSkippedStage()
        {
            var registry = secondSnapshot.Registry;

            Assert.That(Property(secondSession, "Stage"), Is.EqualTo("COMPLETE"));
            Assert.That(Property(secondSession, "Progress"), Is.EqualTo(1f));
            Assert.That(Property(secondSession, "ErrorCount"), Is.EqualTo(0));
            Assert.That(Property(secondSession, "WarningCount"), Is.EqualTo(0));
            Assert.That(Items(Property(secondSession, "Issues")), Is.Empty);
            Assert.That(registry.ForeignKeyResolution.Success, Is.True);
            Assert.That(registry.ForeignKeyResolution.Errors, Is.Empty);
            Assert.That(registry.RecordIndex, Is.Not.Null);
        }

        [Test]
        public void Publish_HasPositiveVersionAndMatchingLowercaseHashes()
        {
            var candidate = (string)Property(secondSession, "CandidateContentHash");
            var current = (string)Property(secondSession, "CurrentContentHash");

            Assert.That(Property(firstSession, "Published"), Is.True);
            Assert.That(Property(secondSession, "Published"), Is.True);
            Assert.That(Property(firstSession, "ReportWriteSucceeded"), Is.True);
            Assert.That(Property(secondSession, "ReportWriteSucceeded"), Is.True);
            Assert.That(firstSnapshot.Version, Is.EqualTo(1));
            Assert.That(secondSnapshot.Version, Is.EqualTo(2));
            Assert.That(Property(secondSession, "PreviousVersion"), Is.EqualTo(1L));
            Assert.That(Property(secondSession, "CurrentVersion"), Is.EqualTo(2L));
            Assert.That(candidate, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(current, Is.EqualTo(candidate));
            Assert.That(secondSnapshot.ContentHash.Hex, Is.EqualTo(current));
        }

        [Test]
        public void Reimport_IsSemanticallyStableAndFollowsVersionContract()
        {
            Assert.That(firstSnapshot.ContentHash, Is.EqualTo(secondSnapshot.ContentHash));
            Assert.That(Property(firstSession, "CurrentContentHash"),
                Is.EqualTo(Property(secondSession, "CurrentContentHash")));
            Assert.That(RegistryFingerprint(firstSnapshot.Registry),
                Is.EqualTo(RegistryFingerprint(secondSnapshot.Registry)));
            Assert.That(secondSnapshot.Version, Is.EqualTo(firstSnapshot.Version + 1));
            Assert.That(secondSnapshot.Registry, Is.Not.SameAs(firstSnapshot.Registry));
        }

        [Test]
        public void PersistedReport_IsStrictUtf8NoBomFinalLfAndMatchesLiveTuple()
        {
            var report = (CsvImportReport)Property(secondSession, "PublishReport");
            var strictUtf8 = new UTF8Encoding(false, true);

            Assert.That(persistedSecondReportBytes, Is.Not.Empty);
            Assert.That(HasUtf8Bom(persistedSecondReportBytes), Is.False);
            Assert.DoesNotThrow(() => strictUtf8.GetString(persistedSecondReportBytes));
            Assert.That(persistedSecondReportBytes[persistedSecondReportBytes.Length - 1], Is.EqualTo(0x0a));
            Assert.That(persistedSecondReportBytes.Length < 2 ||
                        persistedSecondReportBytes[persistedSecondReportBytes.Length - 2] != 0x0a,
                Is.True);
            Assert.That(persistedSecondReportBytes, Is.EqualTo(CsvImportReportJson.SerializeUtf8(report)));
            Assert.That(report.Published, Is.True);
            Assert.That(report.ErrorCount, Is.EqualTo(0));
            Assert.That(report.WarningCount, Is.EqualTo(0));
            Assert.That(report.CurrentContentHash.Hex,
                Is.EqualTo((string)Property(secondSession, "CurrentContentHash")));
        }

        [TestCaseSource(nameof(FamilyCases))]
        public void TypedFamilySet_ExactlyEqualsSourceCatalog(FamilyCase family)
        {
            var sourceIds = ReadSourceIds(family);
            var entries = TypedFamilyEntries(secondSnapshot.Registry, family);
            var typedIds = entries.Select(entry => DefinitionId(entry.Value, family)).ToArray();

            Assert.That(sourceIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(sourceIds.Length));
            Assert.That(typedIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(typedIds.Length));
            Assert.That(typedIds, Is.EquivalentTo(sourceIds),
                family.Name + " typed set must have no missing or extra ID.");
            Assert.That(typedIds, Is.EquivalentTo(family.RequiredIds));
        }

        [TestCaseSource(nameof(RequiredIdCases))]
        public void RequiredId_UsesExactTypedCollectionAndMatchingSourceIdentity(
            FamilyCase family,
            string id)
        {
            var registry = secondSnapshot.Registry;
            var identityMatches = registry.Records.Where(identity =>
                    string.Equals(identity.FileName, family.FileName, StringComparison.Ordinal) &&
                    string.Equals(
                        ParsedPrimaryKey(identity.SourceRecord, family.PrimaryKeyColumn),
                        id,
                        StringComparison.Ordinal))
                .ToArray();
            Assert.That(identityMatches.Length, Is.EqualTo(1),
                "Source PK identity must resolve exactly once: " +
                family.FileName + "." + family.PrimaryKeyColumn + "=" + id);
            var identity = identityMatches[0];

            var directMatches = TypedFamilyEntries(registry, family)
                .Where(entry => string.Equals((string)entry.Key, id, StringComparison.Ordinal))
                .ToArray();
            Assert.That(directMatches.Length, Is.EqualTo(1),
                "Required ID must resolve exactly once in the correct typed collection: " + id);
            var directDefinition = directMatches[0].Value;

            Assert.That(DefinitionId(directDefinition, family), Is.EqualTo(id));
            Assert.That(registry.TryGetTypedDefinition(identity, out var indexedDefinition), Is.True,
                "Generic typed Registry is missing required identity: " + id);
            Assert.That(indexedDefinition, Is.SameAs(directDefinition));
            Assert.That(identity.FileName, Is.EqualTo(family.FileName));
            Assert.That(Property(directDefinition, "SourceRecord"), Is.SameAs(identity.SourceRecord));
            Assert.That(ParsedPrimaryKey(identity.SourceRecord, family.PrimaryKeyColumn), Is.EqualTo(id));
        }

        [Test]
        public void RegistryCollections_AreImmutableAndLookupsAreOrdinal()
        {
            var registry = secondSnapshot.Registry;
            var worlds = (IDictionary)registry.WorldRouteDefinitions.WorldProfiles;
            var typed = (IDictionary)registry.TypedDefinitions;
            var existingWorld = registry.WorldRouteDefinitions.WorldProfiles.Values.Single();

            Assert.Throws<NotSupportedException>(() => worlds.Add("WORLD_MUTATION", existingWorld));
            Assert.Throws<NotSupportedException>(() => typed.Clear());
            Assert.That(registry.WorldRouteDefinitions.WorldProfiles.ContainsKey("world_moonpalace_v1"), Is.False);
            Assert.That(registry.TryGetReferencedPrimaryKey(
                "world_profiles.csv", "world_profile_id", "world_moonpalace_v1", out _), Is.False);
        }

        [Test]
        public void InvalidFixtureAfterValidSeed_PreservesExactPublishedSnapshot()
        {
            var store = new StaticDataRegistryStore();
            ExecuteProductionAndRestoreReport(store);
            var before = store.Current;

            using (var fixture = CsvFailureFixtureFactory.Create())
            {
                fixture.Apply(CsvFailureMutationKind.DuplicatePrimaryKey);
                var failed = fixture.Run(store, "map01-exit-audit-invalid-after-seed");

                Assert.That(failed.Report.Published, Is.False);
                Assert.That(failed.Report.ErrorCount, Is.GreaterThan(0));
                Assert.That(store.Current, Is.SameAs(before));
                Assert.That(store.Current.Registry, Is.SameAs(before.Registry));
                Assert.That(store.Current.ContentHash, Is.SameAs(before.ContentHash));
                Assert.That(store.Current.Version, Is.EqualTo(before.Version));
                Assert.That(failed.Report.CurrentVersion, Is.EqualTo(before.Version));
                Assert.That(failed.Report.CurrentContentHash, Is.EqualTo(before.ContentHash));
            }
        }

        [Test]
        public void InvalidThenProductionValid_RecoversAndReplacesSessionState()
        {
            var store = new StaticDataRegistryStore();
            CsvImportReport failedReport;
            using (var fixture = CsvFailureFixtureFactory.Create())
            {
                fixture.Apply(CsvFailureMutationKind.InvalidInt);
                failedReport = fixture.Run(store, "map01-exit-audit-invalid-first").Report;
            }

            Assert.That(failedReport.Published, Is.False);
            Assert.That(failedReport.ErrorCount, Is.GreaterThan(0));
            Assert.That(store.Current, Is.Null);

            var recovered = ExecuteProductionAndRestoreReport(store);
            Assert.That(Property(recovered, "Published"), Is.True);
            Assert.That(Property(recovered, "ErrorCount"), Is.EqualTo(0));
            Assert.That(Property(recovered, "Stage"), Is.EqualTo("COMPLETE"));
            Assert.That(store.Current, Is.Not.Null);
            Assert.That(store.Current.Version, Is.EqualTo(1));
        }

        [Test]
        public void AuthoringCsvAndMeta_AreByteExactBeforeAfterAudit()
        {
            Assert.That(preservationAfter.AuthoringCsv, Is.EqualTo(preservationBefore.AuthoringCsv));
            Assert.That(preservationAfter.AuthoringMeta, Is.EqualTo(preservationBefore.AuthoringMeta));
            Assert.That(preservationAfter.AuthoringCsv.Count, Is.EqualTo(50));
            Assert.That(preservationAfter.AuthoringMeta.Count, Is.EqualTo(50));
        }

        [Test]
        public void ProductionTestsAndAssemblyDefinitions_AreByteExactBeforeAfterAudit()
        {
            Assert.That(preservationAfter.ProductionCs, Is.EqualTo(preservationBefore.ProductionCs));
            Assert.That(preservationAfter.MapTests, Is.EqualTo(preservationBefore.MapTests));
            Assert.That(preservationAfter.AssemblyDefinitions,
                Is.EqualTo(preservationBefore.AssemblyDefinitions));
        }

        private static object ExecuteProduction(StaticDataRegistryStore store)
        {
            var pipeline = Activator.CreateInstance(EditorType("CsvImportPipeline"), store);
            return Invoke(pipeline, "Execute", (object)null);
        }

        private static object ExecuteProductionAndRestoreReport(StaticDataRegistryStore store)
        {
            var existed = File.Exists(ReportPath);
            var bytes = existed ? File.ReadAllBytes(ReportPath) : null;
            try
            {
                return ExecuteProduction(store);
            }
            finally
            {
                RestoreReport(existed, bytes);
            }
        }

        private static IReadOnlyList<string> ExpectedFileNames()
        {
            return Items(EditorType("CsvImportPipeline")
                    .GetProperty("ExpectedFileNames", BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null))
                .Cast<string>()
                .ToArray();
        }

        private static string[] ReadSourceIds(FamilyCase family)
        {
            var path = Directory.GetFiles(AuthoringRoot, family.FileName, SearchOption.AllDirectories).Single();
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(path), family.FileName);
            Assert.That(read.Success, Is.True);
            Assert.That(read.HadUtf8Bom, Is.True);
            Assert.That(read.Records.Count, Is.GreaterThan(1));
            Assert.That(read.Records[0].Fields[0].Value, Is.EqualTo(family.PrimaryKeyColumn));
            return read.Records.Skip(1).Select(record => record.Fields[0].Value).ToArray();
        }

        private static DictionaryEntryView[] TypedFamilyEntries(
            StaticDataRegistry registry,
            FamilyCase family)
        {
            var owner = Property(registry, family.OwnerProperty);
            var collectionProperty = owner.GetType().GetProperty(
                family.CollectionProperty,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(collectionProperty, Is.Not.Null,
                family.Name + " requires typed Registry collection " +
                family.OwnerProperty + "." + family.CollectionProperty + ".");
            var collection = collectionProperty.GetValue(owner);
            Assert.That(collection, Is.Not.Null);

            return ((IEnumerable)collection).Cast<object>()
                .Select(entry => new DictionaryEntryView(
                    Property(entry, "Key"), Property(entry, "Value")))
                .ToArray();
        }

        private static string DefinitionId(object definition, FamilyCase family)
        {
            var property = definition.GetType().GetProperty(
                family.DefinitionIdProperty,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null,
                family.Name + " definition requires ID property " + family.DefinitionIdProperty + ".");
            return (string)property.GetValue(definition);
        }

        private static string ParsedPrimaryKey(CsvParsedRecord record, string primaryKeyColumn)
        {
            return record.Fields.Single(field => string.Equals(
                field.Schema.ColumnName, primaryKeyColumn, StringComparison.Ordinal)).EffectiveValue;
        }

        private static string RegistryFingerprint(StaticDataRegistry registry)
        {
            var records = registry.Records
                .Select(identity => identity.FileName + ":" + identity.RecordNumber)
                .OrderBy(value => value, StringComparer.Ordinal);
            var typed = registry.TypedDefinitions
                .Select(pair => pair.Key.FileName + ":" + pair.Key.RecordNumber + ":" +
                                pair.Value.GetType().FullName)
                .OrderBy(value => value, StringComparer.Ordinal);
            return string.Join("\n", records.Concat(new[] { "--TYPED--" }).Concat(typed));
        }

        private static PreservationSnapshot CapturePreservation()
        {
            var mapRoot = Path.Combine(Application.dataPath, "_Game", "Map");
            var testRoot = Path.Combine(Application.dataPath, "_Game", "Tests", "EditMode", "Map");
            var authoringCsv = Directory.GetFiles(AuthoringRoot, "*.csv", SearchOption.AllDirectories);
            var authoringMeta = Directory.GetFiles(AuthoringRoot, "*.csv.meta", SearchOption.AllDirectories);
            var productionCs = Directory.GetFiles(mapRoot, "*.cs", SearchOption.AllDirectories);
            var mapTests = Directory.GetFiles(testRoot, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith(".cs.meta", StringComparison.OrdinalIgnoreCase));
            var assemblies = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase));

            return new PreservationSnapshot(
                HashManifest(authoringCsv),
                HashManifest(authoringMeta),
                HashManifest(productionCs),
                HashManifest(mapTests),
                HashManifest(assemblies));
        }

        private static ManifestHash HashManifest(IEnumerable<string> sourcePaths)
        {
            var paths = sourcePaths.Select(Path.GetFullPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var entries = paths.Select(path =>
                RelativeProjectPath(path) + "|" + Sha256(File.ReadAllBytes(path)));
            return new ManifestHash(paths.Length, Sha256(Encoding.UTF8.GetBytes(string.Join("\n", entries))));
        }

        private static string RelativeProjectPath(string path)
        {
            var root = ProjectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                       Path.DirectorySeparatorChar;
            return path.Substring(root.Length).Replace('\\', '/');
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static bool HasUtf8Bom(string path)
        {
            return HasUtf8Bom(File.ReadAllBytes(path));
        }

        private static bool HasUtf8Bom(byte[] bytes)
        {
            return bytes != null && bytes.Length >= 3 &&
                   bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf;
        }

        private static void RestoreOriginalReport()
        {
            RestoreReport(originalReportExisted, originalReportBytes);
        }

        private static void RestoreReport(bool existed, byte[] bytes)
        {
            if (existed)
            {
                File.WriteAllBytes(ReportPath, bytes);
            }
            else if (File.Exists(ReportPath))
            {
                File.Delete(ReportPath);
            }
        }

        private static Type EditorType(string shortName)
        {
            var fullName = EditorNamespace + shortName;
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, "Editor type was not loaded: " + fullName);
            return type;
        }

        private static object Invoke(object instance, string name, params object[] arguments)
        {
            var method = instance.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(candidate => candidate.Name == name &&
                                     candidate.GetParameters().Length == arguments.Length);
            return method.Invoke(instance, arguments);
        }

        private static object Property(object instance, string name)
        {
            Assert.That(instance, Is.Not.Null, "Cannot read property from a null instance: " + name);
            var property = instance.GetType().GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null,
                "Property was not found: " + instance.GetType().FullName + "." + name);
            return property.GetValue(instance);
        }

        private static IReadOnlyList<object> Items(object enumerable)
        {
            return ((IEnumerable)enumerable).Cast<object>().ToArray();
        }

        public sealed class FamilyCase
        {
            public FamilyCase(
                string name,
                string fileName,
                string primaryKeyColumn,
                string ownerProperty,
                string collectionProperty,
                string definitionIdProperty,
                string[] requiredIds)
            {
                Name = name;
                FileName = fileName;
                PrimaryKeyColumn = primaryKeyColumn;
                OwnerProperty = ownerProperty;
                CollectionProperty = collectionProperty;
                DefinitionIdProperty = definitionIdProperty;
                RequiredIds = requiredIds;
            }

            public string Name { get; }
            public string FileName { get; }
            public string PrimaryKeyColumn { get; }
            public string OwnerProperty { get; }
            public string CollectionProperty { get; }
            public string DefinitionIdProperty { get; }
            public string[] RequiredIds { get; }
            public override string ToString() => Name;
        }

        private sealed class DictionaryEntryView
        {
            public DictionaryEntryView(object key, object value)
            {
                Key = key;
                Value = value;
            }

            public object Key { get; }
            public object Value { get; }
        }

        private sealed class PreservationSnapshot
        {
            public PreservationSnapshot(
                ManifestHash authoringCsv,
                ManifestHash authoringMeta,
                ManifestHash productionCs,
                ManifestHash mapTests,
                ManifestHash assemblyDefinitions)
            {
                AuthoringCsv = authoringCsv;
                AuthoringMeta = authoringMeta;
                ProductionCs = productionCs;
                MapTests = mapTests;
                AssemblyDefinitions = assemblyDefinitions;
            }

            public ManifestHash AuthoringCsv { get; }
            public ManifestHash AuthoringMeta { get; }
            public ManifestHash ProductionCs { get; }
            public ManifestHash MapTests { get; }
            public ManifestHash AssemblyDefinitions { get; }
        }

        private sealed class ManifestHash : IEquatable<ManifestHash>
        {
            public ManifestHash(int count, string hash)
            {
                Count = count;
                Hash = hash;
            }

            public int Count { get; }
            public string Hash { get; }

            public bool Equals(ManifestHash other)
            {
                return other != null && Count == other.Count &&
                       string.Equals(Hash, other.Hash, StringComparison.Ordinal);
            }

            public override bool Equals(object obj) => Equals(obj as ManifestHash);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Count * 397) ^ StringComparer.Ordinal.GetHashCode(Hash);
                }
            }

            public override string ToString() => Count + "/" + Hash;
        }
    }
}
