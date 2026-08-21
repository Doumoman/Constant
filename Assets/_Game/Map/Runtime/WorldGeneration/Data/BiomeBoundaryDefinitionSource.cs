using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class BiomeBoundaryDefinitionSource
    {
        public BiomeBoundaryDefinitionSource(
            CsvFileSchema schema,
            CsvScalarAndListParseResult parseResult)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
        }

        public string FileName => Schema.FileName;
        public CsvFileSchema Schema { get; }
        public CsvScalarAndListParseResult ParseResult { get; }
    }
}
