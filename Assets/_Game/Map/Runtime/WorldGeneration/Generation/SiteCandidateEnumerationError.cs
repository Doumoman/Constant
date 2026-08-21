using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum SiteCandidateEnumerationErrorCode
    {
        MissingGrid,
        InvalidGrid,
        MissingWorldProfile,
        MissingGenerationProfile,
        InactiveProfile,
        ProfileWorldMismatch,
        InvalidWorldDimensions,
        InvalidStartRing,
        MissingSpecialMapInput,
        NullSpecialMap,
        DuplicateSpecialMapId,
        MissingRequiredSite,
        UnexpectedRequiredSite,
        SiteRoleMismatch,
        InvalidRequiredCount,
        InvalidSiteDefinition
    }

    public sealed class SiteCandidateEnumerationError
    {
        public SiteCandidateEnumerationError(
            SiteCandidateEnumerationErrorCode errorCode,
            string sourceDefinitionId,
            string message)
        {
            if (!Enum.IsDefined(typeof(SiteCandidateEnumerationErrorCode), errorCode))
                throw new ArgumentOutOfRangeException(nameof(errorCode));
            ReservationValidation.RequireCanonicalId(sourceDefinitionId, nameof(sourceDefinitionId), true);
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.Trim().Length == 0)
                throw new ArgumentException("Error message cannot be empty.", nameof(message));

            ErrorCode = errorCode;
            SourceDefinitionId = sourceDefinitionId;
            Message = message;
        }

        public SiteCandidateEnumerationErrorCode ErrorCode { get; }
        public string SourceDefinitionId { get; }
        public string Message { get; }
    }
}
