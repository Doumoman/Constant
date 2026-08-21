using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SeedReplayVerificationResult
    {
        public const string InvalidBundleCode = "INVALID_BUNDLE";
        public const string InvalidManifestCode = "INVALID_MANIFEST";
        public const string ContentHashMismatchCode = "CONTENT_HASH_MISMATCH";
        public const string GeneratorBuildMismatchCode = "GENERATOR_BUILD_MISMATCH";
        public const string ReplayExecutionFailedCode = "REPLAY_EXECUTION_FAILED";
        public const string ArtifactMismatchCode = "ARTIFACT_MISMATCH";

        public SeedReplayVerificationResult(bool succeeded, string code, string message)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (succeeded)
            {
                if (code.Length != 0 || message.Length != 0)
                    throw new ArgumentException("Successful verification must have empty code and message.");
            }
            else
            {
                if (!IsStableFailureCode(code))
                    throw new ArgumentException("Verification failure code is not stable.", nameof(code));
                if (message.Length == 0)
                    throw new ArgumentException("Verification failure message must be non-empty.", nameof(message));
            }
            Succeeded = succeeded;
            Code = code;
            Message = message;
        }

        public bool Succeeded { get; }
        public string Code { get; }
        public string Message { get; }

        public static SeedReplayVerificationResult Success()
        {
            return new SeedReplayVerificationResult(true, string.Empty, string.Empty);
        }

        public static SeedReplayVerificationResult Failure(string code, string message)
        {
            return new SeedReplayVerificationResult(false, code, message);
        }

        private static bool IsStableFailureCode(string code)
        {
            return code == InvalidBundleCode ||
                   code == InvalidManifestCode ||
                   code == ContentHashMismatchCode ||
                   code == GeneratorBuildMismatchCode ||
                   code == ReplayExecutionFailedCode ||
                   code == ArtifactMismatchCode;
        }
    }
}
