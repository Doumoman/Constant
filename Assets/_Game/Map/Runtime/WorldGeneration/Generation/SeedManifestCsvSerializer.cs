using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.Generation
{
    public static class SeedManifestCsvSerializer
    {
        public const string FileName = "seed_manifest.csv";

        public const string Header =
            "world_profile_id,seed,content_version_hash,generation_profile_id,generator_build_id,approved,generation_started_utc,generation_duration_ms,retry_count_total,failure_rule_ids,notes";

        private const string UtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
        private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static byte[] SerializeHeaderOnly()
        {
            return Encode(Header + "\r\n");
        }

        public static byte[] Serialize(SeedManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            var fields = new[]
            {
                manifest.WorldProfileId,
                manifest.Seed.ToString(CultureInfo.InvariantCulture),
                manifest.ContentVersionHash,
                manifest.GenerationProfileId,
                manifest.GeneratorBuildId,
                manifest.Approved ? "1" : "0",
                manifest.GenerationStartedUtc.ToString(UtcFormat, CultureInfo.InvariantCulture),
                manifest.GenerationDurationMilliseconds.ToString(CultureInfo.InvariantCulture),
                manifest.RetryCountTotal.ToString(CultureInfo.InvariantCulture),
                string.Join("|", manifest.FailureRuleIds),
                manifest.Notes
            };

            var builder = new StringBuilder(Header.Length + 192);
            builder.Append(Header).Append("\r\n");
            AppendRecord(builder, fields);
            builder.Append("\r\n");
            return Encode(builder.ToString());
        }

        public static SeedManifest Deserialize(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            var rows = ParseStrictRows(bytes, nameof(bytes));
            if (rows.Count != 2)
                throw new ArgumentException("Seed manifest must contain exactly a header and one data record.", nameof(bytes));
            if (rows[0].Count != 11 || !string.Equals(string.Join(",", rows[0]), Header, StringComparison.Ordinal))
                throw new ArgumentException("Seed manifest header is not exact.", nameof(bytes));
            if (rows[1].Count != 11)
                throw new ArgumentException("Seed manifest data record must contain exactly 11 fields.", nameof(bytes));

            var values = rows[1];
            if (!ulong.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
                throw new ArgumentException("Seed is not canonical unsigned decimal.", nameof(bytes));
            if (values[5] != "0" && values[5] != "1")
                throw new ArgumentException("Approved must be encoded as 0 or 1.", nameof(bytes));
            if (!DateTimeOffset.TryParseExact(
                    values[6], UtcFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var startedUtc))
                throw new ArgumentException("Generation start timestamp is not canonical UTC.", nameof(bytes));
            if (!int.TryParse(values[7], NumberStyles.None, CultureInfo.InvariantCulture, out var duration))
                throw new ArgumentException("Generation duration is not canonical non-negative Int32.", nameof(bytes));
            if (!int.TryParse(values[8], NumberStyles.None, CultureInfo.InvariantCulture, out var retries))
                throw new ArgumentException("Retry total is not canonical non-negative Int32.", nameof(bytes));

            var failures = values[9].Length == 0
                ? Array.Empty<string>()
                : values[9].Split('|');
            SeedManifest manifest;
            try
            {
                manifest = new SeedManifest(
                    values[0], seed, values[2], values[3], values[4], values[5] == "1",
                    startedUtc, duration, retries, failures, values[10]);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException("Seed manifest contains an invalid field.", nameof(bytes), exception);
            }

            if (!bytes.SequenceEqual(Serialize(manifest)))
                throw new ArgumentException("Seed manifest bytes are not in canonical form.", nameof(bytes));
            return manifest;
        }

        internal static IReadOnlyList<IReadOnlyList<string>> ParseStrictRows(byte[] bytes, string parameterName)
        {
            if (bytes == null) throw new ArgumentNullException(parameterName);
            if (bytes.Length < Utf8Bom.Length ||
                bytes[0] != Utf8Bom[0] || bytes[1] != Utf8Bom[1] || bytes[2] != Utf8Bom[2])
                throw new ArgumentException("CSV must begin with exactly one UTF-8 BOM.", parameterName);

            string text;
            try
            {
                text = StrictUtf8.GetString(bytes, Utf8Bom.Length, bytes.Length - Utf8Bom.Length);
            }
            catch (DecoderFallbackException exception)
            {
                throw new ArgumentException("CSV contains invalid UTF-8.", parameterName, exception);
            }
            if (text.Length == 0 || text[0] == '\ufeff' || text.IndexOf('\ufeff') >= 0)
                throw new ArgumentException("CSV contains an unexpected additional BOM.", parameterName);
            if (!text.EndsWith("\r\n", StringComparison.Ordinal))
                throw new ArgumentException("CSV must end with exactly one CRLF record separator.", parameterName);
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\r' && (index + 1 >= text.Length || text[index + 1] != '\n'))
                    throw new ArgumentException("CSV contains a bare carriage return.", parameterName);
                if (text[index] == '\n' && (index == 0 || text[index - 1] != '\r'))
                    throw new ArgumentException("CSV contains a bare line feed.", parameterName);
            }

            var rows = new List<IReadOnlyList<string>>();
            var fields = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var quotedField = false;
            var closedQuote = false;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (inQuotes)
                {
                    if (character == '"')
                    {
                        if (index + 1 < text.Length && text[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else
                        {
                            inQuotes = false;
                            closedQuote = true;
                        }
                    }
                    else
                    {
                        field.Append(character);
                    }
                    continue;
                }

                if (closedQuote && character != ',' && character != '\r')
                    throw new ArgumentException("CSV has characters after a closing quote.", parameterName);
                if (character == '"')
                {
                    if (field.Length != 0 || quotedField || closedQuote)
                        throw new ArgumentException("CSV contains a malformed quote.", parameterName);
                    quotedField = true;
                    inQuotes = true;
                }
                else if (character == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    quotedField = false;
                    closedQuote = false;
                }
                else if (character == '\r')
                {
                    index++;
                    fields.Add(field.ToString());
                    rows.Add(new List<string>(fields).AsReadOnly());
                    fields.Clear();
                    field.Clear();
                    quotedField = false;
                    closedQuote = false;
                }
                else
                {
                    field.Append(character);
                }
            }
            if (inQuotes || fields.Count != 0 || field.Length != 0 || quotedField || closedQuote)
                throw new ArgumentException("CSV ended with an incomplete record.", parameterName);
            return rows.AsReadOnly();
        }

        private static byte[] Encode(string text)
        {
            var content = StrictUtf8.GetBytes(text);
            var bytes = new byte[Utf8Bom.Length + content.Length];
            Buffer.BlockCopy(Utf8Bom, 0, bytes, 0, Utf8Bom.Length);
            Buffer.BlockCopy(content, 0, bytes, Utf8Bom.Length, content.Length);
            return bytes;
        }

        private static void AppendRecord(StringBuilder builder, IEnumerable<string> fields)
        {
            var first = true;
            foreach (var value in fields)
            {
                if (!first) builder.Append(',');
                first = false;
                AppendField(builder, value);
            }
        }

        private static void AppendField(StringBuilder builder, string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                builder.Append(value);
                return;
            }
            builder.Append('"');
            foreach (var character in value)
            {
                if (character == '"') builder.Append("\"\"");
                else builder.Append(character);
            }
            builder.Append('"');
        }
    }
}
