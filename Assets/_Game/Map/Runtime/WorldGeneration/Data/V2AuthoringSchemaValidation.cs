using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class V2AuthoringSchemaValidationError : IEquatable<V2AuthoringSchemaValidationError>
    {
        public V2AuthoringSchemaValidationError(
            string code,
            string tablePath,
            string columnName,
            string message)
        {
            Code = code ?? string.Empty;
            TablePath = tablePath ?? string.Empty;
            ColumnName = columnName ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string Code { get; }
        public string TablePath { get; }
        public string ColumnName { get; }
        public string Message { get; }

        public bool Equals(V2AuthoringSchemaValidationError other)
        {
            return other != null &&
                   string.Equals(Code, other.Code, StringComparison.Ordinal) &&
                   string.Equals(TablePath, other.TablePath, StringComparison.Ordinal) &&
                   string.Equals(ColumnName, other.ColumnName, StringComparison.Ordinal) &&
                   string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as V2AuthoringSchemaValidationError);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(Code);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(TablePath);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ColumnName);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
                return hash;
            }
        }

        public override string ToString()
        {
            return Code + " " + TablePath +
                   (ColumnName.Length == 0 ? string.Empty : "." + ColumnName) + ": " + Message;
        }

        internal static int Compare(
            V2AuthoringSchemaValidationError left,
            V2AuthoringSchemaValidationError right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.TablePath, right.TablePath);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.ColumnName, right.ColumnName);
            if (comparison != 0) return comparison;
            comparison = StringComparer.Ordinal.Compare(left.Code, right.Code);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.Message, right.Message);
        }
    }

    public sealed class V2AuthoringSchemaValidationResult
    {
        private readonly ReadOnlyCollection<V2AuthoringSchemaValidationError> errors;

        internal V2AuthoringSchemaValidationResult(
            V2AuthoringSchemaRegistry registry,
            IEnumerable<V2AuthoringSchemaValidationError> sourceErrors)
        {
            Registry = registry;
            errors = new ReadOnlyCollection<V2AuthoringSchemaValidationError>(
                (sourceErrors ?? Array.Empty<V2AuthoringSchemaValidationError>()).ToList());
        }

        public bool Success => Registry != null && errors.Count == 0;
        public V2AuthoringSchemaRegistry Registry { get; }
        public V2AuthoringForeignKeyIndex ForeignKeyIndex => Registry == null ? null : Registry.ForeignKeyIndex;
        public string CanonicalDigest => Registry == null ? null : Registry.CanonicalDigest;
        public IReadOnlyList<V2AuthoringSchemaValidationError> Errors => errors;
    }

    public sealed class V2AuthoringForeignKeyIndex
    {
        private readonly IReadOnlyDictionary<string, V2AuthoringTableDescriptor> tablesByPath;
        private readonly IReadOnlyDictionary<string, V2AuthoringColumnDescriptor> columnsByFileAndName;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<V2AuthoringColumnDescriptor>> primaryKeysByFile;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<V2AuthoringForeignKey>> outgoingByFile;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<V2AuthoringForeignKey>> incomingByFile;

        internal V2AuthoringForeignKeyIndex(IEnumerable<V2AuthoringTableDescriptor> sourceTables)
        {
            var tables = sourceTables
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal)
                .ToArray();
            tablesByPath = new ReadOnlyDictionary<string, V2AuthoringTableDescriptor>(
                tables.ToDictionary(value => value.RelativeAuthoringPath, StringComparer.Ordinal));

            var columns = new Dictionary<string, V2AuthoringColumnDescriptor>(StringComparer.Ordinal);
            var primaryKeys = new Dictionary<string, IReadOnlyList<V2AuthoringColumnDescriptor>>(StringComparer.Ordinal);
            var outgoing = new Dictionary<string, List<V2AuthoringForeignKey>>(StringComparer.Ordinal);
            var incoming = new Dictionary<string, List<V2AuthoringForeignKey>>(StringComparer.Ordinal);
            foreach (var table in tables)
            {
                foreach (var column in table.Columns)
                {
                    columns.Add(ColumnKey(table.FileName, column.ColumnName), column);
                    if (column.ForeignKey == null) continue;
                    var edge = column.ForeignKey.Bind(table.FileName, column.ColumnName);
                    Add(outgoing, table.FileName, edge);
                    Add(incoming, edge.TargetFileName, edge);
                }

                primaryKeys.Add(
                    table.FileName,
                    new ReadOnlyCollection<V2AuthoringColumnDescriptor>(table.Columns
                        .Where(value => value.PrimaryKeyOrder.HasValue)
                        .OrderBy(value => value.PrimaryKeyOrder.Value)
                        .ToList()));
            }

            columnsByFileAndName = new ReadOnlyDictionary<string, V2AuthoringColumnDescriptor>(columns);
            primaryKeysByFile = new ReadOnlyDictionary<string, IReadOnlyList<V2AuthoringColumnDescriptor>>(primaryKeys);
            outgoingByFile = FreezeEdges(outgoing);
            incomingByFile = FreezeEdges(incoming);
        }

        public bool TryGetTable(string relativeAuthoringPath, out V2AuthoringTableDescriptor table)
        {
            return tablesByPath.TryGetValue(relativeAuthoringPath, out table);
        }

        public bool TryGetColumn(
            string fileName,
            string columnName,
            out V2AuthoringColumnDescriptor column)
        {
            return columnsByFileAndName.TryGetValue(ColumnKey(fileName, columnName), out column);
        }

        public IReadOnlyList<V2AuthoringColumnDescriptor> GetPrimaryKeyColumns(string fileName)
        {
            return primaryKeysByFile.TryGetValue(fileName, out var values)
                ? values
                : Array.Empty<V2AuthoringColumnDescriptor>();
        }

        public IReadOnlyList<V2AuthoringForeignKey> GetOutgoingForeignKeys(string fileName)
        {
            return outgoingByFile.TryGetValue(fileName, out var values)
                ? values
                : Array.Empty<V2AuthoringForeignKey>();
        }

        public IReadOnlyList<V2AuthoringForeignKey> GetIncomingForeignKeys(string fileName)
        {
            return incomingByFile.TryGetValue(fileName, out var values)
                ? values
                : Array.Empty<V2AuthoringForeignKey>();
        }

        private static string ColumnKey(string fileName, string columnName)
        {
            return (fileName ?? string.Empty) + "\0" + (columnName ?? string.Empty);
        }

        private static void Add(
            IDictionary<string, List<V2AuthoringForeignKey>> index,
            string key,
            V2AuthoringForeignKey value)
        {
            if (!index.TryGetValue(key, out var values))
            {
                values = new List<V2AuthoringForeignKey>();
                index.Add(key, values);
            }
            values.Add(value);
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<V2AuthoringForeignKey>> FreezeEdges(
            IDictionary<string, List<V2AuthoringForeignKey>> source)
        {
            var result = new Dictionary<string, IReadOnlyList<V2AuthoringForeignKey>>(StringComparer.Ordinal);
            foreach (var pair in source)
            {
                result.Add(pair.Key, new ReadOnlyCollection<V2AuthoringForeignKey>(pair.Value
                    .OrderBy(value => value.SourceFileName, StringComparer.Ordinal)
                    .ThenBy(value => value.SourceColumnName, StringComparer.Ordinal)
                    .ThenBy(value => value.TargetDomain)
                    .ThenBy(value => value.TargetFileName, StringComparer.Ordinal)
                    .ThenBy(value => value.TargetColumnName, StringComparer.Ordinal)
                    .ToList()));
            }
            return new ReadOnlyDictionary<string, IReadOnlyList<V2AuthoringForeignKey>>(result);
        }
    }

    public static class V2AuthoringSchemaValidator
    {
        public static V2AuthoringSchemaValidationResult Validate(
            IEnumerable<V2AuthoringTableDescriptor> sourceTables,
            CsvSchemaCatalog legacyCatalog)
        {
            var errors = new List<V2AuthoringSchemaValidationError>();
            if (sourceTables == null)
            {
                Add(errors, "NULL_TABLE_SET", string.Empty, string.Empty,
                    "V2 Authoring table descriptors are required.");
                return Failure(errors);
            }

            var tables = sourceTables.ToList();
            if (tables.Any(value => value == null))
            {
                Add(errors, "NULL_TABLE", string.Empty, string.Empty,
                    "A V2 Authoring table descriptor cannot be null.");
            }
            var presentTables = tables.Where(value => value != null)
                .OrderBy(value => value.RelativeAuthoringPath, StringComparer.Ordinal)
                .ToArray();

            ValidateTableIdentities(presentTables, errors);
            ValidateColumns(presentTables, errors);
            ValidateForeignKeys(presentTables, legacyCatalog, errors);
            ValidateCycles(presentTables, errors);

            var orderedErrors = errors.Distinct()
                .OrderBy(value => value, Comparer<V2AuthoringSchemaValidationError>.Create(
                    V2AuthoringSchemaValidationError.Compare))
                .ToArray();
            if (orderedErrors.Length > 0)
            {
                return new V2AuthoringSchemaValidationResult(null, orderedErrors);
            }

            var index = new V2AuthoringForeignKeyIndex(presentTables);
            var digest = V2AuthoringSchemaCanonicalDigest.Compute(presentTables);
            return new V2AuthoringSchemaValidationResult(
                new V2AuthoringSchemaRegistry(presentTables, index, digest),
                orderedErrors);
        }

        private static void ValidateTableIdentities(
            IReadOnlyList<V2AuthoringTableDescriptor> tables,
            ICollection<V2AuthoringSchemaValidationError> errors)
        {
            foreach (var table in tables)
            {
                if (string.IsNullOrWhiteSpace(table.TableId))
                    Add(errors, "MISSING_TABLE_ID", table.RelativeAuthoringPath, string.Empty,
                        "Table ID is required.");
                if (string.IsNullOrWhiteSpace(table.RelativeAuthoringPath) ||
                    !table.RelativeAuthoringPath.EndsWith(".csv", StringComparison.Ordinal))
                    Add(errors, "INVALID_TABLE_PATH", table.RelativeAuthoringPath, string.Empty,
                        "Relative Authoring path must end with lowercase .csv.");
                var expectedRoot = table.Owner + "/";
                if (!table.RelativeAuthoringPath.StartsWith(expectedRoot, StringComparison.Ordinal))
                    Add(errors, "OWNER_PATH_MISMATCH", table.RelativeAuthoringPath, string.Empty,
                        "Table path must stay below its approved owner root.");
                if (table.RelativeAuthoringPath.IndexOf("Generated", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    table.FileName.StartsWith("generated_", StringComparison.OrdinalIgnoreCase))
                    Add(errors, "GENERATED_PATH", table.RelativeAuthoringPath, string.Empty,
                        "Generated paths and generated_* tables cannot be Authoring schemas.");
            }

            AddDuplicates(tables, value => value.RelativeAuthoringPath,
                "DUPLICATE_TABLE_PATH", errors);
            AddDuplicates(tables, value => value.TableId,
                "DUPLICATE_TABLE_ID", errors);
            AddDuplicates(tables, value => value.FileName,
                "DUPLICATE_FILE_NAME", errors);
            AddCaseCollisions(tables, value => value.RelativeAuthoringPath,
                "TABLE_PATH_CASE_COLLISION", errors);
            AddCaseCollisions(tables, value => value.TableId,
                "TABLE_ID_CASE_COLLISION", errors);
            AddCaseCollisions(tables, value => value.FileName,
                "FILE_NAME_CASE_COLLISION", errors);
        }

        private static void ValidateColumns(
            IReadOnlyList<V2AuthoringTableDescriptor> tables,
            ICollection<V2AuthoringSchemaValidationError> errors)
        {
            foreach (var table in tables)
            {
                if (table.Columns.Any(value => value == null))
                    Add(errors, "NULL_COLUMN", table.RelativeAuthoringPath, string.Empty,
                        "Column descriptor cannot be null.");
                var columns = table.Columns.Where(value => value != null).ToArray();
                foreach (var column in columns)
                {
                    if (string.IsNullOrWhiteSpace(column.ColumnName))
                        Add(errors, "MISSING_COLUMN_NAME", table.RelativeAuthoringPath,
                            column.ColumnName, "Column name is required.");
                    if (column.ColumnOrder <= 0)
                        Add(errors, "INVALID_COLUMN_ORDER", table.RelativeAuthoringPath,
                            column.ColumnName, "Column order must be positive.");
                    if (column.PrimaryKeyOrder.HasValue && !column.IsRequired)
                        Add(errors, "PRIMARY_KEY_NOT_REQUIRED", table.RelativeAuthoringPath,
                            column.ColumnName, "Primary-key columns must be required.");
                    if (column.AllowedValues.Any(string.IsNullOrEmpty) ||
                        column.AllowedValues.Distinct(StringComparer.Ordinal).Count() != column.AllowedValues.Count)
                        Add(errors, "INVALID_ALLOWED_VALUES", table.RelativeAuthoringPath,
                            column.ColumnName, "Allowed values must be non-empty ordinal-unique tokens.");
                }

                AddColumnDuplicates(table, columns, value => value.ColumnName,
                    "DUPLICATE_COLUMN_NAME", errors);
                AddColumnDuplicates(table, columns, value => value.ColumnOrder.ToString(),
                    "DUPLICATE_COLUMN_ORDER", errors);
                foreach (var group in columns.GroupBy(value => value.ColumnName, StringComparer.OrdinalIgnoreCase)
                             .Where(value => value.Select(column => column.ColumnName)
                                 .Distinct(StringComparer.Ordinal).Count() > 1))
                {
                    foreach (var column in group)
                        Add(errors, "COLUMN_CASE_COLLISION", table.RelativeAuthoringPath,
                            column.ColumnName, "Column names collide under ordinal-ignore-case comparison.");
                }

                var columnOrders = columns.Select(value => value.ColumnOrder).Distinct().OrderBy(value => value).ToArray();
                if (!IsContiguous(columnOrders))
                    Add(errors, "NON_CONTIGUOUS_COLUMN_ORDER", table.RelativeAuthoringPath,
                        string.Empty, "Column order must be contiguous from one.");

                var primaryKeys = columns.Where(value => value.PrimaryKeyOrder.HasValue).ToArray();
                if (primaryKeys.Length == 0)
                    Add(errors, "MISSING_PRIMARY_KEY", table.RelativeAuthoringPath,
                        string.Empty, "Every table requires at least one primary-key column.");
                foreach (var duplicate in primaryKeys.GroupBy(value => value.PrimaryKeyOrder.Value)
                             .Where(value => value.Count() > 1))
                    foreach (var column in duplicate)
                        Add(errors, "DUPLICATE_PRIMARY_KEY_ORDER", table.RelativeAuthoringPath,
                            column.ColumnName, "Primary-key order must be unique.");
                var primaryKeyOrders = primaryKeys.Select(value => value.PrimaryKeyOrder.Value)
                    .Distinct().OrderBy(value => value).ToArray();
                if (primaryKeys.Length > 0 && !IsContiguous(primaryKeyOrders))
                    Add(errors, "NON_CONTIGUOUS_PRIMARY_KEY_ORDER", table.RelativeAuthoringPath,
                        string.Empty, "Primary-key order must be contiguous from one.");
            }
        }

        private static void ValidateForeignKeys(
            IReadOnlyList<V2AuthoringTableDescriptor> tables,
            CsvSchemaCatalog legacyCatalog,
            ICollection<V2AuthoringSchemaValidationError> errors)
        {
            var v2Files = tables.GroupBy(value => value.FileName, StringComparer.Ordinal)
                .Where(value => value.Count() == 1)
                .ToDictionary(value => value.Key, value => value.Single(), StringComparer.Ordinal);
            var edgeKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var table in tables)
            foreach (var column in table.Columns.Where(value => value != null && value.ForeignKey != null))
            {
                var foreignKey = column.ForeignKey;
                var edgeKey = table.FileName + "\0" + column.ColumnName + "\0" + foreignKey.TargetDomain +
                              "\0" + foreignKey.TargetFileName + "\0" + foreignKey.TargetColumnName;
                if (!edgeKeys.Add(edgeKey))
                    Add(errors, "DUPLICATE_FOREIGN_KEY", table.RelativeAuthoringPath,
                        column.ColumnName, "Foreign-key edge is duplicated.");

                if (!Enum.IsDefined(typeof(V2AuthoringSchemaDomain), foreignKey.TargetDomain))
                {
                    Add(errors, "INVALID_FOREIGN_KEY_DOMAIN", table.RelativeAuthoringPath,
                        column.ColumnName, "Foreign-key target domain is unknown.");
                    continue;
                }
                if (foreignKey.TargetDomain == V2AuthoringSchemaDomain.Generated)
                {
                    Add(errors, "GENERATED_FOREIGN_KEY_TARGET", table.RelativeAuthoringPath,
                        column.ColumnName, "Authoring schemas cannot target Generated artifacts.");
                    continue;
                }
                if (foreignKey.TargetDomain == V2AuthoringSchemaDomain.AuthoringV2)
                {
                    if (!v2Files.TryGetValue(foreignKey.TargetFileName, out var targetTable))
                    {
                        Add(errors, "MISSING_V2_FOREIGN_KEY_TARGET", table.RelativeAuthoringPath,
                            column.ColumnName, "V2 target file does not exist: " + foreignKey.TargetFileName);
                        continue;
                    }
                    var target = targetTable.Columns.FirstOrDefault(value => value != null &&
                        string.Equals(value.ColumnName, foreignKey.TargetColumnName, StringComparison.Ordinal));
                    if (target == null)
                        Add(errors, "MISSING_V2_FOREIGN_KEY_COLUMN", table.RelativeAuthoringPath,
                            column.ColumnName, "V2 target column does not exist: " + foreignKey.TargetColumnName);
                    else if (!target.PrimaryKeyOrder.HasValue)
                        Add(errors, "V2_FOREIGN_KEY_TARGET_NOT_PRIMARY_KEY", table.RelativeAuthoringPath,
                            column.ColumnName, "V2 target column must be a primary-key column.");
                    continue;
                }

                if (!IsApprovedLegacyEdge(table.FileName, column.ColumnName, foreignKey))
                {
                    Add(errors, "UNAPPROVED_LEGACY_FOREIGN_KEY", table.RelativeAuthoringPath,
                        column.ColumnName, "Only the two approved MAP07/MAP08 provenance edges are allowed.");
                    continue;
                }
                if (legacyCatalog == null || !legacyCatalog.TryGetFile(foreignKey.TargetFileName, out var legacyFile))
                {
                    Add(errors, "MISSING_LEGACY_FOREIGN_KEY_TARGET", table.RelativeAuthoringPath,
                        column.ColumnName, "Legacy target file does not exist in the approved schema catalog.");
                    continue;
                }
                if (!legacyFile.TryGetColumn(foreignKey.TargetColumnName, out var legacyColumn))
                    Add(errors, "MISSING_LEGACY_FOREIGN_KEY_COLUMN", table.RelativeAuthoringPath,
                        column.ColumnName, "Legacy target column does not exist.");
                else if (!legacyColumn.PrimaryKeyOrder.HasValue)
                    Add(errors, "LEGACY_FOREIGN_KEY_TARGET_NOT_PRIMARY_KEY", table.RelativeAuthoringPath,
                        column.ColumnName, "Legacy target column must be a primary-key column.");
            }
        }

        private static void ValidateCycles(
            IReadOnlyList<V2AuthoringTableDescriptor> tables,
            ICollection<V2AuthoringSchemaValidationError> errors)
        {
            var graph = tables.GroupBy(value => value.FileName, StringComparer.Ordinal)
                .Where(value => value.Count() == 1)
                .ToDictionary(
                value => value.Key,
                value => value.Single().Columns.Where(column => column != null &&
                                                                column.ForeignKey != null &&
                                                                column.ForeignKey.TargetDomain == V2AuthoringSchemaDomain.AuthoringV2)
                    .Select(column => column.ForeignKey.TargetFileName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(target => target, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
            var state = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var file in graph.Keys.OrderBy(value => value, StringComparer.Ordinal))
                Visit(file, graph, state, tables, errors);
        }

        private static void Visit(
            string file,
            IReadOnlyDictionary<string, string[]> graph,
            IDictionary<string, int> state,
            IReadOnlyList<V2AuthoringTableDescriptor> tables,
            ICollection<V2AuthoringSchemaValidationError> errors)
        {
            if (state.TryGetValue(file, out var existing) && existing != 0) return;
            state[file] = 1;
            if (graph.TryGetValue(file, out var targets))
            foreach (var target in targets)
            {
                if (!graph.ContainsKey(target)) continue;
                if (state.TryGetValue(target, out var targetState) && targetState == 1)
                {
                    var table = tables.First(value => value.FileName == file);
                    Add(errors, "V2_FOREIGN_KEY_CYCLE", table.RelativeAuthoringPath,
                        string.Empty, "V2 foreign-key cycle reaches " + target + ".");
                }
                else if (!state.TryGetValue(target, out targetState) || targetState == 0)
                {
                    Visit(target, graph, state, tables, errors);
                }
            }
            state[file] = 2;
        }

        private static bool IsApprovedLegacyEdge(
            string sourceFile,
            string sourceColumn,
            V2AuthoringForeignKey foreignKey)
        {
            if (!string.Equals(sourceFile, "terrain_cluster_cells_v2.csv", StringComparison.Ordinal))
                return false;
            return string.Equals(sourceColumn, "source_microchunk_id", StringComparison.Ordinal) &&
                   string.Equals(foreignKey.TargetFileName, "microchunk_catalog.csv", StringComparison.Ordinal) &&
                   string.Equals(foreignKey.TargetColumnName, "microchunk_id", StringComparison.Ordinal) ||
                   string.Equals(sourceColumn, "source_boundary_chunk_id", StringComparison.Ordinal) &&
                   string.Equals(foreignKey.TargetFileName, "boundary_chunk_catalog.csv", StringComparison.Ordinal) &&
                   string.Equals(foreignKey.TargetColumnName, "boundary_chunk_id", StringComparison.Ordinal);
        }

        private static bool IsContiguous(IReadOnlyList<int> values)
        {
            if (values.Count == 0) return false;
            for (var index = 0; index < values.Count; index++)
                if (values[index] != index + 1) return false;
            return true;
        }

        private static void AddDuplicates(
            IEnumerable<V2AuthoringTableDescriptor> tables,
            Func<V2AuthoringTableDescriptor, string> selector,
            string code,
            ICollection<V2AuthoringSchemaValidationError> errors)
        {
            foreach (var group in tables.GroupBy(selector, StringComparer.Ordinal).Where(value => value.Count() > 1))
            foreach (var table in group)
                Add(errors, code, table.RelativeAuthoringPath, string.Empty,
                    "Duplicate exact table identity: " + group.Key);
        }

        private static void AddCaseCollisions(
            IEnumerable<V2AuthoringTableDescriptor> tables,
            Func<V2AuthoringTableDescriptor, string> selector,
            string code,
            ICollection<V2AuthoringSchemaValidationError> errors)
        {
            foreach (var group in tables.GroupBy(selector, StringComparer.OrdinalIgnoreCase)
                         .Where(value => value.Select(selector).Distinct(StringComparer.Ordinal).Count() > 1))
            foreach (var table in group)
                Add(errors, code, table.RelativeAuthoringPath, string.Empty,
                    "Table identities collide under ordinal-ignore-case comparison.");
        }

        private static void AddColumnDuplicates(
            V2AuthoringTableDescriptor table,
            IEnumerable<V2AuthoringColumnDescriptor> columns,
            Func<V2AuthoringColumnDescriptor, string> selector,
            string code,
            ICollection<V2AuthoringSchemaValidationError> errors)
        {
            foreach (var group in columns.GroupBy(selector, StringComparer.Ordinal).Where(value => value.Count() > 1))
            foreach (var column in group)
                Add(errors, code, table.RelativeAuthoringPath, column.ColumnName,
                    "Duplicate exact column identity: " + group.Key);
        }

        private static V2AuthoringSchemaValidationResult Failure(
            IEnumerable<V2AuthoringSchemaValidationError> errors)
        {
            var ordered = errors.Distinct()
                .OrderBy(value => value, Comparer<V2AuthoringSchemaValidationError>.Create(
                    V2AuthoringSchemaValidationError.Compare));
            return new V2AuthoringSchemaValidationResult(null, ordered);
        }

        private static void Add(
            ICollection<V2AuthoringSchemaValidationError> errors,
            string code,
            string tablePath,
            string columnName,
            string message)
        {
            errors.Add(new V2AuthoringSchemaValidationError(code, tablePath, columnName, message));
        }
    }
}
