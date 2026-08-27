using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public enum V2AuthoringOwner
    {
        MicroPattern,
        TerrainCluster,
        Activity,
        EventOverlay,
        SpecialRegion,
    }

    public enum V2AuthoringSchemaDomain
    {
        AuthoringV2,
        LegacyAuthoring,
        Generated,
    }

    public sealed class V2AuthoringForeignKey
    {
        public V2AuthoringForeignKey(
            V2AuthoringSchemaDomain targetDomain,
            string targetFileName,
            string targetColumnName)
            : this(string.Empty, string.Empty, targetDomain, targetFileName, targetColumnName)
        {
        }

        internal V2AuthoringForeignKey(
            string sourceFileName,
            string sourceColumnName,
            V2AuthoringSchemaDomain targetDomain,
            string targetFileName,
            string targetColumnName)
        {
            SourceFileName = sourceFileName ?? string.Empty;
            SourceColumnName = sourceColumnName ?? string.Empty;
            TargetDomain = targetDomain;
            TargetFileName = targetFileName ?? string.Empty;
            TargetColumnName = targetColumnName ?? string.Empty;
        }

        public string SourceFileName { get; }
        public string SourceColumnName { get; }
        public V2AuthoringSchemaDomain TargetDomain { get; }
        public string TargetFileName { get; }
        public string TargetColumnName { get; }

        internal V2AuthoringForeignKey Bind(string sourceFileName, string sourceColumnName)
        {
            return new V2AuthoringForeignKey(
                sourceFileName,
                sourceColumnName,
                TargetDomain,
                TargetFileName,
                TargetColumnName);
        }

        public override string ToString()
        {
            return TargetDomain + ":" + TargetFileName + "." + TargetColumnName;
        }
    }

    public sealed class V2AuthoringColumnDescriptor
    {
        private readonly ReadOnlyCollection<string> allowedValues;

        public V2AuthoringColumnDescriptor(
            int columnOrder,
            string columnName,
            CsvSchemaDataType dataType,
            bool isRequired,
            int? primaryKeyOrder = null,
            string defaultValue = "",
            IEnumerable<string> allowedValues = null,
            V2AuthoringForeignKey foreignKey = null,
            string description = "")
        {
            ColumnOrder = columnOrder;
            ColumnName = columnName ?? string.Empty;
            DataType = dataType;
            IsRequired = isRequired;
            PrimaryKeyOrder = primaryKeyOrder;
            DefaultValue = defaultValue ?? string.Empty;
            this.allowedValues = new ReadOnlyCollection<string>(
                (allowedValues ?? Array.Empty<string>()).Select(value => value ?? string.Empty).ToList());
            ForeignKey = foreignKey;
            Description = description ?? string.Empty;
        }

        public int ColumnOrder { get; }
        public string ColumnName { get; }
        public CsvSchemaDataType DataType { get; }
        public bool IsRequired { get; }
        public int? PrimaryKeyOrder { get; }
        public string DefaultValue { get; }
        public IReadOnlyList<string> AllowedValues => allowedValues;
        public V2AuthoringForeignKey ForeignKey { get; }
        public string Description { get; }
    }

    public sealed class V2AuthoringTableDescriptor
    {
        private readonly ReadOnlyCollection<V2AuthoringColumnDescriptor> columns;

        public V2AuthoringTableDescriptor(
            string tableId,
            V2AuthoringOwner owner,
            string relativeAuthoringPath,
            IEnumerable<V2AuthoringColumnDescriptor> columns,
            string displayName = "")
        {
            TableId = tableId ?? string.Empty;
            Owner = owner;
            RelativeAuthoringPath = (relativeAuthoringPath ?? string.Empty).Replace('\\', '/');
            DisplayName = displayName ?? string.Empty;
            this.columns = new ReadOnlyCollection<V2AuthoringColumnDescriptor>(
                (columns ?? Array.Empty<V2AuthoringColumnDescriptor>())
                .OrderBy(value => value == null ? int.MaxValue : value.ColumnOrder)
                .ThenBy(value => value == null ? string.Empty : value.ColumnName, StringComparer.Ordinal)
                .ToList());
        }

        public string TableId { get; }
        public V2AuthoringOwner Owner { get; }
        public string RelativeAuthoringPath { get; }
        public string DisplayName { get; }
        public IReadOnlyList<V2AuthoringColumnDescriptor> Columns => columns;

        public string FileName
        {
            get
            {
                var separator = RelativeAuthoringPath.LastIndexOf('/');
                return separator < 0
                    ? RelativeAuthoringPath
                    : RelativeAuthoringPath.Substring(separator + 1);
            }
        }
    }
}
