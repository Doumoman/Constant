using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class MandatoryRouteMaskLookupBuilderTests
    {
        private const string FileName = "sector_route_masks.csv";

        private static IEnumerable<int> AllOpenMaskBits => Enumerable.Range(0, 16);
        private static IEnumerable<string> ValidIds => Enumerable.Range(0, 24).Select(index => "ROUTE_VALID_" + index.ToString("D2", CultureInfo.InvariantCulture));
        private static IEnumerable<string> InvalidIds => new[]
        {
            null, string.Empty, "route", "Route", "A-B", "A B", "A.B", "A/B", "A\\B", "A:B",
            "A+B", "A=B", "A@B", "A#B", "A$B", "A%B", "A^B", "A&B", "A*B", "한글"
        };
        private static IEnumerable<int> UndefinedKinds => Enumerable.Range(3, 12);
        private static IEnumerable<int> LookupCases => Enumerable.Range(0, 9);
        private static IEnumerable<int> ShuffleSeeds => Enumerable.Range(0, 12);
        private static IEnumerable<int> StructuralCases => Enumerable.Range(0, 14);
        private static IEnumerable<string> Map06_03PlusSymbols => new[]
        {
            "MandatoryRoutePass", "SectorRouteMaskAssigner",
            "MandatoryRouteOverlayRenderer", "MandatoryRouteOverlayReport",
            "Type0Overlay",
            "MicroChunkDefinition", "MandatoryRouteBatchRunner",
            "MandatoryRouteBatchExitAudit", "OptionalReturnConnection", "OptionalClueAssigner"
        };

        [TestCaseSource(nameof(AllOpenMaskBits))]
        public void OpenMaskAllBitPatternsHaveStableValueSemantics(int bits)
        {
            var mask = Mask(bits);
            var copy = Mask(bits);
            Assert.That(mask, Is.EqualTo(copy));
            Assert.That(mask.GetHashCode(), Is.EqualTo(bits));
            Assert.That(mask.OpenCount, Is.EqualTo(CountBits(bits)));
            Assert.That(mask.HasHorizontalRun, Is.EqualTo((bits & 3) == 3));
            Assert.That(mask.HasVerticalPairConflict, Is.EqualTo((bits & 12) == 12));
            Assert.That(mask.CompareTo(Mask((bits + 1) & 15)), Is.EqualTo(bits.CompareTo((bits + 1) & 15)));
        }

        [TestCaseSource(nameof(ValidIds))]
        public void MaskIdAcceptsCanonicalValuesWithStableOrdinalSemantics(string value)
        {
            Assert.That(MandatoryRouteMaskId.TryCreate(value, out var parsed), Is.True);
            var direct = new MandatoryRouteMaskId(value);
            Assert.That(parsed, Is.EqualTo(direct));
            Assert.That(parsed.Value, Is.EqualTo(value));
            Assert.That(parsed.IsValid, Is.True);
            Assert.That(parsed.GetHashCode(), Is.EqualTo(direct.GetHashCode()));
        }

        [TestCaseSource(nameof(InvalidIds))]
        public void MaskIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(MandatoryRouteMaskId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new MandatoryRouteMaskId(value));
            else Assert.Throws<ArgumentException>(() => new MandatoryRouteMaskId(value));
        }

        [TestCaseSource(nameof(UndefinedKinds))]
        public void LookupRejectsUndefinedRequiredKind(int rawKind)
        {
            var lookup = BuildStarter().Lookup;
            Assert.Throws<ArgumentOutOfRangeException>(() => lookup.GetRequired((MandatoryRouteMaskKind)rawKind));
        }

        [TestCaseSource(nameof(LookupCases))]
        public void ExactLookupApisReturnTheSameRecordIdentity(int lookupCase)
        {
            var lookup = BuildStarter().Lookup;
            var index = lookupCase % 3;
            var expected = lookup.Records[index];
            MandatoryRouteMaskRecord actual;
            if (lookupCase < 3)
                Assert.That(lookup.TryGetById(expected.MaskId, out actual), Is.True);
            else if (lookupCase < 6)
                Assert.That(lookup.TryGetByRouteType(expected.RouteType, out actual), Is.True);
            else
                Assert.That(lookup.TryGetByOpenMask(expected.OpenMask, out actual), Is.True);
            Assert.That(ReferenceEquals(actual, expected), Is.True);
            Assert.That(ReferenceEquals(lookup.GetRequired(expected.Kind), expected), Is.True);
        }

        [TestCaseSource(nameof(ShuffleSeeds))]
        public void ShuffledInputCultureAndBuilderReuseRemainDeterministic(int seed)
        {
            var baseline = Signature(BuildStarter());
            var rows = StarterDefinitions();
            var shuffled = rows.OrderBy(row => StableShuffleKey(row.RouteMaskId, seed)).ToArray();
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = seed % 2 == 0 ? new CultureInfo("en-US") : new CultureInfo("tr-TR");
                var builder = new MandatoryRouteMaskLookupBuilder();
                Assert.That(Signature(builder.Build(shuffled)), Is.EqualTo(baseline));
                Assert.That(Signature(builder.Build(shuffled.Reverse())), Is.EqualTo(baseline));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCaseSource(nameof(StructuralCases))]
        public void StructuralPreflightRejectsExactInvalidScenario(int scenario)
        {
            var rows = StarterDefinitions();
            MandatoryRouteMaskLookupBuildErrorCode expected;
            switch (scenario)
            {
                case 0:
                    rows.RemoveAll(row => row.RouteMaskId == "ROUTE_T1_LR");
                    expected = MandatoryRouteMaskLookupBuildErrorCode.MissingRequiredMask;
                    break;
                case 1:
                    rows.Add(Definition("ROUTE_T1_LR", 1, true, true, false, false, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.DuplicateMaskId;
                    break;
                case 2:
                    Replace(rows, "ROUTE_T2_LRD", Definition("ROUTE_T2_LRD", 1, true, true, false, true, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.DuplicateRouteType;
                    break;
                case 3:
                    Replace(rows, "ROUTE_T2_LRD", Definition("ROUTE_T2_LRD", 2, true, true, false, false, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.DuplicateOpenMask;
                    break;
                case 4:
                    Replace(rows, "ROUTE_T1_LR", Definition("ROUTE_T1_LR", 1, true, true, false, false, true, false));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.InactiveRequiredMask;
                    break;
                case 5:
                    Replace(rows, "ROUTE_T2_LRD", Definition("ROUTE_T2_LRD", 2, true, true, false, true, false, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.MandatoryNotAllowed;
                    break;
                case 6:
                    rows.Add(Definition("ROUTE_EXTRA", 1, true, true, false, false, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.UnexpectedMandatoryMask;
                    break;
                case 7:
                    Replace(rows, "ROUTE_T3_LRU", Definition("ROUTE_T3_LRU", 4, true, true, true, false, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.InvalidRouteType;
                    break;
                case 8:
                    Replace(rows, "ROUTE_T1_LR", Definition("ROUTE_T1_LR", 1, false, true, false, false, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.InvalidOpenMask;
                    break;
                case 9:
                    Replace(rows, "ROUTE_T1_LR", Definition("ROUTE_T1_LR", 1, true, false, false, false, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.InvalidOpenMask;
                    break;
                case 10:
                    Replace(rows, "ROUTE_T2_LRD", Definition("ROUTE_T2_LRD", 2, true, true, true, true, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.UnsupportedVerticalPair;
                    break;
                case 11:
                    Replace(rows, "ROUTE_T3_LRU", Definition("ROUTE_T3_LRU", 3, true, true, false, true, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.InvalidOpenMask;
                    break;
                case 12:
                    rows.Add(null);
                    expected = MandatoryRouteMaskLookupBuildErrorCode.MissingInput;
                    break;
                default:
                    rows.Add(Definition("ROUTE_TYPE5", 5, true, true, false, false, true, true));
                    expected = MandatoryRouteMaskLookupBuildErrorCode.InvalidRouteType;
                    break;
            }
            var result = new MandatoryRouteMaskLookupBuilder().Build(rows);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteMaskLookupBuildStatus.InvalidInput));
            Assert.That(result.Lookup, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(expected));
            Assert.That(result.RetryRequired, Is.False);
        }

        [TestCaseSource(nameof(Map06_03PlusSymbols))]
        public void Map06_03PlusProductionSymbolsAreAbsent(string typeName)
        {
            var assembly = typeof(MandatoryRouteMaskLookupBuilder).Assembly;
            Assert.That(assembly.GetType("StarNight.Map.WorldGeneration.Generation." + typeName, false), Is.Null);
        }

        [Test]
        public void ExactStarterBuildPublishesThreeRegisteredMasksAndDiagnostics()
        {
            var source = StarterDefinitions();
            var result = new MandatoryRouteMaskLookupBuilder().Build(source);
            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteMaskLookupBuildStatus.Completed));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Lookup.Count, Is.EqualTo(3));
            Assert.That(result.Lookup.Records.Select(record => record.MaskId.Value), Is.EqualTo(new[] { "ROUTE_T1_LR", "ROUTE_T2_LRD", "ROUTE_T3_LRU" }));
            Assert.That(result.Lookup.Records.Select(record => record.RouteType), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(result.Lookup.Records.Select(record => record.OpenMask), Is.EqualTo(new[] { MandatoryRouteOpenMask.Type1Horizontal, MandatoryRouteOpenMask.Type2Down, MandatoryRouteOpenMask.Type3Up }));
            Assert.That(result.Diagnostics.SourceRouteMaskCount, Is.EqualTo(15));
            Assert.That(result.Diagnostics.ActiveRouteMaskCount, Is.EqualTo(15));
            Assert.That(result.Diagnostics.MandatoryAllowedRouteMaskCount, Is.EqualTo(3));
            Assert.That(result.Diagnostics.AcceptedMandatoryMaskCount, Is.EqualTo(3));
            Assert.That(new[] { result.Diagnostics.Type1Count, result.Diagnostics.Type2Count, result.Diagnostics.Type3Count }, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(result.Diagnostics.IgnoredType0Count, Is.EqualTo(12));
            Assert.That(result.Diagnostics.RejectedMandatoryCandidateCount, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
        }

        [Test]
        public void WorldDefinitionSetOverloadUsesTypedRouteMaskObjects()
        {
            var set = StarterDefinitionSet();
            var source = set.RouteMasks.Values.ToList();
            var result = new MandatoryRouteMaskLookupBuilder().Build(set);
            Assert.That(result.Success, Is.True, FormatErrors(result));
            for (var index = 0; index < 3; index++)
                Assert.That(ReferenceEquals(result.Lookup.Records[index].SourceDefinition,
                    source.Single(row => row.RouteMaskId == result.Lookup.Records[index].MaskId.Value)), Is.True);
        }

        [Test]
        public void NullInputsReturnStableInvalidResultWithoutThrowing()
        {
            var builder = new MandatoryRouteMaskLookupBuilder();
            var enumerableResult = builder.Build((IEnumerable<SectorRouteMaskDefinition>)null);
            var setResult = builder.Build((WorldRouteDefinitionSet)null);
            Assert.That(enumerableResult.Errors.Single().Code, Is.EqualTo(MandatoryRouteMaskLookupBuildErrorCode.MissingInput));
            Assert.That(setResult.Errors.Single().Code, Is.EqualTo(MandatoryRouteMaskLookupBuildErrorCode.MissingInput));
            Assert.That(Signature(enumerableResult), Is.EqualTo(Signature(builder.Build((IEnumerable<SectorRouteMaskDefinition>)null))));
        }

        [Test]
        public void Type0RowsAreIgnoredEvenWhenTheirShapeResemblesMandatoryMasks()
        {
            var rows = StarterDefinitions();
            rows.Add(Definition("ROUTE_T0_MIMIC", 0, true, true, false, true, true, true));
            var result = new MandatoryRouteMaskLookupBuilder().Build(rows);
            Assert.That(result.Success, Is.True, FormatErrors(result));
            Assert.That(result.Lookup.Count, Is.EqualTo(3));
            Assert.That(result.Diagnostics.IgnoredType0Count, Is.EqualTo(13));
            Assert.That(result.Diagnostics.MandatoryAllowedRouteMaskCount, Is.EqualTo(4));
        }

        [Test]
        public void PublishedCollectionsAndPublicSurfaceAreImmutable()
        {
            var result = BuildStarter();
            Assert.Throws<NotSupportedException>(() => ((IList)result.Lookup.Records).Clear());
            foreach (var type in new[] { typeof(MandatoryRouteMaskRecord), typeof(MandatoryRouteMaskLookup), typeof(MandatoryRouteMaskLookupDiagnostics), typeof(MandatoryRouteMaskLookupBuildResult) })
                Assert.That(type.GetProperties(BindingFlags.Instance | BindingFlags.Public).All(property => property.SetMethod == null), Is.True, type.FullName);
        }

        [Test]
        public void SourceDefinitionsRemainUnchangedAndPreservedByReference()
        {
            var rows = StarterDefinitions();
            var before = string.Join("|", rows.Select(RowSignature));
            var result = new MandatoryRouteMaskLookupBuilder().Build(rows);
            Assert.That(string.Join("|", rows.Select(RowSignature)), Is.EqualTo(before));
            foreach (var record in result.Lookup.Records)
                Assert.That(ReferenceEquals(record.SourceDefinition, rows.Single(row => row.RouteMaskId == record.MaskId.Value)), Is.True);
        }

        [Test]
        public void ErrorOrderingAndDeduplicationAreStable()
        {
            var rows = StarterDefinitions();
            rows.Add(Definition("ROUTE_EXTRA_Z", 1, true, true, false, false, true, true));
            rows.Add(Definition("ROUTE_EXTRA_A", 1, true, true, false, false, true, true));
            var errors = new MandatoryRouteMaskLookupBuilder().Build(rows).Errors;
            var signatures = errors.Select(ErrorSignature).ToArray();
            Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(signatures.Length));
            for (var index = 1; index < errors.Count; index++)
                Assert.That(CompareErrors(errors[index - 1], errors[index]), Is.LessThan(0));
        }

        [Test]
        public void ParallelFreshBuildsProduceOneExactSignature()
        {
            var signatures = new string[24];
            Parallel.For(0, signatures.Length, index => signatures[index] = Signature(new MandatoryRouteMaskLookupBuilder().Build(StarterDefinitions())));
            Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
        }

        [Test]
        public void RuntimeTypesContainNoMutableStaticFieldsAndAssemblyDoesNotReferenceUnityEditor()
        {
            var types = new[]
            {
                typeof(MandatoryRouteOpenMask), typeof(MandatoryRouteMaskId), typeof(MandatoryRouteMaskRecord),
                typeof(MandatoryRouteMaskLookup), typeof(MandatoryRouteMaskLookupBuildError),
                typeof(MandatoryRouteMaskLookupDiagnostics), typeof(MandatoryRouteMaskLookupBuilder)
            };
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            Assert.That(typeof(MandatoryRouteMaskLookupBuilder).Assembly.GetReferencedAssemblies()
                .Any(reference => reference.Name == "UnityEditor"), Is.False);
        }

        [Test]
        public void MissingLookupKeysReturnFalseAndUndefinedKindThrows()
        {
            var lookup = BuildStarter().Lookup;
            Assert.That(lookup.TryGetById(new MandatoryRouteMaskId("UNKNOWN"), out _), Is.False);
            Assert.That(lookup.TryGetByRouteType(0, out _), Is.False);
            Assert.That(lookup.TryGetByOpenMask(default(MandatoryRouteOpenMask), out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => lookup.GetRequired((MandatoryRouteMaskKind)(-1)));
        }

        private static MandatoryRouteMaskLookupBuildResult BuildStarter() =>
            new MandatoryRouteMaskLookupBuilder().Build(StarterDefinitions());

        private static List<SectorRouteMaskDefinition> StarterDefinitions()
        {
            return StarterDefinitionSet().RouteMasks.Values.ToList();
        }

        private static SectorRouteMaskDefinition Definition(string id, int routeType, bool left, bool right, bool up, bool down, bool mandatory, bool active)
        {
            return BuildDefinitionSet(new[] { new MaskRow(id, routeType, left, right, up, down, mandatory, active) })
                .RouteMasks.Values.Single();
        }

        private static void Replace(List<SectorRouteMaskDefinition> rows, string id, SectorRouteMaskDefinition replacement)
        {
            rows.RemoveAll(row => row != null && row.RouteMaskId == id);
            rows.Add(replacement);
        }

        private static MandatoryRouteOpenMask Mask(int bits) =>
            new MandatoryRouteOpenMask((bits & 1) != 0, (bits & 2) != 0, (bits & 4) != 0, (bits & 8) != 0);

        private static int CountBits(int bits)
        {
            var count = 0;
            for (var value = bits; value != 0; value >>= 1) count += value & 1;
            return count;
        }

        private static int StableShuffleKey(string value, int seed)
        {
            unchecked
            {
                var hash = seed + 17;
                for (var index = 0; index < value.Length; index++) hash = hash * 31 + value[index];
                return hash;
            }
        }

        private static string Signature(MandatoryRouteMaskLookupBuildResult result) =>
            result.Status + "|" + string.Join(",", result.Errors.Select(error => error.Code + ":" + error.RouteType + ":" + error.FirstId + ":" + error.SecondId + ":" + error.Message)) + "|" +
            (result.Lookup == null ? "null" : string.Join(",", result.Lookup.Records.Select(record => record.MaskId.Value + ":" + record.RouteType + ":" + record.OpenMask))) + "|" +
            (result.Diagnostics == null ? "null" : string.Join(",", result.Diagnostics.SourceRouteMaskCount, result.Diagnostics.ActiveRouteMaskCount,
                result.Diagnostics.MandatoryAllowedRouteMaskCount, result.Diagnostics.AcceptedMandatoryMaskCount,
                result.Diagnostics.IgnoredType0Count, result.Diagnostics.RngDrawCount, result.Diagnostics.SourceMutationCount));

        private static string RowSignature(SectorRouteMaskDefinition row) => string.Join(":", row.RouteMaskId, row.RouteType,
            row.OpenL, row.OpenR, row.OpenU, row.OpenD, row.MandatoryAllowed, row.DescriptionKo, row.Active);

        private static string FormatErrors(MandatoryRouteMaskLookupBuildResult result) =>
            string.Join("\n", result.Errors.Select(error => error.Code + " " + error.Message));

        private static WorldRouteDefinitionSet StarterDefinitionSet()
        {
            var rows = new List<MaskRow>
            {
                new MaskRow("ROUTE_T1_LR", 1, true, true, false, false, true, true),
                new MaskRow("ROUTE_T2_LRD", 2, true, true, false, true, true, true),
                new MaskRow("ROUTE_T3_LRU", 3, true, true, true, false, true, true)
            };
            for (var index = 0; index < 12; index++)
                rows.Add(new MaskRow("ROUTE_T0_" + index.ToString("D2", CultureInfo.InvariantCulture), 0,
                    (index & 1) != 0, (index & 2) != 0, (index & 4) != 0, (index & 8) != 0, false, true));
            return BuildDefinitionSet(rows);
        }

        private static WorldRouteDefinitionSet BuildDefinitionSet(IReadOnlyList<MaskRow> maskRows)
        {
            var sources = CreateSpecs().Select(spec => BuildSource(spec, maskRows)).ToArray();
            var result = new WorldRouteDefinitionBuilder().Build(sources);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors.Select(error => error.FileName + " " + error.Message)));
            return result.DefinitionSet;
        }

        private static WorldRouteDefinitionSource BuildSource(FileSpec spec, IReadOnlyList<MaskRow> maskRows)
        {
            var dictionaryRows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName, (index + 1).ToString(CultureInfo.InvariantCulture), column.Name, column.DataType,
                index < spec.PrimaryKeyCount ? "1" : "0",
                index < spec.PrimaryKeyCount ? (index + 1).ToString(CultureInfo.InvariantCulture) : string.Empty,
                string.Empty,
                column.DataType == "ENUM" || column.DataType == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty,
                string.Empty, string.Empty, index + 2));
            var catalog = new CsvSchemaCatalogBuilder().Build(dictionaryRows);
            Assert.That(catalog.Success, Is.True, string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            if (spec.FileName == FileName)
                foreach (var row in maskRows)
                    csv += "\n" + string.Join(",", row.Values);
            var read = new Rfc4180CsvReader().Read(new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            Assert.That(validation.Success, Is.True, string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            Assert.That(keys.Success, Is.True);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            return new WorldRouteDefinitionSource(schema, parsed);
        }

        private static FileSpec[] CreateSpecs()
        {
            return new[]
            {
                File("world_profiles.csv", 1, "world_profile_id:ID", "display_name_ko:STRING", "width_tiles:INT", "height_tiles:INT", "sector_width_tiles:INT", "sector_height_tiles:INT", "sector_cols:INT", "sector_rows:INT", "micro_width_tiles:INT", "micro_height_tiles:INT", "micro_cols_per_sector:INT", "micro_rows_per_sector:INT", "min_completion_distance_tiles:INT", "max_shortest_completion_distance_tiles:INT", "normal_completion_min_tiles:INT", "normal_completion_max_tiles:INT", "optional_completion_max_tiles:INT", "max_revisit_ratio:FLOAT", "required_village_count:INT", "active:BOOL", "notes:STRING"),
                File("generation_profiles.csv", 1, "generation_profile_id:ID", "world_profile_id:ID", "mandatory_sector_min:INT", "mandatory_sector_max:INT", "type0_sector_min:INT", "type0_sector_max:INT", "reserved_sector_min:INT", "reserved_sector_max:INT", "inactive_sector_min:INT", "inactive_sector_max:INT", "start_edge_ring_min:INT", "start_edge_ring_max:INT", "mandatory_loop_min:INT", "mandatory_loop_max:INT", "optional_region_depth_min:INT", "optional_region_depth_max:INT", "optional_region_count_min:INT", "optional_region_count_max:INT", "site_reservation_retry_max:INT", "biome_retry_max:INT", "route_retry_max:INT", "sector_solve_retry_max:INT", "active:BOOL", "notes:STRING"),
                File("generation_passes.csv", 3, "generation_profile_id:ID", "pass_order:INT", "pass_id:ID", "class_name:STRING", "rng_stream_id:ID", "input_artifacts:ID_LIST", "output_artifacts:ID_LIST", "failure_policy:ENUM", "max_retry_count:INT", "enabled:BOOL", "notes:STRING"),
                File("rng_streams.csv", 1, "rng_stream_id:ID", "salt_hex:HEX", "reset_scope:ENUM", "description_ko:STRING", "active:BOOL"),
                File(FileName, 1, "route_mask_id:ID", "route_type:INT", "open_l:BOOL", "open_r:BOOL", "open_u:BOOL", "open_d:BOOL", "mandatory_allowed:BOOL", "description_ko:STRING", "active:BOOL"),
                File("socket_band_definitions.csv", 1, "band_id:ID", "axis:ENUM", "min_local_coord:INT", "max_local_coord:INT", "recommended_center:FLOAT", "minimum_clearance_tiles:INT", "description_ko:STRING"),
                File("edge_signatures.csv", 1, "edge_signature_id:ID", "axis:ENUM", "band_id:ID", "traversal_kind:ENUM", "ground_entry_height:INT", "clearance_width:INT", "clearance_height:INT", "tool_requirement:ENUM", "mandatory_allowed:BOOL", "tags:ID_LIST", "notes:STRING"),
                File("edge_signature_compatibility.csv", 2, "signature_a:ID", "signature_b:ID", "compatible:BOOL", "adapter_microchunk_pool_id:ID", "notes:STRING"),
                File("sector_recipe_catalog.csv", 1, "sector_recipe_id:ID", "display_name_ko:STRING", "route_type:INT", "route_mask_id:ID", "primary_biome_id:ID", "secondary_biome_id:ID", "boundary_profile_id:ID", "recipe_kind:ENUM", "microchunk_budget_profile_id:ID", "selection_weight:INT", "supports_special_entry:BOOL", "supports_village_entry:BOOL", "active:BOOL", "notes:STRING"),
                File("sector_recipe_cells.csv", 3, "sector_recipe_id:ID", "chunk_x:INT", "chunk_y:INT", "cell_role:ENUM", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_usage_class:ENUM_LIST", "required_route_roles:ID_LIST", "required_biome_ids:ID_LIST", "required_signature_l:ID", "required_signature_r:ID", "required_signature_u:ID", "required_signature_d:ID", "transform_policy:ENUM_LIST", "notes:STRING"),
                File("sector_recipe_paths.csv", 3, "sector_recipe_id:ID", "path_id:ID", "path_order:INT", "chunk_x:INT", "chunk_y:INT", "enter_side:ENUM", "exit_side:ENUM", "mandatory:BOOL", "traversal_kind:ENUM", "max_jump_tiles:INT", "notes:STRING"),
                File("sector_external_sockets.csv", 2, "sector_recipe_id:ID", "socket_id:ID", "side:ENUM", "edge_chunk_index:INT", "band_id:ID", "traversal_kind:ENUM", "mandatory_allowed:BOOL", "edge_signature_id:ID", "notes:STRING"),
                File("sector_recipe_pool_entries.csv", 3, "sector_recipe_pool_id:ID", "entry_order:INT", "sector_recipe_id:ID", "weight:INT", "min_repeat_distance_sectors:INT", "required_patch_role:ENUM", "active:BOOL")
            };
        }

        private static FileSpec File(string fileName, int primaryKeyCount, params string[] definitions) =>
            new FileSpec(fileName, primaryKeyCount, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                return new ColumnSpec(parts[0], parts[1]);
            }).ToArray());

        private static int CompareErrors(MandatoryRouteMaskLookupBuildError left, MandatoryRouteMaskLookupBuildError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = left.RouteType.CompareTo(right.RouteType);
            if (value != 0) return value;
            value = string.Compare(left.FirstId, right.FirstId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.SecondId, right.SecondId, StringComparison.Ordinal);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static string ErrorSignature(MandatoryRouteMaskLookupBuildError error) =>
            error.Code + "|" + error.RouteType + "|" + error.FirstId + "|" + error.SecondId + "|" + error.Message;

        private static string Bool(bool value) => value ? "1" : "0";

        private sealed class MaskRow
        {
            public MaskRow(string id, int routeType, bool left, bool right, bool up, bool down, bool mandatory, bool active)
            {
                Values = new[] { id, routeType.ToString(CultureInfo.InvariantCulture), Bool(left), Bool(right), Bool(up), Bool(down), Bool(mandatory), "DESC", Bool(active) };
            }
            public IReadOnlyList<string> Values { get; }
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, int primaryKeyCount, IReadOnlyList<ColumnSpec> columns)
            {
                FileName = fileName;
                PrimaryKeyCount = primaryKeyCount;
                Columns = columns;
            }
            public string FileName { get; }
            public int PrimaryKeyCount { get; }
            public IReadOnlyList<ColumnSpec> Columns { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string name, string dataType)
            {
                Name = name;
                DataType = dataType;
            }
            public string Name { get; }
            public string DataType { get; }
        }
    }
}
