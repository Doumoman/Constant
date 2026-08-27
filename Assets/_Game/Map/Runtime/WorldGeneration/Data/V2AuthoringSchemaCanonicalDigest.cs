using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Data
{
    public static class V2AuthoringSchemaCanonicalDigest
    {
        public static string Compute(IEnumerable<V2AuthoringTableDescriptor> sourceTables)
        {
            if (sourceTables == null) throw new ArgumentNullException(nameof(sourceTables));

            var canonical = new StringBuilder();
            foreach (var table in sourceTables.OrderBy(
                         value => value.RelativeAuthoringPath, StringComparer.Ordinal))
            {
                Append(canonical, "TABLE");
                Append(canonical, table.RelativeAuthoringPath);
                Append(canonical, table.TableId);
                Append(canonical, table.Owner.ToString());
                foreach (var column in table.Columns
                             .OrderBy(value => value.ColumnOrder)
                             .ThenBy(value => value.ColumnName, StringComparer.Ordinal))
                {
                    Append(canonical, "COLUMN");
                    Append(canonical, column.ColumnOrder.ToString(CultureInfo.InvariantCulture));
                    Append(canonical, column.ColumnName);
                    Append(canonical, CsvSchemaDataTypes.ToToken(column.DataType));
                    Append(canonical, column.IsRequired ? "1" : "0");
                    Append(canonical, column.DefaultValue);
                    Append(canonical, column.PrimaryKeyOrder.HasValue
                        ? column.PrimaryKeyOrder.Value.ToString(CultureInfo.InvariantCulture)
                        : string.Empty);
                    foreach (var allowedValue in column.AllowedValues.OrderBy(
                                 value => value, StringComparer.Ordinal))
                        Append(canonical, "ALLOWED:" + allowedValue);
                    Append(canonical, "ALLOWED_END");
                    if (column.ForeignKey == null)
                    {
                        Append(canonical, "NO_FK");
                    }
                    else
                    {
                        Append(canonical, column.ForeignKey.TargetDomain.ToString());
                        Append(canonical, column.ForeignKey.TargetFileName);
                        Append(canonical, column.ForeignKey.TargetColumnName);
                    }
                }
            }

            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                return string.Concat(bytes.Select(value => value.ToString("x2")));
            }
        }

        private static void Append(StringBuilder target, string value)
        {
            var safe = value ?? string.Empty;
            target.Append(safe.Length.ToString(CultureInfo.InvariantCulture));
            target.Append(':');
            target.Append(safe);
            target.Append('\n');
        }
    }
}
