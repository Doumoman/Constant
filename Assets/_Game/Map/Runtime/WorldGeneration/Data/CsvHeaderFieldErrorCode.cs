namespace StarNight.Map.WorldGeneration.Data
{
    public enum CsvHeaderFieldErrorCode
    {
        SyntaxReadFailed,
        MissingHeader,
        UnexpectedHeader,
        DuplicateHeader,
        HeaderOrderMismatch,
        FieldCountMismatch,
        RequiredFieldEmpty,
    }
}
