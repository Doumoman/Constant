using System;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using UnityEditor;
using UnityEngine;

namespace StarNight.Map.Editor.WorldGeneration.Data
{
    public sealed class CsvImportNavigationResult
    {
        public CsvImportNavigationResult(
            bool success,
            string reason,
            string projectRelativePath,
            int line)
        {
            Success = success;
            Reason = reason ?? string.Empty;
            ProjectRelativePath = projectRelativePath ?? string.Empty;
            Line = line;
        }

        public bool Success { get; }
        public string Reason { get; }
        public string ProjectRelativePath { get; }
        public int Line { get; }
    }

    public sealed class CsvImportNavigation
    {
        public CsvImportNavigationResult GoToSource(CsvImportIssue issue)
        {
            if (issue == null || string.IsNullOrEmpty(issue.SourceFile))
            {
                return Failure("The issue has no source file.");
            }

            return OpenFile(issue.SourceFile, issue.Line ?? 1);
        }

        public CsvImportNavigationResult GoToForeignKeyTarget(
            CsvImportIssue issue,
            ForeignKeyRecordIndex recordIndex)
        {
            var reason = GetForeignKeyTargetUnavailableReason(issue, recordIndex);
            if (reason.Length != 0) return Failure(reason);

            recordIndex.TryGet(
                issue.TargetFile,
                issue.TargetColumn,
                issue.TargetValue,
                out var identity);
            return OpenFile(
                issue.TargetFile,
                identity.SourceRecord.SourceRecord.StartLocation.PhysicalLine);
        }

        public static string GetForeignKeyTargetUnavailableReason(
            CsvImportIssue issue,
            ForeignKeyRecordIndex recordIndex)
        {
            if (issue == null) return "No issue is selected.";
            if (string.IsNullOrEmpty(issue.TargetFile) ||
                string.IsNullOrEmpty(issue.TargetColumn) ||
                string.IsNullOrEmpty(issue.TargetValue))
            {
                return "The selected issue has no complete FK target tuple.";
            }

            if (recordIndex == null) return "No successful FK record index is available.";
            return recordIndex.TryGet(
                issue.TargetFile,
                issue.TargetColumn,
                issue.TargetValue,
                out _)
                ? string.Empty
                : "The FK target record is unavailable in the current index.";
        }

        public static CsvImportNavigationResult ResolveProjectPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return Failure("Filename is empty.");
            if (Path.IsPathRooted(fileName) ||
                fileName.IndexOf('/') >= 0 ||
                fileName.IndexOf('\\') >= 0 ||
                !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            {
                return Failure("Only one canonical CSV filename is accepted.");
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var authoringRoot = Path.GetFullPath(Path.Combine(
                projectRoot,
                CsvImportPipeline.AuthoringRootProjectRelativePath.Replace(
                    '/', Path.DirectorySeparatorChar)));
            var rootPrefix = authoringRoot.TrimEnd(
                                 Path.DirectorySeparatorChar,
                                 Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            var matches = Directory.Exists(authoringRoot)
                ? Directory.GetFiles(authoringRoot, fileName, SearchOption.AllDirectories)
                    .Where(path => string.Equals(
                        Path.GetFileName(path), fileName, StringComparison.Ordinal))
                    .Select(Path.GetFullPath)
                    .Where(path => path.StartsWith(
                        rootPrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray()
                : Array.Empty<string>();
            if (matches.Length == 0) return Failure("CSV file was not found under the fixed root.");
            if (matches.Length != 1) return Failure("CSV filename is not unique under the fixed root.");

            var projectPrefix = projectRoot.TrimEnd(
                                    Path.DirectorySeparatorChar,
                                    Path.AltDirectorySeparatorChar) +
                                Path.DirectorySeparatorChar;
            if (!matches[0].StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Failure("Resolved path escaped the Unity project root.");
            }

            return new CsvImportNavigationResult(
                true,
                string.Empty,
                matches[0].Substring(projectPrefix.Length).Replace('\\', '/'),
                1);
        }

        private static CsvImportNavigationResult OpenFile(string fileName, int line)
        {
            var resolved = ResolveProjectPath(fileName);
            if (!resolved.Success) return resolved;
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(resolved.ProjectRelativePath);
            if (asset == null)
            {
                return Failure("Unity could not load the resolved CSV asset.");
            }

            var safeLine = Math.Max(1, line);
            if (AssetDatabase.OpenAsset(asset, safeLine))
            {
                return new CsvImportNavigationResult(
                    true, string.Empty, resolved.ProjectRelativePath, safeLine);
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return new CsvImportNavigationResult(
                false,
                "Line opening is unavailable; the CSV asset was selected instead.",
                resolved.ProjectRelativePath,
                safeLine);
        }

        private static CsvImportNavigationResult Failure(string reason)
        {
            return new CsvImportNavigationResult(false, reason, string.Empty, 0);
        }
    }
}
