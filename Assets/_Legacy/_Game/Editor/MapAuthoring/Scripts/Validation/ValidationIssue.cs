#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    [Serializable]
    public sealed class ValidationIssue
    {
        public ValidationIssue(
            ValidationSeverity severity,
            string code,
            string message,
            string assetPath = "",
            UnityEngine.Object context = null,
            bool autoFixable = false)
        {
            Severity = severity;
            Code = code;
            Message = message;
            AssetPath = assetPath;
            Context = context;
            AutoFixable = autoFixable;
        }

        public ValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string AssetPath { get; }
        public UnityEngine.Object Context { get; }
        public bool AutoFixable { get; }

        public override string ToString()
        {
            return $"[{Severity}] {Code}: {Message}";
        }
    }

    public sealed class MapElementValidationReport
    {
        private readonly List<ValidationIssue> issues = new List<ValidationIssue>();

        public MapElementValidationReport(string subject)
        {
            Subject = subject;
        }

        public string Subject { get; }
        public IReadOnlyList<ValidationIssue> Issues => issues;
        public int ErrorCount => issues.Count(issue => issue.Severity == ValidationSeverity.Error);
        public int WarningCount => issues.Count(issue => issue.Severity == ValidationSeverity.Warning);
        public int InfoCount => issues.Count(issue => issue.Severity == ValidationSeverity.Info);
        public bool IsValid => ErrorCount == 0;

        public void Add(
            ValidationSeverity severity,
            string code,
            string message,
            string assetPath = "",
            UnityEngine.Object context = null,
            bool autoFixable = false)
        {
            issues.Add(new ValidationIssue(
                severity,
                code,
                message,
                assetPath,
                context,
                autoFixable));
        }

        public void Merge(MapElementValidationReport other)
        {
            if (other != null)
            {
                issues.AddRange(other.issues);
            }
        }

        public string CreateSummary()
        {
            return $"{Subject}: Error {ErrorCount} · Warning {WarningCount}";
        }
    }
}

#endif
