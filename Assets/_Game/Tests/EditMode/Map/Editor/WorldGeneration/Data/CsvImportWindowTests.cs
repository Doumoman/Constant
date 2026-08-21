using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.Editor.WorldGeneration.Data
{
    [TestFixture]
    public sealed class CsvImportWindowTests
    {
        private const string Namespace = "StarNight.Map.Editor.WorldGeneration.Data.";
        private const string Root = "Assets/_Game/Map/Data/WorldGeneration/Authoring/";

        [Test]
        public void ExpectedInventory_HasExactCount()
        {
            Assert.That(ExpectedFileNames().Count, Is.EqualTo(50));
        }

        [Test]
        public void ExpectedInventory_DictionaryIsFirst()
        {
            Assert.That(ExpectedFileNames()[0], Is.EqualTo("CSV_DATA_DICTIONARY.csv"));
        }

        [Test]
        public void ExpectedInventory_StaticFilesAreOrdinal()
        {
            var files = ExpectedFileNames().Skip(1).ToArray();
            Assert.That(files, Is.EqualTo(files.OrderBy(value => value, StringComparer.Ordinal)));
        }

        [Test]
        public void InitialResult_HasFiftyNotRunRows()
        {
            var result = Invoke(New("CsvImportPipeline"), "CreateNotRunResult");
            var files = Items(Property(result, "Files"));
            Assert.That(files.Count, Is.EqualTo(50));
            Assert.That(files.Select(file => Property(file, "State")),
                Is.All.EqualTo("NOT_RUN"));
        }

        [Test]
        public void InitialResult_UsesOnlyFixedRootPaths()
        {
            var result = Invoke(New("CsvImportPipeline"), "CreateNotRunResult");
            var paths = Items(Property(result, "Files"))
                .Select(file => (string)Property(file, "ProjectRelativePath"));
            Assert.That(paths, Is.All.StartsWith(Root));
        }

        [TestCase("CSV_DATA_DICTIONARY.csv")]
        [TestCase("world_profiles.csv")]
        [TestCase("biome_types.csv")]
        public void Navigation_ResolvesKnownFilename(string filename)
        {
            var result = InvokeStatic("CsvImportNavigation", "ResolveProjectPath", filename);
            Assert.That(Property(result, "Success"), Is.True);
            Assert.That((string)Property(result, "ProjectRelativePath"), Does.StartWith(Root));
        }

        [TestCase("")]
        [TestCase("../world_profiles.csv")]
        [TestCase("Biome/biome_types.csv")]
        [TestCase("Biome\\biome_types.csv")]
        [TestCase("C:\\temp\\world_profiles.csv")]
        [TestCase("/tmp/world_profiles.csv")]
        [TestCase("..")]
        public void Navigation_RejectsInjectedFilename(string filename)
        {
            var result = InvokeStatic("CsvImportNavigation", "ResolveProjectPath", filename);
            Assert.That(Property(result, "Success"), Is.False);
            Assert.That(Property(result, "Reason"), Is.Not.Empty);
        }

        [Test]
        public void InventoryValidation_AcceptsExactInventory()
        {
            Assert.That(InventoryIssues(VirtualInventory()), Is.Empty);
        }

        [Test]
        public void InventoryValidation_ReportsMissingFile()
        {
            var paths = VirtualInventory().Skip(1).ToArray();
            AssertIssueCode(paths, "MISSING_FILE");
        }

        [Test]
        public void InventoryValidation_ReportsUnexpectedFile()
        {
            var paths = VirtualInventory().Concat(new[] { Root + "unexpected.csv" }).ToArray();
            AssertIssueCode(paths, "UNEXPECTED_FILE");
        }

        [Test]
        public void InventoryValidation_ReportsDuplicateFilename()
        {
            var paths = VirtualInventory().Concat(new[] { Root + "Other/world_profiles.csv" })
                .ToArray();
            AssertIssueCode(paths, "DUPLICATE_FILE");
        }

        [Test]
        public void InventoryValidation_RejectsPathOutsideRoot()
        {
            var paths = VirtualInventory().Concat(new[] { "Assets/outside.csv" }).ToArray();
            AssertIssueCode(paths, "PATH_OUTSIDE_FIXED_ROOT");
        }

        [Test]
        public void InventoryValidation_NullInputIsDiagnostic()
        {
            var issues = Items(InvokeStatic(
                "CsvImportPipeline", "ValidateInventory", (object)null));
            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(Property(issues[0], "Code"), Is.EqualTo("MISSING_INVENTORY"));
        }

        [Test]
        public void ReportBytes_AcceptStrictUtf8WithSingleLf()
        {
            Assert.That(ValidateReportBytes(Encoding.UTF8.GetBytes("{}\n")), Is.Empty);
        }

        [Test]
        public void ReportBytes_RejectEmpty()
        {
            Assert.That(ValidateReportBytes(Array.Empty<byte>()), Is.Not.Empty);
        }

        [Test]
        public void ReportBytes_RejectMissingLf()
        {
            Assert.That(ValidateReportBytes(Encoding.UTF8.GetBytes("{}")), Is.Not.Empty);
        }

        [Test]
        public void ReportBytes_RejectDoubleLf()
        {
            Assert.That(ValidateReportBytes(Encoding.UTF8.GetBytes("{}\n\n")), Is.Not.Empty);
        }

        [Test]
        public void ReportBytes_RejectBom()
        {
            Assert.That(ValidateReportBytes(new byte[] { 0xef, 0xbb, 0xbf, 0x7b, 0x7d, 0x0a }),
                Is.Not.Empty);
        }

        [Test]
        public void ReportBytes_RejectInvalidUtf8()
        {
            Assert.That(ValidateReportBytes(new byte[] { 0xc3, 0x28, 0x0a }), Is.Not.Empty);
        }

        [Test]
        public void ReportWriter_WritesAndAtomicallyReplacesInTempDirectory()
        {
            var directory = NewTempDirectory();
            try
            {
                var writer = New("CsvImportReportFileWriter");
                var first = Invoke(writer, "WriteToDirectory",
                    Encoding.UTF8.GetBytes("{\"run\":1}\n"), directory);
                var second = Invoke(writer, "WriteToDirectory",
                    Encoding.UTF8.GetBytes("{\"run\":2}\n"), directory);
                Assert.That(Property(first, "Success"), Is.True);
                Assert.That(Property(second, "Success"), Is.True);
                Assert.That(File.ReadAllText(Path.Combine(directory, "CsvImportReport.json")),
                    Is.EqualTo("{\"run\":2}\n"));
                Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void ReportWriter_RejectsInvalidBytesWithoutCreatingDestination()
        {
            var directory = NewTempDirectory();
            try
            {
                var writer = New("CsvImportReportFileWriter");
                var result = Invoke(writer, "WriteToDirectory",
                    Encoding.UTF8.GetBytes("{}"), directory);
                Assert.That(Property(result, "Success"), Is.False);
                Assert.That(File.Exists(Path.Combine(directory, "CsvImportReport.json")), Is.False);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void WindowState_StartsIdle()
        {
            var state = NewState();
            Assert.That(Property(state, "IsRunning"), Is.False);
        }

        [Test]
        public void WindowState_BeginsOnce()
        {
            var state = NewState();
            Assert.That(Invoke(state, "TryBeginRun"), Is.True);
            Assert.That(Property(state, "IsRunning"), Is.True);
        }

        [Test]
        public void WindowState_BlocksReentry()
        {
            var state = NewState();
            Invoke(state, "TryBeginRun");
            Assert.That(Invoke(state, "TryBeginRun"), Is.False);
        }

        [TestCase(-1f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(2f, 1f)]
        public void WindowState_ClampsRunningProgress(float input, float expected)
        {
            var state = NewState();
            Invoke(state, "TryBeginRun");
            Invoke(state, "UpdateProgress", "TEST", input);
            Assert.That((float)Property(state, "Progress"), Is.EqualTo(expected));
        }

        [Test]
        public void WindowState_IgnoresIdleProgress()
        {
            var state = NewState();
            Invoke(state, "UpdateProgress", "TEST", 0.8f);
            Assert.That(Property(state, "Stage"), Is.EqualTo("NOT_RUN"));
        }

        [Test]
        public void WindowState_CompleteReplacesSnapshotAndClearsRunning()
        {
            var pipeline = New("CsvImportPipeline");
            var initial = Invoke(pipeline, "CreateNotRunResult");
            var state = Activator.CreateInstance(EditorType("CsvImportWindowState"), initial);
            Invoke(state, "TryBeginRun");
            Invoke(state, "Complete", initial);
            Assert.That(Property(state, "IsRunning"), Is.False);
            Assert.That(Property(state, "LastResult"), Is.SameAs(initial));
        }

        [TestCase("ALL", 50)]
        [TestCase("ERROR", 0)]
        [TestCase("WARNING", 0)]
        public void FileFilter_UsesSeverity(string filter, int count)
        {
            var result = Invoke(New("CsvImportPipeline"), "CreateNotRunResult");
            var filtered = InvokeStatic(
                "CsvImportWindowState", "FilterFiles",
                Property(result, "Files"), string.Empty, filter);
            Assert.That(Items(filtered).Count, Is.EqualTo(count));
        }

        [Test]
        public void FileFilter_SearchesFilenameOrdinalIgnoreCase()
        {
            var result = Invoke(New("CsvImportPipeline"), "CreateNotRunResult");
            var filtered = InvokeStatic(
                "CsvImportWindowState", "FilterFiles",
                Property(result, "Files"), "WORLD_PROFILES", "ALL");
            Assert.That(Items(filtered).Count, Is.EqualTo(1));
        }

        [TestCase("ALL", 2)]
        [TestCase("ERROR", 1)]
        [TestCase("WARNING", 1)]
        public void IssueFilter_UsesSeverity(string filter, int count)
        {
            var issues = new[]
            {
                new CsvImportIssue("A", "ERROR", "E", "broken", "a.csv"),
                new CsvImportIssue("B", "WARNING", "W", "careful", "b.csv")
            };
            var filtered = InvokeStatic(
                "CsvImportWindowState", "FilterIssues", issues, string.Empty, filter);
            Assert.That(Items(filtered).Count, Is.EqualTo(count));
        }

        [Test]
        public void IssueFilter_SearchesTargetTuple()
        {
            var issues = new[]
            {
                new CsvImportIssue(
                    "FK", "ERROR", "E", "missing", "a.csv",
                    targetFile: "target.csv", targetColumn: "id", targetValue: "VALUE")
            };
            var filtered = InvokeStatic(
                "CsvImportWindowState", "FilterIssues", issues, "TARGET", "ALL");
            Assert.That(Items(filtered).Count, Is.EqualTo(1));
        }

        [Test]
        public void ForeignKeyNavigation_ExplainsMissingTuple()
        {
            var reason = InvokeStatic(
                "CsvImportNavigation", "GetForeignKeyTargetUnavailableReason",
                new CsvImportIssue("FK", "ERROR", "E", "missing"), null);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void ReportPath_IsExactContract()
        {
            var field = EditorType("CsvImportReportFileWriter").GetField(
                "ReportProjectRelativePath", BindingFlags.Public | BindingFlags.Static);
            Assert.That(field.GetRawConstantValue(),
                Is.EqualTo("MapDesign/MCP/REPORTS/CsvImportReport.json"));
        }

        [Test]
        public void FullValidBaseline_ReimportsPublishesAndWritesReport()
        {
            var result = Invoke(New("CsvImportPipeline"), "Execute", (object)null);
            Assert.That(Property(result, "Published"), Is.True);
            Assert.That(Property(result, "ReportWriteSucceeded"), Is.True);
            Assert.That(Property(result, "ErrorCount"), Is.EqualTo(0));
            Assert.That(Property(result, "RecordIndex"), Is.Not.Null);
            var files = Items(Property(result, "Files"));
            Assert.That(files.Count, Is.EqualTo(50));
            Assert.That(files.Select(file => Property(file, "State")),
                Is.All.EqualTo("SUCCESS"));
            Assert.That(files.Select(file => (string)Property(file, "RawSha256"))
                .All(value => value.Length == 64 && value.All(IsLowerHex)), Is.True);
            Assert.That((string)Property(result, "CurrentContentHash"),
                Does.Match("^[0-9a-f]{64}$"));
        }

        private static object NewState()
        {
            var initial = Invoke(New("CsvImportPipeline"), "CreateNotRunResult");
            return Activator.CreateInstance(EditorType("CsvImportWindowState"), initial);
        }

        private static IReadOnlyList<string> ExpectedFileNames()
        {
            return Items(EditorType("CsvImportPipeline")
                    .GetProperty("ExpectedFileNames", BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null))
                .Cast<string>()
                .ToArray();
        }

        private static string[] VirtualInventory()
        {
            return ExpectedFileNames().Select(file => Root + file).ToArray();
        }

        private static IReadOnlyList<object> InventoryIssues(IEnumerable<string> paths)
        {
            return Items(InvokeStatic(
                "CsvImportPipeline", "ValidateInventory", paths));
        }

        private static void AssertIssueCode(IEnumerable<string> paths, string code)
        {
            Assert.That(
                InventoryIssues(paths).Select(issue => Property(issue, "Code")),
                Does.Contain(code));
        }

        private static string ValidateReportBytes(byte[] bytes)
        {
            return (string)InvokeStatic(
                "CsvImportReportFileWriter", "ValidateBytes", bytes);
        }

        private static string NewTempDirectory()
        {
            var path = Path.Combine(
                Path.GetTempPath(), "StarNightCsvImportTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static bool IsLowerHex(char value)
        {
            return value >= '0' && value <= '9' || value >= 'a' && value <= 'f';
        }

        private static Type EditorType(string shortName)
        {
            var fullName = Namespace + shortName;
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, "Editor type was not loaded: " + fullName);
            return type;
        }

        private static object New(string shortName)
        {
            return Activator.CreateInstance(EditorType(shortName));
        }

        private static object Invoke(object instance, string name, params object[] arguments)
        {
            var method = FindMethod(instance.GetType(), name, arguments.Length, false);
            return method.Invoke(instance, arguments);
        }

        private static object InvokeStatic(string shortType, string name, params object[] arguments)
        {
            var method = FindMethod(EditorType(shortType), name, arguments.Length, true);
            return method.Invoke(null, arguments);
        }

        private static MethodInfo FindMethod(
            Type type,
            string name,
            int parameterCount,
            bool isStatic)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic |
                        (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            return type.GetMethods(flags).Single(method =>
                method.Name == name && method.GetParameters().Length == parameterCount);
        }

        private static object Property(object instance, string name)
        {
            return instance.GetType().GetProperty(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(instance);
        }

        private static IReadOnlyList<object> Items(object enumerable)
        {
            return ((IEnumerable)enumerable).Cast<object>().ToArray();
        }
    }
}
