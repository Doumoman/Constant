#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Text;
using StarNight.Interaction.Carry;
using StarNight.Map;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.ToolAuthoring
{
    public enum ToolValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct ToolValidationIssue
    {
        public ToolValidationIssue(ToolValidationSeverity severity, string assetName, string message)
        {
            Severity = severity;
            AssetName = assetName;
            Message = message;
        }

        public ToolValidationSeverity Severity { get; }
        public string AssetName { get; }
        public string Message { get; }
    }

    public static class ToolInteractionValidation
    {
        public static List<ToolValidationIssue> Validate(
            IReadOnlyList<HandToolDefinition> tools,
            IReadOnlyList<CarryObjectDefinition> carryObjects,
            IReadOnlyList<MapElementDefinition> elements)
        {
            var issues = new List<ToolValidationIssue>();
            ValidateTools(tools, issues);
            ValidateCarryObjects(carryObjects, issues);
            ValidateReactionMatrix(elements, issues);
            return issues;
        }

        public static string BuildMarkdown(IReadOnlyList<ToolValidationIssue> issues)
        {
            int errors = 0;
            int warnings = 0;
            var builder = new StringBuilder();
            builder.AppendLine("# Tool Interaction Validation Report");
            builder.AppendLine();
            builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            for (int index = 0; index < issues.Count; index++)
            {
                ToolValidationIssue issue = issues[index];
                if (issue.Severity == ToolValidationSeverity.Error) errors++;
                if (issue.Severity == ToolValidationSeverity.Warning) warnings++;
                builder.AppendLine($"- [{issue.Severity}] `{issue.AssetName}` — {issue.Message}");
            }
            if (issues.Count == 0)
            {
                builder.AppendLine("- No issues.");
            }
            builder.AppendLine();
            builder.AppendLine($"Errors: {errors} / Warnings: {warnings} / Total: {issues.Count}");
            return builder.ToString();
        }

        private static void ValidateTools(
            IReadOnlyList<HandToolDefinition> tools,
            List<ToolValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < tools.Count; index++)
            {
                HandToolDefinition tool = tools[index];
                if (tool == null) continue;
                if (string.IsNullOrWhiteSpace(tool.ToolId))
                {
                    issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, tool.name, "Tool ID is empty."));
                }
                else if (!ids.Add(tool.ToolId))
                {
                    issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, tool.name, $"Duplicate Tool ID: {tool.ToolId}"));
                }
                if (tool.ToolTags == ToolTag.None)
                {
                    issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, tool.name, "ToolTag is None."));
                }
                if (tool.ResourceMode != ToolResourceMode.Infinite && tool.MaxResource <= 0)
                {
                    issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, tool.name, "Finite resource tool needs Max Resource."));
                }
                if (tool.TargetCellOffsets.Length == 0 && tool.ToolId != "TOOL_WIND_UMBRELLA")
                {
                    issues.Add(new ToolValidationIssue(ToolValidationSeverity.Warning, tool.name, "No target cell offset."));
                }
            }
        }

        private static void ValidateCarryObjects(
            IReadOnlyList<CarryObjectDefinition> carryObjects,
            List<ToolValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < carryObjects.Count; index++)
            {
                CarryObjectDefinition carry = carryObjects[index];
                if (carry == null) continue;
                if (string.IsNullOrWhiteSpace(carry.ObjectId))
                {
                    issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, carry.name, "Object ID is empty."));
                }
                else if (!ids.Add(carry.ObjectId))
                {
                    issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, carry.name, $"Duplicate Object ID: {carry.ObjectId}"));
                }
                if (carry.Footprint.x != 1 || carry.Footprint.y < 1 || carry.Footprint.y > 2)
                {
                    issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, carry.name, "HandCarry footprint must be 1x1 or 1x2."));
                }
            }
        }

        private static void ValidateReactionMatrix(
            IReadOnlyList<MapElementDefinition> elements,
            List<ToolValidationIssue> issues)
        {
            for (int elementIndex = 0; elementIndex < elements.Count; elementIndex++)
            {
                MapElementDefinition element = elements[elementIndex];
                if (element == null || element.ToolReactions?.Entries == null) continue;
                var tools = new HashSet<ToolTag>();
                for (int entryIndex = 0; entryIndex < element.ToolReactions.Entries.Count; entryIndex++)
                {
                    ToolReactionEntry entry = element.ToolReactions.Entries[entryIndex];
                    if (entry == null || entry.Reaction == ElementReactionType.None) continue;
                    if (!tools.Add(entry.Tool))
                    {
                        issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, element.name, $"Duplicate reaction for {entry.Tool}."));
                    }
                    if (ToolReactionReceiver.ResolveFeedback(entry) == FeedbackId.None)
                    {
                        issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, element.name, $"Accepted {entry.Tool} reaction has no feedback."));
                    }
                    if (element.CommonProfile?.Kind == CommonElementKind.UnbreakableBlock)
                    {
                        issues.Add(new ToolValidationIssue(ToolValidationSeverity.Error, element.name, $"Unbreakable element defines {entry.Tool} reaction."));
                    }
                }
            }
        }
    }
}

#endif
