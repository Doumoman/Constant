namespace StarNight.Map.WorldGeneration.Data
{
    public enum CsvReadErrorCode
    {
        InvalidUtf8,
        UnsupportedBom,
        BareCarriageReturn,
        UnexpectedQuoteInUnquotedField,
        UnexpectedCharacterAfterClosingQuote,
        UnterminatedQuotedField,
    }
}
