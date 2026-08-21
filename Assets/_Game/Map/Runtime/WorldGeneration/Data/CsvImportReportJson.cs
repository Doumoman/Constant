using System;
using System.Globalization;
using System.Text;

namespace StarNight.Map.WorldGeneration.Data
{
    public static class CsvImportReportJson
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static string Serialize(CsvImportReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            var builder = new StringBuilder(512);
            builder.Append('{');
            AppendName(builder, "schema_version");
            AppendNumber(builder, report.SchemaVersion);
            AppendSeparator(builder, "attempt_id");
            AppendString(builder, report.AttemptId);
            AppendSeparator(builder, "published");
            builder.Append(report.Published ? "true" : "false");
            AppendSeparator(builder, "previous_version");
            AppendNumber(builder, report.PreviousVersion);
            AppendSeparator(builder, "current_version");
            AppendNumber(builder, report.CurrentVersion);
            AppendSeparator(builder, "previous_content_hash");
            AppendHash(builder, report.PreviousContentHash);
            AppendSeparator(builder, "candidate_content_hash");
            AppendHash(builder, report.CandidateContentHash);
            AppendSeparator(builder, "current_content_hash");
            AppendHash(builder, report.CurrentContentHash);
            AppendSeparator(builder, "error_count");
            AppendNumber(builder, report.ErrorCount);
            AppendSeparator(builder, "warning_count");
            AppendNumber(builder, report.WarningCount);
            AppendSeparator(builder, "issues");
            builder.Append('[');
            for (var index = 0; index < report.Issues.Count; index++)
            {
                if (index > 0) builder.Append(',');
                AppendIssue(builder, report.Issues[index]);
            }

            builder.Append(']');
            builder.Append('}');
            builder.Append('\n');
            return builder.ToString();
        }

        public static byte[] SerializeUtf8(CsvImportReport report)
        {
            return StrictUtf8.GetBytes(Serialize(report));
        }

        private static void AppendIssue(StringBuilder builder, CsvImportIssue issue)
        {
            if (issue == null) throw new ArgumentException("A report cannot contain a null issue.");
            builder.Append('{');
            AppendName(builder, "stage");
            AppendString(builder, issue.Stage);
            AppendSeparator(builder, "severity");
            AppendString(builder, issue.Severity);
            AppendSeparator(builder, "code");
            AppendString(builder, issue.Code);
            AppendSeparator(builder, "message");
            AppendString(builder, issue.Message);
            AppendSeparator(builder, "source_file");
            AppendString(builder, issue.SourceFile);
            AppendSeparator(builder, "record_number");
            AppendNullableNumber(builder, issue.RecordNumber);
            AppendSeparator(builder, "source_field");
            AppendString(builder, issue.SourceField);
            AppendSeparator(builder, "line");
            AppendNullableNumber(builder, issue.Line);
            AppendSeparator(builder, "column");
            AppendNullableNumber(builder, issue.Column);
            AppendSeparator(builder, "offset");
            AppendNullableNumber(builder, issue.Offset);
            AppendSeparator(builder, "target_file");
            AppendString(builder, issue.TargetFile);
            AppendSeparator(builder, "target_column");
            AppendString(builder, issue.TargetColumn);
            AppendSeparator(builder, "target_value");
            AppendString(builder, issue.TargetValue);
            builder.Append('}');
        }

        private static void AppendSeparator(StringBuilder builder, string name)
        {
            builder.Append(',');
            AppendName(builder, name);
        }

        private static void AppendName(StringBuilder builder, string name)
        {
            AppendString(builder, name);
            builder.Append(':');
        }

        private static void AppendHash(StringBuilder builder, ContentVersionHash hash)
        {
            AppendString(builder, hash == null ? null : hash.Hex);
        }

        private static void AppendNullableNumber(StringBuilder builder, int? value)
        {
            if (!value.HasValue)
            {
                builder.Append("null");
                return;
            }

            AppendNumber(builder, value.Value);
        }

        private static void AppendNumber(StringBuilder builder, long value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
        }
    }
}
