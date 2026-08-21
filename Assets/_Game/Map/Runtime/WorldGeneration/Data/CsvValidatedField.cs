using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvValidatedField
    {
        internal CsvValidatedField(
            CsvColumnSchema schema,
            CsvField sourceField,
            string rawValue,
            string effectiveValue,
            bool usedDefault)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            SourceField = sourceField ?? throw new ArgumentNullException(nameof(sourceField));
            RawValue = rawValue ?? throw new ArgumentNullException(nameof(rawValue));
            EffectiveValue = effectiveValue ?? throw new ArgumentNullException(nameof(effectiveValue));
            UsedDefault = usedDefault;
        }

        public CsvColumnSchema Schema { get; }

        public CsvField SourceField { get; }

        public string RawValue { get; }

        public string EffectiveValue { get; }

        public bool UsedDefault { get; }
    }
}
