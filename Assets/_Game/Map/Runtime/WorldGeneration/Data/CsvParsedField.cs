using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvParsedField
    {
        internal CsvParsedField(CsvValidatedField validatedField, CsvParsedValue value)
        {
            ValidatedField = validatedField ??
                             throw new ArgumentNullException(nameof(validatedField));
            Schema = validatedField.Schema;
            RawValue = validatedField.RawValue;
            EffectiveValue = validatedField.EffectiveValue;
            UsedDefault = validatedField.UsedDefault;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public CsvColumnSchema Schema { get; }

        public CsvValidatedField ValidatedField { get; }

        public string RawValue { get; }

        public string EffectiveValue { get; }

        public bool UsedDefault { get; }

        public CsvParsedValue Value { get; }
    }
}
